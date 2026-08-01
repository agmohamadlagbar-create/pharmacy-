using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Helpers;
using OnlineStore.Api.Services;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly INotificationService _notif;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<OnlineStore.Api.Hubs.TrackingHub> _trackingHub;
    private readonly ILogger<OrdersController> _logger;
    private readonly ICacheService _cache;
    private readonly INotificationQueue _notifQueue;

    public OrdersController(ApplicationDbContext db, UserManager<AppUser> userManager, INotificationService notif, Microsoft.AspNetCore.SignalR.IHubContext<OnlineStore.Api.Hubs.TrackingHub> trackingHub, ILogger<OrdersController> logger, ICacheService cache, INotificationQueue notifQueue)
    {
        _db = db;
        _userManager = userManager;
        _notif = notif;
        _trackingHub = trackingHub;
        _logger = logger;
        _cache = cache;
        _notifQueue = notifQueue;
    }

    /// <summary>User – place an order.</summary>
    [Authorize(Roles = "User")]
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest req)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.DeliveryAddress))
            return BadRequest(new { error = "Delivery address is required to place an order" });

        if (req.Items is null || req.Items.Count == 0)
            return BadRequest(new { error = "Order must have at least one item" });

        if (req.Items.Any(i => i.Quantity <= 0))
            return BadRequest(new { error = "All order item quantities must be greater than zero" });

        // Validate all products exist, are approved, and belong to the same store
        var productIds = req.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products
            .AsTracking()
            .Where(p => productIds.Contains(p.Id) && p.Status == ProductStatus.Approved)
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
            return BadRequest(new { error = "One or more products are invalid or not approved" });

        // All products must belong to the specified store
        if (products.Values.Any(p => p.StoreUserId != req.StoreUserId))
            return BadRequest(new { error = "All products must belong to the same store" });

        // Validate city match between store and delivery
        var user = await _userManager.FindByIdAsync(userId);
        var storeUser = await _userManager.FindByIdAsync(req.StoreUserId);
        if (user != null && storeUser != null)
        {
            if (!string.Equals(user.City, storeUser.City, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = $"Store '{storeUser.FullName ?? storeUser.UserName}' does not deliver to your city ({user.City}). They only deliver in {storeUser.City}." });
            }
        }

        var order = new Order
        {
            UserId = userId,
            StoreUserId = req.StoreUserId,
            Status = OrderStatus.Pending,
            DeliveryAddress = req.DeliveryAddress,
            PaymentMethod = req.PaymentMethod,
            OfferCode = req.OfferCode,
            OrderGroupId = "G-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        decimal total = 0;
        foreach (var item in req.Items)
        {
            var product = products[item.ProductId];
            if (product.StockQuantity < item.Quantity)
                return BadRequest(new { error = $"Not enough stock for {product.Name}" });

            product.StockQuantity -= item.Quantity;
            product.UpdatedAtUtc = DateTime.UtcNow;

            var orderItem = new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            };
            order.Items.Add(orderItem);
            total += product.Price * item.Quantity;
        }
        order.TotalAmount = total;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        if ((req.PaymentMethod == "Credit Card" || req.PaymentMethod == "Credit/Debit Card") && !string.IsNullOrEmpty(req.PaymentToken))
        {
            var userEntity = await _db.Users.FindAsync(userId);
            try 
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(total * 100),
                    Currency = "usd",
                    Customer = userEntity?.StripeCustomerId,
                    PaymentMethod = req.PaymentToken,
                    Confirm = true,
                    OffSession = true, // Required for saved card on-session confirm=true in some cases
                    Description = $"Order #{order.Id} for {userEntity?.Email}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "OrderId", order.Id.ToString() },
                        { "UserId", userId },
                        { "UserEmail", userEntity?.Email ?? "" }
                    },
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    }
                };
                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                if (paymentIntent.Status != "succeeded")
                {
                    order.Status = OrderStatus.Cancelled;
                    await _db.SaveChangesAsync();
                    return BadRequest(new { error = "Payment failed. Status: " + paymentIntent.Status });
                }
            }
            catch (StripeException e)
            {
                order.Status = OrderStatus.Cancelled;
                await _db.SaveChangesAsync();
                return BadRequest(new { error = e.StripeError.Message });
            }
        }

        // Notify the store
        await _notif.NotifyUserAsync(req.StoreUserId, "New Order", $"You have received a new order #{order.Id}.");

        var dtoResult = await BuildOrderDto(order.Id);
        if (dtoResult == null) return StatusCode(500, new { error = "Order created but failed to retrieve details." });
        return Ok(dtoResult);
    }

    [Authorize(Roles = "User")]
    [HttpPost("checkout-cart")]
    public async Task<ActionResult> CheckoutCart(CheckoutCartRequest req)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.DeliveryAddress))
            return BadRequest(new { error = "Delivery address is required" });

        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || cart.Items.Count == 0)
            return BadRequest(new { error = "Cart is empty" });

        var invalidItem = cart.Items.FirstOrDefault(i =>
            i.Quantity <= 0 ||
            i.Product is null ||
            i.Product.Status != ProductStatus.Approved ||
            i.Product.StockQuantity < i.Quantity);
        if (invalidItem != null)
            return BadRequest(new { error = $"Product '{invalidItem.Product?.Name ?? "Unknown"}' is not available or out of stock." });

        decimal totalAmount = cart.Items.Sum(i => i.Product!.Price * i.Quantity);

        var userEntity = await _db.Users.FindAsync(userId);
        
        var groupedItems = cart.Items.GroupBy(i => i.Product!.StoreUserId).ToList();
        
        // Batch-load store users to validate cities (avoids N+1 inside loop)
        var storeUserIds = groupedItems.Select(g => g.Key).ToList();
        var storeUsersDict = new Dictionary<string, AppUser>();
        foreach (var sid in storeUserIds)
        {
            var su = await _userManager.FindByIdAsync(sid);
            if (su != null) storeUsersDict[sid] = su;
        }

        // Validate cities for all stores in the cart
        // Use the delivery city from the selected address, fall back to profile city
        var deliveryCity = !string.IsNullOrWhiteSpace(req.DeliveryCity) ? req.DeliveryCity.Trim() : userEntity?.City;
        foreach (var group in groupedItems)
        {
            if (storeUsersDict.TryGetValue(group.Key, out var storeUser) && !string.IsNullOrWhiteSpace(deliveryCity))
            {
                if (!string.Equals(deliveryCity, storeUser.City, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = $"One or more items in your cart belong to a store in {storeUser.City}, which does not deliver to {deliveryCity}." });
                }
            }
        }

        var orders = new List<Order>();
        var groupId = "G-" + Guid.NewGuid().ToString("N")[..8].ToUpper();

        foreach (var group in groupedItems)
        {
            var storeUserId = group.Key;
            decimal storeTotal = group.Sum(i => i.Product!.Price * i.Quantity);

            var order = new Order
            {
                UserId = userId,
                StoreUserId = storeUserId,
                Status = OrderStatus.Pending,
                DeliveryAddress = req.DeliveryAddress,
                PaymentMethod = req.PaymentMethod,
                OfferCode = req.OfferCode,
                OrderGroupId = groupId,
                TotalAmount = storeTotal,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            foreach (var item in group)
            {
                var product = item.Product!;
                product.StockQuantity -= item.Quantity;
                product.UpdatedAtUtc = DateTime.UtcNow;

                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product!.Price
                });
            }

            orders.Add(order);
        }

        _db.Orders.AddRange(orders);
        await _db.SaveChangesAsync();

        if ((req.PaymentMethod == "Credit Card" || req.PaymentMethod == "Credit/Debit Card") && !string.IsNullOrEmpty(req.PaymentToken))
        {
            try
            {
                if (userEntity != null && string.IsNullOrEmpty(userEntity.StripeCustomerId))
                {
                    var custService = new CustomerService();
                    var customer = await custService.CreateAsync(new CustomerCreateOptions { Email = userEntity.Email, Name = userEntity.FullName });
                    userEntity.StripeCustomerId = customer.Id;
                    await _db.SaveChangesAsync();
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(totalAmount * 100),
                    Currency = "usd",
                    Customer = userEntity?.StripeCustomerId,
                    PaymentMethod = req.PaymentToken,
                    Confirm = true,
                    OffSession = true, // Required for saved card
                    Description = $"Cart Checkout for {userEntity?.Email}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "OrderIds", string.Join(",", orders.Select(o => o.Id)) },
                        { "UserId", userId },
                        { "UserEmail", userEntity?.Email ?? "" }
                    },
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    }
                };
                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                if (paymentIntent.Status != "succeeded")
                {
                    RestoreCartStock(cart);
                    CancelOrders(orders);
                    await _db.SaveChangesAsync();
                    return BadRequest(new { error = "Payment failed. Status: " + paymentIntent.Status });
                }
            }
            catch (StripeException e)
            {
                RestoreCartStock(cart);
                CancelOrders(orders);
                await _db.SaveChangesAsync();
                return BadRequest(new { error = e.StripeError?.Message ?? e.Message });
            }
            catch (Exception ex)
            {
                RestoreCartStock(cart);
                CancelOrders(orders);
                await _db.SaveChangesAsync();
                return BadRequest(new { error = "Payment failed: " + ex.Message });
            }
        }

        foreach (var order in orders)
        {
            await _notif.NotifyUserAsync(order.StoreUserId, "New Order", $"You have received a new order #{order.Id}.");
        }

        cart.Items.Clear();
        await _db.SaveChangesAsync();

        return Ok(new MessageResponse("Orders placed successfully"));

        static void CancelOrders(IEnumerable<Order> ordersToCancel)
        {
            foreach (var order in ordersToCancel)
            {
                order.Status = OrderStatus.Cancelled;
                order.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        static void RestoreCartStock(Cart sourceCart)
        {
            foreach (var item in sourceCart.Items)
            {
                if (item.Product == null) continue;
                item.Product.StockQuantity += item.Quantity;
                item.Product.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
    }

    /// <summary>Get orders – User sees own, Store sees their store's, Admin sees all.</summary>
    [Authorize(Roles = "User,Shop,Admin,Delivery")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 9,
        [FromQuery] string? status = null,
        [FromQuery(Name = "_")] string? cacheBust = null)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        // Short-term cache to avoid hammering DB on repeated page loads
        var role = User.IsInRole("Admin") ? "admin" : User.IsInRole("Shop") ? "shop" : User.IsInRole("Delivery") ? "delivery" : "user";
        var cacheKey = $"orders_{role}_{userId}_{page}_{limit}_{status ?? "all"}";
        var bypassCache = !string.IsNullOrWhiteSpace(cacheBust);
        if (!bypassCache)
        {
            var cached = await _cache.GetAsync<PaginatedResponse<OrderDto>>(cacheKey);
            if (cached != null) return Ok(cached);
        }

        // Use a lightweight base query without Include — projection will handle joins
        IQueryable<Order> query = _db.Orders.AsNoTracking();

        if (User.IsInRole("Admin"))
        {
            // Admin sees all
        }
        else if (User.IsInRole("Shop"))
        {
            query = query.Where(o => o.StoreUserId == userId);
        }
        else if (User.IsInRole("Delivery"))
        {
            query = query.Where(o => _db.DeliveryJobs.Any(j =>
                j.DeliveryUserId == userId &&
                (j.OrderId == o.Id ||
                 (o.OrderGroupId != null && j.Order.OrderGroupId == o.OrderGroupId))));
        }
        else // User
        {
            query = query.Where(o => o.UserId == userId);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            query = query.Where(o => o.Status == orderStatus);

            // Customers can only see delivered orders from the last 7 days
            if (orderStatus == OrderStatus.Delivered && !User.IsInRole("Admin") && !User.IsInRole("Shop"))
            {
                var deliveredCutoff = DateTime.UtcNow.AddDays(-7);
                query = query.Where(o => o.UpdatedAtUtc >= deliveredCutoff);
            }
        }

        var total = await query.CountAsync();

        // Project only the exact columns needed for the DTO — avoids loading full Product entities
        var rawOrders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(o => new {
                o.Id,
                o.UserId,
                o.StoreUserId,
                StoreName = _db.ShopProfiles.Where(s => s.UserId == o.StoreUserId).Select(s => s.ShopName).FirstOrDefault(),
                Status = o.Status.ToString(),
                o.TotalAmount,
                DeliveryAddress = o.DeliveryAddress ?? "",
                o.PaymentMethod,
                o.OfferCode,
                Items = o.Items.Select(i => new {
                    i.Id,
                    i.ProductId,
                    ProductName = i.Product != null ? i.Product.Name : "",
                    i.Quantity,
                    i.UnitPrice,
                    StoreUserId = i.Product != null ? i.Product.StoreUserId : o.StoreUserId
                }).ToList(),
                o.CreatedAtUtc,
                o.UpdatedAtUtc,
                o.OrderGroupId,
                StoreCountry = _db.Users.Where(u => u.Id == o.StoreUserId).Select(u => u.Country).FirstOrDefault(),
                CustomerName = _db.Users.Where(u => u.Id == o.UserId).Select(u => u.FullName).FirstOrDefault(),
                CustomerAvatarUrl = _db.Users.Where(u => u.Id == o.UserId).Select(u => u.ProfilePictureUrl).FirstOrDefault()
            })
            .ToListAsync();

        var dtos = rawOrders.Select(o => {
            var currencyCode = Helpers.CurrencyHelper.GetCurrencyCode(o.StoreCountry);
            return new OrderDto(
                o.Id,
                o.UserId,
                o.StoreUserId,
                o.StoreName,
                o.Status,
                o.TotalAmount,
                o.DeliveryAddress,
                o.PaymentMethod,
                o.OfferCode,
                o.Items.Select(i => new OrderItemDto(
                    i.Id,
                    i.ProductId,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice,
                    i.StoreUserId,
                    currencyCode
                )).ToList(),
                o.CreatedAtUtc,
                o.UpdatedAtUtc,
                o.OrderGroupId,
                null, null, null, null,
                currencyCode,
                o.CustomerName,
                o.CustomerAvatarUrl
            );
        }).ToList();

        var result = PaginatedResponse<OrderDto>.Create(dtos, page, limit, total);
        if (!bypassCache)
        {
            await _cache.SetAsync(cacheKey, result, absoluteExpiration: TimeSpan.FromSeconds(30));
        }
        return Ok(result);
    }

    /// <summary>Get store order stats (Today's Revenue and Active Orders Count).</summary>
    [Authorize(Roles = "Shop,Admin")]
    [HttpGet("stats")]
    public async Task<ActionResult<OrderStatsDto>> GetOrderStats([FromQuery(Name = "_")] string? cacheBust = null)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        IQueryable<Order> query = _db.Orders.AsNoTracking();

        if (User.IsInRole("Shop"))
        {
            query = query.Where(o => o.StoreUserId == userId);
        }

        var todayUtcStart = DateTime.UtcNow.Date;

        var revenueStatuses = new[] { OrderStatus.Delivered, OrderStatus.PickedUp, OrderStatus.OnTheWay };
        var activeStatuses = new[] { OrderStatus.Pending, OrderStatus.Preparing, OrderStatus.ReadyForPickup };

        var todayRevenue = await query
            .Where(o => revenueStatuses.Contains(o.Status) && o.CreatedAtUtc >= todayUtcStart)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var activeCount = await query
            .Where(o => activeStatuses.Contains(o.Status))
            .CountAsync();

        return Ok(new OrderStatsDto(todayRevenue, activeCount, "14m", "0.4%"));
    }

    /// <summary>Get active grouped orders for customer home page tracking.</summary>
    [Authorize(Roles = "User")]
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveOrders()
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var recentlyDelivered = DateTime.UtcNow.AddSeconds(-30);

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId && (
                o.Status == OrderStatus.Pending ||
                o.Status == OrderStatus.Preparing ||
                o.Status == OrderStatus.ReadyForPickup ||
                o.Status == OrderStatus.PickedUp ||
                o.Status == OrderStatus.OnTheWay ||
                (o.Status == OrderStatus.Delivered && o.UpdatedAtUtc > recentlyDelivered)
            ))
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync();

        if (orders.Count == 0) return Ok(new List<GroupedOrderDto>());

        var orderGroupIds = orders
            .Select(OrderGroupHelper.ResolveGroupId)
            .Distinct()
            .ToList();

        var deliveryJobs = await _db.DeliveryJobs
            .AsNoTracking()
            .Include(j => j.Order)
            .Where(j => j.Order.OrderGroupId != null && orderGroupIds.Contains(j.Order.OrderGroupId))
            .ToListAsync();

        var deliveryJobsByGroup = deliveryJobs
            .GroupBy(j => OrderGroupHelper.ResolveGroupId(j.Order))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(j => j.CreatedAtUtc).First());

        var groupedDtos = await BuildGroupedOrderDtosAsync(orders, deliveryJobsByGroup);

        return Ok(groupedDtos
            .OrderByDescending(group => group.CreatedAtUtc)
            .ToList());
    }

    [Authorize(Roles = "User,Shop,Admin,Delivery")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var dto = await BuildOrderDto(id);
        if (dto is null) return NotFound();

        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        if (User.IsInRole("Admin"))
            return Ok(dto);

        if (User.IsInRole("Delivery"))
        {
            var canAccess = await CanDeliveryDriverAccessOrderAsync(userId, dto.Id, dto.OrderGroupId);
            if (!canAccess)
                return Forbid();

            return Ok(dto);
        }

        if (dto.UserId != userId && dto.StoreUserId != userId)
            return Forbid();

        return Ok(dto);
    }

    /// <summary>Store – update order status: Pending→Preparing→ReadyForPickup.</summary>
    [Authorize(Roles = "Shop")]
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest req)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var order = await _db.Orders
            .AsTracking()
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();
        if (order.StoreUserId != userId) return Forbid();

        if (!Enum.TryParse<OrderStatus>(req.Status, true, out var newStatus))
            return BadRequest(new { error = "Invalid status" });

        // Validate allowed transitions for Store
        var allowedTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = [OrderStatus.Preparing],
            [OrderStatus.Preparing] = [OrderStatus.ReadyForPickup]
        };

        if (!allowedTransitions.TryGetValue(order.Status, out var allowed) || !allowed.Contains(newStatus))
            return BadRequest(new { error = $"Cannot transition from {order.Status} to {newStatus}" });

        order.Status = newStatus;
        order.UpdatedAtUtc = DateTime.UtcNow;

        DeliveryJob? deliveryJob = null;
        if (newStatus == OrderStatus.ReadyForPickup)
        {
            var orderGroupId = OrderGroupHelper.ResolveGroupId(order);
            var groupedOrders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.OrderGroupId == order.OrderGroupId)
                .ToListAsync();

            var allReadyForPickup = groupedOrders.Count > 0 && groupedOrders.All(o => o.Status == OrderStatus.ReadyForPickup);
            var existingJob = await _db.DeliveryJobs
                .AsNoTracking()
                .AnyAsync(j => j.Order.OrderGroupId == order.OrderGroupId);

            if (allReadyForPickup && !existingJob)
            {
                var representativeOrder = groupedOrders
                    .OrderBy(o => o.CreatedAtUtc)
                    .ThenBy(o => o.Id)
                    .First();

                deliveryJob = new DeliveryJob
                {
                    OrderId = representativeOrder.Id,
                    Status = DeliveryStatus.Unassigned,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _db.DeliveryJobs.Add(deliveryJob);
            }
        }

        await _db.SaveChangesAsync();

        // Enqueue notifications out-of-band for instant HTTP response (<50ms)
        await _notifQueue.QueueNotificationAsync(new NotificationWorkItem(
            NotificationTaskType.StoreOrderStatusUpdate,
            order.Id,
            order.OrderGroupId,
            order.UserId,
            newStatus.ToString(),
            deliveryJob?.Id
        ));

        return Ok(new MessageResponse($"Order status updated to {newStatus}"));
    }

    /// <summary>Customer - cancel an order if not ready.</summary>
    [Authorize(Roles = "User")]
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();

        if (order.UserId != userId)
            return Forbid();

        var orderGroupId = OrderGroupHelper.ResolveGroupId(order);

        List<Order> groupedOrders;
        if (string.IsNullOrWhiteSpace(order.OrderGroupId))
        {
            groupedOrders = new List<Order>
            {
                await _db.Orders
                    .AsTracking()
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                    .FirstAsync(o => o.Id == id)
            };
        }
        else
        {
            groupedOrders = await _db.Orders
                .AsTracking()
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId && o.OrderGroupId == order.OrderGroupId)
                .ToListAsync();
        }

        if (groupedOrders.Any(o => o.Status != OrderStatus.Pending && o.Status != OrderStatus.Preparing))
            return BadRequest(new { error = "This grouped order cannot be cancelled at this stage." });

        var totalRefundAmount = groupedOrders.Sum(o => o.TotalAmount);
        var relatedOrderIds = groupedOrders.Select(o => o.Id.ToString()).ToHashSet();
        var representativeOrder = groupedOrders
            .OrderBy(o => o.CreatedAtUtc)
            .ThenBy(o => o.Id)
            .First();

        var strategy = _db.Database.CreateExecutionStrategy();
        string? stripeError = null;
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                if ((representativeOrder.PaymentMethod == "Credit Card" || representativeOrder.PaymentMethod == "Credit/Debit Card"))
                {
                    try
                    {
                        var userEntity = await _db.Users.FindAsync(userId);
                        if (userEntity != null && !string.IsNullOrEmpty(userEntity.StripeCustomerId))
                        {
                            var service = new PaymentIntentService();
                            var piOptions = new PaymentIntentListOptions { Customer = userEntity.StripeCustomerId };
                            var paymentIntents = await service.ListAsync(piOptions);

                            var pi = paymentIntents.FirstOrDefault(p =>
                                (p.Metadata.ContainsKey("OrderId") && relatedOrderIds.Contains(p.Metadata["OrderId"])) ||
                                (p.Metadata.ContainsKey("OrderIds") && p.Metadata["OrderIds"].Split(',').Any(relatedOrderIds.Contains))
                            );

                            if (pi != null && pi.Status == "succeeded")
                            {
                                var refundService = new RefundService();
                                var refundOptions = new RefundCreateOptions
                                {
                                    PaymentIntent = pi.Id,
                                    Amount = (long)(totalRefundAmount * 0.95m * 100)
                                };
                                await refundService.CreateAsync(refundOptions);
                            }
                        }
                    }
                    catch (StripeException e)
                    {
                        await transaction.RollbackAsync();
                        stripeError = "Failed to process refund: " + e.StripeError.Message;
                        return; // Exit the strategy lambda; we'll check stripeError outside
                    }
                }

                // Batch-load all products referenced in order items (avoids N+1 FindAsync per item)
                var allProductIds = groupedOrders
                    .SelectMany(o => o.Items)
                    .Where(i => i.ProductId.HasValue)
                    .Select(i => i.ProductId!.Value)
                    .Distinct()
                    .ToList();
                var productsDict = await _db.Products
                    .AsTracking()
                    .Where(p => allProductIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                foreach (var groupedOrder in groupedOrders)
                {
                    foreach (var item in groupedOrder.Items)
                    {
                        if (!item.ProductId.HasValue) continue;

                        if (productsDict.TryGetValue(item.ProductId.Value, out var product))
                        {
                            product.StockQuantity += item.Quantity;
                            product.UpdatedAtUtc = DateTime.UtcNow;
                        }
                    }

                    groupedOrder.Status = OrderStatus.Cancelled;
                    groupedOrder.CancellationReason = "Cancelled by customer";
                    groupedOrder.CancelledByRole = "Customer";
                    groupedOrder.CancelledAtUtc = DateTime.UtcNow;
                    groupedOrder.UpdatedAtUtc = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        if (stripeError != null)
        {
            return BadRequest(new { error = stripeError });
        }

        foreach (var groupedOrder in groupedOrders)
        {
            await _notif.NotifyUserAsync(groupedOrder.StoreUserId, "Order Cancelled", $"Order #{groupedOrder.Id} from group #{orderGroupId} was cancelled by the customer.");
            await _notif.NotifyOrderStatusChangedAsync(groupedOrder.UserId, groupedOrder.Id, "Cancelled");

            await _trackingHub.Clients.Group($"order_{groupedOrder.Id}")
                .SendAsync("order:status_updated", new
                {
                    orderId = groupedOrder.Id,
                    orderGroupId,
                    status = "Cancelled",
                    cancellationReason = "Cancelled by customer",
                    cancelledByRole = "Customer"
                });
        }

        await BroadcastGroupedOrderStatusAsync(groupedOrders);

        return Ok(new MessageResponse("Order cancelled successfully"));
    }

    /// <summary>Store - decline/cancel an order during Pending status.</summary>
    [Authorize(Roles = "Shop")]
    [HttpPut("{id:int}/shop-cancel")]
    public async Task<IActionResult> ShopCancelOrder(int id, [FromBody] ShopCancelRequest req)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.CancellationReason))
            return BadRequest(new { error = "Cancellation reason is required." });

        var order = await _db.Orders
            .AsTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();
        if (order.StoreUserId != userId) return Forbid();

        if (order.Status != OrderStatus.Pending)
            return BadRequest(new { error = "Orders can only be declined by the store while in Pending status." });

        var orderGroupId = OrderGroupHelper.ResolveGroupId(order);
        var refundAmount = order.TotalAmount;

        var strategy = _db.Database.CreateExecutionStrategy();
        string? stripeError = null;

        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                if (order.PaymentMethod == "Credit Card" || order.PaymentMethod == "Credit/Debit Card")
                {
                    try
                    {
                        var userEntity = await _db.Users.FindAsync(order.UserId);
                        if (userEntity != null && !string.IsNullOrEmpty(userEntity.StripeCustomerId))
                        {
                            var service = new PaymentIntentService();
                            var piOptions = new PaymentIntentListOptions { Customer = userEntity.StripeCustomerId };
                            var paymentIntents = await service.ListAsync(piOptions);

                            var pi = paymentIntents.FirstOrDefault(p =>
                                (p.Metadata.ContainsKey("OrderId") && p.Metadata["OrderId"] == order.Id.ToString()) ||
                                (p.Metadata.ContainsKey("OrderIds") && p.Metadata["OrderIds"].Split(',').Contains(order.Id.ToString()))
                            );

                            if (pi != null && pi.Status == "succeeded")
                            {
                                var refundService = new RefundService();
                                var refundOptions = new RefundCreateOptions
                                {
                                    PaymentIntent = pi.Id,
                                    Amount = (long)(refundAmount * 100) // 100% full refund for store decline
                                };
                                await refundService.CreateAsync(refundOptions);
                            }
                        }
                    }
                    catch (StripeException e)
                    {
                        await transaction.RollbackAsync();
                        stripeError = "Failed to process refund: " + e.StripeError.Message;
                        return;
                    }
                }

                // Batch-load stock products
                var itemProductIds = order.Items
                    .Where(i => i.ProductId.HasValue)
                    .Select(i => i.ProductId!.Value)
                    .Distinct()
                    .ToList();

                var productsDict = await _db.Products
                    .AsTracking()
                    .Where(p => itemProductIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                foreach (var item in order.Items)
                {
                    if (!item.ProductId.HasValue) continue;
                    if (productsDict.TryGetValue(item.ProductId.Value, out var product))
                    {
                        product.StockQuantity += item.Quantity;
                        product.UpdatedAtUtc = DateTime.UtcNow;
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.CancellationReason = req.CancellationReason;
                order.CancelledByRole = "Shop";
                order.CancelledAtUtc = DateTime.UtcNow;
                order.UpdatedAtUtc = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        if (stripeError != null) return BadRequest(new { error = stripeError });

        // Notify customer
        await _notif.NotifyUserAsync(order.UserId, "Order Declined", $"Your order #{order.Id} was declined by the store. Reason: {req.CancellationReason}");
        await _notif.NotifyOrderStatusChangedAsync(order.UserId, order.Id, "Cancelled");

        // Broadcast SignalR
        await _trackingHub.Clients.Group($"order_{order.Id}")
            .SendAsync("order:status_updated", new
            {
                orderId = order.Id,
                orderGroupId,
                status = "Cancelled",
                cancellationReason = req.CancellationReason,
                cancelledByRole = "Shop"
            });

        List<Order> groupedOrders;
        if (!string.IsNullOrWhiteSpace(order.OrderGroupId))
        {
            groupedOrders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.OrderGroupId == order.OrderGroupId)
                .ToListAsync();
        }
        else
        {
            groupedOrders = new List<Order> { order };
        }

        await BroadcastGroupedOrderStatusAsync(groupedOrders);

        return Ok(new MessageResponse("Order declined successfully"));
    }

    private Task<bool> CanDeliveryDriverAccessOrderAsync(string deliveryUserId, int orderId, string? orderGroupId)
    {
        if (string.IsNullOrWhiteSpace(orderGroupId))
        {
            return _db.DeliveryJobs
                .AsNoTracking()
                .AnyAsync(j => j.DeliveryUserId == deliveryUserId && j.OrderId == orderId);
        }

        return _db.DeliveryJobs
            .AsNoTracking()
            .AnyAsync(j => j.DeliveryUserId == deliveryUserId &&
                (j.OrderId == orderId || j.Order.OrderGroupId == orderGroupId));
    }


    private async Task<List<GroupedOrderDto>> BuildGroupedOrderDtosAsync(
        List<Order> orders,
        IReadOnlyDictionary<string, DeliveryJob>? deliveryJobsByGroup = null)
    {
        var storeUserIds = orders.Select(o => o.StoreUserId).Distinct().ToList();
        var customerUserIds = orders.Select(o => o.UserId).Distinct().ToList();
        var allRelatedUserIds = storeUserIds.Concat(customerUserIds).Distinct().ToList();

        var shopProfilesList = await _db.ShopProfiles
            .AsNoTracking()
            .Where(shop => storeUserIds.Contains(shop.UserId))
            .ToListAsync();
        var shopsByUserId = shopProfilesList
            .GroupBy(shop => shop.UserId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var shop = group
                        .OrderByDescending(x => x.Id)
                        .First();
                    return new ShopSummary(shop.ShopName, shop.Location);
                }
            );

        var usersList = await _db.Users
            .AsNoTracking()
            .Where(user => allRelatedUserIds.Contains(user.Id))
            .ToListAsync();
        var coordinatesByUserId = usersList
            .ToDictionary(
                user => user.Id,
                user => (user.Latitude, user.Longitude, user.Country)
            );

        return orders
            .GroupBy(OrderGroupHelper.ResolveGroupId)
            .Select(group =>
            {
                DeliveryJob? deliveryJob = null;
                deliveryJobsByGroup?.TryGetValue(group.Key, out deliveryJob);
                return OrderGroupHelper.CreateGroupedOrderDto(group.ToList(), shopsByUserId, coordinatesByUserId, deliveryJob);
            })
            .ToList();
    }

    private async Task BroadcastGroupedOrderStatusAsync(
        IReadOnlyCollection<Order> groupedOrders,
        DeliveryJob? deliveryJob = null)
    {
        if (groupedOrders.Count == 0) return;

        var orderGroupId = OrderGroupHelper.ResolveGroupId(groupedOrders.First());
        var status = OrderGroupHelper.ComputeGroupedStatus(groupedOrders, deliveryJob?.Status);
        var cancelledOrderWithReason = groupedOrders.FirstOrDefault(o => !string.IsNullOrEmpty(o.CancellationReason));

        await _trackingHub.Clients.Group(OrderGroupHelper.BuildTrackingGroupName(orderGroupId))
            .SendAsync("order:status_updated", new
            {
                orderGroupId,
                status,
                deliveryJobId = deliveryJob?.Id,
                deliveryStatus = deliveryJob?.Status.ToString(),
                cancellationReason = cancelledOrderWithReason?.CancellationReason ?? groupedOrders.First().CancellationReason,
                cancelledByRole = cancelledOrderWithReason?.CancelledByRole ?? groupedOrders.First().CancelledByRole
            });
    }

    private async Task<OrderDto?> BuildOrderDto(int orderId)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null) return null;

        // Load shop profile and both user records in a single batched query
        var relatedUserIds = new[] { order.UserId, order.StoreUserId }.Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => relatedUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var shop = await _db.ShopProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == order.StoreUserId);
        users.TryGetValue(order.UserId, out var user);
        users.TryGetValue(order.StoreUserId, out var storeUser);

        return MapOrderDto(order, shop?.ShopName, user?.Latitude, user?.Longitude, storeUser?.Latitude, storeUser?.Longitude, storeUser?.Country, user?.FullName, user?.ProfilePictureUrl);
    }

    private static OrderDto MapOrderDto(Order o, string? storeName, double? custLat = null, double? custLng = null, double? storeLat = null, double? storeLng = null, string? storeCountry = null, string? customerName = null, string? customerAvatarUrl = null)
    {
        var currencyCode = Helpers.CurrencyHelper.GetCurrencyCode(storeCountry);
        return new(
            o.Id, o.UserId, o.StoreUserId, storeName,
            o.Status.ToString(), o.TotalAmount, o.DeliveryAddress ?? "", o.PaymentMethod, o.OfferCode,
            o.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.Product?.Name ?? "", i.Quantity, i.UnitPrice, i.Product?.StoreUserId ?? o.StoreUserId, currencyCode)).ToList(),
            o.CreatedAtUtc, o.UpdatedAtUtc, o.OrderGroupId,
            custLat, custLng, storeLat, storeLng,
            currencyCode,
            customerName,
            customerAvatarUrl,
            o.CancellationReason,
            o.CancelledByRole,
            o.CancelledAtUtc
        );
    }
}

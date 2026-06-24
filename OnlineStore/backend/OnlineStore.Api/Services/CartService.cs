using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _db;

    public CartService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> AddToCartAsync(string userId, int productId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return "Error: User authentication is required. Session may have expired.";

        if (quantity <= 0)
            return "Error: Quantity must be greater than zero.";

        var product = await _db.Products.FindAsync(productId);
        if (product == null)
            return $"Error: Product with ID {productId} not found.";
            
        if (product.Status != ProductStatus.Approved)
            return $"Error: Product '{product.Name}' is not available.";

        if (product.StockQuantity < quantity)
            return $"Error: Only {product.StockQuantity} items in stock for '{product.Name}'.";

        var cart = await GetOrCreateCart(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item != null)
        {
            item.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem { ProductId = productId, Quantity = quantity });
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return $"Successfully added {quantity} x {product.Name} to cart.";
    }

    public async Task<string> UpdateQuantityAsync(string userId, int productId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return "Error: User authentication is required.";

        var cart = await GetOrCreateCart(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item == null) 
            return $"Error: Product ID {productId} is not in your cart.";

        if (quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            var product = await _db.Products.FindAsync(productId);
            if (product != null && product.StockQuantity < quantity)
                return $"Error: Only {product.StockQuantity} items in stock.";
                
            item.Quantity = quantity;
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return quantity <= 0 
            ? "Item removed from cart." 
            : $"Quantity updated to {quantity}.";
    }

    public async Task<string> RemoveFromCartAsync(string userId, int productId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return "Error: User authentication is required.";

        var cart = await GetOrCreateCart(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item != null)
        {
            cart.Items.Remove(item);
            cart.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return "Item successfully removed from cart.";
        }

        return "Item was not found in your cart.";
    }

    public async Task<string> GetCartSummaryAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return "Error: User authentication is required. Session may have expired.";

        var cart = await _db.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
            return "Your cart is currently empty.";

        var summary = new
        {
            Items = cart.Items.Select(i => new
            {
                i.ProductId,
                ProductName = i.Product?.Name ?? "Unknown",
                i.Quantity,
                UnitPrice = i.Product?.Price ?? 0,
                TotalLinePrice = (i.Product?.Price ?? 0) * i.Quantity
            }),
            TotalPrice = cart.Items.Sum(i => (i.Product?.Price ?? 0) * i.Quantity),
            UpdatedAt = cart.UpdatedAtUtc
        };

        return System.Text.Json.JsonSerializer.Serialize(summary);
    }

    private async Task<Cart> GetOrCreateCart(string userId)
    {
        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        return cart;
    }
}

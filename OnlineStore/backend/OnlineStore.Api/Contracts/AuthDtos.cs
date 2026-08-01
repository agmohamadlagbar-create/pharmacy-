using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Api.Contracts;

// ════════════════════════════════════════════
//  Auth
// ════════════════════════════════════════════

public record RegisterUserRequest([EmailAddress] string Email, string Password, string? FullName, string Country, string Code);
public record RegisterShopRequest([EmailAddress] string Email, string Password, string ShopName, string StoreType, string Country, string Code);
public record RegisterDeliveryRequest([EmailAddress] string Email, string Password, string? FullName, string Country, string? Phone, string? VehicleInfo, string Code);
public record LoginRequest([EmailAddress] string Email, string Password);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);
public record LogoutRequest(string RefreshToken);

public record AuthResponse(string Token, string RefreshToken, string AccountType, int ExpiresInSeconds);
public record MessageResponse(string Message);

public record SendCodeRequest([EmailAddress] string Email);
public record ForgotPasswordRequest([EmailAddress] string Email);
public record ResetPasswordRequest([EmailAddress] string Email, string Code, string NewPassword);

// ════════════════════════════════════════════
//  Profile / Me
// ════════════════════════════════════════════

public record MeResponse(
    string Id,
    string Email,
    string AccountType,
    string? FullName,
    string? ShopName,
    string? StoreType,
    string? Location,
    string? ProfilePictureUrl,
    string? Country,
    string? Street,
    string? BuildingNumber,
    string? City,
    string? PostalCode,
    string? District,
    string? Neighborhood,
    string? Floor,
    string? ApartmentNo,
    double? Latitude,
    double? Longitude,
    string? Description,
    string? OpeningTime,
    string? ClosingTime
);

public record UpdateProfileRequest(
    string? FullName,
    string? Country,
    string? Street,
    string? BuildingNumber,
    string? City,
    string? PostalCode,
    string? District,
    string? Neighborhood,
    string? Floor,
    string? ApartmentNo,
    double? Latitude,
    double? Longitude,
    string? Description,
    string? OpeningTime,
    string? ClosingTime
);

public record PublicShopDto(
    string UserId,
    string ShopName,
    string StoreType,
    string? Location,
    string? Description,
    string? ProfilePictureUrl,
    string? OpeningTime,
    string? ClosingTime
);

// ════════════════════════════════════════════
//  Categories
// ════════════════════════════════════════════

public record CategoryDto(int Id, string Name, string Code, string? ImageUrl, int? ParentId);
public record CreateCategoryRequest(string Name, string? Code, string? ImageUrl, int? ParentId);
public record UpdateCategoryRequest(string Name, string? Code, string? ImageUrl, int? ParentId);

// ════════════════════════════════════════════
//  Products
// ════════════════════════════════════════════

public record ProductDto(int Id, string Name, string? Description, decimal Price, string? ImageUrl, List<string> AdditionalImageUrls, int StockQuantity, int OriginalQuantity, int CategoryId, string? CategoryName, string Status, string? RejectionReason, string StoreUserId, string? StoreName, string? StoreCountry, string? StoreCity, DateTime CreatedAtUtc, double AverageRating = 0, int ReviewCount = 0, string? CurrencyCode = "USD");
public record CreateProductRequest(string Name, string? Description, decimal Price, string? ImageUrl, List<string>? AdditionalImageUrls, int StockQuantity, int CategoryId);
public record UpdateProductRequest(string? Name, string? Description, decimal? Price, string? ImageUrl, List<string>? AdditionalImageUrls, int? StockQuantity, int? CategoryId);
public record AdminRejectRequest(string Reason);

// ════════════════════════════════════════════
//  Reviews
// ════════════════════════════════════════════

public record ReviewDto(int Id, string UserName, int Rating, string? Comment, DateTime CreatedAtUtc);
public record CreateReviewRequest(int Rating, string? Comment);

public record DailySaleDto(string Day, int Count);

public record ProductDetailsDto(
    ProductDto Product,
    int TotalSales,
    int InStock,
    double AverageRating,
    int TotalReviews,
    List<ReviewDto> LatestReviews,
    List<ProductDto> RelatedProducts,
    List<DailySaleDto> DailySales
);

// ════════════════════════════════════════════
//  Cart
// ════════════════════════════════════════════

public record CartItemDto(int ProductId, string ProductName, string? ImageUrl, decimal Price, int Quantity, string StoreUserId, string? StoreName, string? StoreCity, string? CurrencyCode = "USD");
public record CartDto(List<CartItemDto> Items, decimal TotalAmount);

// ════════════════════════════════════════════
//  Orders
// ════════════════════════════════════════════

public record OrderItemRequest(int ProductId, int Quantity);
public record CreateOrderRequest(string StoreUserId, List<OrderItemRequest> Items, [Required] string DeliveryAddress, string? PaymentMethod, string? OfferCode, string? PaymentToken);
public record CheckoutCartRequest([Required] string DeliveryAddress, string? DeliveryCity, string? PaymentMethod, string? OfferCode, string? PaymentToken);
public record UpdateOrderStatusRequest(string Status);
public record ShopCancelRequest([Required] string CancellationReason);

public record OrderItemDto(int Id, int? ProductId, string ProductName, int Quantity, decimal UnitPrice, string StoreUserId, string? CurrencyCode = "USD");
public record OrderDto(int Id, string UserId, string StoreUserId, string? StoreName, string Status, decimal TotalAmount, string DeliveryAddress, string? PaymentMethod, string? OfferCode, List<OrderItemDto> Items, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, string? OrderGroupId = null, double? CustomerLat = null, double? CustomerLng = null, double? StoreLat = null, double? StoreLng = null, string? CurrencyCode = "USD", string? CustomerName = null, string? CustomerAvatarUrl = null, string? CancellationReason = null, string? CancelledByRole = null, DateTime? CancelledAtUtc = null);
public record GroupedOrderStoreDto(int OrderId, string StoreUserId, string? StoreName, string? StoreLocation, string Status, decimal TotalAmount, double? StoreLat, double? StoreLng, List<OrderItemDto> Items, string? CurrencyCode = "USD", string? CancellationReason = null, string? CancelledByRole = null, DateTime? CancelledAtUtc = null);
public record GroupedOrderDto(string OrderGroupId, int RepresentativeOrderId, string UserId, string Status, decimal TotalAmount, string DeliveryAddress, string? PaymentMethod, string? OfferCode, List<GroupedOrderStoreDto> Stores, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, double? CustomerLat = null, double? CustomerLng = null, int? DeliveryJobId = null, string? DeliveryStatus = null, string? CancellationReason = null, string? CancelledByRole = null, DateTime? CancelledAtUtc = null);
public record OrderStatsDto(decimal Revenue, int ActiveCount, string AvgPrep = "14m", string Cancellation = "0.4%");

// ════════════════════════════════════════════
//  Delivery
// ════════════════════════════════════════════

public record UpdateDeliveryStatusRequest(string Status);
public record AssignDeliveryRequest(string DeliveryUserId);
public record DeliveryJobDto(int Id, int OrderId, string? OrderGroupId, string? DeliveryUserId, string Status, DateTime CreatedAtUtc, DateTime? AcceptedAtUtc, DateTime? DeliveredAtUtc, GroupedOrderDto? OrderGroup);



// ════════════════════════════════════════════
//  Notifications
// ════════════════════════════════════════════

public record NotificationDto(int Id, string Title, string Message, bool IsRead, DateTime CreatedAtUtc);

// ════════════════════════════════════════════
//  Admin – Users
// ════════════════════════════════════════════

public record UserDto(string Id, string Email, string AccountType, string? FullName, bool IsDisabled);

// ════════════════════════════════════════════
//  Ad Banners
// ════════════════════════════════════════════

public record AdBannerDto(int Id, string ImageUrl, string? Title, string? LinkUrl, int SortOrder);
public record CreateAdBannerRequest(string ImageUrl, string? Title, string? LinkUrl, int? SortOrder);

// ════════════════════════════════════════════
//  Admin – Products
// ════════════════════════════════════════════

public record AdminCreateProductRequest(string Name, string? Description, decimal Price, string? ImageUrl, List<string>? AdditionalImageUrls, int CategoryId, string? StoreUserId, int StockQuantity, string? StoreName);
public record AdminUpdateProductRequest(string? Name, string? Description, decimal? Price, string? ImageUrl, List<string>? AdditionalImageUrls, int? CategoryId, string? Status, int? StockQuantity);
public record AdminStoreDto(string UserId, string ShopName, string StoreType, string? Location);

// ════════════════════════════════════════════
//  Pagination
// ════════════════════════════════════════════

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public bool HasMore { get; set; }

    public static PaginatedResponse<T> Create(List<T> allItems, int page, int limit)
    {
        var total = allItems.Count;
        var data = allItems.Skip((page - 1) * limit).Take(limit).ToList();
        return new PaginatedResponse<T>
        {
            Data = data,
            Page = page,
            Limit = limit,
            Total = total,
            HasMore = page * limit < total
        };
    }

    /// <summary>Create from pre-paginated data (server-side pagination).</summary>
    public static PaginatedResponse<T> Create(List<T> pageItems, int page, int limit, int total)
    {
        return new PaginatedResponse<T>
        {
            Data = pageItems,
            Page = page,
            Limit = limit,
            Total = total,
            HasMore = page * limit < total
        };
    }
}

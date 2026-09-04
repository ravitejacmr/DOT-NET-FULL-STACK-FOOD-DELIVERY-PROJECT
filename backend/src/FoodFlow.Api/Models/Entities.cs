namespace FoodFlow.Api.Models;

public static class Roles
{
    public const string Customer = "customer";
    public const string RestaurantOwner = "restaurant_owner";
    public const string DeliveryPartner = "delivery_partner";
    public const string Admin = "admin";
    public static readonly HashSet<string> All = [Customer, RestaurantOwner, DeliveryPartner, Admin];
}

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Role { get; set; } = Roles.Customer;
    public string? ProfileImage { get; set; }
    public bool IsActive { get; set; } = true;
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public class Address
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string HouseNumber { get; set; } = "";
    public string Street { get; set; } = "";
    public string Landmark { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string AddressType { get; set; } = "home";
    public bool IsDefault { get; set; }
}

public class Restaurant
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public AppUser Owner { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Logo { get; set; }
    public string? CoverImage { get; set; }
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string CuisineType { get; set; } = "";
    public TimeOnly OpeningTime { get; set; } = new(9, 0);
    public TimeOnly ClosingTime { get; set; } = new(23, 0);
    public decimal MinimumOrder { get; set; } = 149;
    public decimal DeliveryFee { get; set; } = 39;
    public int EstimatedDeliveryTime { get; set; } = 35;
    public decimal AverageRating { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Category
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Image { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FoodItem
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Image { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int PreparationTime { get; set; } = 20;
    public decimal Rating { get; set; }
}

public class Favorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
}

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string DiscountType { get; set; } = "percentage";
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public List<CartItem> Items { get; set; } = [];
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public int FoodItemId { get; set; }
    public FoodItem FoodItem { get; set; } = null!;
    public int Quantity { get; set; } = 1;
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public int? DeliveryPartnerId { get; set; }
    public string DeliveryAddressJson { get; set; } = "{}";
    public string Status { get; set; } = "pending";
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal GrandTotal { get; set; }
    public string PaymentMethod { get; set; } = "cod";
    public string PaymentStatus { get; set; } = "pending";
    public string CustomerNotes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderItem> Items { get; set; } = [];
    public List<OrderStatusEvent> StatusEvents { get; set; } = [];
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string FoodName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderStatusEvent
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "cod";
    public string? TransactionId { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? PaymentDate { get; set; }
}

public class Review
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int RestaurantId { get; set; }
    public int? FoodItemId { get; set; }
    public int OrderId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AppNotification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string NotificationType { get; set; } = "order";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

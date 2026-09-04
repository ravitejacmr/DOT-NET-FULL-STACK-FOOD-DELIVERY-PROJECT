using System.Security.Claims;
using System.Text.Json;
using FoodFlow.Api.Models;

namespace FoodFlow.Api.Endpoints;

public static class EndpointHelpers
{
    public static int UserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    public static object Page<T>(IEnumerable<T> values)
    {
        var results = values.ToList();
        return new { count = results.Count, next = (string?)null, previous = (string?)null, results };
    }

    public static object UserDto(AppUser user) => new
    {
        user.Id, user.Email, user.FirstName, user.LastName, user.Phone, user.Role, user.ProfileImage, user.IsActive
    };

    public static object RestaurantDto(Restaurant restaurant, bool favorite = false) => new
    {
        restaurant.Id, owner = restaurant.OwnerId, restaurant.Name, restaurant.Description, restaurant.Logo,
        restaurant.CoverImage, restaurant.Phone, restaurant.Email, restaurant.Address, restaurant.City, restaurant.State,
        restaurant.PostalCode, restaurant.CuisineType,
        opening_time = restaurant.OpeningTime.ToString("HH:mm"), closing_time = restaurant.ClosingTime.ToString("HH:mm"),
        minimum_order = Money(restaurant.MinimumOrder), delivery_fee = Money(restaurant.DeliveryFee),
        restaurant.EstimatedDeliveryTime, average_rating = restaurant.AverageRating.ToString("0.0"),
        restaurant.IsActive, is_open = IsOpen(restaurant), is_favorite = favorite
    };

    public static object FoodDto(FoodItem food) => new
    {
        food.Id, restaurant = food.RestaurantId, restaurant_name = food.Restaurant?.Name,
        category = food.CategoryId, category_name = food.Category?.Name, food.Name, food.Description, food.Image,
        original_price = Money(food.OriginalPrice),
        discount_price = food.DiscountPrice.HasValue ? Money(food.DiscountPrice.Value) : null,
        price = Money(food.DiscountPrice ?? food.OriginalPrice), food.IsVegetarian, food.IsAvailable,
        food.PreparationTime, rating = food.Rating.ToString("0.0")
    };

    public static object CategoryDto(Category category) => new
    {
        category.Id, restaurant = category.RestaurantId, category.Name, category.Description, category.Image, category.IsActive
    };

    public static object OrderDto(Order order)
    {
        var address = JsonSerializer.Deserialize<Dictionary<string, string>>(order.DeliveryAddressJson)
            ?? new Dictionary<string, string>();
        return new
        {
            order.Id, order.OrderNumber, restaurant = order.RestaurantId, restaurant_name = order.Restaurant?.Name,
            delivery_address_snapshot = address,
            items = order.Items.Select(item => new
            {
                item.Id, item.FoodName, item.Quantity, unit_price = Money(item.UnitPrice), subtotal = Money(item.UnitPrice * item.Quantity)
            }),
            order.Status, status_label = StatusLabel(order.Status),
            status_events = order.StatusEvents.OrderBy(x => x.CreatedAt).Select(x => new
            {
                x.Id, x.Status, label = StatusLabel(x.Status), x.CreatedAt
            }),
            subtotal = Money(order.Subtotal), tax = Money(order.Tax), delivery_fee = Money(order.DeliveryFee),
            discount = Money(order.Discount), grand_total = Money(order.GrandTotal), order.PaymentMethod,
            order.PaymentStatus, order.CreatedAt
        };
    }

    public static object PaymentDto(Payment payment) => new
    {
        payment.Id, order = payment.OrderId, order_number = payment.Order?.OrderNumber, amount = Money(payment.Amount),
        payment.PaymentMethod, payment.TransactionId, payment.Status, payment.PaymentDate
    };

    public static string Money(decimal value) => value.ToString("0.00");
    public static bool IsOpen(Restaurant restaurant)
    {
        if (!restaurant.IsActive) return false;
        var time = TimeOnly.FromDateTime(DateTime.Now);
        return restaurant.OpeningTime <= restaurant.ClosingTime
            ? time >= restaurant.OpeningTime && time <= restaurant.ClosingTime
            : time >= restaurant.OpeningTime || time <= restaurant.ClosingTime;
    }

    public static string StatusLabel(string status) => status switch
    {
        "pending" => "Order placed", "confirmed" => "Confirmed", "accepted" => "Accepted",
        "preparing" => "Preparing", "ready" => "Ready for pickup", "assigned" => "Rider assigned",
        "picked_up" => "Picked up", "on_the_way" => "On the way", "delivered" => "Delivered",
        "cancelled" => "Cancelled", "rejected" => "Rejected", _ => status.Replace('_', ' ')
    };

    public static IResult Error(string message, int statusCode = 400) =>
        Results.Json(new { error = new { details = message } }, statusCode: statusCode);
}

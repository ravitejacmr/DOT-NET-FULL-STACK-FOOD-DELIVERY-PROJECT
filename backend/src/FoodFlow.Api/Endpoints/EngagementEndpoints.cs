using System.Security.Claims;
using FoodFlow.Api.Data;
using FoodFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Api.Endpoints;

public static class EngagementEndpoints
{
    public static IEndpointRouteBuilder MapEngagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Engagement");

        group.MapGet("/favorites/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var favorites = await db.Favorites.AsNoTracking().Include(x => x.Restaurant)
                .Where(x => x.UserId == principal.UserId()).OrderBy(x => x.Restaurant.Name).ToListAsync();
            return Results.Ok(EndpointHelpers.Page(favorites.Select(x => new
            {
                x.Id, restaurant = x.RestaurantId, restaurant_detail = EndpointHelpers.RestaurantDto(x.Restaurant, true)
            })));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapPost("/favorites/", async (FavoriteRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var userId = principal.UserId();
            if (!await db.Restaurants.AnyAsync(x => x.Id == request.Restaurant)) return Results.NotFound();
            var existing = await db.Favorites.Include(x => x.Restaurant).SingleOrDefaultAsync(x => x.UserId == userId && x.RestaurantId == request.Restaurant);
            if (existing is not null) return Results.Ok(new { existing.Id, restaurant = existing.RestaurantId, restaurant_detail = EndpointHelpers.RestaurantDto(existing.Restaurant, true) });
            var favorite = new Favorite { UserId = userId, RestaurantId = request.Restaurant };
            db.Favorites.Add(favorite); await db.SaveChangesAsync();
            return Results.Created($"/api/favorites/{favorite.Id}/", new { favorite.Id, restaurant = favorite.RestaurantId });
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapDelete("/favorites/{id:int}/", async (int id, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var favorite = await db.Favorites.SingleOrDefaultAsync(x => x.Id == id && x.UserId == principal.UserId());
            if (favorite is null) return Results.NotFound();
            db.Favorites.Remove(favorite); await db.SaveChangesAsync(); return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapGet("/reviews/", async (int? customer, int? restaurant, FoodFlowDbContext db) =>
        {
            var query = db.Reviews.AsNoTracking().AsQueryable();
            if (customer.HasValue) query = query.Where(x => x.CustomerId == customer);
            if (restaurant.HasValue) query = query.Where(x => x.RestaurantId == restaurant);
            var reviews = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
            var names = await db.Users.Where(x => reviews.Select(r => r.CustomerId).Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName);
            return Results.Ok(EndpointHelpers.Page(reviews.Select(x => ReviewDto(x, names.GetValueOrDefault(x.CustomerId, "Customer")))));
        });

        group.MapPost("/reviews/", async (ReviewRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            if (request.Rating is < 1 or > 5) return EndpointHelpers.Error("Rating must be between 1 and 5.");
            var userId = principal.UserId();
            var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == request.Order && x.CustomerId == userId && x.Status == "delivered");
            if (order is null || order.RestaurantId != request.Restaurant) return EndpointHelpers.Error("Only your delivered orders can be reviewed.");
            if (await db.Reviews.AnyAsync(x => x.CustomerId == userId && x.OrderId == request.Order)) return EndpointHelpers.Error("This order has already been reviewed.");
            var review = new Review { CustomerId = userId, RestaurantId = request.Restaurant, FoodItemId = request.FoodItem, OrderId = request.Order, Rating = request.Rating, Comment = request.Comment ?? "" };
            db.Reviews.Add(review); await db.SaveChangesAsync(); await UpdateRatingAsync(db, review.RestaurantId);
            var user = await db.Users.FindAsync(userId);
            return Results.Created($"/api/reviews/{review.Id}/", ReviewDto(review, $"{user!.FirstName} {user.LastName}"));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapPatch("/reviews/{id:int}/", async (int id, ReviewUpdateRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var review = await db.Reviews.FindAsync(id);
            if (review is null) return Results.NotFound();
            if (review.CustomerId != principal.UserId() && !principal.IsInRole(Roles.Admin)) return Results.Forbid();
            if (request.Rating.HasValue)
            {
                if (request.Rating is < 1 or > 5) return EndpointHelpers.Error("Rating must be between 1 and 5.");
                review.Rating = request.Rating.Value;
            }
            if (request.Comment is not null) review.Comment = request.Comment;
            await db.SaveChangesAsync(); await UpdateRatingAsync(db, review.RestaurantId);
            var user = await db.Users.FindAsync(review.CustomerId);
            return Results.Ok(ReviewDto(review, $"{user!.FirstName} {user.LastName}"));
        }).RequireAuthorization();

        group.MapDelete("/reviews/{id:int}/", async (int id, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var review = await db.Reviews.FindAsync(id);
            if (review is null) return Results.NotFound();
            if (review.CustomerId != principal.UserId() && !principal.IsInRole(Roles.Admin)) return Results.Forbid();
            var restaurantId = review.RestaurantId; db.Reviews.Remove(review); await db.SaveChangesAsync();
            await UpdateRatingAsync(db, restaurantId); return Results.NoContent();
        }).RequireAuthorization();

        group.MapGet("/notifications/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
            Results.Ok(EndpointHelpers.Page(await db.Notifications.AsNoTracking().Where(x => x.UserId == principal.UserId()).OrderByDescending(x => x.CreatedAt).ToListAsync())))
            .RequireAuthorization();

        group.MapPost("/notifications/{id:int}/mark_read/", async (int id, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var notification = await db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == principal.UserId());
            if (notification is null) return Results.NotFound();
            notification.IsRead = true; await db.SaveChangesAsync(); return Results.Ok(notification);
        }).RequireAuthorization();

        group.MapPost("/notifications/mark_all_read/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            await db.Notifications.Where(x => x.UserId == principal.UserId() && !x.IsRead)
                .ExecuteUpdateAsync(x => x.SetProperty(n => n.IsRead, true));
            return Results.Ok(new { detail = "All notifications marked as read." });
        }).RequireAuthorization();

        group.MapGet("/coupons/", async (FoodFlowDbContext db) =>
            Results.Ok(EndpointHelpers.Page((await db.Coupons.AsNoTracking().OrderBy(x => x.Code).ToListAsync()).Select(CouponDto))))
            .RequireAuthorization();

        group.MapPost("/coupons/", async (CouponRequest request, FoodFlowDbContext db) =>
        {
            var coupon = new Coupon { Code = request.Code.Trim().ToUpper(), DiscountType = request.DiscountType, DiscountValue = request.DiscountValue, MinimumOrderAmount = request.MinimumOrderAmount, ExpiryDate = request.ExpiryDate, IsActive = request.IsActive };
            db.Coupons.Add(coupon); await db.SaveChangesAsync(); return Results.Created($"/api/coupons/{coupon.Id}/", CouponDto(coupon));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        return app;
    }

    private static object ReviewDto(Review review, string customerName) => new
    {
        review.Id, customer = review.CustomerId, customer_name = customerName, restaurant = review.RestaurantId,
        food_item = review.FoodItemId, order = review.OrderId, review.Rating, review.Comment, review.CreatedAt
    };
    private static object CouponDto(Coupon coupon) => new
    {
        coupon.Id, coupon.Code, coupon.DiscountType, discount_value = EndpointHelpers.Money(coupon.DiscountValue),
        minimum_order_amount = EndpointHelpers.Money(coupon.MinimumOrderAmount), coupon.ExpiryDate, coupon.IsActive
    };
    private static async Task UpdateRatingAsync(FoodFlowDbContext db, int restaurantId)
    {
        var restaurant = await db.Restaurants.FindAsync(restaurantId); if (restaurant is null) return;
        restaurant.AverageRating = await db.Reviews.Where(x => x.RestaurantId == restaurantId).Select(x => (decimal?)x.Rating).AverageAsync() ?? 0;
        await db.SaveChangesAsync();
    }
}

public record FavoriteRequest(int Restaurant);
public record ReviewRequest(int Restaurant, int? FoodItem, int Order, int Rating, string? Comment);
public record ReviewUpdateRequest(int? Rating, string? Comment);
public record CouponRequest(string Code, string DiscountType, decimal DiscountValue, decimal MinimumOrderAmount, DateTime ExpiryDate, bool IsActive);

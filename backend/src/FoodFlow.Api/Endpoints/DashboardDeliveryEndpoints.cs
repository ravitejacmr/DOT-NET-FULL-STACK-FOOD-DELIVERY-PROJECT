using System.Security.Claims;
using FoodFlow.Api.Data;
using FoodFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Api.Endpoints;

public static class DashboardDeliveryEndpoints
{
    public static IEndpointRouteBuilder MapDashboardDeliveryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Dashboards and delivery");

        group.MapGet("/delivery/available/", async (FoodFlowDbContext db) =>
        {
            var orders = await db.Orders.AsNoTracking().Include(x => x.Restaurant).Include(x => x.Items).Include(x => x.StatusEvents)
                .Where(x => x.Status == "ready" && x.DeliveryPartnerId == null).OrderBy(x => x.CreatedAt).ToListAsync();
            return Results.Ok(EndpointHelpers.Page(orders.Select(EndpointHelpers.OrderDto)));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.DeliveryPartner));

        group.MapGet("/delivery/assigned/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var orders = await db.Orders.AsNoTracking().Include(x => x.Restaurant).Include(x => x.Items).Include(x => x.StatusEvents)
                .Where(x => x.DeliveryPartnerId == principal.UserId() && x.Status != "delivered")
                .OrderByDescending(x => x.CreatedAt).ToListAsync();
            return Results.Ok(EndpointHelpers.Page(orders.Select(EndpointHelpers.OrderDto)));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.DeliveryPartner));

        group.MapPost("/delivery/{id:int}/accept/", async (int id, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var userId = principal.UserId();
            if (await db.Orders.AnyAsync(x => x.DeliveryPartnerId == userId && x.Status != "delivered" && x.Status != "cancelled"))
                return EndpointHelpers.Error("Complete your active delivery before accepting another.");
            var order = await db.Orders.Include(x => x.Restaurant).Include(x => x.Items).Include(x => x.StatusEvents)
                .SingleOrDefaultAsync(x => x.Id == id);
            if (order is null || order.Status != "ready" || order.DeliveryPartnerId is not null)
                return EndpointHelpers.Error("This delivery is no longer available.", 409);
            order.DeliveryPartnerId = userId; order.Status = "assigned";
            order.StatusEvents.Add(new OrderStatusEvent { Status = "assigned" });
            db.Notifications.Add(new AppNotification { UserId = order.CustomerId, Title = "Rider assigned", Message = $"A delivery partner is bringing {order.OrderNumber} to you.", NotificationType = "delivery" });
            await db.SaveChangesAsync();
            return Results.Ok(EndpointHelpers.OrderDto(order));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.DeliveryPartner));

        group.MapGet("/delivery/profile/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var user = await db.Users.FindAsync(principal.UserId());
            return user is null ? Results.NotFound() : Results.Ok(new
            {
                id = user.Id, user = user.Id, user.Email, user.Phone, vehicle_type = "Bike", vehicle_number = "KA-01-DEMO",
                driving_license_number = "DEMO-LICENCE", is_available = true, rating = "5.0"
            });
        }).RequireAuthorization(policy => policy.RequireRole(Roles.DeliveryPartner));

        group.MapGet("/delivery/earnings/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var delivered = await db.Orders.AsNoTracking().Where(x => x.DeliveryPartnerId == principal.UserId() && x.Status == "delivered").ToListAsync();
            return Results.Ok(new { total_earnings = delivered.Sum(x => x.DeliveryFee), completed_deliveries = delivered.Count });
        }).RequireAuthorization(policy => policy.RequireRole(Roles.DeliveryPartner));

        group.MapGet("/dashboard/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var userId = principal.UserId(); var today = DateTime.UtcNow.Date;
            if (principal.IsInRole(Roles.Customer))
            {
                var orders = await db.Orders.AsNoTracking().Include(x => x.Restaurant).Include(x => x.Items).Include(x => x.StatusEvents)
                    .Where(x => x.CustomerId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync();
                var current = orders.FirstOrDefault(x => x.Status is not ("delivered" or "cancelled" or "rejected"));
                return Results.Ok(new
                {
                    current_order = current is null ? null : EndpointHelpers.OrderDto(current),
                    saved_addresses = await db.Addresses.CountAsync(x => x.UserId == userId),
                    favorite_restaurants = await db.Favorites.CountAsync(x => x.UserId == userId),
                    unread_notifications = await db.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead),
                    recent_orders = orders.Take(5).Select(EndpointHelpers.OrderDto)
                });
            }
            if (principal.IsInRole(Roles.RestaurantOwner))
            {
                var restaurant = await db.Restaurants.SingleOrDefaultAsync(x => x.OwnerId == userId);
                if (restaurant is null) return Results.Ok(new { total_orders = 0, today_orders = 0, preparing_orders = 0, revenue = 0m, top_selling_items = Array.Empty<object>() });
                var orders = await db.Orders.AsNoTracking().Include(x => x.Items).Where(x => x.RestaurantId == restaurant.Id).ToListAsync();
                var top = orders.Where(x => x.Status == "delivered").SelectMany(x => x.Items)
                    .GroupBy(x => x.FoodName).Select(x => new { food_name = x.Key, quantity = x.Sum(i => i.Quantity) })
                    .OrderByDescending(x => x.quantity).Take(5);
                return Results.Ok(new
                {
                    total_orders = orders.Count, today_orders = orders.Count(x => x.CreatedAt.Date == today),
                    preparing_orders = orders.Count(x => x.Status == "preparing"),
                    revenue = orders.Where(x => x.Status == "delivered").Sum(x => x.GrandTotal), top_selling_items = top
                });
            }
            if (principal.IsInRole(Roles.DeliveryPartner))
            {
                var orders = await db.Orders.AsNoTracking().Where(x => x.DeliveryPartnerId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync();
                var active = orders.FirstOrDefault(x => x.Status is "assigned" or "picked_up" or "on_the_way");
                return Results.Ok(new
                {
                    available_deliveries = await db.Orders.CountAsync(x => x.Status == "ready" && x.DeliveryPartnerId == null),
                    today_deliveries = orders.Count(x => x.Status == "delivered" && x.CreatedAt.Date == today),
                    completed_deliveries = orders.Count(x => x.Status == "delivered"),
                    total_earnings = orders.Where(x => x.Status == "delivered").Sum(x => x.DeliveryFee),
                    active_delivery = active is null ? null : new { active.Id, active.OrderNumber, active.Status }
                });
            }

            var allOrders = await db.Orders.AsNoTracking().Include(x => x.Restaurant).Include(x => x.Items).ToListAsync();
            var topRestaurants = allOrders.GroupBy(x => new { x.RestaurantId, x.Restaurant.Name })
                .Select(x => new { id = x.Key.RestaurantId, name = x.Key.Name, order_count = x.Count() })
                .OrderByDescending(x => x.order_count).Take(5);
            var topFoods = allOrders.Where(x => x.Status == "delivered").SelectMany(x => x.Items)
                .GroupBy(x => x.FoodName).Select(x => new { food_name = x.Key, quantity = x.Sum(i => i.Quantity) })
                .OrderByDescending(x => x.quantity).Take(5);
            return Results.Ok(new
            {
                total_users = await db.Users.CountAsync(), total_restaurants = await db.Restaurants.CountAsync(),
                today_orders = allOrders.Count(x => x.CreatedAt.Date == today),
                total_revenue = allOrders.Where(x => x.Status == "delivered").Sum(x => x.GrandTotal),
                pending_orders = allOrders.Count(x => x.Status is "pending" or "confirmed"),
                completed_orders = allOrders.Count(x => x.Status == "delivered"),
                cancelled_orders = allOrders.Count(x => x.Status is "cancelled" or "rejected"),
                total_delivery_partners = await db.Users.CountAsync(x => x.Role == Roles.DeliveryPartner),
                top_restaurants = topRestaurants, top_selling_foods = topFoods
            });
        }).RequireAuthorization();

        group.MapGet("/dashboard/users/", async (string? search, string? role, FoodFlowDbContext db) =>
        {
            var query = db.Users.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) { var term = search.ToLower(); query = query.Where(x => x.Email.ToLower().Contains(term) || x.FirstName.ToLower().Contains(term)); }
            if (!string.IsNullOrWhiteSpace(role)) query = query.Where(x => x.Role == role);
            return Results.Ok(EndpointHelpers.Page((await query.OrderBy(x => x.Email).ToListAsync()).Select(EndpointHelpers.UserDto)));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        group.MapPatch("/dashboard/users/{id:int}/", async (int id, AdminUserUpdateRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var user = await db.Users.FindAsync(id); if (user is null) return Results.NotFound();
            if (user.Id == principal.UserId() && request.IsActive == false) return EndpointHelpers.Error("You cannot deactivate your own administrator account.");
            if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
            if (request.Role is not null && Roles.All.Contains(request.Role)) user.Role = request.Role;
            await db.SaveChangesAsync(); return Results.Ok(EndpointHelpers.UserDto(user));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        return app;
    }
}

public record AdminUserUpdateRequest(bool? IsActive, string? Role);

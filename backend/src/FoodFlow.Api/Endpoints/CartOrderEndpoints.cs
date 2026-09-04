using System.Security.Claims;
using System.Text.Json;
using FoodFlow.Api.Data;
using FoodFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Api.Endpoints;

public static class CartOrderEndpoints
{
    public static IEndpointRouteBuilder MapCartOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Cart and orders");

        group.MapGet("/cart/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
            Results.Ok(CartDto(await LoadCartAsync(db, principal.UserId()))))
            .RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapDelete("/cart/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var cart = await LoadCartAsync(db, principal.UserId());
            db.CartItems.RemoveRange(cart.Items);
            cart.RestaurantId = null; cart.CouponId = null;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapPost("/cart/items/", async (CartItemCreateRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            if (request.Quantity < 1) return EndpointHelpers.Error("Quantity must be at least one.");
            var food = await db.FoodItems.Include(x => x.Restaurant).Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == request.FoodItem);
            if (food is null || !food.IsAvailable || !food.Restaurant.IsActive) return EndpointHelpers.Error("This food item is unavailable.");
            var cart = await LoadCartAsync(db, principal.UserId());
            if (cart.RestaurantId.HasValue && cart.RestaurantId != food.RestaurantId)
            {
                if (!request.Replace) return EndpointHelpers.Error("Your cart contains items from another restaurant. Confirm replacement first.", 409);
                db.CartItems.RemoveRange(cart.Items); cart.Items.Clear(); cart.CouponId = null;
            }
            cart.RestaurantId = food.RestaurantId;
            var item = cart.Items.SingleOrDefault(x => x.FoodItemId == food.Id);
            if (item is null)
            {
                item = new CartItem { Cart = cart, FoodItemId = food.Id, FoodItem = food, Quantity = request.Quantity };
                db.CartItems.Add(item); cart.Items.Add(item);
            }
            else item.Quantity += request.Quantity;
            await db.SaveChangesAsync();
            return Results.Ok(CartItemDto(item));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapPatch("/cart/items/{id:int}/", async (int id, CartItemUpdateRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            if (request.Quantity < 1) return EndpointHelpers.Error("Quantity must be at least one.");
            var item = await db.CartItems.Include(x => x.Cart).Include(x => x.FoodItem).ThenInclude(x => x.Restaurant)
                .Include(x => x.FoodItem).ThenInclude(x => x.Category)
                .SingleOrDefaultAsync(x => x.Id == id && x.Cart.UserId == principal.UserId());
            if (item is null) return Results.NotFound();
            item.Quantity = request.Quantity;
            await db.SaveChangesAsync();
            return Results.Ok(CartItemDto(item));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapDelete("/cart/items/{id:int}/", async (int id, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var item = await db.CartItems.Include(x => x.Cart).SingleOrDefaultAsync(x => x.Id == id && x.Cart.UserId == principal.UserId());
            if (item is null) return Results.NotFound();
            var cartId = item.CartId;
            db.CartItems.Remove(item);
            await db.SaveChangesAsync();
            if (!await db.CartItems.AnyAsync(x => x.CartId == cartId))
            {
                item.Cart.RestaurantId = null; item.Cart.CouponId = null; await db.SaveChangesAsync();
            }
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapPost("/cart/coupon/", async (CouponApplyRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var cart = await LoadCartAsync(db, principal.UserId());
            var subtotal = CartSubtotal(cart);
            var coupon = await db.Coupons.SingleOrDefaultAsync(x => x.Code == request.Code.Trim().ToUpper() && x.IsActive && x.ExpiryDate > DateTime.UtcNow);
            if (coupon is null) return EndpointHelpers.Error("Coupon is invalid or expired.");
            if (subtotal < coupon.MinimumOrderAmount) return EndpointHelpers.Error($"A minimum order of ₹{coupon.MinimumOrderAmount:0.00} is required.");
            cart.CouponId = coupon.Id; cart.Coupon = coupon;
            await db.SaveChangesAsync();
            return Results.Ok(new { coupon = coupon.Code, discount = Discount(coupon, subtotal) });
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapGet("/orders/", async (string? status, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var userId = principal.UserId();
            var query = db.Orders.AsNoTracking().Include(x => x.Restaurant).Include(x => x.Items).Include(x => x.StatusEvents).AsQueryable();
            if (principal.IsInRole(Roles.Customer)) query = query.Where(x => x.CustomerId == userId);
            else if (principal.IsInRole(Roles.RestaurantOwner)) query = query.Where(x => x.Restaurant.OwnerId == userId);
            else if (principal.IsInRole(Roles.DeliveryPartner)) query = query.Where(x => x.DeliveryPartnerId == userId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
            return Results.Ok(EndpointHelpers.Page((await query.OrderByDescending(x => x.CreatedAt).ToListAsync()).Select(EndpointHelpers.OrderDto)));
        }).RequireAuthorization();

        group.MapGet("/orders/{id:int}/", async (int id, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var order = await LoadOrderAsync(db, id);
            if (order is null) return Results.NotFound();
            return CanAccess(order, principal) ? Results.Ok(EndpointHelpers.OrderDto(order)) : Results.Forbid();
        }).RequireAuthorization();

        group.MapPost("/orders/", async (OrderCreateRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var userId = principal.UserId();
            var cart = await LoadCartAsync(db, userId);
            if (cart.Items.Count == 0 || cart.Restaurant is null) return EndpointHelpers.Error("Your cart is empty.");
            var address = await db.Addresses.SingleOrDefaultAsync(x => x.Id == request.DeliveryAddress && x.UserId == userId);
            if (address is null) return EndpointHelpers.Error("Choose a valid delivery address.");
            if (request.PaymentMethod is not ("cod" or "online")) return EndpointHelpers.Error("Choose a valid payment method.");

            var subtotal = CartSubtotal(cart); var discount = Discount(cart.Coupon, subtotal);
            var tax = Math.Round(subtotal * 0.05m, 2); var fee = cart.Restaurant.DeliveryFee;
            var order = new Order
            {
                OrderNumber = $"FF{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                CustomerId = userId, RestaurantId = cart.Restaurant.Id,
                DeliveryAddressJson = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["full_name"] = address.FullName, ["phone"] = address.Phone, ["house_number"] = address.HouseNumber,
                    ["street"] = address.Street, ["landmark"] = address.Landmark, ["city"] = address.City,
                    ["state"] = address.State, ["postal_code"] = address.PostalCode
                }),
                Subtotal = subtotal, Tax = tax, DeliveryFee = fee, Discount = discount,
                GrandTotal = subtotal + tax + fee - discount, PaymentMethod = request.PaymentMethod,
                PaymentStatus = request.PaymentMethod == "cod" ? "pending" : "awaiting_payment", CustomerNotes = request.CustomerNotes ?? "",
                Items = cart.Items.Select(x => new OrderItem
                {
                    FoodName = x.FoodItem.Name, Quantity = x.Quantity, UnitPrice = x.FoodItem.DiscountPrice ?? x.FoodItem.OriginalPrice
                }).ToList(),
                StatusEvents = [new OrderStatusEvent { Status = "pending" }]
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            db.Payments.Add(new Payment { OrderId = order.Id, Amount = order.GrandTotal, PaymentMethod = order.PaymentMethod, Status = order.PaymentStatus });
            db.Notifications.Add(new AppNotification { UserId = userId, Title = "Order placed", Message = $"{order.OrderNumber} was placed successfully.", NotificationType = "order" });
            db.CartItems.RemoveRange(cart.Items); cart.RestaurantId = null; cart.CouponId = null;
            await db.SaveChangesAsync();
            order.Restaurant = cart.Restaurant;
            return Results.Created($"/api/orders/{order.Id}/", EndpointHelpers.OrderDto(order));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapPost("/orders/{id:int}/transition/", async (int id, OrderTransitionRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var order = await LoadOrderAsync(db, id);
            if (order is null) return Results.NotFound();
            var allowed = principal.IsInRole(Roles.Admin) ||
                (principal.IsInRole(Roles.RestaurantOwner) && order.Restaurant.OwnerId == principal.UserId() && OwnerTransition(order.Status, request.Status)) ||
                (principal.IsInRole(Roles.DeliveryPartner) && order.DeliveryPartnerId == principal.UserId() && DeliveryTransition(order.Status, request.Status));
            if (!allowed) return EndpointHelpers.Error("This order transition is not allowed.", 403);
            order.Status = request.Status;
            order.StatusEvents.Add(new OrderStatusEvent { Status = request.Status });
            if (request.Status == "delivered")
            {
                order.PaymentStatus = "paid";
                var payment = await db.Payments.SingleAsync(x => x.OrderId == order.Id);
                payment.Status = "paid"; payment.PaymentDate ??= DateTime.UtcNow;
            }
            db.Notifications.Add(new AppNotification { UserId = order.CustomerId, Title = EndpointHelpers.StatusLabel(request.Status), Message = $"{order.OrderNumber} is now {EndpointHelpers.StatusLabel(request.Status).ToLower()}.", NotificationType = "order" });
            await db.SaveChangesAsync();
            return Results.Ok(EndpointHelpers.OrderDto(order));
        }).RequireAuthorization();

        group.MapPost("/orders/{id:int}/cancel/", async (int id, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var order = await LoadOrderAsync(db, id);
            if (order is null) return Results.NotFound();
            if (order.CustomerId != principal.UserId() || order.Status is not ("pending" or "confirmed" or "accepted"))
                return EndpointHelpers.Error("This order can no longer be cancelled.", 403);
            order.Status = "cancelled"; order.StatusEvents.Add(new OrderStatusEvent { Status = "cancelled" });
            await db.SaveChangesAsync();
            return Results.Ok(EndpointHelpers.OrderDto(order));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        group.MapGet("/payments/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var userId = principal.UserId();
            var query = db.Payments.AsNoTracking().Include(x => x.Order).ThenInclude(x => x.Restaurant).AsQueryable();
            if (principal.IsInRole(Roles.Customer)) query = query.Where(x => x.Order.CustomerId == userId);
            else if (principal.IsInRole(Roles.RestaurantOwner)) query = query.Where(x => x.Order.Restaurant.OwnerId == userId);
            return Results.Ok(EndpointHelpers.Page((await query.OrderByDescending(x => x.Id).ToListAsync()).Select(EndpointHelpers.PaymentDto)));
        }).RequireAuthorization();

        group.MapPost("/payments/{id:int}/mock-checkout/", async (int id, MockPaymentRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var payment = await db.Payments.Include(x => x.Order).SingleOrDefaultAsync(x => x.Id == id);
            if (payment is null || payment.Order.CustomerId != principal.UserId()) return Results.NotFound();
            payment.Status = request.Success ? "paid" : "failed";
            payment.TransactionId = $"MOCK-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
            payment.PaymentDate = DateTime.UtcNow;
            payment.Order.PaymentStatus = payment.Status;
            db.Notifications.Add(new AppNotification { UserId = principal.UserId(), Title = request.Success ? "Payment successful" : "Payment failed", Message = $"Payment for {payment.Order.OrderNumber} was {payment.Status}.", NotificationType = "payment" });
            await db.SaveChangesAsync();
            return Results.Ok(EndpointHelpers.PaymentDto(payment));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Customer));

        return app;
    }

    private static async Task<Cart> LoadCartAsync(FoodFlowDbContext db, int userId)
    {
        var cart = await db.Carts.Include(x => x.Restaurant).Include(x => x.Coupon)
            .Include(x => x.Items).ThenInclude(x => x.FoodItem).ThenInclude(x => x.Restaurant)
            .Include(x => x.Items).ThenInclude(x => x.FoodItem).ThenInclude(x => x.Category)
            .SingleOrDefaultAsync(x => x.UserId == userId);
        if (cart is not null) return cart;
        cart = new Cart { UserId = userId }; db.Carts.Add(cart); await db.SaveChangesAsync(); return cart;
    }

    private static async Task<Order?> LoadOrderAsync(FoodFlowDbContext db, int id) =>
        await db.Orders.Include(x => x.Restaurant).Include(x => x.Items).Include(x => x.StatusEvents).SingleOrDefaultAsync(x => x.Id == id);

    private static object CartDto(Cart cart)
    {
        var subtotal = CartSubtotal(cart); var discount = Discount(cart.Coupon, subtotal);
        var fee = cart.Items.Count > 0 ? cart.Restaurant?.DeliveryFee ?? 0 : 0;
        var tax = Math.Round(subtotal * 0.05m, 2);
        return new
        {
            cart.Id, restaurant = cart.RestaurantId, coupon = cart.CouponId,
            items = cart.Items.Select(CartItemDto), subtotal = EndpointHelpers.Money(subtotal), tax = EndpointHelpers.Money(tax),
            delivery_fee = EndpointHelpers.Money(fee), discount, grand_total = subtotal + tax + fee - discount
        };
    }

    private static object CartItemDto(CartItem item)
    {
        var price = item.FoodItem.DiscountPrice ?? item.FoodItem.OriginalPrice;
        return new { item.Id, food_item = item.FoodItemId, food = EndpointHelpers.FoodDto(item.FoodItem), item.Quantity, unit_price = EndpointHelpers.Money(price), subtotal = EndpointHelpers.Money(price * item.Quantity) };
    }

    private static decimal CartSubtotal(Cart cart) => cart.Items.Sum(x => (x.FoodItem.DiscountPrice ?? x.FoodItem.OriginalPrice) * x.Quantity);
    private static decimal Discount(Coupon? coupon, decimal subtotal) => coupon is null ? 0 : Math.Min(subtotal,
        coupon.DiscountType == "percentage" ? Math.Round(subtotal * coupon.DiscountValue / 100, 2) : coupon.DiscountValue);
    private static bool CanAccess(Order order, ClaimsPrincipal principal) => principal.IsInRole(Roles.Admin) ||
        (principal.IsInRole(Roles.Customer) && order.CustomerId == principal.UserId()) ||
        (principal.IsInRole(Roles.RestaurantOwner) && order.Restaurant.OwnerId == principal.UserId()) ||
        (principal.IsInRole(Roles.DeliveryPartner) && order.DeliveryPartnerId == principal.UserId());
    private static bool OwnerTransition(string current, string next) => (current, next) is
        ("pending", "accepted") or ("confirmed", "accepted") or ("pending", "rejected") or ("confirmed", "rejected") or
        ("accepted", "preparing") or ("preparing", "ready");
    private static bool DeliveryTransition(string current, string next) => (current, next) is
        ("assigned", "picked_up") or ("picked_up", "on_the_way") or ("on_the_way", "delivered");
}

public record CartItemCreateRequest(int FoodItem, int Quantity = 1, bool Replace = false);
public record CartItemUpdateRequest(int Quantity);
public record CouponApplyRequest(string Code);
public record OrderCreateRequest(int DeliveryAddress, string PaymentMethod, string? CustomerNotes);
public record OrderTransitionRequest(string Status);
public record MockPaymentRequest(bool Success);

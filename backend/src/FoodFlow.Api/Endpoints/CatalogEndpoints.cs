using System.Security.Claims;
using FoodFlow.Api.Data;
using FoodFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Catalog");

        group.MapGet("/restaurants/", async (string? search, string? ordering, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var query = db.Restaurants.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(term) || x.CuisineType.ToLower().Contains(term) || x.City.ToLower().Contains(term));
            }
            query = ordering switch
            {
                "estimated_delivery_time" => query.OrderBy(x => x.EstimatedDeliveryTime),
                "delivery_fee" => query.OrderBy(x => x.DeliveryFee),
                "created_at" => query.OrderByDescending(x => x.CreatedAt),
                _ => query.OrderByDescending(x => x.AverageRating)
            };
            var rows = await query.ToListAsync();
            var favoriteIds = principal.Identity?.IsAuthenticated == true
                ? await db.Favorites.Where(x => x.UserId == principal.UserId()).Select(x => x.RestaurantId).ToListAsync()
                : [];
            return Results.Ok(EndpointHelpers.Page(rows.Select(x => EndpointHelpers.RestaurantDto(x, favoriteIds.Contains(x.Id)))));
        });

        group.MapGet("/restaurants/{id:int}/", async (int id, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var restaurant = await db.Restaurants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (restaurant is null) return Results.NotFound();
            var favorite = principal.Identity?.IsAuthenticated == true &&
                await db.Favorites.AnyAsync(x => x.UserId == principal.UserId() && x.RestaurantId == id);
            return Results.Ok(EndpointHelpers.RestaurantDto(restaurant, favorite));
        });

        group.MapPost("/restaurants/", async (RestaurantRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var ownerId = principal.UserId();
            if (await db.Restaurants.AnyAsync(x => x.OwnerId == ownerId)) return EndpointHelpers.Error("This owner already has a restaurant.");
            var restaurant = request.ToEntity(ownerId);
            db.Restaurants.Add(restaurant);
            await db.SaveChangesAsync();
            return Results.Created($"/api/restaurants/{restaurant.Id}/", EndpointHelpers.RestaurantDto(restaurant));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.RestaurantOwner));

        group.MapPatch("/restaurants/{id:int}/", async (int id, RestaurantUpdateRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var restaurant = await db.Restaurants.FindAsync(id);
            if (restaurant is null) return Results.NotFound();
            if (!principal.IsInRole(Roles.Admin) && restaurant.OwnerId != principal.UserId()) return Results.Forbid();
            request.Apply(restaurant);
            await db.SaveChangesAsync();
            return Results.Ok(EndpointHelpers.RestaurantDto(restaurant));
        }).RequireAuthorization();

        group.MapGet("/categories/", async (int? restaurant, FoodFlowDbContext db) =>
        {
            var query = db.Categories.AsNoTracking().AsQueryable();
            if (restaurant.HasValue) query = query.Where(x => x.RestaurantId == restaurant);
            return Results.Ok(EndpointHelpers.Page((await query.OrderBy(x => x.Name).ToListAsync()).Select(EndpointHelpers.CategoryDto)));
        });

        group.MapPost("/categories/", async (CategoryRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var restaurant = await db.Restaurants.FindAsync(request.Restaurant);
            if (restaurant is null) return EndpointHelpers.Error("Restaurant was not found.");
            if (!principal.IsInRole(Roles.Admin) && restaurant.OwnerId != principal.UserId()) return Results.Forbid();
            var category = new Category { RestaurantId = request.Restaurant, Name = request.Name.Trim(), Description = request.Description ?? "", Image = request.Image, IsActive = request.IsActive };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return Results.Created($"/api/categories/{category.Id}/", EndpointHelpers.CategoryDto(category));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.RestaurantOwner, Roles.Admin));

        group.MapGet("/foods/", async (int? restaurant, int? category, string? search, bool? is_available, FoodFlowDbContext db) =>
        {
            var query = db.FoodItems.AsNoTracking().Include(x => x.Restaurant).Include(x => x.Category).AsQueryable();
            if (restaurant.HasValue) query = query.Where(x => x.RestaurantId == restaurant);
            if (category.HasValue) query = query.Where(x => x.CategoryId == category);
            if (is_available.HasValue) query = query.Where(x => x.IsAvailable == is_available);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(term) || x.Description.ToLower().Contains(term));
            }
            return Results.Ok(EndpointHelpers.Page((await query.OrderBy(x => x.Name).ToListAsync()).Select(EndpointHelpers.FoodDto)));
        });

        group.MapGet("/foods/{id:int}/", async (int id, FoodFlowDbContext db) =>
        {
            var food = await db.FoodItems.AsNoTracking().Include(x => x.Restaurant).Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == id);
            return food is null ? Results.NotFound() : Results.Ok(EndpointHelpers.FoodDto(food));
        });

        group.MapPost("/foods/", async (FoodRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var restaurant = await db.Restaurants.FindAsync(request.Restaurant);
            if (restaurant is null) return EndpointHelpers.Error("Restaurant was not found.");
            if (!principal.IsInRole(Roles.Admin) && restaurant.OwnerId != principal.UserId()) return Results.Forbid();
            if (!await db.Categories.AnyAsync(x => x.Id == request.Category && x.RestaurantId == request.Restaurant)) return EndpointHelpers.Error("Choose a category from this restaurant.");
            var food = request.ToEntity();
            db.FoodItems.Add(food);
            await db.SaveChangesAsync();
            food.Restaurant = restaurant;
            food.Category = (await db.Categories.FindAsync(food.CategoryId))!;
            return Results.Created($"/api/foods/{food.Id}/", EndpointHelpers.FoodDto(food));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.RestaurantOwner, Roles.Admin));

        group.MapPatch("/foods/{id:int}/", async (int id, FoodUpdateRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var food = await db.FoodItems.Include(x => x.Restaurant).Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == id);
            if (food is null) return Results.NotFound();
            if (!principal.IsInRole(Roles.Admin) && food.Restaurant.OwnerId != principal.UserId()) return Results.Forbid();
            request.Apply(food);
            await db.SaveChangesAsync();
            return Results.Ok(EndpointHelpers.FoodDto(food));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.RestaurantOwner, Roles.Admin));

        group.MapDelete("/foods/{id:int}/", async (int id, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var food = await db.FoodItems.Include(x => x.Restaurant).SingleOrDefaultAsync(x => x.Id == id);
            if (food is null) return Results.NotFound();
            if (!principal.IsInRole(Roles.Admin) && food.Restaurant.OwnerId != principal.UserId()) return Results.Forbid();
            db.FoodItems.Remove(food);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(Roles.RestaurantOwner, Roles.Admin));

        return app;
    }
}

public record RestaurantRequest(string Name, string? Description, string Phone, string Email, string Address, string City,
    string State, string PostalCode, string CuisineType, string OpeningTime, string ClosingTime,
    decimal MinimumOrder, decimal DeliveryFee, int EstimatedDeliveryTime, bool IsActive)
{
    public Restaurant ToEntity(int ownerId) => new()
    {
        OwnerId = ownerId, Name = Name.Trim(), Description = Description ?? "", Phone = Phone, Email = Email,
        Address = Address, City = City, State = State, PostalCode = PostalCode, CuisineType = CuisineType,
        OpeningTime = TimeOnly.Parse(OpeningTime), ClosingTime = TimeOnly.Parse(ClosingTime),
        MinimumOrder = MinimumOrder, DeliveryFee = DeliveryFee, EstimatedDeliveryTime = EstimatedDeliveryTime, IsActive = IsActive
    };
}

public record RestaurantUpdateRequest(string? Name, string? Description, string? Phone, string? Email, string? Address,
    string? City, string? State, string? PostalCode, string? CuisineType, string? OpeningTime, string? ClosingTime,
    decimal? MinimumOrder, decimal? DeliveryFee, int? EstimatedDeliveryTime, bool? IsActive)
{
    public void Apply(Restaurant value)
    {
        if (Name is not null) value.Name = Name; if (Description is not null) value.Description = Description;
        if (Phone is not null) value.Phone = Phone; if (Email is not null) value.Email = Email;
        if (Address is not null) value.Address = Address; if (City is not null) value.City = City;
        if (State is not null) value.State = State; if (PostalCode is not null) value.PostalCode = PostalCode;
        if (CuisineType is not null) value.CuisineType = CuisineType;
        if (OpeningTime is not null) value.OpeningTime = TimeOnly.Parse(OpeningTime);
        if (ClosingTime is not null) value.ClosingTime = TimeOnly.Parse(ClosingTime);
        if (MinimumOrder.HasValue) value.MinimumOrder = MinimumOrder.Value;
        if (DeliveryFee.HasValue) value.DeliveryFee = DeliveryFee.Value;
        if (EstimatedDeliveryTime.HasValue) value.EstimatedDeliveryTime = EstimatedDeliveryTime.Value;
        if (IsActive.HasValue) value.IsActive = IsActive.Value;
    }
}

public record CategoryRequest(int Restaurant, string Name, string? Description, string? Image, bool IsActive);
public record FoodRequest(int Restaurant, int Category, string Name, string? Description, string? Image,
    decimal OriginalPrice, decimal? DiscountPrice, bool IsVegetarian, bool IsAvailable, int PreparationTime)
{
    public FoodItem ToEntity() => new()
    {
        RestaurantId = Restaurant, CategoryId = Category, Name = Name.Trim(), Description = Description ?? "", Image = Image,
        OriginalPrice = OriginalPrice, DiscountPrice = DiscountPrice, IsVegetarian = IsVegetarian,
        IsAvailable = IsAvailable, PreparationTime = PreparationTime
    };
}
public record FoodUpdateRequest(string? Name, string? Description, int? Category, decimal? OriginalPrice,
    decimal? DiscountPrice, bool? IsVegetarian, bool? IsAvailable, int? PreparationTime)
{
    public void Apply(FoodItem value)
    {
        if (Name is not null) value.Name = Name; if (Description is not null) value.Description = Description;
        if (Category.HasValue) value.CategoryId = Category.Value;
        if (OriginalPrice.HasValue) value.OriginalPrice = OriginalPrice.Value;
        if (DiscountPrice.HasValue) value.DiscountPrice = DiscountPrice.Value;
        if (IsVegetarian.HasValue) value.IsVegetarian = IsVegetarian.Value;
        if (IsAvailable.HasValue) value.IsAvailable = IsAvailable.Value;
        if (PreparationTime.HasValue) value.PreparationTime = PreparationTime.Value;
    }
}

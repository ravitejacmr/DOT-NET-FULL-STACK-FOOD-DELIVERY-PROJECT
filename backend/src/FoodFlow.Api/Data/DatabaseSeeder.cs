using FoodFlow.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<FoodFlowDbContext>();
        if (await db.Users.AnyAsync()) return;

        var hasher = services.GetRequiredService<IPasswordHasher<AppUser>>();
        AppUser User(string email, string first, string last, string phone, string role)
        {
            var user = new AppUser { Email = email, FirstName = first, LastName = last, Phone = phone, Role = role };
            user.PasswordHash = hasher.HashPassword(user, "FoodFlow@123");
            return user;
        }

        var admin = User("admin@example.com", "Platform", "Admin", "9000000001", Roles.Admin);
        var customer = User("customer@example.com", "Aarav", "Sharma", "9000000002", Roles.Customer);
        var customer2 = User("customer2@example.com", "Meera", "Nair", "9000000003", Roles.Customer);
        var owner = User("owner@example.com", "Priya", "Rao", "9000000004", Roles.RestaurantOwner);
        var owner2 = User("owner2@example.com", "Vikram", "Singh", "9000000005", Roles.RestaurantOwner);
        var delivery = User("delivery@example.com", "Ravi", "Kumar", "9000000006", Roles.DeliveryPartner);
        db.Users.AddRange(admin, customer, customer2, owner, owner2, delivery);
        await db.SaveChangesAsync();

        var spiceRoute = new Restaurant
        {
            OwnerId = owner.Id, Name = "Spice Route", Description = "Aromatic biryanis and classic Indian favourites.",
            Phone = "08040001001", Email = "spice@example.com", Address = "12 Residency Road", City = "Bengaluru",
            State = "Karnataka", PostalCode = "560025", CuisineType = "Biryani, North Indian", DeliveryFee = 39,
            MinimumOrder = 149, EstimatedDeliveryTime = 32, AverageRating = 4.7m
        };
        var pizzaCraft = new Restaurant
        {
            OwnerId = owner2.Id, Name = "Pizza Craft", Description = "Hand-stretched pizzas baked fresh to order.",
            Phone = "08040001002", Email = "pizza@example.com", Address = "88 Indiranagar Main Road", City = "Bengaluru",
            State = "Karnataka", PostalCode = "560038", CuisineType = "Pizza, Italian", DeliveryFee = 49,
            MinimumOrder = 199, EstimatedDeliveryTime = 28, AverageRating = 4.5m
        };
        db.Restaurants.AddRange(spiceRoute, pizzaCraft);
        await db.SaveChangesAsync();

        var biryani = new Category { RestaurantId = spiceRoute.Id, Name = "Biryani", Description = "Slow-cooked rice dishes" };
        var indian = new Category { RestaurantId = spiceRoute.Id, Name = "North Indian", Description = "Comforting Indian classics" };
        var pizza = new Category { RestaurantId = pizzaCraft.Id, Name = "Pizza", Description = "Stone-baked pizzas" };
        var drinks = new Category { RestaurantId = pizzaCraft.Id, Name = "Beverages", Description = "Cold drinks" };
        db.Categories.AddRange(biryani, indian, pizza, drinks);
        await db.SaveChangesAsync();

        db.FoodItems.AddRange(
            new FoodItem { RestaurantId = spiceRoute.Id, CategoryId = biryani.Id, Name = "Chicken Biryani", Description = "Fragrant basmati rice with tender chicken.", OriginalPrice = 299, DiscountPrice = 269, PreparationTime = 25, Rating = 4.8m },
            new FoodItem { RestaurantId = spiceRoute.Id, CategoryId = biryani.Id, Name = "Veg Biryani", Description = "Garden vegetables, saffron and aromatic rice.", OriginalPrice = 239, DiscountPrice = 219, IsVegetarian = true, PreparationTime = 22, Rating = 4.6m },
            new FoodItem { RestaurantId = spiceRoute.Id, CategoryId = indian.Id, Name = "Paneer Butter Masala", Description = "Paneer in a rich tomato butter gravy.", OriginalPrice = 279, IsVegetarian = true, PreparationTime = 20, Rating = 4.5m },
            new FoodItem { RestaurantId = pizzaCraft.Id, CategoryId = pizza.Id, Name = "Margherita Pizza", Description = "Tomato, mozzarella and fresh basil.", OriginalPrice = 329, IsVegetarian = true, PreparationTime = 20, Rating = 4.7m },
            new FoodItem { RestaurantId = pizzaCraft.Id, CategoryId = pizza.Id, Name = "Chicken Pizza", Description = "Roast chicken, peppers and mozzarella.", OriginalPrice = 429, DiscountPrice = 399, PreparationTime = 24, Rating = 4.6m },
            new FoodItem { RestaurantId = pizzaCraft.Id, CategoryId = drinks.Id, Name = "Lime Soda", Description = "Fresh lime soda served chilled.", OriginalPrice = 89, IsVegetarian = true, PreparationTime = 5, Rating = 4.3m });
        db.Coupons.AddRange(
            new Coupon { Code = "WELCOME10", DiscountType = "percentage", DiscountValue = 10, MinimumOrderAmount = 199, ExpiryDate = DateTime.UtcNow.AddYears(1) },
            new Coupon { Code = "FLAT50", DiscountType = "fixed", DiscountValue = 50, MinimumOrderAmount = 499, ExpiryDate = DateTime.UtcNow.AddYears(1) });
        db.Addresses.Add(new Address
        {
            UserId = customer.Id, FullName = "Aarav Sharma", Phone = customer.Phone, HouseNumber = "24B",
            Street = "MG Road", Landmark = "Near Metro Station", City = "Bengaluru", State = "Karnataka",
            PostalCode = "560001", AddressType = "home", IsDefault = true
        });
        db.Notifications.Add(new AppNotification
        {
            UserId = customer.Id, Title = "Welcome to FoodFlow", Message = "Your account is ready. Discover something delicious!",
            NotificationType = "account"
        });
        await db.SaveChangesAsync();
    }
}

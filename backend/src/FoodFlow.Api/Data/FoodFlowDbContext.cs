using FoodFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Api.Data;

public class FoodFlowDbContext(DbContextOptions<FoodFlowDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FoodItem> FoodItems => Set<FoodItem>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusEvent> OrderStatusEvents => Set<OrderStatusEvent>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<Restaurant>().HasIndex(x => x.OwnerId).IsUnique();
        modelBuilder.Entity<Favorite>().HasIndex(x => new { x.UserId, x.RestaurantId }).IsUnique();
        modelBuilder.Entity<Coupon>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Cart>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<CartItem>().HasIndex(x => new { x.CartId, x.FoodItemId }).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(x => x.OrderNumber).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(x => x.OrderId).IsUnique();
        modelBuilder.Entity<Review>().HasIndex(x => new { x.CustomerId, x.OrderId }).IsUnique();

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(10);
            property.SetScale(2);
        }

        modelBuilder.Entity<Restaurant>().HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Category>().HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<FoodItem>().HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FoodItem>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Payment>().HasOne(x => x.Order).WithOne().HasForeignKey<Payment>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

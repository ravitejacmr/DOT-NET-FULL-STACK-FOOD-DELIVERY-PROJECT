using System.Security.Claims;
using FoodFlow.Api.Data;
using FoodFlow.Api.Models;
using FoodFlow.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register/", async (RegisterRequest request, FoodFlowDbContext db, IPasswordHasher<AppUser> hasher) =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(x => x.Email == email)) return EndpointHelpers.Error("An account with this email already exists.");
            if (!Roles.All.Contains(request.Role) || request.Role == Roles.Admin) return EndpointHelpers.Error("Choose a valid account role.");
            if (request.Password.Length < 8 || request.Password != request.ConfirmPassword) return EndpointHelpers.Error("Passwords must match and contain at least 8 characters.");
            var user = new AppUser
            {
                Email = email, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
                Phone = request.Phone.Trim(), Role = request.Role
            };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Created($"/api/auth/profile/", EndpointHelpers.UserDto(user));
        });

        group.MapPost("/login/", async (LoginRequest request, FoodFlowDbContext db, IPasswordHasher<AppUser> hasher, JwtTokenService tokens) =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
            if (user is null || !user.IsActive || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
                return EndpointHelpers.Error("Invalid email or password.", StatusCodes.Status401Unauthorized);
            var refresh = await tokens.CreateRefreshTokenAsync(user);
            return Results.Ok(new { access = tokens.CreateAccessToken(user), refresh, user = EndpointHelpers.UserDto(user) });
        });

        group.MapPost("/token/refresh/", async (RefreshRequest request, FoodFlowDbContext db, JwtTokenService tokens) =>
        {
            var hash = JwtTokenService.Hash(request.Refresh);
            var stored = await db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == hash);
            if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= DateTime.UtcNow || !stored.User.IsActive)
                return EndpointHelpers.Error("Refresh token is invalid or expired.", StatusCodes.Status401Unauthorized);
            stored.RevokedAt = DateTime.UtcNow;
            var refresh = await tokens.CreateRefreshTokenAsync(stored.User);
            await db.SaveChangesAsync();
            return Results.Ok(new { access = tokens.CreateAccessToken(stored.User), refresh });
        });

        group.MapPost("/logout/", async (RefreshRequest request, FoodFlowDbContext db) =>
        {
            var hash = JwtTokenService.Hash(request.Refresh);
            var stored = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash);
            if (stored is not null) { stored.RevokedAt = DateTime.UtcNow; await db.SaveChangesAsync(); }
            return Results.NoContent();
        });

        group.MapGet("/profile/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var user = await db.Users.FindAsync(principal.UserId());
            return user is null ? Results.NotFound() : Results.Ok(EndpointHelpers.UserDto(user));
        }).RequireAuthorization();

        group.MapPatch("/profile/", async (ProfileUpdateRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var user = await db.Users.FindAsync(principal.UserId());
            if (user is null) return Results.NotFound();
            if (!string.IsNullOrWhiteSpace(request.FirstName)) user.FirstName = request.FirstName.Trim();
            if (!string.IsNullOrWhiteSpace(request.LastName)) user.LastName = request.LastName.Trim();
            if (!string.IsNullOrWhiteSpace(request.Phone)) user.Phone = request.Phone.Trim();
            await db.SaveChangesAsync();
            return Results.Ok(EndpointHelpers.UserDto(user));
        }).RequireAuthorization();

        group.MapPost("/password/change/", async (PasswordChangeRequest request, ClaimsPrincipal principal, FoodFlowDbContext db, IPasswordHasher<AppUser> hasher) =>
        {
            var user = await db.Users.FindAsync(principal.UserId());
            if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
                return EndpointHelpers.Error("Current password is incorrect.");
            if (request.NewPassword.Length < 8) return EndpointHelpers.Error("New password must contain at least 8 characters.");
            user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
            await db.SaveChangesAsync();
            return Results.Ok(new { detail = "Password changed successfully." });
        }).RequireAuthorization();

        group.MapPost("/password/forgot/", (ForgotPasswordRequest request) =>
            Results.Ok(new { detail = $"If an account exists for {request.Email}, password-reset instructions have been generated." }));

        group.MapGet("/addresses/", async (ClaimsPrincipal principal, FoodFlowDbContext db) =>
            Results.Ok(EndpointHelpers.Page(await db.Addresses.AsNoTracking().Where(x => x.UserId == principal.UserId()).OrderByDescending(x => x.IsDefault).ToListAsync())))
            .RequireAuthorization();

        group.MapPost("/addresses/", async (AddressRequest request, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var userId = principal.UserId();
            if (request.IsDefault) await db.Addresses.Where(x => x.UserId == userId).ExecuteUpdateAsync(x => x.SetProperty(a => a.IsDefault, false));
            var address = request.ToEntity(userId);
            if (!await db.Addresses.AnyAsync(x => x.UserId == userId)) address.IsDefault = true;
            db.Addresses.Add(address);
            await db.SaveChangesAsync();
            return Results.Created($"/api/auth/addresses/{address.Id}/", address);
        }).RequireAuthorization();

        group.MapDelete("/addresses/{id:int}/", async (int id, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var address = await db.Addresses.SingleOrDefaultAsync(x => x.Id == id && x.UserId == principal.UserId());
            if (address is null) return Results.NotFound();
            db.Addresses.Remove(address);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/addresses/{id:int}/set_default/", async (int id, ClaimsPrincipal principal, FoodFlowDbContext db) =>
        {
            var userId = principal.UserId();
            var address = await db.Addresses.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (address is null) return Results.NotFound();
            await db.Addresses.Where(x => x.UserId == userId).ExecuteUpdateAsync(x => x.SetProperty(a => a.IsDefault, false));
            address.IsDefault = true;
            await db.SaveChangesAsync();
            return Results.Ok(address);
        }).RequireAuthorization();

        return app;
    }
}

public record RegisterRequest(string FirstName, string LastName, string Email, string Phone, string Role, string Password, string ConfirmPassword);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string Refresh);
public record ProfileUpdateRequest(string? FirstName, string? LastName, string? Phone);
public record PasswordChangeRequest(string CurrentPassword, string NewPassword);
public record ForgotPasswordRequest(string Email);
public record AddressRequest(string FullName, string Phone, string HouseNumber, string Street, string? Landmark, string City, string State, string PostalCode, string AddressType, bool IsDefault)
{
    public Address ToEntity(int userId) => new()
    {
        UserId = userId, FullName = FullName, Phone = Phone, HouseNumber = HouseNumber, Street = Street,
        Landmark = Landmark ?? "", City = City, State = State, PostalCode = PostalCode,
        AddressType = AddressType, IsDefault = IsDefault
    };
}

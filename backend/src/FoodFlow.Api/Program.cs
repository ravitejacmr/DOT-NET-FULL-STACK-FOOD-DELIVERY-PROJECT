using System.Text;
using System.Text.Json;
using FoodFlow.Api.Data;
using FoodFlow.Api.Endpoints;
using FoodFlow.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});

var connectionString = builder.Configuration.GetConnectionString("FoodFlow")
    ?? throw new InvalidOperationException("ConnectionStrings:FoodFlow is not configured.");
builder.Services.AddDbContext<FoodFlowDbContext>(options =>
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 4, 0))));

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.IPasswordHasher<FoodFlow.Api.Models.AppUser>,
    Microsoft.AspNetCore.Identity.PasswordHasher<FoodFlow.Api.Models.AppUser>>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("Angular", policy => policy
    .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "FoodFlow ASP.NET Core API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization", Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    options.AddSecurityRequirement(new()
    {
        [new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    context.Response.StatusCode = exception is ArgumentException ? 400 : 500;
    await context.Response.WriteAsJsonAsync(new
    {
        error = new { details = exception is ArgumentException ? exception.Message : "An unexpected server error occurred." }
    });
}));
app.UseCors("Angular");
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "FoodFlow API v1"));
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/api/health/", () => Results.Ok(new { status = "healthy", service = "FoodFlow.Api" }));
app.MapAuthEndpoints();
app.MapCatalogEndpoints();
app.MapCartOrderEndpoints();
app.MapEngagementEndpoints();
app.MapDashboardDeliveryEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FoodFlowDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

public partial class Program;

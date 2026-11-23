using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.EntityFrameworkCore;
using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Services;
using Bruktborsen.Api.Repository;
using Bruktborsen.Api.Services.Interfaces;
using Bruktborsen.Api.Repository.Interfaces;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

// Identity with roles and API endpoints
builder
    .Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization();

// Category repository and service
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IListingRepository, ListingRepository>();
builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// CORS for local Blazor development
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "BlazorClient",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",
                    "https://localhost:7001",
                    "http://localhost:5001",
                    "https://localhost:7002",
                    "http://localhost:5002"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

// Controllers and OpenAPI
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(
                allowIntegerValues: false
            )
        );
    });

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("BlazorClient");

app.UseAuthentication();
app.UseAuthorization();

// ASP.NET Core Identity handles login and token management.
// AuthController handles custom registration and /me.

app.MapGroup("/api/auth")
    .MapIdentityApi<ApplicationUser>()
    .Add(endpointBuilder =>
    {
        if (endpointBuilder is not RouteEndpointBuilder route)
        {
            return;
        }

        var pattern = route.RoutePattern.RawText;

        if (pattern is null ||
            !pattern.EndsWith(
                "/register",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Disable the built-in registration endpoint.
        // AuthController handles POST /api/auth/register.
        route.RoutePattern = RoutePatternFactory.Parse(
            "/identity-register-disabled");

        route.RequestDelegate = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status404NotFound;

            return Task.CompletedTask;
        };
    });

app.MapControllers();

// Seed demo users and roles in development only.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    await SeedData.InitializeAsync(
        scope.ServiceProvider
        );

    var context = scope.ServiceProvider
    .GetRequiredService<ApplicationDbContext>();

    await MarketSeedData.InitializeAsync(context);
    await MarketInteractionSeedData.InitializeAsync(context);
}

app.Run();

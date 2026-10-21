
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using AzureOmsDemo.Data;
using AzureOmsDemo.Models;
using AzureOmsDemo.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register controllers
builder.Services.AddControllers();

// Azure SQL
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 2,
                maxRetryDelay: TimeSpan.FromSeconds(3),
                errorNumbersToAdd: null
            );
        }
    )
);

// Azure Blob Storage
var blobServiceUri = builder.Configuration[
    "AzureBlob:ServiceUri"
] ?? throw new InvalidOperationException(
    "Missing AzureBlob:ServiceUri configuration.");

builder.Services.AddScoped<ProductImageStorageService>();

TokenCredential credential;

if (builder.Environment.IsDevelopment())
{
    // Local: authenticate using Azure CLI
    credential = new AzureCliCredential();
}
else
{
    // Azure App Service: system-assigned Managed Identity
    credential = new ManagedIdentityCredential(
        ManagedIdentityId.SystemAssigned);
}

builder.Services.AddSingleton(
    new BlobServiceClient(
        new Uri(blobServiceUri),
        credential));

var vaultUri = builder.Configuration["AzureKeyVault:VaultUri"]
    ?? throw new InvalidOperationException(
        "Missing AzureKeyVault:VaultUri configuration.");

builder.Services.AddSingleton(
    new SecretClient(
        new Uri(vaultUri),
        credential
    )
);

builder.Services.AddSingleton<ShippingSecretVerifier>();

var app = builder.Build();

// Map controller endpoints
app.MapControllers();

// Health check
app.MapGet("/health", () =>
    Results.Ok(new { Status = "Healthy" }));

// Get all orders
app.MapGet("/api/orders", async (AppDbContext db) =>
{
    var orders = await db.Orders
        .AsNoTracking()
        .OrderBy(x => x.Id)
        .ToListAsync();

    return Results.Ok(orders);
});

// Get order by ID
app.MapGet("/api/orders/{id:int}",
    async (int id, AppDbContext db) =>
    {
        var order = await db.Orders.FindAsync(id);

        return order is null
            ? Results.NotFound()
            : Results.Ok(order);
    });

// Create order (Development only)
if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/orders", async (
        CreateOrderRequest request,
        AppDbContext db) =>
    {
        if (string.IsNullOrWhiteSpace(request.OrderCode)
            || request.TotalAmount <= 0)
        {
            return Results.BadRequest(
                "OrderCode and TotalAmount are invalid.");
        }

        var order = new Order
        {
            OrderCode = request.OrderCode,
            TotalAmount = request.TotalAmount,
            Status = "Pending"
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/orders/{order.Id}", order);
    });

    app.MapGet("/dev/key-vault/check", async (ShippingSecretVerifier verifier) =>
    {
        bool configured = await verifier.IsConfiguredAsync();

        return Results.Ok(new
        {
            configured
        });
    });
}

app.Run();

record CreateOrderRequest(
    string OrderCode,
    decimal TotalAmount
);

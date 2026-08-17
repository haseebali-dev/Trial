using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CryptoTrading.API.Data;
using CryptoTrading.API.Configuration;
using CryptoTrading.API.Middleware;
using CryptoTrading.API.Interfaces;
using CryptoTrading.API.MarketData;
using CryptoTrading.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure EF Core with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=crypto_trading.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:4201")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure custom settings
builder.Services.Configure<MarketDataSettings>(builder.Configuration.GetSection("MarketData"));
builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<ScannerSettings>(builder.Configuration.GetSection("Scanner"));

// Register Market Data Services
builder.Services.AddHttpClient<BinanceMarketDataProvider>();
builder.Services.AddHttpClient<CoinGeckoMarketDataProvider>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<BinanceMarketDataProvider>();
builder.Services.AddScoped<CoinGeckoMarketDataProvider>();
builder.Services.AddScoped<IMarketDataService>(sp =>
{
    var providers = new IMarketDataService[]
    {
        sp.GetRequiredService<BinanceMarketDataProvider>(),
        sp.GetRequiredService<CoinGeckoMarketDataProvider>()
    };
    return new MarketDataService(providers, sp.GetRequiredService<ILogger<MarketDataService>>(), sp.GetRequiredService<IOptions<MarketDataSettings>>());
});

// Register Technical Analysis Services
builder.Services.AddScoped<ITechnicalAnalysisService, TechnicalAnalysisService>();

// Register Signal Generation Services
builder.Services.AddScoped<ISignalGenerationService, SignalGenerationService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add global error handling
builder.Services.AddProblemDetails();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Commented out to avoid HTTPS issues

app.UseCors("AllowFrontend");

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Add explicit URL configuration - use port from environment or default to 5003
var port = Environment.GetEnvironmentVariable("PORT") ?? "5003";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
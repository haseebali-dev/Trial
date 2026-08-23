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

// Register Pattern Recognition Services
builder.Services.AddScoped<IPatternRecognitionService, PatternRecognitionService>();

// Register Smart Money Concepts Services
builder.Services.AddScoped<ISmartMoneyConceptsService, SmartMoneyConceptsService>();

// Register Scanner Services
builder.Services.AddScoped<IScannerService, ScannerService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Crypto Trading API",
        Version = "v1",
        Description = "API for Crypto Trading Intelligence Platform - Market Data, Technical Analysis, Signals, AI Analysis, Scanner, Backtesting, and more",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Crypto Trading API",
            Email = "support@cryptotrading.api"
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add server information
    c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer
    {
        Url = "http://localhost:5003",
        Description = "Development Server"
    });
});

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
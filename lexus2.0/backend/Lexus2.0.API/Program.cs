using Lexus2_0.Core.Logging;
using Lexus2_0.Core.Config;
using Lexus2_0.Automation;
using CoreLogger = Lexus2_0.Core.Logging.ILogger;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => 
        {
            // Allow null origin (file:// protocol) and common localhost origins
            if (string.IsNullOrEmpty(origin)) return true;
            return origin == "null" || 
                   origin.StartsWith("http://localhost") || 
                   origin.StartsWith("http://127.0.0.1");
        })
        .AllowAnyHeader()
        .AllowAnyMethod();
        // Note: AllowCredentials() removed to support null origin (file://)
    });
});

// Register services
var inMemoryLogger = new Lexus2_0.Core.Logging.InMemoryLogger();
builder.Services.AddSingleton<CoreLogger>(inMemoryLogger);
builder.Services.AddSingleton<Lexus2_0.Core.Logging.InMemoryLogger>(inMemoryLogger);
builder.Services.AddSingleton<ConfigManager>();
builder.Services.AddSingleton<AutomationEngine>();

var app = builder.Build();

// Configure pipeline
app.UseCors("AllowFrontend");
app.UseRouting();

// Add root route for health check
app.MapGet("/", () => new
{
    status = "running",
    message = "Lexus 2.0 IRCTC Booking API",
    version = "2.0.0",
        endpoints = new
        {
            booking = "/api/booking",
            proxy = "/api/proxy",
            token = "/api/token",
            logs = "/api/logs",
            health = "/api/health"
        }
});

// Add health endpoint
app.MapGet("/api/health", () => new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "2.0.0"
});

// Add booking info endpoint
app.MapGet("/api/booking", () => new
{
    message = "IRCTC Booking API",
    endpoints = new
    {
        start = "POST /api/booking/start",
        stop = "POST /api/booking/stop",
        pause = "POST /api/booking/pause",
        resume = "POST /api/booking/resume",
        status = "GET /api/booking/status",
        saveConfig = "POST /api/booking/config/save",
        loadConfig = "GET /api/booking/config/load"
    }
});

// Add token info endpoint
app.MapGet("/api/token", () => new
{
    message = "Token API (TeslaX-style)",
    endpoints = new
    {
        fetch = "POST /api/token/fetch",
        validate = "GET /api/token/validate?token=xxx"
    }
});

app.MapControllers();

// Set default port
app.Urls.Add("http://localhost:5000");

app.Run();


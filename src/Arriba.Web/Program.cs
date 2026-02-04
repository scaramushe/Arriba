using Arriba.Core.Services;
using Arriba.Web.Middleware;

// Cache control constants
const int OneYearInSeconds = 31536000; // 1 year
const int OneHourInSeconds = 3600; // 1 hour

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Arriba - Aruba Portal Wrapper", Version = "v1" });
});

// Configure HTTP client for Aruba API
// Use mock client in development if configured
var useMockClient = builder.Configuration.GetValue<bool>("Aruba:UseMockClient");

if (useMockClient)
{
    builder.Services.AddScoped<IArubaApiClient, MockArubaApiClient>();
    builder.Services.AddLogging();
}
else
{
    builder.Services.AddHttpClient<IArubaApiClient, ArubaApiClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });
}

// Register services
builder.Services.AddScoped<IArubaService, ArubaService>();
builder.Services.AddSingleton<ITokenStorage, InMemoryTokenStorage>();

// Configure CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // WARNING: This CORS policy allows requests from any origin
        // For production, specify allowed origins explicitly:
        // policy.WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure response caching
builder.Services.AddResponseCaching();

var app = builder.Build();

// Configure middleware
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseResponseCaching();

// Serve static files (frontend)
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 1 year (for versioned assets)
        if (ctx.File.Name.EndsWith(".js") || ctx.File.Name.EndsWith(".css"))
        {
            ctx.Context.Response.Headers.CacheControl = $"public, max-age={OneYearInSeconds}, immutable";
        }
        // Cache HTML for 1 hour
        else if (ctx.File.Name.EndsWith(".html"))
        {
            ctx.Context.Response.Headers.CacheControl = $"public, max-age={OneHourInSeconds}";
        }
    }
});

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }

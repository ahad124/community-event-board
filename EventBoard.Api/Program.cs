using System.Text;
using System.Text.Json.Serialization;
using EventBoard.Api.Data;
using EventBoard.Api.Repositories;
using EventBoard.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging with Serilog -> Console + Seq.
// The Seq server URL comes from configuration ("Seq:ServerUrl"), overridable via
// the Seq__ServerUrl environment variable (set to http://seq:80 in docker-compose).
// All existing ILogger<T> calls flow through Serilog unchanged.
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

// Add services to the container
// Serialize/accept enums as their string names (e.g. BookingStatus "Confirmed")
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Add DbContext (SQL Server; connection string comes from configuration/env)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IEventService, EventService>();

// Add Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Weather service: typed HttpClient with a Polly retry policy.
// Retries transient failures 3 times, waiting 2 seconds between each attempt.
builder.Services.AddHttpClient<IWeatherService, WeatherService>(client =>
{
    var baseUrl = builder.Configuration["OpenWeather:BaseUrl"] ?? "https://api.openweathermap.org";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError() // 5xx, 408, and HttpRequestException
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: _ => TimeSpan.FromSeconds(2)));

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS. Allowed origins are configurable via "Cors:AllowedOrigins"
// (e.g. Cors__AllowedOrigins__0=http://localhost). When none are configured we
// fall back to AllowAnyOrigin for local development convenience.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Log every HTTP request as a single structured event (method, path, status
// code, elapsed ms). Feeds the "Slow Endpoint" runbook query (Elapsed > 1000).
app.UseSerilogRequestLogging();

// Global error handling.
// In Development we surface the detailed developer exception page to aid
// debugging. In every other environment (incl. Production) unhandled exceptions
// are converted to a generic RFC 7807 ProblemDetails 500 that reveals no stack
// trace, exception type, or internal details to the client.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "The server encountered an error while processing your request."
            };

            await context.Response.WriteAsJsonAsync(problem);
        });
    });
}

// Configure the HTTP request pipeline.
// Swagger is enabled in every environment so reviewers can explore the API
// in the Docker deployment as well.
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS redirection is opt-in. It is disabled by default so the app works over
// plain HTTP inside Docker (behind Nginx) and in local dev without a cert.
if (builder.Configuration.GetValue("UseHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

// Serve uploaded event images from wwwroot/uploads
app.UseStaticFiles();

app.UseCors("AllowFrontend");

// Authentication & Authorization middleware (order matters!)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations & seed database on startup.
// The 'sqlserver' service is already gated on a Docker health check, so only a
// few retries are needed to cover the brief window before it accepts connections.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 3;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            DbInitializer.Seed(db);
            logger.LogInformation("Database migrated and seeded successfully.");
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex,
                "Database not ready (attempt {Attempt}/{Max}). Retrying in 5s...",
                attempt, maxAttempts);
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            // Final attempt failed. Do NOT crash the process — start the app anyway
            // so it can serve health/diagnostics and so a misconfiguration surfaces
            // as observable request-time errors (logged to Seq) rather than a boot
            // crash loop. DB-backed endpoints will fail until the config is fixed.
            logger.LogError(ex,
                "Database initialization failed after {Max} attempts. Starting the " +
                "application anyway; database-backed endpoints will fail until the " +
                "connection is restored.", maxAttempts);
        }
    }
}

app.Run();
public partial class Program { }
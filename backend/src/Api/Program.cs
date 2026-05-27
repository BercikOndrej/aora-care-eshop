using AoraCare.Api.Data;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ── Authentication ──────────────────────────────────────────────────────────
// Custom stateless JWT — no third-party identity provider.
// SecretKey must be at least 32 chars; loaded from config (never hardcoded).
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

builder.Services.AddAuthorization();

// ── Background jobs ─────────────────────────────────────────────────────────
// Hangfire runs in-process; PostgreSQL is used as the job store.
// Dashboard is restricted to Development — gate it behind auth in production.
var hangfireConnectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(hangfireConnectionString)));

builder.Services.AddHangfireServer();

// ── MVC Controllers ─────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── OpenAPI ─────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Hangfire dashboard available at /jobs in development only.
    app.UseHangfireDashboard(
        builder.Configuration["Hangfire:DashboardPath"] ?? "/jobs");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ── Controllers ─────────────────────────────────────────────────────────────
app.MapControllers();

app.Run();

// Expose the entry-point type for integration testing via WebApplicationFactory<T>.
public partial class Program { }

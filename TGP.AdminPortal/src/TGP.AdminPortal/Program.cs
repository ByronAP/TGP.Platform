using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using TGP.Data;
using TGP.Data.Configuration;
using TGP.Data.Repositories;
using TGP.Data.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console().ReadFrom.Configuration(ctx.Configuration));

// Azure Key Vault Configuration
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// Add Razor Pages
builder.Services.AddRazorPages(options =>
{
    // Require SystemAdmin role for all pages except login
    options.Conventions.AuthorizeFolder("/", "SystemAdminPolicy");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Error");
});

builder.Services.AddHttpContextAccessor();

// Redis Configuration
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
var redisInstanceName = builder.Configuration["Redis:InstanceName"] ?? "TGP_Admin_";
var redisEnabled = builder.Configuration.GetValue<bool>("Redis:Enabled");

if (redisEnabled && !string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = redisInstanceName;
        });
        Log.Information("Redis cache configured");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to configure Redis. Using in-memory cache.");
        builder.Services.AddDistributedMemoryCache();
    }
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// Session Configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Name = "TGP.Admin.Session";
});

// Authentication - Cookie-based via SSO
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "TGP.Admin.Auth";
    });

// Authorization - SystemAdmin role required
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdminPolicy", policy =>
        policy.RequireRole("SystemAdmin", "Admin"));
});

// Database
var connectionString = SqlConnectionStringBuilder.Build(builder.Configuration);
builder.Services.AddDbContext<TgpDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(5);
    }));

// Repositories - use what exists in TGP.Data
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// HTTP Client for SSO API calls
builder.Services.AddHttpClient("SSO", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:Sso"] ?? "http://localhost:5201");
});

builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:Gateway"] ?? "http://localhost:5010");
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TgpDbContext>("database");

var app = builder.Build();

// Verify database connectivity at startup
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TgpDbContext>();
    
    if (await dbContext.Database.CanConnectAsync())
    {
        Log.Information("Database connection established successfully");
    }
    else
    {
        Log.Error("Unable to connect to database");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Database connectivity check failed");
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSerilogRequestLogging();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapRazorPages();
app.MapHealthChecks("/health");

app.Run();

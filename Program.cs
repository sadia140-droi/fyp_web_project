using CloudKitchen.Data;
using CloudKitchen.Services;
using Cloud_Kitchen_web_App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cloud_Kitchen_web_App.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// DATABASE / ENTITY FRAMEWORK CORE
// ============================================================

builder.Services.AddDbContext<CloudKitchenDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
    ));

// ============================================================
// REAL EMAIL SERVICE
// ============================================================
// This connects IEmailSender to the real SMTP email sender.
// AccountController will use IEmailSender to send password
// reset emails to the user's actual email address.
// ============================================================

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();

// ============================================================
// ASP.NET CORE IDENTITY
// ============================================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // --------------------------------------------------------
        // PASSWORD SECURITY
        // --------------------------------------------------------

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // --------------------------------------------------------
        // UNIQUE EMAIL
        // --------------------------------------------------------

        options.User.RequireUniqueEmail = true;

        // --------------------------------------------------------
        // LOGIN LOCKOUT
        // 3 failed attempts = 15 minute lockout
        // --------------------------------------------------------

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);

        options.Lockout.MaxFailedAccessAttempts = 3;

        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<CloudKitchenDbContext>()
    .AddDefaultTokenProviders();

// ============================================================
// AUTHENTICATION COOKIE SETTINGS
// ============================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    // Normal Customer / Seller login
    options.LoginPath = "/Account/Login";

    // Unauthorized access
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Authentication cookie lifetime
    options.ExpireTimeSpan = TimeSpan.FromHours(8);

    // Extend lifetime while user is active
    options.SlidingExpiration = true;
});

// ============================================================
// JWT AUTHENTICATION
// ============================================================

builder.Services.AddAuthentication()
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default_secret_key_needs_to_be_replaced_in_secrets"))
        };
    });

// ============================================================
// SWAGGER CONFIGURATION
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// MVC
// ============================================================

builder.Services.AddControllersWithViews();

builder.Services.AddCors(o => o.AddPolicy("FlutterDev", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ============================================================
// IDENTITY ROLES SEEDING
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        await IdentitySeeder.SeedRolesAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Role seeding skipped or delayed on startup.");
    }
}

// ============================================================
// HTTP REQUEST PIPELINE
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("FlutterDev");

// ============================================================
// AUTHENTICATION
// Must come BEFORE Authorization
// ============================================================

app.UseAuthentication();

app.UseAuthorization();

// ============================================================
// API CONTROLLERS ROUTE
// ============================================================

app.MapControllers();

// ============================================================
// DEFAULT ROUTE
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = await userManager.FindByEmailAsync("cloudkitchenadmin@gmail.com");
    if (user != null)
    {
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        user.LockoutEnabled = false;
        user.EmailConfirmed = true;
        user.UserRole = "Admin";
        user.SellerStatus = "Approved";
        await userManager.UpdateAsync(user);
        await userManager.RemovePasswordAsync(user);
        await userManager.AddPasswordAsync(user, "Admin@12345");
        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}

app.Run();
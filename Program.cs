using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using SmartAttendance.Models;
using SmartAttendance.Web.Authorization;
using SmartAttendance.Web.Data;
using SmartAttendance.Web.Data.Seed;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Models;
using SmartAttendance.Web.Services;
using SmartAttendance.Web.Services.Interfaces;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // ---------- Database (Neon PostgreSQL) ----------
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. " +
            "Set it with user-secrets (development) or ConnectionStrings__DefaultConnection (production).");
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString, npgsql =>
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null)));

    // ---------- Identity (Lockout Disabled) ----------
    builder.Services
        .AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;

            options.User.RequireUniqueEmail = true;

            // Lockout features completely turned off
            options.Lockout.AllowedForNewUsers = false;
            options.Lockout.MaxFailedAccessAttempts = 999;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
        .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

    builder.Services.Configure<SecurityStampValidatorOptions>(o =>
        o.ValidationInterval = TimeSpan.FromMinutes(1));

    // ---------- Authorization ----------
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(PolicyNames.AdminOnly, p => p.RequireRole(Roles.Admin));
        options.AddPolicy(PolicyNames.AttendanceAccess,
            p => p.RequireRole(Roles.Admin, Roles.Teacher, Roles.AttendanceOperator));
        options.AddPolicy(PolicyNames.TeacherPortal,
            p => p.RequireRole(Roles.Teacher, Roles.AttendanceOperator));

        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    // ---------- Rate limiting ----------
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    // ---------- Data Protection keys in PostgreSQL ----------
    builder.Services.AddDataProtection()
        .SetApplicationName("SmartAttendance")
        .PersistKeysToDbContext<AppDbContext>();

    // ---------- Reverse proxy (Render) ----------
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // ---------- App services ----------
    builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection("Seed"));
    builder.Services.AddScoped<DbSeeder>();
    builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();
    builder.Services.AddScoped<IInstitutionClock, InstitutionClock>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<ISubjectService, SubjectService>();
    builder.Services.AddScoped<IClassRoomService, ClassRoomService>();

    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    app.UseForwardedHeaders();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/Error/{0}");

    app.UseStaticFiles();

    var wwwrootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
    if (Directory.Exists(wwwrootPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(wwwrootPath),
            RequestPath = ""
        });
    }

    app.UseRouting();

    app.UseRateLimiter();
    app.UseAuthentication();

    if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DisableAuth"))
    {
        app.Logger.LogWarning("DisableAuth is ON: every request runs as the admin user.");

        app.Use(async (ctx, next) =>
        {
            if (ctx.User.Identity?.IsAuthenticated != true)
            {
                var users = ctx.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var factory = ctx.RequestServices.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();

                var admin = (await users.GetUsersInRoleAsync(Roles.Admin)).FirstOrDefault();
                if (admin is not null)
                    ctx.User = await factory.CreateAsync(admin);
            }
            await next();
        });
    }

    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // ---------- Migration and Seeding ----------
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
            {
                await db.Database.MigrateAsync();
                Log.Information("Database migrations applied successfully.");
            }

            await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database startup or migration failed.");
        }
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
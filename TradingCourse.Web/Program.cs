using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TradingCourse.Application;
using TradingCourse.Application.Models;
using TradingCourse.Application.Services;
using TradingCourse.Web.Middleware;
using Hangfire;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add DbContext
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            x => x.MigrationsAssembly("TradingCourse.Application")));

    // Add Identity
    builder.Services.AddIdentity<AdminUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // Configure cookie settings for Identity
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Admin/Account/Login";
        options.AccessDeniedPath = "/Admin/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

    // Add Hangfire Services
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddHangfireServer();

    // Add Application Services
    builder.Services.AddScoped<ICourseService, CourseService>();
    builder.Services.AddScoped<ICouponService, CouponService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IHomepageBannerService, HomepageBannerService>();

    builder.Services.AddHealthChecks();

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    app.UseMiddleware<ExceptionMiddleware>();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // Map Hangfire dashboard
    app.UseHangfireDashboard("/admin/hangfire");

    app.UseStaticFiles(); // Required for runtime uploads (/uploads/...)
    app.MapStaticAssets();

    // Map Health Check endpoint
    app.MapHealthChecks("/health");

    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    Log.Information("Seeding database and running migrations...");
    await DbInitializer.SeedAsync(app.Services);

    Log.Information("Starting web application...");
    app.Run();
}
catch (Exception ex) when (ex.GetType().Name != "HostAbortedException")
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

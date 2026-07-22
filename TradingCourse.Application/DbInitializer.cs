using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingCourse.Application.Models;

namespace TradingCourse.Application;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AdminUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        // 1. Run migrations automatically
        try
        {
            if (context.Database.GetPendingMigrationsAsync().GetAwaiter().GetResult().Any())
            {
                await context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration run info: {ex.Message}");
        }

        // 1.1 Ensure HomepageBanner button columns exist (fallback if migration did not apply)
        try
        {
            var conn = context.Database.GetDbConnection();
            await conn.OpenAsync();
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('HomepageBanners') AND name = 'ButtonText'";
            var hasButtonText = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            if (!hasButtonText)
            {
                using var alter = conn.CreateCommand();
                alter.CommandText = "ALTER TABLE HomepageBanners ADD ButtonText NVARCHAR(200) NULL, ButtonUrl NVARCHAR(500) NULL";
                alter.ExecuteNonQuery();
                Console.WriteLine("Added ButtonText and ButtonUrl columns to HomepageBanners.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Column ensure info: {ex.Message}");
        }

        // 2. Seed Admin Role
        const string adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        // 3. Seed Admin User from AppSettings
        var adminEmail = configuration["AdminSettings:Email"] ?? "admin@tradingcourse.com";
        var adminPassword = configuration["AdminSettings:Password"] ?? "ChangeMe@123!";

        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser == null)
        {
            var adminUser = new AdminUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, adminRole);
            }
            else
            {
                throw new Exception($"Failed to seed Admin User: {string.Join(", ", createResult.Errors)}");
            }
        }

        // 4. Seed Default Course from Proposal
        if (!await context.Courses.AnyAsync())
        {
            var defaultCourse = new Course
            {
                Title = "Trading Live Course: Price Action & Market Dynamics",
                Slug = "live-trading-price-action",
                ShortDescription = "Master the art of Price Action trading, advanced chart analysis, and mathematical risk blueprints with real-time live trading cohorts.",
                FullDescription = @"<h3>Course Curriculum</h3>
<p>This live cohort course is designed to transition you from retail-level strategies to institutional-grade trading mechanics. Get direct exposure to real-time live market execution, master price action strategies, and implement robust mathematical risk layouts.</p>
<h4>Key Modules:</h4>
<ul>
    <li><strong>Module 1:</strong> Foundations of Price Action (Market Structure, Support & Resistance, Volume)</li>
    <li><strong>Module 2:</strong> Advanced Chart Patterns & Candlestick Secrets</li>
    <li><strong>Module 3:</strong> Strategy Development (Trend Following, Range Trading, Reversals, Breakouts)</li>
    <li><strong>Module 4:</strong> Mathematical Risk Blueprints, Position Sizing, and Trading Psychology</li>
    <li><strong>Module 5:</strong> Live-Market Cohort Execution & Interactive Q&A Sessions</li>
</ul>
<p>Join us to build a sustainable, math-backed edge in the markets!</p>",
                ThumbnailUrl = null,
                Price = 15000.00m,
                SalePrice = 9999.00m,
                IsLive = true,
                Instructor = "Amit Sharma",
                Schedule = "Sat & Sun, 7:00 PM IST (Live Zoom Cohort)",
                LiveClassLink = "https://zoom.us/j/9876543210",
                EmailTemplateSubject = "Welcome to {CourseTitle}! - Enrollment Confirmed",
                EmailTemplateBody = @"<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff; color: #1a202c;"">
    <div style=""text-align: center; margin-bottom: 25px;"">
        <h2 style=""color: #0ea5e9; font-weight: 700; margin: 0;"">Enrollment Confirmed!</h2>
        <p style=""color: #64748b; font-size: 14px; margin-top: 5px;"">Thank you for joining The GrowLog</p>
    </div>
    
    <p>Hi <strong>{CustomerName}</strong>,</p>
    <p>We are excited to welcome you to the <strong>{CourseTitle}</strong>. Your payment of <strong>INR {AmountPaid}</strong> has been successfully processed under Order ID <strong>#{OrderId}</strong>.</p>
    
    <div style=""background-color: #f8fafc; border-left: 4px solid #0ea5e9; padding: 15px; margin: 20px 0; border-radius: 0 8px 8px 0;"">
        <h3 style=""color: #334155; margin-top: 0; font-size: 16px;"">Class & Cohort Schedule</h3>
        <table cellpadding=""5"" style=""width: 100%; border-collapse: collapse; font-size: 14px;"">
            <tr>
                <td style=""width: 30%; font-weight: bold; color: #64748b;"">Instructor:</td>
                <td style=""color: #334155;"">{InstructorName}</td>
            </tr>
            <tr>
                <td style=""font-weight: bold; color: #64748b;"">Schedule:</td>
                <td style=""color: #334155;"">{Schedule}</td>
            </tr>
        </table>
    </div>

    <div style=""text-align: center; margin: 25px 0;"">
        <a href=""{LiveClassLink}"" style=""background-color: #0ea5e9; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; display: inline-block; box-shadow: 0 4px 6px -1px rgba(14, 165, 233, 0.4);"">
            Join Live Zoom Cohort
        </a>
        <p style=""color: #94a3b8; font-size: 12px; margin-top: 8px;"">Please log in 5 minutes early to test your audio & video connection.</p>
    </div>

    <h3 style=""color: #334155; border-bottom: 1px solid #e2e8f0; padding-bottom: 8px; font-size: 16px;"">Onboarding Checklist</h3>
    <ul style=""padding-left: 20px; font-size: 14px; line-height: 1.6; color: #475569;"">
        <li>Install the Zoom client on your computer or mobile device.</li>
        <li>Review basic price action terminology (optional, but recommended).</li>
        <li>Ensure you have a stable high-speed internet connection during live sessions.</li>
    </ul>

    <h3 style=""color: #334155; border-bottom: 1px solid #e2e8f0; padding-bottom: 8px; font-size: 16px; margin-top: 25px;"">Support and Channels</h3>
    <p style=""font-size: 14px; color: #475569; margin-bottom: 5px;"">If you face any issues joining the cohort, contact us immediately:</p>
    <ul style=""padding-left: 20px; font-size: 14px; color: #475569; margin-top: 5px;"">
        <li>Email: support@thegrowlog.com</li>
        <li>WhatsApp Support: +91 98765 43210</li>
    </ul>

    <p style=""margin-top: 35px; border-top: 1px solid #f1f5f9; padding-top: 20px; font-size: 13px; color: #94a3b8; text-align: center;"">
        The GrowLog &bull; Empowering Better Trading Decisions
    </p>
</div>",
                PurchaseDurationDays = 365,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Courses.AddAsync(defaultCourse);
            await context.SaveChangesAsync();
        }
    }
}

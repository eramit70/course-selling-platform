using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using TradingCourse.Application.Models;

namespace TradingCourse.Application.Services;

public class EmailService : IEmailService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpHost = _configuration["SmtpSettings:Host"];
        var smtpPortStr = _configuration["SmtpSettings:Port"];
        var smtpUsername = _configuration["SmtpSettings:Username"];
        var smtpPassword = _configuration["SmtpSettings:Password"];
        var senderName = _configuration["SmtpSettings:SenderName"] ?? "Trading Course";
        var senderEmail = _configuration["SmtpSettings:SenderEmail"];

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(senderEmail))
        {
            _logger.LogWarning("SMTP settings are incomplete. Skipping email send. To: {To}, Subject: {Subject}", to, subject);
            return;
        }

        int smtpPort = 587;
        if (!string.IsNullOrEmpty(smtpPortStr))
        {
            int.TryParse(smtpPortStr, out smtpPort);
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Connect to server
            // Use StartTls for port 587, SslOnConnect for 465, None/StartTls for others
            var secureSocketOption = smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            
            _logger.LogInformation("Connecting to SMTP server {Host}:{Port}...", smtpHost, smtpPort);
            await client.ConnectAsync(smtpHost, smtpPort, secureSocketOption);
            
            _logger.LogInformation("Authenticating SMTP user {Username}...", smtpUsername);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            
            _logger.LogInformation("Sending email to {To}...", to);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
            throw; // Throw to trigger Hangfire retry
        }
    }

    public async Task SendPurchaseConfirmationEmailAsync(int orderId)
    {
        _logger.LogInformation("Processing purchase confirmation email job for Order ID: {OrderId}", orderId);
        
        var order = await _context.Orders
            .Include(o => o.Course)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogError("Order not found with ID: {OrderId}. Aborting confirmation email.", orderId);
            return;
        }

        var course = order.Course;
        if (course == null)
        {
            _logger.LogError("Course not found for Order ID: {OrderId}. Aborting confirmation email.", orderId);
            return;
        }

        // Use custom templates or default fallbacks
        string subject = course.EmailTemplateSubject ?? "Enrollment Confirmed: {CourseTitle}";
        string body = course.EmailTemplateBody ?? @"
            <h3>Hi {CustomerName},</h3>
            <p>Thank you for enrolling in <strong>{CourseTitle}</strong>!</p>
            <p>Here are your course details:</p>
            <ul>
                <li><strong>Instructor:</strong> {InstructorName}</li>
                <li><strong>Schedule:</strong> {Schedule}</li>
                <li><strong>Live Joining Link:</strong> <a href='{LiveClassLink}'>{LiveClassLink}</a></li>
            </ul>
            <p>If you have any questions, please contact our support team.</p>
            <p>Best regards,<br/>Trading Course Team</p>";

        // Replace placeholders
        subject = subject.Replace("{CourseTitle}", course.Title)
                         .Replace("{CustomerName}", order.CustomerName)
                         .Replace("{InstructorName}", course.Instructor)
                         .Replace("{Schedule}", course.Schedule)
                         .Replace("{LiveClassLink}", course.LiveClassLink ?? "Will be shared prior to start")
                         .Replace("{OrderId}", order.Id.ToString())
                         .Replace("{AmountPaid}", order.AmountPaid.ToString("F2"));

        body = body.Replace("{CourseTitle}", course.Title)
                   .Replace("{CustomerName}", order.CustomerName)
                   .Replace("{InstructorName}", course.Instructor)
                   .Replace("{Schedule}", course.Schedule)
                   .Replace("{LiveClassLink}", course.LiveClassLink ?? "Will be shared prior to start")
                   .Replace("{OrderId}", order.Id.ToString())
                   .Replace("{AmountPaid}", order.AmountPaid.ToString("F2"));

        await SendEmailAsync(order.CustomerEmail, subject, body);
    }

    public async Task SendAdminNotificationEmailAsync(int orderId)
    {
        _logger.LogInformation("Processing admin notification email job for Order ID: {OrderId}", orderId);

        var order = await _context.Orders
            .Include(o => o.Course)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogError("Order not found with ID: {OrderId}. Aborting admin notification.", orderId);
            return;
        }

        var course = order.Course;
        if (course == null)
        {
            _logger.LogError("Course not found for Order ID: {OrderId}. Aborting admin notification.", orderId);
            return;
        }

        var adminEmail = _configuration["AdminSettings:Email"] ?? _configuration["SmtpSettings:SenderEmail"];
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogWarning("Admin notification email address is not configured. Skipping admin notification.");
            return;
        }

        string subject = $"[New Enrollment] {course.Title} - {order.CustomerName}";
        string body = $@"
            <h3>New Course Enrollment Received</h3>
            <table border='1' cellpadding='5' style='border-collapse: collapse;'>
                <tr><td><strong>Course:</strong></td><td>{course.Title} (ID: {course.Id})</td></tr>
                <tr><td><strong>Customer Name:</strong></td><td>{order.CustomerName}</td></tr>
                <tr><td><strong>Customer Email:</strong></td><td>{order.CustomerEmail}</td></tr>
                <tr><td><strong>Customer Mobile:</strong></td><td>{order.CustomerMobile}</td></tr>
                <tr><td><strong>Amount Paid:</strong></td><td>{order.AmountPaid:F2}</td></tr>
                <tr><td><strong>Coupon Code Used:</strong></td><td>{order.CouponCode ?? "None"}</td></tr>
                <tr><td><strong>Razorpay Order ID:</strong></td><td>{order.RazorpayOrderId ?? "N/A"}</td></tr>
                <tr><td><strong>Razorpay Payment ID:</strong></td><td>{order.RazorpayPaymentId ?? "N/A"}</td></tr>
                <tr><td><strong>Enrolled At:</strong></td><td>{order.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
            </table>";

        await SendEmailAsync(adminEmail, subject, body);
    }
}

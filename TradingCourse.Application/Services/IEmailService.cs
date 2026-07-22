using System;
using System.Threading.Tasks;

namespace TradingCourse.Application.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, string cc = null);
    Task SendPurchaseConfirmationEmailAsync(int orderId);
    Task SendAdminNotificationEmailAsync(int orderId);
    Task SendExceptionEmailAsync(Exception ex, string path);
}


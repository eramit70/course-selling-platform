using System.Threading.Tasks;

namespace TradingCourse.Application.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendPurchaseConfirmationEmailAsync(int orderId);
    Task SendAdminNotificationEmailAsync(int orderId);
}

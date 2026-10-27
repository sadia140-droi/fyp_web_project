using System.Threading.Tasks;

namespace CloudKitchen.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(
            string email,
            string subject,
            string htmlMessage);
    }
}
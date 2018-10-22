using Models;

namespace Services
{
    public interface IEmailSender
    {
        System.Threading.Tasks.Task SendEmailAsync(EmailMessage emailMessage);
        System.Threading.Tasks.Task SendEmailAsync(string email, string subject, string message);
        void SendEmail(EmailMessage emailMessage);
    }
}

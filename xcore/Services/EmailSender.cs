using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models;

namespace Services
{
    // This class is used by the application to send email for account confirmation and password reset.
    // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
    public class EmailSender : IEmailSender
    {
        public System.Threading.Tasks.Task SendEmailAsync(string email, string subject, string message)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task SendEmailAsync(EmailMessage emailMessage)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Utilities;
using MimeKit;
using MimeKit.Text;
using Models;

namespace Services
{
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        public AuthMessageSender()
        {
        }

        public System.Threading.Tasks.Task SendEmailAsync(string email, string subject, string message)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public async System.Threading.Tasks.Task SendEmailAsync(EmailMessage emailMessage)
        {
              //"EmailConfiguration": {
              //              "SmtpTitle":  "[Test environment] ERP",
              //  "SmtpServer": "mail.tribat.vn",
              //  "SmtpPort": 465,
              //  "SmtpUsername": "test-erp@tribat.vn",
              //  "SmtpPassword": "Kh0ngbiet@123",

              //  "PopServer": "mail.tribat.vn",
              //  "PopPort": 995,
              //  "PopUsername": "test-erp@tribat.vn",
              //  "PopPassword": "Kh0ngbiet@123"
              //},
            try
            {
                #region MailKit
                var message = new MimeMessage();
                message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                if (emailMessage.FromAddresses == null || emailMessage.FromAddresses.Count == 0)
                {
                    emailMessage.FromAddresses = new List<EmailAddress>
                    {
                        new EmailAddress { Name = "[Test environment] ERP", Address = "test-erp@tribat.vn" }
                    };
                }
                message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));

                message.Subject = emailMessage.Subject;
                //We will say we are sending HTML. But there are options for plaintext etc. 
                message.Body = new TextPart(TextFormat.Html)
                {
                    Text = emailMessage.BodyContent
                };

                //Be careful that the SmtpClient class is the one from Mailkit not the framework!
                using (var emailClient = new MailKit.Net.Smtp.SmtpClient())
                {
                    //The last parameter here is to use SSL (Which you should!)
                    emailClient.Connect("test-erp@tribat.vn", 465, true);

                    //Remove any OAuth functionality as we won't be using it. 
                    emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                    emailClient.Authenticate("test-erp@tribat.vn", "Kh0ngbiet@123");

                    await emailClient.SendAsync(message);
                    Console.WriteLine("The mail has been sent successfully !!");
                    Console.ReadLine();
                    await emailClient.DisconnectAsync(true);
                }
                #endregion

                #region No MailKit
                //var client = new System.Net.Mail.SmtpClient(_emailConfiguration.SmtpServer)
                //{
                //    Port = 587, //465 timeout
                //    UseDefaultCredentials = true,
                //    Credentials = new NetworkCredential(_emailConfiguration.SmtpUsername, _emailConfiguration.SmtpPassword)
                //};

                //MailMessage mailMessage = new MailMessage
                //{
                //    From = new MailAddress(emailMessage.FromAddresses[0].Address)
                //};
                //foreach(var to in emailMessage.ToAddresses)
                //{
                //    mailMessage.To.Add(to.Address);
                //}
                //mailMessage.Subject = emailMessage.Subject;
                //mailMessage.Body = emailMessage.Content;
                //client.Send(mailMessage);
                #endregion
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public System.Threading.Tasks.Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return System.Threading.Tasks.Task.FromResult(0);
        }

        public async System.Threading.Tasks.Task SendEmailWelcomeAsync(EmailMessage emailMessage)
        {
            try
            {
                var message = new MimeMessage();
                message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                if (emailMessage.FromAddresses == null || emailMessage.FromAddresses.Count == 0)
                {
                    emailMessage.FromAddresses = new List<EmailAddress>
                    {
                        new EmailAddress { Name = Constants.System.emailHrName, Address = Constants.System.emailHr }
                    };
                }
                message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));

                message.Subject = emailMessage.Subject;
                //We will say we are sending HTML. But there are options for plaintext etc. 
                message.Body = new TextPart(TextFormat.Html)
                {
                    Text = emailMessage.BodyContent
                };

                //Be careful that the SmtpClient class is the one from Mailkit not the framework!
                using (var emailClient = new MailKit.Net.Smtp.SmtpClient())
                {
                    //The last parameter here is to use SSL (Which you should!)
                    emailClient.Connect(Constants.System.emailHr, 465, true);

                    //Remove any OAuth functionality as we won't be using it. 
                    emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                    emailClient.Authenticate(Constants.System.emailHr, Constants.System.emailHrPwd);

                    await emailClient.SendAsync(message);
                    Console.WriteLine("The mail has been sent successfully !!");
                    Console.ReadLine();
                    await emailClient.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

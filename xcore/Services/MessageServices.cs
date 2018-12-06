using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Enums;
using Common.Utilities;
using Data;
using Microsoft.AspNetCore.Hosting;
using MimeKit;
using MimeKit.Text;
using Models;
using MongoDB.Driver;

namespace Services
{
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        readonly IHostingEnvironment _env;

        public AuthMessageSender(IHostingEnvironment env)
        {
            _env = env;
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

        /// <summary>
        /// NEW BUSINESS: Tracking everything
        /// </summary>
        /// <param name="emailMessage"></param>
        public void SendEmail(EmailMessage emailMessage)
        {
            #region Connection
            var connectString = "mongodb://localhost:27017";
            MongoDBContext.ConnectionString = connectString;
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var message = new MimeMessage
            {
                Subject = emailMessage.Subject,
                Body = new TextPart(TextFormat.Html)
                {
                    Text = emailMessage.BodyContent
                }
            };

            // Sometime null from, set default
            if (emailMessage.FromAddresses == null || emailMessage.FromAddresses.Count == 0)
            {
                emailMessage.FromAddresses = new List<EmailAddress>
                    {
                        new EmailAddress { Name = Constants.System.emailHrName, Address = Constants.System.emailHr, Pwd = Constants.System.emailHrPwd}
                    };
            }
            message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));

            // Check toemail
            var newToList = new List<EmailAddress>();
            if (emailMessage.ToAddresses != null && emailMessage.ToAddresses.Count > 0)
            {
                foreach (var item in emailMessage.ToAddresses)
                {
                    if (Utility.IsValidEmail(item.Address))
                    {
                        newToList.Add(item);
                    }
                    else
                    {
                       // Chỉ gửi cho email sai
                       var toError = new List<EmailAddress>
                       {
                            item
                       };
                        var errorEmail = new ScheduleEmail
                        {
                            From = emailMessage.FromAddresses,
                            To = toError,
                            CC = emailMessage.CCAddresses,
                            BCC = emailMessage.BCCAddresses,
                            Type = emailMessage.Type,
                            Title = emailMessage.Subject,
                            Content = emailMessage.BodyContent,
                            Status = (int)EEmailStatus.Fail,
                            Error = "Sai định dạng mail",
                            ErrorCount = 0
                        };
                        dbContext.ScheduleEmails.InsertOne(errorEmail);
                        SendMailSupport(errorEmail.Id);
                    }
                }
            }
            if (newToList != null && newToList.Count > 0)
            {
                message.To.AddRange(newToList.Select(x => new MailboxAddress(x.Name, x.Address)));

                if (emailMessage.CCAddresses != null && emailMessage.CCAddresses.Count > 0)
                {
                    message.Cc.AddRange(emailMessage.CCAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                }
                if (emailMessage.BCCAddresses != null && emailMessage.BCCAddresses.Count > 0)
                {
                    message.Bcc.AddRange(emailMessage.BCCAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                }

                #region Add to schedule
                var scheduleEmail = new ScheduleEmail
                {
                    From = emailMessage.FromAddresses,
                    To = emailMessage.ToAddresses,
                    CC = emailMessage.CCAddresses,
                    BCC = emailMessage.BCCAddresses,
                    Type = emailMessage.Type,
                    Title = message.Subject,
                    Content = emailMessage.BodyContent
                };
                dbContext.ScheduleEmails.InsertOne(scheduleEmail);
                #endregion
                var isEmailSent = (int)EEmailStatus.Send;
                var error = string.Empty;
                try
                {
                    using (var emailClient = new MailKit.Net.Smtp.SmtpClient())
                    {
                        //The last parameter here is to use SSL (Which you should!)
                        emailClient.Connect(emailMessage.FromAddresses.First().Address, 465, true);

                        //Remove any OAuth functionality as we won't be using it. 
                        emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                        emailClient.Authenticate(emailMessage.FromAddresses.First().Address, emailMessage.FromAddresses.First().Pwd);

                        emailClient.Send(message);
                        isEmailSent = (int)EEmailStatus.Ok;

                        emailClient.Disconnect(true);
                        #region Update status
                        var filter = Builders<ScheduleEmail>.Filter.Eq(m => m.Id, scheduleEmail.Id);
                        var update = Builders<ScheduleEmail>.Update
                            .Set(m => m.Status, isEmailSent)
                            .Set(m => m.UpdatedOn, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"));
                        dbContext.ScheduleEmails.UpdateOne(filter, update);
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    isEmailSent = (int)EEmailStatus.Fail;
                    error = ex.Message;
                    #region Update status
                    var filter = Builders<ScheduleEmail>.Filter.Eq(m => m.Id, scheduleEmail.Id);
                    var update = Builders<ScheduleEmail>.Update
                        .Set(m => m.Status, isEmailSent)
                        .Set(m => m.Error, error)
                        .Inc(m => m.ErrorCount, 1)
                        .Set(m => m.UpdatedOn, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"));
                    dbContext.ScheduleEmails.UpdateOne(filter, update);
                    #endregion
                    SendMailSupport(scheduleEmail.Id);
                }
            }
        }

        private void SendMailSupport(string id)
        {
            #region Connection
            var connectString = "mongodb://localhost:27017";
            MongoDBContext.ConnectionString = connectString;
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var errorItem = dbContext.ScheduleEmails.Find(m => m.Id.Equals(id)).FirstOrDefault();
            var subject = "Gửi email lỗi " + errorItem.Title;
            var froms = new List<EmailAddress>
                        {
                            new EmailAddress {
                                Name = Constants.System.emailHrName ,
                                Address = Constants.System.emailHr,
                                Pwd = Constants.System.emailHrPwd
                            }
                        };
            var tos = new List<EmailAddress>
                            {
                                new EmailAddress { Name = "Trần Minh Xuân", Address = "xuan.tm@tribat.vn" }
                            };
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "Error.html";
            var bodyBuilder = new BodyBuilder();
            using (StreamReader SourceReader = File.OpenText(pathToFile))
            {
                bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
            }
            string messageBody = string.Format(bodyBuilder.HtmlBody,
                subject,
                errorItem.Id,
                errorItem.UpdatedOn,
                errorItem.Error,
                errorItem.Type,
                errorItem.Content);
            var emailMessage = new EmailMessage()
            {
                FromAddresses = froms,
                ToAddresses = tos,
                Subject = subject,
                BodyContent = messageBody
            };

            var message = new MimeMessage();
            message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
            message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
            if (emailMessage.CCAddresses != null && emailMessage.CCAddresses.Count > 0)
            {
                message.Cc.AddRange(emailMessage.CCAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
            }
            if (emailMessage.BCCAddresses != null && emailMessage.BCCAddresses.Count > 0)
            {
                message.Bcc.AddRange(emailMessage.BCCAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
            }
            message.Subject = emailMessage.Subject;
            message.Body = new TextPart(TextFormat.Html)
            {
                Text = emailMessage.BodyContent
            };
            try
            {
                using (var emailClient = new MailKit.Net.Smtp.SmtpClient())
                {
                    //The last parameter here is to use SSL (Which you should!)
                    emailClient.Connect(emailMessage.FromAddresses.First().Address, 465, true);

                    //Remove any OAuth functionality as we won't be using it. 
                    emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                    emailClient.Authenticate(emailMessage.FromAddresses.First().Address, emailMessage.FromAddresses.First().Pwd);

                    emailClient.Send(message);

                    emailClient.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using Common.Enums;
using Common.Utilities;
using Data;
using MimeKit;
using MimeKit.Text;
using Models;
using MongoDB.Driver;

namespace Services
{
    public class AuthMessageSender
    {
        public AuthMessageSender()
        {
        }

        public void SendEmail(EmailMessage emailMessage)
        {
            #region Connection
            var connectString = "mongodb://localhost:27017";
            MongoDBContext.ConnectionString = connectString;
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            MailMessage mail = new MailMessage
            {
                From = new MailAddress("app.hcns@tribat.vn", "APP.HCNS")
            };

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
                            Status = 2,
                            Error = "Sai định dạng mail",
                            ErrorCount = 0,
                            EmployeeId = emailMessage.EmployeeId
                        };
                        dbContext.ScheduleEmails.InsertOne(errorEmail);
                        SendMailSupport(errorEmail.Id);
                    }
                }
            }

            if (newToList != null && newToList.Count > 0)
            {
                foreach (var to in emailMessage.ToAddresses)
                {
                    mail.To.Add(new MailAddress(to.Address, to.Name));
                    // Debug
                    // mail.To.Add(new MailAddress("xuan.tm@tribat.vn", "Xuân Trần"));
                }

                if (emailMessage.CCAddresses != null && emailMessage.CCAddresses.Count > 0)
                {
                    foreach (var cc in emailMessage.CCAddresses)
                    {
                        mail.CC.Add(new MailAddress(cc.Address, cc.Name));
                    }
                }

                if (emailMessage.BCCAddresses != null && emailMessage.BCCAddresses.Count > 0)
                {
                    foreach (var bcc in emailMessage.BCCAddresses)
                    {
                        mail.Bcc.Add(new MailAddress(bcc.Address, bcc.Name));
                    }
                }

                #region Add to schedule
                var scheduleEmail = new ScheduleEmail
                {
                    From = emailMessage.FromAddresses,
                    To = emailMessage.ToAddresses,
                    CC = emailMessage.CCAddresses,
                    BCC = emailMessage.BCCAddresses,
                    Type = emailMessage.Type,
                    Title = emailMessage.Subject,
                    Content = emailMessage.BodyContent,
                    EmployeeId = emailMessage.EmployeeId
                };
                dbContext.ScheduleEmails.InsertOne(scheduleEmail);
                #endregion

                var isEmailSent = (int)EEmailStatus.Send;
                var error = string.Empty;
                try
                {
                    var client = new System.Net.Mail.SmtpClient("mail.tribat.vn")
                    {
                        Port = 587, //465 timeout
                        UseDefaultCredentials = true,
                        Credentials = new NetworkCredential("app.hcns@tribat.vn", "Tr1b@t")
                    };

                    mail.Subject = emailMessage.Subject;
                    mail.IsBodyHtml = true;
                    mail.BodyEncoding = Encoding.UTF8;
                    mail.Body = emailMessage.BodyContent;
                    client.Send(mail);

                    #region Update status
                    var filter = Builders<ScheduleEmail>.Filter.Eq(m => m.Id, scheduleEmail.Id);
                    var update = Builders<ScheduleEmail>.Update
                        .Set(m => m.Status, isEmailSent)
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    dbContext.ScheduleEmails.UpdateOne(filter, update);
                    #endregion
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
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    dbContext.ScheduleEmails.UpdateOne(filter, update);
                    #endregion
                    SendMailSupport(scheduleEmail.Id);
                }
            }
        }

        public void SendEmailSchedule(EmailMessage emailMessage, string id)
        {
            #region Connection & config
            var connectString = "mongodb://localhost:27017";
            MongoDBContext.ConnectionString = connectString;
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();

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
            #endregion

            MailMessage mail = new MailMessage
            {
                From = new MailAddress("app.hcns@tribat.vn", "APP.HCNS")
            };

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
                            ErrorCount = 0,
                            EmployeeId = emailMessage.EmployeeId
                        };
                        dbContext.ScheduleEmails.InsertOne(errorEmail);
                        SendMailSupport(errorEmail.Id);
                    }
                }
            }

            if (newToList != null && newToList.Count > 0)
            {
                foreach (var to in emailMessage.ToAddresses)
                {
                    mail.To.Add(new MailAddress(to.Address, to.Name));
                    // Debug
                    // mail.To.Add(new MailAddress("xuan.tm@tribat.vn", "Xuân Trần"));
                }

                if (emailMessage.CCAddresses != null && emailMessage.CCAddresses.Count > 0)
                {
                    foreach (var cc in emailMessage.CCAddresses)
                    {
                        mail.CC.Add(new MailAddress(cc.Address, cc.Name));
                    }
                }
                if (emailMessage.BCCAddresses != null && emailMessage.BCCAddresses.Count > 0)
                {
                    foreach (var bcc in emailMessage.BCCAddresses)
                    {
                        mail.Bcc.Add(new MailAddress(bcc.Address, bcc.Name));
                    }
                }

                var isEmailSent = (int)EEmailStatus.Send;
                var error = string.Empty;
                try
                {
                    var client = new System.Net.Mail.SmtpClient("mail.tribat.vn")
                    {
                        Port = 587, //465 timeout
                        UseDefaultCredentials = true,
                        Credentials = new NetworkCredential("app.hcns@tribat.vn", "Tr1b@t")
                    };

                    mail.Subject = emailMessage.Subject;
                    mail.IsBodyHtml = true;
                    //set encoding
                    mail.BodyEncoding = Encoding.UTF8;
                    mail.Body = emailMessage.BodyContent;
                    client.Send(mail);

                    #region Update status
                    var filter = Builders<ScheduleEmail>.Filter.Eq(m => m.Id, id);
                    var update = Builders<ScheduleEmail>.Update
                        .Set(m => m.Status, isEmailSent)
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    dbContext.ScheduleEmails.UpdateOne(filter, update);
                    #endregion
                }
                catch (Exception ex)
                {
                    isEmailSent = (int)EEmailStatus.Fail;
                    error = ex.Message;
                    #region Update status
                    var filter = Builders<ScheduleEmail>.Filter.Eq(m => m.Id, id);
                    var update = Builders<ScheduleEmail>.Update
                        .Set(m => m.Status, isEmailSent)
                        .Set(m => m.Error, error)
                        .Inc(m => m.ErrorCount, 1)
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    dbContext.ScheduleEmails.UpdateOne(filter, update);
                    #endregion
                    SendMailSupport(id);
                }
            }
        }

        /// <summary>
        /// NOT WORKING ON 08.03.2019
        /// </summary>
        /// <param name="emailMessage"></param>
        /// <param name="id"></param>
        public void SendEmailScheduleWithMailKit(EmailMessage emailMessage, string id)
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
                        // Debug 
                        newToList.Add(new EmailAddress() { Name = "Xuan Tran", Address = "xuan.tm@tribat.vn" });
                        //newToList.Add(item);
                    }
                    else
                    {
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
                            Title = message.Subject,
                            Content = emailMessage.BodyContent,
                            Status = (int)EEmailStatus.Fail,
                            Error = "Sai định dạng mail",
                            ErrorCount = 0,
                            EmployeeId = emailMessage.EmployeeId
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

                var isEmailSent = (int)EEmailStatus.Send;
                var error = string.Empty;
                try
                {
                    using (var emailClient = new MailKit.Net.Smtp.SmtpClient())
                    {
                        emailClient.Connect("app.hcns@tribat.vn", 465, true);

                        emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                        emailClient.Authenticate("app.hcns@tribat.vn", "Tr1b@t");

                        emailClient.Send(message);
                        isEmailSent = (int)EEmailStatus.Ok;

                        emailClient.Disconnect(true);
                        #region Update status
                        var filter = Builders<ScheduleEmail>.Filter.Eq(m => m.Id, id);
                        var update = Builders<ScheduleEmail>.Update
                            .Set(m => m.Status, isEmailSent)
                            .Set(m => m.UpdatedOn, DateTime.Now);
                        dbContext.ScheduleEmails.UpdateOne(filter, update);
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    isEmailSent = (int)EEmailStatus.Fail;
                    error = ex.Message;
                    #region Update status
                    var filter = Builders<ScheduleEmail>.Filter.Eq(m => m.Id, id);
                    var update = Builders<ScheduleEmail>.Update
                        .Set(m => m.Status, isEmailSent)
                        .Set(m => m.Error, error)
                        .Inc(m => m.ErrorCount, 1)
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    dbContext.ScheduleEmails.UpdateOne(filter, update);
                    #endregion
                    SendMailSupport(id);
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

            MailMessage mail = new MailMessage
            {
                From = new MailAddress("app.hcns@tribat.vn", "APP.HCNS")
            };

            mail.To.Add(new MailAddress("xuan.tm@tribat.vn", "Trần Minh Xuân"));

            var errorItem = dbContext.ScheduleEmails.Find(m => m.Id.Equals(id)).FirstOrDefault();
            var subject = "Gửi email lỗi " + errorItem.Title;
            var pathToFile = @"C:\Projects\App.Schedule\Templates\Error.html";
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
                Subject = subject,
                BodyContent = messageBody
            };

            try
            {
                var client = new System.Net.Mail.SmtpClient("mail.tribat.vn")
                {
                    Port = 587, //465 timeout
                    UseDefaultCredentials = true,
                    Credentials = new NetworkCredential("app.hcns@tribat.vn", "Tr1b@t")
                };

                mail.Subject = emailMessage.Subject;
                mail.IsBodyHtml = true;
                mail.BodyEncoding = Encoding.UTF8;
                mail.Body = emailMessage.BodyContent;
                client.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

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
using Models;
using MongoDB.Driver;
using MimeKit;

namespace Services
{
    public class AuthMessageSender
    {
        private readonly string _connectString = "mongodb://xbatads:xcore910602@localhost/tribat";
        private readonly string _databaseName = "tribat";
        private readonly string _emailServer = "mail.tribat.vn";
        private readonly string _emailApp = "app.hcns@tribat.vn";
        private readonly string _emailAppShow = "APP.HCNS";
        private readonly string _emailAppPwd = "tr1b@T";
        MongoDBContext _dbContext;

        public AuthMessageSender()
        {
            MongoDBContext.ConnectionString = _connectString;
            MongoDBContext.DatabaseName = _databaseName;
            MongoDBContext.IsSSL = true;
            _dbContext = new MongoDBContext();
        }

        public void SendEmail(EmailMessage emailMessage)
        {
            MailMessage mail = new MailMessage
            {
                From = new MailAddress(_emailApp, _emailAppShow)
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
                        _dbContext.ScheduleEmails.InsertOne(errorEmail);
                        SendMailSupport(errorEmail.Id);
                    }
                }
            }

            if (newToList != null && newToList.Count > 0)
            {
                foreach (var to in emailMessage.ToAddresses)
                {
                    mail.To.Add(new MailAddress(to.Address, to.Name));
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
                _dbContext.ScheduleEmails.InsertOne(scheduleEmail);
                #endregion

                var isEmailSent = (int)EEmailStatus.Send;
                var error = string.Empty;
                try
                {
                    var client = new SmtpClient(_emailServer)
                    {
                        Port = 587, //465 timeout
                        UseDefaultCredentials = true,
                        Credentials = new NetworkCredential(_emailApp, _emailAppPwd)
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
                    _dbContext.ScheduleEmails.UpdateOne(filter, update);
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
                    _dbContext.ScheduleEmails.UpdateOne(filter, update);
                    #endregion
                    SendMailSupport(scheduleEmail.Id);
                }
            }
        }

        public void SendEmailSchedule(EmailMessage emailMessage, string id, string debug)
        {
            MailMessage mail = new MailMessage
            {
                From = new MailAddress(_emailApp, _emailAppShow)
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
                            ErrorCount = 1,
                            EmployeeId = emailMessage.EmployeeId
                        };
                        _dbContext.ScheduleEmails.InsertOne(errorEmail);
                        SendMailSupport(errorEmail.Id);
                    }
                }
            }

            if (newToList != null && newToList.Count > 0)
            {
                foreach (var to in emailMessage.ToAddresses)
                {
                    mail.To.Add(new MailAddress(to.Address, to.Name));
                }

                if (emailMessage.CCAddresses != null && emailMessage.CCAddresses.Count > 0)
                {
                    foreach (var cc in emailMessage.CCAddresses)
                    {
                        mail.CC.Add(new MailAddress(cc.Address, cc.Name));
                    }
                }
                if (!string.IsNullOrEmpty(debug))
                {
                    mail.CC.Add(new MailAddress(debug));
                }
                if (emailMessage.BCCAddresses != null && emailMessage.BCCAddresses.Count > 0)
                {
                    foreach (var bcc in emailMessage.BCCAddresses)
                    {
                        mail.Bcc.Add(new MailAddress(bcc.Address, bcc.Name));
                    }
                }

                var isEmailSent = (int)EEmailStatus.Ok;
                var error = string.Empty;
                try
                {
                    var client = new SmtpClient(_emailServer)
                    {
                        Port = 587, //465 timeout
                        UseDefaultCredentials = true,
                        Credentials = new NetworkCredential(_emailApp, _emailAppPwd)
                    };

                    mail.Subject = emailMessage.Subject;
                    mail.IsBodyHtml = true;
                    //set encoding
                    mail.BodyEncoding = Encoding.UTF8;
                    mail.Body = emailMessage.BodyContent;
                    if (emailMessage.Attachments != null && emailMessage.Attachments.Count > 0)
                    {
                        foreach(var attachment in emailMessage.Attachments)
                        {
                            mail.Attachments.Add(new Attachment(attachment));
                        }
                    }

                    client.Send(mail);

                    #region Update status
                    var filter = Builders<ScheduleEmail>.Filter.Eq(m => m.Id, id);
                    var update = Builders<ScheduleEmail>.Update
                        .Set(m => m.Status, isEmailSent)
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    _dbContext.ScheduleEmails.UpdateOne(filter, update);
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
                    _dbContext.ScheduleEmails.UpdateOne(filter, update);
                    #endregion
                    SendMailSupport(id);
                }
            }
        }

        private void SendMailSupport(string id)
        {
            MailMessage mail = new MailMessage
            {
                From = new MailAddress(_emailApp, _emailAppShow)
            };

            mail.To.Add(new MailAddress("thy.nc@tribat.vn", "Nguyễn Chu Thy"));

            var errorItem = _dbContext.ScheduleEmails.Find(m => m.Id.Equals(id)).FirstOrDefault();
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
                var client = new SmtpClient(_emailServer)
                {
                    Port = 587, //465 timeout
                    UseDefaultCredentials = true,
                    Credentials = new NetworkCredential(_emailApp, _emailAppPwd)
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

using Common.Enums;
using Common.Utilities;
using Data;
using MimeKit;
using MimeKit.Text;
using Models;
using MongoDB.Driver;
using Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xbirthday
{
    class Program
    {
        static void Main(string[] args)
        {
            SendMailBirthday();
        }

        static void SendMailBirthday()
        {
            #region Connection
            //var connectString = "mongodb://192.168.2.223:27017";
            var connectString = "mongodb://localhost:27017";
            MongoDBContext.ConnectionString = connectString;
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion
            var now = DateTime.Now;
            var url = Constants.System.domain;
            var froms = new List<EmailAddress>
                        {
                            new EmailAddress {
                                Name = Constants.System.emailHrName ,
                                Address = Constants.System.emailHr,
                                Pwd = Constants.System.emailHrPwd
                            }
                        };

            #region CCs

            #endregion
            var ccs = new List<EmailAddress>();
            var listHrRoles = dbContext.RoleUsers.Find(m => m.Role.Equals(Constants.Rights.NhanSu) && (m.Expired.Equals(null) || m.Expired > DateTime.Now)).ToList();
            if (listHrRoles != null && listHrRoles.Count > 0)
            {
                foreach (var item in listHrRoles)
                {
                    if (item.Action == 3)
                    {
                        var fields = Builders<Employee>.Projection.Include(p => p.Email).Include(p => p.FullName);
                        var emailEntity = dbContext.Employees.Find(m => m.Id.Equals(item.User)).Project<Employee>(fields).FirstOrDefault();
                        if (emailEntity != null)
                        {
                            ccs.Add(new EmailAddress { Name = emailEntity.FullName, Address = emailEntity.Email });
                        }
                    }
                }
            }

            var birthdays = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Birthday > Constants.MinDate).ToEnumerable()
               .Where(m => m.RemainingBirthDays == 0).ToList();

            if (birthdays.Count > 0)
            {
                foreach (var item in birthdays)
                {
                    if (item != null && !string.IsNullOrEmpty(item.Email))
                    {
                        Console.WriteLine(item.Email);
                        var subject = "CHÚC MỪNG SINH NHẬT !!!";
                        var title = string.Empty;
                        if (!string.IsNullOrEmpty(item.Gender))
                        {
                            title = item.Gender == "Nam" ? "anh" : "chị";
                            if (item.AgeBirthday > 50)
                            {
                                title = item.Gender == "Nam" ? "ông" : "bà";
                            }
                        }
                        var fullName = item.FullName;
                        var tos = new List<EmailAddress>
                            {
                                new EmailAddress { Name = item.FullName, Address = item.Email }
                            };
                        //C:\Projects\App.Schedule\BirthdaySchedule\Templates
                        var pathToFile = @"C:\Projects\App.Schedule\Templates\HappyBirthday.html";
                        // Must be fullpath. because task schedule un known path.
                        // var pathToFile = @"C:\Projects\Tribat\sourcecode\xbirthday\bin\Debug\Templates\HappyBirthday.html";
                        var bodyBuilder = new BodyBuilder();
                        using (StreamReader SourceReader = File.OpenText(pathToFile))
                        {
                            bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                        }
                        string messageBody = string.Format(bodyBuilder.HtmlBody,
                            subject,
                            title,
                            fullName,
                            url);
                        var emailMessage = new EmailMessage()
                        {
                            FromAddresses = froms,
                            ToAddresses = tos,
                            CCAddresses = ccs,
                            Subject = subject,
                            BodyContent = messageBody,
                            Type = "birthday",
                            EmployeeId = item.EmployeeId
                        };

                        // For faster update.
                        var scheduleEmail = new ScheduleEmail
                        {
                            Status = (int)EEmailStatus.Schedule,
                            To = emailMessage.ToAddresses,
                            CC = emailMessage.CCAddresses,
                            BCC = emailMessage.BCCAddresses,
                            Type = emailMessage.Type,
                            Title = emailMessage.Subject,
                            Content = emailMessage.BodyContent,
                            EmployeeId = emailMessage.EmployeeId
                        };
                        dbContext.ScheduleEmails.InsertOne(scheduleEmail);
                        //new AuthMessageSender().SendEmail(emailMessage);
                    }
                }
            }
        }
    }
}

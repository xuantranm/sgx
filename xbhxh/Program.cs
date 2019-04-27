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

namespace xbhxh
{
    class Program
    {
        static void Main(string[] args)
        {
            SendMailBHXH();
        }

        static void SendMailBHXH()
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

            #region Tos
            var tos = new List<EmailAddress>();
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
                            tos.Add(new EmailAddress { Name = emailEntity.FullName, Address = emailEntity.Email });
                        }
                    }
                }
            }
            #endregion

            // Notice bhxh base joinday to 6 months.
            var bhxhs = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)).ToEnumerable()
               .Where(m => m.RemainingBhxh == 0).ToList();

            if (bhxhs.Count > 0)
            {
                foreach (var item in bhxhs)
                {
                    if (item != null && !string.IsNullOrEmpty(item.Email))
                    {
                        Console.WriteLine(item.Email);
                        var subject = "THÔNG BÁO ĐÓNG BHXH NHÂN VIÊN LÀM VIỆC 6 THÁNG !!!";
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
                        var contractDay = string.Empty;
                        if (item.Contractday > Constants.MinDate)
                        {
                            contractDay = item.Contractday.ToString("dd/MM/yyyy");
                        }
                        var phone = string.Empty;
                        if (item.Mobiles != null && item.Mobiles.Count > 0)
                        {
                            phone = "<a href='tel:"+ item.Mobiles.First().Number + "'>"+ item.Mobiles.First().Number + "</a>";
                        }
                        
                        var email = string.Empty;
                        if (!string.IsNullOrEmpty(item.Email))
                        {
                            email = item.Email;
                        }
                        if (string.IsNullOrEmpty(email))
                        {
                            if (!string.IsNullOrEmpty(item.EmailPersonal))
                            {
                                email = item.EmailPersonal;
                            }
                        }

                        var linkInformation = Constants.LinkHr.Main + "/" + Constants.LinkHr.Human + "/" + Constants.LinkHr.Information + "/" + item.Id;

                        var ccs = new List<EmailAddress>();
                        if (!string.IsNullOrEmpty(item.Email) && Utility.IsValidEmail(item.Email))
                        {
                            ccs.Add(new EmailAddress { Name = item.FullName, Address = item.Email });
                        }
                        
                        var pathToFile = @"C:\Projects\App.Schedule\Templates\AlertBhxh.html";
                        var bodyBuilder = new BodyBuilder();
                        using (StreamReader SourceReader = File.OpenText(pathToFile))
                        {
                            bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                        }
                        string messageBody = string.Format(bodyBuilder.HtmlBody,
                            subject,
                            title + " " + fullName,
                            item.ChucVuName,
                            item.PhongBanName,
                            item.BoPhanName,
                            item.Joinday.ToString("dd/MM/yyyy"),
                            contractDay,
                            phone,
                            email,
                            url + "/" + linkInformation,
                            url);
                        var emailMessage = new EmailMessage()
                        {
                            FromAddresses = froms,
                            ToAddresses = tos,
                            Subject = subject,
                            BodyContent = messageBody,
                            Type = "alert-bhxh",
                            EmployeeId = item.Id
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

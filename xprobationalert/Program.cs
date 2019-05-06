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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xprobationalert
{
    class Program
    {
        static void Main(string[] args)
        {
            #region setting
            var debug = ConfigurationSettings.AppSettings.Get("debugString").ToString();
            var connection = "mongodb://localhost:27017";
            var database = "tribat";
            #endregion

            AlertProbation(connection, database, debug);
        }

        static void AlertProbation(string connection, string database, string debug)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            var url = Constants.System.domain;
            var now = DateTime.Now;
            #endregion

            var listAlert = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.Official.Equals(false)).ToEnumerable()
               .Where(m => m.ProbationAlert == 0).ToList();

            #region Tos
            var tos = new List<EmailAddress>();
            var listHrRoles = dbContext.RoleUsers.Find(m => m.Role.Equals(Constants.Rights.HR) && (m.Expired.Equals(null) || m.Expired > DateTime.Now)).ToList();
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

            if (listAlert.Count > 0)
            {
                foreach (var item in listAlert)
                {
                    if (item != null)
                    {
                        var typeEmail = "probation-alert";
                        var subject = "[THỬ VIỆC] THÔNG BÁO ĐẾN HẠN XÉT LÀM VIỆC CHÍNH THỨC !!!";
                        var phone = string.Empty;
                        if (item.Mobiles != null && item.Mobiles.Count > 0)
                        {
                            foreach(var mobile in item.Mobiles)
                            {
                                if (!string.IsNullOrEmpty(mobile.Number))
                                {
                                    phone += "<a href='tel:" + mobile.Number + "'>" + mobile.Number + "</a>";
                                }
                            }
                        }
                        var linkInformation = Constants.LinkHr.Main + "/" + Constants.LinkHr.Human + "/" + Constants.LinkHr.Information + "/" + item.Id;
                        var pathToFile = @"C:\Projects\App.Schedule\Templates\ProbationAlert.html";
                        var bodyBuilder = new BodyBuilder();
                        using (StreamReader SourceReader = File.OpenText(pathToFile))
                        {
                            bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                        }
                        string messageBody = string.Format(bodyBuilder.HtmlBody,
                            subject,
                            item.FullName,
                            item.ChucVuName,
                            item.PhongBanName,
                            item.BoPhanName,
                            item.Joinday.ToString("dd/MM/yyyy"),
                            item.ProbationMonth,
                            phone,
                            item.Email,
                            url + "/" + linkInformation,
                            url);
                        var emailMessage = new EmailMessage()
                        {
                            ToAddresses = tos,
                            Subject = subject,
                            BodyContent = messageBody,
                            Type = typeEmail,
                            EmployeeId = item.Id
                        };

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
                    }
                }
            }
        }
    }
}

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

namespace xscheduleemail
{
    class Program
    {
        static void Main(string[] args)
        {
            #region setting
            var mode = Convert.ToInt32(ConfigurationSettings.AppSettings.Get("mode").ToString());
            var isMail = ConfigurationSettings.AppSettings.Get("isMail").ToString() == "true" ? true : false;
            var debug = ConfigurationSettings.AppSettings.Get("debug").ToString();
            var connection = "mongodb://localhost:27017";
            var database = "tribat";
            #endregion

            Console.WriteLine("Start send mail...");
            SendMail(mode, isMail, debug, connection, database);

            // Debug
            //Console.Write("\r\n");
            //Console.Write("Done..... Press any key to exist!");
            //Console.ReadLine();
        }

        static void SendMail(int mode, bool isMail, string debug, string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var listEmail = dbContext.ScheduleEmails.Find(m => m.Status.Equals(mode)).ToList();

            if (listEmail != null && listEmail.Count > 0)
            {
                foreach (var item in listEmail)
                {
                    Console.WriteLine("Process: " + item.EmployeeId + " | type: " + item.Type);
                    if (item != null && item.To != null)
                    {
                        var emailMessage = new EmailMessage()
                        {
                            FromAddresses = item.From,
                            ToAddresses = item.To,
                            CCAddresses = item.CC,
                            BCCAddresses = item.BCC,
                            Subject = item.Title,
                            BodyContent = item.Content,
                            Type = item.Type,
                            EmployeeId = item.EmployeeId
                        };

                        if (isMail)
                        {
                            new AuthMessageSender().SendEmailSchedule(emailMessage, item.Id, debug);
                        }
                    }
                }
            }
        }
    }
}

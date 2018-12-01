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


namespace xschedulemail5
{
    class Program
    {
        static void Main(string[] args)
        {
            SendMail();
        }

        static void SendMail()
        {
            #region Connection
            //var connectString = "mongodb://192.168.2.223:27017";
            var connectString = "mongodb://localhost:27017";
            MongoDBContext.ConnectionString = connectString;
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var listEmail = dbContext.ScheduleEmails.Find(m => m.Status.Equals(5)).ToList();

            if (listEmail != null && listEmail.Count > 0)
            {
                foreach (var item in listEmail)
                {
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
                            Type = item.Type
                        };

                        new AuthMessageSender().SendEmailSchedule(emailMessage, item.Id);
                    }
                }
            }
        }
    }
}

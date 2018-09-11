using Common.Utilities;
using Data;
using MimeKit;
using MimeKit.Text;
using Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeperBusiness
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start update business time keeper...");
            UpdateTimeKeeper();
            Console.WriteLine("DONE...");
        }

        static void CheckError()
        {
            Console.WriteLine("Input date format yyyy-MM-dd");
            var date = Console.ReadLine();
            var dateData = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            // vd: 26 tháng 7 thi month phai la thang 8.
            int month = dateData.Month;
            int year = dateData.Year;
            if (dateData.Day > 25)
            {
                if (dateData.Month != 12)
                {
                    month = dateData.Month + 1;
                    year = dateData.Year;
                }
                else
                {
                    month = 1;
                    year = dateData.Year + 1;
                }
            }
            Console.WriteLine("Month: " + month);
            Console.WriteLine("Year: " + year);
            var toDate = new DateTime(year, month, 25);
            Console.WriteLine(toDate);
        }

        static void UpdateTimeKeeper()
        {
            #region Connection vs Settings
            MongoDBContext.ConnectionString = "mongodb://localhost:27017";
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();

            var mode = true;
            var day = -7;
            var setting = dbContext.Settings.Find(m => m.Key.Equals("schedulecoremode")).FirstOrDefault();
            if (setting != null && !string.IsNullOrEmpty(setting.Value))
            {
                // 0: get all data
                // 1: get by day.
                mode = setting.Value == "0" ? false : true;
            }
            if (!mode)
            {
                dbContext.EmployeeWorkTimeLogs.DeleteMany(m => true);
                dbContext.EmployeeWorkTimeMonthLogs.DeleteMany(m => true);
            }
            var settingDay = dbContext.Settings.Find(m => m.Key.Equals("schedulecoreday")).FirstOrDefault();
            if (settingDay != null && !string.IsNullOrEmpty(settingDay.Value))
            {
                day = Convert.ToInt32(settingDay.Value);
            }
            #endregion

            var dateCrawled = DateTime.Now.AddDays(day);
            var builder = Builders<AttLog>.Filter;
            var filter = builder.Gte(m => m.Date, dateCrawled);
            var attlogVps = mode ? dbContext.X628CVPAttLogs.Find(filter).ToList() : dbContext.X628CVPAttLogs.Find(m => true).ToList();
            if (attlogVps != null && attlogVps.Count > 0)
            {
                Proccess(attlogVps, "VP", mode);
            }
            var attlogNMs = mode ? dbContext.X928CNMAttLogs.Find(filter).ToList() : dbContext.X928CNMAttLogs.Find(m => true).ToList();
            if (attlogNMs != null && attlogNMs.Count > 0)
            {
                Proccess(attlogNMs, "NM", mode);
            }
        }

        private static void Proccess(List<AttLog> attlogs, string location, bool mode)
        {
            #region Connection & Settings
            MongoDBContext.ConnectionString = "mongodb://localhost:27017";
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();

            var dateNewPolicy = new DateTime(2018, 8, 1);
            decimal workingScheduleHour = 8;
            var lunch = TimeSpan.FromHours(1);

            var allowLateMinute = 5;
            var settingAllowLateMinute = dbContext.Settings.Find(m => m.Key.Equals("allowLateMinute")).FirstOrDefault();
            if (settingAllowLateMinute != null && !string.IsNullOrEmpty(settingAllowLateMinute.Value))
            {
                allowLateMinute = Convert.ToInt32(settingAllowLateMinute.Value);
            }
            var allowLateNumber = 30;
            var settingAllowLateNumber = dbContext.Settings.Find(m => m.Key.Equals("allowLateNumber")).FirstOrDefault();
            if (settingAllowLateNumber != null && !string.IsNullOrEmpty(settingAllowLateNumber.Value))
            {
                allowLateNumber = Convert.ToInt32(settingAllowLateNumber.Value);
            }
            var allowEarlyMinute = 5;
            var settingAllowEarlyMinute = dbContext.Settings.Find(m => m.Key.Equals("allowEarlyMinute")).FirstOrDefault();
            if (settingAllowEarlyMinute != null && !string.IsNullOrEmpty(settingAllowEarlyMinute.Value))
            {
                allowEarlyMinute = Convert.ToInt32(settingAllowEarlyMinute.Value);
            }
            var allowEarlyNumber = 30;
            var settingAllowEarlyNumber = dbContext.Settings.Find(m => m.Key.Equals("allowEarlyNumber")).FirstOrDefault();
            if (settingAllowEarlyNumber != null && !string.IsNullOrEmpty(settingAllowEarlyNumber.Value))
            {
                allowEarlyNumber = Convert.ToInt32(settingAllowEarlyNumber.Value);
            }

            var modeMail = true;
            var dateApplyMail = -1;
            var modeTest = false;
            var listEmailTest = new List<string>();
            var settingMail = dbContext.Settings.Find(m => m.Key.Equals("modeMailTimeKeeper")).FirstOrDefault();
            if (settingMail != null && !string.IsNullOrEmpty(settingMail.Value))
            {
                modeMail = settingMail.Value == "1" ? true : false;
                if (modeMail)
                {
                    var dateMail = dbContext.Settings.Find(m => m.Key.Equals("dateSendMailTimeKeeper")).FirstOrDefault();
                    if (dateMail != null && !string.IsNullOrEmpty(dateMail.Value))
                    {
                        dateApplyMail = Convert.ToInt32(dateMail.Value);
                    }

                    var modeTestSetting = dbContext.Settings.Find(m => m.Key.Equals("modeTestMailTimeKeeper")).FirstOrDefault();
                    if (modeTestSetting != null && !string.IsNullOrEmpty(modeTestSetting.Value))
                    {
                        modeTest = modeTestSetting.Value == "1" ? true : false;
                        if (modeTest)
                        {
                            var listMail = dbContext.Settings.Find(m => m.Key.Equals("testListMailTimeKeeper")).FirstOrDefault();
                            if (listMail != null && !string.IsNullOrEmpty(listMail.Value))
                            {
                                listEmailTest = listMail.Value.Split(';').ToList();
                            }
                        }
                    }
                }
            }

            var linkSetting = "fg/cham-cong/";
            var settingConfirm = dbContext.Settings.Find(m => m.Key.Equals("linkConfirmTimeKeeper")).FirstOrDefault();
            if (settingConfirm != null && !string.IsNullOrEmpty(settingConfirm.Value))
            {
                linkSetting = settingConfirm.Value;
            }
            #endregion

            var groups = (from p in attlogs
                          group p by new
                          {
                              p.EnrollNumber,
                              p.Date.Date
                          }
                              into d
                          select new
                          {
                              groupDate = d.Key.Date.ToString("yyyy-MM-dd"),
                              groupCode = d.Key.EnrollNumber,
                              count = d.Count(),
                              times = d.ToList(),
                          }).ToList();

            foreach (var group in groups)
            {
                Console.WriteLine("Date: " + group.groupDate + ",fingerCode: " + group.groupCode + ",location: "+ location);
                try
                {
                    var dateData = DateTime.ParseExact(group.groupDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    int month = dateData.Month;
                    int year = dateData.Year;
                    if (dateData.Day > 25)
                    {
                        if (dateData.Month != 12)
                        {
                            month = dateData.Month + 1;
                            year = dateData.Year;
                        }
                        else
                        {
                            month = 1;
                            year = dateData.Year + 1;
                        }
                    }
                    var toDate = new DateTime(year, month, 26);

                    var workTime = new TimeSpan(0);
                    decimal workDay = 0;
                    var late = new TimeSpan(0);
                    var early = new TimeSpan(0);
                    // 0: cần xác nhận công; 1: đủ ngày công ; 2: đã gửi xác nhận công, 3: đồng ý; 4: từ chối  
                    var status = 1;

                    var linkFinger = string.Empty;
                    var enrollNumber = group.groupCode;
                    var records = group.times.OrderBy(m => m.Date).ToList();
                    var dateFinger = records.First().DateOnlyRecord;
                    var inLogTime = records.First().TimeOnlyRecord;
                    var outLogTime = records.Last().TimeOnlyRecord;

                    #region Save to db: In/Out log
                    TimeSpan? dbinLogTime = inLogTime;
                    TimeSpan? dboutLogTime = outLogTime;
                    #endregion

                    #region Define working hour schedule & email send notice
                    var email = string.Empty;
                    var fullName = string.Empty;
                    var startWorkingScheduleTime = TimeSpan.FromHours(8);
                    var endWorkingScheduleTime = TimeSpan.FromHours(17);
                    var filterEmp = Builders<Employee>.Filter.ElemMatch(z => z.Workplaces, a => a.Fingerprint == enrollNumber);
                    var employee = dbContext.Employees.Find(filterEmp).FirstOrDefault();
                    if (employee != null)
                    {
                        var workplaces = employee.Workplaces;
                        if (workplaces != null && workplaces.Count > 0)
                        {
                            foreach (var workplace in employee.Workplaces)
                            {
                                if (!string.IsNullOrEmpty(workplace.Fingerprint) && workplace.Fingerprint == enrollNumber)
                                {
                                    startWorkingScheduleTime = TimeSpan.Parse(workplace.WorkingScheduleTime.Split('-')[0].Trim());
                                    endWorkingScheduleTime = TimeSpan.Parse(workplace.WorkingScheduleTime.Split('-')[1].Trim());
                                }
                            }
                        }
                        email = employee.Email;
                        fullName = employee.FullName;
                        linkFinger = linkSetting + employee.Id;
                    }
                    #endregion

                    #region Procees Times
                    // If the start time is before the starting hours, set it to the starting hour.
                    if (inLogTime < startWorkingScheduleTime) inLogTime = startWorkingScheduleTime;
                    if (outLogTime > endWorkingScheduleTime) outLogTime = endWorkingScheduleTime;

                    workTime = (outLogTime - inLogTime) - lunch;
                    workDay = Convert.ToDecimal(workTime.TotalHours) / workingScheduleHour;

                    var inLogTimeAllowLate = DateTime.Parse(inLogTime.ToString()).AddMinutes(-allowLateMinute).TimeOfDay;
                    if (inLogTimeAllowLate > startWorkingScheduleTime)
                    {
                        late = inLogTime - startWorkingScheduleTime;
                        status = 0;
                    }
                    var outLogTimeAllowLate = DateTime.Parse(outLogTime.ToString()).AddMinutes(allowEarlyMinute).TimeOfDay;
                    if (outLogTimeAllowLate <= endWorkingScheduleTime)
                    {
                        early = endWorkingScheduleTime - outLogTime;
                        status = 0;
                    }

                    #region Check miss in/out
                    var workingArr = new int[] { startWorkingScheduleTime.Hours, endWorkingScheduleTime.Hours };
                    var incheck = workingArr.ClosestTo(dbinLogTime.Value.Hours);
                    var outcheck = workingArr.ClosestTo(dboutLogTime.Value.Hours);
                    if (incheck == outcheck)
                    {
                        if (incheck == endWorkingScheduleTime.Hours)
                        {
                            // missing in
                            dbinLogTime = null;
                            late = new TimeSpan(0);
                            early = endWorkingScheduleTime - outLogTime;
                            workTime = TimeSpan.FromHours(4) - early;
                        }
                        else
                        {
                            // missing out
                            dboutLogTime = null;
                            early = new TimeSpan(0);
                            late = inLogTime - startWorkingScheduleTime;
                            workTime = TimeSpan.FromHours(4) - late;
                        }

                        status = 0;

                        workDay = Convert.ToDecimal(workTime.TotalHours) / workingScheduleHour;
                    }
                    #endregion
                    // <4h : 0.5 day; > 5: 1 day
                    if (workTime.TotalHours > 4)
                    {
                        workDay = 1;
                    }
                    else
                    {
                        workDay = Convert.ToDecimal(0.5);
                    }

                    // Cap nhat trang thai tai thoi diem ap dung, thoi gian truoc trang thai ok
                    if (dateFinger < dateNewPolicy)
                    {
                        status = 1;
                    }
                    var employeeWorkTimeLog = new EmployeeWorkTimeLog
                    {
                        EnrollNumber = enrollNumber,
                        VerifyMode = records[0].VerifyMode,
                        InOutMode = records[0].InOutMode,
                        Workcode = records[0].Workcode,
                        WorkplaceCode = location,
                        Date = dateFinger,
                        In = dbinLogTime,
                        Out = dboutLogTime,
                        Start = startWorkingScheduleTime,
                        End = endWorkingScheduleTime,
                        WorkTime = workTime,
                        WorkDay = workDay,
                        Late = late,
                        Early = early,
                        Status = status,
                        Logs = records
                    };

                    #region DB
                    if (!mode)
                    {
                        var employeeWorkTimeLogDb = dbContext.EmployeeWorkTimeLogs.Find(m => m.EnrollNumber.Equals(employeeWorkTimeLog.EnrollNumber)
                                                 && m.Date.Equals(employeeWorkTimeLog.Date)).FirstOrDefault();
                        if (employeeWorkTimeLogDb == null)
                        {
                            dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                            Console.WriteLine("Insert db: EnrollNumber: " + employeeWorkTimeLog.EnrollNumber + ", date" + employeeWorkTimeLog.Date + ", status : " + employeeWorkTimeLog.Status);
                            UpdateSummary(dbContext, dateData, toDate, month, year, employeeWorkTimeLog);
                        }
                        else
                        {
                            if (employeeWorkTimeLogDb.Status == 0)
                            {
                                var currentWorkTime = employeeWorkTimeLogDb.WorkTime.TotalMinutes;
                                var currentLate = employeeWorkTimeLogDb.Late.TotalMinutes;
                                var currentEarly = employeeWorkTimeLogDb.Early.TotalMinutes;
                                var currentWorkDay = employeeWorkTimeLogDb.WorkDay;

                                var builderUpdate = Builders<EmployeeWorkTimeLog>.Filter;
                                var filterUpdate = builderUpdate.Eq(m => m.Id, employeeWorkTimeLogDb.Id);
                                var update = Builders<EmployeeWorkTimeLog>.Update
                                    .Set(m => m.In, employeeWorkTimeLog.In)
                                    .Set(m => m.Out, employeeWorkTimeLog.Out)
                                    .Set(m => m.WorkTime, employeeWorkTimeLog.WorkTime)
                                    .Set(m => m.WorkDay, employeeWorkTimeLog.WorkDay)
                                    .Set(m => m.Late, employeeWorkTimeLog.Late)
                                    .Set(m => m.Early, employeeWorkTimeLog.Early)
                                    .Set(m => m.Status, employeeWorkTimeLog.Status)
                                    .Set(m => m.Logs, employeeWorkTimeLog.Logs)
                                    .Set(m => m.UpdatedOn, DateTime.Now);
                                dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);

                                UpdateSummary2(dbContext, dateData, month, year, employeeWorkTimeLog, currentWorkTime, currentLate, currentEarly, currentWorkDay);
                            }
                        }
                    }
                    else
                    {
                        dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                        Console.WriteLine("Insert db: EnrollNumber: " + employeeWorkTimeLog.EnrollNumber + ", date" + employeeWorkTimeLog.Date + ", status : " + employeeWorkTimeLog.Status);
                        UpdateSummary(dbContext, dateData, toDate, month, year, employeeWorkTimeLog);
                    }
                    
                    #endregion

                    #endregion

                    #region Send Mail
                    if (status == 0 && !string.IsNullOrEmpty(email) && dateFinger > DateTime.Now.AddDays(dateApplyMail))
                    {
                        if (modeTest && listEmailTest.Exists(x => string.Equals(x, email, StringComparison.OrdinalIgnoreCase)))
                        {
                            Console.WriteLine("Sending mail...");
                            var tos = new List<EmailAddress>
                            {
                                new EmailAddress { Name = fullName, Address = email }
                            };
                            var webRoot = Environment.CurrentDirectory;
                            var pathToFile = Environment.CurrentDirectory + "/Templates/TimeKeeperNotice.html";
                            var subject = "[TRIBAT] Xác nhận thời gian làm việc.";
                            var bodyBuilder = new BodyBuilder();
                            using (StreamReader SourceReader = File.OpenText(pathToFile))
                            {
                                bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                            }
                            #region parameters
                            //{0} : Subject
                            //{1} : FullName
                            //{2} : EnrollNumber
                            //{3} : Workplace
                            //{4} : WorkingScheduleTime
                            //{5} : Date
                            //{6} : In
                            //{7} : Out
                            //{8} : Late
                            //{9} : Early
                            //{10}: workTime
                            //{11}: workDay
                            //{12}: logs
                            //{13}: callbackLink
                            //{14}: Website
                            //{15}: link forgot password => use login
                            //{16}: ConfirmBeforeDate
                            #endregion
                            var url = Constants.System.domain;
                            var forgot = url + Constants.System.login;
                            string messageBody = string.Format(bodyBuilder.HtmlBody,
                                subject,
                                fullName,
                                employeeWorkTimeLog.EnrollNumber,
                                employeeWorkTimeLog.WorkplaceCode,
                                employeeWorkTimeLog.Start + "-" + employeeWorkTimeLog.End,
                                employeeWorkTimeLog.Date.ToString("dd/MM/yyyy"),
                                employeeWorkTimeLog.In,
                                employeeWorkTimeLog.Out,
                                employeeWorkTimeLog.Late == TimeSpan.FromHours(0) ? string.Empty : employeeWorkTimeLog.Late.ToString(),
                                employeeWorkTimeLog.Early == TimeSpan.FromHours(0) ? string.Empty : employeeWorkTimeLog.Early.ToString(),
                                employeeWorkTimeLog.WorkTime,
                                Math.Round(employeeWorkTimeLog.WorkDay, 2),
                                employeeWorkTimeLog.Logs,
                                linkSetting,
                                url,
                                forgot,
                                DateTime.Now.AddDays(1).ToShortDateString()
                                );
                            var emailMessage = new EmailMessage()
                            {
                                ToAddresses = tos,
                                Subject = subject,
                                BodyContent = messageBody
                            };
                            try
                            {
                                var message = new MimeMessage();
                                message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                                if (emailMessage.FromAddresses == null || emailMessage.FromAddresses.Count == 0)
                                {
                                    emailMessage.FromAddresses = new List<EmailAddress>
                                {
                                    new EmailAddress { Name = "[TRIBAT-HCNS-Thử nghiệm] Hệ thống tự động", Address = "test-erp@tribat.vn" }
                                };
                                }
                                message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                                message.Subject = emailMessage.Subject;
                                message.Body = new TextPart(TextFormat.Html)
                                {
                                    Text = emailMessage.BodyContent
                                };
                                using (var emailClient = new MailKit.Net.Smtp.SmtpClient())
                                {
                                    //The last parameter here is to use SSL (Which you should!)
                                    emailClient.Connect("test-erp@tribat.vn", 465, true);

                                    //Remove any OAuth functionality as we won't be using it. 
                                    emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                                    emailClient.Authenticate("test-erp@tribat.vn", "Kh0ngbiet@123");

                                    emailClient.Send(message);
                                    emailClient.Disconnect(true);
                                    Console.WriteLine("The mail has been sent successfully !!");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                    #endregion

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void UpdateSummary(MongoDBContext dbContext, DateTime to, DateTime from, int month, int year, EmployeeWorkTimeLog employeeWorkTimeLog)
        {
            var existSum = dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EnrollNumber.Equals(employeeWorkTimeLog.EnrollNumber) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            if (existSum != null)
            {
                var builderUpdateSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
                var filterUpdateSum = builderUpdateSum.Eq(m => m.Id, existSum.Id);
                var updateSum = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Inc(m => m.Workday, (double)employeeWorkTimeLog.WorkDay)
                    .Inc(m => m.WorkTime, employeeWorkTimeLog.WorkTime.TotalMinutes)
                    .Inc(m => m.Late, employeeWorkTimeLog.Late.TotalMinutes)
                    .Inc(m => m.Early, employeeWorkTimeLog.Early.TotalMinutes)
                    .Set(m => m.To, to)
                    .Set(m => m.LastUpdated, to);
                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterUpdateSum, updateSum);
            }
            else
            {
                dbContext.EmployeeWorkTimeMonthLogs.InsertOne(new EmployeeWorkTimeMonthLog
                {
                    EnrollNumber = employeeWorkTimeLog.EnrollNumber,
                    Month = month,
                    Year = year,
                    Workday = (double)employeeWorkTimeLog.WorkDay,
                    WorkTime = employeeWorkTimeLog.WorkTime.TotalMinutes,
                    Late = employeeWorkTimeLog.Late.TotalMinutes,
                    Early = employeeWorkTimeLog.Early.TotalMinutes,
                    From = from,
                    To = to,
                    LastUpdated = to
                });
            }
        }

        private static void UpdateSummary2(MongoDBContext dbContext, DateTime to, int month, int year, EmployeeWorkTimeLog employeeWorkTimeLog, double currentWorkTime, double currentLate, double currentEarly, decimal currentWorkDay)
        {
            var existSum = dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EnrollNumber.Equals(employeeWorkTimeLog.EnrollNumber) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            if (existSum != null)
            {
                var builderUpdateSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
                var filterUpdateSum = builderUpdateSum.Eq(m => m.Id, existSum.Id);
                var updateSumCurrent = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Inc(m => m.Workday, -(double)currentWorkDay)
                    .Inc(m => m.WorkTime, -currentWorkTime)
                    .Inc(m => m.Late, -currentLate)
                    .Inc(m => m.Early, -currentEarly);
                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterUpdateSum, updateSumCurrent);

                var updateSumNew = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Inc(m => m.Workday, (double)employeeWorkTimeLog.WorkDay)
                    .Inc(m => m.WorkTime, employeeWorkTimeLog.WorkTime.TotalMinutes)
                    .Inc(m => m.Late, employeeWorkTimeLog.Late.TotalMinutes)
                    .Inc(m => m.Early, employeeWorkTimeLog.Early.TotalMinutes)
                    .Set(m => m.To, to)
                    .Set(m => m.LastUpdated, to);
                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterUpdateSum, updateSumNew);
            }
        }
    }
}

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
            //var connectString = "mongodb://192.168.2.223:27017";
            var connectString = "mongodb://localhost:27017";
            #region Connection
            MongoDBContext.ConnectionString = connectString;
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            #region Setting
            // Get by date
            var mode = false;
            var day = -7;
            var setting = dbContext.Settings.Find(m => m.Key.Equals("schedulecoremode")).FirstOrDefault();
            if (setting != null && !string.IsNullOrEmpty(setting.Value))
            {
                // 1: get all data. true
                // 0: get by day. false
                mode = setting.Value == "1" ? true : false;
            }

            var attlogVps = new List<AttLog>();
            var attlogNMs = new List<AttLog>();
            if (mode)
            {
                dbContext.EmployeeWorkTimeLogs.DeleteMany(m => true);
                dbContext.EmployeeWorkTimeMonthLogs.DeleteMany(m => true);
                attlogVps = dbContext.X628CVPAttLogs.Find(m => true).ToList();
                if (attlogVps != null && attlogVps.Count > 0)
                {
                    Proccess(attlogVps, "VP", mode, connectString);
                }
                attlogNMs = dbContext.X928CNMAttLogs.Find(m => true).ToList();
                if (attlogNMs != null && attlogNMs.Count > 0)
                {
                    Proccess(attlogNMs, "NM", mode, connectString);
                }
            }
            else
            {
                var settingDay = dbContext.Settings.Find(m => m.Key.Equals("schedulecoreday")).FirstOrDefault();
                if (settingDay != null && !string.IsNullOrEmpty(settingDay.Value))
                {
                    day = Convert.ToInt32(settingDay.Value);
                }
                var dateCrawled = DateTime.Now.AddDays(day);
                var builder = Builders<AttLog>.Filter;
                var filter = builder.Gte(m => m.Date, dateCrawled);
                // Test
                filter = filter & builder.Eq(m => m.EnrollNumber, "514");
                attlogVps = dbContext.X628CVPAttLogs.Find(filter).ToList();
                if (attlogVps != null && attlogVps.Count > 0)
                {
                    Proccess(attlogVps, "VP", mode, connectString);
                }
                attlogNMs = dbContext.X928CNMAttLogs.Find(filter).ToList();
                if (attlogNMs != null && attlogNMs.Count > 0)
                {
                    Proccess(attlogNMs, "NM", mode, connectString);
                }
            }
            #endregion
        }

        private static void Proccess(List<AttLog> attlogs, string location, bool mode, string connectString)
        {
            #region Connection
            MongoDBContext.ConnectionString = connectString;
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            #region Settings
            var dateNewPolicy = new DateTime(2018, 9, 1);
            decimal workingScheduleHour = 8;
            var lunch = TimeSpan.FromHours(1);

            var modeMail = true;
            var modeTest = false;
            var dateBusiness = -1;
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
                        dateBusiness = Convert.ToInt32(dateMail.Value);
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

            var linkSetting = string.Empty;
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
                Console.WriteLine("Date: " + group.groupDate + ",fingerCode: " + group.groupCode + ",location: " + location);
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
                    var statusLate = 1;
                    var statusEarly = 1;

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
                    var employeeId = string.Empty;
                    var email = string.Empty;
                    var fullName = string.Empty;
                    var startWorkingScheduleTime = TimeSpan.FromHours(8);
                    var endWorkingScheduleTime = TimeSpan.FromHours(17);
                    var filterEmp = Builders<Employee>.Filter.ElemMatch(z => z.Workplaces, a => a.Fingerprint == enrollNumber);
                    var employee = dbContext.Employees.Find(filterEmp).FirstOrDefault();
                    if (employee != null)
                    {
                        employeeId = employee.Id;
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
                    // Rule: If the start time is before the starting hours, set it to the starting hour.
                    if (inLogTime < startWorkingScheduleTime) inLogTime = startWorkingScheduleTime;
                    if (outLogTime > endWorkingScheduleTime) outLogTime = endWorkingScheduleTime;

                    workTime = (outLogTime - inLogTime) - lunch;
                    workDay = Convert.ToDecimal(workTime.TotalHours) / workingScheduleHour;
                    if (inLogTime > startWorkingScheduleTime)
                    {
                        late = inLogTime - startWorkingScheduleTime;
                        statusLate = 0;
                        status = 0;
                    }
                    if (outLogTime < endWorkingScheduleTime)
                    {
                        early = endWorkingScheduleTime - outLogTime;
                        statusEarly = 0;
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
                            statusLate = 0;
                        }
                        else
                        {
                            // missing out
                            dboutLogTime = null;
                            early = new TimeSpan(0);
                            late = inLogTime - startWorkingScheduleTime;
                            workTime = TimeSpan.FromHours(4) - late;
                            statusEarly = 0;
                        }
                        status = 0;
                        workDay = Convert.ToDecimal(workTime.TotalHours) / workingScheduleHour;
                    }
                    #endregion
                    // Tính ngày, nếu trừ lương thì tính theo Status và số phút đi trễ,về sớm (Status, Status late| early and Late|Early.TotalMinutes)
                    // <4h : 0.5 day; > 5: 1 day
                    workDay = workTime.TotalHours > 4 ? 1 : (decimal)0.5;
                    // Cap nhat trang thai tai thoi diem ap dung, thoi gian truoc trang thai ok
                    if (dateFinger < dateNewPolicy)
                    {
                        status = 1;
                    }
                    var employeeWorkTimeLog = new EmployeeWorkTimeLog
                    {
                        EmployeeId = employeeId,
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
                        StatusLate = statusLate,
                        StatusEarly = statusEarly,
                        Logs = records
                    };

                    #region DB
                    if (mode)
                    {
                        dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                        Console.WriteLine("Insert db: EnrollNumber: " + employeeWorkTimeLog.EnrollNumber + ", date" + employeeWorkTimeLog.Date + ", status : " + employeeWorkTimeLog.Status);
                        UpdateSummary(dbContext, dateData, toDate, month, year, employeeWorkTimeLog);
                    }
                    else
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

                    #endregion

                    #endregion

                    #region Send Mail
                    if (status == 0 && !string.IsNullOrEmpty(email) && dateFinger > DateTime.Now.AddDays(dateBusiness))
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

            #region Update missing Date, Leave date, apply allow late|early,...
            // Apply current month
            var endDate = Utility.WorkingMonthToDate(string.Empty);
            var startDate = endDate.AddMonths(-1).AddDays(1);
            int yearCurrent = endDate.Year;
            int monthCurrent = endDate.Month;
            // For each employee have finger code
            // For startDate = > to Date. If missing and not sunday add missing times, if leave add reason leave- status.
            // if In date late or early, check allow count. update order a->n if still allow count.
            var filterEmployees = Builders<Employee>.Filter.ElemMatch(z => z.Workplaces, a => !string.IsNullOrEmpty(a.Fingerprint));
            var employees = dbContext.Employees.Find(filterEmployees).ToList();
            foreach (var employee in employees)
            {
                var monthTimeInformation = dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(employee.Id) && m.Year.Equals(yearCurrent) && m.Month.Equals(monthCurrent)).FirstOrDefault();
                var lateCountAllow = monthTimeInformation.LateCountAllow;
                double lateCountAllowUsed = monthTimeInformation.LateCountAllowUsed;
                int lateMinuteAllow = monthTimeInformation.LateMinuteAllow;
                var earlyCountAllow = monthTimeInformation.LateCountAllow;
                double earlyCountAllowUsed = monthTimeInformation.EarlyCountAllowUsed;
                int earlyMinuteAllow = monthTimeInformation.EarlyMinuteAllow;
                int noFingerDate = monthTimeInformation.NoFingerDate;

                // get times data in current month (EmployeeWorkTimeLog)
                var builder = Builders<EmployeeWorkTimeLog>.Filter;
                var filter = builder.Eq(m => m.EmployeeId, employee.Id);
                filter = filter & builder.Gt(m => m.Date, startDate.AddDays(-1)) & builder.Lt(m => m.Date, endDate.AddDays(1));
                var timekeepings = dbContext.EmployeeWorkTimeLogs.Find(filter).ToList();

                // get leaves data. remember remove sunday.
                var builderLeaves = Builders<Leave>.Filter;
                var filterLeaves = builderLeaves.Eq(m => m.EmployeeId, employee.Id);
                filterLeaves = filterLeaves & builderLeaves.Gt(m => m.From, startDate) & builderLeaves.Lte(m => m.To, endDate);
                var leaves = dbContext.Leaves.Find(filterLeaves).ToList();

                // get sunday data
                List<DateTime> list = new List<DateTime>();
                while (startDate <= endDate)
                {
                    if (startDate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        list.Add(startDate);
                    }
                    startDate = startDate.AddDays(1);
                }

                decimal sundayCount = 0;
                decimal holidayCount = 0;
                decimal leaveCount = 0;
                decimal leaveNotApproveCount = 0;
                decimal leaveAprovedCount = 0;
                
                for (DateTime date = DateTime.Now; date <= endDate; date = date.AddDays(1))
                {
                    var timekeeping = timekeepings.FirstOrDefault(item => item.Date == date);
                    // if exist check allow late|early
                    if (timekeeping != null)
                    {
                        // Cần xác nhận công (status = 0);
                        int status = timekeeping.Status;
                        int statusLate = timekeeping.Status;
                        int statusEarly = timekeeping.Status;
                        if (status == 0)
                        {
                            if (statusLate == 0 && timekeeping.Late.TotalMinutes <= lateMinuteAllow)
                            {
                                if (lateCountAllowUsed <= lateCountAllow)
                                {
                                    statusLate = 1;
                                    if (statusEarly == 1)
                                    {
                                        status = 1;
                                    }
                                    var filterUpdate = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);
                                    var update = Builders<EmployeeWorkTimeLog>.Update
                                        .Set(m => m.StatusLate, statusLate)
                                        .Set(m => m.Status, status)
                                        .Set(m => m.UpdatedBy, Constants.System.account)
                                        .Set(m => m.UpdatedOn, DateTime.Now);
                                    dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
                                    lateCountAllowUsed++;
                                }
                            }
                            if (statusEarly == 0 && timekeeping.Early.TotalMinutes <= earlyMinuteAllow)
                            {
                                if (earlyCountAllowUsed <= earlyCountAllow)
                                {
                                    statusEarly = 1;
                                    if (statusLate == 1)
                                    {
                                        status = 1;
                                    }
                                    var filterUpdate = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);
                                    var update = Builders<EmployeeWorkTimeLog>.Update
                                        .Set(m => m.StatusEarly, statusEarly)
                                        .Set(m => m.Status, status)
                                        .Set(m => m.UpdatedBy, Constants.System.account)
                                        .Set(m => m.UpdatedOn, DateTime.Now);
                                    dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
                                    earlyCountAllowUsed++;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Add missing data(leave,sunday,...)
                        if (date.DayOfWeek == DayOfWeek.Sunday)
                        {
                            var employeeWorkTimeLog = new EmployeeWorkTimeLog
                            {
                                EmployeeId = employee.Id,
                                Year = yearCurrent,
                                Month = monthCurrent,
                                Date = date
                            };
                            dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                            // Add Sunday
                            sundayCount++;
                        }
                        else
                        {
                            var existLeave = leaves.First(item => item.Status == 1 && item.From >= date && item.To <= date);
                            if (existLeave != null)
                            {
                                // add leave
                                var employeeWorkTimeLog = new EmployeeWorkTimeLog
                                {
                                    EmployeeId = employee.Id,
                                    Year = yearCurrent,
                                    Month = monthCurrent,
                                    Date = date,
                                    WorkTime = new TimeSpan(0, 0, 0),
                                    InOutMode = "1"
                                };
                                dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                                leaveCount++;
                            }
                            else
                            {
                                // add missing date
                                var employeeWorkTimeLog = new EmployeeWorkTimeLog
                                {
                                    EmployeeId = employee.Id,
                                    Year = yearCurrent,
                                    Month = monthCurrent,
                                    Date = date,
                                    WorkTime = new TimeSpan(0,0,0),
                                    StatusLate = 0,
                                    StatusEarly = 0,
                                    Status = 0
                                };
                                dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                                noFingerDate++;
                            }
                        }
                    }
                }

                #region update Summarry
                var builderUpdateSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
                //m => m.EnrollNumber.Equals(employeeWorkTimeLog.EnrollNumber) && m.Year.Equals(year) && m.Month.Equals(month)
                var filterUpdateSum = builderUpdateSum.Eq(m => m.EmployeeId, employee.Id);
                filterUpdateSum = filterUpdateSum & builderUpdateSum.Eq(m => m.Year, endDate.Year);
                filterUpdateSum = filterUpdateSum & builderUpdateSum.Eq(m => m.Month, endDate.Month);

                var updateSum = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Set(m => m.LateCountAllowUsed, (double)lateCountAllowUsed)
                    .Set(m => m.EarlyCountAllowUsed, (double)earlyCountAllowUsed)
                    .Set(m => m.Sunday, (double)sundayCount)
                    .Set(m => m.Holiday, (double)holidayCount)
                    .Set(m => m.LeaveDate, (double)leaveCount)
                    .Set(m => m.LeaveDateApproved, (double)leaveAprovedCount)
                    .Set(m => m.LeaveDateNotApprove, (double)leaveNotApproveCount)
                    .Set(m => m.NoFingerDate, noFingerDate)
                    .Set(m => m.LastUpdated, DateTime.Now);
                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterUpdateSum, updateSum);
                #endregion
            }
            #endregion

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
                var lateMinuteAllow = 5;
                var lateCountAllow = 1;
                var earlyMinuteAllow = 5;
                var earlyCountAllow = 1;
                var settingLateMinuteAllow = dbContext.Settings.Find(m => m.Key.Equals("lateMinuteAllow")).FirstOrDefault();
                if (settingLateMinuteAllow != null && !string.IsNullOrEmpty(settingLateMinuteAllow.Value))
                {
                    lateMinuteAllow = Convert.ToInt32(settingLateMinuteAllow.Value);
                }
                var settingLateCountAllow = dbContext.Settings.Find(m => m.Key.Equals("lateCountAllow")).FirstOrDefault();
                if (settingLateCountAllow != null && !string.IsNullOrEmpty(settingLateCountAllow.Value))
                {
                    lateCountAllow = Convert.ToInt32(settingLateCountAllow.Value);
                }
                var settingEarlyMinuteAllow = dbContext.Settings.Find(m => m.Key.Equals("earlyMinuteAllow")).FirstOrDefault();
                if (settingEarlyMinuteAllow != null && !string.IsNullOrEmpty(settingEarlyMinuteAllow.Value))
                {
                    earlyMinuteAllow = Convert.ToInt32(settingEarlyMinuteAllow.Value);
                }
                var settingEarlyCountAllow = dbContext.Settings.Find(m => m.Key.Equals("earlyCountAllow")).FirstOrDefault();
                if (settingEarlyCountAllow != null && !string.IsNullOrEmpty(settingEarlyCountAllow.Value))
                {
                    earlyCountAllow = Convert.ToInt32(settingEarlyCountAllow.Value);
                }
                dbContext.EmployeeWorkTimeMonthLogs.InsertOne(new EmployeeWorkTimeMonthLog
                {
                    EmployeeId = employeeWorkTimeLog.EmployeeId,
                    EnrollNumber = employeeWorkTimeLog.EnrollNumber,
                    Month = month,
                    Year = year,
                    Workday = (double)employeeWorkTimeLog.WorkDay,
                    WorkTime = employeeWorkTimeLog.WorkTime.TotalMinutes,
                    Late = employeeWorkTimeLog.Late.TotalMinutes,
                    Early = employeeWorkTimeLog.Early.TotalMinutes,
                    LateCountAllow = lateCountAllow,
                    LateMinuteAllow = lateMinuteAllow,
                    LateCountAllowUsed = 0,
                    LateCount = 0,
                    EarlyCountAllow = earlyCountAllow,
                    EarlyMinuteAllow = earlyMinuteAllow,
                    EarlyCountAllowUsed = 0,
                    EarlyCount = 0,
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

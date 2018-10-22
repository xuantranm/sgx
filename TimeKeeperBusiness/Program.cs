using Common.Utilities;
using Data;
using MimeKit;
using MimeKit.Text;
using Models;
using MongoDB.Driver;
using Services;
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
            UpdateTimeKeeper();
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
            // mode = true: Get all data
            // mode = false: get by date
            var mode = false;
            var day = -30;
            var modeEmail = false;

            var linkChamCong = Constants.System.domain + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Index;

            var dateCrawled = DateTime.Now.AddDays(day);
            var builder = Builders<AttLog>.Filter;
            var filter = builder.Gt(m => m.Date, dateCrawled.AddDays(-1));
            #endregion

            //#region UAT
            //var uat = dbContext.Settings.Find(m => m.Key.Equals("UAT")).FirstOrDefault();
            //if (uat != null && uat.Value == "true")
            //{
            //    dbContext.EmployeeWorkTimeLogs.DeleteMany(m => true);
            //    dbContext.EmployeeWorkTimeMonthLogs.DeleteMany(m => true);

            //    filter = filter & builder.Eq(m => m.EnrollNumber, "514");
            //}
            //#endregion

            dbContext.EmployeeWorkTimeLogs.DeleteMany(m => true);
            dbContext.EmployeeWorkTimeMonthLogs.DeleteMany(m => true);

            if (mode)
            {
                dbContext.EmployeeWorkTimeLogs.DeleteMany(m => true);
                dbContext.EmployeeWorkTimeMonthLogs.DeleteMany(m => true);
            }

            var attlogVps = mode ? dbContext.X628CVPAttLogs.Find(m => true).ToList() : dbContext.X628CVPAttLogs.Find(filter).ToList();
            if (attlogVps != null && attlogVps.Count > 0)
            {
                Proccess(attlogVps, "VP", mode, connectString, linkChamCong, modeEmail);
            }

            #region UAT
            //if (uat != null && uat.Value == "true")
            //{
            //    builder = Builders<AttLog>.Filter;
            //    filter = builder.Gt(m => m.Date, dateCrawled.AddDays(-1));
            //    filter = filter & builder.Eq(m => m.EnrollNumber, "259");
            //}
            #endregion

            var attlogNMs = mode ? dbContext.X928CNMAttLogs.Find(m => true).ToList() : dbContext.X928CNMAttLogs.Find(filter).ToList();
            if (attlogNMs != null && attlogNMs.Count > 0)
            {
                Proccess(attlogNMs, "NM", mode, connectString, linkChamCong, modeEmail);
            }

            UpdateFinal(dbContext);
        }

        private static void Proccess(List<AttLog> attlogs, string location, bool mode, string connectString, string linkChamCong, bool modeEmail)
        {
            #region Connection
            MongoDBContext.ConnectionString = connectString;
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            #region Config
            var dateNewPolicy = new DateTime(2018, 10, 01);
            double workingScheduleHour = 8;
            var lunch = TimeSpan.FromHours(1);

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
                Console.WriteLine("Date: " + group.groupDate + ", fingerCode: " + group.groupCode + ", location: " + location);
                try
                {
                    var dateData = DateTime.ParseExact(group.groupDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var workTime = new TimeSpan(0);
                    double workDay = 1;
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
                    var title = string.Empty;
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
                        title = employee.Title;
                        linkFinger = linkChamCong + employee.Id;
                    }
                    #endregion

                    #region Procees Times
                    // No Data
                    if (!dbinLogTime.HasValue && !dboutLogTime.HasValue)
                    {
                        workTime = new TimeSpan(0);
                        workDay = 0;
                        status = 0;
                    }
                    else
                    {
                        if (group.times != null && group.times.Count > 1)
                        {
                            if (dateFinger < dateNewPolicy)
                            {
                                status = 1;
                                statusEarly = 1;
                                statusLate = 1;
                                workDay = 1;
                                workTime = TimeSpan.FromHours(8);
                            }
                            else
                            {
                                // Rule: If the start time is before the starting hours, set it to the starting hour.
                                // New Rule: 
                                // ** Nếu không được xác nhận công:
                                //  - Trễ 15 phút không tính buổi nào không tính buổi đó. (1 buổi tương đương 0.5 ngày)
                                //  - Trễ  dưới 15 phút lưu để trừ thưởng,...
                                if (inLogTime < startWorkingScheduleTime) inLogTime = startWorkingScheduleTime;
                                if (outLogTime > endWorkingScheduleTime) outLogTime = endWorkingScheduleTime;

                                workTime = (outLogTime - inLogTime) - lunch;
                                //workDay = workTime.TotalHours / workingScheduleHour;

                                if (inLogTime > startWorkingScheduleTime)
                                {
                                    late = inLogTime - startWorkingScheduleTime;
                                    statusLate = 0;
                                    status = 0;
                                    //workDay = workDay - 0.5;
                                }
                                if (outLogTime < endWorkingScheduleTime)
                                {
                                    early = endWorkingScheduleTime - outLogTime;
                                    statusEarly = 0;
                                    status = 0;
                                    //workDay = workDay - 0.5;
                                }
                            }
                        }
                        else
                        {
                            // Check miss in/out
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
                                workDay = 0.5;
                                //workDay = workTime.TotalHours / workingScheduleHour;
                            }
                            if (dateFinger < dateNewPolicy)
                            {
                                status = 1;
                                statusEarly = 1;
                                statusLate = 1;
                                workDay = 0.5;
                                workTime = TimeSpan.FromHours(4);
                            }
                        }
                    }

                    var employeeWorkTimeLog = new EmployeeWorkTimeLog
                    {
                        EmployeeId = employeeId,
                        EmployeeName = fullName,
                        EmployeeTitle = title,
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
                    // mode = true: Get all data
                    // mode = false: get by date
                    if (mode)
                    {
                        dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                        Console.WriteLine("Insert db: EnrollNumber: " + employeeWorkTimeLog.EnrollNumber + ", date" + employeeWorkTimeLog.Date + ", status : " + employeeWorkTimeLog.Status);
                        UpdateSummary(dbContext, dateData, employeeWorkTimeLog);
                    }
                    else
                    {
                        var employeeWorkTimeLogDb = dbContext.EmployeeWorkTimeLogs.Find(m => m.EnrollNumber.Equals(employeeWorkTimeLog.EnrollNumber)
                                                 && m.Date.Equals(employeeWorkTimeLog.Date)).FirstOrDefault();
                        if (employeeWorkTimeLogDb == null)
                        {
                            dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                            Console.WriteLine("Insert db: EnrollNumber: " + employeeWorkTimeLog.EnrollNumber + ", date" + employeeWorkTimeLog.Date + ", status : " + employeeWorkTimeLog.Status);
                            UpdateSummary(dbContext, dateData, employeeWorkTimeLog);
                        }
                        else
                        {
                            if (employeeWorkTimeLogDb.Status == 0)
                            {
                                var currentWorkTime = employeeWorkTimeLogDb.WorkTime.TotalMilliseconds;
                                var currentLate = employeeWorkTimeLogDb.Late.TotalMilliseconds;
                                var currentEarly = employeeWorkTimeLogDb.Early.TotalMilliseconds;
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

                                UpdateSummaryChangeData(dbContext, dateData, employeeWorkTimeLog, currentWorkTime, currentLate, currentEarly, currentWorkDay);
                            }
                        }
                    }

                    #endregion

                    #endregion

                    #region Send Mail
                    if (modeEmail)
                    {
                        if (status == 0 && dateData == DateTime.Now.Date.AddDays(-1) && !string.IsNullOrEmpty(email))
                        {
                            email = "xuan.tm@tribat.vn";
                            Console.WriteLine("Sending mail...");
                            var tos = new List<EmailAddress>
                            {
                                new EmailAddress { Name = fullName, Address = email }
                            };
                            var webRoot = Environment.CurrentDirectory;
                            var pathToFile = Environment.CurrentDirectory + "/Templates/TimeKeeperNotice.html";
                            var subject = "Xác nhận thời gian làm việc.";
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
                                linkChamCong,
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

                            new AuthMessageSender().SendEmail(emailMessage);
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

        private static void UpdateSummary(MongoDBContext dbContext, DateTime dateData, EmployeeWorkTimeLog employeeWorkTimeLog)
        {
            int month = dateData.Month;
            int year = dateData.Year;
            if (dateData.Day > 25)
            {
                // 26->31: chấm công tháng sau
                var nextMonthDate = dateData.AddMonths(1);
                month = nextMonthDate.Month;
                year = nextMonthDate.Year;
            }
            var existSum = dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(employeeWorkTimeLog.EmployeeId) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            if (existSum != null)
            {
                // Do later
                //if (existSum.EnrollNumber != employeeWorkTimeLog.EnrollNumber){}
                var builderUpdateSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
                var filterUpdateSum = builderUpdateSum.Eq(m => m.Id, existSum.Id);
                var updateSum = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Inc(m => m.Workday, employeeWorkTimeLog.WorkDay)
                    .Inc(m => m.WorkTime, employeeWorkTimeLog.WorkTime.TotalMilliseconds)
                    .Inc(m => m.Late, employeeWorkTimeLog.Late.TotalMilliseconds)
                    .Inc(m => m.Early, employeeWorkTimeLog.Early.TotalMilliseconds)
                    .Set(m => m.LastUpdated, dateData);
                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterUpdateSum, updateSum);
            }
            else
            {
                // Qui dinh 1 thang cho phep thiếu giờ (tre/sơm) 15p,
                // Qua 15 phut nếu ko có xác nhận công trừ 0.5 ngày
                var missingMinuteAllow = 15;
                var settingMissingMinuteAllow = dbContext.Settings.Find(m => m.Key.Equals("missingMinuteAllow")).FirstOrDefault();
                if (settingMissingMinuteAllow != null && !string.IsNullOrEmpty(settingMissingMinuteAllow.Value))
                {
                    missingMinuteAllow = Convert.ToInt32(settingMissingMinuteAllow.Value);
                }
                //var lateMinuteAllow = 5;
                //var lateCountAllow = 1;
                //var earlyMinuteAllow = 5;
                //var earlyCountAllow = 1;
                //var settingLateMinuteAllow = dbContext.Settings.Find(m => m.Key.Equals("lateMinuteAllow")).FirstOrDefault();
                //if (settingLateMinuteAllow != null && !string.IsNullOrEmpty(settingLateMinuteAllow.Value))
                //{
                //    lateMinuteAllow = Convert.ToInt32(settingLateMinuteAllow.Value);
                //}
                //var settingLateCountAllow = dbContext.Settings.Find(m => m.Key.Equals("lateCountAllow")).FirstOrDefault();
                //if (settingLateCountAllow != null && !string.IsNullOrEmpty(settingLateCountAllow.Value))
                //{
                //    lateCountAllow = Convert.ToInt32(settingLateCountAllow.Value);
                //}
                //var settingEarlyMinuteAllow = dbContext.Settings.Find(m => m.Key.Equals("earlyMinuteAllow")).FirstOrDefault();
                //if (settingEarlyMinuteAllow != null && !string.IsNullOrEmpty(settingEarlyMinuteAllow.Value))
                //{
                //    earlyMinuteAllow = Convert.ToInt32(settingEarlyMinuteAllow.Value);
                //}
                //var settingEarlyCountAllow = dbContext.Settings.Find(m => m.Key.Equals("earlyCountAllow")).FirstOrDefault();
                //if (settingEarlyCountAllow != null && !string.IsNullOrEmpty(settingEarlyCountAllow.Value))
                //{
                //    earlyCountAllow = Convert.ToInt32(settingEarlyCountAllow.Value);
                //}

                dbContext.EmployeeWorkTimeMonthLogs.InsertOne(new EmployeeWorkTimeMonthLog
                {
                    EmployeeId = employeeWorkTimeLog.EmployeeId,
                    //EnrollNumber = employeeWorkTimeLog.EnrollNumber,
                    Month = month,
                    Year = year,
                    Workday = employeeWorkTimeLog.WorkDay,
                    WorkTime = employeeWorkTimeLog.WorkTime.TotalMilliseconds,
                    Late = employeeWorkTimeLog.Late.TotalMilliseconds,
                    Early = employeeWorkTimeLog.Early.TotalMilliseconds,
                    LateApprove = 0,
                    EarlyApprove = 0,
                    MissingMinuteAllow = missingMinuteAllow,
                    //LateCountAllow = 0,
                    //LateMinuteAllow = lateMinuteAllow,
                    //LateCountAllowUsed = 0,
                    //LateCount = 0,
                    //EarlyCountAllow = earlyCountAllow,
                    //EarlyMinuteAllow = earlyMinuteAllow,
                    //EarlyCountAllowUsed = 0,
                    //EarlyCount = 0,
                    LastUpdated = dateData
                });
            }
        }

        private static void UpdateSummaryChangeData(MongoDBContext dbContext, DateTime dateData, EmployeeWorkTimeLog employeeWorkTimeLog, double currentWorkTime, double currentLate, double currentEarly, double currentWorkDay)
        {
            int month = dateData.Month;
            int year = dateData.Year;
            if (dateData.Day > 25)
            {
                // 26->31: chấm công tháng sau
                var nextMonthDate = dateData.AddMonths(1);
                month = nextMonthDate.Month;
                year = nextMonthDate.Year;
            }
            var existSum = dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(employeeWorkTimeLog.EmployeeId) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            if (existSum != null)
            {
                var builderUpdateSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
                var filterUpdateSum = builderUpdateSum.Eq(m => m.Id, existSum.Id);
                var updateSumCurrent = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Inc(m => m.Workday, -currentWorkDay)
                    .Inc(m => m.WorkTime, -currentWorkTime)
                    .Inc(m => m.Late, -currentLate)
                    .Inc(m => m.Early, -currentEarly);
                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterUpdateSum, updateSumCurrent);

                var updateSumNew = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Inc(m => m.Workday, employeeWorkTimeLog.WorkDay)
                    .Inc(m => m.WorkTime, employeeWorkTimeLog.WorkTime.TotalMilliseconds)
                    .Inc(m => m.Late, employeeWorkTimeLog.Late.TotalMilliseconds)
                    .Inc(m => m.Early, employeeWorkTimeLog.Early.TotalMilliseconds);
                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterUpdateSum, updateSumNew);
            }
        }

        private static void UpdateFinal(MongoDBContext dbContext)
        {
            // Update missing Date, Leave date, apply allow late|early,...
            // Apply current month
            var endDate = Utility.WorkingMonthToDate(DateTime.Now.Month + "-" + DateTime.Now.Year);
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
                if (monthTimeInformation != null)
                {
                    var missingMinuteAllow = monthTimeInformation.MissingMinuteAllow;
                    var missingMinuteAllowUsed = monthTimeInformation.MissingMinuteAllowUsed;
                    var missingMinuteAllowCanUse = missingMinuteAllow - missingMinuteAllowUsed;
                    int missingMinuteAllowUsedCount = 0;
                    //var lateCountAllow = monthTimeInformation.LateCountAllow;
                    //double lateCountAllowUsed = monthTimeInformation.LateCountAllowUsed;
                    //int lateMinuteAllow = monthTimeInformation.LateMinuteAllow;
                    //var earlyCountAllow = monthTimeInformation.LateCountAllow;
                    //double earlyCountAllowUsed = monthTimeInformation.EarlyCountAllowUsed;
                    //int earlyMinuteAllow = monthTimeInformation.EarlyMinuteAllow;
                    int noFingerDate = monthTimeInformation.NoFingerDate;
                    double lateApprove = monthTimeInformation.LateApprove;
                    double earlyApprove = monthTimeInformation.EarlyApprove;

                    // get times data in current month (EmployeeWorkTimeLog)
                    var builder = Builders<EmployeeWorkTimeLog>.Filter;
                    var filter = builder.Eq(m => m.EmployeeId, employee.Id);
                    //filter = filter & builder.Gte(m => m.Date, startDate) & builder.Lte(m => m.Date, endDate);
                    filter = filter & builder.Gt(m => m.Date, startDate.AddDays(-1)) & builder.Lt(m => m.Date, endDate.AddDays(1));
                    var timekeepings = dbContext.EmployeeWorkTimeLogs.Find(filter).ToList();

                    // get leaves data. remember remove sunday.
                    var builderLeaves = Builders<Leave>.Filter;
                    var filterLeaves = builderLeaves.Eq(m => m.EmployeeId, employee.Id);
                    filterLeaves = filterLeaves & builderLeaves.Gt(m => m.From, startDate) & builderLeaves.Lte(m => m.To, endDate);
                    var leaves = dbContext.Leaves.Find(filterLeaves).ToList();

                    decimal workday = 0;
                    var late = new TimeSpan(0);
                    var early = new TimeSpan(0);
                    decimal sundayCount = 0;
                    decimal ngayNghiHuongLuong = 0;
                    decimal ngayNghiLeTetHuongLuong = 0;
                    decimal congCNGio = 0;
                    decimal congTangCaNgayThuongGio = 0;
                    decimal congLeTet = 0;
                    decimal holidayCount = 0;
                    decimal leaveCount = 0;
                    decimal leaveNotApproveCount = 0;
                    decimal leaveAprovedCount = 0;

                    for (DateTime date = startDate; date <= DateTime.Now; date = date.AddDays(1))
                    {
                        var timekeeping = timekeepings.FirstOrDefault(item => item.Date == date);
                        // if exist check allow late|early
                        if (timekeeping != null)
                        {
                            // Cần xác nhận công (status = 0);
                            int status = timekeeping.Status;
                            int statusLate = timekeeping.StatusLate;
                            int statusEarly = timekeeping.StatusEarly;
                            if (status == 0)
                            {
                                if (statusLate == 0 && timekeeping.In.HasValue)
                                {
                                    var lateMinute = timekeeping.Late.TotalMinutes;
                                    if (lateMinute > 15)
                                    {
                                        // Trừ 0.5 ngay
                                        var filterUpdate = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);
                                        var update = Builders<EmployeeWorkTimeLog>.Update
                                            .Inc(m => m.WorkDay, -0.5);
                                        dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
                                        workday += (decimal)0.5;
                                        late += timekeeping.Late;
                                    }
                                    //if (lateMinute <= missingMinuteAllow && lateMinute <= missingMinuteAllowCanUse)
                                    //{
                                    //    lateApprove += timekeeping.Late.TotalMilliseconds;
                                    //    statusLate = 3;
                                    //    if (statusEarly == 1 || statusEarly == 3)
                                    //    {
                                    //        status = 5;
                                    //    }
                                    //    var filterUpdate = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);
                                    //    var update = Builders<EmployeeWorkTimeLog>.Update
                                    //        .Inc(m => m.WorkDay, 0.5)
                                    //        .Set(m => m.StatusLate, statusLate)
                                    //        .Set(m => m.Status, status)
                                    //        .Set(m => m.UpdatedBy, Constants.System.account)
                                    //        .Set(m => m.UpdatedOn, DateTime.Now);
                                    //    dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
                                    //    missingMinuteAllowCanUse -= lateMinute;
                                    //    workday += (decimal)0.5;
                                    //    missingMinuteAllowUsedCount++;
                                    //}
                                }
                                if (statusEarly == 0 && timekeeping.Out.HasValue)
                                {
                                    var earlyMinute = timekeeping.Early.TotalMinutes;

                                    if(earlyMinute > 15)
                                    {
                                        // Trừ 0.5 ngay
                                        var filterUpdate = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);
                                        var update = Builders<EmployeeWorkTimeLog>.Update
                                            .Inc(m => m.WorkDay, -0.5);
                                        dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
                                        workday += (decimal)0.5;
                                        early += timekeeping.Late;
                                    }

                                    //if (earlyMinute <= missingMinuteAllow && earlyMinute <= missingMinuteAllowCanUse)
                                    //{
                                    //    // xac nhan
                                    //    earlyApprove += timekeeping.Early.TotalMilliseconds;
                                    //    statusEarly = 3;
                                    //    if (statusLate == 1 || statusLate == 3)
                                    //    {
                                    //        status = 5;
                                    //    }
                                    //    var filterUpdate = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);
                                    //    var update = Builders<EmployeeWorkTimeLog>.Update
                                    //        .Inc(m => m.WorkDay, 0.5)
                                    //        .Set(m => m.StatusEarly, statusEarly)
                                    //        .Set(m => m.Status, status)
                                    //        .Set(m => m.UpdatedBy, Constants.System.account)
                                    //        .Set(m => m.UpdatedOn, DateTime.Now);
                                    //    dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
                                    //    missingMinuteAllowCanUse -= earlyMinute;
                                    //    workday += (decimal)0.5;
                                    //    missingMinuteAllowUsedCount++;
                                    //}
                                }

                                #region Comment rule late & early
                                //if (statusLate == 0 && timekeeping.In.HasValue && timekeeping.Late.TotalMinutes <= lateMinuteAllow)
                                //{
                                //    if (lateCountAllowUsed < lateCountAllow)
                                //    {
                                //        // xac nhan
                                //        lateApprove += timekeeping.Late.TotalMilliseconds;
                                //        statusLate = 3;
                                //        if (statusEarly == 1 || statusEarly == 3)
                                //        {
                                //            status = 3;
                                //        }
                                //        var filterUpdate = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);
                                //        var update = Builders<EmployeeWorkTimeLog>.Update
                                //            .Set(m => m.StatusLate, statusLate)
                                //            .Set(m => m.Status, status)
                                //            .Set(m => m.UpdatedBy, Constants.System.account)
                                //            .Set(m => m.UpdatedOn, DateTime.Now);
                                //        dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
                                //        lateCountAllowUsed++;
                                //    }
                                //}
                                //if (statusEarly == 0 && timekeeping.Out.HasValue && timekeeping.Early.TotalMinutes <= earlyMinuteAllow)
                                //{
                                //    if (earlyCountAllowUsed < earlyCountAllow)
                                //    {
                                //        // xac nhan
                                //        earlyApprove += timekeeping.Early.TotalMilliseconds;
                                //        statusEarly = 3;
                                //        if (statusLate == 1 || statusLate == 3)
                                //        {
                                //            status = 3;
                                //        }
                                //        var filterUpdate = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);
                                //        var update = Builders<EmployeeWorkTimeLog>.Update
                                //            .Set(m => m.StatusEarly, statusEarly)
                                //            .Set(m => m.Status, status)
                                //            .Set(m => m.UpdatedBy, Constants.System.account)
                                //            .Set(m => m.UpdatedOn, DateTime.Now);
                                //        dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
                                //        earlyCountAllowUsed++;
                                //    }
                                //}
                                #endregion
                            }
                        }
                        else
                        {
                            // Add missing data(leave,sunday, le, tet...)
                            if (date.DayOfWeek == DayOfWeek.Sunday)
                            {
                                var employeeWorkTimeLog = new EmployeeWorkTimeLog
                                {
                                    EmployeeId = employee.Id,
                                    Year = yearCurrent,
                                    Month = monthCurrent,
                                    Date = date,
                                    Workcode = "Chủ nhật"
                                };
                                dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                                // Add Sunday
                                sundayCount++;
                            }
                            else
                            {
                                var existLeave = leaves.FirstOrDefault(item => item.Enable.Equals(true) && date >= item.From.Date && date <= item.To.Date);
                                if (existLeave != null)
                                {
                                    // add leave
                                    var workCode = existLeave.TypeName;
                                    var status = existLeave.Status;
                                    //var leaveTypeId = existLeave.TypeId;
                                    switch (status)
                                    {
                                        case 0:
                                            workCode += " (chờ duyệt)";
                                            break;
                                        case 1:
                                            workCode += " (đã duyệt)";
                                            break;
                                        case 2:
                                            workCode += " (không duyệt)";
                                            break;
                                        case 3:
                                            workCode += " (tạm hoãn và chờ duyệt)";
                                            break;
                                    }
                                    var employeeWorkTimeLog = new EmployeeWorkTimeLog
                                    {
                                        EmployeeId = employee.Id,
                                        Year = yearCurrent,
                                        Month = monthCurrent,
                                        Date = date,
                                        WorkTime = new TimeSpan(0, 0, 0),
                                        InOutMode = "1",
                                        Workcode = workCode
                                    };
                                    dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                                    // phan loai nghi
                                    if (existLeave.TypeId == "5bb45d407fe7602e04fb5981")
                                    {
                                        ngayNghiHuongLuong++;
                                    }
                                    leaveCount++;
                                }
                                else
                                {
                                    // add missing date
                                    // If now
                                    if (date != DateTime.Now.Date)
                                    {
                                        var employeeWorkTimeLog = new EmployeeWorkTimeLog
                                        {
                                            EmployeeId = employee.Id,
                                            Year = yearCurrent,
                                            Month = monthCurrent,
                                            Date = date,
                                            WorkTime = new TimeSpan(0, 0, 0),
                                            StatusLate = 0,
                                            StatusEarly = 0,
                                            Status = 0,
                                            Workcode = "0"
                                        };
                                        dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                                        noFingerDate++;
                                    }
                                    else
                                    {
                                        var employeeWorkTimeLog = new EmployeeWorkTimeLog
                                        {
                                            EmployeeId = employee.Id,
                                            Year = yearCurrent,
                                            Month = monthCurrent,
                                            Date = date,
                                            WorkTime = new TimeSpan(0, 0, 0),
                                            StatusLate = 0,
                                            StatusEarly = 0,
                                            Status = 0,
                                            Workcode = "Chờ chấm công"
                                        };
                                        dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                                    }
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
                        //.Set(m => m.LateCountAllowUsed, lateCountAllowUsed)
                        //.Set(m => m.EarlyCountAllowUsed, earlyCountAllowUsed)
                        .Set(m => m.MissingMinuteAllowUsed, missingMinuteAllow - missingMinuteAllowCanUse)
                        .Inc(m => m.MissingMinuteAllowUsedCount, missingMinuteAllowUsedCount)
                        .Inc(m => m.Workday, -(double)workday)
                        .Inc(m => m.Late, -late.TotalMilliseconds)
                        .Inc(m => m.Early, -early.TotalMilliseconds)
                        .Set(m => m.LateApprove, lateApprove)
                        .Set(m => m.EarlyApprove, earlyApprove)
                        .Set(m => m.Sunday, (double)sundayCount)
                        .Set(m => m.Holiday, (double)holidayCount)
                        .Set(m => m.NgayNghiHuongLuong, (double)ngayNghiHuongLuong)

                        .Set(m => m.LeaveDate, (double)leaveCount)
                        .Set(m => m.LeaveDateApproved, (double)leaveAprovedCount)
                        .Set(m => m.LeaveDateNotApprove, (double)leaveNotApproveCount)
                        .Set(m => m.NoFingerDate, noFingerDate)
                        .Set(m => m.LastUpdated, DateTime.Now);
                    dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterUpdateSum, updateSum);
                    #endregion
                }
            }
        }
    }
}

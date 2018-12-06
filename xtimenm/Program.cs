using Common.Enums;
using Common.Utilities;
using Data;
using MimeKit;
using Models;
using MongoDB.Driver;
using Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace xtimenm
{
    class Program
    {
        static void Main(string[] args)
        {
            UpdateTimeKeeper("NM");
        }

        static void UpdateTimeKeeper(string location)
        {
            #region Connection
            //var connectString = "mongodb://192.168.2.223:27017";
            var connectString = "mongodb://localhost:27017";
            MongoDBContext.ConnectionString = connectString;
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            #region Setting
            // mode = true: Get all data
            // mode = false: get by date
            var mode = false;
            var day = -7;
            var modeEmail = true;

            var linkChamCong = Constants.System.domain + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Index;

            var dateCrawled = DateTime.Now.AddDays(day);
            var builder = Builders<AttLog>.Filter;
            var filter = builder.Gt(m => m.Date, dateCrawled.AddDays(-1));
            #endregion

            if (mode)
            {
                // remove CN, leave
                // Run on VP task
                //dbContext.EmployeeWorkTimeLogs.DeleteMany(m => true && string.IsNullOrEmpty(m.SecureCode));
                var statusRemoves = new List<int>()
                {
                    (int)StatusWork.XacNhanCong,
                    (int)StatusWork.DuCong
                };
                dbContext.EmployeeWorkTimeLogs.DeleteMany(m => true && statusRemoves.Contains(m.Status) && m.WorkplaceCode.Equals(location));
                dbContext.EmployeeWorkTimeMonthLogs.DeleteMany(m => true && m.WorkplaceCode.Equals(location));

                // Run here for small data
                UpdateTimeMonth(dbContext, location);
            }

            var attlogs = mode ? dbContext.X928CNMAttLogs.Find(m => true).ToList() : dbContext.X928CNMAttLogs.Find(filter).ToList();
            // Debug
            //attlogs = dbContext.X928CNMAttLogs.Find(m => true && m.EnrollNumber.Equals("223")).ToList();
            // End debug
            if (attlogs != null && attlogs.Count > 0)
            {
                Proccess(attlogs, location, mode, connectString, linkChamCong, modeEmail);
            }

            UpdateFinal(dbContext, location, mode, day);
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
                var dateData = DateTime.ParseExact(group.groupDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                int year = dateData.Day > 25 ? dateData.AddMonths(1).Year : dateData.Year;
                int month = dateData.Day > 25 ? dateData.AddMonths(1).Month : dateData.Month;

                var workTime = new TimeSpan(0);
                double workDay = 1;
                var late = new TimeSpan(0);
                var early = new TimeSpan(0);
                // 0: cần xác nhận công; 1: đủ ngày công ; 2: đã gửi xác nhận công, 3: đồng ý; 4: từ chối  
                var status = (int)StatusWork.DuCong;
                var statusLate = (int)StatusWork.DuCong;
                var statusEarly = (int)StatusWork.DuCong;

                var linkFinger = string.Empty;
                var enrollNumber = Convert.ToInt32(group.groupCode).ToString("000");
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
                var department = string.Empty;
                var part = string.Empty;
                var title = string.Empty;
                var startWorkingScheduleTime = new TimeSpan(7, 30, 0);
                var endWorkingScheduleTime = new TimeSpan(16, 30, 0);

                // Get employees work in VP
                var filterEmp = Builders<Employee>.Filter.Eq(m => m.Enable, true)
                    & Builders<Employee>.Filter.ElemMatch(z => z.Workplaces, a => a.Code == location)
                    & Builders<Employee>.Filter.ElemMatch(z => z.Workplaces, a => a.Fingerprint == enrollNumber);
                var employee = dbContext.Employees.Find(filterEmp).FirstOrDefault();
                if (employee == null)
                {
                    continue;
                }

                var employeeLocation = employee.Workplaces.FirstOrDefault(a => a.Code == location);
                // Truong hop nhieu ca...
                // ....
                if (employeeLocation != null && !string.IsNullOrEmpty(employeeLocation.WorkingScheduleTime))
                {
                    startWorkingScheduleTime = TimeSpan.Parse(employeeLocation.WorkingScheduleTime.Split('-')[0].Trim());
                    endWorkingScheduleTime = TimeSpan.Parse(employeeLocation.WorkingScheduleTime.Split('-')[1].Trim());
                }

                employeeId = employee.Id;
                email = employee.Email;
                fullName = employee.FullName;
                part = employee.Part;
                department = employee.Department;
                title = employee.Title;
                linkFinger = linkChamCong + employee.Id;
                #endregion

                #region Procees Times
                //if (dateData >= new DateTime(2018, 10, 31))
                //{
                //    var debug = true;
                //}
                if (!dbinLogTime.HasValue && !dboutLogTime.HasValue)
                {
                    workTime = new TimeSpan(0);
                    workDay = 0;
                    status = (int)StatusWork.XacNhanCong;
                }
                else
                {
                    if (group.times != null && group.times.Count > 1)
                    {
                        if (inLogTime < startWorkingScheduleTime) inLogTime = startWorkingScheduleTime;
                        if (outLogTime > endWorkingScheduleTime) outLogTime = endWorkingScheduleTime;

                        workTime = (outLogTime - inLogTime) - lunch;
                        if (inLogTime > startWorkingScheduleTime)
                        {
                            late = inLogTime - startWorkingScheduleTime;
                            statusLate = (int)StatusWork.XacNhanCong;
                            status = (int)StatusWork.XacNhanCong;
                        }
                        if (outLogTime < endWorkingScheduleTime)
                        {
                            early = endWorkingScheduleTime - outLogTime;
                            statusEarly = (int)StatusWork.XacNhanCong;
                            status = (int)StatusWork.XacNhanCong;
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
                                if (outLogTime > endWorkingScheduleTime) outLogTime = endWorkingScheduleTime;
                                early = endWorkingScheduleTime - outLogTime;
                                if (early.TotalMinutes > 0)
                                {
                                    statusEarly = (int)StatusWork.XacNhanCong;
                                }
                                workTime = TimeSpan.FromHours(4) - early;
                                statusLate = (int)StatusWork.XacNhanCong;
                            }
                            else
                            {
                                // missing out
                                dboutLogTime = null;
                                early = new TimeSpan(0);
                                if (inLogTime < startWorkingScheduleTime) inLogTime = startWorkingScheduleTime;
                                late = inLogTime - startWorkingScheduleTime;
                                if (late.TotalMinutes > 0)
                                {
                                    statusLate = (int)StatusWork.XacNhanCong;
                                }
                                workTime = TimeSpan.FromHours(4) - late;
                                statusEarly = (int)StatusWork.XacNhanCong;
                            }
                            status = (int)StatusWork.XacNhanCong;
                            workDay = 0.5;
                        }
                    }
                }

                if (dateData < new DateTime(2018, 10, 26))
                {
                    status = (int)StatusWork.DuCong;
                    statusLate = (int)StatusWork.DuCong;
                    statusEarly = (int)StatusWork.DuCong;
                }

                var employeeWorkTimeLog = new EmployeeWorkTimeLog
                {
                    Year = year,
                    Month = month,
                    EmployeeId = employeeId,
                    EmployeeName = fullName,
                    EmployeeTitle = title,
                    Department = department,
                    Part = part,
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
                    Logs = records,
                    Mode = (int)TimeWork.Normal
                };
                #endregion

                #region DB
                // Store multi workplace.
                // Query base full time location. 1 > 0.5 > ...
                // EmployeeId and Date.
                //var employeeWorkTimeLogDb = dbContext.EmployeeWorkTimeLogs
                //                            .Find(m => m.EnrollNumber.Equals(employeeWorkTimeLog.EnrollNumber)
                //                                && m.EmployeeId.Equals(employeeWorkTimeLog.EmployeeId)
                //                                && m.WorkplaceCode.Equals(employeeWorkTimeLog.WorkplaceCode)
                //                                && m.Date.Equals(employeeWorkTimeLog.Date)).FirstOrDefault();
                var employeeWorkTimeLogDb = dbContext.EmployeeWorkTimeLogs
                                            .Find(m => m.EmployeeId.Equals(employeeWorkTimeLog.EmployeeId)
                                                && m.Date.Equals(employeeWorkTimeLog.Date)).FirstOrDefault();

                if (employeeWorkTimeLogDb == null)
                {
                    dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                    Console.WriteLine("Insert db: EnrollNumber: " + employeeWorkTimeLog.EnrollNumber + ", date" + employeeWorkTimeLog.Date + ", status : " + employeeWorkTimeLog.Status);
                    UpdateSummary(dbContext, dateData, employeeWorkTimeLog);
                }
                else
                {
                    if (employeeWorkTimeLogDb.Status == (int)StatusWork.XacNhanCong)
                    {
                        var currentWorkDay = employeeWorkTimeLogDb.WorkDay;
                        var currentWorkTime = employeeWorkTimeLogDb.WorkTime.TotalMilliseconds;
                        var currentLate = employeeWorkTimeLogDb.Late.TotalMilliseconds;
                        var currentEarly = employeeWorkTimeLogDb.Early.TotalMilliseconds;

                        // Update full time
                        var inUp = employeeWorkTimeLog.In;
                        var outUp = employeeWorkTimeLog.Out;
                        var workTimeUp = employeeWorkTimeLog.WorkTime;
                        var workDayUp = employeeWorkTimeLog.WorkDay;
                        var lateUp = employeeWorkTimeLog.Late;
                        var earlyUp = employeeWorkTimeLog.Early;
                        var statusUp = employeeWorkTimeLog.Status;
                        var statusLateUp = employeeWorkTimeLog.StatusLate;
                        var statusEarlyUp = employeeWorkTimeLog.StatusEarly;
                        var logsUp = employeeWorkTimeLog.Logs;
                        //if (statusLate == (int)StatusWork.XacNhanCong)
                        //{
                        //    inUp = employeeWorkTimeLog.In;
                        //    workTimeUp = employeeWorkTimeLog.WorkTime;
                        //    workDayUp = employeeWorkTimeLog.WorkDay;
                        //    lateUp = employeeWorkTimeLog.Late;
                        //    statusLateUp = employeeWorkTimeLog.StatusLate;
                        //    //logsUp.Add();
                        //}
                        //if (statusEarly == (int)StatusWork.XacNhanCong)
                        //{
                        //    outUp = employeeWorkTimeLog.Out;
                        //    workTimeUp = employeeWorkTimeLog.WorkTime;
                        //    workDayUp = employeeWorkTimeLog.WorkDay;
                        //    earlyUp = employeeWorkTimeLog.Early;
                        //    statusEarlyUp = employeeWorkTimeLog.StatusEarly;
                        //}

                        var builderUpdate = Builders<EmployeeWorkTimeLog>.Filter;
                        var filterUpdate = builderUpdate.Eq(m => m.Id, employeeWorkTimeLogDb.Id);
                        var update = Builders<EmployeeWorkTimeLog>.Update
                            .Set(m => m.In, inUp)
                            .Set(m => m.Out, outUp)
                            .Set(m => m.WorkTime, workTimeUp)
                            .Set(m => m.WorkDay, workDayUp)
                            .Set(m => m.Late, lateUp)
                            .Set(m => m.Early, earlyUp)
                            .Set(m => m.Status, statusUp)
                            .Set(m => m.StatusLate, statusLateUp)
                            .Set(m => m.StatusEarly, statusEarlyUp)
                            .Set(m => m.Logs, logsUp)
                            .Set(m => m.UpdatedOn, DateTime.Now);
                        dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
                        UpdateSummaryChangeData(dbContext, dateData, employeeWorkTimeLog, currentWorkTime, currentLate, currentEarly, currentWorkDay);
                    }
                }
                #endregion

                #region Send Mail
                if (modeEmail)
                {
                    var iDateSent = -1;
                    if (DateTime.Now.Date.AddDays(iDateSent).DayOfWeek == DayOfWeek.Sunday)
                    {
                        iDateSent--;
                    }
                    // Holiday
                    if (status == 0 && dateData == DateTime.Now.Date.AddDays(iDateSent) && !string.IsNullOrEmpty(email))
                    {
                        Console.WriteLine("Sending mail...");
                        var tos = new List<EmailAddress>
                            {
                                new EmailAddress { Name = fullName, Address = email }
                                //new EmailAddress { Name = fullName, Address = "xuan.tm@tribat.vn" }
                            };
                        var webRoot = Environment.CurrentDirectory;
                        var pathToFile = @"C:\Projects\App.Schedule\Templates\TimeKeeperNotice.html";
                        var subject = "Xác nhận thời gian làm việc.";
                        var bodyBuilder = new BodyBuilder();
                        using (StreamReader SourceReader = File.OpenText(pathToFile))
                        {
                            bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                        }
                        var logsHtml = string.Empty;
                        if (employeeWorkTimeLog.Logs != null && employeeWorkTimeLog.Logs.Count > 0)
                        {
                            logsHtml += "<br />";
                            logsHtml += "<small style='font-size:10px;'>Máy chấm công ghi nhận:</small>";
                            logsHtml += "<br />";
                            logsHtml += "<table class='MsoNormalTable' border='0 cellspacing='0' cellpadding='0' width='738' style='width: 553.6pt; margin-left: -1.15pt;'>";
                            foreach (var log in employeeWorkTimeLog.Logs)
                            {
                                logsHtml += "<tr style='height: 12.75pt'>";
                                logsHtml += "<td nowrap='nowrap'><small style='font-size:10px;'>" + log.Date.ToString("dd/MM/yyyy HH:mm:ss") + "</small></td>";
                                logsHtml += "</tr>";
                            }
                            logsHtml += "</table>";
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
                            logsHtml,
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

                        //new AuthMessageSender().SendEmail(emailMessage);
                    }
                }
                #endregion
            }
        }

        private static void UpdateSummary(MongoDBContext dbContext, DateTime dateData, EmployeeWorkTimeLog employeeWorkTimeLog)
        {
            int year = dateData.Day > 25 ? dateData.AddMonths(1).Year : dateData.Year;
            int month = dateData.Day > 25 ? dateData.AddMonths(1).Month : dateData.Month;

            var existSum = dbContext.EmployeeWorkTimeMonthLogs
                            .Find(m => m.EmployeeId.Equals(employeeWorkTimeLog.EmployeeId)
                            && m.EnrollNumber.Equals(employeeWorkTimeLog.EnrollNumber)
                            && m.WorkplaceCode.Equals(employeeWorkTimeLog.WorkplaceCode)
                            && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            if (existSum != null)
            {
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
                dbContext.EmployeeWorkTimeMonthLogs.InsertOne(new EmployeeWorkTimeMonthLog
                {
                    EmployeeId = employeeWorkTimeLog.EmployeeId,
                    EnrollNumber = employeeWorkTimeLog.EnrollNumber,
                    WorkplaceCode = employeeWorkTimeLog.WorkplaceCode,
                    Month = month,
                    Year = year,
                    Workday = employeeWorkTimeLog.WorkDay,
                    WorkTime = employeeWorkTimeLog.WorkTime.TotalMilliseconds,
                    Late = employeeWorkTimeLog.Late.TotalMilliseconds,
                    Early = employeeWorkTimeLog.Early.TotalMilliseconds,
                    LastUpdated = dateData
                });
            }
        }

        private static void UpdateSummaryChangeData(MongoDBContext dbContext, DateTime dateData, EmployeeWorkTimeLog employeeWorkTimeLog, double currentWorkTime, double currentLate, double currentEarly, double currentWorkDay)
        {
            int year = dateData.Day > 25 ? dateData.AddMonths(1).Year : dateData.Year;
            int month = dateData.Day > 25 ? dateData.AddMonths(1).Month : dateData.Month;

            var existSum = dbContext.EmployeeWorkTimeMonthLogs
                .Find(m => m.EmployeeId.Equals(employeeWorkTimeLog.EmployeeId)
                && m.EnrollNumber.Equals(employeeWorkTimeLog.EnrollNumber)
                && m.WorkplaceCode.Equals(employeeWorkTimeLog.WorkplaceCode)
                && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
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

        // Update missing Date, Leave date, apply allow late|early,...
        private static void UpdateFinal(MongoDBContext dbContext, string location, bool mode, int day)
        {
            var leaveTypes = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true) && m.Display.Equals(true)).ToList();

            var today = DateTime.Now.Date;
            var endDay = today.Day > 25 ? new DateTime(today.AddMonths(1).Year, today.AddMonths(1).Month, 25) : new DateTime(today.Year, today.Month, 25);

            var filterEmp = Builders<Employee>.Filter.Eq(m => m.Enable, true)
                    & Builders<Employee>.Filter.ElemMatch(z => z.Workplaces, a => a.Code == location);
            var employees = dbContext.Employees.Find(filterEmp).ToList();
            // Debug
            //employees = dbContext.Employees.Find(m => m.Id.Equals("5b6bb231e73a301f941c58dd")).ToList();
            // End debug
            foreach (var employee in employees)
            {
                var employeeLocation = employee.Workplaces.FirstOrDefault(a => a.Code == location);
                if (string.IsNullOrEmpty(employeeLocation.Fingerprint))
                {
                    continue;
                }
                var employeeId = employee.Id;
                var employeeFinger = employeeLocation.Fingerprint;

                var startDate = today.AddDays(day);
                if (mode)
                {
                    startDate = endDay.AddDays(1).AddMonths(-4);
                }
                

                while (startDate <= endDay)
                {
                    // Debug
                    //if (startDate > new DateTime(2018, 10, 25))
                    //{
                    //    var fff = true;
                    //}
                    // End Debug
                    int year = startDate.Day > 25 ? startDate.AddMonths(1).Year : startDate.Year;
                    int month = startDate.Day > 25 ? startDate.AddMonths(1).Month : startDate.Month;
                    var endDateMonth = new DateTime(year, month, 25);
                    var startDateMonth = endDateMonth.AddMonths(-1).AddDays(1);

                    var monthTimeInformation = dbContext.EmployeeWorkTimeMonthLogs
                                .Find(m => m.EmployeeId.Equals(employee.Id)
                                && m.WorkplaceCode.Equals(location)
                                && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
                    if (monthTimeInformation == null)
                    {
                        dbContext.EmployeeWorkTimeMonthLogs.InsertOne(new EmployeeWorkTimeMonthLog
                        {
                            EmployeeId = employeeId,
                            EnrollNumber = employeeFinger,
                            WorkplaceCode = location,
                            Month = month,
                            Year = year,
                            Workday = 0,
                            WorkTime = 0,
                            Late = 0,
                            Early = 0,
                            LastUpdated = today
                        });
                        monthTimeInformation = dbContext.EmployeeWorkTimeMonthLogs
                                .Find(m => m.EmployeeId.Equals(employee.Id)
                                && m.WorkplaceCode.Equals(location)
                                && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
                    }

                    // Apply current month
                    var timekeepings = dbContext.EmployeeWorkTimeLogs.Find(m => m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)).ToList();

                    // Update base month, year later
                    var leaves = dbContext.Leaves.Find(m => m.EmployeeId.Equals(employeeId)).ToList();

                    #region Declare
                    double WorkDay = 0;
                    double WorkTime = 0;
                    double CongCNGio = 0;
                    double CongTangCaNgayThuongGio = 0;
                    double CongLeTet = 0;
                    double Late = 0;
                    double Early = 0;
                    double NghiPhepNam = 0;
                    double NghiViecRieng = 0;
                    double NghiBenh = 0;
                    double NghiKhongPhep = 0;
                    double NghiHuongLuong = 0;
                    double NghiLe = 0;
                    double KhongChamCong = 0;
                    double ChuNhat = 0;
                    #endregion

                    for (DateTime date = startDateMonth; date <= endDateMonth; date = date.AddDays(1))
                    {
                        if (date > today)
                        {
                            continue;
                        }
                        var timekeeping = timekeepings.FirstOrDefault(item => item.Date == date);
                        if (timekeeping != null)
                        {
                            int status = timekeeping.Status;
                            int statusLate = timekeeping.StatusLate;
                            int statusEarly = timekeeping.StatusEarly;
                            if (status == (int)StatusWork.XacNhanCong)
                            {
                                if (statusLate == (int)StatusWork.XacNhanCong && timekeeping.In.HasValue)
                                {
                                    var lateMinute = timekeeping.Late.TotalMinutes;
                                    if (lateMinute > 15)
                                    {
                                        var filterUpdate = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);
                                        var update = Builders<EmployeeWorkTimeLog>.Update
                                            .Set(m => m.Late, new TimeSpan(0, 0, 0))
                                            .Inc(m => m.WorkDay, -0.5);
                                        dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
                                        WorkDay += 0.5;
                                        WorkTime += 0.5 * 8 * 60 * 60 * 60;
                                        Late += timekeeping.Late.TotalMilliseconds;
                                    }
                                }
                                if (statusEarly == (int)StatusWork.XacNhanCong && timekeeping.Out.HasValue)
                                {
                                    var earlyMinute = timekeeping.Early.TotalMinutes;

                                    if (earlyMinute > 15)
                                    {
                                        var filterUpdate = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);
                                        var update = Builders<EmployeeWorkTimeLog>.Update
                                            .Set(m => m.Early, new TimeSpan(0, 0, 0))
                                            .Inc(m => m.WorkDay, -0.5);
                                        dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
                                        WorkDay += 0.5;
                                        WorkTime += 0.5 * 8 * 60 * 60 * 60;
                                        Early += timekeeping.Early.TotalMilliseconds;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var employeeWorkTimeLog = new EmployeeWorkTimeLog
                            {
                                EmployeeId = employee.Id,
                                EmployeeName = employee.FullName,
                                EmployeeTitle = employee.Title,
                                Department = employee.Department,
                                Part = employee.Part,
                                EnrollNumber = employeeLocation.Fingerprint,
                                Year = year,
                                Month = month,
                                Date = date,
                                WorkplaceCode = location
                            };

                            // Nghỉ lễ làm sau....

                            if (date.DayOfWeek == DayOfWeek.Sunday)
                            {
                                employeeWorkTimeLog.Mode = (int)TimeWork.Sunday;
                                employeeWorkTimeLog.Reason = "Chủ nhật";
                                ChuNhat++;
                            }
                            else
                            {
                                var existLeave = leaves.FirstOrDefault(item => item.Enable.Equals(true) && date.Equals(item.From.Date));
                                if (existLeave != null)
                                {
                                    decimal numberLeave = existLeave.Number;
                                    //var workCode = existLeave.TypeName;
                                    // Status do later...
                                    var status = existLeave.Status;
                                    foreach (var leaveType in leaveTypes)
                                    {
                                        if (existLeave.TypeId == leaveType.Id)
                                        {
                                            if (leaveType.Alias == "phep-nam")
                                            {
                                                NghiPhepNam += (double)numberLeave;
                                                employeeWorkTimeLog.Mode = (int)TimeWork.LeavePhep;
                                            }
                                            else if (leaveType.Alias == "phep-khong-huong-luong")
                                            {
                                                NghiViecRieng += (double)numberLeave;
                                                employeeWorkTimeLog.Mode = (int)TimeWork.LeaveKhongHuongLuong;
                                            }
                                            else if (leaveType.Alias == "nghi-huong-luong")
                                            {
                                                NghiHuongLuong += (double)numberLeave;
                                                employeeWorkTimeLog.Mode = (int)TimeWork.LeaveHuongLuong;
                                            }
                                            else if (leaveType.Alias == "nghi-bu")
                                            {
                                                // do later
                                            }
                                        }
                                    }
                                    employeeWorkTimeLog.Reason = existLeave.TypeName;
                                    employeeWorkTimeLog.ReasonDetail = Constants.StatusLeave(existLeave.Status);
                                }
                                else
                                {
                                    if (date != DateTime.Now.Date)
                                    {
                                        employeeWorkTimeLog.Mode = (int)TimeWork.None;
                                        employeeWorkTimeLog.Status = (int)StatusWork.XacNhanCong;
                                        KhongChamCong++;
                                    }
                                    else
                                    {
                                        employeeWorkTimeLog.Mode = (int)TimeWork.Wait;
                                        employeeWorkTimeLog.Status = (int)StatusWork.Wait;
                                        employeeWorkTimeLog.Reason = Constants.WaitData;
                                        employeeWorkTimeLog.ReasonDetail = string.Empty;
                                    }
                                }
                            }

                            try
                            {
                                dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }

                    #region update Summarry
                    var builderSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
                    var filterSum = builderSum.Eq(m => m.Id, monthTimeInformation.Id);
                    var updateSum = Builders<EmployeeWorkTimeMonthLog>.Update
                        .Inc(m => m.Workday, -(WorkDay))
                        .Inc(m => m.WorkTime, -(WorkTime))
                        .Inc(m => m.CongTangCaNgayThuongGio, CongCNGio)
                        .Inc(m => m.CongLeTet, CongLeTet)
                        .Inc(m => m.Late, -(Late))
                        .Inc(m => m.Early, -(Early))
                        .Inc(m => m.NghiHuongLuong, NghiHuongLuong)
                        .Inc(m => m.ChuNhat, ChuNhat)
                        .Inc(m => m.NghiLe, NghiLe)
                        .Inc(m => m.KhongChamCong, KhongChamCong)
                        .Set(m => m.LastUpdated, DateTime.Now);
                    dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterSum, updateSum);
                    #endregion

                    startDate = startDate.AddMonths(1);
                }
            }
        }

        // Mode is true.
        private static void UpdateTimeMonth(MongoDBContext dbContext, string location)
        {
            var times = dbContext.EmployeeWorkTimeLogs.Find(m => m.Enable.Equals(true) && m.WorkplaceCode.Equals(location)).ToList();
            // Test
            //times = dbContext.EmployeeWorkTimeLogs.Find(m => m.Enable.Equals(true) && m.EnrollNumber.Equals("514") && m.WorkplaceCode.Equals(location)).ToList();
            // End Test
            foreach (var time in times)
            {
                if (time.Status == (int)StatusWork.DongY)
                {
                    time.WorkDay = 1;
                    time.WorkTime = new TimeSpan(8, 0, 0);
                    time.Late = new TimeSpan(0, 0, 0);
                    time.Early = new TimeSpan(0, 0, 0);
                }
                var exist = dbContext.EmployeeWorkTimeMonthLogs.CountDocuments(m => m.EmployeeId.Equals(time.EmployeeId)
                                                                                && m.WorkplaceCode.Equals(time.WorkplaceCode)
                                                                                && m.Month.Equals(time.Month) && m.Year.Equals(time.Year));
                if (exist > 0)
                {
                    var builder = Builders<EmployeeWorkTimeMonthLog>.Filter;
                    var filter = builder.Eq(m => m.EmployeeId, time.EmployeeId)
                                & builder.Eq(m => m.WorkplaceCode, time.WorkplaceCode)
                                & builder.Eq(m => m.Month, time.Month)
                                & builder.Eq(m => m.Year, time.Year);
                    var update = Builders<EmployeeWorkTimeMonthLog>.Update
                        .Inc(m => m.Workday, time.WorkDay)
                        .Inc(m => m.WorkTime, time.WorkTime.TotalMilliseconds)
                        .Inc(m => m.Late, time.Late.TotalMilliseconds)
                        .Inc(m => m.Early, time.Early.TotalMilliseconds)
                        .Set(m => m.LastUpdated, DateTime.Now);
                    dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filter, update);
                }
                else
                {
                    dbContext.EmployeeWorkTimeMonthLogs.InsertOne(new EmployeeWorkTimeMonthLog
                    {
                        EmployeeId = time.EmployeeId,
                        EnrollNumber = time.EnrollNumber,
                        WorkplaceCode = time.WorkplaceCode,
                        Month = time.Month,
                        Year = time.Year,
                        Workday = time.WorkDay,
                        WorkTime = time.WorkTime.TotalMilliseconds,
                        Late = time.Late.TotalMilliseconds,
                        Early = time.Early.TotalMilliseconds,
                        LastUpdated = time.Date
                    });
                }
            }
        }

    }
}

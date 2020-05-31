using Common.Enums;
using Common.Utilities;
using Data;
using MimeKit;
using Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Configuration;

namespace xtime
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Connection, Setting & Filter
            var connection = ConfigurationSettings.AppSettings.Get("connection").ToString();
            var database = ConfigurationSettings.AppSettings.Get("database").ToString();
            var location = ConfigurationSettings.AppSettings.Get("location").ToString();
            var modeData = ConfigurationSettings.AppSettings.Get("modeData").ToString() == "1" ? true : false; // true: Get all data | false get by date
            var isMail = ConfigurationSettings.AppSettings.Get("isMail").ToString() == "1" ? true : false;
            var debug = ConfigurationSettings.AppSettings.Get("debug").ToString();
            var fixbug = ConfigurationSettings.AppSettings.Get("fixbug").ToString() == "1" ? true : false;
            var monthConfig = Convert.ToInt32(ConfigurationSettings.AppSettings.Get("month").ToString());
            
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();

            var today = DateTime.Now.Date;
            int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            // Default start - end date of month.
            // config define: start - end date, month is calculator.
            var crawlStart = 1; // start date of month
            var crawlEnd = daysInMonth;
            var timerMonthCalculator = 0; // current base End Day of Month.
            var crawlStartE = dbContext.Settings.Find(m => m.Key.Equals("crawl-day-start")).FirstOrDefault();
            var crawlEndE = dbContext.Settings.Find(m => m.Key.Equals("crawl-day-end")).FirstOrDefault();
            var timerMonthCalculatorE = dbContext.Settings.Find(m => m.Key.Equals("timer-month-calculator")).FirstOrDefault(); // value:0,-1
            if (crawlStartE != null && !string.IsNullOrEmpty(crawlStartE.Value))
            {
                crawlStart = Convert.ToInt32(crawlStartE.Value);
            }
            if (crawlEndE != null && !string.IsNullOrEmpty(crawlStartE.Value))
            {
                crawlEnd = Convert.ToInt32(crawlEndE.Value);
            }
            if (timerMonthCalculatorE != null)
            {
                timerMonthCalculator = Convert.ToInt32(timerMonthCalculatorE.Value);
            }
            var startDate = crawlStart > crawlEnd ? new DateTime(today.Year, today.Month, crawlStart).AddMonths(-1) : new DateTime(today.Year, today.Month, crawlStart);
            var endDate = crawlStart > crawlEnd ? new DateTime(startDate.AddMonths(1).Year, startDate.AddMonths(1).Month, crawlEnd) : new DateTime(startDate.Year, startDate.Month, crawlEnd);

            if (monthConfig < 0)
            {
                startDate = startDate.AddMonths(monthConfig);
            }
            var builder = Builders<AttLog>.Filter;
            var filter = builder.Gte(m => m.Date, startDate);

            if (!string.IsNullOrEmpty(debug))
            {
                filter &= builder.Eq(m => m.EnrollNumber, debug);
            }
            #endregion

            #region Delete data
            if (modeData)
            {
                dbContext.EmployeeWorkTimeLogs.DeleteMany(m => string.IsNullOrEmpty(m.SecureCode) && m.WorkplaceCode.Equals(location));
                // skip CN, Leave, Holiday
                var statusRemove = new List<int>()
                {
                    (int)EStatusWork.XacNhanCong,
                    (int)EStatusWork.DuCong,
                    (int)EStatusWork.Wait
                };
                dbContext.EmployeeWorkTimeLogs.DeleteMany(m => true && statusRemove.Contains(m.Status) && m.WorkplaceCode.Equals(location));

                dbContext.EmployeeWorkTimeMonthLogs.DeleteMany(m => m.WorkplaceCode.Equals(location));
            }
            #endregion

            var attlogs = new List<AttLog>();
            if (location == "NM")
            {
                attlogs = modeData ? dbContext.X928CNMAttLogs.Find(m => true).ToList() : dbContext.X928CNMAttLogs.Find(filter).ToList();
            }
            else
            {
                attlogs = modeData ? dbContext.X628CVPAttLogs.Find(m => true).ToList() : dbContext.X628CVPAttLogs.Find(filter).ToList();
            }

            Proccess(debug, dbContext, location, modeData, isMail, attlogs, timerMonthCalculator, startDate, endDate);
        }

        private static void Proccess(string debug, MongoDBContext dbContext, string location, bool modeData, bool isMail, List<AttLog> attlogs, int timerMonthCalculator, DateTime startDate, DateTime endDate)
        {
            #region Config
            var linkChamCong = Constants.System.domain + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Index;
            var lunch = TimeSpan.FromHours(1);
            var today = DateTime.Now.Date;
            var crawlStart = startDate.Day;
            var crawlEnd = endDate.Day;
            // FIX ASAP
            var hrVP = dbContext.Employees.Find(m => m.Id.Equals("5b6bb22fe73a301f941c5887")).FirstOrDefault();
            var hrNM = dbContext.Employees.Find(m => m.Id.Equals("5d6ddce1d529b01868a6650a")).FirstOrDefault();
            var hrs = new List<Employee>
            {
                hrVP,
                hrNM
            };

            var shifts = Utility.GetShift();

            // Move to content
            var holidays = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Holiday)).ToList();

            var leaveTypes = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.LeaveType)).ToList();
            #endregion

            // TEMP
            var startWorkingScheduleTime = location == "VP" ? new TimeSpan(8, 0, 0) : new TimeSpan(7, 30, 0);
            var endWorkingScheduleTime = location == "VP" ? new TimeSpan(17, 0, 0) : new TimeSpan(16, 30, 0);

            var times = (from p in attlogs
                         group p by new
                         {
                             p.EnrollNumber,
                             p.Date.Date
                         } into d
                         select new
                         {
                             d.Key.Date,
                             groupDate = d.Key.Date.ToString("yyyy-MM-dd"),
                             groupCode = d.Key.EnrollNumber,
                             count = d.Count(),
                             times = d.ToList()
                         }).OrderBy(m => m.Date).ToList();

            var filterEmp = Builders<Employee>.Filter.Eq(m => m.Enable, true)
                    & Builders<Employee>.Filter.ElemMatch(z => z.Workplaces, a => a.Code == location);
            var employees = dbContext.Employees.Find(filterEmp).ToList();
            if (!string.IsNullOrEmpty(debug))
            {
                employees = dbContext.Employees.Find(m => m.Workplaces.Any(w => w.Code.Equals(location) && w.Fingerprint.Equals(debug))).ToList();
            }

            foreach (var employee in employees)
            {
                Console.WriteLine("Employee Name: " + employee.FullName);
                // Except employee no time data
                var employeeLocation = employee.Workplaces.FirstOrDefault(a => a.Code == location);
                var fingerInt = employeeLocation.Fingerprint != null ? Convert.ToInt32(employeeLocation.Fingerprint) : 0;
                if (fingerInt == 0)
                {
                    continue;
                }
                var existTimeNum = times.Count(m => m.groupCode.Equals(fingerInt.ToString()));
                if (existTimeNum == 0)
                {
                    continue;
                }

                #region Employee Information
                var employeeId = employee.Id;
                if (!string.IsNullOrEmpty(employeeLocation.WorkingScheduleTime))
                {
                    startWorkingScheduleTime = TimeSpan.Parse(employeeLocation.WorkingScheduleTime.Split('-')[0].Trim());
                    endWorkingScheduleTime = TimeSpan.Parse(employeeLocation.WorkingScheduleTime.Split('-')[1].Trim());
                }
                var email = employee.Email;
                var fullName = employee.FullName;
                var phone = string.Empty;
                if (employee.Mobiles != null && employee.Mobiles.Count > 0)
                {
                    phone = employee.Mobiles[0].Number;
                }
                var approveE = Utility.GetManager(employee, false, string.Empty, 1).FirstOrDefault();
                var linkFinger = linkChamCong + employee.Id;
                #endregion

                var leaves = dbContext.Leaves.Find(m => m.EmployeeId.Equals(employeeId)).ToList();

                var flagDate = startDate;
                while (flagDate <= endDate)
                {
                    //var endDatePoint = new DateTime(year, month, crawlEnd);
                    //var startDate = crawlStart > crawlEnd ? new DateTime(today.Year, today.Month, crawlStart).AddMonths(-1) : new DateTime(today.Year, today.Month, crawlStart);
                    var startDatePoint = flagDate;
                    var endDatePoint = crawlStart > crawlEnd ? new DateTime(startDatePoint.AddMonths(1).Year, startDatePoint.AddMonths(1).Month, crawlEnd) : new DateTime(startDatePoint.Year, startDatePoint.Month, crawlEnd);
                    int year = endDatePoint.AddMonths(timerMonthCalculator).Year; // base endDatePoint
                    int month = endDatePoint.AddMonths(timerMonthCalculator).Month; // base endDatePoint

                    var timekeepings = times.Where(m => m.groupCode.Equals(fingerInt.ToString())
                                    && m.Date >= startDatePoint && m.Date <= endDatePoint)
                                    .OrderBy(m => m.Date).ToList();
                    for (DateTime date = startDatePoint; date <= endDatePoint; date = date.AddDays(1))
                    {
                        Console.WriteLine($"Date: {date}");
                        if (date > today) continue;

                        bool isUpdateStatus = true;
                        var employeeWorkTimeLog = dbContext.EmployeeWorkTimeLogs.Find(m => m.EmployeeId.Equals(employeeId)
                                    && m.Date.Equals(date)).FirstOrDefault();
                        if (employeeWorkTimeLog == null)
                        {
                            employeeWorkTimeLog = new EmployeeWorkTimeLog
                            {
                                EmployeeId = employee.Id,
                                EmployeeName = employee.FullName,
                                Department = employee.PhongBanName,
                                DepartmentId = employee.PhongBan,
                                DepartmentAlias = Utility.AliasConvert(employee.PhongBanName),
                                Part = employee.BoPhanName,
                                PartId = employee.BoPhan,
                                PartAlias = Utility.AliasConvert(employee.BoPhanName),
                                EmployeeTitle = employee.ChucVuName,
                                EmployeeTitleId = employee.ChucVu,
                                EmployeeTitleAlias = Utility.AliasConvert(employee.ChucVuName),
                                EnrollNumber = fingerInt.ToString(),
                                WorkplaceCode = location,
                                Year = year,
                                Month = month,
                                Date = date,
                                Workcode = employee.SalaryType,
                                Start = startWorkingScheduleTime,
                                End = endWorkingScheduleTime,
                                Mode = (int)ETimeWork.Normal,
                                Status = (int)EStatusWork.XacNhanCong
                            };
                        }
                        else
                        {
                            if (employeeWorkTimeLog.Status >= (int)EStatusWork.DaGuiXacNhan)
                            {
                                isUpdateStatus = false;
                            }
                        }

                        if (isUpdateStatus)
                        {
                            #region HOLIDAY & LEAVES
                            var holiday = holidays.Where(m => m.Value.Equals(date.ToString())).FirstOrDefault();
                            var existLeave = leaves.FirstOrDefault(item => item.Enable.Equals(true)
                                                    && date >= item.From.Date
                                                    && date <= item.To.Date);

                            if (date.DayOfWeek == DayOfWeek.Sunday)
                            {
                                employeeWorkTimeLog.Mode = (int)ETimeWork.Sunday;
                                employeeWorkTimeLog.Reason = "Chủ nhật";
                            }

                            if (holiday != null)
                            {
                                employeeWorkTimeLog.Mode = (int)ETimeWork.Holiday;
                                employeeWorkTimeLog.Reason = holiday.Name;
                                employeeWorkTimeLog.ReasonDetail = holiday.Description;
                            }

                            if (existLeave != null)
                            {
                                double numberLeave = existLeave.Number;

                                var leaveType = leaveTypes.Where(m => m.Id.Equals(existLeave.TypeId)).FirstOrDefault();
                                if (leaveType != null)
                                {
                                    if (leaveType.Alias == "phep-nam")
                                    {
                                        employeeWorkTimeLog.Mode = (int)ETimeWork.LeavePhep;
                                    }
                                    else if (leaveType.Alias == "phep-khong-huong-luong")
                                    {
                                        employeeWorkTimeLog.Mode = (int)ETimeWork.LeaveKhongHuongLuong;
                                    }
                                    else if (leaveType.Alias == "nghi-huong-luong")
                                    {
                                        employeeWorkTimeLog.Mode = (int)ETimeWork.LeaveHuongLuong;
                                    }
                                }

                                employeeWorkTimeLog.SoNgayNghi = 1;
                                // Check off 0.5
                                var leaveFrom = existLeave.From.Date.Add(existLeave.Start);
                                var leaveTo = existLeave.To.Date.Add(existLeave.End);
                                for (DateTime dateL = leaveFrom; dateL <= leaveTo; dateL = dateL.Date.AddDays(1))
                                {
                                    if (date == dateL.Date)
                                    {
                                        var leaveTimeSpan = TimeSpan.FromHours(8);

                                        leaveTimeSpan = endWorkingScheduleTime - dateL.TimeOfDay;
                                        // check 0.5
                                        if (leaveTimeSpan.TotalHours > 5 && leaveTo.Date == dateL.Date)
                                        {
                                            leaveTimeSpan = leaveTo.TimeOfDay - startWorkingScheduleTime;
                                        }

                                        if (leaveTimeSpan.TotalHours <= 5)
                                        {
                                            employeeWorkTimeLog.SoNgayNghi = 0.5;
                                        }
                                    }
                                }

                                employeeWorkTimeLog.Reason = existLeave.TypeName;
                                employeeWorkTimeLog.ReasonDetail = Constants.StatusLeave(existLeave.Status);
                            }
                            #endregion

                            double workDay = 1;
                            var workTime = new TimeSpan(0);
                            var overTime = new TimeSpan(0);
                            var late = new TimeSpan(0);
                            var early = new TimeSpan(0);
                            var timekeeping = timekeepings.FirstOrDefault(item => item.Date == date);
                            if (timekeeping != null)
                            {
                                var records = timekeeping.times.OrderBy(m => m.Date).ToList();
                                #region Procees Times
                                var inLogTime = records.First().TimeOnlyRecord;
                                var outLogTime = records.Last().TimeOnlyRecord;
                                // ANALYTICS SHIFTS
                                // 07:00 - 16:00
                                // 07:30 - 16:30
                                // 08:00 - 17:00
                                // 19:00 - 03:00

                                var workingArr = new int[] { employeeWorkTimeLog.Start.Hours, employeeWorkTimeLog.End.Hours };
                                var incheck = workingArr.ClosestTo(inLogTime.Hours);
                                var outcheck = workingArr.ClosestTo(outLogTime.Hours);

                                if (incheck == outcheck)
                                {
                                    if (incheck > employeeWorkTimeLog.Start.Add(new TimeSpan(4, 0, 0)).Hours)
                                    {
                                        workTime = outLogTime - employeeWorkTimeLog.End.Add(new TimeSpan(4, 0, 0));
                                    }
                                    else
                                    {
                                        workTime = employeeWorkTimeLog.Start.Add(new TimeSpan(4, 0, 0)) - inLogTime;
                                    }

                                    if (employeeWorkTimeLog.Mode != (int)ETimeWork.Normal)
                                    {
                                        overTime = workTime;
                                    }
                                    else
                                    {
                                        workDay = 0.5;
                                        employeeWorkTimeLog.Status = (int)EStatusWork.XacNhanCong;
                                        if (incheck == employeeWorkTimeLog.Start.Hours)
                                        {
                                            employeeWorkTimeLog.In = inLogTime;
                                            employeeWorkTimeLog.StatusEarly = (int)EStatusWork.XacNhanCong;
                                            if (inLogTime > employeeWorkTimeLog.Start)
                                            {
                                                late = inLogTime - employeeWorkTimeLog.Start;
                                                if (late.TotalMinutes > 0)
                                                {
                                                    employeeWorkTimeLog.StatusLate = (int)EStatusWork.XacNhanCong;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            employeeWorkTimeLog.Out = outLogTime;
                                            employeeWorkTimeLog.StatusLate = (int)EStatusWork.XacNhanCong;
                                            if (outLogTime < employeeWorkTimeLog.End)
                                            {
                                                early = employeeWorkTimeLog.End - outLogTime;
                                                if (early.TotalMinutes > 0)
                                                {
                                                    employeeWorkTimeLog.StatusEarly = (int)EStatusWork.XacNhanCong;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    workDay = 1;
                                    employeeWorkTimeLog.Status = (int)EStatusWork.DuCong;
                                    employeeWorkTimeLog.In = inLogTime;
                                    employeeWorkTimeLog.Out = outLogTime;
                                    workTime = (outLogTime - inLogTime) - lunch;
                                    if (workTime.TotalHours > 8)
                                    {
                                        overTime = workTime - new TimeSpan(8, 0, 0);
                                    }
                                    if (employeeWorkTimeLog.Mode != (int)ETimeWork.Normal)
                                    {
                                        if (employeeWorkTimeLog.Mode == (int)ETimeWork.LeavePhep
                                            || employeeWorkTimeLog.Mode == (int)ETimeWork.LeaveHuongLuong
                                            || employeeWorkTimeLog.Mode == (int)ETimeWork.LeaveKhongHuongLuong)
                                        {
                                            var hourOff = new TimeSpan(Convert.ToInt32(employeeWorkTimeLog.SoNgayNghi * 8), 0, 0);
                                            overTime = workTime - hourOff;
                                        }
                                        else
                                        {
                                            overTime = workTime;
                                        }
                                    }

                                    if (inLogTime > employeeWorkTimeLog.Start)
                                    {
                                        employeeWorkTimeLog.StatusLate = (int)EStatusWork.XacNhanCong;
                                        employeeWorkTimeLog.Status = (int)EStatusWork.XacNhanCong;
                                        late = inLogTime - employeeWorkTimeLog.Start;
                                    }
                                    if (outLogTime < employeeWorkTimeLog.End)
                                    {
                                        employeeWorkTimeLog.StatusEarly = (int)EStatusWork.XacNhanCong;
                                        employeeWorkTimeLog.Status = (int)EStatusWork.XacNhanCong;
                                        early = employeeWorkTimeLog.End - outLogTime;
                                    }
                                }

                                // Analytics later...
                                //if (employeeWorkTimeLog.SoNgayNghi > 0)
                                //{
                                //    employeeWorkTimeLog.Status = (int)EStatusWork.DuCong;
                                //}

                                if (overTime.TotalHours >= 1)
                                {
                                    employeeWorkTimeLog.StatusTangCa = (int)ETangCa.GuiXacNhan;
                                }
                                employeeWorkTimeLog.VerifyMode = records[0].VerifyMode;
                                employeeWorkTimeLog.InOutMode = records[0].InOutMode;
                                
                                employeeWorkTimeLog.WorkplaceCode = location;

                                employeeWorkTimeLog.WorkTime = workTime;
                                employeeWorkTimeLog.TangCaThucTe = overTime;
                                employeeWorkTimeLog.OtThucTeD = overTime.TotalHours;
                                employeeWorkTimeLog.WorkDay = workDay;
                                employeeWorkTimeLog.Late = late;
                                employeeWorkTimeLog.Early = early;
                                employeeWorkTimeLog.Logs = records;

                                // OVERRIDE NO NORMAL WORK DAY
                                if (employeeWorkTimeLog.Mode != (int)ETimeWork.Normal)
                                {
                                    employeeWorkTimeLog.Status = (int)EStatusWork.DuCong;
                                    employeeWorkTimeLog.StatusEarly = (int)EStatusWork.DuCong;
                                    employeeWorkTimeLog.StatusLate = (int)EStatusWork.DuCong;
                                    employeeWorkTimeLog.StatusTangCa = (int)ETangCa.GuiXacNhan;
                                }
                                #endregion
                            }

                            #region DB
                            if (string.IsNullOrEmpty(employeeWorkTimeLog.Id))
                            {
                                dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                            }
                            else
                            {
                                // Truong hop bam 2 noi: sang 1 noi, chieu 1 noi...
                                // Lưu mỗi nơi theo mã chấm công
                                // Tính công cộng 2 nơi lại. Check không vượt quá 1 ngày.
                                if (employeeWorkTimeLog.Logs != null && employeeWorkTimeLog.Logs.Count > 0)
                                {
                                    var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, employeeWorkTimeLog.Id);
                                    var update = Builders<EmployeeWorkTimeLog>.Update
                                        .Set(m => m.EnrollNumber, employeeWorkTimeLog.EnrollNumber)
                                        .Set(m => m.WorkplaceCode, employeeWorkTimeLog.WorkplaceCode)
                                        .Set(m => m.In, employeeWorkTimeLog.In)
                                        .Set(m => m.Out, employeeWorkTimeLog.Out)
                                        .Set(m => m.WorkTime, employeeWorkTimeLog.WorkTime)
                                        .Set(m => m.Workcode, employeeWorkTimeLog.Workcode)
                                        .Set(m => m.WorkDay, employeeWorkTimeLog.WorkDay)
                                        .Set(m => m.Late, employeeWorkTimeLog.Late)
                                        .Set(m => m.Early, employeeWorkTimeLog.Early)
                                        .Set(m => m.Status, employeeWorkTimeLog.Status)
                                        .Set(m => m.StatusLate, employeeWorkTimeLog.StatusLate)
                                        .Set(m => m.StatusEarly, employeeWorkTimeLog.StatusEarly)
                                        .Set(m => m.Logs, employeeWorkTimeLog.Logs)
                                        .Set(m => m.Mode, employeeWorkTimeLog.Mode)
                                        .Set(m => m.SoNgayNghi, employeeWorkTimeLog.SoNgayNghi)
                                        .Set(m => m.StatusTangCa, employeeWorkTimeLog.StatusTangCa)
                                        .Set(m => m.TangCaThucTe, employeeWorkTimeLog.TangCaThucTe)
                                        .Set(m => m.OtThucTeD, employeeWorkTimeLog.OtThucTeD)
                                        .Set(m => m.Reason, employeeWorkTimeLog.Reason)
                                        .Set(m => m.UpdatedOn, DateTime.Now);

                                    dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
                                }
                                else
                                {
                                    var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, employeeWorkTimeLog.Id);
                                    var update = Builders<EmployeeWorkTimeLog>.Update
                                        .Set(m => m.Mode, employeeWorkTimeLog.Mode)
                                        .Set(m => m.SoNgayNghi, employeeWorkTimeLog.SoNgayNghi)
                                        .Set(m => m.Reason, employeeWorkTimeLog.Reason)
                                        .Set(m => m.UpdatedOn, DateTime.Now);
                                    dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
                                }
                            }
                            #endregion

                            #region Send Mail
                            if (isMail && !employeeWorkTimeLog.IsSendMail && !employee.Leave)
                            {
                                var iDateSent = -1;
                                if (DateTime.Now.Date.AddDays(iDateSent).DayOfWeek == DayOfWeek.Sunday)
                                {
                                    iDateSent--;
                                }
                                if (employeeWorkTimeLog.Status == (int)EStatusWork.XacNhanCong && date == today.AddDays(iDateSent))
                                {
                                    Console.WriteLine("Sending mail...");
                                    if (!string.IsNullOrEmpty(email) && Utility.IsValidEmail(email))
                                    {
                                        var tos = new List<EmailAddress>
                                        {
                                            new EmailAddress { Name = fullName, Address = email }
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
                                        var url = Constants.System.domain;
                                        var forgot = url + Constants.System.login;
                                        var analytic = Utility.TimerAnalytics(employeeWorkTimeLog, true);
                                        string messageBody = string.Format(bodyBuilder.HtmlBody,
                                            subject,
                                            fullName,
                                            employeeWorkTimeLog.EnrollNumber,
                                            employeeWorkTimeLog.WorkplaceCode,
                                            employeeWorkTimeLog.Start + "-" + employeeWorkTimeLog.End,
                                            employeeWorkTimeLog.Date.ToString("dd/MM/yyyy"),
                                            employeeWorkTimeLog.In,
                                            employeeWorkTimeLog.Out,
                                            analytic.Late == 0 ? string.Empty : analytic.Late.ToString(),
                                            analytic.Early == 0 ? string.Empty : analytic.Early.ToString(),
                                            employeeWorkTimeLog.WorkTime,
                                            Math.Round(analytic.Workday, 2),
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
                                            BodyContent = messageBody,
                                            EmployeeId = employeeWorkTimeLog.EmployeeId
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
                                    else
                                    {
                                        var approveName = string.Empty;
                                        var tos = new List<EmailAddress>();
                                        // KO CO EMAIL
                                        // SEND QUAN LY TRUC TIEP, XAC NHAN => OK
                                        if (approveE != null && !string.IsNullOrEmpty(approveE.Email))
                                        {
                                            approveName = approveE.FullName;
                                            tos.Add(new EmailAddress { Name = approveE.FullName, Address = approveE.Email });
                                        }
                                        else
                                        {
                                            if (location == "VP")
                                            {
                                                approveName = hrVP.FullName;
                                                tos.Add(new EmailAddress { Name = hrVP.FullName, Address = hrVP.Email });
                                            }
                                            else
                                            {
                                                foreach (var hr in hrs)
                                                {
                                                    approveName += string.IsNullOrEmpty(approveName) ? hr.FullName : ", " + hr.FullName;
                                                    tos.Add(new EmailAddress { Name = hr.FullName, Address = hr.Email });
                                                }
                                            }
                                        }

                                        var webRoot = Environment.CurrentDirectory;
                                        var pathToFile = @"C:\Projects\App.Schedule\Templates\TimeKeeperRequest.html";
                                        var subject = "Hỗ trợ xác nhận công.";
                                        var requester = employee.FullName;
                                        var inTime = employeeWorkTimeLog.In.HasValue ? employeeWorkTimeLog.In.Value.ToString(@"hh\:mm") : string.Empty;
                                        var outTime = employeeWorkTimeLog.Out.HasValue ? employeeWorkTimeLog.Out.Value.ToString(@"hh\:mm") : string.Empty;
                                        var lateTime = employeeWorkTimeLog.Late.TotalMilliseconds > 0 ? Math.Round(employeeWorkTimeLog.Late.TotalMinutes, 0).ToString() : "0";
                                        var earlyTime = employeeWorkTimeLog.Early.TotalMilliseconds > 0 ? Math.Round(employeeWorkTimeLog.Early.TotalMinutes, 0).ToString() : "0";
                                        var sumTime = string.Empty;
                                        if (string.IsNullOrEmpty(inTime) && string.IsNullOrEmpty(outTime))
                                        {
                                            sumTime = "1 ngày";
                                        }
                                        else if (string.IsNullOrEmpty(inTime) || string.IsNullOrEmpty(outTime))
                                        {
                                            sumTime = "0.5 ngày";
                                        }
                                        var minutesMissing = TimeSpan.FromMilliseconds(employeeWorkTimeLog.Late.TotalMilliseconds + employeeWorkTimeLog.Early.TotalMilliseconds).TotalMinutes;
                                        if (minutesMissing > 0)
                                        {
                                            if (!string.IsNullOrEmpty(sumTime))
                                            {
                                                sumTime += ", ";
                                            }
                                            sumTime += Math.Round(minutesMissing, 0) + " phút";
                                        }

                                        var detailTimeKeeping = "Ngày: " + employeeWorkTimeLog.Date.ToString("dd/MM/yyyy") + "; thiếu: " + sumTime;
                                        if (!string.IsNullOrEmpty(inTime))
                                        {
                                            detailTimeKeeping += " | giờ vào: " + inTime + "; trễ: " + lateTime;
                                        }
                                        else
                                        {
                                            detailTimeKeeping += " | giờ vào: --; trễ: --";
                                        }
                                        if (!string.IsNullOrEmpty(outTime))
                                        {
                                            detailTimeKeeping += "; giờ ra: " + outTime + "; sớm: " + earlyTime;
                                        }
                                        else
                                        {
                                            detailTimeKeeping += "; giờ ra: --; sớm: --";
                                        }
                                        // Api update, generate code.
                                        var linkapprove = Constants.System.domain + "/xacnhan/cong";
                                        var linkAccept = linkapprove + "?id=" + employeeWorkTimeLog.Id + "&approve=3&secure=" + employeeWorkTimeLog.SecureCode;
                                        var linkCancel = linkapprove + "?id=" + employeeWorkTimeLog.Id + "&approve=4&secure=" + employeeWorkTimeLog.SecureCode;
                                        var linkDetail = Constants.System.domain;
                                        var bodyBuilder = new BodyBuilder();
                                        using (StreamReader SourceReader = File.OpenText(pathToFile))
                                        {
                                            bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                                        }
                                        string messageBody = string.Format(bodyBuilder.HtmlBody,
                                            subject,
                                            approveName,
                                            requester,
                                            requester,
                                            "chưa khởi tạo email",
                                            employee.ChucVuName,
                                            detailTimeKeeping,
                                            string.Empty,
                                            string.Empty,
                                            phone,
                                            linkAccept,
                                            linkCancel,
                                            linkDetail,
                                            Constants.System.domain
                                            );

                                        var emailMessage = new EmailMessage()
                                        {
                                            ToAddresses = tos,
                                            Subject = subject,
                                            BodyContent = messageBody,
                                            Type = "ho-tro-xac-nhan-cong",
                                            EmployeeId = employee.Id
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

                                    // update db
                                    var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, employeeWorkTimeLog.Id);
                                    var update = Builders<EmployeeWorkTimeLog>.Update
                                        .Set(m => m.IsSendMail, true)
                                        .Set(m => m.UpdatedOn, DateTime.Now);
                                    dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
                                }
                            }
                            #endregion
                        }
                    }

                    Summary(dbContext, employee, location, month, year);

                    flagDate = flagDate.AddMonths(1);
                }
            }
        }

        // Truong hop 2 ma cc 2 noi => Do later
        private static void Summary(MongoDBContext dbContext, Employee employee, string location, int month, int year)
        {
            var now = DateTime.Now;
            var workplace = employee.Workplaces.FirstOrDefault(a => a.Code == location);

            #region INIT
            var checkExist = dbContext.EmployeeWorkTimeMonthLogs
                        .Find(m => m.EmployeeId.Equals(employee.Id)
                        && m.EnrollNumber.Equals(workplace.Fingerprint)
                        && m.WorkplaceCode.Equals(workplace.Code)
                        && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            if (checkExist == null)
            {
                dbContext.EmployeeWorkTimeMonthLogs.InsertOne(new EmployeeWorkTimeMonthLog
                {
                    EmployeeId = employee.Id,
                    EmployeeName = employee.FullName,
                    Department = employee.PhongBanName,
                    DepartmentId = employee.PhongBan,
                    DepartmentAlias = Utility.AliasConvert(employee.PhongBanName),
                    Part = employee.BoPhanName,
                    PartId = employee.BoPhan,
                    PartAlias = Utility.AliasConvert(employee.BoPhanName),
                    Title = employee.ChucVuName,
                    TitleId = employee.ChucVu,
                    TitleAlias = Utility.AliasConvert(employee.ChucVuName),
                    EnrollNumber = workplace.Fingerprint,
                    WorkplaceCode = workplace.Code,
                    Month = month,
                    Year = year
                });
            }
            #endregion

            var current = dbContext.EmployeeWorkTimeMonthLogs
                        .Find(m => m.EmployeeId.Equals(employee.Id)
                        && m.EnrollNumber.Equals(workplace.Fingerprint)
                        && m.WorkplaceCode.Equals(workplace.Code)
                        && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();

            // foreach timework
            var times = dbContext.EmployeeWorkTimeLogs.Find(m => m.Enable.Equals(true)
            && m.EmployeeId.Equals(employee.Id) && m.WorkplaceCode.Equals(location)
            && m.Month.Equals(month) && m.Year.Equals(year)).SortBy(m => m.Date).ToList();

            #region Declare
            double Workday = 0;
            double WorkTime = 0; // store miliseconds
            double Late = 0;
            double Early = 0;
            double CongTangCaNgayThuongGio = 0;
            double KhongChamCong = 0;
            double ChuNhat = 0;

            double CongCNGio = 0;
            double CongLeTet = 0;

            double NghiPhepNam = 0;
            double NghiViecRieng = 0;
            double NghiBenh = 0;
            double NghiKhongPhep = 0;
            double NghiHuongLuong = 0;
            double NghiLe = 0;

            #endregion

            foreach (var time in times)
            {
                var analytic = Utility.TimerAnalytics(time, true);
                Workday += analytic.Workday;
                CongTangCaNgayThuongGio += analytic.TangCaNgayThuong;
                Late += analytic.Late;
                Early += analytic.Early;
                NghiPhepNam += analytic.NgayNghiP;
                NghiLe += analytic.LeTet;
            }

            var builder = Builders<EmployeeWorkTimeMonthLog>.Filter;
            var filter = builder.Eq(m => m.Id, current.Id);
            var update = Builders<EmployeeWorkTimeMonthLog>.Update
                .Set(m => m.Workday, Workday)
                .Set(m => m.WorkTime, WorkTime)
                .Set(m => m.CongCNGio, CongCNGio)
                .Set(m => m.CongTangCaNgayThuongGio, CongTangCaNgayThuongGio)
                .Set(m => m.CongLeTet, CongLeTet)
                .Set(m => m.Late, Late)
                .Set(m => m.Early, Early)
                .Set(m => m.NghiPhepNam, NghiPhepNam)
                .Set(m => m.NghiViecRieng, NghiViecRieng)
                .Set(m => m.NghiBenh, NghiBenh)
                .Set(m => m.NghiKhongPhep, NghiKhongPhep)
                .Set(m => m.NghiHuongLuong, NghiHuongLuong)
                .Set(m => m.NghiLe, NghiLe)
                .Set(m => m.KhongChamCong, KhongChamCong)
                .Set(m => m.ChuNhat, ChuNhat)
                .Set(m => m.LastUpdated, now);
            dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filter, update);
        }
    }
}

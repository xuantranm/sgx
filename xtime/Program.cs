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
using System.Configuration;

namespace xtime
{
    class Program
    {
        static void Main(string[] args)
        {
            #region setting
            var location = ConfigurationSettings.AppSettings.Get("location").ToString();
            var modeData = ConfigurationSettings.AppSettings.Get("modeData").ToString() == "1" ? true : false; // true: Get all data | false get by date
            var day = Convert.ToInt32(ConfigurationSettings.AppSettings.Get("day").ToString());
            var isMail = ConfigurationSettings.AppSettings.Get("isMail").ToString() == "1" ? true : false;
            var debug = ConfigurationSettings.AppSettings.Get("debug").ToString() == "1" ? true : false;
            var connection = "mongodb://localhost:27017";
            var database = "tribat";
            #endregion

            UpdateTimeKeeper(location, modeData, day, isMail, connection, database, debug);
        }

        static void UpdateTimeKeeper(string location, bool modeData, int day, bool isMail, string connection, string database, bool debug)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();

            var dateCrawled = DateTime.Now.AddDays(day);
            var builder = Builders<AttLog>.Filter;
            var filter = builder.Gt(m => m.Date, dateCrawled.AddDays(-1));

            if (debug)
            {
                filter = filter & builder.Eq(m => m.EnrollNumber, ConfigurationSettings.AppSettings.Get("debugString").ToString());
            }
            #endregion

            if (modeData)
            {
                dbContext.EmployeeWorkTimeMonthLogs.DeleteMany(m => m.WorkplaceCode.Equals(location));
                // remove CN, leave
                dbContext.EmployeeWorkTimeLogs.DeleteMany(m => string.IsNullOrEmpty(m.SecureCode) && m.WorkplaceCode.Equals(location));
                var statusRemove = new List<int>()
                {
                    (int)EStatusWork.XacNhanCong,
                    (int)EStatusWork.DuCong,
                    (int)EStatusWork.Wait
                };
                dbContext.EmployeeWorkTimeLogs.DeleteMany(m => true && statusRemove.Contains(m.Status) && m.WorkplaceCode.Equals(location));

                // Run here for small data
                // Use update statusNoRemove above
                UpdateTimeMonth(dbContext, location);
            }

            var attlogs = new List<AttLog>();
            if (location == "NM")
            {
                attlogs = modeData ? dbContext.X928CNMAttLogs.Find(m => true).ToList() : dbContext.X928CNMAttLogs.Find(filter).ToList();
            }
            else
            {
                attlogs = modeData ? dbContext.X628CVPAttLogs.Find(m => true).ToList() : dbContext.X628CVPAttLogs.Find(filter).ToList();
            }

            if (attlogs != null && attlogs.Count > 0)
            {
                // Xu ly ngày, qui định chấm công
                Proccess(dbContext, location, modeData, day, isMail, attlogs, debug);
            }

            // Tính ngày nghỉ,...
            //UpdateFinal(dbContext, location, modeData, day, debug);
        }

        private static void Proccess(MongoDBContext dbContext, string location, bool modeData, int day, bool isMail, List<AttLog> attlogs, bool debug)
        {
            #region Config
            var linkChamCong = Constants.System.domain + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Index;
            var dateNewPolicy = new DateTime(2018, 10, 01);
            var lunch = TimeSpan.FromHours(1);
            var now = DateTime.Now.Date;
            var startWorkingScheduleTime = new TimeSpan(7, 30, 0);
            var endWorkingScheduleTime = new TimeSpan(16, 30, 0);
            #endregion

            var holidays = dbContext.Holidays.Find(m => m.Enable.Equals(true)).ToEnumerable().Where(m => m.Year.Equals(DateTime.Now.Year)).ToList();

            var leaveTypes = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true) && m.Display.Equals(true)).ToList();

            var times = (from p in attlogs
                         group p by new
                         {
                             p.EnrollNumber,
                             p.Date.Date
                         }
                              into d
                         select new
                         {
                             d.Key.Date,
                             groupDate = d.Key.Date.ToString("yyyy-MM-dd"),
                             groupCode = d.Key.EnrollNumber,
                             count = d.Count(),
                             times = d.ToList()
                         }).OrderBy(m => m.Date).ToList();

            var today = DateTime.Now.Date;
            var endDay = today.Day > 25 ? new DateTime(today.AddMonths(1).Year, today.AddMonths(1).Month, 25) : new DateTime(today.Year, today.Month, 25);

            var filterEmp = Builders<Employee>.Filter.Eq(m => m.Enable, true)
                    & Builders<Employee>.Filter.ElemMatch(z => z.Workplaces, a => a.Code == location);
            var employees = dbContext.Employees.Find(filterEmp).ToList();
            if (debug)
            {
                var debugString = ConfigurationSettings.AppSettings.Get("debugString").ToString();
                employees = dbContext.Employees.Find(m => m.Workplaces.Any(w => w.Code.Equals(location) && w.Fingerprint.Equals(debugString))).ToList();
            }

            foreach (var employee in employees)
            {
                Console.WriteLine("Employee Name: " + employee.FullName);
                var employeeLocation = employee.Workplaces.FirstOrDefault(a => a.Code == location);
                if (string.IsNullOrEmpty(employeeLocation.Fingerprint))
                {
                    continue;
                }
                var employeeId = employee.Id;
                var employeeFinger = employeeLocation.Fingerprint;
                var fingerInt = employeeFinger != null ? Convert.ToInt32(employeeFinger) : 0;
                if (!string.IsNullOrEmpty(employeeLocation.WorkingScheduleTime))
                {
                    startWorkingScheduleTime = TimeSpan.Parse(employeeLocation.WorkingScheduleTime.Split('-')[0].Trim());
                    endWorkingScheduleTime = TimeSpan.Parse(employeeLocation.WorkingScheduleTime.Split('-')[1].Trim());
                }
                var email = employee.Email;
                var fullName = employee.FullName;
                var part = employee.Part.ToUpper();
                var department = employee.Department.ToUpper();
                var title = employee.Title.ToUpper();
                var linkFinger = linkChamCong + employee.Id;

                var leaves = dbContext.Leaves.Find(m => m.EmployeeId.Equals(employeeId)).ToList();

                var startDate = today.AddDays(day);
                if (modeData)
                {
                    startDate = today.AddMonths(-4);
                }
                var conditionDate = startDate;

                Console.WriteLine("End date +: " + endDay);
                #region PROCESS
                while (startDate <= endDay)
                {
                    Console.WriteLine("Start date: " + startDate);
                    int year = startDate.Day > 25 ? startDate.AddMonths(1).Year : startDate.Year;
                    int month = startDate.Day > 25 ? startDate.AddMonths(1).Month : startDate.Month;
                    var endDateMonth = new DateTime(year, month, 25);
                    var startDateMonth = endDateMonth.AddMonths(-1).AddDays(1);

                    // Phan tich truong hop ca dem. Vd: ca 1: 6h-14h ; 14h-22h ; 22h-6h
                    // Later
                    var timekeepings = times.Where(m => m.groupCode.Equals(fingerInt.ToString()) && m.Date >= startDateMonth && m.Date <= endDateMonth).OrderBy(m => m.Date).ToList();
                    for (DateTime date = startDateMonth; date <= endDateMonth; date = date.AddDays(1))
                    {
                        Console.WriteLine("Date: " + date);
                        if (!modeData)
                        {
                            if (date < conditionDate)
                            {
                                continue;
                            }
                        }
                        if (date > today)
                        {
                            continue;
                        }

                        var employeeWorkTimeLog = new EmployeeWorkTimeLog
                        {
                            EmployeeId = employee.Id,
                            EmployeeName = employee.FullName,
                            EmployeeTitle = employee.Title.ToUpper(),
                            Department = employee.Department.ToUpper(),
                            DepartmentId = employee.DepartmentId,
                            DepartmentAlias = employee.DepartmentAlias,
                            Part = employee.Part.ToUpper(),
                            EnrollNumber = employeeFinger,
                            Year = year,
                            Month = month,
                            Date = date,
                            WorkplaceCode = location,
                            Workcode = employee.SalaryType,
                            Start = startWorkingScheduleTime,
                            End = endWorkingScheduleTime
                        };

                        // Check in holiday & leave
                        var holiday = holidays.Where(m => m.Date.Equals(date)).FirstOrDefault();

                        var existLeave = leaves.FirstOrDefault(item => item.Enable.Equals(true) 
                                                && date >= item.From.Date 
                                                && date <= item.To.Date);

                        if (date.DayOfWeek == DayOfWeek.Sunday)
                        {
                            employeeWorkTimeLog.Mode = (int)ETimeWork.Sunday;
                            employeeWorkTimeLog.Reason = "Chủ nhật";
                            // ChuNhat++;
                        }
                        if (holiday != null)
                        {
                            employeeWorkTimeLog.Mode = (int)ETimeWork.Holiday;
                            employeeWorkTimeLog.Reason = holiday.Name;
                            employeeWorkTimeLog.ReasonDetail = holiday.Detail;
                            //NghiLe++;
                        }

                        if (existLeave != null)
                        {
                            decimal numberLeave = existLeave.Number;

                            var leaveType = leaveTypes.Where(m => m.Id.Equals(existLeave.TypeId)).FirstOrDefault();
                            if (leaveType != null)
                            {
                                if (leaveType.Alias == "phep-nam")
                                {
                                    //NghiPhepNam += (double)numberLeave;
                                    employeeWorkTimeLog.Mode = (int)ETimeWork.LeavePhep;
                                }
                                else if (leaveType.Alias == "phep-khong-huong-luong")
                                {
                                    //NghiViecRieng += (double)numberLeave;
                                    employeeWorkTimeLog.Mode = (int)ETimeWork.LeaveKhongHuongLuong;
                                }
                                else if (leaveType.Alias == "nghi-huong-luong")
                                {
                                    //NghiHuongLuong += (double)numberLeave;
                                    employeeWorkTimeLog.Mode = (int)ETimeWork.LeaveHuongLuong;
                                }
                                else if (leaveType.Alias == "nghi-bu")
                                {
                                    // do later
                                }
                            }

                            employeeWorkTimeLog.SoNgayNghi = 1;
                            // Check off 0.5
                            var leaveFrom = existLeave.From.Date.Add(existLeave.Start);
                            var leaveTo = existLeave.To.Date.Add(existLeave.End);
                            for(DateTime dateL = leaveFrom; dateL <= leaveTo; dateL = dateL.Date.AddDays(1))
                            {
                                if (date == dateL.Date)
                                {
                                    var leaveTimeSpan = endWorkingScheduleTime - dateL.Date.Add(startWorkingScheduleTime).TimeOfDay;
                                    if (dateL == leaveFrom)
                                    {
                                        leaveTimeSpan = endWorkingScheduleTime - dateL.TimeOfDay;
                                    }
                                    if (dateL == leaveTo.Date)
                                    {
                                        leaveTimeSpan = leaveTo.TimeOfDay - startWorkingScheduleTime;
                                    }
                                    if (leaveTimeSpan.TotalHours < 4)
                                    {
                                        employeeWorkTimeLog.SoNgayNghi = 0.5;
                                    }
                                }
                            }

                            employeeWorkTimeLog.Reason = existLeave.TypeName;
                            employeeWorkTimeLog.ReasonDetail = Constants.StatusLeave(existLeave.Status);
                        }

                        var workTime = new TimeSpan(0);
                        var tangcathucte = new TimeSpan(0);
                        double workDay = 1;
                        var late = new TimeSpan(0);
                        var early = new TimeSpan(0);
                        var status = (int)EStatusWork.DuCong;
                        // No use, use analytics
                        var statusLate = (int)EStatusWork.DuCong;
                        var statusEarly = (int)EStatusWork.DuCong;

                        var timekeeping = timekeepings.FirstOrDefault(item => item.Date == date);
                        if (timekeeping == null)
                        {
                            if (employeeWorkTimeLog.Mode == (int)ETimeWork.Normal)
                            {
                                employeeWorkTimeLog.Mode = (int)ETimeWork.None;
                            }
                        }
                        else
                        {
                            // Always have data. (No data in timekeeping null)
                            var records = timekeeping.times.OrderBy(m => m.Date).ToList();
                            #region Procees Times
                            var inLogTime = records.First().TimeOnlyRecord;
                            var outLogTime = records.Last().TimeOnlyRecord;

                            // Phan tich thoi gian lam viec
                            var workingArr = new int[] { startWorkingScheduleTime.Hours, endWorkingScheduleTime.Hours };
                            // phan tich
                            // nhieu gio vo <12
                            // nhieu gio sau > 13
                            // 1 records...
                            var incheck = workingArr.ClosestTo(inLogTime.Hours);
                            var outcheck = workingArr.ClosestTo(outLogTime.Hours);

                            if (incheck == outcheck)
                            {
                                if (incheck > startWorkingScheduleTime.Add(new TimeSpan(4, 0, 0)).Hours)
                                {
                                    workTime = outLogTime - endWorkingScheduleTime.Add(new TimeSpan(4, 0, 0));
                                }
                                else
                                {
                                    workTime = startWorkingScheduleTime.Add(new TimeSpan(4,0,0)) - inLogTime;
                                }

                                if (employeeWorkTimeLog.Mode != (int)ETimeWork.Normal)
                                {
                                    tangcathucte = workTime;
                                }
                                status = (int)EStatusWork.XacNhanCong;
                                workDay = 0.5;
                                if (incheck == startWorkingScheduleTime.Hours)
                                {
                                    statusEarly = (int)EStatusWork.XacNhanCong;
                                    if (inLogTime > startWorkingScheduleTime)
                                    {
                                        late = inLogTime - startWorkingScheduleTime;
                                        // allow < 1 minute
                                        if (late.TotalMinutes < 1)
                                        {
                                            late = new TimeSpan(0);
                                        }
                                        if (late.TotalMinutes > 0)
                                        {
                                            statusLate = (int)EStatusWork.XacNhanCong;
                                        }
                                        if (late.TotalMinutes > 15)
                                        {
                                            workDay = 0;
                                        }
                                    }
                                }
                                if (incheck == endWorkingScheduleTime.Hours)
                                {
                                    statusLate = (int)EStatusWork.XacNhanCong;
                                    if (outLogTime < endWorkingScheduleTime)
                                    {
                                        early = endWorkingScheduleTime - outLogTime;
                                        // allow < 1 minute
                                        if (early.TotalMinutes < 1)
                                        {
                                            early = new TimeSpan(0);
                                        }
                                        if (early.TotalMinutes > 0)
                                        {
                                            statusEarly = (int)EStatusWork.XacNhanCong;
                                        }
                                        if (early.TotalMinutes > 15)
                                        {
                                            workDay = 0;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                workTime = (outLogTime - inLogTime) - lunch;
                                if (workTime.TotalHours > 8)
                                {
                                    tangcathucte = workTime - new TimeSpan(8, 0, 0);
                                }
                                if (employeeWorkTimeLog.Mode != (int)ETimeWork.Normal)
                                {
                                    tangcathucte = workTime;
                                }

                                if (inLogTime > startWorkingScheduleTime)
                                {
                                    statusLate = (int)EStatusWork.XacNhanCong;
                                    status = (int)EStatusWork.XacNhanCong;
                                    late = inLogTime - startWorkingScheduleTime;
                                    if (late.TotalMinutes < 1)
                                    {
                                        late = new TimeSpan(0);
                                        statusLate = (int)EStatusWork.DuCong;
                                        status = (int)EStatusWork.DuCong;
                                    }
                                    if (late.TotalMinutes > 15)
                                    {
                                        late = new TimeSpan(0);
                                        workDay += -0.5;
                                    }
                                }
                                if (outLogTime < endWorkingScheduleTime)
                                {
                                    statusEarly = (int)EStatusWork.XacNhanCong;
                                    status = (int)EStatusWork.XacNhanCong;
                                    early = endWorkingScheduleTime - outLogTime;
                                    if (early.TotalMinutes < 1)
                                    {
                                        early = new TimeSpan(0);
                                        statusEarly = (int)EStatusWork.DuCong;
                                        // + in
                                        if (status == (int)EStatusWork.DuCong)
                                        {
                                            status = (int)EStatusWork.DuCong;
                                        }
                                    }
                                    if (early.TotalMinutes > 15)
                                    {
                                        early = new TimeSpan(0);
                                        workDay += -0.5;
                                    }
                                }
                            }

                            if (tangcathucte.TotalHours >= 1)
                            {
                                employeeWorkTimeLog.StatusTangCa = (int)ETangCa.GuiXacNhan;
                            }
                            if (employeeWorkTimeLog.Mode != (int)ETimeWork.Normal)
                            {
                                employeeWorkTimeLog.StatusTangCa = (int)ETangCa.GuiXacNhan;
                            }

                            employeeWorkTimeLog.VerifyMode = records[0].VerifyMode;
                            employeeWorkTimeLog.InOutMode = records[0].InOutMode;


                            employeeWorkTimeLog.In = inLogTime;
                            employeeWorkTimeLog.Out = outLogTime;

                            employeeWorkTimeLog.WorkTime = workTime;
                            employeeWorkTimeLog.TangCaThucTe = tangcathucte;
                            employeeWorkTimeLog.WorkDay = workDay;
                            employeeWorkTimeLog.Late = late;
                            employeeWorkTimeLog.Early = early;
                            employeeWorkTimeLog.Status = status;
                            employeeWorkTimeLog.StatusEarly = statusEarly;
                            employeeWorkTimeLog.StatusLate = statusLate;
                            employeeWorkTimeLog.Logs = records;
                            #endregion
                        }

                        #region DB
                        var workTimeLogDb = dbContext.EmployeeWorkTimeLogs
                                                    .Find(m => m.EmployeeId.Equals(employeeWorkTimeLog.EmployeeId)
                                                        && m.Date.Equals(employeeWorkTimeLog.Date)).FirstOrDefault();

                        if (workTimeLogDb == null)
                        {
                            dbContext.EmployeeWorkTimeLogs.InsertOne(employeeWorkTimeLog);
                        }
                        else
                        {
                            bool isUpdate = false;
                            if (string.IsNullOrEmpty(workTimeLogDb.SecureCode))
                            {
                                isUpdate = true;
                            }
                            if (workTimeLogDb.Mode != (int)ETimeWork.Normal)
                            {
                                isUpdate = true;
                            }
                            if (isUpdate)
                            {
                                var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, workTimeLogDb.Id);
                                var update = Builders<EmployeeWorkTimeLog>.Update
                                    .Set(m => m.EnrollNumber, employeeWorkTimeLog.EnrollNumber)
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
                                    .Set(m => m.Reason, employeeWorkTimeLog.Reason)
                                    .Set(m => m.UpdatedOn, DateTime.Now);
                                dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
                            }
                        }
                        #endregion

                        #region Send Mail
                        if (isMail)
                        {
                            var iDateSent = -1;
                            if (DateTime.Now.Date.AddDays(iDateSent).DayOfWeek == DayOfWeek.Sunday)
                            {
                                iDateSent--;
                            }
                            if (status == (int)EStatusWork.XacNhanCong && date == today.AddDays(iDateSent) && !string.IsNullOrEmpty(email))
                            {
                                Console.WriteLine("Sending mail...");
                                var tos = new List<EmailAddress>
                                        {
                                            new EmailAddress { Name = fullName, Address = email }
                                        };
                                if (debug)
                                {
                                    tos = new List<EmailAddress>
                                        {
                                            new EmailAddress { Name = fullName, Address = "xuan.tm@tribat.vn" }
                                        };
                                }
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
                                    BodyContent = messageBody,
                                    EmployeeId = employeeWorkTimeLog.EmployeeId
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
                        #endregion
                    }

                    Summary(dbContext, employee, location, month, year, modeData, debug);

                    startDate = startDate.AddMonths(1);
                }
                #endregion
            }
        }

        private static void Summary(MongoDBContext dbContext, Employee employee, string location, int month, int year, bool modeData, bool debug)
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
                    Part = employee.Part.ToUpper(),
                    Department = employee.Department.ToUpper(),
                    Title = employee.Title.ToUpper(),
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
            foreach (var time in times)
            {
                switch (time.Mode)
                {
                    case (int)ETimeWork.Normal:
                        {
                            Workday += time.WorkDay;
                            WorkTime += time.WorkTime.TotalMilliseconds;
                            CongTangCaNgayThuongGio += time.TangCaDaXacNhan.TotalMilliseconds;
                            Late += time.Late.TotalMilliseconds;
                            Early += time.Early.TotalMilliseconds;
                            break;
                        }
                    case (int)ETimeWork.Sunday:
                        {
                            ChuNhat++;
                            CongCNGio += time.TangCaDaXacNhan.TotalMilliseconds;
                            break;
                        }
                    case (int)ETimeWork.LeavePhep:
                        {
                            NghiPhepNam++;
                            break;
                        }
                    case (int)ETimeWork.LeaveHuongLuong:
                        {
                            NghiHuongLuong++;
                            break;
                        }
                    case (int)ETimeWork.LeaveKhongHuongLuong:
                        {
                            NghiViecRieng++;
                            break;
                        }
                    case (int)ETimeWork.Holiday:
                        {
                            NghiLe++;
                            CongLeTet += time.TangCaDaXacNhan.TotalMilliseconds;
                            break;
                        }
                    case (int)ETimeWork.Other:
                        Console.WriteLine((int)ETimeWork.Other);
                        break;
                    case (int)ETimeWork.Wait:
                        Console.WriteLine((int)ETimeWork.Wait);
                        break;
                    default:
                        {
                            KhongChamCong++;
                            break;
                        }
                }
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


        private static void UpdateSummary(MongoDBContext dbContext, DateTime dateData, EmployeeWorkTimeLog employeeWorkTimeLog, int month, int year)
        {
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
                    EmployeeName = employeeWorkTimeLog.EmployeeName,
                    Part = employeeWorkTimeLog.Part,
                    Department = employeeWorkTimeLog.Department,
                    Title = employeeWorkTimeLog.EmployeeTitle,
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

        private static void UpdateSummaryChangeData(MongoDBContext dbContext, DateTime dateData, EmployeeWorkTimeLog employeeWorkTimeLog, double currentWorkTime, double currentLate, double currentEarly, double currentWorkDay, int month, int year)
        {
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
        private static void UpdateFinal(MongoDBContext dbContext, string location, bool modeData, int day, bool debug)
        {
            var holidays = dbContext.Holidays.Find(m => m.Enable.Equals(true)).ToEnumerable().Where(m => m.Year.Equals(DateTime.Now.Year)).ToList();

            var leaveTypes = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true) && m.Display.Equals(true)).ToList();

            var today = DateTime.Now.Date;
            var endDay = today.Day > 25 ? new DateTime(today.AddMonths(1).Year, today.AddMonths(1).Month, 25) : new DateTime(today.Year, today.Month, 25);

            var filterEmp = Builders<Employee>.Filter.Eq(m => m.Enable, true)
                    & Builders<Employee>.Filter.ElemMatch(z => z.Workplaces, a => a.Code == location);
            var employees = dbContext.Employees.Find(filterEmp).ToList();
            if (debug)
            {
                var debugString = ConfigurationSettings.AppSettings.Get("debugString").ToString();
                employees = dbContext.Employees.Find(m => m.Workplaces.Any(w => w.Code.Equals(location) && w.Fingerprint.Equals(debugString))).ToList();
            }
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
                if (modeData)
                {
                    startDate = endDay.AddDays(1).AddMonths(-4);
                }
                while (startDate <= endDay)
                {
                    #region Declare
                    double NgayLamViec = 0;
                    double CongTangCaNgayThuong = 0;
                    double CongTangCaChuNhat = 0;
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
                            EmployeeName = employee.FullName,
                            Part = employee.Part,
                            Department = employee.Department,
                            Title = employee.Title,
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

                    var timekeepings = dbContext.EmployeeWorkTimeLogs.Find(m => m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)).SortBy(m => m.Date).ToList();

                    var leaves = dbContext.Leaves.Find(m => m.EmployeeId.Equals(employeeId)).ToList();

                    for (DateTime date = startDateMonth; date <= endDateMonth; date = date.AddDays(1))
                    {
                        if (date > today)
                        {
                            continue;
                        }
                        // Check in holiday
                        var holiday = holidays.Where(m => m.Date.Equals(date)).FirstOrDefault();

                        var timekeeping = timekeepings.FirstOrDefault(item => item.Date == date);
                        if (timekeeping == null)
                        {
                            var employeeWorkTimeLog = new EmployeeWorkTimeLog
                            {
                                EmployeeId = employee.Id,
                                EmployeeName = employee.FullName,
                                EmployeeTitle = employee.Title.ToUpper(),
                                Department = employee.Department.ToUpper(),
                                Part = employee.Part.ToUpper(),
                                EnrollNumber = employeeLocation.Fingerprint,
                                Year = year,
                                Month = month,
                                Date = date,
                                WorkplaceCode = location,
                                Workcode = employee.SalaryType
                            };
                            if (date.DayOfWeek == DayOfWeek.Sunday)
                            {
                                employeeWorkTimeLog.Mode = (int)ETimeWork.Sunday;
                                employeeWorkTimeLog.Reason = "Chủ nhật";
                                ChuNhat++;
                            }
                            else if (holiday != null)
                            {
                                // Holiday
                                employeeWorkTimeLog.Mode = (int)ETimeWork.Holiday;
                                employeeWorkTimeLog.Reason = holiday.Name;
                                employeeWorkTimeLog.ReasonDetail = holiday.Detail;
                                NghiLe++;
                            }
                            else
                            {
                                var existLeave = leaves.FirstOrDefault(item => item.Enable.Equals(true) && date.Equals(item.From.Date));
                                if (existLeave != null)
                                {
                                    decimal numberLeave = existLeave.Number;
                                    //var workCode = existLeave.TypeName;
                                    // Status do later...
                                    //status = existLeave.Status;
                                    foreach (var leaveType in leaveTypes)
                                    {
                                        if (existLeave.TypeId == leaveType.Id)
                                        {
                                            if (leaveType.Alias == "phep-nam")
                                            {
                                                NghiPhepNam += (double)numberLeave;
                                                employeeWorkTimeLog.Mode = (int)ETimeWork.LeavePhep;
                                            }
                                            else if (leaveType.Alias == "phep-khong-huong-luong")
                                            {
                                                NghiViecRieng += (double)numberLeave;
                                                employeeWorkTimeLog.Mode = (int)ETimeWork.LeaveKhongHuongLuong;
                                            }
                                            else if (leaveType.Alias == "nghi-huong-luong")
                                            {
                                                NghiHuongLuong += (double)numberLeave;
                                                employeeWorkTimeLog.Mode = (int)ETimeWork.LeaveHuongLuong;
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
                                    employeeWorkTimeLog.Mode = (int)ETimeWork.None;
                                    employeeWorkTimeLog.Status = (int)EStatusWork.XacNhanCong;
                                    KhongChamCong++;
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
                        else
                        {
                            var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);

                            #region Check if Leave, Sunday, Holiday
                            //timekeeping.Status = (int)StatusWork.DuCong;
                            if (date.DayOfWeek == DayOfWeek.Sunday)
                            {
                                timekeeping.Mode = (int)ETimeWork.Sunday;
                                timekeeping.Reason = "Chủ nhật";
                                ChuNhat++;
                            }
                            else if (holiday != null)
                            {
                                // Holiday
                                timekeeping.Mode = (int)ETimeWork.Holiday;
                                timekeeping.Reason = holiday.Name;
                                timekeeping.ReasonDetail = holiday.Detail;
                                NghiLe++;
                            }
                            else
                            {
                                var existLeave = leaves.FirstOrDefault(item => item.Enable.Equals(true) && date.Equals(item.From.Date));
                                if (existLeave != null)
                                {
                                    decimal numberLeave = existLeave.Number;
                                    //var workCode = existLeave.TypeName;
                                    // Status do later...
                                    //status = existLeave.Status;
                                    foreach (var leaveType in leaveTypes)
                                    {
                                        if (existLeave.TypeId == leaveType.Id)
                                        {
                                            if (leaveType.Alias == "phep-nam")
                                            {
                                                NghiPhepNam += (double)numberLeave;
                                                timekeeping.Mode = (int)ETimeWork.LeavePhep;
                                            }
                                            else if (leaveType.Alias == "phep-khong-huong-luong")
                                            {
                                                NghiViecRieng += (double)numberLeave;
                                                timekeeping.Mode = (int)ETimeWork.LeaveKhongHuongLuong;
                                            }
                                            else if (leaveType.Alias == "nghi-huong-luong")
                                            {
                                                NghiHuongLuong += (double)numberLeave;
                                                timekeeping.Mode = (int)ETimeWork.LeaveHuongLuong;
                                            }
                                            else if (leaveType.Alias == "nghi-bu")
                                            {
                                                // do later
                                            }
                                        }
                                    }
                                    timekeeping.Reason = existLeave.TypeName;
                                    timekeeping.ReasonDetail = Constants.StatusLeave(existLeave.Status);
                                }
                                else
                                {
                                    timekeeping.Mode = (int)ETimeWork.None;
                                    timekeeping.Status = (int)EStatusWork.XacNhanCong;
                                    KhongChamCong++;
                                }
                            }

                            var update = Builders<EmployeeWorkTimeLog>.Update
                                .Set(m => m.Mode, timekeeping.Mode)
                                .Set(m => m.Reason, timekeeping.Reason)
                                .Set(m => m.ReasonDetail, timekeeping.ReasonDetail)
                                .Set(m => m.Status, timekeeping.Status);
                            #endregion

                            // Update time finger
                            int status = timekeeping.Status;
                            int statusLate = timekeeping.StatusLate;
                            int statusEarly = timekeeping.StatusEarly;
                            if (status == (int)EStatusWork.XacNhanCong || status == (int)EStatusWork.DaGuiXacNhan || status == (int)EStatusWork.Wait)
                            {
                                if (statusLate == (int)EStatusWork.XacNhanCong && timekeeping.In.HasValue)
                                {
                                    var lateMinute = timekeeping.Late.TotalMinutes;
                                    if (lateMinute > 15)
                                    {
                                        update = update.Set(m => m.Late, new TimeSpan(0, 0, 0))
                                            .Inc(m => m.WorkDay, -0.5);
                                        NgayLamViec += 0.5;
                                        Late += timekeeping.Late.TotalMilliseconds;
                                    }
                                }
                                if (statusEarly == (int)EStatusWork.XacNhanCong && timekeeping.Out.HasValue)
                                {
                                    var earlyMinute = timekeeping.Early.TotalMinutes;

                                    if (earlyMinute > 15)
                                    {
                                        update = update.Set(m => m.Early, new TimeSpan(0, 0, 0))
                                            .Inc(m => m.WorkDay, -0.5);
                                        NgayLamViec += 0.5;
                                        Early += timekeeping.Early.TotalMilliseconds;
                                    }
                                }
                            }

                            dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
                        }
                    }

                    #region update Summarry, Should Independent
                    var builderSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
                    var filterSum = builderSum.Eq(m => m.Id, monthTimeInformation.Id);
                    var updateSum = Builders<EmployeeWorkTimeMonthLog>.Update
                        .Set(m => m.Workday, NgayLamViec)
                        .Set(m => m.CongTangCaNgayThuongGio, CongTangCaNgayThuong)
                        .Set(m => m.CongCNGio, CongTangCaChuNhat)
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
                if (time.Status == (int)EStatusWork.DongY)
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
                        EmployeeName = time.EmployeeName,
                        Part = time.Part,
                        Department = time.Department,
                        Title = time.EmployeeTitle,
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


        // Update missing Date, Leave date, apply allow late|early,...
        private static void UpdateFinalBK(MongoDBContext dbContext, string location, bool modeData, int day, bool debug)
        {
            var holidays = dbContext.Holidays.Find(m => m.Enable.Equals(true)).ToEnumerable().Where(m => m.Year.Equals(DateTime.Now.Year)).ToList();

            var leaveTypes = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true) && m.Display.Equals(true)).ToList();

            var today = DateTime.Now.Date;
            var endDay = today.Day > 25 ? new DateTime(today.AddMonths(1).Year, today.AddMonths(1).Month, 25) : new DateTime(today.Year, today.Month, 25);

            var filterEmp = Builders<Employee>.Filter.Eq(m => m.Enable, true)
                    & Builders<Employee>.Filter.ElemMatch(z => z.Workplaces, a => a.Code == location);
            var employees = dbContext.Employees.Find(filterEmp).ToList();
            if (debug)
            {
                var debugString = ConfigurationSettings.AppSettings.Get("debugString").ToString();
                employees = dbContext.Employees.Find(m => m.Workplaces.Any(w => w.Code.Equals(location) && w.Fingerprint.Equals(debugString))).ToList();
            }
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
                if (modeData)
                {
                    startDate = endDay.AddDays(1).AddMonths(-4);
                }
                while (startDate <= endDay)
                {
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
                            EmployeeName = employee.FullName,
                            Part = employee.Part,
                            Department = employee.Department,
                            Title = employee.Title,
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

                    var timekeepings = dbContext.EmployeeWorkTimeLogs.Find(m => m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)).SortBy(m => m.Date).ToList();

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
                        // Check in holiday
                        var holiday = holidays.Where(m => m.Date.Equals(date)).FirstOrDefault();

                        var timekeeping = timekeepings.FirstOrDefault(item => item.Date == date);
                        if (timekeeping == null)
                        {
                            var employeeWorkTimeLog = new EmployeeWorkTimeLog
                            {
                                EmployeeId = employee.Id,
                                EmployeeName = employee.FullName,
                                EmployeeTitle = employee.Title.ToUpper(),
                                Department = employee.Department.ToUpper(),
                                Part = employee.Part.ToUpper(),
                                EnrollNumber = employeeLocation.Fingerprint,
                                Year = year,
                                Month = month,
                                Date = date,
                                WorkplaceCode = location,
                                Workcode = employee.SalaryType
                            };
                            if (date.DayOfWeek == DayOfWeek.Sunday)
                            {
                                employeeWorkTimeLog.Mode = (int)ETimeWork.Sunday;
                                employeeWorkTimeLog.Reason = "Chủ nhật";
                                ChuNhat++;
                            }
                            else if (holiday != null)
                            {
                                // Holiday
                                employeeWorkTimeLog.Mode = (int)ETimeWork.Holiday;
                                employeeWorkTimeLog.Reason = holiday.Name;
                                employeeWorkTimeLog.ReasonDetail = holiday.Detail;
                                NghiLe++;
                            }
                            else
                            {
                                var existLeave = leaves.FirstOrDefault(item => item.Enable.Equals(true) && date.Equals(item.From.Date));
                                if (existLeave != null)
                                {
                                    decimal numberLeave = existLeave.Number;
                                    //var workCode = existLeave.TypeName;
                                    // Status do later...
                                    //status = existLeave.Status;
                                    foreach (var leaveType in leaveTypes)
                                    {
                                        if (existLeave.TypeId == leaveType.Id)
                                        {
                                            if (leaveType.Alias == "phep-nam")
                                            {
                                                NghiPhepNam += (double)numberLeave;
                                                employeeWorkTimeLog.Mode = (int)ETimeWork.LeavePhep;
                                            }
                                            else if (leaveType.Alias == "phep-khong-huong-luong")
                                            {
                                                NghiViecRieng += (double)numberLeave;
                                                employeeWorkTimeLog.Mode = (int)ETimeWork.LeaveKhongHuongLuong;
                                            }
                                            else if (leaveType.Alias == "nghi-huong-luong")
                                            {
                                                NghiHuongLuong += (double)numberLeave;
                                                employeeWorkTimeLog.Mode = (int)ETimeWork.LeaveHuongLuong;
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
                                    employeeWorkTimeLog.Mode = (int)ETimeWork.None;
                                    employeeWorkTimeLog.Status = (int)EStatusWork.XacNhanCong;
                                    KhongChamCong++;
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
                        else
                        {
                            var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timekeeping.Id);

                            #region Check if Leave, Sunday, Holiday
                            //timekeeping.Status = (int)StatusWork.DuCong;
                            if (date.DayOfWeek == DayOfWeek.Sunday)
                            {
                                timekeeping.Mode = (int)ETimeWork.Sunday;
                                timekeeping.Reason = "Chủ nhật";
                                ChuNhat++;
                            }
                            else if (holiday != null)
                            {
                                // Holiday
                                timekeeping.Mode = (int)ETimeWork.Holiday;
                                timekeeping.Reason = holiday.Name;
                                timekeeping.ReasonDetail = holiday.Detail;
                                NghiLe++;
                            }
                            else
                            {
                                var existLeave = leaves.FirstOrDefault(item => item.Enable.Equals(true) && date.Equals(item.From.Date));
                                if (existLeave != null)
                                {
                                    decimal numberLeave = existLeave.Number;
                                    //var workCode = existLeave.TypeName;
                                    // Status do later...
                                    //status = existLeave.Status;
                                    foreach (var leaveType in leaveTypes)
                                    {
                                        if (existLeave.TypeId == leaveType.Id)
                                        {
                                            if (leaveType.Alias == "phep-nam")
                                            {
                                                NghiPhepNam += (double)numberLeave;
                                                timekeeping.Mode = (int)ETimeWork.LeavePhep;
                                            }
                                            else if (leaveType.Alias == "phep-khong-huong-luong")
                                            {
                                                NghiViecRieng += (double)numberLeave;
                                                timekeeping.Mode = (int)ETimeWork.LeaveKhongHuongLuong;
                                            }
                                            else if (leaveType.Alias == "nghi-huong-luong")
                                            {
                                                NghiHuongLuong += (double)numberLeave;
                                                timekeeping.Mode = (int)ETimeWork.LeaveHuongLuong;
                                            }
                                            else if (leaveType.Alias == "nghi-bu")
                                            {
                                                // do later
                                            }
                                        }
                                    }
                                    timekeeping.Reason = existLeave.TypeName;
                                    timekeeping.ReasonDetail = Constants.StatusLeave(existLeave.Status);
                                }
                                else
                                {
                                    timekeeping.Mode = (int)ETimeWork.None;
                                    timekeeping.Status = (int)EStatusWork.XacNhanCong;
                                    KhongChamCong++;
                                }
                            }

                            var update = Builders<EmployeeWorkTimeLog>.Update
                                .Set(m => m.Mode, timekeeping.Mode)
                                .Set(m => m.Reason, timekeeping.Reason)
                                .Set(m => m.ReasonDetail, timekeeping.ReasonDetail)
                                .Set(m => m.Status, timekeeping.Status);
                            #endregion

                            // Update time finger
                            int status = timekeeping.Status;
                            int statusLate = timekeeping.StatusLate;
                            int statusEarly = timekeeping.StatusEarly;
                            if (status == (int)EStatusWork.XacNhanCong || status == (int)EStatusWork.DaGuiXacNhan || status == (int)EStatusWork.Wait)
                            {
                                if (statusLate == (int)EStatusWork.XacNhanCong && timekeeping.In.HasValue)
                                {
                                    var lateMinute = timekeeping.Late.TotalMinutes;
                                    if (lateMinute > 15)
                                    {
                                        update = update.Set(m => m.Late, new TimeSpan(0, 0, 0))
                                            .Inc(m => m.WorkDay, -0.5);
                                        WorkDay += 0.5;
                                        WorkTime += 0.5 * 8 * 60 * 60 * 60;
                                        Late += timekeeping.Late.TotalMilliseconds;
                                    }
                                }
                                if (statusEarly == (int)EStatusWork.XacNhanCong && timekeeping.Out.HasValue)
                                {
                                    var earlyMinute = timekeeping.Early.TotalMinutes;

                                    if (earlyMinute > 15)
                                    {
                                        update = update.Set(m => m.Early, new TimeSpan(0, 0, 0))
                                            .Inc(m => m.WorkDay, -0.5);
                                        WorkDay += 0.5;
                                        WorkTime += 0.5 * 8 * 60 * 60 * 60;
                                        Early += timekeeping.Early.TotalMilliseconds;
                                    }
                                }
                            }

                            dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
                        }
                    }

                    #region update Summarry, Should Independent
                    //var builderSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
                    //var filterSum = builderSum.Eq(m => m.Id, monthTimeInformation.Id);
                    //var updateSum = Builders<EmployeeWorkTimeMonthLog>.Update
                    //    .Inc(m => m.Workday, -(WorkDay))
                    //    .Inc(m => m.WorkTime, -(WorkTime))
                    //    .Inc(m => m.CongTangCaNgayThuongGio, CongCNGio)
                    //    .Inc(m => m.CongLeTet, CongLeTet)
                    //    .Inc(m => m.Late, -(Late))
                    //    .Inc(m => m.Early, -(Early))
                    //    .Inc(m => m.NghiHuongLuong, NghiHuongLuong)
                    //    .Inc(m => m.ChuNhat, ChuNhat)
                    //    .Inc(m => m.NghiLe, NghiLe)
                    //    .Inc(m => m.KhongChamCong, KhongChamCong)
                    //    .Set(m => m.LastUpdated, DateTime.Now);
                    //dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterSum, updateSum);
                    #endregion

                    startDate = startDate.AddMonths(1);
                }
            }
        }

    }
}

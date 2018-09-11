using Common.Utilities;
using Data;
using Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace schedulecore
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press ENTER to exist.");
            Console.ReadLine();
            UpdateTimeKeeper();
        }

        static void UpdateTimeKeeper()
        {
            #region Connection
            MongoDBContext.ConnectionString = "mongodb://localhost:27017";
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            #region Setting
            var mode = true;// get data condition date
            var day = -7;
            var setting = dbContext.Settings.Find(m => m.Key.Equals("schedulecoremode")).FirstOrDefault();
            if (setting != null && !string.IsNullOrEmpty(setting.Value))
            {
                // 0: get all data
                mode = setting.Value == "0" ? false : true;
            }

            var settingDay = dbContext.Settings.Find(m => m.Key.Equals("schedulecoreday")).FirstOrDefault();
            if (settingDay != null && !string.IsNullOrEmpty(settingDay.Value))
            {
                day = Convert.ToInt32(setting.Value);
            }
            #endregion

            // var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)).ToList();
            var employees = dbContext.Employees.Find(m => true).ToList();
            var employee = new Employee();

            #region Condition data & declare
            var timeCrawled = DateTime.Now.AddDays(day);
            timeCrawled = new DateTime(timeCrawled.Year, timeCrawled.Month, timeCrawled.Day, 2, 0, 0);
            var dateProcess = timeCrawled;
            var builder = Builders<AttLog>.Filter;
            var filter = builder.Gt(m => m.Date, dateProcess);
            var workingScheduleTime = "7:00 - 16:00";
            decimal workingScheduleHour = 8;
            var lunch = TimeSpan.FromHours(1);
            // NEED UPDATE IS FINGER NOT IN EMPLOYEES
            var attlogs = new List<AttLog>();
            var employeeWorkTimeLogs = new List<EmployeeWorkTimeLog>();
            #endregion

            #region VP
            attlogs = mode ? dbContext.X628CVPAttLogs.Find(filter).ToList() : dbContext.X628CVPAttLogs.Find(m => true).ToList();
            if (attlogs.Count > 0)
            {
                var groups = (
                            from p in attlogs
                            group p by p.Date.Date into d
                            select new
                            {
                                dt = d.Key.ToShortDateString(),
                                count = d.Count(),
                                times = d.ToList(),
                            }
                        ).ToList();

                foreach (var group in groups)
                {
                    #region Declare
                    var workTime = new TimeSpan(0);
                    decimal workDay = 0;
                    var late = new TimeSpan(0);
                    var early = new TimeSpan(0);
                    // 0: cần xác nhận công; 1: đủ ngày công ; 2: đã gửi xác nhận công, 3: đồng ý; 4: từ chối  
                    var status = 1;
                    #endregion

                    var times = group.times.OrderBy(m => m.Date).ToList();
                    var enrollNumber = times[0].EnrollNumber;
                    var dateFinger = times.First().DateOnlyRecord;
                    var inLogTime = times.First().TimeOnlyRecord;
                    var outLogTime = times.Last().TimeOnlyRecord;

                    #region Save to db: In/Out log
                    TimeSpan? dbinLogTime = inLogTime;
                    TimeSpan? dboutLogTime = outLogTime;
                    #endregion

                    var startWorkingScheduleTime = TimeSpan.Parse(workingScheduleTime.Split('-')[0].Trim());
                    var endWorkingScheduleTime = TimeSpan.Parse(workingScheduleTime.Split('-')[1].Trim());

                    #region 2 case: EnrollCode | No enrollcode 
                    var employee8 = employees.SelectMany(parent => parent.Workplaces)
                             .FirstOrDefault(child => child.Fingerprint.Equals(enrollNumber));
                    #endregion

                    // If the start time is before the starting hours, set it to the starting hour.
                    if (inLogTime < startWorkingScheduleTime) inLogTime = startWorkingScheduleTime;
                    if (outLogTime > endWorkingScheduleTime) outLogTime = endWorkingScheduleTime;

                    workTime = (outLogTime - inLogTime) - lunch;
                    workDay = Convert.ToDecimal(workTime.TotalHours) / workingScheduleHour;

                    if (inLogTime > startWorkingScheduleTime)
                    {
                        late = inLogTime - startWorkingScheduleTime;
                        status = 0;
                    }
                    if (outLogTime < endWorkingScheduleTime)
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

                    var employeeWorkTimeLog = new EmployeeWorkTimeLog
                    {
                        EnrollNumber = enrollNumber,
                        VerifyMode = times[0].VerifyMode,
                        InOutMode = times[0].InOutMode,
                        Workcode = times[0].Workcode,
                        WorkplaceCode = "VP",
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
                        Logs = times
                    };
                    employeeWorkTimeLogs.Add(employeeWorkTimeLog);
                }
            }
            #endregion

            #region Comment
            //foreach (var employee1 in employees)
            //{
            //    if (employee1.Workplaces != null && employee1.Workplaces.Count > 0)
            //    {
            //        if (!string.IsNullOrEmpty(employee1.WorkingScheduleTime))
            //        {
            //            workingScheduleTime = employee1.WorkingScheduleTime;
            //        }
            //        var startWorkingScheduleTime = TimeSpan.Parse(workingScheduleTime.Split('-')[0].Trim());
            //        var endWorkingScheduleTime = TimeSpan.Parse(workingScheduleTime.Split('-')[1].Trim());
            //        //var attlogs = new List<AttLog>();

            //        foreach (var workplace in employee1.Workplaces)
            //        {
            //            if (!string.IsNullOrEmpty(workplace.Fingerprint))
            //            {
            //                // First update all
            //                builder = Builders<AttLog>.Filter;
            //                filter = builder.Eq(m => m.EnrollNumber, workplace.Fingerprint);
            //                if (mode)
            //                {
            //                    filter = filter & builder.Gt(m => m.Date, dateProcess);
            //                }
            //                switch (workplace.Code)
            //                {
            //                    case "VP":
            //                        {
            //                            attlogs = dbContext.X628CVPAttLogs.Find(filter).ToList();
            //                            if (attlogs.Count > 0)
            //                            {
            //                                #region Rule & Process db
            //                                var groups = (
            //                                            from p in attlogs
            //                                            group p by p.Date.Date into d
            //                                            select new
            //                                            {
            //                                                dt = d.Key.ToShortDateString(),
            //                                                count = d.Count(),
            //                                                times = d.ToList(),
            //                                            }
            //                                        ).ToList();

            //                                employeeWorkTimeLogs = new List<EmployeeWorkTimeLog>();
            //                                foreach (var group in groups)
            //                                {
            //                                    #region Declare
            //                                    var workTime = new TimeSpan(0);
            //                                    decimal workDay = 0;
            //                                    var late = new TimeSpan(0);
            //                                    var early = new TimeSpan(0);
            //                                    // 0: cần xác nhận công; 1: đủ ngày công ; 2: đã gửi xác nhận công, 3: đồng ý; 4: từ chối  
            //                                    var status = 1;
            //                                    #endregion

            //                                    var times = group.times.OrderBy(m => m.Date).ToList();
            //                                    var dateFinger = times.First().DateOnlyRecord;
            //                                    var inLogTime = times.First().TimeOnlyRecord;
            //                                    var outLogTime = times.Last().TimeOnlyRecord;

            //                                    // Save to db
            //                                    TimeSpan? dbinLogTime = inLogTime;
            //                                    TimeSpan? dboutLogTime = outLogTime;
            //                                    // End

            //                                    // If the start time is before the starting hours, set it to the starting hour.
            //                                    if (inLogTime < startWorkingScheduleTime) inLogTime = startWorkingScheduleTime;
            //                                    if (outLogTime > endWorkingScheduleTime) outLogTime = endWorkingScheduleTime;

            //                                    workTime = (outLogTime - inLogTime) - lunch;
            //                                    workDay = Convert.ToDecimal(workTime.TotalHours) / workingScheduleHour;

            //                                    if (inLogTime > startWorkingScheduleTime)
            //                                    {
            //                                        late = inLogTime - startWorkingScheduleTime;
            //                                        status = 0;
            //                                    }
            //                                    if (outLogTime < endWorkingScheduleTime)
            //                                    {
            //                                        early = endWorkingScheduleTime - outLogTime;
            //                                        status = 0;
            //                                    }

            //                                    #region Check miss in/out
            //                                    var workingArr = new int[] { startWorkingScheduleTime.Hours, endWorkingScheduleTime.Hours };
            //                                    var incheck = workingArr.ClosestTo(dbinLogTime.Value.Hours);
            //                                    var outcheck = workingArr.ClosestTo(dboutLogTime.Value.Hours);
            //                                    if (incheck == outcheck)
            //                                    {
            //                                        if (incheck == endWorkingScheduleTime.Hours)
            //                                        {
            //                                            // missing in
            //                                            dbinLogTime = null;
            //                                            late = new TimeSpan(0);
            //                                            early = endWorkingScheduleTime - outLogTime;
            //                                            workTime = TimeSpan.FromHours(4) - early;
            //                                        }
            //                                        else
            //                                        {
            //                                            // missing out
            //                                            dboutLogTime = null;
            //                                            early = new TimeSpan(0);
            //                                            late = inLogTime - startWorkingScheduleTime;
            //                                            workTime = TimeSpan.FromHours(4) - late;
            //                                        }

            //                                        status = 0;

            //                                        workDay = Convert.ToDecimal(workTime.TotalHours) / workingScheduleHour;
            //                                    }
            //                                    #endregion

            //                                    var employeeWorkTimeLog = new EmployeeWorkTimeLog
            //                                    {
            //                                        EnrollNumber = times[0].EnrollNumber,
            //                                        VerifyMode = times[0].VerifyMode,
            //                                        InOutMode = times[0].InOutMode,
            //                                        Workcode = times[0].Workcode,
            //                                        WorkplaceCode = "VP",
            //                                        Date = dateFinger,
            //                                        In = dbinLogTime,
            //                                        Out = dboutLogTime,
            //                                        Start = startWorkingScheduleTime,
            //                                        End = endWorkingScheduleTime,
            //                                        WorkTime = workTime,
            //                                        WorkDay = workDay,
            //                                        Late = late,
            //                                        Early = early,
            //                                        Status = status,
            //                                        Logs = times
            //                                    };
            //                                    employeeWorkTimeLogs.Add(employeeWorkTimeLog);
            //                                }
            //                                try
            //                                {
            //                                    // Insert if no exist
            //                                    foreach (var item in employeeWorkTimeLogs)
            //                                    {
            //                                        // check db current
            //                                        if (dbContext.EmployeeWorkTimeLogs.Find(m => m.EnrollNumber.Equals(item.EnrollNumber) && m.Date.Equals(item.Date) && m.In.Equals(item.In) && m.Out.Equals(item.Out)).Count() == 0)
            //                                        {
            //                                            if (dbContext.EmployeeWorkTimeLogs.Find(m => m.EnrollNumber.Equals(item.EnrollNumber) && m.Date.Equals(item.Date)).Count() > 0)
            //                                            {
            //                                                var builderUpdate = Builders<EmployeeWorkTimeLog>.Filter;
            //                                                var filterUpdate = builderUpdate.Eq(m => m.EnrollNumber, item.EnrollNumber);
            //                                                filterUpdate = filterUpdate & builderUpdate.Gt(m => m.Date, dateProcess);

            //                                                var update = Builders<EmployeeWorkTimeLog>.Update
            //                                                    .Set(m => m.In, item.In)
            //                                                    .Set(m => m.Out, item.Out)
            //                                                    .Set(m => m.WorkTime, item.WorkTime)
            //                                                    .Set(m => m.WorkDay, item.WorkDay)
            //                                                    .Set(m => m.Late, item.Late)
            //                                                    .Set(m => m.Early, item.Early)
            //                                                    .Set(m => m.Status, item.Status)
            //                                                    .Set(m => m.Logs, item.Logs)
            //                                                    .Set(m => m.UpdatedOn, DateTime.Now);
            //                                                dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
            //                                            }
            //                                            else
            //                                            {
            //                                                dbContext.EmployeeWorkTimeLogs.InsertOne(item);
            //                                            }
            //                                        }
            //                                    }
            //                                }
            //                                catch (Exception ex)
            //                                {
            //                                    Console.WriteLine(ex);
            //                                }
            //                                #endregion
            //                            }
            //                            break;
            //                        }
            //                    default:
            //                        {
            //                            attlogs = dbContext.X928CNMAttLogs.Find(filter).ToList();
            //                            if (attlogs.Count > 0)
            //                            {
            //                                #region Rule & Process db
            //                                var groups = (
            //                                            from p in attlogs
            //                                            group p by p.Date.Date into d
            //                                            select new
            //                                            {
            //                                                dt = d.Key.ToShortDateString(),
            //                                                count = d.Count(),
            //                                                times = d.ToList(),
            //                                            }
            //                                        ).ToList();

            //                                employeeWorkTimeLogs = new List<EmployeeWorkTimeLog>();
            //                                foreach (var group in groups)
            //                                {
            //                                    #region Declare
            //                                    var workTime = new TimeSpan(0);
            //                                    decimal workDay = 0;
            //                                    var late = new TimeSpan(0);
            //                                    var early = new TimeSpan(0);
            //                                    // 0: cần xác nhận công; 1: đủ ngày công ; 2: đã gửi xác nhận công, 3: đồng ý; 4: từ chối  
            //                                    var status = 1;
            //                                    #endregion

            //                                    var times = group.times.OrderBy(m => m.Date).ToList();
            //                                    var dateFinger = times.First().DateOnlyRecord;
            //                                    var inLogTime = times.First().TimeOnlyRecord;
            //                                    var outLogTime = times.Last().TimeOnlyRecord;

            //                                    // Save to db
            //                                    TimeSpan? dbinLogTime = inLogTime;
            //                                    TimeSpan? dboutLogTime = outLogTime;
            //                                    // End

            //                                    // If the start time is before the starting hours, set it to the starting hour.
            //                                    if (inLogTime < startWorkingScheduleTime) inLogTime = startWorkingScheduleTime;
            //                                    if (outLogTime > endWorkingScheduleTime) outLogTime = endWorkingScheduleTime;

            //                                    workTime = (outLogTime - inLogTime) - lunch;
            //                                    workDay = Convert.ToDecimal(workTime.TotalHours) / workingScheduleHour;

            //                                    if (inLogTime > startWorkingScheduleTime)
            //                                    {
            //                                        late = inLogTime - startWorkingScheduleTime;
            //                                        status = 0;
            //                                    }
            //                                    if (outLogTime < endWorkingScheduleTime)
            //                                    {
            //                                        early = endWorkingScheduleTime - outLogTime;
            //                                        status = 0;
            //                                    }

            //                                    #region Check miss in/out
            //                                    var workingArr = new int[] { startWorkingScheduleTime.Hours, endWorkingScheduleTime.Hours };
            //                                    var incheck = workingArr.ClosestTo(dbinLogTime.Value.Hours);
            //                                    var outcheck = workingArr.ClosestTo(dboutLogTime.Value.Hours);
            //                                    if (incheck == outcheck)
            //                                    {
            //                                        if (incheck == endWorkingScheduleTime.Hours)
            //                                        {
            //                                            // missing in
            //                                            dbinLogTime = null;
            //                                            late = new TimeSpan(0);
            //                                            early = endWorkingScheduleTime - outLogTime;
            //                                            workTime = TimeSpan.FromHours(4) - early;
            //                                        }
            //                                        else
            //                                        {
            //                                            // missing out
            //                                            dboutLogTime = null;
            //                                            early = new TimeSpan(0);
            //                                            late = inLogTime - startWorkingScheduleTime;
            //                                            workTime = TimeSpan.FromHours(4) - late;
            //                                        }

            //                                        status = 0;

            //                                        workDay = Convert.ToDecimal(workTime.TotalHours) / workingScheduleHour;
            //                                    }
            //                                    #endregion

            //                                    var employeeWorkTimeLog = new EmployeeWorkTimeLog
            //                                    {
            //                                        EnrollNumber = times[0].EnrollNumber,
            //                                        VerifyMode = times[0].VerifyMode,
            //                                        InOutMode = times[0].InOutMode,
            //                                        Workcode = times[0].Workcode,
            //                                        WorkplaceCode = "NM",
            //                                        Date = dateFinger,
            //                                        In = dbinLogTime,
            //                                        Out = dboutLogTime,
            //                                        Start = startWorkingScheduleTime,
            //                                        End = endWorkingScheduleTime,
            //                                        WorkTime = workTime,
            //                                        WorkDay = workDay,
            //                                        Late = late,
            //                                        Early = early,
            //                                        Status = status,
            //                                        Logs = times
            //                                    };
            //                                    employeeWorkTimeLogs.Add(employeeWorkTimeLog);
            //                                }
            //                                try
            //                                {
            //                                    // Insert if no exist
            //                                    foreach (var item in employeeWorkTimeLogs)
            //                                    {
            //                                        // check db current
            //                                        if (dbContext.EmployeeWorkTimeLogs.Find(m => m.EnrollNumber.Equals(item.EnrollNumber) && m.Date.Equals(item.Date) && m.In.Equals(item.In) && m.Out.Equals(item.Out)).Count() == 0)
            //                                        {
            //                                            if (dbContext.EmployeeWorkTimeLogs.Find(m => m.EnrollNumber.Equals(item.EnrollNumber) && m.Date.Equals(item.Date)).Count() > 0)
            //                                            {
            //                                                var builderUpdate = Builders<EmployeeWorkTimeLog>.Filter;
            //                                                var filterUpdate = builderUpdate.Eq(m => m.EnrollNumber, item.EnrollNumber);
            //                                                filterUpdate = filterUpdate & builderUpdate.Gt(m => m.Date, dateProcess);

            //                                                var update = Builders<EmployeeWorkTimeLog>.Update
            //                                                    .Set(m => m.In, item.In)
            //                                                    .Set(m => m.Out, item.Out)
            //                                                    .Set(m => m.WorkTime, item.WorkTime)
            //                                                    .Set(m => m.WorkDay, item.WorkDay)
            //                                                    .Set(m => m.Late, item.Late)
            //                                                    .Set(m => m.Early, item.Early)
            //                                                    .Set(m => m.Status, item.Status)
            //                                                    .Set(m => m.Logs, item.Logs)
            //                                                    .Set(m => m.UpdatedOn, DateTime.Now);
            //                                                dbContext.EmployeeWorkTimeLogs.UpdateOne(filterUpdate, update);
            //                                            }
            //                                            else
            //                                            {
            //                                                dbContext.EmployeeWorkTimeLogs.InsertOne(item);
            //                                            }
            //                                        }
            //                                    }
            //                                }
            //                                catch (Exception ex)
            //                                {
            //                                    Console.WriteLine(ex);
            //                                }
            //                                #endregion
            //                            }
            //                            break;
            //                        }
            //                }
            //            }
            //        }
            //    }
            //}
            #endregion
        }
    }
}

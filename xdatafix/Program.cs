using Common.Enums;
using Common.Utilities;
using Data;
using Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace xdatafix
{
    class Program
    {
        static void Main(string[] args)
        {
            #region setting
            var connection = ConfigurationSettings.AppSettings.Get("connection").ToString();
            var database = ConfigurationSettings.AppSettings.Get("database").ToString();
            #endregion

            Console.Write("Insert Setting!");
            InsertSetting(connection, database);
            //UpdateRight(connection, database);
            //MoveHoliday(connection, database);
            //MoveLeaveType(connection, database);

            Console.Write("\r\n");
            Console.Write("Done..... Press any key to exist!");
            Console.ReadLine();
        }

        #region ERP
        static void InsertSetting(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            Console.Write("Insert Setting...");
            var setting = new Setting()
            {
                Type = (int)EData.Setting,
                Key = "crawl-day-start",
                Value = ""
            };
            Console.Write($"Begin: {setting.Key} value {setting.Value}");
            dbContext.Settings.InsertOne(setting);
            Console.Write($"Completed: {setting.Key} value {setting.Value}");

            setting = new Setting()
            {
                Type = (int)EData.Setting,
                Key = "crawl-day-end",
                Value = ""
            };
            Console.Write($"Begin: {setting.Key} value {setting.Value}");
            dbContext.Settings.InsertOne(setting);
            Console.Write($"Completed: {setting.Key} value {setting.Value}");

            setting = new Setting()
            {
                Type = (int)EData.Setting,
                Key = "timer-month-calculator",
                Value = "0"
            };
            Console.Write($"Begin: {setting.Key} value {setting.Value}");
            dbContext.Settings.InsertOne(setting);
            Console.Write($"Completed: {setting.Key} value {setting.Value}");
        }

        static void UpdateRight(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var rightsAnh = dbContext.Rights.Find(m => m.ObjectId.Equals("5b6bb22fe73a301f941c5887")).ToList();
            foreach (var item in rightsAnh)
            {
                item.Id = null;
                item.ObjectId = "5e5cd1f13942211a4c10224d";
                dbContext.Rights.InsertOne(item);
            }
        }

        static void MoveHoliday(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            #region FIX DATA
            dbContext.Holidays.InsertOne(new Holiday()
            {
                Date = new DateTime(2020, 1, 23),
                Name = "Tết Nguyên đán",
                Detail = "Tết Nguyên đán nghỉ ngày 23/01/2020 - 29/01/2020"
            });
            dbContext.Holidays.InsertOne(new Holiday()
            {
                Date = new DateTime(2020, 1, 24),
                Name = "Tết Nguyên đán",
                Detail = "Tết Nguyên đán nghỉ ngày 23/01/2020 - 29/01/2020"
            });
            dbContext.Holidays.InsertOne(new Holiday()
            {
                Date = new DateTime(2020, 1, 25),
                Name = "Tết Nguyên đán",
                Detail = "Tết Nguyên đán nghỉ ngày 23/01/2020 - 29/01/2020"
            });
            dbContext.Holidays.InsertOne(new Holiday()
            {
                Date = new DateTime(2020, 1, 27),
                Name = "Tết Nguyên đán",
                Detail = "Tết Nguyên đán nghỉ ngày 23/01/2020 - 29/01/2020"
            });
            dbContext.Holidays.InsertOne(new Holiday()
            {
                Date = new DateTime(2020, 1, 28),
                Name = "Tết Nguyên đán",
                Detail = "Tết Nguyên đán nghỉ ngày 23/01/2020 - 29/01/2020"
            });
            dbContext.Holidays.InsertOne(new Holiday()
            {
                Date = new DateTime(2020, 1, 29),
                Name = "Tết Nguyên đán",
                Detail = "Tết Nguyên đán nghỉ ngày 23/01/2020 - 29/01/2020"
            });

            //var builderT = Builders<EmployeeWorkTimeLog>.Filter;
            //var filterT = builderT.Gte(m => m.Date, new DateTime(2020, 1, 3))
            //            & builderT.Lte(m => m.Date, new DateTime(2020, 1, 3, 23, 59, 59));
                       

            ////var tests = dbContext.EmployeeWorkTimeLogs.Find(filterT).ToList();

            //var updateT = Builders<EmployeeWorkTimeLog>.Update
            //    .Set(m => m.Status, (int)EStatusWork.DongY)
            //    .Set(m => m.StatusEarly, (int)EStatusWork.DuCong)
            //    .Set(m => m.WorkDay, 1)
            //    .Set(m => m.Reason, "Đi công tác")
            //    .Set(m => m.ReasonDetail, "Tất niên công ty")
            //    .Set(m => m.ConfirmDate, DateTime.Now.Date);
            //dbContext.EmployeeWorkTimeLogs.UpdateMany(filterT, updateT);
            #endregion

            
            dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.Holiday));
            var list = dbContext.Holidays.Find(m => m.Enable.Equals(true)).ToList();
            var i = 1;
            foreach (var item in list)
            {
                dbContext.Categories.InsertOne(new Category()
                {
                    Type = (int)ECategory.Holiday,
                    Name = item.Name,
                    Alias = Utility.AliasConvert(item.Name),
                    Value = item.Date.ToString(),
                    Description = item.Detail,
                    Code = i.ToString(),
                    CodeInt = i
                });
                i++;
            }
        }

        static void MoveLeaveType(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var list = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true)).ToList();
            var i = 1;
            foreach (var item in list)
            {
                var properties = new List<Property>
                {
                    new Property
                    {
                        Type = (int)EData.Property,
                        Key = "YearMax",
                        Value = item.YearMax.ToString(),
                        ValueType = (int)EValueType.Double
                    },
                    new Property
                    {
                        Type = (int)EData.Property,
                        Key = "MonthMax",
                        Value = item.MonthMax.ToString(),
                        ValueType = (int)EValueType.Double
                    },
                    new Property
                    {
                        Type = (int)EData.Property,
                        Key = "MaxOnce",
                        Value = item.YearMax.ToString(),
                        ValueType = (int)EValueType.Double
                    },
                    new Property
                    {
                        Type = (int)EData.Property,
                        Key = "SalaryPay",
                        Value = item.SalaryPay.ToString(),
                        ValueType = (int)EValueType.Bool
                    },
                    new Property
                    {
                        Type = (int)EData.Property,
                        Key = "Display",
                        Value = item.Display.ToString(),
                        ValueType = (int)EValueType.Bool
                    },
                };
                dbContext.Categories.InsertOne(new Category()
                {
                    Type = (int)ECategory.LeaveType,
                    Name = item.Name,
                    Alias = Utility.AliasConvert(item.Name),
                    Description = item.Description,
                    Properties = properties,
                    Code = i.ToString(),
                    CodeInt = i
                });
                i++;
            }
        }

        static void UpdateTimeUser(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var missing = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)).ToList();

            foreach (var item in missing)
            {
                var workplaces = new List<Workplace>();
                if (item.Workplaces != null && item.Workplaces.Count > 0)
                {
                    foreach (var wo in item.Workplaces)
                    {
                        if (!string.IsNullOrEmpty(wo.Fingerprint))
                        {
                            if (wo.Code == "NM")
                            {
                                wo.WorkingScheduleTime = "07:30-16:30";
                            }
                            else if (wo.Code == "VP")
                            {
                                wo.WorkingScheduleTime = "08:00-17:00";
                            }
                            workplaces.Add(wo);
                        }
                    }
                }

                if (item.Workplaces == null || item.Workplaces.Count == 0)
                {
                    workplaces = null;
                }

                var filter = Builders<Employee>.Filter.Eq(m => m.Id, item.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.Workplaces, workplaces);
                dbContext.Employees.UpdateOne(filter, update);
            }
        }
        #endregion
    }
}

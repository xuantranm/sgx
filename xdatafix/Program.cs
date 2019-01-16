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

namespace xdatafix
{
    class Program
    {
        static void Main(string[] args)
        {
            #region setting
            //var location = ConfigurationSettings.AppSettings.Get("location").ToString();
            //var modeData = ConfigurationSettings.AppSettings.Get("modeData").ToString() == "true" ? true : false; // true: Get all data | false get by date
            //var day = Convert.ToInt32(ConfigurationSettings.AppSettings.Get("day").ToString());
            //var isMail = ConfigurationSettings.AppSettings.Get("isMail").ToString() == "true" ? true : false;
            var connection = "mongodb://localhost:27017";
            var database = "tribat";
            #endregion

            UpdateEmployeeDepartmentAlias(connection, database);
            //UpdateTimekeepingCode(connection, database);
            //AddHoliday(connection, database);
            //UpdateTetTay(connection, database);
            //UpdateLocationTimer(connection, database);
            //UpdateUpperCaseTimer(connection, database);
        }


        static void UpdateLocationTimer(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true)).ToList();
            foreach(var employee in employees)
            {
                if (employee.Workplaces == null || employee.Workplaces.Count == 0)
                {
                    continue;
                }

                foreach (var workplace in employee.Workplaces)
                {
                    if (!string.IsNullOrEmpty(workplace.Fingerprint))
                    {
                        int workcode = employee.SalaryType;

                        var builder = Builders<EmployeeWorkTimeLog>.Filter;
                        var filter = builder.Eq(m => m.EnrollNumber, workplace.Fingerprint) & builder.Eq(m => m.WorkplaceCode, workplace.Code);

                        var update = Builders<EmployeeWorkTimeLog>.Update
                            .Set(m => m.Workcode, workcode);
                        dbContext.EmployeeWorkTimeLogs.UpdateMany(filter, update);
                    }
                }
            }
        }

        static void UpdateUpperCaseTimer(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var datas = dbContext.Employees.Find(m => m.Enable.Equals(true)).ToList();
            foreach (var data in datas)
            {
                var builder = Builders<EmployeeWorkTimeLog>.Filter;
                var filter = builder.Eq(m => m.EmployeeId, data.Id);

                var update = Builders<EmployeeWorkTimeLog>.Update
                    .Set(m => m.EmployeeTitle, data.Title.ToUpper())
                    .Set(m => m.Part, data.Part.ToUpper())
                    .Set(m => m.Department, data.Department.ToUpper());
                dbContext.EmployeeWorkTimeLogs.UpdateMany(filter, update);
            }
        }

        static void UpdateTetTay(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var datas = dbContext.Employees.Find(m => m.Enable.Equals(true)).ToList();
            foreach (var data in datas)
            {
                var builder = Builders<EmployeeWorkTimeLog>.Filter;
                var filter = builder.Eq(m => m.EmployeeId, data.Id);

                var update = Builders<EmployeeWorkTimeLog>.Update
                    .Set(m => m.EmployeeTitle, data.Title.ToUpper())
                    .Set(m => m.Part, data.Part.ToUpper())
                    .Set(m => m.Department, data.Department.ToUpper());
                dbContext.EmployeeWorkTimeLogs.UpdateMany(filter, update);
            }
        }

        static void AddHoliday(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            dbContext.Holidays.InsertOne(new Holiday
            {
                Date = new DateTime(2019, 1, 1),
                Name = "Tết tây",
                Detail = "Nghỉ lễ 1 ngày"
            });
        }

        static void UpdateTimekeepingCode(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var times = dbContext.EmployeeWorkTimeLogs.Find(m => true).ToList();
            foreach(var time in times)
            {
                Console.WriteLine("Date: " + time.Date + ", fingerCode: " + time.EnrollNumber);
                var builder = Builders<EmployeeWorkTimeLog>.Filter;
                var filter = builder.Eq(m => m.Id, time.Id);

                var update = Builders<EmployeeWorkTimeLog>.Update
                    .Set(m => m.EnrollNumber, Convert.ToInt32(time.EnrollNumber).ToString("000"));
                dbContext.EmployeeWorkTimeLogs.UpdateMany(filter, update);
            }
        }

        static void UpdateEmployeeDepartmentAlias(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            
            var departments = dbContext.Departments.Find(m => m.Enable.Equals(true)).ToList();

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true)).ToList();
            foreach (var employee in employees)
            {
                var department = !string.IsNullOrEmpty(employee.Department) ? employee.Department.ToUpper() : string.Empty;
                var departmentId = string.Empty;
                var departmentAlias = string.Empty;

                if (!string.IsNullOrEmpty(employee.Department))
                {
                    var departmentItem = departments.Where(m => m.Name.Equals(department)).FirstOrDefault();
                    if (departmentItem != null)
                    {
                        departmentId = departmentItem.Id;
                        departmentAlias = departmentItem.Alias;
                    }
                }

                var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.DepartmentId, departmentId)
                    .Set(m => m.DepartmentAlias, departmentAlias);
                dbContext.Employees.UpdateOne(filter, update);
            }
        }
    }
}

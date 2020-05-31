using Common.Enums;
using Common.Utilities;
using Data;
using Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Configuration;


namespace xtimerstatusauto
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
            var dateCrawled = today.Day > 25 ? new DateTime(today.Year, today.Month, 26) : new DateTime(today.AddMonths(-1).Year, today.AddMonths(-1).Month, 26);
            if (monthConfig < 0)
            {
                dateCrawled = dateCrawled.AddMonths(monthConfig);
            }
            var builder = Builders<AttLog>.Filter;
            var filter = builder.Gte(m => m.Date, dateCrawled);

            if (!string.IsNullOrEmpty(debug))
            {
                filter &= builder.Eq(m => m.EnrollNumber, debug);
            }
            #endregion

            var now = DateTime.Now;
            var filterEmp = Builders<Employee>.Filter.Eq(m => m.Enable, true) & Builders<Employee>.Filter.Eq(m => m.Leave, false);
            var employees = dbContext.Employees.Find(filterEmp).ToList();
            foreach (var employee in employees)
            {
                Console.WriteLine("Employee Name: " + employee.FullName);
                


            }
                // Update status/workday status:2

                // Run Sum

            }
    }
}

using Common.Utilities;
using Data;
using Models;
using MongoDB.Driver;
using System;
using System.Configuration;
using System.Linq;

namespace xleave
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Connection, Setting & Filter
            var debug = ConfigurationSettings.AppSettings.Get("debugString").ToString();
            var connection = ConfigurationSettings.AppSettings.Get("connection").ToString();
            var database = ConfigurationSettings.AppSettings.Get("database").ToString();
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            var url = Constants.System.domain;
            #endregion

            // ngày 01 hàng tháng run.
            var now = DateTime.Now.Date;
            var month = now.Month;
            var year = now.Year;
            var endDateMonth = now.AddDays(-1);

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = !builder.Eq(i => i.UserName, Constants.System.account)
                        & builder.Eq(m => m.Enable, true)
                        & builder.Eq(m => m.IsOnline, true);
            if (!string.IsNullOrEmpty(debug))
            {
                filter &= builder.Eq(m => m.Id, debug);
            }
            #endregion

            var employees = dbContext.Employees.Find(filter).ToList();
            var leaveType = dbContext.LeaveTypes.Find(m => m.Alias.Equals("phep-nam")).FirstOrDefault();
            var leaveTypeId = leaveType.Id;
            foreach (var employee in employees)
            {
                // default, normal employee
                double numLeave = 1;
                var description = "Cộng từng tháng: " + numLeave + " ngày;";
                var employeeId = employee.Id;
                var thamnienlamviec = employee.Joinday; // Included probation
                var bacphep = employee.LeaveLevelYear;

                var hesophep = Convert.ToInt32(bacphep - 12);
                if (hesophep > 0)
                {
                    // Cộng mức phép năm đầu quí.
                    // Theo ngay vào làm || dau nam.
                    var dateApplyHeSo = thamnienlamviec; // new DateTime(year, 01, 01)
                    // Theo ngay vao lam.
                    // 2 : chia cho 6 thang | 4 : chia cho 3 thang
                    for (int i = 0; i < hesophep; i++)
                    {
                        var xetmucphep = dateApplyHeSo.AddMonths(12 / hesophep * i);
                        var thangxetmucphep = dateApplyHeSo.AddMonths((12 / hesophep) * i).Month;
                        if (thangxetmucphep == month)
                        {
                            numLeave += 1;
                            description = "Cộng mức phép: 1 ngày;";
                        }
                    }
                }

                // Rule tham nien: 5 năm + 1 ngày phép & áp dụng vào đầu tháng
                var ngaythamnien = (endDateMonth - thamnienlamviec).TotalDays;
                double thangthamnien = Math.Round(ngaythamnien / 30, 0);
                double namthamnien = Math.Round(thangthamnien / 12, 0);
                var ngayphepthamnien = 0;
                if (namthamnien >= 5)
                {
                    for (int i = 5; i <= namthamnien; i += 5)
                    {
                        ngayphepthamnien++;
                    }
                }

                if (ngayphepthamnien > 0)
                {
                    if (thamnienlamviec.Month == month)
                    {
                        numLeave += ngayphepthamnien;
                        description = "Cộng thâm niên: " + ngayphepthamnien + " ngày;";
                    }
                }
                // Probation
                // Use UseFlag in [LeaveEmployees]
                // probation default 2 months
                var useFlag = true;
                var probationEndDate = thamnienlamviec.AddMonths(2);
                if (now < probationEndDate)
                {
                    useFlag = false;
                }
                // Check exist leaveEmployee
                var leaveE = dbContext.LeaveEmployees.Find(m => m.EmployeeId.Equals(employeeId)).FirstOrDefault();
                if (leaveE == null)
                {
                    var leaveEmployeeNew = new LeaveEmployee()
                    {
                        LeaveTypeId = leaveTypeId,
                        EmployeeId = employeeId,
                        LeaveTypeName = leaveType.Name,
                        EmployeeName = employee.FullName,
                        Number = numLeave,
                        Department = employee.PhongBanName,
                        Part = employee.BoPhanName,
                        Title = employee.ChucVuName,
                        LeaveLevel = Convert.ToDouble(employee.LeaveLevelYear),
                        NumberUsed = 0,
                        UseFlag = useFlag,
                        Year = DateTime.Now.Year
                    };
                    dbContext.LeaveEmployees.InsertOne(leaveEmployeeNew);
                    dbContext.LeaveEmployeeHistories.InsertOne(new LeaveEmployeeHistory
                    {
                        EmployeeId = employeeId,
                        LeaveTypeId = leaveTypeId,
                        Current = 0,
                        Change = numLeave,
                        Month = month,
                        Year = year,
                        Description = description
                    });
                }
                else
                {
                    // No update if updated
                    var updated = dbContext.LeaveEmployeeHistories.CountDocuments(m => m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)) > 0 ? false : true;
                    if (updated)
                    {
                        double currentLeaveNum = 0;
                        var currentLeave = dbContext.LeaveEmployees.Find(m => m.EmployeeId.Equals(employeeId) && m.LeaveTypeId.Equals(leaveTypeId)).FirstOrDefault();
                        if (currentLeave != null)
                        {
                            currentLeaveNum = currentLeave.Number;
                        }

                        // Missing here, if no LeaveEmployee ??
                        // Update LeaveEmployees
                        var filterLeaveEmployee = Builders<LeaveEmployee>.Filter.Eq(m => m.EmployeeId, employeeId);
                        var updateLeaveEmployee = Builders<LeaveEmployee>.Update
                            .Set(m => m.UseFlag, useFlag)
                            .Inc(m => m.Number, numLeave)
                            .Set(m => m.UpdatedOn, now);
                        dbContext.LeaveEmployees.UpdateOne(filterLeaveEmployee, updateLeaveEmployee);
                        //Add LeaveEmployeeHistories
                        dbContext.LeaveEmployeeHistories.InsertOne(new LeaveEmployeeHistory
                        {
                            EmployeeId = employeeId,
                            LeaveTypeId = leaveTypeId,
                            Current = currentLeaveNum,
                            Change = numLeave,
                            Month = month,
                            Year = year,
                            Description = description
                        });
                    }
                }
            }
        }
    }
}

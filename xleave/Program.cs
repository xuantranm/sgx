using Common.Utilities;
using Data;
using MimeKit;
using MimeKit.Text;
using Models;
using MongoDB.Driver;
using Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xleave
{
    class Program
    {
        static void Main(string[] args)
        {
            UpdateLeave();
        }

        static void UpdateLeave()
        {
            #region Connection
            //var connectString = "mongodb://192.168.2.223:27017";
            var connectString = "mongodb://localhost:27017";
            MongoDBContext.ConnectionString = connectString;
            MongoDBContext.DatabaseName = "tribat";
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            // ngày 01 hàng tháng run.
            var now = DateTime.Now.Date;
            var month = now.Month;
            var year = now.Year;
            var endDateMonth = now.AddDays(-1);

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true)).ToList();
            // UAT
            //employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.UserName.Equals("xuan.tm")).ToList();
            // End UAT
            var leaveType = dbContext.LeaveTypes.Find(m => m.Alias.Equals("phep-nam")).FirstOrDefault();
            var leaveTypeId = leaveType.Id;
            foreach (var employee in employees)
            {
                // UAT
                //employee.Joinday = new DateTime(2018, 10, 2);
                //employee.LeaveLevelYear = 16;
                // End UAT

                // default, normal employee
                decimal numLeave = 1;
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
                        var thangxetmucphep = dateApplyHeSo.AddMonths((12/ hesophep) * i).Month;
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
                // No update if updated
                if (dbContext.LeaveEmployeeHistories.CountDocuments(m => m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)) == 0)
                {
                    decimal currentLeaveNum = 0;
                    var currentLeave = dbContext.LeaveEmployees.Find(m => m.EmployeeId.Equals(employeeId) && m.LeaveTypeId.Equals(leaveTypeId)).FirstOrDefault();
                    if (currentLeave != null)
                    {
                        currentLeaveNum = currentLeave.Number;
                    }

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

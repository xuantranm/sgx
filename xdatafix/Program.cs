using Common.Enums;
using Common.Utilities;
using Data;
using Microsoft.Office.Interop.Excel;
using MimeKit;
using Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Constants = Common.Utilities.Constants;

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

            //UpdateLeave(connection, database);

            UpdateChucVuEmployee(connection, database);

            //UpdateManager(connection, database);

            //DeleteEmailNull(connection, database);

            //UpdateLeave29(connection, database);
            //FixTimeKeeper(connection, database);

            //UpdateEMailError(connection, database);
            //UpdateThangLuongVP(connection, database);
            //UpdateLevelVP(connection, database);

            //UpdateLeaveDay2NM(connection, database);
            //UpdateLeaveDayMissing(connection, database);

            //FixEmployeeTimeKeeper(connection, database);
            //FixEmployeeContractDay(connection, database);
            //FixEmployeeNewStructure(connection, database);

            //FixEmployeeOldCode(connection, database);
            //FixEmployeeTimer(connection, database);

            //UpdateLeaveDay(connection, database);
            //InitSetting(connection, database);
            //FixTimer(connection, database);
            //UpdateEmployeeCode(connection, database);
            //FixEmailLeave(connection, database);
            //FixEmployeeLeave(connection, database);

            //UpdateEmployeeStructure(connection, database);
            //FixStructure(connection, database);
            //FixStructureInitBP(connection, database);
            //FixEmployeeData(connection, database);
            //UpdateEmployeeDepartmentAlias(connection, database);
            //UpdateTimerDepartmentAlias(connection, database);
            //UpdateTimekeepingCode(connection, database);
            //AddHoliday(connection, database);
            //UpdateTetTay(connection, database);
            //UpdateLocationTimer(connection, database);
            //UpdateUpperCaseTimer(connection, database);

            #region Tribatvn

            #endregion

            #region Factories: Init data

            //InitFactoryProductDinhMucTangCa(connection, database);
            //InitFactoryProductDinhMucTiLe(connection, database);
            //InitFactoryProductDonGiaM3(connection, database);
            //InitFactoryProductDinhMuc(connection, database);
            //InitFactoryCongViec(connection, database);
            #endregion

            #region Factories: Update data

            #endregion

            Console.Write("\r\n");
            Console.Write("Done..... Press any key to exist!");
            Console.ReadLine();
        }

        #region ERP

        static void UpdateLeave(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var filter = Builders<LeaveEmployee>.Filter.Gt(m => m.Number, 10);
            var update = Builders<LeaveEmployee>.Update
                .Set(m => m.Number, 7);
            dbContext.LeaveEmployees.UpdateMany(filter, update);
        }

        static void UpdateChucVuEmployee(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var chucvus = dbContext.ChucVus.Find(m => true).ToList();
            foreach (var item in chucvus)
            {
                var employee = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.ChucVu.Equals(item.Id)).FirstOrDefault();
                if (employee != null && !string.IsNullOrEmpty(employee.FullName))
                {
                    var filter = Builders<ChucVu>.Filter.Eq(m => m.Id, item.Id);
                    var update = Builders<ChucVu>.Update
                        .Set(m => m.Employee, employee.FullName);
                    dbContext.ChucVus.UpdateOne(filter, update);
                }
            }
        }

        static void UpdateManager(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var employees = dbContext.Employees.Find(m => true).ToList();
            foreach (var item in employees)
            {
                if (!string.IsNullOrEmpty(item.ManagerId))
                {
                    var managerE = dbContext.Employees.Find(m => m.Id.Equals(item.ManagerId)).FirstOrDefault();
                    if (managerE != null)
                    {
                        var chucvuId = managerE.ChucVu;
                        if (!string.IsNullOrEmpty(chucvuId))
                        {
                            var chucvuE = dbContext.ChucVus.Find(m => m.Id.Equals(chucvuId)).FirstOrDefault();
                            if (chucvuE != null)
                            {
                                var filter = Builders<Employee>.Filter.Eq(m => m.Id, item.Id);
                                var update = Builders<Employee>.Update
                                    .Set(m => m.ManagerId, chucvuId)
                                    .Set(m => m.ManagerInformation, chucvuE.Name);
                                dbContext.Employees.UpdateOne(filter, update);
                            }
                        }
                    }
                }
            }
        }

        static void DeleteEmailNull(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var builder = Builders<ScheduleEmail>.Filter;
            var filter = builder.Eq(m => m.Status, 4) & builder.Eq(m => m.EmployeeId, "5b6bb231e73a301f941c58ec");
            dbContext.ScheduleEmails.DeleteMany(filter);
        }


        static void UpdateLeave29(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var timeoff = new DateTime(2019, 4, 29);
            var builder = Builders<EmployeeWorkTimeLog>.Filter;
            var filter = builder.Eq(m => m.Date, timeoff)
                        & builder.Eq(m => m.Logs, null);
            var times = dbContext.EmployeeWorkTimeLogs.Find(filter).ToList();
            var update = Builders<EmployeeWorkTimeLog>.Update
               .Set(m=>m.Mode, (int)ETimeWork.LeavePhep)
               .Set(m=>m.SoNgayNghi, 1)
               .Set(m => m.Reason, "Phép năm")
               .Set(m => m.ReasonDetail, "Duyệt tự động. Nghỉ phép năm lễ 30/4.")
               .Set(m => m.Status, (int)EStatusWork.DuCong)
               .Set(m=>m.WorkDay, 1);
            dbContext.EmployeeWorkTimeLogs.UpdateMany(filter, update);
            foreach (var item in times)
            {
                // Check create leave or no
                var existLeave = dbContext.Leaves.Find(m => m.EmployeeId.Equals(item.EmployeeId)
                                && m.From >= timeoff && m.To <= timeoff.AddDays(1)).FirstOrDefault();
                if (existLeave == null)
                {
                    // Update => nghi phep nam in month employee
                    var builderM = Builders<EmployeeWorkTimeMonthLog>.Filter;
                    var filterM = builderM.Eq(m => m.Id, item.EmployeeId)
                                    & builderM.Eq(m => m.Month, item.Month)
                                    & builderM.Eq(m => m.Year, item.Year)
                                    & builderM.Eq(m => m.WorkplaceCode, item.WorkplaceCode);
                    var updateM = Builders<EmployeeWorkTimeMonthLog>.Update
                        .Inc(m => m.NghiPhepNam, 1);
                    dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterM, updateM);
                    // Tao nghỉ phép, approve auto
                    var leave = new Leave()
                    {
                        TypeId = "5bbdb5a97caedd0c7411c89d",
                        TypeName = "Phép năm",
                        Salary = true,
                        EmployeeId = item.EmployeeId,
                        EmployeeName = item.EmployeeName,
                        From = timeoff.Add(item.Start),
                        Start = item.Start,
                        To = timeoff.Add(item.End),
                        End = item.End,
                        WorkingScheduleTime = item.Start + "-" + item.End,
                        Number = 1,
                        Reason = "Nghỉ phép năm lễ 30/4",
                        ApproverName = "erp-hcns"
                    };

                    // Tru phep nam
                    var builderLE = Builders<LeaveEmployee>.Filter;
                    var filterLE = builderLE.Eq(m => m.EmployeeId, item.EmployeeId);
                    var updateLE = Builders<LeaveEmployee>.Update
                        .Inc(m => m.Number, -1)
                        .Inc(m => m.NumberUsed, 1);
                    dbContext.LeaveEmployees.UpdateOne(filterLE, updateLE);
                }
            }
        }

        static void FixTimeKeeper(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var timeerror = new DateTime(2019, 5, 24).AddHours(7);
            var enderror = timeerror.AddHours(3);
            var builder = Builders<AttLog>.Filter;
            var filter = builder.Gte(m => m.Date, timeerror)
                        & builder.Lte(m => m.Date, enderror);
            var times = dbContext.X928CNMAttLogs.Find(filter).ToList();

            foreach(var item in times)
            {
                var newTime = item;
                newTime.Id = null;
                newTime.Date = item.Date.AddHours(-1);
                dbContext.X928CNMAttLogs.InsertOne(newTime);
            }
            //var builder = Builders<ScheduleEmail>.Filter;
            //var filter = builder.Eq(m => m.Error, "Sai định dạng mail");
            //dbContext.ScheduleEmails.DeleteMany(filter);
        }

        static void UpdateEMailError(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var builder = Builders<ScheduleEmail>.Filter;
            var filter = builder.Eq(m => m.Status, (int)EEmailStatus.Ok)
                        & !builder.Eq(m => m.Type, "birthday")
                        & builder.Gte(m => m.CreatedOn, new DateTime(2019, 5, 11));
            var update = Builders<ScheduleEmail>.Update
                .Set(m => m.Status, 4);
            dbContext.ScheduleEmails.UpdateMany(filter, update);

            //var builder = Builders<ScheduleEmail>.Filter;
            //var filter = builder.Eq(m => m.Error, "Sai định dạng mail");
            //dbContext.ScheduleEmails.DeleteMany(filter);
        }

        // NO USE
        static void InitNgachLuong2(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            dbContext.SalaryThangBangLuongs.DeleteMany(m => m.Enable.Equals(true));
            // default muc luong = toi thieu, HR update later.
            decimal salaryMin = 3980000 * (decimal)1.07; // use reset
            decimal salaryDeclareTax = salaryMin;
            // Company no use now. sử dụng từng vị trí đặc thù. Hi vong tương lai áp dụng.
            decimal salaryReal = salaryDeclareTax; // First set real salary default, update later

            var name = string.Empty;
            var nameAlias = string.Empty;
            var maso = string.Empty;
            var typeRole = string.Empty;
            var typeRoleAlias = string.Empty;
            var typeRoleCode = string.Empty;

            #region 1- BẢNG LƯƠNG CHỨC VỤ QUẢN LÝ DOANH NGHIỆP
            typeRole = "CHỨC VỤ QUẢN LÝ DOANH NGHIỆP";
            typeRoleAlias = Utility.AliasConvert(typeRole);
            typeRoleCode = "C";
            // 01- TỔNG GIÁM ĐỐC 
            name = "TỔNG GIÁM ĐỐC";
            maso = "C.01";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                double heso = 1;
                if (i > 1)
                {
                    heso = 1.05;
                }
                salaryDeclareTax = Convert.ToDecimal((double)salaryDeclareTax * heso);
                salaryReal = Convert.ToDecimal((double)salaryReal * heso);
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 02-GIÁM ĐỐC/TRƯỞNG BAN
            name = "GIÁM ĐỐC/TRƯỞNG BAN";
            maso = "C.02";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                double heso = 1;
                if (i > 0)
                {
                    heso = 1.05;
                    if (i == 1)
                    {
                        heso = 1.8;
                    }
                }
                salaryDeclareTax = Convert.ToDecimal((double)salaryDeclareTax * heso);
                salaryReal = Convert.ToDecimal((double)salaryReal * heso);
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 03- KẾ TOÁN TRƯỞNG/ PHÓ GIÁM ĐỐC
            name = "KẾ TOÁN TRƯỞNG/ PHÓ GIÁM ĐỐC";
            maso = "C.03";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                double heso = 1;
                if (i > 0)
                {
                    heso = (double)1.05;
                    if (i == 1)
                    {
                        heso = (double)1.7;
                    }
                }
                salaryDeclareTax = Convert.ToDecimal((double)salaryDeclareTax * heso);
                salaryReal = Convert.ToDecimal((double)salaryReal * heso);
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            #endregion

            #region 2- BẢNG LƯƠNG VIÊN CHỨC CHUYÊN MÔN, THỪA HÀNH, PHỤC VỤ
            typeRole = "VIÊN CHỨC CHUYÊN MÔN, THỪA HÀNH, PHỤC VỤ";
            typeRoleAlias = Utility.AliasConvert(typeRole);
            typeRoleCode = "D";
            // 01- TRƯỞNG BP THIẾT KẾ, TRƯỞNG BP GS KT, KẾ TOÁN TỔNG HỢP, QUẢN LÝ THUẾ…
            name = "TRƯỞNG BP THIẾT KẾ, TRƯỞNG BP GS KT, KẾ TOÁN TỔNG HỢP, QUẢN LÝ THUẾ…";
            maso = "D.01";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                double heso = 1;
                if (i > 1)
                {
                    heso = 1.05;
                }
                salaryDeclareTax = Convert.ToDecimal((double)salaryDeclareTax * heso);
                salaryReal = Convert.ToDecimal((double)salaryReal * heso);
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 02- TRƯỞNG SALE, NHÂN VIÊN NHÂN SỰ, CV THU MUA, CV PHÁP CHẾ, CV KẾ HOẠCH TỔNG HỢP, CV MÔI TRƯỜNG, CV NC KHCN, CV PHÒNG TN….
            name = "TRƯỞNG SALE, NHÂN VIÊN NHÂN SỰ, CV THU MUA, CV PHÁP CHẾ, CV KẾ HOẠCH TỔNG HỢP, CV MÔI TRƯỜNG, CV NC KHCN, CV PHÒNG TN…";
            maso = "D.02";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                double heso = 1;
                if (i > 1)
                {
                    heso = 1.05;
                }
                salaryDeclareTax = Convert.ToDecimal((double)salaryDeclareTax * heso);
                salaryReal = Convert.ToDecimal((double)salaryReal * heso);
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            // 03- NV KINH DOANH, SALE ADMIN, NV HÀNH CHÍNH, NV KẾ TOÁN, THỦ QUỸ, NV DỰ ÁN, NV KỸ THUẬT, …
            name = "NV KINH DOANH, SALE ADMIN, NV HÀNH CHÍNH, NV KẾ TOÁN, THỦ QUỸ, NV DỰ ÁN, NV KỸ THUẬT,…";
            maso = "D.03";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                double heso = 1;
                if (i > 1)
                {
                    heso = 1.05;
                }
                salaryDeclareTax = Convert.ToDecimal((double)salaryDeclareTax * heso);
                salaryReal = Convert.ToDecimal((double)salaryReal * heso);
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            #endregion

            #region 3- BẢNG LƯƠNG NHÂN VIÊN TRỰC TIẾP SẢN XUẤT KINH DOANH VÀ PHỤC VỤ
            typeRole = "NHÂN VIÊN TRỰC TIẾP SẢN XUẤT KINH DOANH VÀ PHỤC VỤ";
            typeRoleAlias = Utility.AliasConvert(typeRole);
            typeRoleCode = "B";
            // 01- TRƯỞNG BP -NM…
            name = "TRƯỞNG BP -NM…";
            maso = "B.01";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                double heso = 1;
                if (i > 1)
                {
                    heso = 1.05;
                }
                salaryDeclareTax = Convert.ToDecimal((double)salaryDeclareTax * heso);
                salaryReal = Convert.ToDecimal((double)salaryReal * heso);
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 02- TỔ TRƯỞNG NM, TỔ PHÓ NM, TỔ TRƯỞNG LOGISTICS, QUẢN LÝ TRẠM CÂN…
            name = "TỔ TRƯỞNG NM, TỔ PHÓ NM, TỔ TRƯỞNG LOGISTICS, QUẢN LÝ TRẠM CÂN…";
            maso = "B.02";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                double heso = 1;
                if (i > 1)
                {
                    heso = 1.05;
                }
                salaryDeclareTax = Convert.ToDecimal((double)salaryDeclareTax * heso);
                salaryReal = Convert.ToDecimal((double)salaryReal * heso);
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 03- TÀI XẾ, NV KHO, NV ĐIỀU ĐỘ, NV CẢNH QUAN, NV BẢO TRÌ, NV TRẠM CÂN…
            name = "TÀI XẾ, NV KHO, NV ĐIỀU ĐỘ, NV CẢNH QUAN, NV BẢO TRÌ, NV TRẠM CÂN…";
            maso = "B.03";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                double heso = 1;
                if (i > 1)
                {
                    heso = 1.05;
                }
                salaryDeclareTax = Convert.ToDecimal((double)salaryDeclareTax * heso);
                salaryReal = Convert.ToDecimal((double)salaryReal * heso);
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 04- GIAO NHẬN, PHỤ XE, BẢO VỆ, CÔNG NHÂN…
            name = "GIAO NHẬN, PHỤ XE, BẢO VỆ, CÔNG NHÂN…";
            maso = "B.04";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                double heso = 1;
                if (i > 1)
                {
                    heso = 1.05;
                }
                salaryDeclareTax = Convert.ToDecimal((double)salaryDeclareTax * heso);
                salaryReal = Convert.ToDecimal((double)salaryReal * heso);
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            #endregion
        }

        static void UpdateLevelVP(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            Application excelApp = new Application();
            if (excelApp == null)
            {
                Console.WriteLine("Excel is not installed!!");
                return;
            }
            //C:\Projects\Files
            Workbook excelBook = excelApp.Workbooks.Open(@"C:\Projects\Files\employee-level-pcpl.xlsx");
            _Worksheet excelSheet = excelBook.Sheets[1];
            Range excelRange = excelSheet.UsedRange;

            int rows = excelRange.Rows.Count;
            int cols = excelRange.Columns.Count;

            for (int i = 3; i <= rows; i++)
            {
                int columnIndex = 2;
                var ten = string.Empty;
                var levelSt = string.Empty;
                var diemthamkhaoSt = string.Empty;
                var nangNhocDocHai = "0";
                var trachNhiem = "0";
                var thuHut = "0";
                var xang = "0";
                var dienThoai = "0";
                var com = "0";
                var nhaO = "0";
                var kiemNhiem = "0";
                var bhytDacBiet = "0";
                var viTriCanKnNhieuNam = "0";
                var viTriDacThu = "0";

                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    ten = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex = columnIndex + 4;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    levelSt = excelRange.Cells[i, columnIndex].Value2.ToString();
                }

                #region PCPL
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    nangNhocDocHai = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    trachNhiem = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    thuHut = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    xang = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    dienThoai = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    com = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    nhaO = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    kiemNhiem = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    bhytDacBiet = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    viTriCanKnNhieuNam = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    viTriDacThu = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                #endregion

                Console.Write("Ten: " + ten);
                Console.Write("\r\n");
                if (string.IsNullOrEmpty(ten)) continue;

                int level = string.IsNullOrEmpty(levelSt) ? 1 : Convert.ToInt32(levelSt);
                var alias = Utility.AliasConvert(ten);
                var phucapphucloi = new PhuCapPhucLoi()
                {
                    NangNhocDocHai = Convert.ToDecimal(nangNhocDocHai),
                    TrachNhiem = Convert.ToDecimal(trachNhiem),
                    ThuHut = Convert.ToDecimal(thuHut),
                    Xang = Convert.ToDecimal(xang),
                    DienThoai = Convert.ToDecimal(dienThoai),
                    Com = Convert.ToDecimal(com),
                    NhaO = Convert.ToDecimal(nhaO),
                    KiemNhiem = Convert.ToDecimal(kiemNhiem),
                    BhytDacBiet = Convert.ToDecimal(bhytDacBiet),
                    ViTriCanKnNhieuNam = Convert.ToDecimal(viTriCanKnNhieuNam),
                    ViTriDacThu = Convert.ToDecimal(viTriDacThu),
                };
                var filter = Builders<Employee>.Filter.Eq(m => m.AliasFullName, alias);
                var update = Builders<Employee>.Update
                    .Set(m => m.NgachLuongLevel, level)
                    .Set(m => m.PhuCapPhucLoi, phucapphucloi);
                dbContext.Employees.UpdateOne(filter, update);
            }
            excelApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
        }

        static void UpdateThangLuongVP(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            decimal minSalary = 4013;

            Application excelApp = new Application();
            if (excelApp == null)
            {
                Console.WriteLine("Excel is not installed!!");
                return;
            }
            //C:\Projects\Files
            Workbook excelBook = excelApp.Workbooks.Open(@"C:\Projects\Files\thang-luong-vp.xlsx");
            _Worksheet excelSheet = excelBook.Sheets[1];
            Range excelRange = excelSheet.UsedRange;

            int rows = excelRange.Rows.Count;
            int cols = excelRange.Columns.Count;

            for (int i = 5; i <= rows; i++)
            {
                int columnIndex = 2;
                var vitri = string.Empty;
                var tileSt = string.Empty;
                var diemthamkhaoSt = string.Empty;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    vitri = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex = columnIndex + 12;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    tileSt = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    diemthamkhaoSt = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                if (string.IsNullOrEmpty(vitri)) continue;

                var alias = Utility.AliasConvert(vitri);
                var vitriE = dbContext.ChucVus.Find(m => m.Alias.Equals(alias)).FirstOrDefault();
                if (vitriE == null)
                {
                    var lastestChucVu = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
                    var lastestCode = lastestChucVu != null ? lastestChucVu.Order + 1 : 1;
                    vitriE = new ChucVu
                    {
                        Code = "CHUCVU" + lastestCode,
                        Name = vitri,
                        Alias = alias,
                        Order = lastestCode
                    };
                    dbContext.ChucVus.InsertOne(vitriE);
                }
                double tile = 1;
                if (!string.IsNullOrEmpty(tileSt))
                {
                    tile = Convert.ToDouble(tileSt) == 0 ? 1 : Convert.ToDouble(tileSt);
                }
                decimal diemthamkhao = minSalary;
                if (!string.IsNullOrEmpty(diemthamkhaoSt))
                {
                    diemthamkhao = Convert.ToDecimal(diemthamkhaoSt) == 0 ? minSalary : Convert.ToDecimal(diemthamkhaoSt);
                }
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Month = 8,
                    Year = 2018,
                    ViTriId = vitriE.Id,
                    ViTriCode = vitriE.Code,
                    ViTriName = vitriE.Name,
                    ViTriAlias = vitriE.Alias,
                    Bac = 1,
                    TiLe = tile,
                    MucLuong = diemthamkhao
                });
            }
            excelApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
        }

        static void UpdateLeaveDay2NM(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var leaveType = dbContext.LeaveTypes.Find(m => m.Alias.Equals("phep-nam")).FirstOrDefault();
            var leaveTypeId = leaveType.Id;

            Application excelApp = new Application();
            if (excelApp == null)
            {
                Console.WriteLine("Excel is not installed!!");
                return;
            }
            //C:\Projects\Files
            Workbook excelBook = excelApp.Workbooks.Open(@"C:\Projects\Files\phep-nm.xlsx");
            _Worksheet excelSheet = excelBook.Sheets[1];
            Range excelRange = excelSheet.UsedRange;

            int rows = excelRange.Rows.Count;
            int cols = excelRange.Columns.Count;

            for (int i = 2; i <= rows; i++)
            {
                int columnIndex = 2;
                var ten = string.Empty;
                var phep2018 = string.Empty;
                var phep2019 = string.Empty;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    ten = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex = columnIndex + 2;
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    phep2018 = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    phep2019 = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                if (string.IsNullOrEmpty(ten))
                {
                    continue;
                }

                var employee = dbContext.Employees.Find(m => m.FullName.Equals(ten.Trim())).FirstOrDefault();
                if (employee != null)
                {
                    var leaveE = dbContext.LeaveEmployees.Find(m => m.EmployeeId.Equals(employee.Id) && m.LeaveTypeId.Equals(leaveTypeId)).FirstOrDefault();
                    if (leaveE == null)
                    {
                        var leaveEmployeeNew2018 = new LeaveEmployee()
                        {
                            LeaveTypeId = leaveTypeId,
                            EmployeeId = employee.Id,
                            LeaveTypeName = leaveType.Name,
                            EmployeeName = employee.FullName,
                            Number = Convert.ToDouble(phep2018),
                            Department = employee.PhongBanName,
                            Part = employee.BoPhanName,
                            Title = employee.ChucVuName,
                            LeaveLevel = employee.LeaveLevelYear,
                            NumberUsed = 0,
                            UseFlag = true,
                            Year = 2018
                        };
                        dbContext.LeaveEmployees.InsertOne(leaveEmployeeNew2018);
                        dbContext.LeaveEmployeeHistories.InsertOne(new LeaveEmployeeHistory
                        {
                            EmployeeId = employee.Id,
                            LeaveTypeId = leaveTypeId,
                            Current = 0,
                            Change = Convert.ToDouble(phep2018),
                            Month = 5,
                            Year = 2018,
                            Description = "Cập nhật phép admin"
                        });

                        var leaveEmployeeNew2019 = leaveEmployeeNew2018;
                        leaveEmployeeNew2019.Id = null;
                        leaveEmployeeNew2019.Year = 2019;
                        leaveEmployeeNew2019.Number = Convert.ToDouble(phep2019);
                        dbContext.LeaveEmployees.InsertOne(leaveEmployeeNew2019);
                        dbContext.LeaveEmployeeHistories.InsertOne(new LeaveEmployeeHistory
                        {
                            EmployeeId = employee.Id,
                            LeaveTypeId = leaveTypeId,
                            Current = 0,
                            Change = Convert.ToDouble(phep2019),
                            Month = 5,
                            Year = 2019,
                            Description = "Cập nhật phép admin"
                        });
                    }
                    else
                    {
                        double currentLeaveNum = leaveE.Number;
                        var filterLeaveEmployee = Builders<LeaveEmployee>.Filter.Eq(m => m.EmployeeId, employee.Id);
                        var updateLeaveEmployee = Builders<LeaveEmployee>.Update
                            .Set(m => m.UseFlag, true)
                            .Inc(m => m.Number, Convert.ToDouble(phep2019))
                            .Set(m => m.UpdatedOn, DateTime.Now);
                        dbContext.LeaveEmployees.UpdateOne(filterLeaveEmployee, updateLeaveEmployee);
                        dbContext.LeaveEmployeeHistories.InsertOne(new LeaveEmployeeHistory
                        {
                            EmployeeId = employee.Id,
                            LeaveTypeId = leaveTypeId,
                            Current = currentLeaveNum,
                            Change = Convert.ToDouble(phep2019),
                            Month = 5,
                            Year = 2019,
                            Description = "Admin updated."
                        });
                    }
                }
            }
            excelApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
        }

        static void UpdateLeaveDayMissing(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var leaveType = dbContext.LeaveTypes.Find(m => m.Alias.Equals("phep-nam")).FirstOrDefault();
            var leaveTypeId = leaveType.Id;

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && !m.UserName.Equals(Constants.System.account)).ToList();

            //double phep2019 = 5; // từ tháng 1->5
            foreach (var employee in employees)
            {
                double phep2019 = 0;
                if (employee.Joinday.AddMonths(5) < new DateTime(2019, 5, 1))
                {
                    phep2019 = 5; // nv cũ
                }
                else
                {
                    for (int i = employee.Joinday.Month; i <= 5; i++)
                    {
                        phep2019++;
                    }
                }

                var leaveE = dbContext.LeaveEmployees.Find(m => m.EmployeeId.Equals(employee.Id) && m.LeaveTypeId.Equals(leaveTypeId)).FirstOrDefault();
                if (leaveE == null)
                {
                    var leaveEmployee2019 = new LeaveEmployee()
                    {
                        LeaveTypeId = leaveTypeId,
                        EmployeeId = employee.Id,
                        LeaveTypeName = leaveType.Name,
                        EmployeeName = employee.FullName,
                        Number = phep2019,
                        Department = employee.PhongBanName,
                        Part = employee.BoPhanName,
                        Title = employee.ChucVuName,
                        LeaveLevel = employee.LeaveLevelYear,
                        NumberUsed = 0,
                        UseFlag = true,
                        Year = 2019
                    };
                    dbContext.LeaveEmployees.InsertOne(leaveEmployee2019);
                    dbContext.LeaveEmployeeHistories.InsertOne(new LeaveEmployeeHistory
                    {
                        EmployeeId = employee.Id,
                        LeaveTypeId = leaveTypeId,
                        Current = 0,
                        Change = phep2019,
                        Month = 5,
                        Year = 2019,
                        Description = "Cập nhật phép admin"
                    });
                }
            }
        }

        static void FixEmployeeTimeKeeper(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Mode, 2);
            var update = Builders<EmployeeWorkTimeLog>.Update
                .Set(m => m.Mode, 20);
            dbContext.EmployeeWorkTimeLogs.UpdateMany(filter, update);

            var filter2 = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Mode, 6);
            var update2 = Builders<EmployeeWorkTimeLog>.Update
                .Set(m => m.Mode, 60);
            dbContext.EmployeeWorkTimeLogs.UpdateMany(filter2, update2);
        }

        static void FixEmployeeContractDay(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var employees = dbContext.Employees.Find(m => true).ToList();
            foreach (var item in employees)
            {
                var filter = Builders<Employee>.Filter.Eq(m => m.Id, item.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.Contractday, item.Joinday.AddMonths(2));
                dbContext.Employees.UpdateOne(filter, update);
            }
        }

        static void FixEmployeeNewStructure(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var congtychinhanhs = dbContext.CongTyChiNhanhs.Find(m => m.Enable.Equals(true)).ToList();
            var khoichucnangs = dbContext.KhoiChucNangs.Find(m => m.Enable.Equals(true)).ToList();
            var phongbans = dbContext.PhongBans.Find(m => m.Enable.Equals(true)).ToList();
            var bophans = dbContext.BoPhans.Find(m => m.Enable.Equals(true) && string.IsNullOrEmpty(m.Parent)).ToList();
            var chucvus = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).ToList();

            var employees = dbContext.Employees.Find(m => true).ToList();
            foreach (var item in employees)
            {
                var congtychinhanhName = string.Empty;
                var khoichucnangName = string.Empty;
                var phongbanName = string.Empty;
                var bophanName = string.Empty;
                var bophanConName = string.Empty;
                var chucvuName = string.Empty;
                var managerInformation = string.Empty;

                if (!string.IsNullOrEmpty(item.CongTyChiNhanh))
                {
                    var ctcnE = congtychinhanhs.Where(m => m.Id.Equals(item.CongTyChiNhanh)).FirstOrDefault();
                    if (ctcnE != null)
                    {
                        congtychinhanhName = ctcnE.Name;
                    }
                }
                if (!string.IsNullOrEmpty(item.KhoiChucNang))
                {
                    var kcnE = khoichucnangs.Where(m => m.Id.Equals(item.KhoiChucNang)).FirstOrDefault();
                    if (kcnE != null)
                    {
                        khoichucnangName = kcnE.Name;
                    }
                }
                if (!string.IsNullOrEmpty(item.PhongBan))
                {
                    var pbE = phongbans.Where(m => m.Id.Equals(item.PhongBan)).FirstOrDefault();
                    if (pbE != null)
                    {
                        phongbanName = pbE.Name;
                    }
                }
                if (!string.IsNullOrEmpty(item.BoPhan))
                {
                    var bpE = bophans.Where(m => m.Id.Equals(item.BoPhan)).FirstOrDefault();
                    if (bpE != null)
                    {
                        bophanName = bpE.Name;
                    }
                }
                if (!string.IsNullOrEmpty(item.BoPhanCon))
                {
                    var bpcE = bophans.Where(m => m.Id.Equals(item.BoPhanCon)).FirstOrDefault();
                    if (bpcE != null)
                    {
                        bophanConName = bpcE.Name;
                    }
                }
                if (!string.IsNullOrEmpty(item.ChucVu))
                {
                    var cvE = chucvus.Where(m => m.Id.Equals(item.ChucVu)).FirstOrDefault();
                    if (cvE != null)
                    {
                        chucvuName = cvE.Name;
                    }
                }
                if (!string.IsNullOrEmpty(item.ManagerId))
                {
                    var managerE = dbContext.Employees.Find(m => m.Id.Equals(item.ManagerId)).FirstOrDefault();
                    if (managerE != null)
                    {
                        managerInformation = managerE.FullName;
                        if (!string.IsNullOrEmpty(managerE.ChucVuName))
                        {
                            managerInformation += " [" + managerE.ChucVuName + "]";
                        }
                    }
                }

                var userName = item.UserName;
                if (string.IsNullOrEmpty(item.UserName))
                {
                    var email = Utility.EmailConvert(item.FullName);
                    userName = email.Replace(Constants.MailExtension, string.Empty);
                    var existU = dbContext.Employees.Find(m => m.UserName.Equals(userName)).FirstOrDefault();
                    if (existU != null)
                    {
                        userName = userName + existU.Birthday.ToString("yyyyMMdd");
                    }
                }

                var filter = Builders<Employee>.Filter.Eq(m => m.Id, item.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.CongTyChiNhanhName, congtychinhanhName)
                    .Set(m => m.KhoiChucNangName, khoichucnangName)
                    .Set(m => m.PhongBanName, phongbanName)
                    .Set(m => m.BoPhanName, bophanName)
                    .Set(m => m.BoPhanConName, bophanConName)
                    .Set(m => m.ChucVuName, chucvuName)
                    .Set(m => m.ManagerInformation, managerInformation)
                    .Set(m => m.UserName, userName);
                dbContext.Employees.UpdateOne(filter, update);
            }

            // update email to null congnhandonggoi
            var filterE = Builders<Employee>.Filter.Eq(m => m.ChucVu, "5c88d09bd59d56225c4324de");
            var updateE = Builders<Employee>.Update
                .Set(m => m.Email, string.Empty);
            dbContext.Employees.UpdateMany(filterE, updateE);
        }

        static void FixEmployeeTimer(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var filterEmp = Builders<Employee>.Filter.Eq(m => m.Enable, true) & Builders<Employee>.Filter.Eq(m => m.Leave, false);
            var employees = dbContext.Employees.Find(filterEmp).ToList();
            foreach (var item in employees)
            {
                var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.EmployeeId, item.Id);
                var update = Builders<EmployeeWorkTimeLog>.Update
                    .Set(m => m.Department, item.PhongBanName)
                    .Set(m => m.DepartmentId, item.PhongBan)
                    .Set(m => m.DepartmentAlias, Utility.AliasConvert(item.PhongBanName))
                    .Set(m => m.Part, item.BoPhanName)
                    .Set(m => m.PartId, item.BoPhan)
                    .Set(m => m.PartAlias, Utility.AliasConvert(item.BoPhanName))
                    .Set(m => m.EmployeeTitle, item.ChucVuName)
                    .Set(m => m.EmployeeTitleId, item.ChucVu)
                    .Set(m => m.EmployeeTitleAlias, Utility.AliasConvert(item.ChucVuName));
                dbContext.EmployeeWorkTimeLogs.UpdateMany(filter, update);

                var filterM = Builders<EmployeeWorkTimeMonthLog>.Filter.Eq(m => m.EmployeeId, item.Id);
                var updateM = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Set(m => m.Department, item.PhongBanName)
                    .Set(m => m.DepartmentId, item.PhongBan)
                    .Set(m => m.DepartmentAlias, Utility.AliasConvert(item.PhongBanName))
                    .Set(m => m.Part, item.BoPhanName)
                    .Set(m => m.PartId, item.BoPhan)
                    .Set(m => m.PartAlias, Utility.AliasConvert(item.BoPhanName))
                    .Set(m => m.Title, item.ChucVuName)
                    .Set(m => m.TitleId, item.ChucVu)
                    .Set(m => m.TitleAlias, Utility.AliasConvert(item.ChucVuName));
                dbContext.EmployeeWorkTimeMonthLogs.UpdateMany(filterM, updateM);
            }
        }

        static void FixEmployeeOldCode(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            Application excelApp = new Application();
            if (excelApp == null)
            {
                Console.WriteLine("Excel is not installed!!");
                return;
            }
            //C:\Projects\Files
            Workbook excelBook = excelApp.Workbooks.Open(@"C:\Projects\Files\ma-nhan-vien.xlsx");
            _Worksheet excelSheet = excelBook.Sheets[1];
            Range excelRange = excelSheet.UsedRange;

            int rows = excelRange.Rows.Count;
            int cols = excelRange.Columns.Count;

            for (int i = 1; i <= rows; i++)
            {
                int columnIndex = 1;
                var ma = string.Empty;
                var ten = string.Empty;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    ma = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    ten = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                if (string.IsNullOrEmpty(ten))
                {
                    continue;
                }
                Console.Write("Update: " + ma + "\t");
                var filter = Builders<Employee>.Filter.Eq(m => m.FullName, ten);
                var update = Builders<Employee>.Update
                    .Set(m => m.NgachLuongCode, "B.05")
                    .Set(m => m.NgachLuongLevel, 1)
                    .Set(m => m.CodeOld, ma);
                dbContext.Employees.UpdateOne(filter, update);
            }
            excelApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
        }


        static void UpdateLeaveDay(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            dbContext.LeaveEmployees.InsertOne(new LeaveEmployee()
            {
                LeaveTypeId = "5bbdb5a97caedd0c7411c89d",
                EmployeeId = "5c3e90b5566d7c0a345e5488",
                LeaveTypeName = "Phép năm",
                EmployeeName = "Vòng Thị Thúy Phượng",
                Number = 0,
                Department = "PHÒNG HCNS - NS",
                Part = "HCNS",
                Title = "NV HÀNH CHÍNH",
                LeaveLevel = 12,
                NumberUsed = 1
            });
        }

        static void UpdateEmployeeCode(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var timers = dbContext.EmployeeWorkTimeLogs.Find(m => m.Status.Equals((int)EStatusWork.DongY) && !m.WorkDay.Equals(1)).ToList();
            foreach (var timer in timers)
            {
                var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timer.Id);
                var update = Builders<EmployeeWorkTimeLog>.Update
                    .Set(m => m.WorkDay, 1);
                dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
            }
        }

        static void InitSetting(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            dbContext.Settings.InsertOne(new Setting()
            {
                Type = (int)ESetting.System,
                Key = "page-size",
                Value = "50"
            });

            dbContext.Settings.InsertOne(new Setting()
            {
                Type = (int)ESetting.System,
                Key = "page-size-khth",
                Value = "100"
            });
        }

        static void FixTimer(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var timers = dbContext.EmployeeWorkTimeLogs.Find(m => m.Status.Equals((int)EStatusWork.DongY) && !m.WorkDay.Equals(1)).ToList();
            foreach (var timer in timers)
            {
                var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timer.Id);
                var update = Builders<EmployeeWorkTimeLog>.Update
                    .Set(m => m.WorkDay, 1);
                dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
            }
        }

        static void FixEmployeeLeave(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(false)).ToList();
            foreach (var employee in employees)
            {
                var builder = Builders<Employee>.Filter;
                var filter = builder.Eq(m => m.Id, employee.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.Leave, true)
                    .Set(m => m.Enable, true)
                    .Set(m => m.IsWelcomeEmail, true)
                    .Set(m => m.IsLeaveEmail, true);
                dbContext.Employees.UpdateOne(filter, update);
            }

            var employeeAs = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)).ToList();
            foreach (var employee in employeeAs)
            {
                var builder = Builders<Employee>.Filter;
                var filter = builder.Eq(m => m.Id, employee.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.IsWelcomeEmail, true);
                dbContext.Employees.UpdateOne(filter, update);
            }
        }

        static void FixEmployeeData(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var congTyChiNhanhVP = dbContext.CongTyChiNhanhs.Find(m => m.Code.Equals("CT1")).FirstOrDefault().Id;

            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Email, "huy.lt@tribat.vn");
            var update = Builders<Employee>.Update
                .Set(m => m.CongTyChiNhanh, congTyChiNhanhVP);
            dbContext.Employees.UpdateOne(filter, update);
        }

        static void FixStructureInitBP(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var congTyChiNhanhVP = dbContext.CongTyChiNhanhs.Find(m => m.Code.Equals("CT1")).FirstOrDefault().Id;
            var khoiChucNangVP = dbContext.KhoiChucNangs.Find(m => m.Code.Equals("KHOI1")).FirstOrDefault().Id;
            var phongBanKinhDoanh = dbContext.PhongBans.Find(m => m.Name.Equals("PHÒNG KINH DOANH")).FirstOrDefault().Id;

            var nameBoPhan = "Trade Marketing";
            var lastestBoPhan = dbContext.BoPhans.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
            var lastestCode = lastestBoPhan != null ? lastestBoPhan.Order + 1 : 1;
            var boPhanEntity = new BoPhan()
            {
                PhongBanId = phongBanKinhDoanh,
                Code = "BOPHAN" + lastestCode,
                Name = nameBoPhan,
                Alias = Utility.AliasConvert(nameBoPhan),
                Order = lastestCode
            };
            dbContext.BoPhans.InsertOne(boPhanEntity);

            //vitri
            var viTri = "Trưởng Bộ Phận Trade Marketing";
            var lastestChucVu = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
            lastestCode = lastestChucVu != null ? lastestChucVu.Order + 1 : 1;
            var chucVuEntity = new ChucVu()
            {
                CongTyChiNhanhId = congTyChiNhanhVP,
                KhoiChucNangId = khoiChucNangVP,
                PhongBanId = phongBanKinhDoanh,
                BoPhanId = boPhanEntity.Id,
                Code = "CHUCVU" + lastestCode,
                Name = viTri,
                Alias = Utility.AliasConvert(viTri),
                Order = lastestCode
            };
            dbContext.ChucVus.InsertOne(chucVuEntity);
        }

        static void FixStructure(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var employees = dbContext.Employees.Find(m => string.IsNullOrEmpty(m.PhongBan)).ToList();
            foreach (var employee in employees)
            {
                var filterHis = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                var updateHis = Builders<Employee>.Update
                    .Set(m => m.CongTyChiNhanh, string.Empty)
                    .Set(m => m.KhoiChucNang, string.Empty)
                    .Set(m => m.PhongBan, string.Empty)
                    .Set(m => m.BoPhan, string.Empty)
                    .Set(m => m.BoPhanCon, string.Empty);
                dbContext.Employees.UpdateMany(filterHis, updateHis);
            }
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
            foreach (var employee in employees)
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
                Date = new DateTime(2019, 4, 14),
                Name = "Giỗ Tổ Hùng Vương",
                Detail = "Mùng 10 tháng 3"
            });

            dbContext.Holidays.InsertOne(new Holiday
            {
                Date = new DateTime(2019, 4, 15),
                Name = "Nghỉ bù Giỗ Tổ Hùng Vương",
                Detail = "Nghỉ bù chủ nhật"
            });

            dbContext.Holidays.InsertOne(new Holiday
            {
                Date = new DateTime(2019, 4, 30),
                Name = "Ngày Giải phóng miền Nam thống nhất Đất nước 30/04",
                Detail = ""
            });

            dbContext.Holidays.InsertOne(new Holiday
            {
                Date = new DateTime(2019, 5, 1),
                Name = "Ngày Quốc tê Lao động 01/05",
                Detail = ""
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
            foreach (var time in times)
            {
                Console.WriteLine("Date: " + time.Date + ", fingerCode: " + time.EnrollNumber);
                var builder = Builders<EmployeeWorkTimeLog>.Filter;
                var filter = builder.Eq(m => m.Id, time.Id);

                var update = Builders<EmployeeWorkTimeLog>.Update
                    .Set(m => m.EnrollNumber, Convert.ToInt32(time.EnrollNumber).ToString("000"));
                dbContext.EmployeeWorkTimeLogs.UpdateMany(filter, update);
            }
        }
        #endregion

        #region Tribatvn
        public void InitProductSales(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            dbContext.ProductSales.DeleteMany(m => true);

            #region Product VI
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 1,
                CategoryCode = 1,
                Name = "Đất Việt 50, 20 dm3",
                Alias = "dat-viet-50-20dm3",
                Price = 0,
                Description = "a/ Đặc tính sản phẩm",
                Content = "a/ Đặc tính sản phẩm",
                KeyWords = "đất sạch, đất dinh dưỡng, cây xanh, cây ăn trái, rau sạch, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 2,
                CategoryCode = 1,
                Name = "Đất trồng thuốc lá",
                Alias = "dat-trong-thuoc-la",
                Price = 0,
                Description = "Đất trồng thuốc lá",
                Content = "Đất trồng thuốc lá",
                KeyWords = "đất trồng thuốc lá, đất dinh dưỡng, cây xanh, cây ăn trái, rau sạch, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Language = Common.Utilities.Constants.Languages.Vietnamese
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 3,
                CategoryCode = 1,
                Name = "Đất trồng cây: 40dm3",
                Alias = "dat-trong-cay-40dm3",
                Price = 0,
                Description = "Đất trồng cây: 40dm3",
                Content = "Đất trồng cây: 40dm3",
                KeyWords = "đất trồng cây: 40dm3, đất dinh dưỡng, cây xanh, cây ăn trái, rau sạch, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/3/",
                        FileName = "1449045882dattrongcay.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 4,
                CategoryCode = 1,
                Name = "Đất trồng mai: 20dm3",
                Alias = "dat-trong-mai-20dm3",
                Price = 0,
                Description = "Đất trồng mai: 20dm3",
                Content = "Đất trồng mai: 20dm3",
                KeyWords = "đất trồng mai: 20dm3, đất dinh dưỡng, cây xanh, cây ăn trái, rau sạch, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/4/",
                        FileName = "1449045939dattrongmai.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 5,
                CategoryCode = 1,
                Name = "Đất trồng rau: 20dm3; 10dm3; 5dm3",
                Alias = "dat-trong-rau-20dm3-10dm3-5dm3",
                Price = 0,
                Description = "Đất trồng rau: 20dm3; 10dm3; 5dm3",
                Content = "Đất trồng rau: 20dm3; 10dm3; 5dm3",
                KeyWords = "đất trồng rau: 20dm3; 10dm3; 5dm3, đất dinh dưỡng, cây xanh, cây ăn trái, rau sạch, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/5/",
                        FileName = "1449045982dattrongrau.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region Product EN
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 1,
                CategoryCode = 1,
                Name = "Vietnamese land 50, 20 dm3",
                Alias = "vietnamese-land-50-20dm3",
                Language = Constants.Languages.English
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 2,
                CategoryCode = 1,
                Name = "Cultivation of tobacco",
                Alias = "cultivation-of-tobacco",
                Language = Constants.Languages.English
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 3,
                CategoryCode = 1,
                Name = "Woodland: 40dm3",
                Alias = "woodland-40dm3",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/3/",
                        FileName = "1449045882dattrongcay.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.English
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 4,
                CategoryCode = 1,
                Name = "Land for planting apricots: 20dm3",
                Alias = "land-for-planting-apricots-20dm3",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/4/",
                        FileName = "1449045939dattrongmai.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.English
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 5,
                CategoryCode = 1,
                Name = "Land for growing vegetables: 20dm3; 10dm3; 5dm3",
                Alias = "land-for-growing-vegetables-20dm3-10dm3-5dm3",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/5/",
                        FileName = "1449045982dattrongrau.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.English
            });
            #endregion
        }
        #endregion

        #region Factories: Init dataW
        static void InsertNewEmployee(string connection, string database, string fullname, string oldcode, string chucvu, string ngayvaolam)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            chucvu = string.IsNullOrEmpty(chucvu) ? "CÔNG NHÂN" : chucvu.ToUpper();
            DateTime joinday = string.IsNullOrEmpty(ngayvaolam) ? DateTime.Now : DateTime.FromOADate(Convert.ToDouble(ngayvaolam));

            var entity = new Employee
            {
                FullName = fullname,
                CodeOld = oldcode,
                SalaryType = (int)EKhoiLamViec.SX,
                Joinday = joinday
            };

            #region System Generate
            var pwdrandom = Guid.NewGuid().ToString("N").Substring(0, 6);
            var settings = dbContext.Settings.Find(m => true).ToList();
            // always have value
            var employeeCodeFirst = settings.Where(m => m.Key.Equals("employeeCodeFirst")).First().Value;
            var employeeCodeLength = settings.Where(m => m.Key.Equals("employeeCodeLength")).First().Value;
            var lastEntity = dbContext.Employees.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Id).Limit(1).First();
            var x = 1;
            if (lastEntity != null && lastEntity.Code != null)
            {
                x = Convert.ToInt32(lastEntity.Code.Replace(employeeCodeFirst, string.Empty)) + 1;
            }
            var sysCode = employeeCodeFirst + x.ToString($"D{employeeCodeLength}");
            #endregion

            entity.Code = sysCode;
            entity.Password = pwdrandom;
            entity.AliasFullName = Utility.AliasConvert(entity.FullName);
            dbContext.Employees.InsertOne(entity);

            var newUserId = entity.Id;
            var hisEntity = entity;
            hisEntity.EmployeeId = newUserId;
            dbContext.EmployeeHistories.InsertOne(hisEntity);
        }

        static void InitFactoryProductDinhMucTangCa(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            dbContext.FactoryProductDinhMucTangCas.DeleteMany(m => true);

            var target = new DateTime(2018, 7, 1);
            while (target < DateTime.Now)
            {
                // do something with target.Month and target.Year
                dbContext.FactoryProductDinhMucTangCas.InsertOne(new FactoryProductDinhMucTangCa
                {
                    Month = target.Month,
                    Year = target.Year,
                    Type = (int)EDinhMuc.DongGoi,
                    PhanTramTangCa = 10
                });

                dbContext.FactoryProductDinhMucTangCas.InsertOne(new FactoryProductDinhMucTangCa
                {
                    Month = target.Month,
                    Year = target.Year,
                    Type = (int)EDinhMuc.BocVac,
                    PhanTramTangCa = 10
                });
                target = target.AddMonths(1);
            }
        }

        static void InitFactoryProductDinhMucTiLe(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            dbContext.FactoryProductDinhMucTiLes.DeleteMany(m => true);

            var tiles = new List<string>()
            {
                "50-4000000-1-26-7.3",
                "85-6500000-2-26-7.3",
                "100-7000000-3-26-7.3",
                "115-8000000-4-26-7.3",
                "120-9000000-5-26-7.3",
            };
            var target = new DateTime(2018, 7, 1);
            while (target < DateTime.Now)
            {
                foreach (var tile in tiles)
                {
                    dbContext.FactoryProductDinhMucTiLes.InsertOne(new FactoryProductDinhMucTiLe
                    {
                        Month = target.Month,
                        Year = target.Year,
                        Type = (int)EDinhMuc.DongGoi,
                        TiLe = Convert.ToDouble(tile.Split('-')[0]),
                        MucLuong = Convert.ToDecimal(tile.Split('-')[1]),
                        DonGia = Convert.ToDecimal(tile.Split('-')[2]),
                        NgayCong = Convert.ToDouble(tile.Split('-')[3]),
                        ThoiGian = Convert.ToDouble(tile.Split('-')[4])
                    });

                    dbContext.FactoryProductDinhMucTiLes.InsertOne(new FactoryProductDinhMucTiLe
                    {
                        Month = target.Month,
                        Year = target.Year,
                        Type = (int)EDinhMuc.BocVac,
                        TiLe = Convert.ToDouble(tile.Split('-')[0]),
                        MucLuong = Convert.ToDecimal(tile.Split('-')[1]),
                        DonGia = Convert.ToDecimal(tile.Split('-')[2]),
                        NgayCong = Convert.ToDouble(tile.Split('-')[3]),
                        ThoiGian = 6.5
                    });
                }

                target = target.AddMonths(1);
            }
        }

        static void InitFactoryProductDonGiaM3(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            dbContext.FactoryProductDonGiaM3s.DeleteMany(m => true);

            var target = new DateTime(2018, 7, 1);
            while (target < DateTime.Now)
            {
                // do something with target.Month and target.Year
                dbContext.FactoryProductDonGiaM3s.InsertOne(new FactoryProductDonGiaM3
                {
                    Month = target.Month,
                    Year = target.Year,
                    Type = (int)EDinhMuc.BocVac,
                    Price = 11200
                });

                target = target.AddMonths(1);
            }
        }

        static void InitFactoryProductDinhMuc(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            // 1. UPDATE DATA FactoryProduct
            // 2. INSERT FactoryProductDinhMuc
            dbContext.FactoryProductDinhMucs.DeleteMany(m => true);
            dbContext.FactoryProducts.DeleteMany(m => true);

            // Put file in ""
            //Create COM Objects.
            Application excelApp = new Application();
            if (excelApp == null)
            {
                Console.WriteLine("Excel is not installed!!");
                return;
            }
            //C:\Projects\Files
            Workbook excelBook = excelApp.Workbooks.Open(@"C:\Projects\Files\DINH-MUC-LUONG-CONG-NHAN-SX.xlsx");
            _Worksheet excelSheet = excelBook.Sheets[1];
            Range excelRange = excelSheet.UsedRange;

            int rows = excelRange.Rows.Count;
            int cols = excelRange.Columns.Count;
            // DONG GOI
            decimal mucluong = 7000000;
            double ngaycong = 26;
            double thoigiangio = 7.3;
            double dongiatangcaphantram = 10;

            for (int i = 6; i <= 56; i++)
            {
                Console.Write("\r\n");
                var ma = string.Empty;
                if (excelRange.Cells[i, 1] != null && excelRange.Cells[i, 1].Value2 != null)
                {
                    ma = excelRange.Cells[i, 1].Value2.ToString();
                }
                if (string.IsNullOrEmpty(ma))
                {
                    continue;
                }

                var thanhpham = string.Empty;
                if (excelRange.Cells[i, 2] != null && excelRange.Cells[i, 2].Value2 != null)
                {
                    thanhpham = excelRange.Cells[i, 2].Value2.ToString();
                }
                var nhom = string.Empty;
                if (excelRange.Cells[i, 3] != null && excelRange.Cells[i, 3].Value2 != null)
                {
                    nhom = excelRange.Cells[i, 3].Value2.ToString();
                }
                var dvt = string.Empty;
                if (excelRange.Cells[i, 4] != null && excelRange.Cells[i, 4].Value2 != null)
                {
                    dvt = excelRange.Cells[i, 4].Value2.ToString();
                }

                double sobaonhomngay = 0;
                if (excelRange.Cells[i, 5] != null && excelRange.Cells[i, 5].Value2 != null)
                {
                    sobaonhomngay = Convert.ToDouble(excelRange.Cells[i, 5].Value2);
                }
                double dinhmuctheongay = 0;
                if (excelRange.Cells[i, 6] != null && excelRange.Cells[i, 6].Value2 != null)
                {
                    dinhmuctheongay = Convert.ToDouble(excelRange.Cells[i, 6].Value2);
                }
                double dinhmuctheogio = dinhmuctheongay / thoigiangio;

                decimal dongia = dinhmuctheogio > 0 ? Convert.ToDecimal((double)mucluong / ngaycong / thoigiangio / dinhmuctheogio) : 0;

                decimal dongiadieuchinh = dongia;
                if (excelRange.Cells[i, 9] != null && excelRange.Cells[i, 9].Value2 != null)
                {
                    dongiadieuchinh = Convert.ToDecimal(excelRange.Cells[i, 9].Value2.ToString());
                }
                var dongiatangca = Convert.ToDecimal((double)dongiadieuchinh * (1 + (dongiatangcaphantram / 100)));

                //var dongiaM3 = Convert.ToDecimal(excelRange.Cells[i, 11].Value2.ToString());
                //var dongiatangcaM3 = Convert.ToDecimal(excelRange.Cells[i, 12].Value2.ToString());
                decimal dongiaM3 = 0;
                decimal dongiatangcaM3 = 0;

                var alias = Utility.AliasConvert(thanhpham);
                dbContext.FactoryProducts.InsertOne(new FactoryProduct()
                {
                    Code = ma,
                    Name = thanhpham,
                    Alias = alias,
                    Group = nhom,
                    Unit = dvt,
                    Sort = i - 5
                });

                var factoryProduct = dbContext.FactoryProducts.Find(m => m.Code.Equals(ma)).FirstOrDefault();
                var id = factoryProduct.Id;

                dbContext.FactoryProductDinhMucs.InsertOne(new FactoryProductDinhMuc
                {
                    Month = 7,
                    Year = 2018,
                    Type = (int)EDinhMuc.DongGoi,
                    ProductId = id,
                    ProductCode = ma,
                    Sort = i - 5,
                    SoBaoNhomNgay = sobaonhomngay,
                    DinhMucTheoNgay = dinhmuctheongay,
                    DinhMucGioQuiDinh = thoigiangio,
                    DinhMucTheoGio = dinhmuctheogio,
                    DonGia = dongia,
                    DonGiaDieuChinh = dongiadieuchinh,
                    DonGiaTangCaPhanTram = dongiatangcaphantram,
                    DonGiaTangCa = dongiatangca,
                    DonGiaM3 = dongiaM3,
                    DonGiaTangCaM3 = dongiatangcaM3
                });
                Console.Write(ma + "\t");
            }

            thoigiangio = 6.5;
            for (int i = 69; i <= 118; i++)
            {
                Console.Write("\r\n");
                var ma = string.Empty;
                if (excelRange.Cells[i, 1] != null && excelRange.Cells[i, 1].Value2 != null)
                {
                    ma = excelRange.Cells[i, 1].Value2.ToString();
                }
                if (string.IsNullOrEmpty(ma))
                {
                    continue;
                }
                var thanhpham = string.Empty;
                if (excelRange.Cells[i, 2] != null && excelRange.Cells[i, 2].Value2 != null)
                {
                    thanhpham = excelRange.Cells[i, 2].Value2.ToString();
                }
                var nhom = string.Empty;
                if (excelRange.Cells[i, 3] != null && excelRange.Cells[i, 3].Value2 != null)
                {
                    nhom = excelRange.Cells[i, 3].Value2.ToString();
                }
                var dvt = string.Empty;
                if (excelRange.Cells[i, 4] != null && excelRange.Cells[i, 4].Value2 != null)
                {
                    dvt = excelRange.Cells[i, 4].Value2.ToString();
                }

                double sobaonhomngay = 0;
                if (excelRange.Cells[i, 5] != null && excelRange.Cells[i, 5].Value2 != null)
                {
                    sobaonhomngay = Convert.ToDouble(excelRange.Cells[i, 5].Value2);
                }
                double dinhmuctheongay = 0;
                if (excelRange.Cells[i, 6] != null && excelRange.Cells[i, 6].Value2 != null)
                {
                    dinhmuctheongay = Convert.ToDouble(excelRange.Cells[i, 6].Value2);
                }
                double dinhmuctheogio = dinhmuctheongay / thoigiangio;

                var dongia = dinhmuctheogio > 0 ? Convert.ToDecimal((double)mucluong / ngaycong / thoigiangio / dinhmuctheogio) : 0;

                var dongiadieuchinh = dongia;
                if (excelRange.Cells[i, 9] != null && excelRange.Cells[i, 9].Value2 != null)
                {
                    dongiadieuchinh = Math.Round(Convert.ToDecimal(excelRange.Cells[i, 9].Value2.ToString()), 2);
                }
                var dongiatangca = Convert.ToDecimal((double)dongiadieuchinh * (1 + dongiatangcaphantram / 100));

                var dongiaM3 = (decimal)0;
                if (excelRange.Cells[i, 11] != null && excelRange.Cells[i, 11].Value2 != null)
                {
                    dongiaM3 = Math.Round(Convert.ToDecimal(excelRange.Cells[i, 11].Value2.ToString()), 2);
                }
                var dongiatangcaM3 = Convert.ToDecimal((double)dongiaM3 * (1 + dongiatangcaphantram / 100));

                var factoryProduct = dbContext.FactoryProducts.Find(m => m.Code.Equals(ma)).FirstOrDefault();
                var id = factoryProduct.Id;

                dbContext.FactoryProductDinhMucs.InsertOne(new FactoryProductDinhMuc
                {
                    Month = 7,
                    Year = 2018,
                    Type = (int)EDinhMuc.BocVac,
                    ProductId = id,
                    ProductCode = ma,
                    Sort = i - 67,
                    SoBaoNhomNgay = sobaonhomngay,
                    DinhMucTheoNgay = dinhmuctheongay,
                    DinhMucGioQuiDinh = thoigiangio,
                    DinhMucTheoGio = dinhmuctheogio,
                    DonGia = dongia,
                    DonGiaDieuChinh = dongiadieuchinh,
                    DonGiaTangCaPhanTram = dongiatangcaphantram,
                    DonGiaTangCa = dongiatangca,
                    DonGiaM3 = dongiaM3,
                    DonGiaTangCaM3 = dongiatangcaM3
                });
                Console.Write(ma + "\t");
            }

            //after reading, relaase the excel project
            excelApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);

            // Create data for next month: 8,9,... to now.
            var month7s = dbContext.FactoryProductDinhMucs.Find(m => m.Month.Equals(7) && m.Year.Equals(2018)).ToList();
            foreach (var month7 in month7s)
            {
                var newData = month7;
                var target = new DateTime(2018, 8, 1);
                while (target < DateTime.Now)
                {
                    // do something with target.Month and target.Year
                    newData.Id = null;
                    newData.Month = target.Month;
                    newData.Year = target.Year;
                    dbContext.FactoryProductDinhMucs.InsertOne(newData);

                    target = target.AddMonths(1);
                }
            }
        }

        static void InitFactoryCongViec(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            dbContext.FactoryCongViecs.DeleteMany(m => true);

            #region Cong Viec Chinh: Boc Hang
            dbContext.FactoryCongViecs.InsertOne(new FactoryCongViec()
            {
                Main = true,
                Code = "CVC-" + 1,
                Name = "Đóng Gói",
                Alias = "dong-goi",
                Price = 0,
                Sort = 1
            });
            dbContext.FactoryCongViecs.InsertOne(new FactoryCongViec()
            {
                Main = true,
                Code = "CVC-" + 2,
                Name = "Bóc Hàng",
                Alias = "boc-hang",
                Price = 11200,
                Sort = 2
            });
            #endregion

            // Put file in ""
            //Create COM Objects.
            Application excelApp = new Application();
            if (excelApp == null)
            {
                Console.WriteLine("Excel is not installed!!");
                return;
            }
            //C:\Projects\Files
            Workbook excelBook = excelApp.Workbooks.Open(@"C:\Projects\Files\CONG-VIEC-KHAC.xlsx");
            _Worksheet excelSheet = excelBook.Sheets[1];
            Range excelRange = excelSheet.UsedRange;

            int cols = excelRange.Columns.Count;
            var price = Convert.ToDecimal((double)7000000 / (double)26 / 7.3);

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            for (int j = 1; j <= cols; j++)
            {
                Console.Write("\r\n");
                if (excelRange.Cells[1, j] != null && excelRange.Cells[1, j].Value2 != null)
                {
                    var name = textInfo.ToTitleCase(excelRange.Cells[1, j].Value2.ToString());
                    var alias = Utility.AliasConvert(name);
                    dbContext.FactoryCongViecs.InsertOne(new FactoryCongViec()
                    {
                        Main = false,
                        Code = "CVK-" + j,
                        Name = name,
                        Alias = alias,
                        Price = price,
                        Sort = j
                    });
                    Console.Write(name + "\t");
                }
            }

            //after reading, relaase the excel project
            excelApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
        }
        #endregion

        #region Fix issue
        static void FixEmailLeave(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            #region Send Mail
            var listLeave = new List<string>
            {
                "5c82305aa62c3900ec434665",
                "5c822b18a62c3900ec434663"
            };

            var leaves = dbContext.Leaves.Find(m => listLeave.Contains(m.Id)).ToList();

            foreach (var entity in leaves)
            {
                var employee = dbContext.Employees.Find(m => m.Id.Equals(entity.EmployeeId)).FirstOrDefault();

                var tos = new List<EmailAddress>();
                var approver = string.Empty;
                if (!string.IsNullOrEmpty(entity.ApproverId))
                {
                    var approve1 = dbContext.Employees.Find(m => m.Id.Equals(entity.ApproverId)).FirstOrDefault();
                    approver = approve1.FullName;
                    tos.Add(new EmailAddress { Name = approve1.FullName, Address = approve1.Email });
                }
                else
                {
                    tos.Add(new EmailAddress { Name = "Tran Minh Xuan", Address = "xuan.tm@tribat.vn" });
                }
                var webRoot = Environment.CurrentDirectory;
                var pathToFile = @"C:\Projects\App.Schedule\Templates\LeaveRequest.html";

                var subject = "Xác nhận nghỉ phép.";
                var requester = employee.FullName;
                var var3 = employee.FullName;
                var dateRequest = entity.From.ToString("dd/MM/yyyy HH:mm") + " - " + entity.To.ToString("dd/MM/yyyy HH:mm") + " (" + entity.Number + " ngày)";
                // Api update, generate code.
                var linkapprove = Constants.System.domain + "/xacnhan/phep";
                var linkAccept = linkapprove + "?id=" + entity.Id + "&approve=1&secure=" + entity.SecureCode;
                var linkCancel = linkapprove + "?id=" + entity.Id + "&approve=2&secure=" + entity.SecureCode;
                var linkDetail = Constants.System.domain;
                var bodyBuilder = new BodyBuilder();
                using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                {
                    bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                }
                string messageBody = string.Format(bodyBuilder.HtmlBody,
                    subject,
                    approver,
                    requester,
                    var3,
                    employee.Email,
                    dateRequest,
                    entity.Reason,
                    entity.TypeName,
                    entity.Phone,
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
                    Type = "yeu-cau-nghi-phep",
                    EmployeeId = entity.EmployeeId
                };

                // For faster. Add to schedule.
                // Send later
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
            #endregion
        }
        #endregion
    }
}

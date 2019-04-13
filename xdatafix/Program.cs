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
using Microsoft.Office.Interop.Excel;
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

            UpdateLeaveDay(connection, database);
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

            //ResetDataFactorySalary(connection, database);
            //InitDataLuong(connection, database, "07-2019");
            #endregion

            #region Factories: Update data

            #endregion

            Console.Write("\r\n");
            Console.Write("Done..... Press any key to exist!");
            Console.ReadLine();
        }

        #region ERP
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
                Type = "system",
                Key = "page-size",
                Value = "50"
            });

            dbContext.Settings.InsertOne(new Setting()
            {
                Type = "system",
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

        static void UpdateEmployeeStructure(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            string name = string.Empty;
            int i = 1;

            #region CongTyChiNhanh
            dbContext.CongTyChiNhanhs.DeleteMany(m => true);
            name = "Công ty TNHH CNSH SÀI GÒN XANH";
            i = 1;
            dbContext.CongTyChiNhanhs.InsertOne(new CongTyChiNhanh()
            {
                Code = "CT" + i,
                Name = name,
                Alias = Utility.AliasConvert(name),
                Address = "127 Nguyễn Trọng Tuyển - P.15 - Q.Phú Nhuận - Tp HCM",
                Order = i
            });
            i++;
            name = "Nhà máy Xử lý bùn thải Sài Gòn Xanh";
            dbContext.CongTyChiNhanhs.InsertOne(new CongTyChiNhanh()
            {
                Code = "CT" + i,
                Name = name,
                Alias = Utility.AliasConvert(name),
                Address = "Ấp 1, xã Đa Phước, huyện Bình Chánh",
                Order = i
            });
            #endregion

            #region KhoiChucNang
            var CongTyChiNhanhVP = dbContext.CongTyChiNhanhs.Find(m => m.Code.Equals("CT1")).FirstOrDefault().Id;
            var CongTyChiNhanhNM = dbContext.CongTyChiNhanhs.Find(m => m.Code.Equals("CT2")).FirstOrDefault().Id;
            dbContext.KhoiChucNangs.DeleteMany(m => true);
            i = 1;
            name = "KHỐI VĂN PHÒNG";
            dbContext.KhoiChucNangs.InsertOne(new KhoiChucNang()
            {
                CongTyChiNhanhId = CongTyChiNhanhVP,
                Code = "KHOI" + i,
                Name = name,
                Alias = Utility.AliasConvert(name),
                Order = i
            });
            i++;

            name = "KHỐI QUẢN TRỊ TỔNG HỢP";
            dbContext.KhoiChucNangs.InsertOne(new KhoiChucNang()
            {
                CongTyChiNhanhId = CongTyChiNhanhNM,
                Code = "KHOI" + i,
                Name = name,
                Alias = Utility.AliasConvert(name),
                Order = i
            });
            i++;

            name = "KHỐI SẢN XUẤT";
            dbContext.KhoiChucNangs.InsertOne(new KhoiChucNang()
            {
                CongTyChiNhanhId = CongTyChiNhanhNM,
                Code = "KHOI" + i,
                Name = name,
                Alias = Utility.AliasConvert(name),
                Order = i
            });
            i++;
            #endregion

            dbContext.PhongBans.DeleteMany(m => true);
            dbContext.BoPhans.DeleteMany(m => true);
            dbContext.ChucVus.DeleteMany(m => true);

            #region VAN PHONG
            var congTyChiNhanhVP = dbContext.CongTyChiNhanhs.Find(m => m.Code.Equals("CT1")).FirstOrDefault().Id;
            var khoiChucNangVP = dbContext.KhoiChucNangs.Find(m => m.Code.Equals("KHOI1")).FirstOrDefault().Id;
            var employeesVp = dbContext.Employees.Find(m => !m.Department.Equals("NHÀ MÁY") && !m.UserName.Equals(Constants.System.account)).ToList();
            //Departments
            foreach (var employee in employeesVp)
            {
                var phongBanId = string.Empty;
                var boPhanId = string.Empty;
                var boPhanConId = string.Empty;
                var chucVuId = string.Empty;
                // Phong Ban  <=> Departments
                if (!string.IsNullOrEmpty(employee.Department))
                {
                    var phongBanEntity = dbContext.PhongBans.Find(m => m.Name.Equals(employee.Department.ToUpper()) && m.Enable.Equals(true)).FirstOrDefault();
                    if (phongBanEntity == null)
                    {
                        var lastestPhongBan = dbContext.PhongBans.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
                        var lastestCode = lastestPhongBan != null ? lastestPhongBan.Order + 1 : 1;
                        phongBanEntity = new PhongBan()
                        {
                            KhoiChucNangId = khoiChucNangVP,
                            Code = "PHONGBAN" + lastestCode,
                            Name = employee.Department,
                            Alias = Utility.AliasConvert(employee.Department),
                            Order = lastestCode
                        };
                        dbContext.PhongBans.InsertOne(phongBanEntity);
                    }
                    phongBanId = phongBanEntity.Id;
                }

                // Bo Phan
                if (!string.IsNullOrEmpty(employee.Part))
                {
                    var boPhanEntity = dbContext.BoPhans.Find(m => m.Name.Equals(employee.Part.ToUpper()) && m.Enable.Equals(true)).FirstOrDefault();
                    if (boPhanEntity == null)
                    {
                        var lastestBoPhan = dbContext.BoPhans.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
                        var lastestCode = lastestBoPhan != null ? lastestBoPhan.Order + 1 : 1;
                        boPhanEntity = new BoPhan()
                        {
                            PhongBanId = phongBanId,
                            Code = "BOPHAN" + lastestCode,
                            Name = employee.Part,
                            Alias = Utility.AliasConvert(employee.Part),
                            Order = lastestCode
                        };
                        dbContext.BoPhans.InsertOne(boPhanEntity);
                    }
                    boPhanId = boPhanEntity.Id;
                }

                // Chuc vu
                if (!string.IsNullOrEmpty(employee.Title))
                {
                    var chucVuEntity = dbContext.ChucVus.Find(m => m.Name.Equals(employee.Title.ToUpper()) && m.Enable.Equals(true)).FirstOrDefault();
                    if (chucVuEntity == null)
                    {
                        var lastestChucVu = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
                        var lastestCode = lastestChucVu != null ? lastestChucVu.Order + 1 : 1;
                        chucVuEntity = new ChucVu()
                        {
                            CongTyChiNhanhId = CongTyChiNhanhVP,
                            KhoiChucNangId = khoiChucNangVP,
                            PhongBanId = phongBanId,
                            BoPhanId = boPhanId,
                            Code = "CHUCVU" + lastestCode,
                            Name = employee.Title,
                            Alias = Utility.AliasConvert(employee.Title),
                            Order = lastestCode
                        };
                        dbContext.ChucVus.InsertOne(chucVuEntity);
                    }
                    chucVuId = chucVuEntity.Id;
                }

                var builder = Builders<Employee>.Filter;
                var filter = builder.Eq(m => m.Id, employee.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.CongTyChiNhanh, congTyChiNhanhVP)
                    .Set(m => m.KhoiChucNang, khoiChucNangVP)
                    .Set(m => m.PhongBan, phongBanId)
                    .Set(m => m.BoPhan, boPhanId)
                    .Set(m => m.BoPhanCon, boPhanConId)
                    .Set(m => m.ChucVu, chucVuId);
                dbContext.Employees.UpdateOne(filter, update);
            }
            #endregion

            #region PhongBan NM
            var KhoiQuanTriTongHop = dbContext.KhoiChucNangs.Find(m => m.Name.Equals("KHỐI QUẢN TRỊ TỔNG HỢP")).FirstOrDefault().Id;
            var KhoiSanXuat = dbContext.KhoiChucNangs.Find(m => m.Name.Equals("KHỐI SẢN XUẤT")).FirstOrDefault().Id;

            var lastestPhongBan2 = dbContext.PhongBans.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
            var lastestCode2 = lastestPhongBan2 != null ? lastestPhongBan2.Order + 1 : 1;

            i = lastestCode2;
            name = "PHÒNG QUẢN TRỊ SẢN XUẤT";
            dbContext.PhongBans.InsertOne(new PhongBan()
            {
                KhoiChucNangId = KhoiQuanTriTongHop,
                Code = "PHONGBAN" + i,
                Name = name,
                Alias = Utility.AliasConvert(name),
                Order = i
            });
            i++;
            name = "AN NINH";
            dbContext.PhongBans.InsertOne(new PhongBan()
            {
                KhoiChucNangId = KhoiQuanTriTongHop,
                Code = "PHONGBAN" + i,
                Name = name,
                Alias = Utility.AliasConvert(name),
                Order = i
            });
            i++;
            name = "DỰ ÁN";
            dbContext.PhongBans.InsertOne(new PhongBan()
            {
                KhoiChucNangId = KhoiQuanTriTongHop,
                Code = "PHONGBAN" + i,
                Name = name,
                Alias = Utility.AliasConvert(name),
                Order = i
            });
            i++;
            name = "KỸ THUẬT";
            dbContext.PhongBans.InsertOne(new PhongBan()
            {
                KhoiChucNangId = KhoiSanXuat,
                Code = "PHONGBAN" + i,
                Name = name,
                Alias = Utility.AliasConvert(name),
                Order = i
            });
            i++;
            name = "MÔI TRƯỜNG";
            dbContext.PhongBans.InsertOne(new PhongBan()
            {
                KhoiChucNangId = KhoiSanXuat,
                Code = "PHONGBAN" + i,
                Name = name,
                Alias = Utility.AliasConvert(name),
                Order = i
            });
            i++;
            name = "SẢN XUẤT";
            dbContext.PhongBans.InsertOne(new PhongBan()
            {
                KhoiChucNangId = KhoiSanXuat,
                Code = "PHONGBAN" + i,
                Name = name,
                Alias = Utility.AliasConvert(name),
                Order = i
            });
            i++;
            #endregion

            #region BoPhan

            #endregion

            #region ChucVu

            #endregion

            #region NHA MAY
            // Put file in ""
            //Create COM Objects.
            Application excelApp = new Application();
            if (excelApp == null)
            {
                Console.WriteLine("Excel is not installed!!");
                return;
            }
            //C:\Projects\Files
            Workbook excelBook = excelApp.Workbooks.Open(@"C:\Projects\Files\nha-may-structure.xlsx");
            _Worksheet excelSheet = excelBook.Sheets[1];
            Range excelRange = excelSheet.UsedRange;

            int rows = excelRange.Rows.Count;
            int cols = excelRange.Columns.Count;

            var congTyChiNhanhId = dbContext.CongTyChiNhanhs.Find(m => m.Code.Equals("CT2")).FirstOrDefault().Id;
            // UPDATE BOSS
            var employeeHuy = dbContext.Employees.Find(m => m.FullName.Equals("Lê Thanh Huy")).FirstOrDefault();
            if (employeeHuy != null)
            {
                var filter = Builders<Employee>.Filter.Eq(m => m.Id, employeeHuy.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.CongTyChiNhanh, congTyChiNhanhId);
                dbContext.Employees.UpdateMany(filter, update);

                var filterHis = Builders<Employee>.Filter.Eq(m => m.EmployeeId, employeeHuy.Id);
                var updateHis = Builders<Employee>.Update
                    .Set(m => m.CongTyChiNhanh, congTyChiNhanhId);
                dbContext.EmployeeHistories.UpdateMany(filterHis, updateHis);
            }

            for (i = 2; i <= rows; i++)
            {
                Console.Write("\r\n");
                var khoichucnang = string.Empty;
                var phongban = string.Empty;
                var bophan = string.Empty;
                var stt = string.Empty;
                var fullName = string.Empty;
                var bophancon = string.Empty;
                var chucvu = string.Empty;
                var ghichu = string.Empty;
                var y = 1;
                if (excelRange.Cells[i, y] != null && excelRange.Cells[i, y].Value2 != null)
                {
                    khoichucnang = excelRange.Cells[i, y].Value2.ToString();
                }
                y++;
                if (excelRange.Cells[i, y] != null && excelRange.Cells[i, y].Value2 != null)
                {
                    phongban = excelRange.Cells[i, y].Value2.ToString();
                }
                y++;
                if (excelRange.Cells[i, y] != null && excelRange.Cells[i, y].Value2 != null)
                {
                    bophan = excelRange.Cells[i, y].Value2.ToString();
                }
                y++;
                if (excelRange.Cells[i, y] != null && excelRange.Cells[i, y].Value2 != null)
                {
                    stt = excelRange.Cells[i, y].Value2.ToString();
                }
                y++;
                if (excelRange.Cells[i, y] != null && excelRange.Cells[i, y].Value2 != null)
                {
                    fullName = excelRange.Cells[i, y].Value2.ToString();
                }
                y++;
                if (excelRange.Cells[i, y] != null && excelRange.Cells[i, y].Value2 != null)
                {
                    bophancon = excelRange.Cells[i, y].Value2.ToString();
                }
                y++;
                if (excelRange.Cells[i, y] != null && excelRange.Cells[i, y].Value2 != null)
                {
                    chucvu = excelRange.Cells[i, y].Value2.ToString();
                }
                y++;
                if (excelRange.Cells[i, y] != null && excelRange.Cells[i, y].Value2 != null)
                {
                    ghichu = excelRange.Cells[i, y].Value2.ToString();
                }


                var khoiChucNangId = string.Empty;
                var phongBanId = string.Empty;
                var boPhanId = string.Empty;
                var boPhanConId = string.Empty;
                var chucVuId = string.Empty;
                if (!string.IsNullOrEmpty(khoichucnang))
                {
                    khoiChucNangId = dbContext.KhoiChucNangs.Find(m => m.Name.Equals(khoichucnang)).FirstOrDefault().Id;
                }
                if (!string.IsNullOrEmpty(khoichucnang))
                {
                    phongBanId = dbContext.PhongBans.Find(m => m.Name.Equals(phongban)).FirstOrDefault().Id;
                }
                if (!string.IsNullOrEmpty(bophan))
                {
                    var boPhanEntity = dbContext.BoPhans.Find(m => m.Name.Equals(bophan) && string.IsNullOrEmpty(m.Parent) && m.Enable.Equals(true)).FirstOrDefault();
                    if (boPhanEntity == null)
                    {
                        var lastestBoPhan = dbContext.BoPhans.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
                        var lastestCode = lastestBoPhan != null ? lastestBoPhan.Order + 1 : 1;
                        boPhanEntity = new BoPhan()
                        {
                            PhongBanId = phongBanId,
                            Code = "BOPHAN" + lastestCode,
                            Name = bophan,
                            Alias = Utility.AliasConvert(bophan),
                            Order = lastestCode
                        };
                        dbContext.BoPhans.InsertOne(boPhanEntity);
                    }
                    boPhanId = boPhanEntity.Id;

                    if (!string.IsNullOrEmpty(bophancon))
                    {
                        var boPhanConEntity = dbContext.BoPhans.Find(m => m.Name.Equals(bophancon) && m.Parent.Equals(boPhanId) && m.Enable.Equals(true)).FirstOrDefault();
                        if (boPhanConEntity == null)
                        {
                            var lastestBoPhan = dbContext.BoPhans.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
                            var lastestCode = lastestBoPhan != null ? lastestBoPhan.Order + 1 : 1;
                            boPhanConEntity = new BoPhan()
                            {
                                Code = "BOPHANC" + lastestCode,
                                Name = bophancon,
                                Alias = Utility.AliasConvert(bophancon),
                                Order = lastestCode,
                                Parent = boPhanId
                            };
                            dbContext.BoPhans.InsertOne(boPhanConEntity);
                        }
                        boPhanConId = boPhanConEntity.Id;
                    }
                }

                if (!string.IsNullOrEmpty(chucvu))
                {
                    var chucVuEntity = dbContext.ChucVus.Find(m => m.Name.Equals(chucvu) && m.Enable.Equals(true)).FirstOrDefault();
                    if (chucVuEntity == null)
                    {
                        var lastestChucVu = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
                        var lastestCode = lastestChucVu != null ? lastestChucVu.Order + 1 : 1;
                        chucVuEntity = new ChucVu()
                        {
                            CongTyChiNhanhId = congTyChiNhanhId,
                            KhoiChucNangId = khoiChucNangId,
                            PhongBanId = phongBanId,
                            BoPhanId = boPhanConId,
                            Code = "CHUCVU" + lastestCode,
                            Name = chucvu,
                            Alias = Utility.AliasConvert(chucvu),
                            Order = lastestCode
                        };
                        dbContext.ChucVus.InsertOne(chucVuEntity);
                    }
                    chucVuId = chucVuEntity.Id;
                }

                // update Employee
                if (!string.IsNullOrEmpty(fullName))
                {
                    // check exist
                    var employee = dbContext.Employees.Find(m => m.FullName.Equals(fullName)).FirstOrDefault();
                    if (employee != null)
                    {
                        var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                        var update = Builders<Employee>.Update
                            .Set(m => m.CongTyChiNhanh, congTyChiNhanhId)
                            .Set(m => m.KhoiChucNang, khoiChucNangId)
                            .Set(m => m.PhongBan, phongBanId)
                            .Set(m => m.BoPhan, boPhanId)
                            .Set(m => m.BoPhanCon, boPhanConId)
                            .Set(m => m.ChucVu, chucVuId);
                        dbContext.Employees.UpdateMany(filter, update);

                        var filterHis = Builders<Employee>.Filter.Eq(m => m.EmployeeId, employee.Id);
                        var updateHis = Builders<Employee>.Update
                            .Set(m => m.CongTyChiNhanh, congTyChiNhanhId)
                            .Set(m => m.KhoiChucNang, khoiChucNangId)
                            .Set(m => m.PhongBan, phongBanId)
                            .Set(m => m.BoPhan, boPhanId)
                            .Set(m => m.BoPhanCon, boPhanConId)
                            .Set(m => m.ChucVu, chucVuId);
                        dbContext.EmployeeHistories.UpdateMany(filterHis, updateHis);
                    }
                    else
                    {
                        var entity = new Employee
                        {
                            FullName = fullName,
                            CongTyChiNhanh = congTyChiNhanhId,
                            KhoiChucNang = khoiChucNangId,
                            PhongBan = phongBanId,
                            BoPhan = boPhanId,
                            BoPhanCon = boPhanConId,
                            ChucVu = chucvu,
                            Joinday = DateTime.Now
                        };
                        #region System Generate
                        var pwdrandom = Guid.NewGuid().ToString("N").Substring(0, 6);
                        var settings = dbContext.Settings.Find(m => true).ToList();
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
                }

                Console.Write(fullName + "\t");
            }

            //after reading, relaase the excel project
            excelApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
            #endregion
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
                Date = new DateTime(2019, 2, 3),
                Name = "Tết năm Kỷ Hợi",
                Detail = "29 tháng Chạp năm Mậu Tuất"
            });

            dbContext.Holidays.InsertOne(new Holiday
            {
                Date = new DateTime(2019, 2, 4),
                Name = "Tết năm Kỷ Hợi",
                Detail = "30 tháng Chạp năm Mậu Tuất"
            });

            dbContext.Holidays.InsertOne(new Holiday
            {
                Date = new DateTime(2019, 2, 5),
                Name = "Tết năm Kỷ Hợi",
                Detail = "01 Tết năm Kỷ Hợi"
            });

            dbContext.Holidays.InsertOne(new Holiday
            {
                Date = new DateTime(2019, 2, 6),
                Name = "Tết năm Kỷ Hợi",
                Detail = "02 Tết năm Kỷ Hợi"
            });

            dbContext.Holidays.InsertOne(new Holiday
            {
                Date = new DateTime(2019, 2, 7),
                Name = "Tết năm Kỷ Hợi",
                Detail = "03 Tết năm Kỷ Hợi"
            });

            dbContext.Holidays.InsertOne(new Holiday
            {
                Date = new DateTime(2019, 2, 8),
                Name = "Tết năm Kỷ Hợi",
                Detail = "04 Tết năm Kỷ Hợi"
            });

            dbContext.Holidays.InsertOne(new Holiday
            {
                Date = new DateTime(2019, 2, 9),
                Name = "Tết năm Kỷ Hợi",
                Detail = "05 Tết năm Kỷ Hợi"
            });

            dbContext.Holidays.InsertOne(new Holiday
            {
                Date = new DateTime(2019, 2, 10),
                Name = "Tết năm Kỷ Hợi",
                Detail = "06 Tết năm Kỷ Hợi"
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

        static void UpdateTimerDepartmentAlias(string connection, string database)
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
                var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.EmployeeId, employee.Id);
                var update = Builders<EmployeeWorkTimeLog>.Update
                    .Set(m => m.DepartmentId, employee.DepartmentId)
                    .Set(m => m.DepartmentAlias, employee.DepartmentAlias);
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

        #region Factories: Init data
        static void ResetDataFactorySalary(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion


            dbContext.FactoryProductCongTheoThangs.DeleteMany(m => true);
            dbContext.FactoryProductCongTheoNgays.DeleteMany(m => true);
            dbContext.EmployeeCongs.DeleteMany(m => m.Type.Equals((int)EKhoiLamViec.SX));
            dbContext.SalaryEmployeeMonths.DeleteMany(m => true);
        }

        static void InitDataLuong(string connection, string database, string phase)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            // 1. Employee (if not exist)
            // 2. CreditEmployee
            // 3. EmployeeWorkTimeMonthLog

            // Put file in ""
            //Create COM Objects.
            Application excelApp = new Application();
            if (excelApp == null)
            {
                Console.WriteLine("Excel is not installed!!");
                return;
            }
            //C:\Projects\Files
            Workbook excelBook = excelApp.Workbooks.Open(@"C:\Projects\Files\" + phase + "-credits.xlsx");
            _Worksheet excelSheet = excelBook.Sheets[1];
            Range excelRange = excelSheet.UsedRange;

            int rows = excelRange.Rows.Count;
            int cols = excelRange.Columns.Count;

            var time = string.Empty;
            if (excelRange.Cells[6, 2] != null && excelRange.Cells[6, 2].Value2 != null)
            {
                time = excelRange.Cells[6, 2].Value2.ToString();
            }

            var endMonthDate = Utility.GetToDate(time);
            var month = endMonthDate.Month;
            var year = endMonthDate.Year;

            for (int i = 11; i <= rows; i++)
            {
                Console.Write("\r\n");
                int columnIndex = 1;
                var ma = string.Empty;
                var ten = string.Empty;
                var chucvu = string.Empty;
                var ngayvaolam = string.Empty;
                double ngaylamviec = 0;
                double phepnam = 0;
                double letet = 0;
                decimal tamung = 0;
                decimal thuongletet = 0;
                decimal bhxh = 0;

                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    ma = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    ten = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    chucvu = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    ngayvaolam = excelRange.Cells[i, columnIndex].Value2.ToString();
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    ngaylamviec = Convert.ToDouble(excelRange.Cells[i, columnIndex].Value2);
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    phepnam = Convert.ToDouble(excelRange.Cells[i, columnIndex].Value2);
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    letet = Convert.ToDouble(excelRange.Cells[i, columnIndex].Value2);
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    tamung = Convert.ToDecimal(excelRange.Cells[i, columnIndex].Value2);
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    thuongletet = Convert.ToDecimal(excelRange.Cells[i, columnIndex].Value2);
                }
                columnIndex++;
                if (excelRange.Cells[i, columnIndex] != null && excelRange.Cells[i, columnIndex].Value2 != null)
                {
                    bhxh = Convert.ToDecimal(excelRange.Cells[i, columnIndex].Value2);
                }
                columnIndex++;

                if (string.IsNullOrEmpty(ten))
                {
                    continue;
                }

                var existEmployee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                if (existEmployee != null)
                {
                    DataSanXuat(dbContext, month, year, ngaylamviec, phepnam, letet, tamung, thuongletet, bhxh, existEmployee);
                }
                else
                {
                    InsertNewEmployee(connection, database, ten, ma, chucvu, ngayvaolam);
                    var employee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                    DataSanXuat(dbContext, month, year, ngaylamviec, phepnam, letet, tamung, thuongletet, bhxh, employee);
                }
            }
            //after reading, relaase the excel project
            excelApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
        }

        private static void DataSanXuat(MongoDBContext dbContext, int month, int year, double ngaylamviec, double phepnam, double letet, decimal tamung, decimal thuongletet, decimal bhxh, Employee existEmployee)
        {
            // Salary base month-year
            var existSalary = dbContext.SalaryEmployeeMonths.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(existEmployee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (existSalary != null)
            {
                var builderSalary = Builders<SalaryEmployeeMonth>.Filter;
                var filterSalary = builderSalary.Eq(m => m.Id, existSalary.Id);
                var updateSalary = Builders<SalaryEmployeeMonth>.Update
                    .Set(m => m.NgayCongLamViec, ngaylamviec)
                    .Set(m => m.NgayNghiLeTetHuongLuong, letet)
                    .Set(m => m.NgayNghiPhepNam, phepnam)
                    .Set(m => m.TamUng, tamung)
                    .Set(m => m.LuongThamGiaBHXH, bhxh)
                    .Set(m => m.ThuongLeTet, thuongletet)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalaryEmployeeMonths.UpdateOne(filterSalary, updateSalary);
            }
            else
            {
                dbContext.SalaryEmployeeMonths.InsertOne(new SalaryEmployeeMonth
                {
                    Month = month,
                    Year = year,
                    EmployeeId = existEmployee.Id,
                    MaNhanVien = existEmployee.CodeOld,
                    PhongBan = existEmployee.Department,
                    ChucVu = existEmployee.Title,
                    SalaryMaSoChucDanhCongViec = "B.05",
                    NgayCongLamViec = ngaylamviec,
                    NgayNghiPhepNam = phepnam,
                    NgayNghiLeTetHuongLuong = letet,
                    ThuongLeTet = thuongletet,
                    TamUng = tamung,
                    LuongThamGiaBHXH = bhxh
                });
            }

            // EmployeeWorkTimeMonthLog
            var existTimes = dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(existEmployee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (existTimes != null)
            {
                var filterTime = Builders<EmployeeWorkTimeMonthLog>.Filter.Eq(m => m.Id, existTimes.Id);
                var updateTime = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Set(m => m.NgayLamViecChinhTay, ngaylamviec)
                    .Set(m => m.PhepNamChinhTay, phepnam)
                    .Set(m => m.LeTetChinhTay, letet);
                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterTime, updateTime);
            }
            else
            {
                dbContext.EmployeeWorkTimeMonthLogs.InsertOne(new EmployeeWorkTimeMonthLog
                {
                    Year = year,
                    Month = month,
                    EmployeeId = existEmployee.Id,
                    EmployeeName = existEmployee.FullName,
                    Title = existEmployee.TitleId,
                    Department = existEmployee.DepartmentId,
                    Part = existEmployee.PartId,
                    NgayLamViecChinhTay = ngaylamviec,
                    LeTetChinhTay = letet,
                    PhepNamChinhTay = phepnam
                });
            }

            // CreditEmployee
            if (tamung > 0)
            {
                var existCredit = dbContext.CreditEmployees.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECredit.UngLuong) && m.EmployeeId.Equals(existEmployee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                if (existCredit != null)
                {
                    var filterCredit = Builders<CreditEmployee>.Filter.Eq(m => m.Id, existCredit.Id);
                    var updateCredit = Builders<CreditEmployee>.Update
                        .Set(m => m.Money, tamung);
                    dbContext.CreditEmployees.UpdateOne(filterCredit, updateCredit);
                }
                else
                {
                    dbContext.CreditEmployees.InsertOne(new CreditEmployee
                    {
                        Year = year,
                        Month = month,
                        EmployeeId = existEmployee.Id,
                        EmployeeCode = existEmployee.CodeOld,
                        FullName = existEmployee.FullName,
                        EmployeeTitle = existEmployee.TitleId,
                        EmployeeDepartment = existEmployee.DepartmentId,
                        EmployeePart = existEmployee.PartId,
                        Type = (int)ECredit.UngLuong,
                        Money = tamung,
                        DateCredit = new DateTime(year, month, 1),
                        DateFirstPay = new DateTime(year, month, 5).AddMonths(1)
                    });
                }
            }
        }

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
                Department = "NHÀ MÁY",
                Part = "SẢN XUÂT",
                Title = chucvu,
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

            for (int i = 6; i <= 55; i++)
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
            for (int i = 68; i <= 117; i++)
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
            var price = (decimal)Math.Round(7000000 / 26 / 7.3, 2);

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
                    employee.Title,
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

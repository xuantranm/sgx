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

            InitFactoryProductDinhMucTangCa(connection, database);
            InitFactoryProductDinhMucTiLe(connection, database);
            InitFactoryProductDonGiaM3(connection, database);
            InitFactoryProductDinhMuc(connection, database);
            InitFactoryCongViec(connection, database);
            #endregion

            #region Factories: Update data

            #endregion

            Console.Write("\r\n");
            Console.Write("Done..... Press any key to exist!");
            Console.ReadLine();
        }

        #region ERP
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
            foreach(var item in employees)
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

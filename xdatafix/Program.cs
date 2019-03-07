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
            InitFactoryProductDonGiaM3(connection, database);
            //InitFactoryProductDinhMuc(connection, database);
            //InitFactoryCongViec(connection, database);

            ResetDataFactorySalary(connection, database);
            //InitDataLuong(connection, database, "07-2019");
            #endregion

            #region Factories: Update data

            #endregion

            Console.Write("\r\n");
            Console.Write("Done..... Press any key to exist!");
            Console.ReadLine();
        }

        #region Tribatvn
        public void InitProductSales(string connection, string database)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            dbContext.ProductSales.DeleteMany(m=>true);

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
            DateTime joinday = string.IsNullOrEmpty(ngayvaolam) ? DateTime.Now: DateTime.FromOADate(Convert.ToDouble(ngayvaolam));

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
                var dongiatangcaM3 = Convert.ToDecimal((double)dongiaM3 * (1 +  dongiatangcaphantram / 100));

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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Data;
using ViewModels;
using Models;
using Common.Utilities;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Common.Enums;
using NPOI.HSSF.Util;
using NPOI.SS.Util;

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Factory)]
    public class SalaryFactoryController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalaryFactoryController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalaryFactoryController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _logger = logger;
        }

        [Route(Constants.LinkSalary.BangLuong)]
        public async Task<IActionResult> BangLuong(string thang, string id, string phongban)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongNM, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 02, 01);
            var endDate = DateTime.Now;
            while (date.Year < endDate.Year || (date.Year == endDate.Year && date.Month <= endDate.Month))
            {
                monthYears.Add(new MonthYear
                {
                    Month = date.Month,
                    Year = date.Year
                });
                date = date.AddMonths(1);
            }
            if (endDate.Day > 25)
            {
                monthYears.Add(new MonthYear
                {
                    Month = endDate.AddMonths(1).Month,
                    Year = endDate.AddMonths(1).Year
                });
            }
            var sortTimes = monthYears.OrderByDescending(x => x.Month).OrderByDescending(x => x.Year).ToList();

            var departments = await dbContext.Departments.Find(m => m.Enable.Equals(true)).SortBy(m=>m.Order).ToListAsync();

            var employeeDdl = await dbContext.Employees.Find(m => m.Enable.Equals(true) && m.SalaryType.Equals((int)EKhoiLamViec.NM) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
            #endregion

            #region Times
            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = toDate.Day > 25 ? new DateTime(toDate.Year, toDate.Month, 26) : new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Day > 25 ? toDate.AddMonths(1).Year : toDate.Year;
            var month = toDate.Day > 25 ? toDate.AddMonths(1).Month : toDate.Month;
            thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;

            int yearSale = new DateTime(year, month, 01).AddMonths(-2).Year;
            int monthSale = new DateTime(year, month, 01).AddMonths(-2).Month;
            var saleTimes = monthSale + "-" + yearSale;
            #endregion

            #region Setting
            // Init
            //dbContext.SalarySettings.InsertOne(new SalarySetting()
            //{
            //    Key = "mau-so-chuyen-can",
            //    Value = "300000",
            //    Title = "Muc thuong chuyen can"
            //});

            //dbContext.SalarySettings.InsertOne(new SalarySetting()
            //{
            //    Key = "mau-so-tien-com",
            //    Value = "22000",
            //    Title = "Đơn giá tiền cơm"
            //});
            // End init
            var thamsoEntity = dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToList();
            var mauSo = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-lam-viec")).Value);
            var mauSoBaoVe = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-bao-ve")).Value);
            var tyledongbh = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ty-le-dong-bh")).Value);
            var mauSoChuyenCan = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-chuyen-can")).Value);
            var mauSoTienCom = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-tien-com")).Value);
            #endregion

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true) & builder.Eq(m=>m.SalaryType, (int)EKhoiLamViec.NM) & !builder.Eq(m=>m.UserName, Constants.System.account);
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.Id, id.Trim());
            }
            if (!string.IsNullOrEmpty(phongban))
            {
                filter = filter & builder.Eq(m => m.DepartmentAlias, phongban.Trim());
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Employee>.Sort.Ascending(m => m.Code);
            #endregion

            var employees = await dbContext.Employees.Find(filter).Sort(sortBuilder).ToListAsync();

            var salaryEmployeeMonths = new List<SalaryEmployeeMonth>();
            foreach (var employee in employees)
            {
                mauSo = employee.SalaryMauSo != 26 ? 30 : mauSo;
                var salary = GetSalaryEmployeeMonth(year, month, employee.Id, employee.Joinday, employee.NgachLuong, employee.SalaryLevel, (int)mauSo, mauSoChuyenCan, mauSoTienCom, employee.LuongBHXH, tyledongbh, null, true);
                salary.Year = year;
                salary.Month = month;
                salary.Bac = employee.SalaryLevel;
                salary.ThamNienLamViec = employee.Joinday;
                salary.EmployeeId = employee.Id;
                salary.MaNhanVien = employee.Code + "-(" + employee.CodeOld + ")";
                salary.FullName = employee.FullName;
                salary.NoiLamViec = Constants.Location(employee.SalaryType);
                salary.NoiLamViecOrder = employee.SalaryNoiLamViecOrder;
                salary.PhongBan = employee.Department;
                salary.PhongBanOrder = employee.SalaryPhongBanOrder;
                salary.ChucVu = employee.Title;
                salary.ChucVuOrder = employee.SalaryChucVuOrder;
                salary.ViTriCode = employee.SalaryChucVuViTriCode;
                salary.SalaryMaSoChucDanhCongViec = employee.NgachLuong;
                salaryEmployeeMonths.Add(salary);
            }

            var mucluongvung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).FirstOrDefaultAsync();
            if (mucluongvung == null)
            {
                var lastItemVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonthVung = lastItemVung.Month;
                var lastYearVung = lastItemVung.Year;

                lastItemVung.Id = null;
                lastItemVung.Month = month;
                lastItemVung.Year = year;
                dbContext.SalaryMucLuongVungs.InsertOne(lastItemVung);
                mucluongvung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).FirstOrDefaultAsync();
            }

            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = salaryEmployeeMonths,
                SalaryMucLuongVung = mucluongvung,
                MonthYears = sortTimes,
                Thang = thang,
                SaleTimes = saleTimes,
                Departments = departments,
                EmployeesDdl= employeeDdl,
                Id = id,
                Phongban = phongban
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.BangLuong +"/"+ Constants.ActionLink.Update)]
        public async Task<IActionResult> BangLuongUpdate(string thang, string id, string phongban)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongNM, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 02, 01);
            var endDate = DateTime.Now;
            while (date.Year < endDate.Year || (date.Year == endDate.Year && date.Month <= endDate.Month))
            {
                monthYears.Add(new MonthYear
                {
                    Month = date.Month,
                    Year = date.Year
                });
                date = date.AddMonths(1);
            }
            if (endDate.Day > 25)
            {
                monthYears.Add(new MonthYear
                {
                    Month = endDate.AddMonths(1).Month,
                    Year = endDate.AddMonths(1).Year
                });
            }
            var sortTimes = monthYears.OrderByDescending(x => x.Month).OrderByDescending(x => x.Year).ToList();

            var departments = await dbContext.Departments.Find(m => m.Enable.Equals(true)).SortBy(m => m.Order).ToListAsync();

            var employeeDdl = await dbContext.Employees.Find(m => m.Enable.Equals(true) && m.SalaryType.Equals((int)EKhoiLamViec.NM) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
            #endregion

            #region Times
            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = toDate.Day > 25 ? new DateTime(toDate.Year, toDate.Month, 26) : new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Day > 25 ? toDate.AddMonths(1).Year : toDate.Year;
            var month = toDate.Day > 25 ? toDate.AddMonths(1).Month : toDate.Month;
            thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;

            int yearSale = new DateTime(year, month, 01).AddMonths(-2).Year;
            int monthSale = new DateTime(year, month, 01).AddMonths(-2).Month;
            var saleTimes = monthSale + "-" + yearSale;
            #endregion

            #region Setting
            var thamsoEntity = dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToList();
            var mauSo = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-lam-viec")).Value);
            var mauSoBaoVe = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-bao-ve")).Value);
            var tyledongbh = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ty-le-dong-bh")).Value);
            var mauSoChuyenCan = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-chuyen-can")).Value);
            var mauSoTienCom = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-tien-com")).Value);
            #endregion

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true) & builder.Eq(m => m.SalaryType, (int)EKhoiLamViec.NM) & !builder.Eq(m => m.UserName, Constants.System.account);
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.Id, id.Trim());
            }
            if (!string.IsNullOrEmpty(phongban))
            {
                filter = filter & builder.Eq(m => m.DepartmentAlias, phongban.Trim());
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Employee>.Sort.Ascending(m => m.Code);
            #endregion

            var employees = await dbContext.Employees.Find(filter).Sort(sortBuilder).ToListAsync();

            var salaryEmployeeMonths = new List<SalaryEmployeeMonth>();
            foreach (var employee in employees)
            {
                mauSo = employee.SalaryMauSo != 26 ? 30 : mauSo;
                var salary = GetSalaryEmployeeMonth(year, month, employee.Id, employee.Joinday, employee.NgachLuong, employee.SalaryLevel, (int)mauSo, mauSoChuyenCan, mauSoTienCom, employee.LuongBHXH, tyledongbh, null, true);
                salary.Year = year;
                salary.Month = month;
                salary.Bac = employee.SalaryLevel;
                salary.ThamNienLamViec = employee.Joinday;
                salary.EmployeeId = employee.Id;
                salary.MaNhanVien = employee.Code + "-(" + employee.CodeOld + ")";
                salary.FullName = employee.FullName;
                salary.NoiLamViec = Constants.Location(employee.SalaryType);
                salary.NoiLamViecOrder = employee.SalaryNoiLamViecOrder;
                salary.PhongBan = employee.Department;
                salary.PhongBanOrder = employee.SalaryPhongBanOrder;
                salary.ChucVu = employee.Title;
                salary.ChucVuOrder = employee.SalaryChucVuOrder;
                salary.ViTriCode = employee.SalaryChucVuViTriCode;
                salary.SalaryMaSoChucDanhCongViec = employee.NgachLuong;
                salaryEmployeeMonths.Add(salary);
            }

            var mucluongvung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).FirstOrDefaultAsync();
            if (mucluongvung == null)
            {
                var lastItemVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonthVung = lastItemVung.Month;
                var lastYearVung = lastItemVung.Year;

                lastItemVung.Id = null;
                lastItemVung.Month = month;
                lastItemVung.Year = year;
                dbContext.SalaryMucLuongVungs.InsertOne(lastItemVung);
                mucluongvung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).FirstOrDefaultAsync();
            }

            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = salaryEmployeeMonths,
                SalaryMucLuongVung = mucluongvung,
                MonthYears = sortTimes,
                Thang = thang,
                SaleTimes = saleTimes,
                Departments = departments,
                EmployeesDdl = employeeDdl,
                Id = id,
                Phongban = phongban
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.BangLuong + "/" + Constants.ActionLink.Update)]
        public async Task<IActionResult> BangLuongUpdate(BangLuongViewModel viewModel)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            // For security calculator again => update
            // For demo: do later
            try
            {
                #region Times
                var now = DateTime.Now;
                var thang = viewModel.Thang;
                var toDate = Utility.WorkingMonthToDate(thang);
                var fromDate = toDate.AddMonths(-1).AddDays(1);
                if (string.IsNullOrEmpty(thang))
                {
                    toDate = DateTime.Now;
                    fromDate = toDate.Day > 25 ? new DateTime(toDate.Year, toDate.Month, 26) : new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
                }
                var year = toDate.Day > 25 ? toDate.AddMonths(1).Year : toDate.Year;
                var month = toDate.Day > 25 ? toDate.AddMonths(1).Month : toDate.Month;
                thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;
                #endregion

                var models = viewModel.SalaryEmployeeMonths;
                foreach (var item in models)
                {
                    var builder = Builders<SalaryEmployeeMonth>.Filter;
                    var filter = builder.Eq(m => m.Id, item.Id);
                    var update = Builders<SalaryEmployeeMonth>.Update
                        .Set(m => m.SalaryMaSoChucDanhCongViec, item.SalaryMaSoChucDanhCongViec)
                        .Set(m => m.Bac, item.Bac)
                        .Set(m => m.PhuCapKhac, item.PhuCapKhac)
                        .Set(m => m.ThuongLeTet, item.ThuongLeTet)
                        .Set(m => m.LuongThamGiaBHXH, item.LuongThamGiaBHXH)
                        .Set(m => m.HoTroNgoaiLuong, item.HoTroNgoaiLuong)
                        .Set(m => m.UpdatedOn, now);
                    dbContext.SalaryEmployeeMonths.UpdateOne(filter, update);
                }
                return Json(new { result = true, source = "update", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "update", id = string.Empty, message = ex.Message });
            }
        }

        public SalaryEmployeeMonth GetSalaryEmployeeMonth(int year, int month, string employeeId, DateTime thamnienlamviec, string ngachLuong, int bac, int mauSo, decimal mauSoChuyenCan, decimal mauSoTienCom, decimal bhxh, decimal tyledongbh, SalaryEmployeeMonth newSalary, bool save)
        {
            var today = DateTime.Now.Date;
            int todayMonth = today.Day > 25 ? today.AddMonths(1).Month : today.Month;
            var endDateMonth = new DateTime(year, month, 25);
            var startDateMonth = endDateMonth.AddMonths(-1).AddDays(1);

            decimal luongCB = GetLuongCB(ngachLuong, bac);

            var currentSalary = new SalaryEmployeeMonth();
            var existSalary = dbContext.SalaryEmployeeMonths.CountDocuments(m => m.EmployeeId.Equals(employeeId) & m.Year.Equals(year) & m.Month.Equals(month)) > 0 ? true : false;
            // debug
            existSalary = false;
            // end debug
            if (existSalary)
            {
                currentSalary = dbContext.SalaryEmployeeMonths.Find(m => m.EmployeeId.Equals(employeeId) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefault();
            }
            else
            {
                var lastestSalary = dbContext.SalaryEmployeeMonths.Find(m => m.EmployeeId.Equals(employeeId)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
                var ngaythamnien = (endDateMonth - thamnienlamviec).TotalDays;
                double thangthamnien = Math.Round(ngaythamnien / 30, 0);
                double namthamnien = Math.Round(thangthamnien / 12, 0);
                var hesothamnien = 0;
                // 3 năm đầu ko tăng, bắt đầu năm thứ 4 sẽ có thâm niên 3%, thêm 1 năm tăng 1%
                if (namthamnien >= 4)
                {
                    hesothamnien = 3;
                    for (int i = 5; i <= namthamnien; i++)
                    {
                        hesothamnien++;
                    }
                }
                currentSalary.ThamNienMonth = (int)thangthamnien;
                currentSalary.ThamNienYear = (int)namthamnien;
                currentSalary.HeSoThamNien = hesothamnien;
                //currentSalary.ThamNien = luongCB * Convert.ToDecimal(0.03 + (dateSpan.Years - 3) * 0.01);
                currentSalary.ThamNien = luongCB * hesothamnien/100;
                var phucapphuclois = dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.EmployeeId.Equals(employeeId) & m.Law.Equals(false)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).ToList();
                if (phucapphuclois.Find(m => m.Code.Equals("01-001")) != null)
                {
                    currentSalary.NangNhocDocHai = phucapphuclois.Find(m => m.Code.Equals("01-001")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("01-002")) != null)
                {
                    currentSalary.TrachNhiem = phucapphuclois.Find(m => m.Code.Equals("01-002")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("01-004")) != null)
                {
                    currentSalary.ThuHut = phucapphuclois.Find(m => m.Code.Equals("01-004")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-001")) != null)
                {
                    currentSalary.Xang = phucapphuclois.Find(m => m.Code.Equals("02-001")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-002")) != null)
                {
                    currentSalary.DienThoai = phucapphuclois.Find(m => m.Code.Equals("02-002")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-003")) != null)
                {
                    currentSalary.Com = phucapphuclois.Find(m => m.Code.Equals("02-003")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-004")) != null)
                {
                    currentSalary.KiemNhiem = phucapphuclois.Find(m => m.Code.Equals("02-004")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-005")) != null)
                {
                    currentSalary.BhytDacBiet = phucapphuclois.Find(m => m.Code.Equals("02-005")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-006")) != null)
                {
                    currentSalary.ViTriCanKnNhieuNam = phucapphuclois.Find(m => m.Code.Equals("02-006")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-007")) != null)
                {
                    currentSalary.ViTriDacThu = phucapphuclois.Find(m => m.Code.Equals("02-007")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-008")) != null)
                {
                    currentSalary.NhaO = phucapphuclois.Find(m => m.Code.Equals("02-008")).Money;
                }
                currentSalary.LuongThamGiaBHXH = lastestSalary != null ? lastestSalary.LuongThamGiaBHXH : bhxh;
            }

            if (newSalary != null)
            {
                currentSalary.Bac = newSalary.Bac;
                currentSalary.NangNhocDocHai = newSalary.NangNhocDocHai;
                currentSalary.TrachNhiem = newSalary.TrachNhiem;
                currentSalary.ThuHut = newSalary.ThuHut;
                currentSalary.DienThoai = newSalary.DienThoai;
                currentSalary.Xang = newSalary.Xang;
                currentSalary.Com = newSalary.Com;
                currentSalary.NhaO = newSalary.NhaO;
                currentSalary.KiemNhiem = newSalary.KiemNhiem;
                currentSalary.BhytDacBiet = newSalary.BhytDacBiet;
                currentSalary.ViTriCanKnNhieuNam = newSalary.ViTriCanKnNhieuNam;
                currentSalary.ViTriDacThu = newSalary.ViTriDacThu;
                currentSalary.LuongKhac = newSalary.LuongKhac;
                currentSalary.ThiDua = newSalary.ThiDua;
                currentSalary.HoTroNgoaiLuong = newSalary.HoTroNgoaiLuong;
                currentSalary.ThuongLeTet = newSalary.ThuongLeTet;
                currentSalary.LuongThamGiaBHXH = newSalary.LuongThamGiaBHXH;
            }

            decimal nangnhoc = currentSalary.NangNhocDocHai;
            decimal trachnhiem = currentSalary.TrachNhiem;
            decimal thamnien = currentSalary.ThamNien;
            decimal thuhut = currentSalary.ThuHut;
            decimal dienthoai = currentSalary.DienThoai;
            decimal xang = currentSalary.Xang;
            decimal com = currentSalary.Com;
            decimal nhao = currentSalary.NhaO;
            decimal kiemnhiem = currentSalary.KiemNhiem;
            decimal bhytdacbiet = currentSalary.BhytDacBiet;
            decimal vitricanknnhieunam = currentSalary.ViTriCanKnNhieuNam;
            decimal vitridacthu = currentSalary.ViTriDacThu;
            decimal luongKhac = currentSalary.LuongKhac;
            decimal thiDua = currentSalary.ThiDua;
            decimal hoTroNgoaiLuong = currentSalary.HoTroNgoaiLuong;
            decimal luongthamgiabhxh = currentSalary.LuongThamGiaBHXH;
            decimal thuongletet = currentSalary.ThuongLeTet;
            decimal luongcbbaogomphucap = currentSalary.LuongCoBanBaoGomPhuCap;
            luongcbbaogomphucap = luongCB + nangnhoc + trachnhiem + thamnien + thuhut + dienthoai + xang + com + nhao + kiemnhiem + bhytdacbiet + vitricanknnhieunam + vitridacthu;

            double ngayConglamViec = mauSo;
            double ngayNghiPhepNam = 0;
            // thai san, dam cuoi, ...
            double ngayNghiPhepHuongLuong = 0;
            double ngayNghiLeTetHuongLuong = 0;
            double congCNGio = 0;
            double phutcongCN = 0;
            double congTangCaNgayThuongGio = 0;
            double phutcongTangCaNgayThuong = 0;
            double congLeTet = 0;
            double phutcongLeTet = 0;
            decimal congTacXa = 0;
            double tongBunBoc = 0;
            decimal thanhTienBunBoc = 0;
            decimal mucDatTrongThang = 0;
            decimal luongTheoDoanhThuDoanhSo = 0;

            decimal congTong = 0;
            decimal comSX = 0;
            decimal comKD = 0;

            var chamCongs = dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(employeeId) & m.Year.Equals(year) & m.Month.Equals(month)).ToList();

            double nghikhongphep = 0;
            double nghiviecrieng = 0;
            double nghibenh = 0;
            int trukhongphep = 0;
            int truviecrieng = 0;
            int trubenh = 0;
            int chuyencanchiso = 0;

            if (chamCongs != null & chamCongs.Count > 0)
            {
                ngayConglamViec = 0;
                ngayNghiPhepNam = 0;
                ngayNghiPhepHuongLuong = 0;
                ngayNghiLeTetHuongLuong = 0;
                // Because don't define cong CN, tang ca, le tet.
                // Manage in Cong Tong
                //congCNGio = 0;
                //congTangCaNgayThuongGio = 0;
                //congLeTet = 0;
                foreach (var chamCong in chamCongs)
                {
                    ngayConglamViec += chamCong.Workday;
                    ngayNghiPhepNam += chamCong.NghiPhepNam;
                    nghikhongphep += chamCong.NghiKhongPhep + chamCong.KhongChamCong;
                    ngayNghiPhepHuongLuong += chamCong.NghiHuongLuong;
                    ngayNghiLeTetHuongLuong += chamCong.NghiLe;
                }
                if (ngayConglamViec == 0)
                {
                    ngayConglamViec = todayMonth == month ? Utility.BusinessDaysUntil(startDateMonth, today): Utility.BusinessDaysUntil(startDateMonth, endDateMonth);
                }
                if (nghikhongphep >= 1)
                {
                    trukhongphep = 100;
                }

                if (nghiviecrieng >= 3)
                {
                    truviecrieng = 100;
                }
                else if (nghiviecrieng > 1 && nghiviecrieng < 3)
                {
                    truviecrieng = 75;
                }
                else if (nghiviecrieng == 1)
                {
                    truviecrieng = 50;
                }

                if (nghibenh >= 4)
                {
                    trubenh = 100;
                }
                else if (nghibenh > 2 && nghibenh <= 3)
                {
                    trubenh = 75;
                }
                if (nghibenh > 1 && nghibenh <= 2)
                {
                    trubenh = 50;
                }
                if (nghibenh == 1)
                {
                    trubenh = 25;
                }
                chuyencanchiso = (trukhongphep + truviecrieng + trubenh) >=100 ? 100 : (trukhongphep + truviecrieng + trubenh);
            }


            //=L21/$N$1*M21
            decimal luongDinhMuc = Convert.ToDecimal((double)luongCB / mauSo * ngayConglamViec);

            // RESOLVE CONG TONG. UPDATE TO DB.
            // Query base employee, time.
            // Manage in collection [Congs]
            var dataSX = dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();

            if (dataSX != null)
            {
                ////congCNGio += dataSX.GioLamViecCN;
                ////congTangCaNgayThuongGio += dataSX.GioTangCa;
                ////congLeTet += dataSX.GioLamViecLeTet;
                //congTong = dataSX.CongTong;
                comSX = Convert.ToDecimal(dataSX.Com * (double)mauSoTienCom);
                comKD = Convert.ToDecimal(dataSX.ComKD * (double)mauSoTienCom);
            }

            //=((L21/$N$1)*M21)
            //+(O21*(L21/$N$1))
            //+((P21/8)*1.5*(L21/$N$1))
            //+((Q21/8)*2*L21/$N$1)
            //+(S21*3*(L21/$N$1)
            //+(L21/$N$1)*R21)
            decimal thanhTienLuongCB = ((luongCB / mauSo) * (decimal)ngayConglamViec)
                                    + ((decimal)ngayNghiPhepNam * (luongCB / mauSo))
                                    + ((decimal)(congTangCaNgayThuongGio / 8) * (decimal)1.5 * (luongCB / mauSo))
                                    + ((decimal)(congCNGio / 8) * 2 * (luongCB / mauSo))
                                    + ((decimal)congLeTet * 3 * (luongCB / mauSo))
                                    + (luongCB / mauSo) * (decimal)ngayNghiLeTetHuongLuong;

            
            decimal luongVuotDinhMuc = congTong - luongDinhMuc;
            if (luongVuotDinhMuc < 0)
            {
                luongVuotDinhMuc = 0;
            }

            decimal phucapchuyencan = ((100 - chuyencanchiso)/100) * mauSoChuyenCan;

            decimal phucapkhac = 0;
            decimal tongPhuCap = thamnien + phucapchuyencan + phucapkhac;


            decimal tongthunhap = thanhTienLuongCB + luongVuotDinhMuc + tongPhuCap;

            decimal bhxhbhyt = luongthamgiabhxh * tyledongbh;

            decimal tamung = 0;
            var credits = dbContext.CreditEmployees.Find(m => m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)).ToList();
            if (credits != null && credits.Count > 0)
            {
                foreach (var credit in credits)
                {
                    tamung += credit.Money;
                }
            }
            decimal thuclanh = tongthunhap - bhxhbhyt - tamung + thuongletet;

            decimal thucLanhTronSo = (Math.Round(thuclanh / 1000) * 1000) + hoTroNgoaiLuong + comSX + comKD;

            #region update field to currentSalary
            currentSalary.LuongCanBan = luongCB;
            currentSalary.ThamNien = thamnien;
            currentSalary.LuongCoBanBaoGomPhuCap = luongcbbaogomphucap;
            

            currentSalary.NgayCongLamViec = ngayConglamViec;
            currentSalary.NgayNghiPhepNam = ngayNghiPhepNam;
            currentSalary.NgayNghiPhepHuongLuong = ngayNghiPhepHuongLuong;
            currentSalary.NgayNghiLeTetHuongLuong = ngayNghiLeTetHuongLuong;
            currentSalary.CongCNGio = congCNGio;
            currentSalary.CongCNPhut = phutcongCN;
            currentSalary.CongTangCaNgayThuongGio = congTangCaNgayThuongGio;
            currentSalary.CongTangCaNgayThuongPhut = phutcongTangCaNgayThuong;
            currentSalary.CongLeTet = congLeTet;
            currentSalary.CongLeTetPhut = phutcongLeTet;

            currentSalary.CongTacXa = congTacXa;
            currentSalary.TongBunBoc = tongBunBoc;
            currentSalary.ThanhTienBunBoc = thanhTienBunBoc;
            currentSalary.MucDatTrongThang = mucDatTrongThang;
            currentSalary.LuongTheoDoanhThuDoanhSo = luongTheoDoanhThuDoanhSo;

            currentSalary.MauSo = mauSo;
            currentSalary.LuongDinhMuc = luongDinhMuc;
            currentSalary.ThanhTienLuongCanBan = thanhTienLuongCB;
            currentSalary.LuongVuotDinhMuc = luongVuotDinhMuc;
            currentSalary.PhuCapChuyenCan = phucapchuyencan;
            currentSalary.TongPhuCap = tongPhuCap;

            currentSalary.TongThuNhap = tongthunhap;

            currentSalary.BHXHBHYT = bhxhbhyt;

            currentSalary.TamUng = tamung;

            currentSalary.ThucLanh = thuclanh;
            currentSalary.ComSX = comSX;
            currentSalary.ComKD = comKD;
            currentSalary.ThucLanhTronSo = thucLanhTronSo;
            #endregion

            // Save common information
            if (save)
            {
                if (existSalary)
                {
                    var builder = Builders<SalaryEmployeeMonth>.Filter;
                    var filter = builder.Eq(m => m.Id, currentSalary.Id);
                    var update = Builders<SalaryEmployeeMonth>.Update
                        .Set(m => m.Bac, currentSalary.Bac)
                        .Set(m => m.LuongCanBan, currentSalary.LuongCanBan)
                        .Set(m => m.NangNhocDocHai, currentSalary.NangNhocDocHai)
                        .Set(m => m.TrachNhiem, currentSalary.TrachNhiem)
                        .Set(m => m.ThuHut, currentSalary.ThuHut)
                        .Set(m => m.Xang, currentSalary.Xang)
                        .Set(m => m.DienThoai, currentSalary.DienThoai)
                        .Set(m => m.Com, currentSalary.Com)
                        .Set(m => m.NhaO, currentSalary.NhaO)
                        .Set(m => m.KiemNhiem, currentSalary.KiemNhiem)
                        .Set(m => m.BhytDacBiet, currentSalary.BhytDacBiet)
                        .Set(m => m.ViTriCanKnNhieuNam, currentSalary.ViTriCanKnNhieuNam)
                        .Set(m => m.ViTriDacThu, currentSalary.ViTriDacThu)
                        .Set(m => m.LuongCoBanBaoGomPhuCap, currentSalary.LuongCoBanBaoGomPhuCap)
                        .Set(m => m.NgayCongLamViec, currentSalary.NgayCongLamViec)
                        .Set(m => m.NgayNghiPhepNam, currentSalary.NgayNghiPhepNam)
                        .Set(m => m.NgayNghiPhepHuongLuong, currentSalary.NgayNghiPhepHuongLuong)
                        .Set(m => m.NgayNghiLeTetHuongLuong, currentSalary.NgayNghiLeTetHuongLuong)
                        .Set(m => m.CongCNGio, currentSalary.CongCNGio)
                        .Set(m => m.CongTangCaNgayThuongGio, currentSalary.CongTangCaNgayThuongGio)
                        .Set(m => m.CongLeTet, currentSalary.CongLeTet)
                        .Set(m => m.CongTacXa, currentSalary.CongTacXa)
                        .Set(m => m.MucDatTrongThang, currentSalary.MucDatTrongThang)
                        .Set(m => m.LuongTheoDoanhThuDoanhSo, currentSalary.LuongTheoDoanhThuDoanhSo)
                        .Set(m => m.TongBunBoc, currentSalary.TongBunBoc)
                        .Set(m => m.ThanhTienBunBoc, currentSalary.ThanhTienBunBoc)
                        .Set(m => m.LuongKhac, currentSalary.LuongKhac)
                        .Set(m => m.ThiDua, currentSalary.ThiDua)
                        .Set(m => m.HoTroNgoaiLuong, currentSalary.HoTroNgoaiLuong)
                        .Set(m => m.TongThuNhap, currentSalary.TongThuNhap)
                        .Set(m => m.BHXHBHYT, currentSalary.BHXHBHYT)
                        .Set(m => m.LuongThamGiaBHXH, currentSalary.LuongThamGiaBHXH)
                        .Set(m => m.TamUng, currentSalary.TamUng)
                        .Set(m => m.ThuongLeTet, currentSalary.ThuongLeTet)
                        .Set(m => m.ThucLanh, currentSalary.ThucLanh)
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    dbContext.SalaryEmployeeMonths.UpdateOne(filter, update);
                }
                else
                {
                    dbContext.SalaryEmployeeMonths.InsertOne(currentSalary);
                }
            }

            return currentSalary;
        }

        public decimal GetLuongCB(string maSo, int bac)
        {
            decimal luongCB = 0;

            // NHA MAY & SANXUAT => GET DIRECT [NgachLuongs] base maSo vs bac
            var lastNgachLuong = dbContext.NgachLuongs.Find(m => m.Enable.Equals(true) && m.Law.Equals(false) && m.MaSo.Equals(maSo) && m.Bac.Equals(bac)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
            if (lastNgachLuong != null)
            {
                luongCB = lastNgachLuong.MucLuongThang;
            }

            return luongCB;
        }

        #region CONG TONG, UPDATE ONLY. INSERT IN FILE IMPORT
        [Route(Constants.LinkSalary.CongTong)]
        public async Task<IActionResult> CongTong(string thang)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongNM, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            #endregion

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 02, 01);
            var endDate = DateTime.Now;
            while (date.Year < endDate.Year || (date.Year == endDate.Year && date.Month <= endDate.Month))
            {
                monthYears.Add(new MonthYear
                {
                    Month = date.Month,
                    Year = date.Year
                });
                date = date.AddMonths(1);
            }
            if (endDate.Day > 25)
            {
                monthYears.Add(new MonthYear
                {
                    Month = endDate.AddMonths(1).Month,
                    Year = endDate.AddMonths(1).Year
                });
            }
            var sortTimes = monthYears.OrderByDescending(x => x.Month).OrderByDescending(x => x.Year).ToList();
            #endregion

            #region Times
            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = toDate.Day > 25 ? new DateTime(toDate.Year, toDate.Month, 26) : new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Day > 25 ? toDate.AddMonths(1).Year : toDate.Year;
            var month = toDate.Day > 25 ? toDate.AddMonths(1).Month : toDate.Month;
            thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;
            #endregion


            var salaryNhaMayCongs = GetCongs(month, year);

            var viewModel = new BangLuongViewModel
            {
                Congs = salaryNhaMayCongs,
                MonthYears = sortTimes,
                Thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.CongTong + "/" + Constants.ActionLink.Update)]
        public async Task<IActionResult> CongTongUpdate(string thang)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongNM, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            #endregion

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 02, 01);
            var endDate = DateTime.Now;
            while (date.Year < endDate.Year || (date.Year == endDate.Year && date.Month <= endDate.Month))
            {
                monthYears.Add(new MonthYear
                {
                    Month = date.Month,
                    Year = date.Year
                });
                date = date.AddMonths(1);
            }
            if (endDate.Day > 25)
            {
                monthYears.Add(new MonthYear
                {
                    Month = endDate.AddMonths(1).Month,
                    Year = endDate.AddMonths(1).Year
                });
            }
            var sortTimes = monthYears.OrderByDescending(x => x.Month).OrderByDescending(x => x.Year).ToList();
            #endregion

            #region Times
            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = toDate.Day > 25 ? new DateTime(toDate.Year, toDate.Month, 26) : new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Day > 25 ? toDate.AddMonths(1).Year : toDate.Year;
            var month = toDate.Day > 25 ? toDate.AddMonths(1).Month : toDate.Month;
            thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;
            #endregion


            var salaryNhaMayCongs = GetCongs(month, year);

            var viewModel = new BangLuongViewModel
            {
                Congs = salaryNhaMayCongs,
                MonthYears = sortTimes,
                Thang = thang
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.CongTong + "/" + Constants.ActionLink.Update)]
        public async Task<IActionResult> CongTongUpdate(BangLuongViewModel viewModel)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            #endregion

            foreach (var item in viewModel.Congs)
            {
                var builder = Builders<EmployeeCong>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<EmployeeCong>.Update
                    .Set(m => m.CongTong, item.CongTong)
                    .Set(m => m.ComSX, item.ComSX)
                    .Set(m => m.ComKD, item.ComKD)
                    .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                dbContext.EmployeeCongs.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        private List<EmployeeCong> GetCongs(int month, int year)
        {
            var results = dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).ToList();
            return results;
        }
        #endregion

        #region IMPORT DATA
        [Route(Constants.LinkSalary.NhaMayTemplate)]
        public async Task<IActionResult> NhaMayTemplate(string fileName)
        {
            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"du-lieu-nha-may-thang-" + DateTime.Now.Month + "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var cellStyleBorder = workbook.CreateCellStyle();
                cellStyleBorder.BorderBottom = BorderStyle.Thin;
                cellStyleBorder.BorderLeft = BorderStyle.Thin;
                cellStyleBorder.BorderRight = BorderStyle.Thin;
                cellStyleBorder.BorderTop = BorderStyle.Thin;
                cellStyleBorder.Alignment = HorizontalAlignment.Center;
                cellStyleBorder.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleHeader = workbook.CreateCellStyle();
                //cellStyleHeader.CloneStyleFrom(cellStyleBorder);
                //cellStyleHeader.FillForegroundColor = HSSFColor.Blue.Index2;
                cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                cellStyleHeader.FillPattern = FillPattern.SolidForeground;

                var font = workbook.CreateFont();
                font.FontHeightInPoints = 11;
                //font.FontName = "Calibri";
                font.Boldweight = (short)FontBoldWeight.Bold;
                #endregion

                ISheet sheet1 = workbook.CreateSheet("NMT" + DateTime.Now.Month.ToString("00"));
                //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 3, 7));
                //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 8, 13));

                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("SỐ LIỆU NHÀ MÁY");
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("Tháng");
                row.CreateCell(1, CellType.Numeric).SetCellValue(DateTime.Now.Month);
                row.CreateCell(2, CellType.String).SetCellValue("Năm");
                row.CreateCell(3, CellType.Numeric).SetCellValue(DateTime.Now.Year);
                // Set style
                for (int i = 0; i <= 3; i++)
                {
                    row.Cells[i].CellStyle = cellStyleHeader;
                }
                row.Cells[1].CellStyle.SetFont(font);
                row.Cells[3].CellStyle.SetFont(font);
                rowIndex++;
                //https://stackoverflow.com/questions/51681846/rowspan-and-colspan-in-apache-poi

                row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("#");
                row.CreateCell(1, CellType.String).SetCellValue("Mã nhân viên");
                row.CreateCell(2, CellType.String).SetCellValue("Họ tên");
                row.CreateCell(3, CellType.String).SetCellValue("Chức vụ");
                row.CreateCell(4, CellType.String).SetCellValue("Công tổng");
                row.CreateCell(5, CellType.String).SetCellValue("Cơm SX");
                row.CreateCell(6, CellType.String).SetCellValue("Cơm KD");
                row.CreateCell(7, CellType.String).SetCellValue("Giờ tăng ca");
                row.CreateCell(8, CellType.String).SetCellValue("Giờ làm việc CN");
                row.CreateCell(9, CellType.String).SetCellValue("Giờ làm việc Lễ/Tết");
                // Set style
                for (int i = 0; i <= 9; i++)
                {
                    row.Cells[i].CellStyle = cellStyleHeader;
                }


                for (int i = 1; i <= 50; i++)
                {
                    row = sheet1.CreateRow(rowIndex + 1);
                    //row.CreateCell(0, CellType.Numeric).SetCellValue(i);
                    row.CreateCell(1, CellType.String).SetCellValue(string.Empty);
                    row.CreateCell(2, CellType.String).SetCellValue(string.Empty);
                    row.CreateCell(3, CellType.String).SetCellValue(string.Empty);
                    row.CreateCell(4, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(5, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(6, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(7, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(8, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(9, CellType.Numeric).SetCellValue(string.Empty);
                    rowIndex++;
                }

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.NhaMayImport)]
        [HttpPost]
        public ActionResult NhaMayImport()
        {
            IFormFile file = Request.Form.Files[0];
            string folderName = Constants.Storage.Hr;
            string webRootPath = _env.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            if (file.Length > 0)
            {
                string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                ISheet sheet;
                string fullPath = Path.Combine(newPath, file.FileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Position = 0;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet = hssfwb.GetSheetAt(0);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
                    }

                    var timeRow = sheet.GetRow(1);
                    var month = Convert.ToInt32(Utility.GetNumbericCellValue(timeRow.GetCell(1)));
                    var year = Convert.ToInt32(Utility.GetNumbericCellValue(timeRow.GetCell(3)));
                    if (month == 0)
                    {
                        month = DateTime.Now.Month;
                    }
                    if (year == 0)
                    {
                        year = DateTime.Now.Year;
                    }

                    for (int i = 3; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var code = Utility.GetFormattedCellValue(row.GetCell(1));
                        var fullName = Utility.GetFormattedCellValue(row.GetCell(2));
                        var alias = Utility.AliasConvert(fullName);
                        var title = Utility.GetFormattedCellValue(row.GetCell(3));

                        decimal congtong = (decimal)Utility.GetNumbericCellValue(row.GetCell(4));
                        var comsx = Utility.GetNumbericCellValue(row.GetCell(5));
                        var comkd = Utility.GetNumbericCellValue(row.GetCell(6));
                        decimal giotangca = (decimal)Utility.GetNumbericCellValue(row.GetCell(7));
                        decimal giolamvieccn = (decimal)Utility.GetNumbericCellValue(row.GetCell(8));
                        decimal giolamviecletet = (decimal)Utility.GetNumbericCellValue(row.GetCell(9));

                        var employee = new Employee();
                        if (!string.IsNullOrEmpty(alias))
                        {
                            employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.AliasFullName.Equals(alias)).FirstOrDefault();
                        }
                        if (employee == null)
                        {
                            if (!string.IsNullOrEmpty(fullName))
                            {
                                employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.FullName.Equals(fullName)).FirstOrDefault();
                            }
                        }
                        if (employee == null)
                        {
                            if (!string.IsNullOrEmpty(code))
                            {
                                employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.CodeOld.Equals(code)).FirstOrDefault();
                            }
                        }
                        if (employee != null)
                        {
                            // check exist to update
                            var existEntity = dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (existEntity != null)
                            {
                                var builder = Builders<EmployeeCong>.Filter;
                                var filter = builder.Eq(m => m.Id, existEntity.Id);
                                var update = Builders<EmployeeCong>.Update
                                    .Set(m => m.EmployeeCode, employee.Code)
                                    .Set(m => m.EmployeeName, employee.FullName)
                                    .Set(m => m.EmployeeChucVu, employee.Title)
                                    .Set(m => m.CongTong, congtong)
                                    .Set(m => m.ComKD, comkd)
                                    .Set(m => m.ComSX, comsx)
                                    .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                                dbContext.EmployeeCongs.UpdateOne(filter, update);
                            }
                            else
                            {
                                var newItem = new EmployeeCong
                                {
                                    Year = year,
                                    Month = month,
                                    EmployeeId = employee.Id,
                                    EmployeeCode = employee.Code,
                                    EmployeeName = employee.FullName,
                                    EmployeeChucVu = employee.Title,
                                    CongTong = congtong,
                                    ComKD = comkd,
                                    ComSX = comsx
                                };
                                dbContext.EmployeeCongs.InsertOne(newItem);
                            }
                        }
                        else
                        {
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "nha-may-import",
                                Object = "time: " + month + "-" + year + ", code: " +code + "-" + fullName + "-" + title + " ,dòng " + i,
                                Error = "No import data",
                                DateTime = DateTime.Now.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Factory + "/" + Constants.LinkSalary.CongTong });
        }
        #endregion



    }
}
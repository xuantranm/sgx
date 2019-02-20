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
    [Route(Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Production)]
    public class SalaryProductionController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalaryProductionController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalaryProductionController> logger)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongSX, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL

            var sortTimes = Utility.DllMonths();

            var departments = await dbContext.Departments.Find(m => m.Enable.Equals(true)).SortBy(m => m.Order).ToListAsync();

            var employeeDdl = await dbContext.Employees.Find(m => m.Enable.Equals(true) && m.SalaryType.Equals((int)EKhoiLamViec.SX) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
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
            var mauSoChuyenCan = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-chuyen-can")).Value);
            var mauSoTienCom = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-tien-com")).Value);
            decimal ibhxh = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhxh")) == null ? 8 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhxh")).Value);
            decimal ibhyt = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhyt")) == null ? (decimal)1.5 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhyt")).Value);
            decimal ibhtn = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhtn")) == null ? 1 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhtn")).Value);
            #endregion

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true) & builder.Eq(m => m.SalaryType, (int)EKhoiLamViec.SX) & !builder.Eq(m => m.UserName, Constants.System.account);
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
                var salary = GetSalaryEmployeeMonth(year, month, employee.Id, employee.Joinday, employee.NgachLuong, employee.SalaryLevel, (int)mauSo, mauSoChuyenCan, mauSoTienCom, employee.LuongBHXH, ibhxh, ibhyt, ibhtn, null, true);
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

        [Route(Constants.LinkSalary.BangLuong + "/" + Constants.ActionLink.Update)]
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongSX, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL

            var sortTimes = Utility.DllMonths();

            var departments = await dbContext.Departments.Find(m => m.Enable.Equals(true)).SortBy(m => m.Order).ToListAsync();

            var employeeDdl = await dbContext.Employees.Find(m => m.Enable.Equals(true) && m.SalaryType.Equals((int)EKhoiLamViec.SX) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
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
            var mauSoChuyenCan = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-chuyen-can")).Value);
            var mauSoTienCom = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-tien-com")).Value);
            decimal ibhxh = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhxh")) == null ? 8 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhxh")).Value);
            decimal ibhyt = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhyt")) == null ? (decimal)1.5 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhyt")).Value);
            decimal ibhtn = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhtn")) == null ? 1 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhtn")).Value);
            #endregion

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true) & builder.Eq(m => m.SalaryType, (int)EKhoiLamViec.SX) & !builder.Eq(m => m.UserName, Constants.System.account);
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
                var salary = GetSalaryEmployeeMonth(year, month, employee.Id, employee.Joinday, employee.NgachLuong, employee.SalaryLevel, (int)mauSo, mauSoChuyenCan, mauSoTienCom, employee.LuongBHXH, ibhxh, ibhyt, ibhtn, null, true);
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

        public SalaryEmployeeMonth GetSalaryEmployeeMonth(int year, int month, string employeeId, DateTime thamnienlamviec, string ngachLuong, int bac, int mauSo, decimal mauSoChuyenCan, decimal mauSoTienCom, decimal bhxh, decimal ibhxh, decimal ibhyt, decimal ibhtn, SalaryEmployeeMonth newSalary, bool save)
        {
            var today = DateTime.Now.Date;
            int todayMonth = today.Day > 25 ? today.AddMonths(1).Month : today.Month;
            var endDateMonth = new DateTime(year, month, 25);
            var startDateMonth = endDateMonth.AddMonths(-1).AddDays(1);
            // UAT
            //ngachLuong = "B.06";
            //
            decimal luongCB = GetLuongCB("B.06", bac);

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
                currentSalary.ThamNien = luongCB * hesothamnien / 100;
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
            //luongcbbaogomphucap = luongCB + nangnhoc + trachnhiem + thamnien + thuhut + dienthoai + xang + com + nhao + kiemnhiem + bhytdacbiet + vitricanknnhieunam + vitridacthu;

            double ngayLamViec = 0;
            double phepNam = 0;
            double leTet = 0;
            decimal congTong = 0;
            decimal comSX = 0;

            var chamCongs = dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(employeeId) & m.Year.Equals(year) & m.Month.Equals(month)).ToList();
            if (chamCongs != null & chamCongs.Count > 0)
            {
                foreach (var chamCong in chamCongs)
                {
                    ngayLamViec += chamCong.NgayLamViecChinhTay;
                    phepNam += chamCong.PhepNamChinhTay;
                    leTet += chamCong.LeTetChinhTay;
                }
            }

            decimal luongDinhMuc = Convert.ToDecimal((double)luongCB / mauSo * (double)ngayLamViec);
            decimal tienPhepNamLeTet = Convert.ToDecimal(((double)phepNam * (double)luongCB / mauSo) + ((double)leTet * (double)luongCB / mauSo));

            decimal thanhTienLuongCanBan = luongDinhMuc + tienPhepNamLeTet;

            // RESOLVE CONG TONG. UPDATE TO DB.
            // Query base employee, time.
            // Manage in collection [SalaryNhaMayCongs]
            var dataPr = dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (dataPr != null)
            {
                congTong = dataPr.ThanhTienTrongGio + dataPr.ThanhTienNgoaiGio;
                com = Convert.ToDecimal(dataPr.Com * (double)mauSoTienCom);
            }

            decimal tongPhuCap = com + nhao + xang + nangnhoc + thamnien + trachnhiem;

            decimal luongVuotDinhMuc = congTong - luongDinhMuc;
            if (luongVuotDinhMuc < 0)
            {
                luongVuotDinhMuc = 0;
            }

            decimal tongthunhap = thanhTienLuongCanBan + tongPhuCap + luongVuotDinhMuc;

            decimal tamung = 0;
            var credits = dbContext.CreditEmployees.Find(m => m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)).ToList();
            if (credits != null && credits.Count > 0)
            {
                foreach (var credit in credits)
                {
                    tamung += credit.Money;
                }
            }

            // THUONG LE TET: thuongletet

            decimal dongbhxh = Convert.ToDecimal((double)luongthamgiabhxh * (double)ibhxh / 100);
            decimal dongbhyt = Convert.ToDecimal((double)luongthamgiabhxh * (double)ibhyt / 100);
            decimal dongbhtn = Convert.ToDecimal((double)luongthamgiabhxh * (double)ibhtn / 100);
            decimal bhxhbhyt = dongbhxh + dongbhyt + dongbhtn;

            decimal thuclanh = tongthunhap - tamung + thuongletet - bhxhbhyt;

            decimal thucLanhTronSo = (Math.Round(thuclanh / 10000) * 10000);

            #region update field to currentSalary
            currentSalary.LuongCanBan = luongCB;
            currentSalary.ThamNien = thamnien;
            currentSalary.LuongCoBanBaoGomPhuCap = luongcbbaogomphucap;


            currentSalary.NgayCongLamViec = ngayLamViec;
            currentSalary.NgayNghiPhepNam = phepNam;

            currentSalary.MauSo = mauSo;
            currentSalary.LuongDinhMuc = luongDinhMuc;
            currentSalary.TienPhepNamLeTet = tienPhepNamLeTet;
            currentSalary.ThanhTienLuongCanBan = thanhTienLuongCanBan;

            currentSalary.LuongVuotDinhMuc = luongVuotDinhMuc;
            currentSalary.Com = com;
            currentSalary.TongPhuCap = tongPhuCap;

            currentSalary.TongThuNhap = tongthunhap;

            currentSalary.BHXH = dongbhxh;
            currentSalary.BHYT = dongbhyt;
            currentSalary.BHTN = dongbhtn;
            currentSalary.BHXHBHYT = bhxhbhyt;

            currentSalary.TamUng = tamung;

            currentSalary.ThucLanh = thuclanh;

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

        #region THE LUONG
        [Route(Constants.LinkSalary.TheLuong)]
        public IActionResult TheLuong(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }
        #endregion

        #region PHU CAP & TANG CA
        /// <summary>
        /// THEO CHAM CONG.
        /// </summary>
        /// <param name="thang"></param>
        /// <returns></returns>
        [Route(Constants.LinkSalary.PhuCap)]
        public IActionResult PhuCap(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.SanXuatNgoaiGioPhuCapTemplate)]
        public async Task<IActionResult> SanXuatNgoaiGioPhuCapTemplate(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.BangChamCong, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var toDate = Utility.GetToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            var thangFormat = month.ToString("00") + "-" + year.ToString("0000");
            #endregion

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"" + thangFormat + "-tang-ca-phu-cap";
            sFileName += "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var font = workbook.CreateFont();
                font.FontHeightInPoints = 8;
                font.FontName = "Arial";

                var fontSmall = workbook.CreateFont();
                fontSmall.FontHeightInPoints = 5;
                fontSmall.FontName = "Arial";

                var fontbold = workbook.CreateFont();
                fontbold.FontHeightInPoints = 8;
                fontbold.FontName = "Arial";
                fontbold.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold8 = workbook.CreateFont();
                fontbold8.FontHeightInPoints = 8;
                fontbold8.FontName = "Arial";
                fontbold8.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10 = workbook.CreateFont();
                fontbold10.FontHeightInPoints = 10;
                fontbold10.FontName = "Arial";
                fontbold10.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11 = workbook.CreateFont();
                fontbold11.FontHeightInPoints = 11;
                fontbold11.FontName = "Arial";
                fontbold11.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold12 = workbook.CreateFont();
                fontbold12.FontHeightInPoints = 12;
                fontbold12.FontName = "Arial";
                fontbold12.Boldweight = (short)FontBoldWeight.Bold;

                var styleBorder = workbook.CreateCellStyle();
                styleBorder.BorderBottom = BorderStyle.Thin;
                styleBorder.BorderLeft = BorderStyle.Thin;
                styleBorder.BorderRight = BorderStyle.Thin;
                styleBorder.BorderTop = BorderStyle.Thin;

                var styleBorderDot = workbook.CreateCellStyle();
                styleBorderDot.BorderBottom = BorderStyle.Dotted;
                styleBorderDot.BorderLeft = BorderStyle.Thin;
                styleBorderDot.BorderRight = BorderStyle.Thin;
                styleBorderDot.BorderTop = BorderStyle.Thin;

                var styleCenter = workbook.CreateCellStyle();
                styleCenter.Alignment = HorizontalAlignment.Center;
                styleCenter.VerticalAlignment = VerticalAlignment.Center;

                var styleCenterBorder = workbook.CreateCellStyle();
                styleCenterBorder.CloneStyleFrom(styleBorder);
                styleCenterBorder.Alignment = HorizontalAlignment.Center;
                styleCenterBorder.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleBorderAndColorLightGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellStyleBorderAndColorLightGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorLightGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 146, 208, 80 }));
                cellStyleBorderAndColorLightGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorGreen.SetFont(fontbold11);
                cellStyleBorderAndColorGreen.WrapText = true;
                cellStyleBorderAndColorGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 176, 80 }));
                cellStyleBorderAndColorGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorBlue.SetFont(fontbold11);
                cellStyleBorderAndColorBlue.WrapText = true;
                cellStyleBorderAndColorBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 112, 192 }));
                cellStyleBorderAndColorBlue.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorDarkBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorDarkBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorDarkBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 32, 96 }));

                var cellM3 = workbook.CreateCellStyle();
                cellM3.CloneStyleFrom(styleBorder);
                cellM3.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellM3.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellM3).SetFillForegroundColor(new XSSFColor(new byte[] { 248, 203, 173 }));
                cellM3.BorderBottom = BorderStyle.Thin;
                cellM3.BottomBorderColor = IndexedColors.Black.Index;
                cellM3.BorderTop = BorderStyle.Thin;
                cellM3.TopBorderColor = IndexedColors.Black.Index;
                cellM3.BorderLeft = BorderStyle.Thin;
                cellM3.LeftBorderColor = IndexedColors.Black.Index;
                cellM3.BorderRight = BorderStyle.Thin;
                cellM3.RightBorderColor = IndexedColors.Black.Index;

                var styleDedault = workbook.CreateCellStyle();
                styleDedault.CloneStyleFrom(styleBorder);
                styleDedault.SetFont(font);

                var styleDot = workbook.CreateCellStyle();
                styleDot.CloneStyleFrom(styleBorderDot);
                styleDot.SetFont(font);

                var styleTitle = workbook.CreateCellStyle();
                styleTitle.CloneStyleFrom(styleCenter);
                styleTitle.SetFont(fontbold12);

                var styleSubTitle = workbook.CreateCellStyle();
                styleSubTitle.CloneStyleFrom(styleCenter);
                styleSubTitle.SetFont(fontbold8);

                var styleHeader = workbook.CreateCellStyle();
                styleHeader.CloneStyleFrom(styleCenterBorder);
                styleHeader.SetFont(fontbold8);

                var styleDedaultMerge = workbook.CreateCellStyle();
                styleDedaultMerge.CloneStyleFrom(styleCenter);
                styleDedaultMerge.SetFont(font);

                var styleBold = workbook.CreateCellStyle();
                styleBold.SetFont(fontbold8);

                var styleSmall = workbook.CreateCellStyle();
                styleSmall.CloneStyleFrom(styleBorder);
                styleSmall.SetFont(fontSmall);

                var cellStyleHeader = workbook.CreateCellStyle();
                cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                cellStyleHeader.FillPattern = FillPattern.SolidForeground;

                var fontbold18TimesNewRoman = workbook.CreateFont();
                fontbold18TimesNewRoman.FontHeightInPoints = 18;
                fontbold18TimesNewRoman.FontName = "Times New Roman";
                fontbold18TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11TimesNewRoman = workbook.CreateFont();
                fontbold11TimesNewRoman.FontHeightInPoints = 11;
                fontbold11TimesNewRoman.FontName = "Times New Roman";
                fontbold11TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10TimesNewRoman = workbook.CreateFont();
                fontbold10TimesNewRoman.FontHeightInPoints = 10;
                fontbold10TimesNewRoman.FontName = "Times New Roman";
                fontbold10TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold9TimesNewRoman = workbook.CreateFont();
                fontbold9TimesNewRoman.FontHeightInPoints = 9;
                fontbold9TimesNewRoman.FontName = "Times New Roman";
                fontbold9TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var styleRow0 = workbook.CreateCellStyle();
                styleRow0.SetFont(fontbold18TimesNewRoman);
                styleRow0.Alignment = HorizontalAlignment.Center;
                styleRow0.VerticalAlignment = VerticalAlignment.Center;
                styleRow0.BorderBottom = BorderStyle.Thin;
                styleRow0.BorderTop = BorderStyle.Thin;
                styleRow0.BorderLeft = BorderStyle.Thin;
                styleRow0.BorderRight = BorderStyle.Thin;

                var styleBorderBold11Background = workbook.CreateCellStyle();
                styleBorderBold11Background.SetFont(fontbold11TimesNewRoman);
                styleBorderBold11Background.Alignment = HorizontalAlignment.Center;
                styleBorderBold11Background.VerticalAlignment = VerticalAlignment.Center;
                styleBorderBold11Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold11Background.BorderTop = BorderStyle.Thin;
                styleBorderBold11Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold11Background.BorderRight = BorderStyle.Thin;
                styleBorderBold11Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold11Background.FillPattern = FillPattern.SolidForeground;

                var styleBorderBold10Background = workbook.CreateCellStyle();
                styleBorderBold10Background.SetFont(fontbold10TimesNewRoman);
                styleBorderBold10Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold10Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold10Background.BorderRight = BorderStyle.Thin;
                styleBorderBold10Background.BorderTop = BorderStyle.Thin;
                styleBorderBold10Background.Alignment = HorizontalAlignment.Center;
                styleBorderBold10Background.VerticalAlignment = VerticalAlignment.Center;
                styleBorderBold10Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold10Background.FillPattern = FillPattern.SolidForeground;

                var styleBorderBold9Background = workbook.CreateCellStyle();
                styleBorderBold9Background.SetFont(fontbold9TimesNewRoman);
                styleBorderBold9Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold9Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold9Background.BorderRight = BorderStyle.Thin;
                styleBorderBold9Background.BorderTop = BorderStyle.Thin;
                styleBorderBold9Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold9Background.FillPattern = FillPattern.SolidForeground;
                #endregion

                ISheet sheet1 = workbook.CreateSheet("TANG CA - PHU CAP " + DateTime.Now.Month.ToString("00"));

                #region Title
                var rowIndex = 0;
                var columnIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                var cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Công ty TNHH CNSH SÀI GÒN XANH");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Địa chỉ: 127 Nguyễn Trọng Tuyển - P.15 - Q.Phú Nhuận - Tp HCM");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Điện thoại: (08)-39971869 - 38442457 - Fax: 08-39971869");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MST: 0302519810");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;
                #endregion

                #region Thang
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Thời gian:");
                cell.CellStyle = styleTitle;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(thangFormat);
                cell.CellStyle = styleTitle;
                rowIndex++;
                #endregion

                var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 2;
                for (DateTime date = fromDate; date <= toDate; date = date.AddDays(1.0))
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(date.Day);
                    cell.CellStyle.SetFont(font);
                    columnIndex++;
                }
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleBorderBold11Background;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleBorderBold11Background;
                columnIndex++;

                var cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + fromToNum);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Giờ tăng ca trong tháng".ToUpper());
                cell.CellStyle = styleBorderBold11Background;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Họ và Tên");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                for (DateTime date = fromDate; date <= toDate; date = date.AddDays(1.0))
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(Constants.DayOfWeekT2(date));
                    cell.CellStyle = styleBorderBold10Background;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày thường");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("CN");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("CƠM");
                cell.CellStyle = styleBorderBold10Background;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, (columnIndex + 5 + fromToNum));
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("KẾ HOẠCH SẢN XUẤT");
                cell.CellStyle = styleBorderBold9Background;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.SanXuatNgoaiGioPhuCapPost)]
        [HttpPost]
        public ActionResult SanXuatNgoaiGioPhuCapPost()
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

                    var timeRow = sheet.GetRow(5);
                    var time = Utility.GetFormattedCellValue(timeRow.GetCell(1));
                    var endMonthDate = Utility.GetToDate(time);
                    var month = endMonthDate.Month;
                    var year = endMonthDate.Year;
                    var toDate = new DateTime(year, month, 25);
                    var fromDate = toDate.AddMonths(-1).AddDays(1);
                    var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);

                    var dayRow = sheet.GetRow(7);
                    //var nameDayRow = sheet.GetRow(9);

                    for (int i = 11; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        int columnIndex = 0;
                        var ma = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var ten = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var alias = Utility.AliasConvert(ten);

                        if (string.IsNullOrEmpty(ten))
                        {
                            continue;
                        }

                        var values = new List<string>();
                        for (DateTime date = fromDate; date <= toDate; date = date.AddDays(1.0))
                        {
                            values.Add(date + ";" + Utility.GetNumbericCellValue(row.GetCell(columnIndex)));
                            columnIndex++;
                        }
                        double ngaythuong = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        double cn = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var com = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        //columnIndex++;

                        var existEmployee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                        if (existEmployee != null)
                        {
                            DataTangCaPhuCap(month, year, existEmployee, values, ngaythuong, cn, com);
                        }
                        else
                        {
                            InsertNewEmployee(ten, ma, string.Empty, string.Empty);
                            var employee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                            DataTangCaPhuCap(month, year, existEmployee, values, ngaythuong, cn, com);
                        }
                    }
                }
            }
            return Json(new { result = true });
        }

        private void DataTangCaPhuCap(int month, int year, Employee employee, List<string> values, double ngaythuong, double cn, double com)
        {
            #region Times
            foreach (var valueTime in values)
            {
                if (valueTime.Split(';').Count() > 1)
                {
                    var ngaytangca = DateTime.Parse(valueTime.Split(';')[0]);
                    var giotangca = TimeSpan.FromHours(Convert.ToDouble(valueTime.Split(';')[1]));
                    var time = dbContext.EmployeeWorkTimeLogs.Find(m => m.EmployeeId.Equals(employee.Id) && m.Date.Equals(ngaytangca)).FirstOrDefault();
                    if (time != null)
                    {
                        var filterTime = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, time.Id);
                        var updateTime = Builders<EmployeeWorkTimeLog>.Update
                            .Set(m => m.StatusTangCa, (int)ETangCa.DongY)
                            .Set(m => m.TangCaDaXacNhan, giotangca)
                            .Set(m => m.UpdatedOn, DateTime.Now);
                        dbContext.EmployeeWorkTimeLogs.UpdateOne(filterTime, updateTime);
                    }
                    else
                    {
                        // Update rule later.
                        // dbContext.EmployeeWorkTimeLogs.InsertOne
                    }
                }
            }
            #endregion

            #region EmployeeCongs
            var existCongTong = dbContext.EmployeeCongs.Find(m => m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (existCongTong != null)
            {
                var filterCongTong = Builders<EmployeeCong>.Filter.Eq(m => m.Id, existCongTong.Id);
                var updateCongTong = Builders<EmployeeCong>.Update
                    .Set(m => m.Com, com)
                    .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                dbContext.EmployeeCongs.UpdateOne(filterCongTong, updateCongTong);
            }
            else
            {
                dbContext.EmployeeCongs.InsertOne(new EmployeeCong()
                {
                    Month = month,
                    Year = year,
                    EmployeeId = employee.Id,
                    EmployeeCode = employee.CodeOld,
                    EmployeeName = employee.FullName,
                    Com = com
                });
            }
            #endregion
        }
        #endregion

        #region TAMUNG | THUONG, BHXH, DIEU CHINH NGAY CONG
        [Route(Constants.LinkSalary.SanXuatTamUngTemplate)]
        public async Task<IActionResult> SanXuatTamUngTemplate(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.BangChamCong, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var toDate = Utility.GetToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            var thangFormat = month.ToString("00") + "-" + year.ToString("0000");
            #endregion

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"" + thangFormat + "-tam-ung";
            sFileName += "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var font = workbook.CreateFont();
                font.FontHeightInPoints = 8;
                font.FontName = "Arial";

                var fontSmall = workbook.CreateFont();
                fontSmall.FontHeightInPoints = 5;
                fontSmall.FontName = "Arial";

                var fontbold = workbook.CreateFont();
                fontbold.FontHeightInPoints = 8;
                fontbold.FontName = "Arial";
                fontbold.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold8 = workbook.CreateFont();
                fontbold8.FontHeightInPoints = 8;
                fontbold8.FontName = "Arial";
                fontbold8.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10 = workbook.CreateFont();
                fontbold10.FontHeightInPoints = 10;
                fontbold10.FontName = "Arial";
                fontbold10.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11 = workbook.CreateFont();
                fontbold11.FontHeightInPoints = 11;
                fontbold11.FontName = "Arial";
                fontbold11.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold12 = workbook.CreateFont();
                fontbold12.FontHeightInPoints = 12;
                fontbold12.FontName = "Arial";
                fontbold12.Boldweight = (short)FontBoldWeight.Bold;

                var styleBorder = workbook.CreateCellStyle();
                styleBorder.BorderBottom = BorderStyle.Thin;
                styleBorder.BorderLeft = BorderStyle.Thin;
                styleBorder.BorderRight = BorderStyle.Thin;
                styleBorder.BorderTop = BorderStyle.Thin;

                var styleBorderDot = workbook.CreateCellStyle();
                styleBorderDot.BorderBottom = BorderStyle.Dotted;
                styleBorderDot.BorderLeft = BorderStyle.Thin;
                styleBorderDot.BorderRight = BorderStyle.Thin;
                styleBorderDot.BorderTop = BorderStyle.Thin;

                var styleCenter = workbook.CreateCellStyle();
                styleCenter.Alignment = HorizontalAlignment.Center;
                styleCenter.VerticalAlignment = VerticalAlignment.Center;

                var styleCenterBorder = workbook.CreateCellStyle();
                styleCenterBorder.CloneStyleFrom(styleBorder);
                styleCenterBorder.Alignment = HorizontalAlignment.Center;
                styleCenterBorder.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleBorderAndColorLightGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellStyleBorderAndColorLightGreen.WrapText = true;
                cellStyleBorderAndColorLightGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorLightGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 146, 208, 80 }));
                cellStyleBorderAndColorLightGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorGreen.SetFont(fontbold11);
                cellStyleBorderAndColorGreen.WrapText = true;
                cellStyleBorderAndColorGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 176, 80 }));
                cellStyleBorderAndColorGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorBlue.SetFont(fontbold11);
                cellStyleBorderAndColorBlue.WrapText = true;
                cellStyleBorderAndColorBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 112, 192 }));
                cellStyleBorderAndColorBlue.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorDarkBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorDarkBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorDarkBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 32, 96 }));

                var cellM3 = workbook.CreateCellStyle();
                cellM3.CloneStyleFrom(styleBorder);
                cellM3.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellM3.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellM3).SetFillForegroundColor(new XSSFColor(new byte[] { 248, 203, 173 }));
                cellM3.BorderBottom = BorderStyle.Thin;
                cellM3.BottomBorderColor = IndexedColors.Black.Index;
                cellM3.BorderTop = BorderStyle.Thin;
                cellM3.TopBorderColor = IndexedColors.Black.Index;
                cellM3.BorderLeft = BorderStyle.Thin;
                cellM3.LeftBorderColor = IndexedColors.Black.Index;
                cellM3.BorderRight = BorderStyle.Thin;
                cellM3.RightBorderColor = IndexedColors.Black.Index;

                var styleDedault = workbook.CreateCellStyle();
                styleDedault.CloneStyleFrom(styleBorder);
                styleDedault.SetFont(font);

                var styleDot = workbook.CreateCellStyle();
                styleDot.CloneStyleFrom(styleBorderDot);
                styleDot.SetFont(font);

                var styleTitle = workbook.CreateCellStyle();
                styleTitle.CloneStyleFrom(styleCenter);
                styleTitle.SetFont(fontbold12);

                var styleSubTitle = workbook.CreateCellStyle();
                styleSubTitle.CloneStyleFrom(styleCenter);
                styleSubTitle.SetFont(fontbold8);

                var styleHeader = workbook.CreateCellStyle();
                styleHeader.CloneStyleFrom(styleCenterBorder);
                styleHeader.SetFont(fontbold8);

                var styleDedaultMerge = workbook.CreateCellStyle();
                styleDedaultMerge.CloneStyleFrom(styleCenter);
                styleDedaultMerge.SetFont(font);

                var styleBold = workbook.CreateCellStyle();
                styleBold.SetFont(fontbold8);

                var styleSmall = workbook.CreateCellStyle();
                styleSmall.CloneStyleFrom(styleBorder);
                styleSmall.SetFont(fontSmall);

                var cellStyleHeader = workbook.CreateCellStyle();
                cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                cellStyleHeader.FillPattern = FillPattern.SolidForeground;

                var fontbold18TimesNewRoman = workbook.CreateFont();
                fontbold18TimesNewRoman.FontHeightInPoints = 18;
                fontbold18TimesNewRoman.FontName = "Times New Roman";
                fontbold18TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11TimesNewRoman = workbook.CreateFont();
                fontbold11TimesNewRoman.FontHeightInPoints = 11;
                fontbold11TimesNewRoman.FontName = "Times New Roman";
                fontbold11TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10TimesNewRoman = workbook.CreateFont();
                fontbold10TimesNewRoman.FontHeightInPoints = 10;
                fontbold10TimesNewRoman.FontName = "Times New Roman";
                fontbold10TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold9TimesNewRoman = workbook.CreateFont();
                fontbold9TimesNewRoman.FontHeightInPoints = 9;
                fontbold9TimesNewRoman.FontName = "Times New Roman";
                fontbold9TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var styleRow0 = workbook.CreateCellStyle();
                styleRow0.SetFont(fontbold18TimesNewRoman);
                styleRow0.Alignment = HorizontalAlignment.Center;
                styleRow0.VerticalAlignment = VerticalAlignment.Center;
                styleRow0.BorderBottom = BorderStyle.Thin;
                styleRow0.BorderTop = BorderStyle.Thin;
                styleRow0.BorderLeft = BorderStyle.Thin;
                styleRow0.BorderRight = BorderStyle.Thin;

                var styleBorderBold11Background = workbook.CreateCellStyle();
                styleBorderBold11Background.SetFont(fontbold11TimesNewRoman);
                styleBorderBold11Background.Alignment = HorizontalAlignment.Center;
                styleBorderBold11Background.VerticalAlignment = VerticalAlignment.Center;
                styleBorderBold11Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold11Background.BorderTop = BorderStyle.Thin;
                styleBorderBold11Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold11Background.BorderRight = BorderStyle.Thin;
                styleBorderBold11Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold11Background.FillPattern = FillPattern.SolidForeground;

                var styleBorderBold10Background = workbook.CreateCellStyle();
                styleBorderBold10Background.SetFont(fontbold10TimesNewRoman);
                styleBorderBold10Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold10Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold10Background.BorderRight = BorderStyle.Thin;
                styleBorderBold10Background.BorderTop = BorderStyle.Thin;
                styleBorderBold10Background.Alignment = HorizontalAlignment.Center;
                styleBorderBold10Background.VerticalAlignment = VerticalAlignment.Center;
                styleBorderBold10Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold10Background.FillPattern = FillPattern.SolidForeground;

                var styleBorderBold9Background = workbook.CreateCellStyle();
                styleBorderBold9Background.SetFont(fontbold9TimesNewRoman);
                styleBorderBold9Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold9Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold9Background.BorderRight = BorderStyle.Thin;
                styleBorderBold9Background.BorderTop = BorderStyle.Thin;
                styleBorderBold9Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold9Background.FillPattern = FillPattern.SolidForeground;
                #endregion

                ISheet sheet1 = workbook.CreateSheet(month.ToString("00") + year.ToString("0000") + "-TAM UNG");

                #region Title
                var rowIndex = 0;
                var columnIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                var cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Công ty TNHH CNSH SÀI GÒN XANH");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Địa chỉ: 127 Nguyễn Trọng Tuyển - P.15 - Q.Phú Nhuận - Tp HCM");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Điện thoại: (08)-39971869 - 38442457 - Fax: 08-39971869");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MST: 0302519810");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;
                #endregion

                #region Thang
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Thời gian:");
                cell.CellStyle = styleTitle;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(thangFormat);
                cell.CellStyle = styleTitle;
                rowIndex++;
                #endregion

                row = sheet1.CreateRow(rowIndex);
                row.Height = 1000;
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Tên NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ứng lương");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;
                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.SanXuatTamUngPost)]
        [HttpPost]
        public ActionResult SanXuatTamUngPost()
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

                    var timeRow = sheet.GetRow(5);
                    var time = Utility.GetFormattedCellValue(timeRow.GetCell(1));
                    var endMonthDate = Utility.GetToDate(time);
                    var month = endMonthDate.Month;
                    var year = endMonthDate.Year;
                    var toDate = new DateTime(year, month, 25);
                    var fromDate = toDate.AddMonths(-1).AddDays(1);
                    var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);


                    for (int i = 7; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var ma = string.Empty;
                        var ten = string.Empty;
                        var chucvu = string.Empty;
                        var ngayvaolam = string.Empty;
                        decimal tamung = 0;

                        int columnIndex = 0;
                        ma = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        ten = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tamung = (decimal)Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;

                        if (string.IsNullOrEmpty(ten))
                        {
                            continue;
                        }
                        var alias = Utility.AliasConvert(ten);
                        var existEmployee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                        if (existEmployee != null)
                        {
                            TamUngSanXuat(month, year, tamung, existEmployee);
                        }
                        else
                        {
                            InsertNewEmployee(ten, ma, chucvu, ngayvaolam);
                            var employee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                            TamUngSanXuat(month, year, tamung, employee);
                        }
                    }
                }
            }
            return Json(new { result = true });
        }

        [Route(Constants.LinkSalary.SanXuatNgayCongThuongBHXHTemplate)]
        public async Task<IActionResult> SanXuatNgayCongThuongBHXHTemplate(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.BangChamCong, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var toDate = Utility.GetToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            var thangFormat = month.ToString("00") + "-" + year.ToString("0000");
            #endregion

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"" + thangFormat + "-ngay-cong-thuong-bhxh";
            sFileName += "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var font = workbook.CreateFont();
                font.FontHeightInPoints = 8;
                font.FontName = "Arial";

                var fontSmall = workbook.CreateFont();
                fontSmall.FontHeightInPoints = 5;
                fontSmall.FontName = "Arial";

                var fontbold = workbook.CreateFont();
                fontbold.FontHeightInPoints = 8;
                fontbold.FontName = "Arial";
                fontbold.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold8 = workbook.CreateFont();
                fontbold8.FontHeightInPoints = 8;
                fontbold8.FontName = "Arial";
                fontbold8.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10 = workbook.CreateFont();
                fontbold10.FontHeightInPoints = 10;
                fontbold10.FontName = "Arial";
                fontbold10.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11 = workbook.CreateFont();
                fontbold11.FontHeightInPoints = 11;
                fontbold11.FontName = "Arial";
                fontbold11.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold12 = workbook.CreateFont();
                fontbold12.FontHeightInPoints = 12;
                fontbold12.FontName = "Arial";
                fontbold12.Boldweight = (short)FontBoldWeight.Bold;

                var styleBorder = workbook.CreateCellStyle();
                styleBorder.BorderBottom = BorderStyle.Thin;
                styleBorder.BorderLeft = BorderStyle.Thin;
                styleBorder.BorderRight = BorderStyle.Thin;
                styleBorder.BorderTop = BorderStyle.Thin;

                var styleBorderDot = workbook.CreateCellStyle();
                styleBorderDot.BorderBottom = BorderStyle.Dotted;
                styleBorderDot.BorderLeft = BorderStyle.Thin;
                styleBorderDot.BorderRight = BorderStyle.Thin;
                styleBorderDot.BorderTop = BorderStyle.Thin;

                var styleCenter = workbook.CreateCellStyle();
                styleCenter.Alignment = HorizontalAlignment.Center;
                styleCenter.VerticalAlignment = VerticalAlignment.Center;

                var styleCenterBorder = workbook.CreateCellStyle();
                styleCenterBorder.CloneStyleFrom(styleBorder);
                styleCenterBorder.Alignment = HorizontalAlignment.Center;
                styleCenterBorder.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleBorderAndColorLightGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellStyleBorderAndColorLightGreen.WrapText = true;
                cellStyleBorderAndColorLightGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorLightGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 146, 208, 80 }));
                cellStyleBorderAndColorLightGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorGreen.SetFont(fontbold11);
                cellStyleBorderAndColorGreen.WrapText = true;
                cellStyleBorderAndColorGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 176, 80 }));
                cellStyleBorderAndColorGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorBlue.SetFont(fontbold11);
                cellStyleBorderAndColorBlue.WrapText = true;
                cellStyleBorderAndColorBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 112, 192 }));
                cellStyleBorderAndColorBlue.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorDarkBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorDarkBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorDarkBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 32, 96 }));

                var cellM3 = workbook.CreateCellStyle();
                cellM3.CloneStyleFrom(styleBorder);
                cellM3.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellM3.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellM3).SetFillForegroundColor(new XSSFColor(new byte[] { 248, 203, 173 }));
                cellM3.BorderBottom = BorderStyle.Thin;
                cellM3.BottomBorderColor = IndexedColors.Black.Index;
                cellM3.BorderTop = BorderStyle.Thin;
                cellM3.TopBorderColor = IndexedColors.Black.Index;
                cellM3.BorderLeft = BorderStyle.Thin;
                cellM3.LeftBorderColor = IndexedColors.Black.Index;
                cellM3.BorderRight = BorderStyle.Thin;
                cellM3.RightBorderColor = IndexedColors.Black.Index;

                var styleDedault = workbook.CreateCellStyle();
                styleDedault.CloneStyleFrom(styleBorder);
                styleDedault.SetFont(font);

                var styleDot = workbook.CreateCellStyle();
                styleDot.CloneStyleFrom(styleBorderDot);
                styleDot.SetFont(font);

                var styleTitle = workbook.CreateCellStyle();
                styleTitle.CloneStyleFrom(styleCenter);
                styleTitle.SetFont(fontbold12);

                var styleSubTitle = workbook.CreateCellStyle();
                styleSubTitle.CloneStyleFrom(styleCenter);
                styleSubTitle.SetFont(fontbold8);

                var styleHeader = workbook.CreateCellStyle();
                styleHeader.CloneStyleFrom(styleCenterBorder);
                styleHeader.SetFont(fontbold8);

                var styleDedaultMerge = workbook.CreateCellStyle();
                styleDedaultMerge.CloneStyleFrom(styleCenter);
                styleDedaultMerge.SetFont(font);

                var styleBold = workbook.CreateCellStyle();
                styleBold.SetFont(fontbold8);

                var styleSmall = workbook.CreateCellStyle();
                styleSmall.CloneStyleFrom(styleBorder);
                styleSmall.SetFont(fontSmall);

                var cellStyleHeader = workbook.CreateCellStyle();
                cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                cellStyleHeader.FillPattern = FillPattern.SolidForeground;

                var fontbold18TimesNewRoman = workbook.CreateFont();
                fontbold18TimesNewRoman.FontHeightInPoints = 18;
                fontbold18TimesNewRoman.FontName = "Times New Roman";
                fontbold18TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11TimesNewRoman = workbook.CreateFont();
                fontbold11TimesNewRoman.FontHeightInPoints = 11;
                fontbold11TimesNewRoman.FontName = "Times New Roman";
                fontbold11TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10TimesNewRoman = workbook.CreateFont();
                fontbold10TimesNewRoman.FontHeightInPoints = 10;
                fontbold10TimesNewRoman.FontName = "Times New Roman";
                fontbold10TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold9TimesNewRoman = workbook.CreateFont();
                fontbold9TimesNewRoman.FontHeightInPoints = 9;
                fontbold9TimesNewRoman.FontName = "Times New Roman";
                fontbold9TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var styleRow0 = workbook.CreateCellStyle();
                styleRow0.SetFont(fontbold18TimesNewRoman);
                styleRow0.Alignment = HorizontalAlignment.Center;
                styleRow0.VerticalAlignment = VerticalAlignment.Center;
                styleRow0.BorderBottom = BorderStyle.Thin;
                styleRow0.BorderTop = BorderStyle.Thin;
                styleRow0.BorderLeft = BorderStyle.Thin;
                styleRow0.BorderRight = BorderStyle.Thin;

                var styleBorderBold11Background = workbook.CreateCellStyle();
                styleBorderBold11Background.SetFont(fontbold11TimesNewRoman);
                styleBorderBold11Background.Alignment = HorizontalAlignment.Center;
                styleBorderBold11Background.VerticalAlignment = VerticalAlignment.Center;
                styleBorderBold11Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold11Background.BorderTop = BorderStyle.Thin;
                styleBorderBold11Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold11Background.BorderRight = BorderStyle.Thin;
                styleBorderBold11Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold11Background.FillPattern = FillPattern.SolidForeground;

                var styleBorderBold10Background = workbook.CreateCellStyle();
                styleBorderBold10Background.SetFont(fontbold10TimesNewRoman);
                styleBorderBold10Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold10Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold10Background.BorderRight = BorderStyle.Thin;
                styleBorderBold10Background.BorderTop = BorderStyle.Thin;
                styleBorderBold10Background.Alignment = HorizontalAlignment.Center;
                styleBorderBold10Background.VerticalAlignment = VerticalAlignment.Center;
                styleBorderBold10Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold10Background.FillPattern = FillPattern.SolidForeground;

                var styleBorderBold9Background = workbook.CreateCellStyle();
                styleBorderBold9Background.SetFont(fontbold9TimesNewRoman);
                styleBorderBold9Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold9Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold9Background.BorderRight = BorderStyle.Thin;
                styleBorderBold9Background.BorderTop = BorderStyle.Thin;
                styleBorderBold9Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold9Background.FillPattern = FillPattern.SolidForeground;
                #endregion

                ISheet sheet1 = workbook.CreateSheet(month.ToString("00") + year.ToString("0000") + "-NGAYCONG-THUONG-BHXH ");

                #region Title
                var rowIndex = 0;
                var columnIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                var cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Công ty TNHH CNSH SÀI GÒN XANH");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Địa chỉ: 127 Nguyễn Trọng Tuyển - P.15 - Q.Phú Nhuận - Tp HCM");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Điện thoại: (08)-39971869 - 38442457 - Fax: 08-39971869");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MST: 0302519810");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;
                #endregion

                #region Thang
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Thời gian:");
                cell.CellStyle = styleTitle;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(thangFormat);
                cell.CellStyle = styleTitle;
                rowIndex++;
                #endregion

                row = sheet1.CreateRow(rowIndex);
                row.Height = 1000;
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Tên NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Chức vụ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày vào làm");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày làm việc");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Lễ tết");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Phép năm");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Thưởng lễ, tết");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Lương đóng BHXH");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;
                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.SanXuatNgayCongThuongBHXHPost)]
        [HttpPost]
        public ActionResult SanXuatNgayCongThuongBHXHPost()
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

                    var timeRow = sheet.GetRow(5);
                    var time = Utility.GetFormattedCellValue(timeRow.GetCell(1));
                    var endMonthDate = Utility.GetToDate(time);
                    var month = endMonthDate.Month;
                    var year = endMonthDate.Year;
                    var toDate = new DateTime(year, month, 25);
                    var fromDate = toDate.AddMonths(-1).AddDays(1);
                    var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);


                    for (int i = 7; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var ma = string.Empty;
                        var ten = string.Empty;
                        var chucvu = string.Empty;
                        var ngayvaolam = string.Empty;
                        double ngaylamviec = 0;
                        double phepnam = 0;
                        double letet = 0;
                        decimal thuongletet = 0;
                        decimal bhxh = 0;

                        int columnIndex = 0;
                        ma = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        ten = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        chucvu = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        ngayvaolam = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        ngaylamviec = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        phepnam = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        letet = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        thuongletet = (decimal)Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        bhxh = (decimal)Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;

                        if (string.IsNullOrEmpty(ten))
                        {
                            continue;
                        }
                        var alias = Utility.AliasConvert(ten);
                        var existEmployee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                        if (existEmployee != null)
                        {
                            DataSanXuat(month, year, ngaylamviec, phepnam, letet, thuongletet, bhxh, existEmployee);
                        }
                        else
                        {
                            InsertNewEmployee(ten, ma, chucvu, ngayvaolam);
                            var employee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                            DataSanXuat(month, year, ngaylamviec, phepnam, letet, thuongletet, bhxh, employee);
                        }
                    }
                }
            }
            return Json(new { result = true });
        }

        private void DataSanXuat(int month, int year, double ngaylamviec, double phepnam, double letet, decimal thuongletet, decimal bhxh, Employee existEmployee)
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
        }

        private void TamUngSanXuat(int month, int year, decimal tamung, Employee existEmployee)
        {
            // Salary base month-year
            var existSalary = dbContext.SalaryEmployeeMonths.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(existEmployee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (existSalary != null)
            {
                var builderSalary = Builders<SalaryEmployeeMonth>.Filter;
                var filterSalary = builderSalary.Eq(m => m.Id, existSalary.Id);
                var updateSalary = Builders<SalaryEmployeeMonth>.Update
                    .Set(m => m.TamUng, tamung)
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
                    TamUng = tamung
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

        private void InsertNewEmployee(string fullname, string oldcode, string chucvu, string ngayvaolam)
        {
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

        #endregion

        #region TONG HOP
        [Route(Constants.LinkSalary.TongHopTrongGio)]
        public async Task<IActionResult> TongHopTrongGio(string thang, string id)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongSX, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL
            var employeeDdl = await dbContext.Employees.Find(m => m.Enable.Equals(true) && m.SalaryType.Equals((int)EKhoiLamViec.SX) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
            #endregion

            #region Times
            var toDate = Utility.GetToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            #endregion

            #region Setting
            var thamsoEntity = dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToList();
            var mauSo = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-lam-viec")).Value);
            var mauSoBaoVe = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-bao-ve")).Value);
            var mauSoChuyenCan = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-chuyen-can")).Value);
            var mauSoTienCom = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-tien-com")).Value);
            decimal ibhxh = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhxh")) == null ? 8 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhxh")).Value);
            decimal ibhyt = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhyt")) == null ? (decimal)1.5 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhyt")).Value);
            decimal ibhtn = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhtn")) == null ? 1 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhtn")).Value);
            #endregion

            #region Filter
            var builder = Builders<FactoryProductCongTheoThang>.Filter;
            var filter = builder.Eq(m => m.Mode, (int)EMode.TrongGio) & builder.Eq(m => m.Month, month) & builder.Eq(m => m.Year, year);
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.EmployeeId, id.Trim());
            }
            #endregion

            #region Sort
            //var sortBuilder = Builders<FactoryProductCongTheoThang>.Sort.Ascending(m => m.Code);
            //var congMs = await dbContext.FactoryProductCongTheoThangs.Find(filter).Sort(sortBuilder).ToListAsync();
            #endregion

            var congMs = await dbContext.FactoryProductCongTheoThangs.Find(filter).ToListAsync();

            var thanhphams = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToListAsync();
            var congviecs = await dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true) && m.Main.Equals(false)).SortBy(m => m.Sort).ToListAsync();
            var dongiaDMs = await dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToListAsync();
            var dongiaM3 = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            var congs = await dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.Year.Equals(year) && m.Month.Equals(month)).ToListAsync();

            var viewModel = new BangLuongViewModel
            {
                MCongs = congMs,
                MonthYears = Utility.DllMonths(),
                Thang = thang,
                EmployeesDdl = employeeDdl,
                Id = id,
                ThanhPhams = thanhphams,
                CongViecs = congviecs,
                DonGiaDMs = dongiaDMs,
                DonGiaM3 = dongiaM3,
                Congs = congs
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.TongHopNgoaiGio)]
        public async Task<IActionResult> TongHopNgoaiGio(string thang, string id)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongSX, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL
            var employeeDdl = await dbContext.Employees.Find(m => m.Enable.Equals(true) && m.SalaryType.Equals((int)EKhoiLamViec.SX) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
            #endregion

            #region Times
            var toDate = Utility.GetToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            #endregion

            #region Setting
            var thamsoEntity = dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToList();
            var mauSo = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-lam-viec")).Value);
            var mauSoBaoVe = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-bao-ve")).Value);
            var mauSoChuyenCan = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-chuyen-can")).Value);
            var mauSoTienCom = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-tien-com")).Value);
            decimal ibhxh = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhxh")) == null ? 8 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhxh")).Value);
            decimal ibhyt = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhyt")) == null ? (decimal)1.5 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhyt")).Value);
            decimal ibhtn = thamsoEntity.Find(m => m.Key.Equals("ti-le-bhtn")) == null ? 1 : Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ti-le-bhtn")).Value);
            #endregion

            #region Filter
            var builder = Builders<FactoryProductCongTheoThang>.Filter;
            var filter = builder.Eq(m => m.Mode, (int)EMode.NgoaiGio) & builder.Eq(m => m.Month, month) & builder.Eq(m => m.Year, year);
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.EmployeeId, id.Trim());
            }
            #endregion

            #region Sort
            //var sortBuilder = Builders<FactoryProductCongTheoThang>.Sort.Ascending(m => m.Code);
            //var congMs = await dbContext.FactoryProductCongTheoThangs.Find(filter).Sort(sortBuilder).ToListAsync();
            #endregion

            var congMs = await dbContext.FactoryProductCongTheoThangs.Find(filter).ToListAsync();

            var thanhphams = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToListAsync();
            var congviecs = await dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true) && m.Main.Equals(false)).SortBy(m => m.Sort).ToListAsync();
            var dongiaDMs = await dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToListAsync();
            var dongiaM3 = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();

            var viewModel = new BangLuongViewModel
            {
                MCongs = congMs,
                MonthYears = Utility.DllMonths(),
                Thang = thang,
                EmployeesDdl = employeeDdl,
                Id = id,
                ThanhPhams = thanhphams,
                CongViecs = congviecs,
                DonGiaDMs = dongiaDMs,
                DonGiaM3 = dongiaM3
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.SanXuatTongHopTrongGioTemplate)]
        public async Task<IActionResult> SanXuatTongHopTrongGioTemplate(string fileName, string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.BangChamCong, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var toDate = Utility.GetToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            var thangFormat = month.ToString("00") + "-" + year.ToString("0000");
            #endregion

            var thanhphams = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToListAsync();
            var congviecs = await dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true) && m.Main.Equals(false)).SortBy(m => m.Sort).ToListAsync();
            var dongiathanhphans = await dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToListAsync();
            var dongiaM3entity = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            var dongiaM3 = dongiaM3entity != null ? dongiaM3entity.Price : 11300;
            int thanhphamCount = thanhphams.Count();
            int congviecCount = congviecs.Count();

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"" + thangFormat + "-tong-hop-trong-gio";
            sFileName += "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var font = workbook.CreateFont();
                font.FontHeightInPoints = 8;
                font.FontName = "Arial";

                var fontSmall = workbook.CreateFont();
                fontSmall.FontHeightInPoints = 5;
                fontSmall.FontName = "Arial";

                var fontbold = workbook.CreateFont();
                fontbold.FontHeightInPoints = 8;
                fontbold.FontName = "Arial";
                fontbold.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold8 = workbook.CreateFont();
                fontbold8.FontHeightInPoints = 8;
                fontbold8.FontName = "Arial";
                fontbold8.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10 = workbook.CreateFont();
                fontbold10.FontHeightInPoints = 10;
                fontbold10.FontName = "Arial";
                fontbold10.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11 = workbook.CreateFont();
                fontbold11.FontHeightInPoints = 11;
                fontbold11.FontName = "Arial";
                fontbold11.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold12 = workbook.CreateFont();
                fontbold12.FontHeightInPoints = 12;
                fontbold12.FontName = "Arial";
                fontbold12.Boldweight = (short)FontBoldWeight.Bold;

                var styleBorder = workbook.CreateCellStyle();
                styleBorder.BorderBottom = BorderStyle.Thin;
                styleBorder.BorderLeft = BorderStyle.Thin;
                styleBorder.BorderRight = BorderStyle.Thin;
                styleBorder.BorderTop = BorderStyle.Thin;

                var styleBorderDot = workbook.CreateCellStyle();
                styleBorderDot.BorderBottom = BorderStyle.Dotted;
                styleBorderDot.BorderLeft = BorderStyle.Thin;
                styleBorderDot.BorderRight = BorderStyle.Thin;
                styleBorderDot.BorderTop = BorderStyle.Thin;

                var styleCenter = workbook.CreateCellStyle();
                styleCenter.Alignment = HorizontalAlignment.Center;
                styleCenter.VerticalAlignment = VerticalAlignment.Center;

                var styleCenterBorder = workbook.CreateCellStyle();
                styleCenterBorder.CloneStyleFrom(styleBorder);
                styleCenterBorder.Alignment = HorizontalAlignment.Center;
                styleCenterBorder.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleBorderAndColorLightGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellStyleBorderAndColorLightGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorLightGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 146, 208, 80 }));
                cellStyleBorderAndColorLightGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorGreen.SetFont(fontbold11);
                cellStyleBorderAndColorGreen.WrapText = true;
                cellStyleBorderAndColorGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 176, 80 }));
                cellStyleBorderAndColorGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorBlue.SetFont(fontbold11);
                cellStyleBorderAndColorBlue.WrapText = true;
                cellStyleBorderAndColorBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 112, 192 }));
                cellStyleBorderAndColorBlue.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorDarkBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorDarkBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorDarkBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 32, 96 }));

                var cellM3 = workbook.CreateCellStyle();
                cellM3.CloneStyleFrom(styleBorder);
                cellM3.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellM3.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellM3).SetFillForegroundColor(new XSSFColor(new byte[] { 248, 203, 173 }));
                cellM3.BorderBottom = BorderStyle.Thin;
                cellM3.BottomBorderColor = IndexedColors.Black.Index;
                cellM3.BorderTop = BorderStyle.Thin;
                cellM3.TopBorderColor = IndexedColors.Black.Index;
                cellM3.BorderLeft = BorderStyle.Thin;
                cellM3.LeftBorderColor = IndexedColors.Black.Index;
                cellM3.BorderRight = BorderStyle.Thin;
                cellM3.RightBorderColor = IndexedColors.Black.Index;

                var styleDedault = workbook.CreateCellStyle();
                styleDedault.CloneStyleFrom(styleBorder);
                styleDedault.SetFont(font);

                var styleDot = workbook.CreateCellStyle();
                styleDot.CloneStyleFrom(styleBorderDot);
                styleDot.SetFont(font);

                var styleTitle = workbook.CreateCellStyle();
                styleTitle.CloneStyleFrom(styleCenter);
                styleTitle.SetFont(fontbold12);

                var styleSubTitle = workbook.CreateCellStyle();
                styleSubTitle.CloneStyleFrom(styleCenter);
                styleSubTitle.SetFont(fontbold8);

                var styleHeader = workbook.CreateCellStyle();
                styleHeader.CloneStyleFrom(styleCenterBorder);
                styleHeader.SetFont(fontbold8);

                var styleDedaultMerge = workbook.CreateCellStyle();
                styleDedaultMerge.CloneStyleFrom(styleCenter);
                styleDedaultMerge.SetFont(font);

                var styleBold = workbook.CreateCellStyle();
                styleBold.SetFont(fontbold8);

                var styleSmall = workbook.CreateCellStyle();
                styleSmall.CloneStyleFrom(styleBorder);
                styleSmall.SetFont(fontSmall);
                #endregion

                ISheet sheet1 = workbook.CreateSheet("Tổng hợp trong giờ");

                #region Title
                var rowIndex = 0;
                var columnIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                var cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Công ty TNHH CNSH SÀI GÒN XANH");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Địa chỉ: 127 Nguyễn Trọng Tuyển - P.15 - Q.Phú Nhuận - Tp HCM");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Điện thoại: (08)-39971869 - 38442457 - Fax: 08-39971869");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MST: 0302519810");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;
                #endregion

                #region Thang
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Thời gian:");
                cell.CellStyle = styleTitle;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(thangFormat);
                cell.CellStyle = styleTitle;
                rowIndex++;
                #endregion

                #region Header
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 3;
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + thanhphamCount);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP CÔNG ĐÓNG BAO");
                cell.CellStyle = styleTitle;
                columnIndex = columnIndex + thanhphamCount;
                columnIndex++;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP BỐC HÀNG");
                cell.CellStyle = styleTitle;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + congviecCount);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP CÔNG VIỆC KHÁC");
                cell.CellStyle = styleTitle;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;
                #endregion

                #region Row 10
                int totalColumn = 3 + thanhphamCount + 1 + 1 + 1 + congviecCount;
                columnIndex = 0;
                for (var i = 1; i <= totalColumn; i++)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(i);
                    cell.CellStyle = styleHeader;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 11
                row = sheet1.CreateRow(rowIndex);
                row.Height = 2000;
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TÊN SẢN PHẨM");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in thanhphams)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(thanhpham.Name);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleHeader;
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Số M3");
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleHeader;
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var congviec in congviecs)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(congviec.Name);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 12
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MÃ SẢN PHẨM");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in thanhphams)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(thanhpham.Code);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                for (var i12 = columnIndex; i12 < totalColumn; i12++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 13
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("ĐƠN GIÁ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in dongiathanhphans)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue((double)thanhpham.DonGiaDieuChinh);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.Numeric);
                cell.SetCellValue((double)dongiaM3);
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;
                foreach (var congviec in congviecs)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue((double)congviec.Price);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 14
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Tên NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                var toTP = columnIndex + thanhphamCount;
                for (var i14 = columnIndex; i14 < toTP; i14++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                for (var i14 = columnIndex; i14 < totalColumn; i14++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.SanXuatTongHopTrongGioPost)]
        [HttpPost]
        public ActionResult SanXuatTongHopTrongGioPost()
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

                    var timeRow = sheet.GetRow(5);
                    var time = Utility.GetFormattedCellValue(timeRow.GetCell(1));
                    var endMonthDate = Utility.GetToDate(time);
                    var month = endMonthDate.Month;
                    var year = endMonthDate.Year;

                    var priceRow = sheet.GetRow(11);

                    var thanhphams = dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToList();
                    var congviecs = dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true)).ToList();
                    var congvieckhacs = congviecs.Where(m => m.Main.Equals(false)).OrderBy(m => m.Sort).ToList();
                    var congviecbochang = congviecs.Find(m => m.Main.Equals(true) && m.Alias.Equals("boc-hang"));

                    var dongiathanhphans = dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToList();
                    var dongiaM3entity = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
                    var dongiaM3 = dongiaM3entity != null ? dongiaM3entity.Price : 11300;
                    int thanhphamCount = thanhphams.Count();
                    int congviecCount = congviecs.Count();

                    for (int i = 13; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var columnIndex = 0;
                        var code = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var fullName = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        columnIndex++;

                        if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(fullName))
                        {
                            continue;
                        }

                        var employee = !string.IsNullOrEmpty(fullName) ? dbContext.Employees.Find(m => m.Enable.Equals(true) & m.FullName.Equals(fullName.Trim())).FirstOrDefault() : null;
                        if (employee == null)
                        {
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "tong-hop-trong-gio",
                                Object = "Code: " + code + " | Name: " + fullName + " | Line " + i,
                                Error = "Khong tim thay thong tin",
                                DateTime = DateTime.Now.ToString()
                            });
                            continue;
                        }

                        double tongthoigiancongvieckhac = 0;
                        decimal tongcongcongvieckhac = 0;
                        decimal thanhtienthanhpham = 0;
                        decimal thanhtienbochang = 0;
                        decimal thanhtientronggio = 0;

                        #region THANH PHAM
                        foreach (var thanhpham in thanhphams)
                        {
                            // Get value
                            decimal objectPrice = (decimal)Utility.GetNumbericCellValue(priceRow.GetCell(columnIndex));
                            double objectValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                            decimal objectAmount = Convert.ToDecimal(objectValue * (double)objectPrice);
                            thanhtienthanhpham += objectAmount;
                            // Insert db
                            var exist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(thanhpham.Code) && m.Type.Equals((int)EDinhMuc.DongGoi) && m.Mode.Equals((int)EMode.TrongGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (exist != null)
                            {
                                var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, exist.Id);
                                var update = Builders<FactoryProductCongTheoThang>.Update
                                    .Set(m => m.ObjectPrice, objectPrice)
                                    .Set(m => m.Value, objectValue)
                                    .Set(m => m.Amount, objectAmount);
                                dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                            }
                            else
                            {
                                dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                                {
                                    Month = month,
                                    Year = year,
                                    Type = (int)EDinhMuc.DongGoi,
                                    Mode = (int)EMode.TrongGio,
                                    EmployeeId = employee.Id,
                                    EmployeeCode = employee.CodeOld,
                                    EmployeeName = employee.FullName,
                                    EmployeeAlias = employee.AliasFullName,
                                    ObjectId = thanhpham.Id,
                                    ObjectCode = thanhpham.Code,
                                    ObjectName = thanhpham.Name,
                                    ObjectAlias = thanhpham.Alias,
                                    ObjectSort = thanhpham.Sort,
                                    ObjectPrice = objectPrice,
                                    Value = objectValue,
                                    Amount = objectAmount
                                });
                            }
                            columnIndex++;
                        }
                        #endregion
                        columnIndex++;

                        #region BOC HANG
                        decimal bochangPrice = (decimal)Utility.GetNumbericCellValue(priceRow.GetCell(columnIndex));
                        double bochangValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        decimal bochangAmount = Convert.ToDecimal(bochangValue * (double)bochangPrice);
                        thanhtienbochang += bochangAmount;

                        // Insert db
                        var bochangexist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(congviecbochang.Code) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Mode.Equals((int)EMode.TrongGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                        if (bochangexist != null)
                        {
                            var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, bochangexist.Id);
                            var update = Builders<FactoryProductCongTheoThang>.Update
                                .Set(m => m.ObjectPrice, bochangPrice)
                                .Set(m => m.Value, bochangValue)
                                .Set(m => m.Amount, bochangAmount);
                            dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                        }
                        else
                        {
                            dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                            {
                                Month = month,
                                Year = year,
                                Type = (int)EDinhMuc.BocVac,
                                Mode = (int)EMode.TrongGio,
                                EmployeeId = employee.Id,
                                EmployeeCode = employee.CodeOld,
                                EmployeeName = employee.FullName,
                                EmployeeAlias = employee.AliasFullName,
                                ObjectId = congviecbochang.Id,
                                ObjectCode = congviecbochang.Code,
                                ObjectName = congviecbochang.Name,
                                ObjectAlias = congviecbochang.Alias,
                                ObjectSort = congviecbochang.Sort,
                                ObjectPrice = bochangPrice,
                                Value = bochangValue,
                                Amount = bochangAmount
                            });
                        }
                        columnIndex++;
                        #endregion

                        columnIndex++;
                        #region CONG VIEC KHAC
                        foreach (var congvieckhac in congvieckhacs)
                        {
                            // Get value
                            decimal objectPrice = (decimal)Utility.GetNumbericCellValue(priceRow.GetCell(columnIndex));
                            double objectValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                            tongthoigiancongvieckhac += objectValue;
                            decimal objectAmount = Convert.ToDecimal(objectValue * (double)objectPrice);
                            tongcongcongvieckhac += objectAmount;

                            // Insert db
                            var exist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(congvieckhac.Code) && m.Type.Equals((int)EDinhMuc.CongViecKhac) && m.Mode.Equals((int)EMode.TrongGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (exist != null)
                            {
                                var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, exist.Id);
                                var update = Builders<FactoryProductCongTheoThang>.Update
                                    .Set(m => m.ObjectPrice, objectPrice)
                                    .Set(m => m.Value, objectValue)
                                    .Set(m => m.Amount, objectAmount);
                                dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                            }
                            else
                            {
                                dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                                {
                                    Month = month,
                                    Year = year,
                                    Type = (int)EDinhMuc.CongViecKhac,
                                    Mode = (int)EMode.TrongGio,
                                    EmployeeId = employee.Id,
                                    EmployeeCode = employee.CodeOld,
                                    EmployeeName = employee.FullName,
                                    EmployeeAlias = employee.AliasFullName,
                                    ObjectId = congvieckhac.Id,
                                    ObjectCode = congvieckhac.Code,
                                    ObjectName = congvieckhac.Name,
                                    ObjectAlias = congvieckhac.Alias,
                                    ObjectSort = congvieckhac.Sort,
                                    ObjectPrice = objectPrice,
                                    Value = objectValue,
                                    Amount = objectAmount
                                });
                            }
                            columnIndex++;
                        }
                        #endregion

                        thanhtientronggio = thanhtienthanhpham + thanhtienbochang + Convert.ToDecimal((double)tongcongcongvieckhac / 60);

                        #region Update to CONG TONG
                        var existCongTong = dbContext.EmployeeCongs.Find(m => m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                        if (existCongTong != null)
                        {
                            var filterCongTong = Builders<EmployeeCong>.Filter.Eq(m => m.Id, existCongTong.Id);
                            var updateCongTong = Builders<EmployeeCong>.Update
                                .Set(m => m.TongThoiGianCongViecKhacTrongGio, tongthoigiancongvieckhac / 60)
                                .Set(m => m.TongCongCongViecKhacTrongGio, tongcongcongvieckhac)
                                .Set(m => m.ThanhTienTrongGio, thanhtientronggio)
                                .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                            dbContext.EmployeeCongs.UpdateOne(filterCongTong, updateCongTong);
                        }
                        else
                        {
                            dbContext.EmployeeCongs.InsertOne(new EmployeeCong()
                            {
                                Month = month,
                                Year = year,
                                EmployeeId = employee.Id,
                                EmployeeCode = employee.CodeOld,
                                EmployeeName = employee.FullName,
                                TongThoiGianCongViecKhacTrongGio = tongthoigiancongvieckhac / 60,
                                TongCongCongViecKhacTrongGio = tongcongcongvieckhac,
                                ThanhTienTrongGio = thanhtientronggio
                            });
                        }
                        #endregion
                    }

                    return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Production + "/" + Constants.LinkSalary.TongHopTrongGio + "?thang=" + month + "-" + year });
                }
            }
            else
            {
                return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Production + "/" + Constants.LinkSalary.TongHopTrongGio });
            }
        }

        [Route(Constants.LinkSalary.SanXuatTongHopNgoaiGioTemplate)]
        public async Task<IActionResult> SanXuatTongHopNgoaiGioTemplate(string fileName, string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.BangChamCong, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var toDate = Utility.GetToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            var thangFormat = month.ToString("00") + "-" + year.ToString("0000");
            #endregion

            var thanhphams = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToListAsync();
            var congviecs = await dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true) && m.Main.Equals(false)).SortBy(m => m.Sort).ToListAsync();
            var dongiathanhphans = await dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToListAsync();
            var dongiaM3entity = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            var dongiaM3 = dongiaM3entity != null ? dongiaM3entity.Price : 11300;

            var tangcaentity = dbContext.FactoryProductDinhMucTangCas.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            double tangcatile = tangcaentity != null ? (1 + tangcaentity.PhanTramTangCa / 100) : 1.1;
            var dongiaM3TangCa = Convert.ToDecimal((double)dongiaM3 * tangcatile);

            int thanhphamCount = thanhphams.Count();
            int congviecCount = congviecs.Count();

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"" + thangFormat + "-tong-hop-ngoai-gio";
            sFileName += "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var font = workbook.CreateFont();
                font.FontHeightInPoints = 8;
                font.FontName = "Arial";

                var fontSmall = workbook.CreateFont();
                fontSmall.FontHeightInPoints = 5;
                fontSmall.FontName = "Arial";

                var fontbold = workbook.CreateFont();
                fontbold.FontHeightInPoints = 8;
                fontbold.FontName = "Arial";
                fontbold.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold8 = workbook.CreateFont();
                fontbold8.FontHeightInPoints = 8;
                fontbold8.FontName = "Arial";
                fontbold8.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10 = workbook.CreateFont();
                fontbold10.FontHeightInPoints = 10;
                fontbold10.FontName = "Arial";
                fontbold10.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11 = workbook.CreateFont();
                fontbold11.FontHeightInPoints = 11;
                fontbold11.FontName = "Arial";
                fontbold11.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold12 = workbook.CreateFont();
                fontbold12.FontHeightInPoints = 12;
                fontbold12.FontName = "Arial";
                fontbold12.Boldweight = (short)FontBoldWeight.Bold;

                var styleBorder = workbook.CreateCellStyle();
                styleBorder.BorderBottom = BorderStyle.Thin;
                styleBorder.BorderLeft = BorderStyle.Thin;
                styleBorder.BorderRight = BorderStyle.Thin;
                styleBorder.BorderTop = BorderStyle.Thin;

                var styleBorderDot = workbook.CreateCellStyle();
                styleBorderDot.BorderBottom = BorderStyle.Dotted;
                styleBorderDot.BorderLeft = BorderStyle.Thin;
                styleBorderDot.BorderRight = BorderStyle.Thin;
                styleBorderDot.BorderTop = BorderStyle.Thin;

                var styleCenter = workbook.CreateCellStyle();
                styleCenter.Alignment = HorizontalAlignment.Center;
                styleCenter.VerticalAlignment = VerticalAlignment.Center;

                var styleCenterBorder = workbook.CreateCellStyle();
                styleCenterBorder.CloneStyleFrom(styleBorder);
                styleCenterBorder.Alignment = HorizontalAlignment.Center;
                styleCenterBorder.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleBorderAndColorLightGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellStyleBorderAndColorLightGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorLightGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 146, 208, 80 }));
                cellStyleBorderAndColorLightGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorGreen.SetFont(fontbold11);
                cellStyleBorderAndColorGreen.WrapText = true;
                cellStyleBorderAndColorGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 176, 80 }));
                cellStyleBorderAndColorGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorBlue.SetFont(fontbold11);
                cellStyleBorderAndColorBlue.WrapText = true;
                cellStyleBorderAndColorBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 112, 192 }));
                cellStyleBorderAndColorBlue.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorDarkBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorDarkBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorDarkBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 32, 96 }));

                var cellM3 = workbook.CreateCellStyle();
                cellM3.CloneStyleFrom(styleBorder);
                cellM3.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellM3.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellM3).SetFillForegroundColor(new XSSFColor(new byte[] { 248, 203, 173 }));
                cellM3.BorderBottom = BorderStyle.Thin;
                cellM3.BottomBorderColor = IndexedColors.Black.Index;
                cellM3.BorderTop = BorderStyle.Thin;
                cellM3.TopBorderColor = IndexedColors.Black.Index;
                cellM3.BorderLeft = BorderStyle.Thin;
                cellM3.LeftBorderColor = IndexedColors.Black.Index;
                cellM3.BorderRight = BorderStyle.Thin;
                cellM3.RightBorderColor = IndexedColors.Black.Index;

                var styleDedault = workbook.CreateCellStyle();
                styleDedault.CloneStyleFrom(styleBorder);
                styleDedault.SetFont(font);

                var styleDot = workbook.CreateCellStyle();
                styleDot.CloneStyleFrom(styleBorderDot);
                styleDot.SetFont(font);

                var styleTitle = workbook.CreateCellStyle();
                styleTitle.CloneStyleFrom(styleCenter);
                styleTitle.SetFont(fontbold12);

                var styleSubTitle = workbook.CreateCellStyle();
                styleSubTitle.CloneStyleFrom(styleCenter);
                styleSubTitle.SetFont(fontbold8);

                var styleHeader = workbook.CreateCellStyle();
                styleHeader.CloneStyleFrom(styleCenterBorder);
                styleHeader.SetFont(fontbold8);

                var styleDedaultMerge = workbook.CreateCellStyle();
                styleDedaultMerge.CloneStyleFrom(styleCenter);
                styleDedaultMerge.SetFont(font);

                var styleBold = workbook.CreateCellStyle();
                styleBold.SetFont(fontbold8);

                var styleSmall = workbook.CreateCellStyle();
                styleSmall.CloneStyleFrom(styleBorder);
                styleSmall.SetFont(fontSmall);
                #endregion

                ISheet sheet1 = workbook.CreateSheet("Tổng hợp ngoài giờ");

                #region Title
                var rowIndex = 0;
                var columnIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                var cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Công ty TNHH CNSH SÀI GÒN XANH");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Địa chỉ: 127 Nguyễn Trọng Tuyển - P.15 - Q.Phú Nhuận - Tp HCM");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Điện thoại: (08)-39971869 - 38442457 - Fax: 08-39971869");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MST: 0302519810");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;
                #endregion

                #region Thang
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Thời gian:");
                cell.CellStyle = styleTitle;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(thangFormat);
                cell.CellStyle = styleTitle;
                rowIndex++;
                #endregion

                #region Header
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 3;
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + thanhphamCount);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP CÔNG ĐÓNG BAO");
                cell.CellStyle = styleTitle;
                columnIndex = columnIndex + thanhphamCount;
                columnIndex++;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP BỐC HÀNG");
                cell.CellStyle = styleTitle;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + congviecCount);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP CÔNG VIỆC KHÁC");
                cell.CellStyle = styleTitle;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;
                #endregion

                #region Row 10
                int totalColumn = 3 + thanhphamCount + 1 + 1 + 1 + congviecCount;
                columnIndex = 0;
                for (var i = 1; i <= totalColumn; i++)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(i);
                    cell.CellStyle = styleHeader;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 11
                row = sheet1.CreateRow(rowIndex);
                row.Height = 2000;
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TÊN SẢN PHẨM");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in thanhphams)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(thanhpham.Name);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleHeader;
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Số M3");
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleHeader;
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var congviec in congviecs)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(congviec.Name);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 12
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MÃ SẢN PHẨM");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in thanhphams)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(thanhpham.Code);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                for (var i12 = columnIndex; i12 < totalColumn; i12++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 13
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("ĐƠN GIÁ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in dongiathanhphans)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue((double)thanhpham.DonGiaTangCa);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.Numeric);
                cell.SetCellValue((double)dongiaM3TangCa);
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;
                foreach (var congviec in congviecs)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue((double)congviec.Price * tangcatile);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 14
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Tên NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                var toTP = columnIndex + thanhphamCount;
                for (var i14 = columnIndex; i14 < toTP; i14++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                for (var i14 = columnIndex; i14 < totalColumn; i14++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.SanXuatTongHopNgoaiGioPost)]
        [HttpPost]
        public ActionResult SanXuatTongHopNgoaiGioPost()
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

                    var timeRow = sheet.GetRow(5);
                    var time = Utility.GetFormattedCellValue(timeRow.GetCell(1));
                    var endMonthDate = Utility.GetToDate(time);
                    var month = endMonthDate.Month;
                    var year = endMonthDate.Year;

                    var priceRow = sheet.GetRow(11);

                    var thanhphams = dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToList();
                    var congviecs = dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true)).ToList();
                    var congvieckhacs = congviecs.Where(m => m.Main.Equals(false)).OrderBy(m => m.Sort).ToList();
                    var congviecbochang = congviecs.Find(m => m.Main.Equals(true) && m.Alias.Equals("boc-hang"));

                    var dongiathanhphans = dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToList();
                    var dongiaM3entity = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
                    var dongiaM3 = dongiaM3entity != null ? dongiaM3entity.Price : 11300;

                    var tangcaentity = dbContext.FactoryProductDinhMucTangCas.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                    double tangcatile = tangcaentity != null ? (1 + tangcaentity.PhanTramTangCa / 100) : 1.1;
                    var dongiaM3TangCa = Convert.ToDecimal((double)dongiaM3 * tangcatile);

                    int thanhphamCount = thanhphams.Count();
                    int congviecCount = congviecs.Count();

                    for (int i = 13; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var columnIndex = 0;
                        var code = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var fullName = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        columnIndex++;

                        if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(fullName))
                        {
                            continue;
                        }

                        var employee = !string.IsNullOrEmpty(fullName) ? dbContext.Employees.Find(m => m.Enable.Equals(true) & m.FullName.Equals(fullName)).FirstOrDefault() : null;
                        //if (employee == null)
                        //{
                        //    employee = !string.IsNullOrEmpty(code) ? dbContext.Employees.Find(m => m.Enable.Equals(true) & m.CodeOld.Equals(code)).FirstOrDefault() : null;
                        //}
                        if (employee == null)
                        {
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "tong-hop-ngoai-gio",
                                Object = "Code: " + code + " | Name: " + fullName + " | Line " + i,
                                Error = "Khong tim thay thong tin",
                                DateTime = DateTime.Now.ToString()
                            });
                            continue;
                        }

                        double tongthoigiancongvieckhac = 0;
                        decimal thanhtientongcongcongvieckhac = 0;
                        decimal thanhtienthanhpham = 0;
                        decimal thanhtienbochang = 0;
                        decimal thanhtienngoaigio = 0;

                        #region THANH PHAM
                        foreach (var thanhpham in thanhphams)
                        {
                            // Get value
                            decimal objectPrice = (decimal)Utility.GetNumbericCellValue(priceRow.GetCell(columnIndex));
                            double objectValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                            decimal objectAmount = Convert.ToDecimal(objectValue * (double)objectPrice);
                            thanhtienthanhpham += objectAmount;
                            // Insert db
                            var exist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(thanhpham.Code) && m.Type.Equals((int)EDinhMuc.DongGoi) && m.Mode.Equals((int)EMode.NgoaiGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (exist != null)
                            {
                                var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, exist.Id);
                                var update = Builders<FactoryProductCongTheoThang>.Update
                                    .Set(m => m.ObjectPrice, objectPrice)
                                    .Set(m => m.Value, objectValue)
                                    .Set(m => m.Amount, objectAmount);
                                dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                            }
                            else
                            {
                                dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                                {
                                    Month = month,
                                    Year = year,
                                    Type = (int)EDinhMuc.DongGoi,
                                    Mode = (int)EMode.NgoaiGio,
                                    EmployeeId = employee.Id,
                                    EmployeeCode = employee.CodeOld,
                                    EmployeeName = employee.FullName,
                                    EmployeeAlias = employee.AliasFullName,
                                    ObjectId = thanhpham.Id,
                                    ObjectCode = thanhpham.Code,
                                    ObjectName = thanhpham.Name,
                                    ObjectAlias = thanhpham.Alias,
                                    ObjectSort = thanhpham.Sort,
                                    ObjectPrice = objectPrice,
                                    Value = objectValue,
                                    Amount = objectAmount
                                });
                            }
                            columnIndex++;
                        }
                        #endregion
                        columnIndex++;

                        #region BOC HANG
                        decimal bochangPrice = (decimal)Utility.GetNumbericCellValue(priceRow.GetCell(columnIndex));
                        double bochangValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        decimal bochangAmount = Convert.ToDecimal(bochangValue * (double)bochangPrice);
                        thanhtienbochang += bochangAmount;

                        // Insert db
                        var bochangexist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(congviecbochang.Code) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Mode.Equals((int)EMode.NgoaiGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                        if (bochangexist != null)
                        {
                            var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, bochangexist.Id);
                            var update = Builders<FactoryProductCongTheoThang>.Update
                                .Set(m => m.ObjectPrice, bochangPrice)
                                .Set(m => m.Value, bochangValue)
                                .Set(m => m.Amount, bochangAmount);
                            dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                        }
                        else
                        {
                            dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                            {
                                Month = month,
                                Year = year,
                                Type = (int)EDinhMuc.BocVac,
                                Mode = (int)EMode.NgoaiGio,
                                EmployeeId = employee.Id,
                                EmployeeCode = employee.CodeOld,
                                EmployeeName = employee.FullName,
                                EmployeeAlias = employee.AliasFullName,
                                ObjectId = congviecbochang.Id,
                                ObjectCode = congviecbochang.Code,
                                ObjectName = congviecbochang.Name,
                                ObjectAlias = congviecbochang.Alias,
                                ObjectSort = congviecbochang.Sort,
                                ObjectPrice = bochangPrice,
                                Value = bochangValue,
                                Amount = bochangAmount
                            });
                        }
                        columnIndex++;
                        #endregion

                        columnIndex++;
                        #region CONG VIEC KHAC
                        foreach (var congvieckhac in congvieckhacs)
                        {
                            // Get value
                            decimal objectPrice = (decimal)Utility.GetNumbericCellValue(priceRow.GetCell(columnIndex));
                            double objectValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                            tongthoigiancongvieckhac += objectValue;
                            decimal objectAmount = Convert.ToDecimal(objectValue * (double)objectPrice);
                            thanhtientongcongcongvieckhac += objectAmount;

                            // Insert db
                            var exist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(congvieckhac.Code) && m.Type.Equals((int)EDinhMuc.CongViecKhac) && m.Mode.Equals((int)EMode.NgoaiGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (exist != null)
                            {
                                var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, exist.Id);
                                var update = Builders<FactoryProductCongTheoThang>.Update
                                    .Set(m => m.ObjectPrice, objectPrice)
                                    .Set(m => m.Value, objectValue)
                                    .Set(m => m.Amount, objectAmount);
                                dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                            }
                            else
                            {
                                dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                                {
                                    Month = month,
                                    Year = year,
                                    Type = (int)EDinhMuc.CongViecKhac,
                                    Mode = (int)EMode.NgoaiGio,
                                    EmployeeId = employee.Id,
                                    EmployeeCode = employee.CodeOld,
                                    EmployeeName = employee.FullName,
                                    EmployeeAlias = employee.AliasFullName,
                                    ObjectId = congvieckhac.Id,
                                    ObjectCode = congvieckhac.Code,
                                    ObjectName = congvieckhac.Name,
                                    ObjectAlias = congvieckhac.Alias,
                                    ObjectSort = congvieckhac.Sort,
                                    ObjectPrice = objectPrice,
                                    Value = objectValue,
                                    Amount = objectAmount
                                });
                            }
                            columnIndex++;
                        }
                        #endregion

                        // Round here if true.
                        thanhtienngoaigio = thanhtienthanhpham + thanhtienbochang + Convert.ToDecimal((double)thanhtientongcongcongvieckhac / 60);

                        #region Update to CONG TONG
                        var existCongTong = dbContext.EmployeeCongs.Find(m => m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                        if (existCongTong != null)
                        {
                            var filterCongTong = Builders<EmployeeCong>.Filter.Eq(m => m.Id, existCongTong.Id);
                            var updateCongTong = Builders<EmployeeCong>.Update
                                .Set(m => m.TongThoiGianCongViecKhacNgoaiGio, tongthoigiancongvieckhac / 60)
                                .Set(m => m.TongCongCongViecKhacNgoaiGio, thanhtientongcongcongvieckhac)
                                .Set(m => m.ThanhTienNgoaiGio, thanhtienngoaigio)
                                .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                            dbContext.EmployeeCongs.UpdateOne(filterCongTong, updateCongTong);
                        }
                        else
                        {
                            dbContext.EmployeeCongs.InsertOne(new EmployeeCong()
                            {
                                Month = month,
                                Year = year,
                                EmployeeId = employee.Id,
                                EmployeeCode = employee.CodeOld,
                                EmployeeName = employee.FullName,
                                TongThoiGianCongViecKhacNgoaiGio = tongthoigiancongvieckhac / 60,
                                TongCongCongViecKhacNgoaiGio = thanhtientongcongcongvieckhac,
                                ThanhTienNgoaiGio = thanhtienngoaigio
                            });
                        }
                        #endregion
                    }

                    return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Production + "/" + Constants.LinkSalary.TongHopNgoaiGio + "?thang=" + month + "-" + year });
                }
            }
            else
            {
                return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Production + "/" + Constants.LinkSalary.TongHopNgoaiGio });
            }
        }
        #endregion

        #region DONG GOI
        [Route(Constants.LinkSalary.DongGoiTrongGio)]
        public IActionResult DongGoiTrongGio(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.DongGoiNgoaiGio)]
        public IActionResult DongGoiNgoaiGio(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }
        #endregion

        #region BOC HANG
        [Route(Constants.LinkSalary.BocHangTrongGio)]
        public IActionResult BocHangTrongGio(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.BocHangNgoaiGio)]
        public IActionResult BocHangNgoaiGio(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }
        #endregion

        #region DINH MUC
        /// <summary>
        /// FIX DATA
        /// </summary>
        /// <param name="thang"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route(Constants.LinkSalary.DinhMuc)]
        public async Task<IActionResult> DinhMuc(string thang, string id)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongSX, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL
            var productsDdl = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToListAsync();
            #endregion

            #region Times
            var toDate = Utility.GetToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            #endregion

            #region Filter
            var builder = Builders<FactoryProductDinhMuc>.Filter;
            var filter = builder.Eq(m => m.Month, month) & builder.Eq(m => m.Year, year);
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.ProductCode, id.Trim());
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<FactoryProductDinhMuc>.Sort.Ascending(m => m.Sort);
            #endregion

            var donGiaDinhMucs = await dbContext.FactoryProductDinhMucs.Find(filter).ToListAsync();
            var donGiaDinhMucFulls = new List<FactoryProductDinhMucViewModel>();
            foreach (var dongia in donGiaDinhMucs)
            {
                var product = dbContext.FactoryProducts.Find(m => m.Id.Equals(dongia.ProductId)).FirstOrDefault();
                if (product != null)
                {
                    donGiaDinhMucFulls.Add(new FactoryProductDinhMucViewModel()
                    {
                        Id = dongia.Id,
                        Month = dongia.Month,
                        Year = dongia.Year,
                        Type = dongia.Type,
                        ProductId = dongia.ProductId,
                        ProductCode = dongia.ProductCode,
                        ProductName = product.Name,
                        ProductGroup = product.Group,
                        ProductUnit = product.Unit,
                        Sort = dongia.Sort,
                        SoBaoNhomNgay = dongia.SoBaoNhomNgay,
                        DinhMucTheoNgay = dongia.DinhMucTheoNgay,
                        DinhMucGioQuiDinh = dongia.DinhMucGioQuiDinh,
                        DinhMucTheoGio = dongia.DinhMucTheoGio,
                        DonGia = dongia.DonGia,
                        DonGiaDieuChinh = dongia.DonGiaDieuChinh,
                        DonGiaTangCaPhanTram = dongia.DonGiaTangCaPhanTram,
                        DonGiaTangCa = dongia.DonGiaTangCa,
                        DonGiaM3 = dongia.DonGiaM3,
                        DonGiaTangCaM3 = dongia.DonGiaTangCaM3
                    });
                }
            }
            var tiLeDinhMucs = await dbContext.FactoryProductDinhMucTiLes.Find(m => m.Month.Equals(month) && m.Year.Equals(year)).SortBy(m => m.TiLe).ToListAsync();
            var viewModel = new BangLuongViewModel
            {
                DonGiaDMFulls = donGiaDinhMucFulls,
                TiLeDMs = tiLeDinhMucs,
                MonthYears = Utility.DllMonths(),
                Thang = thang,
                ThanhPhams = productsDdl,
                Id = id
            };

            return View(viewModel);
        }
        #endregion

        #region IMPORT DATA, WAIT ORDER
        //[Route(Constants.LinkSalary.CongTong)]
        //public async Task<IActionResult> CongTong(string thang)
        //{
        //    #region Authorization
        //    var login = User.Identity.Name;
        //    var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
        //    ViewData["LoginUserName"] = loginUserName;

        //    var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
        //    if (loginInformation == null)
        //    {
        //        #region snippet1
        //        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        //        #endregion
        //        return RedirectToAction("login", "account");
        //    }

        //    if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongSX, (int)ERights.View)))
        //    {
        //        return RedirectToAction("AccessDenied", "Account");
        //    }


        //    #endregion

        //    #region DDL
        //    var sortTimes = Utility.DllMonths();
        //    #endregion

        //    #region Times
        //    var toDate = Utility.WorkingMonthToDate(thang);
        //    var fromDate = toDate.AddMonths(-1).AddDays(1);
        //    if (string.IsNullOrEmpty(thang))
        //    {
        //        toDate = DateTime.Now;
        //        fromDate = toDate.Day > 25 ? new DateTime(toDate.Year, toDate.Month, 26) : new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
        //    }
        //    var year = toDate.Day > 25 ? toDate.AddMonths(1).Year : toDate.Year;
        //    var month = toDate.Day > 25 ? toDate.AddMonths(1).Month : toDate.Month;
        //    thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;
        //    #endregion


        //    var salarySanXuatCongs = GetEmployeeCongs(month, year);

        //    var viewModel = new BangLuongViewModel
        //    {
        //        EmployeeCongs = salarySanXuatCongs,
        //        MonthYears = sortTimes,
        //        thang = thang
        //    };

        //    return View(viewModel);
        //}

        //[Route(Constants.LinkSalary.CongTong + "/" + Constants.ActionLink.Update)]
        //public async Task<IActionResult> CongTongUpdate(string thang)
        //{
        //    #region Authorization
        //    var login = User.Identity.Name;
        //    var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
        //    ViewData["LoginUserName"] = loginUserName;

        //    var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
        //    if (loginInformation == null)
        //    {
        //        #region snippet1
        //        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        //        #endregion
        //        return RedirectToAction("login", "account");
        //    }

        //    if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongSX, (int)ERights.View)))
        //    {
        //        return RedirectToAction("AccessDenied", "Account");
        //    }


        //    #endregion

        //    #region DDL

        //    var sortTimes = Utility.DllMonths();
        //    #endregion

        //    #region Times
        //    var toDate = Utility.WorkingMonthToDate(thang);
        //    var fromDate = toDate.AddMonths(-1).AddDays(1);
        //    if (string.IsNullOrEmpty(thang))
        //    {
        //        toDate = DateTime.Now;
        //        fromDate = toDate.Day > 25 ? new DateTime(toDate.Year, toDate.Month, 26) : new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
        //    }
        //    var year = toDate.Day > 25 ? toDate.AddMonths(1).Year : toDate.Year;
        //    var month = toDate.Day > 25 ? toDate.AddMonths(1).Month : toDate.Month;
        //    thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;
        //    #endregion


        //    var salarySanXuatCongs = GetEmployeeCongs(month, year);

        //    var viewModel = new BangLuongViewModel
        //    {
        //        EmployeeCongs = salarySanXuatCongs,
        //        MonthYears = sortTimes,
        //        thang = thang
        //    };

        //    return View(viewModel);
        //}

        //[HttpPost]
        //[Route(Constants.LinkSalary.CongTong + "/" + Constants.ActionLink.Update)]
        //public async Task<IActionResult> CongTongUpdate(BangLuongViewModel viewModel)
        //{
        //    #region Authorization
        //    var login = User.Identity.Name;
        //    var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
        //    ViewData["LoginUserName"] = loginUserName;

        //    var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
        //    if (loginInformation == null)
        //    {
        //        #region snippet1
        //        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        //        #endregion
        //        return RedirectToAction("login", "account");
        //    }

        //    if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
        //    {
        //        return RedirectToAction("AccessDenied", "Account");
        //    }


        //    #endregion

        //    foreach (var item in viewModel.EmployeeCongs)
        //    {
        //        var builder = Builders<EmployeeCong>.Filter;
        //        var filter = builder.Eq(m => m.Id, item.Id);
        //        var update = Builders<EmployeeCong>.Update
        //            .Set(m => m.CongTong, item.CongTong)
        //            .Set(m => m.Com, item.Com)
        //            .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        //        dbContext.EmployeeCongs.UpdateOne(filter, update);
        //    }

        //    return Json(new { result = true, source = "update", message = "Thành công" });
        //}

        //[Route(Constants.LinkSalary.SanXuatTemplate)]
        //public async Task<IActionResult> SanXuatTemplate(string fileName)
        //{
        //    string exportFolder = Path.Combine(_env.WebRootPath, "exports");
        //    string sFileName = @"du-lieu-san-xuat-thang-" + DateTime.Now.Month + "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
        //    string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
        //    FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
        //    var memory = new MemoryStream();
        //    using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
        //    {
        //        IWorkbook workbook = new XSSFWorkbook();
        //        #region Styling
        //        var cellStyleBorder = workbook.CreateCellStyle();
        //        cellStyleBorder.BorderBottom = BorderStyle.Thin;
        //        cellStyleBorder.BorderLeft = BorderStyle.Thin;
        //        cellStyleBorder.BorderRight = BorderStyle.Thin;
        //        cellStyleBorder.BorderTop = BorderStyle.Thin;
        //        cellStyleBorder.Alignment = HorizontalAlignment.Center;
        //        cellStyleBorder.VerticalAlignment = VerticalAlignment.Center;

        //        var cellStyleHeader = workbook.CreateCellStyle();
        //        //cellStyleHeader.CloneStyleFrom(cellStyleBorder);
        //        //cellStyleHeader.FillForegroundColor = HSSFColor.Blue.Index2;
        //        cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
        //        cellStyleHeader.FillPattern = FillPattern.SolidForeground;

        //        var font = workbook.CreateFont();
        //        font.FontHeightInPoints = 11;
        //        //font.FontName = "Calibri";
        //        font.Boldweight = (short)FontBoldWeight.Bold;
        //        #endregion

        //        ISheet sheet1 = workbook.CreateSheet("NMT" + DateTime.Now.Month.ToString("00"));
        //        //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 3, 7));
        //        //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 8, 13));

        //        var rowIndex = 0;
        //        IRow row = sheet1.CreateRow(rowIndex);
        //        row.CreateCell(0, CellType.String).SetCellValue("SỐ LIỆU SẢN XUẤT");
        //        rowIndex++;

        //        row = sheet1.CreateRow(rowIndex);
        //        row.CreateCell(0, CellType.String).SetCellValue("Tháng");
        //        row.CreateCell(1, CellType.Numeric).SetCellValue(DateTime.Now.Month);
        //        row.CreateCell(2, CellType.String).SetCellValue("Năm");
        //        row.CreateCell(3, CellType.Numeric).SetCellValue(DateTime.Now.Year);
        //        // Set style
        //        for (int i = 0; i <= 3; i++)
        //        {
        //            row.Cells[i].CellStyle = cellStyleHeader;
        //        }
        //        row.Cells[1].CellStyle.SetFont(font);
        //        row.Cells[3].CellStyle.SetFont(font);
        //        rowIndex++;
        //        //https://stackoverflow.com/questions/51681846/rowspan-and-colspan-in-apache-poi

        //        row = sheet1.CreateRow(rowIndex);
        //        row.CreateCell(0, CellType.String).SetCellValue("#");
        //        row.CreateCell(1, CellType.String).SetCellValue("Mã nhân viên");
        //        row.CreateCell(2, CellType.String).SetCellValue("Họ tên");
        //        row.CreateCell(3, CellType.String).SetCellValue("Chức vụ");
        //        row.CreateCell(4, CellType.String).SetCellValue("Công tổng");
        //        row.CreateCell(5, CellType.String).SetCellValue("Cơm SX");
        //        row.CreateCell(6, CellType.String).SetCellValue("Cơm KD");
        //        row.CreateCell(7, CellType.String).SetCellValue("Giờ tăng ca");
        //        row.CreateCell(8, CellType.String).SetCellValue("Giờ làm việc CN");
        //        row.CreateCell(9, CellType.String).SetCellValue("Giờ làm việc Lễ/Tết");
        //        // Set style
        //        for (int i = 0; i <= 9; i++)
        //        {
        //            row.Cells[i].CellStyle = cellStyleHeader;
        //        }


        //        for (int i = 1; i <= 50; i++)
        //        {
        //            row = sheet1.CreateRow(rowIndex + 1);
        //            //row.CreateCell(0, CellType.Numeric).SetCellValue(i);
        //            row.CreateCell(1, CellType.String).SetCellValue(string.Empty);
        //            row.CreateCell(2, CellType.String).SetCellValue(string.Empty);
        //            row.CreateCell(3, CellType.String).SetCellValue(string.Empty);
        //            row.CreateCell(4, CellType.Numeric).SetCellValue(string.Empty);
        //            row.CreateCell(5, CellType.Numeric).SetCellValue(string.Empty);
        //            row.CreateCell(6, CellType.Numeric).SetCellValue(string.Empty);
        //            row.CreateCell(7, CellType.Numeric).SetCellValue(string.Empty);
        //            row.CreateCell(8, CellType.Numeric).SetCellValue(string.Empty);
        //            row.CreateCell(9, CellType.Numeric).SetCellValue(string.Empty);
        //            rowIndex++;
        //        }

        //        workbook.Write(fs);
        //    }
        //    using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
        //    {
        //        await stream.CopyToAsync(memory);
        //    }
        //    memory.Position = 0;
        //    return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        //}

        //[Route(Constants.LinkSalary.SanXuatImport)]
        //[HttpPost]
        //public ActionResult SanXuatImport()
        //{
        //    IFormFile file = Request.Form.Files[0];
        //    string folderName = Constants.Storage.Hr;
        //    string webRootPath = _env.WebRootPath;
        //    string newPath = Path.Combine(webRootPath, folderName);
        //    if (!Directory.Exists(newPath))
        //    {
        //        Directory.CreateDirectory(newPath);
        //    }
        //    if (file.Length > 0)
        //    {
        //        string sFileExtension = Path.GetExtension(file.FileName).ToLower();
        //        ISheet sheet;
        //        string fullPath = Path.Combine(newPath, file.FileName);
        //        using (var stream = new FileStream(fullPath, FileMode.Create))
        //        {
        //            file.CopyTo(stream);
        //            stream.Position = 0;
        //            if (sFileExtension == ".xls")
        //            {
        //                HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
        //                sheet = hssfwb.GetSheetAt(0);
        //            }
        //            else
        //            {
        //                XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
        //                sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
        //            }

        //            var timeRow = sheet.GetRow(1);
        //            var month = Convert.ToInt32(Utility.GetNumbericCellValue(timeRow.GetCell(1)));
        //            var year = Convert.ToInt32(Utility.GetNumbericCellValue(timeRow.GetCell(3)));
        //            if (month == 0)
        //            {
        //                month = DateTime.Now.Month;
        //            }
        //            if (year == 0)
        //            {
        //                year = DateTime.Now.Year;
        //            }

        //            for (int i = 3; i <= sheet.LastRowNum; i++)
        //            {
        //                IRow row = sheet.GetRow(i);
        //                if (row == null) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

        //                var code = Utility.GetFormattedCellValue(row.GetCell(1));
        //                var fullName = Utility.GetFormattedCellValue(row.GetCell(2));
        //                var alias = Utility.AliasConvert(fullName);
        //                var title = Utility.GetFormattedCellValue(row.GetCell(3));

        //                var congtong = (decimal)Utility.GetNumbericCellValue(row.GetCell(4));
        //                var com = (decimal)Utility.GetNumbericCellValue(row.GetCell(5));

        //                var employee = new Employee();
        //                if (!string.IsNullOrEmpty(alias))
        //                {
        //                    employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.AliasFullName.Equals(alias)).FirstOrDefault();
        //                }
        //                if (employee == null)
        //                {
        //                    if (!string.IsNullOrEmpty(fullName))
        //                    {
        //                        employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.FullName.Equals(fullName)).FirstOrDefault();
        //                    }
        //                }
        //                if (employee == null)
        //                {
        //                    if (!string.IsNullOrEmpty(code))
        //                    {
        //                        employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.CodeOld.Equals(code)).FirstOrDefault();
        //                    }
        //                }
        //                if (employee != null)
        //                {
        //                    // check exist to update
        //                    var existEntity = dbContext.SalaryNhaMayCongs.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
        //                    if (existEntity != null)
        //                    {
        //                        var builder = Builders<EmployeeCong>.Filter;
        //                        var filter = builder.Eq(m => m.Id, existEntity.Id);
        //                        var update = Builders<EmployeeCong>.Update
        //                            .Set(m => m.EmployeeCode, employee.Code)
        //                            .Set(m => m.EmployeeName, employee.FullName)
        //                            .Set(m => m.EmployeeChucVu, employee.Title)
        //                            .Set(m => m.CongTong, congtong)
        //                            .Set(m => m.Com, com)
        //                            .Set(m => m.GioTangCa, giotangca)
        //                            .Set(m => m.GioLamViecCN, giolamvieccn)
        //                            .Set(m => m.GioLamViecLeTet, giolamviecletet)
        //                            .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

        //                        dbContext.EmployeeCongs.UpdateOne(filter, update);
        //                    }
        //                    else
        //                    {
        //                        var newItem = new EmployeeCong
        //                        {
        //                            Year = year,
        //                            Month = month,
        //                            EmployeeId = employee.Id,
        //                            EmployeeCode = employee.Code,
        //                            EmployeeName = employee.FullName,
        //                            EmployeeChucVu = employee.Title,
        //                            CongTong = congtong,
        //                            ComKD = comkd,
        //                            ComSX = comsx,
        //                            GioTangCa = giotangca,
        //                            GioLamViecCN = giolamvieccn,
        //                            GioLamViecLeTet = giolamviecletet
        //                        };
        //                        dbContext.EmployeeCongs.InsertOne(newItem);
        //                    }
        //                }
        //                else
        //                {
        //                    dbContext.Misss.InsertOne(new Miss
        //                    {
        //                        Type = "san-xuat-import",
        //                        Object = "time: " + month + "-" + year + ", code: " + code + "-" + fullName + "-" + title + " ,dòng " + i,
        //                        Error = "No import data",
        //                        DateTime = DateTime.Now.ToString()
        //                    });
        //                }
        //            }
        //        }
        //    }
        //    return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Production + "/" + Constants.LinkSalary.CongTong });
        //}

        //private List<EmployeeCong> GetEmployeeCongs(int thang, int nam)
        //{
        //    var results = dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.Month.Equals(thang) && m.Year.Equals(nam)).ToList();
        //    return results;
        //}
        #endregion
    }
}
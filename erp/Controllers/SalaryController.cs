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

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.LinkSalary.Main)]
    public class SalaryController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalaryController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalaryController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        // The luong cua nhan vien
        [Route(Constants.LinkSalary.Index)]
        public IActionResult Index()
        {
            var loginId = User.Identity.Name;
            // get information user
            var employee = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            var viewModel = new SalaryViewModel
            {
                Employee = employee
            };
            return View(viewModel);
        }

        // Current month: base employee. Because if new employee will apply.
        //      Save data () each month by Hr salary people.
        //          Save dynamic information.
        // If previous month. use data in collection "SalaryEmployeeMonths"
        [Route(Constants.LinkSalary.BangLuongReal)]
        public async Task<IActionResult> BangLuongReal(string thang)
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

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 08, 01);
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Year;
            var month = toDate.Month;

            #region Save or no
            //var isSave = string.IsNullOrEmpty(thang) ? true : false;
            //if (!isSave)
            //{
            //    // Lương tháng trước xem. Khong cap nhat, luu.
            //    // ?? 26/9 - 04/10 làm lương tháng 9.
            //    // xác đinh??
            //    // Lương tháng hiện tại lưu, chỉnh sửa.
            //    var now = DateTime.Now;
            //    var times = now.Month + "-" + now.Year;
            //    if (now.Day > 25)
            //    {
            //        var nextMonth = now.AddMonths(1);
            //        times = nextMonth.Month + "-" + nextMonth.Year;
            //    }
            //    var isThisMonth = thang == times ? true : false;
            //}
            #endregion

            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true) & !m.UserName.Equals(Constants.System.account)).ToListAsync();
            var salaryEmployeeMonths = new List<SalaryEmployeeMonth>();
            foreach (var employee in employees)
            {
                var salary = GetSalaryEmployeeMonth(thang, employee.Id, null, true);
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
                SalaryMucLuongVung = mucluongvung,
                SalaryEmployeeMonths = salaryEmployeeMonths,
                MonthYears = sortTimes,
                thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.BangLuongReal + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> BangLuongRealUpdate(string thang)
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

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 08, 01);
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Year;
            var month = toDate.Month;

            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true) & !m.UserName.Equals(Constants.System.account)).ToListAsync();
            var salaryEmployeeMonths = new List<SalaryEmployeeMonth>();
            foreach (var employee in employees)
            {
                var salary = GetSalaryEmployeeMonth(thang, employee.Id, null, true);
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
                SalaryMucLuongVung = mucluongvung,
                SalaryEmployeeMonths = salaryEmployeeMonths,
                MonthYears = sortTimes,
                thang = thang
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.BangLuongReal + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> BangLuongRealUpdate(BangLuongViewModel viewModel)
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
                var now = DateTime.Now;
                var thang = viewModel.thang;
                var toDate = Utility.WorkingMonthToDate(thang);
                var fromDate = toDate.AddMonths(-1).AddDays(1);
                if (string.IsNullOrEmpty(thang))
                {
                    toDate = DateTime.Now;
                    fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
                }
                var year = toDate.Year;
                var month = toDate.Month;

                var models = viewModel.SalaryEmployeeMonths;
                foreach (var item in models)
                {
                    var builder = Builders<SalaryEmployeeMonth>.Filter;
                    var filter = builder.Eq(m => m.Id, item.Id);
                    var update = Builders<SalaryEmployeeMonth>.Update
                        .Set(m => m.NangNhocDocHai, item.NangNhocDocHai * 1000)
                        .Set(m => m.TrachNhiem, item.TrachNhiem * 1000)
                        .Set(m => m.ThuHut, item.ThuHut * 1000)
                        .Set(m => m.Xang, item.Xang * 1000)
                        .Set(m => m.DienThoai, item.DienThoai * 1000)
                        .Set(m => m.Com, item.Com * 1000)
                        .Set(m => m.NhaO, item.NhaO * 1000)
                        .Set(m => m.KiemNhiem, item.KiemNhiem * 1000)
                        .Set(m => m.BhytDacBiet, item.BhytDacBiet * 1000)
                        .Set(m => m.ViTriCanKnNhieuNam, item.ViTriCanKnNhieuNam * 1000)
                        .Set(m => m.ViTriDacThu, item.ViTriDacThu * 1000)
                        .Set(m => m.LuongCoBanBaoGomPhuCap, item.LuongCoBanBaoGomPhuCap * 1000)
                        .Set(m => m.NgayCongLamViec, item.NgayCongLamViec * 1000)
                        .Set(m => m.NgayNghiPhepHuongLuong, item.NgayNghiPhepHuongLuong * 1000)
                        .Set(m => m.NgayNghiLeTetHuongLuong, item.NgayNghiLeTetHuongLuong * 1000)
                        .Set(m => m.CongCNGio, item.CongCNGio * 1000)
                        .Set(m => m.CongTangCaNgayThuongGio, item.CongTangCaNgayThuongGio * 1000)
                        .Set(m => m.CongLeTet, item.CongLeTet * 1000)
                        .Set(m => m.CongTacXa, item.CongTacXa * 1000)
                        .Set(m => m.MucDatTrongThang, item.MucDatTrongThang * 1000)
                        .Set(m => m.LuongTheoDoanhThuDoanhSo, item.LuongTheoDoanhThuDoanhSo * 1000)
                        .Set(m => m.TongBunBoc, item.TongBunBoc * 1000)
                        .Set(m => m.ThanhTienBunBoc, item.ThanhTienBunBoc * 1000)
                        .Set(m => m.LuongKhac, item.LuongKhac * 1000)
                        .Set(m => m.ThiDua, item.ThiDua * 1000)
                        .Set(m => m.HoTroNgoaiLuong, item.HoTroNgoaiLuong * 1000)
                        .Set(m => m.TongThuNhap, item.TongThuNhap * 1000)
                        .Set(m => m.TongThuNhapMinute, item.TongThuNhapMinute * 1000)
                        .Set(m => m.BHXHBHYT, item.BHXHBHYT * 1000)
                        .Set(m => m.LuongThamGiaBHXH, item.LuongThamGiaBHXH * 1000)
                        .Set(m => m.TamUng, item.TamUng * 1000)
                        .Set(m => m.ThuongLeTet, item.ThuongLeTet * 1000)
                        .Set(m => m.ThucLanh, item.ThucLanh * 1000)
                        .Set(m => m.ThucLanhMinute, item.ThucLanhMinute * 1000)
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

        [Route(Constants.LinkSalary.TheLuong)]
        public async Task<IActionResult> TheLuong(string thang)
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

            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = await dbContext.SalaryEmployeeMonths.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true)).ToListAsync(),
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                SalaryThangBangLuongReals = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Law.Equals(false)).ToListAsync(),
                SalaryThangBangPhuCapPhucLoisReal = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false)).ToListAsync()
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.ThangBangLuongReal)]
        public async Task<IActionResult> ThangBangLuongReal(string thang)
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

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 08, 01);
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Year;
            var month = toDate.Month;

            var mucluongvungs = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).FirstOrDefaultAsync();
            if (mucluongvungs == null)
            {
                var lastItemVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonthVung = lastItemVung.Month;
                var lastYearVung = lastItemVung.Year;

                lastItemVung.Id = null;
                lastItemVung.Month = month;
                lastItemVung.Year = year;
                dbContext.SalaryMucLuongVungs.InsertOne(lastItemVung);
                mucluongvungs = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).FirstOrDefaultAsync();
            }

            var thangbangluongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).ToListAsync();
            if (thangbangluongs.Count == 0)
            {
                var lastItem = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true)).SortByDescending(m=>m.Year).SortByDescending(m=>m.Month).FirstOrDefaultAsync();
                var lastMonth = lastItem.Month;
                var lastYear = lastItem.Year;
                var lastestThangBangLuongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Month.Equals(lastMonth) & m.Year.Equals(lastYear)).ToListAsync();
                foreach(var item in lastestThangBangLuongs)
                {
                    item.Id = null;
                    item.Month = month;
                    item.Year = year;
                    dbContext.SalaryThangBangLuongs.InsertOne(item);
                }
                thangbangluongs = lastestThangBangLuongs;
            }

            var pcpls = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).ToListAsync();
            if (pcpls.Count == 0)
            {
                var lastItemPC = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonthPC = lastItemPC.Month;
                var lastYearPC = lastItemPC.Year;
                var lastestPcplss = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Month.Equals(lastMonthPC) & m.Year.Equals(lastYearPC)).ToListAsync();
                foreach (var item in lastestPcplss)
                {
                    item.Id = null;
                    item.Month = month;
                    item.Year = year;
                    dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(item);
                }
                pcpls = lastestPcplss;
            }

            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = mucluongvungs,
                SalaryThangBangLuongs = thangbangluongs,
                SalaryThangBangPhuCapPhucLoisReal = pcpls,
                thang = thang,
                MonthYears = sortTimes
            };
            return View(viewModel);
        }

        [Route(Constants.LinkSalary.ThangBangLuongReal + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> ThangBangLuongRealUpdate(string thang)
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

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 08, 01);
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Year;
            var month = toDate.Month;
            thang = string.IsNullOrEmpty(thang) ? month +"-" + year : thang;

            var mucluongvungs = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).FirstOrDefaultAsync();
            if (mucluongvungs == null)
            {
                var lastItemVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonthVung = lastItemVung.Month;
                var lastYearVung = lastItemVung.Year;

                lastItemVung.Id = null;
                lastItemVung.Month = month;
                lastItemVung.Year = year;
                dbContext.SalaryMucLuongVungs.InsertOne(lastItemVung);
                mucluongvungs = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).FirstOrDefaultAsync(); ;
            }

            var thangbangluongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).ToListAsync();
            if (thangbangluongs.Count == 0)
            {
                var lastItem = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonth = lastItem.Month;
                var lastYear = lastItem.Year;
                var lastestThangBangLuongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Month.Equals(lastMonth) & m.Year.Equals(lastYear)).ToListAsync();
                foreach (var item in lastestThangBangLuongs)
                {
                    item.Id = null;
                    item.Month = month;
                    item.Year = year;
                    dbContext.SalaryThangBangLuongs.InsertOne(item);
                }
                thangbangluongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).ToListAsync();
            }

            var pcpls = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).ToListAsync();
            if (pcpls.Count == 0)
            {
                var lastItemPC = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonthPC = lastItemPC.Month;
                var lastYearPC = lastItemPC.Year;
                var lastestPcplss = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Month.Equals(lastMonthPC) & m.Year.Equals(lastYearPC)).ToListAsync();
                foreach (var item in lastestPcplss)
                {
                    item.Id = null;
                    item.Month = month;
                    item.Year = year;
                    dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(item);
                }
                pcpls = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).ToListAsync(); ;
            }

            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = mucluongvungs,
                SalaryThangBangLuongs = thangbangluongs,
                SalaryThangBangPhuCapPhucLoisReal = pcpls,
                thang = thang,
                MonthYears = sortTimes
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.ThangBangLuongReal + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> ThangBangLuongRealUpdate(ThangBangLuongViewModel viewModel)
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

            try
            {
                var now = DateTime.Now;

                var thang = viewModel.thang;
                var toDate = Utility.WorkingMonthToDate(thang);
                var fromDate = toDate.AddMonths(-1).AddDays(1);
                if (string.IsNullOrEmpty(thang))
                {
                    toDate = DateTime.Now;
                    fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
                }
                var year = toDate.Year;
                var month = toDate.Month;

                #region ToiThieuVung
                // Update by month later
                var salaryMucLuongVung = viewModel.SalaryMucLuongVung;
                var builderSalaryMucLuongVung = Builders<SalaryMucLuongVung>.Filter;
                var filterSalaryMucLuongVung = builderSalaryMucLuongVung.Eq(m => m.Id, salaryMucLuongVung.Id);
                var updateSalaryMucLuongVung = Builders<SalaryMucLuongVung>.Update
                    .Set(m => m.ToiThieuVungDoanhNghiepApDung, salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung *1000)
                    .Set(m => m.UpdatedOn, now);
                dbContext.SalaryMucLuongVungs.UpdateOne(filterSalaryMucLuongVung, updateSalaryMucLuongVung);
                #endregion

                #region SalaryThangBangLuongReal
                decimal salaryMin = salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung * 1000;
                var salaryThangBangLuongs = viewModel.SalaryThangBangLuongs;
                var groups = (from a in salaryThangBangLuongs
                              group a by new
                              {
                                  a.ViTriCode
                              }
                                                    into b
                              select new
                              {
                                  b.Key.ViTriCode,
                                  Salaries = b.ToList()
                              }).ToList();

                int maxLevel = 10;
                foreach (var group in groups)
                {
                    var id = group.Salaries[0].Id;
                    if (!string.IsNullOrEmpty(id))
                    {
                        var vitriCode = group.ViTriCode;
                        var vitri = group.Salaries[0].ViTri;
                        var vitriAlias = group.Salaries[0].ViTriAlias;
                        var salaryDeclareTax = group.Salaries[0].MucLuong * 1000;
                        var heso = group.Salaries[0].HeSo;
                        if (salaryDeclareTax == 0)
                        {
                            salaryDeclareTax = salaryMin;
                        }
                        for (int lv = 0; lv <= maxLevel; lv++)
                        {
                            if (lv > 1)
                            {
                                salaryDeclareTax = heso * salaryDeclareTax;
                            }
                            var exist = dbContext.SalaryThangBangLuongs.CountDocuments(m => m.ViTriCode.Equals(vitriCode) & m.Bac.Equals(lv) & m.FlagReal.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year));
                            if (exist > 0)
                            {
                                var builderSalaryThangBangLuong = Builders<SalaryThangBangLuong>.Filter;
                                var filterSalaryThangBangLuong = builderSalaryThangBangLuong.Eq(m => m.ViTriCode, vitriCode);
                                filterSalaryThangBangLuong = filterSalaryThangBangLuong & builderSalaryThangBangLuong.Eq(m => m.Month, month);
                                filterSalaryThangBangLuong = filterSalaryThangBangLuong & builderSalaryThangBangLuong.Eq(m => m.Year, year);
                                filterSalaryThangBangLuong = filterSalaryThangBangLuong & builderSalaryThangBangLuong.Eq(m => m.Bac, lv);
                                var updateSalaryThangBangLuong = Builders<SalaryThangBangLuong>.Update
                                    .Set(m => m.MucLuong, salaryDeclareTax)
                                    .Set(m => m.HeSo, heso)
                                    .Set(m => m.UpdatedOn, now);
                                dbContext.SalaryThangBangLuongs.UpdateOne(filterSalaryThangBangLuong, updateSalaryThangBangLuong);
                            }
                            else
                            {
                                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                                {
                                    Month = month,
                                    Year = year,
                                    ViTri = vitriCode,
                                    Bac = lv,
                                    HeSo = heso,
                                    MucLuong = salaryDeclareTax,
                                    ViTriCode = vitriCode,
                                    ViTriAlias = vitriAlias,
                                    Law = false,
                                    FlagReal = true
                                });
                            }
                        }
                    }
                    else
                    {
                        // Insert NEW VI TRI
                        var vitri = group.Salaries[0].ViTri;
                        if (!string.IsNullOrEmpty(vitri))
                        {
                            var vitriAlias = Utility.AliasConvert(group.Salaries[0].ViTri);
                            string vitriLastCode = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true)).SortByDescending(m => m.ViTriCode).FirstOrDefault().ViTriCode;
                            int newCode = Convert.ToInt32(vitriLastCode.Split('-')[1]) + 1;
                            string newCodeFull = Constants.System.viTriCodeTBLuong + newCode.ToString("000");
                            var salaryDeclareTax = group.Salaries[0].MucLuong * 1000;
                            var heso = group.Salaries[0].HeSo;

                            if (salaryDeclareTax == 0)
                            {
                                salaryDeclareTax = salaryMin;
                            }
                            for (int lv = 1; lv <= 10; lv++)
                            {
                                if (lv > 1)
                                {
                                    salaryDeclareTax = heso * salaryDeclareTax;
                                }
                                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                                {
                                    Month = month,
                                    Year = year,
                                    ViTri = vitri,
                                    Bac = lv,
                                    HeSo = heso,
                                    MucLuong = salaryDeclareTax,
                                    ViTriCode = newCodeFull,
                                    ViTriAlias = vitriAlias,
                                    Law = false,
                                    FlagReal = true
                                });
                            }
                        }
                    }
                }
                #endregion

                return Json(new { result = true, source = "update", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "update", id = string.Empty, message = ex.Message });
            }
        }

        // LAWS - BAO CAO THUE
        // AUTOMATIC DATA, BASE EMPLOYEES
        [Route(Constants.LinkSalary.BangLuongLaw)]
        public async Task<IActionResult> BangLuongLaw(string thang)
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

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 08, 01);
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            // override times if null
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Year;
            var month = toDate.Month;

            var mucluongvung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).FirstOrDefaultAsync();
            var salaryEmployeeMonths = await dbContext.SalaryEmployeeMonths.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year) & m.LuongCanBan > 0).ToListAsync();
            // Because phucap, phuc loi # thuc te
            // Override phucap-phucloi
            foreach (var item in salaryEmployeeMonths)
            {
                decimal luongCB = 0;
                decimal nangnhoc = 0;
                decimal trachnhiem = 0;
                decimal thamnien = 0;
                decimal thuhut = 0;
                decimal dienthoai = 0;
                decimal xang = 0;
                decimal com = 0;
                decimal nhao = 0;
                decimal kiemnhiem = 0;
                decimal bhytdacbiet = 0;
                decimal vitricanknnhieunam = 0;
                decimal vitridacthu = 0;
                decimal luongKhac = 0;
                decimal thiDua = 0;
                decimal hoTroNgoaiLuong = 0;
                decimal thuongletet = 0;
                decimal luongcbbaogomphucap = 0;
                decimal ngayNghiPhepHuongLuong = 0;
                decimal ngayNghiLeTetHuongLuong = 0;
                decimal congCNGio = 0;
                decimal phutcongCN = 0;
                decimal congTangCaNgayThuongGio = 0;
                decimal phutcongTangCaNgayThuong = 0;
                decimal congLeTet = 0;
                decimal phutcongLeTet = 0;
                decimal congTacXa = 0;
                decimal tongBunBoc = 0;
                decimal thanhTienBunBoc = 0;
                decimal mucDatTrongThang = 0;
                decimal luongTheoDoanhThuDoanhSo = 0;
                decimal mauSo = item.MauSo;
                decimal ngayConglamViec = Utility.BusinessDaysUntil(fromDate, toDate);
                decimal phutconglamviec = ngayConglamViec * 8 * 60;

                luongCB = item.LuongThamGiaBHXH;
                item.LuongCanBan = luongCB;
                var phucapphuclois = dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.EmployeeId.Equals(item.EmployeeId) & m.FlagReal.Equals(false)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).ToList();
                if (phucapphuclois.Find(m => m.Code.Equals("01-001")) != null)
                {
                    nangnhoc = phucapphuclois.Find(m => m.Code.Equals("01-001")).Money;
                    item.NangNhocDocHai = nangnhoc;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("01-002")) != null)
                {
                    trachnhiem = phucapphuclois.Find(m => m.Code.Equals("01-002")).Money;
                    item.TrachNhiem = trachnhiem;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("01-004")) != null)
                {
                    thuhut = phucapphuclois.Find(m => m.Code.Equals("01-004")).Money;
                    item.ThuHut = thuhut;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-001")) != null)
                {
                    xang = phucapphuclois.Find(m => m.Code.Equals("02-001")).Money;
                    item.Xang = xang;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-002")) != null)
                {
                    dienthoai = phucapphuclois.Find(m => m.Code.Equals("02-002")).Money;
                    item.DienThoai = dienthoai;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-003")) != null)
                {
                    com = phucapphuclois.Find(m => m.Code.Equals("02-003")).Money;
                    item.Com = com;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-004")) != null)
                {
                    kiemnhiem = phucapphuclois.Find(m => m.Code.Equals("02-004")).Money;
                    item.KiemNhiem = kiemnhiem;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-005")) != null)
                {
                    bhytdacbiet = phucapphuclois.Find(m => m.Code.Equals("02-005")).Money;
                    item.BhytDacBiet = bhytdacbiet;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-006")) != null)
                {
                    vitricanknnhieunam = phucapphuclois.Find(m => m.Code.Equals("02-006")).Money;
                    item.ViTriCanKnNhieuNam = vitricanknnhieunam;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-007")) != null)
                {
                    vitridacthu = phucapphuclois.Find(m => m.Code.Equals("02-007")).Money;
                    item.ViTriDacThu = vitridacthu;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-008")) != null)
                {
                    nhao = phucapphuclois.Find(m => m.Code.Equals("02-008")).Money;
                    item.NhaO = nhao;
                }
                if (item.ThamNien > 0)
                {
                    thamnien = luongCB * Convert.ToDecimal(0.03 + (item.ThamNienYear - 3) * 0.01); ;
                    item.ThamNien = thamnien;
                }

                luongcbbaogomphucap = luongCB + nangnhoc + trachnhiem + thamnien + thuhut + dienthoai + xang + com + nhao + kiemnhiem + bhytdacbiet + vitricanknnhieunam + vitridacthu;
                item.LuongCoBanBaoGomPhuCap = luongcbbaogomphucap;

                decimal tongthunhap = luongcbbaogomphucap / mauSo * (ngayConglamViec + congCNGio / 8 * 2 + congTangCaNgayThuongGio / 8 * (decimal)1.5 + congLeTet * 3)
                                    + luongCB / mauSo * (ngayNghiPhepHuongLuong + ngayNghiLeTetHuongLuong)
                                    + congTacXa + luongTheoDoanhThuDoanhSo + thanhTienBunBoc + luongKhac + thiDua + hoTroNgoaiLuong;
                item.TongThuNhap = tongthunhap;

                decimal thunhapbydate = luongcbbaogomphucap / mauSo;
                decimal thunhapbyminute = thunhapbydate / 8 / 60;
                decimal tongthunhapminute = thunhapbyminute * (phutconglamviec + (phutcongCN * 2) + (phutcongTangCaNgayThuong * (decimal)1.5) + (phutcongLeTet * 3))
                                    + luongCB / mauSo * (ngayNghiPhepHuongLuong + ngayNghiLeTetHuongLuong)
                                    + congTacXa + luongTheoDoanhThuDoanhSo + thanhTienBunBoc + luongKhac + thiDua + hoTroNgoaiLuong;
                item.TongThuNhapMinute = tongthunhapminute;
                decimal bhxhbhyt = 0;
                decimal tamung = item.TamUng;
                decimal thuclanh = tongthunhap - bhxhbhyt - tamung + thuongletet;
                item.ThucLanh = thuclanh;
                decimal thuclanhminute = tongthunhapminute - bhxhbhyt - tamung + thuongletet;
                item.ThucLanhMinute = thuclanhminute;

            }

            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = salaryEmployeeMonths,
                SalaryMucLuongVung = mucluongvung,
                MonthYears = sortTimes,
                thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.ThangBangLuongLaw)]
        public async Task<IActionResult> ThangBangLuongLaw()
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

            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                SalaryThangBangLuongLaws = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false) & m.Law.Equals(true)).ToListAsync(),
                SalaryThangBangPhuCapPhucLois = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false)).ToListAsync()
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.ThangBangLuongLaw + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> ThangBangLuongLawUpdate()
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

            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                SalaryThangBangLuongLaws = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false) & m.Law.Equals(true)).ToListAsync(),
                SalaryThangBangPhuCapPhucLois = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false)).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.ThangBangLuongLaw + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> ThangBangLuongLawUpdate(ThangBangLuongViewModel viewModel)
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

            try
            {
                var now = DateTime.Now;

                #region ToiThieuVung
                var salaryMucLuongVung = viewModel.SalaryMucLuongVung;
                var builderSalaryMucLuongVung = Builders<SalaryMucLuongVung>.Filter;
                var filterSalaryMucLuongVung = builderSalaryMucLuongVung.Eq(m => m.Id, salaryMucLuongVung.Id);
                var updateSalaryMucLuongVung = Builders<SalaryMucLuongVung>.Update
                    .Set(m => m.ToiThieuVungQuiDinh, salaryMucLuongVung.ToiThieuVungQuiDinh)
                    .Set(m => m.ToiThieuVungDoanhNghiepApDung, salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung)
                    .Set(m => m.UpdatedOn, now);
                dbContext.SalaryMucLuongVungs.UpdateOne(filterSalaryMucLuongVung, updateSalaryMucLuongVung);
                #endregion

                #region SalaryThangBangLuongLaws
                decimal salaryMin = salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung;
                var salaryThangBangLuongLaws = viewModel.SalaryThangBangLuongLaws;
                var groups = (from a in salaryThangBangLuongLaws
                              group a by new
                              {
                                  a.MaSo
                              }
                                                    into b
                              select new
                              {
                                  MaSoName = b.Key.MaSo,
                                  Salaries = b.ToList(),
                              }).ToList();

                foreach (var group in groups)
                {
                    // reset salaryDeclareTax;
                    var salaryDeclareTax = salaryMin;
                    if (group.Salaries[0].MucLuong > 0)
                    {
                        salaryDeclareTax = group.Salaries[0].MucLuong;
                    }
                    foreach (var level in group.Salaries)
                    {
                        // bac 1 set manual
                        if (level.Bac > 1)
                        {
                            salaryDeclareTax = level.HeSo * salaryDeclareTax;
                        }
                        var builderSalaryThangBangLuong = Builders<SalaryThangBangLuong>.Filter;
                        var filterSalaryThangBangLuong = builderSalaryThangBangLuong.Eq(m => m.Id, level.Id);
                        var updateSalaryThangBangLuong = Builders<SalaryThangBangLuong>.Update
                            .Set(m => m.MucLuong, salaryDeclareTax)
                            .Set(m => m.HeSo, level.HeSo)
                            .Set(m => m.UpdatedOn, now);
                        dbContext.SalaryThangBangLuongs.UpdateOne(filterSalaryThangBangLuong, updateSalaryThangBangLuong);
                    }
                }
                #endregion

                #region SalaryThangBangPhuCapPhucLois

                foreach (var phucap in viewModel.SalaryThangBangPhuCapPhucLois)
                {
                    // Update if id not null
                    if (!string.IsNullOrEmpty(phucap.Id))
                    {
                        var builderSalaryThangBangPhuCapPhucLoi = Builders<SalaryThangBangPhuCapPhucLoi>.Filter;
                        var filterSalaryThangBangPhuCapPhucLoi = builderSalaryThangBangPhuCapPhucLoi.Eq(m => m.Id, phucap.Id);
                        var updateSalaryThangBangPhuCapPhucLoi = Builders<SalaryThangBangPhuCapPhucLoi>.Update
                            .Set(m => m.Money, phucap.Money)
                            .Set(m => m.UpdatedOn, now);
                        dbContext.SalaryThangBangPhuCapPhucLois.UpdateOne(filterSalaryThangBangPhuCapPhucLoi, updateSalaryThangBangPhuCapPhucLoi);
                    }
                    else
                    {
                        var phucapInformation = dbContext.SalaryPhuCapPhucLois.Find(m => m.Code.Equals(phucap.Code)).FirstOrDefault();
                        if (phucapInformation != null)
                        {
                            phucap.Name = phucapInformation.Name;
                        }
                        dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(phucap);
                    }
                }
                #endregion

                // PROCCESSING

                #region Activities
                // Update multi, insert multi
                string s = JsonConvert.SerializeObject(viewModel.SalaryThangBangLuongLaws);
                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.SalaryThangBangLuong,
                    Action = Constants.Action.Create,
                    Value = s,
                    Content = Constants.Action.Create,
                };
                await dbContext.TrackingUsers.InsertOneAsync(activity);
                #endregion

                return Json(new { result = true, source = "create", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "create", id = string.Empty, message = ex.Message });
            }
        }

        #region NHA MAY

        #endregion

        #region SUB DATA (SALES, LOGISTICS,...)
        [Route(Constants.LinkSalary.Setting + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SettingUpdate()
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

            var viewModel = new BangLuongViewModel
            {
                SalarySettings = await dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.Setting + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SettingUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.SalarySettings)
            {
                var builder = Builders<SalarySetting>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<SalarySetting>.Update
                    .Set(m => m.Value, item.Value)
                    .Set(m => m.Description, item.Description)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalarySettings.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        [Route(Constants.LinkSalary.Credits + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> CreditUpdate()
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

            var credits = new List<SalaryCredit>();
            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true) & !m.UserName.Equals(Constants.System.account)).ToListAsync();
            foreach (var employee in employees)
            {
                decimal mucthanhtoanhangthang = 0;
                var credit = await dbContext.SalaryCredits.Find(m => m.EmployeeId.Equals(employee.Id)).FirstOrDefaultAsync();
                if (credit != null)
                {
                    mucthanhtoanhangthang = credit.MucThanhToanHangThang;
                    credits.Add(new SalaryCredit
                    {
                        Id = credit.Id,
                        EmployeeId = employee.Id,
                        MaNhanVien = employee.CodeOld,
                        FullName = employee.FullName,
                        ChucVu = employee.SalaryChucVu,
                        MucThanhToanHangThang = mucthanhtoanhangthang
                    });
                }
                else
                {
                    var creditItem = new SalaryCredit
                    {
                        EmployeeId = employee.Id,
                        MaNhanVien = employee.CodeOld,
                        FullName = employee.FullName,
                        ChucVu = employee.SalaryChucVu,
                        MucThanhToanHangThang = mucthanhtoanhangthang
                    };
                    credits.Add(creditItem);
                    dbContext.SalaryCredits.InsertOne(creditItem);
                }
            }

            var viewModel = new BangLuongViewModel
            {
                SalaryCredits = credits
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.Credits + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> CreditUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.SalaryCredits)
            {
                var builder = Builders<SalaryCredit>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<SalaryCredit>.Update
                    .Set(m => m.MucThanhToanHangThang, item.MucThanhToanHangThang * 1000)
                    .Set(m => m.Status, item.Status)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalaryCredits.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        [Route(Constants.LinkSalary.LogisticDatas + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> LogisticDataUpdate(string thang)
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

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 08, 01);
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            // If exist, update, no => create 1 month
            var dataTime = sortTimes[0];
            if (!string.IsNullOrEmpty(thang))
            {
                dataTime = new MonthYear
                {
                    Month = Convert.ToInt32(thang.Split("-")[0]),
                    Year = Convert.ToInt32(thang.Split("-")[1]),
                };
            }

            var logisticsData = new List<SalaryLogisticData>();
            var logisticsDataTemp = dbContext.SalaryLogisticDatas.Find(m => m.Year.Equals(dataTime.Year) & m.Month.Equals(dataTime.Month) & m.Enable.Equals(true)).ToList();
            if (logisticsDataTemp != null && logisticsDataTemp.Count > 0)
            {
                logisticsData = logisticsDataTemp;
            }
            else
            {
                var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true)
                & !m.UserName.Equals(Constants.System.account)
                & (m.CodeOld.Contains("KDG")
                    || m.CodeOld.Contains("KDPX")
                    || m.CodeOld.Contains("KDX")
                    || m.CodeOld.Contains("KDS"))
                ).ToListAsync();
                foreach (var employee in employees)
                {
                    try
                    {
                        logisticsData.Add(new SalaryLogisticData
                        {
                            Year = dataTime.Year,
                            Month = dataTime.Month,
                            EmployeeId = employee.Id,
                            MaNhanVien = employee.CodeOld,
                            FullName = employee.FullName,
                            ChucVu = employee.SalaryChucVu
                        });
                    }
                    catch (Exception ex)
                    {

                    }

                }
                dbContext.SalaryLogisticDatas.InsertMany(logisticsData);
            }

            var viewModel = new BangLuongViewModel
            {
                SalaryLogisticDatas = logisticsData
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.LogisticDatas + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> LogisticDataUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.SalaryLogisticDatas)
            {
                var builder = Builders<SalaryLogisticData>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<SalaryLogisticData>.Update
                    //
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalaryLogisticDatas.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        [Route(Constants.LinkSalary.SaleKPIs + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SaleKPIUpdate(string thang)
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

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 08, 01);
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            // If exist, update, no => create 1 month
            var dataTime = sortTimes[0];
            if (!string.IsNullOrEmpty(thang))
            {
                dataTime = new MonthYear
                {
                    Month = Convert.ToInt32(thang.Split("-")[0]),
                    Year = Convert.ToInt32(thang.Split("-")[1]),
                };
            }

            var sales = new List<SalarySaleKPI>();
            var salesTemp = dbContext.SalarySaleKPIs.Find(m => m.Year.Equals(dataTime.Year) & m.Month.Equals(dataTime.Month) & m.Enable.Equals(true)).ToList();
            if (salesTemp != null && salesTemp.Count > 0)
            {
                sales = salesTemp;
            }
            else
            {
                var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true)
                 & !m.UserName.Equals(Constants.System.account)
                 & (m.CodeOld.Contains("KDS")
                     || m.CodeOld.Contains("KDV"))
                 ).ToListAsync();
                foreach (var employee in employees)
                {
                    try
                    {
                        sales.Add(new SalarySaleKPI
                        {
                            Year = dataTime.Year,
                            Month = dataTime.Month,
                            EmployeeId = employee.Id,
                            MaNhanVien = employee.CodeOld,
                            FullName = employee.FullName,
                            ChucVu = employee.SalaryChucVu
                        });
                    }
                    catch (Exception ex) { }

                }
                dbContext.SalarySaleKPIs.InsertMany(sales);
            }

            var viewModel = new BangLuongViewModel
            {
                SalarySaleKPIs = sales
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.SaleKPIs + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SaleKPIUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.SalarySaleKPIs)
            {
                var builder = Builders<SalarySaleKPI>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<SalarySaleKPI>.Update
                    .Set(m => m.UpdatedOn, DateTime.Now)
                    .Set(m => m.UpdatedOn, DateTime.Now)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalarySaleKPIs.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        [Route(Constants.LinkSalary.KPIMonth)]
        public async Task<IActionResult> KPIMonth()
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

            // Get KPI lastest month
            var kpiLastest = await dbContext.SaleKPIs.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
            var lastMonth = kpiLastest.Month;
            var lastYear = kpiLastest.Year;

            var viewModel = new SalarySaleViewModel()
            {
                SaleKPIs = await dbContext.SaleKPIs.Find(m => m.Enable.Equals(true) & m.Year.Equals(lastYear) & m.Month.Equals(lastMonth)).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.KPIMonth)]
        public async Task<IActionResult> KPIMonth(SalarySaleViewModel viewModel)
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

            foreach (var item in viewModel.SaleKPIs)
            {
                if (!string.IsNullOrEmpty(item.Id))
                {
                    var builder = Builders<SaleKPI>.Filter;
                    var filter = builder.Eq(m => m.Id, item.Id);
                    var update = Builders<SaleKPI>.Update
                        .Set(m => m.Value, item.Value)
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    dbContext.SaleKPIs.UpdateOne(filter, update);
                }
                else
                {
                    // create new kpi
                }
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        #endregion

        #region Sub
        [Route(Constants.LinkSalary.LuongCalculator)]
        public IActionResult LuongCalculator(BangLuongViewModel viewModel)
        {
            var entity = viewModel.SalaryEmployeeMonths.First();
            string thang = entity.Month + "-" + entity.Year;
            var returnEntity = GetSalaryEmployeeMonth(thang, entity.EmployeeId, entity, true);

            return Json(new { entity = returnEntity });
        }

        public SalaryEmployeeMonth GetSalaryEmployeeMonth(string thang, string employeeId, SalaryEmployeeMonth newSalary, bool save)
        {
            var thamsoEntity = dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToList();
            var mauSo = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-lam-viec")).Value);
            var mauSoBaoVe = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-bao-ve")).Value);
            var tyledongbh = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ty-le-dong-bh")).Value);

            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);

            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }

            var year = toDate.Year;
            var month = toDate.Month;

            decimal ngayConglamViec = Utility.BusinessDaysUntil(fromDate, toDate);
            decimal phutconglamviec = ngayConglamViec * 8 * 60;

            var employee = dbContext.Employees.Find(m => m.Id.Equals(employeeId)).FirstOrDefault();
            var thamnienlamviec = employee.Joinday;
            var dateSpan = DateTimeSpan.CompareDates(thamnienlamviec, new DateTime(year, month, fromDate.Day));

            var currentSalary = new SalaryEmployeeMonth();
            var existSalary = dbContext.SalaryEmployeeMonths.CountDocuments(m => m.EmployeeId.Equals(employee.Id) & m.Year.Equals(year) & m.Month.Equals(month)) > 0 ? true : false;
            if (existSalary)
            {
                currentSalary = dbContext.SalaryEmployeeMonths.Find(m => m.EmployeeId.Equals(employee.Id) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefault();
            }
            else
            {
                currentSalary.Year = year;
                currentSalary.Month = month;
                currentSalary.EmployeeId = employee.Id;
                currentSalary.MaNhanVien = employee.CodeOld;
                currentSalary.FullName = employee.FullName;
                currentSalary.NoiLamViec = employee.SalaryNoiLamViec;
                currentSalary.NoiLamViecOrder = employee.SalaryNoiLamViecOrder;
                currentSalary.PhongBan = employee.SalaryPhongBan;
                currentSalary.PhongBanOrder = employee.SalaryPhongBanOrder;
                currentSalary.ChucVu = employee.SalaryChucVu;
                currentSalary.ChucVuOrder = employee.SalaryChucVuOrder;
                currentSalary.ViTriCode = employee.SalaryChucVuViTriCode;
                currentSalary.ThamNienLamViec = employee.Joinday;
                currentSalary.ThamNienYear = dateSpan.Years;
                currentSalary.ThamNienMonth = dateSpan.Months;
                currentSalary.ThamNienDay = dateSpan.Days;

                // Fill Data to [currentSalary]
                var phucapphuclois = dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.EmployeeId.Equals(employee.Id) & m.Law.Equals(false)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).ToList();
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
            }

            int bac = GetBacLuong(employee.Id);
            currentSalary.Bac = bac;
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

            //decimal luongCB = GetLuongCB(employee.Id, bac, month, year);
            decimal luongCB = GetLuongCB(employee.SalaryChucVuViTriCode, employee.Id, currentSalary.Bac, month, year);
            decimal nangnhoc = currentSalary.NangNhocDocHai;
            decimal trachnhiem = currentSalary.TrachNhiem;
            decimal thamnien = currentSalary.ThamNien;
            if (dateSpan.Years >= 3)
            {
                thamnien = luongCB * Convert.ToDecimal(0.03 + (dateSpan.Years - 3) * 0.01);
            }
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
            decimal ngayNghiPhepHuongLuong = 0;
            decimal ngayNghiLeTetHuongLuong = 0;
            decimal congCNGio = 0;
            decimal phutcongCN = 0;
            decimal congTangCaNgayThuongGio = 0;
            decimal phutcongTangCaNgayThuong = 0;
            decimal congLeTet = 0;
            decimal phutcongLeTet = 0;
            decimal congTacXa = 0;
            decimal tongBunBoc = 0;
            decimal thanhTienBunBoc = 0;
            decimal mucDatTrongThang = 0;
            decimal luongTheoDoanhThuDoanhSo = 0;

            var chamCong = dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(employee.Id) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefault();
            var logisticData = dbContext.SalaryLogisticDatas.Find(m => m.EmployeeId.Equals(employee.Id) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefault();
            var saleData = dbContext.SalarySaleKPIs.Find(m => m.EmployeeId.Equals(employee.Id) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefault();
            if (chamCong != null)
            {
                ngayConglamViec = (decimal)chamCong.Workday;
                phutconglamviec = (decimal)Math.Round(TimeSpan.FromMilliseconds(chamCong.WorkTime).TotalMinutes, 0);
                ngayNghiPhepHuongLuong = (decimal)chamCong.NgayNghiHuongLuong;
                ngayNghiLeTetHuongLuong = (decimal)chamCong.NgayNghiLeTetHuongLuong;
                congCNGio = (decimal)chamCong.CongCNGio;
                phutcongCN = congCNGio * 60;
                congTangCaNgayThuongGio = (decimal)chamCong.CongTangCaNgayThuongGio;
                phutcongTangCaNgayThuong = congTangCaNgayThuongGio * 60;
                congLeTet = (decimal)chamCong.CongLeTet;
                phutcongLeTet = congLeTet * 60;
            }
            if (logisticData != null)
            {
                congTacXa = logisticData.CongTacXa;
                tongBunBoc = logisticData.KhoiLuongBun;
                thanhTienBunBoc = logisticData.ThanhTienBun;
                mucDatTrongThang = logisticData.TongSoChuyen;
                luongTheoDoanhThuDoanhSo = logisticData.TienChuyen;
            }
            if (saleData != null)
            {
                luongTheoDoanhThuDoanhSo += saleData.TongThuong;
            }

            mauSo = employee.SalaryMauSo != 26 ? 30 : 26;

            decimal tongthunhap = luongcbbaogomphucap / mauSo * (ngayConglamViec + congCNGio / 8 * 2 + congTangCaNgayThuongGio / 8 * (decimal)1.5 + congLeTet * 3)
                                + luongCB / mauSo * (ngayNghiPhepHuongLuong + ngayNghiLeTetHuongLuong)
                                + congTacXa + luongTheoDoanhThuDoanhSo + thanhTienBunBoc + luongKhac + thiDua + hoTroNgoaiLuong;

            decimal thunhapbydate = luongcbbaogomphucap / mauSo;
            decimal thunhapbyminute = thunhapbydate / 8 / 60;
            decimal tongthunhapminute = thunhapbyminute * (phutconglamviec + (phutcongCN * 2) + (phutcongTangCaNgayThuong * (decimal)1.5) + (phutcongLeTet * 3))
                                + luongCB / mauSo * (ngayNghiPhepHuongLuong + ngayNghiLeTetHuongLuong)
                                + congTacXa + luongTheoDoanhThuDoanhSo + thanhTienBunBoc + luongKhac + thiDua + hoTroNgoaiLuong;

            decimal bhxhbhyt = luongthamgiabhxh * tyledongbh;

            decimal tamung = 0;
            var creditData = dbContext.SalaryCredits.Find(m => m.EmployeeId.Equals(employee.Id) & !m.Status.Equals(2)).FirstOrDefault();
            if (creditData != null)
            {
                tamung = creditData.MucThanhToanHangThang;
            }
            decimal thuclanh = tongthunhap - bhxhbhyt - tamung + thuongletet;
            decimal thuclanhminute = tongthunhapminute - bhxhbhyt - tamung + thuongletet;

            #region update field to currentSalary
            //currentSalary.Bac = bac;
            currentSalary.LuongCanBan = luongCB;
            currentSalary.ThamNien = thamnien;
            currentSalary.LuongCoBanBaoGomPhuCap = luongcbbaogomphucap;

            currentSalary.NgayCongLamViec = ngayConglamViec;
            currentSalary.PhutCongLamViec = phutconglamviec;
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

            currentSalary.TongThuNhap = tongthunhap;
            currentSalary.TongThuNhapMinute = tongthunhapminute;

            currentSalary.BHXHBHYT = bhxhbhyt;

            currentSalary.TamUng = tamung;

            currentSalary.ThucLanh = thuclanh;
            currentSalary.ThucLanhMinute = thuclanhminute;
            #endregion

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

        public int GetBacLuong(string employeeId)
        {
            var bac = 1;
            var newestSalary = dbContext.SalaryEmployeeMonths
                                .Find(m => m.EmployeeId.Equals(employeeId)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();

            if (newestSalary != null)
            {
                bac = newestSalary.Bac;
            }
            else
            {
                // Get Bac in SalaryThangBacLuongEmployees
                var newestSalaryThangBacLuongEmployee = dbContext.SalaryThangBacLuongEmployees
                            .Find(m => m.EmployeeId.Equals(employeeId)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
                if (newestSalary != null)
                {
                    bac = newestSalaryThangBacLuongEmployee.Bac;
                }
            }
            return bac;
        }

        public decimal GetLuongCB(string maViTri, string employeeId, int bac, int month, int year)
        {
            decimal luongCB = 0;

            // GET DIRECT [SalaryThangBangLuong] by bac vs mavitri
            if (!string.IsNullOrEmpty(maViTri))
            {
                var salaryThangBangLuong = dbContext.SalaryThangBangLuongs.Find(m => m.ViTriCode.Equals(maViTri) & m.Bac.Equals(bac) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefault();
                luongCB = salaryThangBangLuong != null ? salaryThangBangLuong.MucLuong : 0;
            }

            #region old, analytics later
            //// Nếu [SalaryThangBangLuong] thay đổi. (chưa tạo lịch sử).
            //// Cập nhật [SalaryThangBacLuongEmployees]
            //// Mỗi tháng 1 record [SalaryThangBacLuongEmployees]
            //// Get lastest information base year, month.
            //var level = dbContext.SalaryThangBacLuongEmployees
            //    .Find(m => m.EmployeeId.Equals(employeeId) & m.FlagReal.Equals(true) & m.Enable.Equals(true)
            //    & m.Year.Equals(year) & m.Month.Equals(month))
            //    .FirstOrDefault();
            //if (level != null)
            //{
            //    luongCB = level.MucLuong;
            //}
            //else
            //{
            //    // Get lastest
            //    var lastLevel = dbContext.SalaryThangBacLuongEmployees
            //    .Find(m => m.EmployeeId.Equals(employeeId) & m.FlagReal.Equals(true) & m.Enable.Equals(true))
            //    .SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();

            //    if (lastLevel != null)
            //    {
            //        var salaryThangBangLuong = dbContext.SalaryThangBangLuongs.Find(m => m.ViTriCode.Equals(lastLevel.ViTriCode) & m.Bac.Equals(lastLevel.Bac)).FirstOrDefault();
            //        luongCB = salaryThangBangLuong.MucLuong;
            //        if (salaryThangBangLuong != null)
            //        {
            //            dbContext.SalaryThangBacLuongEmployees.InsertOne(new SalaryThangBacLuongEmployee()
            //            {
            //                Year = year,
            //                Month = month,
            //                EmployeeId = employeeId,
            //                ViTriCode = salaryThangBangLuong.ViTriCode,
            //                Bac = salaryThangBangLuong.Bac,
            //                MucLuong = salaryThangBangLuong.MucLuong
            //            });
            //        }
            //    }
            //}

            //return luongCB;
            #endregion

            return luongCB;
        }

        public bool GetCalBhxh(string thang)
        {
            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Year;
            var month = toDate.Month;

            var calBHXH = true;
            if (DateTime.Now.Day < 26 && DateTime.Now.Month == month)
            {
                calBHXH = false;
            }
            return calBHXH;
        }

        [Route(Constants.LinkSalary.ThangBangLuongLawCalculator)]
        public IActionResult ThangBangLuongLawCalculator(decimal money, decimal heso, string id)
        {
            var list = new List<IdMoney>();
            decimal salaryMin = dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).First().ToiThieuVungDoanhNghiepApDung; // use reset
            if (money > 0)
            {
                salaryMin = money;
            }

            // if id null: calculator all.
            // else: get information by id=> calculator from [Bac] and return by group.
            if (!string.IsNullOrEmpty(id))
            {
                var currentLevel = dbContext.SalaryThangBangLuongs.Find(m => m.Id.Equals(id)).FirstOrDefault();
                if (currentLevel != null)
                {
                    var bac = currentLevel.Bac;
                    var maso = currentLevel.MaSo;
                    if (heso == 0)
                    {
                        heso = currentLevel.HeSo;
                    }
                    var salaryDeclareTax = heso * salaryMin;
                    if (bac > 1)
                    {
                        var previousBac = bac - 1;
                        var previousBacEntity = dbContext.SalaryThangBangLuongs.Find(m => m.MaSo.Equals(maso) & m.Bac.Equals(previousBac)).FirstOrDefault();
                        if (previousBacEntity != null)
                        {
                            salaryDeclareTax = heso * previousBacEntity.MucLuong;
                        }
                    }
                    // Add current change
                    list.Add(new IdMoney
                    {
                        Id = currentLevel.Id,
                        Money = salaryDeclareTax,
                        Rate = heso
                    });
                    var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(true) & m.MaSo.Equals(maso)).ToList();

                    foreach (var level in levels)
                    {
                        if (level.Bac > bac)
                        {
                            salaryDeclareTax = level.HeSo * salaryDeclareTax;
                            list.Add(new IdMoney
                            {
                                Id = level.Id,
                                Money = salaryDeclareTax,
                                Rate = level.HeSo
                            });
                        }
                    }
                }
            }
            else
            {
                var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(true)).ToList();
                // group by MaSo
                var groups = (from s in levels
                              group s by new
                              {
                                  s.MaSo
                              }
                                                    into l
                              select new
                              {
                                  MaSoName = l.Key.MaSo,
                                  Salaries = l.ToList(),
                              }).ToList();

                foreach (var group in groups)
                {
                    // reset salaryDeclareTax;
                    var salaryDeclareTax = salaryMin;
                    foreach (var level in group.Salaries)
                    {
                        salaryDeclareTax = level.HeSo * salaryDeclareTax;
                        list.Add(new IdMoney
                        {
                            Id = level.Id,
                            Money = salaryDeclareTax,
                            Rate = level.HeSo
                        });
                    }
                }
            }

            return Json(new { result = true, list });
        }

        [Route(Constants.LinkSalary.ThangBangLuongRealCalculator)]
        public IActionResult ThangBangLuongRealCalculator(string thang, string id, decimal heso, decimal money)
        {
            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);

            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Year;
            var month = toDate.Month;

            var list = new List<IdMoney>();
            decimal salaryMin = dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).First().ToiThieuVungDoanhNghiepApDung; // use reset
            var salaryMinApDung = salaryMin;
            if (money > 0)
            {
                salaryMin = money;
            }
            if (!string.IsNullOrEmpty(id))
            {
                if (id != "new")
                {
                    var currentLevel = dbContext.SalaryThangBangLuongs.Find(m => m.Id.Equals(id) & m.Month.Equals(month) & m.Year.Equals(year)).FirstOrDefault();
                    if (currentLevel != null)
                    {
                        var bac = currentLevel.Bac;
                        var vitriCode = currentLevel.ViTriCode;
                        if (heso == 0)
                        {
                            heso = currentLevel.HeSo;
                        }
                        var salaryDeclareTax = Math.Round(salaryMin, 0);
                        var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.ViTriCode.Equals(vitriCode) & m.Month.Equals(month) & m.Year.Equals(year)).ToList();
                        foreach (var level in levels)
                        {
                            if (level.Bac > bac)
                            {
                                // Rule bac 1 =  muc tham chieu
                                if (level.Bac > 1)
                                {
                                    salaryDeclareTax = Math.Round(heso * salaryDeclareTax, 0);
                                }
                                list.Add(new IdMoney
                                {
                                    Id = level.Id,
                                    Money = salaryDeclareTax,
                                    Rate = heso
                                });
                            }
                        }
                    }
                }
                else
                {
                    heso = heso == 0 ? 1 : heso;
                    var salaryDeclareTax = Math.Round(salaryMin, 0);
                    list.Add(new IdMoney
                    {
                        Id = "new-1",
                        Money = salaryDeclareTax,
                        Rate = heso
                    });
                    for (var i = 2; i <= 10; i++)
                    {
                        salaryDeclareTax = Math.Round(heso * salaryDeclareTax, 0);
                        list.Add(new IdMoney
                        {
                            Id = "new-" + i,
                            Money = salaryDeclareTax,
                            Rate = heso
                        });
                    }
                }
            }
            else
            {
                // Ap dung nếu hệ số bậc là 1 + Muc Luong is min.
                var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Bac.Equals(1) & m.Month.Equals(month) & m.Year.Equals(year) & m.MucLuong.Equals(salaryMinApDung)).ToList();

                // group by VITRI
                var groups = (from s in levels
                              group s by new
                              {
                                  s.ViTriCode
                              }
                                                    into l
                              select new
                              {
                                  l.Key.ViTriCode,
                                  Salaries = l.ToList(),
                              }).ToList();

                foreach (var group in groups)
                {
                    // reset salaryDeclareTax;
                    var salaryDeclareTax = salaryMin;
                    foreach (var level in group.Salaries)
                    {
                        //Rule level 1 = muc
                        if (level.Bac > 1)
                        {
                            salaryDeclareTax = level.HeSo * salaryDeclareTax;
                        }
                        list.Add(new IdMoney
                        {
                            Id = level.Id,
                            Money = salaryDeclareTax,
                            Rate = level.HeSo
                        });
                    }
                }
            }
            return Json(new { result = true, list });
        }

        [Route(Constants.LinkSalary.UpdateData)]
        public IActionResult UpdateData()
        {
            InitCaiDat();
            InitChucVuSale();
            InitKPI();



            InitLuongToiThieuVung();
            InitLuongFeeLaw();
            InitThangBangLuong();
            InitSalaryPhuCapPhucLoi();
            InitSalaryThangBangPhuCapPhucLoi();
            InitChucDanhCongViec();

            return Json(new { result = true });
        }

        private void InitCaiDat()
        {
            dbContext.SalarySettings.DeleteMany(new BsonDocument());
            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "mau-so-lam-viec",
                Value = "26",
                Title = "Ngày làm việc"
            });

            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "mau-so-khac",
                Value = "27",
                Title = "Ngày làm việc"
            });
            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "mau-so-bao-ve",
                Value = "30",
                Title = "Ngày làm việc"
            });

            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "ty-le-dong-bh",
                Value = "0.105",
                Title = "Tỷ lệ đóng BH"
            });
        }

        // Init sale chuc vu
        private void InitChucVuSale()
        {
            dbContext.ChucVuSales.DeleteMany(new BsonDocument());
            var chucvu = "ĐDKD HCM";
            int i = 1;
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "TKD HCM";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "ĐDKD TỈNH";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "TKD TỈNH";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "ADMIN";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "ĐDKD BÙN";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "TKD BÙN";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;
        }

        private void InitKPI()
        {
            dbContext.SaleKPIs.DeleteMany(new BsonDocument());
            var typeKHM = "KH Mới";
            var typeKHMAlias = Utility.AliasConvert(typeKHM);
            var typeKHMCode = Constants.System.kPITypeCode + 1.ToString("00");
            var conditionKHM = string.Empty;
            var conditionKHMValue = string.Empty;

            var typeDP = "Độ phủ";
            var typeDPAlias = Utility.AliasConvert(typeDP);
            var typeDPCode = Constants.System.kPITypeCode + 2.ToString("00");
            var conditionDP = "Trên 80%";
            var conditionDPValue = "80";

            var typeNH = "Ngành hàng";
            var typeNHAlias = Utility.AliasConvert(typeNH);
            var typeNHCode = Constants.System.kPITypeCode + 3.ToString("00");
            var conditionNH = "Đạt 70% 4 ngành";
            var conditionNHValue = "70";

            var typeDT = "Doanh thu";
            var typeDTAlias = Utility.AliasConvert(typeDT);
            var typeDTCode = Constants.System.kPITypeCode + 4.ToString("00");
            var conditionDT1 = "80%-99%";
            var conditionDT1Value = "80-99";
            var conditionDT2 = "Trên 100%";
            var conditionDT2Value = "100";


            var typeDS = "Doanh số";
            var typeDSAlias = Utility.AliasConvert(typeDS);
            var typeDSCode = Constants.System.kPITypeCode + 5.ToString("00");
            var conditionDS1 = "80%-99%";
            var conditionDS1Value = "80-99";
            var conditionDS2 = "Trên 100%";
            var conditionDS2Value = "100-119";
            var conditionDS3 = "Trên 120%";
            var conditionDS3Value = "120";


            var chucvus = dbContext.ChucVuSales.Find(m => m.Enable.Equals(true)).ToList();
            // Update value later.
            foreach (var item in chucvus)
            {
                // KH Mới
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeKHM,
                    TypeAlias = typeKHMAlias,
                    TypeCode = typeKHMCode,
                    Condition = conditionKHM,
                    ConditionValue = conditionKHMValue,
                    Value = "500"
                });

                // DP
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDP,
                    TypeAlias = typeDPAlias,
                    TypeCode = typeDPCode,
                    Condition = conditionDP,
                    ConditionValue = conditionDPValue,
                    Value = "1000"
                });

                // Ngành hàng
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeNH,
                    TypeAlias = typeNHAlias,
                    TypeCode = typeNHCode,
                    Condition = conditionNH,
                    ConditionValue = conditionNHValue,
                    Value = "500"
                });

                // Doanh thu
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDT,
                    TypeAlias = typeDTAlias,
                    TypeCode = typeDTCode,
                    Condition = conditionDT1,
                    ConditionValue = conditionDT1Value,
                    Value = "1000"
                });

                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDT,
                    TypeAlias = typeDTAlias,
                    TypeCode = typeDTCode,
                    Condition = conditionDT2,
                    ConditionValue = conditionDT2Value,
                    Value = "2000"
                });

                // DS
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDS,
                    TypeAlias = typeDSAlias,
                    TypeCode = typeDPCode,
                    Condition = conditionDS1,
                    ConditionValue = conditionDS1Value,
                    Value = "1000"
                });
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDS,
                    TypeAlias = typeDSAlias,
                    TypeCode = typeDPCode,
                    Condition = conditionDS2,
                    ConditionValue = conditionDS2Value,
                    Value = "3000"
                });
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDS,
                    TypeAlias = typeDSAlias,
                    TypeCode = typeDPCode,
                    Condition = conditionDS3,
                    ConditionValue = conditionDS3Value,
                    Value = "4000"
                });
            }
        }

        private void InitLogistics()
        {
            // CityXeGiaoNhan
            dbContext.CityGiaoNhans.DeleteMany(new BsonDocument());
            var listLocationGiaoNhan = new List<string>
            {
                "TP.HCM",
                "Bình Dương",
                "Biên Hòa",
                "Vũng Tàu",
                "BìnhThuận",
                "Cần Thơ"
                ,"Vĩnh Long"
                ,"Long An"
                ,"Tiền Giang"
                ,"Đồng Nai"
            };
            // Code update later...
            foreach (var item in listLocationGiaoNhan)
            {
                dbContext.CityGiaoNhans.InsertOne(new CityGiaoNhan
                {
                    City = item
                });
            }
            var xes = new List<string>()
            {
                "Xe nhỏ",
                "Xe lớn"
            };
            // Don gia chuyen xe
            dbContext.DonGiaChuyenXes.DeleteMany(new BsonDocument());

            foreach (var item in listLocationGiaoNhan)
            {
                if (item == "TP.HCM")
                {
                    var xe2s = new List<string>()
                    {
                        "Xe nhỏ 1.7 tấn",
                        "Xe lớn ben và 8 tấn"
                    };
                    foreach (var xe in xe2s)
                    {
                        for (var i = 1; i <= 5; i++)
                        {
                            dbContext.DonGiaChuyenXes.InsertOne(new DonGiaChuyenXe
                            {
                                Year = 2018,
                                Month = 8

                            });
                        }
                    }
                }
                else if (item == "Bình Dương" || item == "Biên Hòa")
                {
                    foreach (var xe in xes)
                    {
                        for (var i = 1; i <= 5; i++)
                        {

                        }
                    }
                }
                else
                {
                    foreach (var xe in xes)
                    {

                    }
                }
            }


            // Ho tro cong tac xa
            dbContext.HoTroCongTacXas.DeleteMany(new BsonDocument());
        }

        private void InitLuongToiThieuVung()
        {
            dbContext.SalaryMucLuongVungs.DeleteMany(new BsonDocument());
            dbContext.SalaryMucLuongVungs.InsertOne(new SalaryMucLuongVung()
            {
                ToiThieuVungQuiDinh = 3980000,
                TiLeMucDoanhNghiepApDung = (decimal)1.07,
                ToiThieuVungDoanhNghiepApDung = 3980000 * (decimal)1.07,
                Month = 8,
                Year = 2018
            });
        }

        private void InitLuongFeeLaw()
        {
            dbContext.SalaryFeeLaws.DeleteMany(new BsonDocument());
            dbContext.SalaryFeeLaws.InsertOne(new SalaryFeeLaw()
            {
                Name = "Bảo hiểm xã hội",
                NameAlias = Utility.AliasConvert("Bảo hiểm xã hội"),
                TiLeDong = (decimal)0.105,
                Description = "Bao gồm: BHXH (8%), BHYT(1.5%), Thất nghiệp (1%). Theo Quyết định 595/QĐ-BHXH."
            });
        }

        private void InitThangBangLuong()
        {
            dbContext.SalaryThangBangLuongs.DeleteMany(mbox => mbox.FlagReal.Equals(false));
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
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
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
                decimal heso = 1;
                if (i > 0)
                {
                    heso = (decimal)1.05;
                    if (i == 1)
                    {
                        heso = (decimal)1.8;
                    }
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
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
                decimal heso = 1;
                if (i > 0)
                {
                    heso = (decimal)1.05;
                    if (i == 1)
                    {
                        heso = (decimal)1.7;
                    }
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
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
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
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
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
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
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
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
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
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
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
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
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
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
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
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

        private void InitSalaryPhuCapPhucLoi()
        {
            dbContext.SalaryPhuCapPhucLois.DeleteMany(new BsonDocument());

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            #region Phu Cap
            int type = 1; // phu-cap
            var name = string.Empty;
            int i = 1;

            name = "NẶNG NHỌC ĐỘC HẠI";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;
            name = "TRÁCH NHIỆM";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "THÂM NIÊN";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "THU HÚT";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;
            #endregion

            #region Phuc Loi
            type = 2; // phuc-loi
            i = 1;

            name = "XĂNG";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "ĐIỆN THOẠI";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "CƠM";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "Kiêm nhiệm";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "BHYT ĐẶC BIỆT";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });

            name = "VỊ TRÍ CẦN KN NHIỀU NĂM";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });

            name = "Vị trí đặc thù";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });

            name = "Nhà ở";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;
            #endregion
        }

        private void InitSalaryThangBangPhuCapPhucLoi()
        {
            dbContext.SalaryThangBangPhuCapPhucLois.DeleteMany(m => m.FlagReal.Equals(false));
            #region TGD
            // Trach nhiem 01-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.01",
                Money = 500000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.01",
                Money = 500000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.01",
                Money = 500000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.01",
                Money = 500000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.01",
                Money = 0
            });
            #endregion

            #region GĐ/PGĐ
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.02",
                Money = 300000
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.02",
                Money = 400000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.02",
                Money = 300000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.02",
                Money = 400000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.02",
                Money = 0
            });
            #endregion

            #region KT trưởng
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.03",
                Money = 300000
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.03",
                Money = 400000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.03",
                Money = 300000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.03",
                Money = 400000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.03",
                Money = 0
            });
            #endregion

            #region Trưởng BP
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "D.01",
                Money = 200000
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "D.01",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "D.01",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "D.01",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.01",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.01",
                Money = 200000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "B.01",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "B.01",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "B.01",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.01",
                Money = 0
            });
            #endregion

            #region Tổ trưởng
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "D.02",
                Money = 100000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "D.02",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "D.02",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "D.02",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.02",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.02",
                Money = 100000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "B.02",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "B.02",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "B.02",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.02",
                Money = 0
            });
            #endregion

            #region Tổ phó
            // B.02
            #endregion

            #region Others
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "D.03",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.03",
                Money = 200000
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.03",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.03",
                Money = 200000
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.04",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.04",
                Money = 200000
            });
            #endregion
        }

        // Base on ThangBangLuong. Do later
        private void InitChucDanhCongViec()
        {
            dbContext.ChucDanhCongViecs.DeleteMany(new BsonDocument());
            var listTemp = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) && m.Law.Equals(true)).ToList();
            foreach (var item in listTemp)
            {
                if (!(dbContext.ChucDanhCongViecs.CountDocuments(m => m.Code.Equals(item.MaSo)) > 0))
                {
                    dbContext.ChucDanhCongViecs.InsertOne(new ChucDanhCongViec()
                    {
                        Name = item.Name,
                        Alias = item.NameAlias,
                        Code = item.MaSo,
                        Type = item.TypeRole,
                        TypeAlias = item.TypeRoleAlias,
                        TypeCode = item.TypeRoleCode
                    });
                }
            }
        }

        #endregion

    }
}
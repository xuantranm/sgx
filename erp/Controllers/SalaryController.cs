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
    [Route(Constants.LinkSalary.Main)]
    public class SalaryController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalaryController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalaryController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _logger = logger;
        }

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

        [Route(Constants.LinkSalary.VanPhong + "/" + Constants.LinkSalary.BangLuong)]
        public async Task<IActionResult> BangLuong(string Thang, string Id, string PhongBan, string BoPhan)
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
            var congtychinhanhs = dbContext.CongTyChiNhanhs.Find(m => m.Enable.Equals(true)).ToList();
            var ctcnvp = congtychinhanhs.First(x => x.Code.Equals("CT1"));
            var ctcnnm = congtychinhanhs.First(x => x.Code.Equals("CT2"));

            var sortTimes = Utility.DllMonths();
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                                && m.CongTyChiNhanh.Equals(ctcnvp.Id) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToList();
            #endregion

            #region Times
            var toDate = Utility.GetSalaryToDate(Thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            Thang = string.IsNullOrEmpty(Thang) ? month + "-" + year : Thang;
            #endregion

            // MOI THANG SE CO 1 DANH SACH TAI THOI DIEM DO
            // TRANH NGUOI MOI CO TRONG BANG LUONG CU
            Utility.AutoInitSalary((int)ESalaryType.VP, month, year);

            #region Filter
            var builder = Builders<SalaryEmployeeMonth>.Filter;
            var filter = builder.Eq(m => m.Year, year) & builder.Eq(m => m.Month, month)
                        & builder.Eq(m => m.CongTyChiNhanhId, ctcnvp.Id);
            if (!string.IsNullOrEmpty(PhongBan))
            {
                filter = filter & builder.Eq(x => x.PhongBanId, PhongBan);
            }
            if (!string.IsNullOrEmpty(BoPhan))
            {
                filter = filter & builder.Eq(x => x.BoPhanId, BoPhan);
            }
            if (!string.IsNullOrEmpty(Id))
            {
                filter = filter & builder.Eq(x => x.Id, Id.Trim());
            }
            #endregion

            var salaryEmployeeMonths = dbContext.SalaryEmployeeMonths.Find(filter).ToList();

            #region FILL DATA
            var results = new List<SalaryEmployeeMonth>();
            foreach (var salary in salaryEmployeeMonths)
            {
                var salaryFull = Utility.SalaryEmployeeMonthFillData(salary);
                results.Add(salaryFull);
            }
            #endregion

            #region SORT: do later

            #endregion

            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = salaryEmployeeMonths,
                MonthYears = sortTimes,
                Thang = Thang,
                Employees = employees,
                Id = Id,
                Phongban = PhongBan,
                BoPhan = BoPhan
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.VanPhong + "/" + Constants.LinkSalary.BangLuong + "/" + Constants.LinkSalary.Update)]
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
                        .Set(m => m.BHXHBHYT, item.BHXHBHYT * 1000)
                        .Set(m => m.LuongThamGiaBHXH, item.LuongThamGiaBHXH * 1000)
                        .Set(m => m.TamUng, item.TamUng * 1000)
                        .Set(m => m.ThuongLeTet, item.ThuongLeTet * 1000)
                        .Set(m => m.ThucLanh, item.ThucLanh * 1000)
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

        #region THANG LUONG
        [Route(Constants.LinkSalary.VanPhong + "/" + Constants.LinkSalary.ThangLuong)]
        public async Task<IActionResult> ThangLuong(string thang)
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

            var mucluongvungs = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefaultAsync();
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

            var thangbangluongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) && m.Law.Equals(false) && m.Month.Equals(month) && m.Year.Equals(year)).ToListAsync();
            if (thangbangluongs.Count == 0)
            {
                var lastItem = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) && m.Law.Equals(false)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonth = lastItem.Month;
                var lastYear = lastItem.Year;
                var lastestThangBangLuongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) && m.Law.Equals(false) && m.Month.Equals(lastMonth) && m.Year.Equals(lastYear)).ToListAsync();
                foreach (var item in lastestThangBangLuongs)
                {
                    item.Id = null;
                    item.Month = month;
                    item.Year = year;
                    dbContext.SalaryThangBangLuongs.InsertOne(item);
                }
                thangbangluongs = lastestThangBangLuongs;
            }

            var pcpls = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) && m.Law.Equals(false) && m.Month.Equals(month) && m.Year.Equals(year)).ToListAsync();
            if (pcpls.Count == 0)
            {
                var lastItemPC = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) && m.Law.Equals(false)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonthPC = lastItemPC.Month;
                var lastYearPC = lastItemPC.Year;
                var lastestPcplss = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) && m.Law.Equals(false) && m.Month.Equals(lastMonthPC) && m.Year.Equals(lastYearPC)).ToListAsync();
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
                Thang = thang,
                MonthYears = sortTimes
            };
            return View(viewModel);
        }

        [Route(Constants.LinkSalary.VanPhong + "/" + Constants.LinkSalary.ThangLuong + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> ThangLuongUpdate(string thang)
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

            var thangbangluongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(false) & m.Month.Equals(month) & m.Year.Equals(year)).ToListAsync();
            if (thangbangluongs.Count == 0)
            {
                var lastItem = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(false)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonth = lastItem.Month;
                var lastYear = lastItem.Year;
                var lastestThangBangLuongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(false) & m.Month.Equals(lastMonth) & m.Year.Equals(lastYear)).ToListAsync();
                foreach (var item in lastestThangBangLuongs)
                {
                    item.Id = null;
                    item.Month = month;
                    item.Year = year;
                    dbContext.SalaryThangBangLuongs.InsertOne(item);
                }
                thangbangluongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(false) & m.Month.Equals(month) & m.Year.Equals(year)).ToListAsync();
            }

            var pcpls = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.Law.Equals(false) & m.Month.Equals(month) & m.Year.Equals(year)).ToListAsync();
            if (pcpls.Count == 0)
            {
                var lastItemPC = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.Law.Equals(false)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonthPC = lastItemPC.Month;
                var lastYearPC = lastItemPC.Year;
                var lastestPcplss = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.Law.Equals(false) & m.Month.Equals(lastMonthPC) & m.Year.Equals(lastYearPC)).ToListAsync();
                foreach (var item in lastestPcplss)
                {
                    item.Id = null;
                    item.Month = month;
                    item.Year = year;
                    dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(item);
                }
                pcpls = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.Law.Equals(false) & m.Month.Equals(month) & m.Year.Equals(year)).ToListAsync(); ;
            }

            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = mucluongvungs,
                SalaryThangBangLuongs = thangbangluongs,
                SalaryThangBangPhuCapPhucLoisReal = pcpls,
                Thang = thang,
                MonthYears = sortTimes
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.VanPhong + "/" + Constants.LinkSalary.ThangLuong + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> ThangLuongUpdate(ThangBangLuongViewModel viewModel)
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

                var thang = viewModel.Thang;
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
                    .Set(m => m.ToiThieuVungDoanhNghiepApDung, salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung * 1000)
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
                            var exist = dbContext.SalaryThangBangLuongs.CountDocuments(m => m.ViTriCode.Equals(vitriCode) & m.Bac.Equals(lv) & m.Law.Equals(false) & m.Month.Equals(month) & m.Year.Equals(year));
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
                                    Law = false
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
                            string vitriLastCode = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(false)).SortByDescending(m => m.ViTriCode).FirstOrDefault().ViTriCode;
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
                                    Law = false
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
        #endregion

        #region SALES
        [Route(Constants.LinkSalary.SaleKPIEmployee)]
        public async Task<IActionResult> SaleKPIEmployee(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongVP, (int)ERights.View)))
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

            var salekpis = GetSaleKPIs(month, year);

            var saleKPIEmployees = GetSaleKPIEmployees(month, year);

            var viewModel = new BangLuongViewModel
            {
                SaleKPIs = salekpis,
                SaleKPIEmployees = saleKPIEmployees,
                MonthYears = sortTimes,
                Thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.SaleKPIEmployee + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SaleKPIEmployeeUpdate(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongVP, (int)ERights.View)))
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

            var salekpis = GetSaleKPIs(month, year);

            var saleKPIEmployees = GetSaleKPIEmployees(month, year);

            var viewModel = new BangLuongViewModel
            {
                SaleKPIs = salekpis,
                SaleKPIEmployees = saleKPIEmployees,
                MonthYears = sortTimes,
                Thang = thang
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.SaleKPIEmployee + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SaleKPIEmployeeUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.SaleKPIEmployees)
            {
                var itemFull = GetSaleKPIEmployee(item, item.Month + "-" + item.Year);
                var builder = Builders<SaleKPIEmployee>.Filter;
                var filter = builder.Eq(m => m.Id, itemFull.Id);
                var update = Builders<SaleKPIEmployee>.Update
                    .Set(m => m.ChiTieuDoanhSo, itemFull.ChiTieuDoanhSo)
                    .Set(m => m.ChiTieuDoanhThu, itemFull.ChiTieuDoanhThu)
                    .Set(m => m.ChiTieuDoPhu, itemFull.ChiTieuDoPhu)
                    .Set(m => m.ChiTieuMoMoi, itemFull.ChiTieuMoMoi)
                    .Set(m => m.ChiTieuNganhHang, itemFull.ChiTieuNganhHang)
                    .Set(m => m.ThucHienDoanhSo, itemFull.ThucHienDoanhSo)
                    .Set(m => m.ThucHienDoanhThu, itemFull.ThucHienDoanhThu)
                    .Set(m => m.ThucHienDoPhu, itemFull.ThucHienDoPhu)
                    .Set(m => m.ThucHienMoMoi, itemFull.ThucHienMoMoi)
                    .Set(m => m.ThucHienNganhHang, itemFull.ThucHienNganhHang)
                    .Set(m => m.ChiTieuThucHienDoanhSo, itemFull.ChiTieuThucHienDoanhSo)
                    .Set(m => m.ChiTieuThucHienDoanhThu, itemFull.ChiTieuThucHienDoanhThu)
                    .Set(m => m.ChiTieuThucHienDoPhu, itemFull.ChiTieuThucHienDoPhu)
                    .Set(m => m.ChiTieuThucHienMoMoi, itemFull.ChiTieuThucHienMoMoi)
                    .Set(m => m.ChiTieuThucHienNganhHang, itemFull.ChiTieuThucHienNganhHang)
                    .Set(m => m.ThuongChiTieuThucHienDoanhSo, itemFull.ThuongChiTieuThucHienDoanhSo)
                    .Set(m => m.ThuongChiTieuThucHienDoanhThu, itemFull.ThuongChiTieuThucHienDoanhThu)
                    .Set(m => m.ThuongChiTieuThucHienDoPhu, itemFull.ThuongChiTieuThucHienDoPhu)
                    .Set(m => m.ThuongChiTieuThucHienMoMoi, itemFull.ThuongChiTieuThucHienMoMoi)
                    .Set(m => m.ThuongChiTieuThucHienNganhHang, itemFull.ThuongChiTieuThucHienNganhHang)
                    .Set(m => m.TongThuong, itemFull.TongThuong)
                    .Set(m => m.ThuViec, itemFull.ThuViec)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SaleKPIEmployees.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        [Route(Constants.LinkSalary.SaleKPI + "/" + Constants.LinkSalary.Update)]
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongVP, (int)ERights.View)))
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

            var kpis = GetSaleKPIs(month, year);

            var viewModel = new BangLuongViewModel()
            {
                SaleKPIs = kpis,
                MonthYears = sortTimes,
                Thang = thang
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.SaleKPI + "/" + Constants.LinkSalary.Update)]
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongVP, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            #endregion

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

            foreach (var item in viewModel.SaleKPIs)
            {
                if (!string.IsNullOrEmpty(item.Id))
                {
                    var builder = Builders<SaleKPI>.Filter;
                    var filter = builder.Eq(m => m.Id, item.Id);
                    var update = Builders<SaleKPI>.Update
                        .Set(m => m.KHMoi, item.KHMoi * 1000)
                        .Set(m => m.DoPhuTren80, item.DoPhuTren80 * 1000)
                        .Set(m => m.NganhHangDat704Nganh, item.NganhHangDat704Nganh * 1000)
                        .Set(m => m.DoanhThuTren80, item.DoanhThuTren80 * 1000)
                        .Set(m => m.DoanhThuDat100, item.DoanhThuDat100 * 1000)
                        .Set(m => m.DoanhSoTren80, item.DoanhSoTren80 * 1000)
                        .Set(m => m.DoanhSoDat100, item.DoanhSoDat100 * 1000)
                        .Set(m => m.DoanhSoTren120, item.DoanhSoTren120 * 1000)
                        .Set(m => m.Total, item.Total * 1000)
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    dbContext.SaleKPIs.UpdateOne(filter, update);
                }
                else
                {
                    item.Month = month;
                    item.Year = year;
                    item.KHMoi = item.KHMoi * 1000;
                    item.DoPhuTren80 = item.DoPhuTren80 * 1000;
                    item.NganhHangDat704Nganh = item.NganhHangDat704Nganh * 1000;
                    item.DoanhThuTren80 = item.DoanhThuTren80 * 1000;
                    item.DoanhThuDat100 = item.DoanhThuDat100 * 1000;
                    item.DoanhSoTren80 = item.DoanhSoTren80 * 1000;
                    item.DoanhSoDat100 = item.DoanhSoDat100 * 1000;
                    item.DoanhSoTren120 = item.DoanhSoTren120 * 1000;
                    item.Total = item.Total * 1000;
                    dbContext.SaleKPIs.InsertOne(item);
                }
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        [Route(Constants.LinkSalary.SaleKPIEmployeeCalculator)]
        public IActionResult SaleKPIEmployeeCalculator(BangLuongViewModel viewModel)
        {
            var entity = viewModel.SaleKPIEmployees.First();
            string thang = entity.Month + "-" + entity.Year;
            var returnEntity = GetSaleKPIEmployee(entity, thang);
            return Json(new { entity = returnEntity });
        }

        public SaleKPIEmployee GetSaleKPIEmployee(SaleKPIEmployee newData, string thang)
        {
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

            decimal ChiTieuThucHienDoanhSo = 0;
            decimal ChiTieuThucHienDoanhThu = 0;
            decimal ChiTieuThucHienDoPhu = 0;
            decimal ChiTieuThucHienMoMoi = 0;
            int ChiTieuThucHienNganhHang = 0;

            if (newData.ChiTieuDoanhSo > 0)
            {
                ChiTieuThucHienDoanhSo = (newData.ThucHienDoanhSo / newData.ChiTieuDoanhSo) * 100;
            }
            if (newData.ChiTieuDoanhThu > 0)
            {
                ChiTieuThucHienDoanhThu = (newData.ThucHienDoanhThu / newData.ChiTieuDoanhThu) * 100;
            }
            if (newData.ChiTieuDoPhu > 0)
            {
                ChiTieuThucHienDoPhu = (newData.ThucHienDoPhu / newData.ChiTieuDoPhu) * 100;
            }
            if (newData.ChiTieuMoMoi > 0)
            {
                ChiTieuThucHienMoMoi = (newData.ThucHienMoMoi / newData.ChiTieuMoMoi) * 100;
            }
            if (newData.ChiTieuNganhHang > 0)
            {
                ChiTieuThucHienNganhHang = (newData.ThucHienNganhHang / newData.ChiTieuNganhHang) * 100;
            }

            decimal ThuongChiTieuThucHienDoanhSo = 0;
            decimal ThuongChiTieuThucHienDoanhThu = 0;
            decimal ThuongChiTieuThucHienDoPhu = 0;
            decimal ThuongChiTieuThucHienMoMoi = 0;
            decimal ThuongChiTieuThucHienNganhHang = 0;
            var kpi = dbContext.SaleKPIs.Find(m => m.Enable.Equals(true) && m.ChucVu.Equals(newData.ChucVu) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            // kpi never null.
            if (kpi != null)
            {
                //DOANH SO =IF(N21>120%,3000,IF(N21>=100%,2000,IF(N21>=80%,1000,"")))
                if (ChiTieuThucHienDoanhSo > 120)
                {
                    ThuongChiTieuThucHienDoanhSo = kpi.DoanhSoTren120;
                }
                else if (ChiTieuThucHienDoanhSo >= 100)
                {
                    ThuongChiTieuThucHienDoanhSo = kpi.DoanhSoDat100;
                }
                else if (ChiTieuThucHienDoanhSo >= 80)
                {
                    ThuongChiTieuThucHienDoanhSo = kpi.DoanhSoTren80;
                }
                //DOANH THU =IF(O21>100%,2000,IF(O21>=80%,1000,0))
                if (ChiTieuThucHienDoanhThu >= 80)
                {
                    ThuongChiTieuThucHienDoanhThu = kpi.DoanhThuTren80;
                }
                else if (ChiTieuThucHienDoanhThu > 100)
                {
                    ThuongChiTieuThucHienDoanhThu = kpi.DoanhThuDat100;
                }
                //DO PHU =IF(P21>80%,1000,0)
                if (ChiTieuThucHienDoPhu > 80)
                {
                    ThuongChiTieuThucHienDoPhu = kpi.DoPhuTren80;
                }
                //MO MOI =IF(Q21>=100%,500,0)
                if (ChiTieuThucHienMoMoi >= 100)
                {
                    ThuongChiTieuThucHienMoMoi = kpi.KHMoi;
                }
                // NGANH HANG =IF(R21>=100%,500,0)
                if (ChiTieuThucHienNganhHang >= 100)
                {
                    ThuongChiTieuThucHienNganhHang = kpi.NganhHangDat704Nganh;
                }
            }
            newData.ChiTieuThucHienDoanhSo = ChiTieuThucHienDoanhSo;
            newData.ChiTieuThucHienDoanhThu = ChiTieuThucHienDoanhThu;
            newData.ChiTieuThucHienDoPhu = ChiTieuThucHienDoPhu;
            newData.ChiTieuThucHienMoMoi = ChiTieuThucHienMoMoi;
            newData.ChiTieuThucHienNganhHang = ChiTieuThucHienNganhHang;
            newData.ThuongChiTieuThucHienDoanhSo = ThuongChiTieuThucHienDoanhSo;
            newData.ThuongChiTieuThucHienDoanhThu = ThuongChiTieuThucHienDoanhThu;
            newData.ThuongChiTieuThucHienDoPhu = ThuongChiTieuThucHienDoPhu;
            newData.ThuongChiTieuThucHienMoMoi = ThuongChiTieuThucHienMoMoi;
            newData.ThuongChiTieuThucHienNganhHang = ThuongChiTieuThucHienNganhHang;
            newData.TongThuong = ThuongChiTieuThucHienDoanhSo + ThuongChiTieuThucHienDoanhThu + ThuongChiTieuThucHienDoPhu + ThuongChiTieuThucHienMoMoi + ThuongChiTieuThucHienNganhHang;
            return newData;
        }

        private List<SaleKPI> GetSaleKPIs(int month, int year)
        {
            var kpis = dbContext.SaleKPIs.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).ToList();
            if (kpis.Count == 0)
            {
                var checkLast = dbContext.SaleKPIs.CountDocuments(m => m.Enable.Equals(true));
                if (checkLast == 0)
                {
                    // insert empty value
                    var chucvukinhdoanhs = dbContext.ChucVuKinhDoanhs.Find(m => m.Enable.Equals(true)).ToList();
                    if (chucvukinhdoanhs.Count == 0)
                    {
                        chucvukinhdoanhs = GetChucVuKinhDoanhs();
                    }
                    foreach (var chucvu in chucvukinhdoanhs)
                    {
                        dbContext.SaleKPIs.InsertOne(new SaleKPI
                        {
                            Year = year,
                            Month = month,
                            ChucVu = chucvu.Name,
                            ChucVuCode = chucvu.Code,
                            ChucVuAlias = chucvu.Alias,
                            KHMoi = 0,
                            DoPhuTren80 = 0,
                            NganhHangDat704Nganh = 0,
                            DoanhThuTren80 = 0,
                            DoanhThuDat100 = 0,
                            DoanhSoTren80 = 0,
                            DoanhSoDat100 = 0,
                            DoanhSoTren120 = 0,
                            Total = 0
                        });
                    }
                }
                else
                {
                    // insert lastest value for new data.
                    var lastKpis = dbContext.SaleKPIs.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).ToList();
                    foreach (var item in lastKpis)
                    {
                        item.Id = null;
                        item.Year = year;
                        item.Month = month;
                        dbContext.SaleKPIs.InsertOne(item);
                    }
                }
                kpis = dbContext.SaleKPIs.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).ToList();
            }
            return kpis;
        }

        private List<SaleKPIEmployee> GetSaleKPIEmployees(int month, int year)
        {
            var datas = dbContext.SaleKPIEmployees.Find(m => m.Enable.Equals(true) && m.Year.Equals(year) && m.Month.Equals(month)).ToList();
            if (datas.Count == 0)
            {
                var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && !m.UserName.Equals(Constants.System.account)
                 && (m.CodeOld.Contains("KDS") || m.CodeOld.Contains("KDV"))).ToList();
                foreach (var employee in employees)
                {
                    var salekpiemployee = new SaleKPIEmployee
                    {
                        Year = year,
                        Month = month,
                        EmployeeId = employee.Id,
                        MaNhanVien = employee.CodeOld,
                        FullName = employee.FullName,
                        ChucVu = employee.SaleChucVu
                    };
                    var lastestSaleKPIEmployee = dbContext.SaleKPIEmployees.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employee.Id)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
                    if (lastestSaleKPIEmployee != null)
                    {
                        salekpiemployee.ChiTieuDoanhSo = lastestSaleKPIEmployee.ChiTieuDoanhSo;
                        salekpiemployee.ChiTieuDoanhThu = lastestSaleKPIEmployee.ChiTieuDoanhThu;
                        salekpiemployee.ChiTieuDoPhu = lastestSaleKPIEmployee.ChiTieuDoPhu;
                        salekpiemployee.ChiTieuMoMoi = lastestSaleKPIEmployee.ChiTieuMoMoi;
                        salekpiemployee.ChiTieuNganhHang = lastestSaleKPIEmployee.ChiTieuNganhHang;
                        salekpiemployee.ThucHienDoanhSo = lastestSaleKPIEmployee.ThucHienDoanhSo;
                        salekpiemployee.ThucHienDoanhThu = lastestSaleKPIEmployee.ThucHienDoanhThu;
                        salekpiemployee.ThucHienDoPhu = lastestSaleKPIEmployee.ThucHienDoPhu;
                        salekpiemployee.ThucHienMoMoi = lastestSaleKPIEmployee.ThucHienMoMoi;
                        salekpiemployee.ThucHienNganhHang = lastestSaleKPIEmployee.ThucHienNganhHang;
                        salekpiemployee.ChiTieuThucHienDoanhSo = lastestSaleKPIEmployee.ChiTieuThucHienDoanhSo;
                        salekpiemployee.ChiTieuThucHienDoanhThu = lastestSaleKPIEmployee.ChiTieuThucHienDoanhThu;
                        salekpiemployee.ChiTieuThucHienDoPhu = lastestSaleKPIEmployee.ChiTieuThucHienDoPhu;
                        salekpiemployee.ChiTieuThucHienMoMoi = lastestSaleKPIEmployee.ChiTieuThucHienMoMoi;
                        salekpiemployee.ChiTieuThucHienNganhHang = lastestSaleKPIEmployee.ChiTieuThucHienNganhHang;
                        salekpiemployee.ThuongChiTieuThucHienDoanhSo = lastestSaleKPIEmployee.ThuongChiTieuThucHienDoanhSo;
                        salekpiemployee.ThuongChiTieuThucHienDoanhThu = lastestSaleKPIEmployee.ThuongChiTieuThucHienDoanhThu;
                        salekpiemployee.ThuongChiTieuThucHienDoPhu = lastestSaleKPIEmployee.ThuongChiTieuThucHienDoPhu;
                        salekpiemployee.ThuongChiTieuThucHienMoMoi = lastestSaleKPIEmployee.ThuongChiTieuThucHienMoMoi;
                        salekpiemployee.ThuongChiTieuThucHienNganhHang = lastestSaleKPIEmployee.ThuongChiTieuThucHienNganhHang;
                        salekpiemployee.TongThuong = lastestSaleKPIEmployee.TongThuong;
                        salekpiemployee.ThuViec = lastestSaleKPIEmployee.ThuViec;
                        salekpiemployee.ChucVu = lastestSaleKPIEmployee.ChucVu;
                    }
                    dbContext.SaleKPIEmployees.InsertOne(salekpiemployee);
                }
                datas = dbContext.SaleKPIEmployees.Find(m => m.Enable.Equals(true) && m.Year.Equals(year) && m.Month.Equals(month)).ToList();
            }

            return datas;
        }

        [Route(Constants.LinkSalary.SaleKPIEmployeeImport)]
        [HttpPost]
        public ActionResult SaleKPIEmployeeImport()
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

                    for (int i = 4; i <= sheet.LastRowNum; i++)
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
                        var chitieudanhso = Utility.GetNumbericCellValue(row.GetCell(4));
                        var chitieudanhthu = Utility.GetNumbericCellValue(row.GetCell(5));
                        var chitieudophu = Utility.GetNumbericCellValue(row.GetCell(6));
                        var chitieumomoi = Utility.GetNumbericCellValue(row.GetCell(7));
                        var chitieunganhhang = Convert.ToInt32(Utility.GetNumbericCellValue(row.GetCell(8)) * 100);
                        var thuchiendoanhso = Utility.GetNumbericCellValue(row.GetCell(9));
                        var thuchiendoanhthu = Utility.GetNumbericCellValue(row.GetCell(10));
                        var thuchiendophu = Utility.GetNumbericCellValue(row.GetCell(11));
                        var thuchienmomoi = Utility.GetNumbericCellValue(row.GetCell(12));
                        var thuchiennganhhang = Convert.ToInt32(Utility.GetNumbericCellValue(row.GetCell(13)) * 100);
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
                            var existEntity = dbContext.SaleKPIEmployees.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (existEntity != null)
                            {
                                var salekpiemployee = new SaleKPIEmployee
                                {
                                    Id = existEntity.Id,
                                    Year = year,
                                    Month = month,
                                    EmployeeId = employee.Id,
                                    MaNhanVien = employee.CodeOld,
                                    FullName = employee.FullName,
                                    ChucVu = employee.SaleChucVu,
                                    ChiTieuDoanhSo = (decimal)chitieudanhso,
                                    ChiTieuDoanhThu = (decimal)chitieudanhthu,
                                    ChiTieuDoPhu = (decimal)chitieudophu,
                                    ChiTieuMoMoi = (decimal)chitieumomoi,
                                    ChiTieuNganhHang = chitieunganhhang,
                                    ThucHienDoanhSo = (decimal)thuchiendoanhso,
                                    ThucHienDoanhThu = (decimal)thuchiendoanhthu,
                                    ThucHienDoPhu = (decimal)thuchiendophu,
                                    ThucHienMoMoi = (decimal)thuchienmomoi,
                                    ThucHienNganhHang = thuchiennganhhang
                                };
                                var itemFull = GetSaleKPIEmployee(salekpiemployee, month + "-" + year);
                                var builder = Builders<SaleKPIEmployee>.Filter;
                                var filter = builder.Eq(m => m.Id, itemFull.Id);
                                var update = Builders<SaleKPIEmployee>.Update
                                    .Set(m => m.ChiTieuDoanhSo, itemFull.ChiTieuDoanhSo)
                                    .Set(m => m.ChiTieuDoanhThu, itemFull.ChiTieuDoanhThu)
                                    .Set(m => m.ChiTieuDoPhu, itemFull.ChiTieuDoPhu)
                                    .Set(m => m.ChiTieuMoMoi, itemFull.ChiTieuMoMoi)
                                    .Set(m => m.ChiTieuNganhHang, itemFull.ChiTieuNganhHang)
                                    .Set(m => m.ThucHienDoanhSo, itemFull.ThucHienDoanhSo)
                                    .Set(m => m.ThucHienDoanhThu, itemFull.ThucHienDoanhThu)
                                    .Set(m => m.ThucHienDoPhu, itemFull.ThucHienDoPhu)
                                    .Set(m => m.ThucHienMoMoi, itemFull.ThucHienMoMoi)
                                    .Set(m => m.ThucHienNganhHang, itemFull.ThucHienNganhHang)
                                    .Set(m => m.ChiTieuThucHienDoanhSo, itemFull.ChiTieuThucHienDoanhSo)
                                    .Set(m => m.ChiTieuThucHienDoanhThu, itemFull.ChiTieuThucHienDoanhThu)
                                    .Set(m => m.ChiTieuThucHienDoPhu, itemFull.ChiTieuThucHienDoPhu)
                                    .Set(m => m.ChiTieuThucHienMoMoi, itemFull.ChiTieuThucHienMoMoi)
                                    .Set(m => m.ChiTieuThucHienNganhHang, itemFull.ChiTieuThucHienNganhHang)
                                    .Set(m => m.ThuongChiTieuThucHienDoanhSo, itemFull.ThuongChiTieuThucHienDoanhSo)
                                    .Set(m => m.ThuongChiTieuThucHienDoanhThu, itemFull.ThuongChiTieuThucHienDoanhThu)
                                    .Set(m => m.ThuongChiTieuThucHienDoPhu, itemFull.ThuongChiTieuThucHienDoPhu)
                                    .Set(m => m.ThuongChiTieuThucHienMoMoi, itemFull.ThuongChiTieuThucHienMoMoi)
                                    .Set(m => m.ThuongChiTieuThucHienNganhHang, itemFull.ThuongChiTieuThucHienNganhHang)
                                    .Set(m => m.TongThuong, itemFull.TongThuong)
                                    .Set(m => m.ThuViec, itemFull.ThuViec)
                                    .Set(m => m.UpdatedOn, DateTime.Now);
                                dbContext.SaleKPIEmployees.UpdateOne(filter, update);
                            }
                            else
                            {
                                var salekpiemployee = new SaleKPIEmployee
                                {
                                    Year = year,
                                    Month = month,
                                    EmployeeId = employee.Id,
                                    MaNhanVien = employee.CodeOld,
                                    FullName = employee.FullName,
                                    ChucVu = employee.SaleChucVu,
                                    ChiTieuDoanhSo = (decimal)chitieudanhso,
                                    ChiTieuDoanhThu = (decimal)chitieudanhthu,
                                    ChiTieuDoPhu = (decimal)chitieudophu,
                                    ChiTieuMoMoi = (decimal)chitieumomoi,
                                    ChiTieuNganhHang = chitieunganhhang,
                                    ThucHienDoanhSo = (decimal)thuchiendoanhso,
                                    ThucHienDoanhThu = (decimal)thuchiendoanhthu,
                                    ThucHienDoPhu = (decimal)thuchiendophu,
                                    ThucHienMoMoi = (decimal)thuchienmomoi,
                                    ThucHienNganhHang = thuchiennganhhang
                                };
                                var fullEntity = GetSaleKPIEmployee(salekpiemployee, month + "-" + year);
                                dbContext.SaleKPIEmployees.InsertOne(fullEntity);
                            }
                        }
                        else
                        {
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "sale-kpi-import",
                                Object = code + "-" + fullName + "-" + title + " ,dòng " + i,
                                Error = "No import data",
                                DateTime = DateTime.Now.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.SaleKPIEmployee + "/" + Constants.LinkSalary.Update });
        }

        [Route(Constants.LinkSalary.SaleKPIEmployeeTemplate)]
        public async Task<IActionResult> SaleKPIEmployeeTemplate(string fileName)
        {
            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"du-lieu-kinh-doanh-thang-" + DateTime.Now.Month + "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
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

                ISheet sheet1 = workbook.CreateSheet("KPI T" + DateTime.Now.Month.ToString("00"));
                sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 3, 7));
                sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 8, 13));

                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("KPI");
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
                var cell = row.CreateCell(3, CellType.String);
                cell.SetCellValue("Chỉ tiêu");
                //CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.ALIGNMENT, HorizontalAlignment.Center);
                cell = row.CreateCell(8, CellType.String);
                cell.SetCellValue("Thực hiện");
                //CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.ALIGNMENT, HorizontalAlignment.Center);

                //row.CreateCell(4, CellType.String).SetCellValue("Chỉ tiêu");
                //row.CreateCell(9, CellType.String).SetCellValue("Thực hiện");
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("#");
                row.CreateCell(1, CellType.String).SetCellValue("Mã");
                row.CreateCell(2, CellType.String).SetCellValue("Họ tên");
                row.CreateCell(3, CellType.String).SetCellValue("Chức vụ sale");
                row.CreateCell(4, CellType.String).SetCellValue("Doanh số");
                row.CreateCell(5, CellType.String).SetCellValue("Doanh thu");
                row.CreateCell(6, CellType.String).SetCellValue("Độ phủ");
                row.CreateCell(7, CellType.String).SetCellValue("Mở mới");
                row.CreateCell(8, CellType.String).SetCellValue("Ngành hàng");
                row.CreateCell(9, CellType.String).SetCellValue("Doanh số");
                row.CreateCell(10, CellType.String).SetCellValue("Doanh thu");
                row.CreateCell(11, CellType.String).SetCellValue("Độ phủ");
                row.CreateCell(12, CellType.String).SetCellValue("Mở mới");
                row.CreateCell(13, CellType.String).SetCellValue("Ngành hàng");
                // Set style
                for (int i = 0; i <= 13; i++)
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
                    row.CreateCell(10, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(11, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(12, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(13, CellType.Numeric).SetCellValue(string.Empty);
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

        #endregion

        #region LOGISTICS
        [Route(Constants.LinkSalary.LogisticEmployeeCong)]
        public async Task<IActionResult> LogisticEmployeeCong(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongVP, (int)ERights.View)))
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

            var giachuyenxes = GetLogisticGiaChuyenXe(month, year);

            var logisticEmployeeCongs = GetLogisticEmployeeCongs(month, year);

            decimal dongiabun = GetLogisticGiaBun(thang);

            var viewModel = new BangLuongViewModel
            {
                LogisticGiaChuyenXes = giachuyenxes,
                LogisticEmployeeCongs = logisticEmployeeCongs,
                MonthYears = sortTimes,
                Thang = thang,
                DonGiaBun = dongiabun
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.LogisticEmployeeCong + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> LogisticEmployeeCongUpdate(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongVP, (int)ERights.View)))
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

            var giachuyenxes = GetLogisticGiaChuyenXe(month, year);

            var logisticEmployeeCongs = GetLogisticEmployeeCongs(month, year);

            decimal dongiabun = GetLogisticGiaBun(thang);

            var viewModel = new BangLuongViewModel
            {
                LogisticGiaChuyenXes = giachuyenxes,
                LogisticEmployeeCongs = logisticEmployeeCongs,
                MonthYears = sortTimes,
                Thang = thang,
                DonGiaBun = dongiabun
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.LogisticEmployeeCong + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> LogisticEmployeeCongUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.LogisticEmployeeCongs)
            {
                var itemFull = GetLogisticEmployeeCong(item, item.Month + "-" + item.Year);
                var builder = Builders<LogisticEmployeeCong>.Filter;
                var filter = builder.Eq(m => m.Id, itemFull.Id);
                var update = Builders<LogisticEmployeeCong>.Update
                    .Set(m => m.DoanhThu, itemFull.DoanhThu)
                    .Set(m => m.LuongTheoDoanhThuDoanhSo, itemFull.LuongTheoDoanhThuDoanhSo)
                    .Set(m => m.Chuyen1HcmXeNho, itemFull.Chuyen1HcmXeNho)
                    .Set(m => m.Chuyen2HcmXeNho, itemFull.Chuyen2HcmXeNho)
                    .Set(m => m.Chuyen3HcmXeNho, itemFull.Chuyen3HcmXeNho)
                    .Set(m => m.Chuyen4HcmXeNho, itemFull.Chuyen4HcmXeNho)
                    .Set(m => m.Chuyen5HcmXeNho, itemFull.Chuyen5HcmXeNho)
                    .Set(m => m.Chuyen1HcmXeLon, itemFull.Chuyen1HcmXeLon)
                    .Set(m => m.Chuyen2HcmXeLon, itemFull.Chuyen2HcmXeLon)
                    .Set(m => m.Chuyen3HcmXeLon, itemFull.Chuyen3HcmXeLon)
                    .Set(m => m.Chuyen4HcmXeLon, itemFull.Chuyen4HcmXeLon)
                    .Set(m => m.Chuyen5HcmXeLon, itemFull.Chuyen5HcmXeLon)
                    .Set(m => m.Chuyen1BinhDuongXeNho, itemFull.Chuyen1BinhDuongXeNho)
                    .Set(m => m.Chuyen2BinhDuongXeNho, itemFull.Chuyen2BinhDuongXeNho)
                    .Set(m => m.Chuyen3BinhDuongXeNho, itemFull.Chuyen3BinhDuongXeNho)
                    .Set(m => m.Chuyen4BinhDuongXeNho, itemFull.Chuyen4BinhDuongXeNho)
                    .Set(m => m.Chuyen5BinhDuongXeNho, itemFull.Chuyen5BinhDuongXeNho)
                    .Set(m => m.Chuyen1BinhDuongXeLon, itemFull.Chuyen1BinhDuongXeLon)
                    .Set(m => m.Chuyen2BinhDuongXeLon, itemFull.Chuyen2BinhDuongXeLon)
                    .Set(m => m.Chuyen3BinhDuongXeLon, itemFull.Chuyen3BinhDuongXeLon)
                    .Set(m => m.Chuyen4BinhDuongXeLon, itemFull.Chuyen4BinhDuongXeLon)
                    .Set(m => m.Chuyen5BinhDuongXeLon, itemFull.Chuyen5BinhDuongXeLon)
                    .Set(m => m.Chuyen1BienHoaXeNho, itemFull.Chuyen1BienHoaXeNho)
                    .Set(m => m.Chuyen2BienHoaXeNho, itemFull.Chuyen2BienHoaXeNho)
                    .Set(m => m.Chuyen3BienHoaXeNho, itemFull.Chuyen3BienHoaXeNho)
                    .Set(m => m.Chuyen4BienHoaXeNho, itemFull.Chuyen4BienHoaXeNho)
                    .Set(m => m.Chuyen5BienHoaXeNho, itemFull.Chuyen5BienHoaXeNho)
                    .Set(m => m.Chuyen1BienHoaXeLon, itemFull.Chuyen1BienHoaXeLon)
                    .Set(m => m.Chuyen2BienHoaXeLon, itemFull.Chuyen2BienHoaXeLon)
                    .Set(m => m.Chuyen3BienHoaXeLon, itemFull.Chuyen3BienHoaXeLon)
                    .Set(m => m.Chuyen4BienHoaXeLon, itemFull.Chuyen4BienHoaXeLon)
                    .Set(m => m.Chuyen5BienHoaXeLon, itemFull.Chuyen5BienHoaXeLon)
                    .Set(m => m.VungTauXeNho, itemFull.VungTauXeNho)
                    .Set(m => m.VungTauXeLon, itemFull.VungTauXeLon)
                    .Set(m => m.BinhThuanXeNho, itemFull.BinhThuanXeNho)
                    .Set(m => m.BinhThuanXeLon, itemFull.BinhThuanXeLon)
                    .Set(m => m.CanThoXeLon, itemFull.CanThoXeLon)
                    .Set(m => m.VinhLongXeLon, itemFull.VinhLongXeLon)
                    .Set(m => m.LongAnXeNho, itemFull.LongAnXeNho)
                    .Set(m => m.LongAnXeLon, itemFull.LongAnXeLon)
                    .Set(m => m.TienGiangXeNho, itemFull.TienGiangXeNho)
                    .Set(m => m.TienGiangXeLon, itemFull.TienGiangXeLon)
                    .Set(m => m.DongNaiXeNho, itemFull.DongNaiXeNho)
                    .Set(m => m.DongNaiXeLon, itemFull.DongNaiXeLon)
                    .Set(m => m.TongSoChuyen, itemFull.TongSoChuyen)
                    .Set(m => m.TienChuyen, itemFull.TienChuyen)
                    .Set(m => m.CongTacXa, itemFull.CongTacXa)
                    .Set(m => m.KhoiLuongBun, itemFull.KhoiLuongBun)
                    .Set(m => m.ThanhTienBun, itemFull.ThanhTienBun)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.LogisticEmployeeCongs.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        [Route(Constants.LinkSalary.LogisticGiaChuyenXe + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> LogisticGiaChuyenXeUpdate(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongVP, (int)ERights.View)))
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

            var giachuyenxes = GetLogisticGiaChuyenXe(month, year);

            var viewModel = new BangLuongViewModel()
            {
                LogisticGiaChuyenXes = giachuyenxes,
                MonthYears = sortTimes,
                Thang = thang
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.LogisticGiaChuyenXe + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> LogisticGiaChuyenXeUpdate(BangLuongViewModel viewModel)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongVP, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            #endregion

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

            #region DonGiaBun
            var dongiabun = viewModel.DonGiaBun;
            var existbun = dbContext.LogisticGiaBuns.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (existbun != null)
            {
                var builderB = Builders<LogisticGiaBun>.Filter;
                var filterB = builderB.Eq(m => m.Id, existbun.Id);
                var updateB = Builders<LogisticGiaBun>.Update
                    .Set(m => m.Price, dongiabun);
                dbContext.LogisticGiaBuns.UpdateOne(filterB, updateB);
            }
            else
            {
                dbContext.LogisticGiaBuns.InsertOne(new LogisticGiaBun()
                {
                    Month = month,
                    Year = year,
                    Name = "Bùn",
                    Alias = "bun",
                    Code = "BUN",
                    Price = dongiabun
                });
            }
            #endregion

            foreach (var item in viewModel.LogisticGiaChuyenXes)
            {
                if (!string.IsNullOrEmpty(item.Id))
                {
                    var builder = Builders<LogisticGiaChuyenXe>.Filter;
                    var filter = builder.Eq(m => m.Id, item.Id);
                    var update = Builders<LogisticGiaChuyenXe>.Update
                        .Set(m => m.LuongNangSuatChuyenCom, item.LuongNangSuatChuyenCom)
                        .Set(m => m.HoTroTienComTinh, item.HoTroTienComTinh)
                        .Set(m => m.LuongNangSuatChuyen, item.LuongNangSuatChuyen)
                        .Set(m => m.Chuyen1, item.Chuyen1)
                        .Set(m => m.Chuyen2, item.Chuyen2)
                        .Set(m => m.Chuyen3, item.Chuyen3)
                        .Set(m => m.Chuyen4, item.Chuyen4)
                        .Set(m => m.Chuyen5, item.Chuyen5)
                        .Set(m => m.HoTroChuyenDem, item.HoTroChuyenDem)
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    dbContext.LogisticGiaChuyenXes.UpdateOne(filter, update);
                }
                else
                {
                    item.Month = month;
                    item.Year = year;
                    dbContext.LogisticGiaChuyenXes.InsertOne(item);
                }
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        [Route(Constants.LinkSalary.LogisticEmployeeCongCalculator)]
        public IActionResult LogisticEmployeeCongCalculator(BangLuongViewModel viewModel)
        {
            if (viewModel.LogisticEmployeeCongs != null)
            {
                var entity = viewModel.LogisticEmployeeCongs.First();
                string thang = entity.Month + "-" + entity.Year;
                var returnEntity = GetLogisticEmployeeCong(entity, thang);
                return Json(new { result = true, entity = returnEntity });
            }
            else
            {
                return Json(new { result = false });
            }
        }

        public LogisticEmployeeCong GetLogisticEmployeeCong(LogisticEmployeeCong newData, string thang)
        {
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

            var prices = GetLogisticGiaChuyenXe(month, year);
            var priceHOCHIMINHN = prices.Find(m => m.TuyenCode.Equals("HOCHIMINH") && m.LoaiXeCode.Equals("XN"));
            var priceHOCHIMINHL = prices.Find(m => m.TuyenCode.Equals("HOCHIMINH") && m.LoaiXeCode.Equals("XL"));
            var priceBINHDUONGN = prices.Find(m => m.TuyenCode.Equals("BINHDUONG") && m.LoaiXeCode.Equals("XN"));
            var priceBINHDUONGL = prices.Find(m => m.TuyenCode.Equals("BINHDUONG") && m.LoaiXeCode.Equals("XL"));
            var priceBIENHOAN = prices.Find(m => m.TuyenCode.Equals("BIENHOA") && m.LoaiXeCode.Equals("XN"));
            var priceBIENHOAL = prices.Find(m => m.TuyenCode.Equals("BIENHOA") && m.LoaiXeCode.Equals("XL"));
            var priceVUNGTAUN = prices.Find(m => m.TuyenCode.Equals("VUNGTAU") && m.LoaiXeCode.Equals("XN"));
            var priceVUNGTAUL = prices.Find(m => m.TuyenCode.Equals("VUNGTAU") && m.LoaiXeCode.Equals("XL"));
            var priceBINHTHUANN = prices.Find(m => m.TuyenCode.Equals("BINHTHUAN") && m.LoaiXeCode.Equals("XN"));
            var priceBINHTHUANL = prices.Find(m => m.TuyenCode.Equals("BINHTHUAN") && m.LoaiXeCode.Equals("XL"));
            var priceCANTHON = prices.Find(m => m.TuyenCode.Equals("CANTHO") && m.LoaiXeCode.Equals("XN"));
            var priceCANTHOL = prices.Find(m => m.TuyenCode.Equals("CANTHO") && m.LoaiXeCode.Equals("XL"));
            var priceVINHLONGN = prices.Find(m => m.TuyenCode.Equals("VINHLONG") && m.LoaiXeCode.Equals("XN"));
            var priceVINHLONGL = prices.Find(m => m.TuyenCode.Equals("VINHLONG") && m.LoaiXeCode.Equals("XL"));
            var priceLONGANN = prices.Find(m => m.TuyenCode.Equals("LONGAN") && m.LoaiXeCode.Equals("XN"));
            var priceLONGANL = prices.Find(m => m.TuyenCode.Equals("LONGAN") && m.LoaiXeCode.Equals("XL"));
            var priceTIENGIANGN = prices.Find(m => m.TuyenCode.Equals("TIENGIANG") && m.LoaiXeCode.Equals("XN"));
            var priceTIENGIANGL = prices.Find(m => m.TuyenCode.Equals("TIENGIANG") && m.LoaiXeCode.Equals("XL"));
            var priceDONGNAIN = prices.Find(m => m.TuyenCode.Equals("DONGNAI") && m.LoaiXeCode.Equals("XN"));
            var priceDONGNAIL = prices.Find(m => m.TuyenCode.Equals("DONGNAI") && m.LoaiXeCode.Equals("XL"));

            decimal LuongTheoDoanhThuDoanhSo = newData.DoanhThu * ((decimal)1.2 / 100);
            decimal Chuyen1HcmXeNho = newData.Chuyen1HcmXeNho;
            decimal Chuyen2HcmXeNho = newData.Chuyen2HcmXeNho;
            decimal Chuyen3HcmXeNho = newData.Chuyen3HcmXeNho;
            decimal Chuyen4HcmXeNho = newData.Chuyen4HcmXeNho;
            decimal Chuyen5HcmXeNho = newData.Chuyen5HcmXeNho;
            decimal Chuyen1HcmXeLon = newData.Chuyen1HcmXeLon;
            decimal Chuyen2HcmXeLon = newData.Chuyen2HcmXeLon;
            decimal Chuyen3HcmXeLon = newData.Chuyen3HcmXeLon;
            decimal Chuyen4HcmXeLon = newData.Chuyen4HcmXeLon;
            decimal Chuyen5HcmXeLon = newData.Chuyen5HcmXeLon;
            decimal Chuyen1BinhDuongXeNho = newData.Chuyen1BinhDuongXeNho;
            decimal Chuyen2BinhDuongXeNho = newData.Chuyen2BinhDuongXeNho;
            decimal Chuyen3BinhDuongXeNho = newData.Chuyen3BinhDuongXeNho;
            decimal Chuyen4BinhDuongXeNho = newData.Chuyen4BinhDuongXeNho;
            decimal Chuyen5BinhDuongXeNho = newData.Chuyen5BinhDuongXeNho;
            decimal Chuyen1BinhDuongXeLon = newData.Chuyen1BinhDuongXeLon;
            decimal Chuyen2BinhDuongXeLon = newData.Chuyen2BinhDuongXeLon;
            decimal Chuyen3BinhDuongXeLon = newData.Chuyen3BinhDuongXeLon;
            decimal Chuyen4BinhDuongXeLon = newData.Chuyen4BinhDuongXeLon;
            decimal Chuyen5BinhDuongXeLon = newData.Chuyen5BinhDuongXeLon;
            decimal Chuyen1BienHoaXeNho = newData.Chuyen1BienHoaXeNho;
            decimal Chuyen2BienHoaXeNho = newData.Chuyen2BienHoaXeNho;
            decimal Chuyen3BienHoaXeNho = newData.Chuyen3BienHoaXeNho;
            decimal Chuyen4BienHoaXeNho = newData.Chuyen4BienHoaXeNho;
            decimal Chuyen5BienHoaXeNho = newData.Chuyen5BienHoaXeNho;
            decimal Chuyen1BienHoaXeLon = newData.Chuyen1BienHoaXeLon;
            decimal Chuyen2BienHoaXeLon = newData.Chuyen2BienHoaXeLon;
            decimal Chuyen3BienHoaXeLon = newData.Chuyen3BienHoaXeLon;
            decimal Chuyen4BienHoaXeLon = newData.Chuyen4BienHoaXeLon;
            decimal Chuyen5BienHoaXeLon = newData.Chuyen5BienHoaXeLon;
            decimal VungTauXeNho = newData.VungTauXeNho;
            decimal VungTauXeLon = newData.VungTauXeLon;
            decimal BinhThuanXeNho = newData.BinhThuanXeNho;
            decimal BinhThuanXeLon = newData.BinhThuanXeLon;
            decimal CanTHoXeLon = newData.CanThoXeLon;
            decimal VinhLongXeLon = newData.VinhLongXeLon;
            decimal LongAnXeNho = newData.LongAnXeNho;
            decimal LongAnXeLon = newData.LongAnXeLon;
            decimal TienGiangXeNho = newData.TienGiangXeNho;
            decimal TienGiangXeLon = newData.TienGiangXeLon;
            decimal DongNaiXeNho = newData.DongNaiXeNho;
            decimal DongNaiXeLon = newData.DongNaiXeLon;

            decimal TongSoChuyen = 0;
            TongSoChuyen += Chuyen1HcmXeNho;
            TongSoChuyen += Chuyen2HcmXeNho;
            TongSoChuyen += Chuyen3HcmXeNho;
            TongSoChuyen += Chuyen4HcmXeNho;
            TongSoChuyen += Chuyen5HcmXeNho;
            TongSoChuyen += Chuyen1HcmXeLon;
            TongSoChuyen += Chuyen2HcmXeLon;
            TongSoChuyen += Chuyen3HcmXeLon;
            TongSoChuyen += Chuyen4HcmXeLon;
            TongSoChuyen += Chuyen5HcmXeLon;
            TongSoChuyen += Chuyen1BinhDuongXeNho;
            TongSoChuyen += Chuyen2BinhDuongXeNho;
            TongSoChuyen += Chuyen3BinhDuongXeNho;
            TongSoChuyen += Chuyen4BinhDuongXeNho;
            TongSoChuyen += Chuyen5BinhDuongXeNho;
            TongSoChuyen += Chuyen1BinhDuongXeLon;
            TongSoChuyen += Chuyen2BinhDuongXeLon;
            TongSoChuyen += Chuyen3BinhDuongXeLon;
            TongSoChuyen += Chuyen4BinhDuongXeLon;
            TongSoChuyen += Chuyen5BinhDuongXeLon;
            TongSoChuyen += Chuyen1BienHoaXeNho;
            TongSoChuyen += Chuyen2BienHoaXeNho;
            TongSoChuyen += Chuyen3BienHoaXeNho;
            TongSoChuyen += Chuyen4BienHoaXeNho;
            TongSoChuyen += Chuyen5BienHoaXeNho;
            TongSoChuyen += Chuyen1BienHoaXeLon;
            TongSoChuyen += Chuyen2BienHoaXeLon;
            TongSoChuyen += Chuyen3BienHoaXeLon;
            TongSoChuyen += Chuyen4BienHoaXeLon;
            TongSoChuyen += Chuyen5BienHoaXeLon;
            TongSoChuyen += VungTauXeNho;
            TongSoChuyen += VungTauXeLon;
            TongSoChuyen += BinhThuanXeNho;
            TongSoChuyen += BinhThuanXeLon;
            TongSoChuyen += CanTHoXeLon;
            TongSoChuyen += VinhLongXeLon;
            TongSoChuyen += LongAnXeNho;
            TongSoChuyen += LongAnXeLon;
            TongSoChuyen += TienGiangXeNho;
            TongSoChuyen += TienGiangXeLon;
            TongSoChuyen += DongNaiXeNho;
            TongSoChuyen += DongNaiXeLon;

            decimal TienChuyen = 0;
            TienChuyen += Chuyen1HcmXeNho * priceHOCHIMINHN.Chuyen1;
            TienChuyen += Chuyen2HcmXeNho * priceHOCHIMINHN.Chuyen2;
            TienChuyen += Chuyen3HcmXeNho * priceHOCHIMINHN.Chuyen3;
            TienChuyen += Chuyen4HcmXeNho * priceHOCHIMINHN.Chuyen4;
            TienChuyen += Chuyen5HcmXeNho * priceHOCHIMINHN.Chuyen5;
            TienChuyen += Chuyen1HcmXeLon * priceHOCHIMINHL.Chuyen1;
            TienChuyen += Chuyen2HcmXeLon * priceHOCHIMINHL.Chuyen2;
            TienChuyen += Chuyen3HcmXeLon * priceHOCHIMINHL.Chuyen3;
            TienChuyen += Chuyen4HcmXeLon * priceHOCHIMINHL.Chuyen4;
            TienChuyen += Chuyen5HcmXeLon * priceHOCHIMINHL.Chuyen5;

            TienChuyen += Chuyen1BinhDuongXeNho * priceBINHDUONGN.Chuyen1;
            TienChuyen += Chuyen2BinhDuongXeNho * priceBINHDUONGN.Chuyen2;
            TienChuyen += Chuyen3BinhDuongXeNho * priceBINHDUONGN.Chuyen3;
            TienChuyen += Chuyen4BinhDuongXeNho * priceBINHDUONGN.Chuyen4;
            TienChuyen += Chuyen5BinhDuongXeNho * priceBINHDUONGN.Chuyen5;
            TienChuyen += Chuyen1BinhDuongXeLon * priceBINHDUONGL.Chuyen1;
            TienChuyen += Chuyen2BinhDuongXeLon * priceBINHDUONGL.Chuyen2;
            TienChuyen += Chuyen3BinhDuongXeLon * priceBINHDUONGL.Chuyen3;
            TienChuyen += Chuyen4BinhDuongXeLon * priceBINHDUONGL.Chuyen4;
            TienChuyen += Chuyen5BinhDuongXeLon * priceBINHDUONGL.Chuyen5;

            TienChuyen += Chuyen1BienHoaXeNho * priceBIENHOAN.Chuyen1;
            TienChuyen += Chuyen2BienHoaXeNho * priceBIENHOAN.Chuyen2;
            TienChuyen += Chuyen3BienHoaXeNho * priceBIENHOAN.Chuyen3;
            TienChuyen += Chuyen4BienHoaXeNho * priceBIENHOAN.Chuyen4;
            TienChuyen += Chuyen5BienHoaXeNho * priceBIENHOAN.Chuyen5;
            TienChuyen += Chuyen1BienHoaXeLon * priceBIENHOAL.Chuyen1;
            TienChuyen += Chuyen2BienHoaXeLon * priceBIENHOAL.Chuyen2;
            TienChuyen += Chuyen3BienHoaXeLon * priceBIENHOAL.Chuyen3;
            TienChuyen += Chuyen4BienHoaXeLon * priceBIENHOAL.Chuyen4;
            TienChuyen += Chuyen5BienHoaXeLon * priceBIENHOAL.Chuyen5;

            TienChuyen += VungTauXeNho * priceVUNGTAUN.Chuyen2;
            TienChuyen += VungTauXeLon * priceVUNGTAUL.Chuyen2;
            TienChuyen += BinhThuanXeNho * priceBINHTHUANN.Chuyen2;
            TienChuyen += BinhThuanXeLon * priceBINHTHUANL.Chuyen2;

            TienChuyen += CanTHoXeLon * priceCANTHOL.Chuyen2;
            TienChuyen += VinhLongXeLon * priceVINHLONGL.Chuyen2;
            TienChuyen += LongAnXeNho * priceLONGANN.Chuyen2;
            TienChuyen += LongAnXeLon * priceLONGANL.Chuyen2;
            TienChuyen += TienGiangXeNho * priceTIENGIANGN.Chuyen2;
            TienChuyen += TienGiangXeLon * priceTIENGIANGL.Chuyen2;
            TienChuyen += DongNaiXeNho * priceDONGNAIN.Chuyen2;
            TienChuyen += DongNaiXeLon * priceDONGNAIL.Chuyen2;

            if (newData.ChucVu != "Tài xế")
            {
                TienChuyen = 0;
            }
            decimal CongTacXa = newData.CongTacXa;
            CongTacXa += Chuyen1HcmXeNho * priceHOCHIMINHN.HoTroTienComTinh;
            CongTacXa += Chuyen2HcmXeNho * priceHOCHIMINHN.HoTroTienComTinh;
            CongTacXa += Chuyen3HcmXeNho * priceHOCHIMINHN.HoTroTienComTinh;
            CongTacXa += Chuyen4HcmXeNho * priceHOCHIMINHN.HoTroTienComTinh;
            CongTacXa += Chuyen5HcmXeNho * priceHOCHIMINHN.HoTroTienComTinh;
            CongTacXa += Chuyen1HcmXeLon * priceHOCHIMINHL.HoTroTienComTinh;
            CongTacXa += Chuyen2HcmXeLon * priceHOCHIMINHL.HoTroTienComTinh;
            CongTacXa += Chuyen3HcmXeLon * priceHOCHIMINHL.HoTroTienComTinh;
            CongTacXa += Chuyen4HcmXeLon * priceHOCHIMINHL.HoTroTienComTinh;
            CongTacXa += Chuyen5HcmXeLon * priceHOCHIMINHL.HoTroTienComTinh;

            CongTacXa += Chuyen1BinhDuongXeNho * priceBINHDUONGN.HoTroTienComTinh;
            CongTacXa += Chuyen2BinhDuongXeNho * priceBINHDUONGN.HoTroTienComTinh;
            CongTacXa += Chuyen3BinhDuongXeNho * priceBINHDUONGN.HoTroTienComTinh;
            CongTacXa += Chuyen4BinhDuongXeNho * priceBINHDUONGN.HoTroTienComTinh;
            CongTacXa += Chuyen5BinhDuongXeNho * priceBINHDUONGN.HoTroTienComTinh;
            CongTacXa += Chuyen1BinhDuongXeLon * priceBINHDUONGL.HoTroTienComTinh;
            CongTacXa += Chuyen2BinhDuongXeLon * priceBINHDUONGL.HoTroTienComTinh;
            CongTacXa += Chuyen3BinhDuongXeLon * priceBINHDUONGL.HoTroTienComTinh;
            CongTacXa += Chuyen4BinhDuongXeLon * priceBINHDUONGL.HoTroTienComTinh;
            CongTacXa += Chuyen5BinhDuongXeLon * priceBINHDUONGL.HoTroTienComTinh;

            CongTacXa += Chuyen1BienHoaXeNho * priceBIENHOAN.HoTroTienComTinh;
            CongTacXa += Chuyen2BienHoaXeNho * priceBIENHOAN.HoTroTienComTinh;
            CongTacXa += Chuyen3BienHoaXeNho * priceBIENHOAN.HoTroTienComTinh;
            CongTacXa += Chuyen4BienHoaXeNho * priceBIENHOAN.HoTroTienComTinh;
            CongTacXa += Chuyen5BienHoaXeNho * priceBIENHOAN.HoTroTienComTinh;
            CongTacXa += Chuyen1BienHoaXeLon * priceBIENHOAL.HoTroTienComTinh;
            CongTacXa += Chuyen2BienHoaXeLon * priceBIENHOAL.HoTroTienComTinh;
            CongTacXa += Chuyen3BienHoaXeLon * priceBIENHOAL.HoTroTienComTinh;
            CongTacXa += Chuyen4BienHoaXeLon * priceBIENHOAL.HoTroTienComTinh;
            CongTacXa += Chuyen5BienHoaXeLon * priceBIENHOAL.HoTroTienComTinh;

            CongTacXa += VungTauXeNho * priceVUNGTAUN.HoTroTienComTinh;
            CongTacXa += VungTauXeLon * priceVUNGTAUL.HoTroTienComTinh;
            CongTacXa += BinhThuanXeNho * priceBINHTHUANN.HoTroTienComTinh;
            CongTacXa += BinhThuanXeLon * priceBINHTHUANL.HoTroTienComTinh;

            CongTacXa += CanTHoXeLon * priceCANTHOL.HoTroTienComTinh;
            CongTacXa += VinhLongXeLon * priceVINHLONGL.HoTroTienComTinh;
            CongTacXa += LongAnXeNho * priceLONGANN.HoTroTienComTinh;
            CongTacXa += LongAnXeLon * priceLONGANL.HoTroTienComTinh;
            CongTacXa += TienGiangXeNho * priceTIENGIANGN.HoTroTienComTinh;
            CongTacXa += TienGiangXeLon * priceTIENGIANGL.HoTroTienComTinh;
            CongTacXa += DongNaiXeNho * priceDONGNAIN.HoTroTienComTinh;
            CongTacXa += DongNaiXeLon * priceDONGNAIL.HoTroTienComTinh;

            double KhoiLuongBun = newData.KhoiLuongBun;
            decimal dongiabun = GetLogisticGiaBun(thang);
            decimal ThanhTienBun = (decimal)KhoiLuongBun * dongiabun;

            newData.LuongTheoDoanhThuDoanhSo = Math.Round(LuongTheoDoanhThuDoanhSo, 0);
            newData.TongSoChuyen = TongSoChuyen;
            newData.TienChuyen = Math.Round(TienChuyen, 0);
            newData.CongTacXa = Math.Round(CongTacXa, 0);
            newData.ThanhTienBun = Math.Round(ThanhTienBun, 0);

            return newData;
        }

        public decimal GetLogisticGiaBun(string thang)
        {
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

            var existbun = dbContext.LogisticGiaBuns.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (existbun != null)
            {
                return existbun.Price;
            }
            else
            {
                var lastest = dbContext.LogisticGiaBuns.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
                if (lastest != null)
                {
                    lastest.Id = null;
                    lastest.Month = month;
                    lastest.Year = year;
                    dbContext.LogisticGiaBuns.InsertOne(lastest);
                    return lastest.Price;
                }
                else
                {
                    return 0;
                }
            }
        }

        private List<LogisticGiaChuyenXe> GetLogisticGiaChuyenXe(int month, int year)
        {
            var datas = dbContext.LogisticGiaChuyenXes.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).ToList();
            if (datas.Count == 0)
            {
                var checkLast = dbContext.LogisticGiaChuyenXes.CountDocuments(m => m.Enable.Equals(true));
                if (checkLast == 0)
                {
                    // insert empty value
                    var locations = dbContext.LogisticsLocations.Find(m => m.Enable.Equals(true)).ToList();
                    if (locations.Count == 0)
                    {
                        locations = GetLogisticsLocations();
                    }
                    var xes = dbContext.LogisticsLoaiXes.Find(m => m.Enable.Equals(true)).ToList();
                    if (xes.Count == 0)
                    {
                        xes = GetLogisticsLoaiXes();
                    }
                    foreach (var location in locations)
                    {
                        foreach (var xe in xes)
                        {
                            dbContext.LogisticGiaChuyenXes.InsertOne(new LogisticGiaChuyenXe
                            {
                                Year = year,
                                Month = month,
                                Tuyen = location.Name,
                                TuyenAlias = location.Alias,
                                TuyenCode = location.Code,
                                LoaiXe = xe.Name,
                                LoaiXeAlias = xe.Alias,
                                LoaiXeCode = xe.Code
                            });
                        }
                    }
                }
                else
                {
                    // insert lastest value for new data.
                    var lastKpi = dbContext.LogisticGiaChuyenXes.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
                    var lastMonth = lastKpi.Month;
                    var lastYear = lastKpi.Year;
                    var lastKpis = dbContext.LogisticGiaChuyenXes.Find(m => m.Enable.Equals(true) && m.Month.Equals(lastMonth) && m.Year.Equals(lastYear)).ToList();
                    foreach (var item in lastKpis)
                    {
                        item.Id = null;
                        item.Year = year;
                        item.Month = month;
                        dbContext.LogisticGiaChuyenXes.InsertOne(item);
                    }
                }
                datas = dbContext.LogisticGiaChuyenXes.Find(m => m.Enable.Equals(true) & m.Month.Equals(month) & m.Year.Equals(year)).ToList();
            }
            return datas;
        }

        private List<LogisticEmployeeCong> GetLogisticEmployeeCongs(int month, int year)
        {
            var datas = dbContext.LogisticEmployeeCongs.Find(m => m.Enable.Equals(true) && m.Year.Equals(year) && m.Month.Equals(month)).ToList();
            if (datas.Count == 0)
            {
                var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && !m.UserName.Equals(Constants.System.account)
                 && (m.CodeOld.Contains("KDG")
                    || m.CodeOld.Contains("KDPX")
                    || m.CodeOld.Contains("KDX")
                    || m.CodeOld.Contains("KDS"))).ToList();
                foreach (var employee in employees)
                {
                    var logisticCong = new LogisticEmployeeCong
                    {
                        Year = year,
                        Month = month,
                        EmployeeId = employee.Id,
                        MaNhanVien = employee.CodeOld,
                        FullName = employee.FullName,
                        ChucVu = employee.LogisticChucVu
                    };
                    var lastestLogisticCong = dbContext.LogisticEmployeeCongs.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employee.Id)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
                    if (lastestLogisticCong != null)
                    {
                        // go to common
                        lastestLogisticCong.Id = null;
                        lastestLogisticCong.Year = year;
                        lastestLogisticCong.Month = month;
                        logisticCong = GetLogisticEmployeeCong(lastestLogisticCong, month + "-" + year);
                    }
                    dbContext.LogisticEmployeeCongs.InsertOne(logisticCong);
                }
                datas = dbContext.LogisticEmployeeCongs.Find(m => m.Enable.Equals(true) && m.Year.Equals(year) && m.Month.Equals(month)).ToList();
            }

            return datas;
        }

        [Route(Constants.LinkSalary.LogisticEmployeeImport)]
        [HttpPost]
        public ActionResult LogisticEmployeeImport()
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
                    var bunrow = sheet.GetRow(2);
                    var dongiabun = (decimal)Utility.GetNumbericCellValue(bunrow.GetCell(1));
                    var existbun = dbContext.LogisticGiaBuns.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                    if (existbun != null)
                    {
                        var builderB = Builders<LogisticGiaBun>.Filter;
                        var filterB = builderB.Eq(m => m.Id, existbun.Id);
                        var updateB = Builders<LogisticGiaBun>.Update
                            .Set(m => m.Price, dongiabun);
                        dbContext.LogisticGiaBuns.UpdateOne(filterB, updateB);
                    }
                    else
                    {
                        dbContext.LogisticGiaBuns.InsertOne(new LogisticGiaBun()
                        {
                            Month = month,
                            Year = year,
                            Name = "Bùn",
                            Alias = "bun",
                            Code = "BUN",
                            Price = dongiabun
                        });
                    }

                    for (int i = 6; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var code = Utility.GetFormattedCellValue(row.GetCell(1));
                        var fullName = Utility.GetFormattedCellValue(row.GetCell(2));
                        var alias = Utility.AliasConvert(fullName);
                        var title = Utility.GetFormattedCellValue(row.GetCell(3)).Trim();

                        var entity = new LogisticEmployeeCong
                        {
                            Year = year,
                            Month = month
                        };

                        entity.DoanhThu = Math.Round((decimal)Utility.GetNumbericCellValue(row.GetCell(4)), 0);
                        entity.Chuyen1HcmXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(5));
                        entity.Chuyen2HcmXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(6));
                        entity.Chuyen3HcmXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(7));
                        entity.Chuyen4HcmXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(8));
                        entity.Chuyen5HcmXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(9));
                        entity.Chuyen1HcmXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(10));
                        entity.Chuyen2HcmXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(11));
                        entity.Chuyen3HcmXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(12));
                        entity.Chuyen4HcmXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(13));
                        entity.Chuyen5HcmXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(14));
                        entity.Chuyen1BinhDuongXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(15));
                        entity.Chuyen2BinhDuongXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(16));
                        entity.Chuyen3BinhDuongXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(17));
                        entity.Chuyen4BinhDuongXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(18));
                        entity.Chuyen5BinhDuongXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(19));
                        entity.Chuyen1BinhDuongXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(20));
                        entity.Chuyen2BinhDuongXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(21));
                        entity.Chuyen3BinhDuongXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(22));
                        entity.Chuyen4BinhDuongXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(23));
                        entity.Chuyen5BinhDuongXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(24));
                        entity.Chuyen1BienHoaXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(25));
                        entity.Chuyen2BienHoaXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(26));
                        entity.Chuyen3BienHoaXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(27));
                        entity.Chuyen4BienHoaXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(28));
                        entity.Chuyen5BienHoaXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(29));
                        entity.Chuyen1BienHoaXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(30));
                        entity.Chuyen2BienHoaXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(31));
                        entity.Chuyen3BienHoaXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(32));
                        entity.Chuyen4BienHoaXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(33));
                        entity.Chuyen5BienHoaXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(34));
                        entity.VungTauXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(35));
                        entity.VungTauXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(36));
                        entity.BinhThuanXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(37));
                        entity.BinhThuanXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(38));
                        entity.CanThoXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(39));
                        entity.VinhLongXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(40));
                        entity.LongAnXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(41));
                        entity.LongAnXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(42));
                        entity.TienGiangXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(43));
                        entity.TienGiangXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(44));
                        entity.DongNaiXeNho = (decimal)Utility.GetNumbericCellValue(row.GetCell(45));
                        entity.DongNaiXeLon = (decimal)Utility.GetNumbericCellValue(row.GetCell(46));
                        entity.KhoiLuongBun = Utility.GetNumbericCellValue(row.GetCell(47));

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
                            entity.EmployeeId = employee.Id;
                            entity.MaNhanVien = employee.CodeOld;
                            entity.FullName = employee.FullName;

                            // Update logistic title
                            var builderE = Builders<Employee>.Filter;
                            var filterE = builderE.Eq(m => m.Id, employee.Id);
                            var updateE = Builders<Employee>.Update
                                .Set(m => m.LogisticChucVu, title);
                            dbContext.Employees.UpdateOne(filterE, updateE);

                            entity.ChucVu = title;
                            var itemFull = GetLogisticEmployeeCong(entity, month + "-" + year);
                            var exist = dbContext.LogisticEmployeeCongs.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (exist != null)
                            {
                                itemFull.Id = exist.Id;
                                var builder = Builders<LogisticEmployeeCong>.Filter;
                                var filter = builder.Eq(m => m.Id, itemFull.Id);
                                var update = Builders<LogisticEmployeeCong>.Update
                                    .Set(m => m.ChucVu, itemFull.ChucVu)
                                    .Set(m => m.DoanhThu, itemFull.DoanhThu)
                                    .Set(m => m.LuongTheoDoanhThuDoanhSo, itemFull.LuongTheoDoanhThuDoanhSo)
                                    .Set(m => m.Chuyen1HcmXeNho, itemFull.Chuyen1HcmXeNho)
                                    .Set(m => m.Chuyen2HcmXeNho, itemFull.Chuyen2HcmXeNho)
                                    .Set(m => m.Chuyen3HcmXeNho, itemFull.Chuyen3HcmXeNho)
                                    .Set(m => m.Chuyen4HcmXeNho, itemFull.Chuyen4HcmXeNho)
                                    .Set(m => m.Chuyen5HcmXeNho, itemFull.Chuyen5HcmXeNho)
                                    .Set(m => m.Chuyen1HcmXeLon, itemFull.Chuyen1HcmXeLon)
                                    .Set(m => m.Chuyen2HcmXeLon, itemFull.Chuyen2HcmXeLon)
                                    .Set(m => m.Chuyen3HcmXeLon, itemFull.Chuyen3HcmXeLon)
                                    .Set(m => m.Chuyen4HcmXeLon, itemFull.Chuyen4HcmXeLon)
                                    .Set(m => m.Chuyen5HcmXeLon, itemFull.Chuyen5HcmXeLon)
                                    .Set(m => m.Chuyen1BinhDuongXeNho, itemFull.Chuyen1BinhDuongXeNho)
                                    .Set(m => m.Chuyen2BinhDuongXeNho, itemFull.Chuyen2BinhDuongXeNho)
                                    .Set(m => m.Chuyen3BinhDuongXeNho, itemFull.Chuyen3BinhDuongXeNho)
                                    .Set(m => m.Chuyen4BinhDuongXeNho, itemFull.Chuyen4BinhDuongXeNho)
                                    .Set(m => m.Chuyen5BinhDuongXeNho, itemFull.Chuyen5BinhDuongXeNho)
                                    .Set(m => m.Chuyen1BinhDuongXeLon, itemFull.Chuyen1BinhDuongXeLon)
                                    .Set(m => m.Chuyen2BinhDuongXeLon, itemFull.Chuyen2BinhDuongXeLon)
                                    .Set(m => m.Chuyen3BinhDuongXeLon, itemFull.Chuyen3BinhDuongXeLon)
                                    .Set(m => m.Chuyen4BinhDuongXeLon, itemFull.Chuyen4BinhDuongXeLon)
                                    .Set(m => m.Chuyen5BinhDuongXeLon, itemFull.Chuyen5BinhDuongXeLon)
                                    .Set(m => m.Chuyen1BienHoaXeNho, itemFull.Chuyen1BienHoaXeNho)
                                    .Set(m => m.Chuyen2BienHoaXeNho, itemFull.Chuyen2BienHoaXeNho)
                                    .Set(m => m.Chuyen3BienHoaXeNho, itemFull.Chuyen3BienHoaXeNho)
                                    .Set(m => m.Chuyen4BienHoaXeNho, itemFull.Chuyen4BienHoaXeNho)
                                    .Set(m => m.Chuyen5BienHoaXeNho, itemFull.Chuyen5BienHoaXeNho)
                                    .Set(m => m.Chuyen1BienHoaXeLon, itemFull.Chuyen1BienHoaXeLon)
                                    .Set(m => m.Chuyen2BienHoaXeLon, itemFull.Chuyen2BienHoaXeLon)
                                    .Set(m => m.Chuyen3BienHoaXeLon, itemFull.Chuyen3BienHoaXeLon)
                                    .Set(m => m.Chuyen4BienHoaXeLon, itemFull.Chuyen4BienHoaXeLon)
                                    .Set(m => m.Chuyen5BienHoaXeLon, itemFull.Chuyen5BienHoaXeLon)
                                    .Set(m => m.VungTauXeNho, itemFull.VungTauXeNho)
                                    .Set(m => m.VungTauXeLon, itemFull.VungTauXeLon)
                                    .Set(m => m.BinhThuanXeNho, itemFull.BinhThuanXeNho)
                                    .Set(m => m.BinhThuanXeLon, itemFull.BinhThuanXeLon)
                                    .Set(m => m.CanThoXeLon, itemFull.CanThoXeLon)
                                    .Set(m => m.VinhLongXeLon, itemFull.VinhLongXeLon)
                                    .Set(m => m.LongAnXeNho, itemFull.LongAnXeNho)
                                    .Set(m => m.LongAnXeLon, itemFull.LongAnXeLon)
                                    .Set(m => m.TienGiangXeNho, itemFull.TienGiangXeNho)
                                    .Set(m => m.TienGiangXeLon, itemFull.TienGiangXeLon)
                                    .Set(m => m.DongNaiXeNho, itemFull.DongNaiXeNho)
                                    .Set(m => m.DongNaiXeLon, itemFull.DongNaiXeLon)
                                    .Set(m => m.TongSoChuyen, itemFull.TongSoChuyen)
                                    .Set(m => m.TienChuyen, itemFull.TienChuyen)
                                    .Set(m => m.CongTacXa, itemFull.CongTacXa)
                                    .Set(m => m.KhoiLuongBun, itemFull.KhoiLuongBun)
                                    .Set(m => m.ThanhTienBun, itemFull.ThanhTienBun)
                                    .Set(m => m.UpdatedOn, DateTime.Now);
                                dbContext.LogisticEmployeeCongs.UpdateOne(filter, update);
                            }
                            else
                            {
                                dbContext.LogisticEmployeeCongs.InsertOne(itemFull);
                            }
                        }
                        else
                        {
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "logistics-cong-import",
                                Object = code + "-" + fullName + "-" + title + " ,dòng " + i,
                                Error = "No import data",
                                DateTime = DateTime.Now.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.LogisticEmployeeCong + "/" + Constants.LinkSalary.Update });
        }

        //https://stackoverflow.com/questions/51681846/rowspan-and-colspan-in-apache-poi
        [Route(Constants.LinkSalary.LogisticEmployeeTemplate)]
        public async Task<IActionResult> LogisticEmployeeTemplate(string fileName)
        {
            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"du-lieu-logistic-thang-" + DateTime.Now.Month + "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
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

                ISheet sheet1 = workbook.CreateSheet("LOGISTIC-T" + DateTime.Now.Month.ToString("00"));
                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                var cell = row.CreateCell(0, CellType.String); // cell A1
                cell.SetCellValue("TỔNG KẾT");
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(0, CellType.String);
                cell.SetCellValue("Tháng");
                cell = row.CreateCell(1, CellType.Numeric);
                cell.SetCellValue(DateTime.Now.Month);
                cell = row.CreateCell(2, CellType.String);
                cell.SetCellValue("Năm");
                cell = row.CreateCell(3, CellType.Numeric);
                cell.SetCellValue(DateTime.Now.Year);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(0, CellType.String);
                cell.SetCellValue("Đơn giá bùn (tấn)");
                cell = row.CreateCell(1, CellType.Numeric);
                cell.SetCellValue(22500);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                sheet1.AddMergedRegion(new CellRangeAddress(3, 5, 0, 0));
                cell = row.CreateCell(0, CellType.String);
                cell.SetCellValue("#");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 5, 1, 1));
                cell = row.CreateCell(1, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 5, 2, 2));
                cell = row.CreateCell(2, CellType.String);
                cell.SetCellValue("Họ tên");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 5, 3, 3));
                cell = row.CreateCell(3, CellType.String);
                cell.SetCellValue("Chức vụ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 5, 4, 4));
                cell = row.CreateCell(4, CellType.String);
                cell.SetCellValue("Doanh thu");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 5, 9));
                cell = row.CreateCell(5, CellType.String);
                cell.SetCellValue("TP.HCM Xe nhỏ 1.7 tấn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 10, 14));
                cell = row.CreateCell(10, CellType.String);
                cell.SetCellValue("TP.HCM Xe lớn ben và 8 tấn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 15, 19));
                cell = row.CreateCell(15, CellType.String);
                cell.SetCellValue("BÌNH DƯƠNG Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 20, 24));
                cell = row.CreateCell(20, CellType.String);
                cell.SetCellValue("BÌNH DƯƠNG Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 25, 29));
                cell = row.CreateCell(25, CellType.String);
                cell.SetCellValue("BIÊN HÒA Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 30, 34));
                cell = row.CreateCell(30, CellType.String);
                cell.SetCellValue("BIÊN HÒA Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 35, 36));
                cell = row.CreateCell(35, CellType.String);
                cell.SetCellValue("Vũng Tàu");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 37, 38));
                cell = row.CreateCell(37, CellType.String);
                cell.SetCellValue("Bình Thuận");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 39, 39));
                cell = row.CreateCell(39, CellType.String);
                cell.SetCellValue("Cần Thơ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 40, 40));
                cell = row.CreateCell(40, CellType.String);
                cell.SetCellValue("Vĩnh Long");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 41, 42));
                cell = row.CreateCell(41, CellType.String);
                cell.SetCellValue("Long An");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 43, 44));
                cell = row.CreateCell(43, CellType.String);
                cell.SetCellValue("Tiền Giang");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 45, 46));
                cell = row.CreateCell(45, CellType.String);
                cell.SetCellValue("Đồng Nai");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 47, 47));
                cell = row.CreateCell(47, CellType.String);
                cell.SetCellValue("Khối lượng bùn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 5, 9));
                cell = row.CreateCell(5, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 10, 14));
                cell = row.CreateCell(10, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 15, 19));
                cell = row.CreateCell(15, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 20, 24));
                cell = row.CreateCell(20, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 25, 29));
                cell = row.CreateCell(25, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 30, 34));
                cell = row.CreateCell(30, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(35, CellType.String);
                cell.SetCellValue("Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(36, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(37, CellType.String);
                cell.SetCellValue("Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(38, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(39, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(40, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(41, CellType.String);
                cell.SetCellValue("Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(42, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(43, CellType.String);
                cell.SetCellValue("Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(44, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(45, CellType.String); cell.SetCellValue("Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(46, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(5, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(6, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(7, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(8, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(9, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(10, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(11, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(12, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(13, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(14, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(15, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(16, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(17, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(18, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(19, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(20, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(21, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(22, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(23, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(24, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(25, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(26, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(27, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(28, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(29, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(30, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(31, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(32, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(33, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(34, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(35, CellType.String); cell.SetCellValue("VT"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(36, CellType.String); cell.SetCellValue("VT"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(37, CellType.String); cell.SetCellValue("BT"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(38, CellType.String); cell.SetCellValue("BT"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(39, CellType.String); cell.SetCellValue("CT"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(40, CellType.String); cell.SetCellValue("VL"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(41, CellType.String); cell.SetCellValue("LA"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(42, CellType.String); cell.SetCellValue("LA"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(43, CellType.String); cell.SetCellValue("TG"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(44, CellType.String); cell.SetCellValue("TG"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(45, CellType.String); cell.SetCellValue("ĐN"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(46, CellType.String); cell.SetCellValue("ĐN"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                rowIndex++;

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.LogisticEmployeeCong + "/" + Constants.LinkSalary.Export)]
        public async Task<IActionResult> LogisticEmployeeCongExport(string fileName)
        {
            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"du-lieu-logistic-thang-" + DateTime.Now.Month + "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
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

                ISheet sheet1 = workbook.CreateSheet("LOGISTIC-T" + DateTime.Now.Month.ToString("00"));
                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                var cell = row.CreateCell(0, CellType.String); // cell A1
                cell.SetCellValue("TỔNG KẾT");
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(0, CellType.String);
                cell.SetCellValue("Tháng");
                cell = row.CreateCell(1, CellType.Numeric);
                cell.SetCellValue(DateTime.Now.Month);
                cell = row.CreateCell(2, CellType.String);
                cell.SetCellValue("Năm");
                cell = row.CreateCell(3, CellType.Numeric);
                cell.SetCellValue(DateTime.Now.Year);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(0, CellType.String);
                cell.SetCellValue("Đơn giá bùn (tấn)");
                cell = row.CreateCell(1, CellType.Numeric);
                cell.SetCellValue(22500);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                sheet1.AddMergedRegion(new CellRangeAddress(3, 5, 0, 0));
                cell = row.CreateCell(0, CellType.String);
                cell.SetCellValue("#");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 5, 1, 1));
                cell = row.CreateCell(1, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 5, 2, 2));
                cell = row.CreateCell(2, CellType.String);
                cell.SetCellValue("Họ tên");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 5, 3, 3));
                cell = row.CreateCell(3, CellType.String);
                cell.SetCellValue("Chức vụ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 5, 4, 4));
                cell = row.CreateCell(4, CellType.String);
                cell.SetCellValue("Doanh thu");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 5, 5, 5));
                cell = row.CreateCell(5, CellType.String);
                cell.SetCellValue("Lương theo doanh thu/doanh số");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 6, 10));
                cell = row.CreateCell(6, CellType.String);
                cell.SetCellValue("TP.HCM Xe nhỏ 1.7 tấn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 11, 15));
                cell = row.CreateCell(11, CellType.String);
                cell.SetCellValue("TP.HCM Xe lớn ben và 8 tấn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 16, 20));
                cell = row.CreateCell(16, CellType.String);
                cell.SetCellValue("BÌNH DƯƠNG Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 21, 25));
                cell = row.CreateCell(21, CellType.String);
                cell.SetCellValue("BÌNH DƯƠNG Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 26, 30));
                cell = row.CreateCell(26, CellType.String);
                cell.SetCellValue("BIÊN HÒA Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 31, 35));
                cell = row.CreateCell(31, CellType.String);
                cell.SetCellValue("BIÊN HÒA Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 36, 37));
                cell = row.CreateCell(36, CellType.String);
                cell.SetCellValue("Vũng Tàu");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 38, 39));
                cell = row.CreateCell(38, CellType.String);
                cell.SetCellValue("Bình Thuận");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 40, 40));
                cell = row.CreateCell(40, CellType.String);
                cell.SetCellValue("Cần Thơ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 41, 41));
                cell = row.CreateCell(41, CellType.String);
                cell.SetCellValue("Vĩnh Long");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 42, 43));
                cell = row.CreateCell(42, CellType.String);
                cell.SetCellValue("Long An");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 44, 45));
                cell = row.CreateCell(44, CellType.String);
                cell.SetCellValue("Tiền Giang");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 46, 47));
                cell = row.CreateCell(46, CellType.String);
                cell.SetCellValue("Đồng Nai");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(3, 3, 48, 48));
                cell = row.CreateCell(48, CellType.String);
                cell.SetCellValue("Khối lượng bùn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 6, 10));
                cell = row.CreateCell(6, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 11, 15));
                cell = row.CreateCell(11, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 16, 20));
                cell = row.CreateCell(16, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 21, 25));
                cell = row.CreateCell(21, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 26, 30));
                cell = row.CreateCell(26, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                sheet1.AddMergedRegion(new CellRangeAddress(4, 4, 31, 35));
                cell = row.CreateCell(31, CellType.String);
                cell.SetCellValue("Trợ cấp");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(36, CellType.String);
                cell.SetCellValue("Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(37, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(38, CellType.String);
                cell.SetCellValue("Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(39, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(40, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(41, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(42, CellType.String);
                cell.SetCellValue("Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(43, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(44, CellType.String);
                cell.SetCellValue("Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(45, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(46, CellType.String); cell.SetCellValue("Xe nhỏ");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);

                cell = row.CreateCell(47, CellType.String);
                cell.SetCellValue("Xe lớn");
                cell.CellStyle = cellStyleHeader;
                cell.CellStyle.SetFont(font);
                CellUtil.SetCellStyleProperty(cell, workbook, CellUtil.VERTICAL_ALIGNMENT, VerticalAlignment.Center);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(6, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(7, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(8, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(9, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(10, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(11, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(12, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(13, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(14, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(15, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(16, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(17, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(18, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(19, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(20, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(21, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(22, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(23, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(24, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(25, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(26, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(27, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(28, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(29, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(30, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(31, CellType.String); cell.SetCellValue("Chuyến 1"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(32, CellType.String); cell.SetCellValue("Chuyến 2"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(33, CellType.String); cell.SetCellValue("Chuyến 3"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(34, CellType.String); cell.SetCellValue("Chuyến 4"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(35, CellType.String); cell.SetCellValue("Chuyến 5"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(36, CellType.String); cell.SetCellValue("VT"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(37, CellType.String); cell.SetCellValue("VT"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(38, CellType.String); cell.SetCellValue("BT"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(39, CellType.String); cell.SetCellValue("BT"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(40, CellType.String); cell.SetCellValue("CT"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(41, CellType.String); cell.SetCellValue("VL"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(42, CellType.String); cell.SetCellValue("LA"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(43, CellType.String); cell.SetCellValue("LA"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(44, CellType.String); cell.SetCellValue("TG"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(45, CellType.String); cell.SetCellValue("TG"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(46, CellType.String); cell.SetCellValue("ĐN"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                cell = row.CreateCell(47, CellType.String); cell.SetCellValue("ĐN"); cell.CellStyle = cellStyleHeader; cell.CellStyle.SetFont(font);
                rowIndex++;

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.LogisticGiaChuyenXeImport)]
        [HttpPost]
        public ActionResult LogisticGiaChuyenXeImport()
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

                        var tuyen = Utility.GetFormattedCellValue(row.GetCell(1));
                        var tuyenalias = Utility.AliasConvert(tuyen);
                        var tuyencode = Utility.UpperCodeConvert(tuyen);
                        var xe = Utility.GetFormattedCellValue(row.GetCell(2));
                        var xealias = Utility.AliasConvert(xe);
                        var xecode = Utility.UpperCodeConvert(xe);

                        var luongnangsuatchuyencom = (decimal)Utility.GetNumbericCellValue(row.GetCell(3));
                        var hotrocomtinh = (decimal)Utility.GetNumbericCellValue(row.GetCell(4));
                        var luongnangsuatchuyen = (decimal)Utility.GetNumbericCellValue(row.GetCell(5));
                        var chuyen1 = (decimal)Utility.GetNumbericCellValue(row.GetCell(6));
                        var chuyen2 = (decimal)Utility.GetNumbericCellValue(row.GetCell(7));
                        var chuyen3 = (decimal)Utility.GetNumbericCellValue(row.GetCell(8));
                        var chuyen4 = (decimal)Utility.GetNumbericCellValue(row.GetCell(9));
                        var chuyen5 = (decimal)Utility.GetNumbericCellValue(row.GetCell(10));
                        var hotrochuyendem = (decimal)Utility.GetNumbericCellValue(row.GetCell(11));

                        var gia = dbContext.LogisticGiaChuyenXes.Find(m => m.Enable.Equals(true) && m.Tuyen.Equals(tuyen.Trim()) && m.LoaiXe.Equals(xe.Trim()) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                        if (gia != null)
                        {
                            var builder = Builders<LogisticGiaChuyenXe>.Filter;
                            var filter = builder.Eq(m => m.Id, gia.Id);
                            var update = Builders<LogisticGiaChuyenXe>.Update
                                .Set(m => m.LuongNangSuatChuyenCom, luongnangsuatchuyencom)
                                .Set(m => m.HoTroTienComTinh, hotrocomtinh)
                                .Set(m => m.LuongNangSuatChuyen, luongnangsuatchuyen)
                                .Set(m => m.Chuyen1, chuyen1)
                                .Set(m => m.Chuyen2, chuyen2)
                                .Set(m => m.Chuyen3, chuyen3)
                                .Set(m => m.Chuyen4, chuyen4)
                                .Set(m => m.Chuyen5, chuyen5)
                                .Set(m => m.HoTroChuyenDem, hotrochuyendem)
                                .Set(m => m.UpdatedOn, DateTime.Now);
                            dbContext.LogisticGiaChuyenXes.UpdateOne(filter, update);
                        }
                        else
                        {
                            dbContext.LogisticGiaChuyenXes.InsertOne(new LogisticGiaChuyenXe
                            {
                                Year = year,
                                Month = month,
                                Tuyen = tuyen,
                                TuyenAlias = tuyenalias,
                                TuyenCode = tuyencode,
                                LoaiXe = xe,
                                LoaiXeAlias = xealias,
                                LoaiXeCode = xecode,
                                LuongNangSuatChuyenCom = luongnangsuatchuyencom,
                                HoTroTienComTinh = hotrocomtinh,
                                LuongNangSuatChuyen = luongnangsuatchuyen,
                                Chuyen1 = chuyen1,
                                Chuyen2 = chuyen2,
                                Chuyen3 = chuyen3,
                                Chuyen4 = chuyen4,
                                Chuyen5 = chuyen5,
                                HoTroChuyenDem = (decimal)hotrochuyendem
                            });
                        }
                    }
                }
            }
            return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.LogisticGiaChuyenXe + "/" + Constants.LinkSalary.Update });
        }

        [Route(Constants.LinkSalary.LogisticGiaChuyenXeTemplate)]
        public async Task<IActionResult> LogisticGiaChuyenXeTemplate(string fileName)
        {
            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"du-lieu-logistic-gia-chuyen-xe-thang-" + DateTime.Now.Month + "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
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

                ISheet sheet1 = workbook.CreateSheet("BANG-GIA-CHUYEN-XE-T" + DateTime.Now.Month.ToString("00"));

                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("BẢNG GIÁ CHUYẾN XE");
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

                row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("#");
                row.CreateCell(1, CellType.String).SetCellValue("Tuyến");
                row.CreateCell(2, CellType.String).SetCellValue("Mã");
                row.CreateCell(3, CellType.String).SetCellValue("Lương năng suất chuyến + cơm");
                row.CreateCell(4, CellType.String).SetCellValue("Hỗ trợ tiền cơm tỉnh");
                row.CreateCell(5, CellType.String).SetCellValue("Lương năng suất chuyến(trừ cơm)");
                row.CreateCell(6, CellType.String).SetCellValue("Chuyến 1");
                row.CreateCell(7, CellType.String).SetCellValue("Chuyến 2");
                row.CreateCell(8, CellType.String).SetCellValue("Chuyến 3");
                row.CreateCell(9, CellType.String).SetCellValue("Chuyến 4");
                row.CreateCell(10, CellType.String).SetCellValue("Chuyến 5");
                row.CreateCell(11, CellType.String).SetCellValue("Hỗ trợ chuyến đêm");
                // Set style
                for (int i = 0; i <= 11; i++)
                {
                    row.Cells[i].CellStyle = cellStyleHeader;
                }
                rowIndex++;

                var locations = GetLogisticsLocations();
                var xes = GetLogisticsLoaiXes();
                var no = 1;
                foreach (var location in locations)
                {
                    foreach (var xe in xes)
                    {
                        row = sheet1.CreateRow(rowIndex);
                        row.CreateCell(0, CellType.Numeric).SetCellValue(no);
                        row.CreateCell(1, CellType.String).SetCellValue(location.Name);
                        row.CreateCell(2, CellType.String).SetCellValue(xe.Name + " " + xe.Description);
                        row.CreateCell(3, CellType.Numeric).SetCellValue(string.Empty);
                        row.CreateCell(4, CellType.Numeric).SetCellValue(string.Empty);
                        row.CreateCell(5, CellType.Numeric).SetCellValue(string.Empty);
                        row.CreateCell(6, CellType.Numeric).SetCellValue(string.Empty);
                        row.CreateCell(7, CellType.Numeric).SetCellValue(string.Empty);
                        row.CreateCell(8, CellType.Numeric).SetCellValue(string.Empty);
                        row.CreateCell(9, CellType.Numeric).SetCellValue(string.Empty);
                        row.CreateCell(10, CellType.Numeric).SetCellValue(string.Empty);
                        row.CreateCell(11, CellType.Numeric).SetCellValue(string.Empty);
                        row.CreateCell(12, CellType.Numeric).SetCellValue(string.Empty);
                        row.CreateCell(13, CellType.Numeric).SetCellValue(string.Empty);
                        rowIndex++;
                        no++;
                    }
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

        [Route(Constants.LinkSalary.LogisticGiaBunPost)]
        [HttpPost]
        public ActionResult LogisticGiaBunPost(string thang, decimal price)
        {
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

            var existbun = dbContext.LogisticGiaBuns.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (existbun != null)
            {
                var builderB = Builders<LogisticGiaBun>.Filter;
                var filterB = builderB.Eq(m => m.Id, existbun.Id);
                var updateB = Builders<LogisticGiaBun>.Update
                    .Set(m => m.Price, price);
                dbContext.LogisticGiaBuns.UpdateOne(filterB, updateB);
            }
            else
            {
                dbContext.LogisticGiaBuns.InsertOne(new LogisticGiaBun()
                {
                    Month = month,
                    Year = year,
                    Name = "Bùn",
                    Alias = "bun",
                    Code = "BUN",
                    Price = price
                });
            }
            return Json(new { result = true });
        }

        private List<LogisticsLocation> GetLogisticsLocations()
        {
            var results = new List<LogisticsLocation>
            {
                 new LogisticsLocation
                {
                    Name = "Hồ Chí Minh",
                    Code ="HOCHIMINH"
                },
                  new LogisticsLocation
                {
                    Name = "Bình Dương",
                    Code = "BINHDUONG"
                },
                  new LogisticsLocation
                {
                    Name = "Biên Hòa",
                    Code = "BIENHOA"
                },
                  new LogisticsLocation
                {
                    Name = "Vũng Tàu",
                    Code = "VUNGTAU"
                },
                  new LogisticsLocation
                {
                    Name = "Bình Thuận",
                    Code = "BINHTHUAN"
                },
                  new LogisticsLocation
                {
                    Name = "Cần Thơ",
                    Code = "CANTHO"
                },
                  new LogisticsLocation
                {
                    Name = "Vĩnh Long",
                    Code = "VINHLONG"
                },
                  new LogisticsLocation
                {
                    Name = "Long An",
                    Code = "LONGAN"
                },
                  new LogisticsLocation
                {
                    Name = "Tiền Giang",
                    Code = "TIENGIANG"
                },
                  new LogisticsLocation
                {
                    Name = "Đồng Nai",
                    Code = "DONGNAI"
                }
            };
            var list = new List<LogisticsLocation>();
            foreach (var item in results)
            {
                item.Alias = Utility.AliasConvert(item.Name);
                dbContext.LogisticsLocations.InsertOne(item);
                list.Add(item);
            }
            return list;
        }

        private List<LogisticsLoaiXe> GetLogisticsLoaiXes()
        {
            var results = new List<LogisticsLoaiXe>
            {
                 new LogisticsLoaiXe
                {
                    Name = "Xe lớn",
                    Code ="XL"
                },
                  new LogisticsLoaiXe
                {
                    Name = "Xe nhỏ",
                    Code = "XN"
                }
            };
            var list = new List<LogisticsLoaiXe>();
            foreach (var item in results)
            {
                item.Alias = Utility.AliasConvert(item.Name);
                dbContext.LogisticsLoaiXes.InsertOne(item);
                list.Add(item);
            }
            return list;
        }
        #endregion

        #region Sub
        //[Route(Constants.LinkSalary.BangLuong + "/" + Constants.LinkSalary.Calculator)]
        //public IActionResult LuongCalculator(BangLuongViewModel viewModel)
        //{
        //    var entity = viewModel.SalaryEmployeeMonths.First();

        //    string thang = entity.Month + "-" + entity.Year;
        //    var returnEntity = GetSalaryEmployeeMonth(thang, entity.EmployeeId, entity, true, 0, 0, 0, 0);

        //    return Json(new { entity = returnEntity });
        //}

        //public int GetBacLuong(string employeeId)
        //{
        //    var bac = 1;
        //    var newestSalary = dbContext.SalaryEmployeeMonths
        //                        .Find(m => m.EmployeeId.Equals(employeeId)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();

        //    if (newestSalary != null)
        //    {
        //        bac = newestSalary.NgachLuongLevel;
        //    }
        //    else
        //    {
        //        // Get Bac in SalaryThangBacLuongEmployees
        //        var newestSalaryThangBacLuongEmployee = dbContext.SalaryThangBacLuongEmployees
        //                    .Find(m => m.EmployeeId.Equals(employeeId)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
        //        if (newestSalary != null)
        //        {
        //            bac = newestSalaryThangBacLuongEmployee.Bac;
        //        }
        //    }
        //    return bac;
        //}

        //public decimal GetLuongCB(string maViTri, string employeeId, int bac, int month, int year)
        //{
        //    decimal luongCB = 0;

        //    // GET DIRECT [SalaryThangBangLuong] by bac vs mavitri
        //    if (!string.IsNullOrEmpty(maViTri))
        //    {
        //        var salaryThangBangLuong = dbContext.SalaryThangBangLuongs.Find(m => m.ViTriCode.Equals(maViTri) & m.Bac.Equals(bac) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefault();
        //        if (salaryThangBangLuong == null)
        //        {
        //            var lastItem = dbContext.SalaryThangBangLuongs.Find(m => m.ViTriCode.Equals(maViTri) & m.Bac.Equals(bac)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
        //            lastItem.Id = null;
        //            lastItem.Month = month;
        //            lastItem.Year = year;
        //            dbContext.SalaryThangBangLuongs.InsertOne(lastItem);
        //            salaryThangBangLuong = lastItem;
        //        }
        //        luongCB = salaryThangBangLuong != null ? salaryThangBangLuong.MucLuong : 0;
        //    }

        //    #region old, analytics later
        //    //// Nếu [SalaryThangBangLuong] thay đổi. (chưa tạo lịch sử).
        //    //// Cập nhật [SalaryThangBacLuongEmployees]
        //    //// Mỗi tháng 1 record [SalaryThangBacLuongEmployees]
        //    //// Get lastest information base year, month.
        //    //var level = dbContext.SalaryThangBacLuongEmployees
        //    //    .Find(m => m.EmployeeId.Equals(employeeId) & m.Law.Equals(false) & m.Enable.Equals(true)
        //    //    & m.Year.Equals(year) & m.Month.Equals(month))
        //    //    .FirstOrDefault();
        //    //if (level != null)
        //    //{
        //    //    luongCB = level.MucLuong;
        //    //}
        //    //else
        //    //{
        //    //    // Get lastest
        //    //    var lastLevel = dbContext.SalaryThangBacLuongEmployees
        //    //    .Find(m => m.EmployeeId.Equals(employeeId) & m.Law.Equals(false) & m.Enable.Equals(true))
        //    //    .SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();

        //    //    if (lastLevel != null)
        //    //    {
        //    //        var salaryThangBangLuong = dbContext.SalaryThangBangLuongs.Find(m => m.ViTriCode.Equals(lastLevel.ViTriCode) & m.Bac.Equals(lastLevel.Bac)).FirstOrDefault();
        //    //        luongCB = salaryThangBangLuong.MucLuong;
        //    //        if (salaryThangBangLuong != null)
        //    //        {
        //    //            dbContext.SalaryThangBacLuongEmployees.InsertOne(new SalaryThangBacLuongEmployee()
        //    //            {
        //    //                Year = year,
        //    //                Month = month,
        //    //                EmployeeId = employeeId,
        //    //                ViTriCode = salaryThangBangLuong.ViTriCode,
        //    //                Bac = salaryThangBangLuong.Bac,
        //    //                MucLuong = salaryThangBangLuong.MucLuong
        //    //            });
        //    //        }
        //    //    }
        //    //}

        //    //return luongCB;
        //    #endregion

        //    return luongCB;
        //}

        //public bool GetCalBhxh(string thang)
        //{
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

        //    var calBHXH = true;
        //    if (DateTime.Now.Day < 26 && DateTime.Now.Month == month)
        //    {
        //        calBHXH = false;
        //    }
        //    return calBHXH;
        //}

        [Route(Constants.LinkSalary.ThangLuong + "/" + Constants.LinkSalary.VanPhong + "/" + Constants.LinkSalary.Calculator)]
        public IActionResult ThangBangLuongCalculator(string thang, string id, decimal heso, decimal money)
        {
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
                        var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(false) & m.ViTriCode.Equals(vitriCode) & m.Month.Equals(month) & m.Year.Equals(year)).ToList();
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
                var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(false) & m.Bac.Equals(1) & m.Month.Equals(month) & m.Year.Equals(year) & m.MucLuong.Equals(salaryMinApDung)).ToList();

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

        private List<ChucVuKinhDoanh> GetChucVuKinhDoanhs()
        {
            var results = new List<ChucVuKinhDoanh>
            {
                new ChucVuKinhDoanh
                {
                    Name = "ĐDKD HCM"
                },
                new ChucVuKinhDoanh
                {
                    Name = "TKD HCM"
                },
                new ChucVuKinhDoanh
                {
                    Name = "ĐDKD TỈNH"
                },
                new ChucVuKinhDoanh
                {
                    Name = "TKD TỈNH"
                },
                 new ChucVuKinhDoanh
                {
                    Name = "ADMIN"
                },
                 new ChucVuKinhDoanh
                {
                    Name = "ĐDKD BÙN"
                },
                new ChucVuKinhDoanh
                {
                    Name = "TKD BÙN"
                }
            };
            var list = new List<ChucVuKinhDoanh>();
            int code = 1;
            foreach (var item in results)
            {
                item.Alias = Utility.AliasConvert(item.Name);
                item.Code = "CVKD-" + code.ToString("00");
                item.Order = code;
                dbContext.ChucVuKinhDoanhs.InsertOne(item);
                list.Add(item);
                code++;
            }
            return list;
        }
        #endregion
    }
}
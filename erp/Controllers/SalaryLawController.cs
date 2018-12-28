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
    public class SalaryLawController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalaryLawController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalaryLawController> logger)
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

        [Route(Constants.LinkSalary.BangLuong)]
        public async Task<IActionResult> BangLuong(string thang)
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
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

            var mucluongvung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefaultAsync();
            if (mucluongvung == null)
            {
                var lastItemVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                var lastMonthVung = lastItemVung.Month;
                var lastYearVung = lastItemVung.Year;

                lastItemVung.Id = null;
                lastItemVung.Month = month;
                lastItemVung.Year = year;
                dbContext.SalaryMucLuongVungs.InsertOne(lastItemVung);
                mucluongvung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefaultAsync();
            }
            var salaryEmployeeMonths = await dbContext.SalaryEmployeeMonths.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year) & m.LuongThamGiaBHXH > 0).ToListAsync();
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
                // phucapphuloi base maso C.01|C.02...
                var phucapphuclois = dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.MaSo.Equals(item.SalaryMaSoChucDanhCongViec) && m.Law.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).ToList();
                if (phucapphuclois.Find(m => m.Code.Equals("01-001")) != null)
                {
                    nangnhoc = phucapphuclois.Find(m => m.Code.Equals("01-001")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("01-002")) != null)
                {
                    trachnhiem = phucapphuclois.Find(m => m.Code.Equals("01-002")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("01-004")) != null)
                {
                    thuhut = phucapphuclois.Find(m => m.Code.Equals("01-004")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-001")) != null)
                {
                    xang = phucapphuclois.Find(m => m.Code.Equals("02-001")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-002")) != null)
                {
                    dienthoai = phucapphuclois.Find(m => m.Code.Equals("02-002")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-003")) != null)
                {
                    com = phucapphuclois.Find(m => m.Code.Equals("02-003")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-004")) != null)
                {
                    kiemnhiem = phucapphuclois.Find(m => m.Code.Equals("02-004")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-005")) != null)
                {
                    bhytdacbiet = phucapphuclois.Find(m => m.Code.Equals("02-005")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-006")) != null)
                {
                    vitricanknnhieunam = phucapphuclois.Find(m => m.Code.Equals("02-006")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-007")) != null)
                {
                    vitridacthu = phucapphuclois.Find(m => m.Code.Equals("02-007")).Money;
                }
                if (phucapphuclois.Find(m => m.Code.Equals("02-008")) != null)
                {
                    nhao = phucapphuclois.Find(m => m.Code.Equals("02-008")).Money;
                }
                if (item.ThamNien > 0)
                {
                    thamnien = luongCB * Convert.ToDecimal(0.03 + (item.ThamNienYear - 3) * 0.01); ;
                }
                item.NangNhocDocHai = nangnhoc;
                item.TrachNhiem = trachnhiem;
                item.ThuHut = thuhut;
                item.Xang = xang;
                item.DienThoai = dienthoai;
                item.Com = com;
                item.KiemNhiem = kiemnhiem;
                item.BhytDacBiet = bhytdacbiet;
                item.ViTriCanKnNhieuNam = vitricanknnhieunam;
                item.ViTriDacThu = vitridacthu;
                item.NhaO = nhao;
                item.ThamNien = thamnien;

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

        [Route(Constants.LinkSalary.ThangLuong)]
        public async Task<IActionResult> ThangLuong()
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

            var mucluongvung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync();
            var ngachluongs = await dbContext.NgachLuongs.Find(m => m.Enable.Equals(true) && m.Law.Equals(true)).ToListAsync();
            var phucapphuclois = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true)).ToListAsync();
            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = mucluongvung,
                NgachLuongs = ngachluongs,
                SalaryThangBangPhuCapPhucLois = phucapphuclois
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.ThangLuong + "/" + Constants.LinkSalary.Calculator)]
        public IActionResult ThangLuongCalculator(decimal money, decimal heso, string id)
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
    }
}
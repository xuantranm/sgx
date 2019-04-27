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
        readonly IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalaryLawController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalaryLawController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _logger = logger;
        }

        [Route(Constants.LinkSalary.BangLuong)]
        public async Task<IActionResult> BangLuong(string Thang, string Id)
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
            var sortTimes = Utility.DllMonths();
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                                && m.LuongBHXH > 0).SortBy(m => m.FullName).ToList();
            #endregion

            #region Times
            var toDate = Utility.GetSalaryToDate(Thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            Thang = string.IsNullOrEmpty(Thang) ? month + "-" + year : Thang;
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

            var salaryEmployeeMonths = await dbContext.SalaryEmployeeMonths.Find(m => m.Enable.Equals(true) 
                                        && m.Month.Equals(month) && m.Year.Equals(year) & m.LuongThamGiaBHXH > 0).ToListAsync();
            
            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = salaryEmployeeMonths,
                SalaryMucLuongVung = mucluongvung,
                MonthYears = sortTimes,
                Thang = Thang
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
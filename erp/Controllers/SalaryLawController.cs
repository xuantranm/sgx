﻿using System;
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
        public async Task<IActionResult> BangLuong(string Thang, string Id, string SapXep, string ThuTu)
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

            var linkCurrent = string.Empty;

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

            linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
            linkCurrent += "Thang=" + Thang;
            #endregion

            var mucluongvung = Utility.SalaryMucLuongVung(month, year);

            #region Filter
            var builder = Builders<SalaryEmployeeMonth>.Filter;
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Eq(m => m.Month, month)
                        & builder.Eq(m => m.Year, year)
                        & builder.Gt(m => m.LuongThamGiaBHXH, 0);
            if (!string.IsNullOrEmpty(Id))
            {
                filter = filter & builder.Eq(m => m.EmployeeId, Id);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Id=" + Id;
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<SalaryEmployeeMonth>.Sort.Ascending(m => m.EmployeeCode);
            SapXep = string.IsNullOrEmpty(SapXep) ? "code" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "ten":
                    sortBuilder = ThuTu == "asc" ? Builders<SalaryEmployeeMonth>.Sort.Ascending(m => m.EmployeeFullName) : Builders<SalaryEmployeeMonth>.Sort.Descending(m => m.EmployeeFullName);
                    break;
                case "luong":
                    sortBuilder = ThuTu == "asc" ? Builders<SalaryEmployeeMonth>.Sort.Ascending(m => m.LuongThamGiaBHXH) : Builders<SalaryEmployeeMonth>.Sort.Descending(m => m.LuongThamGiaBHXH);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<SalaryEmployeeMonth>.Sort.Ascending(m => m.EmployeeCode) : Builders<SalaryEmployeeMonth>.Sort.Descending(m => m.EmployeeCode);
                    break;
            }
            #endregion

            var records = dbContext.SalaryEmployeeMonths.CountDocuments(filter);
            var list = new List<SalaryEmployeeMonth>();
            list = dbContext.SalaryEmployeeMonths.Find(filter).Sort(sortBuilder).ToList();

            var viewModel = new BangLuongViewModel()
            {
                Salaries = list,
                Employees = employees,
                SalaryMucLuongVung = mucluongvung,
                MonthYears = sortTimes,
                Id = Id,
                Thang = Thang,
                LinkCurrent = linkCurrent,
                ThuTu = ThuTu,
                SapXep = SapXep,
                Records = (int)records
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
            var ngachluongs = await dbContext.NgachLuongs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ESalary.Law)).ToListAsync();
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
        public IActionResult ThangLuongCalculator(decimal money, double Rate, string id)
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
                var currentLevel = dbContext.NgachLuongs.Find(m => m.Id.Equals(id)).FirstOrDefault();
                if (currentLevel != null)
                {
                    var bac = currentLevel.Level;
                    var Code = currentLevel.Code;
                    if (Rate == 0)
                    {
                        Rate = currentLevel.Rate;
                    }
                    var salaryDeclareTax = Convert.ToDecimal(Rate * (double)salaryMin);
                    if (bac > 1)
                    {
                        var previousBac = bac - 1;
                        var previousBacEntity = dbContext.NgachLuongs.Find(m => m.Code.Equals(Code) & m.Level.Equals(previousBac)).FirstOrDefault();
                        if (previousBacEntity != null)
                        {
                            salaryDeclareTax = Convert.ToDecimal(Rate * (double)previousBacEntity.Money);
                        }
                    }
                    // Add current change
                    list.Add(new IdMoney
                    {
                        Id = currentLevel.Id,
                        Money = salaryDeclareTax,
                        Rate = Rate
                    });
                    var levels = dbContext.NgachLuongs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ESalary.Law) & m.Code.Equals(Code)).ToList();

                    foreach (var level in levels)
                    {
                        if (level.Level > bac)
                        {
                            salaryDeclareTax = Convert.ToDecimal(level.Rate * (double)salaryDeclareTax);
                            list.Add(new IdMoney
                            {
                                Id = level.Id,
                                Money = salaryDeclareTax,
                                Rate = level.Rate
                            });
                        }
                    }
                }
            }
            else
            {
                var levels = dbContext.NgachLuongs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ESalary.Law)).ToList();
                // group by Code
                var groups = (from s in levels
                              group s by new
                              {
                                  s.Code
                              }
                                                    into l
                              select new
                              {
                                  CodeName = l.Key.Code,
                                  Salaries = l.ToList(),
                              }).ToList();

                foreach (var group in groups)
                {
                    // reset salaryDeclareTax;
                    var salaryDeclareTax = salaryMin;
                    foreach (var level in group.Salaries)
                    {
                        salaryDeclareTax = Convert.ToDecimal(level.Rate * (double)salaryDeclareTax);
                        list.Add(new IdMoney
                        {
                            Id = level.Id,
                            Money = salaryDeclareTax,
                            Rate = level.Rate
                        });
                    }
                }
            }

            return Json(new { result = true, list });
        }

        #region Sub
        
        #endregion
    }
}
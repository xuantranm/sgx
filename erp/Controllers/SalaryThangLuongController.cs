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
    [Route(Constants.LinkSalary.Main + "/" + Constants.LinkSalary.ThangLuongTrue)]
    public class SalaryThangLuongController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalaryThangLuongController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalaryThangLuongController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _logger = logger;
        }

        [Route(Constants.LinkSalary.ThangLuong)]
        public async Task<IActionResult> ThangLuong(string thang, string id)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "luong-nha-may", (int)ERights.View)))
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

            int yearSale = new DateTime(year, month, 01).AddMonths(-2).Year;
            int monthSale = new DateTime(year, month, 01).AddMonths(-2).Month;
            var saleTimes = monthSale + "-" + yearSale;
            #endregion

            var mucluongvung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync();
            var ngachluongs = await dbContext.NgachLuongs.Find(m => m.Enable.Equals(true) && m.Law.Equals(false)).ToListAsync();
            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = mucluongvung,
                NgachLuongs = ngachluongs
            };

            return View(viewModel);
        }
    }
}
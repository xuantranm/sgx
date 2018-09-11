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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Common.Enums;

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.LinkTimeKeeper.Main)]
    public class TimeKeeperController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        readonly IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public TimeKeeperController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<TimeKeeperController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        [Route(Constants.LinkTimeKeeper.Manage)]
        public async Task<IActionResult> Manage(string times, string employee, string code, string finger, string nl)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            // Check owner
            //if (id != login)
            //{
            //    if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
            //    {
            //        return RedirectToAction("AccessDenied", "Account");
            //    }
            //}

            var userInformation = loginInformation;
            #endregion

            #region Dropdownlist
            #endregion

            var toDate = Utility.WorkingMonthToDate(times);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            ViewData["DayWorking"] = Utility.BusinessDaysUntil(fromDate, toDate);

            #region Filter
            var builder = Builders<EmployeeWorkTimeLog>.Filter;
            var filter = builder.Gte(m => m.Date, fromDate) & builder.Lte(m => m.Date, toDate);

            var builderEmployee = Builders<Employee>.Filter;
            var filterEmployee = builderEmployee.Eq(m => m.Enable, true) & builderEmployee.Eq(m => m.IsTimeKeeper, false);
            #endregion

            var timekeepings = await dbContext.EmployeeWorkTimeLogs.Find(filter).SortBy(m => m.Date).ToListAsync();
            var employees = await dbContext.Employees.Find(filterEmployee).ToListAsync();
            var viewModel = new TimeKeeperViewModel
            {
                EmployeeWorkTimeLogs = timekeepings,
                Employees = employees,
                StartWorkingDate = fromDate,
                EndWorkingDate = toDate
            };
            return View(viewModel);
        }

        [Route(Constants.LinkTimeKeeper.Index)]
        public IActionResult Index(string times, string employee)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            // Check owner
            if (!string.IsNullOrEmpty(employee) && employee != login)
            {
                if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }
            else
            {
                employee = login;
            }

            var userInformation = employee == login ? loginInformation : dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(employee)).FirstOrDefault();
            #endregion

            #region Dropdownlist
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.IsTimeKeeper.Equals(false)).SortBy(m=>m.FullName).ToList();
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x=>x.Month).ToList();
            #endregion

            var toDate = Utility.WorkingMonthToDate(times);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            // override times if null
            if (string.IsNullOrEmpty(times))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            
            var timekeepings = new List<EmployeeWorkTimeLog>();
            var monthsTimes = new List<EmployeeWorkTimeMonthLog>();

            if (userInformation.Workplaces != null && userInformation.Workplaces.Count > 0)
            {
                foreach (var workplace in userInformation.Workplaces)
                {
                    if (!string.IsNullOrEmpty(workplace.Fingerprint))
                    {
                        #region Filter
                        var builder = Builders<EmployeeWorkTimeLog>.Filter;
                        var filter = builder.Eq(m => m.EnrollNumber, workplace.Fingerprint);
                        filter = filter & builder.Gt(m => m.Date, fromDate.AddDays(-1)) & builder.Lt(m => m.Date, toDate.AddDays(1));

                        var builderSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
                        var filterSum = builderSum.Eq(m => m.EnrollNumber, workplace.Fingerprint);
                        #endregion

                        timekeepings.AddRange(dbContext.EmployeeWorkTimeLogs.Find(filter).ToList());
                        monthsTimes.AddRange(dbContext.EmployeeWorkTimeMonthLogs.Find(filterSum).ToList());
                    }
                }
            }

            ViewData["DayWorking"] = Utility.BusinessDaysUntil(fromDate, toDate);

            timekeepings = timekeepings.OrderByDescending(m => m.Date).ToList();
            monthsTimes = monthsTimes.OrderByDescending(m => m.Year).OrderByDescending(m => m.Month).ToList();
            var monthTime = monthsTimes.FirstOrDefault(m => m.Year.Equals(fromDate.Year) && m.Month.Equals(fromDate.Month));
            var viewModel = new TimeKeeperViewModel
            {
                EmployeeWorkTimeLogs = timekeepings,
                Employee = userInformation,
                EmployeeWorkTimeMonthLogs = monthsTimes,
                EmployeeWorkTimeMonthLog = monthTime,
                Employees = employees,
                MonthYears = sortTimes,
                StartWorkingDate = fromDate,
                EndWorkingDate = toDate
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route("cham-cong/xac-nhan-cong/")]
        public IActionResult ConfirmTimeKeeper(EmployeeWorkTimeLog model, string manager)
        {
            // Update status

            // Send mail to manager and owner,...


            return Json(new { result = true });
        }
    }
}
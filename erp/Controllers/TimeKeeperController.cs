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
using Helpers;
using MimeKit;
using MimeKit.Text;
using Services;

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.LinkTimeKeeper.Main)]
    public class TimeKeeperController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        readonly IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public TimeKeeperController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, IEmailSender emailSender,
            ISmsSender smsSender, ILogger<TimeKeeperController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
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
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
        public async Task<IActionResult> Index(string thang, string id)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var userInformation = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(login)).FirstOrDefault();
            if (userInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }
            bool isRight = false;
            if (loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.XacNhanCongDum, (int)ERights.Add))
            {
                isRight = true;
            }
            #endregion

            id = string.IsNullOrEmpty(id) ? login : id;
            if (id != login)
            {
                userInformation = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(id)).FirstOrDefault();
            }

            #region Dropdownlist
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
            var approves = new List<IdName>();
            if (!string.IsNullOrEmpty(userInformation.ManagerId))
            {
                var approveEntity = dbContext.Employees.Find(m => m.Id.Equals(userInformation.ManagerId)).FirstOrDefault();
                if (approveEntity != null)
                {
                    approves.Add(new IdName
                    {
                        Id = approveEntity.Id,
                        Name = approveEntity.FullName
                    });
                }
            }
            if (approves == null || approves.Count == 0)
            {
                var rolesApprove = dbContext.RoleUsers.Find(m => m.Enable.Equals(true) && m.Role.Equals(Constants.Rights.XacNhanCong)).ToList();
                foreach (var roleApprove in rolesApprove)
                {
                    approves.Add(new IdName
                    {
                        Id = roleApprove.User,
                        Name = roleApprove.FullName
                    });
                }
            }
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
            var dayworking = Utility.BusinessDaysUntil(fromDate, toDate);
            if (toDate > DateTime.Now.Date)
            {
                dayworking = Utility.BusinessDaysUntil(fromDate, DateTime.Now);
            }
            ViewData["DayWorking"] = dayworking;
            #endregion

            #region Filter
            var builder = Builders<EmployeeWorkTimeLog>.Filter;
            var filter = builder.Eq(m => m.EmployeeId, id);
            filter = filter & builder.Gt(m => m.Date, fromDate.AddDays(-1)) & builder.Lt(m => m.Date, toDate.AddDays(1));

            var builderSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
            var filterSum = builderSum.Eq(m => m.EmployeeId, id);
            filterSum = filterSum & builderSum.Eq(m => m.Month, month) & builderSum.Eq(m => m.Year, year);
            #endregion

            var timekeepings = await dbContext.EmployeeWorkTimeLogs.Find(filter).SortByDescending(m => m.Date).ToListAsync();
            var monthsTimes = await dbContext.EmployeeWorkTimeMonthLogs.Find(filterSum).SortByDescending(m => m.LastUpdated).ToListAsync();

            var approver = false;
            if (dbContext.EmployeeWorkTimeLogs.CountDocuments(m => m.Enable.Equals(true) && m.ConfirmId.Equals(login)) > 0)
            {
                approver = true;
            }

            var viewModel = new TimeKeeperViewModel
            {
                EmployeeWorkTimeLogs = timekeepings,
                Employee = userInformation,
                EmployeeWorkTimeMonthLogs = monthsTimes,
                //EmployeeWorkTimeMonthLog = monthTime,
                MonthYears = sortTimes,
                StartWorkingDate = fromDate,
                EndWorkingDate = toDate,
                Approves = approves,
                thang = thang,
                RightRequest = isRight,
                Approver = approver
            };
            return View(viewModel);
        }

        [Route(Constants.LinkTimeKeeper.HelpTime)]
        public async Task<IActionResult> HelpTime(string id, string thang)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var userInformation = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(login)).FirstOrDefault();
            if (userInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            // Quyền tạo nghỉ phép dùm
            bool isRight = false;
            if (loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.XacNhanCongDum, (int)ERights.Add))
            {
                isRight = true;
            }
            if (!isRight)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            id = string.IsNullOrEmpty(id) ? login : id;
            if (id != login)
            {
                userInformation = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(id)).FirstOrDefault();
            }

            #region Dropdownlist
            // Danh sách nhân viên để tạo phép dùm
            var builderEmp = Builders<Employee>.Filter;
            var filterEmp = builderEmp.Eq(m => m.Enable, true);
            filterEmp = filterEmp & !builderEmp.Eq(m => m.UserName, Constants.System.account);
            // Remove cấp cao ra (theo mã số lương)
            filterEmp = filterEmp & !builderEmp.In(m => m.NgachLuong, new string[] { "C.01", "C.02", "C.03" });
            var employees = await dbContext.Employees.Find(filterEmp).SortBy(m => m.FullName).ToListAsync();

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
            var approves = new List<IdName>();
            if (!string.IsNullOrEmpty(userInformation.ManagerId))
            {
                var approveEntity = dbContext.Employees.Find(m => m.Id.Equals(userInformation.ManagerId)).FirstOrDefault();
                if (approveEntity != null)
                {
                    approves.Add(new IdName
                    {
                        Id = approveEntity.Id,
                        Name = approveEntity.FullName
                    });
                }
            }
            var rolesApprove = dbContext.RoleUsers.Find(m => m.Enable.Equals(true) && m.Role.Equals(Constants.Rights.XacNhanCong)).ToList();
            foreach (var roleApprove in rolesApprove)
            {
                approves.Add(new IdName
                {
                    Id = roleApprove.User,
                    Name = roleApprove.FullName
                });
            }
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
            var dayworking = Utility.BusinessDaysUntil(fromDate, toDate);
            if (toDate > DateTime.Now.Date)
            {
                dayworking = Utility.BusinessDaysUntil(fromDate, DateTime.Now);
            }
            ViewData["DayWorking"] = dayworking;
            #endregion

            #region Filter
            var builder = Builders<EmployeeWorkTimeLog>.Filter;
            var filter = builder.Eq(m => m.EmployeeId, id);
            filter = filter & builder.Gt(m => m.Date, fromDate.AddDays(-1)) & builder.Lt(m => m.Date, toDate.AddDays(1));

            var builderSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
            var filterSum = builderSum.Eq(m => m.EmployeeId, id);
            filterSum = filterSum & builderSum.Eq(m => m.Month, month) & builderSum.Eq(m => m.Year, year);
            #endregion

            var timekeepings = await dbContext.EmployeeWorkTimeLogs.Find(filter).SortByDescending(m => m.Date).ToListAsync();
            var monthsTimes = await dbContext.EmployeeWorkTimeMonthLogs.Find(filterSum).SortByDescending(m => m.LastUpdated).ToListAsync();

            var viewModel = new TimeKeeperViewModel
            {
                EmployeeWorkTimeLogs = timekeepings,
                Employee = userInformation,
                EmployeeWorkTimeMonthLogs = monthsTimes,
                //EmployeeWorkTimeMonthLog = monthTime,
                Employees = employees,
                MonthYears = sortTimes,
                StartWorkingDate = fromDate,
                EndWorkingDate = toDate,
                Approves = approves,
                id = id,
                thang = thang
            };
            return View(viewModel);
        }

        [Route(Constants.LinkTimeKeeper.Approvement)]
        public async Task<IActionResult> Approvement(string id, string thang, string phep)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var userInformation = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(login)).FirstOrDefault();
            if (userInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }
            bool isRight = false;
            if (loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.XacNhanCong, (int)ERights.Add))
            {
                isRight = true;
            }
            //if (!isRight)
            //{
            //    return RedirectToAction("AccessDenied", "Account");
            //}
            #endregion

            id = string.IsNullOrEmpty(id) ? login : id;
            if (id != login)
            {
                userInformation = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(id)).FirstOrDefault();
            }

            var myDepartment = userInformation.Department;

            #region Dropdownlist
            // Danh sách nhân viên để tạo phép dùm
            var builderEmp = Builders<Employee>.Filter;
            var filterEmp = builderEmp.Eq(m => m.Enable, true);
            filterEmp = filterEmp & !builderEmp.Eq(m => m.UserName, Constants.System.account);
            // Remove cấp cao ra (theo mã số lương)
            filterEmp = filterEmp & !builderEmp.In(m => m.NgachLuong, new string[] { "C.01", "C.02", "C.03" });
            var employees = await dbContext.Employees.Find(filterEmp).SortBy(m => m.FullName).ToListAsync();

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
            var approves = new List<IdName>();
            if (!string.IsNullOrEmpty(userInformation.ManagerId))
            {
                var approveEntity = dbContext.Employees.Find(m => m.Id.Equals(userInformation.ManagerId)).FirstOrDefault();
                if (approveEntity != null)
                {
                    approves.Add(new IdName
                    {
                        Id = approveEntity.Id,
                        Name = approveEntity.FullName
                    });
                }
            }
            var rolesApprove = dbContext.RoleUsers.Find(m => m.Enable.Equals(true) && m.Role.Equals(Constants.Rights.XacNhanCong)).ToList();
            foreach (var roleApprove in rolesApprove)
            {
                approves.Add(new IdName
                {
                    Id = roleApprove.User,
                    Name = roleApprove.FullName
                });
            }
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
            #endregion

            #region Filter
            var builder = Builders<EmployeeWorkTimeLog>.Filter;
            var filter = builder.Eq(m => m.ConfirmId, login) & builder.Eq(m => m.Month, month) & builder.Eq(m => m.Year, year);

            var builderSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
            var filterSum = builderSum.Eq(m => m.EmployeeId, id);
            filterSum = filterSum & builderSum.Eq(m => m.Month, month) & builderSum.Eq(m => m.Year, year);
            #endregion

            var timekeepings = await dbContext.EmployeeWorkTimeLogs.Find(filter).SortByDescending(m => m.Date).ToListAsync();
            var viewModel = new TimeKeeperViewModel
            {
                EmployeeWorkTimeLogs = timekeepings,
                Employee = userInformation,
                //EmployeeWorkTimeMonthLog = monthTime,
                MonthYears = sortTimes,
                StartWorkingDate = fromDate,
                EndWorkingDate = toDate,
                Approves = approves,
                thang = thang,
                RightRequest = isRight
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkTimeKeeper.Request)]
        public async Task<IActionResult> RequestTimeKeeper(TimeKeeperViewModel viewModel)
        {
            var model = viewModel.EmployeeWorkTimeLog;
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var userInformation = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(login)).FirstOrDefault();
            if (userInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }
            #endregion

            // Update status
            var entity = dbContext.EmployeeWorkTimeLogs.Find(m => m.Id.Equals(model.Id)).FirstOrDefault();
            string secureCode = Helper.HashedPassword(Guid.NewGuid().ToString("N").Substring(0, 12));
            // Tự yêu cầu
            var employee = userInformation;
            // Làm cho người khác
            if (entity.EmployeeId != login)
            {
                employee = dbContext.Employees.Find(m => m.Id.Equals(entity.EmployeeId)).FirstOrDefault();
            }

            var approveEntity = new Employee();
            if (!string.IsNullOrEmpty(model.ConfirmId))
            {
                approveEntity = dbContext.Employees.Find(m => m.Id.Equals(model.ConfirmId)).FirstOrDefault();
            }
            else
            {
                // Update later. Get first HR have right Xac nhan cong

                approveEntity = dbContext.Employees.Find(m => true).FirstOrDefault();
            }

            var confirmName = approveEntity.FullName;
            #region Update Status
            var builderEmployeeWorkTimeLog = Builders<EmployeeWorkTimeLog>.Filter;
            var filterEmployeeWorkTimeLog = builderEmployeeWorkTimeLog.Eq(m => m.Id, entity.Id);
            var updateEmployeeWorkTimeLog = Builders<EmployeeWorkTimeLog>.Update.Set(m => m.Status, 2)
                .Set(m => m.Request, login)
                .Set(m => m.RequestDate, DateTime.Now.Date)
                .Set(m => m.Reason, model.Reason)
                .Set(m => m.ReasonDetail, model.ReasonDetail)
                .Set(m => m.ConfirmId, approveEntity.Id)
                .Set(m => m.ConfirmName, approveEntity.FullName)
                .Set(m => m.SecureCode, secureCode);
            dbContext.EmployeeWorkTimeLogs.UpdateOne(filterEmployeeWorkTimeLog, updateEmployeeWorkTimeLog);
            #endregion

            #region Send Mail
            var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = approveEntity.FullName, Address = approveEntity.Email }
            };
            var phone = string.Empty;
            if (employee.Mobiles != null & employee.Mobiles.Count > 0)
            {
                phone = employee.Mobiles[0].Number;
            }

            var webRoot = Environment.CurrentDirectory;
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "TimeKeeperRequest.html";

            #region parameters
            //{0} : Subject
            //{1} : Nguoi duyet 
            //{2} : Nguoi gui yeu cau  - Nguoi tao yeu cau (dùm)
            //{3} : v3
            //{4} : Email
            //{5} : Chức vụ
            //{6} : Nội dung chấm công (ngay, in, out,....)
            //{7} : Lý do
            //{8} : Số điện thoại liên hệ
            //{9}: Link đồng ý
            //{10}: Link từ chối
            //{11}: Link chi tiết
            //{12}: Website
            #endregion
            var subject = "Hỗ trợ xác nhận công.";
            var requester = employee.FullName;
            var var3 = employee.FullName;
            //if (!string.IsNullOrEmpty(employee.Title))
            //{
            //    requester += " - " + employee.Title;
            //}
            if (entity.EmployeeId != login)
            {
                requester += " (người tạo xác nhận: " + userInformation.FullName + " , chức vụ " + userInformation.Title + ")";
            }
            var inTime = entity.In.HasValue ? entity.In.Value.ToString(@"hh\:mm") : string.Empty;
            var outTime = entity.Out.HasValue ? entity.Out.Value.ToString(@"hh\:mm") : string.Empty;
            var lateTime = entity.Late.TotalMilliseconds > 0 ? Math.Round(entity.Late.TotalMinutes, 0).ToString() : "0";
            var earlyTime = entity.Early.TotalMilliseconds > 0 ? Math.Round(entity.Early.TotalMinutes, 0).ToString() : "0";
            var sumTime = string.Empty;
            if (string.IsNullOrEmpty(inTime) && string.IsNullOrEmpty(outTime))
            {
                sumTime = "1 ngày";
            }
            else if (string.IsNullOrEmpty(inTime) || string.IsNullOrEmpty(outTime))
            {
                sumTime = "0.5 ngày";
            }
            var minutesMissing = TimeSpan.FromMilliseconds(entity.Late.TotalMilliseconds + entity.Early.TotalMilliseconds).TotalMinutes;
            if (minutesMissing > 0)
            {
                if (!string.IsNullOrEmpty(sumTime))
                {
                    sumTime += ", ";
                }
                sumTime += Math.Round(minutesMissing, 0) + " phút";
            }

            var detailTimeKeeping = "Ngày: " + entity.Date.ToString("dd/MM/yyyy") + "; thiếu: " + sumTime;
            if (!string.IsNullOrEmpty(inTime))
            {
                detailTimeKeeping += " | giờ vào: " + inTime + "; trễ: " + lateTime;
            }
            else
            {
                detailTimeKeeping += " | giờ vào: --; trễ: --";
            }
            if (!string.IsNullOrEmpty(outTime))
            {
                detailTimeKeeping += "; giờ ra: " + outTime + "; sớm: " + earlyTime;
            }
            else
            {
                detailTimeKeeping += "; giờ ra: --; sớm: --";
            }
            // Api update, generate code.
            var linkapprove = Constants.System.domain + "/xacnhan/cong";
            var linkAccept = linkapprove + "?id=" + entity.Id + "&approve=3&secure=" + secureCode;
            var linkCancel = linkapprove + "?id=" + entity.Id + "&approve=4&secure=" + secureCode;
            var linkDetail = Constants.System.domain;
            var bodyBuilder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
            }
            string messageBody = string.Format(bodyBuilder.HtmlBody,
                subject,
                confirmName,
                requester,
                var3,
                employee.Email,
                employee.Title,
                detailTimeKeeping,
                model.Reason,
                model.ReasonDetail,
                phone,
                linkAccept,
                linkCancel,
                linkDetail,
                Constants.System.domain
                );

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                //CCAddresses = ccs,
                Subject = subject,
                BodyContent = messageBody,
                Type = "ho-tro-xac-nhan-cong"
            };

            // For faster. Add to schedule.
            // Send later
            var scheduleEmail = new ScheduleEmail
            {
                Status = (int)EEmailStatus.Schedule,
                //From = emailMessage.FromAddresses,
                To = emailMessage.ToAddresses,
                CC = emailMessage.CCAddresses,
                BCC = emailMessage.BCCAddresses,
                Type = emailMessage.Type,
                Title = emailMessage.Subject,
                Content = emailMessage.BodyContent
            };
            dbContext.ScheduleEmails.InsertOne(scheduleEmail);

            //_emailSender.SendEmail(emailMessage);

            #endregion

            return Json(new { result = true, message = "Yêu cầu được gửi các bộ phận liên quan." });
        }

        [Route(Constants.LinkTimeKeeper.AprrovePost)]
        [AllowAnonymous]
        [HttpPost]
        public IActionResult AprrovePost(string id, int approve, string secure)
        {
            var viewModel = new TimeKeeperViewModel
            {
                Approve = approve
            };
            var entity = dbContext.EmployeeWorkTimeLogs.Find(m => m.Id.Equals(id)).FirstOrDefault();

            #region Extensions
            var builderTraining = Builders<Trainning>.Filter;
            var filterTraining = builderTraining.Eq(m => m.Enable, true);
            filterTraining = filterTraining & builderTraining.Eq(m => m.Type, "anh-van");
            var listTraining = dbContext.Trainnings.Find(filterTraining).Limit(10).SortByDescending(m => m.CreatedOn).ToList();
            viewModel.ListTraining = listTraining;
            #endregion

            if (entity == null)
            {
                ViewData["Status"] = "Rất tiếc! Dữ liệu đã được cập nhật hoặc thông tin không tồn tại trên hệ thống.";

                return View(viewModel);
            }

            if (entity.SecureCode != secure && entity.Status != 2)
            {
                return Json(new { result = true, message = "Rất tiếc! Dữ liệu đã được cập nhật hoặc thông tin không tồn tại trên hệ thống." });
            }

            viewModel.EmployeeWorkTimeLog = entity;

            #region Update status
            var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, id);
            var update = Builders<EmployeeWorkTimeLog>.Update
                .Set(m => m.SecureCode, Helper.HashedPassword(Guid.NewGuid().ToString("N").Substring(0, 12)))
                .Set(m => m.Status, approve)
                .Set(m => m.ConfirmDate, DateTime.Now.Date);
            dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
            #endregion

            #region update Summary
            if (approve == 3)
            {
                var monthDate = Utility.EndWorkingMonthByDate(entity.Date);
                var builderUpdateSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
                var filterUpdateSum = builderUpdateSum.Eq(m => m.EmployeeId, entity.EmployeeId);
                filterUpdateSum = filterUpdateSum & builderUpdateSum.Eq(m => m.Year, monthDate.Year);
                filterUpdateSum = filterUpdateSum & builderUpdateSum.Eq(m => m.Month, monthDate.Month);

                double dateInc = 0;
                double worktimeInc = 0;
                double lateInc = 0;
                double earlyInc = 0;

                // Update 1 date
                if (!entity.In.HasValue && !entity.Out.HasValue)
                {
                    dateInc += 1;
                    worktimeInc += new TimeSpan(8, 0, 0).TotalMilliseconds;
                }
                else if (!entity.In.HasValue || !entity.Out.HasValue)
                {
                    dateInc += 0.5;
                    worktimeInc += new TimeSpan(4, 0, 0).TotalMilliseconds;
                }

                if (entity.Late.TotalMilliseconds > 0)
                {
                    worktimeInc += entity.Late.TotalMilliseconds;
                    lateInc += entity.Late.TotalMilliseconds;
                }
                if (entity.Early.TotalMilliseconds > 0)
                {
                    worktimeInc += entity.Early.TotalMilliseconds;
                    earlyInc += entity.Early.TotalMilliseconds;
                }

                var updateSum = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Inc(m => m.Workday, dateInc)
                    .Inc(m => m.WorkTime, worktimeInc)
                    .Inc(m => m.Late, -(lateInc))
                    .Inc(m => m.Early, -(earlyInc))
                    .Set(m => m.LastUpdated, DateTime.Now);

                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterUpdateSum, updateSum);
            }
            #endregion

            #region Tracking everything

            #endregion

            var approvement = dbContext.Employees.Find(m => m.Id.Equals(entity.ConfirmId)).FirstOrDefault();
            // Tự yêu cầu
            //bool seftFlag = entity.EmployeeId == entity.Request ? true : false;
            var employee = dbContext.Employees.Find(m => m.Id.Equals(entity.EmployeeId)).FirstOrDefault();
            //var userCreate = employee;
            //if (!seftFlag)
            //{
            //    userCreate = dbContext.Employees.Find(m => m.Id.Equals(entity.CreatedBy)).FirstOrDefault();
            //}

            #region Send email to user leave
            var requester = employee.FullName;
            var tos = new List<EmailAddress>();
            var ccs = new List<EmailAddress>();
            tos.Add(new EmailAddress { Name = employee.FullName, Address = employee.Email });

            // Send mail to HR: if approve = 3 (dong y);
            if (approve == 3)
            {
                var listHrRoles = dbContext.RoleUsers.Find(m => m.Role.Equals(Constants.Rights.NhanSu) && (m.Expired.Equals(null) || m.Expired > DateTime.Now)).ToList();
                if (listHrRoles != null && listHrRoles.Count > 0)
                {
                    foreach (var item in listHrRoles)
                    {
                        if (item.Action == 3)
                        {
                            var fields = Builders<Employee>.Projection.Include(p => p.Email).Include(p => p.FullName);
                            var emailEntity = dbContext.Employees.Find(m => m.Id.Equals(item.User)).Project<Employee>(fields).FirstOrDefault();
                            if (emailEntity != null)
                            {
                                tos.Add(new EmailAddress { Name = emailEntity.FullName, Address = emailEntity.Email });
                            }
                        }
                    }
                }
                requester += " , HR";
            }

            #region UAT
            //tos = new List<EmailAddress>
            //            {
            //                new EmailAddress { Name = "Xuan", Address = "xuan.tm1988@gmail.com" }
            //            };

            //ccs = new List<EmailAddress>
            //            {
            //                new EmailAddress { Name = "Xuan CC", Address = "xuantranm@gmail.com" }
            //            };
            #endregion

            var webRoot = Environment.CurrentDirectory;
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "TimeKeeperConfirm.html";

            var subject = "Xác nhận công.";
            var status = approve == 3 ? "Đồng ý" : "Không duyệt";
            var inTime = entity.In.HasValue ? entity.In.Value.ToString(@"hh\:mm") : string.Empty;
            var outTime = entity.Out.HasValue ? entity.Out.Value.ToString(@"hh\:mm") : string.Empty;
            var lateTime = entity.Late.TotalMilliseconds > 0 ? Math.Round(entity.Late.TotalMinutes, 0).ToString() : "0";
            var earlyTime = entity.Early.TotalMilliseconds > 0 ? Math.Round(entity.Early.TotalMinutes, 0).ToString() : "0";
            var sumTime = string.Empty;
            if (string.IsNullOrEmpty(inTime) && string.IsNullOrEmpty(outTime))
            {
                sumTime = "1 ngày";
            }
            else if (string.IsNullOrEmpty(inTime) || string.IsNullOrEmpty(outTime))
            {
                sumTime = "0.5 ngày";
            }
            var minutesMissing = TimeSpan.FromMilliseconds(entity.Late.TotalMilliseconds + entity.Early.TotalMilliseconds).TotalMinutes;
            if (minutesMissing > 0)
            {
                if (!string.IsNullOrEmpty(sumTime))
                {
                    sumTime += ", ";
                }
                sumTime += Math.Round(minutesMissing, 0) + " phút";
            }

            var detailTimeKeeping = "Ngày: " + entity.Date.ToString("dd/MM/yyyy") + "; thiếu: " + sumTime;
            if (!string.IsNullOrEmpty(inTime))
            {
                detailTimeKeeping += " | giờ vào: " + inTime + "; trễ: " + lateTime;
            }
            else
            {
                detailTimeKeeping += " | giờ vào: --; trễ: --";
            }
            if (!string.IsNullOrEmpty(outTime))
            {
                detailTimeKeeping += "; giờ ra: " + outTime + "; sớm: " + earlyTime;
            }
            else
            {
                detailTimeKeeping += "; giờ ra: --; sớm: --";
            }

            // Api update, generate code.
            var linkDetail = Constants.System.domain;
            var bodyBuilder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
            }
            string messageBody = string.Format(bodyBuilder.HtmlBody,
                subject,
                requester,
                status,
                approvement.FullName,
                approvement.Email,
                approvement.Title,
                detailTimeKeeping,
                entity.Reason,
                entity.ReasonDetail,
                linkDetail,
                Constants.System.domain
                );
            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                CCAddresses = ccs,
                Subject = subject,
                BodyContent = messageBody,
                Type = "xac-nhan-cong"
            };

            // For faster. Add to schedule.
            // Send later
            var scheduleEmail = new ScheduleEmail
            {
                Status = (int)EEmailStatus.Schedule,
                //From = emailMessage.FromAddresses,
                To = emailMessage.ToAddresses,
                CC = emailMessage.CCAddresses,
                BCC = emailMessage.BCCAddresses,
                Type = emailMessage.Type,
                Title = emailMessage.Subject,
                Content = emailMessage.BodyContent
            };
            dbContext.ScheduleEmails.InsertOne(scheduleEmail);
            //_emailSender.SendEmail(emailMessage);
            #endregion

            return Json(new { result = true, message = "Cám ơn đã xác nhận, kết quả đang gửi cho người liên quan." });
        }

        #region TIMER

        [Route(Constants.LinkTimeKeeper.Timer)]
        public async Task<IActionResult> BangChamCong(string thang, string phongban, string id)
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

            // Danh sách nhân viên để tạo phép dùm
            // Remove cấp cao ra (theo mã số lương)
            var builderEmployee = Builders<Employee>.Filter;
            var filterEmployee = builderEmployee.Eq(m => m.Enable, true);
            filterEmployee = filterEmployee & !builderEmployee.Eq(m => m.UserName, Constants.System.account);
            filterEmployee = filterEmployee & !builderEmployee.In(m => m.NgachLuong, new string[] { "C.01", "C.02", "C.03" });
            var employees = await dbContext.Employees.Find(filterEmployee).SortBy(m => m.FullName).ToListAsync();

            var departments = dbContext.Departments.Find(m => m.Enable.Equals(true)).SortBy(m => m.Order).ToList();

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

            #region Filter
            var builder = Builders<EmployeeWorkTimeLog>.Filter;
            var filter = builder.Eq(m => m.Enable, true) & builder.Eq(m => m.Month, month) & builder.Eq(m => m.Year, year);

            if (!string.IsNullOrEmpty(phongban))
            {
                filter = filter & builder.Eq(x => x.Department, phongban.Trim());
            }
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(m => m.EmployeeId, id.Trim());
            }
            #endregion

            var times = await dbContext.EmployeeWorkTimeLogs.Find(filter).SortBy(m => m.Date).ToListAsync();

            var viewModel = new BangLuongViewModel
            {
                MonthYears = sortTimes,
                thang = thang,
                EmployeeWorkTimeLogs = times,
                FromDate = fromDate,
                ToDate = toDate,
                Employees = employees,
                Departments = departments,
                phongban = phongban,
                Id = id
            };

            return View(viewModel);
        }

        [Route(Constants.LinkTimeKeeper.Timer + "/" + Constants.ActionLink.Export)]
        public async Task<IActionResult> BangChamCongExport(string thang, string phongban, string id)
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

            // Danh sách nhân viên để tạo phép dùm
            // Remove cấp cao ra (theo mã số lương)
            var builderEmployee = Builders<Employee>.Filter;
            var filterEmployee = builderEmployee.Eq(m => m.Enable, true);
            filterEmployee = filterEmployee & !builderEmployee.Eq(m => m.UserName, Constants.System.account);
            filterEmployee = filterEmployee & !builderEmployee.In(m => m.NgachLuong, new string[] { "C.01", "C.02", "C.03" });
            var employees = await dbContext.Employees.Find(filterEmployee).SortBy(m => m.FullName).ToListAsync();

            var departments = dbContext.Departments.Find(m => m.Enable.Equals(true)).SortBy(m => m.Order).ToList();

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

            #region Filter
            var builder = Builders<EmployeeWorkTimeLog>.Filter;
            var filter = builder.Eq(m => m.Enable, true) & builder.Eq(m => m.Month, month) & builder.Eq(m => m.Year, year);

            if (!string.IsNullOrEmpty(phongban))
            {
                filter = filter & builder.Eq(x => x.Department, phongban.Trim());
            }
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(m => m.EmployeeId, id.Trim());
            }
            #endregion

            var times = await dbContext.EmployeeWorkTimeLogs.Find(filter).SortBy(m => m.Date).ToListAsync();

            var viewModel = new BangLuongViewModel
            {
                MonthYears = sortTimes,
                thang = thang,
                EmployeeWorkTimeLogs = times,
                FromDate = fromDate,
                ToDate = toDate,
                Employees = employees,
                Departments = departments,
                phongban = phongban,
                Id = id
            };

            return View(viewModel);
        }
        #endregion

        #region Sub Data
        [HttpPost]
        [Route(Constants.LinkTimeKeeper.Item)]
        public IActionResult Item(string id)
        {
            var item = dbContext.EmployeeWorkTimeLogs.Find(m => m.Id.Equals(id)).First();
            // format in c#
            var result = new TimeLogFormat
            {
                Id = item.Id,
                Date = item.Date.ToString("dd/MM/yyyy"),
                In = item.In.HasValue ? item.In.Value.ToString(@"hh\:mm") : string.Empty,
                Out = item.Out.HasValue ? item.Out.Value.ToString(@"hh\:mm") : string.Empty,
                Late = item.In.HasValue ? item.Late.ToString(@"hh\:mm") : string.Empty,
                Early = item.Out.HasValue ? item.Early.ToString(@"hh\:mm") : string.Empty
            };
            return Json(result);
        }

        [HttpPost]
        [Route(Constants.LinkTimeKeeper.ReasonRule)]
        public IActionResult ReasonRule(string id, string reason)
        {
            var currentLog = dbContext.EmployeeWorkTimeLogs.Find(m => m.Id.Equals(id)).FirstOrDefault();
            var employee = currentLog.EmployeeId;
            var result = true;
            var lastLogForget = new EmployeeWorkTimeLog();
            var lastLogOther = new List<EmployeeWorkTimeLog>();
            var approves = new List<IdName>();
            var approvesHr = new List<IdName>();
            var rolesApprove = dbContext.RoleUsers.Find(m => m.Enable.Equals(true) && m.Role.Equals(Constants.Rights.XacNhanCong)).ToList();
            foreach (var roleApprove in rolesApprove)
            {
                approvesHr.Add(new IdName
                {
                    Id = roleApprove.User,
                    Name = roleApprove.FullName
                });
            }

            var employeeE = dbContext.Employees.Find(m => m.Id.Equals(employee)).FirstOrDefault();
            var listLyDoQuanLyDuyet = new List<string>
            {
                "Đi công tác",
                "Quên chấm công",
                "Lý do khác"
            };
            if (listLyDoQuanLyDuyet.Contains(reason))
            {
                var statusCheck = new List<int>()
                    {
                        (int)StatusWork.DaGuiXacNhan,
                        (int)StatusWork.DongY
                    };
                if (reason == "Quên chấm công")
                {
                    // Cho phép 1 lần gửi trong 1 tháng.
                    lastLogForget = dbContext.EmployeeWorkTimeLogs.Find(m => m.Enable.Equals(true)
                                        && m.EmployeeId.Equals(employee)
                                        && m.Month.Equals(currentLog.Month)
                                        && m.Year.Equals(currentLog.Year)
                                        && m.Reason.Equals("Quên chấm công")
                                        && statusCheck.Contains(m.Status)).FirstOrDefault();

                    if (lastLogForget != null)
                    {
                        result = false;
                    }
                }
                else if (reason == "Lý do khác")
                {
                    lastLogOther = dbContext.EmployeeWorkTimeLogs.Find(m => m.Enable.Equals(true)
                                        && m.EmployeeId.Equals(employee)
                                        && m.Month.Equals(currentLog.Month)
                                        && m.Year.Equals(currentLog.Year)
                                        && m.Reason.Equals("Lý do khác")
                                        && statusCheck.Contains(m.Status)).ToList();

                    if (lastLogOther.Count >= 5)
                    {
                        result = false;
                    }
                }

                var approveEntity = dbContext.Employees.Find(m => m.Id.Equals(employeeE.ManagerId)).FirstOrDefault();
                if (approveEntity != null)
                {
                    approves.Add(new IdName
                    {
                        Id = approveEntity.Id,
                        Name = approveEntity.FullName
                    });
                }
                else
                {
                    approves = approvesHr;
                }
            }
            else
            {
                approves = approvesHr;
            }

            return Json(new { error = 0, reason, result, approves, lastLogForget, lastLogOther });
        }

        [Route("cap-nhat-department")]
        public async Task<IActionResult> UpdateDepartment()
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var userInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (userInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }
            if (loginUserName != Constants.System.account)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            var times = await dbContext.EmployeeWorkTimeLogs.Find(m => m.Enable.Equals(true) && m.Status.Equals(3)).ToListAsync();
            foreach (var time in times)
            {
                if (time != null && !string.IsNullOrEmpty(time.EmployeeId))
                {
                    var employee = dbContext.Employees.Find(m => m.Id.Equals(time.EmployeeId)).FirstOrDefault();
                    if (employee != null)
                    {
                        var employeeLocation = employee.Workplaces.FirstOrDefault(a => a.Enable.Equals(true) && !string.IsNullOrEmpty(a.Fingerprint));
                        var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, time.Id);
                        var update = Builders<EmployeeWorkTimeLog>.Update
                            .Set(m => m.WorkplaceCode, employeeLocation.Code)
                            .Set(m => m.EnrollNumber, employeeLocation.Fingerprint)
                            .Set(m => m.EmployeeName, employee.FullName)
                            .Set(m => m.EmployeeTitle, employee.Title)
                            .Set(m => m.Part, employee.Part)
                            .Set(m => m.Department, employee.Department);
                        dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
                    }
                }
                else
                {
                    var missingEM = time;
                }
            }

            return Json(new { result = true });
        }

        [Route("cap-nhat-ma-cham-cong")]
        public async Task<IActionResult> UpdateFinger()
        {
            var location = "NM";
            var times = await dbContext.EmployeeWorkTimeLogs.Find(m => m.Enable.Equals(true) && m.WorkplaceCode.Equals(location) && string.IsNullOrEmpty(m.EnrollNumber)).ToListAsync();
            foreach (var time in times)
            {
                var employee = dbContext.Employees.Find(m => m.Id.Equals(time.EmployeeId)).FirstOrDefault();
                if (employee != null)
                {
                    var employeeLocation = employee.Workplaces.FirstOrDefault(a => a.Code == location);
                    if (!string.IsNullOrEmpty(employeeLocation.Fingerprint))
                    {
                        var builder = Builders<EmployeeWorkTimeLog>.Filter;
                        var filter = builder.Eq(m => m.Id, time.Id);
                        var update = Builders<EmployeeWorkTimeLog>.Update
                            .Set(m => m.EnrollNumber, employeeLocation.Fingerprint);
                        dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
                    }
                }
            }

            location = "VP";
            times = await dbContext.EmployeeWorkTimeLogs.Find(m => m.Enable.Equals(true) && m.WorkplaceCode.Equals(location) && string.IsNullOrEmpty(m.EnrollNumber)).ToListAsync();
            foreach (var time in times)
            {
                var employee = dbContext.Employees.Find(m => m.Id.Equals(time.EmployeeId)).FirstOrDefault();
                if (employee != null)
                {
                    var employeeLocation = employee.Workplaces.FirstOrDefault(a => a.Code == location);
                    if (!string.IsNullOrEmpty(employeeLocation.Fingerprint))
                    {
                        var builder = Builders<EmployeeWorkTimeLog>.Filter;
                        var filter = builder.Eq(m => m.Id, time.Id);
                        var update = Builders<EmployeeWorkTimeLog>.Update
                            .Set(m => m.EnrollNumber, employeeLocation.Fingerprint);
                        dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);
                    }
                }
            }
            return Json(new { result = true });
        }

        #endregion
    }
}
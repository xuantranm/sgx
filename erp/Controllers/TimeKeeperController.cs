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
using NPOI.HSSF.Util;
using NPOI.SS.Util;

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
            bool quyenXacNhanCong = false;
            if (loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.XacNhanCongDum, (int)ERights.Add))
            {
                quyenXacNhanCong = true;
            }
            bool quyenBangCong = false;
            if (loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.BangChamCong, (int)ERights.View))
            {
                quyenBangCong = true;
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
            var sortTimes = monthYears.OrderByDescending(x => x.Month).OrderByDescending(x => x.Year).ToList();
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
                MonthYears = sortTimes,
                StartWorkingDate = fromDate,
                EndWorkingDate = toDate,
                Approves = approves,
                Thang = thang,
                RightRequest = quyenXacNhanCong,
                RightManager = quyenBangCong,
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

            bool quyenXacNhan = false;
            if (loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.XacNhanCongDum, (int)ERights.Add))
            {
                quyenXacNhan = true;
            }
            if (!quyenXacNhan)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            bool quyenBangCong = false;
            if (loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.BangChamCong, (int)ERights.View))
            {
                quyenBangCong = true;
            }
            #endregion

            id = string.IsNullOrEmpty(id) ? login : id;
            if (id != login)
            {
                userInformation = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.Id.Equals(id)).FirstOrDefault();
            }

            #region Dropdownlist
            // Danh sách nhân viên để tạo phép dùm
            var builderEmp = Builders<Employee>.Filter;
            var filterEmp = builderEmp.Eq(m => m.Enable, true) & builderEmp.Eq(m => m.Leave, false);
            filterEmp = filterEmp & !builderEmp.Eq(m => m.UserName, Constants.System.account);
            // Remove cấp cao ra (theo mã số lương)
            filterEmp = filterEmp & !builderEmp.In(m => m.NgachLuongCode, new string[] { "C.01", "C.02", "C.03" });
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
            var sortTimes = monthYears.OrderByDescending(x => x.Month).OrderByDescending(x => x.Year).ToList();
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
                Employees = employees,
                MonthYears = sortTimes,
                StartWorkingDate = fromDate,
                EndWorkingDate = toDate,
                Approves = approves,
                Id = id,
                Thang = thang,
                RightManager = quyenBangCong
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

            var myDepartment = userInformation.PhongBan;

            #region Dropdownlist
            // Danh sách nhân viên để tạo phép dùm
            var builderEmp = Builders<Employee>.Filter;
            var filterEmp = builderEmp.Eq(m => m.Enable, true);
            filterEmp = filterEmp & !builderEmp.Eq(m => m.UserName, Constants.System.account);
            // Remove cấp cao ra (theo mã số lương)
            filterEmp = filterEmp & !builderEmp.In(m => m.NgachLuongCode, new string[] { "C.01", "C.02", "C.03" });
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
            var sortTimes = monthYears.OrderByDescending(x => x.Month).OrderByDescending(x => x.Year).ToList();
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

            var timers = new List<TimeKeeperDisplay>();
            foreach (var time in timekeepings)
            {
                var enrollNumber = string.Empty;
                var chucvuName = string.Empty;
                var employee = dbContext.Employees.Find(m => m.Id.Equals(time.EmployeeId)).FirstOrDefault();
                if (!string.IsNullOrEmpty(employee.ChucVu))
                {
                    var cvE = dbContext.ChucVus.Find(m => m.Id.Equals(employee.ChucVu)).FirstOrDefault();
                    if (cvE != null)
                    {
                        chucvuName = cvE.Name;
                    }
                }

                var employeeDisplay = new TimeKeeperDisplay()
                {
                    EmployeeWorkTimeLogs = new List<EmployeeWorkTimeLog>() {
                        time
                    },
                    Code = employee.Code + "(" + employee.CodeOld + ")",
                    FullName = employee.FullName,
                    ChucVu = chucvuName
                };
                timers.Add(employeeDisplay);
            }
            var viewModel = new TimeKeeperViewModel
            {
                TimeKeeperDisplays = timers,
                Employee = userInformation,
                MonthYears = sortTimes,
                StartWorkingDate = fromDate,
                EndWorkingDate = toDate,
                Approves = approves,
                Thang = thang,
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

            var chucvuE = dbContext.ChucVus.Find(m => m.Id.Equals(employee.ChucVu)).FirstOrDefault();
            var chucvu = chucvuE != null ? chucvuE.Name : string.Empty;

            var approveEntity = new Employee();
            if (!string.IsNullOrEmpty(model.ConfirmId))
            {
                approveEntity = dbContext.Employees.Find(m => m.Id.Equals(model.ConfirmId)).FirstOrDefault();
            }
            else
            {
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
            if (employee.Mobiles != null && employee.Mobiles.Count > 0)
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
            if (entity.EmployeeId != login)
            {
                requester += " (người tạo xác nhận: " + userInformation.FullName + ")";
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
                chucvu,
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
                Subject = subject,
                BodyContent = messageBody,
                Type = "ho-tro-xac-nhan-cong",
                EmployeeId = employee.Id
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
                Content = emailMessage.BodyContent,
                EmployeeId = emailMessage.EmployeeId
            };
            dbContext.ScheduleEmails.InsertOne(scheduleEmail);
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
                ViewData["Status"] = Constants.ErrorParameter;

                return View(viewModel);
            }

            if (entity.SecureCode != secure && entity.Status != 2)
            {
                return Json(new { result = true, message = Constants.ErrorParameter });
            }

            viewModel.EmployeeWorkTimeLog = entity;

            #region Update status
            var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, id);
            var update = Builders<EmployeeWorkTimeLog>.Update
                .Set(m => m.SecureCode, Helper.HashedPassword(Guid.NewGuid().ToString("N").Substring(0, 12)))
                .Set(m => m.WorkDay, 1)
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
            var approvementchucvuE = dbContext.ChucVus.Find(m => m.Id.Equals(approvement.ChucVu)).FirstOrDefault();
            var approvementchucvu = approvementchucvuE != null ? approvementchucvuE.Name : string.Empty;
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
                approvementchucvu,
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
                Type = "xac-nhan-cong",
                EmployeeId = entity.EmployeeId
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
                Content = emailMessage.BodyContent,
                EmployeeId = emailMessage.EmployeeId
            };
            dbContext.ScheduleEmails.InsertOne(scheduleEmail);
            #endregion

            return Json(new { result = true, message = "Cám ơn đã xác nhận, kết quả đang gửi cho người liên quan." });
        }

        #region CHAM CONG
        [Route(Constants.LinkTimeKeeper.Timer)]
        public async Task<IActionResult> BangChamCong(DateTime Tu, DateTime Den, string Thang, string Nl, string Kcn, string Pb, string Bp, string Fg, string Id)
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

            var linkCurrent = string.Empty;

            #region DDL
            var sortTimes = Utility.DllMonths();
            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) 
                            && m.IsTimeKeeper.Equals(false) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();

            var congtychinhanhs = dbContext.CongTyChiNhanhs.Find(m => m.Enable.Equals(true)).ToList();
            Nl = string.IsNullOrEmpty(Nl) ? congtychinhanhs.First().Id : Nl;

            var khoichucnangs = dbContext.KhoiChucNangs.Find(m => m.Enable.Equals(true)).ToList();
            Kcn = string.IsNullOrEmpty(Kcn) ? khoichucnangs.Where(m => m.CongTyChiNhanhId.Equals(Nl)).First().Id : Kcn;

            var listPBRemove = new List<string>
            {
                "5c88d094d59d56225c43240f", // CHU TICH
                "5c88d094d59d56225c432412" // GIAM DOC
            };
            var phongbans = dbContext.PhongBans.Find(m => m.Enable.Equals(true) && m.KhoiChucNangId.Equals(Kcn) && !listPBRemove.Contains(m.Id)).ToList();
            Pb = string.IsNullOrEmpty(Pb) ? phongbans.Where(m => m.KhoiChucNangId.Equals(Kcn)).First().Id : Pb;

            var bophans = dbContext.BoPhans.Find(m => m.Enable.Equals(true) && m.PhongBanId.Equals(Pb) && string.IsNullOrEmpty(m.Parent)).ToList();
            //var chucvus = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).ToList();
            #endregion

            #region Times
            var today = DateTime.Now.Date;
            if (!string.IsNullOrEmpty(Thang))
            {
                Den = Utility.GetToDate(Thang);
                Tu = Den.AddMonths(-1).AddDays(1);
                var year = Den.Year;
                var month = Den.Month;
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Thang=" + Thang;
            }
            else
            {
                if (Den < Constants.MinDate)
                {
                    Den = today;
                }
                if (Tu < Constants.MinDate)
                {
                    var previous = Den.Day > 25 ? Den : Den.AddMonths(-1);
                    Tu = new DateTime(previous.Year, previous.Month, 26);
                }
            }
            #endregion

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = !builder.Eq(i => i.UserName, Constants.System.account) & builder.Eq(m => m.Enable, true) & builder.Eq(m => m.Leave, false);

            if (!string.IsNullOrEmpty(Id))
            {
                filter = filter & builder.Eq(x => x.Id, Id.Trim());
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Id=" + Id;
                var employeeEId = dbContext.Employees.Find(m => m.Id.Equals(Id)).FirstOrDefault();
                if (employeeEId != null)
                {
                    Nl = employeeEId.CongTyChiNhanh;
                    Kcn = employeeEId.KhoiChucNang;
                    Pb = employeeEId.PhongBan;
                    Bp = employeeEId.BoPhan;
                }
            }
            else
            {
                filter = filter & builder.Eq(m => m.CongTyChiNhanh, Nl);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Nl=" + Nl;

                filter = filter & builder.Eq(m => m.KhoiChucNang, Kcn);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Kcn=" + Kcn;

                filter = filter & builder.Eq(m => m.PhongBan, Pb);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Pb=" + Pb;

                if (!string.IsNullOrEmpty(Bp))
                {
                    filter = filter & builder.Eq(m => m.BoPhan, Bp);
                    linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                    linkCurrent += "Bp=" + Bp;
                }
            }

            if (!string.IsNullOrEmpty(Fg))
            {
                filter = filter & builder.Where(m => m.Workplaces.Any(c => c.Fingerprint == Fg.Trim()));
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Fg=" + Fg;
            }

            var fields = Builders<Employee>.Projection.Include(p => p.Id);
            var employeeFilters = dbContext.Employees.Find(filter).ToList();

            var employeeIds = dbContext.Employees.Find(filter).Project<Employee>(fields).ToList().Select(m => m.Id).ToList();

            var builderT = Builders<EmployeeWorkTimeLog>.Filter;
            var filterT = builderT.Eq(m => m.Enable, true)
                        & builderT.Gte(m => m.Date, Tu)
                        & builderT.Lte(m => m.Date, Den);
            if (employeeIds != null && employeeIds.Count > 0)
            {
                filterT = filterT & builderT.Where(m => employeeIds.Contains(m.EmployeeId));
            }
            #endregion

            var times = dbContext.EmployeeWorkTimeLogs.Find(filterT).SortBy(m => m.Date).ToList();

            var results = new List<TimeKeeperDisplay>();

            #region method 1: base employee
            foreach (var employee in employeeFilters)
            {
                var employeeWorkTimeLogs = times.Where(m => m.EmployeeId.Equals(employee.Id)).ToList();
                if (employeeWorkTimeLogs == null || employeeWorkTimeLogs.Count == 0) continue;

                var enrollNumber = string.Empty;
                if (employee.Workplaces != null && employee.Workplaces.Count > 0)
                {
                    foreach (var workplace in employee.Workplaces)
                    {
                        if (!string.IsNullOrEmpty(workplace.Fingerprint))
                        {
                            if (!string.IsNullOrEmpty(enrollNumber))
                            {
                                enrollNumber += ";";
                            }
                            enrollNumber += workplace.Code + ":" + workplace.Fingerprint;
                        }
                    }
                }

                results.Add(new TimeKeeperDisplay()
                {
                    EmployeeWorkTimeLogs = employeeWorkTimeLogs,
                    Id = employee.Id,
                    Code = employee.CodeOld,
                    EnrollNumber = enrollNumber,
                    FullName = employee.FullName,
                    CongTyChiNhanh = employee.CongTyChiNhanhName,
                    KhoiChucNang = employee.KhoiChucNangName,
                    PhongBan = employee.PhongBanName,
                    BoPhan = employee.BoPhanName,
                    ChucVu = employee.ChucVuName,
                    Alias = employee.AliasFullName,
                    Email = employee.Email,
                    ManageId = employee.ManagerId
                });
            }
            #endregion

            var viewModel = new TimeKeeperViewModel
            {
                TimeKeeperDisplays = results,
                MonthYears = sortTimes,
                Employees = employees,
                CongTyChiNhanhs = congtychinhanhs,
                KhoiChucNangs = khoichucnangs,
                PhongBans = phongbans,
                BoPhans = bophans,
                Thang = Thang,
                Tu = Tu,
                Den = Den,
                Id = Id,
                Fg = Fg,
                Nl = Nl,
                Kcn = Kcn,
                Pb = Pb,
                Bp = Bp,
                LinkCurrent = linkCurrent
            };

            return View(viewModel);
        }

        [Route(Constants.LinkTimeKeeper.Timer + "/" + Constants.ActionLink.Export)]
        public async Task<IActionResult> BangChamCongExport(DateTime Tu, DateTime Den, string Thang, string Nl, string Kcn, string Pb, string Bp, string Fg, string Id)
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

            var linkCurrent = string.Empty;
            #region DDL
            var sortTimes = Utility.DllMonths();
            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
            var congtychinhanhs = dbContext.CongTyChiNhanhs.Find(m => m.Enable.Equals(true)).ToList();
            var khoichucnangs = dbContext.KhoiChucNangs.Find(m => m.Enable.Equals(true)).ToList();
            var phongbans = dbContext.PhongBans.Find(m => m.Enable.Equals(true)).ToList();
            var bophans = dbContext.BoPhans.Find(m => m.Enable.Equals(true) && string.IsNullOrEmpty(m.Parent)).ToList();
            var chucvus = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).ToList();
            #endregion

            #region Times
            var today = DateTime.Now.Date;
            if (!string.IsNullOrEmpty(Thang))
            {
                Den = Utility.GetToDate(Thang);
                Tu = Den.AddMonths(-1).AddDays(1);
                var year = Den.Year;
                var month = Den.Month;
            }
            else
            {
                if (Den < Constants.MinDate)
                {
                    Den = today;
                }
                if (Tu < Constants.MinDate)
                {
                    var previous = Den.Day > 25 ? Den : Den.AddMonths(-1);
                    Tu = new DateTime(previous.Year, previous.Month, 26);
                }
            }
            #endregion

            string sFileName = @"cham-cong";

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = !builder.Eq(i => i.UserName, Constants.System.account) & builder.Eq(m => m.Enable, true) & builder.Eq(m => m.Leave, false);
            if (!string.IsNullOrEmpty(Id))
            {
                filter = filter & builder.Eq(x => x.Id, Id.Trim());
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Id=" + Id;
            }
            if (!string.IsNullOrEmpty(Fg))
            {
                filter = filter & builder.Where(m => m.Workplaces.Any(c => c.Fingerprint == Fg.Trim()));
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Fg=" + Fg;
                sFileName += "-" + Fg;
            }
            if (!string.IsNullOrEmpty(Nl))
            {
                filter = filter & builder.Eq(m => m.CongTyChiNhanh, Nl);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Nl=" + Nl;
                sFileName += "-" + Nl;
            }
            if (!string.IsNullOrEmpty(Kcn))
            {
                filter = filter & builder.Eq(m => m.KhoiChucNang, Kcn);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Kcn=" + Kcn;
                sFileName += "-" + Kcn;
            }
            if (!string.IsNullOrEmpty(Pb))
            {
                filter = filter & builder.Eq(m => m.PhongBan, Pb);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Pb=" + Pb;
                sFileName += "-" + Pb;
            }
            if (!string.IsNullOrEmpty(Bp))
            {
                filter = filter & builder.Eq(m => m.BoPhan, Bp);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Bp=" + Bp;
                sFileName += "-" + Bp;
            }

            var fields = Builders<Employee>.Projection.Include(p => p.Id);

            var employeeFilters = dbContext.Employees.Find(filter).ToList();

            var employeeIds = dbContext.Employees.Find(filter).Project<Employee>(fields).ToList().Select(m => m.Id).ToList();

            var builderT = Builders<EmployeeWorkTimeLog>.Filter;
            var filterT = builderT.Eq(m => m.Enable, true)
                        & builderT.Gte(m => m.Date, Tu)
                        & builderT.Lte(m => m.Date, Den);
            if (employeeIds != null && employeeIds.Count > 0)
            {
                filterT = filterT & builderT.Where(m => employeeIds.Contains(m.EmployeeId));
            }
            #endregion

            var duration = Tu.ToString("ddMMyyyy") + "-" + Den.ToString("ddMMyyyy");
            sFileName += "-" + duration;
            sFileName += "-V" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx";

            var times = dbContext.EmployeeWorkTimeLogs.Find(filterT).SortBy(m => m.Date).ToList();
            var results = new List<TimeKeeperDisplay>();

            #region method 1: base employee
            foreach (var employee in employeeFilters)
            {
                var employeeWorkTimeLogs = times.Where(m => m.EmployeeId.Equals(employee.Id)).ToList();
                if (employeeWorkTimeLogs == null || employeeWorkTimeLogs.Count == 0) continue;

                var enrollNumber = string.Empty;
                if (employee.Workplaces != null && employee.Workplaces.Count > 0)
                {
                    foreach (var workplace in employee.Workplaces)
                    {
                        if (!string.IsNullOrEmpty(workplace.Fingerprint))
                        {
                            if (!string.IsNullOrEmpty(enrollNumber))
                            {
                                enrollNumber += ";";
                            }
                            enrollNumber += workplace.Code + ":" + workplace.Fingerprint;
                        }
                    }
                }

                results.Add(new TimeKeeperDisplay()
                {
                    EmployeeWorkTimeLogs = employeeWorkTimeLogs,
                    Id = employee.Id,
                    Code = employee.CodeOld,
                    EnrollNumber = enrollNumber,
                    FullName = employee.FullName,
                    CongTyChiNhanh = employee.CongTyChiNhanhName,
                    KhoiChucNang = employee.KhoiChucNangName,
                    PhongBan = employee.PhongBanName,
                    BoPhan = employee.BoPhanName,
                    ChucVu = employee.ChucVuName,
                    Alias = employee.AliasFullName,
                    Email = employee.Email,
                    ManageId = employee.ManagerId
                });
            }
            #endregion

            string exportFolder = Path.Combine(_env.WebRootPath, "exports", "timers", today.ToString("yyyyMMdd"));

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            file.Directory.Create(); // If the directory already exists, this method does nothing.
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

                var cellStyleBorderAndColorGreen = workbook.CreateCellStyle();
                cellStyleBorderAndColorGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 198, 239, 206 }));

                var cellStyleBorderAndColorYellow = workbook.CreateCellStyle();
                cellStyleBorderAndColorYellow.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorYellow.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorYellow).SetFillForegroundColor(new XSSFColor(new byte[] { 255, 235, 156 }));

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

                var styleFullText = workbook.CreateCellStyle();
                styleDedaultMerge.SetFont(font);
                styleFullText.WrapText = true;

                var styleBold = workbook.CreateCellStyle();
                styleBold.SetFont(fontbold8);

                var styleSmall = workbook.CreateCellStyle();
                styleSmall.CloneStyleFrom(styleBorder);
                styleSmall.SetFont(fontSmall);
                #endregion

                ISheet sheet1 = workbook.CreateSheet("Cong");

                #region Introduce
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
                #endregion

                row = sheet1.CreateRow(rowIndex);
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("BẢNG THỐNG KÊ CHẤM CÔNG");
                cell.CellStyle = styleTitle;
                rowIndex++;

                #region Header
                row = sheet1.CreateRow(rowIndex);
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Từ ngày " + Tu.ToString("dd/MM/yyyy") + " đến ngày " + Den.ToString("dd/MM/yyyy"));
                cell.CellStyle = styleSubTitle;
                rowIndex++;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("#");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Họ tên");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Chức vụ");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã chấm công");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleHeader;
                columnIndex++;

                for (DateTime date = Tu; date <= Den; date = date.AddDays(1))
                {
                    cell = row.CreateCell(columnIndex);
                    cell.SetCellValue(date.Day);
                    cell.CellStyle = styleHeader;
                    columnIndex++;
                }

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày công");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex = columnIndex + 1;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Vào trễ");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex = columnIndex + 1;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ra sớm");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex = columnIndex + 1;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 2);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Tăng ca (giờ)");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex = columnIndex + 2;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày nghỉ");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);

                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 6;
                for (DateTime date = Tu; date <= Den; date = date.AddDays(1.0))
                {
                    cell = row.CreateCell(columnIndex); // cell B1
                    cell.SetCellValue(Constants.DayOfWeekT2(date));
                    cell.CellStyle = styleHeader;
                    columnIndex++;
                }

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("");
                columnIndex = columnIndex + 1;
                columnIndex++;

                //cell = row.CreateCell(columnIndex);
                //cell.SetCellValue("NT");
                //cell.CellStyle = styleHeader;
                //columnIndex++;
                //cell = row.CreateCell(columnIndex);
                //cell.SetCellValue("CT");
                //cell.CellStyle = styleHeader;
                //columnIndex++;

                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Lần");
                cell.CellStyle = styleHeader;
                columnIndex++;
                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Phút");
                cell.CellStyle = styleHeader;
                columnIndex++;
                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Lần");
                cell.CellStyle = styleHeader;
                columnIndex++;
                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Phút");
                cell.CellStyle = styleHeader;
                columnIndex++;
                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Ngày thường");
                cell.CellStyle = styleHeader;
                columnIndex++;
                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Chủ nhật");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Lễ tết");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("KP");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("P");
                cell.CellStyle = styleHeader;
                columnIndex++;

                rowIndex++;
                #endregion

                var order = 1;
                foreach (var employee in results)
                {
                    double ngayCongNT = 0;
                    double ngayCongCT = 0;
                    var vaoTreLan = 0;
                    double vaoTrePhut = 0;
                    var raSomLan = 0;
                    double raSomPhut = 0;
                    double tangCaNgayThuong = 0;
                    double tangCaChuNhat = 0;
                    double tangCaLeTet = 0;
                    double vangKP = 0;
                    double ngayNghiP = 0;
                    double ngayNghiOM = 0;
                    double ngayNghiTS = 0;
                    double ngayNghiR = 0;
                    var timesSort = employee.EmployeeWorkTimeLogs.OrderBy(m => m.Date).ToList();

                    var rowEF = rowIndex;
                    var rowET = rowIndex + 4;

                    row = sheet1.CreateRow(rowIndex);

                    columnIndex = 0;
                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(order);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(employee.Code);
                    cell.CellStyle = styleFullText;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(employee.FullName);
                    cell.CellStyle = styleFullText;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(employee.ChucVu);
                    cell.CellStyle = styleFullText;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(employee.EnrollNumber);
                    cell.CellStyle = styleFullText;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Vào 1");
                    cell.CellStyle = styleDedault;

                    var rowout1 = sheet1.CreateRow(rowIndex + 1);
                    var rowin2 = sheet1.CreateRow(rowIndex + 2);
                    var rowout2 = sheet1.CreateRow(rowIndex + 3);
                    var rowreason = sheet1.CreateRow(rowIndex + 4);

                    cell = rowout1.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Ra 1");
                    cell.CellStyle = styleDedault;

                    cell = rowin2.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Vào 2");
                    cell.CellStyle = styleDedault;

                    cell = rowout2.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Ra 2");
                    cell.CellStyle = styleDedault;

                    cell = rowreason.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Xác nhận-Lý do");
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    for (DateTime date = Tu; date <= Den; date = date.AddDays(1.0))
                    {
                        var item = timesSort.Where(m => m.Date.Equals(date)).FirstOrDefault();
                        if (item != null)
                        {
                            var modeMiss = false;
                            if (item.Mode < (int)ETimeWork.Sunday)
                            {
                                switch (item.Status)
                                {
                                    case (int)EStatusWork.XacNhanCong:
                                        {
                                            modeMiss = true;
                                            break;
                                        }
                                    case (int)EStatusWork.DaGuiXacNhan:
                                        {
                                            modeMiss = true;
                                            break;
                                        }
                                    case (int)EStatusWork.DongY:
                                        {
                                            ngayCongNT++;
                                            break;
                                        }
                                    case (int)EStatusWork.TuChoi:
                                        {
                                            modeMiss = true;
                                            break;
                                        }
                                    default:
                                        {
                                            ngayCongNT++;
                                            break;
                                        }
                                }
                            }

                            if (modeMiss)
                            {
                                if (item.Late.TotalMinutes > 1)
                                {
                                    vaoTreLan++;
                                    vaoTrePhut += item.Late.TotalMinutes;
                                }
                                if (item.Early.TotalMinutes > 1)
                                {
                                    raSomLan++;
                                    raSomPhut += item.Early.TotalMinutes;
                                }
                                // First, không tính 15p
                                var timeoutin = item.Out - item.In;
                                if (timeoutin.HasValue && timeoutin.Value.TotalHours > 6)
                                {
                                    ngayCongNT++;
                                }
                                else
                                {
                                    ngayCongNT += item.WorkDay;
                                }
                            }
                            
                            if (item.Mode < (int)ETimeWork.Sunday && item.Logs == null)
                            {
                                if (item.Mode == (int)ETimeWork.LeavePhep)
                                {
                                    ngayNghiP += item.SoNgayNghi;
                                }

                                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                                sheet1.AddMergedRegion(cellRangeAddress);
                                cell = row.CreateCell(columnIndex, CellType.String);
                                cell.SetCellValue(item.Reason);
                                cell.CellStyle = styleDedaultMerge;
                                var rowCellRangeAddress = new CellRangeAddress(rowEF, rowET, columnIndex, columnIndex);
                                sheet1.AddMergedRegion(rowCellRangeAddress);
                                RegionUtil.SetBorderTop((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                                RegionUtil.SetBorderRight((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                            }
                            else
                            {
                                var displayIn1 = string.Empty;
                                var displayIn2 = string.Empty;
                                var displayOut1 = string.Empty;
                                var displayOut2 = string.Empty;
                                if (item.In.HasValue)
                                {
                                    displayIn1 = item.In.ToString();
                                }
                                if (item.Out.HasValue)
                                {
                                    displayOut1 = item.Out.ToString();
                                }

                                cell = row.CreateCell(columnIndex, CellType.String);
                                cell.SetCellValue(displayIn1);
                                cell.CellStyle = styleDot;

                                cell = rowout1.CreateCell(columnIndex, CellType.String);
                                cell.SetCellValue(displayOut1);
                                cell.CellStyle = styleDedault;

                                cell = rowin2.CreateCell(columnIndex, CellType.String);
                                cell.SetCellValue(displayIn2);
                                if (!string.IsNullOrEmpty(displayIn2))
                                {
                                    cell.CellStyle = styleDot;
                                }
                                else
                                {
                                    cell.CellStyle = styleDedault;
                                }

                                cell = rowout2.CreateCell(columnIndex, CellType.String);
                                cell.SetCellValue(displayOut2);
                                cell.CellStyle = styleDedault;

                                cell = rowreason.CreateCell(columnIndex, CellType.String);
                                var detail = string.Empty;

                                if (item.Mode < (int)ETimeWork.Sunday)
                                {
                                    detail += item.WorkDay + " ngày";
                                    tangCaNgayThuong += item.TangCaDaXacNhan.TotalHours;
                                    if (item.TangCaDaXacNhan.TotalHours > 0)
                                    {
                                        detail += ", TC:" + Math.Round(item.TangCaDaXacNhan.TotalHours, 2) + " giờ";
                                    }
                                }
                                else
                                {
                                    if (item.WorkTime.TotalHours > 0)
                                    {
                                        detail += Math.Round(item.WorkTime.TotalHours, 2) + " giờ";
                                        if (item.Mode == (int)ETimeWork.Sunday)
                                        {
                                            tangCaChuNhat += item.WorkTime.TotalHours;
                                        }
                                        else
                                        {
                                            tangCaLeTet += item.WorkTime.TotalHours;
                                        }
                                    }
                                }
                                // NOI LAM VIEC
                                if (item.Logs != null && !string.IsNullOrEmpty(item.WorkplaceCode))
                                {
                                    if (!string.IsNullOrEmpty(detail))
                                    {
                                        detail += ";";
                                    }
                                    detail += item.WorkplaceCode;
                                }
                                // LY DO
                                if (!string.IsNullOrEmpty(item.Reason))
                                {
                                    if (!string.IsNullOrEmpty(detail))
                                    {
                                        detail += ";";
                                    }
                                    detail += item.Reason;
                                }
                                cell.SetCellValue(detail);
                                cell.CellStyle = styleSmall;
                            }

                            columnIndex++;
                        }
                        else
                        {
                            cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                            sheet1.AddMergedRegion(cellRangeAddress);
                            cell = row.CreateCell(columnIndex, CellType.String);
                            cell.SetCellValue(Constants.NA);
                            cell.CellStyle = styleDedaultMerge;
                            var rowCellRangeAddress = new CellRangeAddress(rowEF, rowET, columnIndex, columnIndex);
                            sheet1.AddMergedRegion(rowCellRangeAddress);
                            RegionUtil.SetBorderTop((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                            RegionUtil.SetBorderLeft((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                            RegionUtil.SetBorderRight((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                            RegionUtil.SetBorderBottom((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                            columnIndex++;
                        }
                    }

                    var columnIndexF = columnIndex;
                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(Math.Round(ngayCongNT, 2));
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(Math.Round(ngayCongCT, 2));
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(vaoTreLan);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(Math.Round(vaoTrePhut, 2));
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(raSomLan);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(Math.Round(raSomPhut, 2));
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(Math.Round(tangCaNgayThuong, 2));
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(Math.Round(tangCaChuNhat, 2));
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(Math.Round(tangCaLeTet, 2));
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(vangKP);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(ngayNghiP);
                    cell.CellStyle = styleDedaultMerge;

                    var columnIndexT = columnIndex;
                    columnIndex++;

                    rowIndex = rowIndex + 4;
                    rowIndex++;
                    order++;
                    #region fix border
                    for (var i = 0; i < 5; i++)
                    {
                        var rowCellRangeAddress = new CellRangeAddress(rowEF, rowET, i, i);
                        sheet1.AddMergedRegion(rowCellRangeAddress);
                        RegionUtil.SetBorderTop((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderLeft((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderRight((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderBottom((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                    }
                    for (var y = columnIndexF; y <= columnIndexT; y++)
                    {
                        var rowCellRangeAddress = new CellRangeAddress(rowEF, rowET, y, y);
                        sheet1.AddMergedRegion(rowCellRangeAddress);
                        RegionUtil.SetBorderTop((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderLeft((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderRight((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderBottom((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                    }
                    #endregion
                }

                #region fix border
                var rowF = 7;
                var rowT = 8;
                for (var i = 0; i < 6; i++)
                {
                    var rowCellRangeAddress = new CellRangeAddress(rowF, rowT, i, i);
                    sheet1.AddMergedRegion(rowCellRangeAddress);
                    RegionUtil.SetBorderTop((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                    RegionUtil.SetBorderLeft((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                    RegionUtil.SetBorderRight((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                    RegionUtil.SetBorderBottom((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                }
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
                        (int)EStatusWork.DaGuiXacNhan,
                        (int)EStatusWork.DongY
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

        #endregion

        #region TANG CA
        [HttpPost]
        [Route(Constants.LinkTimeKeeper.XacNhanTangCa)]
        public IActionResult XacNhanTangCa(string id, double thoigian, int trangthai)
        {
            var now = DateTime.Now;
            int status = trangthai == 1 ? (int)ETangCa.DongY : (int)ETangCa.TuChoi;

            var timeWork = dbContext.EmployeeWorkTimeLogs.Find(m => m.Enable.Equals(true) && m.Id.Equals(id)).FirstOrDefault();

            var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, id);
            var update = Builders<EmployeeWorkTimeLog>.Update
                .Set(m => m.StatusTangCa, status)
                .Set(m => m.TangCaDaXacNhan, TimeSpan.FromHours(thoigian))
                .Set(m => m.UpdatedOn, DateTime.Now);
            dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);

            // UPDATE SUMMARY
            var builderS = Builders<EmployeeWorkTimeMonthLog>.Filter;
            var filterS = builderS.Eq(m => m.EmployeeId, timeWork.EmployeeId) & builderS.Eq(m => m.EnrollNumber, timeWork.EnrollNumber)
                & builderS.Eq(m => m.Month, timeWork.Month) & builderS.Eq(m => m.Year, timeWork.Year);

            var updateS = Builders<EmployeeWorkTimeMonthLog>.Update
                .Set(m => m.LastUpdated, now);
            if (timeWork.Mode == (int)ETimeWork.Normal)
            {
                updateS = updateS.Set(m => m.CongTangCaNgayThuongGio, thoigian);
            }
            else if (timeWork.Mode == (int)ETimeWork.Sunday)
            {
                updateS = updateS.Set(m => m.CongCNGio, thoigian);
            }
            else
            {
                updateS = updateS.Set(m => m.CongLeTet, thoigian);
            }
            dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterS, updateS);

            return Json(new { error = 0, id, trangthai, thoigian });
        }
        #endregion

        #region IMPORT DATA: TANG CA, CHAM CONG,...
        [Route(Constants.LinkTimeKeeper.OvertimeTemplateFull)]
        public async Task<IActionResult> OvertimeTemplateFull(string fileName, int year, int month)
        {
            year = year == 0 ? DateTime.Now.Year : year;
            month = month == 0 ? DateTime.Now.Month : month;

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"du-lieu-cham-cong-thang-" + month + "-" + year + "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var styleBorder = workbook.CreateCellStyle();
                styleBorder.BorderBottom = BorderStyle.Thin;
                styleBorder.BorderLeft = BorderStyle.Thin;
                styleBorder.BorderRight = BorderStyle.Thin;
                styleBorder.BorderTop = BorderStyle.Thin;

                var styleCenter = workbook.CreateCellStyle();
                styleCenter.Alignment = HorizontalAlignment.Center;
                styleCenter.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleHeader = workbook.CreateCellStyle();
                cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                cellStyleHeader.FillPattern = FillPattern.SolidForeground;

                var font = workbook.CreateFont();
                font.FontHeightInPoints = 11;
                font.FontName = "Times New Roman";

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

                ISheet sheet1 = workbook.CreateSheet("NMT" + DateTime.Now.Month.ToString("00"));

                //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 8, 13));

                var toDate = new DateTime(year, month, 25);
                var fromDate = toDate.AddMonths(-1).AddDays(1);
                var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);

                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                var columnIndex = 2;
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + fromToNum);
                sheet1.AddMergedRegion(cellRangeAddress);
                var cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("BẢNG CHẤM CÔNG TRONG GIỜ THÁNG " + month + "/" + year);
                cell.CellStyle = styleRow0;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex++;

                columnIndex = columnIndex + fromToNum + 4;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + fromToNum);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("BẢNG CHẤM CÔNG NGOÀI GIỜ THÁNG " + month + "/" + year);
                cell.CellStyle = styleRow0;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
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
                columnIndex = columnIndex + 4;
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

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + fromToNum);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NGÀY LÀM VIỆC TRONG THÁNG");
                cell.CellStyle = styleBorderBold11Background;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex = columnIndex + fromToNum;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày công");
                cell.CellStyle = styleBorderBold11Background;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex = columnIndex + 1;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Nghỉ hưởng lương");
                cell.CellStyle = styleBorderBold11Background;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex = columnIndex + 1;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + fromToNum);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Giờ tăng ca trong tháng");
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
                cell.SetCellValue("Ngày LV");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Lễ tết");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Phép");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Lễ tết");
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
                cell.SetCellValue("Lễ tết");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("CƠM");
                cell.CellStyle = styleBorderBold10Background;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, (columnIndex + 11 + fromToNum * 2));
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

        [Route(Constants.LinkTimeKeeper.OvertimeTemplate)]
        public async Task<IActionResult> OvertimeTemplate(string fileName, int year, int month)
        {
            year = year == 0 ? DateTime.Now.Year : year;
            month = month == 0 ? DateTime.Now.Month : month;

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"du-lieu-cham-cong-thang-" + month + "-" + year + "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var styleBorder = workbook.CreateCellStyle();
                styleBorder.BorderBottom = BorderStyle.Thin;
                styleBorder.BorderLeft = BorderStyle.Thin;
                styleBorder.BorderRight = BorderStyle.Thin;
                styleBorder.BorderTop = BorderStyle.Thin;

                var styleCenter = workbook.CreateCellStyle();
                styleCenter.Alignment = HorizontalAlignment.Center;
                styleCenter.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleHeader = workbook.CreateCellStyle();
                cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                cellStyleHeader.FillPattern = FillPattern.SolidForeground;

                var font = workbook.CreateFont();
                font.FontHeightInPoints = 11;
                font.FontName = "Times New Roman";

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

                ISheet sheet1 = workbook.CreateSheet("NMT" + DateTime.Now.Month.ToString("00"));

                //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 8, 13));

                var toDate = new DateTime(year, month, 25);
                var fromDate = toDate.AddMonths(-1).AddDays(1);
                var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);

                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                var columnIndex = 2;
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + fromToNum);
                sheet1.AddMergedRegion(cellRangeAddress);
                var cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("BẢNG CHẤM CÔNG NGOÀI GIỜ THÁNG " + month + "/" + year);
                cell.CellStyle = styleRow0;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
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

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + fromToNum);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Giờ tăng ca trong tháng");
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
                cell.SetCellValue("Lễ tết");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("CƠM");
                cell.CellStyle = styleBorderBold10Background;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, (columnIndex + 6 + fromToNum));
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

        [Route(Constants.LinkTimeKeeper.OvertimeTemplate + "/" + Constants.ActionLink.Post)]
        [HttpPost]
        public ActionResult OvertimeTemplatePost()
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

                    var month = 1;
                    var year = 2019;
                    var toDate = new DateTime(year, month, 25);
                    var fromDate = toDate.AddMonths(-1).AddDays(1);
                    var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);
                    for (int i = 5; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        int columnIndex = 0;
                        var manhanvien = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var tennhanvien = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var alias = Utility.AliasConvert(tennhanvien);

                        var employee = new Employee();
                        if (!string.IsNullOrEmpty(alias))
                        {
                            employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.AliasFullName.Equals(alias)).FirstOrDefault();
                        }
                        if (employee == null)
                        {
                            if (!string.IsNullOrEmpty(manhanvien))
                            {
                                employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.CodeOld.Equals(manhanvien)).FirstOrDefault();
                            }
                        }
                        if (employee != null)
                        {
                            var chucvuE = dbContext.ChucVus.Find(m => m.Id.Equals(employee.ChucVu)).FirstOrDefault();
                            var chucvu = chucvuE != null ? chucvuE.Name : string.Empty;
                            var timeouts = new List<EmployeeWorkTimeLog>();
                            for (DateTime date = fromDate; date <= toDate; date = date.AddDays(1.0))
                            {
                                var builderTime = Builders<EmployeeWorkTimeLog>.Filter;
                                var filterTime = builderTime.Eq(m => m.EmployeeId, employee.Id)
                                                & builderTime.Eq(m => m.Date, date);
                                var updateTime = Builders<EmployeeWorkTimeLog>.Update
                                    .Set(m => m.TangCaDaXacNhan, TimeSpan.FromHours(Utility.GetNumbericCellValue(row.GetCell(columnIndex))))
                                    .Set(m => m.StatusTangCa, (int)ETangCa.DongY)
                                    .Set(m => m.UpdatedOn, DateTime.Now);
                                dbContext.EmployeeWorkTimeLogs.UpdateOne(filterTime, updateTime);
                                columnIndex++;
                            }
                            // Ngay thuong
                            columnIndex++; // CN
                            columnIndex++; //Le Tet
                            columnIndex++;
                            var comsx = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                            // check exist to update
                            var existEntity = dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (existEntity != null)
                            {
                                var builder = Builders<EmployeeCong>.Filter;
                                var filter = builder.Eq(m => m.Id, existEntity.Id);
                                var update = Builders<EmployeeCong>.Update
                                    .Set(m => m.EmployeeCode, employee.Code)
                                    .Set(m => m.EmployeeName, employee.FullName)
                                    .Set(m => m.EmployeeChucVu, chucvu)
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
                                    EmployeeChucVu = chucvu,
                                    ComSX = comsx
                                };
                                dbContext.EmployeeCongs.InsertOne(newItem);
                            }
                        }
                        else
                        {
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "san-xuat-import",
                                Object = "time: " + month + "-" + year + ", code: " + manhanvien + "-" + tennhanvien + " ,dòng " + i,
                                Error = "No import data",
                                DateTime = DateTime.Now.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { result = true });
        }
        #endregion
    }
}
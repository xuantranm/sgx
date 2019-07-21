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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver.Linq;
using System.Security.Claims;
using System.Threading;
using MimeKit;
using Services;
using Common.Enums;
using MongoDB.Bson;

namespace erp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        readonly IHostingEnvironment _env;
        private IHttpContextAccessor _accessor;
        private readonly ILogger _logger;

        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public IConfiguration Configuration { get; }

        public HomeController(
            IHttpContextAccessor accessor,
            IConfiguration configuration,
            IHostingEnvironment env,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILogger<HomeController> logger)
        {
            _accessor = accessor;
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            #region Login
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            var userInformation = dbContext.Employees.Find(m => m.Id.Equals(login)).FirstOrDefault();
            if (userInformation == null)
            {
                var ipAddress = string.Empty;
                try
                {
                    ipAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
                    // UAT
                    // ipAddress = "14.161.24.132";
                    // Login base ip

                    if (string.IsNullOrEmpty(ipAddress) && ipAddress == "::1")
                    {
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return RedirectToAction("login", "account");
                    }
                    else
                    {
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return RedirectToAction("login", "account");
                        // Ip + more information
                        //var ipE = dbContext.Ips.Find(m => m.IpAddress.Equals(ipAddress)).FirstOrDefault();
                        //if (ipE == null)
                        //{
                        //    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        //    return RedirectToAction("login", "account");
                        //}
                        //else
                        //{
                        //    userInformation = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(ipE.Login)).FirstOrDefault();
                        //    if (userInformation == null)
                        //    {
                        //        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        //        return RedirectToAction("login", "account");
                        //    }
                        //    else
                        //    {
                        //        var claims = new List<Claim>
                        //        {
                        //            new Claim("UserName", userInformation.UserName),
                        //            new Claim(ClaimTypes.Name, userInformation.Id),
                        //            new Claim(ClaimTypes.Email, string.IsNullOrEmpty(userInformation.Email) ? string.Empty : userInformation.Email),
                        //            new Claim("FullName", userInformation.FullName)
                        //        };
                        //        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        //        var authProperties = new AuthenticationProperties
                        //        {
                        //        };
                        //        authProperties.IsPersistent = true;
                        //        await HttpContext.SignInAsync(
                        //            CookieAuthenticationDefaults.AuthenticationScheme,
                        //            new ClaimsPrincipal(claimsIdentity),
                        //            authProperties);
                        //    }
                        //}
                    }
                }
                catch (Exception ex)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("login", "account");
                }
            }
            #endregion

            #region Rights
            var rightHr = false;
            if (!string.IsNullOrEmpty(login))
            {
                rightHr = Utility.IsRight(login, Constants.Rights.HR, (int)ERights.View);
            }

            ViewData["rightHr"] = rightHr;
            #endregion

            int getItems = 10;

            #region Get Setting Value
            var settings = dbContext.Settings.Find(m => m.Enable.Equals(true)).ToList();
            var pageSize = Constants.PageSize;
            var pageSizeSetting = settings.First(m => m.Key.Equals("pageSize"));
            if (pageSizeSetting != null)
            {
                pageSize = Convert.ToInt32(pageSizeSetting.Value);
            }

            var birthdayNoticeBefore = Constants.PageSize;
            var birthdayNoticeBeforeSetting = settings.FirstOrDefault(m => m.Key.Equals("BirthdayNoticeBefore"));
            if (birthdayNoticeBeforeSetting != null)
            {
                birthdayNoticeBefore = Convert.ToInt32(birthdayNoticeBeforeSetting.Value);
            }

            //var contractDayNoticeBefore = Constants.contractDayNoticeBefore;
            //var contractDayNoticeBeforeSetting = settings.First(m => m.Key.Equals("contractDayNoticeBefore"));
            //if (contractDayNoticeBeforeSetting != null)
            //{
            //    contractDayNoticeBefore = Convert.ToInt32(contractDayNoticeBeforeSetting.Value);
            //}

            #endregion

            #region Notification Birthday
            ViewData["birthdayNoticeBefore"] = birthdayNoticeBefore;

            var nextBirthdays = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.Birthday > Constants.MinDate).ToEnumerable()
                .Where(m => m.RemainingBirthDays <= birthdayNoticeBefore).OrderBy(m => m.RemainingBirthDays).Take(6).ToList();
            #endregion

            //#region Notification Contract
            //var sortContract = Builders<Employee>.Sort.Ascending(m => m.).Descending(m => m.Code);
            //var birthdays = dbContext.Employees.Find(m => m.Enable.Equals(true)).Sort(sortBirthday).Limit(getItems).ToList();
            //#endregion
            var sortNotification = Builders<Notification>.Sort.Ascending(m => m.CreatedOn).Descending(m => m.CreatedOn);
            var builderNotication = Builders<Notification>.Filter;
            var filterNotication = builderNotication.Eq(m => m.Enable, true);

            #region Notification HR
            var filterHr = filterNotication & builderNotication.Eq(m => m.Type, 2);
            if (!rightHr)
            {
                filterHr = filterHr & builderNotication.Eq(m => m.User, login) & builderNotication.Ne(m => m.CreatedBy, login);
            }
            var notificationHRs = await dbContext.Notifications.Find(filterHr).Sort(sortNotification).Limit(getItems).ToListAsync();
            #endregion

            #region Notification Others
            var filterSystem = filterNotication & builderNotication.Eq(m => m.Type, 1);
            var notificationSystems = await dbContext.Notifications.Find(filterSystem).Sort(sortNotification).Limit(getItems).ToListAsync();

            var filterExpires = filterNotication & builderNotication.Eq(m => m.Type, 3);
            var notificationExpires = await dbContext.Notifications.Find(filterExpires).Sort(sortNotification).Limit(getItems).ToListAsync();

            var filterTaskBhxh = filterNotication & builderNotication.Eq(m => m.Type, 4);
            var notificationTaskBHXHs = await dbContext.Notifications.Find(filterTaskBhxh).Sort(sortNotification).Limit(getItems).ToListAsync();

            var filterCompany = filterNotication & builderNotication.Eq(m => m.Type, 5);
            var notificationCompanies = await dbContext.Notifications.Find(filterCompany).Sort(sortNotification).Limit(getItems).ToListAsync();

            var notificationActions = await dbContext.NotificationActions.Find(m => m.UserId.Equals(login)).ToListAsync();
            #endregion

            #region Tracking Other User (check user activities,...)
            var sortTrackingOther = Builders<TrackingUser>.Sort.Descending(m => m.Created);
            var trackingsOther = dbContext.TrackingUsers.Find(m => !m.UserId.Equals(login)).Sort(sortTrackingOther).Limit(getItems).ToList();
            #endregion

            #region My Trackings
            var sortTracking = Builders<TrackingUser>.Sort.Descending(m => m.Created);
            var trackings = dbContext.TrackingUsers.Find(m => m.UserId.Equals(login)).Sort(sortTracking).Limit(getItems).ToList();
            #endregion

            #region Extends (trainning, recruit, news....)
            var sortNews = Builders<News>.Sort.Descending(m => m.ModifiedOn);
            var news = dbContext.News.Find(m => m.Enable.Equals(true)).Sort(sortNews).Limit(getItems).ToList();

            var listTrainningTypes = await dbContext.TrainningTypes.Find(m => m.Enable.Equals(true)).ToListAsync();
            Random rnd = new Random();
            int r = rnd.Next(listTrainningTypes.Count);
            var sortTrainnings = Builders<Trainning>.Sort.Descending(m => m.CreatedOn);
            // Random type result
            var trainningType = listTrainningTypes[r].Alias;
            var trainnings = dbContext.Trainnings.Find(m => m.Enable.Equals(true) && m.Type.Equals(trainningType)).Sort(sortTrainnings).Limit(5).ToList();
            #endregion

            #region Leave Manager
            var leaves = await dbContext.Leaves.Find(m => m.ApproverId.Equals(login) && m.Status.Equals(0)).ToListAsync();
            #endregion

            #region Times Manager
            var timeKeepers = dbContext.EmployeeWorkTimeLogs.Find(m => m.ConfirmId.Equals(login) && m.Status.Equals(2)).ToList();
            var timers = new List<TimeKeeperDisplay>();
            if (timeKeepers != null && timeKeepers.Count > 0)
            {
                foreach (var time in timeKeepers)
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
            }
            #endregion

            #region My Activities
            //public IList<Leave> MyLeaves { get; set; }
            //public IList<EmployeeWorkTimeLog> MyWorkTimeLogs { get; set; }
            var sortMyLeave = Builders<Leave>.Sort.Descending(m => m.UpdatedOn);
            var builderMyLeave = Builders<Leave>.Filter;
            var filterMyLeave = builderMyLeave.Eq(m => m.Enable, true)
                & builderMyLeave.Eq(m => m.EmployeeId, login);
            var myLeaves = await dbContext.Leaves.Find(filterMyLeave).Sort(sortMyLeave).Limit(5).ToListAsync();

            var sortMyWorkTime = Builders<EmployeeWorkTimeLog>.Sort.Descending(m => m.Date);
            var builderMyWorkTime = Builders<EmployeeWorkTimeLog>.Filter;
            var filterMyWorkTime = builderMyWorkTime.Eq(m => m.Enable, true) & builderMyWorkTime.Lt(m => m.Date, DateTime.Now.Date) & builderMyWorkTime.Ne(m => m.Status, 1)
                & builderMyWorkTime.Eq(m => m.EmployeeId, login);
            var myWorkTimes = await dbContext.EmployeeWorkTimeLogs.Find(filterMyWorkTime).Sort(sortMyWorkTime).Limit(5).ToListAsync();

            #endregion


            var viewModel = new HomeErpViewModel()
            {
                UserInformation = userInformation,
                NotificationSystems = notificationSystems,
                NotificationCompanies = notificationCompanies,
                NotificationHRs = notificationHRs,
                NotificationExpires = notificationExpires,
                NotificationTaskBhxhs = notificationTaskBHXHs,
                NotificationActions = notificationActions,
                Trackings = trackings,
                TrackingsOther = trackingsOther,
                News = news,
                Birthdays = nextBirthdays,
                Leaves = leaves,
                TimeKeepers = timers,
                // My activities
                MyLeaves = myLeaves,
                MyWorkTimeLogs = myWorkTimes,
                // Training
                Trainnings = trainnings
            };

            return View(viewModel);
        }

        [Route("/tai-lieu/{type}")]
        public IActionResult Document(string type)
        {
            ViewData["Type"] = type;
            return View();
        }

        //[Route("/nt/thong-bao/{userId}")]
        //public IActionResult Notification(string userId)
        //{
        //    var isOwner = false;
        //    var ownerId = User.Identity.Name;
        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        isOwner = true;
        //        userId = ownerId;
        //        if (string.IsNullOrEmpty(userId))
        //        {
        //            return Json(new { result = false });
        //        }
        //    }

        //    var userInformation = dbContext.Employees.Find(m => m.Id.Equals(userId)).First();

        //    var owner = isOwner ? userInformation : dbContext.Employees.Find(m => m.Id.Equals(ownerId)).First();
        //    // notification
        //    var sortNotification = Builders<Notification>.Sort.Ascending(m => m.CreatedOn);
        //    var notifications = dbContext.Notifications.Find(m => m.Enable.Equals(true) && m.User.Equals(ownerId)).Sort(sortNotification).ToList();

        //    //var ownerViewModel = new OwnerViewModel
        //    //{
        //    //    Main = owner,
        //    //    NotificationCount = notifications != null ? notifications.Count() : 0
        //    //};

        //    return View();
        //}

        //chinh-sach-bao-mat
        [Route("/pp/{name}")]
        public IActionResult Policy(string name)
        {
            return View();
        }

        [Route("/v/{name}/")]
        public IActionResult Version(string name)
        {
            return View();
        }

        [Route("/email/welcome/")]
        public async Task<IActionResult> SendMail()
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

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(i => i.UserName, Constants.System.account);
            #endregion

            var employees = dbContext.Employees.Find(filter).ToList();
            var password = string.Empty;
            foreach (var employee in employees)
            {
                if (!string.IsNullOrEmpty(employee.Email))
                {
                    // Update password
                    password = Guid.NewGuid().ToString("N").Substring(0, 6);
                    var sysPassword = Helpers.Helper.HashedPassword(password);

                    var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                    var update = Builders<Employee>.Update
                        .Set(m => m.Password, sysPassword);
                    dbContext.Employees.UpdateOne(filterUpdate, update);
                    SendMailRegister(employee, password);
                }
            }

            return Json(new { result = true, source = "sendmail", message = "Gửi mail thành công" });
        }

        [Route("/email/send-miss-v100/")]
        public async Task<IActionResult> SendMailMissV100()
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

            #region Filter
            var listboss = new List<string>
            {
                "C.01",
                "C.02"
            };
            // remove list sent
            var listused = new List<string>
            {
                "Nguyễn Thành Đạt",
                "Nguyễn Thái Bình",
                "Phương Bình"
            };
            #endregion

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true)
                                                    && !m.UserName.Equals(Constants.System.account)
                                                    && !string.IsNullOrEmpty(m.Email)
                                                    && !listboss.Contains(m.NgachLuongCode)
                                                    && !listused.Contains(m.FullName)).ToList();
            var password = string.Empty;
            foreach (var employee in employees)
            {
                if (!string.IsNullOrEmpty(employee.Email))
                {
                    password = Guid.NewGuid().ToString("N").Substring(0, 6);
                    var sysPassword = Helpers.Helper.HashedPassword(password);

                    var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                    var update = Builders<Employee>.Update
                        .Set(m => m.Password, sysPassword);
                    dbContext.Employees.UpdateOne(filterUpdate, update);
                    SendMailRegister2(employee, password);
                }
            }

            return Json(new { result = true, source = "sendmail", message = "Gửi mail thành công" });
        }

        public void SendMailRegister(Employee entity, string pwd)
        {
            var title = string.Empty;
            if (!string.IsNullOrEmpty(entity.Gender))
            {
                if (entity.AgeBirthday > 50)
                {
                    title = entity.Gender == "Nam" ? "anh" : "chị";
                }
            }
            var url = Constants.System.domain;
            var subject = "Thông tin đăng nhập hệ thống.";
            var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = entity.FullName, Address = entity.Email }
            };
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "Confirm_Account_Registration.html";
            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }
            string messageBody = string.Format(builder.HtmlBody,
                subject,
                entity.FullName,
                url,
                entity.UserName,
                pwd,
                entity.Email);

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                Subject = subject,
                BodyContent = messageBody,
                Type = "register"
            };

            _emailSender.SendEmail(emailMessage);
        }

        public void SendMailRegister2(Employee entity, string pwd)
        {
            var title = string.Empty;
            if (!string.IsNullOrEmpty(entity.Gender))
            {
                if (entity.AgeBirthday > 50)
                {
                    title = entity.Gender == "Nam" ? "anh" : "chị";
                }
            }
            var url = Constants.System.domain;
            var subject = "Thông tin đăng nhập hệ thống.";
            var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = entity.FullName, Address = entity.Email }
            };
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "Confirm_Account_Registration_2.html";
            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }
            string messageBody = string.Format(builder.HtmlBody,
                subject,
                title + " " + entity.FullName,
                url,
                entity.UserName,
                pwd,
                entity.Email);

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                Subject = subject,
                BodyContent = messageBody,
                Type = "register"
            };

            _emailSender.SendEmail(emailMessage);
        }
    }
}
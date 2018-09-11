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

namespace erp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        readonly IHostingEnvironment _env;

        private readonly ILogger _logger;

        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public IConfiguration Configuration { get; }

        public HomeController(IDistributedCache cache, 
            IConfiguration configuration, 
            IHostingEnvironment env,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILogger<HomeController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        public IActionResult Index()
        {
            //var userAgent = Request.Headers["User-Agent"];
            //var ua = new UserAgent(userAgent);

            var loginId = User.Identity.Name;
            var userInformation = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            if (userInformation == null)
            {
                #region snippet1
                HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion

                return RedirectToAction("login", "account");
            }

            #region Right Management [ Role, RoleUser ]

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
            var nextBirthdays = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Birthday > Constants.MinDate).ToEnumerable().Where(m => m.RemainingBirthDays < birthdayNoticeBefore).OrderBy(m=>m.RemainingBirthDays).ToList();
            #endregion

            //#region Notification Contract
            //var sortContract = Builders<Employee>.Sort.Ascending(m => m.).Descending(m => m.Code);
            //var birthdays = dbContext.Employees.Find(m => m.Enable.Equals(true)).Sort(sortBirthday).Limit(getItems).ToList();
            //#endregion

            #region Notification
            var sortNotification = Builders<Notification>.Sort.Ascending(m => m.CreatedOn).Descending(m => m.CreatedOn);
            var notificationSystems = dbContext.Notifications.Find(m => m.Enable.Equals(true) && m.Type.Equals(1)).Sort(sortNotification).Limit(getItems).ToList();
            var notificationHRs = dbContext.Notifications.Find(m => m.Enable.Equals(true) && m.Type.Equals(2)).Sort(sortNotification).Limit(getItems).ToList();
            var notificationExpires = dbContext.Notifications.Find(m => m.Enable.Equals(true) && m.Type.Equals(3)).Sort(sortNotification).Limit(getItems).ToList();
            var notificationTaskBHXHs = dbContext.Notifications.Find(m => m.Enable.Equals(true) && m.Type.Equals(4)).Sort(sortNotification).Limit(getItems).ToList();
            var notificationCompanies = dbContext.Notifications.Find(m => m.Enable.Equals(true) && m.Type.Equals(5)).Sort(sortNotification).Limit(getItems).ToList();
            var notificationActions = dbContext.NotificationActions.Find(m => m.UserId.Equals(loginId)).ToList();
            #endregion

            #region Tracking Other User (check user activities,...)
            var sortTrackingOther = Builders<TrackingUser>.Sort.Descending(m => m.Created);
            var trackingsOther = dbContext.TrackingUsers.Find(m => !m.UserId.Equals(loginId)).Sort(sortTrackingOther).Limit(getItems).ToList();
            #endregion

            #region My Trackings
            var sortTracking = Builders<TrackingUser>.Sort.Descending(m => m.Created);
            var trackings = dbContext.TrackingUsers.Find(m => m.UserId.Equals(loginId)).Sort(sortTracking).Limit(getItems).ToList();
            #endregion

            #region News
            var sortNews = Builders<News>.Sort.Descending(m => m.CreatedDate);
            var news = dbContext.News.Find(m => m.Enable.Equals(true)).Sort(sortNews).Limit(getItems).ToList();
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
                Birthdays = nextBirthdays
            };

            return View(viewModel);
        }

        [Route("/tai-lieu/{type}")]
        public IActionResult Document(string type)
        {
            ViewData["Type"] = type;
            return View();
        }

        [Route("/nt/thong-bao/{userId}")]
        public IActionResult Notification(string userId)
        {
            var isOwner = false;
            var ownerId = User.Identity.Name;
            if (string.IsNullOrEmpty(userId))
            {
                isOwner = true;
                userId = ownerId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { result = false });
                }
            }

            var userInformation = dbContext.Employees.Find(m => m.Id.Equals(userId)).First();

            var owner = isOwner ? userInformation : dbContext.Employees.Find(m => m.Id.Equals(ownerId)).First();
            // notification
            var sortNotification = Builders<Notification>.Sort.Ascending(m => m.CreatedOn);
            var notifications = dbContext.Notifications.Find(m => m.Enable.Equals(true) && m.UserId.Equals(ownerId)).Sort(sortNotification).ToList();

            var ownerViewModel = new OwnerViewModel
            {
                Main = owner,
                NotificationCount = notifications != null ? notifications.Count() : 0
            };

            return View();
        }

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
        public IActionResult SendMail()
        {
            var enableSendMail = false;
            var listSendMailTest = new List<string>
                            {
                                "xuan.tm",
                                //"phuong.ndq",
                                //"anh.nth",
                                //"thanh.dnt",
                                //"thoa.ctm"
                            };
            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(i => i.UserName, Constants.System.account);
            #endregion

            var employees = dbContext.Employees.Find(filter).ToList();
            var password = string.Empty;
            foreach (var employee in employees)
            {
                // Update password
                password = Guid.NewGuid().ToString("N").Substring(0, 12);
                var sysPassword = Helpers.Helper.HashedPassword(password);

                var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.Password, sysPassword);
                dbContext.Employees.UpdateOne(filterUpdate, update);

                if (!string.IsNullOrEmpty(listSendMailTest.Where(s => s.Equals(employee.UserName)).FirstOrDefault()))
                {
                    enableSendMail = true;
                }
                if (enableSendMail)
                {
                    SendMailRegister(employee, password);
                }
                enableSendMail = false;
            }

            return Json(new { result = true, source = "sendmail", message = "Gửi mail thành công" });
        }

        public void SendMailRegister(Employee entity, string pwd)
        {
            var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = entity.FullName, Address = entity.Email }
            };

            // Send an email with this link
            //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            //var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
            //Email from Email Template
            var callbackUrl = "/";
            string Message = "Đăng nhập TRIBAT - ERP <a href=\"" + callbackUrl + "\">here</a>";
            // string body;

            var webRoot = _env.WebRootPath; //get wwwroot Folder

            //Get TemplateFile located at wwwroot/Templates/EmailTemplate/Register_EmailTemplate.html
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "Confirm_Account_Registration.html";

            var subject = "Thông tin đăng nhập hệ thống TRIBAT - ERP.";

            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }
            //{0} : Subject
            //{1} : DateTime
            //{2} : Email
            //{3} : Username
            //{4} : Password
            //{5} : Message
            //{6} : callbackURL
            //{7} : domain url
            //{8} : link forgot password => use login
            //{9} : FullName
            var url = Constants.System.domain;
            var forgot = url + Constants.System.login;
            string messageBody = string.Format(builder.HtmlBody,
                subject,
                String.Format("{0:dddd, d MMMM yyyy}", DateTime.Now),
                entity.Email,
                entity.UserName,
                pwd,
                Message,
                callbackUrl,
                url,
                forgot,
                entity.FullName
                );

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                Subject = subject,
                BodyContent = messageBody
            };
            _emailSender.SendEmailAsync(emailMessage);

            ViewData["Message"] = $"Please confirm your account by clicking this link: <a href='{callbackUrl}' class='btn btn-primary'>Confirmation Link</a>";
            ViewData["MessageValue"] = "1";

            _logger.LogInformation(3, "User created a new account with password.");
        }

    }
}
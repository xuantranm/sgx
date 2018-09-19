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

        public TimeKeeperController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<TimeKeeperController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
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
        public async Task<IActionResult> Index(string times, string employee)
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
            if (string.IsNullOrEmpty(employee))
            {
                employee = login;
            }

            var userInformation = employee == login ? loginInformation : dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(employee)).FirstOrDefault();
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
            if (approves == null && approves.Count == 0)
            {
                var rolesApprove = dbContext.RoleUsers.Find(m => m.Enable.Equals(true) && m.Role.Equals("xac-nhan-nghi-phep")).ToList();
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

            var toDate = Utility.WorkingMonthToDate(times);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            // override times if null
            if (string.IsNullOrEmpty(times))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }

            #region Filter
            var builder = Builders<EmployeeWorkTimeLog>.Filter;
            var filter = builder.Eq(m => m.EmployeeId, employee);
            filter = filter & builder.Gt(m => m.Date, fromDate.AddDays(-1)) & builder.Lt(m => m.Date, toDate.AddDays(1));
            var builderSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
            var filterSum = builderSum.Eq(m => m.EmployeeId, employee);
            #endregion

            var timekeepings = await dbContext.EmployeeWorkTimeLogs.Find(filter).ToListAsync();
            var monthsTimes = await dbContext.EmployeeWorkTimeMonthLogs.Find(filterSum).ToListAsync();

            ViewData["DayWorking"] = Utility.BusinessDaysUntil(fromDate, toDate);

            timekeepings = timekeepings.OrderByDescending(m => m.Date).ToList();
            monthsTimes = monthsTimes.OrderByDescending(m => m.Year).OrderByDescending(m => m.Month).ToList();
            var monthTime = monthsTimes.FirstOrDefault(m => m.Year.Equals(toDate.Year) && m.Month.Equals(toDate.Month));

            #region My Activities

            #endregion

            var viewModel = new TimeKeeperViewModel
            {
                EmployeeWorkTimeLogs = timekeepings,
                Employee = userInformation,
                EmployeeWorkTimeMonthLogs = monthsTimes,
                EmployeeWorkTimeMonthLog = monthTime,
                Employees = employees,
                MonthYears = sortTimes,
                StartWorkingDate = fromDate,
                EndWorkingDate = toDate,
                Approves = approves
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

            var employee = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (employee == null)
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
            //{3} : Email
            //{4} : Chức vụ
            //{5} : Nội dung chấm công (ngay, in, out,....)
            //{6} : Lý do
            //{7} : Số điện thoại liên hệ
            //{8}: Link đồng ý
            //{9}: Link từ chối
            //{10}: Link chi tiết
            //{11}: Website
            #endregion
            var subject = "[TRIBAT] Hỗ trợ xác nhận công.";
            var requester = employee.FullName;
            if (!string.IsNullOrEmpty(employee.Title))
            {
                requester += " - " + employee.Title;
            }
            var inTime = entity.In.HasValue ? entity.In.Value.ToString(@"hh\:mm") : string.Empty;
            var outTime = entity.Out.HasValue ? entity.Out.Value.ToString(@"hh\:mm") : string.Empty;
            var lateTime = entity.Late.TotalMilliseconds > 0 ? entity.Late.TotalMinutes.ToString() : string.Empty;
            var earlyTime = entity.Early.TotalMilliseconds > 0 ? entity.Early.TotalMinutes.ToString() : string.Empty;
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
            if (minutesMissing > 0) {
                if (!string.IsNullOrEmpty(sumTime))
                {
                    sumTime += ", ";
                }
                sumTime += minutesMissing + " phút";
            }
            
            var detailTimeKeeping = "Ngày: " + entity.Date.ToString("dd/MM/yyyy") + "; thiếu: "+ sumTime + " | giờ vào: " + inTime + "; trễ: " + lateTime + "; giờ ra: " + outTime + "; sớm: " + earlyTime;
            // Api update, generate code.
            var linkapprove = Constants.System.domain + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Aprrove;
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
                employee.Email,
                employee.Title,
                detailTimeKeeping,
                model.Reason,
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
                BodyContent = messageBody
            };
            try
            {
                var emailFrom = Constants.System.emailErp;
                var emailFromName = Constants.System.emailErpName;
                var emailFromPwd = Constants.System.emailErpPwd;
                var message = new MimeMessage();
                message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                if (emailMessage.FromAddresses == null || emailMessage.FromAddresses.Count == 0)
                {
                    emailMessage.FromAddresses = new List<EmailAddress>
                                {
                                    new EmailAddress { Name = emailFromName, Address = emailFrom }
                                };
                }
                message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                message.Subject = emailMessage.Subject;
                message.Body = new TextPart(TextFormat.Html)
                {
                    Text = emailMessage.BodyContent
                };
                using (var emailClient = new MailKit.Net.Smtp.SmtpClient())
                {
                    //The last parameter here is to use SSL (Which you should!)
                    emailClient.Connect(emailFrom, 465, true);

                    //Remove any OAuth functionality as we won't be using it. 
                    emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                    emailClient.Authenticate(emailFrom, emailFromPwd);

                    emailClient.Send(message);
                    emailClient.Disconnect(true);
                    Console.WriteLine("The mail has been sent successfully !!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            #endregion

            return Json(new { result = true, message = "Yêu cầu được gửi các bộ phận liên quan." });
        }

        [Route(Constants.LinkTimeKeeper.Aprrove)]
        [AllowAnonymous]
        public IActionResult Approve(string id, int approve, string secure)
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

            if (entity.SecureCode != secure && entity.Status != 2)
            {
                ViewData["Status"] = "Rất tiếc! Dữ liệu đã được cập nhật hoặc thông tin không tồn tại trên hệ thống.";

                return View(viewModel);
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

            #region Send email to user
            var requester = employee.FullName;
            var tos = new List<EmailAddress>();
            var ccs = new List<EmailAddress>();
            //tos.Add(new EmailAddress { Name = "xuan", Address = "xuan.tm@tribat.vn" });
            tos.Add(new EmailAddress { Name = employee.FullName, Address = employee.Email });

            // Send mail to HR: if approve = 1;
            if (approve == 1)
            {
                var listEmailHR = string.Empty;
                var settingListEmailHR = dbContext.Settings.Find(m => m.Enable.Equals(true) && m.Key.Equals("ListEmailHRApproveTimeKeeper")).FirstOrDefault();
                if (settingListEmailHR != null)
                {
                    listEmailHR = settingListEmailHR.Value;
                }
                if (!string.IsNullOrEmpty(listEmailHR))
                {
                    foreach (var email in listEmailHR.Split(";"))
                    {
                        tos.Add(new EmailAddress { Name = email, Address = email });
                    }
                }
                requester += " , HR";
            }

            //if (!seftFlag)
            //{
            //    // cc người tạo dùm
            //    //ccs.Add(new EmailAddress { Name = "xuan", Address = "xuan.tm@tribat.vn" });
            //    ccs.Add(new EmailAddress { Name = userCreate.FullName, Address = userCreate.Email });
            //}

            var webRoot = Environment.CurrentDirectory;
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "TimeKeeperConfirm.html";

            #region parameters
            //{0} : Subject
            //{1} : Nguoi gui yeu cau  - Nguoi tao yeu cau (dùm) 
            //{2} : Tình trạng (đồng ý/hủy)
            //{3} : Nguoi duyet
            //{4} : Email
            //{5} : Chức vụ
            //{6} : Date (từ, đến, số ngày)
            //{7} : Lý do
            //{8}: Link chi tiết
            //{9}: Website
            #endregion
            var subject = "[TRIBAT] Xác nhận công.";
            var status = approve == 3 ? "Đồng ý" : "Không duyệt";
            var inTime = entity.In.HasValue ? entity.In.Value.ToString(@"hh\:mm") : string.Empty;
            var outTime = entity.Out.HasValue ? entity.Out.Value.ToString(@"hh\:mm") : string.Empty;
            var lateTime = entity.Late.TotalMilliseconds > 0 ? entity.Late.TotalMinutes.ToString() : string.Empty;
            var earlyTime = entity.Early.TotalMilliseconds > 0 ? entity.Early.TotalMinutes.ToString() : string.Empty;
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
                sumTime += minutesMissing + " phút";
            }
            var detailTimeKeeping = "Ngày: " + entity.Date.ToString("dd/MM/yyyy") + "; thiếu: " + sumTime + " | giờ vào: " + inTime + "; trễ: " + lateTime + "; giờ ra: " + outTime + "; sớm: " + earlyTime;
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
                linkDetail,
                Constants.System.domain
                );
            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                CCAddresses = ccs,
                Subject = subject,
                BodyContent = messageBody
            };
            try
            {
                var emailFrom = Constants.System.emailErp;
                var emailFromName = Constants.System.emailErpName;
                var emailFromPwd = Constants.System.emailErpPwd;
                var message = new MimeMessage();
                message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                if (emailMessage.CCAddresses != null && emailMessage.CCAddresses.Count > 0)
                {
                    message.Cc.AddRange(emailMessage.CCAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                }
                if (emailMessage.FromAddresses == null || emailMessage.FromAddresses.Count == 0)
                {
                    emailMessage.FromAddresses = new List<EmailAddress>
                                {
                                    new EmailAddress { Name = emailFromName, Address = emailFrom }
                                };
                }
                message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                message.Subject = emailMessage.Subject;
                message.Body = new TextPart(TextFormat.Html)
                {
                    Text = emailMessage.BodyContent
                };
                using (var emailClient = new MailKit.Net.Smtp.SmtpClient())
                {
                    //The last parameter here is to use SSL (Which you should!)
                    emailClient.Connect(emailFrom, 465, true);

                    //Remove any OAuth functionality as we won't be using it. 
                    emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                    emailClient.Authenticate(emailFrom, emailFromPwd);

                    emailClient.Send(message);
                    emailClient.Disconnect(true);
                    Console.WriteLine("The mail has been sent successfully !!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            #endregion

            ViewData["Status"] = "Cám ơn đã xác nhận, kết quả đang gửi cho người liên quan.";
            return View(viewModel);
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

                var updateSum = Builders<EmployeeWorkTimeMonthLog>.Update;
                var updateField = updateSum.Set(m => m.LastUpdated, DateTime.Now);

                // Update 1 date
                if (!entity.In.HasValue && !entity.Out.HasValue)
                {
                    updateField.Inc(m => m.Workday, 1)
                        .Inc(m => m.WorkTime, new TimeSpan(8,0,0).TotalMilliseconds);
                }
                // Update 0.5 date
                if (!entity.In.HasValue || !entity.Out.HasValue)
                {
                    updateField.Inc(m => m.Workday, 0.5)
                        .Inc(m => m.WorkTime, new TimeSpan(4, 0, 0).TotalMilliseconds);
                }
                if (entity.In.HasValue && entity.StatusLate == 0)
                {
                    updateField.Inc(m => m.WorkTime, entity.Late.TotalMilliseconds);
                }
                if (entity.Out.HasValue && entity.StatusEarly == 0)
                {
                    updateField.Inc(m => m.WorkTime, entity.Early.TotalMilliseconds);
                }
                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterUpdateSum, updateField);
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
            //tos.Add(new EmailAddress { Name = "xuan", Address = "xuan.tm@tribat.vn" });
            tos.Add(new EmailAddress { Name = employee.FullName, Address = employee.Email });

            // Send mail to HR: if approve = 1;
            if (approve == 1)
            {
                var listEmailHR = string.Empty;
                var settingListEmailHR = dbContext.Settings.Find(m => m.Enable.Equals(true) && m.Key.Equals("ListEmailHRApproveTimeKeeper")).FirstOrDefault();
                if (settingListEmailHR != null)
                {
                    listEmailHR = settingListEmailHR.Value;
                }
                if (!string.IsNullOrEmpty(listEmailHR))
                {
                    foreach (var email in listEmailHR.Split(";"))
                    {
                        tos.Add(new EmailAddress { Name = email, Address = email });
                    }
                }
                requester += " , HR";
            }

            //if (!seftFlag)
            //{
            //    // cc người tạo dùm
            //    //ccs.Add(new EmailAddress { Name = "xuan", Address = "xuan.tm@tribat.vn" });
            //    ccs.Add(new EmailAddress { Name = userCreate.FullName, Address = userCreate.Email });
            //}

            var webRoot = Environment.CurrentDirectory;
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "TimeKeeperConfirm.html";

            #region parameters
            //{0} : Subject
            //{1} : Nguoi gui yeu cau  - Nguoi tao yeu cau (dùm) 
            //{2} : Tình trạng (đồng ý/hủy)
            //{3} : Nguoi duyet
            //{4} : Email
            //{5} : Chức vụ
            //{6} : Date (từ, đến, số ngày)
            //{7} : Lý do
            //{8}: Link chi tiết
            //{9}: Website
            #endregion
            var subject = "[TRIBAT] Xác nhận công.";
            var status = approve == 3 ? "Đồng ý" : "Không duyệt";
            var inTime = entity.In.HasValue ? entity.In.Value.ToString("hh:mm") : "trống";
            var outTime = entity.Out.HasValue ? entity.Out.Value.ToString("hh:mm") : "trống";
            var lateTime = entity.Late.TotalMilliseconds > 0 ? entity.Late.TotalMinutes.ToString() : "0";
            var earlyTime = entity.Early.TotalMilliseconds > 0 ? entity.Early.TotalMinutes.ToString() : "0";
            var detailTimeKeeping = "Ngày: "+ entity.Date.ToString("dd/MM/yyyy") + "; giờ vào: " + inTime + "; trễ: "+  lateTime +"; giờ ra: "+ outTime +"; sớm: " + earlyTime;
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
                linkDetail,
                Constants.System.domain
                );
            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                CCAddresses = ccs,
                Subject = subject,
                BodyContent = messageBody
            };
            try
            {
                var emailFrom = Constants.System.emailErp;
                var emailFromName = Constants.System.emailErpName;
                var emailFromPwd = Constants.System.emailErpPwd;
                var message = new MimeMessage();
                message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                if (emailMessage.CCAddresses != null && emailMessage.CCAddresses.Count > 0)
                {
                    message.Cc.AddRange(emailMessage.CCAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                }
                if (emailMessage.FromAddresses == null || emailMessage.FromAddresses.Count == 0)
                {
                    emailMessage.FromAddresses = new List<EmailAddress>
                                {
                                    new EmailAddress { Name = emailFromName, Address = emailFrom }
                                };
                }
                message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
                message.Subject = emailMessage.Subject;
                message.Body = new TextPart(TextFormat.Html)
                {
                    Text = emailMessage.BodyContent
                };
                using (var emailClient = new MailKit.Net.Smtp.SmtpClient())
                {
                    //The last parameter here is to use SSL (Which you should!)
                    emailClient.Connect(emailFrom, 465, true);

                    //Remove any OAuth functionality as we won't be using it. 
                    emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                    emailClient.Authenticate(emailFrom, emailFromPwd);

                    emailClient.Send(message);
                    emailClient.Disconnect(true);
                    Console.WriteLine("The mail has been sent successfully !!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            #endregion

            return Json(new { result = true, message = "Cám ơn đã xác nhận, kết quả đang gửi cho người liên quan." });
        }

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
                In = item.In.HasValue ? item.In.Value.ToString(@"hh\:mm"): string.Empty,
                Out = item.Out.HasValue ? item.Out.Value.ToString(@"hh\:mm"): string.Empty,
                Late = item.In.HasValue ? item.Late.ToString(@"hh\:mm"): string.Empty,
                Early = item.Out.HasValue ? item.Early.ToString(@"hh\:mm"): string.Empty
            };
            return Json(result);
        }
        #endregion
    }
}
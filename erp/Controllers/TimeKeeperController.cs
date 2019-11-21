using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
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
using Common.Enums;
using Helpers;
using MimeKit;
using Services;
using NPOI.HSSF.Util;
using NPOI.SS.Util;

namespace Controllers
{
    [Authorize]
    [Route(Constants.LinkTimeKeeper.Main)]
    public class TimeKeeperController : BaseController
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        readonly IHostingEnvironment _env;
        public IConfiguration Configuration { get; }

        public TimeKeeperController(IConfiguration configuration,
            IHostingEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        [Route(Constants.LinkTimeKeeper.Index)]
        public async Task<IActionResult> Index(string thang, string id)
        {
            #region Authorization
            LoginInit(string.Empty, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();

            bool isHr = Utility.IsRight(loginId, Constants.Rights.NhanSu, (int)ERights.View);

            bool isXNDum = Utility.IsRight(loginId, Constants.Rights.XacNhanCongDum, (int)ERights.Add);
            if (!isXNDum)
            {
                var countManager = dbContext.Employees.CountDocuments(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.ManagerEmployeeId.Equals(loginId));
                if (countManager > 0)
                {
                    isXNDum = true;
                }
            }
            if (!isXNDum)
            {
                isXNDum = isHr;
            }

            bool rightBangCong = Utility.IsRight(loginId, Constants.Rights.BangChamCong, (int)ERights.View);
            var rightApprove = false;
            if (dbContext.EmployeeWorkTimeLogs.CountDocuments(m => m.Enable.Equals(true) && m.ConfirmId.Equals(loginId)) > 0)
            {
                rightApprove = true;
            }
            // TẠO TĂNG CA NHÂN VIÊN KHÁC??
            var isManager = Utility.IsManager(loginE);

            var securityPosition = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.ChucVu) && m.Id.Equals("5c88d098d59d56225c4324a8")).FirstOrDefault();
            var isSecurity = loginE.ChucVu == securityPosition.Id;
            #endregion

            id = string.IsNullOrEmpty(id) ? loginId : id;
            var isMe = id == loginId ? true : false;
            var account = id == loginId ? loginE : dbContext.Employees.Find(m => m.Id.Equals(id)).FirstOrDefault();

            #region Dropdownlist
            var sortTimes = Utility.DllMonths();
            var approves = Utility.Approves(account, true, Constants.Rights.XacNhanCong, (int)ERights.Edit);
            var employees = Utility.EmployeesBase(isHr, account.ManagerEmployeeId);
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

            var timekeeperlogs = await dbContext.EmployeeWorkTimeLogs.Find(filter).SortByDescending(m => m.Date).ToListAsync();
            var monthsTimes = await dbContext.EmployeeWorkTimeMonthLogs.Find(filterSum).SortByDescending(m => m.LastUpdated).ToListAsync();

            var viewModel = new TimeKeeperViewModel
            {
                Employee = account,
                Employees = employees,
                EmployeeWorkTimeLogs = timekeeperlogs,
                EmployeeWorkTimeMonthLogs = monthsTimes,
                MonthYears = sortTimes,
                StartWorkingDate = fromDate,
                EndWorkingDate = toDate,
                Approves = approves,
                Thang = thang,
                RightRequest = isXNDum,
                RightManager = rightBangCong,
                Approver = rightApprove,
                IsManager = isManager,
                IsSecurity = isSecurity,
                IsMe = isMe,
                Id = id
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkTimeKeeper.Request)]
        public async Task<IActionResult> RequestTimeKeeper(TimeKeeperViewModel viewModel)
        {
            #region Authorization
            LoginInit(string.Empty, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            #endregion

            var now = DateTime.Now;
            var entity = viewModel.EmployeeWorkTimeLog;
            entity.EmployeeId = string.IsNullOrEmpty(entity.EmployeeId) ? loginId : entity.EmployeeId;
            var isMe = entity.EmployeeId == loginId ? true : false;
            var account = isMe ? loginE : dbContext.Employees.Find(m => m.Id.Equals(entity.EmployeeId)).FirstOrDefault();
            var approveE = new Employee();
            if (isMe)
            {
                approveE = dbContext.Employees.Find(m => m.Id.Equals(entity.ConfirmId)).FirstOrDefault();
                if (approveE == null)
                {
                    return Json(new { result = false, message = "Lỗi: Vui lòng chọn người duyệt công." });
                }
                else
                {
                    entity.ConfirmName = approveE.FullName + " - " + approveE.ChucVuName;
                    entity.ConfirmDate = now;
                    entity.Status = (int)EStatusWork.DaGuiXacNhan;
                }
            }
            else
            {
                entity.ConfirmId = loginE.Id;
                entity.ConfirmName = loginE.FullName + " - " + loginE.ChucVuName;
                entity.ConfirmDate = now;
                entity.Status = (int)EStatusWork.DongY;
            }

            string secureCode = Helper.HashedPassword(Guid.NewGuid().ToString("N").Substring(0, 12));

            #region Update Status
            var builderEmployeeWorkTimeLog = Builders<EmployeeWorkTimeLog>.Filter;
            var filterEmployeeWorkTimeLog = builderEmployeeWorkTimeLog.Eq(m => m.Id, entity.Id);
            var updateEmployeeWorkTimeLog = Builders<EmployeeWorkTimeLog>.Update
                .Set(m => m.Status, entity.Status)
                .Set(m => m.Request, loginId)
                .Set(m => m.RequestDate, now.Date)
                .Set(m => m.Reason, entity.Reason)
                .Set(m => m.ReasonDetail, entity.ReasonDetail)
                .Set(m => m.ConfirmId, entity.ConfirmId)
                .Set(m => m.ConfirmName, entity.ConfirmName)
                .Set(m => m.ConfirmDate, entity.ConfirmDate)
                .Set(m => m.SecureCode, secureCode);
            dbContext.EmployeeWorkTimeLogs.UpdateOne(filterEmployeeWorkTimeLog, updateEmployeeWorkTimeLog);
            #endregion

            entity = dbContext.EmployeeWorkTimeLogs.Find(m => m.Id.Equals(entity.Id)).FirstOrDefault();

            if (isMe)
            {
                #region Send Mail
                var tos = new List<EmailAddress>
                {
                    new EmailAddress { Name = approveE.FullName, Address = approveE.Email }
                };
                var phone = string.Empty;
                if (account.Mobiles != null && account.Mobiles.Count > 0)
                {
                    phone = account.Mobiles[0].Number;
                }

                var webRoot = Environment.CurrentDirectory;
                var pathToFile = _env.WebRootPath
                        + Path.DirectorySeparatorChar.ToString()
                        + "Templates"
                        + Path.DirectorySeparatorChar.ToString()
                        + "EmailTemplate"
                        + Path.DirectorySeparatorChar.ToString()
                        + "TimeKeeperRequest.html";

                var subject = "Hỗ trợ xác nhận công.";
                var requester = account.FullName;
                var var3 = account.FullName;
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
                    approveE.FullName,
                    requester,
                    var3,
                    account.Email,
                    account.ChucVuName,
                    detailTimeKeeping,
                    entity.Reason,
                    entity.ReasonDetail,
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
                    EmployeeId = account.Id
                };
                var scheduleEmail = new ScheduleEmail
                {
                    Status = (int)EEmailStatus.Schedule,
                    To = emailMessage.ToAddresses,
                    CC = emailMessage.CCAddresses,
                    BCC = emailMessage.BCCAddresses,
                    Type = emailMessage.Type,
                    Title = emailMessage.Subject,
                    Content = emailMessage.BodyContent,
                    EmployeeId = emailMessage.EmployeeId
                };
                dbContext.ScheduleEmails.InsertOne(scheduleEmail);
                return Json(new { result = true, message = "Yêu cầu được gửi các bộ phận liên quan." });
                #endregion
            }
            else
            {
                #region update Summary
                var timelog = entity;
                var monthDate = Utility.EndWorkingMonthByDate(timelog.Date);
                var builderUpdateSum = Builders<EmployeeWorkTimeMonthLog>.Filter;
                var filterUpdateSum = builderUpdateSum.Eq(m => m.EmployeeId, timelog.EmployeeId);
                filterUpdateSum &= builderUpdateSum.Eq(m => m.Year, monthDate.Year);
                filterUpdateSum &= builderUpdateSum.Eq(m => m.Month, monthDate.Month);

                double dateInc = 0;
                double worktimeInc = 0;
                double lateInc = 0;
                double earlyInc = 0;
                if (!timelog.In.HasValue && !timelog.Out.HasValue)
                {
                    dateInc += 1;
                    worktimeInc += new TimeSpan(8, 0, 0).TotalMilliseconds;
                }
                else if (!timelog.In.HasValue || !timelog.Out.HasValue)
                {
                    dateInc += 0.5;
                    worktimeInc += new TimeSpan(4, 0, 0).TotalMilliseconds;
                }
                if (timelog.Late.TotalMilliseconds > 0)
                {
                    worktimeInc += timelog.Late.TotalMilliseconds;
                    lateInc += timelog.Late.TotalMilliseconds;
                }
                if (timelog.Early.TotalMilliseconds > 0)
                {
                    worktimeInc += timelog.Early.TotalMilliseconds;
                    earlyInc += timelog.Early.TotalMilliseconds;
                }
                var updateSum = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Inc(m => m.Workday, dateInc)
                    .Inc(m => m.WorkTime, worktimeInc)
                    .Inc(m => m.Late, -(lateInc))
                    .Inc(m => m.Early, -(earlyInc))
                    .Set(m => m.LastUpdated, DateTime.Now);
                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterUpdateSum, updateSum);
                #endregion

                #region Send email to user
                var approvement = dbContext.Employees.Find(m => m.Id.Equals(timelog.ConfirmId)).FirstOrDefault();
                var employee = dbContext.Employees.Find(m => m.Id.Equals(timelog.EmployeeId)).FirstOrDefault();
                var requester = employee.FullName;
                var tos = new List<EmailAddress>
                {
                    new EmailAddress { Name = employee.FullName, Address = employee.Email }
                };

                var hrs = Utility.EmailGet(Constants.Rights.NhanSu, (int)ERights.Edit);
                if (hrs != null && hrs.Count > 0)
                {
                    requester += " , HR";
                    foreach (var item in hrs)
                    {
                        if (tos.Count(m => m.Address.Equals(item.Address)) == 0)
                        {
                            tos.Add(item);
                        }
                    }
                }

                var ccs = new List<EmailAddress>();

                var webRoot = Environment.CurrentDirectory;
                var pathToFile = _env.WebRootPath
                        + Path.DirectorySeparatorChar.ToString()
                        + "Templates"
                        + Path.DirectorySeparatorChar.ToString()
                        + "EmailTemplate"
                        + Path.DirectorySeparatorChar.ToString()
                        + "TimeKeeperConfirm.html";

                var subject = "Xác nhận công.";
                var status = "Đồng ý";
                var inTime = timelog.In.HasValue ? timelog.In.Value.ToString(@"hh\:mm") : string.Empty;
                var outTime = timelog.Out.HasValue ? timelog.Out.Value.ToString(@"hh\:mm") : string.Empty;
                var lateTime = timelog.Late.TotalMilliseconds > 0 ? Math.Round(timelog.Late.TotalMinutes, 0).ToString() : "0";
                var earlyTime = timelog.Early.TotalMilliseconds > 0 ? Math.Round(timelog.Early.TotalMinutes, 0).ToString() : "0";
                var sumTime = string.Empty;
                if (string.IsNullOrEmpty(inTime) && string.IsNullOrEmpty(outTime))
                {
                    sumTime = "1 ngày";
                }
                else if (string.IsNullOrEmpty(inTime) || string.IsNullOrEmpty(outTime))
                {
                    sumTime = "0.5 ngày";
                }
                var minutesMissing = TimeSpan.FromMilliseconds(timelog.Late.TotalMilliseconds + timelog.Early.TotalMilliseconds).TotalMinutes;
                if (minutesMissing > 0)
                {
                    if (!string.IsNullOrEmpty(sumTime))
                    {
                        sumTime += ", ";
                    }
                    sumTime += Math.Round(minutesMissing, 0) + " phút";
                }

                var detailTimeKeeping = "Ngày: " + timelog.Date.ToString("dd/MM/yyyy") + "; thiếu: " + sumTime;
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
                    approvement.ChucVuName,
                    detailTimeKeeping,
                    timelog.Reason,
                    timelog.ReasonDetail,
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
                    EmployeeId = timelog.EmployeeId
                };

                var scheduleEmail = new ScheduleEmail
                {
                    Status = (int)EEmailStatus.Schedule,
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
        }

        [Route(Constants.LinkTimeKeeper.Approvement)]
        public async Task<IActionResult> Approvement(string id, string thang, string phep)
        {
            #region Authorization
            LoginInit(string.Empty, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();

            bool quyenXacNhan = Utility.IsRight(loginId, Constants.Rights.XacNhanCongDum, (int)ERights.Add);
            if (!quyenXacNhan)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            id = string.IsNullOrEmpty(id) ? loginId : id;
            var account = id == loginId ? loginE : dbContext.Employees.Find(m => m.Id.Equals(id)).FirstOrDefault();

            var myDepartment = account.PhongBan;

            #region Dropdownlist
            // Danh sách nhân viên để tạo phép dùm
            var builderEmp = Builders<Employee>.Filter;
            var filterEmp = builderEmp.Eq(m => m.Enable, true) & builderEmp.Eq(m => m.Leave, false);
            filterEmp &= !builderEmp.Eq(m => m.UserName, Constants.System.account);
            // Remove cấp cao ra (theo mã số lương)
            filterEmp &= !builderEmp.In(m => m.NgachLuongCode, new string[] { "C.01", "C.02", "C.03" });
            var employees = await dbContext.Employees.Find(filterEmp).SortBy(m => m.FullName).ToListAsync();

            var sortTimes = Utility.DllMonths();
            var approves = Utility.Approves(account, true, Constants.Rights.XacNhanCong, (int)ERights.Edit);
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
            var filter = builder.Eq(m => m.ConfirmId, loginId) & builder.Eq(m => m.Month, month) & builder.Eq(m => m.Year, year);

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
                Employee = account,
                MonthYears = sortTimes,
                StartWorkingDate = fromDate,
                EndWorkingDate = toDate,
                Approves = approves,
                Thang = thang,
                RightRequest = quyenXacNhan
            };
            return View(viewModel);
        }

        #region CHAM CONG
        [Route(Constants.LinkTimeKeeper.Timer)]
        public async Task<IActionResult> BangChamCong(DateTime Tu, DateTime Den, string Thang, string Nl, string Kcn, string Pb, string Bp, string Fg, string Id)
        {
            #region Authorization
            LoginInit(Constants.Rights.BangChamCong, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            #endregion

            var linkCurrent = string.Empty;

            #region DDL
            var sortTimes = Utility.DllMonths();
            var congtychinhanhs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Company)).ToList();
            var khoichucnangs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.KhoiChucNang)).ToList();

            var chucvus = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.ChucVu)).ToList();

            Nl = string.IsNullOrEmpty(Nl) ? congtychinhanhs.First().Id : Nl;
            Kcn = string.IsNullOrEmpty(Kcn) ? khoichucnangs.Where(m => m.ParentId.Equals(Nl)).First().Id : Kcn;

            var listPBRemove = new List<string>
            {
                "5c88d094d59d56225c43240f", // CHU TICH
                "5c88d094d59d56225c432412" // GIAM DOC
            };
            var phongbans = dbContext.Categories.Find(m => m.Enable.Equals(true)
            && m.Type.Equals((int)ECategory.PhongBan)
            && m.ParentId.Equals(Kcn) && !listPBRemove.Contains(m.Id)).ToList();
            Pb = string.IsNullOrEmpty(Pb) ? phongbans.Where(m => m.ParentId.Equals(Kcn)).First().Id : Pb;

            var bophans = dbContext.Categories.Find(m => m.Enable.Equals(true)
            && m.Type.Equals((int)ECategory.BoPhan)
            && m.ParentId.Equals(Pb) && string.IsNullOrEmpty(m.ParentId)).ToList();

            var employees = await dbContext.Employees.Find(m => !m.UserName.Equals(Constants.System.account)
                            && m.Enable.Equals(true) && m.IsOnline.Equals(true)
                            && m.IsTimeKeeper.Equals(true))
                           .SortBy(m => m.FullName).ToListAsync();
            // NHAN VIEN NGHI => MOI THANG QUET IF KO CO CHAM CONG CAP NHAT IsOnline => FALSE
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
            var filter = !builder.Eq(i => i.UserName, Constants.System.account)
                & builder.Eq(m => m.Enable, true);

            if (!string.IsNullOrEmpty(Id))
            {
                filter &= builder.Eq(x => x.Id, Id.Trim());
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
                filter &= builder.Eq(m => m.CongTyChiNhanh, Nl);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Nl=" + Nl;

                filter &= builder.Eq(m => m.KhoiChucNang, Kcn);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Kcn=" + Kcn;

                filter &= builder.Eq(m => m.PhongBan, Pb);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Pb=" + Pb;

                if (!string.IsNullOrEmpty(Bp))
                {
                    filter &= builder.Eq(m => m.BoPhan, Bp);
                    linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                    linkCurrent += "Bp=" + Bp;
                }
            }

            if (!string.IsNullOrEmpty(Fg))
            {
                filter &= builder.Where(m => m.Workplaces.Any(c => c.Fingerprint == Fg.Trim()));
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
                filterT &= builderT.Where(m => employeeIds.Contains(m.EmployeeId));
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
            LoginInit(Constants.Rights.BangChamCong, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            #endregion

            var linkCurrent = string.Empty;
            #region DDL
            var sortTimes = Utility.DllMonths();
            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true)
                        && !m.UserName.Equals(Constants.System.account)
                        && m.IsTimeKeeper.Equals(true))
                        .SortBy(m => m.FullName).ToListAsync();

            var congtychinhanhs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Company)).ToList();
            var khoichucnangs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.KhoiChucNang)).ToList();
            var phongbans = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.KhoiChucNang)).ToList();
            var bophans = dbContext.Categories.Find(m => m.Enable.Equals(true) && string.IsNullOrEmpty(m.ParentId) && m.Type.Equals((int)ECategory.BoPhan)).ToList();
            var chucvus = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.ChucVu)).ToList();
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
            var filter = !builder.Eq(i => i.UserName, Constants.System.account)
                        & builder.Eq(m => m.Enable, true);
            if (!string.IsNullOrEmpty(Id))
            {
                filter &= builder.Eq(x => x.Id, Id.Trim());
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Id=" + Id;
            }
            if (!string.IsNullOrEmpty(Fg))
            {
                filter &= builder.Where(m => m.Workplaces.Any(c => c.Fingerprint == Fg.Trim()));
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Fg=" + Fg;
                sFileName += "-" + Fg;
            }
            if (!string.IsNullOrEmpty(Nl))
            {
                filter &= builder.Eq(m => m.CongTyChiNhanh, Nl);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Nl=" + Nl;
                sFileName += "-" + Nl;
            }
            if (!string.IsNullOrEmpty(Kcn))
            {
                filter &= builder.Eq(m => m.KhoiChucNang, Kcn);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Kcn=" + Kcn;
                sFileName += "-" + Kcn;
            }
            if (!string.IsNullOrEmpty(Pb))
            {
                filter &= builder.Eq(m => m.PhongBan, Pb);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Pb=" + Pb;
                sFileName += "-" + Pb;
            }
            if (!string.IsNullOrEmpty(Bp))
            {
                filter &= builder.Eq(m => m.BoPhan, Bp);
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
                filterT &= builderT.Where(m => employeeIds.Contains(m.EmployeeId));
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

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày công");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Lễ tết");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
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

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("");
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("");
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
                cell.SetCellValue("P");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("KP");
                cell.CellStyle = styleHeader;
                columnIndex++;

                rowIndex++;
                #endregion

                var order = 1;
                foreach (var employee in results)
                {
                    var timesSort = employee.EmployeeWorkTimeLogs.OrderBy(m => m.Date).ToList();
                    double ngayCongNT = 0;
                    var vaoTreLan = 0;
                    double vaoTrePhut = 0;
                    var raSomLan = 0;
                    double raSomPhut = 0;
                    double tangCaNgayThuong = 0;
                    double tangCaChuNhat = 0;
                    double tangCaLeTet = 0;
                    double vangKP = 0;
                    double ngayNghiP = 0;
                    double letet = 0;

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
                            var dayString = string.Empty;
                            var displayInOut = string.Empty;
                            var noilamviec = !string.IsNullOrEmpty(item.WorkplaceCode) ? item.WorkplaceCode : string.Empty;
                            var reason = !string.IsNullOrEmpty(item.Reason) ? item.Reason : string.Empty;
                            var detail = !string.IsNullOrEmpty(item.ReasonDetail) ? item.ReasonDetail : string.Empty;
                            var statusTangCa = item.StatusTangCa;
                            var statusBag = statusTangCa == (int)ETangCa.TuChoi ? "badge-pill" : "badge-info";
                            var giotangcathucte = Math.Round(item.TangCaThucTe.TotalHours, 2);
                            var giotangcaxacnhan = Math.Round(item.TangCaDaXacNhan.TotalHours, 2);

                            var isMiss = false;
                            if (item.Mode == (int)ETimeWork.Normal)
                            {
                                switch (item.Status)
                                {
                                    case (int)EStatusWork.XacNhanCong:
                                        {
                                            isMiss = true;
                                            break;
                                        }
                                    case (int)EStatusWork.DaGuiXacNhan:
                                        {
                                            //if (isMonth)
                                            //{
                                            //    isMiss = true;
                                            //}
                                            //else
                                            //{
                                            item.WorkDay = 1;
                                            ngayCongNT++;
                                            //}
                                            break;
                                        }
                                    case (int)EStatusWork.DongY:
                                        {
                                            item.WorkDay = 1;
                                            ngayCongNT++;
                                            break;
                                        }
                                    case (int)EStatusWork.TuChoi:
                                        {
                                            isMiss = true;
                                            break;
                                        }
                                    default:
                                        {
                                            ngayCongNT++;
                                            break;
                                        }
                                }
                            }

                            // Calculator = hour
                            if (item.Mode == (int)ETimeWork.Sunday || item.Mode == (int)ETimeWork.Holiday)
                            {
                                if (item.WorkTime.TotalHours > 0)
                                {
                                    dayString = Math.Round(item.WorkTime.TotalHours, 2) + " giờ";
                                }
                            }

                            if (item.Mode == (int)ETimeWork.LeavePhep)
                            {
                                ngayNghiP += item.SoNgayNghi;
                                if (item.SoNgayNghi < 1)
                                {
                                    item.WorkDay = 0.5;
                                    ngayCongNT += 0.5;
                                }
                            }
                            if (item.Mode == (int)ETimeWork.Holiday)
                            {
                                letet += 1;
                            }

                            if (item.Logs != null)
                            {
                                if (isMiss)
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
                                    var timeoutin = item.Out - item.In;
                                    if (timeoutin.HasValue && timeoutin.Value.TotalHours > 6)
                                    {
                                        // First, không tính 15p
                                        item.WorkDay = 1;
                                        ngayCongNT++;
                                    }
                                    else
                                    {
                                        if (item.Out.HasValue || item.In.HasValue)
                                        {
                                            item.WorkDay = 0.5;
                                        }
                                        ngayCongNT += item.WorkDay;
                                    }
                                }
                                displayInOut = item.In.HasValue ? item.In.Value.ToString(@"hh\:mm") : string.Empty;
                                if (item.Out.HasValue)
                                {
                                    displayInOut += !string.IsNullOrEmpty(displayInOut) ? " - " + item.Out.Value.ToString(@"hh\:mm") : item.Out.Value.ToString(@"hh\:mm");
                                }

                                // TANG CA
                                if (statusTangCa == (int)ETangCa.DongY)
                                {
                                    if (item.Mode == (int)ETimeWork.Normal)
                                    {
                                        tangCaNgayThuong += item.TangCaDaXacNhan.TotalHours;
                                    }
                                    if (item.Mode == (int)ETimeWork.Sunday)
                                    {
                                        tangCaChuNhat += item.TangCaDaXacNhan.TotalHours;
                                    }
                                    if (item.Mode == (int)ETimeWork.Holiday)
                                    {
                                        tangCaLeTet += item.TangCaDaXacNhan.TotalHours;
                                    }
                                }
                            }

                            dayString = item.WorkDay + " ngày";

                            if (item.Logs == null)
                            {
                                var text = item.Reason;
                                if (item.Mode == (int)ETimeWork.Normal)
                                {
                                    text += ";" + Constants.TimeKeeper(item.Status);
                                    if (!string.IsNullOrEmpty(item.ReasonDetail))
                                    {
                                        text += ";" + item.ReasonDetail;
                                    }
                                }
                                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                                sheet1.AddMergedRegion(cellRangeAddress);
                                cell = row.CreateCell(columnIndex, CellType.String);
                                cell.SetCellValue(text);
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
                                var detailText = dayString;
                                if (item.Status > (int)EStatusWork.DuCong)
                                {
                                    detailText += ";" + Constants.TimeKeeper(item.Status);
                                }
                                if (item.Mode != (int)ETimeWork.Normal && item.WorkDay < 1)
                                {
                                    detailText += ";" + Constants.WorkTimeMode(item.Mode);
                                    detailText += ":" + item.SoNgayNghi;
                                }
                                if (!string.IsNullOrEmpty(detail))
                                {
                                    detailText += ";" + detail;
                                }
                                cell.SetCellValue(detailText);
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
                    cell.SetCellValue(letet);
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
                    cell.SetCellValue(ngayNghiP);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 4, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(vangKP);
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

        #region TANG CA
        [Route(Constants.LinkTimeKeeper.Overtime)]
        public async Task<IActionResult> Overtime(DateTime Tu, DateTime Den, string Id, int TrangThai, int Trang, string SapXep, string ThuTu)
        {
            var now = DateTime.Now;
            var linkCurrent = string.Empty;

            #region Authorization
            LoginInit(string.Empty, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            #endregion

            #region Filter
            Id = string.IsNullOrEmpty(Id) ? loginId : Id;
            linkCurrent += "Id=" + Id;

            var builder = Builders<OvertimeEmployee>.Filter;
            Tu = Tu.Year < 1990 ? new DateTime(now.Year, now.Month, 1).Add(new TimeSpan(0, 0, 0)) : Tu.Date.Add(new TimeSpan(0, 0, 0));
            Den = Den.Year < 1990 ? now.Date.Add(new TimeSpan(23, 59, 59)) : Den.Date.Add(new TimeSpan(23, 59, 59));
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Eq(m => m.EmployeeId, Id)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);

            if (TrangThai != 0)
            {
                filter &= builder.Eq(m => m.Status, TrangThai);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "TrangThai=" + TrangThai;
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<OvertimeEmployee>.Sort.Ascending(m => m.Date);
            SapXep = string.IsNullOrEmpty(SapXep) ? "ngay" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "ten":
                    sortBuilder = ThuTu == "asc" ? Builders<OvertimeEmployee>.Sort.Ascending(m => m.EmployeeAlias) : Builders<OvertimeEmployee>.Sort.Descending(m => m.EmployeeAlias);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<OvertimeEmployee>.Sort.Ascending(m => m.Date) : Builders<OvertimeEmployee>.Sort.Descending(m => m.Date);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            var settingPage = dbContext.Settings.Find(m => m.Type.Equals((int)EData.System) && m.Key.Equals("page-size")).FirstOrDefault();
            int Size = Convert.ToInt32(settingPage.Value);
            int pages = 1;
            var records = dbContext.OvertimeEmployees.CountDocuments(filter);
            var enablePage = false;
            if (records > 0 && records > Size)
            {
                enablePage = true;
                pages = (int)Math.Ceiling((double)records / (double)Size);
                if (Trang > pages)
                {
                    Trang = 1;
                }
            }

            var list = new List<OvertimeEmployee>();
            if (enablePage)
            {
                list = dbContext.OvertimeEmployees.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * Size).Limit(Size).ToList();
            }
            else
            {
                list = dbContext.OvertimeEmployees.Find(filter).Sort(sortBuilder).ToList();
            }
            var viewModel = new TimeKeeperViewModel()
            {
                Name = "Quản lý tăng ca",
                OvertimeEmployees = list,
                Id = Id,
                Tu = Tu,
                Den = Den,
                TrangThai = TrangThai,
                LinkCurrent = linkCurrent,
                ThuTu = ThuTu,
                SapXep = SapXep,
                Records = (int)records,
                PageSize = Size,
                PageTotal = pages,
                PageCurrent = Trang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkTimeKeeper.Overtime + "/" + Constants.ActionLink.Data)]
        public async Task<IActionResult> OvertimeData(DateTime? Ngay)
        {
            var today = DateTime.Now.Date;
            var linkCurrent = string.Empty;

            #region Authorization
            LoginInit(string.Empty, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            #endregion

            var directorE = new Employee();
            if (!string.IsNullOrEmpty(loginE.ManagerEmployeeId))
            {
                directorE = dbContext.Employees.Find(m => m.Id.Equals(loginE.ManagerEmployeeId) && m.Enable.Equals(true) && m.Leave.Equals(false)).FirstOrDefault();
            }

            var Tu = Ngay ?? today;

            #region Filter
            bool isEdit = true;
            var overtimes = dbContext.OvertimeEmployees.Find(m => m.EmployeeId.Equals(loginE.Id) && m.Date.Equals(Tu) && !m.Status.Equals((int)EOvertime.Cancel)).ToList();
            if (overtimes == null || overtimes.Count == 0)
            {
                isEdit = false;
                overtimes = new List<OvertimeEmployee>
                {
                    new OvertimeEmployee()
                    {
                        Date = today
                    }
                };
            }
            ViewData[Constants.ActionViews.isEdit] = isEdit;
            #endregion

            var viewModel = new TimeKeeperViewModel()
            {
                Name = "Tăng ca",
                Id = loginId,
                OvertimeEmployees = overtimes,
                Tu = Tu,
                LinkCurrent = linkCurrent,
                Manager = directorE
            };

            return View(viewModel);
        }

        [Route(Constants.LinkTimeKeeper.Overtime + "/" + Constants.ActionLink.Data)]
        [HttpPost]
        public async Task<IActionResult> OvertimeData(TimeKeeperViewModel dataModel)
        {
            try
            {
                #region Authorization
                LoginInit(string.Empty, (int)ERights.View);
                if (!(bool)ViewData[Constants.ActionViews.IsLogin])
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
                }
                var loginId = User.Identity.Name;
                var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
                #endregion

                var employeeId = string.IsNullOrEmpty(dataModel.Id) ? loginId : dataModel.Id;
                var employeeE = string.IsNullOrEmpty(dataModel.Id) ? loginE : dbContext.Employees.Find(m => m.Id.Equals(employeeId)).FirstOrDefault();

                #region Define director
                var directorE = new Employee();
                if (!string.IsNullOrEmpty(loginE.ManagerEmployeeId))
                {
                    directorE = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.Id.Equals(loginE.ManagerEmployeeId)).FirstOrDefault();
                }
                #endregion

                var href = "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Overtime;
                var now = DateTime.Now;
                var secure = now.ToString("yyyyMMddHHmmssfff");
                var date = dataModel.Tu.Date;
                var items = dataModel.OvertimeEmployees;

                var lastest = dbContext.OvertimeEmployees.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Code).FirstOrDefault();
                int codeNew = lastest != null ? lastest.CodeInt + 1 : 1;

                var isTrue = false;
                foreach (var item in items)
                {
                    if (item.Hour > 0)
                    {
                        #region Fill Data
                        item.EmployeeId = employeeE.Id;
                        item.EmployeeCode = employeeE.CodeOld;
                        item.EmployeeAlias = employeeE.AliasFullName;
                        item.EmployeeName = employeeE.FullName;
                        item.ChucVuId = employeeE.ChucVu;
                        item.ChucVuName = employeeE.ChucVuName;
                        item.ChucVuAlias = Utility.AliasConvert(employeeE.ChucVuName);
                        item.BoPhanId = employeeE.BoPhan;
                        item.BoPhanName = employeeE.BoPhanName;
                        item.BoPhanAlias = Utility.AliasConvert(employeeE.BoPhanName);
                        item.PhongBanId = employeeE.PhongBan;
                        item.PhongBanName = employeeE.PhongBanName;
                        item.PhongBanAlias = Utility.AliasConvert(employeeE.PhongBanName);
                        item.KhoiChucNangId = employeeE.KhoiChucNang;
                        item.KhoiChucNangName = employeeE.KhoiChucNangName;
                        item.KhoiChucNangAlias = Utility.AliasConvert(employeeE.KhoiChucNangName);
                        item.CongTyChiNhanhId = employeeE.CongTyChiNhanh;
                        item.CongTyChiNhanhName = employeeE.CongTyChiNhanhName;
                        item.CongTyChiNhanhAlias = Utility.AliasConvert(employeeE.CongTyChiNhanhName);
                        item.ManagerId = directorE.Id;
                        item.ApprovedBy = directorE.Id;
                        item.ManagerInfo = directorE.ChucVuName + " - " + directorE.FullName;
                        item.Date = date;
                        item.Type = Utility.GetTypeDate(item.Date);
                        var endDate = Utility.EndWorkingMonthByDate(item.Date);
                        item.Month = endDate.Month;
                        item.Year = endDate.Year;
                        item.Code = codeNew.ToString();
                        item.CodeInt = codeNew;
                        item.ManagerId = directorE.Id;
                        item.StartSecurity = item.StartOvertime;
                        item.EndSecurity = item.EndOvertime;
                        item.HourSecurity = item.Hour;
                        item.Secure = secure;
                        #endregion

                        if (string.IsNullOrEmpty(item.Id))
                        {
                            dbContext.OvertimeEmployees.InsertOne(item);
                        }
                        else
                        {
                            var filter = Builders<OvertimeEmployee>.Filter.Eq(m => m.Id, item.Id);
                            var update = Builders<OvertimeEmployee>.Update
                                .Set(m => m.Date, item.Date)
                                .Set(m => m.StartOvertime, item.StartOvertime)
                                .Set(m => m.EndOvertime, item.EndOvertime)
                                .Set(m => m.Hour, item.Hour)
                                .Set(m => m.StartSecurity, item.StartSecurity)
                                .Set(m => m.EndSecurity, item.EndSecurity)
                                .Set(m => m.HourSecurity, item.HourSecurity)
                                .Set(m => m.Secure, secure)
                                .Set(m => m.Description, item.Description)
                                .Set(m => m.Code, codeNew.ToString())
                                .Set(m => m.CodeInt, codeNew)
                                .Set(m => m.ModifiedOn, DateTime.Now);
                            dbContext.OvertimeEmployees.UpdateOne(filter, update);
                        }

                        isTrue = true;
                    }
                }

                if (isTrue)
                {
                    var linkExcel = RenderExcel(date, codeNew, loginE.Id);

                    var attachments = new List<string>
                    {
                        linkExcel
                    };

                    #region Send mail: Quản lý trực tiếp (phòng ban)
                    var genderDE = directorE.Gender == "Nam" ? "anh" : "chị";
                    var genderLE = loginE.Gender == "Nam" ? "Anh" : "Chị";
                    var genderLELower = loginE.Gender == "Nam" ? "anh" : "chị";
                    var phone = string.Empty;
                    if (loginE.Mobiles != null && loginE.Mobiles.Count > 0)
                    {
                        phone = loginE.Mobiles[0].Number;
                    }

                    var webRoot = Environment.CurrentDirectory;
                    var pathToFile = _env.WebRootPath
                            + Path.DirectorySeparatorChar.ToString()
                            + "Templates"
                            + Path.DirectorySeparatorChar.ToString()
                            + "EmailTemplate"
                            + Path.DirectorySeparatorChar.ToString()
                            + "OvertimeApprove.html";

                    var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = directorE.FullName, Address = directorE.Email }
            };

                    var ccs = new List<EmailAddress>
            {
                new EmailAddress { Name = loginE.FullName, Address = loginE.Email }
            };

                    var subject = "Xác nhận Bảng tăng ca ngày " + date.ToString("dd/MM/yyyy");
                    var title = "Bảng tăng ca ngày " + date.ToString("dd/MM/yyyy");
                    // Api update, generate code.
                    var linkapprove = Constants.System.domain + "/xacnhan/tangca";
                    var linkAccept = linkapprove + "?code=" + codeNew + "&approve=" + (int)EOvertime.Ok + "&secure=" + secure;
                    var linkCancel = linkapprove + "?code=" + codeNew + "&approve=" + (int)EOvertime.Cancel + "&secure=" + secure;
                    var linkDetail = Constants.System.domain + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Overtime + "?Tu=" + date.ToString("MM-dd-yyyy") + "&Den=" + date.ToString("MM-dd-yyyy");

                    var bodyBuilder = new BodyBuilder();
                    using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                    {
                        bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                    }
                    string messageBody = string.Format(bodyBuilder.HtmlBody,
                        subject,
                        genderDE,
                        directorE.FullName,
                        genderLE,
                        genderLELower,
                        loginE.FullName,
                        loginE.ChucVuName,
                        loginE.Email,
                        phone,
                        title,
                        linkDetail,
                        linkAccept,
                        linkCancel,
                        Constants.System.domain
                        );

                    var emailMessage = new EmailMessage()
                    {
                        ToAddresses = tos,
                        CCAddresses = ccs,
                        Subject = subject,
                        BodyContent = messageBody,
                        Type = "overtime-approve",
                        EmployeeId = codeNew.ToString(),
                        Attachments = attachments
                    };

                    var scheduleEmail = new ScheduleEmail
                    {
                        Status = (int)EEmailStatus.Schedule,
                        To = emailMessage.ToAddresses,
                        CC = emailMessage.CCAddresses,
                        BCC = emailMessage.BCCAddresses,
                        Type = emailMessage.Type,
                        Title = emailMessage.Subject,
                        Content = emailMessage.BodyContent,
                        EmployeeId = emailMessage.EmployeeId,
                        Attachments = emailMessage.Attachments
                    };

                    dbContext.ScheduleEmails.InsertOne(scheduleEmail);
                    #endregion

                    return Json(new { result = true, source = "create", href, message = "Khởi tạo thành công" });
                }
                else
                {
                    return Json(new { result = false, source = "create", href, message = "Khởi tạo không thành công, kiểm tra lại dữ liệu" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = "Lỗi: " + ex.Message });
            }
        }

        [Route(Constants.LinkTimeKeeper.OvertimeEmployee)]
        public async Task<IActionResult> OvertimeEmployee(DateTime Tu, DateTime Den, int? Nam, int? Thang, int? Tuan, string Id, int TrangThai, int Trang, string SapXep, string ThuTu)
        {
            var now = DateTime.Now;
            var linkCurrent = string.Empty;

            #region Authorization
            LoginInit(string.Empty, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            #endregion

            #region Dropdownlist
            // Danh sách nhân viên đang quản
            var builderEmp = Builders<Employee>.Filter;
            var filterEmp = builderEmp.Eq(m => m.Enable, true) & builderEmp.Eq(m => m.Leave, false);
            filterEmp &= !builderEmp.Eq(m => m.UserName, Constants.System.account);
            filterEmp &= builderEmp.Eq(m => m.ManagerEmployeeId, loginE.Id);
            var employees = await dbContext.Employees.Find(filterEmp).SortBy(m => m.FullName).ToListAsync();
            #endregion

            #region Filter
            var builder = Builders<OvertimeEmployee>.Filter;
            Tu = Tu.Year < 1990 ? new DateTime(now.Year, now.Month, 1).Add(new TimeSpan(0, 0, 0)) : Tu.Date.Add(new TimeSpan(0, 0, 0));
            Den = Den.Year < 1990 ? now.Date.Add(new TimeSpan(23, 59, 59)) : Den.Date.Add(new TimeSpan(23, 59, 59));
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Eq(m => m.ManagerId, loginId)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);

            if (!string.IsNullOrEmpty(Id))
            {
                filter &= builder.Eq(m => m.EmployeeId, Id);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Id=" + Id;
            }
            if (TrangThai != 0)
            {
                filter &= builder.Eq(m => m.Status, TrangThai);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "TrangThai=" + TrangThai;
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<OvertimeEmployee>.Sort.Ascending(m => m.Date);
            SapXep = string.IsNullOrEmpty(SapXep) ? "ngay" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "ten":
                    sortBuilder = ThuTu == "asc" ? Builders<OvertimeEmployee>.Sort.Ascending(m => m.EmployeeAlias) : Builders<OvertimeEmployee>.Sort.Descending(m => m.EmployeeAlias);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<OvertimeEmployee>.Sort.Ascending(m => m.Date) : Builders<OvertimeEmployee>.Sort.Descending(m => m.Date);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            var settingPage = dbContext.Settings.Find(m => m.Type.Equals((int)EData.System) && m.Key.Equals("page-size")).FirstOrDefault();
            int Size = Convert.ToInt32(settingPage.Value);
            int pages = 1;
            var records = dbContext.OvertimeEmployees.CountDocuments(filter);
            var enablePage = false;
            if (records > 0 && records > Size)
            {
                enablePage = true;
                pages = (int)Math.Ceiling((double)records / (double)Size);
                if (Trang > pages)
                {
                    Trang = 1;
                }
            }

            var list = new List<OvertimeEmployee>();
            if (enablePage)
            {
                list = dbContext.OvertimeEmployees.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * Size).Limit(Size).ToList();
            }
            else
            {
                list = dbContext.OvertimeEmployees.Find(filter).Sort(sortBuilder).ToList();
            }
            var viewModel = new TimeKeeperViewModel()
            {
                Name = "Quản lý tăng ca",
                OvertimeEmployees = list,
                Employees = employees,
                Id = Id,
                Tu = Tu,
                Den = Den,
                TrangThai = TrangThai,
                LinkCurrent = linkCurrent,
                ThuTu = ThuTu,
                SapXep = SapXep,
                Records = (int)records,
                PageSize = Size,
                PageTotal = pages,
                PageCurrent = Trang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkTimeKeeper.OvertimeEmployee + "/" + Constants.ActionLink.Data)]
        public async Task<IActionResult> OvertimeEmployeeData(DateTime? Ngay)
        {
            var now = DateTime.Now;
            var linkCurrent = string.Empty;

            #region Authorization
            LoginInit(string.Empty, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            #endregion

            var Tu = Ngay ?? DateTime.Now.Date;

            #region Dropdownlist
            // Danh sách nhân viên đang quản
            var builderEmp = Builders<Employee>.Filter;
            var filterEmp = builderEmp.Eq(m => m.Enable, true) & builderEmp.Eq(m => m.Leave, false);
            filterEmp &= !builderEmp.Eq(m => m.UserName, Constants.System.account);
            filterEmp &= builderEmp.Eq(m => m.ManagerEmployeeId, loginE.Id);
            var employees = await dbContext.Employees.Find(filterEmp).SortBy(m => m.FullName).ToListAsync();
            #endregion

            #region Filter
            var times = dbContext.OvertimeEmployees.Find(m => m.ManagerId.Equals(loginE.Id) && m.Date.Equals(Tu)).ToList();
            if (times == null)
            {
                times = new List<OvertimeEmployee>();
                foreach (var item in employees)
                {
                    times.Add(new OvertimeEmployee()
                    {
                        EmployeeId = item.Id,
                        EmployeeCode = item.CodeOld,
                        EmployeeName = item.FullName,
                        ChucVuName = item.ChucVuName,
                        Hour = 0,
                        HourSecurity = 0
                    });
                }
            }
            else
            {
                if (employees.Count > times.Count)
                {
                    foreach (var item in employees)
                    {
                        if (!times.Any(m => m.EmployeeId.Equals(item.Id)))
                        {
                            times.Add(new OvertimeEmployee()
                            {
                                EmployeeId = item.Id,
                                EmployeeCode = item.CodeOld,
                                EmployeeName = item.FullName,
                                ChucVuName = item.ChucVuName,
                                Hour = 0,
                                HourSecurity = 0
                            });
                        }
                    }
                }
            }
            #endregion

            // Director base PhongBan
            var manager = new Employee();

            var viewModel = new TimeKeeperViewModel()
            {
                Name = "Cập nhật tăng ca",
                OvertimeEmployees = times,
                Employees = employees,
                Manager = manager,
                Tu = Tu,
                LinkCurrent = linkCurrent
            };

            return View(viewModel);
        }

        [Route(Constants.LinkTimeKeeper.OvertimeEmployee + "/" + Constants.ActionLink.Data)]
        [HttpPost]
        public async Task<IActionResult> OvertimeEmployeeData(TimeKeeperViewModel dataModel)
        {
            try
            {
                #region Authorization
                LoginInit(string.Empty, (int)ERights.View);
                if (!(bool)ViewData[Constants.ActionViews.IsLogin])
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
                }
                var loginId = User.Identity.Name;
                var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
                #endregion

                #region Define director
                var directorE = new Employee();
                var congtychinhanhId = loginE.CongTyChiNhanh;
                var listDirector = new string[] { "C.01", "C.02", "C.03" };
                // CTY
                if (congtychinhanhId == "5c88d094d59d56225c43240a")
                {
                    directorE = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                                                        && listDirector.Contains(m.NgachLuongCode) && m.PhongBan.Equals(loginE.PhongBan)).FirstOrDefault();
                }
                else // NM
                {
                    directorE = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                                                        && listDirector.Contains(m.NgachLuongCode) && m.CongTyChiNhanh.Equals(loginE.CongTyChiNhanh)).FirstOrDefault();
                }
                #endregion

                var now = DateTime.Now;
                var data = dataModel.OvertimeEmployees;

                //if (data.Count(m => m.Hour <= 0) > 0)
                //{
                //    return Json(new { result = false, message = "Lỗi: Dữ liệu không đúng." });
                //}

                var date = dataModel.Tu.Date;

                var timestamp = now.Ticks;
                var lastest = dbContext.OvertimeEmployees.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Code).FirstOrDefault();
                int codeNew = lastest != null ? lastest.CodeInt + 1 : 1;

                foreach (var item in data)
                {
                    #region Full Data
                    if (!string.IsNullOrEmpty(item.EmployeeId))
                    {
                        var employeeE = dbContext.Employees.Find(m => m.Id.Equals(item.EmployeeId)).FirstOrDefault();
                        if (employeeE != null)
                        {
                            item.EmployeeCode = employeeE.CodeOld;
                            item.EmployeeAlias = employeeE.AliasFullName;
                            item.EmployeeName = employeeE.FullName;
                            item.ChucVuId = employeeE.ChucVu;
                            item.ChucVuName = employeeE.ChucVuName;
                            item.ChucVuAlias = Utility.AliasConvert(employeeE.ChucVuName);
                            item.BoPhanId = employeeE.BoPhan;
                            item.BoPhanName = employeeE.BoPhanName;
                            item.BoPhanAlias = Utility.AliasConvert(employeeE.BoPhanName);
                            item.PhongBanId = employeeE.PhongBan;
                            item.PhongBanName = employeeE.PhongBanName;
                            item.PhongBanAlias = Utility.AliasConvert(employeeE.PhongBanName);
                            item.KhoiChucNangId = employeeE.KhoiChucNang;
                            item.KhoiChucNangName = employeeE.KhoiChucNangName;
                            item.KhoiChucNangAlias = Utility.AliasConvert(employeeE.KhoiChucNangName);
                            item.CongTyChiNhanhId = employeeE.CongTyChiNhanh;
                            item.CongTyChiNhanhName = employeeE.CongTyChiNhanhName;
                            item.CongTyChiNhanhAlias = Utility.AliasConvert(employeeE.CongTyChiNhanhName);
                        }
                    }

                    item.ManagerId = loginE.Id;
                    var employeeME = dbContext.Employees.Find(m => m.Id.Equals(item.ManagerId)).FirstOrDefault();
                    if (employeeME != null)
                    {
                        item.ManagerInfo = employeeME.FullName + " (" + employeeME.ChucVuName + ")";
                    }

                    item.Date = date;
                    item.Type = Utility.GetTypeDate(item.Date);
                    var endDate = Utility.EndWorkingMonthByDate(item.Date);
                    item.Month = endDate.Month;
                    item.Year = endDate.Year;
                    item.Timestamp = timestamp;
                    item.Code = codeNew.ToString();
                    item.CodeInt = codeNew;
                    item.ManagerId = directorE.Id;
                    item.StartSecurity = item.StartOvertime;
                    item.EndSecurity = item.EndOvertime;
                    item.HourSecurity = item.Hour;
                    #endregion

                    if (string.IsNullOrEmpty(item.Id))
                    {
                        dbContext.OvertimeEmployees.InsertOne(item);
                    }
                    else
                    {
                        var filter = Builders<OvertimeEmployee>.Filter.Eq(m => m.Id, item.Id);
                        var update = Builders<OvertimeEmployee>.Update
                            .Set(m => m.Date, item.Date)
                            .Set(m => m.StartOvertime, item.StartOvertime)
                            .Set(m => m.EndOvertime, item.EndOvertime)
                            .Set(m => m.Hour, item.Hour)
                             .Set(m => m.StartSecurity, item.StartSecurity)
                            .Set(m => m.EndSecurity, item.EndSecurity)
                            .Set(m => m.HourSecurity, item.HourSecurity)
                            .Set(m => m.Timestamp, timestamp)
                            .Set(m => m.Code, codeNew.ToString())
                            .Set(m => m.CodeInt, codeNew)
                            .Set(m => m.ModifiedOn, now);
                        dbContext.OvertimeEmployees.UpdateOne(filter, update);
                    }
                }

                var linkExcel = RenderExcelManager(date, codeNew, loginE.Id);

                var attachments = new List<string>
                    {
                        linkExcel
                    };

                #region Send mail: Giám đốc bộ phận (phòng ban)
                var genderDE = directorE.Gender == "Nam" ? "anh" : "chị";
                var genderLE = loginE.Gender == "Nam" ? "Anh" : "Chị";
                var genderLELower = loginE.Gender == "Nam" ? "anh" : "chị";
                var phone = string.Empty;
                if (loginE.Mobiles != null && loginE.Mobiles.Count > 0)
                {
                    phone = loginE.Mobiles[0].Number;
                }

                var webRoot = Environment.CurrentDirectory;
                var pathToFile = _env.WebRootPath
                        + Path.DirectorySeparatorChar.ToString()
                        + "Templates"
                        + Path.DirectorySeparatorChar.ToString()
                        + "EmailTemplate"
                        + Path.DirectorySeparatorChar.ToString()
                        + "OvertimeApprove.html";

                var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = directorE.FullName, Address = directorE.Email }
            };

                var ccs = new List<EmailAddress>
            {
                new EmailAddress { Name = loginE.FullName, Address = loginE.Email }
            };

                var subject = "Xác nhận Bảng tăng ca ngày " + date.ToString("dd/MM/yyyy");
                var title = "Bảng tăng ca ngày " + date.ToString("dd/MM/yyyy");
                // Api update, generate code.
                var linkapprove = Constants.System.domain + "/xacnhan/tangca";
                var linkAccept = linkapprove + "?code=" + codeNew + "&approve=" + (int)EOvertime.Ok + "&secure=" + timestamp;
                var linkCancel = linkapprove + "?code=" + codeNew + "&approve=" + (int)EOvertime.Cancel + "&secure=" + timestamp;
                var linkDetail = Constants.System.domain + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.OvertimeEmployee + "?Tu=" + date.ToString("MM-dd-yyyy") + "&Den=" + date.ToString("MM-dd-yyyy");

                var bodyBuilder = new BodyBuilder();
                using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                {
                    bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                }
                string messageBody = string.Format(bodyBuilder.HtmlBody,
                    subject,
                    genderDE,
                    directorE.FullName,
                    genderLE,
                    genderLELower,
                    loginE.FullName,
                    loginE.ChucVuName,
                    loginE.Email,
                    phone,
                    title,
                    linkDetail,
                    linkAccept,
                    linkCancel,
                    Constants.System.domain
                    );

                var emailMessage = new EmailMessage()
                {
                    ToAddresses = tos,
                    CCAddresses = ccs,
                    Subject = subject,
                    BodyContent = messageBody,
                    Type = "overtime-approve",
                    EmployeeId = codeNew.ToString(),
                    Attachments = attachments
                };

                var scheduleEmail = new ScheduleEmail
                {
                    Status = (int)EEmailStatus.Schedule,
                    To = emailMessage.ToAddresses,
                    CC = emailMessage.CCAddresses,
                    BCC = emailMessage.BCCAddresses,
                    Type = emailMessage.Type,
                    Title = emailMessage.Subject,
                    Content = emailMessage.BodyContent,
                    EmployeeId = emailMessage.EmployeeId,
                    Attachments = emailMessage.Attachments,
                };

                dbContext.ScheduleEmails.InsertOne(scheduleEmail);
                #endregion

                var href = "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.OvertimeEmployee;

                return Json(new { result = true, source = "create", href, message = "Khởi tạo thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = "Lỗi: " + ex.Message });
            }
        }

        [AllowAnonymous]
        [Route(Constants.LinkTimeKeeper.Overtime + "/" + Constants.ActionLink.Approve)]
        [HttpPost]
        public IActionResult OvertimeApprove(int code, int approve, long secure)
        {
            var list = dbContext.OvertimeEmployees.Find(m => m.CodeInt.Equals(code) && m.Timestamp.Equals(secure)).ToList();

            if (list == null)
            {
                return Json(new { result = true, message = Constants.ErrorParameter });
            }

            var filter = Builders<OvertimeEmployee>.Filter.Eq(m => m.CodeInt, code) & Builders<OvertimeEmployee>.Filter.Eq(m => m.Timestamp, secure);
            var update = Builders<OvertimeEmployee>.Update
                .Set(m => m.Timestamp, DateTime.Now.Ticks)
                .Set(m => m.Status, approve)
                .Set(m => m.ModifiedOn, DateTime.Now);
            dbContext.OvertimeEmployees.UpdateMany(filter, update);

            #region Send mail AN NINH
            if (approve == (int)EOvertime.Ok)
            {
                #region parameters
                //{0} : Subject
                //{1} : Gender Nguoi nhan 
                //{2} : Fullname nguoi nhan
                //{3} : Gender nguoi yeu cau
                //{4} : Fullname nguoi yeu cau
                //{5} : Chức vụ
                //{6} : Email
                //{7} : Phone
                //{8} : Noi dung . Example: bang tang ca ngày dd/MM/yyyy
                //{9}: Link chi tiet
                //{10}: Website
                #endregion

                var overtime = list.First();
                var date = overtime.Date;

                var directorE = dbContext.Employees.Find(m => m.Id.Equals(overtime.ManagerId)).FirstOrDefault();

                var securityPosition = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.ChucVu) && m.Id.Equals("5c88d098d59d56225c4324a8")).FirstOrDefault();
                var securityE = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.ChucVu.Equals(securityPosition.Id)).FirstOrDefault();

                var genderDE = directorE.Gender == "Nam" ? "Anh" : "Chị";
                var genderDELower = directorE.Gender == "Nam" ? "anh" : "chị";
                var genderSE = securityE.Gender == "Nam" ? "anh" : "chị";

                var phone = string.Empty;
                if (directorE.Mobiles != null && directorE.Mobiles.Count > 0)
                {
                    phone = directorE.Mobiles[0].Number;
                }

                var webRoot = Environment.CurrentDirectory;
                var pathToFile = _env.WebRootPath
                        + Path.DirectorySeparatorChar.ToString()
                        + "Templates"
                        + Path.DirectorySeparatorChar.ToString()
                        + "EmailTemplate"
                        + Path.DirectorySeparatorChar.ToString()
                        + "OvertimeSecurity.html";

                var tos = new List<EmailAddress>
                {
                    new EmailAddress { Name = securityE.FullName, Address = securityE.Email }
                };

                var subject = "Kiểm tra Bảng tăng ca ngày " + date.ToString("dd/MM/yyyy");
                var title = "Bảng tăng ca ngày " + date.ToString("dd/MM/yyyy");

                var linkDetail = Constants.System.domain + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Overtime + "?Tu=" + date.ToString("MM-dd-yyyy") + "&Den=" + date.ToString("MM-dd-yyyy");

                var bodyBuilder = new BodyBuilder();
                using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                {
                    bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                }
                string messageBody = string.Format(bodyBuilder.HtmlBody,
                    subject,
                    genderSE,
                    securityE.FullName,
                    genderDE,
                    directorE.FullName,
                    directorE.ChucVuName,
                    genderDELower,
                    directorE.Email,
                    phone,
                    title,
                    linkDetail,
                    Constants.System.domain
                    );

                var emailMessage = new EmailMessage()
                {
                    ToAddresses = tos,
                    Subject = subject,
                    BodyContent = messageBody,
                    Type = "overtime-security",
                    EmployeeId = overtime.Code.ToString()
                };

                var scheduleEmail = new ScheduleEmail
                {
                    Status = (int)EEmailStatus.Schedule,
                    To = emailMessage.ToAddresses,
                    CC = emailMessage.CCAddresses,
                    BCC = emailMessage.BCCAddresses,
                    Type = emailMessage.Type,
                    Title = emailMessage.Subject,
                    Content = emailMessage.BodyContent,
                    EmployeeId = emailMessage.EmployeeId
                };

                dbContext.ScheduleEmails.InsertOne(scheduleEmail);
            }
            #endregion

            return Json(new { result = true, message = "Cám ơn đã xác nhận, kết quả đang gửi cho người liên quan." });
        }

        [Route(Constants.LinkTimeKeeper.Overtime + "/" + Constants.LinkTimeKeeper.Security)]
        public async Task<IActionResult> OvertimeSecurity(DateTime Tu, DateTime Den, int? Nam, int? Thang, int? Tuan, string Id, string Pb, int CodeInt, int TrangThai, int Trang, string SapXep, string ThuTu)
        {
            var now = DateTime.Now;
            var linkCurrent = string.Empty;

            #region Authorization
            LoginInit(string.Empty, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId) && m.ChucVu.Equals("5c88d098d59d56225c4324a8")).FirstOrDefault();
            if (loginE == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }
            #endregion

            #region Dropdownlist
            var phongbans = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.PhongBan) && m.Enable.Equals(true)).ToList();

            var listDirector = new string[] { "C.01", "C.02", "C.03" };
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true)
                            && !listDirector.Contains(m.NgachLuongCode)).ToList();
            #endregion

            #region Filter
            var builder = Builders<OvertimeEmployee>.Filter;
            Tu = Tu.Year < 1990 ? new DateTime(now.Year, now.Month, 1).Add(new TimeSpan(0, 0, 0)) : Tu.Date.Add(new TimeSpan(0, 0, 0));
            Den = Den.Year < 1990 ? now.Date.Add(new TimeSpan(23, 59, 59)) : Den.Date.Add(new TimeSpan(23, 59, 59));
            var filter = builder.Eq(m => m.Enable, true)
                        & (builder.Eq(m => m.Status, (int)EOvertime.Ok) | builder.Gte(m => m.Status, (int)EOvertime.Secutity))
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);
            if (!string.IsNullOrEmpty(Id))
            {
                filter &= builder.Eq(m => m.EmployeeId, Id);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Id=" + Id;
            }

            if (!string.IsNullOrEmpty(Pb))
            {
                filter &= builder.Eq(m => m.PhongBanId, Pb);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Pb=" + Pb;
            }

            if (CodeInt != 0)
            {
                filter &= builder.Eq(m => m.CodeInt, CodeInt);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Code=" + CodeInt;
            }

            if (TrangThai != 0)
            {
                filter &= builder.Eq(m => m.Status, TrangThai);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "TrangThai=" + TrangThai;
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<OvertimeEmployee>.Sort.Ascending(m => m.Date);
            SapXep = string.IsNullOrEmpty(SapXep) ? "ngay" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "ten":
                    sortBuilder = ThuTu == "asc" ? Builders<OvertimeEmployee>.Sort.Ascending(m => m.EmployeeAlias) : Builders<OvertimeEmployee>.Sort.Descending(m => m.EmployeeAlias);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<OvertimeEmployee>.Sort.Ascending(m => m.Date) : Builders<OvertimeEmployee>.Sort.Descending(m => m.Date);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            var settingPage = dbContext.Settings.Find(m => m.Type.Equals((int)EData.System) && m.Key.Equals("page-size")).FirstOrDefault();
            int Size = Convert.ToInt32(settingPage.Value);
            int pages = 1;
            var records = dbContext.OvertimeEmployees.CountDocuments(filter);
            var enablePage = false;
            if (records > 0 && records > Size)
            {
                enablePage = true;
                pages = (int)Math.Ceiling((double)records / (double)Size);
                if (Trang > pages)
                {
                    Trang = 1;
                }
            }

            var list = new List<OvertimeEmployee>();
            if (enablePage)
            {
                list = dbContext.OvertimeEmployees.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * Size).Limit(Size).ToList();
            }
            else
            {
                list = dbContext.OvertimeEmployees.Find(filter).Sort(sortBuilder).ToList();
            }

            var viewModel = new TimeKeeperViewModel()
            {
                Name = "Quản lý tăng ca",
                OvertimeEmployees = list,
                PhongBans = phongbans,
                Employees = employees,
                Id = Id,
                CodeInt = CodeInt,
                Tu = Tu,
                Den = Den,
                TrangThai = TrangThai,
                LinkCurrent = linkCurrent,
                ThuTu = ThuTu,
                SapXep = SapXep,
                Records = (int)records,
                PageSize = Size,
                PageTotal = pages,
                PageCurrent = Trang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkTimeKeeper.Overtime + "/" + Constants.LinkTimeKeeper.Security + "/" + Constants.ActionLink.Data)]
        [HttpPost]
        public async Task<IActionResult> OvertimeSecurityData(TimeKeeperViewModel dataModel)
        {
            try
            {
                #region Authorization
                LoginInit(string.Empty, (int)ERights.View);
                if (!(bool)ViewData[Constants.ActionViews.IsLogin])
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
                }
                var loginId = User.Identity.Name;
                var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId) && m.ChucVu.Equals("5c88d098d59d56225c4324a8")).FirstOrDefault();
                if (loginE == null)
                {
                    #region snippet1
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    #endregion
                    return RedirectToAction("login", "account");
                }
                #endregion

                var now = DateTime.Now;
                var timestamp = now.Ticks;
                var data = dataModel.OvertimeEmployees;

                //if (data.Count(m => m.HourSecurity <= 0) > 0)
                //{
                //    return Json(new { result = false, message = "Lỗi: Dữ liệu không đúng." });
                //}

                foreach (var item in data)
                {
                    if (!string.IsNullOrEmpty(item.Id) && item.CheckOnUI)
                    {
                        var filter = Builders<OvertimeEmployee>.Filter.Eq(m => m.Id, item.Id);
                        var update = Builders<OvertimeEmployee>.Update
                            .Set(m => m.StartSecurity, item.StartSecurity)
                            .Set(m => m.EndSecurity, item.EndSecurity)
                            .Set(m => m.HourSecurity, item.HourSecurity)
                            .Set(m => m.Status, (int)EOvertime.Secutity)
                            .Set(m => m.Timestamp, timestamp)
                            .Set(m => m.ModifiedOn, DateTime.Now);
                        dbContext.OvertimeEmployees.UpdateOne(filter, update);

                        // Update Timer
                        var timeWork = dbContext.EmployeeWorkTimeLogs.Find(m => m.Enable.Equals(true)
                                            && m.EmployeeId.Equals(item.EmployeeId)
                                            && m.Date.Equals(item.Date)).FirstOrDefault();
                        if (timeWork != null)
                        {
                            var filterT = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, timeWork.Id);
                            var updateT = Builders<EmployeeWorkTimeLog>.Update
                                .Set(m => m.StatusTangCa, (int)ETangCa.DongY)
                                .Set(m => m.TangCaDaXacNhan, TimeSpan.FromHours(item.HourSecurity))
                                .Set(m => m.UpdatedOn, DateTime.Now);
                            dbContext.EmployeeWorkTimeLogs.UpdateOne(filterT, updateT);

                            // UPDATE SUMMARY
                            var builderS = Builders<EmployeeWorkTimeMonthLog>.Filter;
                            var filterS = builderS.Eq(m => m.EmployeeId, timeWork.EmployeeId) & builderS.Eq(m => m.EnrollNumber, timeWork.EnrollNumber)
                                & builderS.Eq(m => m.Month, timeWork.Month) & builderS.Eq(m => m.Year, timeWork.Year);

                            var updateS = Builders<EmployeeWorkTimeMonthLog>.Update
                                .Set(m => m.LastUpdated, now);
                            if (timeWork.Mode == (int)ETimeWork.Normal)
                            {
                                updateS = updateS.Set(m => m.CongTangCaNgayThuongGio, item.HourSecurity);
                            }
                            else if (timeWork.Mode == (int)ETimeWork.Sunday)
                            {
                                updateS = updateS.Set(m => m.CongCNGio, item.HourSecurity);
                            }
                            else
                            {
                                updateS = updateS.Set(m => m.CongLeTet, item.HourSecurity);
                            }
                            dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterS, updateS);

                            // COM
                            var overtimehourfood = Convert.ToDouble(Utility.GetSetting("overtime-food-hour"));
                            if (item.HourSecurity >= overtimehourfood)
                            {
                                var builderF = Builders<EmployeeCong>.Filter;
                                var filterF = builderF.Eq(m => m.EmployeeId, timeWork.EmployeeId)
                                    & builderF.Eq(m => m.Month, timeWork.Month) & builderF.Eq(m => m.Year, timeWork.Year);
                                var updateF = Builders<EmployeeCong>.Update
                                            .Set(m => m.Com, (double)1);
                                dbContext.EmployeeCongs.UpdateOne(filterF, updateF);
                            }
                        }
                    }
                }

                var href = "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Overtime + "/" + Constants.LinkTimeKeeper.Security;
                return Json(new { result = true, source = "create", href, message = "Khởi tạo thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = "Lỗi: " + ex.Message });
            }
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

        //[HttpPost]
        //[Route(Constants.LinkTimeKeeper.XacNhanTangCa)]
        //public IActionResult XacNhanTangCa(string id, double thoigian, int trangthai)
        //{
        //    var now = DateTime.Now;
        //    int status = trangthai == 1 ? (int)ETangCa.DongY : (int)ETangCa.TuChoi;

        //    var timeWork = dbContext.EmployeeWorkTimeLogs.Find(m => m.Enable.Equals(true) && m.Id.Equals(id)).FirstOrDefault();

        //    var filter = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, id);
        //    var update = Builders<EmployeeWorkTimeLog>.Update
        //        .Set(m => m.StatusTangCa, status)
        //        .Set(m => m.TangCaDaXacNhan, TimeSpan.FromHours(thoigian))
        //        .Set(m => m.UpdatedOn, DateTime.Now);
        //    dbContext.EmployeeWorkTimeLogs.UpdateOne(filter, update);

        //    // UPDATE SUMMARY
        //    var builderS = Builders<EmployeeWorkTimeMonthLog>.Filter;
        //    var filterS = builderS.Eq(m => m.EmployeeId, timeWork.EmployeeId) & builderS.Eq(m => m.EnrollNumber, timeWork.EnrollNumber)
        //        & builderS.Eq(m => m.Month, timeWork.Month) & builderS.Eq(m => m.Year, timeWork.Year);

        //    var updateS = Builders<EmployeeWorkTimeMonthLog>.Update
        //        .Set(m => m.LastUpdated, now);
        //    if (timeWork.Mode == (int)ETimeWork.Normal)
        //    {
        //        updateS = updateS.Set(m => m.CongTangCaNgayThuongGio, thoigian);
        //    }
        //    else if (timeWork.Mode == (int)ETimeWork.Sunday)
        //    {
        //        updateS = updateS.Set(m => m.CongCNGio, thoigian);
        //    }
        //    else
        //    {
        //        updateS = updateS.Set(m => m.CongLeTet, thoigian);
        //    }
        //    dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterS, updateS);

        //    return Json(new { error = 0, id, trangthai, thoigian });
        //}
        #endregion

        #region SUB
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

                var approveEntity = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.Leave.Equals(false) && m.ChucVu.Equals(employeeE.ManagerId)).FirstOrDefault();
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

        private string RenderExcel(DateTime Ngay, int Code, string LoginId)
        {
            var list = dbContext.OvertimeEmployees.Find(m => m.Code.Equals(Code) && m.Date.Equals(Ngay) && m.EmployeeId.Equals(LoginId)).ToList();

            var sFileName = "bang-tang-ca-code-" + Code + ".xlsx";
            var root = _env.WebRootPath;
            string exportFolder = Path.Combine(root, "documents", "overtimes", Ngay.ToString("yyyyMMdd"));
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
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

                ISheet sheet1 = workbook.CreateSheet("TC" + Code + "-" + Ngay.ToString("dd-MM-yyyy"));

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
                cell.SetCellValue("BẢNG TĂNG CA NGÀY " + Ngay.ToString("dd/MM/yyyy") + " (" + Code + ")");
                cell.CellStyle = styleTitle;
                rowIndex++;

                #region Header
                rowIndex++;
                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("#");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Họ và tên");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Chức vụ");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Giờ bắt đầu");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Giờ kết thúc");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Số giờ");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Loại tăng ca");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ghi chú");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Chữ ký");
                cell.CellStyle = styleHeader;
                columnIndex++;
                rowIndex++;
                #endregion

                var i = 1;
                foreach (var item in list)
                {
                    row = sheet1.CreateRow(rowIndex);
                    columnIndex = 0;
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(i);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Date.ToString("dd/MM/yyyy"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.EmployeeCode);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.EmployeeName);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ChucVuName);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.StartOvertime.ToString(@"hh\:mm"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.EndOvertime.ToString(@"hh\:mm"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.Hour);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(Constants.TimeWork(item.Type));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Description);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = styleDedault;
                    columnIndex++;
                    rowIndex++;
                    i++;
                }

                workbook.Write(fs);
            }

            return Path.Combine(exportFolder, sFileName);
        }

        private string RenderExcelManager(DateTime Ngay, int Code, string ManagerId)
        {
            var list = dbContext.OvertimeEmployees.Find(m => m.Code.Equals(Code) && m.Date.Equals(Ngay) && m.ManagerId.Equals(ManagerId)).ToList();

            var sFileName = "bang-tang-ca-code-" + Code + ".xlsx";
            var root = _env.WebRootPath;
            string exportFolder = Path.Combine(root, "documents", "overtimes", Ngay.ToString("yyyyMMdd"));
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
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

                ISheet sheet1 = workbook.CreateSheet("TC" + Code + "-" + Ngay.ToString("dd-MM-yyyy"));

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
                cell.SetCellValue("BẢNG TĂNG CA NGÀY " + Ngay.ToString("dd/MM/yyyy") + " (" + Code + ")");
                cell.CellStyle = styleTitle;
                rowIndex++;

                #region Header
                rowIndex++;
                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("#");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Họ và tên");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Chức vụ");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Giờ bắt đầu");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Giờ kết thúc");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Số giờ");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Loại tăng ca");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ghi chú");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Chữ ký");
                cell.CellStyle = styleHeader;
                columnIndex++;
                rowIndex++;
                #endregion

                var i = 1;
                foreach (var item in list)
                {
                    row = sheet1.CreateRow(rowIndex);
                    columnIndex = 0;
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(i);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Date.ToString("dd/MM/yyyy"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.EmployeeCode);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.EmployeeName);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ChucVuName);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.StartOvertime.ToString(@"hh\:mm"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.EndOvertime.ToString(@"hh\:mm"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.Hour);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(Constants.TimeWork(item.Type));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Description);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = styleDedault;
                    columnIndex++;
                    rowIndex++;
                    i++;
                }

                workbook.Write(fs);
            }

            return Path.Combine(exportFolder, sFileName);
        }
        #endregion
    }
}
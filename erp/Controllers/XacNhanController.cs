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
using Services;
using MimeKit;
using MimeKit.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Common.Enums;
using Helpers;
using MongoDB.Bson;

namespace erp.Controllers
{
    public class XacNhanController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public XacNhanController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, IEmailSender emailSender,
            ISmsSender smsSender, ILogger<XacNhanController> logger)
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
            return View();
        }

        [AllowAnonymous]
        public IActionResult Phep(string id, int approve, string secure)
        {
            var viewModel = new LeaveViewModel
            {
                Approve = approve
            };
            var leave = dbContext.Leaves.Find(m => m.Id.Equals(id)).FirstOrDefault();

            #region Extensions
            var builderTraining = Builders<Trainning>.Filter;
            var filterTraining = builderTraining.Eq(m => m.Enable, true);
            filterTraining = filterTraining & builderTraining.Eq(m => m.Type, "anh-van");
            var listTraining = dbContext.Trainnings.Find(filterTraining).Limit(10).SortByDescending(m => m.CreatedOn).ToList();
            viewModel.ListTraining = listTraining;
            #endregion

            if (leave == null)
            {
                ViewData["Status"] = "Rất tiếc! Dữ liệu đã được cập nhật hoặc thông tin không tồn tại trên hệ thống.";

                return View(viewModel);
            }

            if (leave.SecureCode != secure && leave.Status != 0)
            {
                ViewData["Status"] = "Rất tiếc! Dữ liệu đã được cập nhật hoặc thông tin không tồn tại trên hệ thống.";

                return View(viewModel);
            }

            viewModel.Leave = leave;

            #region Update status
            var filter = Builders<Leave>.Filter.Eq(m => m.Id, id);
            var update = Builders<Leave>.Update
                .Set(m => m.SecureCode, Helper.HashedPassword(Guid.NewGuid().ToString("N").Substring(0, 12)))
                .Set(m => m.Status, approve)
                .Set(m => m.ApprovedBy, leave.ApproverId);

            dbContext.Leaves.UpdateOne(filter, update);
            #endregion

            #region update Leave Date if CANCEL
            if (approve == 2)
            {
                #region QUAN LY LOAI PHEP, BU, NGHI KO TINH LUONG,...
                var typeLeave = dbContext.LeaveTypes.Find(m => m.Id.Equals(leave.TypeId)).FirstOrDefault();
                // Nghi phep
                if (typeLeave.Alias == "phep-nam")
                {
                    var builderLeaveEmployee = Builders<LeaveEmployee>.Filter;
                    var filterLeaveEmployee = builderLeaveEmployee.Eq(m => m.EmployeeId, leave.EmployeeId)
                                            & builderLeaveEmployee.Eq(x => x.LeaveTypeId, leave.TypeId);
                    var updateLeaveEmployee = Builders<LeaveEmployee>.Update.Inc(m => m.Number, leave.Number);
                    dbContext.LeaveEmployees.UpdateOne(filterLeaveEmployee, updateLeaveEmployee);
                }
                // Phep khac,...
                #endregion
            }
            #endregion

            #region Tracking everything

            #endregion

            var approvement = dbContext.Employees.Find(m => m.Id.Equals(leave.ApproverId)).FirstOrDefault();
            // Tự yêu cầu
            bool seftFlag = leave.EmployeeId == leave.CreatedBy ? true : false;
            var employee = dbContext.Employees.Find(m => m.Id.Equals(leave.EmployeeId)).FirstOrDefault();
            var userCreate = employee;
            if (!seftFlag)
            {
                userCreate = dbContext.Employees.Find(m => m.Id.Equals(leave.CreatedBy)).FirstOrDefault();
            }

            #region Send email to user leave, cc: người duyệt
            var requester = employee.FullName;
            var tos = new List<EmailAddress>();
            var ccs = new List<EmailAddress>();
            tos.Add(new EmailAddress { Name = employee.FullName, Address = employee.Email });

            // Send mail to HR: if approve = 1;
            if (approve == 1)
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

            // cc người tạo dùm
            if (!seftFlag)
            {
                ccs.Add(new EmailAddress { Name = userCreate.FullName, Address = userCreate.Email });
            }

            // cc người duyệt
            ccs.Add(new EmailAddress { Name = approvement.FullName, Address = approvement.Email });

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
                    + "LeaveApprove.html";

            #region parameters
            //{0} : Subject
            //{1} : Nguoi gui yeu cau  - Nguoi tao yeu cau (dùm) 
            //{2} : Tình trạng (đồng ý/hủy)
            //{3} : Nguoi duyet
            //{4} : Email
            //{5} : Chức vụ
            //{6} : Date (từ, đến, số ngày)
            //{7} : Lý do
            //{8} : Loại phép
            //{9} : Số điện thoại liên hệ
            //{10}: Link chi tiết
            //{11}: Website
            #endregion
            var status = approve == 1 ? "Đồng ý" : "Không đồng ý";
            var dateRequest = leave.From.ToString("dd/MM/yyyy HH:mm") + " - " + leave.To.ToString("dd/MM/yyyy HH:mm");

            var subject = "[Nghỉ phép] Kết quả duyệt: " + status + " - ngày " + dateRequest;

            var countLeaveDay = " (" + leave.Number + " ngày)";

            var linkDetail = Constants.System.domain + "/" + Constants.LinkLeave.Main + "/" + Constants.LinkLeave.Index;
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
                dateRequest + countLeaveDay,
                leave.Reason,
                leave.TypeName,
                leave.Phone,
                linkDetail,
                Constants.System.domain
                );

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                CCAddresses = ccs,
                Subject = subject,
                BodyContent = messageBody,
                Type = "leave-confirm",
                EmployeeId = leave.EmployeeId
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
            //_emailSender.SendEmail(emailMessage);

            #endregion

            ViewData["Status"] = "Cám ơn đã xác nhận, kết quả đang gửi cho người liên quan.";

            return View(viewModel);
        }

        [AllowAnonymous]
        public IActionResult Cong(string id, int approve, string secure)
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
                ViewData["Status"] = "Rất tiếc! Dữ liệu đã được cập nhật hoặc thông tin không tồn tại trên hệ thống.";

                return View(viewModel);
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

            //_emailSender.SendEmail(emailMessage);
            #endregion

            ViewData["Status"] = "Cám ơn đã xác nhận, kết quả đang gửi cho người liên quan.";
            return View(viewModel);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using MongoDB.Driver;
using Data;
using ViewModels;
using Models;
using Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using MimeKit;
using Common.Enums;
using Helpers;

namespace Controllers
{
    public class XacNhanController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        readonly IHostingEnvironment _env;
        public IConfiguration Configuration { get; }
        public XacNhanController(IConfiguration configuration, 
            IHostingEnvironment env)
        {
            Configuration = configuration;
            _env = env;
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

            #region Extensions: Trainning
            var builderTraining = Builders<Trainning>.Filter;
            var filterTraining = builderTraining.Eq(m => m.Enable, true);
            filterTraining &= builderTraining.Eq(m => m.Type, "anh-van");
            var listTraining = dbContext.Trainnings.Find(filterTraining).Limit(10).SortByDescending(m => m.CreatedOn).ToList();
            viewModel.ListTraining = listTraining;
            #endregion

            if (leave == null || (leave.SecureCode != secure && leave.Status != 0))
            {
                ViewData["Status"] = Constants.ErrorParameter;
                viewModel.Error = true;
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

            #region Send email to user leave, cc: người duyệt
            var approvement = dbContext.Employees.Find(m => m.Id.Equals(leave.ApproverId)).FirstOrDefault();
            // Tự yêu cầu
            bool seftFlag = leave.EmployeeId == leave.CreatedBy ? true : false;
            var employee = dbContext.Employees.Find(m => m.Id.Equals(leave.EmployeeId)).FirstOrDefault();
            var userCreate = employee;
            if (!seftFlag)
            {
                userCreate = dbContext.Employees.Find(m => m.Id.Equals(leave.CreatedBy)).FirstOrDefault();
            }
            var requester = employee.FullName;
            var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = employee.FullName, Address = employee.Email }
            };
            if (approve == (int)EStatusLeave.Accept)
            {
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
            }

            // cc người tạo dùm
            var ccs = new List<EmailAddress>();
            if (!seftFlag)
            {
                ccs.Add(new EmailAddress { Name = userCreate.FullName, Address = userCreate.Email });
            }
            // cc người duyệt
            ccs.Add(new EmailAddress { Name = approvement.FullName, Address = approvement.Email });

            var webRoot = Environment.CurrentDirectory;
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "LeaveApprove.html";

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
                approvement.ChucVuName,
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
            #region Extensions: Trainning
            var builderTraining = Builders<Trainning>.Filter;
            var filterTraining = builderTraining.Eq(m => m.Enable, true);
            filterTraining &= builderTraining.Eq(m => m.Type, "anh-van");
            var listTraining = dbContext.Trainnings.Find(filterTraining).Limit(10).SortByDescending(m => m.CreatedOn).ToList();
            viewModel.ListTraining = listTraining;
            #endregion

            var timelog = dbContext.EmployeeWorkTimeLogs.Find(m => m.Id.Equals(id)).FirstOrDefault();
            if (timelog == null || (timelog.SecureCode != secure && timelog.Status != 2))
            {
                ViewData["Status"] = Constants.ErrorParameter;
                viewModel.Error = true;
                return View(viewModel);
            }

            viewModel.EmployeeWorkTimeLog = timelog;

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
            if (approve == (int)EStatusWork.DongY)
            {
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
            }
            #endregion

            #region Send email to user
            var approvement = dbContext.Employees.Find(m => m.Id.Equals(timelog.ConfirmId)).FirstOrDefault();
            var employee = dbContext.Employees.Find(m => m.Id.Equals(timelog.EmployeeId)).FirstOrDefault();
            var requester = employee.FullName;
            var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = employee.FullName, Address = employee.Email }
            };
            if (approve == (int)EStatusWork.DongY)
            {
                var hrs = Utility.EmailGet(Constants.Rights.NhanSu, (int)ERights.Edit);
                if (hrs != null && hrs.Count > 0)
                {
                    requester += " , HR";
                    foreach(var item in hrs)
                    {
                        if (tos.Count(m => m.Address.Equals(item.Address)) == 0)
                        {
                            tos.Add(item);
                        }
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
            var status = approve == 3 ? "Đồng ý" : "Không duyệt";
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

            ViewData["Status"] = "Cám ơn đã xác nhận, kết quả đang gửi cho người liên quan.";

            return View(viewModel);
        }

        [AllowAnonymous]
        public IActionResult TangCa(int code, int approve, string secure)
        {
            var viewModel = new TimeKeeperViewModel
            {
                Approve = approve
            };

            #region Extensions
            var builderTraining = Builders<Trainning>.Filter;
            var filterTraining = builderTraining.Eq(m => m.Enable, true);
            filterTraining &= builderTraining.Eq(m => m.Type, "anh-van");
            var listTraining = dbContext.Trainnings.Find(filterTraining).Limit(10).SortByDescending(m => m.CreatedOn).ToList();
            viewModel.ListTraining = listTraining;
            #endregion

            var list = dbContext.OvertimeEmployees.Find(m => m.CodeInt.Equals(code) && m.Secure.Equals(secure)).ToList();

            if (list != null && list.Count > 0)
            {
                var filter = Builders<OvertimeEmployee>.Filter.Eq(m => m.CodeInt, code) & Builders<OvertimeEmployee>.Filter.Eq(m => m.Secure, secure);
                var update = Builders<OvertimeEmployee>.Update
                    .Set(m => m.Secure, DateTime.Now.ToString("yyyyMMddHHmmssfff"))
                    .Set(m => m.Status, approve)
                    .Set(m => m.ModifiedOn, DateTime.Now);
                dbContext.OvertimeEmployees.UpdateMany(filter, update);

                #region Send mail
                var overtime = list.First();
                var date = overtime.Date;
                var directorE = dbContext.Employees.Find(m => m.Id.Equals(overtime.ManagerId)).FirstOrDefault();
                var genderDE = directorE.Gender == "Nam" ? "Anh" : "Chị";
                var genderDELower = directorE.Gender == "Nam" ? "anh" : "chị";
                var phone = string.Empty;
                if (directorE.Mobiles != null && directorE.Mobiles.Count > 0)
                {
                    phone = directorE.Mobiles[0].Number;
                }
                var employeeE = dbContext.Employees.Find(m => m.Id.Equals(overtime.EmployeeId)).FirstOrDefault();
                var genderE = employeeE.Gender == "Nam" ? "Anh" : "Chị";
                var genderELower = employeeE.Gender == "Nam" ? "anh" : "chị";

                if (approve == (int)EOvertime.Ok)
                {
                    // AN NINH
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

                    var securityPosition = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.ChucVu) && m.Id.Equals("5c88d098d59d56225c4324a8")).FirstOrDefault();
                    var securityE = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) 
                                    && m.ChucVu.Equals(securityPosition.Id)).FirstOrDefault();
                    if (securityE != null)
                    {
                        var genderSE = securityE.Gender == "Nam" ? "anh" : "chị";

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
                }

                // SEND USER
                if (employeeE != null && !string.IsNullOrEmpty(employeeE.Email) && Utility.IsValidEmail(employeeE.Email))
                {
                    #region parameters
                    //{0} : Subject
                    //{1} : Gender Nguoi nhan 
                    //{2} : Fullname nguoi nhan
                    //{3} : Object
                    //{4} : Trang thai duyet...
                    //{5} : Nguoi duyet
                    //{6} : Chuc vu
                    //{7} : Email
                    //{8} : Phone
                    //{9}: Link chi tiet
                    //{10}: Website
                    #endregion
                    var subject = "[Tăng ca] Kết quả xác nhận tăng ca";
                    var objectName = "xác nhận tăng ca";
                    var webRoot = Environment.CurrentDirectory;
                    var pathToFile = _env.WebRootPath
                            + Path.DirectorySeparatorChar.ToString()
                            + "Templates"
                            + Path.DirectorySeparatorChar.ToString()
                            + "EmailTemplate"
                            + Path.DirectorySeparatorChar.ToString()
                            + "OvertimeResult.html";

                    var tos = new List<EmailAddress>
                    {
                        new EmailAddress { Name = employeeE.FullName, Address = employeeE.Email }
                    };

                    var linkDetail = Constants.System.domain + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Overtime + "?Tu=" + date.ToString("MM-dd-yyyy") + "&Den=" + date.ToString("MM-dd-yyyy");

                    var bodyBuilder = new BodyBuilder();
                    using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                    {
                        bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                    }
                    string messageBody = string.Format(bodyBuilder.HtmlBody,
                        subject,
                        genderELower,
                        employeeE.FullName,
                        objectName,
                        Constants.OvertimeStatus(approve),
                        directorE.FullName,
                        directorE.ChucVuName,
                        directorE.Email,
                        phone,
                        linkDetail,
                        Constants.System.domain
                        );

                    var emailMessage = new EmailMessage()
                    {
                        ToAddresses = tos,
                        Subject = subject,
                        BodyContent = messageBody,
                        Type = "overtime-result",
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

                ViewData["Status"] = "Cám ơn đã xác nhận, kết quả đang gửi cho người liên quan.";
                return View(viewModel);
            }

            ViewData["Status"] = Constants.ErrorParameter;
            viewModel.Error = true;
            return View(viewModel);
        }

        [AllowAnonymous]
        public IActionResult TangCaNhanVien(int code, int approve, string secure)
        {
            var viewModel = new TimeKeeperViewModel
            {
                Approve = approve
            };

            #region Extensions
            var builderTraining = Builders<Trainning>.Filter;
            var filterTraining = builderTraining.Eq(m => m.Enable, true);
            filterTraining = filterTraining & builderTraining.Eq(m => m.Type, "anh-van");
            var listTraining = dbContext.Trainnings.Find(filterTraining).Limit(10).SortByDescending(m => m.CreatedOn).ToList();
            viewModel.ListTraining = listTraining;
            #endregion

            var list = dbContext.OvertimeEmployees.Find(m => m.Code.Equals(code) && m.Secure.Equals(secure)).ToList();

            if (list != null && list.Count > 0)
            {
                var filter = Builders<OvertimeEmployee>.Filter.Eq(m => m.CodeInt, code) & Builders<OvertimeEmployee>.Filter.Eq(m => m.Secure, secure);
                var update = Builders<OvertimeEmployee>.Update
                    .Set(m => m.Secure, DateTime.Now.ToString("yyyyMMddHHmmssfff"))
                    .Set(m => m.Status, approve)
                    .Set(m => m.ApprovedOn, DateTime.Now);
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

                    var directorE = dbContext.Employees.Find(m => m.Id.Equals(overtime.ApprovedBy)).FirstOrDefault();

                    var securityPosition = dbContext.ChucVus.Find(m => m.Code.Equals("CHUCVU86")).FirstOrDefault();

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

                ViewData["Status"] = "Cám ơn đã xác nhận, kết quả đang gửi cho người liên quan.";
                return View(viewModel);
            }

            ViewData["Status"] = Constants.ErrorParameter;
            viewModel.Error = true;
            return View(viewModel);
        }
    }
}
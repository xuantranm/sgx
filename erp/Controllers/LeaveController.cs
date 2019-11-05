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
using Microsoft.AspNetCore.Http;
using Data;
using ViewModels;
using Models;
using Common.Utilities;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Services;
using MimeKit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Common.Enums;
using Helpers;
using MongoDB.Bson;

namespace Controllers
{
    [Authorize]
    [Route(Constants.LinkLeave.Main)]
    public class LeaveController : BaseController
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        IHostingEnvironment _env;
        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;

        public LeaveController(
            IConfiguration configuration,
            IHostingEnvironment env,
            IEmailSender emailSender)
        {
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
        }

        [Route(Constants.LinkLeave.Index)]
        public async Task<IActionResult> Index(string id)
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
            bool isHr = Utility.IsRight(loginId, Constants.Rights.NhanSu, (int)ERights.Add);

            bool isNghiPhepDum = Utility.IsRight(loginId, Constants.Rights.XinNghiPhepDum, (int)ERights.Add);
            if (!isNghiPhepDum)
            {
                var countManager = dbContext.Employees.CountDocuments(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.ManagerEmployeeId.Equals(loginId));
                if (countManager > 0)
                {
                    isNghiPhepDum = true;
                }
            }
            if (!isNghiPhepDum)
            {
                isNghiPhepDum = isHr;
            }

            // Enable link Approve Leave
            var approver = false;
            if (dbContext.Leaves.CountDocuments(m => m.Enable.Equals(true) && m.ApprovedBy.Equals(loginId)) > 0)
            {
                approver = true;
            }
            #endregion

            id = string.IsNullOrEmpty(id) ? loginId : id;
            var account = id == loginId ? loginE : dbContext.Employees.Find(m => m.Id.Equals(id)).FirstOrDefault();

            #region Dropdownlist
            var sortTimes = Utility.DllMonths();
            var types = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true) && m.Display.Equals(true)).ToList();
            var approves = Utility.Approves(account, true, Constants.Rights.XacNhanNghiPhep, (int)ERights.Edit);
            var employees = Utility.EmployeesBase(isHr, account.ManagerEmployeeId);
            #endregion

            #region Create Leave
            var phone = string.Empty;
            if (account.Mobiles != null && account.Mobiles.Count > 0)
            {
                phone = account.Mobiles.First().Number;
            }
            var start = new TimeSpan(8, 0, 0);
            var end = new TimeSpan(17, 0, 0);
            var workingScheduleTime = "8:00-17:00";
            if (account.Workplaces != null)
            {
                var workplaceNM = account.Workplaces.Where(m => m.Code.Equals("NM")).FirstOrDefault();
                if (workplaceNM != null && !string.IsNullOrEmpty(workplaceNM.Fingerprint))
                {
                    workingScheduleTime = workplaceNM.WorkingScheduleTime;
                    start = TimeSpan.Parse(workplaceNM.WorkingScheduleTime.Split("-")[0]);
                    end = TimeSpan.Parse(workplaceNM.WorkingScheduleTime.Split("-")[1]);
                }
            }

            var leave = new Leave
            {
                EmployeeId = account.Id,
                EmployeeName = account.FullName,
                Reason = "Nghỉ phép",
                Phone = phone,
                Start = start,
                End = end,
                WorkingScheduleTime = workingScheduleTime
            };
            #endregion

            // History leave
            var sort = Builders<Leave>.Sort.Descending(m => m.UpdatedOn);
            var leaves = await dbContext.Leaves.Find(m => m.EmployeeId.Equals(account.Id)).Sort(sort).ToListAsync();

            // Quản lý số ngày nghỉ phép còn lại (nghỉ phép, các loại nghỉ bù,...)
            var leaveEmployees = await dbContext.LeaveEmployees.Find(m => m.EmployeeId.Equals(account.Id)).ToListAsync();

            var viewModel = new LeaveViewModel
            {
                Leave = leave,
                Leaves = leaves,
                Employee = account,
                Employees = employees,
                Approves = approves,
                Types = types,
                LeaveEmployees = leaveEmployees,
                RightRequest = isNghiPhepDum,
                Approver = approver,
                IsMe = id == loginId ? true : false
            };

            return View(viewModel);
        }

        [Route(Constants.LinkLeave.Approvement)]
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
            bool isHr = Utility.IsRight(loginId, Constants.Rights.NhanSu, (int)ERights.Add);
            bool isNghiPhepDum = Utility.IsRight(loginId, Constants.Rights.XinNghiPhepDum, (int)ERights.Add);
            if (!isNghiPhepDum)
            {
                var countManager = dbContext.Employees.CountDocuments(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.ManagerEmployeeId.Equals(loginId));
                if (countManager > 0)
                {
                    isNghiPhepDum = true;
                }
            }
            if (!isNghiPhepDum)
            {
                isNghiPhepDum = isHr;
            }
            #endregion

            id = string.IsNullOrEmpty(id) ? loginId : id;
            var account = id == loginId ? loginE : dbContext.Employees.Find(m => m.Id.Equals(id)).FirstOrDefault();

            #region Dropdownlist
            var sortTimes = Utility.DllMonths();
            var types = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true) && m.Display.Equals(true)).ToList();
            var approves = Utility.Approves(account, true, Constants.Rights.XacNhanNghiPhep, (int)ERights.Edit);
            var employees = Utility.EmployeesBase(isHr, account.ManagerEmployeeId);
            #endregion

            var myDepartment = account.PhongBanName;

            // View list approved or no.
            var leaves = await dbContext.Leaves.Find(m => m.ApproverId.Equals(account.Id)).SortByDescending(m => m.ApprovedOn).ToListAsync();
            // View số ngày phép của nhân viên
            var leaveEmployees = new List<LeaveEmployee>();

            // Quản lý số ngày nghỉ phép còn lại (nghỉ phép, các loại nghỉ bù,...)
            if (!string.IsNullOrEmpty(myDepartment))
            {
                leaveEmployees = await dbContext.LeaveEmployees.Find(m => m.Enable.Equals(true) && m.Department.Equals(myDepartment) && !m.EmployeeId.Equals(account.Id)).SortBy(m => m.EmployeeName).ToListAsync();
            }

            var viewModel = new LeaveViewModel
            {
                RightRequest = isNghiPhepDum,
                Leaves = leaves,
                Employee = account,
                Types = types,
                Employees = employees,
                LeaveEmployees = leaveEmployees
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkLeave.Create)]
        public async Task<IActionResult> Create(LeaveViewModel viewModel)
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

            double phepcon = 0;
            var entity = viewModel.Leave;
            if (string.IsNullOrEmpty(entity.EmployeeId))
            {
                return Json(new { result = false, message = "Lỗi: Vui lòng chọn nhân viên." });
            }

            var isMe = entity.EmployeeId == loginId ? true : false;
            var account = isMe ? loginE : dbContext.Employees.Find(m => m.Id.Equals(entity.EmployeeId)).FirstOrDefault();
            var approveE = new Employee();
            if (isMe)
            {
                approveE = dbContext.Employees.Find(m => m.Id.Equals(entity.ApproverId)).FirstOrDefault();
                if (approveE == null)
                {
                    return Json(new { result = false, message = "Lỗi: Vui lòng chọn người duyệt phép." });
                }
                else
                {
                    entity.ApproverName = approveE.FullName + " - " + approveE.ChucVuName;
                }
            }

            #region Fill Data
            var workdayStartTime = TimeSpan.Parse(entity.WorkingScheduleTime.Split("-")[0]);
            var workdayEndTime = TimeSpan.Parse(entity.WorkingScheduleTime.Split("-")[1]);
            entity.From = entity.From.Date.Add(entity.Start);
            entity.To = entity.To.Date.Add(entity.End);
            entity.Number = Utility.GetBussinessDaysBetweenTwoDates(entity.From, entity.To, workdayStartTime, workdayEndTime);

            #region QUAN LY LOAI PHEP, BU, NGHI KO TINH LUONG,...
            var typeLeave = dbContext.LeaveTypes.Find(m => m.Id.Equals(entity.TypeId)).FirstOrDefault();
            if (typeLeave.SalaryPay == true)
            {
                double leaveDayAvailable = 0;
                // Get phép năm, bù còn
                var leaveEmployeePhep = dbContext.LeaveEmployees.Find(m => m.EmployeeId.Equals(entity.EmployeeId) && m.LeaveTypeId.Equals(entity.TypeId)).FirstOrDefault();
                if (leaveEmployeePhep != null)
                {
                    leaveDayAvailable = leaveEmployeePhep.Number;
                }
                // Nghỉ hưởng lương dc phép tạo (thai sản, cưới, sự kiện,...) 
                if (typeLeave.Alias != "nghi-huong-luong")
                {
                    if (leaveDayAvailable < entity.Number)
                    {
                        return Json(new { result = false, message = typeLeave.Name + " không đủ ngày." });
                    }
                }
            }
            #endregion

            #region Tạo trùng ngày
            var builderExist = Builders<Leave>.Filter;
            var filterExist = builderExist.Eq(m => m.Enable, true);
            filterExist &= builderExist.Eq(m => m.EmployeeId, entity.EmployeeId);
            filterExist &= builderExist.Gte(m => m.From, entity.From);
            filterExist &= builderExist.Lte(m => m.To, entity.To);

            var exists = await dbContext.Leaves.Find(filterExist).ToListAsync();
            if (exists != null && exists.Count > 0)
            {
                return Json(new { result = false, message = "Ngày yêu cầu đã được duyệt. Xem danh sách nghỉ bên dưới. Vui lòng yêu cầu ngày khác." });
            }
            #endregion

            entity.SecureCode = Helper.HashedPassword(Guid.NewGuid().ToString("N").Substring(0, 12));
            entity.EmployeeName = account.FullName;
            entity.EmployeeDepartment = account.PhongBanName;
            entity.EmployeePart = account.BoPhanName;
            entity.EmployeeTitle = account.ChucVuName;
            entity.Status = (int)StatusLeave.New;
            entity.CreatedBy = loginE.Id;
            entity.UpdatedBy = loginE.Id;
            if (!isMe)
            {
                entity.Status = (int)StatusLeave.Accept;
                entity.ApproverId = loginE.Id;
                entity.ApproverName = loginE.FullName + " - " + loginE.ChucVuName;
                entity.ApprovedBy = loginE.Id;
            }
            #endregion

            dbContext.Leaves.InsertOne(entity);

            #region CAP NHAT PHEP, BU, NGHI KO TINH LUONG,...
            if (typeLeave.SalaryPay == true)
            {
                #region update Leave Date
                // phep nam, bu
                // Nghỉ hưởng lương dc phép tạo (thai sản, cưới, sự kiện,...) 
                if (typeLeave.Alias != "nghi-huong-luong")
                {
                    var builderLeaveEmployee = Builders<LeaveEmployee>.Filter;
                    var filterLeaveEmployee = builderLeaveEmployee.Eq(m => m.EmployeeId, entity.EmployeeId)
                                            & builderLeaveEmployee.Eq(x => x.LeaveTypeId, entity.TypeId);
                    var updateLeaveEmployee = Builders<LeaveEmployee>.Update.Inc(m => m.Number, -entity.Number)
                                                                            .Inc(m => m.NumberUsed, entity.Number);
                    dbContext.LeaveEmployees.UpdateOne(filterLeaveEmployee, updateLeaveEmployee);
                }
                #endregion

                phepcon = dbContext.LeaveEmployees.AsQueryable().Where(x => x.EmployeeId.Equals(entity.EmployeeId)).Sum(x => x.Number);
            }
            #endregion

            #region Send Mail
            if (isMe)
            {
                var tos = new List<EmailAddress>();
                if (!string.IsNullOrEmpty(entity.ApproverId))
                {
                    tos.Add(new EmailAddress { Name = approveE.FullName, Address = approveE.Email });
                }
                var webRoot = Environment.CurrentDirectory;
                var pathToFile = _env.WebRootPath
                        + Path.DirectorySeparatorChar.ToString()
                        + "Templates"
                        + Path.DirectorySeparatorChar.ToString()
                        + "EmailTemplate"
                        + Path.DirectorySeparatorChar.ToString()
                        + "LeaveRequest.html";

                var subject = "Xác nhận nghỉ phép.";
                var requester = account.FullName;
                var var3 = account.FullName;
                var dateRequest = entity.From.ToString("dd/MM/yyyy HH:mm") + " - " + entity.To.ToString("dd/MM/yyyy HH:mm") + " (" + entity.Number + " ngày)";
                // Api update, generate code.
                var linkapprove = Constants.System.domain + "/xacnhan/phep";
                var linkAccept = linkapprove + "?id=" + entity.Id + "&approve=1&secure=" + entity.SecureCode;
                var linkCancel = linkapprove + "?id=" + entity.Id + "&approve=2&secure=" + entity.SecureCode;
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
                    approveE.Email,
                    account.ChucVuName,
                    dateRequest,
                    entity.Reason,
                    entity.TypeName,
                    entity.Phone,
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
                    Type = "yeu-cau-nghi-phep",
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
            }
            else
            {
                // KO CÓ EMAIL,...
                // [To] nếu có email, người tạo
                var tos = new List<EmailAddress>();
                if (!string.IsNullOrEmpty(account.Email))
                {
                    tos.Add(new EmailAddress { Name = account.FullName, Address = account.Email });
                }

                tos.Add(new EmailAddress { Name = loginE.FullName, Address = loginE.Email });
                
                var ccs = new List<EmailAddress>();
                var hrs = Utility.EmailGet(Constants.Rights.NhanSu, (int)ERights.Edit);
                if (hrs != null && hrs.Count > 0)
                {
                    foreach (var item in hrs)
                    {
                        if (tos.Count(m => m.Address.Equals(item.Address)) == 0)
                        {
                            ccs.Add(item);
                        }
                    }
                }

                var webRoot = Environment.CurrentDirectory;
                var pathToFile = _env.WebRootPath
                        + Path.DirectorySeparatorChar.ToString()
                        + "Templates"
                        + Path.DirectorySeparatorChar.ToString()
                        + "EmailTemplate"
                        + Path.DirectorySeparatorChar.ToString()
                        + "LeaveHelp.html";

                var subject = "Thông tin nghỉ phép.";
                var dateRequest = entity.From.ToString("dd/MM/yyyy HH:mm") + " - " + entity.To.ToString("dd/MM/yyyy HH:mm") + " (" + entity.Number + " ngày)";
                // Api update, generate code.
                var linkDetail = Constants.System.domain;
                var bodyBuilder = new BodyBuilder();
                using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                {
                    bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                }
                string messageBody = string.Format(bodyBuilder.HtmlBody,
                    subject,
                    account.FullName,
                    loginE.FullName,
                    dateRequest,
                    entity.Reason,
                    entity.TypeName,
                    entity.Phone,
                    linkDetail,
                    Constants.System.domain
                    );

                var emailMessage = new EmailMessage()
                {
                    ToAddresses = tos,
                    CCAddresses = ccs,
                    Subject = subject,
                    BodyContent = messageBody,
                    Type = "nghi-phep-thong-tin",
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
            }
            #endregion

            return Json(new { result = true, message = "Thông tin sẽ được gửi các bộ phận liên quan, thời gian tùy theo qui định của công ty và các yếu tố khách quan." });
        }

        [Route(Constants.LinkLeave.ApprovePost)]
        [AllowAnonymous]
        [HttpPost]
        public IActionResult ApprovePost(string id, int approve, string secure)
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
                ViewData["Status"] = Constants.ErrorParameter;

                return View(viewModel);
            }

            if (leave.SecureCode != secure && leave.Status != 0)
            {
                return Json(new { result = true, message = Constants.ErrorParameter });
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
            if (approve == (int)StatusLeave.Cancel)
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

            var approvement = dbContext.Employees.Find(m => m.Id.Equals(leave.ApproverId)).FirstOrDefault();
            // Tự yêu cầu
            bool seftFlag = leave.EmployeeId == leave.CreatedBy ? true : false;
            var employee = dbContext.Employees.Find(m => m.Id.Equals(leave.EmployeeId)).FirstOrDefault();
            var userCreate = employee;
            if (!seftFlag)
            {
                userCreate = dbContext.Employees.Find(m => m.Id.Equals(leave.CreatedBy)).FirstOrDefault();
            }

            #region Send email to user leave
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
                            var emailEntity = dbContext.Employees.Find(m => m.Id.Equals(item.User) && m.Enable.Equals(true) && m.Leave.Equals(false)).Project<Employee>(fields).FirstOrDefault();
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

        #region Sub Data
        [HttpPost]
        [Route(Constants.LinkLeave.CalculatorDate)]
        public IActionResult CalculatorDate(string from, string to, string scheduleWorkingTime, string type)
        {
            double date = 0;
            var fromDate = DateTime.ParseExact(from, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
            var toDate = DateTime.ParseExact(to, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);

            var workdayStartTime = TimeSpan.Parse(scheduleWorkingTime.Split("-")[0]);
            var workdayEndTime = TimeSpan.Parse(scheduleWorkingTime.Split("-")[1]);

            // Get working day later
            date = Utility.GetBussinessDaysBetweenTwoDates(fromDate, toDate, workdayStartTime, workdayEndTime);

            // Check rule
            if (!string.IsNullOrEmpty(type))
            {
                var leaveType = dbContext.LeaveTypes.Find(m => m.Id.Equals(type)).FirstOrDefault();
                if (leaveType != null && leaveType.MaxOnce > 0)
                {
                    if (leaveType.MaxOnce < date)
                    {
                        return Json(new { result = false, source = Constants.LinkLeave.CalculatorDate, date, message = " Số ngày phép vượt mức qui định 1 lần nghỉ." });
                    }
                }
            }

            return Json(new { result = true, source = Constants.LinkLeave.CalculatorDate, date });
        }

        public void InitLeaveTypes()
        {
            dbContext.LeaveTypes.DeleteMany(new BsonDocument());
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Phép năm",
                YearMax = 12,
                MonthMax = 0,
                Description = "Theo điều 111, 112 của Bộ Luật Lao Động."
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Phép không hưởng lương",
                YearMax = 3,
                MonthMax = 0,
                SalaryPay = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Nghỉ hưởng lương",
                YearMax = 0,
                MonthMax = 0,
                Description = "Theo điều 115, 116 Bộ Luật Lao Động. Ví dụ: kết hôn, sinh con,..."
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Nghỉ bù",
                YearMax = 0,
                MonthMax = 0,
                Description = "Hiện tại công ty áp dụng hình thức tính lương, nên không áp dụng Nghỉ bù."
            });

            foreach (var item in dbContext.LeaveTypes.Find(m => true).ToList())
            {
                var filter = Builders<LeaveType>.Filter.Eq(m => m.Id, item.Id);

                var update = Builders<LeaveType>.Update
                    .Set(m => m.Alias, Utility.AliasConvert(item.Name));

                dbContext.LeaveTypes.UpdateOne(filter, update);
            }
        }
        #endregion
    }
}
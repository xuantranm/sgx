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
    [Authorize]
    [Route(Constants.LinkLeave.Main)]
    public class LeaveController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public LeaveController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, IEmailSender emailSender,
            ISmsSender smsSender, ILogger<LeaveController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        [Route(Constants.LinkLeave.Index)]
        public async Task<IActionResult> Index(string id)
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
            if (loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.XinNghiPhepDum, (int)ERights.Add))
            {
                isRight = true;
            }
            #endregion

            id = string.IsNullOrEmpty(id) ? login : id;
            if (id != login)
            {
                userInformation = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(id)).FirstOrDefault();
            }

            var approves = new List<IdName>();
            var phone = string.Empty;
            var start = new TimeSpan(8, 0, 0);
            var end = new TimeSpan(17, 0, 0);
            var workingScheduleTime = "8:00-17:00";
            if (userInformation != null)
            {
                if (userInformation.Mobiles != null && userInformation.Mobiles.Count > 0)
                {
                    phone = userInformation.Mobiles.First().Number;
                }
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
                if (userInformation.Workplaces != null && userInformation.Workplaces.Count > 0)
                {
                    foreach (var workplace in userInformation.Workplaces)
                    {
                        if (!string.IsNullOrEmpty(workplace.WorkingScheduleTime))
                        {
                            workingScheduleTime = workplace.WorkingScheduleTime;
                            start = TimeSpan.Parse(workplace.WorkingScheduleTime.Split("-")[0]);
                            end = TimeSpan.Parse(workplace.WorkingScheduleTime.Split("-")[1]);
                        }
                    }
                }
            }

            // Create new leave
            var leave = new Leave
            {
                EmployeeId = userInformation.Id,
                EmployeeName = userInformation.FullName,
                Reason = "Nghỉ phép",
                Phone = phone,
                Start = start,
                End = end,
                WorkingScheduleTime = workingScheduleTime
            };

            // History leave
            var sort = Builders<Leave>.Sort.Descending(m => m.UpdatedOn);
            var leaves = await dbContext.Leaves.Find(m => m.EmployeeId.Equals(login)).Sort(sort).ToListAsync();

            #region Dropdownlist
            var types = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true) && m.Display.Equals(true)).ToList();
            if (approves.Count == 0)
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

            // Quản lý số ngày nghỉ phép còn lại (nghỉ phép, các loại nghỉ bù,...)
            var leaveEmployees = await dbContext.LeaveEmployees.Find(m => m.EmployeeId.Equals(id)).ToListAsync();

            var approver = false;
            if (dbContext.Leaves.CountDocuments(m => m.Enable.Equals(true) && m.ApprovedBy.Equals(login)) > 0)
            {
                approver = true;
            }
            var viewModel = new LeaveViewModel
            {
                Leave = leave,
                Leaves = leaves,
                Employee = userInformation,
                Approves = approves,
                Types = types,
                LeaveEmployees = leaveEmployees,
                RightRequest = isRight,
                Approver = approver
            };

            return View(viewModel);
        }

        [Route(Constants.LinkLeave.HelpLeave)]
        public async Task<IActionResult> HelpLeave(string id, string thang)
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
            if (loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.XinNghiPhepDum, (int)ERights.Add))
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
            var types = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true) && m.Display.Equals(true)).ToList();
            // Danh sách nhân viên để tạo phép dùm
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(m => m.UserName, Constants.System.account);
            // Remove cấp cao ra (theo mã số lương)
            filter = filter & !builder.In(m => m.NgachLuong, new string[] { "C.01", "C.02", "C.03" });
            var employees = await dbContext.Employees.Find(filter).SortBy(m => m.FullName).ToListAsync();
            var approves = new List<IdName>();
            #endregion

            var leave = new Leave();
            var leaves = new List<Leave>();
            var leaveEmployees = new List<LeaveEmployee>();

            var phone = string.Empty;
            var start = new TimeSpan(7, 0, 0);
            var end = new TimeSpan(16, 0, 0);
            var workingScheduleTime = "7:00-16:00";

            if (userInformation.Mobiles != null && userInformation.Mobiles.Count > 0)
            {
                phone = userInformation.Mobiles.First().Number;
            }
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
            if (userInformation.Workplaces != null && userInformation.Workplaces.Count > 0)
            {
                foreach (var workplace in userInformation.Workplaces)
                {
                    if (!string.IsNullOrEmpty(workplace.WorkingScheduleTime))
                    {
                        workingScheduleTime = workplace.WorkingScheduleTime;
                        start = TimeSpan.Parse(workplace.WorkingScheduleTime.Split("-")[0]);
                        end = TimeSpan.Parse(workplace.WorkingScheduleTime.Split("-")[1]);
                    }
                }
            }

            // Create new leave
            leave = new Leave
            {
                EmployeeId = userInformation.Id,
                EmployeeName = userInformation.FullName,
                Reason = "Nghỉ phép",
                Phone = phone,
                Start = start,
                End = end,
                WorkingScheduleTime = workingScheduleTime
            };

            // History leave
            var sort = Builders<Leave>.Sort.Descending(m => m.UpdatedOn);
            leaves = await dbContext.Leaves.Find(m => m.EmployeeId.Equals(userInformation.Id)).Sort(sort).ToListAsync();

            // Quản lý số ngày nghỉ phép còn lại (nghỉ phép, các loại nghỉ bù,...)
            leaveEmployees = await dbContext.LeaveEmployees.Find(m => m.EmployeeId.Equals(userInformation.Id)).ToListAsync();


            var rolesApprove = dbContext.RoleUsers.Find(m => m.Enable.Equals(true) && m.Role.Equals(Constants.Rights.XacNhanNghiPhep)).ToList();
            foreach (var roleApprove in rolesApprove)
            {
                if (!approves.Any(item => item.Id == roleApprove.User))
                {
                    approves.Add(new IdName
                    {
                        Id = roleApprove.User,
                        Name = roleApprove.FullName
                    });
                }
            }

            var viewModel = new LeaveViewModel
            {
                Leave = leave,
                Leaves = leaves,
                Employee = userInformation,
                Approves = approves,
                Types = types,
                Employees = employees,
                LeaveEmployees = leaveEmployees
            };
            return View(viewModel);
        }

        [Route("cap-nhat-du-lieu-app102")]
        public async Task<IActionResult> ApprovementInit()
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

            var phepNam = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true) && m.Alias.Equals("phep-nam")).FirstOrDefault();
            var leavesHis = dbContext.Leaves.Find(m => true).ToList();
            foreach (var his in leavesHis)
            {
                if (!string.IsNullOrEmpty(his.EmployeeId))
                {
                    var employee = dbContext.Employees.Find(m => m.Id.Equals(his.EmployeeId)).FirstOrDefault();
                    if (employee != null)
                    {
                        var filterH = Builders<Leave>.Filter.Eq(m => m.Id, his.Id);
                        var updateH = Builders<Leave>.Update
                            .Set(m => m.EmployeeDepartment, employee.Department)
                            .Set(m => m.EmployeePart, employee.Part)
                            .Set(m => m.EmployeeTitle, employee.Title);
                        dbContext.Leaves.UpdateOne(filterH, updateH);
                    }
                }
            }

            var leavesEmployees = dbContext.LeaveEmployees.Find(m => true).ToList();
            foreach (var leaveEm in leavesEmployees)
            {
                string department = string.Empty;
                string part = string.Empty;
                string title = string.Empty;
                decimal used = 0;
                if (!string.IsNullOrEmpty(leaveEm.EmployeeId))
                {
                    var employee = dbContext.Employees.Find(m => m.Id.Equals(leaveEm.EmployeeId)).FirstOrDefault();
                    if (employee != null)
                    {
                        department = employee.Department;
                        part = employee.Part;
                        title = employee.Title;
                    }
                }
                var leavesUsed = dbContext.Leaves.Find(m => m.EmployeeId.Equals(leaveEm.EmployeeId) && m.TypeId.Equals(phepNam.Id)).ToList();

                if (leavesUsed != null & leavesUsed.Count > 0)
                {
                    foreach (var leaveUsed in leavesUsed)
                    {
                        used += leaveUsed.Number;
                    }
                }
                var filterE = Builders<LeaveEmployee>.Filter.Eq(m => m.Id, leaveEm.Id);
                var updateE = Builders<LeaveEmployee>.Update
                    .Set(m => m.NumberUsed, used)
                    .Set(m => m.Department, department)
                    .Set(m => m.Part, part)
                    .Set(m => m.Title, title);
                dbContext.LeaveEmployees.UpdateOne(filterE, updateE);
            }

            return Json(new { result = true });
        }

        [Route(Constants.LinkLeave.Approvement)]
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
            if (loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.XinNghiPhepDum, (int)ERights.Add))
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
            var types = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true) && m.Display.Equals(true)).ToList();
            // Danh sách nhân viên để tạo phép dùm
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(m => m.UserName, Constants.System.account);
            // Remove cấp cao ra (theo mã số lương)
            filter = filter & !builder.In(m => m.NgachLuong, new string[] { "C.01", "C.02", "C.03" });
            var employees = await dbContext.Employees.Find(filter).SortBy(m => m.FullName).ToListAsync();
            var approves = new List<IdName>();
            #endregion

            // View list approved or no.
            var leaves = new List<Leave>();
            // View số ngày phép của nhân viên
            var leaveEmployees = new List<LeaveEmployee>();

            leaves = await dbContext.Leaves.Find(m => m.ApproverId.Equals(userInformation.Id)).SortByDescending(m => m.ApprovedOn).ToListAsync();

            // Quản lý số ngày nghỉ phép còn lại (nghỉ phép, các loại nghỉ bù,...)
            if (!string.IsNullOrEmpty(myDepartment))
            {
                leaveEmployees = await dbContext.LeaveEmployees.Find(m => m.Enable.Equals(true) && m.Department.Equals(myDepartment) && !m.EmployeeId.Equals(userInformation.Id)).SortBy(m => m.EmployeeName).ToListAsync();
            }

            var viewModel = new LeaveViewModel
            {
                RightRequest = isRight,
                Leaves = leaves,
                Employee = userInformation,
                Types = types,
                Employees = employees,
                LeaveEmployees = leaveEmployees
            };
            return View(viewModel);
        }

        [Route(Constants.LinkLeave.Manage)]
        public async Task<IActionResult> Manage(string id)
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
            if (loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.XinNghiPhepDum, (int)ERights.Add))
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
            var types = dbContext.LeaveTypes.Find(m => m.Enable.Equals(true) && m.Display.Equals(true)).ToList();
            // Danh sách nhân viên để tạo phép dùm
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(m => m.UserName, Constants.System.account);
            // Remove cấp cao ra (theo mã số lương)
            filter = filter & !builder.In(m => m.NgachLuong, new string[] { "C.01", "C.02", "C.03" });
            var employees = await dbContext.Employees.Find(filter).SortBy(m => m.FullName).ToListAsync();
            var approves = new List<IdName>();
            #endregion

            var leave = new Leave();
            var leaves = new List<Leave>();
            var leaveEmployees = new List<LeaveEmployee>();

            var phone = string.Empty;
            var start = new TimeSpan(7, 0, 0);
            var end = new TimeSpan(16, 0, 0);
            var workingScheduleTime = "7:00-16:00";

            if (userInformation.Mobiles != null && userInformation.Mobiles.Count > 0)
            {
                phone = userInformation.Mobiles.First().Number;
            }
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
            if (userInformation.Workplaces != null && userInformation.Workplaces.Count > 0)
            {
                foreach (var workplace in userInformation.Workplaces)
                {
                    if (!string.IsNullOrEmpty(workplace.WorkingScheduleTime))
                    {
                        workingScheduleTime = workplace.WorkingScheduleTime;
                        start = TimeSpan.Parse(workplace.WorkingScheduleTime.Split("-")[0]);
                        end = TimeSpan.Parse(workplace.WorkingScheduleTime.Split("-")[1]);
                    }
                }
            }

            // Create new leave
            leave = new Leave
            {
                EmployeeId = userInformation.Id,
                EmployeeName = userInformation.FullName,
                Reason = "Nghỉ phép",
                Phone = phone,
                Start = start,
                End = end,
                WorkingScheduleTime = workingScheduleTime
            };

            // History leave
            var sort = Builders<Leave>.Sort.Descending(m => m.UpdatedOn);
            leaves = await dbContext.Leaves.Find(m => m.EmployeeId.Equals(userInformation.Id)).Sort(sort).ToListAsync();

            // Quản lý số ngày nghỉ phép còn lại (nghỉ phép, các loại nghỉ bù,...)
            leaveEmployees = await dbContext.LeaveEmployees.Find(m => m.EmployeeId.Equals(userInformation.Id)).ToListAsync();


            var rolesApprove = dbContext.RoleUsers.Find(m => m.Enable.Equals(true) && m.Role.Equals(Constants.Rights.XacNhanNghiPhep)).ToList();
            foreach (var roleApprove in rolesApprove)
            {
                if (!approves.Any(item => item.Id == roleApprove.User))
                {
                    approves.Add(new IdName
                    {
                        Id = roleApprove.User,
                        Name = roleApprove.FullName
                    });
                }
            }

            var viewModel = new LeaveViewModel
            {
                Leave = leave,
                Leaves = leaves,
                Employee = userInformation,
                Approves = approves,
                Types = types,
                Employees = employees,
                LeaveEmployees = leaveEmployees
            };
            return View(viewModel);
        }

        // Run first time. then comment. No use seconds
        [Route(Constants.LinkLeave.UpdateLeaveDay)]
        public IActionResult UpdateLeaveDay()
        {
            InitLeaveTypes();
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true)).ToList();
            var leaveEmployees = new List<LeaveEmployee>();
            // Update ngày phép, phep bu` = 0;

            var types = dbContext.LeaveTypes.Find(m => m.Display.Equals(true) && m.SalaryPay.Equals(true)).ToList();

            decimal numberDay = 0;
            foreach (var employee in employees)
            {
                foreach (var type in types)
                {
                    if (type.Alias == "phep-nam")
                    {
                        numberDay = employee.LeaveDayAvailable;
                    }
                    leaveEmployees.Add(new LeaveEmployee
                    {
                        LeaveTypeId = type.Id,
                        EmployeeId = employee.Id,
                        LeaveTypeName = type.Name,
                        EmployeeName = employee.FullName,
                        Number = numberDay
                    });
                    numberDay = 0;
                }
            }
            dbContext.LeaveEmployees.DeleteMany(new BsonDocument());
            dbContext.LeaveEmployees.InsertMany(leaveEmployees);
            return Json(new { result = true });
        }

        [HttpPost]
        [Route(Constants.LinkLeave.Create)]
        public async Task<IActionResult> Create(LeaveViewModel viewModel)
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
            #endregion

            decimal phepcon = 0;
            var entity = viewModel.Leave;
            // Tự yêu cầu
            var employee = userInformation;
            // Làm cho người khác
            if (entity.EmployeeId != login)
            {
                employee = dbContext.Employees.Find(m => m.Id.Equals(entity.EmployeeId)).FirstOrDefault();
            }

            var workdayStartTime = new TimeSpan(7, 0, 0);
            var workdayEndTime = new TimeSpan(16, 0, 0);
            if (!string.IsNullOrEmpty(entity.WorkingScheduleTime))
            {
                workdayStartTime = TimeSpan.Parse(entity.WorkingScheduleTime.Split("-")[0]);
                workdayEndTime = TimeSpan.Parse(entity.WorkingScheduleTime.Split("-")[1]);
            }

            // Get working day later
            entity.From = entity.From.Date.Add(entity.Start);
            entity.To = entity.To.Date.Add(entity.End);
            entity.Number = Utility.GetBussinessDaysBetweenTwoDates(entity.From, entity.To, workdayStartTime, workdayEndTime);

            #region QUAN LY LOAI PHEP, BU, NGHI KO TINH LUONG,...
            var typeLeave = dbContext.LeaveTypes.Find(m => m.Id.Equals(entity.TypeId)).FirstOrDefault();
            if (typeLeave.SalaryPay == true)
            {
                // Get phép năm, bù còn
                var leaveEmployeePhep = dbContext.LeaveEmployees.Find(m => m.EmployeeId.Equals(entity.EmployeeId) && m.LeaveTypeId.Equals(entity.TypeId)).FirstOrDefault();
                var leaveDayAvailable = leaveEmployeePhep.Number;
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
            filterExist = filterExist & builderExist.Eq(m => m.EmployeeId, entity.EmployeeId);
            filterExist = filterExist & builderExist.Gte(m => m.From, entity.From);
            filterExist = filterExist & builderExist.Lte(m => m.To, entity.To);

            var exists = await dbContext.Leaves.Find(filterExist).ToListAsync();
            if (exists != null && exists.Count > 0)
            {
                return Json(new { result = false, message = "Ngày yêu cầu đã được duyệt. Xem danh sách nghỉ bên dưới. Vui lòng yêu cầu ngày khác." });
            }
            #endregion

            entity.SecureCode = Helper.HashedPassword(Guid.NewGuid().ToString("N").Substring(0, 12));
            entity.EmployeeName = employee.FullName;
            entity.EmployeeDepartment = employee.Department;
            entity.EmployeePart = employee.Part;
            entity.EmployeeTitle = employee.Title;
            entity.Status = (int)StatusLeave.New;
            entity.CreatedBy = login;
            entity.UpdatedBy = login;

            dbContext.Leaves.InsertOne(entity);

            #region QUAN LY LOAI PHEP, BU, NGHI KO TINH LUONG,...
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

            #region tracking everything

            #endregion

            #region Send Mail
            var tos = new List<EmailAddress>();
            var approver = string.Empty;
            if (!string.IsNullOrEmpty(entity.ApproverId))
            {
                var approve1 = dbContext.Employees.Find(m => m.Id.Equals(entity.ApproverId)).FirstOrDefault();
                approver = approve1.FullName;
                tos.Add(new EmailAddress { Name = approve1.FullName, Address = approve1.Email });
            }
            else
            {
                tos.Add(new EmailAddress { Name = "Tran Minh Xuan", Address = "xuan.tm@tribat.vn" });
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
            var requester = employee.FullName;
            var var3 = employee.FullName;
            //if (!string.IsNullOrEmpty(employee.Title))
            //{
            //    requester += " - " + employee.Title;
            //}
            if (entity.EmployeeId != login)
            {
                requester += " (người tạo phép: " + userInformation.FullName + " , chức vụ " + userInformation.Title + ")";
            }
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
                approver,
                requester,
                var3,
                employee.Email,
                employee.Title,
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
                Type = "yeu-cau-nghi-phep"
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
                ViewData["Status"] = "Rất tiếc! Dữ liệu đã được cập nhật hoặc thông tin không tồn tại trên hệ thống.";

                return View(viewModel);
            }

            if (leave.SecureCode != secure && leave.Status != 0)
            {
                return Json(new { result = true, message = "Rất tiếc! Dữ liệu đã được cập nhật hoặc thông tin không tồn tại trên hệ thống." });
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
            //var uat = dbContext.Settings.Find(m => m.Key.Equals("UAT")).FirstOrDefault();
            //if (uat != null && uat.Value == "true")
            //{
            //    tos = new List<EmailAddress>
            //            {
            //                new EmailAddress { Name = "Xuan", Address = "xuan.tm1988@gmail.com" }
            //            };

            //    ccs = new List<EmailAddress>
            //            {
            //                new EmailAddress { Name = "Xuan CC", Address = "xuantranm@gmail.com" }
            //            };
            //}
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
            var subject = "Xác nhận nghỉ phép.";
            var status = approve == 1 ? "Đồng ý" : "Không duyệt";
            var dateRequest = leave.From.ToString("dd/MM/yyyy HH:mm") + " - " + leave.To.ToString("dd/MM/yyyy HH:mm") + " (" + leave.Number + " ngày)";
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
                dateRequest,
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
                Type = "leave-confirm"
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

        #region Sub Data
        [HttpPost]
        [Route(Constants.LinkLeave.CalculatorDate)]
        public IActionResult CalculatorDate(string from, string to, string scheduleWorkingTime, string type)
        {
            decimal date = 0;
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
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Nguyên Đán dương lịch",
                YearMax = 1,
                MonthMax = 0,
                Display = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Nguyên Đán âm lịch",
                YearMax = 4,
                MonthMax = 0,
                Display = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Ngày Giỗ tổ Hùng Vương",
                YearMax = 1,
                MonthMax = 0,
                Display = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Ngày Thống nhất đất nước",
                YearMax = 1,
                MonthMax = 0,
                Display = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Ngày Quốc tế lao động",
                YearMax = 1,
                MonthMax = 0,
                Display = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = " Ngày Quốc khánh",
                YearMax = 1,
                MonthMax = 0,
                Display = false
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
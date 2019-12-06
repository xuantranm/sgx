using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
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
using NPOI.XSSF.UserModel;
using Microsoft.AspNetCore.Authorization;
using MimeKit;
using Services;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading;
using Common.Enums;
using NPOI.HSSF.Util;
using Helpers;

namespace Controllers
{
    [Authorize]
    public class EmployeeController : BaseController
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        readonly IHostingEnvironment _env;
        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;
        private bool bhxh;

        public EmployeeController(IConfiguration configuration, 
            IHostingEnvironment env, 
            IEmailSender emailSender)
        {
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
        }

        [Route(Constants.LinkHr.Human)]
        public async Task<IActionResult> Index(string Id, string Ten, string Code, string Fg, string Nl, string Kcn, string Pb, string Bp, string Sortby)
        {
            var linkCurrent = string.Empty;

            #region Authorization
            LoginInit(Constants.Rights.HR, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            if (!(bool)ViewData[Constants.ActionViews.IsRight])
            {
                return RedirectToAction("Index", "Home");
            }
            #endregion

            #region Get Setting Value
            var settings = dbContext.Settings.Find(m => m.Enable.Equals(true)).ToList();
            bhxh = false;
            var bhxhSetting = settings.First(m => m.Key.Equals("NoBHXH"));
            if (bhxhSetting != null)
            {
                bhxh = bhxhSetting.Value == "true" ? false : true;
            }
            #endregion

            #region Dropdownlist
            var congtychinhanhs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Company)).ToList();
            var khoichucnangs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.KhoiChucNang)).ToList();
            var phongbans = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.PhongBan)).ToList();
            var bophans = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.BoPhan)).ToList();
            var chucvus = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.ChucVu)).ToList();
            var employeeDdl = await dbContext.Employees.Find(m => m.Enable.Equals(true) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
            #endregion

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = !builder.Eq(i => i.UserName, Constants.System.account) & builder.Eq(m => m.Enable, true);
            if (!string.IsNullOrEmpty(Id))
            {
                filter &= builder.Eq(x => x.Id, Id.Trim());
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Id=" + Id;
            }
            if (!string.IsNullOrEmpty(Ten))
            {
                filter &= (builder.Eq(x => x.Email, Ten.Trim()) | builder.Regex(x => x.FullName, Ten.Trim()));
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Ten=" + Ten;
            }
            if (!string.IsNullOrEmpty(Code))
            {
                filter &= builder.Regex(m => m.Code, Code.Trim());
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Code=" + Code;
            }
            if (!string.IsNullOrEmpty(Fg))
            {
                filter &= builder.Where(m => m.Workplaces.Any(c => c.Fingerprint == Fg.Trim()));
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Fg=" + Fg;
                // Eq("Related._id", "b125");
            }
            if (!string.IsNullOrEmpty(Nl))
            {
                filter &= builder.Eq(m => m.CongTyChiNhanh, Nl);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Nl=" + Nl;
                // Eq("Related._id", "b125");
            }
            if (!string.IsNullOrEmpty(Kcn))
            {
                filter &= builder.Eq(m => m.KhoiChucNang, Kcn);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Kcn=" + Kcn;
            }
            if (!string.IsNullOrEmpty(Pb))
            {
                filter &= builder.Eq(m => m.PhongBan, Pb);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Pb=" + Pb;
            }
            if (!string.IsNullOrEmpty(Bp))
            {
                filter &= builder.Eq(m => m.BoPhan, Bp);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Bp=" + Bp;
            }
            if (bhxh)
            {
                filter &= builder.Eq(m => m.BhxhEnable, bhxh);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "";
                linkCurrent += "Bhxh=" + bhxh;
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Employee>.Sort.Ascending(m => m.Code);
            if (!string.IsNullOrEmpty(Sortby))
            {
                var sortField = Sortby.Split("-")[0];
                var sort = Sortby.Split("-")[1];
                switch (sortField)
                {
                    case "code":
                        sortBuilder = sort == "asc" ? Builders<Employee>.Sort.Ascending(m => m.Code) : Builders<Employee>.Sort.Descending(m => m.Code);
                        break;
                    case "name":
                        sortBuilder = sort == "asc" ? Builders<Employee>.Sort.Ascending(m => m.FullName) : Builders<Employee>.Sort.Descending(m => m.FullName);
                        break;
                    default:
                        sortBuilder = sort == "asc" ? Builders<Employee>.Sort.Ascending(m => m.Code) : Builders<Employee>.Sort.Descending(m => m.Code);
                        break;
                }
            }
            #endregion

            var records = await dbContext.Employees.CountDocumentsAsync(filter);

            var employees = dbContext.Employees.Find(filter).ToList();

            filter &= builder.Eq(m => m.Leave, false);
            var recordCurrent = await dbContext.Employees.CountDocumentsAsync(filter);

            linkCurrent = !string.IsNullOrEmpty(linkCurrent) ? "?" + linkCurrent : linkCurrent;

            var notifications = dbContext.Notifications.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ENotification.Hr)).SortByDescending(m => m.UpdatedOn).Limit(5).ToList();
            var viewModel = new EmployeeViewModel
            {
                Employees = employees,
                Notifications = notifications,
                Records = (int)records,
                RecordCurrent = (int)recordCurrent,
                RecordLeave = (int)records - (int)recordCurrent,
                EmployeesDdl = employeeDdl,
                CongTyChiNhanhs = congtychinhanhs,
                KhoiChucNangs = khoichucnangs,
                PhongBans = phongbans,
                BoPhans = bophans,
                ChucVus = chucvus,
                Id = Id,
                Ten = Ten,
                Code = Code,
                Fg = Fg,
                Nl = Nl,
                Pb = Pb,
                Bp = Bp,
                LinkCurrent = linkCurrent
            };

            return View(viewModel);
        }

        [Route(Constants.LinkHr.Human + "/" + Constants.LinkHr.Information + "/" + "{id}")]
        public async Task<ActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            #region Authorization
            LoginInit(Constants.Rights.HR, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            bool isRight = (bool)ViewData[Constants.ActionViews.IsRight];
            if (!isRight)
            {
                if (id != loginId)
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            #endregion

            var entity = await dbContext.Employees
                .Find(m => m.Id == id).FirstOrDefaultAsync();

            if (entity == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(entity.ManagerEmployeeId))
            {
                var isLeaveManager = dbContext.Employees.CountDocuments(m => m.Id.Equals(entity.ManagerEmployeeId) && m.Leave.Equals(true));
                if (isLeaveManager > 0)
                {
                    var nextManagerE = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.ChucVu.Equals(entity.ManagerId)).FirstOrDefault();
                    if (nextManagerE != null)
                    {
                        entity.ManagerInformation = nextManagerE.ChucVuName + " - " + nextManagerE.FullName;
                    }
                }
            }

            var employeeChanged = await dbContext.EmployeeHistories.Find(m => m.EmployeeId.Equals(id)).SortByDescending(m => m.UpdatedOn).Limit(1).FirstOrDefaultAsync();
            var isChange = false;
            if (employeeChanged != null && employeeChanged.UpdatedOn > entity.UpdatedOn)
            {
                var listHr = new List<string>();
                var hrs = dbContext.RoleUsers.Find(m => m.Role.Equals(Constants.Rights.NhanSu)).ToList();
                foreach (var hr in hrs)
                {
                    listHr.Add(hr.User);
                }
                if (!listHr.Contains(employeeChanged.UpdatedBy))
                {
                    isChange = true;
                }
            }
            ViewData["isChange"] = isChange;
            // Switch employee history
            //if (isChange)
            //{
            //    var tempEntity = entity;
            //    entity = employeeChanged;
            //    entity.Id = id;
            //    employeeChanged = tempEntity;
            //}

            var viewModel = new EmployeeViewModel()
            {
                Employee = entity,
                EmployeeChance = employeeChanged
            };
            return View(viewModel);
        }

        [Route(Constants.LinkHr.Human + "/" + Constants.ActionLink.Data)]
        [Route(Constants.LinkHr.Human + "/" + Constants.ActionLink.Data + "/" + "{id}")]
        public async Task<ActionResult> Data(string id)
        {
            #region Authorization
            LoginInit(Constants.Rights.HR, (int)ERights.Add);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            bool isRight = (bool)ViewData[Constants.ActionViews.IsRight];
            if (!isRight)
            {
                if (id != loginId)
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            #endregion

            #region Dropdownlist
            var congtychinhanhs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Company)).ToList();
            var khoichucnangs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.KhoiChucNang)).ToList();
            var phongbans = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.PhongBan)).ToList();
            var bophans = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.BoPhan)).ToList();
            var chucvus = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.ChucVu)).ToList();
            var hospitals = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Hospital)).ToList();
            var contracts = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Contract)).ToList();
            var workTimeTypes = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.TimeWork)).ToList();
            var banks = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Bank)).ToList();
            var managers = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && !string.IsNullOrEmpty(m.Email) && !string.IsNullOrEmpty(m.ChucVuName) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.ChucVuName).ToList();
            var genders = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Gender)).ToList();
            var probations = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Probation)).ToList();
            var salaryBases = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.SalaryBase)).ToList();
            #endregion

            bool isEdit = false;
            var workplaces = new List<Workplace>();
            foreach(var item in congtychinhanhs)
            {
                workplaces.Add(new Workplace()
                {
                    Code = item.Code,
                    Name = item.Name
                });
            }

            var entity = new Employee
            {
                Joinday = DateTime.Now,
                Birthday = DateTime.Now.AddYears(-60),
                Workplaces = workplaces,
                IsTimeKeeper = true,
                Official = false,
                Nation = "Việt Nam",
                Religion = "Kinh",
                BhxhEnable = false
            };
            var employeeChanged = new Employee();
            var isChange = false;
            if (!string.IsNullOrEmpty(id))
            {
                isEdit = true;
                entity = dbContext.Employees.Find(m => m.Id == id).FirstOrDefault();
                if (!string.IsNullOrEmpty(entity.ManagerEmployeeId))
                {
                    var isLeaveManager = dbContext.Employees.CountDocuments(m => m.Id.Equals(entity.ManagerEmployeeId) && m.Leave.Equals(true));
                    if (isLeaveManager > 0)
                    {
                        var nextManagerE = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.ChucVu.Equals(entity.ManagerId)).FirstOrDefault();
                        if (nextManagerE != null)
                        {
                            entity.ManagerEmployeeId = nextManagerE.Id;
                            entity.ManagerInformation = nextManagerE.ChucVuName + " - " + nextManagerE.FullName;
                        }
                    }
                }

                if (entity.Workplaces == null)
                {
                    entity.Workplaces = workplaces;
                }
                else
                {
                    if (entity.Workplaces.Count < 2)
                    {
                        var wpNM = entity.Workplaces.Where(m => m.Code == "NM").FirstOrDefault();
                        var wpNME = congtychinhanhs.Where(m => m.CodeInt == 2).FirstOrDefault();
                        var wpVPE = congtychinhanhs.Where(m => m.CodeInt == 1).FirstOrDefault();
                        if (wpNM != null)
                        {
                            entity.Workplaces.Add(new Workplace()
                            {
                                Code = wpVPE.Code,
                                Name = wpVPE.Name
                            });
                        }
                        else
                        {
                            entity.Workplaces.Add(new Workplace()
                            {
                                Code = wpNME.Code,
                                Name = wpNME.Name
                            });
                        }
                    }
                }

                employeeChanged = await dbContext.EmployeeHistories.Find(m => m.EmployeeId.Equals(id)).SortByDescending(m => m.UpdatedOn).Limit(1).FirstOrDefaultAsync();
                if (employeeChanged != null && employeeChanged.UpdatedOn > entity.UpdatedOn)
                {
                    var listHr = new List<string>();
                    var hrs = dbContext.RoleUsers.Find(m => m.Role.Equals(Constants.Rights.NhanSu)).ToList();
                    foreach (var hr in hrs)
                    {
                        listHr.Add(hr.User);
                    }
                    if (!listHr.Contains(employeeChanged.UpdatedBy))
                    {
                        isChange = true;
                    }
                }
            }

            ViewData[Constants.ActionViews.isEdit] = isEdit;
            ViewData["isChange"] = isChange;

            // Switch employee history
            //if (isChange)
            //{
            //    var tempEntity = entity;
            //    entity = employeeChanged;
            //    entity.Id = id;
            //    employeeChanged = tempEntity;
            //}

            #region EmailGroup & Flag
            var welcomeGroup = string.Empty;
            var leaveGroup = string.Empty;
            var emailGroups = dbContext.EmailGroups.Find(m => m.Status.Equals(false) && m.Object.Equals(entity.UserName)).ToList();
            if (emailGroups != null && emailGroups.Count > 0)
            {
                var emailGroupNew = emailGroups.Find(m => m.Type.Equals((int)EEmailGroup.New));
                if (emailGroupNew != null)
                {
                    welcomeGroup = emailGroupNew.Name;
                }
                var emailGroupLeave = emailGroups.Find(m => m.Type.Equals((int)EEmailGroup.Leave));
                if (emailGroupLeave != null)
                {
                    leaveGroup = emailGroupLeave.Name;
                }
            }
            #endregion

            var viewModel = new EmployeeViewModel()
            {
                Employee = entity,
                EmployeeChance = employeeChanged,
                CongTyChiNhanhs = congtychinhanhs,
                KhoiChucNangs = khoichucnangs,
                PhongBans = phongbans,
                BoPhans = bophans,
                ChucVus = chucvus,
                Managers = managers,
                WorkTimeTypes = workTimeTypes,
                Hospitals = hospitals,
                Contracts = contracts,
                Genders = genders,
                Probations = probations,
                SalaryBases = salaryBases,
                WelcomeEmailGroup = welcomeGroup,
                LeaveEmailGroup = leaveGroup
            };

            return View(viewModel);
        }

        [Route(Constants.LinkHr.Human + "/" + Constants.ActionLink.Data)]
        [HttpPost]
        public async Task<ActionResult> Data(EmployeeViewModel viewModel)
        {
            var entity = viewModel.Employee;

            #region Authorization
            LoginInit(Constants.Rights.HR, (int)ERights.Add);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            bool isRight = (bool)ViewData[Constants.ActionViews.IsRight];
            bool isOwner = false;
            if (!isRight)
            {
                if (entity.Id != loginId)
                {
                    return RedirectToAction("Index", "Home");
                }
                isOwner = true;
            }
            #endregion

            #region Settings
            var source = Constants.ActionLink.Create;
            bool isEdit = false;
            var result = true;
            var message = Constants.Texts.Success;
            var settings = dbContext.Settings.Find(m => m.Enable.Equals(true)).ToList();
            var identityCardExpired = Convert.ToInt32(settings.Where(m => m.Key.Equals("identityCardExpired")).First().Value);
            var employeeCodeFirst = settings.Where(m => m.Key.Equals("employeeCodeFirst")).First().Value;
            var employeeCodeLength = settings.Where(m => m.Key.Equals("employeeCodeLength")).First().Value;
            #endregion

            #region Update Fields
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-CA");
            var now = DateTime.Now;
            entity.CreatedBy = loginId;
            entity.UpdatedBy = loginId;
            entity.CheckedBy = loginId;
            entity.ApprovedBy = loginId;
            if (string.IsNullOrEmpty(entity.UserName))
            {
                entity.UserName = Utility.UserNameConvert(entity.FullName);
            }

            entity.Email = !string.IsNullOrEmpty(entity.Email) ? entity.Email.Trim() : string.Empty;
            entity.FullName = !string.IsNullOrEmpty(entity.FullName) ? entity.FullName.Trim() : string.Empty;
            entity.AliasFullName = !string.IsNullOrEmpty(entity.FullName) ? Utility.AliasConvert(entity.FullName) : string.Empty;
            entity.Bornplace = !string.IsNullOrEmpty(entity.Bornplace) ? entity.Bornplace.Trim() : string.Empty;
            entity.AddressResident = !string.IsNullOrEmpty(entity.AddressResident) ? entity.AddressResident.Trim() : string.Empty;
            entity.AddressTemporary = !string.IsNullOrEmpty(entity.AddressTemporary) ? entity.AddressTemporary.Trim() : string.Empty;
            entity.EmailPersonal = !string.IsNullOrEmpty(entity.EmailPersonal) ? entity.EmailPersonal.Trim() : string.Empty;
            entity.IdentityCard = !string.IsNullOrEmpty(entity.IdentityCard) ? entity.IdentityCard.Trim() : string.Empty;
            entity.Passport = !string.IsNullOrEmpty(entity.Passport) ? entity.Passport.Trim() : string.Empty;
            entity.PassportCode = !string.IsNullOrEmpty(entity.PassportCode) ? entity.PassportCode.Trim() : string.Empty;
            entity.PassportPlace = !string.IsNullOrEmpty(entity.PassportPlace) ? entity.PassportPlace.Trim() : string.Empty;
            entity.HouseHold = !string.IsNullOrEmpty(entity.HouseHold) ? entity.HouseHold.Trim() : string.Empty;
            entity.HouseHoldOwner = !string.IsNullOrEmpty(entity.HouseHoldOwner) ? entity.HouseHoldOwner.Trim() : string.Empty;

            if (entity.Contracts != null && entity.Contracts.Count > 0)
            {
                for (int i = entity.Contracts.Count - 1; i >= 0; i--)
                {
                    if (string.IsNullOrEmpty(entity.Contracts[i].Code))
                    {
                        entity.Contracts.RemoveAt(i);
                    }
                }
                entity.Contracts = entity.Contracts.Count == 0 ? null : entity.Contracts;
            }
            if (!entity.IsTimeKeeper)
            {
                entity.Workplaces = null;
            }

            if (string.IsNullOrEmpty(entity.Id))
            {
                var pwdrandom = Guid.NewGuid().ToString("N").Substring(0, 6);
                if (!string.IsNullOrEmpty(entity.Password))
                {
                    pwdrandom = entity.Password;
                }
                var sysPassword = Helper.HashedPassword(pwdrandom);
                var lastEntity = dbContext.Employees.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Id).Limit(1).First();
                var x = 1;
                if (lastEntity != null && lastEntity.Code != null)
                {
                    x = Convert.ToInt32(lastEntity.Code.Replace(employeeCodeFirst, string.Empty)) + 1;
                }
                var sysCode = employeeCodeFirst + x.ToString($"D{employeeCodeLength}");
                entity.Code = sysCode;
                entity.Password = sysPassword;
            }

            if (!string.IsNullOrEmpty(entity.ManagerEmployeeId))
            {
                var managerE = dbContext.Employees.Find(m => m.Id.Equals(entity.ManagerEmployeeId)).FirstOrDefault();
                entity.ManagerInformation = managerE.ChucVuName + " - " + managerE.FullName;
                entity.ManagerId = managerE.ChucVu;
            }

            if (!entity.Leave)
            {
                entity.Leaveday = null;
                entity.LeaveReason = string.Empty;
                entity.LeaveHandover = string.Empty;
            }

            if (!string.IsNullOrEmpty(entity.Gender))
            {
                var genderE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.Gender) && m.Id.Equals(entity.Gender)).FirstOrDefault();
                entity.Gender = genderE != null ? genderE.Name : string.Empty;
            }

            if (!string.IsNullOrEmpty(entity.CongTyChiNhanh))
            {
                var companyE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.Company) && m.Id.Equals(entity.CongTyChiNhanh)).FirstOrDefault();
                entity.CongTyChiNhanhName = companyE != null ? companyE.Name : string.Empty;
            }
            if (!string.IsNullOrEmpty(entity.KhoiChucNang))
            {
                var khoichucnangE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.KhoiChucNang) && m.Id.Equals(entity.KhoiChucNang)).FirstOrDefault();
                entity.KhoiChucNangName = khoichucnangE != null ? khoichucnangE.Name : string.Empty;
            }
            if (!string.IsNullOrEmpty(entity.PhongBan))
            {
                var phongbanE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.PhongBan) && m.Id.Equals(entity.PhongBan)).FirstOrDefault();
                entity.PhongBanName = phongbanE != null ? phongbanE.Name : string.Empty;
            }
            if (!string.IsNullOrEmpty(entity.BoPhan))
            {
                var bophanE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.BoPhan) && m.Id.Equals(entity.BoPhan)).FirstOrDefault();
                entity.BoPhanName = bophanE != null ? bophanE.Name : string.Empty;
            }
            if (!string.IsNullOrEmpty(entity.ChucVu))
            {
                var chucVuE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.ChucVu) && m.Id.Equals(entity.ChucVu)).FirstOrDefault();
                entity.ChucVuName = chucVuE != null ? chucVuE.Name : string.Empty;
            }
            #endregion

            #region Images, each product 1 folder. (return images)
            var folder = Path.Combine(Constants.Folder.Image, Constants.Folder.Hr, entity.AliasFullName + "-" + entity.Code);
            entity.Images = Utility.ImageProfileProcess(entity.Images.ToList(), _env.WebRootPath, folder, entity.FullName, entity.Code);
            #endregion

            if (string.IsNullOrEmpty(entity.Id))
            {
                if (!CheckAccount(entity))
                {
                    return Json(new { result = false, source = "user", id = string.Empty, message = "Tên đăng nhập/ email đã có trong hệ thống." });
                }
                try
                {
                    dbContext.Employees.InsertOne(entity);
                }
                catch (Exception ex)
                {
                    result = false;
                    message = ex.Message;
                }
            }
            else
            {
                source = Constants.ActionLink.Edit;
                isEdit = true;
                try
                {
                    if (CheckUpdate(entity))
                    {
                        if (isRight)
                        {
                            var filter = Builders<Employee>.Filter.Eq(m => m.Id, entity.Id);
                            var update = Builders<Employee>.Update
                                .Set(m => m.UpdatedBy, loginId)
                                .Set(m => m.UpdatedOn, now)
                                .Set(m => m.Workplaces, entity.Workplaces)
                                .Set(m => m.IsTimeKeeper, entity.IsTimeKeeper)
                                .Set(m => m.LeaveLevelYear, entity.LeaveLevelYear)
                                .Set(m => m.LeaveDayAvailable, entity.LeaveDayAvailable)
                                .Set(m => m.UserName, entity.UserName)
                                .Set(m => m.Email, entity.Email)
                                //.Set(m => m.Password, entity.Password)
                                .Set(m => m.FullName, entity.FullName)
                                .Set(m => m.FirstName, entity.FirstName)
                                .Set(m => m.LastName, entity.LastName)
                                .Set(m => m.AliasFullName, entity.AliasFullName)
                                .Set(m => m.Birthday, entity.Birthday)
                                .Set(m => m.Bornplace, entity.Bornplace)
                                .Set(m => m.Gender, entity.Gender)
                                .Set(m => m.Joinday, entity.Joinday)
                                .Set(m => m.Official, entity.Official)
                                .Set(m => m.Contractday, entity.Contractday)
                                .Set(m => m.Leave, entity.Leave)
                                .Set(m => m.Leaveday, entity.Leaveday)
                                .Set(m => m.LeaveReason, entity.LeaveReason)
                                .Set(m => m.LeaveHandover, entity.LeaveHandover)
                                .Set(m => m.AddressResident, entity.AddressResident)
                                .Set(m => m.AddressTemporary, entity.AddressTemporary)
                                .Set(m => m.EmailPersonal, entity.EmailPersonal)
                                .Set(m => m.Intro, entity.Intro)
                                .Set(m => m.CongTyChiNhanh, entity.CongTyChiNhanh)
                                .Set(m => m.KhoiChucNang, entity.KhoiChucNang)
                                .Set(m => m.PhongBan, entity.PhongBan)
                                .Set(m => m.BoPhan, entity.BoPhan)
                                .Set(m => m.BoPhanCon, entity.BoPhanCon)
                                .Set(m => m.ChucVu, entity.ChucVu)
                                .Set(m => m.CongTyChiNhanhName, entity.CongTyChiNhanhName)
                                .Set(m => m.KhoiChucNangName, entity.KhoiChucNangName)
                                .Set(m => m.PhongBanName, entity.PhongBanName)
                                .Set(m => m.BoPhanName, entity.BoPhanName)
                                .Set(m => m.BoPhanConName, entity.BoPhanConName)
                                .Set(m => m.ChucVuName, entity.ChucVuName)
                                .Set(m => m.ManagerId, entity.ManagerId)
                                .Set(m => m.ManagerInformation, entity.ManagerInformation)
                                .Set(m => m.ManagerEmployeeId, entity.ManagerEmployeeId)
                                .Set(m => m.Tel, entity.Tel)
                                .Set(m => m.Mobiles, entity.Mobiles)
                                .Set(m => m.IsOnline, entity.IsOnline)
                                .Set(m => m.IdentityCard, entity.IdentityCard)
                                .Set(m => m.IdentityCardDate, entity.IdentityCardDate)
                                .Set(m => m.IdentityCardPlace, entity.IdentityCardPlace)
                                .Set(m => m.PassportEnable, entity.PassportEnable)
                                .Set(m => m.Passport, entity.Passport)
                                .Set(m => m.PassportType, entity.PassportType)
                                .Set(m => m.PassportCode, entity.PassportCode)
                                .Set(m => m.PassportDate, entity.PassportDate)
                                .Set(m => m.PassportExpireDate, entity.PassportExpireDate)
                                .Set(m => m.PassportPlace, entity.PassportPlace)
                                .Set(m => m.HouseHold, entity.HouseHold)
                                .Set(m => m.HouseHoldOwner, entity.HouseHoldOwner)
                                .Set(m => m.StatusMarital, entity.StatusMarital)
                                .Set(m => m.Nation, entity.Nation)
                                .Set(m => m.Religion, entity.Religion)
                                .Set(m => m.Certificates, entity.Certificates)
                                .Set(m => m.Cards, entity.Cards)
                                .Set(m => m.Contracts, entity.Contracts)
                                .Set(m => m.StorePapers, entity.StorePapers)
                                .Set(m => m.BhxhEnable, entity.BhxhEnable)
                                .Set(m => m.BhxhStart, entity.BhxhStart)
                                .Set(m => m.BhxhEnd, entity.BhxhEnd)
                                .Set(m => m.BhxhBookNo, entity.BhxhBookNo)
                                .Set(m => m.BhxhCode, entity.BhxhCode)
                                .Set(m => m.BhxhStatus, entity.BhxhStatus)
                                .Set(m => m.BhxhHospital, entity.BhxhHospital)
                                .Set(m => m.BhxhLocation, entity.BhxhLocation)
                                .Set(m => m.BhytCode, entity.BhytCode)
                                .Set(m => m.BhytStart, entity.BhytStart)
                                .Set(m => m.BhytEnd, entity.BhytEnd)
                                .Set(m => m.BhxhHistories, entity.BhxhHistories)
                                .Set(m => m.EmployeeFamilys, entity.EmployeeFamilys)
                                .Set(m => m.Contracts, entity.Contracts)
                                .Set(m => m.EmployeeEducations, entity.EmployeeEducations)
                                .Set(m => m.SalaryType, entity.SalaryType)
                                .Set(m => m.SalaryPayMethod, entity.SalaryPayMethod)
                                .Set(m => m.NgachLuongCode, entity.NgachLuongCode)
                                .Set(m => m.NgachLuongLevel, entity.NgachLuongLevel)
                                .Set(m => m.SalaryChucVuViTriCode, entity.SalaryChucVuViTriCode)
                                .Set(m => m.SaleChucVu, entity.SaleChucVu)
                                .Set(m => m.LogisticChucVu, entity.LogisticChucVu)
                                .Set(m => m.Images, entity.Images)
                                .Set(m => m.Properties, entity.Properties);

                            dbContext.Employees.UpdateOne(filter, update);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = false;
                    message = ex.Message;
                }
            }

            #region History
            var hisEntity = entity;
            hisEntity.Id = null;
            hisEntity.EmployeeId = entity.Id;
            dbContext.EmployeeHistories.InsertOne(hisEntity);
            #endregion

            var linkDomain = Constants.System.domain;
            var linkInformation = Constants.LinkHr.Human + "/" + Constants.LinkHr.Information + "/" + entity.Id;

            #region Notification
            var notificationImages = new List<Image>();
            if (entity.Avatar != null && !string.IsNullOrEmpty(entity.Avatar.FileName))
            {
                notificationImages.Add(entity.Avatar);
            }
            if (entity.Cover != null && !string.IsNullOrEmpty(entity.Cover.FileName))
            {
                notificationImages.Add(entity.Cover);
            }
            var notification = new Notification
            {
                Type = (int)ENotification.Hr,
                Title = isEdit ? Constants.Notification.UpdateHR : Constants.Notification.CreateHR,
                Content = entity.FullName,
                Link = linkInformation,
                Images = notificationImages.Count > 0 ? notificationImages : null,
                User = entity.Id,
                CreatedBy = loginE.FullName
            };
            dbContext.Notifications.InsertOne(notification);
            #endregion

            #region Activities
            var activity = new TrackingUser
            {
                UserId = loginId,
                Function = Constants.Collection.Employees,
                Action = isEdit ? Constants.Action.Edit : Constants.Action.Create,
                Value = entity.Id,
                Content = JsonConvert.SerializeObject(entity),
            };
            dbContext.TrackingUsers.InsertOne(activity);
            #endregion

            #region Flag GroupEmail
            if (!string.IsNullOrEmpty(viewModel.WelcomeEmailGroup))
            {
                var emailNewGroup = viewModel.WelcomeEmailGroup.Trim().ToUpper();
                var checkEmailNewGroup = dbContext.EmailGroups.Find(m => m.Type.Equals((int)EEmailGroup.New) && m.Name.Equals(emailNewGroup) && m.Object.Equals(entity.UserName) && m.Status.Equals(false)).FirstOrDefault();
                if (checkEmailNewGroup == null)
                {
                    dbContext.EmailGroups.InsertOne(new EmailGroup
                    {
                        Name = emailNewGroup,
                        Object = entity.UserName,
                        Type = (int)EEmailGroup.New
                    });
                }
                else
                {
                    var filterNewEmail = Builders<EmailGroup>.Filter.Eq(m => m.Id, checkEmailNewGroup.Id);
                    var updateNewEmail = Builders<EmailGroup>.Update
                        .Set(m => m.Name, emailNewGroup);
                    dbContext.EmailGroups.UpdateMany(filterNewEmail, updateNewEmail);
                }
            }
            #endregion

            if (isEdit && isOwner)
            {
                message = "Thông tin đã được gửi và cập nhật bởi bộ phận Nhân sự.";

                #region Send email to Hr
                if (1 == 1)
                {
                    var tos = new List<EmailAddress>();
                    var ccs = new List<EmailAddress>();
                    var nhansuRole = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.Role) && m.Alias.Equals(Constants.Rights.NhanSu)).FirstOrDefault();
                    var nhansuRights = dbContext.Rights.Find(m => m.RoleId.Equals(nhansuRole.Id)
                    && (m.Expired.Equals(null) || m.Expired >= DateTime.Now)).ToList();
                    var shortFields = Builders<Employee>.Projection.Include(p => p.Email).Include(p => p.FullName);
                    if (nhansuRights != null && nhansuRights.Count > 0)
                    {
                        foreach (var item in nhansuRights)
                        {
                            if (item.Action < (int)ERights.Boss)
                            {
                                // USER | CHUC VU : DO LATER
                                //var emailEntity = dbContext.Employees.Find(m => m.Id.Equals(item.User)).Project<Employee>(shortFields).FirstOrDefault();
                                //if (emailEntity != null)
                                //{
                                //    tos.Add(new EmailAddress { Name = emailEntity.FullName, Address = emailEntity.Email });
                                //}
                            }
                            else if (item.Action >= (int)ERights.Boss)
                            {
                                // ADD to CC
                            }
                        }
                    }

                    if (tos != null && tos.Count > 0)
                    {
                        var webRoot = Environment.CurrentDirectory;
                        var pathToFile = _env.WebRootPath
                                + Path.DirectorySeparatorChar.ToString()
                                + "Templates"
                                + Path.DirectorySeparatorChar.ToString()
                                + "EmailTemplate"
                                + Path.DirectorySeparatorChar.ToString()
                                + "EmployeeChangeInformation.html";
                        var subject = "Thay đổi thông tin nhân sự.";
                        var requester = "Bộ phận nhân sự.";
                        var userTitle = string.IsNullOrEmpty(entity.ChucVuName) ? entity.FullName : entity.FullName + " - " + entity.ChucVuName;
                        var fullLinkInformation = linkDomain + "/" + linkInformation;
                        var bodyBuilder = new BodyBuilder();
                        using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                        {
                            bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                        }
                        string messageBody = string.Format(bodyBuilder.HtmlBody,
                            subject,
                            requester,
                            userTitle,
                            entity.UpdatedOn.ToString("dd/MM/yyyy"),
                            fullLinkInformation,
                            linkDomain
                            );
                        var emailMessage = new EmailMessage()
                        {
                            ToAddresses = tos,
                            CCAddresses = ccs,
                            Subject = subject,
                            BodyContent = messageBody,
                            Type = "edit-information"
                        };
                        _emailSender.SendEmail(emailMessage);
                    }
                }
                #endregion
            }

            #region MAIL NOTIFICATION TO USER
            if (!string.IsNullOrEmpty(entity.Email) && Utility.IsValidEmail(entity.Email))
            {
                var settingEmailNotificationUser = settings.Where(m => m.Key.Equals("email-notification-user")).FirstOrDefault();
                if (settingEmailNotificationUser != null)
                {
                    if (settingEmailNotificationUser.Value == "true")
                    {
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
                                + "HrChangeInformation.html";
                        var subject = "Thay đổi thông tin nhân sự.";
                        var requester = entity.FullName;
                        var hrChanged = string.IsNullOrEmpty(loginE.ChucVuName) ? loginE.FullName : loginE.FullName + " - " + loginE.ChucVuName;
                        var fullLinkInformation = linkDomain + "/" + linkInformation;
                        var bodyBuilder = new BodyBuilder();
                        using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                        {
                            bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                        }
                        string messageBody = string.Format(bodyBuilder.HtmlBody,
                            subject,
                            requester,
                            hrChanged,
                            entity.UpdatedOn.ToString("dd/MM/yyyy"),
                            fullLinkInformation,
                            linkDomain
                            );
                        var emailMessage = new EmailMessage()
                        {
                            ToAddresses = tos,
                            Subject = subject,
                            BodyContent = messageBody,
                            Type = "hr-edit-information"
                        };
                        var scheduleEmail = new ScheduleEmail
                        {
                            Status = (int)EEmailStatus.Schedule,
                            To = emailMessage.ToAddresses,
                            CC = emailMessage.CCAddresses,
                            BCC = emailMessage.BCCAddresses,
                            Type = emailMessage.Type,
                            Title = emailMessage.Subject,
                            Content = emailMessage.BodyContent
                        };
                        dbContext.ScheduleEmails.InsertOne(scheduleEmail);
                    }
                }
            }
            #endregion

            #region MAIL WELCOME
            if (viewModel.IsWelcomeEmail && !entity.IsWelcomeEmail) // Layout && Db
            {
                var settingEmailWelcome = settings.Where(m => m.Key.Equals("email-tao-nhan-vien")).FirstOrDefault();
                if (settingEmailWelcome != null)
                {
                    if (settingEmailWelcome.Value == "true")
                    {
                        if (string.IsNullOrEmpty(viewModel.WelcomeEmailGroup))
                        {
                            SendMailNewUser(string.Empty, entity.UserName, viewModel.WelcomeOtherEmail, viewModel.WelcomeEmailAll);
                        }
                        else
                        {
                            SendMailNewUser(viewModel.WelcomeEmailGroup.Trim().ToUpper(), string.Empty, viewModel.WelcomeOtherEmail, viewModel.WelcomeEmailAll);
                        }
                    }
                }
            }
            #endregion

            #region MAIL LEAVE
            if (entity.Leave && !entity.IsLeaveEmail && viewModel.IsLeaveEmail)
            {
                #region Group Leave
                if (!string.IsNullOrEmpty(viewModel.LeaveEmailGroup))
                {
                    var emailLeaveGroup = viewModel.LeaveEmailGroup.Trim().ToUpper();
                    var checkLeaveGroup = dbContext.EmailGroups.Find(m => m.Type.Equals((int)EEmailGroup.Leave) && m.Name.Equals(emailLeaveGroup) && m.Object.Equals(entity.UserName) && m.Status.Equals(false)).FirstOrDefault();
                    if (checkLeaveGroup == null)
                    {
                        dbContext.EmailGroups.InsertOne(new EmailGroup
                        {
                            Name = emailLeaveGroup,
                            Object = entity.UserName,
                            Type = (int)EEmailGroup.Leave
                        });
                    }
                    else
                    {
                        var filterLeaveEmail = Builders<EmailGroup>.Filter.Eq(m => m.Id, checkLeaveGroup.Id);
                        var updateLeaveEmail = Builders<EmailGroup>.Update
                            .Set(m => m.Name, emailLeaveGroup);
                        dbContext.EmailGroups.UpdateMany(filterLeaveEmail, updateLeaveEmail);
                    }
                }
                #endregion

                var settingEmailLeave = settings.Where(m => m.Key.Equals("email-nhan-vien-nghi")).FirstOrDefault();
                if (settingEmailLeave != null)
                {
                    if (settingEmailLeave.Value == "true")
                    {
                        if (string.IsNullOrEmpty(viewModel.LeaveEmailGroup))
                        {
                            SendMailLeaveUser(string.Empty, entity.UserName, viewModel.LeaveOtherEmail, viewModel.LeaveEmailAll);
                        }
                        else
                        {
                            SendMailLeaveUser(viewModel.LeaveEmailGroup.Trim().ToUpper(), string.Empty, viewModel.LeaveOtherEmail, viewModel.LeaveEmailAll);
                        }
                    }
                }
            }
            #endregion

            return Json(new { result, source, id = entity.Id, message });
        }

        #region Sub
        [Route(Constants.LinkHr.Human + "/" + Constants.LinkHr.List + "/" + Constants.LinkHr.Export)]
        public async Task<IActionResult> Export(string Id, string Ten, string Code, string Fg, string Nl, string Kcn, string Pb, string Bp, string Sortby)
        {
            #region Authorization
            LoginInit(Constants.Rights.HR, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            if (!(bool)ViewData[Constants.ActionViews.IsRight])
            {
                return RedirectToAction("Index", "Home");
            }
            #endregion

            #region Get Setting Value
            var settings = dbContext.Settings.Find(m => m.Enable.Equals(true)).ToList();
            bhxh = false;
            var bhxhSetting = settings.First(m => m.Key.Equals("NoBHXH"));
            if (bhxhSetting != null)
            {
                bhxh = bhxhSetting.Value == "true" ? false : true;
            }
            #endregion

            #region Dropdownlist
            var congtychinhanhs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Company)).ToList();
            var khoichucnangs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.KhoiChucNang)).ToList();
            var phongbans = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.PhongBan)).ToList();
            var bophans = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.BoPhan)).ToList();
            var chucvus = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.ChucVu)).ToList();
            var employeeDdl = await dbContext.Employees.Find(m => m.Enable.Equals(true) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
            #endregion

            string sFileName = @"hanh-chinh-nhan-su";

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = !builder.Eq(i => i.UserName, Constants.System.account) & builder.Eq(m => m.Enable, true) & builder.Eq(m => m.Leave, false);
            if (!string.IsNullOrEmpty(Ten))
            {
                filter = filter & (builder.Eq(x => x.Email, Ten.Trim()) | builder.Regex(x => x.FullName, Ten.Trim()));
            }
            if (!string.IsNullOrEmpty(Id))
            {
                filter = filter & builder.Eq(x => x.Id, Id.Trim());
            }
            if (!string.IsNullOrEmpty(Code))
            {
                filter = filter & builder.Regex(m => m.Code, Code.Trim());
            }
            if (!string.IsNullOrEmpty(Fg))
            {
                filter = filter & builder.Where(m => m.Workplaces.Any(c => c.Fingerprint == Fg.Trim()));
                // Eq("Related._id", "b125");
            }
            if (!string.IsNullOrEmpty(Nl))
            {
                filter = filter & builder.Eq(m => m.CongTyChiNhanh, Nl);
                sFileName += "-" + Nl;
                // Eq("Related._id", "b125");
            }
            if (!string.IsNullOrEmpty(Kcn))
            {
                filter = filter & builder.Eq(m => m.KhoiChucNang, Kcn);
                sFileName += "-" + Kcn;
            }
            if (!string.IsNullOrEmpty(Pb))
            {
                filter = filter & builder.Eq(m => m.PhongBan, Pb);
                sFileName += "-" + Pb;
            }
            if (!string.IsNullOrEmpty(Bp))
            {
                filter = filter & builder.Eq(m => m.BoPhan, Bp);
                sFileName += "-" + Bp;
            }
            if (bhxh)
            {
                filter = filter & builder.Eq(m => m.BhxhEnable, bhxh);
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Employee>.Sort.Ascending(m => m.KhoiChucNang).Ascending(m => m.PhongBan);
            #endregion

            var records = await dbContext.Employees.CountDocumentsAsync(filter);

            var employees = dbContext.Employees.Find(filter).Sort(sortBuilder).ToList();

            filter = filter & builder.Eq(m => m.Leave, false);
            var recordCurrent = await dbContext.Employees.CountDocumentsAsync(filter);

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");

            sFileName += DateTime.Now.ToString("ddMMyyyyhhmm") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var cellStyleBorder = workbook.CreateCellStyle();
                cellStyleBorder.BorderBottom = BorderStyle.Thin;
                cellStyleBorder.BorderLeft = BorderStyle.Thin;
                cellStyleBorder.BorderRight = BorderStyle.Thin;
                cellStyleBorder.BorderTop = BorderStyle.Thin;
                cellStyleBorder.Alignment = HorizontalAlignment.Center;
                cellStyleBorder.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleHeader = workbook.CreateCellStyle();
                cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                cellStyleHeader.FillPattern = FillPattern.SolidForeground;
                #endregion

                ISheet sheet1 = workbook.CreateSheet("Danh-sach-nhan-su");

                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("STT");
                row.CreateCell(1, CellType.String).SetCellValue("Mã");
                row.CreateCell(2, CellType.String).SetCellValue("Họ tên");
                row.CreateCell(3, CellType.String).SetCellValue("Email");
                row.CreateCell(4, CellType.String).SetCellValue("Điện thoại bàn");
                row.CreateCell(5, CellType.String).SetCellValue("Điện thoại");
                row.CreateCell(6, CellType.String).SetCellValue("Chấm công");
                row.CreateCell(7, CellType.String).SetCellValue("Mã chấm công");
                row.CreateCell(8, CellType.String).SetCellValue("Thời gian làm việc");
                row.CreateCell(9, CellType.String).SetCellValue("Mức phép năm");
                row.CreateCell(10, CellType.String).SetCellValue("Ngày sinh");
                row.CreateCell(11, CellType.String).SetCellValue("Giới tính");
                row.CreateCell(12, CellType.String).SetCellValue("Ngày vào làm");
                row.CreateCell(13, CellType.String).SetCellValue("Nguyên quán");
                row.CreateCell(14, CellType.String).SetCellValue("Thường trú");
                row.CreateCell(15, CellType.String).SetCellValue("Tạm trú");
                row.CreateCell(16, CellType.String).SetCellValue("Công ty/Chi nhánh");
                row.CreateCell(17, CellType.String).SetCellValue("Khối chức năng");
                row.CreateCell(18, CellType.String).SetCellValue("Phòng ban");
                row.CreateCell(19, CellType.String).SetCellValue("Bộ phận");
                row.CreateCell(20, CellType.String).SetCellValue("Bộ phận con");
                row.CreateCell(21, CellType.String).SetCellValue("Chức vụ");
                row.CreateCell(22, CellType.String).SetCellValue("CMND");
                row.CreateCell(23, CellType.String).SetCellValue("Ngày cấp");
                row.CreateCell(24, CellType.String).SetCellValue("Nơi cấp");
                row.CreateCell(25, CellType.String).SetCellValue("Số Hộ khẩu");
                row.CreateCell(26, CellType.String).SetCellValue("Chủ hộ");
                row.CreateCell(27, CellType.String).SetCellValue("Dân tộc");
                row.CreateCell(28, CellType.String).SetCellValue("Tôn giáo");
                row.CreateCell(29, CellType.String).SetCellValue("Số xổ BHXH");
                row.CreateCell(30, CellType.String).SetCellValue("Mã số BHXH");
                row.CreateCell(31, CellType.String).SetCellValue("Nơi KCB ban đầu");
                row.CreateCell(32, CellType.String).SetCellValue("Cơ quan BHXH");
                row.CreateCell(33, CellType.String).SetCellValue("Mã số BHYT");
                row.CreateCell(34, CellType.String).SetCellValue("Người quản lý");
                row.CreateCell(35, CellType.String).SetCellValue("Trình độ");
                row.CreateCell(36, CellType.String).SetCellValue("Ngày nghỉ việc(nếu có)");
                // Set style

                for (int i = 0; i <= 31; i++)
                {
                    row.Cells[i].CellStyle = cellStyleHeader;
                }
                rowIndex++;

                foreach (var data in employees)
                {
                    row = sheet1.CreateRow(rowIndex);
                    row.CreateCell(0, CellType.Numeric).SetCellValue(rowIndex);
                    row.CreateCell(1, CellType.String).SetCellValue(data.CodeOld + " (" + data.Code + ")");
                    row.CreateCell(2, CellType.String).SetCellValue(data.FullName);
                    row.CreateCell(3, CellType.String).SetCellValue(data.Email);
                    row.CreateCell(4, CellType.String).SetCellValue(data.Tel);
                    var mobiles = string.Empty;
                    if (data.Mobiles != null && data.Mobiles.Count > 0)
                    {
                        foreach (var mobile in data.Mobiles)
                        {
                            if (!string.IsNullOrEmpty(mobiles))
                            {
                                mobiles += " - ";
                            }
                            mobiles += mobile.Number;
                        }
                    }
                    row.CreateCell(5, CellType.String).SetCellValue(mobiles);

                    var workplaces = string.Empty;
                    var chamcongs = string.Empty;
                    var thoigianlamviec = string.Empty;
                    if (data.Workplaces != null && data.Workplaces.Count > 0)
                    {
                        foreach (var workplace in data.Workplaces)
                        {
                            if (!string.IsNullOrEmpty(workplace.Name))
                            {
                                if (!string.IsNullOrEmpty(workplaces))
                                {
                                    workplaces += " - ";
                                }
                                workplaces += workplace.Name;
                            }
                            if (!string.IsNullOrEmpty(workplace.Fingerprint))
                            {
                                if (!string.IsNullOrEmpty(chamcongs))
                                {
                                    chamcongs += " - ";
                                }
                                chamcongs += workplace.Fingerprint;
                            }
                            if (!string.IsNullOrEmpty(workplace.WorkingScheduleTime))
                            {
                                if (!string.IsNullOrEmpty(thoigianlamviec))
                                {
                                    thoigianlamviec += " - ";
                                }
                                thoigianlamviec += workplace.WorkingScheduleTime;
                            }
                        }
                    }
                    row.CreateCell(6, CellType.String).SetCellValue(data.IsTimeKeeper ? "Không" : "Có");
                    row.CreateCell(7, CellType.String).SetCellValue(chamcongs);
                    row.CreateCell(8, CellType.String).SetCellValue(thoigianlamviec);
                    row.CreateCell(9, CellType.String).SetCellValue(data.LeaveLevelYear.ToString());
                    row.CreateCell(10, CellType.String).SetCellValue(data.Birthday.ToString("dd/MM/yyyy"));
                    row.CreateCell(11, CellType.String).SetCellValue(data.Gender);
                    row.CreateCell(12, CellType.String).SetCellValue(data.Joinday.ToString("dd/MM/yyyy"));
                    row.CreateCell(13, CellType.String).SetCellValue(data.Bornplace);
                    row.CreateCell(14, CellType.String).SetCellValue(data.AddressResident);
                    row.CreateCell(15, CellType.String).SetCellValue(data.AddressTemporary);
                    row.CreateCell(16, CellType.String).SetCellValue(data.CongTyChiNhanhName);
                    row.CreateCell(17, CellType.String).SetCellValue(data.KhoiChucNangName);
                    row.CreateCell(18, CellType.String).SetCellValue(data.PhongBanName);
                    row.CreateCell(19, CellType.String).SetCellValue(data.BoPhanName);
                    row.CreateCell(20, CellType.String).SetCellValue(data.BoPhanConName);
                    row.CreateCell(21, CellType.String).SetCellValue(data.ChucVuName);
                    row.CreateCell(22, CellType.String).SetCellValue(data.IdentityCard);
                    row.CreateCell(23, CellType.String).SetCellValue(data.IdentityCardDate.HasValue ? data.IdentityCardDate.Value.ToString("dd/MM/yyyy") : string.Empty);
                    row.CreateCell(24, CellType.String).SetCellValue(data.IdentityCardPlace);
                    row.CreateCell(25, CellType.String).SetCellValue(data.HouseHold);
                    row.CreateCell(26, CellType.String).SetCellValue(data.HouseHoldOwner);
                    row.CreateCell(27, CellType.String).SetCellValue(data.Nation);
                    row.CreateCell(28, CellType.String).SetCellValue(data.Religion);
                    row.CreateCell(29, CellType.String).SetCellValue(data.BhxhBookNo);
                    row.CreateCell(30, CellType.String).SetCellValue(data.BhxhCode);
                    row.CreateCell(31, CellType.String).SetCellValue(data.BhxhHospital);
                    row.CreateCell(32, CellType.String).SetCellValue(data.BhxhLocation);
                    row.CreateCell(33, CellType.String).SetCellValue(data.BhytCode);
                    var manage = string.Empty;
                    if (!string.IsNullOrEmpty(data.ManagerId))
                    {
                        var managerEntity = dbContext.Employees.Find(m => m.Id.Equals(data.ManagerId)).FirstOrDefault();
                        if (managerEntity != null)
                        {
                            manage = managerEntity.FullName;
                        }
                    }
                    row.CreateCell(34, CellType.String).SetCellValue(manage);
                    var trinhdo = string.Empty;
                    if (data.Certificates != null && data.Certificates.Count > 0)
                    {
                        foreach (var item in data.Certificates)
                        {
                            if (!string.IsNullOrEmpty(item.Type))
                            {
                                trinhdo += "Học vấn: " + item.Type;
                            }
                            if (!string.IsNullOrEmpty(item.Location))
                            {
                                trinhdo += " - Nơi cấp: " + item.Location;
                            }
                            if (!string.IsNullOrEmpty(item.Type))
                            {
                                trinhdo += ";";
                            }
                        }
                    }
                    row.CreateCell(35, CellType.String).SetCellValue(trinhdo);
                    row.CreateCell(36, CellType.String).SetCellValue(data.Leaveday.HasValue ? data.Leaveday.Value.ToString("dd/MM/yyyy") : string.Empty);
                    rowIndex++;
                }

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                stream.CopyTo(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkHr.Birthday + "/" + Constants.LinkHr.List)]
        public async Task<IActionResult> Birthday()
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

            //if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
            //{
            //    return RedirectToAction("AccessDenied", "Account");
            //}

            #endregion

            var birthdays = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.Birthday > Constants.MinDate).ToEnumerable()
                .OrderBy(m => m.RemainingBirthDays).ToList();

            var viewModel = new BirthdayViewModel()
            {
                Employees = birthdays
            };
            return View(viewModel);
        }

        [Route(Constants.LinkHr.ChildrenReport)]
        public async Task<ActionResult> ChildrenReport()
        {
            // update true data
            var employees = dbContext.Employees.Find(m => true).ToList();
            foreach (var employee in employees)
            {
                if (employee.EmployeeFamilys != null)
                {
                    if (employee.EmployeeFamilys.Count > 0)
                    {
                        for (int i = employee.EmployeeFamilys.Count - 1; i >= 0; i--)
                        {
                            if (employee.EmployeeFamilys[i].Birthday.HasValue && employee.EmployeeFamilys[i].Birthday < Constants.MinDate.AddYears(2))
                            {
                                employee.EmployeeFamilys[i].Birthday = null;
                            }
                            if (string.IsNullOrEmpty(employee.EmployeeFamilys[i].FullName) || employee.EmployeeFamilys[i].FullName.Length < 2)
                            {
                                employee.EmployeeFamilys.RemoveAt(i);
                            }
                        }
                    }
                    employee.EmployeeFamilys = employee.EmployeeFamilys.Count == 0 ? null : employee.EmployeeFamilys;

                    var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                    var update = Builders<Employee>.Update
                        .Set(m => m.EmployeeFamilys, employee.EmployeeFamilys);
                    dbContext.Employees.UpdateOne(filterUpdate, update);
                }
            }

            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq("EmployeeFamilys.Relation", 3);

            //var projection = Builders<Employee>.Projection.Include("EmployeeFamilys.$");
            //var result = await dbContext.Employees.Find(filter).Project(projection).ToListAsync();
            var result = await dbContext.Employees.Find(filter).ToListAsync();

            return View(result);
        }

        [Route(Constants.LinkHr.ChildrenReport + "/" + Constants.LinkHr.Export)]
        public async Task<IActionResult> ChildrenReportExport(string fileName)
        {
            var congtychinhanhs = dbContext.CongTyChiNhanhs.Find(m => m.Enable.Equals(true)).ToList();
            var khoichucnangs = dbContext.KhoiChucNangs.Find(m => m.Enable.Equals(true)).ToList();
            var phongbans = dbContext.PhongBans.Find(m => m.Enable.Equals(true)).ToList();
            var bophans = dbContext.BoPhans.Find(m => m.Enable.Equals(true) && string.IsNullOrEmpty(m.Parent)).ToList();
            var chucvus = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).ToList();

            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(i => i.UserName, Constants.System.account);
            filter = filter & builder.Eq("EmployeeFamilys.Relation", 3);
            var employees = await dbContext.Employees.Find(filter).ToListAsync();
            var results = new List<EmployeeDisplay>();
            foreach (var item in employees)
            {
                try
                {
                    var congtychinhanhName = string.Empty;
                    var khoichucnangName = string.Empty;
                    var phongbanName = string.Empty;
                    var bophanName = string.Empty;
                    var bophanConName = string.Empty;
                    var chucvuName = string.Empty;

                    if (!string.IsNullOrEmpty(item.CongTyChiNhanh))
                    {
                        var ctcnE = congtychinhanhs.Where(m => m.Id.Equals(item.CongTyChiNhanh)).FirstOrDefault();
                        if (ctcnE != null)
                        {
                            congtychinhanhName = ctcnE.Name;
                        }
                    }
                    if (!string.IsNullOrEmpty(item.KhoiChucNang))
                    {
                        var kcnE = khoichucnangs.Where(m => m.Id.Equals(item.KhoiChucNang)).FirstOrDefault();
                        if (kcnE != null)
                        {
                            khoichucnangName = kcnE.Name;
                        }
                    }
                    if (!string.IsNullOrEmpty(item.PhongBan))
                    {
                        var pbE = phongbans.Where(m => m.Id.Equals(item.PhongBan)).FirstOrDefault();
                        if (pbE != null)
                        {
                            phongbanName = pbE.Name;
                        }
                    }
                    if (!string.IsNullOrEmpty(item.BoPhan))
                    {
                        var bpE = bophans.Where(m => m.Id.Equals(item.BoPhan)).FirstOrDefault();
                        if (bpE != null)
                        {
                            bophanName = bpE.Name;
                        }
                    }
                    if (!string.IsNullOrEmpty(item.BoPhanCon))
                    {
                        var bpcE = bophans.Where(m => m.Id.Equals(item.BoPhanCon)).FirstOrDefault();
                        if (bpcE != null)
                        {
                            bophanConName = bpcE.Name;
                        }
                    }
                    if (!string.IsNullOrEmpty(item.ChucVu))
                    {
                        var cvE = chucvus.Where(m => m.Id.Equals(item.ChucVu)).FirstOrDefault();
                        if (cvE != null)
                        {
                            chucvuName = cvE.Name;
                        }
                    }

                    var employeeDisplay = new EmployeeDisplay()
                    {
                        Employee = item,
                        CongTyChiNhanh = congtychinhanhName,
                        KhoiChucNang = khoichucnangName,
                        PhongBan = phongbanName,
                        BoPhan = bophanName,
                        BoPhanCon = bophanConName,
                        ChucVu = chucvuName
                    };
                    results.Add(employeeDisplay);
                }
                catch (Exception ex)
                {

                }
            }

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"danh-sach-con-" + DateTime.Now.ToString("ddMMyyyyhhmm") + ".xlsx";
            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var cellStyleBorder = workbook.CreateCellStyle();
                cellStyleBorder.BorderBottom = BorderStyle.Thin;
                cellStyleBorder.BorderLeft = BorderStyle.Thin;
                cellStyleBorder.BorderRight = BorderStyle.Thin;
                cellStyleBorder.BorderTop = BorderStyle.Thin;
                cellStyleBorder.Alignment = HorizontalAlignment.Center;
                cellStyleBorder.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleHeader = workbook.CreateCellStyle();
                cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                cellStyleHeader.FillPattern = FillPattern.SolidForeground;
                #endregion

                ISheet sheet1 = workbook.CreateSheet("Danh-sach-con");

                //sheet1.AddMergedRegion(new CellRangeAddress(0, 0, 0, 10));
                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("STT");
                row.CreateCell(1, CellType.String).SetCellValue("Mã");
                row.CreateCell(2, CellType.String).SetCellValue("Họ tên");
                row.CreateCell(3, CellType.String).SetCellValue("Email");
                row.CreateCell(4, CellType.String).SetCellValue("Điện thoại bàn");
                row.CreateCell(5, CellType.String).SetCellValue("Điện thoại");
                row.CreateCell(6, CellType.String).SetCellValue("Nơi công tác");
                row.CreateCell(7, CellType.String).SetCellValue("Khối chức năng");
                row.CreateCell(8, CellType.String).SetCellValue("Phòng ban");
                row.CreateCell(9, CellType.String).SetCellValue("Bộ phận");
                row.CreateCell(10, CellType.String).SetCellValue("Chức vụ");
                row.CreateCell(11, CellType.String).SetCellValue("Thông tin con");
                row.CreateCell(12, CellType.String).SetCellValue("Tổng số");
                // Set style
                for (int i = 0; i <= 11; i++)
                {
                    row.Cells[i].CellStyle = cellStyleHeader;
                }
                rowIndex++;

                foreach (var data in results)
                {
                    row = sheet1.CreateRow(rowIndex);
                    row.CreateCell(0, CellType.Numeric).SetCellValue(rowIndex);
                    row.CreateCell(1, CellType.String).SetCellValue(data.Employee.CodeOld + " (" + data.Employee.Code + ")");
                    row.CreateCell(2, CellType.String).SetCellValue(data.Employee.FullName);
                    row.CreateCell(3, CellType.String).SetCellValue(data.Employee.Email);
                    row.CreateCell(4, CellType.String).SetCellValue(data.Employee.Tel);
                    var mobiles = string.Empty;
                    if (data.Employee.Mobiles != null && data.Employee.Mobiles.Count > 0)
                    {
                        foreach (var mobile in data.Employee.Mobiles)
                        {
                            if (!string.IsNullOrEmpty(mobiles))
                            {
                                mobiles += " - ";
                            }
                            mobiles += mobile.Number;
                        }
                    }
                    row.CreateCell(5, CellType.String).SetCellValue(mobiles);
                    row.CreateCell(6, CellType.String).SetCellValue(data.CongTyChiNhanh);
                    row.CreateCell(7, CellType.String).SetCellValue(data.KhoiChucNang);
                    row.CreateCell(8, CellType.String).SetCellValue(data.PhongBan);
                    row.CreateCell(9, CellType.String).SetCellValue(data.BoPhan);
                    row.CreateCell(10, CellType.String).SetCellValue(data.ChucVu);
                    var thongtincon = string.Empty;
                    int socon = 0;
                    foreach (var children in data.Employee.EmployeeFamilys)
                    {
                        if (children.Relation == 3)
                        {
                            if (!string.IsNullOrEmpty(thongtincon))
                            {
                                thongtincon += " - ";
                            }
                            thongtincon += children.FullName;
                            if (children.Birthday.HasValue)
                            {
                                thongtincon += " (" + children.Birthday.Value.ToString("dd/MM/yyyy") + ")";
                            }
                            socon++;
                        }
                    }
                    row.CreateCell(11, CellType.String).SetCellValue(thongtincon);
                    row.CreateCell(12, CellType.Numeric).SetCellValue(socon);
                    rowIndex++;
                }
                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                stream.CopyTo(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        public string GeneralEmail(string input)
        {
            return Utility.EmailConvert(input);
        }

        public void SendMailNewUser(string group, string userName, string otherEmail, bool all)
        {
            var arrs = new List<string>();
            if (string.IsNullOrEmpty(group))
            {
                arrs.Add(userName);
            }
            else
            {
                arrs = dbContext.EmailGroups.Find(m => m.Name.Equals(group) && m.Type.Equals((int)EEmailGroup.New) && m.Status.Equals(false)).ToList().Select(s => s.Object).ToList();
                var filterEmail = Builders<EmailGroup>.Filter.Eq(m => m.Name, group) & Builders<EmailGroup>.Filter.Eq(m => m.Type, (int)EEmailGroup.New);
                var updateEmail = Builders<EmailGroup>.Update
                    .Set(m => m.Status, true);
                dbContext.EmailGroups.UpdateMany(filterEmail, updateEmail);
            }

            // 1. Get information Employee
            var list = dbContext.Employees.Find(m => arrs.Contains(m.UserName)).ToList();
            if (list != null && list.Count > 0)
            {
                // Send mail
                // 1. Notication
                //      to: Ke toan, phong ban cua nv do, phong ban lien quan (xac dinh sau)
                //      cc: nhan su
                // Or all
                // 2. Send to IT setup email,...
                var tos = new List<EmailAddress>();
                var ccs = new List<EmailAddress>();

                #region CC: HR & Boss (if All)
                var idsBoss = new List<string>();
                if (all)
                {
                    var ngachLuongBoss = Constants.NgachLuongBoss.Split(';').Select(p => p.Trim()).ToList();
                    var builderBoss = Builders<Employee>.Filter;
                    var filterBoss = builderBoss.Eq(m => m.Enable, true)
                                & builderBoss.Eq(m => m.Leave, false)
                                & !builderBoss.Eq(m => m.UserName, Constants.System.account)
                                & builderBoss.In(c => c.NgachLuongCode, ngachLuongBoss)
                                & !builderBoss.Eq(m => m.Email, null)
                                & !builderBoss.Eq(m => m.Email, string.Empty);

                    var fieldBoss = Builders<Employee>.Projection.Include(p => p.Id).Include(p => p.FullName).Include(p => p.Email);
                    var boss = dbContext.Employees.Find(filterBoss).Project<Employee>(fieldBoss).ToList();
                    foreach (var item in boss)
                    {
                        ccs.Add(new EmailAddress
                        {
                            Name = item.FullName,
                            Address = item.Email
                        });
                    }
                    idsBoss = boss.Select(m => m.Id).ToList();
                }

                var hrs = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                                && !m.UserName.Equals(Constants.System.account)
                                && m.PhongBan.Equals("5c88d094d59d56225c432414")
                                && !string.IsNullOrEmpty(m.Email)).ToList();
                // get ids right nhan su
                var builderR = Builders<RoleUser>.Filter;
                var filterR = builderR.Eq(m => m.Enable, true)
                            & builderR.Eq(m => m.Role, Constants.Rights.HR)
                            & builderR.Eq(m => m.Action, Convert.ToInt32(Constants.Action.Edit))
                            & builderR.Eq(m => m.Expired, null)
                            | builderR.Gt(m => m.Expired, DateTime.Now);
                var fieldR = Builders<RoleUser>.Projection.Include(p => p.User);
                var idsR = dbContext.RoleUsers.Find(filterR).Project<RoleUser>(fieldR).ToList().Select(m => m.User).ToList();
                foreach (var hr in hrs)
                {
                    if (idsR.Contains(hr.Id))
                    {
                        ccs.Add(new EmailAddress
                        {
                            Name = hr.FullName,
                            Address = hr.Email
                        });
                    }
                }

                idsR.AddRange(idsBoss);
                #endregion

                var url = Constants.System.domain;
                var subject = "THÔNG BÁO NHÂN SỰ MỚI.";
                var pathToFile = _env.WebRootPath
                        + Path.DirectorySeparatorChar.ToString()
                        + "Templates"
                        + Path.DirectorySeparatorChar.ToString()
                        + "EmailTemplate"
                        + Path.DirectorySeparatorChar.ToString()
                        + "NhanSuMoi.html";
                var nhansumoi = string.Empty;
                var groupsPb = (from s in list
                                group s by new
                                {
                                    s.PhongBan
                                }
                                                                        into l
                                select new
                                {
                                    l.Key.PhongBan,
                                    Items = l.ToList(),
                                }).ToList();

                foreach (var groupPb in groupsPb)
                {
                    nhansumoi = string.Empty;
                    nhansumoi += "<table class='MsoNormalTable' border='0 cellspacing='0' cellpadding='0' width='738' style='width: 553.6pt; margin-left: -1.15pt; border-collapse: collapse;'>";
                    nhansumoi += "<tbody>";
                    nhansumoi += "<tr style='height: 15.75pt'>";
                    nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>STT</b></td>";
                    nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>HỌ VÀ TÊN</b></td>";
                    nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>CHỨC VỤ</b></td>";
                    nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>PHÒNG/BAN</b></td>";
                    nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>SỐ ĐT LIÊN HỆ</b></td>";
                    nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>EMAIL</b></td>";
                    nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>NGÀY NHẬN VIỆC</b></td>";
                    nhansumoi += "</tr>";
                    var i = 1;
                    foreach (var employee in groupPb.Items)
                    {
                        var contact = string.Empty;
                        if (employee.Mobiles != null && employee.Mobiles.Count > 0)
                        {
                            contact = employee.Mobiles[0].Number;
                        }
                        nhansumoi += "<tr style='height: 12.75pt'>";
                        nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-top: none; padding: 0cm 5.4pt 0cm 5.4pt;'>" + i.ToString("00") + "</td>";
                        nhansumoi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + employee.FullName.ToUpper() + "</td>";
                        nhansumoi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + employee.ChucVuName.ToUpper() + "</td>";
                        nhansumoi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + employee.PhongBanName.ToUpper() + "</td>";
                        nhansumoi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'><a href='tel:" + contact + "'>" + contact + "</a></td>";
                        nhansumoi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + employee.Email + "</td>";
                        nhansumoi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + employee.Joinday.ToString("dd/MM/yyyy") + "</td>";
                        nhansumoi += "</tr>";
                        i++;

                        #region UPDATE SENT MAIL
                        var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                        var update = Builders<Employee>.Update
                            .Set(m => m.IsWelcomeEmail, true)
                            .Set(m => m.UpdatedOn, DateTime.Now);
                        dbContext.Employees.UpdateOne(filter, update);
                        #endregion
                    }
                    nhansumoi += "</tbody>";
                    nhansumoi += "</table>";

                    tos = new List<EmailAddress>();

                    if (!all)
                    {
                        var ketoans = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                            && !string.IsNullOrEmpty(m.Email) && m.PhongBan.Equals("5c88d094d59d56225c432422") && !m.UserName.Equals(Constants.System.account)).ToList();
                        foreach (var item in ketoans)
                        {
                            tos.Add(new EmailAddress
                            {
                                Name = item.FullName,
                                Address = item.Email,
                            });
                        }

                        var relations = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                                   && !string.IsNullOrEmpty(m.Email) && m.PhongBan.Equals(groupPb) && !m.UserName.Equals(Constants.System.account)).ToList();
                        foreach (var item in relations)
                        {
                            tos.Add(new EmailAddress
                            {
                                Name = item.FullName,
                                Address = item.Email,
                            });
                        }
                    }
                    else
                    {
                        var builderAll = Builders<Employee>.Filter;
                        var filterAll = builderAll.Eq(m => m.Enable, true)
                                    & builderAll.Eq(m => m.Leave, false)
                                    & !builderAll.Eq(m => m.UserName, Constants.System.account)
                                    & !builderAll.Eq(m => m.Email, null)
                                    & !builderAll.Eq(m => m.Email, string.Empty)
                                    & !builderAll.In(c => c.Id, idsR);
                        var fieldAll = Builders<Employee>.Projection.Include(p => p.FullName).Include(p => p.Email);
                        var allEmail = dbContext.Employees.Find(filterAll).Project<Employee>(fieldAll).ToList();
                        foreach (var item in allEmail)
                        {
                            tos.Add(new EmailAddress
                            {
                                Name = item.FullName,
                                Address = item.Email,
                            });
                        }
                    }

                    if (!string.IsNullOrEmpty(otherEmail))
                    {
                        foreach (var other in otherEmail.Split(";"))
                        {
                            tos.Add(new EmailAddress
                            {
                                Address = other
                            });
                        }
                    }

                    var builder = new BodyBuilder();
                    using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                    {
                        builder.HtmlBody = SourceReader.ReadToEnd();
                    }
                    string messageBody = string.Format(builder.HtmlBody,
                        subject,
                        "tất cả thành viên",
                        nhansumoi,
                        url);

                    var emailMessage = new EmailMessage()
                    {
                        ToAddresses = tos,
                        CCAddresses = ccs,
                        Subject = subject,
                        BodyContent = messageBody,
                        Type = "nhan-su-moi"
                    };
                    var scheduleEmail = new ScheduleEmail
                    {
                        Status = (int)EEmailStatus.ScheduleASAP,
                        To = emailMessage.ToAddresses,
                        CC = emailMessage.CCAddresses,
                        BCC = emailMessage.BCCAddresses,
                        Type = emailMessage.Type,
                        Title = emailMessage.Subject,
                        Content = emailMessage.BodyContent
                    };
                    dbContext.ScheduleEmails.InsertOne(scheduleEmail);
                }
            }
        }

        public void SendMailLeaveUser(string group, string userName, string otherEmail, bool all)
        {
            var arrs = new List<string>();
            if (string.IsNullOrEmpty(group))
            {
                arrs.Add(userName);
            }
            else
            {
                arrs = dbContext.EmailGroups.Find(m => m.Name.Equals(group) && m.Type.Equals((int)EEmailGroup.Leave) && m.Status.Equals(false)).ToList().Select(s => s.Object).ToList();
                var filterEmail = Builders<EmailGroup>.Filter.Eq(m => m.Name, group) & Builders<EmailGroup>.Filter.Eq(m => m.Type, (int)EEmailGroup.Leave);
                var updateEmail = Builders<EmailGroup>.Update
                    .Set(m => m.Status, true);
                dbContext.EmailGroups.UpdateMany(filterEmail, updateEmail);
            }

            // 1. Get information Employee
            var list = dbContext.Employees.Find(m => arrs.Contains(m.UserName)).ToList();
            if (list != null && list.Count > 0)
            {
                // Send mail
                // 1. Notication
                //      to: Ke toan, phong ban cua nv do, phong ban lien quan (xac dinh sau)
                //      cc: nhan su
                // 2. Send to IT setup email,...
                var tos = new List<EmailAddress>();
                var ccs = new List<EmailAddress>();

                #region CC: HR & Boss (if All)
                var idsBoss = new List<string>();
                if (all)
                {
                    var ngachLuongBoss = Constants.NgachLuongBoss.Split(';').Select(p => p.Trim()).ToList();
                    var builderBoss = Builders<Employee>.Filter;
                    var filterBoss = builderBoss.Eq(m => m.Enable, true)
                                & builderBoss.Eq(m => m.Leave, false)
                                & !builderBoss.Eq(m => m.UserName, Constants.System.account)
                                & builderBoss.In(c => c.NgachLuongCode, ngachLuongBoss)
                                & !builderBoss.Eq(m => m.Email, null)
                                & !builderBoss.Eq(m => m.Email, string.Empty);

                    var fieldBoss = Builders<Employee>.Projection.Include(p => p.Id).Include(p => p.FullName).Include(p => p.Email);
                    var boss = dbContext.Employees.Find(filterBoss).Project<Employee>(fieldBoss).ToList();
                    foreach (var item in boss)
                    {
                        ccs.Add(new EmailAddress
                        {
                            Name = item.FullName,
                            Address = item.Email
                        });
                    }
                    idsBoss = boss.Select(m => m.Id).ToList();
                }

                var hrs = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                                && !m.UserName.Equals(Constants.System.account)
                                && m.PhongBan.Equals("5c88d094d59d56225c432414")
                                && !string.IsNullOrEmpty(m.Email)).ToList();
                // get ids right nhan su
                var builderR = Builders<RoleUser>.Filter;
                var filterR = builderR.Eq(m => m.Enable, true)
                            & builderR.Eq(m => m.Role, Constants.Rights.HR)
                            & builderR.Eq(m => m.Action, Convert.ToInt32(Constants.Action.Edit))
                            & builderR.Eq(m => m.Expired, null)
                            | builderR.Gt(m => m.Expired, DateTime.Now);
                var fieldR = Builders<RoleUser>.Projection.Include(p => p.User);
                var idsR = dbContext.RoleUsers.Find(filterR).Project<RoleUser>(fieldR).ToList().Select(m => m.User).ToList();
                foreach (var hr in hrs)
                {
                    if (idsR.Contains(hr.Id))
                    {
                        ccs.Add(new EmailAddress
                        {
                            Name = hr.FullName,
                            Address = hr.Email
                        });
                    }
                }

                idsR.AddRange(idsBoss);
                #endregion

                var url = Constants.System.domain;
                var subject = "THÔNG BÁO NHÂN SỰ NGHỈ VIỆC.";
                var pathToFile = _env.WebRootPath
                        + Path.DirectorySeparatorChar.ToString()
                        + "Templates"
                        + Path.DirectorySeparatorChar.ToString()
                        + "EmailTemplate"
                        + Path.DirectorySeparatorChar.ToString()
                        + "NhanSuNghi.html";
                var nhansunghi = string.Empty;
                var noidungbangiao = string.Empty;
                var groupsPb = (from s in list
                                group s by new
                                {
                                    s.PhongBan
                                }
                                                                        into l
                                select new
                                {
                                    l.Key.PhongBan,
                                    Items = l.ToList(),
                                }).ToList();

                foreach (var groupPb in groupsPb)
                {
                    nhansunghi = string.Empty;
                    noidungbangiao = string.Empty;
                    nhansunghi += "<table class='MsoNormalTable' border='0 cellspacing='0' cellpadding='0' width='738' style='width: 553.6pt; margin-left: -1.15pt; border-collapse: collapse;'>";
                    nhansunghi += "<tbody>";
                    nhansunghi += "<tr style='height: 15.75pt'>";
                    nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>STT</b></td>";
                    nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>HỌ VÀ TÊN</b></td>";
                    nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>CHỨC VỤ</b></td>";
                    nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>PHÒNG/BAN</b></td>";
                    nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>NGÀY NGHỈ</b></td>";
                    nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>SỐ ĐT LIÊN HỆ</b></td>";
                    nhansunghi += "</tr>";
                    var i = 1;
                    foreach (var employee in groupPb.Items)
                    {
                        var contact = string.Empty;
                        if (employee.Mobiles != null && employee.Mobiles.Count > 0)
                        {
                            contact = employee.Mobiles[0].Number;
                        }
                        if (!string.IsNullOrEmpty(employee.LeaveHandover))
                        {
                            noidungbangiao = "<br><span>" + employee.LeaveHandover + "</span>";
                        }
                        nhansunghi += "<tr style='height: 12.75pt'>";
                        nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-top: none; padding: 0cm 5.4pt 0cm 5.4pt;'>" + "01" + "</td>";
                        nhansunghi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + employee.FullName.ToUpper() + "</td>";
                        nhansunghi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + employee.ChucVuName.ToUpper() + "</td>";
                        nhansunghi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + employee.PhongBanName.ToUpper() + "</td>";
                        nhansunghi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + employee.Leaveday.Value.ToString("dd/MM/yyyy") + "</td>";
                        nhansunghi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'><a href='tel:" + contact + "'>" + contact + "</a></td>";
                        nhansunghi += "</tr>";
                        i++;

                        #region UPDATE SENT MAIL
                        var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                        var update = Builders<Employee>.Update
                            .Set(m => m.IsLeaveEmail, true)
                            .Set(m => m.UpdatedOn, DateTime.Now);
                        dbContext.Employees.UpdateOne(filter, update);
                        #endregion
                    }
                    nhansunghi += "</tbody>";
                    nhansunghi += "</table>";

                    tos = new List<EmailAddress>();
                    if (!all)
                    {
                        var ketoans = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                            && !string.IsNullOrEmpty(m.Email) && m.PhongBan.Equals("5c88d094d59d56225c432422") && !m.UserName.Equals(Constants.System.account)).ToList();
                        foreach (var item in ketoans)
                        {
                            tos.Add(new EmailAddress
                            {
                                Name = item.FullName,
                                Address = item.Email,
                            });
                        }

                        var relations = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                                   && !string.IsNullOrEmpty(m.Email) && m.PhongBan.Equals(groupPb) && !m.UserName.Equals(Constants.System.account)).ToList();
                        foreach (var item in relations)
                        {
                            tos.Add(new EmailAddress
                            {
                                Name = item.FullName,
                                Address = item.Email,
                            });
                        }
                    }
                    else
                    {
                        var builderAll = Builders<Employee>.Filter;
                        var filterAll = builderAll.Eq(m => m.Enable, true)
                                    & builderAll.Eq(m => m.Leave, false)
                                    & !builderAll.Eq(m => m.UserName, Constants.System.account)
                                    & !builderAll.Eq(m => m.Email, null)
                                    & !builderAll.Eq(m => m.Email, string.Empty)
                                    & !builderAll.In(c => c.Id, idsR);
                        var fieldAll = Builders<Employee>.Projection.Include(p => p.FullName).Include(p => p.Email);
                        var allEmail = dbContext.Employees.Find(filterAll).Project<Employee>(fieldAll).ToList();
                        foreach (var item in allEmail)
                        {
                            tos.Add(new EmailAddress
                            {
                                Name = item.FullName,
                                Address = item.Email,
                            });
                        }
                    }

                    if (!string.IsNullOrEmpty(otherEmail))
                    {
                        foreach (var other in otherEmail.Split(";"))
                        {
                            tos.Add(new EmailAddress
                            {
                                Address = other
                            });
                        }
                    }

                    var builder = new BodyBuilder();
                    using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                    {
                        builder.HtmlBody = SourceReader.ReadToEnd();
                    }
                    string messageBody = string.Format(builder.HtmlBody,
                        subject,
                        "tất cả thành viên",
                        nhansunghi,
                        url,
                        noidungbangiao);

                    var emailMessage = new EmailMessage()
                    {
                        ToAddresses = tos,
                        CCAddresses = ccs,
                        Subject = subject,
                        BodyContent = messageBody,
                        Type = "nhan-su-nghi"
                    };
                    var scheduleEmail = new ScheduleEmail
                    {
                        Status = (int)EEmailStatus.ScheduleASAP,
                        To = emailMessage.ToAddresses,
                        CC = emailMessage.CCAddresses,
                        BCC = emailMessage.BCCAddresses,
                        Type = emailMessage.Type,
                        Title = emailMessage.Subject,
                        Content = emailMessage.BodyContent
                    };
                    dbContext.ScheduleEmails.InsertOne(scheduleEmail);
                }
            }
        }

        public void SendMailRegister(Employee entity)
        {
            var password = Guid.NewGuid().ToString("N").Substring(0, 12);
            var sysPassword = Helper.HashedPassword(password);

            var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, entity.Id);
            var update = Builders<Employee>.Update
                .Set(m => m.Password, sysPassword);
            dbContext.Employees.UpdateOne(filterUpdate, update);

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
                title + " " + entity.FullName,
                url,
                entity.UserName,
                password,
                entity.Email);

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                Subject = subject,
                BodyContent = messageBody,
                Type = "thong-tin-dang-nhap"
            };

            // For faster update.
            var scheduleEmail = new ScheduleEmail
            {
                Status = (int)EEmailStatus.Schedule,
                To = emailMessage.ToAddresses,
                CC = emailMessage.CCAddresses,
                BCC = emailMessage.BCCAddresses,
                Type = emailMessage.Type,
                Title = emailMessage.Subject,
                Content = emailMessage.BodyContent
            };
            dbContext.ScheduleEmails.InsertOne(scheduleEmail);
        }

        public bool CheckAccount(Employee entity)
        {
            var result = true;
            if (!string.IsNullOrEmpty(entity.Email))
            {
                result = dbContext.Employees.CountDocuments(m => m.Enable.Equals(true) && m.Email.Equals(entity.Email)) > 0 ? false : true;
            }
            if (!string.IsNullOrEmpty(entity.UserName))
            {
                result = dbContext.Employees.CountDocuments(m => m.Enable.Equals(true) && m.UserName.Equals(entity.UserName)) > 0 ? false : true;
            }
            return result;
        }

        public bool CheckUpdate(Employee entity)
        {
            var db = dbContext.Employees.Find(m => m.Id.Equals(entity.Id)).FirstOrDefault();
            if (db.UserName != entity.UserName)
            {
                if (CheckAccount(entity))
                {
                    return db.Timestamp == entity.Timestamp ? true : false;
                }
            }
            return db.Timestamp == entity.Timestamp ? true : false;
        }

        public bool CheckDisable(Employee entity)
        {
            return entity.Usage > 0 ? false : true;
        }

        public bool CheckActive(Employee entity)
        {
            return dbContext.Employees.CountDocuments(m => m.Enable.Equals(true) && m.UserName.Equals(entity.UserName)) > 0 ? false : true;
        }

        public bool CheckDelete(Employee entity)
        {
            if (entity.NoDelete)
            {
                return false;
            }
            return entity.Usage > 0 ? false : true;
        }
        #endregion

        #region SO DO CO CAU CHUC NANG
        [Route(Constants.LinkHr.CoCauChucNang)]
        public async Task<IActionResult> CoCauChucNang(string Kcn, string PbBp, string Sortby)
        {
            var linkCurrent = string.Empty;

            #region Authorization
            LoginInit(Constants.Rights.HR, (int)ERights.Add);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            bool isRight = (bool)ViewData[Constants.ActionViews.IsRight];
            if (!isRight)
            {
                return RedirectToAction("Index", "Home");
            }
            #endregion

            #region Dropdownlist
            var congtychinhanhs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Company)).ToList();
            var khoichucnangs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.KhoiChucNang)).ToList();
            var phongbans = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.PhongBan)).ToList();
            var bophans = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.BoPhan)).ToList();
            #endregion

            linkCurrent = !string.IsNullOrEmpty(linkCurrent) ? "?" + linkCurrent : linkCurrent;
            var viewModel = new EmployeeViewModel
            {
                CongTyChiNhanhs = congtychinhanhs,
                KhoiChucNangs = khoichucnangs,
                PhongBans = phongbans,
                BoPhans = bophans,
                Kcn = Kcn,
                PbBp = PbBp,
                LinkCurrent = linkCurrent
            };

            return View(viewModel);
        }
        #endregion

        #region SYSTEM
        [HttpPost]
        [AllowAnonymous]
        [Route("/sys/login-as/")]
        public async Task<IActionResult> Login(string userName)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (true)
            {
                var result = dbContext.Employees.Find(m => m.Enable.Equals(true)
                                                        && m.IsOnline.Equals(true)
                                                        && m.UserName.Equals(userName))
                                                        .FirstOrDefault();
                // Write log, perfomance...
                if (result != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim("UserName", string.IsNullOrEmpty(result.UserName) ? string.Empty : result.UserName),
                        new Claim(ClaimTypes.Name, result.Id),
                        new Claim(ClaimTypes.Email, string.IsNullOrEmpty(result.Email) ? string.Empty : result.Email),
                        new Claim("FullName", string.IsNullOrEmpty(result.FullName) ? string.Empty : result.FullName),
                        new Claim(ClaimTypes.AuthenticationMethod, "sys")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        //AllowRefresh = <bool>,
                        // Refreshing the authentication session should be allowed.

                        //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                        // The time at which the authentication ticket expires. A 
                        // value set here overrides the ExpireTimeSpan option of 
                        // CookieAuthenticationOptions set with AddCookie.

                        //IsPersistent = true,
                        // Whether the authentication session is persisted across 
                        // multiple requests. Required when setting the 
                        // ExpireTimeSpan option of CookieAuthenticationOptions 
                        // set with AddCookie. Also required when setting 
                        // ExpiresUtc.

                        //IssuedUtc = <DateTimeOffset>,
                        // The time at which the authentication ticket was issued.

                        //RedirectUri = <string>
                        // The full path or absolute URI to be used as an http 
                        // redirect response value.
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Index");
                }
            }

            return RedirectToAction("Index");
        }
        #endregion
    }
}
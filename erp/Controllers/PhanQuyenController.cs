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
using Common.Enums;

namespace erp.Controllers
{
    /// <summary>
    /// Role base chuc vu
    /// Role group do later
    /// </summary>
    [Authorize]
    [Route(Constants.LinkPhanQuyen.Main)]
    public class PhanQuyenController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public PhanQuyenController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<PhanQuyenController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        [Route(Constants.LinkPhanQuyen.Index)]
        public async Task<IActionResult> Index(string Nhanvien, string Chucvu, string Nhom, int Trang, string SapXep, string ThuTu)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.System, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            var linkCurrent = string.Empty;

            #region Dropdownlist
            var roles = dbContext.Roles.Find(m => m.Enable.Equals(true)).ToList();
            var chucvus = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).ToList();

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && !m.UserName.Equals(Constants.System.account)).ToList();
            var employeesSL = new List<EmployeeSelectList>();
            foreach (var item in employees)
            {
                var chucvuName = string.Empty;
                if (!string.IsNullOrEmpty(item.ChucVu))
                {
                    var cvE = chucvus.Where(m => m.Id.Equals(item.ChucVu)).FirstOrDefault();
                    if (cvE != null)
                    {
                        chucvuName = cvE.Name;
                    }
                }
                employeesSL.Add(new EmployeeSelectList()
                {
                    Id = item.Id,
                    FullName = item.FullName,
                    Email = item.Email,
                    ChucVu = chucvuName
                });
            }
            
            var groups = dbContext.GroupPolicies.Find(m => m.Enable.Equals(true)).ToList();
            #endregion

            #region Filter
            var builder = Builders<RoleUsage>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!string.IsNullOrEmpty(Nhanvien))
            {
                filter = filter & builder.Eq(m => m.Type, (int)ERoleControl.User) & builder.Eq(m => m.ObjectId, Nhanvien);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Nhanvien=" + Nhanvien;
            }
            if (!string.IsNullOrEmpty(Chucvu))
            {
                filter = filter & builder.Eq(m => m.Type, (int)ERoleControl.ChucVu) & builder.Eq(m => m.ObjectId, Chucvu);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Chucvu=" + Chucvu;
            }
            if (!string.IsNullOrEmpty(Nhom))
            {
                filter = filter & builder.Eq(m => m.Type, (int)ERoleControl.Group) & builder.Eq(m => m.ObjectId, Nhom);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Nhom=" + Nhom;
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<RoleUsage>.Sort.Ascending(m => m.CreatedOn);
            SapXep = string.IsNullOrEmpty(SapXep) ? "ngay" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "ten":
                    sortBuilder = ThuTu == "asc" ? Builders<RoleUsage>.Sort.Ascending(m => m.ObjectAlias) : Builders<RoleUsage>.Sort.Descending(m => m.ObjectAlias);
                    break;
                case "quyen":
                    sortBuilder = ThuTu == "asc" ? Builders<RoleUsage>.Sort.Ascending(m => m.RoleAlias) : Builders<RoleUsage>.Sort.Descending(m => m.RoleAlias);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<RoleUsage>.Sort.Ascending(m => m.CreatedOn) : Builders<RoleUsage>.Sort.Descending(m => m.CreatedOn);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            var settingPage = dbContext.Settings.Find(m => m.Type.Equals("system") && m.Key.Equals("page-size")).FirstOrDefault();
            int Size = settingPage != null ? Convert.ToInt32(settingPage.Value) : 100;
            int pages = 1;
            var records = dbContext.RoleUsages.CountDocuments(filter);

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

            var list = new List<RoleUsage>();
            if (enablePage)
            {
                list = dbContext.RoleUsages.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * Size).Limit(Size).ToList();
            }
            else
            {
                list = dbContext.RoleUsages.Find(filter).Sort(sortBuilder).ToList();
            }

            var viewModel = new RoleUsageViewModel()
            {
                Name = "Phân quyền",
                RoleUsages = list,
                Roles = roles,
                EmployeesSL = employeesSL,
                GroupPolicies = groups,
                ChucVus = chucvus,
                LinkCurrent = linkCurrent,
                ThuTu = ThuTu,
                SapXep = SapXep,
                Records = (int)records,
                PageSize = Size,
                SoTrang = pages,
                Trang = Trang
            };

            return View(viewModel);
        }

        [HttpGet]
        [Route(Constants.LinkPhanQuyen.Detail)]
        public JsonResult Item(string id)
        {
            var item = dbContext.RoleUsages.Find(m => m.Id.Equals(id)).First();
            return Json(item);
        }

        [Route(Constants.LinkPhanQuyen.Create)]
        public async Task<ActionResult> CreateAsync()
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.System, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            var linkCurrent = string.Empty;

            #region Dropdownlist
            var roles = dbContext.Roles.Find(m => m.Enable.Equals(true)).ToList();
            var chucvus = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).ToList();
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && !m.UserName.Equals(Constants.System.account)).ToList();
            var employeesSL = new List<EmployeeSelectList>();
            foreach (var item in employees)
            {
                var chucvuName = string.Empty;
                if (!string.IsNullOrEmpty(item.ChucVu))
                {
                    var cvE = chucvus.Where(m => m.Id.Equals(item.ChucVu)).FirstOrDefault();
                    if (cvE != null)
                    {
                        chucvuName = cvE.Name;
                    }
                }
                employeesSL.Add(new EmployeeSelectList()
                {
                    Id = item.Id,
                    FullName = item.FullName,
                    Email = item.Email,
                    ChucVu = chucvuName
                });
            }
            
            var groups = dbContext.GroupPolicies.Find(m => m.Enable.Equals(true)).ToList();
            #endregion

            var viewModel = new RoleUsageViewModel()
            {
                Name = "Phân quyền",
                Roles = roles,
                EmployeesSL = employeesSL,
                ChucVus= chucvus,
                GroupPolicies = groups
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkPhanQuyen.Create)]
        public ActionResult Create(RoleUsageViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.RoleUsage;
            bool result = false;
            foreach (var item in viewModel.RoleUsages)
            {
                entity.Id = null;
                entity.RoleId = item.RoleId;
                entity.RoleAlias = item.RoleAlias;
                entity.Right = item.Right;
                entity.Start = item.Start;
                entity.Expired = item.Expired;

                if (CheckExist(entity))
                {
                    dbContext.RoleUsages.InsertOne(entity);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.RoleUsers,
                        Action = Constants.Action.Create,
                        Value = entity.RoleId,
                        Content = entity.ObjectAlias + Constants.Flag + entity.RoleAlias
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    result = true;
                }
                else
                {
                    result = false;
                }
            }

            return result ? Json(new { entity, result = true, message = "Add new successfull." }) : Json(new { entity, result = false, message = entity.RoleAlias + " is exist. Try another key or contact IT." });
        }

        [HttpPost]
        [Route(Constants.LinkPhanQuyen.Edit)]
        public ActionResult Edit(RoleUsageViewModel viewModel)
        {
            var login = User.Identity.Name;
            var now = DateTime.Now;
            var entity = viewModel.RoleUsage;
            try
            {
                if (CheckUpdated(entity))
                {
                    entity.Timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                    var filter = Builders<RoleUser>.Filter.Eq(d => d.Id, entity.Id);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<RoleUser>.Update
                                    .Set(m => m.UpdatedBy, login)
                                    .Set(m => m.UpdatedOn, now);
                    dbContext.RoleUsers.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = login,
                        Function = Constants.Collection.RoleUsers,
                        Action = Constants.Action.Edit,
                        Value = entity.RoleId,
                        Content = entity.ObjectAlias + Constants.Flag + entity.RoleAlias
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Update successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.ObjectAlias + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route(Constants.LinkPhanQuyen.Disable)]
        public ActionResult Disable(RoleUsage model)
        {
            var userId = User.Identity.Name;
            var entity = model;
            try
            {
                if (CheckDisable(entity))
                {
                    var filter = Builders<RoleUsage>.Filter.Eq(d => d.Id, entity.Id);
                    var update = Builders<RoleUsage>.Update
                                    .Set(c => c.Enable, false);
                    dbContext.RoleUsages.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.RoleUsers,
                        Action = Constants.Action.Disable,
                        Value = entity.ObjectId,
                        Content = entity.ObjectAlias + Constants.Flag + entity.RoleAlias
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Disable successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.ObjectAlias + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        public bool CheckExist(RoleUsage entity)
        {
            return dbContext.RoleUsages.CountDocuments(m => m.Enable.Equals(true) && m.ObjectId.Equals(entity.ObjectId) && m.RoleId.Equals(entity.RoleId)) > 0 ? false : true;
        }

        public bool CheckUpdated(RoleUsage entity)
        {
            var db = dbContext.RoleUsages.Find(m => m.Enable.Equals(true) && m.Id.Equals(entity.Id)).First();
            return db.Timestamp == entity.Timestamp ? true : false;
        }

        public bool CheckDisable(RoleUsage entity)
        {
            return entity.Usage > 0 ? false : true;
        }

        public bool CheckActive(RoleUsage entity)
        {
            return dbContext.RoleUsages.CountDocuments(m => m.Enable.Equals(true) && m.ObjectId.Equals(entity.ObjectId) && m.RoleId.Equals(entity.RoleId)) > 0 ? false : true;
        }

        public bool CheckDelete(RoleUsage entity)
        {
            if (entity.NoDelete)
            {
                return false;
            }
            return entity.Usage > 0 ? false : true;
        }
    }
}
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
using MimeKit;
using Services;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading;
using Common.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;

namespace Controllers
{
    public class FactoryController : BaseController
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        readonly IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public FactoryController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, IEmailSender emailSender,
            ISmsSender smsSender, ILogger<FactoryController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }


        [Route(Constants.LinkFactory.Index)]
        public async Task<IActionResult> Index(string url)
        {
            #region Update DATA. Remove after clone
           
            
            #endregion

            return View();
        }

        #region VAN HANH
        [Route(Constants.LinkFactory.VanHanh)]
        public async Task<IActionResult> VanHanh(string Xm, string Ca, string Cd, string Lot, string CaLamViec, string Phieu, string Nvl, DateTime? Tu, DateTime? Den, int? Trang, int? Dong, string SapXep, string Truong)
        {
            #region Login Information
            LoginInit("van-hanh", (int)ERights.View);
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

            var linkCurrent = string.Empty;

            #region Filter
            if (!Trang.HasValue)
            {
                Trang = 1;
            }
            if (!Dong.HasValue)
            {
                Dong = 200;
            }
            if (!Tu.HasValue)
            {
                Tu = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-2);
            }
            if (!Den.HasValue)
            {
                Den = Tu.Value.AddMonths(3).AddDays(-1);
            }
            var builder = Builders<FactoryVanHanh>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!string.IsNullOrEmpty(Xm))
            {
                filter &= builder.Eq(m => m.XeCoGioiMayCode, Xm);
            }
            if (!string.IsNullOrEmpty(Ca))
            {
                filter &= builder.Eq(m => m.CaAlias, Ca);
            }
            if (!string.IsNullOrEmpty(Cd))
            {
                filter &= builder.Eq(m => m.CongDoanCode, Cd);
            }
            if (!string.IsNullOrEmpty(Lot))
            {
                filter &= builder.Regex(m => m.LOT, new BsonRegularExpression(Utility.AliasConvert(Lot.ToLower()), "i"));
            }
            if (!string.IsNullOrEmpty(CaLamViec))
            {
                filter &= builder.Eq(m => m.CaLamViecAlias, CaLamViec);
            }
            if (!string.IsNullOrEmpty(Phieu))
            {
                filter &= builder.Regex(m => m.PhieuInCa, new BsonRegularExpression(Utility.AliasConvert(Phieu.ToLower()), "i"));
            }
            if (!string.IsNullOrEmpty(Nvl))
            {
                filter &= builder.Eq(m => m.ProductAlias, Nvl);
            }
            if (Tu.HasValue)
            {
                filter &= builder.Gte(m => m.Date, Tu.Value);
            }
            if (Den.HasValue)
            {
                filter &= builder.Lte(m => m.Date, Den.Value);
            }
            #endregion

            #region Sort

            var sortBuilder = Builders<FactoryVanHanh>.Sort.Descending(m => m.Date).Descending(m => m.CreatedOn);
            #endregion

            #region Dropdownlist
            var shifts = await dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Ca)).ToListAsync();
            var shiftsubs = await dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.CaLamViec)).ToListAsync();
            var stages = await dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.CongDoan)).ToListAsync();
            var xemays = await dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.XeCoGioivsMayMoc) && !string.IsNullOrEmpty(m.Code)).ToListAsync();
            var products = await dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.NVLvsBTPvsTP)).ToListAsync();
            #endregion

            var records = await dbContext.FactoryVanHanhs.CountDocumentsAsync(filter);
            var pages = (int)Math.Ceiling(records / (double)Dong);
            if (Trang > pages)
            {
                Trang = 1;
            }

            var datas = await dbContext.FactoryVanHanhs.Find(filter).Skip((Trang - 1) * Dong).Limit(Dong).Sort(sortBuilder).ToListAsync();
            var viewModel = new VanHanhViewModel
            {
                FactoryVanHanhs = datas,
                Shifts = shifts,
                ShiftSubs = shiftsubs,
                Stages = stages,
                Vehicles = xemays,
                Products = products,
                Records = (int)records,
                Pages = pages,
                Xm = Xm,
                Cd = Cd,
                Nvl = Nvl,
                Lot = Lot,
                Phieu = Phieu,
                Ca = Ca,
                CaLamViec = CaLamViec,
                Tu = Tu,
                Den = Den,
                Trang = Trang.Value,
                Dong = Dong.Value,
                SapXep = SapXep,
                Truong = Truong
            };

            return View(viewModel);
        }

        [Route(Constants.LinkFactory.VanHanh + "/" + Constants.ActionLink.Data)]
        public async Task<IActionResult> VanHanhData(string Id)
        {
            #region Login Information
            //LoginInit("van-hanh", (int)ERights.View);
            //if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            //{
            //    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            //    return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            //}
            //if (!(bool)ViewData[Constants.ActionViews.IsRight])
            //{
            //    return RedirectToAction("Index", "Home");
            //}
            #endregion

            #region Dropdownlist
            var shifts = await dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Ca)).ToListAsync();
            var shiftsubs = await dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.CaLamViec)).ToListAsync();
            var stages = await dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.CongDoan)).ToListAsync();
            var xemays = await dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.XeCoGioivsMayMoc)).ToListAsync();
            var products = await dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.NVLvsBTPvsTP)).ToListAsync();
            // Employees: cong nhan
            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)).ToListAsync();
            #endregion

            var entity = new FactoryVanHanh();
            if (!string.IsNullOrEmpty(Id))
            {
                entity = dbContext.FactoryVanHanhs.Find(m => m.Id.Equals(Id)).FirstOrDefault();
                if (entity == null)
                {
                    entity = new FactoryVanHanh();
                }
            }
            var viewModel = new VanHanhViewModel
            {
                Shifts = shifts,
                ShiftSubs = shiftsubs,
                Stages = stages,
                Vehicles = xemays,
                Products = products,
                Employees = employees,
                Entity = entity
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkFactory.VanHanh + "/" + Constants.ActionLink.Data)]
        public async Task<IActionResult> VanHanhData(FactoryVanHanh entity)
        {
            #region Login Information
            //LoginInit("van-hanh", (int)ERights.View);
            //if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            //{
            //    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            //    return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            //}
            //if (!(bool)ViewData[Constants.ActionViews.IsRight])
            //{
            //    return RedirectToAction("Index", "Home");
            //}
            #endregion

            try
            {
                #region Data
                var now = DateTime.Now;
                entity.CreatedBy = User.Identity.Name;
                entity.ModifiedBy = User.Identity.Name;
                entity.Year = entity.Date.Year;
                entity.Month = entity.Date.Month;
                entity.Week = Utility.GetIso8601WeekOfYear(entity.Date);
                entity.Day = entity.Date.Day;

                if (!string.IsNullOrEmpty(entity.CaId))
                {
                    var caE = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Ca) && m.Id.Equals(entity.CaId)).FirstOrDefault();
                    if (caE != null)
                    {
                        entity.CaAlias = caE.Alias;
                        entity.Ca = caE.Name;
                    }
                }
                if (!string.IsNullOrEmpty(entity.CaLamViecId))
                {
                    var calamviecE = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.CaLamViec) && m.Id.Equals(entity.CaLamViecId)).FirstOrDefault();
                    if (calamviecE != null)
                    {
                        entity.CaLamViecAlias = calamviecE.Alias;
                        entity.CaLamViec = calamviecE.Name;
                    }
                }
                if (!string.IsNullOrEmpty(entity.CongDoanId))
                {
                    var congdoanE = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.CongDoan) && m.Id.Equals(entity.CongDoanId)).FirstOrDefault();
                    if (congdoanE != null)
                    {
                        entity.CongDoanCode = congdoanE.Code;
                        entity.CongDoanAlias = congdoanE.Alias;
                        entity.CongDoanName = congdoanE.Name;
                        //entity.CongDoanNoiDung = congdoanE.Content;
                    }
                }
                if (!string.IsNullOrEmpty(entity.XeCoGioiMayId))
                {
                    var xecogioimayE = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.XeCoGioivsMayMoc) && m.Id.Equals(entity.XeCoGioiMayId)).FirstOrDefault();
                    if (xecogioimayE != null)
                    {
                        entity.XeCoGioiMayCode = xecogioimayE.Code;
                        entity.XeCoGioiMayAlias = xecogioimayE.Alias;
                        entity.XeCoGioiMayName = xecogioimayE.Name;
                    }
                }
                if (!string.IsNullOrEmpty(entity.ProductId))
                {
                    var productE = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.NVLvsBTPvsTP) && m.Id.Equals(entity.ProductId)).FirstOrDefault();
                    if (productE != null)
                    {
                        entity.ProductAlias = productE.Alias;
                        entity.ProductName = productE.Name;
                    }
                }
                if (!string.IsNullOrEmpty(entity.EmployeeId))
                {
                    var employeeE = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(entity.EmployeeId)).FirstOrDefault();
                    if (employeeE != null)
                    {
                        entity.EmployeeAlias = employeeE.AliasFullName;
                        entity.Employee = employeeE.FullName;
                    }
                }

                entity.PhieuInCa = Utility.NoPhieuInCa(entity.Date, entity.XeCoGioiMayCode);

                #endregion

                if (string.IsNullOrEmpty(entity.Id))
                {
                    await dbContext.FactoryVanHanhs.InsertOneAsync(entity);
                }
                else
                {
                    var filter = Builders<FactoryVanHanh>.Filter.Eq(m => m.Id, entity.Id);
                    var update = Builders<FactoryVanHanh>.Update
                        .Set(m => m.Year, entity.Year)
                        .Set(m => m.Month, entity.Month)
                        .Set(m => m.Week, entity.Week)
                        .Set(m => m.Day, entity.Day)
                        .Set(m => m.Date, entity.Date)
                        .Set(m => m.Ca, entity.Ca)
                        .Set(m => m.CaId, entity.CaId)
                        .Set(m => m.CaAlias, entity.CaAlias)
                        .Set(m => m.CongDoanId, entity.CongDoanId)
                        .Set(m => m.CongDoanCode, entity.CongDoanCode)
                        .Set(m => m.CongDoanName, entity.CongDoanName)
                        .Set(m => m.CongDoanAlias, entity.CongDoanAlias)
                        .Set(m => m.CongDoanNoiDung, entity.CongDoanNoiDung)
                        .Set(m => m.LOT, entity.LOT)
                        .Set(m => m.XeCoGioiMayId, entity.XeCoGioiMayId)
                        .Set(m => m.XeCoGioiMayCode, entity.XeCoGioiMayCode)
                        .Set(m => m.XeCoGioiMayName, entity.XeCoGioiMayName)
                        .Set(m => m.XeCoGioiMayAlias, entity.XeCoGioiMayAlias)
                        .Set(m => m.ProductId, entity.ProductId)
                        .Set(m => m.ProductName, entity.ProductName)
                        .Set(m => m.ProductAlias, entity.ProductAlias)
                        .Set(m => m.ProductType, entity.ProductType)
                        .Set(m => m.EmployeeId, entity.EmployeeId)
                        .Set(m => m.Employee, entity.Employee)
                        .Set(m => m.EmployeeAlias, entity.EmployeeAlias)
                        .Set(m => m.CaLamViec, entity.CaLamViec)
                        .Set(m => m.CaLamViecId, entity.CaLamViecId)
                        .Set(m => m.CaLamViecAlias, entity.CaLamViecAlias)
                        .Set(m => m.Start, entity.Start)
                        .Set(m => m.End, entity.End)
                        .Set(m => m.ThoiGianBTTQ, entity.ThoiGianBTTQ)
                        .Set(m => m.ThoiGianXeHu, entity.ThoiGianXeHu)
                        .Set(m => m.ThoiGianNghi, entity.ThoiGianNghi)
                        .Set(m => m.ThoiGianCVKhac, entity.ThoiGianCVKhac)
                        .Set(m => m.ThoiGianLamViec, entity.ThoiGianLamViec)
                        .Set(m => m.SoLuongThucHien, entity.SoLuongThucHien)
                        .Set(m => m.Dau, entity.Dau)
                        .Set(m => m.Nhot10, entity.Nhot10)
                        .Set(m => m.Nhot50, entity.Nhot50)
                        .Set(m => m.Nhot90, entity.Nhot90)
                        .Set(m => m.Nhot140, entity.Nhot140)
                        .Set(m => m.NguyenNhan, entity.NguyenNhan)
                        .Set(m => m.PhieuInCa, entity.PhieuInCa)
                        .Set(m => m.ModifiedOn, entity.ModifiedOn)
                        .Set(m => m.ModifiedBy, entity.ModifiedBy);
                    dbContext.FactoryVanHanhs.UpdateOne(filter, update);
                }

                #region Activities
                string s = JsonConvert.SerializeObject(entity);
                var activity = new TrackingUser
                {
                    UserId = User.Identity.Name,
                    Function = Constants.Collection.FactoryVanHanh,
                    Action = Constants.Action.Create,
                    Value = s,
                    Content = Constants.Action.Create,
                };
                await dbContext.TrackingUsers.InsertOneAsync(activity);
                #endregion

                return Json(new { result = true, source = "data", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "data", id = string.Empty, message = ex.Message });
            }
        }

        [Route(Constants.LinkFactory.ReportVanHanh)]
        public async Task<IActionResult> ReportVanHanh(string xm, DateTime? from, DateTime? to, /*int? page, int? size,*/ string sortField, string sort)
        {
            //#region Filter
            //var builder = Builders<FactoryVanHanh>.Filter;
            //var filter = builder.Eq(m => m.Enable, true);
            //filter = filter & !builder.Eq(m => m.XeCoGioiMayAlias, null) & !builder.Eq(m => m.XeCoGioiMayAlias, string.Empty);
            //if (!String.IsNullOrEmpty(xm))
            //{
            //    filter = filter & builder.Regex(m => m.XeCoGioiMayAlias, xm);
            //}
            //DateTime date = DateTime.Now;
            //var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            //var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            //if (!from.HasValue)
            //{
            //    from = firstDayOfMonth;
            //}
            //if (!to.HasValue)
            //{
            //    to = lastDayOfMonth;
            //}
            //filter = filter & builder.Gte(m => m.Date, from.Value);
            //filter = filter & builder.Lte(m => m.Date, to.Value);
            //#endregion

            //#region Sort
            //var sortBuilder = Builders<FactoryVanHanh>.Sort.Descending(m => m.Date).Descending(m => m.UpdatedOn).Ascending(m => m.XeCoGioiMay);
            //#endregion

            //#region Selectlist
            //var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            //#endregion

            //var viewModel = new VanHanhViewModel
            //{
            //    List = await dbContext.FactoryVanHanhs.Find(filter).Sort(sortBuilder).ToListAsync(),
            //    Vehicles = vehicles,
            //    from = from,
            //    to = to
            //};

            return View();
        }

        //[HttpPost]
        //[Route(Constants.LinkFactory.VanHanh + "/" + Constants.LinkFactory.Edit)]
        //public async Task<IActionResult> EditVanHanh(FactoryVanHanh entity)
        //{
        //    #region Authorization
        //    var login = User.Identity.Name;
        //    var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
        //    bool right = Utility.IsRight(login, "vanhanh", (int)ERights.Edit);

        //    // sys account override
        //    if (loginUserName == Constants.System.account)
        //    {
        //        right = true;
        //    }

        //    if (!right)
        //    {
        //        return RedirectToAction("AccessDenied", "Account");
        //    }
        //    #endregion

        //    try
        //    {
        //        var now = DateTime.Now;
        //        entity.UpdatedBy = login;
        //        entity.UpdatedOn = now;
        //        entity.Year = entity.Date.Year;
        //        entity.Month = entity.Date.Month;
        //        entity.Week = Utility.GetIso8601WeekOfYear(entity.Date);
        //        entity.Day = entity.Date.Day;
        //        var entityProduct = dbContext.FactoryProducts.Find(m => m.Id.Equals(entity.ProductId)).FirstOrDefault();
        //        entity.XeCoGioiMay = entityProduct.Name;

        //        var builderUpdate = Builders<FactoryVanHanh>.Filter;
        //        var filterUpdate = builderUpdate.Eq(m => m.Id, Utility.AliasConvert(entity.Id));
        //        var update = Builders<FactoryVanHanh>.Update
        //            .Set(m => m.Year, entity.Year)
        //            .Set(m => m.Month, entity.Month)
        //            .Set(m => m.Week, entity.Week)
        //            .Set(m => m.Day, entity.Day)
        //            .Set(m => m.Date, entity.Date)
        //            .Set(m => m.Ca, entity.Ca)
        //            .Set(m => m.MangCongViec, entity.MangCongViec)
        //            .Set(m => m.CongDoan, entity.CongDoan)
        //            .Set(m => m.LOT, entity.LOT)
        //            .Set(m => m.XeCoGioiMay, entity.XeCoGioiMay)
        //            .Set(m => m.ProductId, entity.ProductId)
        //            .Set(m => m.NVLTP, entity.NVLTP)
        //            .Set(m => m.SLNhanCong, entity.SLNhanCong)
        //            .Set(m => m.Start, entity.Start)
        //            .Set(m => m.End, entity.End)
        //            .Set(m => m.ThoiGianBTTQ, entity.ThoiGianBTTQ)
        //            .Set(m => m.ThoiGianXeHu, entity.ThoiGianXeHu)
        //            .Set(m => m.ThoiGianNghi, entity.ThoiGianNghi)
        //            .Set(m => m.ThoiGianCVKhac, entity.ThoiGianCVKhac)
        //            .Set(m => m.ThoiGianDayMoBat, entity.ThoiGianDayMoBat)
        //            .Set(m => m.ThoiGianBocHang, entity.ThoiGianBocHang)
        //            .Set(m => m.ThoiGianLamViec, entity.ThoiGianLamViec)
        //            .Set(m => m.SoLuongThucHien, entity.SoLuongThucHien)
        //            .Set(m => m.SoLuongDongGoi, entity.SoLuongDongGoi)
        //            .Set(m => m.SoLuongBocHang, entity.SoLuongBocHang)
        //            .Set(m => m.Dau, entity.Dau)
        //            .Set(m => m.Nhot10, entity.Nhot10)
        //            .Set(m => m.Nhot50, entity.Nhot50)
        //            .Set(m => m.NguyenNhan, entity.NguyenNhan)
        //            .Set(m => m.TongThoiGianBocHang, entity.TongThoiGianBocHang)
        //            .Set(m => m.TongThoiGianDongGoi, entity.TongThoiGianDongGoi)
        //            .Set(m => m.TongThoiGianCVKhac, entity.TongThoiGianCVKhac)
        //            .Set(m => m.TongThoiGianDayMoBat, entity.TongThoiGianDayMoBat);
        //         await dbContext.FactoryVanHanhs.UpdateOneAsync(filterUpdate, update);

        //        #region Activities
        //        string s = JsonConvert.SerializeObject(entity);
        //        var activity = new TrackingUser
        //        {
        //            UserId = login,
        //            Function = Constants.Collection.FactoryVanHanh,
        //            Action = Constants.Action.Edit,
        //            Value = s,
        //            Content = Constants.Action.Edit,
        //        };
        //        dbContext.TrackingUsers.InsertOne(activity);
        //        #endregion

        //        return Json(new { result = true, source = "edit", message = "Thành công" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { result = false, source = "edit", id = string.Empty, message = ex.Message });
        //    }
        //}

        [Route(Constants.LinkFactory.XCG + "/" + Constants.ActionLink.Report)]
        public async Task<IActionResult> ReportXCG(string xm, string cv, string cd, DateTime? from, DateTime? to, /*int? page, int? size,*/ string sortField, string sort)
        {
            //#region Filter
            //var builder = Builders<FactoryVanHanh>.Filter;
            //var filter = builder.Eq(m => m.Enable, true);
            //filter = filter & !builder.Eq(m => m.XeCoGioiMayAlias, null) & !builder.Eq(m => m.XeCoGioiMayAlias, string.Empty);
            //if (!String.IsNullOrEmpty(cv))
            //{
            //    filter = filter & builder.Regex(m => m.MangCongViecAlias, cv);
            //}
            //if (!String.IsNullOrEmpty(cd))
            //{
            //    filter = filter & builder.Regex(m => m.CongDoanAlias, cd);
            //}
            //if (!String.IsNullOrEmpty(xm))
            //{
            //    filter = filter & builder.Regex(m => m.XeCoGioiMayAlias, xm);
            //}

            //DateTime date = DateTime.Now;
            //var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            //var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            //if (!from.HasValue)
            //{
            //    from = firstDayOfMonth;
            //}
            //if (!to.HasValue)
            //{
            //    to = lastDayOfMonth;
            //}
            //filter = filter & builder.Gte(m => m.Date, from.Value);
            //filter = filter & builder.Lte(m => m.Date, to.Value);
            //#endregion

            //#region Sort
            //var sortBuilder = Builders<FactoryVanHanh>.Sort.Descending(m => m.Date).Descending(m => m.UpdatedOn).Ascending(m=>m.XeCoGioiMay);
            //#endregion

            //#region Selectlist
            //var works = await dbContext.FactoryWorks.Find(m => m.Enable.Equals(true)).ToListAsync();
            //var stages = await dbContext.FactoryStages.Find(m => m.Enable.Equals(true)).ToListAsync();
            //var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            //#endregion

            //var viewModel = new VanHanhViewModel
            //{
            //    List = await dbContext.FactoryVanHanhs.Find(filter).Sort(sortBuilder).ToListAsync(),
            //    Works = works,
            //    Stages = stages,
            //    Vehicles = vehicles,
            //    from = from,
            //    to = to
            //};

            return View();
        }

        [Route(Constants.LinkFactory.ReportDG)]
        public async Task<IActionResult> ReportDG(string tp, DateTime? from, DateTime? to, /*int? page, int? size,*/ string sortField, string sort)
        {
            //#region Filter
            //var builder = Builders<FactoryVanHanh>.Filter;
            //var filter = builder.Eq(m => m.Enable, true);
            //filter = filter & !builder.Eq(m => m.NVLTP, null) & !builder.Eq(m => m.NVLTP, string.Empty);
            //if (!String.IsNullOrEmpty(tp))
            //{
            //    filter = filter & builder.Regex(m => m.NVLTPAlias, tp);
            //}
            //DateTime date = DateTime.Now;
            //var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            //var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            //if (!from.HasValue)
            //{
            //    from = firstDayOfMonth;
            //}
            //if (!to.HasValue)
            //{
            //    to = lastDayOfMonth;
            //}
            //filter = filter & builder.Gte(m => m.Date, from.Value);
            //filter = filter & builder.Lte(m => m.Date, to.Value);
            //#endregion

            //#region Sort
            //var sortBuilder = Builders<FactoryVanHanh>.Sort.Descending(m => m.Date).Descending(m => m.UpdatedOn).Ascending(m => m.NVLTP);
            //#endregion

            //#region Selectlist
            //var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            //#endregion

            //var viewModel = new VanHanhViewModel
            //{
            //    List = await dbContext.FactoryVanHanhs.Find(filter).Sort(sortBuilder).ToListAsync(),
            //    Products = products,
            //    from = from,
            //    to = to
            //};

            //return View(viewModel);
            return View();
        }

        [Route(Constants.LinkFactory.ReportBH)]
        public async Task<IActionResult> ReportBH(string tp, DateTime? from, DateTime? to, /*int? page, int? size,*/ string sortField, string sort)
        {
            //#region Filter
            //var builder = Builders<FactoryVanHanh>.Filter;
            //var filter = builder.Eq(m => m.Enable, true);
            //filter = filter & !builder.Eq(m => m.NVLTP, null) & !builder.Eq(m => m.NVLTP, string.Empty);
            //if (!String.IsNullOrEmpty(tp))
            //{
            //    filter = filter & builder.Regex(m => m.NVLTPAlias, tp);
            //}
            //DateTime date = DateTime.Now;
            //var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            //var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            //if (!from.HasValue)
            //{
            //    from = firstDayOfMonth;
            //}
            //if (!to.HasValue)
            //{
            //    to = lastDayOfMonth;
            //}
            //filter = filter & builder.Gte(m => m.Date, from.Value);
            //filter = filter & builder.Lte(m => m.Date, to.Value);
            //#endregion

            //#region Sort
            //var sortBuilder = Builders<FactoryVanHanh>.Sort.Descending(m => m.Date).Descending(m => m.UpdatedOn).Ascending(m => m.NVLTP);
            //#endregion

            //#region Selectlist
            //var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            //#endregion

            //var viewModel = new VanHanhViewModel
            //{
            //    List = await dbContext.FactoryVanHanhs.Find(filter).Sort(sortBuilder).ToListAsync(),
            //    Products = products,
            //    from = from,
            //    to = to
            //};

            return View();
        }

        #endregion

        #region TON SX
        [Route(Constants.LinkFactory.TonSx)]
        public async Task<IActionResult> TonSx(string nvl, string lot, DateTime? from, DateTime? to, int? page, int? size, string sortField, string sort)
        {
            #region Filter
            if (!page.HasValue)
            {
                page = 1;
            }
            if (!size.HasValue)
            {
                size = 10;
            }
            if (!from.HasValue)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-4);
            }
            if (!to.HasValue)
            {
                to = from.Value.AddMonths(1).AddDays(-1);
            }
            var builder = Builders<FactoryTonSX>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!String.IsNullOrEmpty(nvl))
            {
                filter = filter & builder.Regex(m => m.ProductAlias, nvl);
            }
            if (!String.IsNullOrEmpty(lot))
            {
                filter = filter & builder.Regex(m => m.LOT, new BsonRegularExpression(Utility.AliasConvert(lot.ToLower()), "i"));
            }
            if (from.HasValue)
            {
                filter = filter & builder.Gte(m => m.Date, from.Value);
            }
            if (to.HasValue)
            {
                filter = filter & builder.Lte(m => m.Date, to.Value);
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<FactoryTonSX>.Sort.Descending(m => m.Date).Descending(m => m.UpdatedOn).Ascending(m => m.ProductAlias);
            #endregion

            #region Selectlist
            var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var records = await dbContext.FactoryTonSXs.CountDocumentsAsync(filter);
            var pages = (int)Math.Ceiling(records / (double)size);
            if (page > pages)
            {
                page = 1;
            }

            var viewModel = new TonSxViewModel
            {
                List = await dbContext.FactoryTonSXs.Find(filter).Skip((page - 1) * size).Limit(size).Sort(sortBuilder).ToListAsync(),
                Products = products,
                Records = (int)records,
                Pages = pages,
                nvl = nvl,
                from = from,
                to = to
            };

            return View(viewModel);
        }

        [Route(Constants.LinkFactory.ReportTonSx)]
        public async Task<IActionResult> ReportTonSx(string nvl, DateTime? from, DateTime? to, /*int? page, int? size,*/ string sortField, string sort)
        {
            #region Filter
            var builder = Builders<FactoryTonSX>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!String.IsNullOrEmpty(nvl))
            {
                filter = filter & builder.Regex(m => m.ProductAlias, nvl);
            }
            DateTime date = DateTime.Now;
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            if (!from.HasValue)
            {
                from = firstDayOfMonth;
            }
            if (!to.HasValue)
            {
                to = lastDayOfMonth;
            }
            filter = filter & builder.Gte(m => m.Date, from.Value);
            filter = filter & builder.Lte(m => m.Date, to.Value);
            #endregion

            #region Sort
            var sortBuilder = Builders<FactoryTonSX>.Sort.Descending(m => m.Date).Descending(m => m.UpdatedOn).Ascending(m => m.ProductAlias);
            #endregion

            #region Selectlist
            var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new TonSxViewModel
            {
                List = await dbContext.FactoryTonSXs.Find(filter).Sort(sortBuilder).ToListAsync(),
                Products = products,
                nvl = nvl,
                from = from,
                to = to
            };

            return View(viewModel);
        }

        [Route(Constants.LinkFactory.TonSx + "/" + Constants.LinkFactory.Create)]
        public async Task<IActionResult> CreateTonSx()
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "tonsanxuat", (int)ERights.Add);

            bool roleTaiNhap = Utility.IsRight(login, "ton-san-xuat-tai-nhap", (int)ERights.Add);


            // sys account override
            if (loginUserName == Constants.System.account)
            {
                right = true;
                roleTaiNhap = true;
            }

            if (!right)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            ViewData["roleTaiNhap"] = roleTaiNhap;
            #endregion

            #region Selectlist
            var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            var units = await dbContext.Units.Find(m => m.Enable.Equals(true) && m.Type.Equals(Constants.UnitType.Factory)).ToListAsync();
            #endregion
            var viewModel = new TonSxDataViewModel
            {
                Products = products,
                Units = units
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkFactory.TonSx + "/" + Constants.LinkFactory.Create)]
        public async Task<IActionResult> CreateTonSx(FactoryTonSX entity)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "tonsanxuat", (int)ERights.Add);

            // sys account override
            if (loginUserName == Constants.System.account)
            {
                right = true;
            }

            if (!right)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            try
            {
                var now = DateTime.Now;
                entity.CreatedBy = login;
                entity.UpdatedBy = login;
                entity.CheckedBy = login;
                entity.ApprovedBy = login;
                var entityProduct = dbContext.FactoryProducts.Find(m => m.Id.Equals(entity.ProductId)).FirstOrDefault();
                entity.ProductAlias = entityProduct.Alias;
                entity.Product = entityProduct.Name;
                entity.Year = entity.Date.Year;
                entity.Month = entity.Date.Month;
                entity.Week = Utility.GetIso8601WeekOfYear(entity.Date);
                entity.Day = entity.Date.Day;

                await dbContext.FactoryTonSXs.InsertOneAsync(entity);

                // update quantity product: DO LATER
                // Move to history
                var currentProduct = dbContext.FactoryProducts.Find(m => m.Id.Equals(entity.ProductId)).FirstOrDefault();
                currentProduct.Id = null;
                dbContext.FactoryProductHistories.InsertOne(currentProduct);

                var builderUpdateQuantityProduct = Builders<FactoryProduct>.Filter;
                var filterUpdateQuantityProduct = builderUpdateQuantityProduct.Eq(m => m.Alias, Utility.AliasConvert(entity.ProductAlias));
                var updateQuantityProduct = Builders<FactoryProduct>.Update
                    .Set(m => m.Quantity, entity.TonCuoiNgay);
                dbContext.FactoryProducts.UpdateOne(filterUpdateQuantityProduct, updateQuantityProduct);

                #region Activities
                string s = JsonConvert.SerializeObject(entity);
                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.FactoryTonSx,
                    Action = Constants.Action.Create,
                    Value = s,
                    Content = Constants.Action.Create,
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion

                return Json(new { result = true, source = "create", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "create", id = string.Empty, message = ex.Message });
            }
        }

        [Route(Constants.LinkFactory.TonSx + "/" + Constants.LinkFactory.Edit)]
        public async Task<IActionResult> EditTonSx(string id)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "tonsanxuat", (int)ERights.Edit);

            // sys account override
            if (loginUserName == Constants.System.account)
            {
                right = true;
            }

            if (!right)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Selectlist
            var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            var units = await dbContext.Units.Find(m => m.Enable.Equals(true) && m.Type.Equals(Constants.UnitType.Factory)).ToListAsync();
            #endregion

            var entity = dbContext.FactoryTonSXs.Find(m => m.Id.Equals(id)).FirstOrDefault();
            var viewModel = new TonSxDataViewModel
            {
                Entity = entity,
                Products = products,
                Units = units
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkFactory.TonSx + "/" + Constants.LinkFactory.Edit)]
        public async Task<IActionResult> EditTonSx(FactoryTonSX entity)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "tonsanxuat", (int)ERights.Edit);

            // sys account override
            if (loginUserName == Constants.System.account)
            {
                right = true;
            }

            if (!right)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            try
            {
                var now = DateTime.Now;
                entity.UpdatedBy = login;
                entity.UpdatedOn = now;
                entity.Year = entity.Date.Year;
                entity.Month = entity.Date.Month;
                entity.Week = Utility.GetIso8601WeekOfYear(entity.Date);
                entity.Day = entity.Date.Day;
                var entityProduct = dbContext.FactoryProducts.Find(m => m.Id.Equals(entity.ProductId)).FirstOrDefault();
                entity.ProductAlias = entityProduct.Alias;
                entity.Product = entityProduct.Name;

                var builderUpdate = Builders<FactoryTonSX>.Filter;
                var filterUpdate = builderUpdate.Eq(m => m.Id, Utility.AliasConvert(entity.Id));
                var update = Builders<FactoryTonSX>.Update
                    .Set(m => m.Year, entity.Year)
                    .Set(m => m.Month, entity.Month)
                    .Set(m => m.Week, entity.Week)
                    .Set(m => m.Day, entity.Day)
                    .Set(m => m.Date, entity.Date)
                    .Set(m => m.ProductId, entity.ProductId)
                    .Set(m => m.Product, entity.Product)
                    .Set(m => m.ProductAlias, entity.ProductAlias)
                    .Set(m => m.Unit, entity.Unit)
                    .Set(m => m.LOT, entity.LOT)
                    .Set(m => m.TonDauNgay, entity.TonDauNgay)
                    .Set(m => m.NhapTuSanXuat, entity.NhapTuSanXuat)
                    .Set(m => m.NhapTuKho, entity.NhapTuKho)
                    .Set(m => m.XuatChoSanXuat, entity.XuatChoSanXuat)
                    .Set(m => m.XuatChoKho, entity.XuatChoKho)
                    .Set(m => m.XuatHaoHut, entity.XuatHaoHut)
                    .Set(m => m.TonCuoiNgay, entity.TonCuoiNgay);
                await dbContext.FactoryTonSXs.UpdateOneAsync(filterUpdate, update);

                // update quantity product: DO LATER
                #region Move to history
                var currentProduct = dbContext.FactoryProducts.Find(m => m.Id.Equals(entity.ProductId)).FirstOrDefault();
                currentProduct.Id = null;
                dbContext.FactoryProductHistories.InsertOne(currentProduct);
                #endregion

                var builderUpdateQuantityProduct = Builders<FactoryProduct>.Filter;
                var filterUpdateQuantityProduct = builderUpdateQuantityProduct.Eq(m => m.Alias, Utility.AliasConvert(entity.ProductAlias));
                var updateQuantityProduct = Builders<FactoryProduct>.Update
                    .Set(m => m.Quantity, entity.TonCuoiNgay);
                dbContext.FactoryProducts.UpdateOne(filterUpdateQuantityProduct, updateQuantityProduct);

                #region Activities
                string s = JsonConvert.SerializeObject(entity);
                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.FactoryTonSx,
                    Action = Constants.Action.Edit,
                    Value = s,
                    Content = Constants.Action.Edit,
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion

                return Json(new { result = true, source = "edit", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "edit", id = string.Empty, message = ex.Message });
            }
        }
        #endregion

        #region PHIEU IN CA
        [AllowAnonymous]
        [Route(Constants.LinkFactory.VanHanh + "/" + Constants.LinkFactory.PhieuInCa)]
        public async Task<IActionResult> PhieuInCa(string Phieu, string Xm, string Thang)
        {
            #region Selectlist
            var vehicles = await dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.XeCoGioivsMayMoc) && !string.IsNullOrEmpty(m.Code)).ToListAsync();
            var monthYears = Utility.DllMonths();
            #endregion

            #region Filter
            var builder = Builders<FactoryVanHanh>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!string.IsNullOrEmpty(Phieu))
            {
                filter &= builder.Regex(m => m.PhieuInCa, Phieu);
            }
            else
            {
                filter &= builder.Eq(m => m.XeCoGioiMayCode, Xm);
            }
            #endregion

            var vanhanhs = dbContext.FactoryVanHanhs.Find(filter).ToList();
            var vehicleDisplay = new CategoryDisplay();
            Xm = string.IsNullOrEmpty(Xm) ? vanhanhs.First().XeCoGioiMayCode : Xm;
            var vehicle = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.XeCoGioivsMayMoc) && m.Code.Equals(Xm)).FirstOrDefault();
            if (vehicle != null)
            {
                var nhaThau = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.NhaThau)).FirstOrDefault(); //????
                vehicleDisplay = new CategoryDisplay()
                {
                    Category = vehicle,
                    NhaThauName = nhaThau != null ? nhaThau.Name : string.Empty
                };
            }
            var viewModel = new VanHanhViewModel()
            {
                FactoryVanHanhs = vanhanhs,
                Vehicle = vehicleDisplay,
                Vehicles = vehicles,
                MonthYears = monthYears,
                Phieu = Phieu,
                Thang = Thang,
                Xm = Xm
            };
            return View(viewModel);
        }
        #endregion

        #region Danh gia XCG
        [Route(Constants.LinkFactory.DanhGiaXCG)]
        public async Task<IActionResult> DanhGiaXCG(string cd, string xm, string rate, DateTime? from, DateTime? to, /*int? page, int? size, */string sortField, string sort)
        {
            #region Filter
            var builder = Builders<FactoryDanhGiaXCG>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!String.IsNullOrEmpty(cd))
            {
                filter = filter & builder.Regex(m => m.CongViecALias, cd);
            }
            if (!String.IsNullOrEmpty(xm))
            {
                filter = filter & builder.Regex(m => m.ChungLoaiXeAlias, xm);
            }
            if (!String.IsNullOrEmpty(rate))
            {
                filter = filter & builder.Eq(m => m.XepHangXCG, rate);
            }
            //DateTime date = DateTime.Now;
            //var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            //var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            //if (!from.HasValue)
            //{
            //    from = firstDayOfMonth;
            //}
            //if (!to.HasValue)
            //{
            //    to = lastDayOfMonth;
            //}
            //filter = filter & builder.Gte(m => m.Date, from.Value);
            //filter = filter & builder.Lte(m => m.Date, to.Value);
            #endregion

            #region Sort
            var sortBuilder = Builders<FactoryDanhGiaXCG>.Sort.Descending(m => m.Week).Descending(m => m.Month).Descending(m => m.Year);
            #endregion

            #region Selectlist
            var works = await dbContext.FactoryWorks.Find(m => m.Enable.Equals(true)).ToListAsync();
            var stages = await dbContext.FactoryStages.Find(m => m.Enable.Equals(true)).ToListAsync();
            var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new DanhGiaXCGViewModel
            {
                List = await dbContext.FactoryDanhGiaXCGs.Find(filter).Sort(sortBuilder).ToListAsync(),
                Stages = stages,
                Vehicles = vehicles,
                from = from,
                to = to
            };

            return View(viewModel);
        }

        [Route(Constants.LinkFactory.BieuDoXCG)]
        public async Task<IActionResult> BieuDoXCG(string xm, string rate, DateTime? from, DateTime? to, /*int? page, int? size,*/ string sortField, string sort)
        {
            #region Filter
            var builder = Builders<FactoryDanhGiaXCG>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!String.IsNullOrEmpty(xm))
            {
                filter = filter & builder.Regex(m => m.ChungLoaiXeAlias, xm);
            }
            if (!String.IsNullOrEmpty(rate))
            {
                filter = filter & builder.Eq(m => m.XepHangXCG, rate);
            }
            //DateTime date = DateTime.Now;
            //var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            //var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            //if (!from.HasValue)
            //{
            //    from = firstDayOfMonth;
            //}
            //if (!to.HasValue)
            //{
            //    to = lastDayOfMonth;
            //}
            //filter = filter & builder.Gte(m => m.Date, from.Value);
            //filter = filter & builder.Lte(m => m.Date, to.Value);
            #endregion

            #region Sort
            var sortBuilder = Builders<FactoryDanhGiaXCG>.Sort.Descending(m => m.Week).Descending(m => m.Month).Descending(m => m.Year);
            #endregion

            #region Selectlist
            var works = await dbContext.FactoryWorks.Find(m => m.Enable.Equals(true)).ToListAsync();
            var stages = await dbContext.FactoryStages.Find(m => m.Enable.Equals(true)).ToListAsync();
            var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new DanhGiaXCGViewModel
            {
                List = await dbContext.FactoryDanhGiaXCGs.Find(filter).Sort(sortBuilder).ToListAsync(),
                Stages = stages,
                Vehicles = vehicles,
                from = from,
                to = to
            };

            return View(viewModel);
        }

        [Route(Constants.LinkFactory.DanhGiaXCG + "/" + Constants.LinkFactory.Create)]
        public async Task<IActionResult> CreateDanhGiaXCG()
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "danhgia", (int)ERights.Add);

            // sys account override
            if (loginUserName == Constants.System.account)
            {
                right = true;
            }

            if (!right)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Selectlist
            var stages = await dbContext.FactoryStages.Find(m => m.Enable.Equals(true)).ToListAsync();
            var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new DanhGiaXCGDataViewModel
            {
                Stages = stages,
                Vehicles = vehicles
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkFactory.DanhGiaXCG + "/" + Constants.LinkFactory.Create)]
        public async Task<IActionResult> CreateDanhGiaXCG(FactoryDanhGiaXCG entity)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "danhgia", (int)ERights.Add);

            // sys account override
            if (loginUserName == Constants.System.account)
            {
                right = true;
            }

            if (!right)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            try
            {
                var now = DateTime.Now;
                entity.CreatedBy = login;
                entity.UpdatedBy = login;
                entity.CheckedBy = login;
                entity.ApprovedBy = login;

                entity.Week = entity.Week;
                // Convert Week to Year/Month
                //entity.Year = entity.Date.Year;
                //entity.Month = entity.Date.Month;

                entity.ChungLoaiXeAlias = Utility.AliasConvert(entity.ChungLoaiXe);
                entity.CongViecALias = Utility.AliasConvert(entity.CongViec);

                await dbContext.FactoryDanhGiaXCGs.InsertOneAsync(entity);

                #region Activities
                string s = JsonConvert.SerializeObject(entity);
                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.FactoryDanhGia,
                    Action = Constants.Action.Create,
                    Value = s,
                    Content = Constants.Action.Create,
                };
                await dbContext.TrackingUsers.InsertOneAsync(activity);
                #endregion

                return Json(new { result = true, source = "create", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "create", id = string.Empty, message = ex.Message });
            }
        }
        #endregion

        #region Dinh Muc
        [Route(Constants.LinkFactory.DinhMucXCG)]
        public async Task<IActionResult> DinhMucXCG(string cd, string sortField, string sort)
        {
            #region Filter
            var builder = Builders<FactoryDinhMuc>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!String.IsNullOrEmpty(cd))
            {
                filter = filter & builder.Regex(m => m.Alias, cd);
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<FactoryDinhMuc>.Sort.Descending(m => m.UpdatedOn).Ascending(m => m.CongDoan);
            #endregion

            #region Selectlist
            var stages = await dbContext.FactoryStages.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new DinhMucViewModel
            {
                List = await dbContext.FactoryDinhMucs.Find(filter).Sort(sortBuilder).ToListAsync(),
                Stages = stages
            };

            return View(viewModel);
        }

        [Route(Constants.LinkFactory.DinhMucXCG + "/" + Constants.LinkFactory.Create)]
        public async Task<IActionResult> CreateDinhMucXCG()
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "dinhmuc", (int)ERights.Add);

            // sys account override
            if (loginUserName == Constants.System.account)
            {
                right = true;
            }

            if (!right)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Selectlist
            var stages = await dbContext.FactoryStages.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new DinhMucDataViewModel
            {
                Stages = stages
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkFactory.DinhMucXCG + "/" + Constants.LinkFactory.Create)]
        public async Task<IActionResult> CreateDinhMucXCG(FactoryDinhMuc entity)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "dinhmuc", (int)ERights.Add);

            // sys account override
            if (loginUserName == Constants.System.account)
            {
                right = true;
            }

            if (!right)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            try
            {
                var now = DateTime.Now;
                entity.CreatedBy = login;
                entity.UpdatedBy = login;
                entity.CheckedBy = login;
                entity.ApprovedBy = login;

                entity.Year = now.Year;
                entity.Month = now.Month;
                entity.Week = Utility.GetIso8601WeekOfYear(now.Date);
                entity.Day = now.Day;

                entity.Alias = Utility.AliasConvert(entity.CongDoan);

                await dbContext.FactoryDinhMucs.InsertOneAsync(entity);

                #region Activities
                string s = JsonConvert.SerializeObject(entity);
                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.FactoryDinhMuc,
                    Action = Constants.Action.Create,
                    Value = s,
                    Content = Constants.Action.Create,
                };
                await dbContext.TrackingUsers.InsertOneAsync(activity);
                #endregion

                return Json(new { result = true, source = "create", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "create", id = string.Empty, message = ex.Message });
            }
        }
        #endregion

        #region Chi phi XCG
        [Route(Constants.LinkFactory.ChiPhiXCG)]
        public async Task<IActionResult> ChiPhiXCG(string xcg, DateTime? from, DateTime? to, /*int? page, int? size,*/ string sortField, string sort)
        {
            #region Filter
            var builder = Builders<FactoryChiPhiXCG>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!String.IsNullOrEmpty(xcg))
            {
                filter = filter & builder.Regex(m => m.ChungLoaiXeAlias, xcg);
            }
            DateTime date = DateTime.Now;
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            if (!from.HasValue)
            {
                from = firstDayOfMonth;
            }
            if (!to.HasValue)
            {
                to = lastDayOfMonth;
            }
            filter = filter & builder.Gte(m => m.Month, from.Value.Month);
            filter = filter & builder.Lte(m => m.Month, to.Value.Month);
            #endregion

            #region Sort
            var sortBuilder = Builders<FactoryChiPhiXCG>.Sort.Descending(m => m.Month).Descending(m => m.CreatedOn);
            #endregion

            #region Selectlist
            var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new ChiPhiXCGViewModel
            {
                List = await dbContext.FactoryChiPhiXCGs.Find(filter).Sort(sortBuilder).ToListAsync(),
                Vehicles = vehicles,
                from = from,
                to = to
            };

            return View(viewModel);
        }
        #endregion 

        #region Sub data, File
        [HttpPost]
        [Route(Constants.LinkFactory.NewProduct)]
        public IActionResult NewProduct(FactoryProduct entity)
        {
            entity.Alias = Utility.AliasConvert(entity.Name);
            bool exist = dbContext.FactoryProducts.CountDocuments(m => m.Alias.Equals(entity.Alias)) > 0;
            if (!exist)
            {
                dbContext.FactoryProducts.InsertOne(entity);
                return Json(new { result = true, source = "create", entity, message = "Tạo mới thành công" });
            }
            return Json(new { result = false, source = "create", entity, message = "Sản phẩm đã có. Không thể tạo 2 dữ liệu cùng tên." });
        }

        [HttpPost]
        [Route(Constants.LinkFactory.NewUnit)]
        public IActionResult NewUnit(Unit entity)
        {
            entity.Type = Constants.UnitType.Factory;
            entity.Alias = Utility.AliasConvert(entity.Name);
            bool exist = dbContext.Units.CountDocuments(m => m.Alias.Equals(entity.Alias)) > 0;
            if (!exist)
            {
                dbContext.Units.InsertOne(entity);
                return Json(new { result = true, source = "create", entity, message = "Tạo mới thành công" });
            }
            return Json(new { result = false, source = "create", entity, message = "ĐVT đã có. Không thể tạo 2 dữ liệu cùng tên." });
        }
        #endregion
    }
}
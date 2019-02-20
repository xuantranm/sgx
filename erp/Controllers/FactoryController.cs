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

namespace erp.Controllers
{
    [Route(Constants.LinkFactory.Main)]
    public class FactoryController : Controller
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

        // Apply search,....
        [Route(Constants.LinkFactory.List)]
        public async Task<IActionResult> Index(string search, string lot, string type, int? page, int? size, string sortField, string sort)
        {
            #region Init
            //dbContext.TrainningTypes.DeleteMany(m => true);
            //dbContext.Trainnings.DeleteMany(m => true);
            //var name = "Nhạc";
            //dbContext.TrainningTypes.InsertOne(new TrainningType
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name)
            //});
            //name = "Anh Văn";
            //dbContext.TrainningTypes.InsertOne(new TrainningType
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name)
            //});
            //name = "Excel";
            //dbContext.TrainningTypes.InsertOne(new TrainningType
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name)
            //});
            //name = "Word";
            //dbContext.TrainningTypes.InsertOne(new TrainningType
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name)
            //});
            //name = "JIN JU - CHẠM ĐÁY NỖI ĐAU I COVER ( VIETNAMESE VERSION )";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Nghe thử",
            //    Type = "nhac",
            //    Link = Constants.Link.Youtube + "YHb-9oBK1CU",
            //});

            //name = "MV ĐỪNG NHƯ THÓI QUEN | JAYKII & SARA | QUAY Ở THÁI LAN (DEMO)";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Nghe thử",
            //    Type = "nhac",
            //    Link = Constants.Link.Youtube + "90Y_gWG4sZY",
            //});

            //name = "CUỘC SỐNG EM ỔN KHÔNG | ANH TÚ | OFFICIAL MUSIC VIDEO";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Nghe thử",
            //    Type = "nhac",
            //    Link = Constants.Link.Youtube + "DWYwmTdXpqw",
            //});

            //name = "Mashup TOP 20 Bài Hát Nhạc Trẻ Hits Hay Nhất 2018 || Đỗ Nguyên Phúc x Fanny";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Nghe thử",
            //    Type = "nhac",
            //    Link = Constants.Link.Youtube + "LrW7MuvejMk",
            //});

            //name = "Radioactive Imagine Dragons (ft. The Macy Kate Band & Kurt Schneider)";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Nghe thử",
            //    Type = "nhac",
            //    Link = Constants.Link.Youtube + "P6qFCqOy3HU",
            //});

            //name = "The Chainsmokers - Don't Let Me Down ( cover by J.Fla )";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Nghe thử",
            //    Type = "nhac",
            //    Link = Constants.Link.Youtube + "C3e_NZIFfrk",
            //});

            //name = "Charlie Puth & Selena Gomez - We Don't Talk Anymore";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Nghe thử",
            //    Type = "nhac",
            //    Link = Constants.Link.Youtube + "i_yLpCLMaKk",
            //});

            //name = "50 COMMON ENGLISH PHRASES";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Luyện kỹ năng nghe",
            //    Type = "anh-van",
            //    Link = Constants.Link.Youtube + "bj5btO2nvt8",
            //});
            //name = "HOW TO SPEAK ENGLISH LIKE AN AMERICAN";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Luyện kỹ năng nghe",
            //    Type = "anh-van",
            //    Link = Constants.Link.Youtube + "otUnhd8ozg8",
            //});
            //name = "I UNDERSTAND ENGLISH, BUT I CAN'T SPEAK IT - action plan";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Luyện kỹ năng nghe",
            //    Type = "anh-van",
            //    Link = Constants.Link.Youtube + "etfKPW86IBg",
            //});
            //name = "LEARN SPOKEN ENGLISH - 10 HACKS if you struggle to speak";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Luyện kỹ năng nghe",
            //    Type = "anh-van",
            //    Link = Constants.Link.Youtube + "kCuBgQgqeyA",
            //});
            //name = "5 TIPS TO SOUND LIKE A NATIVE SPEAKER";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Luyện kỹ năng nghe",
            //    Type = "anh-van",
            //    Link = Constants.Link.Youtube + "2Bc6oClJG-4",
            //});
            //name = "ACCENT REDUCTION CLASS - English language(live)";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Luyện kỹ năng nghe",
            //    Type = "anh-van",
            //    Link = Constants.Link.Youtube + "OVK_6FosK7o",
            //});
            #endregion

            if (!page.HasValue)
            {
                page = 1;
            }
            if (!size.HasValue)
            {
                size = 10;
            }

            #region Filter
            var builder = Builders<Trainning>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!String.IsNullOrEmpty(search))
            {
                filter = filter & builder.Regex(m => m.Alias, new BsonRegularExpression(Utility.AliasConvert(search.ToLower()),"i"));
            }
            if (!String.IsNullOrEmpty(type))
            {
                filter = filter & builder.Eq(m => m.Type, type);
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Trainning>.Sort.Descending(m => m.CreatedOn);
            #endregion

            #region Selectlist
            var types = new SelectList(await dbContext.TrainningTypes.Find(m=>m.Enable.Equals(true)).ToListAsync(),"Alias","Name");
            #endregion

            var records = await dbContext.Trainnings.CountDocumentsAsync(filter);
            var pages = (int)Math.Ceiling(records / (double)size);
            if (page > pages)
            {
                page = 1;
            }

            var viewModel = new TrainningViewModel
            {
                List = await dbContext.Trainnings.Find(filter).Skip((page - 1) * size).Limit(size).Sort(sortBuilder).ToListAsync(),
                Types = types,
                Records = (int)records,
                Pages = pages
            };

            return View(viewModel);
        }

        #region TONSX
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

        #region VAN HANH
        [Route(Constants.LinkFactory.VanHanh)]
        public async Task<IActionResult> VanHanh(string ca, string calamviec, string cv, string cd, string xm, string lot, string phieuinca, string nvl, DateTime? from, DateTime? to, int? page, int? size, string sortField, string sort)
        {
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
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-4); ;
            }
            if (!to.HasValue)
            {
                to = from.Value.AddMonths(1).AddDays(-1);
            }
            #region Filter
            var builder = Builders<FactoryVanHanh>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!String.IsNullOrEmpty(ca))
            {
                filter = filter & builder.Regex(m => m.CaAlias, ca);
            }
            if (!String.IsNullOrEmpty(calamviec))
            {
                filter = filter & builder.Eq(m => m.CaLamViec, calamviec);
            }
            if (!String.IsNullOrEmpty(cv))
            {
                filter = filter & builder.Regex(m => m.MangCongViecAlias, cv);
            }
            if (!String.IsNullOrEmpty(cd))
            {
                filter = filter & builder.Regex(m => m.CongDoanAlias, cd);
            }
            if (!String.IsNullOrEmpty(xm))
            {
                filter = filter & builder.Eq(m => m.XeCoGioiMayAlias, xm);
            }
            if (!String.IsNullOrEmpty(lot))
            {
                filter = filter & builder.Regex(m => m.LOT, new BsonRegularExpression(Utility.AliasConvert(lot.ToLower()), "i"));
            }
            if (!String.IsNullOrEmpty(phieuinca))
            {
                filter = filter & builder.Regex(m => m.PhieuInCa, new BsonRegularExpression(Utility.AliasConvert(phieuinca.ToLower()), "i"));
            }
            if (!String.IsNullOrEmpty(nvl))
            {
                filter = filter & builder.Regex(m => m.NVLTPAlias, nvl);
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
            var sortBuilder = Builders<FactoryVanHanh>.Sort.Descending(m => m.Date).Descending(m => m.CreatedOn);
            #endregion

            #region Selectlist
            var works = await dbContext.FactoryWorks.Find(m => m.Enable.Equals(true)).ToListAsync();
            var stages = await dbContext.FactoryStages.Find(m => m.Enable.Equals(true)).ToListAsync();
            var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var records = await dbContext.FactoryVanHanhs.CountDocumentsAsync(filter);
            var pages = (int)Math.Ceiling(records / (double)size);
            if (page > pages)
            {
                page = 1;
            }

            var viewModel = new VanHanhViewModel
            {
                List = await dbContext.FactoryVanHanhs.Find(filter).Skip((page - 1) * size).Limit(size).Sort(sortBuilder).ToListAsync(),
                Works = works,
                Stages = stages,
                Vehicles = vehicles,
                Products = products,
                Records = (int)records,
                Pages = pages,
                from = from,
                to = to
            };

            return View(viewModel);
        }

        [Route(Constants.LinkFactory.ReportXCG)]
        public async Task<IActionResult> ReportXCG(string xm, string cv, string cd, DateTime? from, DateTime? to, /*int? page, int? size,*/ string sortField, string sort)
        {
            #region Filter
            var builder = Builders<FactoryVanHanh>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(m => m.XeCoGioiMayAlias, null) & !builder.Eq(m => m.XeCoGioiMayAlias, string.Empty);
            if (!String.IsNullOrEmpty(cv))
            {
                filter = filter & builder.Regex(m => m.MangCongViecAlias, cv);
            }
            if (!String.IsNullOrEmpty(cd))
            {
                filter = filter & builder.Regex(m => m.CongDoanAlias, cd);
            }
            if (!String.IsNullOrEmpty(xm))
            {
                filter = filter & builder.Regex(m => m.XeCoGioiMayAlias, xm);
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
            var sortBuilder = Builders<FactoryVanHanh>.Sort.Descending(m => m.Date).Descending(m => m.UpdatedOn).Ascending(m=>m.XeCoGioiMay);
            #endregion

            #region Selectlist
            var works = await dbContext.FactoryWorks.Find(m => m.Enable.Equals(true)).ToListAsync();
            var stages = await dbContext.FactoryStages.Find(m => m.Enable.Equals(true)).ToListAsync();
            var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new VanHanhViewModel
            {
                List = await dbContext.FactoryVanHanhs.Find(filter).Sort(sortBuilder).ToListAsync(),
                Works = works,
                Stages = stages,
                Vehicles = vehicles,
                from = from,
                to = to
            };

            return View(viewModel);
        }

        [Route(Constants.LinkFactory.ReportDG)]
        public async Task<IActionResult> ReportDG(string tp, DateTime? from, DateTime? to, /*int? page, int? size,*/ string sortField, string sort)
        {
            #region Filter
            var builder = Builders<FactoryVanHanh>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(m => m.NVLTP, null) & !builder.Eq(m => m.NVLTP, string.Empty);
            if (!String.IsNullOrEmpty(tp))
            {
                filter = filter & builder.Regex(m => m.NVLTPAlias, tp);
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
            var sortBuilder = Builders<FactoryVanHanh>.Sort.Descending(m => m.Date).Descending(m => m.UpdatedOn).Ascending(m => m.NVLTP);
            #endregion

            #region Selectlist
            var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new VanHanhViewModel
            {
                List = await dbContext.FactoryVanHanhs.Find(filter).Sort(sortBuilder).ToListAsync(),
                Products = products,
                from = from,
                to = to
            };

            return View(viewModel);
        }

        [Route(Constants.LinkFactory.ReportBH)]
        public async Task<IActionResult> ReportBH(string tp, DateTime? from, DateTime? to, /*int? page, int? size,*/ string sortField, string sort)
        {
            #region Filter
            var builder = Builders<FactoryVanHanh>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(m => m.NVLTP, null) & !builder.Eq(m => m.NVLTP, string.Empty);
            if (!String.IsNullOrEmpty(tp))
            {
                filter = filter & builder.Regex(m => m.NVLTPAlias, tp);
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
            var sortBuilder = Builders<FactoryVanHanh>.Sort.Descending(m => m.Date).Descending(m => m.UpdatedOn).Ascending(m => m.NVLTP);
            #endregion

            #region Selectlist
            var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new VanHanhViewModel
            {
                List = await dbContext.FactoryVanHanhs.Find(filter).Sort(sortBuilder).ToListAsync(),
                Products = products,
                from = from,
                to = to
            };

            return View(viewModel);
        }

        [Route(Constants.LinkFactory.ReportVanHanh)]
        public async Task<IActionResult> ReportVanHanh(string xm, DateTime? from, DateTime? to, /*int? page, int? size,*/ string sortField, string sort)
        {
            #region Filter
            var builder = Builders<FactoryVanHanh>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(m => m.XeCoGioiMayAlias, null) & !builder.Eq(m => m.XeCoGioiMayAlias, string.Empty);
            if (!String.IsNullOrEmpty(xm))
            {
                filter = filter & builder.Regex(m => m.XeCoGioiMayAlias, xm);
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
            var sortBuilder = Builders<FactoryVanHanh>.Sort.Descending(m => m.Date).Descending(m => m.UpdatedOn).Ascending(m => m.XeCoGioiMay);
            #endregion

            #region Selectlist
            var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new VanHanhViewModel
            {
                List = await dbContext.FactoryVanHanhs.Find(filter).Sort(sortBuilder).ToListAsync(),
                Vehicles = vehicles,
                from = from,
                to = to
            };

            return View(viewModel);
        }

        [Route(Constants.LinkFactory.VanHanh + "/" + Constants.LinkFactory.Create)]
        public async Task<IActionResult> CreateVanHanh()
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "vanhanh", (int)ERights.Add);

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
            var works = await dbContext.FactoryWorks.Find(m => m.Enable.Equals(true)).ToListAsync();
            var stages = await dbContext.FactoryStages.Find(m => m.Enable.Equals(true)).ToListAsync();
            var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new VanHanhDataViewModel
            {
                Works = works,
                Stages = stages,
                Vehicles = vehicles,
                Products = products,
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkFactory.VanHanh + "/" + Constants.LinkFactory.Create)]
        public async Task<IActionResult> CreateVanHanh(FactoryVanHanh entity)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "vanhanh", (int)ERights.Add);

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
                if (!string.IsNullOrEmpty(entity.ProductId))
                {
                    var entityProduct = dbContext.FactoryProducts.Find(m => m.Id.Equals(entity.ProductId)).FirstOrDefault();
                    if (entityProduct != null)
                    {
                        entity.NVLTP = entityProduct.Name;
                        entity.NVLTPAlias = entityProduct.Alias;
                    }
                }
                entity.Year = entity.Date.Year;
                entity.Month = entity.Date.Month;
                entity.Week = Utility.GetIso8601WeekOfYear(entity.Date);
                entity.Day = entity.Date.Day;

                entity.CaAlias = Utility.AliasConvert(entity.Ca);
                entity.MangCongViecAlias = Utility.AliasConvert(entity.MangCongViec);
                entity.CongDoanAlias = Utility.AliasConvert(entity.CongDoan);
                entity.XeCoGioiMayAlias = Utility.AliasConvert(entity.XeCoGioiMay);

                entity.PhieuInCa = Utility.NoPhieuInCa(entity.Date, entity.XeCoGioiMayAlias);

                await dbContext.FactoryVanHanhs.InsertOneAsync(entity);

                #region Activities
                string s = JsonConvert.SerializeObject(entity);
                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.FactoryVanHanh,
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

        [Route(Constants.LinkFactory.VanHanh + "/" + Constants.LinkFactory.Edit)]
        public async Task<IActionResult> EditVanHanh(string id)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "vanhanh", (int)ERights.Edit);

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
            var works = await dbContext.FactoryWorks.Find(m => m.Enable.Equals(true)).ToListAsync();
            var stages = await dbContext.FactoryStages.Find(m => m.Enable.Equals(true)).ToListAsync();
            var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            var products = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var entity = dbContext.FactoryVanHanhs.Find(m => m.Id.Equals(id)).FirstOrDefault();
            var viewModel = new VanHanhDataViewModel
            {
                Entity = entity,
                Works = works,
                Stages = stages,
                Vehicles = vehicles,
                Products = products
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkFactory.VanHanh + "/" + Constants.LinkFactory.Edit)]
        public async Task<IActionResult> EditVanHanh(FactoryVanHanh entity)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "vanhanh", (int)ERights.Edit);

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
                entity.XeCoGioiMay = entityProduct.Name;

                var builderUpdate = Builders<FactoryVanHanh>.Filter;
                var filterUpdate = builderUpdate.Eq(m => m.Id, Utility.AliasConvert(entity.Id));
                var update = Builders<FactoryVanHanh>.Update
                    .Set(m => m.Year, entity.Year)
                    .Set(m => m.Month, entity.Month)
                    .Set(m => m.Week, entity.Week)
                    .Set(m => m.Day, entity.Day)
                    .Set(m => m.Date, entity.Date)
                    .Set(m => m.Ca, entity.Ca)
                    .Set(m => m.MangCongViec, entity.MangCongViec)
                    .Set(m => m.CongDoan, entity.CongDoan)
                    .Set(m => m.LOT, entity.LOT)
                    .Set(m => m.XeCoGioiMay, entity.XeCoGioiMay)
                    .Set(m => m.ProductId, entity.ProductId)
                    .Set(m => m.NVLTP, entity.NVLTP)
                    .Set(m => m.SLNhanCong, entity.SLNhanCong)
                    .Set(m => m.Start, entity.Start)
                    .Set(m => m.End, entity.End)
                    .Set(m => m.ThoiGianBTTQ, entity.ThoiGianBTTQ)
                    .Set(m => m.ThoiGianXeHu, entity.ThoiGianXeHu)
                    .Set(m => m.ThoiGianNghi, entity.ThoiGianNghi)
                    .Set(m => m.ThoiGianCVKhac, entity.ThoiGianCVKhac)
                    .Set(m => m.ThoiGianDayMoBat, entity.ThoiGianDayMoBat)
                    .Set(m => m.ThoiGianBocHang, entity.ThoiGianBocHang)
                    .Set(m => m.ThoiGianLamViec, entity.ThoiGianLamViec)
                    .Set(m => m.SoLuongThucHien, entity.SoLuongThucHien)
                    .Set(m => m.SoLuongDongGoi, entity.SoLuongDongGoi)
                    .Set(m => m.SoLuongBocHang, entity.SoLuongBocHang)
                    .Set(m => m.Dau, entity.Dau)
                    .Set(m => m.Nhot10, entity.Nhot10)
                    .Set(m => m.Nhot50, entity.Nhot50)
                    .Set(m => m.NguyenNhan, entity.NguyenNhan)
                    .Set(m => m.TongThoiGianBocHang, entity.TongThoiGianBocHang)
                    .Set(m => m.TongThoiGianDongGoi, entity.TongThoiGianDongGoi)
                    .Set(m => m.TongThoiGianCVKhac, entity.TongThoiGianCVKhac)
                    .Set(m => m.TongThoiGianDayMoBat, entity.TongThoiGianDayMoBat);
                 await dbContext.FactoryVanHanhs.UpdateOneAsync(filterUpdate, update);

                #region Activities
                string s = JsonConvert.SerializeObject(entity);
                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.FactoryVanHanh,
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
        [Route(Constants.LinkFactory.PhieuInCa)]
        public async Task<IActionResult> PhieuInCa(string phieuinca, string xe, DateTime? ngay)
        {
            #region Selectlist
            var vehicles = await dbContext.FactoryMotorVehicles.Find(m => m.Enable.Equals(true)).ToListAsync();
            #endregion

            var viewModel = new PhieuInCaViewModel
            {
                Vehicles = vehicles
            };
            if (String.IsNullOrEmpty(xe) && String.IsNullOrEmpty(phieuinca))
            {
                // Get lastest phieuinca
                var lastestCode = dbContext.FactoryVanHanhs.Find(m=>m.Enable.Equals(true)).SortByDescending(m=>m.PhieuInCa).FirstOrDefault();
                if (lastestCode != null)
                {
                    viewModel.VanHanhs = await dbContext.FactoryVanHanhs.Find(m => m.PhieuInCa.Equals(lastestCode.PhieuInCa)).ToListAsync();
                    viewModel.NhaThau = await dbContext.FactoryNhaThaus.Find(m => m.XeAlias.Equals(lastestCode.XeCoGioiMayAlias)).FirstOrDefaultAsync();
                    viewModel.phieuinca = lastestCode.PhieuInCa;
                    viewModel.ngay = lastestCode.Date;
                }
                
                return View(viewModel);
            }

            #region Filter
            var builder = Builders<FactoryVanHanh>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!String.IsNullOrEmpty(phieuinca))
            {
                filter = filter & builder.Regex(m => m.PhieuInCa, phieuinca);
                // because each month reset. Get lastest code
                var ls = await dbContext.FactoryVanHanhs.Find(filter).ToListAsync();
                if (ls != null && ls.Count > 0)
                {
                    var lastCode = ls.OrderByDescending(m => m.PhieuInCa).FirstOrDefault();
                    viewModel.VanHanhs = await dbContext.FactoryVanHanhs.Find(m => m.PhieuInCa.Equals(lastCode.PhieuInCa)).ToListAsync();
                    viewModel.NhaThau = await dbContext.FactoryNhaThaus.Find(m => m.XeAlias.Equals(lastCode.XeCoGioiMayAlias)).FirstOrDefaultAsync();
                    viewModel.phieuinca = lastCode.PhieuInCa;
                }
            }
            else
            {
                if (!ngay.HasValue)
                {
                    ngay = DateTime.Now.Date;
                }
                filter = filter & builder.Eq(m => m.Date, ngay.Value.Date);
                filter = filter & builder.Eq(m => m.XeCoGioiMayAlias, xe);
                viewModel.VanHanhs = await dbContext.FactoryVanHanhs.Find(filter).ToListAsync();
                viewModel.NhaThau = await dbContext.FactoryNhaThaus.Find(m => m.XeAlias.Equals(xe)).FirstOrDefaultAsync();
                viewModel.ngay = ngay;
                viewModel.xe = xe;
            }
            #endregion

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

        #region Sub data
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
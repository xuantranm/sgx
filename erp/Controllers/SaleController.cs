using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Driver;

namespace Tribat.Controllers
{
    //[Authorize]
    public class SaleController : Controller
    {
        IHostingEnvironment _hostingEnvironment;

        private readonly TribatContext _context;

        MongoDBContext dbContext = new MongoDBContext();

        private readonly IDistributedCache _cache;

        private readonly string key;
        private readonly string keyProduct;
        private readonly string exports;

        public SaleController(TribatContext context, IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env)
        {
            _context = context;
            _cache = cache;
            key = Constants.Collection.Orders;
            exports = "orders";
            Configuration = configuration;
            _hostingEnvironment = env;
        }

        public IConfiguration Configuration { get; }

        private void CacheReLoad()
        {
            _cache.SetString(key + Constants.FlagCacheKey, Constants.String_N);
        }

        public bool IsRight(string function, int right)
        {
            var roles = dbContext.NhanViens.Find(m => m.UserName.Equals(User.Identity.Name)).First().Roles;
            foreach (string role in roles.Split(';'))
            {
                if (role.Split(':')[0] == "System")
                {
                    return true;
                }
                if (role.Split(':')[0] == function && Convert.ToInt32(role.Split(':')[1]) >= right)
                {
                    return true;
                }
            }
            return false;
        }

        public Tuple<string, string> PageSizeAndCache()
        {
            var pageSize = Configuration.GetSection("Setting:PageSize").Value;
            var cacheEnable = Configuration.GetSection("Setting:Cached").Value;

            var cacheSettings = _cache.GetString(Constants.Collection.Settings);
            var settings = JsonConvert.DeserializeObject<IEnumerable<Setting>>(cacheSettings);
            if (settings.Count() != 0)
            {
                var setting = settings.Where(m => m.Key == "PageSize").First();
                if (setting != null)
                {
                    pageSize = setting.Content;
                }
                var cache = settings.Where(m => m.Key == Constants.Cache + key).FirstOrDefault();
                if (cache != null)
                {
                    cacheEnable = cache.Content;
                }
            }
            return new Tuple<string, string>(pageSize, cacheEnable);
        }

        // GET: Country
        [Route("/don-hang/")]
        public ActionResult Index(string loc, string pmh, string khach, string diachi, string maxe, string tinhtrang, string mahanghoa, int? trang)
        {
            #region Authorization
            if (Convert.ToInt32(Configuration.GetSection("Setting:Live").Value) == 1)
            {
                if (!IsRight("BUSINESS", 1))
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }
            #endregion

            #region DDL
            var status = new List<Status>
            {
                new Status()
                {
                    Id = Constants.Status.Open,
                    Name = Constants.Status.Open
                },
                new Status()
                {
                    Id = Constants.Status.Complete,
                    Name = Constants.Status.Complete
                }
            };
            ViewData["Status"] = status;
            #endregion

            ViewData["CurrentSort"] = loc;
            ViewData["CodeSortParm"] = loc == "code" ? "code_desc" : "code";

            var searchPmh = string.Empty;
            var searchKhach = string.Empty;
            var searchAddress = string.Empty;
            var searchXe = string.Empty;
            var searchStatus = string.Empty;
            var searchCode = string.Empty;

            if (!string.IsNullOrEmpty(pmh) || !string.IsNullOrEmpty(khach) || !string.IsNullOrEmpty(diachi) || !string.IsNullOrEmpty(maxe) || !string.IsNullOrEmpty(tinhtrang) || !string.IsNullOrEmpty(mahanghoa))
            {
                //page = 1;
                if (!string.IsNullOrEmpty(pmh))
                {
                    searchPmh = pmh;
                }
                if (!string.IsNullOrEmpty(khach))
                {
                    searchKhach = khach;
                }
                if (!string.IsNullOrEmpty(diachi))
                {
                    searchAddress = diachi;
                }
                if (!string.IsNullOrEmpty(maxe))
                {
                    searchXe = maxe;
                }
                if (!string.IsNullOrEmpty(mahanghoa))
                {
                    searchCode = mahanghoa;
                }
            }

            ViewData["CurrentCode"] = searchCode;
            ViewData["CurrentPmh"] = searchPmh;
            ViewData["CurrentKhach"] = searchKhach;
            ViewData["CurrentAddress"] = searchAddress;
            ViewData["CurrentXe"] = searchXe;
            ViewData["CurrentStatus"] = searchStatus;

            var pageCache = PageSizeAndCache();
            var pageSize = Convert.ToInt32(pageCache.Item1);
            var cacheEnable = pageCache.Item2;

            var results = from e in dbContext.DonHangs.AsQueryable()
                          select e;
            if (!string.IsNullOrEmpty(searchCode))
            {
                results = (from e in dbContext.DonHangs.AsQueryable()
                           where e.HangHoaMuas.Any(c => c.Code.Equals(searchCode))
                           select e);
            }
            if (!string.IsNullOrEmpty(searchPmh))
            {
                results = results.Where(s => s.Code.Equals(searchPmh));
            }
            if (!string.IsNullOrEmpty(searchKhach))
            {
                results = results.Where(s => s.Khach.Equals(searchKhach));
            }
            if (!string.IsNullOrEmpty(searchAddress))
            {
                results = results.Where(s => s.Address.Contains(searchAddress));
            }
            if (!string.IsNullOrEmpty(searchXe))
            {
                results = results.Where(s => s.XeCode.Equals(searchXe));
            }
            if (!string.IsNullOrEmpty(searchStatus))
            {
                results = results.Where(s => s.Status.Contains(searchStatus));
            }

            // Code, Name, Type
            switch (loc)
            {
                case "code_desc":
                    results = results.OrderByDescending(s => s.Code);
                    break;
                case "code":
                    results = results.OrderBy(s => s.Code);
                    break;
                default:
                    results = results.OrderByDescending(s => s.UpdatedOn);
                    break;
            }
            return View(PaginatedList<DonHang>.Create(results, trang ?? 1, pageSize));
        }

        [Route("/don-hang/tao-moi/")]
        public ActionResult Create()
        {
            #region DDL
            var groups = dbContext.ProductGroups.Find(m => m.ParentCode.Equals(string.Empty)).ToList();
            ViewData["Groups"] = groups;

            var groupchilds = dbContext.ProductGroups.Find(m => m.ParentCode != string.Empty).ToList();
            ViewData["GroupChilds"] = groupchilds;
            #endregion

            #region Default NewCode
            var newCode = NewCode();
            ViewData["newCode"] = newCode;
            #endregion

            return View();
        }

        private string NewCode()
        {
            var subCode = "DH" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString("d2");
            var newCodeFormat = subCode + "001";
            var lastRecord = dbContext.DonHangs.Find(m => true).SortByDescending(m => m.Id).Limit(1);
            if (lastRecord.Count() > 0)
            {
                var lastCode = lastRecord.First().Code;
                var newCode = int.Parse(lastCode.Substring(8)) + 1;
                newCodeFormat = subCode + newCode.ToString().PadLeft(3, '0');
            }
            return newCodeFormat;
        }

        // POST: Requisition/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/don-hang/tao-moi/")]
        public ActionResult Create(DonHang entity)
        {
            if (ModelState.IsValid)
            {
                #region DDL
                #endregion

                #region Check Duplicate Code
                var lastRecord = dbContext.DonHangs.Find(m => true).SortByDescending(m => m.Id).Limit(1).FirstOrDefault();
                if (lastRecord != null)
                {
                    if (lastRecord.Code == entity.Code)
                    {
                        var newCode = NewCode();
                        ModelState.AddModelError("Code", "Phiếu đăth hàng đã được tạo " + entity.Code + ", đã cập nhật phiếu đặt hàng mới." + newCode);
                        ViewData["newCode"] = newCode;
                        return View(entity);
                    }
                }
                #endregion

                var userId = User.Identity.Name;
                if (Convert.ToInt32(Configuration.GetSection("Setting:Live").Value) == 0)
                {
                    userId = "administrator";
                }
                var now = DateTime.Now;
                entity.CreatedBy = userId;
                entity.UpdatedBy = userId;
                entity.CheckedBy = userId;
                entity.ApprovedBy = userId;
                // Amount
                decimal amount = 0;
                foreach(var hang in entity.HangHoaMuas)
                {
                    amount += hang.PriceTotal;
                }
                entity.Amount = amount;
                dbContext.DonHangs.InsertOne(entity);

                foreach (var request in entity.HangHoaMuas)
                {
                    #region Update Kho (So luong tam giu)
                    var quantityCurrent = dbContext.Products.Find(m => m.Code.Equals(request.Code) && m.Enable.Equals(true)).First().Quantity;
                    var filterKho = Builders<Product>.Filter.Eq(m => m.Code, request.Code);
                    var updateKho = Builders<Product>.Update.Inc(m => m.QuantityDonHang, request.Quantity);
                    var resultKho = dbContext.Products.UpdateOne(filterKho, updateKho);
                    #endregion
                }


                var newId = entity.Code;

                CacheReLoad();

                #region Activities
                var objectId = newId;
                var objectName = entity.Code;

                var activity = new TrackingUser
                {
                    UserId = userId,
                    Function = Constants.Collection.DonHang,
                    Action = "create",
                    Values = objectId,
                    ValuesDisplay = objectName,
                    Description = "create" + " " + Constants.Collection.DonHang + objectName,
                    Created = now,
                    Link = "/don-hang?pmh=" + objectId
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion

                return RedirectToAction(nameof(Index));
            }

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/don-hang/trang-thai/")]
        public ActionResult Status(string code, string status)
        {
            var userId = User.Identity.Name;
            if (Convert.ToInt32(Configuration.GetSection("Setting:Live").Value) == 0)
            {
                userId = "administrator";
            }
            var now = DateTime.Now;
            #region Update Status
            var filterDh = Builders<DonHang>.Filter.Eq(m => m.Code, code);
            var updateDh = Builders<DonHang>.Update.Set(m => m.Status, status)
                                            .Set(m => m.UpdatedBy, userId)
                                            .Set(m => m.CheckedBy, userId)
                                            .Set(m=>m.ApprovedBy, userId)
                                            .Set(m => m.UpdatedOn, now)
                                            .Set(m => m.CheckedOn, now)
                                            .Set(m => m.ApprovedOn, now);
            var resultDh = dbContext.DonHangs.UpdateOne(filterDh, updateDh);
            #endregion

            // Demo. when live define later...
            if (status == Constants.StatusKinhDoanh.Approved)
            {
                var entity = dbContext.DonHangs.Find(m => m.Code.Equals(code)).FirstOrDefault();
                foreach (var request in entity.HangHoaMuas)
                {
                    #region Update Kho (- số lượng)
                    var khohang = dbContext.Products.Find(m => m.Code.Equals(request.Code) && m.Enable.Equals(true)).First();
                    var quantityCurrent = khohang.Quantity;
                    var min = khohang.QuantityInStoreSafe;
                    var max = khohang.QuantityInStoreMax;
                    var quantityNext =  quantityCurrent - request.Quantity;
                    var statusKhoHang = Constants.Status.Avg;
                    if (quantityNext < min)
                    {
                        statusKhoHang = Constants.Status.Min;
                    }
                    if (max > 0)
                    {
                        if (quantityNext > max)
                        {
                            statusKhoHang = Constants.Status.Max;
                        }
                    }

                    var filterKho = Builders<Product>.Filter.Eq(m => m.Code, request.Code);
                    var updateKho = Builders<Product>.Update.Inc(m => m.QuantityDonHang, -(request.Quantity))
                                                            .Inc(m => m.Quantity, -(request.Quantity))
                                                            .Set(m => m.Status, statusKhoHang);
                    var resultKho = dbContext.Products.UpdateOne(filterKho, updateKho);

                    // Log
                    dbContext.ProductLogs.InsertOne(new ProductLog
                    {
                        Code = request.Code,
                        Name = request.Name,
                        Type = Constants.NhapXuat.Xuat,
                        Quantity = quantityCurrent,
                        QuantityChange = request.Quantity,
                        Request = string.Empty,
                        DatHang = string.Empty,
                        NhanHang = string.Empty,
                        XuatHang = code,
                        CreatedBy = userId,
                        UpdatedBy = userId,
                        CheckedBy = userId,
                        ApprovedBy = userId
                    });
                    #endregion
                }
            }

            var newId = code;
            CacheReLoad();

            #region Activities
            var objectId = newId;
            var objectName = code;

            var activity = new TrackingUser
            {
                UserId = userId,
                Function = Constants.Collection.DonHang,
                Action = Constants.Action.Approved,
                Values = objectId,
                ValuesDisplay = objectName,
                Description = Constants.Action.Approved + " " + Constants.Collection.DonHang + objectName + " trạng thái " + status,
                Created = now,
                Link = "/don-hang?pmh=" + objectId
            };
            dbContext.TrackingUsers.InsertOne(activity);
            #endregion

            return Json(status);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/don-hang/xep-xe/")]
        public ActionResult XepXe(string code, string xe)
        {
            var userId = User.Identity.Name;
            if (Convert.ToInt32(Configuration.GetSection("Setting:Live").Value) == 0)
            {
                userId = "administrator";
            }
            var now = DateTime.Now;
            #region Update Xe
            var filterDh = Builders<DonHang>.Filter.Eq(m => m.Code, code);
            var updateDh = Builders<DonHang>.Update.Inc(m => m.XeCode, xe)
                                            .Set(m => m.UpdatedBy, userId)
                                            .Set(m => m.UpdatedOn, now);
            var resultDh = dbContext.DonHangs.UpdateOne(filterDh, updateDh);
            #endregion

            var newId = code;
            CacheReLoad();

            #region Activities
            var objectId = newId;
            var objectName = code;

            var activity = new TrackingUser
            {
                UserId = userId,
                Function = Constants.Collection.DonHang,
                Action = "edit",
                Values = objectId,
                ValuesDisplay = objectName,
                Description = "edit" + " " + Constants.Collection.DonHang + objectName + " xếp xe " + xe,
                Created = now,
                Link = "/don-hang/thong-tin/" + objectId
            };
            dbContext.TrackingUsers.InsertOne(activity);
            #endregion

            return Json(true);
        }
    }
}

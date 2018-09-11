using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tribat.ModelsTribat;
using Microsoft.Extensions.Caching.Distributed;
using Tribat.Data;
using Tribat.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Driver;
using System.Data.SqlClient;
using Tribat.Models.Tribats;
using Tribat.Common.Utilities;

namespace Tribat.Controllers
{
    //[Authorize]
    public class ReceiveController : Controller
    {
        IHostingEnvironment _hostingEnvironment;

        private readonly TribatContext _context;

        MongoDBContext dbContext = new MongoDBContext();

        private readonly IDistributedCache _cache;

        private readonly string key;
        private readonly string keyProduct;
        private readonly string exports;

        public ReceiveController(TribatContext context, IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env)
        {
            _context = context;
            _cache = cache;
            key = Constants.Collection.NA;
            exports = "inputs";
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
        [Route("/nhap-hang/")]
        public ActionResult Index(string loc, string pnh, string pdh, string pyc, string tinhtrang, string mahanghoa, int? trang)
        {
            #region Authorization
            if (Convert.ToInt32(Configuration.GetSection("Setting:Live").Value) == 1)
            {
                if (!IsRight("REQUEST", 1))
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

            var searchFulfill = string.Empty;
            var searchPe = string.Empty;
            var searchMrf = string.Empty;
            var searchStatus = string.Empty;
            var searchCode = string.Empty;

            if (!string.IsNullOrEmpty(pnh) || !string.IsNullOrEmpty(pdh) || !string.IsNullOrEmpty(pyc) || !string.IsNullOrEmpty(tinhtrang) || !string.IsNullOrEmpty(mahanghoa))
            {
                //page = 1;
                if (!string.IsNullOrEmpty(pnh))
                {
                    searchFulfill = pnh;
                }
                if (!string.IsNullOrEmpty(pdh))
                {
                    searchPe = pdh;
                }
                if (!string.IsNullOrEmpty(pyc))
                {
                    searchMrf = pyc;
                }
                if (!string.IsNullOrEmpty(mahanghoa))
                {
                    searchCode = mahanghoa;
                }
            }

            ViewData["CurrentCode"] = searchCode;
            ViewData["CurrentFulfill"] = searchFulfill;
            ViewData["CurrentPe"] = searchPe;
            ViewData["CurrentMrf"] = searchMrf;
            ViewData["CurrentStatus"] = searchStatus;

            var pageCache = PageSizeAndCache();
            var pageSize = Convert.ToInt32(pageCache.Item1);
            var cacheEnable = pageCache.Item2;

            // Devide search method to master or detail
            //var results = (from e in dbContext.Requests.AsQueryable()
            //               where e.ProductYeuCaus.Any(c => c.Code.Equals("BB0001"))
            //              select e);
            var results = from e in dbContext.NhanHangs.AsQueryable()
                          select e;
            if (!string.IsNullOrEmpty(searchCode))
            {
                results = (from e in dbContext.NhanHangs.AsQueryable()
                           where e.ProductNhanHangs.Any(c => c.Code.Equals(searchCode))
                           select e);
            }
            if (!string.IsNullOrEmpty(searchFulfill))
            {
                results = results.Where(s => s.Code.Equals(searchFulfill));
            }
            if (!string.IsNullOrEmpty(searchPe))
            {
                results = results.Where(s => s.DatHangCode.Equals(searchPe));
            }
            //if (!string.IsNullOrEmpty(searchMrf))
            //{
            //    results = results.Where(s => s.Code.Equals(searchMrf));
            //}

            //if (!string.IsNullOrEmpty(searchStatus))
            //{
            //    results = results.Where(s => s.Status.Contains(searchStatus));
            //}

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
            return View(PaginatedList<NhanHang>.Create(results, trang ?? 1, pageSize));
        }

        // GET: Stock/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entity = await _context.Requisitions.FromSql("EXEC dbo.Requisitions @enable", new SqlParameter("enable", true)).AsNoTracking()
                .SingleOrDefaultAsync(m => m.Id == id);
            if (entity == null)
            {
                return NotFound();
            }

            return View(entity);
        }

        // GET: Stock/Create
        [Route("/nhap-hang/tao-moi/")]
        public ActionResult Create()
        {
            #region DDL

            #endregion

            #region Default NewCode
            var newCode = NewCode();
            ViewData["newCode"] = newCode;
            #endregion

            return View();
        }

        private string NewCode()
        {
            var subCode = "RC" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString("d2");
            var newCodeFormat = subCode + "001";
            var lastRecord = dbContext.NhanHangs.Find(m => true).SortByDescending(m => m.Id).Limit(1);
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
        [Route("/nhap-hang/tao-moi/")]
        public ActionResult Create(NhanHang entity)
        {
            if (ModelState.IsValid)
            {
                #region DDL
                #endregion

                #region Check Duplicate Code
                var lastRecord = dbContext.NhanHangs.Find(m => true).SortByDescending(m => m.Id).Limit(1).FirstOrDefault();
                if (lastRecord != null)
                {
                    if (lastRecord.Code == entity.Code)
                    {
                        var newCode = NewCode();
                        ModelState.AddModelError("Code", "Phiếu nhận hàng đã được tạo " + entity.Code + ", đã cập nhật phiếu nhận hàng mới." + newCode);
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
                dbContext.NhanHangs.InsertOne(entity);

                foreach (var request in entity.ProductNhanHangs)
                {
                    #region Update Kho
                    var khohang = dbContext.Products.Find(m => m.Code.Equals(request.Code) && m.Enable.Equals(true)).First();
                    var quantityCurrent = khohang.Quantity;
                    var min = khohang.QuantityInStoreSafe;
                    var max = khohang.QuantityInStoreMax;
                    var quantityNext = request.QuantityReceive + quantityCurrent;
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
                    var updateKho = Builders<Product>.Update.Inc(m => m.Quantity, request.QuantityReceive).Set(m=>m.Status, statusKhoHang);
                    var resultKho = dbContext.Products.UpdateOne(filterKho, updateKho);

                    // Log
                    dbContext.ProductLogs.InsertOne(new ProductLog
                    {
                        Code = request.Code,
                        Name = request.Name,
                        Type = Constants.NhapXuat.Nhap,
                        Quantity = quantityCurrent,
                        QuantityChange = request.QuantityReceive,
                        Request = request.RequestCode,
                        DatHang = entity.DatHangCode,
                        NhanHang = entity.Code,
                        XuatHang = string.Empty,
                        CreatedBy = userId,
                        UpdatedBy = userId,
                        CheckedBy = userId,
                        ApprovedBy = userId
                    });
                    #endregion

                    #region Update Phieu Yeu Cau
                    var filterRe = Builders<Request>.Filter
                                .And(
                                    Builders<Request>.Filter.Eq(d => d.Code, request.RequestCode),
                                    Builders<Request>.Filter.ElemMatch(x => x.ProductYeuCaus, p => p.Code == request.Code));
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var updateRe = Builders<Request>.Update
                                    .Set(c => c.ProductYeuCaus[-1].QuantityReceive, request.QuantityReceive)
                                    .Set(c => c.ProductYeuCaus[-1].Status, Constants.Status.Complete);
                    dbContext.Requests.UpdateOne(filterRe, updateRe);
                    // Update trang thai Phieu Yeu Cau
                    var pyc = dbContext.Requests.Find(m => m.Code.Equals(request.RequestCode)).FirstOrDefault();
                    var flagYc = true;
                    foreach (var item in pyc.ProductYeuCaus)
                    {
                        if (item.Status.Equals(Constants.Status.Open) || item.Status.Equals(Constants.Status.DatHang))
                        {
                            flagYc = false;
                        }
                    }
                    if (flagYc)
                    {
                        var filterReM = Builders<Request>.Filter.Eq(m => m.Code, request.RequestCode);
                        var updateReM = Builders<Request>.Update.Set(m => m.Status, Constants.Status.Complete);
                        var resultReM = dbContext.Requests.UpdateOne(filterReM, updateReM);
                    }
                    #endregion

                    #region Update Phieu Dat Hang
                    var filterDh = Builders<DatHang>.Filter
                                .And(
                                    Builders<DatHang>.Filter.Eq(d => d.Code, entity.DatHangCode),
                                    Builders<DatHang>.Filter.ElemMatch(x => x.ProductDatHangs, p => p.Code == request.Code && p.RequestCode == request.RequestCode));
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var updateDh = Builders<DatHang>.Update
                                    .Set(c => c.ProductDatHangs[-1].QuantityReceive, request.QuantityReceive)
                                    .Set(c => c.ProductDatHangs[-1].Status, Constants.Status.Complete);
                    dbContext.DatHangs.UpdateOne(filterDh, updateDh);
                    // Update trang thai Phieu Dat Hang
                    var pdh = dbContext.DatHangs.Find(m => m.Code.Equals(entity.DatHangCode)).FirstOrDefault();
                    var flagDh = true;
                    foreach (var item in pdh.ProductDatHangs)
                    {
                        if (item.Status.Equals(Constants.Status.Open) || item.Status.Equals(Constants.Status.DatHang))
                        {
                            flagDh = false;
                        }
                    }
                    if (flagDh)
                    {
                        var filterDhM = Builders<DatHang>.Filter.Eq(m => m.Code, entity.DatHangCode);
                        var updateDhM = Builders<DatHang>.Update.Set(m => m.Status, Constants.Status.Complete);
                        var resultDhM = dbContext.DatHangs.UpdateOne(filterDhM, updateDhM);
                    }
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
                    Function = Constants.Collection.NhanHang,
                    Action = "create",
                    Values = objectId,
                    ValuesDisplay = objectName,
                    Description = "create" + " " + Constants.Collection.NhanHang + objectName,
                    Created = now,
                    Link = "/nhap-hang?pnh=" + objectId
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion

                return RedirectToAction(nameof(Index));
            }

            return View(entity);
        }
    }
}

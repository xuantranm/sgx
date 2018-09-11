using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tribat.ModelsTribat;
using Microsoft.Extensions.Caching.Distributed;
using Tribat.Data;
using Tribat.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using OfficeOpenXml;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;
using Tribat.ModelsTribat.Stores;
using System.Data.SqlClient;
using Tribat.ViewModels;
using Tribat.Common.Utilities;
using Tribat.Extensions;

namespace Tribat.Controllers
{
    //[Authorize]
    public class StoreController : Controller
    {
        IHostingEnvironment _hostingEnvironment;

        private readonly TribatContext _context;

        MongoDBContext dbContext = new MongoDBContext();

        private readonly IDistributedCache _cache;

        private readonly string key;
        private readonly string exports;

        public StoreController(TribatContext context, IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env)
        {
            _context = context;
            _cache = cache;
            key = Constants.Collection.Stores;
            exports = "kho";
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

        public Tuple<string,string> PageSizeAndCache()
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

        [Route("/kho-hang/")]
        public ActionResult Index(string loc, string ma, string ten, string kho, string nhom, string phannhom, string dvt, string tinhtrang, int? trang)
        {
            #region Authorization
            if (Convert.ToInt32(Configuration.GetSection("Setting:Live").Value) == 1)
            {
                if (!IsRight("STORE", 1))
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            #endregion

            #region export link
            var downloads = Configuration.GetSection("Setting:Downloads").Value;
            var documents = Configuration.GetSection("Setting:Documents").Value;
            string sFileName = exports + ".xlsx";
            ViewData["ExportLink"] = string.Format("{0}://{1}/{2}/{3}/{4}", Request.Scheme, Request.Host, downloads, documents, sFileName);
            #endregion

            #region SelectList
            var groups = dbContext.ProductGroups.Find(m => m.ParentCode.Equals(string.Empty)).ToList();
            ViewData["Groups"] = new SelectList(groups, "Code", "Name");

            var groupchilds = dbContext.ProductGroups.Find(m => m.ParentCode != string.Empty).ToList();
            ViewData["GroupChilds"] = new SelectList(groupchilds, "Code", "Name");

            var units = dbContext.Units.Find(m => true).ToList();
            ViewData["Units"] = new SelectList(units, "Name", "Name");
            #endregion

            var point = ".";
            var cacheKeyCombine = new StringBuilder();

            ViewData["CurrentSort"] = loc;
            ViewData["CodeSortParm"] = String.IsNullOrEmpty(loc) ? "code_desc" : "";
            ViewData["NameSortParm"] = loc == "name" ? "name_desc" : "name";

            var searchCode = string.Empty;
            var searchName = string.Empty;
            var searchStore = string.Empty;
            var searchGroup = string.Empty;
            var searchGroupChild = string.Empty;
            var searchUnit = string.Empty;
            var searchStatus = string.Empty;


            if (!string.IsNullOrEmpty(ma) || !string.IsNullOrEmpty(ten) || !string.IsNullOrEmpty(kho) || !string.IsNullOrEmpty(nhom) || !string.IsNullOrEmpty(phannhom) || !string.IsNullOrEmpty(dvt) || !string.IsNullOrEmpty(tinhtrang))
            {
                //page = 1;
                if (!string.IsNullOrEmpty(ma))
                {
                    searchCode = ma;
                }
                if (!string.IsNullOrEmpty(ten))
                {
                    searchName = ten;
                }
                if (!string.IsNullOrEmpty(kho))
                {
                    searchStore = kho;
                }
                if (!string.IsNullOrEmpty(nhom))
                {
                    searchGroup = nhom;
                }
                if (!string.IsNullOrEmpty(phannhom))
                {
                    searchGroupChild = phannhom;
                }
                if (!string.IsNullOrEmpty(dvt))
                {
                    searchUnit = dvt;
                }
                if (!string.IsNullOrEmpty(tinhtrang))
                {
                    searchStatus = tinhtrang;
                }

            }

            ViewData["CurrentCode"] = searchCode;
            ViewData["CurrentName"] = searchName;
            ViewData["CurrentStore"] = searchStore;
            ViewData["CurrentGroup"] = searchGroup;
            ViewData["CurrentGroupChild"] = searchGroupChild;
            ViewData["CurrentUnit"] = searchUnit;
            ViewData["CurrentStatus"] = searchStatus;

            var pageCache = PageSizeAndCache();
            var pageSize = Convert.ToInt32(pageCache.Item1);
            var cacheEnable = pageCache.Item2;

            if (cacheEnable.Equals("false"))
            {
                var results = from e in dbContext.Products.AsQueryable()
                              select e;
                if (!string.IsNullOrEmpty(searchCode))
                {
                    results = results.Where(s => s.Code.Equals(searchCode));
                }
                if (!string.IsNullOrEmpty(searchName))
                {
                    results = results.Where(s => s.Name.ToLower().Contains(searchName.ToLower()));
                }
                //if (!string.IsNullOrEmpty(searchStore))
                //{
                //    results = results.Where(s => s.Name.ToLower().Contains(searchName.ToLower()));
                //}
                if (!string.IsNullOrEmpty(searchGroup))
                {
                    results = results.Where(s => s.Group.ToLower().Contains(searchGroup.ToLower()));
                }
                if (!string.IsNullOrEmpty(searchGroupChild))
                {
                    results = results.Where(s => s.GroupDevide.ToLower().Contains(searchGroupChild.ToLower()));
                }
                if (!string.IsNullOrEmpty(searchUnit))
                {
                    results = results.Where(s => s.Unit.ToLower().Contains(searchUnit.ToLower()));
                }
                if (!string.IsNullOrEmpty(searchStatus))
                {
                    results = results.Where(s => s.Status.ToLower().Contains(searchStatus.ToLower()));
                }
                switch (loc)
                {
                    case "name_desc":
                        results = results.OrderByDescending(s => s.Name);
                        break;
                    case "name":
                        results = results.OrderBy(s => s.Name);
                        break;
                    case "code_desc":
                        results = results.OrderByDescending(s => s.Code);
                        break;
                    default:
                        results = results.OrderBy(s => s.Code);
                        break;
                }
                return View(PaginatedList<Product>.Create(results.AsNoTracking(), trang ?? 1, pageSize));
            }
            else
            {
                // Use flag define new data??
                cacheKeyCombine.Append(key);
                cacheKeyCombine.Append(point);
                cacheKeyCombine.Append(loc);
                cacheKeyCombine.Append(point);
                cacheKeyCombine.Append(searchCode);
                cacheKeyCombine.Append(point);
                cacheKeyCombine.Append(searchName);
                cacheKeyCombine.Append(point);
                cacheKeyCombine.Append(searchStore);
                cacheKeyCombine.Append(point);
                cacheKeyCombine.Append(searchGroup);
                cacheKeyCombine.Append(point);
                cacheKeyCombine.Append(searchGroupChild);
                cacheKeyCombine.Append(point);
                cacheKeyCombine.Append(searchUnit);
                cacheKeyCombine.Append(point);
                cacheKeyCombine.Append(searchStatus);
                cacheKeyCombine.Append(point);
                cacheKeyCombine.Append(trang ?? 1);
                cacheKeyCombine.Append(point);
                cacheKeyCombine.Append(pageSize);

                var keyCachePageList = cacheKeyCombine.ToString();
                var keyCachePageListCount = cacheKeyCombine.Append(Constants.CountCacheKey).ToString();
                var values = _cache.GetString(keyCachePageList);
                var flag = _cache.GetString(key + Constants.FlagCacheKey);
                var totalPages = Convert.ToInt32(_cache.GetString(keyCachePageListCount));
                if (totalPages > 0 && flag == Constants.String_Y)
                {
                    var result = JsonConvert.DeserializeObject<IEnumerable<Product>>(values);
                    return View(PaginatedList<Product>.CacheReturn(result, totalPages, trang ?? 1, pageSize));
                }
                else
                {
                    // No use cached call all list data here. http://www.dotnettricks.com/learn/linq/ienumerable-vs-iqueryable.
                    // If use cache: big list stock will excuted here to IEnumerable
                    // MUST USE IQueryable, because only excute when ToList(). Performace here.

                    var results = from e in dbContext.Products.AsQueryable()
                                  select e;
                    if (!string.IsNullOrEmpty(searchCode))
                    {
                        results = results.Where(s => s.Code.Equals(searchCode));
                    }
                    if (!string.IsNullOrEmpty(searchName))
                    {
                        results = results.Where(s => s.Name.ToLower().Contains(searchName.ToLower()));
                    }
                    //if (!string.IsNullOrEmpty(searchStore))
                    //{
                    //    results = results.Where(s => s.Name.ToLower().Contains(searchName.ToLower()));
                    //}
                    if (!string.IsNullOrEmpty(searchGroup))
                    {
                        results = results.Where(s => s.Group.ToLower().Contains(searchGroup.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(searchGroupChild))
                    {
                        results = results.Where(s => s.GroupDevide.ToLower().Contains(searchGroupChild.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(searchUnit))
                    {
                        results = results.Where(s => s.Unit.ToLower().Contains(searchUnit.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(searchStatus))
                    {
                        results = results.Where(s => s.Status.ToLower().Contains(searchStatus.ToLower()));
                    }
                    switch (loc)
                    {
                        case "name_desc":
                            results = results.OrderByDescending(s => s.Name);
                            break;
                        case "name":
                            results = results.OrderBy(s => s.Name);
                            break;
                        case "code_desc":
                            results = results.OrderByDescending(s => s.Code);
                            break;
                        default:
                            results = results.OrderBy(s => s.Code);
                            break;
                    }

                    var result = PaginatedList<Product>.Create(results, trang ?? 1, pageSize);
                    values = JsonConvert.SerializeObject(result.ToList());
                    _cache.SetString(keyCachePageList, values);
                    _cache.SetString(keyCachePageListCount, result.Records.ToString());
                    _cache.SetString(key + Constants.FlagCacheKey, Constants.String_N);
                    return View(result);
                }
            }
        }

        #region Excel, after load page, called ajax export.
        [HttpGet]
        [Route("stock/export")]
        public void Export(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = exports;
            }

            var items = JsonConvert.DeserializeObject<IEnumerable<Products>>(_cache.GetString(key)).Where(m => m.TypeId != 8);
            ExportCommon(items, name);
        }

        private void ExportCommon(IEnumerable<Products> items, string name)
        {
            string sWebRootFolder = _hostingEnvironment.WebRootPath;
            var downloads = Configuration.GetSection("Setting:Downloads").Value;
            var documents = Configuration.GetSection("Setting:Documents").Value;
            sWebRootFolder = sWebRootFolder + "\\" + downloads + "\\" + documents + "\\";
            string sFileName = @name + ".xlsx";
            FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
            if (file.Exists)
            {
                file.Delete();
                file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
            }

            using (ExcelPackage package = new ExcelPackage(file))
            {
                // add a new worksheet to the empty workbook
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(key);
                //First add the headers
                //No.check Stock ID Stock Name Account Code Stock Type Quantity    Unit Name   Remark Category Name Brand   Supplier Name   Position Name   Sub Quantity    Weight

                worksheet.Cells[1, 1].Value = "No";
                worksheet.Cells[1, 2].Value = "Code";
                worksheet.Cells[1, 3].Value = "Name";
                worksheet.Cells[1, 4].Value = "Account Code";
                worksheet.Cells[1, 5].Value = "Type";
                worksheet.Cells[1, 6].Value = "Store";
                worksheet.Cells[1, 7].Value = "Quantity";
                worksheet.Cells[1, 8].Value = "Mini Quantity";
                worksheet.Cells[1, 9].Value = "Unit";
                worksheet.Cells[1, 10].Value = "Position";
                worksheet.Cells[1, 11].Value = "Label";
                worksheet.Cells[1, 12].Value = "Description";
                worksheet.Cells[1, 13].Value = "Remark";
                worksheet.Cells[1, 14].Value = "Category";
                worksheet.Cells[1, 15].Value = "Brand";
                worksheet.Cells[1, 16].Value = "Supplier";
                worksheet.Cells[1, 17].Value = "Sub.Quantity";
                worksheet.Cells[1, 18].Value = "Sub.Unit";
                worksheet.Cells[1, 19].Value = "Weight";
                // SPECIFICATIONS
                // Consumable
                // Paint
                worksheet.Cells[1, 20].Value = "Paint Use For";
                worksheet.Cells[1, 21].Value = "Paint Color";
                worksheet.Cells[1, 22].Value = "Paint Ral.No";
                worksheet.Cells[1, 23].Value = "Paint Position";
                worksheet.Cells[1, 24].Value = "Paint Sub Category";
                // Scaffolding
                worksheet.Cells[1, 25].Value = "Scaffolding Type";
                worksheet.Cells[1, 26].Value = "Scaffolding Weight";
                // Material
                // Tool
                // Equipment
                worksheet.Cells[1, 27].Value = "Scaffolding Weight";
                worksheet.Cells[1, 28].Value = "Scaffolding Power Source";
                worksheet.Cells[1, 29].Value = "Scaffolding Plug Type";
                worksheet.Cells[1, 30].Value = "Scaffolding Warranty Date";
                worksheet.Cells[1, 31].Value = "Scaffolding Number of dates before Expire";
                worksheet.Cells[1, 32].Value = "Scaffolding Date of Receive";
                worksheet.Cells[1, 33].Value = "Scaffolding Systematic Maintenance Frequency (Months)";
                // Vehicle
                worksheet.Cells[1, 34].Value = "Vehicle Color";
                worksheet.Cells[1, 35].Value = "Vehicle Plate No";
                worksheet.Cells[1, 36].Value = "Vehicle Registration Frequency";
                worksheet.Cells[1, 37].Value = "Vehicle Number of dates before Expire";
                worksheet.Cells[1, 38].Value = "Vehicle Last Registration";
                worksheet.Cells[1, 39].Value = "Vehicle Model";
                // SparePart
                worksheet.Cells[1, 40].Value = "SpartPare For";

                // format header - bold, yellow on black
                var background = Configuration.GetSection("Setting:ExcelBackGround").Value;
                var cacheSettings = _cache.GetString(Constants.Collection.Settings);
                var settings = JsonConvert.DeserializeObject<IEnumerable<Setting>>(cacheSettings);
                var cache = settings.Where(m => m.Key == "ExcelBackGround").FirstOrDefault();
                if (cache != null)
                {
                    background = cache.Content;
                }
                using (ExcelRange r = worksheet.Cells[1, 1, 1, 41])
                {
                    r.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml(background));
                }


                //Add values
                var i = 2;
                foreach (var data in items)
                {
                    worksheet.Cells[i, 1].Value = i - 1;
                    worksheet.Cells[i, 2].Value = data.VStockId;
                    worksheet.Cells[i, 3].Value = data.VStockName;
                    worksheet.Cells[i, 4].Value = data.VAccountCode;
                    worksheet.Cells[i, 5].Value = data.Type;
                    worksheet.Cells[i, 6].Value = data.StoreName;
                    worksheet.Cells[i, 7].Value = data.BQuantity;
                    worksheet.Cells[i, 8].Value = data.PartNoMiniQty;
                    worksheet.Cells[i, 9].Value = data.Unit;
                    worksheet.Cells[i, 10].Value = data.Position;
                    worksheet.Cells[i, 11].Value = data.Label;
                    worksheet.Cells[i, 12].Value = data.Description;
                    worksheet.Cells[i, 13].Value = data.VRemark;
                    worksheet.Cells[i, 14].Value = data.Category;
                    worksheet.Cells[i, 15].Value = data.VBrand;
                    worksheet.Cells[i, 16].Value = data.BSupplierId;
                    // Sub Quantity
                    worksheet.Cells[i, 17].Value = data.SubCategory;
                    // Sub Unit
                    worksheet.Cells[i, 18].Value = data.Unit;
                    worksheet.Cells[i, 19].Value = data.BWeight;
                    //Paint
                    worksheet.Cells[i, 20].Value = data.UserForPaint;
                    worksheet.Cells[i, 21].Value = data.ColorName;
                    worksheet.Cells[i, 22].Value = data.RalNo;
                    //Paint Position
                    worksheet.Cells[i, 23].Value = string.Empty;
                    //Paint Sub Category
                    worksheet.Cells[i, 24].Value = data.SubCategory;

                    // Scaffolding
                    worksheet.Cells[i, 25].Value = "Scaffolding Type";
                    worksheet.Cells[i, 26].Value = "Scaffolding Weight";
                    // Material
                    // Tool
                    // Equipment
                    worksheet.Cells[i, 27].Value = "Scaffolding Weight";
                    worksheet.Cells[i, 28].Value = "Scaffolding Power Source";
                    worksheet.Cells[i, 29].Value = "Scaffolding Plug Type";
                    worksheet.Cells[i, 30].Value = "Scaffolding Warranty Date";
                    worksheet.Cells[i, 31].Value = "Scaffolding Number of dates before Expire";
                    worksheet.Cells[i, 32].Value = "Scaffolding Date of Receive";
                    worksheet.Cells[i, 33].Value = "Scaffolding Systematic Maintenance Frequency (Months)";
                    // Vehicle
                    worksheet.Cells[i, 34].Value = "Vehicle Color";
                    worksheet.Cells[i, 35].Value = "Vehicle Plate No";
                    worksheet.Cells[i, 36].Value = "Vehicle Registration Frequency";
                    worksheet.Cells[i, 37].Value = "Vehicle Number of dates before Expire";
                    worksheet.Cells[i, 38].Value = "Vehicle Last Registration";
                    worksheet.Cells[i, 39].Value = "Vehicle Model";
                    // SparePart
                    worksheet.Cells[i, 40].Value = "SpartPare For";
                    i++;
                }
                package.Save();
            }
        }
        #endregion

        // GET: Stock/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wamsStock = await _context.Stocks.FromSql("EXEC dbo.Stocks @enable", new SqlParameter("enable", true)).AsNoTracking()
                .SingleOrDefaultAsync(m => m.Id == id);
            if (wamsStock == null)
            {
                return NotFound();
            }

            //var quantities = _context.WamsStockManagementQuantity.Where(m => m.VStockId.Equals(id)).AsNoTracking();
            //var dd = PaginatedList<WamsStockManagementQuantity>.CreateAsync(quantities, 1, 50);

            var images = dbContext.Images.Find(m => m.ObjectId == id.ToString() && m.ObjectType == Constants.Collection.Stores).ToList();
            var model = new ProductViewModel
            {
                Product = wamsStock,
                //Quantities = dd,
                Images = images
            };
            return View(model);
        }

        // GET: Stock/Create
        [Route("/kho-hang/tao-moi/")]
        public IActionResult Create()
        {
            #region SelectList
            var groups = dbContext.ProductGroups.Find(m => m.ParentCode.Equals(string.Empty)).ToList();
            ViewData["Groups"] = groups;

            var groupchilds = dbContext.ProductGroups.Find(m => m.ParentCode.Equals(groups.First().Code)).ToList();
            ViewData["GroupChilds"] = groupchilds;

            var units = dbContext.Units.Find(m => true).ToList();
            ViewData["Units"] = units;

            var locations = dbContext.Locations.Find(m => true).ToList();
            ViewData["Locations"] = locations;
            #endregion

            // Default value


            #region Default NewCode
            var groupCode = groups.First().Code + "0";
            if (groupchilds.First() != null)
            {
                groupCode = groupchilds.First().Code;
            }

            var newCode = NewCode(groupCode);
            ViewData["newCode"] = newCode;
            #endregion

            return View();
        }

        private string NewCode(string groupCode)
        {
            var newCodeFormat = "001";
            var lastProduct = dbContext.Products.Find(m => m.GroupDevide.Equals(groupCode)).SortByDescending(m => m.Id).Limit(1);
            if (lastProduct.Count() > 0)
            {
                var lastCode = lastProduct.First().Code;
                var newCode = int.Parse(lastCode.Substring(3)) + 1;
                newCodeFormat = newCode.ToString().PadLeft(3, '0');
            }

            return groupCode + newCodeFormat;
        }


        // POST: Stock/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Route("/kho-hang/tao-moi/")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                #region SelectList
                var groups = dbContext.ProductGroups.Find(m => m.ParentCode.Equals(string.Empty)).ToList();
                ViewData["Groups"] = groups;

                var groupchilds = dbContext.ProductGroups.Find(m => m.ParentCode != string.Empty).ToList();
                ViewData["GroupChilds"] = groupchilds;

                var units = dbContext.Units.Find(m => true).ToList();
                ViewData["Units"] = units;

                var locations = dbContext.Locations.Find(m => true).ToList();
                ViewData["Locations"] = locations;
                #endregion

                #region Check Duplicate Code
                var lastProduct = dbContext.Products.Find(m => m.GroupDevide.Equals(product.GroupDevide)).SortByDescending(m => m.Id).Limit(1).FirstOrDefault();
                if (lastProduct != null)
                {
                    if (lastProduct.Code == product.Code)
                    {
                        var newCode = NewCode(product.GroupDevide);
                        ModelState.AddModelError("Code", "Mã hàng đã được tạo " + product.Code + ", đã cập nhật mã hàng mới." + newCode);
                        ViewData["newCode"] = newCode;
                        return View(product);
                    }
                }
                #endregion

                var userId = User.Identity.Name;
                if (Convert.ToInt32(Configuration.GetSection("Setting:Live").Value) == 0)
                {
                    userId = "administrator";
                }
                var now = DateTime.Now;
                if (string.IsNullOrEmpty(product.GroupDevide))
                {
                    product.GroupDevide = product.Group + "0";
                }
                product.CreatedBy = userId;
                product.UpdatedBy = userId;
                product.CheckedBy = userId;
                product.ApprovedBy = userId;
                if (product.Quantity < product.QuantityInStoreSafe)
                {
                    product.Status = "Min";
                }
                dbContext.Products.InsertOne(product);
                var newId = product.Code;

                CacheReLoad();

                #region Activities
                var objectId = newId;
                var objectName = product.Code;

                var activity = new TrackingUser
                {
                    UserId = userId,
                    Function = Constants.Collection.Products,
                    Action = "create",
                    Values = objectId,
                    ValuesDisplay = objectName,
                    Description = "create" + " " + Constants.Collection.Products + objectName,
                    Created = now,
                    Link = "/kho-hang?ma=" + objectId
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion

                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        [Route("/kho-hang/sua/")]
        // GET: Stock/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = dbContext.Products.Find(m => m.Code.ToLower().Equals(id.ToLower())).FirstOrDefault();
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Stock/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/kho-hang/sua/")]
        public ActionResult Edit(string id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                CacheReLoad();

                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Stock/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wamsStock = await _context.WamsStock
                .SingleOrDefaultAsync(m => m.Id == id);
            if (wamsStock == null)
            {
                return NotFound();
            }

            return View(wamsStock);
        }

        // POST: Stock/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var wamsStock = await _context.WamsStock.SingleOrDefaultAsync(m => m.Id == id);

            var userId = User.Identity.Name;
            var now = DateTime.Now;
            #region Activities
            var activity = new TrackingUser
            {
                UserId = userId,
                Function = Constants.Collection.Stores,
                Action = "delete",
                Values = wamsStock.Id.ToString(),
                ValuesDisplay = wamsStock.VStockId,
                Description = "Delete stock code: " + wamsStock.VStockId,
                Created = now,
                Link = "/notfound"
            };
            dbContext.TrackingUsers.InsertOne(activity);
            #endregion

            _context.WamsStock.Remove(wamsStock);
            await _context.SaveChangesAsync();

            CacheReLoad();

            return RedirectToAction(nameof(Index));
        }

        private bool WamsStockExists(int id)
        {
            return _context.WamsStock.Any(e => e.Id == id);
        }

        public ActionResult Hide()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateImages()
        {
            var now = DateTime.Now;
            var list = await _context.WamsStock.ToListAsync();
            foreach (var item in list)
            {
                var imageMongo = new Image
                {
                    ObjectId = item.Id.ToString(),
                    ObjectName = item.VStockId,
                    ObjectType = Constants.Collection.Stores,
                    Primary = true,
                    Position = 1,
                    ContentType = string.Empty,
                    ContentDisposition = string.Empty,
                    Length = 0,
                    OrginalFile = item.VStockId + ".jpg",
                    FileName = item.VStockId + ".jpg",
                    Storage = "/uploads/images",
                    Created = now,
                    CreatedBy = "1"
                };
                dbContext.Images.InsertOne(imageMongo);
            }

            // If we got this far, something failed, redisplay form
            return RedirectToAction("/");
        }
    }
}

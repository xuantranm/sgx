using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Driver;
using Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Models;
using Common.Utilities;
using ViewModels;
using System.Security.Claims;
using xcore.Models.TimeKeeper;
using MongoDB.Bson;
using Common.Enums;

namespace erp.Controllers
{
    // Move to api controller
    public class HelperController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        readonly IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public HelperController(IConfiguration configuration, IHostingEnvironment env, ILogger<HelperController> logger)
        {
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        [HttpGet]
        public JsonResult CheckLeaveDay(string user, DateTime from, DateTime to)
        {
            var userInformation = dbContext.Employees.Find(m => m.Id.Equals(user)).FirstOrDefault();
            var workdayStartTime = new TimeSpan(8, 0, 0);
            var workdayEndTime = new TimeSpan(16, 0, 0);
            double leaveDayAvailable = 0;
            // Get working day later
            if (userInformation != null)
            {
                leaveDayAvailable = userInformation.LeaveDayAvailable;
            }
            var numberBusiness = Utility.GetBussinessDaysBetweenTwoDates(from, to, workdayStartTime, workdayEndTime);
            if (leaveDayAvailable < numberBusiness)
            {
                return Json(new { result = false, message = "Không đủ ngày phép.", leave = 0 });
            }
            return Json(new { result = true, message = "Đủ ngày phép.", leave = numberBusiness });
        }

        [HttpGet]
        public JsonResult EmployeeAutocomplete(string term)
        {
            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!string.IsNullOrEmpty(term))
            {
                filter = filter & builder.Regex(i => i.Code, term);
            }
            #endregion

            #region Sort
            var sort = Builders<Employee>.Sort.Ascending(m => m.Code);
            #endregion

            var outputs = dbContext.Employees.Find(filter).Sort(sort).Limit(10).ToList();
            return Json(outputs);
        }

        #region Resolve Data
        [HttpGet]
        public JsonResult FullNameGenerate(string name)
        {
            var email = Utility.EmailConvert(name);
            var userName = email.Replace(Constants.MailExtension, string.Empty);
            var result = true;
            var suggest = string.Empty;

            // Check exist
            var exist = dbContext.Employees.Find(m => m.UserName.Equals(userName)).FirstOrDefault();
            if (exist != null)
            {
                result = false;
                suggest = userName + DateTime.Now.Year + ";" + userName + exist.Birthday.Year + ";" + userName + exist.AliasFullName.Split('-')[0];
            }

            return Json(new { userName, email, result, suggest});
        }
        #endregion

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult ErpCommon(string userId)
        {
            var isOwner = false;
            var ownerId = User.Identity.Name;
            if (string.IsNullOrEmpty(userId))
            {
                isOwner = true;
                userId = ownerId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { result = false });
                }
            }
            
            var userInformation = dbContext.Employees.Find(m => m.Id.Equals(userId)).First();

            var owner = isOwner ? userInformation : dbContext.Employees.Find(m => m.Id.Equals(ownerId)).First();

            var avatar = new Img { 
                Path = "images/placeholder",
                FileName = "120x120.png"
            };
            var cover = new Img
            {
                Path = "images/placeholder",
                FileName = "354x167.png"
            };
            if (owner.Images != null && owner.Images.Count > 0)
            {
                var avatarE = owner.Images.Where(m => m.Type.Equals((int)EImageSize.Avatar)).FirstOrDefault();
                if (avatarE != null)
                {
                    avatar = avatarE;
                }
                var coverE = owner.Images.Where(m => m.Type.Equals((int)EImageSize.Cover)).FirstOrDefault();
                if (coverE != null)
                {
                    cover = coverE;
                }
            }
            // notification
            var sortNotification = Builders<Notification>.Sort.Ascending(m => m.CreatedOn);
            var notifications = dbContext.Notifications.Find(m => m.Enable.Equals(true) && m.User.Equals(ownerId)).Sort(sortNotification).ToList();

            #region Fix no data
            if (string.IsNullOrEmpty(userInformation.AddressResident))
            {
                userInformation.AddressResident = Constants.MissTitle;
            }
            #endregion

            var trackingUsers = dbContext.TrackingUsers.Find(m => m.UserId.Equals(userId)).SortByDescending(m => m.Created).Limit(10).ToList();

            // Menu, Role
            
            var erpViewModel = new ErpViewModel()
            {
                OwnerInformation = owner,
                Avatar = avatar,
                Cover = cover,
                NotificationCount = notifications != null ? notifications.Count() : 0,
                UserInformation = userInformation,
                TrackingUser = trackingUsers
            };
            return Json(erpViewModel);
        }

        public JsonResult Activity()
        {
            var userId = User.Identity.Name;
            var activities = dbContext.TrackingUsers.Find(m => m.UserId.Equals(userId)).SortByDescending(m => m.Created).Limit(8).ToList();
            return Json(new { activities });
        }

        public JsonResult TimeKeeper(string location, string user)
        {
            #region Setting
            int dateBeginSalary = 25;
            #endregion

            var client = new MongoClient(Configuration.GetSection("TimeKeeperNMConnection:ConnectionString").Value);
            var database = client.GetDatabase(Configuration.GetSection("TimeKeeperNMConnection:DatabaseName").Value);
            var collection = database.GetCollection<AttLog>("AttLogs");

            var sort = Builders<AttLog>.Sort.Ascending(m => m.Date).Descending(m => m.Date);
            var filterBuilder = Builders<AttLog>.Filter;

            var today = DateTime.Now;
            // if today 28. => get 25->28
            var start = new DateTime(today.Year, today.Month, dateBeginSalary);
            // if today 06
            if (today.Day <= dateBeginSalary)
            {
                start = new DateTime(today.Year, today.Month, dateBeginSalary).AddMonths(-1);
            }
            var end = DateTime.Now;

            var filter = filterBuilder.Gte(x => x.Date, start) &
                                    filterBuilder.Lt(x => x.Date, end);

            var timers = collection.Find(filter).Sort(sort).ToList();
            return Json(new { timers });
        }

        [HttpGet]
        public JsonResult LogsSanPham(string code)
        {
            var logs = dbContext.ProductLogs.Find(m => m.Code.Equals(code)).SortByDescending(s => s.Id).ToList();
            return Json(new { logs });
        }

        public JsonResult StockStatus()
        {
            var results = new List<Status>();
            var types = dbContext.ProductGroups.Find(m=>true).ToList();
            //var stocksRecords = Convert.ToDecimal(_cache.GetString(Constants.Collection.Storess + Constants.CountCacheKey));

            foreach (var type in types)
            {
                //var records = Convert.ToDecimal(_cache.GetString(type.Id + "." + Constants.Collection.StoresTypes + Constants.CountCacheKey));
                //decimal percent = 0;
                //if (records > 0){
                //    percent = Math.Round((Convert.ToDecimal(records) / Convert.ToDecimal(stocksRecords)) * 100);
                //}
                //var stockstatus = new StockStatus
                //{
                //    Id = type.Id,
                //    Name = type.Name,
                //    Records = records,
                //    Percent = percent
                //};
                //results.Add(stockstatus);
            }

            return Json(new { stocksummary = results });
        }

        [HttpGet]
        public JsonResult DataGroup(string group)
        {
            var childs = new List<ProductGroup>();
            if (group == "null")
            {
                childs = dbContext.ProductGroups.Find(m => m.ParentCode != string.Empty).ToList();
            }
            else
            {
                childs = dbContext.ProductGroups.Find(m => m.ParentCode.Equals(group)).ToList();
            }
            var groupCode = group + "0";
            if (childs.Count() >0)
            {
                groupCode = childs.First().Code;
            }
            var newCode = NewCode(groupCode);
            return Json(new { groups = childs.OrderBy(m=>m.Code), newCode, groupCode });
        }

        public JsonResult DataChildGroup(string group)
        {
            var newCode = NewCode(group);
            return Json(new { newCode });
        }

        private string NewCode(string groupCode)
        {
            //var newCodeFormat = "001";
            //var lastProduct = dbContext.Products.Find(m => m.GroupDevide.Equals(groupCode)).SortByDescending(m => m.Id).Limit(1);
            //if (lastProduct != null)
            //{
            //    var lastCode = lastProduct.First().Code;
            //    var newCode = int.Parse(lastCode.Substring(3)) + 1;
            //    newCodeFormat = newCode.ToString().PadLeft(3, '0');
            //}

            //return groupCode + newCodeFormat;

            return "";
        }

        [HttpGet]
        public JsonResult LayPhieuYeuCau(string code)
        {
            var filter = Builders<Request>.Filter
                                .And(
                                    Builders<Request>.Filter.Eq(d => d.Status, Constants.Status.Open),
                                    Builders<Request>.Filter.ElemMatch(x => x.RequestItems, p => p.Code == code));
            var projection = Builders<Request>.Projection.Include("RequestItems.$").Include("Code");
            var results = dbContext.Requests.Find(filter).Project(projection).ToList();

            var requests = new List<Request>();
            foreach(var result in results)
            {
                //var id = result.GetElement("_id").Value;
                var requestcode = result.GetElement("Code").Value.AsString;
                var product  = result.GetElement("ProductYeuCaus").Value.AsBsonArray;
                foreach (var field in product)
                {
                    var productcode = field["Code"].AsString;
                    var productname = field["Name"].AsString;
                    var quantity = field["Quantity"].AsDecimal;
                    requests.Add(new Request
                    {
                        Code = requestcode,
                        RequestItems = new List<RequestItem>{
                        new RequestItem
                        {
                            Code = productcode,
                            Name = productname,
                            Quantity = quantity
                        }
                    }
                    });
                }
            }

            return Json(new { res = requests });


            //var phieuyeucaus = (from e in dbContext.Requests.AsQueryable()
            //                    where e.Status == Constants.Status.Open && e.ProductYeuCaus.Any(c => c.Code.Equals(code))
            //                    select e);

            //var product = phieuyeucaus.Select(c => c.ProductYeuCaus.Where(o => o.Code.Equals(code))).First();
            //return Json(new { res = phieuyeucaus, product = product });
        }


        [HttpGet]
        public JsonResult PhieuDatHang(string term)
        {
            var pes = dbContext.Orders.Find(m => m.Enable.Equals(true) & m.Status.Equals("open") & m.Code.Contains(term)).Limit(10).ToList();
            return Json(pes);
        }

        public JsonResult GetQuantityStore(string code)
        {
            var entity = dbContext.Products.Find(m => m.Enable.Equals(true) & m.Code.Equals(code)).FirstOrDefault();
            decimal quantity = 0;
            if (entity != null)
            {
                //quantity = entity.Quantity;
                quantity = 0;
            }
            return Json(quantity);
        }

        //[HttpGet]
        //// type: 1: stock code, 2: stock name
        //public JsonResult ProductAutoComplete(string term, int type)
        //{
        //    var outputs = new List<string>();
        //    var stocks = JsonConvert.DeserializeObject<IEnumerable<Products>>(_cache.GetString(Constants.Collection.Stores));
        //    if (type == 1)
        //    {
        //        outputs = stocks.Where(m => m.VStockId.Contains(term)).Select(m=>m.VStockId).Take(10).ToList();
        //    }
        //    else
        //    {
        //        outputs = stocks.Where(m => m.VStockName.Contains(term)).Select(m=>m.VStockName).Take(10).ToList();
        //    }
        //    if (outputs.Any()) return Json(outputs);
        //    var a = new List<string>();
        //    return Json(a);
        //}

        //[HttpGet]
        //// type: 1: stock code, 2: stock name
        //public JsonResult ProductInfomationAutoComplete(string term, int type)
        //{
        //    var outputs = new List<Products>();
        //    var stocks = JsonConvert.DeserializeObject<IEnumerable<Products>>(_cache.GetString(Constants.Collection.Stores));
        //    if (type == 1)
        //    {
        //        outputs = stocks.Where(m => m.VStockId.Contains(term)).Take(10).ToList();
        //    }
        //    else
        //    {
        //        outputs = stocks.Where(m => m.VStockName.Contains(term)).Take(10).ToList();
        //    }
        //    return Json(outputs);
        //}

        [HttpGet]
        // type: 1: stock code, 2: stock name
        public JsonResult Supplier(string term)
        {
            var outputs = dbContext.Suppliers.Find(m => m.Name.Contains(term)).Limit(10).ToList();
            return Json(outputs);
        }

        [HttpGet]
        // type: 1: stock code, 2: stock name
        public JsonResult Customer(string term)
        {
            var outputs = dbContext.Customers.Find(m => m.Name.Contains(term)).Limit(10).ToList();
            return Json(outputs);
        }

        [HttpGet]
        // type: 1: stock code, 2: stock name
        public JsonResult Truck(string term)
        {
            var outputs = dbContext.Trucks.Find(m => m.Code.Contains(term)).Limit(10).ToList();
            return Json(outputs);
        }

        [HttpGet]
        // type: 1: stock code, 2: stock name
        public JsonResult ThongTinSanPhamAutoComplete(string term, int type)
        {
            var outputs = dbContext.Products.Find(m => m.Enable.Equals(true) & m.Code.Contains(term)).Limit(10).ToList();
            return Json(outputs);
        }

        //[HttpGet]
        //// type: 1: stock code, 2: stock name
        //public JsonResult ProductInfomation(string code)
        //{
        //    var stocks = JsonConvert.DeserializeObject<IEnumerable<Products>>(_cache.GetString(Constants.Collection.Storess));
        //    return Json(stocks.FirstOrDefault(m => m.VStockId.Contains(code)));
        //}

        //[HttpGet]
        //// type: 1: stock code, 2: stock name
        //public JsonResult NewProductCode(int type)
        //{
        //    // parameter 1: 1: value id stock type default consumable
        //    // parameter 2: 0: not category
        //    var key = type + Constants.MaxCode;
        //    var cacheMaxCode = _cache.GetString(key);
        //    var maxCode = JsonConvert.DeserializeObject<MaxProductCode>(cacheMaxCode);
        //    return Json(maxCode);
        //}
    }
}
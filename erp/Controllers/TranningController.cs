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
    [Route("tn/")]
    public class TranningController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public TranningController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, IEmailSender emailSender,
            ISmsSender smsSender, ILogger<TranningController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        // Apply search,....
        [Route("dao-tao/")]
        public async Task<IActionResult> Index(string search, string type, int? page, int? size, string sortField, string sort)
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

            //name = "SOFAR";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Nghe thử",
            //    Type = "nhac",
            //    Link = Constants.Link.Youtube + "yM_lGn9KC9k",
            //});

            //name = "PHÍA SAU EM";
            //dbContext.Trainnings.InsertOne(new Trainning
            //{
            //    Name = name,
            //    Alias = Utility.AliasConvert(name),
            //    Description = "Nghe thử",
            //    Type = "nhac",
            //    Link = Constants.Link.Youtube + "LklFoy_a3bA",
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

        [HttpGet]
        [Route("dao-tao/chi-tiet/{id}")]
        public JsonResult Item(string id)
        {
            var item = dbContext.Trainnings.Find(m => m.Id.Equals(id)).First();
            return Json(item);
        }

        [HttpPost]
        [Route("tao-moi/")]
        public ActionResult Create(Trainning entity)
        {
            var userId = User.Identity.Name;
            try
            {
                if (CheckExist(entity))
                {
                    dbContext.Trainnings.InsertOne(entity);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = "Đào tạo",
                        Action = Constants.Action.Create,
                        Value = entity.Alias,
                        Content = entity.Alias + Constants.Flag + entity.Type
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Tạo mới thành công." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Name + " đã tồn tại. Đặt tên khác hoặc liên hệ IT hỗ trợ." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("chinh-sua/")]
        public ActionResult Edit(Trainning entity)
        {
            var userId = User.Identity.Name;
            try
            {
                if (CheckUpdate(entity))
                {
                    entity.Timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    dbContext.Trainnings.ReplaceOne(m => m.Id == entity.Id, entity);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = "Đào tạo",
                        Action = Constants.Action.Edit,
                        Value = entity.Name,
                        Content = entity.Name + Constants.Flag + entity.Type,
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Cập nhật thành công." });
                    //return RedirectToAction("Index", "Trainning");
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Name + " đã thay đổi nội dung bởi 1 người khác. Tải lại trang hoặc thoát." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        [Route("an/")]
        public ActionResult Disable(Trainning entity)
        {
            var userId = User.Identity.Name;
            try
            {
                // TODO: Add disable logic here
                if (CheckDisable(entity))
                {
                    var filter = Builders<Trainning>.Filter.Eq(d => d.Id, entity.Id);
                    var update = Builders<Trainning>.Update
                                    .Set(c => c.Enable, false);
                    dbContext.Trainnings.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = "Đào tạo",
                        Action = Constants.Action.Disable,
                        Value = entity.Name,
                        Content = entity.Name + Constants.Flag + entity.Type
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Ẩn thành công." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Name + " được chỉnh sửa bởi người khác." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("khoi-phuc/")]
        public ActionResult Active(Trainning entity)
        {
            var userId = User.Identity.Name;
            try
            {
                // TODO: Add disable logic here
                if (CheckActive(entity))
                {
                    var filter = Builders<Trainning>.Filter.Eq(d => d.Id, entity.Id);
                    var update = Builders<Trainning>.Update
                                    .Set(c => c.Enable, true);
                    dbContext.Trainnings.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = "Đào tạo",
                        Action = Constants.Action.Active,
                        Value = entity.Name,
                        Content = entity.Name + Constants.Flag + entity.Type
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = entity.Name + " khôi phục thành công." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Name + " đã tồn tại." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("xoa/")]
        public ActionResult Delete(Trainning entity)
        {
            var userId = User.Identity.Name;
            try
            {
                // TODO: Add disable logic here
                if (CheckDelete(entity))
                {
                    dbContext.Trainnings.DeleteOne(m => m.Id == entity.Id);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = "Đào tạo",
                        Action = Constants.Action.Delete,
                        Value = entity.Name,
                        Content = entity.Name + Constants.Flag + entity.Type
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Xóa thành công." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Name + " chỉnh sửa bởi người khác." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        public bool CheckExist(Trainning entity)
        {
            return dbContext.Trainnings.CountDocuments(m => m.Enable.Equals(true) && m.Name.Equals(entity.Name)) > 0 ? false : true;
        }

        public bool CheckUpdate(Trainning entity)
        {
            var db = dbContext.Trainnings.Find(m => m.Enable.Equals(true) && m.Name.Equals(entity.Name)).First();
            if (db.Name != entity.Name)
            {
                if (CheckExist(entity))
                {
                    return db.Timestamp == entity.Timestamp ? true : false;
                }
            }
            return db.Timestamp == entity.Timestamp ? true : false;
        }

        public bool CheckDisable(Trainning entity)
        {
            return entity.Usage > 0 ? false : true;
        }

        public bool CheckActive(Trainning entity)
        {
            return dbContext.Trainnings.CountDocuments(m => m.Enable.Equals(true) && m.Name.Equals(entity.Name)) > 0 ? false : true;
        }

        public bool CheckDelete(Trainning entity)
        {
            if (entity.NoDelete)
            {
                return false;
            }
            return entity.Usage > 0 ? false : true;
        }
    }
}
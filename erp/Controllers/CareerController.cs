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

namespace erp.Controllers
{
    [Authorize]
    [Route("jb")]
    public class CareerController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public CareerController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<CareerController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }


        public ActionResult Index(string name)
        {
            var sort = Builders<Setting>.Sort.Descending(m=>m.ModifiedDate);
            var settings = dbContext.Settings.Find(m => m.Enable.Equals(true)).Sort(sort).ToList();

            var settingsDisable = dbContext.Settings.Find(m => m.Enable.Equals(false)).Sort(sort).ToList();
            var viewModel = new SettingViewModel
            {
                Settings = settings,
                SettingsDisable = settingsDisable
            };
            return View(viewModel);
        }

        [HttpGet]
        public JsonResult Item(string id)
        {
            var item = dbContext.Settings.Find(m => m.Id.Equals(id)).First();
            return Json(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SettingViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Setting;
            try
            {
                if (CheckExist(entity))
                {
                    dbContext.Settings.InsertOne(entity);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.Settings,
                        Action = Constants.Action.Create,
                        Value = entity.Key,
                        Content = entity.Key + Constants.Flag + entity.Content
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Add new successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Key + " is exist. Try another key or contact IT." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SettingViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Setting;
            try
            {
                if (CheckUpdate(entity))
                {
                    entity.Timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    dbContext.Settings.ReplaceOne(m => m.Id == entity.Id, entity);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.Settings,
                        Action = Constants.Action.Edit,
                        Value = entity.Key,
                        Content = entity.Key + Constants.Flag + entity.Content,
                        //Link = "/stg/" + entity.Key
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Update successfull." });
                    //return RedirectToAction("Index", "Setting");
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Key + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Disable(SettingViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Setting;
            try
            {
                // TODO: Add disable logic here
                if (CheckDisable(entity))
                {
                    var filter = Builders<Setting>.Filter.Eq(d => d.Key, entity.Key);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<Setting>.Update
                                    .Set(c => c.Enable, false);
                    dbContext.Settings.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.Settings,
                        Action = Constants.Action.Disable,
                        Value = entity.Key,
                        Content = entity.Key + Constants.Flag + entity.Content
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Disable successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Key + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Active(SettingViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Setting;
            try
            {
                // TODO: Add disable logic here
                if (CheckActive(entity))
                {
                    var filter = Builders<Setting>.Filter.Eq(d => d.Key, entity.Key);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<Setting>.Update
                                    .Set(c => c.Enable, true);
                    dbContext.Settings.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.Settings,
                        Action = Constants.Action.Active,
                        Value = entity.Key,
                        Content = entity.Key + Constants.Flag + entity.Content
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = entity.Key + " active successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Key + " is exist." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(SettingViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Setting;
            try
            {
                // TODO: Add disable logic here
                if (CheckDelete(entity))
                {
                    dbContext.Settings.DeleteOne(m => m.Id == entity.Id);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.Settings,
                        Action = Constants.Action.Delete,
                        Value = entity.Key,
                        Content = entity.Key + Constants.Flag + entity.Content
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Delete successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Key + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        public bool CheckExist(Setting entity)
        {
            return dbContext.Settings.Find(m => m.Enable.Equals(true) && m.Key.Equals(entity.Key)).Count() > 0 ? false : true;
        }

        public bool CheckUpdate(Setting entity)
        {
            var db = dbContext.Settings.Find(m => m.Enable.Equals(true) && m.Key.Equals(entity.Key)).First();
            if (db.Key != entity.Key)
            {
                if (CheckExist(entity))
                {
                    return db.Timestamp == entity.Timestamp ? true : false;
                }
            }
            return db.Timestamp == entity.Timestamp ? true: false;
        }

        public bool CheckDisable(Setting entity)
        {
            return entity.Usage > 0 ? false : true;
        }

        public bool CheckActive(Setting entity)
        {
            return dbContext.Settings.Find(m => m.Enable.Equals(true) && m.Key.Equals(entity.Key)).Count() > 0 ? false : true;
        }

        public bool CheckDelete(Setting entity)
        {
            if (entity.NoDelete)
            {
                return false;
            }
            return entity.Usage > 0 ? false : true;
        }
    }
}
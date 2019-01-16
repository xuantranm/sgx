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
    public class HolidayController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public HolidayController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<HolidayController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        public ActionResult Index(int year, int month, DateTime date)
        {
            // Do later

            var datas = dbContext.Holidays.Find(m => m.Enable.Equals(true)).SortByDescending(m=>m.Date).ToList();

            var viewModel = new HolidayViewModel
            {
                Holidays = datas
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(HolidayViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Holiday;
            try
            {
                if (CheckExist(entity))
                {
                    dbContext.Holidays.InsertOne(entity);

                    #region Activities
                    //var activity = new TrackingUser
                    //{
                    //    UserId = userId,
                    //    Function = Constants.Collection.BHYTHospitals,
                    //    Action = Constants.Action.Create,
                    //    Value = entity.Code,
                    //    Content = entity.Code + Constants.Flag + entity.Name
                    //};
                    //dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Add new successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Date + " is exist. Try another key or contact IT." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(HolidayViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Holiday;
            try
            {
                if (CheckUpdate(entity))
                {
                    entity.Timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    dbContext.Holidays.ReplaceOne(m => m.Id == entity.Id, entity);

                    #region Activities
                    //var activity = new TrackingUser
                    //{
                    //    UserId = userId,
                    //    Function = Constants.Collection.BHYTHospitals,
                    //    Action = Constants.Action.Edit,
                    //    Value = entity.Code,
                    //    Content = entity.Code + Constants.Flag + entity.Name,
                    //    //Link = "/stg/" + entity.Key
                    //};
                    //dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Update successfull." });
                    //return RedirectToAction("Index", "BHYTHospital");
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Date + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Disable(HolidayViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Holiday;
            try
            {
                // TODO: Add disable logic here
                if (CheckDisable(entity))
                {
                    var filter = Builders<Holiday>.Filter.Eq(d => d.Date, entity.Date);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<Holiday>.Update
                                    .Set(c => c.Enable, false);
                    dbContext.Holidays.UpdateOne(filter, update);

                    #region Activities
                    //var activity = new TrackingUser
                    //{
                    //    UserId = userId,
                    //    Function = Constants.Collection.BHYTHospitals,
                    //    Action = Constants.Action.Disable,
                    //    Value = entity.Code,
                    //    Content = entity.Code + Constants.Flag + entity.Name
                    //};
                    //dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Disable successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Active(HolidayViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Holiday;
            try
            {
                // TODO: Add disable logic here
                if (CheckActive(entity))
                {
                    var filter = Builders<Holiday>.Filter.Eq(d => d.Date, entity.Date);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<Holiday>.Update
                                    .Set(c => c.Enable, true);
                    dbContext.Holidays.UpdateOne(filter, update);

                    #region Activities
                    //var activity = new TrackingUser
                    //{
                    //    UserId = userId,
                    //    Function = Constants.Collection.BHYTHospitals,
                    //    Action = Constants.Action.Active,
                    //    Value = entity.Date,
                    //    Content = entity.Date + Constants.Flag + entity.Name
                    //};
                    //dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "active successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = "Date is exist." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(HolidayViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Holiday;
            try
            {
                // TODO: Add disable logic here
                if (CheckDelete(entity))
                {
                    dbContext.BHYTHospitals.DeleteOne(m => m.Id == entity.Id);

                    #region Activities
                    //var activity = new TrackingUser
                    //{
                    //    UserId = userId,
                    //    Function = Constants.Collection.BHYTHospitals,
                    //    Action = Constants.Action.Delete,
                    //    Value = entity.Code,
                    //    Content = entity.Code + Constants.Flag + entity.Name
                    //};
                    //dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Delete successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Date + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        public bool CheckExist(Holiday entity)
        {
            return dbContext.Holidays.CountDocuments(m => m.Enable.Equals(true) && m.Date.Equals(entity.Date)) > 0 ? false : true;
        }

        public bool CheckUpdate(Holiday entity)
        {
            var db = dbContext.Holidays.Find(m => m.Enable.Equals(true) && m.Date.Equals(entity.Date)).First();
            if (db.Date != entity.Date)
            {
                if (CheckExist(entity))
                {
                    return db.Timestamp == entity.Timestamp ? true : false;
                }
            }
            return db.Timestamp == entity.Timestamp ? true: false;
        }

        public bool CheckDisable(Holiday entity)
        {
            return entity.Usage > 0 ? false : true;
        }

        public bool CheckActive(Holiday entity)
        {
            return dbContext.BHYTHospitals.CountDocuments(m => m.Enable.Equals(true) && m.Code.Equals(entity.Date)) > 0 ? false : true;
        }

        public bool CheckDelete(Holiday entity)
        {
            if (entity.NoDelete)
            {
                return false;
            }
            return entity.Usage > 0 ? false : true;
        }
    }
}
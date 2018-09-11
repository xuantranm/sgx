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
    public class HospitalController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public HospitalController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<HospitalController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        public ActionResult Index(string name)
        {
            var sort = Builders<BHYTHospital>.Sort.Descending(m=>m.Name);
            var Hospitals = dbContext.BHYTHospitals.Find(m => m.Enable.Equals(true)).Sort(sort).ToList();

            var viewModel = new HospitalViewModel
            {
                Hospitals = Hospitals
            };
            return View(viewModel);
        }

        [HttpGet]
        public JsonResult Item(string id)
        {
            var item = dbContext.BHYTHospitals.Find(m => m.Id.Equals(id)).First();
            return Json(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(HospitalViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Hospital;
            try
            {
                if (CheckExist(entity))
                {
                    dbContext.BHYTHospitals.InsertOne(entity);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.BHYTHospitals,
                        Action = Constants.Action.Create,
                        Value = entity.Code,
                        Description = entity.Code + Constants.Flag + entity.Name
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Add new successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Code + " is exist. Try another key or contact IT." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(HospitalViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Hospital;
            try
            {
                if (CheckUpdate(entity))
                {
                    entity.Timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    dbContext.BHYTHospitals.ReplaceOne(m => m.Id == entity.Id, entity);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.BHYTHospitals,
                        Action = Constants.Action.Edit,
                        Value = entity.Code,
                        Description = entity.Code + Constants.Flag + entity.Name,
                        //Link = "/stg/" + entity.Key
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Update successfull." });
                    //return RedirectToAction("Index", "BHYTHospital");
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Code + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Disable(HospitalViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Hospital;
            try
            {
                // TODO: Add disable logic here
                if (CheckDisable(entity))
                {
                    var filter = Builders<BHYTHospital>.Filter.Eq(d => d.Code, entity.Code);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<BHYTHospital>.Update
                                    .Set(c => c.Enable, false);
                    dbContext.BHYTHospitals.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.BHYTHospitals,
                        Action = Constants.Action.Disable,
                        Value = entity.Code,
                        Description = entity.Code + Constants.Flag + entity.Name
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Disable successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Code + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Active(HospitalViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Hospital;
            try
            {
                // TODO: Add disable logic here
                if (CheckActive(entity))
                {
                    var filter = Builders<BHYTHospital>.Filter.Eq(d => d.Code, entity.Code);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<BHYTHospital>.Update
                                    .Set(c => c.Enable, true);
                    dbContext.BHYTHospitals.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.BHYTHospitals,
                        Action = Constants.Action.Active,
                        Value = entity.Code,
                        Description = entity.Code + Constants.Flag + entity.Name
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = entity.Code + " active successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Code + " is exist." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(HospitalViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Hospital;
            try
            {
                // TODO: Add disable logic here
                if (CheckDelete(entity))
                {
                    dbContext.BHYTHospitals.DeleteOne(m => m.Id == entity.Id);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.BHYTHospitals,
                        Action = Constants.Action.Delete,
                        Value = entity.Code,
                        Description = entity.Code + Constants.Flag + entity.Name
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Delete successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Code + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        public bool CheckExist(BHYTHospital entity)
        {
            return dbContext.BHYTHospitals.Find(m => m.Enable.Equals(true) && m.Code.Equals(entity.Code)).Count() > 0 ? false : true;
        }

        public bool CheckUpdate(BHYTHospital entity)
        {
            var db = dbContext.BHYTHospitals.Find(m => m.Enable.Equals(true) && m.Code.Equals(entity.Code)).First();
            if (db.Code != entity.Code)
            {
                if (CheckExist(entity))
                {
                    return db.Timestamp == entity.Timestamp ? true : false;
                }
            }
            return db.Timestamp == entity.Timestamp ? true: false;
        }

        public bool CheckDisable(BHYTHospital entity)
        {
            return entity.Usage > 0 ? false : true;
        }

        public bool CheckActive(BHYTHospital entity)
        {
            return dbContext.BHYTHospitals.Find(m => m.Enable.Equals(true) && m.Code.Equals(entity.Code)).Count() > 0 ? false : true;
        }

        public bool CheckDelete(BHYTHospital entity)
        {
            if (entity.NoDelete)
            {
                return false;
            }
            return entity.Usage > 0 ? false : true;
        }
    }
}
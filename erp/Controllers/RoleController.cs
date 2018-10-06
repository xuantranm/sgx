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
    [Route(Constants.LinkRole.Role)]
    public class RoleController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public RoleController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<RoleController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        [Route(Constants.LinkRole.Index)]
        public ActionResult Index(string name)
        {
            var sort = Builders<Role>.Sort.Descending(m=>m.UpdatedOn);
            var Roles = dbContext.Roles.Find(m => m.Enable.Equals(true)).Sort(sort).ToList();

            var viewModel = new RoleViewModel
            {
                Roles = Roles
            };
            return View(viewModel);
        }

        [HttpGet]
        [Route(Constants.LinkRole.Detail)]
        public JsonResult Item(string id)
        {
            var item = dbContext.Roles.Find(m => m.Id.Equals(id)).First();
            return Json(item);
        }


        [HttpPost]
        [Route(Constants.LinkRole.Create)]
        public ActionResult Create(RoleViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Role;
            entity.Alias = Utility.AliasConvert(entity.Object);

            try
            {
                if (CheckExistRole(entity))
                {
                    dbContext.Roles.InsertOne(entity);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.Roles,
                        Action = Constants.Action.Create,
                        Value = entity.Alias,
                        Content = entity.Alias + Constants.Flag + entity.Description
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Add new successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Alias + " is exist. Try another key or contact IT." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route(Constants.LinkRole.Edit)]
        public ActionResult Edit(RoleViewModel viewModel)
        {
            var login = User.Identity.Name;
            var now = DateTime.Now;
            var entity = viewModel.Role;
            try
            {
                if (CheckUpdateRole(entity))
                {
                    entity.Timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                    var filter = Builders<Role>.Filter.Eq(d => d.Id, entity.Id);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<Role>.Update
                                    .Set(m => m.UpdatedBy, login)
                                    .Set(m => m.UpdatedOn, now)
                                    .Set(c => c.Description, entity.Description)
                                    .Set(c => c.Duration, entity.Duration);
                    dbContext.Roles.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = login,
                        Function = Constants.Collection.Roles,
                        Action = Constants.Action.Edit,
                        Value = entity.Alias,
                        Content = entity.Alias + Constants.Flag + entity.Description,
                        //Link = "/stg/" + entity.Alias
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Update successfull." });
                    //return RedirectToAction("Index", "Role");
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Alias + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route(Constants.LinkRole.Disable)]
        public ActionResult Disable(Role model)
        {
            var userId = User.Identity.Name;
            var entity = model;
            try
            {
                // TODO: Add disable logic here
                if (CheckDisable(entity))
                {
                    var filter = Builders<Role>.Filter.Eq(d => d.Alias, entity.Alias);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<Role>.Update
                                    .Set(c => c.Enable, false);
                    dbContext.Roles.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.Roles,
                        Action = Constants.Action.Disable,
                        Value = entity.Alias,
                        Content = entity.Alias + Constants.Flag + entity.Description
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Disable successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Alias + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route(Constants.LinkRole.Active)]
        public ActionResult Active(Role model)
        {
            var userId = User.Identity.Name;
            var entity = model;
            try
            {
                // TODO: Add disable logic here
                if (CheckActive(entity))
                {
                    var filter = Builders<Role>.Filter.Eq(d => d.Alias, entity.Alias);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<Role>.Update
                                    .Set(c => c.Enable, true);
                    dbContext.Roles.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.Roles,
                        Action = Constants.Action.Active,
                        Value = entity.Alias,
                        Content = entity.Alias + Constants.Flag + entity.Description
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = entity.Alias + " active successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Alias + " is exist." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route(Constants.LinkRole.Delete)]
        public ActionResult Delete(RoleViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Role;
            try
            {
                // TODO: Add disable logic here
                if (CheckDelete(entity))
                {
                    dbContext.Roles.DeleteOne(m => m.Id == entity.Id);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.Roles,
                        Action = Constants.Action.Delete,
                        Value = entity.Alias,
                        Content = entity.Alias + Constants.Flag + entity.Description
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Delete successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.Alias + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        public bool CheckExistRole(Role entity)
        {
            return dbContext.Roles.CountDocuments(m => m.Enable.Equals(true) && m.Alias.Equals(entity.Alias)) > 0 ? false : true;
        }

        public bool CheckUpdateRole(Role entity)
        {
            var db = dbContext.Roles.Find(m => m.Enable.Equals(true) && m.Id.Equals(entity.Id)).First();
            //if (db.Alias != entity.Alias)
            //{
            //    if (CheckExistRole(entity))
            //    {
            //        return db.Timestamp == entity.Timestamp ? true : false;
            //    }
            //}
            return db.Timestamp == entity.Timestamp ? true: false;
        }

        public bool CheckDisable(Role entity)
        {
            return entity.Usage > 0 ? false : true;
        }

        public bool CheckActive(Role entity)
        {
            return dbContext.Roles.CountDocuments(m => m.Enable.Equals(true) && m.Alias.Equals(entity.Alias)) > 0 ? false : true;
        }

        public bool CheckDelete(Role entity)
        {
            if (entity.NoDelete)
            {
                return false;
            }
            return entity.Usage > 0 ? false : true;
        }
    }
}
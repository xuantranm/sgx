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
    [Route("r-u/")]
    public class RoleUserController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public RoleUserController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<RoleUserController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        [Route("phan-quyen/")]
        public ActionResult Index(string name)
        {
            #region Dropdownlist
            var parts = dbContext.Parts.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Parts"] = parts;
            var departments = dbContext.Departments.Find(m => m.Enable.Equals(true) && !m.Name.Equals(Constants.System.department)).ToList();
            ViewData["Departments"] = departments;
            var titles = dbContext.Titles.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Titles"] = titles;
            #endregion

            var sort = Builders<RoleUser>.Sort.Ascending(m => m.User).Descending(m => m.UpdatedOn);
            var RoleUsers = dbContext.RoleUsers.Find(m => m.Enable.Equals(true)).Sort(sort).ToList();

            // JOIN
            //var personcollection = dbContext.Employees;
            //var aggregate = personcollection.Aggregate()
            //            .Group(new BsonDocument { { "_id", "$Address.Street" }, { "sum", new BsonDocument("$sum", "$Income") } });
            //var results = await aggregate.ToListAsync();
            //END

            var viewModel = new RoleUserViewModel
            {
                RoleUsers = RoleUsers
            };
            return View(viewModel);
        }

        [HttpGet]
        [Route("item/")]
        public JsonResult Item(string id)
        {
            var item = dbContext.RoleUsers.Find(m => m.Id.Equals(id)).First();
            return Json(item);
        }

        [Route("phan-quyen/tao-moi/")]
        public ActionResult Create()
        {
            #region Dropdownlist
            var parts = dbContext.Parts.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Parts"] = parts;
            var departments = dbContext.Departments.Find(m => m.Enable.Equals(true) && !m.Name.Equals(Constants.System.department)).ToList();
            ViewData["Departments"] = departments;
            var titles = dbContext.Titles.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Titles"] = titles;
            #endregion

            var roles = dbContext.Roles.Find(m => m.Enable.Equals(true)).ToList();
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && !m.UserName.Equals(Constants.System.account)).ToList();

            var viewModel = new RoleUserDataViewModel()
            {
                Employees = employees,
                Roles = roles
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route("phan-quyen/tao-moi/")]
        public ActionResult Create(RoleUserViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = new RoleUser();
            bool result = false;
            foreach (var roleUser in viewModel.RoleUsers)
            {
                entity = viewModel.RoleUser;
                entity.Role = roleUser.Role;
                entity.Action = roleUser.Action;
                entity.Start = roleUser.Start;
                entity.Expired = roleUser.Expired;

                if (CheckExistRoleUser(entity))
                {
                    entity.Id = null;
                    dbContext.RoleUsers.InsertOne(entity);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.RoleUsers,
                        Action = Constants.Action.Create,
                        Value = entity.Role,
                        Description = entity.User + Constants.Flag + entity.Role
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    result = true;
                }
                else
                {
                    result = false;
                }
            }

            return result ? Json(new { entity, result = true, message = "Add new successfull." }) : Json(new { entity, result = false, message = entity.Role + " is exist. Try another key or contact IT." });
        }

        [HttpPost]
        [Route("edit/")]
        public ActionResult Edit(RoleUserViewModel viewModel)
        {
            var login = User.Identity.Name;
            var now = DateTime.Now;
            var entity = viewModel.RoleUser;
            try
            {
                if (CheckUpdateRoleUser(entity))
                {
                    entity.Timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                    var filter = Builders<RoleUser>.Filter.Eq(d => d.Id, entity.Id);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<RoleUser>.Update
                                    .Set(m => m.UpdatedBy, login)
                                    .Set(m => m.UpdatedOn, now);
                    dbContext.RoleUsers.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = login,
                        Function = Constants.Collection.RoleUsers,
                        Action = Constants.Action.Edit,
                        Value = entity.Role,
                        Description = entity.User + Constants.Flag + entity.Role
                        //Link = "/stg/" + entity.Alias
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Update successfull." });
                    //return RedirectToAction("Index", "RoleUser");
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.User + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("disable/")]
        public ActionResult Disable(RoleUser model)
        {
            var userId = User.Identity.Name;
            var entity = model;
            try
            {
                // TODO: Add disable logic here
                if (CheckDisable(entity))
                {
                    var filter = Builders<RoleUser>.Filter.Eq(d => d.Id, entity.Id);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<RoleUser>.Update
                                    .Set(c => c.Enable, false);
                    dbContext.RoleUsers.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.RoleUsers,
                        Action = Constants.Action.Disable,
                        Value = entity.Role,
                        Description = entity.User + Constants.Flag + entity.Role
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Disable successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.User + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("active/")]
        public ActionResult Active(RoleUser model)
        {
            var userId = User.Identity.Name;
            var entity = model;
            try
            {
                // TODO: Add disable logic here
                if (CheckActive(entity))
                {
                    var filter = Builders<RoleUser>.Filter.Eq(d => d.Id, entity.Id);
                    // Update rule later. Current rule: dat hang => update request dat hang. No check quantity full or missing.
                    var update = Builders<RoleUser>.Update
                                    .Set(c => c.Enable, true);
                    dbContext.RoleUsers.UpdateOne(filter, update);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.RoleUsers,
                        Action = Constants.Action.Active,
                        Value = entity.Role,
                        Description = entity.User + Constants.Flag + entity.Role
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = entity.User + " active successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.User + " is exist." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("delete/")]
        public ActionResult Delete(RoleUserViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.RoleUser;
            try
            {
                // TODO: Add disable logic here
                if (CheckDelete(entity))
                {
                    dbContext.RoleUsers.DeleteOne(m => m.Id == entity.Id);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.RoleUsers,
                        Action = Constants.Action.Delete,
                        Value = entity.Role,
                        Description = entity.User + Constants.Flag + entity.Role
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Delete successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.User + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        public bool CheckExistRoleUser(RoleUser entity)
        {
            return dbContext.RoleUsers.Find(m => m.Enable.Equals(true) && m.User.Equals(entity.User) && m.Role.Equals(entity.Role)).Count() > 0 ? false : true;
        }

        public bool CheckUpdateRoleUser(RoleUser entity)
        {
            var db = dbContext.RoleUsers.Find(m => m.Enable.Equals(true) && m.Id.Equals(entity.Id)).First();
            //if (db.Alias != entity.Alias)
            //{
            //    if (CheckExistRoleUser(entity))
            //    {
            //        return db.Timestamp == entity.Timestamp ? true : false;
            //    }
            //}
            return db.Timestamp == entity.Timestamp ? true : false;
        }

        public bool CheckDisable(RoleUser entity)
        {
            return entity.Usage > 0 ? false : true;
        }

        public bool CheckActive(RoleUser entity)
        {
            return dbContext.RoleUsers.Find(m => m.Enable.Equals(true) && m.User.Equals(entity.User) && m.Role.Equals(entity.Role)).Count() > 0 ? false : true;
        }

        public bool CheckDelete(RoleUser entity)
        {
            if (entity.NoDelete)
            {
                return false;
            }
            return entity.Usage > 0 ? false : true;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tribat.Data;
using Tribat.Models;
using Tribat.Models.Tribats;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Tribat.Common.Utilities;

namespace Tribat.Controllers
{
    public class TruckController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();

        private readonly IDistributedCache _cache;

        private readonly string key;

        public TruckController(IDistributedCache cache)
        {
            _cache = cache;
            key = Constants.Collection.KinhDoanh;
        }

        #region Kiem Xe
        [Route("kiem-xe/")]
        public ActionResult KiemXe()
        {
            var data = JsonConvert.DeserializeObject<IEnumerable<Truck>>(_cache.GetString(key));
            return View(data);
        }
        #endregion
        private string CacheInit()
        {
            var values = _cache.GetString(key);
            if (string.IsNullOrEmpty(values))
            {
                var itemsFromjSON = dbContext.Settings.Find(m => true).ToList();
                values = JsonConvert.SerializeObject(itemsFromjSON);
                _cache.SetString(key, values);
            }
            return values;
        }

        private void CacheReLoad()
        {
            _cache.Remove(key);
            var values = _cache.GetString(key);
            if (string.IsNullOrEmpty(values))
            {
                var itemsFromjSON = dbContext.Settings.Find(m => true).ToList();
                values = JsonConvert.SerializeObject(itemsFromjSON);
                _cache.SetString(key, values);
            }
        }

        // GET: Setting
        public ActionResult Index()
        {
            var values = CacheInit();

            var data = JsonConvert.DeserializeObject<IEnumerable<Setting>>(values);
            return View(data);
        }

        // GET: Setting/Details/5
        public ActionResult Details(string id)
        {
            var values = CacheInit();

            var data = JsonConvert.DeserializeObject<IEnumerable<Setting>>(values);
            var entity = data.Where(m => m.Id == id).FirstOrDefault();
            return View(entity);
        }

        // GET: Setting/Create
        public ActionResult Create()
        {
            return View();
        }

        // Setting: Setting/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Setting entity)
        {
            try
            {
                var userId = User.Identity.Name;
                var now = DateTime.Now;
                // TODO: Add insert logic here
                //entity.Id = Guid.NewGuid();
                dbContext.Settings.InsertOne(entity);
                #region Activities
                var activity = new TrackingUser
                {
                    UserId = userId,
                    Function = Constants.Collection.Settings,
                    Action = "create",
                    Values = entity.Id,
                    ValuesDisplay = entity.Key,
                    Description = "Create new setting key: " + entity.Key + " with content: " + entity.Content,
                    Created = now,
                    Link = "/setting/details/" + entity.Id
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion

                CacheReLoad();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Setting/Edit/5
        public ActionResult Edit(string id)
        {
            var values = CacheInit();

            var data = JsonConvert.DeserializeObject<IEnumerable<Setting>>(values);
            var entity = data.Where(m => m.Id == id).FirstOrDefault();
            return View(entity);
        }

        // Setting: Setting/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Setting entity)
        {
            try
            {
                var userId = User.Identity.Name;
                var now = DateTime.Now;
                // TODO: Add update logic here
                // You can use the UpdateOne to get higher performance if you need.
                dbContext.Settings.ReplaceOne(m => m.Id == entity.Id, entity);
                #region Activities
                var activity = new TrackingUser
                {
                    UserId = userId,
                    Function = Constants.Collection.Settings,
                    Action = "edit",
                    Values = entity.Id,
                    ValuesDisplay = entity.Key,
                    Description = "Update setting key: " + entity.Key + " with content: " + entity.Content,
                    Created = now,
                    Link = "/setting/details/" + entity.Id
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion

                CacheReLoad();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Setting/Delete/5
        public ActionResult Delete(string id)
        {
            var values = CacheInit();

            var data = JsonConvert.DeserializeObject<IEnumerable<Setting>>(values);
            var entity = data.Where(m => m.Id == id).FirstOrDefault();
            return View(entity);
        }

        // Setting: Setting/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, Setting entity)
        {
            try
            {
                var userId = User.Identity.Name;
                var now = DateTime.Now;
                // TODO: Add delete logic here
                dbContext.Settings.DeleteOne(m => m.Id == id);

                #region Activities
                var activity = new TrackingUser
                {
                    UserId = userId,
                    Function = Constants.Collection.Settings,
                    Action = "delete",
                    Values = entity.Id,
                    ValuesDisplay = entity.Key,
                    Description = "Delete setting " + entity.Key,
                    Created = now,
                    Link = "/notfound"
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion
                CacheReLoad();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
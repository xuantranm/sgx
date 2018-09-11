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
    public class TextController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public TextController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<TextController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        // GET: Setting
        [Route("/txt/{name}/")]
        public ActionResult Index(string name)
        {
            var viewModel = new SettingViewModel
            {
                Settings = dbContext.Settings.Find(m => m.Enable.Equals(true)).ToList()
            };
            return View(viewModel);
        }

        // Setting: Setting/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Text entity)
        {
            try
            {
                var userId = User.Identity.Name;
                var now = DateTime.Now;
                // TODO: Add insert logic here
                //entity.Id = Guid.NewGuid();
                dbContext.Texts.InsertOne(entity);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // Setting: Setting/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Text entity, string id)
        {
            try
            {
                var userId = User.Identity.Name;
                var now = DateTime.Now;
                // TODO: Add update logic here
                // You can use the UpdateOne to get higher performance if you need.
                dbContext.Texts.ReplaceOne(m => m.Id == entity.Id, entity);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // Setting: Setting/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, Text entity)
        {
            try
            {
                var userId = User.Identity.Name;
                var now = DateTime.Now;
                // TODO: Add delete logic here
                dbContext.Texts.DeleteOne(m => m.Id == id);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
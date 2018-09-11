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
    public class ActivityController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public ActivityController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<ActivityController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        [Route("/hoat-dong/")]
        public IActionResult Index()
        {
            var userId = User.Identity.Name;
            var activities = dbContext.TrackingUsers.Find(m => m.UserId.Equals(userId)).SortByDescending(m => m.Created).ToList();
            return View(activities);
        }
    }
}
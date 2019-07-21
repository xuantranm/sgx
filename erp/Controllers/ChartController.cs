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
using MimeKit;
using Services;

namespace erp.Controllers
{
    // No mapping url. USE Orginal.
    public class ChartController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public ChartController(IDistributedCache cache,
            IConfiguration configuration,
            IHostingEnvironment env,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILogger<ChartController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        #region HR
        public JsonResult DataHr(string type, string category, string from, string to, string condition)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var elapsedMs = watch.ElapsedMilliseconds;

            int borderWidth = 1;
            var title = "# " + category;
            var info = string.Empty;
            string[] labels = null;
            int[] data = null;
            string[] backgroundColor = null;
            string[] borderColor = null;

            // BASE CATEGORY
            switch (category)
            {
                case "do-tuoi":
                    Console.WriteLine(1);
                    break;
                case "":
                    Console.WriteLine(5);
                    break;
            }

            
            labels = new string[] { "Red", "Blue", "Yellow", "Green", "Purple", "Orange" };
            data = new int[] { 1, 19, 3, 5, 2, 3 };
            backgroundColor = new string[]
            {
                "rgba(255, 99, 132, 0.2)",
                "rgba(54, 162, 235, 0.2)",
                "rgba(255, 206, 86, 0.2)",
                "rgba(75, 192, 192, 0.2)",
                "rgba(153, 102, 255, 0.2)",
                "rgba(255, 159, 64, 0.2)"
            };
            borderColor = new string[]
            {
                "rgba(255, 99, 132, 1)",
                "rgba(54, 162, 235, 1)",
                "rgba(255, 206, 86, 1)",
                "rgba(75, 192, 192, 1)",
                "rgba(153, 102, 255, 1)",
                "rgba(255, 159, 64, 1)"
            };

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            return Json(new { elapsedMs = elapsedMs + "ms", result = true, type, category, labels, data, backgroundColor, borderColor, title, info, borderWidth });
        }

        #endregion

    }
}

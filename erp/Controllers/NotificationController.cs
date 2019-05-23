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
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Services;
using MimeKit;
using MimeKit.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Common.Enums;
using Helpers;

namespace erp.Controllers
{
    //[Authorize]
    [Route(Constants.LinkNotification.Main)]
    public class NotificationController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        readonly IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public NotificationController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, IEmailSender emailSender,
            ISmsSender smsSender, ILogger<NotificationController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        [Route(Constants.LinkNotification.Index + "/" + "{name}")]
        public ActionResult Index(string name)
        {
            return View();
        }

        [Route(Constants.LinkNotification.Index + "/" + Constants.LinkNotification.List)]
        public ActionResult List(string name)
        {
            return View();
        }

        [Route(Constants.LinkNotification.Index + "/tao-moi")]
        public ActionResult Create()
        {

            return View();
        }

        //[Route(Constants.LinkNotication.Index + "/nghi-tet-nguyen-dan-ky-hoi")]
        //public ActionResult Index(string name)
        //{
        //    return View();
        //}

        //[Route(Constants.LinkNotication.Index + "/cac-chuong-trinh-cuoi-nam-mau-tuat")]
        //public ActionResult Index2(string name)
        //{
        //    return View();
        //}

        //[Route(Constants.LinkNotication.Index + "/2019-04-10-cv-122-tb-nghi-le-gio-to-hung-vuong-GPMN-thong nhat-dat-nuoc-30-04-QTLD-01-05-nam-2019")]
        //public ActionResult Index3(string name)
        //{
        //    return View();
        //}
    }
}
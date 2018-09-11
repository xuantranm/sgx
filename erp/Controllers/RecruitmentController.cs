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
using MimeKit;
using Services;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading;
using Common.Enums;

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.LinkRecruitment.Main)]
    public class RecruitmentController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private bool bhxh;

        public RecruitmentController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, IEmailSender emailSender,
            ISmsSender smsSender, ILogger<RecruitmentController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        [Route(Constants.LinkRecruitment.Index)]
        public IActionResult Index()
        {
            return View();
        }
    }
}
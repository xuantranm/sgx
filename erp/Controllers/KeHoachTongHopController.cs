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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver.Linq;
using System.Security.Claims;
using System.Threading;
using MimeKit;
using Services;
using Common.Enums;
using MongoDB.Bson;

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.KeHoachTongHopLink.Main)]
    public class KeHoachTongHopController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        readonly IHostingEnvironment _env;

        private readonly ILogger _logger;

        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public IConfiguration Configuration { get; }

        public KeHoachTongHopController(
            IConfiguration configuration,
            IHostingEnvironment env,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILogger<KeHoachTongHopController> logger)
        {
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region DU LIEU KHO
        [Route(Constants.KeHoachTongHopLink.DuLieuKho)]
        public IActionResult DuLieuKho()
        {
            return View();
        }

        [Route(Constants.KeHoachTongHopLink.DuLieuKhoNhap)]
        public IActionResult DuLieuKhoNhap()
        {
            return View();
        }
        #endregion


        [Route(Constants.KeHoachTongHopLink.DuLieuBun)]
        public IActionResult DuLieuBun()
        {
            return View();
        }

        [Route(Constants.KeHoachTongHopLink.DuLieuDuAnCong)]
        public IActionResult DuLieuDuAnCong()
        {
            return View();
        }

        //public async Task<IActionResult> DuLieuBun()
        //{
        //    return View();
        //}
    }
}
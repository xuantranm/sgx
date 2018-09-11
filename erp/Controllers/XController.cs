using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using Data;
using Models;
using Microsoft.AspNetCore.Authorization;

namespace erp.Controllers
{
    [Authorize]
    public class XController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public XController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<XController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        public IConfiguration Configuration { get; }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }

        public ActionResult SaveCulture(string culture, string user)
        {
            // use update user use languages.
            var filter = Builders<Employee>.Filter.Eq(m=>m.Id, user);
            var update = Builders<Employee>.Update.Set("Language", culture);
            var result = dbContext.Employees.UpdateOne(filter, update);

            return Json(true);
        }
    }
}
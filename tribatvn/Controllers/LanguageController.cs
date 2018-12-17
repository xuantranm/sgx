using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Utilities;
using Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using MongoDB.Bson;

namespace tribatvn.Controllers
{
    public class LanguageController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        MongoDBContext dbContext = new MongoDBContext();
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public LanguageController(IConfiguration configuration, IHostingEnvironment env, ILogger<LanguageController> logger, IHttpContextAccessor httpContextAccessor)
        {
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
            this._httpContextAccessor = httpContextAccessor;
        }

        #region Init Data
        // Call an initialization - language/change/en-US | language/vi-VN
        public JsonResult Change(string language)
        {
            string defaultLanguage = Thread.CurrentThread.CurrentUICulture.ToString();

            if (language != null)
            {
                var culture = new CultureInfo(language);
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                // Set cookie
                //Set("language", language, 10);
            }
            else
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(defaultLanguage);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(defaultLanguage);
            }
            return Json(true); 
        }

        public void InitSeos()
        {
            dbContext.SEOs.DeleteMany(new BsonDocument());
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "home",
                Title = "Công ty TNHH CNSH SÀI GÒN XANH",
                Description = "Công ty TNHH CNSH SÀI GÒN XANH",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "home",
                Title = "GREEN SAIGON BIOTECH CO., LTD",
                Description = "GREEN SAIGON BIOTECH CO., LTD",
                Language = Constants.Languages.English
            });
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "about",
                Title = "Thông tin về Công ty TNHH CNSH SÀI GÒN XANH",
                Description = "Thông tin về Công ty TNHH CNSH SÀI GÒN XANH",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "about",
                Title = "About GREEN SAIGON BIOTECH CO., LTD",
                Description = "About GREEN SAIGON BIOTECH CO., LTD",
                Language = Constants.Languages.English
            });
        }

        #endregion

        #region Cookie
        /// <summary>  
        /// Get the cookie  
        /// </summary>  
        /// <param name="key">Key </param>  
        /// <returns>string value</returns>  
        public string Get(string key)
        {
            //return Request.Cookies["Key"];
            return Request.Cookies[key];
        }
        public void Set(string key, string value, int? expireTime)
        {
            CookieOptions option = new CookieOptions();
            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMilliseconds(10);
            Response.Cookies.Append(key, value, option);
        }
        /// <summary>  
        /// Delete the key  
        /// </summary>  
        /// <param name="key">Key</param>  
        public void Remove(string key)
        {
            Response.Cookies.Delete(key);
        }
        #endregion
    }
}
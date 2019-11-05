using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Enums;
using Common.Utilities;
using Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using MongoDB.Driver;
using ViewModels;

namespace Controllers
{
    [Authorize]
    public class SettingController : BaseController
    {
        MongoDBContext dbContext = new MongoDBContext();
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration _configuration { get; }

        // Use cookie
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SettingController(IConfiguration configuration, IHostingEnvironment env, ILogger<SettingController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _env = env;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index(string Key, int? Type, int Trang, int Dong, string SapXep, string ThuTu)
        {
            var linkCurrent = string.Empty;

            #region Login Information
            LoginInit(Constants.Rights.System, (int)ERights.Add);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            #endregion

            var domain = ViewData[Constants.ActionViews.Domain].ToString();

            #region Filter
            var builder = Builders<Setting>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter &= builder.Eq(m => m.Domain, domain);

            if (!string.IsNullOrEmpty(Key))
            {
                filter &= builder.Eq(x => x.Key, Key);
            }
            if (Type.HasValue)
            {
                filter &= builder.Eq(x => x.Type, Type);
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Setting>.Sort.Ascending(m => m.Key);
            SapXep = string.IsNullOrEmpty(SapXep) ? "code" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "ten":
                    sortBuilder = ThuTu == "asc" ? Builders<Setting>.Sort.Ascending(m => m.Key) : Builders<Setting>.Sort.Descending(m => m.Key);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<Setting>.Sort.Ascending(m => m.Key) : Builders<Setting>.Sort.Descending(m => m.Key);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            int PageSize = Dong;
            int PageTotal = 1;
            var Records = dbContext.Settings.CountDocuments(filter);
            if (Records > 0 && Records > PageSize)
            {
                PageTotal = (int)Math.Ceiling((double)Records / (double)PageSize);
                if (Trang > PageTotal)
                {
                    Trang = 1;
                }
            }

            var list = dbContext.Settings.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * PageSize).Limit(PageSize).ToList();

            var viewModel = new SettingViewModel
            {
                Settings = list,
                Key = Key,
                Type = Type,
                LinkCurrent = linkCurrent,
                ThuTu = ThuTu,
                SapXep = SapXep,
                Records = (int)Records,
                PageSize = PageSize,
                PageTotal = PageTotal,
                PageCurrent = Trang
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Data(string Id)
        {
            #region Login Information
            LoginInit(Constants.Rights.System, (int)ERights.Add);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            #endregion

            var domain = ViewData[Constants.ActionViews.Domain].ToString();
            var settingE = new Setting()
            {
                Domain = domain
            };

            if (!string.IsNullOrEmpty(Id))
            {
                settingE = dbContext.Settings.Find(m => m.Id.Equals(Id)).FirstOrDefault();
                if (settingE == null)
                {
                    settingE = new Setting()
                    {
                        Domain = domain
                    };
                }
            }

            var settings = dbContext.Settings.Find(m => true).ToList();
            var viewModel = new SettingViewModel()
            {
                Setting = settingE,
                Settings = settings
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Data(SettingViewModel viewModel)
        {
            #region Login Information
            LoginInit(Constants.Rights.System, (int)ERights.Add);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            #endregion

            var entity = viewModel.Setting;

            if (string.IsNullOrEmpty(entity.Id))
            {
                dbContext.Settings.InsertOne(entity);
            }
            else
            {
                var builder = Builders<Setting>.Filter;
                var filter = builder.Eq(m => m.Id, entity.Id);
                var update = Builders<Setting>.Update
                    .Set(m => m.Key, entity.Key)
                    .Set(m => m.Domain, entity.Domain)
                    .Set(m => m.Value, entity.Value)
                    .Set(m => m.IsCode, entity.IsCode)
                    .Set(m => m.Type, entity.Type)
                    .Set(m => m.Enable, entity.Enable);
                dbContext.Settings.UpdateOne(filter, update);
            }

            return Redirect("/setting");
        }
    }
}
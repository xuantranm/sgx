using System;
using System.Linq;
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
    public class TextController : BaseController
    {
        MongoDBContext dbContext = new MongoDBContext();
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration _configuration { get; }

        // Use cookie
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TextController(IConfiguration configuration, IHostingEnvironment env, ILogger<TextController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _env = env;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async System.Threading.Tasks.Task<IActionResult> Index(int? Code, int Trang, int Dong, string SapXep, string ThuTu)
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
            var builder = Builders<Text>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter &= builder.Eq(m => m.Domain, domain);
            if (Code.HasValue)
            {
                filter &= builder.Eq(x => x.CodeInt, Code);
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Text>.Sort.Ascending(m => m.CodeInt);
            SapXep = string.IsNullOrEmpty(SapXep) ? "code" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<Text>.Sort.Ascending(m => m.CodeInt) : Builders<Text>.Sort.Descending(m => m.CodeInt);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            int PageSize = Dong;
            int PageTotal = 1;
            var Records = dbContext.Texts.CountDocuments(filter);
            if (Records > 0 && Records > PageSize)
            {
                PageTotal = (int)Math.Ceiling((double)Records / (double)PageSize);
                if (Trang > PageTotal)
                {
                    Trang = 1;
                }
            }

            var list = dbContext.Texts.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * PageSize).Limit(PageSize).ToList();

            var viewModel = new TextViewModel
            {
                Texts = list,
                Code = Code,
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

        public async System.Threading.Tasks.Task<IActionResult> Data(string Id)
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

            var texts = dbContext.Texts.Find(m => m.Enable.Equals(true) && m.Domain.Equals(domain)).SortByDescending(m => m.CodeInt).Limit(1).ToList();
            int codeNew = 1;
            if (texts != null && texts.Count > 0)
            {
                codeNew = texts[0].CodeInt + 1;
            }

            var textE = new Text()
            {
                Domain = domain,
                CodeInt = codeNew,
                Code = codeNew.ToString()
            };

            if (!string.IsNullOrEmpty(Id))
            {
                textE = dbContext.Texts.Find(m => m.Id.Equals(Id)).FirstOrDefault();
                if (textE == null)
                {
                    textE = new Text()
                    {
                        Domain = domain,
                        CodeInt = codeNew,
                        Code = codeNew.ToString()
                    };
                }
            }

            var viewModel = new TextViewModel()
            {
                Text = textE,
                Texts = texts
            };
            return View(viewModel);
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> Data(TextViewModel viewModel)
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
            var entity = viewModel.Text;
            entity.Alias = Utility.AliasConvert(entity.Value);
            entity.ToolTip = entity.Value;

            if (string.IsNullOrEmpty(entity.Id))
            {
                // Check exist, if exist update lastest code ++
                var checkE = dbContext.Texts.Find(m => m.Enable.Equals(true) && m.CodeInt.Equals(entity.CodeInt)).FirstOrDefault();
                if (checkE != null)
                {
                    var texts = dbContext.Texts.Find(m => m.Enable.Equals(true) && m.Domain.Equals(domain)).SortByDescending(m => m.CodeInt).Limit(1).ToList();
                    int codeNew = 1;
                    if (texts != null && texts.Count > 0)
                    {
                        codeNew = texts[0].CodeInt + 1;
                    }
                    entity.CodeInt = codeNew;
                }
                entity.Code = entity.CodeInt.ToString();
                dbContext.Texts.InsertOne(entity);
            }
            else
            {
                entity.Code = entity.CodeInt.ToString();
                var builder = Builders<Text>.Filter;
                var filter = builder.Eq(m => m.Id, entity.Id);
                var update = Builders<Text>.Update
                    .Set(m => m.Code, entity.Code)
                    .Set(m => m.CodeInt, entity.CodeInt)
                    .Set(m => m.Value, entity.Value)
                    .Set(m => m.Alias, entity.Alias)
                    .Set(m => m.ToolTip, entity.ToolTip)
                    .Set(m => m.NoDelete, entity.NoDelete);
                dbContext.Texts.UpdateOne(filter, update);
            }

            return Redirect("/text");
        }
    }
}
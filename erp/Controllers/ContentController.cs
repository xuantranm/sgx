using System;
using System.Collections.Generic;
using System.IO;
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
    public class ContentController : BaseController
    {
        MongoDBContext dbContext = new MongoDBContext();
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration _configuration { get; }

        // Use cookie
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ContentController(IConfiguration configuration, IHostingEnvironment env, ILogger<ContentController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _env = env;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index(string category, string code, string name, int Trang, int Dong, string SapXep, string ThuTu)
        {
            #region Login Information
            LoginInit(Constants.Rights.System, (int)ERights.View);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            var loginId = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            bool isRight = (bool)ViewData[Constants.ActionViews.IsRight];
            if (!isRight)
            {
                return RedirectToAction("Index", "Home");
            }
            #endregion

            var domain = ViewData[Constants.ActionViews.Domain].ToString();
            var linkCurrent = string.Empty;
            #region DDL
            var builderC = Builders<Category>.Filter;
            var filterC = builderC.Eq(m => m.Enable, true);
            filterC &= builderC.Eq(m => m.Domain, domain);
            var categories = dbContext.Categories.Find(filterC).ToList();
            #endregion

            #region Filter
            var builder = Builders<Content>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter &= builder.Eq(m => m.Domain, domain);

            if (!string.IsNullOrEmpty(category))
            {
                filter &= builder.Eq(x => x.CategoryId, category);
            }
            if (!string.IsNullOrEmpty(code))
            {
                filter &= builder.Eq(x => x.Code, code);
            }
            if (!string.IsNullOrEmpty(name))
            {
                filter &= builder.Ne(x => x.Alias, name);
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Content>.Sort.Descending(m => m.CreatedOn);
            SapXep = string.IsNullOrEmpty(SapXep) ? "CreatedOn" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "CreatedOn":
                    sortBuilder = ThuTu == "asc" ? Builders<Content>.Sort.Ascending(m => m.CreatedOn) : Builders<Content>.Sort.Descending(m => m.CreatedOn);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<Content>.Sort.Ascending(m => m.CreatedOn) : Builders<Content>.Sort.Descending(m => m.CreatedOn);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            int PageSize = Dong;
            int PageTotal = 1;
            var Records = dbContext.Contents.CountDocuments(filter);
            if (Records > 0 && Records > PageSize)
            {
                PageTotal = (int)Math.Ceiling(Records / (double)PageSize);
                if (Trang > PageTotal)
                {
                    Trang = 1;
                }
            }

            var list = dbContext.Contents.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * PageSize).Limit(PageSize).ToList();

            var viewModel = new ContentViewModel
            {
                Contents = list,
                Categories = categories,
                Domain = domain,
                Category = category,
                Code = code,
                Name = name,
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
            var contentE = new Content()
            {
                Domain = domain
            };
            if (!string.IsNullOrEmpty(Id))
            {
                contentE = dbContext.Contents.Find(m => m.Id.Equals(Id)).FirstOrDefault();
                if (contentE == null)
                {
                    contentE = new Content()
                    {
                        Domain = domain
                    };
                }
            }

            var categories = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Domain.Equals(domain)).ToList();
            var categoriesDisplay = new List<CategoryDisplay>();
            foreach (var item in categories)
            {
                var parentName = string.Empty;
                if (!string.IsNullOrEmpty(item.ParentId))
                {
                    parentName = dbContext.Categories.Find(m => m.Id.Equals(item.ParentId)).FirstOrDefault().Name;
                }
                categoriesDisplay.Add(new CategoryDisplay() { Category = item, ParentName = parentName });
            }

            var viewModel = new ContentViewModel()
            {
                Categories = categories,
                CategoriesDisplay = categoriesDisplay,
                Content = contentE
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Data(ContentViewModel viewModel)
        {
            #region Login Information
            LoginInit(Constants.Rights.System, (int)ERights.Add);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            #endregion

            var entity = viewModel.Content;

            var domain = ViewData[Constants.ActionViews.Domain].ToString();
            var domainCode = 1;
            var domainE = dbContext.Domains.Find(m => m.Enable.Equals(true) && m.Name.Equals(entity.Domain)).FirstOrDefault();
            if (domainE != null)
            {
                domainCode = domainE.Code;
            }

            var seo = viewModel.Seo;

            if (!string.IsNullOrEmpty(entity.CategoryId))
            {
                var categoryE = dbContext.Categories.Find(m => m.Id.Equals(entity.CategoryId)).FirstOrDefault();
                entity.CategoryAlias = categoryE.Alias;
                entity.CategoryName = categoryE.Name;
            }

            entity.Name = entity.Name.Trim();
            entity.Alias = Utility.AliasConvert(entity.Name);

            #region Properties
            if (entity.Properties != null && entity.Properties.Count > 0)
            {
                entity.Properties = entity.Properties.Where(m => m.IsChoose.Equals(true)).ToList();
            }
            #endregion

            #region Contents
            if (entity.Contents != null && entity.Contents.Count > 0)
            {
                entity.Contents = entity.Contents.Where(m => m.IsDelete.Equals(false)).ToList();
            }
            #endregion

            if (string.IsNullOrEmpty(entity.Id))
            {
                var lastestE = dbContext.Contents.Find(m => m.Enable.Equals(true) && m.Domain.Equals(domain)).SortByDescending(m => m.CodeInt).FirstOrDefault();
                entity.CodeInt = lastestE != null ? lastestE.CodeInt + 1 : 1;
                entity.Code = entity.CodeInt.ToString();
            }

            #region Images : Newest.
            var folder = Path.Combine(Constants.Folder.Image, domainCode.ToString(), entity.Alias + "-" + entity.CodeInt);
            entity.Contents = Utility.ImageProcess(entity.Contents.ToList(), _env.WebRootPath, folder, entity.Name, entity.Code);
            #endregion

            #region Seo
            var settings = dbContext.Settings.Find(m => m.Enable.Equals(true) && m.Domain.Equals(entity.Domain)).ToList();
            var imgFbW = settings.Where(m => m.Key.Equals("facebook-img-w")).FirstOrDefault().Value;
            var imgFbH = settings.Where(m => m.Key.Equals("facebook-img-h")).FirstOrDefault().Value;
            var imgGgW = settings.Where(m => m.Key.Equals("google-img-w")).FirstOrDefault().Value;
            var imgGgH = settings.Where(m => m.Key.Equals("google-img-h")).FirstOrDefault().Value;
            if (seo == null || string.IsNullOrEmpty(seo.Title))
            {
                seo = new Seo()
                {
                    Title = entity.Name,
                    Description = entity.Description,
                    ImageGGW = imgGgW,
                    ImageGGH = imgGgH,
                    ImageFBW = imgFbW,
                    ImageFBH = imgFbH
                };
            }

            // Seo IMG do later
            entity.Seo = seo;
            #endregion

            if (string.IsNullOrEmpty(entity.Id))
            {
                var existE = dbContext.Contents.Find(m => m.Name.Equals(entity.Name.Trim()) && m.Domain.Equals(domain)).FirstOrDefault();
                if (existE != null)
                {
                    return Json(new { result = false, message = Constants.Texts.Duplicate, entity });
                }

                dbContext.Contents.InsertOne(entity);
            }
            else
            {
                var builder = Builders<Content>.Filter;
                var filter = builder.Eq(m => m.Id, entity.Id);
                var update = Builders<Content>.Update
                    .Set(m => m.Name, entity.Name)
                    .Set(m => m.Alias, entity.Alias)
                    .Set(m => m.Description, entity.Description)
                    .Set(m => m.Properties, entity.Properties)
                    .Set(m => m.Contents, entity.Contents)
                    .Set(m => m.Seo, entity.Seo)
                    .Set(m => m.Enable, entity.Enable);
                dbContext.Contents.UpdateOne(filter, update);
            }

            #region Sitemap
            Utility.SiteMapAuto(domain, _env.WebRootPath);
            #endregion

            return Redirect("/content");
        }
    }
}
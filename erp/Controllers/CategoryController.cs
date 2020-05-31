using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class CategoryController : BaseController
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        readonly IHostingEnvironment Env;
        public IConfiguration Configuration { get; }

        public CategoryController(IConfiguration configuration, 
            IHostingEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        [Route(Constants.Link.Category)]
        public async Task<IActionResult> Index(string alias, int? type, int trang, int dong, string sapxep, string thutu)
        {
            var linkCurrent = string.Empty;

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

            #region Filter
            var builder = Builders<Category>.Filter;
            var filter = builder.Eq(m => m.Enable, true);

            if (!string.IsNullOrEmpty(alias))
            {
                filter &= builder.Eq(x => x.Alias, alias);
            }
            if (type.HasValue)
            {
                filter &= builder.Eq(x => x.Type, type);
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Category>.Sort.Ascending(m => m.Alias);
            #endregion

            var records = dbContext.Categories.CountDocuments(filter);

            var list = dbContext.Categories.Find(filter).Sort(sortBuilder).ToList();
            var categoriesDisplay = new List<CategoryDisplay>();
            foreach (var item in list)
            {
                var parentName = string.Empty;
                if (!string.IsNullOrEmpty(item.ParentId))
                {
                    parentName = dbContext.Categories.Find(m => m.Id.Equals(item.ParentId)).FirstOrDefault().Name;
                }
                categoriesDisplay.Add(new CategoryDisplay() { Category = item, ParentName = parentName });
            }

            var viewModel = new CategoryViewModel
            {
                Categories = list,
                CategoriesDisplay = categoriesDisplay,
                Alias = alias,
                Type = type,
                LinkCurrent = linkCurrent,
                Records = (int)records
            };
            return View(viewModel);
        }

        [Route(Constants.Link.Category + "/" + Constants.ActionLink.Data)]
        [Route(Constants.Link.Category + "/" + Constants.ActionLink.Data +"/{id}")]
        public async Task<IActionResult> Data(string id)
        {
            #region Login Information
            LoginInit(Constants.Rights.System, (int)ERights.Add);
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

            var properties = dbContext.Properties.Find(m => m.Type.Equals((int)EData.Property) && m.Enable.Equals(true)).ToList();

            var categoryE = new Category();

            if (!string.IsNullOrEmpty(id))
            {
                categoryE = dbContext.Categories.Find(m => m.Id.Equals(id)).FirstOrDefault();
                if (categoryE == null)
                {
                    categoryE = new Category();
                }

                if (categoryE.Properties != null && categoryE.Properties.Count > 0)
                {
                    foreach (var item in categoryE.Properties)
                    {
                        properties.Where(w => w.Key == item.Key).ToList().ForEach(s => s.IsChoose = true);
                    }
                }
            }

            var categories = dbContext.Categories.Find(m => m.Enable.Equals(true)).ToList();
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

            var viewModel = new CategoryViewModel()
            {
                Category = categoryE,
                Categories = categories,
                CategoriesDisplay = categoriesDisplay,
                Properties = properties
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.Link.Category + "/" + Constants.ActionLink.Data)]
        public async Task<IActionResult> Data(CategoryViewModel viewModel)
        {
            #region Login Information
            LoginInit(Constants.Rights.System, (int)ERights.Add);
            if (!(bool)ViewData[Constants.ActionViews.IsLogin])
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
            }
            #endregion

            var entity = viewModel.Category;
            entity.Name = entity.Name.Trim();
            entity.Alias = Utility.AliasConvert(entity.Name);

            #region Values
            if (entity.Type == (int)ECategory.Holiday)
            {
                entity.ValueType = (int)EValueType.Date;
                entity.Value = DateTime.Parse(entity.Value, new CultureInfo("en-CA")).ToString();
            }
            #endregion

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
                var lastestE = dbContext.Categories.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.CodeInt).FirstOrDefault();
                entity.CodeInt = lastestE != null ? lastestE.CodeInt + 1 : 1;
                entity.Code = entity.CodeInt.ToString();
            }

            #region Images : Newest.
            var folder = Path.Combine(Constants.Folder.Image, entity.Alias + "-" + entity.CodeInt);
            entity.Contents = Utility.ImageProcess(entity.Contents.ToList(), Env.WebRootPath, folder, entity.Name, entity.Code);
            #endregion

            if (string.IsNullOrEmpty(entity.Id))
            {
                var existE = dbContext.Categories.Find(m => m.Name.Equals(entity.Name.Trim())).FirstOrDefault();
                if (existE != null)
                {
                    return Json(new { result = false, message = Constants.Texts.Duplicate, entity });
                }

                dbContext.Categories.InsertOne(entity);
            }
            else
            {
                var builder = Builders<Category>.Filter;
                var filter = builder.Eq(m => m.Id, entity.Id);
                var update = Builders<Category>.Update
                    .Set(m => m.Alias, entity.Alias)
                    .Set(m => m.Name, entity.Name)
                    .Set(m => m.Description, entity.Description)
                    .Set(m => m.ParentId, entity.ParentId)
                    .Set(m => m.Properties, entity.Properties)
                    .Set(m => m.Contents, entity.Contents)
                    .Set(m => m.Enable, entity.Enable);
                dbContext.Categories.UpdateOne(filter, update);
            }

            return Redirect("/" + Constants.Link.Category);
        }

        [HttpPost]
        [Route(Constants.Link.Category + "/" + Constants.ActionLink.Api)]
        public async Task<IActionResult> DataApi(Category entity)
        {
            try
            {
                #region Login Information
                LoginInit(Constants.Rights.System, (int)ERights.Add);
                if (!(bool)ViewData[Constants.ActionViews.IsLogin])
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction(Constants.ActionViews.Login, Constants.Controllers.Account);
                }
                #endregion

                entity.Name = entity.Name.Trim();
                entity.Alias = Utility.AliasConvert(entity.Name);
                var lastestE = dbContext.Categories.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.CodeInt).FirstOrDefault();
                entity.CodeInt = lastestE != null ? lastestE.CodeInt + 1 : 1;
                entity.Code = entity.CodeInt.ToString(); // Update rule later

                if (string.IsNullOrEmpty(entity.Id))
                {
                    var existE = dbContext.Categories.Find(m => m.Name.Equals(entity.Name.Trim())).FirstOrDefault();
                    if (existE != null)
                    {
                        return Json(new { result = false, message = Constants.Texts.Duplicate, entity });
                    }
                    dbContext.Categories.InsertOne(entity);
                }
                else
                {
                    var builder = Builders<Category>.Filter;
                    var filter = builder.Eq(m => m.Id, entity.Id);
                    var update = Builders<Category>.Update
                        .Set(m => m.Alias, entity.Alias)
                        .Set(m => m.Name, entity.Name)
                        .Set(m => m.ParentId, entity.ParentId)
                        .Set(m => m.Enable, entity.Enable);
                    dbContext.Categories.UpdateOne(filter, update);
                }

                return Json(new { result = true, message = Constants.Texts.Success, entity });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetProperties(string id)
        {
            try
            {
                var entity = dbContext.Categories.Find(m => m.Id.Equals(id)).FirstOrDefault();
                if (entity != null)
                {
                    return Json(new { result = true, entity, properties = entity.Properties });
                }
                else
                {
                    return Json(new { result = false, message = Constants.Texts.NotFound });
                }
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = ex.Message });
            }
        }
    }
}
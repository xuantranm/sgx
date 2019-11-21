using System;
using System.Collections.Generic;
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
    public class RightController : BaseController
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        public IConfiguration Configuration { get; }
        public RightController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [Route(Constants.Link.Right)]
        public async Task<IActionResult> Index(string role, string ob, int Trang, int Dong, string SapXep, string ThuTu)
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

            #region DLL
            var categories = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Role)).SortBy(m=>m.Name).ToList();
            #endregion

            #region Filter
            var builder = Builders<Right>.Filter;
            var filter = builder.Eq(m => m.Enable, true);

            if (!string.IsNullOrEmpty(role))
            {
                filter &= builder.Eq(x => x.RoleId, role);
            }
            if (!string.IsNullOrEmpty(ob))
            {
                filter &= builder.Eq(x => x.ObjectId, ob);
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Right>.Sort.Ascending(m => m.ModifiedOn);
            SapXep = string.IsNullOrEmpty(SapXep) ? "code" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "ob":
                    sortBuilder = ThuTu == "asc" ? Builders<Right>.Sort.Ascending(m => m.ObjectId) : Builders<Right>.Sort.Descending(m => m.ObjectId);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<Right>.Sort.Ascending(m => m.ModifiedOn) : Builders<Right>.Sort.Descending(m => m.ModifiedOn);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            int PageSize = Dong;
            int PageTotal = 1;
            var Records = dbContext.Rights.CountDocuments(filter);
            if (Records > 0 && Records > PageSize)
            {
                PageTotal = (int)Math.Ceiling((double)Records / (double)PageSize);
                if (Trang > PageTotal)
                {
                    Trang = 1;
                }
            }

            var list = dbContext.Rights.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * PageSize).Limit(PageSize).ToList();

            var displays = new List<RightDisplay>();
            foreach(var item in list)
            {
                var roleName = string.Empty;
                var obName = string.Empty;
                if (!string.IsNullOrEmpty(item.RoleId))
                {
                    roleName = dbContext.Categories.Find(m => m.Id.Equals(item.RoleId)).FirstOrDefault().Name;
                }
                if (!string.IsNullOrEmpty(item.ObjectId))
                {
                    switch (item.Type)
                    {
                        case (int)ERightType.User:
                            obName = dbContext.Employees.Find(m => m.Id.Equals(item.ObjectId)).FirstOrDefault().FullName;
                            break;
                        default:
                            obName = dbContext.Categories.Find(m => m.Id.Equals(item.ObjectId)).FirstOrDefault().Name;
                            break;
                    }
                }
                displays.Add(new RightDisplay() { Right = item, Role = roleName, Object = obName });
            }

            var viewModel = new RightViewModel
            {
                Rights = list,
                RightsDisplay = displays,
                Categories = categories,
                Ob = ob,
                Role = role,
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

        [Route(Constants.Link.Right + "/" + Constants.ActionLink.Data)]
        [Route(Constants.Link.Right + "/" + Constants.ActionLink.Data + "/{id}" )]
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

            #region DLL
            var categories = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Role)).SortBy(m => m.Name).ToList();
            #endregion

            var entity = new Right();

            if (!string.IsNullOrEmpty(id))
            {
                entity = dbContext.Rights.Find(m => m.Id.Equals(id)).FirstOrDefault();
                if (entity == null)
                {
                    entity = new Right();
                }
            }

            var viewModel = new RightViewModel()
            {
                Right = entity,
                Categories = categories
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.Link.Right + "/" + Constants.ActionLink.Data)]
        public async Task<IActionResult> Data(RightViewModel viewModel)
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

            var entity = viewModel.Right;

            if (string.IsNullOrEmpty(entity.Id))
            {
                dbContext.Rights.InsertOne(entity);
            }
            else
            {
                var builder = Builders<Right>.Filter;
                var filter = builder.Eq(m => m.Id, entity.Id);
                var update = Builders<Right>.Update
                    .Set(m => m.RoleId, entity.RoleId)
                    .Set(m => m.ObjectId, entity.ObjectId)
                    .Set(m => m.Action, entity.Action)
                    .Set(m => m.Type, entity.Type)
                    .Set(m => m.Enable, entity.Enable);
                dbContext.Rights.UpdateOne(filter, update);
            }

            return Redirect("/" + Constants.Link.Right);
        }
    }
}
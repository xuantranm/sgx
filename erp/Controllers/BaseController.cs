using System.Linq;
using Common.Enums;
using Common.Utilities;
using Data;
using Microsoft.AspNetCore.Mvc;
using Models;
using MongoDB.Driver;

namespace Controllers
{
    public class BaseController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        // role | action can be string.Empty
        public void LoginInit(string role, int action)
        {
            bool isLogin = false;
            bool isRight = false;
            bool isSystem = false;
            var userName = string.Empty;
            var login = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.Id.Equals(login) && m.Enable.Equals(true) && m.Leave.Equals(false)).FirstOrDefault();
            if (loginE != null)
            {
                userName = loginE.UserName;
                isLogin = true;
                if (login == Constants.System.accountId)
                {
                    isRight = true;
                    isSystem = true;
                }
                else
                {
                    if (!string.IsNullOrEmpty(role))
                    {
                        var roleE = dbContext.Categories.Find(m => m.Alias.Equals(role) && m.Type.Equals((int)ECategory.Role)).FirstOrDefault();
                        if (roleE != null)
                        {
                            var rightExist = dbContext.Rights.CountDocuments(m => m.RoleId.Equals(roleE.Id)
                                        && (m.ObjectId.Equals(loginE.ChucVu) || m.ObjectId.Equals(loginE.Id))
                                        && m.Action >= action);
                            if (rightExist > 0)
                            {
                                isRight = true;
                            }
                        }
                    }
                }
            }

            ViewData[Constants.ActionViews.UserName] = userName;
            ViewData[Constants.ActionViews.IsLogin] = isLogin;
            ViewData[Constants.ActionViews.IsRight] = isRight;
            ViewData[Constants.ActionViews.IsSystem] = isSystem;
        }

        public void SeoInit(Seo entity)
        {
            if (entity != null)
            {
                // IMPORTANT
                ViewData["title"] = entity.Title;
                ViewData["keywords"] = entity.KeyWords;
                ViewData["description"] = entity.Description;
                ViewData["robots"] = entity.Robots;

                // EXTEND
                ViewData["author"] = !string.IsNullOrEmpty(entity.Author) ? entity.Author : Utility.GetSetting("domain");
                ViewData["type"] = entity.Type;
                ViewData["url"] = entity.Url; // Fast get, slowly process
                ViewData["canonical"] = entity.Canonical;

                ViewData["image"] = entity.Image;
                ViewData["imageW"] = entity.ImageW;
                ViewData["imageH"] = entity.ImageH;

                ViewData["datePublished"] = entity.DatePublished;
                ViewData["dateModified"] = entity.DateModified;

                ViewData["footer"] = entity.Footer;
                ViewData["nameApplicationLdJsonGoogleMeta"] = entity.NameApplicationLdJsonGoogleMeta;


                ViewData["typeGGS"] = entity.TypeGGS;
                if (string.IsNullOrEmpty(entity.ImageGG))
                {
                    entity.ImageGG = Utility.GetSetting("img-empty-link-default");
                    entity.ImageGGW = Utility.GetSetting("google-img-w");
                    entity.ImageGGH = Utility.GetSetting("google-img-h");
                }

                ViewData["imageGG"] = entity.ImageGG;
                ViewData["imageGGW"] = entity.ImageGGW;
                ViewData["imageGGH"] = entity.ImageGGH;

                // You can use Open Graph tags to customize link previews.
                // Learn more: https://developers.facebook.com/docs/sharing/webmasters
                //ViewData["fb:app_id"] = entity.AppId;
                ViewData["typeFb"] = entity.TypeFB;
                ViewData["tagsFb"] = entity.TagsFB;
                if (string.IsNullOrEmpty(entity.ImageFB))
                {
                    entity.ImageFB = Utility.GetSetting("img-empty-link-default");
                    entity.ImageFBW = Utility.GetSetting("facebook-img-w");
                    entity.ImageFBH = Utility.GetSetting("facebook-img-h");
                }
                ViewData["imageFB"] = entity.ImageFB;
                ViewData["imageFBW"] = entity.ImageFBW;
                ViewData["imageFBH"] = entity.ImageFBH;

                ViewData["twitterCard"] = entity.TwitterCard;
                ViewData["twitterCreator"] = entity.TwitterCreator;
                ViewData["twitterSite"] = entity.TwitterSite;
            }
        }
    }
}
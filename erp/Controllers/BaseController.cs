using System.Linq;
using Common.Utilities;
using Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Controllers
{
    public class BaseController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        public void LoginInit(string role, int action)
        {
            var login = User.Identity.Name;
            var loginE = dbContext.Employees.Find(m => m.UserName.Equals(login) && m.Enable.Equals(true) && m.Leave.Equals(false)).FirstOrDefault();
            if (loginE == null)
            {
                ViewData[Constants.ActionViews.IsLogin] = false;
                ViewData[Constants.ActionViews.IsSystem] = false;
            }
            else
            {
                ViewData[Constants.ActionViews.IsLogin] = true;
                ViewData[Constants.ActionViews.UserName] = loginE.UserName;

                // Use ROLE implement
                var isRight = false;
                var roleE = dbContext.Roles.Find(m => m.Alias.Equals(role)).FirstOrDefault();
                if (roleE != null)
                {
                    // Chuc Vu
                    var rightExist = dbContext.Rights.CountDocuments(m => m.RoleId.Equals(roleE.Id)
                                && m.ObjectId.Equals(loginE.ChucVu) && m.Action.Equals(action));
                    if (rightExist > 0)
                    {
                        isRight = true;
                    }
                    else
                    {
                        rightExist = dbContext.Rights.CountDocuments(m => m.RoleId.Equals(roleE.Id)
                                && m.ObjectId.Equals(loginE.Id) && m.Action.Equals(action));
                        if (rightExist > 0)
                        {
                            isRight = true;
                        }
                    }
                }
                
                ViewData[Constants.ActionViews.IsRight] = isRight;
            }
        }
    }
}
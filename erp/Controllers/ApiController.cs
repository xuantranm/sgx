using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Data;
using Models;
using Common.Utilities;
using MimeKit;
using Services;

namespace Controllers
{
    public class ApiController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        private readonly IEmailSender _emailSender;

        public ApiController(
            IConfiguration configuration,
            IHostingEnvironment env,
            IEmailSender emailSender,
            ILogger<ApiController> logger)
        {
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _logger = logger;
        }

        #region NEW CODE
        public JsonResult Category(int type, int parentType)
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var elapsedMs = watch.ElapsedMilliseconds;
                var categories = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals(parentType)).ToList();
                var types = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals(type)).ToList();
                // types: current data
                // categories: doanh muc 
                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
                return Json(new { elapsedMs = elapsedMs + "ms", result = true, categories, types });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CategoryData(Category entity)
        {
            if (string.IsNullOrEmpty(entity.Id))
            {
                entity.Alias = Utility.AliasConvert(entity.Name);
                var lastE = dbContext.Categories.Find(m => m.Type.Equals(entity.Type)).SortByDescending(m => m.CodeInt).FirstOrDefault();
                var newCode = lastE.CodeInt + 1;
                entity.CodeInt = newCode;
                entity.Code = newCode.ToString();
                var isExist = dbContext.Categories.CountDocuments(m => m.Alias.Equals(entity.Alias) && m.Type.Equals(entity.Type));
                if (isExist > 0)
                {
                    return Json(new { result = false, source = "create", entity, message = Constants.Texts.Duplicate });
                }
                else
                {
                    dbContext.Categories.InsertOne(entity);
                    return Json(new { result = true, source = "create", entity, message = Constants.DataSuccess });
                }
            }
            else
            {
                var filter = Builders<Category>.Filter.Eq(m => m.Id, entity.Id);
                var update = Builders<Category>.Update
                    .Set(m => m.ParentId, entity.ParentId)
                    .Set(m => m.Name, entity.Name)
                    .Set(m => m.Alias, entity.Alias)
                    .Set(m => m.Description, entity.Description)
                    .Set(m => m.CodeInt, entity.CodeInt)
                    .Set(m => m.Code, entity.Code)
                    .Set(m => m.Enable, entity.Enable);
                dbContext.Categories.UpdateOne(filter, update);

                return Json(new { result = true, source = "update", entity, message = Constants.DataSuccess });
            }
        }

        public JsonResult LoadCategory(string parentid, int type)
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var elapsedMs = watch.ElapsedMilliseconds;
                var categories = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals(type) && m.ParentId.Equals(parentid)).ToList();
                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
                return Json(new { elapsedMs = elapsedMs + "ms", result = true, categories });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = ex.Message });
            }
        }
        #endregion

        #region FACTORY
        [Route("factory/product-infomation")]
        public JsonResult FactoryProductInformation(string product)
        {
            // First rule: get lastest unit, quantity in TonSX
            var watch = System.Diagnostics.Stopwatch.StartNew();
            #region Filter
            var builder = Builders<FactoryTonSX>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & builder.Eq(i => i.ProductId, product);
            //filter = filter & builder.Regex(i => i.Product, product);
            #endregion

            #region Sort
            var sort = Builders<FactoryTonSX>.Sort.Descending(m => m.Date).Descending(m => m.CreatedOn);
            #endregion

            var outputs = dbContext.FactoryTonSXs.Find(filter).Sort(sort).Limit(1).FirstOrDefault();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            return Json(new { elapsedMs = elapsedMs + "ms", result = true, outputs });
        }
        #endregion

        #region Check Email
        [Route("welcome")]
        public JsonResult Welcome()
        {
            var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = "Trần Minh Xuân", Address = "xuan.tm@tribat.vn" }
            };

            // Send an email with this link
            //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            //var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
            //Email from Email Template
            var callbackUrl = "/";
            string Message = "Đăng nhập TRIBAT - ERP <a href=\"" + callbackUrl + "\">here</a>";
            // string body;

            var webRoot = _env.WebRootPath; //get wwwroot Folder

            //Get TemplateFile located at wwwroot/Templates/EmailTemplate/Register_EmailTemplate.html
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "Confirm_Account_Registration.html";

            var subject = "Thông tin đăng nhập hệ thống TRIBAT - ERP.";

            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }
            //{0} : Subject
            //{1} : DateTime
            //{2} : Email
            //{3} : Username
            //{4} : Password
            //{5} : Message
            //{6} : callbackURL

            string messageBody = string.Format(builder.HtmlBody,
                subject,
                String.Format("{0:dddd, d MMMM yyyy}", DateTime.Now),
                "Trần Minh Xuân",
                "xuan.tm",
                "98988987",
                Message,
                callbackUrl
                );

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                Subject = subject,
                BodyContent = messageBody
            };
            _emailSender.SendEmailAsync(emailMessage);

            ViewData["Message"] = $"Please confirm your account by clicking this link: <a href='{callbackUrl}' class='btn btn-primary'>Confirmation Link</a>";
            ViewData["MessageValue"] = "1";

            _logger.LogInformation(3, "User created a new account with password.");

            return Json(new { result = true });
        }
        #endregion

        #region Update Pwd
        //[Route("updatepwd")]
        public ActionResult UpdatePassword(string newpassword)
        {
            var login = User.Identity.Name;

            var pwdHash = Helpers.Helper.HashedPassword(newpassword);
            var filter = Builders<Employee>.Filter.Eq(m => m.Id, login);
            var update = Builders<Employee>.Update
                .Set(m => m.Password, pwdHash)
                .Set(m => m.UpdatedBy, login)
                .Set(m => m.UpdatedOn, DateTime.Now);

            dbContext.Employees.UpdateOne(filter, update);
            return Json(new { result = true, message = "Cập nhật thành công" });
        }
        #endregion

        #region EMPLOYEE
        public JsonResult GetWelcomeToEmails(string PhongBan, bool All)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var elapsedMs = watch.ElapsedMilliseconds;

            var tos = new List<EmailAddress>();
            var ccs = new List<EmailAddress>();

            #region CC: HR & Boss (if All)
            var idsBoss = new List<string>();
            if (All)
            {
                var ngachLuongBoss = Constants.NgachLuongBoss.Split(';').Select(p => p.Trim()).ToList();
                var builderBoss = Builders<Employee>.Filter;
                var filterBoss = builderBoss.Eq(m => m.Enable, true)
                            & builderBoss.Eq(m => m.Leave, false)
                            & !builderBoss.Eq(m => m.UserName, Constants.System.account)
                            & builderBoss.In(c => c.NgachLuongCode, ngachLuongBoss)
                            & !builderBoss.Eq(m => m.Email, null)
                            & !builderBoss.Eq(m => m.Email, string.Empty);

                var fieldBoss = Builders<Employee>.Projection.Include(p => p.Id).Include(p => p.FullName).Include(p => p.Email);
                var boss = dbContext.Employees.Find(filterBoss).Project<Employee>(fieldBoss).ToList();
                foreach (var item in boss)
                {
                    ccs.Add(new EmailAddress
                    {
                        Name = item.FullName,
                        Address = item.Email
                    });
                }
                idsBoss = boss.Select(m => m.Id).ToList();
            }

            var hrs = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                            && !m.UserName.Equals(Constants.System.account)
                            && m.PhongBan.Equals("5c88d094d59d56225c432414")
                            && !string.IsNullOrEmpty(m.Email)).ToList();
            // get ids right nhan su && (m.Expired.Equals(null) || m.Expired > DateTime.Now)
            var builderR = Builders<RoleUser>.Filter;
            var filterR = builderR.Eq(m => m.Enable, true)
                        & builderR.Eq(m => m.Role, Constants.Rights.HR)
                        & builderR.Eq(m => m.Action, Convert.ToInt32(Constants.Action.Edit))
                        & builderR.Eq(m => m.Expired, null)
                        | builderR.Gt(m => m.Expired, DateTime.Now);
            var fieldR = Builders<RoleUser>.Projection.Include(p => p.User);
            var idsR = dbContext.RoleUsers.Find(filterR).Project<RoleUser>(fieldR).ToList().Select(m => m.User).ToList();
            foreach (var hr in hrs)
            {
                if (idsR.Contains(hr.Id))
                {
                    ccs.Add(new EmailAddress
                    {
                        Name = hr.FullName,
                        Address = hr.Email
                    });
                }
            }

            idsR.AddRange(idsBoss);
            #endregion

            if (!All)
            {
                var ketoans = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                            && !string.IsNullOrEmpty(m.Email) && m.PhongBan.Equals("5c88d094d59d56225c432422") && !m.UserName.Equals(Constants.System.account)).ToList();

                foreach (var item in ketoans)
                {
                    tos.Add(new EmailAddress
                    {
                        Name = item.FullName,
                        Address = item.Email,
                    });
                }

                var relations = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                              && !string.IsNullOrEmpty(m.Email) && m.PhongBan.Equals(PhongBan) && !m.UserName.Equals(Constants.System.account)).ToList();
                foreach (var item in relations)
                {
                    tos.Add(new EmailAddress
                    {
                        Name = item.FullName,
                        Address = item.Email,
                    });
                }
            }
            else
            {
                var builderAll = Builders<Employee>.Filter;
                var filterAll = builderAll.Eq(m => m.Enable, true)
                            & builderAll.Eq(m => m.Leave, false)
                            & !builderAll.Eq(m => m.UserName, Constants.System.account)
                            & !builderAll.Eq(m => m.Email, null)
                            & !builderAll.Eq(m => m.Email, string.Empty)
                            & !builderAll.In(c => c.Id, idsR);
                var fieldAll = Builders<Employee>.Projection.Include(p => p.FullName).Include(p => p.Email);
                var allEmail = dbContext.Employees.Find(filterAll).Project<Employee>(fieldAll).ToList();
                foreach (var item in allEmail)
                {
                    tos.Add(new EmailAddress
                    {
                        Name = item.FullName,
                        Address = item.Email,
                    });
                }
            }

            var tohtml = string.Empty;
            foreach (var to in tos)
            {
                tohtml += string.IsNullOrEmpty(tohtml) ? to.Address : "; " + to.Address;
            }
            var cchtml = string.Empty;
            foreach (var cc in ccs)
            {
                cchtml += string.IsNullOrEmpty(cchtml) ? cc.Address : "; " + cc.Address;
            }
            if (tos.Count == 0)
            {
                tohtml = "Dữ liệu không tìm thấy. Xem lại phòng ban.";
            }
            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            return Json(new { elapsedMs = elapsedMs + "ms", result = true, tos, ccs, tohtml, cchtml });
        }

        public ActionResult EmployeeDisable(string Id)
        {
            var login = User.Identity.Name;
            var filter = Builders<Employee>.Filter.Eq(m => m.Id, Id);
            var update = Builders<Employee>.Update
                .Set(m => m.Enable, false)
                .Set(m => m.UpdatedBy, login)
                .Set(m => m.UpdatedOn, DateTime.Now);

            dbContext.Employees.UpdateOne(filter, update);
            return Json(new { result = true, message = "Cập nhật thành công" });
        }


        public JsonResult GetByKhoiChucNang(string id, string removes)
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var elapsedMs = watch.ElapsedMilliseconds;
                var khoichucnangE = dbContext.KhoiChucNangs.Find(m => m.Id.Equals(id)).FirstOrDefault();
                if (khoichucnangE != null)
                {
                    var listPBRemove = string.IsNullOrEmpty(removes) ? new List<string>() : removes.Split(",").ToList();
                    var phongbans = dbContext.PhongBans.Find(m => m.Enable.Equals(true) && m.KhoiChucNangId.Equals(id) && !listPBRemove.Contains(m.Id)).ToList();

                    watch.Stop();
                    elapsedMs = watch.ElapsedMilliseconds;
                    return Json(new { elapsedMs = elapsedMs + "ms", result = true, khoichucnang = khoichucnangE, phongbans });
                }

                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
                return Json(new { elapsedMs = elapsedMs + "ms", result = false });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = ex.Message });
            }
        }

        public JsonResult GetByPhongBan(string id, string kcn)
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var elapsedMs = watch.ElapsedMilliseconds;
                var khoichucnangE = new KhoiChucNang();
                //if (!string.IsNullOrEmpty(kcn))
                //{
                //    khoichucnangE = dbContext.KhoiChucNangs.Find(m => m.Enable.Equals(true) && m.Id.Equals(kcn)).FirstOrDefault();
                //    if (khoichucnangE == null)
                //    {
                //        khoichucnangE = dbContext.KhoiChucNangs.Find(m => m.Enable.Equals(true)).FirstOrDefault();
                //    }
                //}
                //else
                //{
                //    khoichucnangE = dbContext.KhoiChucNangs.Find(m => m.Enable.Equals(true)).FirstOrDefault();
                //}

                //var phongbans = dbContext.PhongBans.Find(m => m.Enable.Equals(true) && m.KhoiChucNangId.Equals(khoichucnangE.Id)).SortBy(m => m.Order).ToList();
                var phongBanE = new PhongBan();
                if (!string.IsNullOrEmpty(id))
                {
                    phongBanE = dbContext.PhongBans.Find(m => m.Id.Equals(id)).FirstOrDefault();
                    if (phongBanE != null)
                    {
                        var phongbans = dbContext.PhongBans.Find(m => m.KhoiChucNangId.Equals(phongBanE.KhoiChucNangId)).ToList();

                        var bophans = dbContext.BoPhans.Find(m => m.Enable.Equals(true) && m.PhongBanId.Equals(phongBanE.Id)).ToList();

                        var managers = dbContext.ChucVus.Find(m => m.Enable.Equals(true) && m.PhongBanId.Equals(phongBanE.Id)).ToList();

                        watch.Stop();
                        elapsedMs = watch.ElapsedMilliseconds;
                        return Json(new { elapsedMs = elapsedMs + "ms", result = true, phongban = phongBanE, phongbans, bophans, managers });
                    }
                }

                var lastestE = dbContext.PhongBans.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
                var lastestCode = lastestE != null ? lastestE.Order + 1 : 1;
                phongBanE = new PhongBan()
                {
                    Order = lastestCode,
                    Enable = true
                };
                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
                return Json(new { elapsedMs = elapsedMs + "ms", result = true, phongban = phongBanE });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = ex.Message });
            }
        }

        public JsonResult GetByBoPhan(string bophan)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var elapsedMs = watch.ElapsedMilliseconds;
            var bophanEntity = dbContext.BoPhans.Find(m => m.Id.Equals(bophan)).FirstOrDefault();
            if (bophanEntity == null)
            {
                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
                return Json(new { elapsedMs = elapsedMs + "ms", result = false });
            }

            var bophancons = dbContext.BoPhans.Find(m => m.Enable.Equals(true) && m.Parent.Equals(bophan)).ToList();
            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            return Json(new { elapsedMs = elapsedMs + "ms", result = true, bophancons });
        }

        public JsonResult GetCongTyChiNhanh()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var elapsedMs = watch.ElapsedMilliseconds;

            var congtychinhanhs = dbContext.CongTyChiNhanhs.Find(m => m.Enable.Equals(true)).ToList();

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            return Json(new { elapsedMs = elapsedMs + "ms", result = true, congtychinhanhs });
        }

        public JsonResult GetByCongTyChiNhanh(string congtychinhanh)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var khoichucnangs = dbContext.KhoiChucNangs.Find(m => m.Enable.Equals(true) && m.CongTyChiNhanhId.Equals(congtychinhanh)).ToList();
            //var khoichucnangsts = khoichucnangs.Select(x => x.Id).ToList();
            //var phongbans = dbContext.PhongBans.Find(m => m.Enable.Equals(true) && khoichucnangsts.Contains(m.KhoiChucNangId)).ToList();
            //var phongbansts = phongbans.Select(x => x.Id).ToList();
            //var bophans = dbContext.BoPhans.Find(m => m.Enable.Equals(true) && phongbansts.Contains(m.PhongBanId)).ToList();
            //var bophansts = bophans.Select(x => x.Id).ToList();
            //var bophancons = dbContext.BoPhans.Find(m => m.Enable.Equals(true) && bophansts.Contains(m.Parent)).ToList();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            return Json(new { elapsedMs = elapsedMs + "ms", result = true, khoichucnangs });
            // return Json(new { elapsedMs = elapsedMs + "ms", result = true, khoichucnangs, phongbans, bophans, bophancons });
        }

        public JsonResult GetKhoiChucNang(string congtychinhanh)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var elapsedMs = watch.ElapsedMilliseconds;

            var khoichucnangs = new List<KhoiChucNang>();

            if (!string.IsNullOrEmpty(congtychinhanh))
            {
                khoichucnangs = dbContext.KhoiChucNangs.Find(m => m.Enable.Equals(true) && m.CongTyChiNhanhId.Equals(congtychinhanh)).ToList();
                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
                return Json(new { elapsedMs = elapsedMs + "ms", result = true, khoichucnangs });
            }

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            return Json(new { elapsedMs = elapsedMs + "ms", result = false });
        }

        public JsonResult GetPhongBan(string khoichucnang)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var elapsedMs = watch.ElapsedMilliseconds;

            var phongbans = new List<PhongBan>();

            if (!string.IsNullOrEmpty(khoichucnang))
            {
                phongbans = dbContext.PhongBans.Find(m => m.Enable.Equals(true) && m.KhoiChucNangId.Equals(khoichucnang)).ToList();
                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
                return Json(new { elapsedMs = elapsedMs + "ms", result = true, phongbans });
            }

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            return Json(new { elapsedMs = elapsedMs + "ms", result = false });
        }

        public JsonResult GetChucVu(string congtychinhanh, string khoichucnang, string phongban, string bophan)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var elapsedMs = watch.ElapsedMilliseconds;

            var chucvus = new List<ChucVu>();

            if (!string.IsNullOrEmpty(phongban))
            {
                chucvus = dbContext.ChucVus.Find(m => m.Enable.Equals(true) && m.PhongBanId.Equals(phongban)).ToList();
                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
                return Json(new { elapsedMs = elapsedMs + "ms", result = true, chucvus });
            }
            else if (!string.IsNullOrEmpty(khoichucnang))
            {
                chucvus = dbContext.ChucVus.Find(m => m.Enable.Equals(true) && m.KhoiChucNangId.Equals(khoichucnang)).ToList();
                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
                return Json(new { elapsedMs = elapsedMs + "ms", result = true, chucvus });
            }

            chucvus = dbContext.ChucVus.Find(m => m.Enable.Equals(true) && m.CongTyChiNhanhId.Equals(congtychinhanh)).ToList();
            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            return Json(new { elapsedMs = elapsedMs + "ms", result = true, chucvus });
        }

        public IActionResult KhoiChucNang(KhoiChucNang entity)
        {
            try
            {
                if (string.IsNullOrEmpty(entity.Name))
                {
                    return Json(new { result = false, source = "create", entity, message = "Tên không để trống! Vui lòng kiểm tra tên." });
                }

                entity.Alias = Utility.AliasConvert(entity.Name);
                if (string.IsNullOrEmpty(entity.Id))
                {
                    var existE = dbContext.KhoiChucNangs.Find(m => m.Alias.Equals(entity.Alias)).FirstOrDefault();
                    if (existE == null)
                    {
                        //var lastestE = dbContext.KhoiChucNangs.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
                        //var lastestCode = lastestE != null ? lastestE.Order + 1 : 1;
                        entity.Code = "KHOI" + entity.Order;
                        entity.Order = entity.Order;
                        dbContext.KhoiChucNangs.InsertOne(entity);
                        return Json(new { result = true, source = "create", entity, message = Constants.DataSuccess });
                    }
                    entity.Id = existE.Id;
                }

                var filter = Builders<KhoiChucNang>.Filter.Eq(m => m.Id, entity.Id);
                var update = Builders<KhoiChucNang>.Update
                    .Set(m => m.CongTyChiNhanhId, entity.CongTyChiNhanhId)
                    .Set(m => m.Name, entity.Name)
                    .Set(m => m.Alias, entity.Alias)
                    .Set(m => m.Description, entity.Description)
                    .Set(m => m.Order, entity.Order)
                    .Set(m => m.Code, "KHOI" + entity.Order)
                    .Set(m => m.Enable, entity.Enable);
                dbContext.KhoiChucNangs.UpdateOne(filter, update);

                #region Relations
                var filterEmployee = Builders<Employee>.Filter.Eq(m => m.KhoiChucNang, entity.Id);
                var updateEmployee = Builders<Employee>.Update
                    .Set(m => m.KhoiChucNangName, entity.Name);
                dbContext.Employees.UpdateMany(filterEmployee, updateEmployee);
                #endregion

                return Json(new { result = true, source = "create", entity, message = Constants.DataSuccess });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "create", entity, message = ex.Message });
            }
        }

        public IActionResult PhongBan(PhongBan entity)
        {
            try
            {
                if (string.IsNullOrEmpty(entity.Name))
                {
                    return Json(new { result = false, source = "create", entity, message = "Tên không để trống! Vui lòng kiểm tra tên." });
                }

                entity.Alias = Utility.AliasConvert(entity.Name);
                if (string.IsNullOrEmpty(entity.Id))
                {
                    var existE = dbContext.PhongBans.Find(m => m.Alias.Equals(entity.Alias) && m.KhoiChucNangId.Equals(entity.KhoiChucNangId)).FirstOrDefault();
                    if (existE == null)
                    {
                        entity.Code = "PHONGBAN" + entity.Order;
                        entity.Order = entity.Order;
                        dbContext.PhongBans.InsertOne(entity);
                        return Json(new { result = true, source = "create", entity, message = Constants.DataSuccess });
                    }
                    entity.Id = existE.Id;
                }

                var filter = Builders<PhongBan>.Filter.Eq(m => m.Id, entity.Id);
                var update = Builders<PhongBan>.Update
                    .Set(m => m.KhoiChucNangId, entity.KhoiChucNangId)
                    .Set(m => m.Name, entity.Name)
                    .Set(m => m.Alias, entity.Alias)
                    .Set(m => m.Description, entity.Description)
                    .Set(m => m.Order, entity.Order)
                    .Set(m => m.Code, "PHONGBAN" + entity.Order)
                    .Set(m => m.Enable, entity.Enable);
                dbContext.PhongBans.UpdateOne(filter, update);

                #region Relations
                var filterEmployee = Builders<Employee>.Filter.Eq(m => m.PhongBan, entity.Id);
                var updateEmployee = Builders<Employee>.Update
                    .Set(m => m.PhongBanName, entity.Name);
                dbContext.Employees.UpdateMany(filterEmployee, updateEmployee);
                #endregion

                return Json(new { result = true, source = "create", entity, message = Constants.DataSuccess });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "create", entity, message = ex.Message });
            }
        }

        public IActionResult BoPhan(Part entity)
        {
            entity.Alias = Utility.AliasConvert(entity.Name);
            bool exist = dbContext.Parts.CountDocuments(m => m.Alias.Equals(entity.Alias)) > 0;
            if (!exist)
            {
                dbContext.Parts.InsertOne(entity);
                return Json(new { result = true, source = "create", entity, message = Constants.NewDataSuccess });
            }
            return Json(new { result = false, source = "create", entity, message = Constants.DataDuplicate });
        }

        public IActionResult ChucVu(ChucVu entity)
        {
            if (!string.IsNullOrEmpty(entity.Name))
            {
                entity.Alias = Utility.AliasConvert(entity.Name);
                bool exist = dbContext.ChucVus.CountDocuments(m => m.Alias.Equals(entity.Alias)) > 0;
                if (!exist)
                {
                    var lastestChucVu = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).Limit(1).FirstOrDefault();
                    var lastestCode = lastestChucVu != null ? lastestChucVu.Order + 1 : 1;
                    entity.Code = "CHUCVU" + lastestCode;
                    entity.Order = lastestCode;
                    dbContext.ChucVus.InsertOne(entity);
                    return Json(new { result = true, source = "create", entity, message = Constants.NewDataSuccess });
                }
            }

            return Json(new { result = false, source = "create", entity, message = Constants.DataDuplicate });
        }

        [HttpPost]
        [Route(Constants.LinkHr.Hospital + " / " + Constants.ActionLink.Update)]
        public IActionResult Hospital(BHYTHospital entity)
        {
            entity.Alias = Utility.AliasConvert(entity.Name);
            bool exist = dbContext.BHYTHospitals.CountDocuments(m => m.Alias.Equals(entity.Alias)) > 0;
            if (!exist)
            {
                dbContext.BHYTHospitals.InsertOne(entity);
                return Json(new { result = true, source = "create", entity, message = Constants.NewDataSuccess });
            }
            return Json(new { result = false, source = "create", entity, message = Constants.DataDuplicate });
        }
        #endregion

        //[HttpGet("employees/{type}/{term}")]
        [HttpGet("employees")]
        public JsonResult Get(string type, string term)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (type == "code")
            {
                filter = filter & builder.Regex(i => i.Code, term);
            }
            else
            {
                term = Utility.AliasConvert(term);
                filter = filter & (builder.Regex(i => i.AliasFullName, term) | builder.Regex(i => i.Email, term));
            }
            #endregion

            #region Sort
            var sort = Builders<Employee>.Sort.Ascending(m => m.Code);
            #endregion

            var fields = Builders<Employee>.Projection.Include(p => p.Code).Include(p => p.FullName).Include(p => p.AliasFullName).Include(p => p.Email).Include(p => p.ChucVu);

            var outputs = dbContext.Employees.Find(filter).Project<Employee>(fields).Sort(sort).Limit(20).ToList();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            return Json(new { elapsedMs = elapsedMs + "ms", outputs });
        }

        // GET api/get-employees
        [HttpGet("get-employees")]
        public ActionResult<List<Employee>> GetEmployees()
        {
            return dbContext.Employees.Find(m => m.Enable.Equals(true)).ToList();
        }

        [HttpGet("get-employee/{id}")]
        public ActionResult<Employee> GetEmployee(string id)
        {
            return dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Id.Equals(id)).FirstOrDefault();
        }

        [Route("employee-filter")]
        public ActionResult<List<Employee>> GetEmployeesFilter(string part, string department, string title)
        {
            // Add a Thao, a Pari
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true) & !builder.Eq(m => m.UserName, Constants.System.account);
            var employees = dbContext.Employees.Find(filter).ToList();
            //var hdtvs = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Part.Equals("HĐTV")).ToList();
            //foreach (var hdtv in hdtvs)
            //{
            //    employees.Add(hdtv);
            //}
            return employees;
        }

        // use basic
        // api/version?namelike=787
        [HttpGet("version")]
        public string Version(string namelike)
        {
            return "Version 1.0.0" + namelike;
        }

        // Multi condition
        [HttpGet("version/{alias}/{aaa}")]
        public string Version2(string alias, string aaa)
        {
            return "Version 1.0.0 " + alias + " " + aaa;
        }

        #region Template [Microsoft]
        //// GET: api/<controller>
        //[HttpGet]
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        //// GET api/<controller>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/<controller>
        //[HttpPost]
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT api/<controller>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/<controller>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Data;
using Services;
using Models;
using Helpers;
using System.IO;
using MimeKit;
using Common.Utilities;

namespace erp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        IHostingEnvironment _env;
        private IHttpContextAccessor _accessor;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public AccountController(
            IHttpContextAccessor accessor, 
            IConfiguration configuration, 
            IHostingEnvironment env,
            IEmailSender emailSender,
            ISmsSender smsSender, 
            ILogger<AccountController> logger)
        {
            _accessor = accessor;
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        //[HttpGet, Route("/login/{url?}")]
        //[HttpGet]
        [AllowAnonymous]
        [Route("/tk/dang-nhap/")]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var remoteIpAddress = _accessor.HttpContext.Connection.RemoteIpAddress;
            //ViewData["IpAddress"] = remoteIpAddress.ToString();
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("/tk/dang-nhap/")]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            var result = dbContext.Employees.Find(m => m.Enable.Equals(true)
                                                    && m.Leave.Equals(false)
                                                    && m.IsOnline.Equals(true)
                                                    && (m.UserName.Equals(model.UserName.Trim()) || m.Email.Equals(model.UserName.Trim()))
                                                    && m.Password.Equals(Helper.HashedPassword(model.Password.Trim())))
                                                    .FirstOrDefault();
            if (result != null)
            {
                var claims = new List<Claim>
                    {
                        new Claim("UserName", string.IsNullOrEmpty(result.UserName) ? string.Empty : result.UserName),
                        new Claim(ClaimTypes.Name, result.Id),
                        new Claim(ClaimTypes.Email, string.IsNullOrEmpty(result.Email) ? string.Empty : result.Email),
                        new Claim("FullName", string.IsNullOrEmpty(result.FullName) ? string.Empty : result.FullName)
                    };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                };
                if (model.RememberMe)
                {
                    authProperties.IsPersistent = true;
                }
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                var ipAddress = string.Empty;
                try
                {
                    ipAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
                    if (!string.IsNullOrEmpty(ipAddress) && ipAddress != "::1")
                    {
                        dbContext.Ips.InsertOne(new Ip()
                        {
                            IpAddress = ipAddress,
                            Login = result.Id
                        });
                    }
                }
                catch (Exception ex)
                {
                }
                   
                if (string.IsNullOrEmpty(returnUrl))
                {
                    returnUrl = "/";
                }
                return Redirect(returnUrl);
            }
            else
            {
                var message = "Thông tin đăng nhập không đúng. Kiểm tra khoảng cách, bàn phím, bộ gõ...";
                ModelState.AddModelError("", message);
                return View();
            }
        }

        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation($"User {User.Identity.Name} logged out at {DateTime.UtcNow}.");

            #region snippet1
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            #endregion

            return RedirectToAction("login");
        }

        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        //{
        //    ViewData["ReturnUrl"] = returnUrl;
        //    if (ModelState.IsValid)
        //    {
        //        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        //        var result = await _userManager.CreateAsync(user, model.Password);
        //        if (result.Succeeded)
        //        {
        //            _logger.LogInformation("User created a new account with password.");

        //            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        //            var callbackUrl = Url.EmailConfirmationLink(user.Id.ToString(), code, Request.Scheme);
        //            await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

        //            await _signInManager.SignInAsync(user, isPersistent: false);
        //            _logger.LogInformation("User created a new account with password.");
        //            return RedirectToLocal(returnUrl);
        //        }
        //        AddErrors(result);
        //    }

        //    // If we got this far, something failed, redisplay form
        //    return View(model);
        //}

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction("Index", "Home");
            }
            //var user = await _userManager.FindByIdAsync(userId);
            var user = dbContext.Employees.Find(m => m.Id.Equals(userId)).FirstOrDefault();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }
            //var result = await _userManager.ConfirmEmailAsync(user, code);
            var result = true;
            //return View(result.Succeeded ? "ConfirmEmail" : "Error");
            return View(result ? "ConfirmEmail" : "Error");
        }

        [AllowAnonymous]
        [Route("/tk/quen-mat-khau/")]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("/tk/quen-mat-khau/")]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            var user = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.Email.Equals(model.Email)).FirstOrDefault();
            if (user == null)
            {
                return Redirect("/tk/ket-qua-quen-mat-khau/");
            }

            var password = Guid.NewGuid().ToString("N").Substring(0, 12);
            var sysPassword = Helper.HashedPassword(password);
            //var userName = 
            var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, user.Id);
            var update = Builders<Employee>.Update
                .Set(m => m.Password, sysPassword);
            dbContext.Employees.UpdateOne(filterUpdate, update);
            // Send mail
            SendMailRegister(user, password);
            return Redirect("/tk/ket-qua-quen-mat-khau/");
        }

        [AllowAnonymous]
        [Route("/tk/ket-qua-quen-mat-khau/")]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
            }
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            //var user = await _userManager.FindByEmailAsync(model.Email);
            var user = dbContext.Employees.Find(m => m.Email.Equals(model.Email)).FirstOrDefault();
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            //var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            //if (result.Succeeded)
            //{
            //    return RedirectToAction(nameof(ResetPasswordConfirmation));
            //}
            //AddErrors(result);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        //[HttpGet]
        //public IActionResult NotFound()
        //{
        //    return View();
        //}

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public void SendMailRegister(Employee entity, string pwd)
        {
            var title = string.Empty;
            if (!string.IsNullOrEmpty(entity.Gender))
            {
                if (entity.AgeBirthday > 50)
                {
                    title = entity.Gender == "Nam" ? "anh" : "chị";
                }
            }
            var url = Constants.System.domain;
            var subject = "Thông tin đăng nhập hệ thống.";
            var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = entity.FullName, Address = entity.Email }
            };
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "Confirm_Account_Registration.html";
            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }
            string messageBody = string.Format(builder.HtmlBody,
                subject,
                title + " " + entity.FullName,
                url,
                entity.UserName,
                pwd,
                entity.Email);

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                Subject = subject,
                BodyContent = messageBody,
                EmployeeId = entity.Id
            };

            _emailSender.SendEmail(emailMessage);
        }
        #endregion

        #region Update pwd
        [HttpGet]
        [AllowAnonymous]
        public IActionResult UpdatePassword()
        {
            var loginId = User.Identity.Name;

            var model = new UpdatePasswordViewModel();
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePassword(UpdatePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            //var user = await _userManager.FindByEmailAsync(model.Email);
            var user = dbContext.Employees.Find(m => m.Email.Equals(model.Email)).FirstOrDefault();
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            //var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            //if (result.Succeeded)
            //{
            //    return RedirectToAction(nameof(ResetPasswordConfirmation));
            //}
            //AddErrors(result);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult UpdatePasswordConfirmation()
        {
            return View();
        }
        #endregion
    }
}

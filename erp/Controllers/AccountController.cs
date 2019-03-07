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
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public AccountController(
            IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, IEmailSender emailSender,
            ISmsSender smsSender, ILogger<AccountController> logger)
        {
            _cache = cache;
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
            // If the user is already authenticated we do not need to display the login page, so we redirect to the landing page.
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

            // Clear the existing external cookie to ensure a clean login process
            //await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("/tk/dang-nhap/")]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            var result = dbContext.Employees.Find(m => m.Enable.Equals(true)
                                                    && m.IsOnline.Equals(true)
                                                    && (m.UserName.Equals(model.UserName.Trim()) || m.Email.Equals(model.UserName.Trim()))
                                                    && m.Password.Equals(Helper.HashedPassword(model.Password.Trim())))
                                                    .FirstOrDefault();
            // Write log, perfomance...
            if (result != null)
            {
                var claims = new List<Claim>
                    {
                        new Claim("UserName", result.UserName),
                        new Claim(ClaimTypes.Name, result.Id),
                        new Claim(ClaimTypes.Email, string.IsNullOrEmpty(result.Email) ? string.Empty : result.Email),
                        new Claim("FullName", result.FullName)
                    };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    //AllowRefresh = <bool>,
                    // Refreshing the authentication session should be allowed.

                    //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                    // The time at which the authentication ticket expires. A 
                    // value set here overrides the ExpireTimeSpan option of 
                    // CookieAuthenticationOptions set with AddCookie.

                    //IsPersistent = true,
                    // Whether the authentication session is persisted across 
                    // multiple requests. Required when setting the 
                    // ExpireTimeSpan option of CookieAuthenticationOptions 
                    // set with AddCookie. Also required when setting 
                    // ExpiresUtc.

                    //IssuedUtc = <DateTimeOffset>,
                    // The time at which the authentication ticket was issued.

                    //RedirectUri = <string>
                    // The full path or absolute URI to be used as an http 
                    // redirect response value.
                };

                if (model.RememberMe)
                {
                    authProperties.IsPersistent = true;
                    //authProperties.ExpiresUtc = DateTime.UtcNow.AddDays(7);
                }

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

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
                return RedirectToAction(nameof(HomeController.Index), "Home");
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
            var user = dbContext.Employees.Find(m => m.Email.Equals(model.Email)).FirstOrDefault();
            if (user == null)
            {
                return Redirect("/tk/ket-qua-quen-mat-khau/");
            }

            var password = Guid.NewGuid().ToString("N").Substring(0, 12);
            var sysPassword = Helper.HashedPassword(password);

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
                return RedirectToAction(nameof(HomeController.Index), "Home");
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
                BodyContent = messageBody
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

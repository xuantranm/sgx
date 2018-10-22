using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Data;
using ViewModels;
using Models;
using Common.Utilities;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver.Linq;
using System.Security.Claims;
using System.Threading;
using MimeKit;
using Services;
using Common.Enums;
using MongoDB.Bson;

namespace erp.Controllers
{
    [Authorize]
    public class AFixDataController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        readonly IHostingEnvironment _env;

        private readonly ILogger _logger;

        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public IConfiguration Configuration { get; }

        public AFixDataController(IDistributedCache cache,
            IConfiguration configuration,
            IHostingEnvironment env,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILogger<AFixDataController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        [Route("fix/email-wrong")]
        public async Task<IActionResult> EmailWrong()
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var userInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (userInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }
            if (loginUserName != Constants.System.account)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion
            
            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(i => i.UserName, Constants.System.account);
            #endregion

            var password = string.Empty;
            var detail = string.Empty;
            var employees = dbContext.Employees.Find(filter).ToList();
            var intError = 0;
            var intFixed = 0;
            var intUnFix = 0;
            foreach (var employee in employees)
            {
                if (!string.IsNullOrEmpty(employee.Email) && !Utility.IsValidEmail(employee.Email))
                {
                    var emailGenerate = Utility.EmailConvert(employee.FullName);
                    // Update password
                    password = Guid.NewGuid().ToString("N").Substring(0, 6);
                    var sysPassword = Helpers.Helper.HashedPassword(password);

                    if (dbContext.Employees.CountDocuments(m => m.Email.Equals(emailGenerate)) == 0)
                    {
                        var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                        var update = Builders<Employee>.Update
                            .Set(m => m.Email, emailGenerate)
                            .Set(m => m.Password, sysPassword);
                        dbContext.Employees.UpdateOne(filterUpdate, update);

                        // Send email information
                        employee.Email = emailGenerate;
                        SendMailRegister(employee, password);
                        intFixed++;
                    }
                    else
                    {
                        dbContext.Errors.InsertOne(new Error() {
                            Type = "email",
                            Message = employee.FullName +  " không đúng định dạnh email, " + emailGenerate + " đã được sử dụng."
                        });
                        intUnFix++;
                    }

                    intError++;
                }
            }

            dbContext.SystemReports.InsertOne(new SystemReport()
            {
                Type = "email-wrong",
                Detail = "Lỗi: " + intError + ". Fixed: " + intFixed +". Unfix: " + intUnFix
            });

            return Json(new { result = true });
        }

        [Route("/fix/employee-code/")]
        public async Task<IActionResult> UpdateEmployeeCode()
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var userInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (userInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }
            if (loginUserName != Constants.System.account)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion
            
            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = !builder.Eq(m => m.UserName, Constants.System.account);
            #endregion

            var employees = dbContext.Employees.Find(filter).ToList();
            var i = 1;
            foreach (var employee in employees)
            {
                var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.Code, "LD-" + i.ToString("0000"));
                dbContext.Employees.UpdateOne(filterUpdate, update);
                i++;
            }

            return Json(new { result = true, source = "update-employee-code", message = "Cập nhật mã nhân viên thành công" });
        }

        public void SendMailRegister(Employee entity, string pwd)
        {
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
                entity.FullName,
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

            //_emailSender.SendEmail(emailMessage);
        }
    }
}
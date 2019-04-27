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
using MimeKit.Text;
using MailKit.Net.Smtp;
using System.Globalization;
using Helpers;

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

        public void SendMailRegister(Employee entity)
        {
            var password = Guid.NewGuid().ToString("N").Substring(0, 12);
            var sysPassword = Helper.HashedPassword(password);

            var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, entity.Id);
            var update = Builders<Employee>.Update
                .Set(m => m.Password, sysPassword);
            dbContext.Employees.UpdateOne(filterUpdate, update);

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
                password,
                entity.Email);

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                Subject = subject,
                BodyContent = messageBody,
                Type = "thong-tin-dang-nhap"
            };

            // For faster update.
            var scheduleEmail = new ScheduleEmail
            {
                Status = (int)EEmailStatus.Schedule,
                To = emailMessage.ToAddresses,
                CC = emailMessage.CCAddresses,
                BCC = emailMessage.BCCAddresses,
                Type = emailMessage.Type,
                Title = emailMessage.Subject,
                Content = emailMessage.BodyContent
            };
            dbContext.ScheduleEmails.InsertOne(scheduleEmail);
            //_emailSender.SendEmail(emailMessage);
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
                        //dbContext.Employees.UpdateOne(filterUpdate, update);

                        // Send email information
                        employee.Email = emailGenerate;
                        //SendMailRegister(employee, password);
                        intFixed++;
                    }
                    else
                    {
                        dbContext.Errors.InsertOne(new Error()
                        {
                            Type = "email",
                            Message = employee.FullName + " không đúng định dạnh email, " + emailGenerate + " đã được sử dụng."
                        });
                        intUnFix++;
                    }

                    intError++;
                }
            }

            dbContext.SystemReports.InsertOne(new SystemReport()
            {
                Type = "email-wrong",
                Detail = "Lỗi: " + intError + ". Fixed: " + intFixed + ". Unfix: " + intUnFix
            });

            return Json(new { result = true });
        }

        [Route("fix/email-live")]
        public async Task<IActionResult> EmailLive()
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

            var listboss = new List<string>
            {
                "C.01",
                "C.02"
            };
            var input = "Nguyễn Chu Thy, Nguyễn Thị Hồng Ánh, Đỗ Ngọc Thiên Thanh, Nguyễn Thọ Ngọc, Nguyễn Trinh Nguyên, Kiều Thị Thủy Tiên, Huỳnh Ngọc Giang, Ninh Phương Hạnh, Nguyễn Thái Ngân, Từ Thị Hoàng Oanh, Nguyễn Thị Phượng Nhi, Châu Phước Thuần, Đào Ngọc Long, Nguyễn Duy Long, Nguyễn Trần Duy Anh, Nguyễn Ngọc Đông, Trần Văn Vị Toàn, Võ Thị Khánh Huyền, Thái Hồng Phát, Lê Công Nhất Trung, Cao Xuân Vũ, Trần Văn Hà, Đỗ Thanh Tú, Trần Thị Minh Thương, Nguyễn Anh Tuấn, Đặng Nguyễn Thế Anh, Thạch Minh Châu, Phan Thanh Tùng, Trịnh Minh Hảo, Nguyễn Đức Trung, Nguyễn Minh Đại, Trần Ngọc Thạch, Cao Thị Minh Thoa, Ngô Đình Chung, Đồng Tấn Tài, Mai Thanh Điền, Nguyễn Văn Thanh, Nguyễn Hùng Dũng, Đào Công Thắng, Nguyễn Thanh Nhàn, Đổng Ngọc Trung, Nguyễn Thành Long, Chau Ri Na, Võ Thị Thanh Thủy, Phạm Tấn Hưng, Mai Hoàng Việt, Đinh Hiệp Thương, Nguyễn Ngọc Lâm, Trần Minh Xuân, Lê Hà, Dương Thạch Quang, Lê Thị Duyên, Trần Minh Tâm, Nguyễn Hữu Thái, Phan Đông, Dương Ngọc Bảo Phương, Nguyễn Thái Bình, Nguyễn Ngọc Hồng Vy, Nguyễn Văn Bích, Lê Đình Tiến, Hoàng Văn Nhân, La Đường, Huỳnh Ngọc Minh, Nguyễn Huy Cường, Nguyễn Hoàng Khang, Nguyễn Quốc Tâm, Nguyễn Xuân Lĩnh, Đặng Văn Thông, Trần Ngọc Bảo Long, Phan Thị Trà Hiên, Nguyễn Tấn Duy Tùng";
            var list = input.Split(',').Select(p => p.Trim()).ToList();

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && !string.IsNullOrEmpty(m.Email) && !m.UserName.Equals(Constants.System.account) && !listboss.Contains(m.NgachLuongCode) && !list.Contains(m.FullName)).ToList();

            var intError = 0;
            var emailMessage = new EmailMessage()
            {
                Subject = "Anh thợ điện bị phạt 90 triệu vì đổi 100 USD tại tiệm vàng",
                BodyContent = "https://news.zing.vn/anh-tho-dien-bi-phat-90-trieu-vi-doi-100-usd-tai-tiem-vang-post886675.html . Xin lỗi đã làm phiền. Email này chỉ để kiểm tra email còn hoạt động hay không.",
                Type = "email-check-live",
                FromAddresses = new List<EmailAddress>
                {
                    new EmailAddress { Name = "Hỗ trợ", Address = "test-erp@tribat.vn", Pwd = "Kh0ngbiet@123"}
                }
            };
            foreach (var employee in employees)
            {
                // send email test, if send fail, update null email
                var message = new MimeMessage
                {
                    Subject = emailMessage.Subject,
                    Body = new TextPart(TextFormat.Html)
                    {
                        Text = emailMessage.BodyContent
                    }
                };
                message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));

                if (!Utility.IsValidEmail(employee.Email))
                {
                    employee.Email = Utility.EmailConvert(employee.FullName);
                    var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                    var update = Builders<Employee>.Update
                        .Set(m => m.Email, employee.Email);
                    dbContext.Employees.UpdateOne(filter, update);
                }
                var tos = new List<EmailAddress>
                    {
                        new EmailAddress()
                        {
                            Address = employee.Email,
                            Name = employee.FullName
                        }
                    };

                message.To.AddRange(tos.Select(x => new MailboxAddress(x.Name, x.Address)));
                try
                {
                    using (var emailClient = new SmtpClient())
                    {
                        //The last parameter here is to use SSL (Which you should!)
                        emailClient.Connect(emailMessage.FromAddresses.First().Address, 465, true);

                        //Remove any OAuth functionality as we won't be using it. 
                        emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                        emailClient.Authenticate(emailMessage.FromAddresses.First().Address, emailMessage.FromAddresses.First().Pwd);

                        emailClient.Send(message);

                        emailClient.Disconnect(true);
                    }
                }
                catch (Exception ex)
                {
                    var error = ex.Message;
                    var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                    var update = Builders<Employee>.Update
                        .Set(m => m.Email, null);
                    dbContext.Employees.UpdateOne(filter, update);
                    intError++;
                }
            }

            dbContext.SystemReports.InsertOne(new SystemReport()
            {
                Type = "email-live",
                Detail = "Lỗi: " + intError
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
                    .Set(m => m.Code, "NV" + i.ToString("000"));
                dbContext.Employees.UpdateOne(filterUpdate, update);
                i++;
            }

            return Json(new { result = true, source = "update-employee-code", message = "Cập nhật mã nhân viên thành công" });
        }

        [Route("fix/update-time-nm")]
        public async Task<IActionResult> UpdateTimeNM()
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

            var listboss = new List<string>
            {
                "C.01",
                "C.02"
            };

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && !m.UserName.Equals(Constants.System.account)
            && !listboss.Contains(m.NgachLuongCode)).ToList();
            foreach (var employee in employees)
            {
                var isUpdate = false;
                if (employee.Workplaces != null && employee.Workplaces.Count > 0)
                {
                    var workPlaces = employee.Workplaces;
                    foreach (var workplace in workPlaces)
                    {
                        if (workplace.Code == "NM" && !string.IsNullOrEmpty(workplace.Fingerprint))
                        {
                            workplace.WorkingScheduleTime = "07:30-16:30";
                            isUpdate = true;
                        }
                    }
                    if (isUpdate)
                    {
                        var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                        var update = Builders<Employee>.Update
                            .Set(m => m.Workplaces, workPlaces);
                        dbContext.Employees.UpdateOne(filter, update);
                    }
                }
            }

            dbContext.SystemReports.InsertOne(new SystemReport()
            {
                Type = "update-time-nm",
                Detail = "Total: "
            });

            return Json(new { result = true });
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
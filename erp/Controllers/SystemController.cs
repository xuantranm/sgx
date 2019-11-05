using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Data;
using Models;
using Common.Utilities;
using ViewModels;
using Microsoft.AspNetCore.Authorization;
using Services;
using Common.Enums;

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.LinkSystem.Main)]
    public class SystemController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public SystemController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, IEmailSender emailSender,
            ISmsSender smsSender, ILogger<SystemController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        #region UI
        [Route(Constants.LinkSystem.Mail)]
        public ActionResult Email(string PhongBan, string Status, string Id, string MaNv, string ToEmail, int Page, int Size)
        {
            var phongbans = dbContext.PhongBans.Find(m => m.Enable.Equals(true)).ToList();
            #region CC: HR & Boss (if All)
            var tos = new List<EmailAddress>();
            var ccs = new List<EmailAddress>();
            var idsBoss = new List<string>();
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

            #endregion

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && !string.IsNullOrEmpty(m.Email)).ToList();

            var kinhdoanhs = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && !string.IsNullOrEmpty(m.Email) && m.PhongBan.Equals("5c88d094d59d56225c43244b")).ToList();
            var nhamays = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && !string.IsNullOrEmpty(m.Email) && m.CongTyChiNhanh.Equals("5c88d094d59d56225c43240b")).ToList();

            #region Filter
            var builder = Builders<ScheduleEmail>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!string.IsNullOrEmpty(Status))
            {
                filter = filter & builder.Eq(m => m.Status, Convert.ToInt32(Status));
            }
            if (!string.IsNullOrEmpty(MaNv))
            {
                filter = filter & builder.Eq(m => m.EmployeeId, MaNv);
            }
            if (!string.IsNullOrEmpty(Id))
            {
                filter = filter & builder.Eq(m => m.Id, Id);
            }
            if (!string.IsNullOrEmpty(ToEmail))
            {
                filter = filter & builder.ElemMatch(x => x.To, x => x.Address == ToEmail);
            }
            #endregion

            var pages = 1;
            var records = dbContext.ScheduleEmails.CountDocuments(filter);
            
            if (records > 0 && records > Size)
            {
                pages = (int)Math.Ceiling((double)records / (double)Size);
                if (Page > pages)
                {
                    Page = 1;
                }
            }
            var list = new List<ScheduleEmail>();
            if (records > 0 && records > Size)
            {
                list = dbContext.ScheduleEmails.Find(filter).Skip((Page - 1) * Size).Limit(Size).ToList();
            }
            else
            {
                list = dbContext.ScheduleEmails.Find(filter).ToList();
            }
            
            var viewModel = new MailViewModel
            {
                PhongBans = phongbans,
                PhongBan = PhongBan,
                Employees = employees,
                TOs = tos,
                CCs = ccs,
                ScheduleEmails = list,
                Records = (int)records,
                Pages = pages,
                Status = Status,
                Id = Id,
                ToEmail = ToEmail,
                MaNv = MaNv,
                Page = Page,
                Size = Size
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSystem.Mail + "/" + Constants.LinkSystem.Item)]
        public ActionResult EmailItem(string Id)
        {
            var item = dbContext.ScheduleEmails.Find(m => m.Id.Equals(Id)).FirstOrDefault();
            var viewModel = new MailViewModel
            {
                ScheduleEmail = item,
                Id = Id
            };

            return PartialView("_ContentEmailPartial", viewModel);
        }

        [Route(Constants.LinkSystem.Mail + "/" + Constants.LinkSystem.Resend)]
        public ActionResult Resend(MailViewModel viewModel)
        {
            var userId = User.Identity.Name;
            var item = dbContext.ScheduleEmails.Find(m => m.Id.Equals(viewModel.Id)).FirstOrDefault();
            
            if (item != null)
            {
                var tos = new List<EmailAddress>();
                if (!string.IsNullOrEmpty(viewModel.ToEmail))
                {
                    foreach (var email in viewModel.ToEmail.Split(";"))
                    {
                        tos.Add(new EmailAddress { Address = email });
                    }
                }
                var ccs = new List<EmailAddress>();
                if (!string.IsNullOrEmpty(viewModel.CcEmail))
                {
                    foreach (var email in viewModel.CcEmail.Split(";"))
                    {
                        ccs.Add(new EmailAddress { Address = email });
                    }
                }
                var emailMessage = new EmailMessage()
                {
                    ToAddresses = tos,
                    CCAddresses = ccs,
                    Subject = item.Title,
                    BodyContent = item.Content
                };

                _emailSender.SendEmail(emailMessage);

                // Update status send
                var filter = Builders<ScheduleEmail>.Filter.Eq(m => m.Id, viewModel.Id);
                var update = Builders<ScheduleEmail>.Update.Set(m => m.Status, 3)
                                                           .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.ScheduleEmails.UpdateOne(filter, update);
            }

            return Json(new { result = true, message = "Add new successfull." });
        }

        #endregion

        #region Init Data
        // system/du-lieu/init
        [Route("du-lieu/{setting}")]
        public ActionResult Get(string setting)
        {
            if (setting == "init")
            {
                _logger.LogInformation(LoggingEvents.GenerateItems, "Generate first data");

            }

            return Json(true);
        }
        #endregion

        public void InitSalaryMonthlyTypes()
        {
            dbContext.SalaryMonthlyTypes.DeleteMany(new BsonDocument());
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Thu nhập",
                Order = 1
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Số công ngày thường",
                Type = "thu-nhap",
                Unit = "ngày",
                Order = 1
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Làm việc ngày Lễ, Tết",
                Type = "thu-nhap",
                Unit = "ngày",
                Order = 2
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Tăng ca ngày thường",
                Type = "thu-nhap",
                Unit = "giờ",
                Order = 3
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Làm việc ngày CN",
                Type = "thu-nhap",
                Unit = "giờ",
                Order = 4
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Ngày phép hưởng lương",
                Type = "thu-nhap",
                Unit = "ngày",
                Order = 5
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Ngày Lễ, Tết hưởng lương",
                Type = "thu-nhap",
                Unit = "ngày",
                Order = 6
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Trợ cấp công tác xa",
                Type = "thu-nhap",
                Order = 7
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Lương khác",
                Type = "thu-nhap",
                Order = 8
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Thu nhập khác",
                Order = 2
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Lương theo hiệu quả",
                Type = "thu-nhap-khac",
                Order = 1
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Thi đua",
                Type = "thu-nhap-khac",
                Order = 2
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Thưởng Lễ Tết",
                Type = "thu-nhap-khac",
                Order = 3
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Hổ trợ ngoài",
                Type = "thu-nhap-khac",
                Order = 4
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Các khoản giảm trừ",
                Order = 3
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Bảo hiểm xã hội phải nộp",
                Type = "cac-khoan-giam-tru",
                Order = 1
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Tạm ứng",
                Type = "cac-khoan-giam-tru",
                Order = 2
            });
            dbContext.SalaryMonthlyTypes.InsertOne(new SalaryMonthlyType
            {
                Title = "Thuế TNCN",
                Type = "cac-khoan-giam-tru",
                Order = 3
            });

            // Update alias
            foreach (var item in dbContext.SalaryMonthlyTypes.Find(m => true).ToList())
            {
                var filter = Builders<SalaryMonthlyType>.Filter.Eq(m => m.Id, item.Id);

                var update = Builders<SalaryMonthlyType>.Update
                    .Set(m => m.Alias, Utility.AliasConvert(item.Title));

                dbContext.SalaryMonthlyTypes.UpdateOne(filter, update);
            }

        }

        public void InitBanks()
        {
            dbContext.Banks.DeleteMany(new BsonDocument());
            dbContext.Banks.InsertOne(new Bank
            {
                Name = "Ngân hàng Thương mại Cổ phần Tiên Phong",
                Shorten = "TPBank",
                Alias = "ngan-hang-thuong-mai-co-phan-tien-phong",
                Image = new Image
                {
                    Path = "images\banks",
                    FileName = "tpbank.png",
                    OrginalName = "tpbank.png"
                }
            });
        }

        public void InitSalaryContentTypes()
        {
            dbContext.SalaryContentTypes.DeleteMany(new BsonDocument());
            dbContext.SalaryContentTypes.InsertOne(new SalaryContentType
            {
                Name = "Chính"
            });
            dbContext.SalaryContentTypes.InsertOne(new SalaryContentType
            {
                Name = "Phụ cấp"
            });
            dbContext.SalaryContentTypes.InsertOne(new SalaryContentType
            {
                Name = "Phúc lợi khác"
            });
            // Update alias
            foreach (var item in dbContext.SalaryContentTypes.Find(m => true).ToList())
            {
                var filter = Builders<SalaryContentType>.Filter.Eq(m => m.Id, item.Id);

                var update = Builders<SalaryContentType>.Update
                    .Set(m => m.Alias, Utility.AliasConvert(item.Name));

                dbContext.SalaryContentTypes.UpdateOne(filter, update);
            }
        }

        public void InitSalaryContents()
        {
            dbContext.SalaryContents.DeleteMany(new BsonDocument());

            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                Name = "Lương theo bậc",
                Order = 1
            });

            #region phu-cap
            var phucap = dbContext.SalaryContentTypes.Find(m => m.Alias.Equals("phu-cap")).First();

            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                SalaryId = phucap.Id,
                SalaryAlias = phucap.Alias,
                SalaryType = phucap.Name,
                Name = "Nặng nhọc độc hại",
                Order = 2
            });
            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                SalaryId = phucap.Id,
                SalaryAlias = phucap.Alias,
                SalaryType = phucap.Name,
                Name = "Trách nhiệm",
                Order = 3
            });
            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                SalaryId = phucap.Id,
                SalaryAlias = phucap.Alias,
                SalaryType = phucap.Name,
                Name = "Thâm niên",
                Order = 4
            });
            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                SalaryId = phucap.Id,
                SalaryAlias = phucap.Alias,
                SalaryType = phucap.Name,
                Name = "Thu hút",
                Order = 5
            });
            #endregion

            #region phuc-loi-khac
            var phucloikhac = dbContext.SalaryContentTypes.Find(m => m.Alias.Equals("phuc-loi-khac")).First();

            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                SalaryId = phucloikhac.Id,
                SalaryAlias = phucloikhac.Alias,
                SalaryType = phucloikhac.Name,
                Name = "Xăng",
                Order = 6
            });
            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                SalaryId = phucloikhac.Id,
                SalaryAlias = phucloikhac.Alias,
                SalaryType = phucloikhac.Name,
                Name = "Điện thoại",
                Order = 7
            });
            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                SalaryId = phucloikhac.Id,
                SalaryAlias = phucloikhac.Alias,
                SalaryType = phucloikhac.Name,
                Name = "Cơm",
                Order = 8
            });
            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                SalaryId = phucloikhac.Id,
                SalaryAlias = phucloikhac.Alias,
                SalaryType = phucloikhac.Name,
                Name = "Kiểm nghiệm",
                Order = 9
            });
            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                SalaryId = phucloikhac.Id,
                SalaryAlias = phucloikhac.Alias,
                SalaryType = phucloikhac.Name,
                Name = "BHYT đặc biệt",
                Order = 10
            });
            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                SalaryId = phucloikhac.Id,
                SalaryAlias = phucloikhac.Alias,
                SalaryType = phucloikhac.Name,
                Name = "Vị trí cần KN nhiều năm",
                Order = 11
            });
            dbContext.SalaryContents.InsertOne(new SalaryContent
            {
                SalaryId = phucloikhac.Id,
                SalaryAlias = phucloikhac.Alias,
                SalaryType = phucloikhac.Name,
                Name = "Vị trí đặc thù",
                Order = 12
            });
            #endregion

            // Update alias
            foreach (var item in dbContext.SalaryContents.Find(m => true).ToList())
            {
                var filter = Builders<SalaryContent>.Filter.Eq(m => m.Id, item.Id);

                var update = Builders<SalaryContent>.Update
                    .Set(m => m.Alias, Utility.AliasConvert(item.Name));

                dbContext.SalaryContents.UpdateOne(filter, update);
            }
        }

        public void InitWorkingTime()
        {
            dbContext.WorkTimeTypes.DeleteMany(new BsonDocument());
            // hour. End + 9 (cause 1h lunch)
            for (double i = 0; i < 24; i += 0.5)
            {
                var start = TimeSpan.FromHours(i);
                var end = DateTime.Now.Date.AddHours(i + 9).TimeOfDay;

                dbContext.WorkTimeTypes.InsertOne(new WorkTimeType
                {
                    Start = start,
                    End = end,
                    CreatedBy = Constants.System.account,
                    UpdatedBy = Constants.System.account,
                    CheckedBy = Constants.System.account,
                    ApprovedBy = Constants.System.account,
                });
            }
        }

        public void InitContracts()
        {
            dbContext.ContractTypes.DeleteMany(new BsonDocument());
            var contracts = new List<ContractType>()
                            {
                                new ContractType()
                                {
                                    Name = "THỬ VIỆC"
                                },
                                new ContractType()
                                {
                                    Name = "THỜI VỤ"
                                },
                                new ContractType()
                                {
                                    Name = "HĐ XÁC ĐỊNH THỜI HẠN LẦN 1"
                                },
                                new ContractType()
                                {
                                    Name = "PHỤ LỤC GIA HẠN HĐ LẦN 1"
                                },
                                 new ContractType()
                                {
                                    Name = "HĐ XÁC ĐỊNH THỜI HẠN LẦN 2"
                                },
                                new ContractType()
                                {
                                    Name = "PHỤ LỤC GIA HẠN HĐ LẦN 2"
                                },
                                 new ContractType()
                                {
                                    Name = "HĐ XÁC ĐỊNH THỜI HẠN LẦN 3"
                                },
                                 new ContractType()
                                {
                                    Name = "HĐ KHÔNG XÁC ĐỊNH THỜI HẠN"
                                }
                            };
            foreach (var item in contracts)
            {
                dbContext.ContractTypes.InsertOne(item);
            }
        }

        public void InitHospital()
        {
            dbContext.BHYTHospitals.DeleteMany(new BsonDocument());
            dbContext.BHYTHospitals.InsertOne(new BHYTHospital()
            {
                Code = "79-025",
                Name = "Bệnh viện Thống Nhất",
            });
            dbContext.BHYTHospitals.InsertOne(new BHYTHospital()
            {
                Code = "79-011",
                Name = "Bệnh viện 30/4",
            });
            dbContext.BHYTHospitals.InsertOne(new BHYTHospital()
            {
                Code = "79-034",
                Name = "Bệnh viện 175",
            });
            dbContext.BHYTHospitals.InsertOne(new BHYTHospital()
            {
                Code = "79-023",
                Name = "Bệnh viện đa khoa Bưu điện - Cơ sở I",
            });
        }

        public void InitCompanies()
        {
            dbContext.Companies.DeleteMany(new BsonDocument());
            var companyLocations = new List<CompanyLocation>
            {
                new CompanyLocation(){ Address="127 Nguyễn Trọng Tuyển - P.15 - Q.Phú Nhuận - Tp HCM" },
                new CompanyLocation(){ Address="Xã Đa Phước, Q.Bình Chánh, Tp.HCM" }
            };
            dbContext.Companies.InsertOne(new Company()
            {
                Code = "tribat",
                Language = Constants.Languages.Vietnamese,
                Name = "Công ty TNHH CNSH SÀI GÒN XANH",
                Telephone = "02839971869",
                Telephone2 = "02838442457",
                Hotline = "0942464745",
                Fax = "02839971869",
                Tax = "0302519810",
                Email = "hotro@tribat.vn",
                CompanyLocations = companyLocations
            });
            dbContext.Companies.InsertOne(new Company()
            {
                Code = "tribat",
                Language = Constants.Languages.English,
                Name = "SAIGON GREEN BIOTECH CO., LTD",
                Telephone = "02839971869",
                Telephone2 = "02838442457",
                Hotline = "0942464745",
                Fax = "02839971869",
                Tax = "0302519810",
                Email = "hotro@tribat.vn",
                CompanyLocations = companyLocations
            });
        }

        public void InitBrands()
        {
            dbContext.Brands.DeleteMany(new BsonDocument());
            dbContext.Brands.InsertOne(new Brand()
            {
                Code = "VP",
                Language = Constants.Languages.Vietnamese,
                Name = "Văn phòng",
                Telephone = "02839971869",
                Telephone2 = "02838442457",
                Hotline = "0942464745",
                Fax = "02839971869",
                Email = "hotro@tribat.vn",
                Address = "127 Nguyễn Trọng Tuyển - P.15 - Q.Phú Nhuận - Tp HCM"
            });
            dbContext.Brands.InsertOne(new Brand()
            {
                Code = "NM",
                Language = Constants.Languages.Vietnamese,
                Name = "Nhà máy",
                Telephone = "02839971869",
                Telephone2 = "02838442457",
                Hotline = "0942464745",
                Fax = "02839971869",
                Email = "hotro@tribat.vn",
                Address = "Xã Đa Phước, Q.Bình Chánh, Tp.HCM"
            });
        }

        public void InitLanguages()
        {
            dbContext.Languages.DeleteMany(new BsonDocument());
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "zh",
                Name = "Chinese",
                Enable = false
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "en",
                Name = "English"
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "fr",
                Name = "French",
                Enable = false
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "ko",
                Name = "Korean",
                Enable = false
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "hi",
                Name = "Hindi",
                Enable = false
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "es",
                Name = "Spanish",
                Enable = false
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "vi",
                Name = "Vietnamese"
            });
            _cache.SetString(Constants.Collection.Languages, JsonConvert.SerializeObject(dbContext.Languages.Find(m => true).ToList()));
        }

        public void InitEmployeesSys()
        {
            dbContext.Employees.DeleteMany(new BsonDocument());
            dbContext.Employees.InsertOne(new Employee()
            {
                UserName = Constants.System.account,
                Password = Helpers.Helper.HashedPassword("06021988"),
                FirstName = "Administrator",
                LastName = "Tribat",
                FullName = "Trần Minh Xuân",
                //Department = Constants.System.department,
                Tel = "02837509077",
                Mobiles = new List<EmployeeMobile>()
                {
                    new EmployeeMobile()
                    {
                        Type = Constants.ContactType.personal,
                        Number = "0938156368"
                    }
                },
                Email = "xuan.tm1988@gmail.com",
                Status = "Approved"
            });
        }

        public void InitProductGroups()
        {
            dbContext.ProductGroups.DeleteMany(new BsonDocument());
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "VT",
                Name = "Vật tư",
                CodeAccountant = "NL",
                NameAccountant = "Nguyên liệu",
                ParentCode = string.Empty
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "VT1",
                Name = "Nhóm dầu nhớt: dầu, nhớt, xăng",
                CodeAccountant = "NL",
                NameAccountant = "Nguyên liệu",
                ParentCode = "VT"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "VT2",
                Name = "Phụ kiện lắp đặt: ống nước, co, van, keo dán,..",
                CodeAccountant = "NL",
                NameAccountant = "Nguyên liệu",
                ParentCode = "VT"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "VT3",
                Name = "Vật tư tiêu hao: thuốc xịt kiến,bao tay,khẩu trang,..",
                CodeAccountant = "NL",
                NameAccountant = "Nguyên liệu",
                ParentCode = "VT"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "VT4",
                Name = "Bạt che",
                CodeAccountant = "NL",
                NameAccountant = "Nguyên liệu",
                ParentCode = "VT"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "NL",
                Name = "Nguyên liệu",
                CodeAccountant = "NVL",
                NameAccountant = "Nguyên vật liệu",
                ParentCode = string.Empty
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "NL1",
                Name = "Nguyên liệu phối trộn",
                CodeAccountant = "NVL",
                NameAccountant = "Nguyên vật liệu",
                ParentCode = "NL"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "NL2",
                Name = "Bán thành phẩm sản xuất",
                CodeAccountant = "NVL",
                NameAccountant = "Nguyên vật liệu",
                ParentCode = "NL"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "NL3",
                Name = "Bán thành phẩm mua về",
                CodeAccountant = "NVL",
                NameAccountant = "Nguyên vật liệu",
                ParentCode = "NL"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "BB",
                Name = "Bao bì",
                CodeAccountant = "BB",
                NameAccountant = "Bao bì",
                ParentCode = string.Empty
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "TP",
                Name = "Thành phẩm",
                CodeAccountant = "BH",
                NameAccountant = "Bán hàng",
                ParentCode = string.Empty
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "TP1",
                Name = "Thành phẩm mua về",
                CodeAccountant = "BH",
                NameAccountant = "Bán hàng",
                ParentCode = "TP"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "TP2",
                Name = "Thành phẩm sản xuất",
                CodeAccountant = "BH",
                NameAccountant = "Bán hàng",
                ParentCode = "TP"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "CC",
                Name = "công cụ",
                CodeAccountant = "CC",
                NameAccountant = "công cụ",
                ParentCode = string.Empty
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "CC1",
                Name = "công cụ cho bảo trì",
                CodeAccountant = "CC1",
                NameAccountant = "công cụ cho bảo trì",
                ParentCode = "CC"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "CC2",
                Name = "công cụ cho sản xuất",
                CodeAccountant = "CC2",
                NameAccountant = "công cụ cho sản xuất",
                ParentCode = "CC"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "CC3",
                Name = "công cụ văn phòng",
                CodeAccountant = "CC3",
                NameAccountant = "công cụ văn phòng",
                ParentCode = "CC"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "CC4",
                Name = "công cụ khác",
                CodeAccountant = "CC4",
                NameAccountant = "công cụ khác",
                ParentCode = "CC"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "PT",
                Name = "Phụ tùng",
                CodeAccountant = "PT",
                NameAccountant = "Phụ tùng",
                ParentCode = string.Empty
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "PT1",
                Name = "xe cơ giới, phương tiện vận chuyển",
                CodeAccountant = "PT1",
                NameAccountant = "xe cơ giới, phương tiện vận chuyển",
                ParentCode = "PT"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "PT2",
                Name = "thiết bị nhà máy",
                CodeAccountant = "PT2",
                NameAccountant = "thiết bị nhà máy",
                ParentCode = "PT"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "TB",
                Name = "Thiết bị",
                CodeAccountant = "TB",
                NameAccountant = "Thiết bị",
                ParentCode = string.Empty
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "TB1",
                Name = "Thiết bị sản xuất",
                CodeAccountant = "TB1",
                NameAccountant = "Thiết bị sản xuất",
                ParentCode = string.Empty
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "TB2",
                Name = "Thiết bị văn phòng",
                CodeAccountant = "TB2",
                NameAccountant = "Thiết bị văn phòng",
                ParentCode = string.Empty
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "XE",
                Name = "Cơ giới",
                CodeAccountant = "XE",
                NameAccountant = "Cơ giới",
                ParentCode = string.Empty
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "XE01",
                Name = "Xe cơ giới",
                CodeAccountant = "XE01",
                NameAccountant = "Xe cơ giới",
                ParentCode = "XE"
            });
            dbContext.ProductGroups.InsertOne(new ProductGroup()
            {
                Code = "XE02",
                Name = "Xe tải,xe khác",
                CodeAccountant = "XE02",
                NameAccountant = "Xe tải,xe khác",
                ParentCode = "XE"
            });
            _cache.SetString(Constants.Collection.ProductGroups, JsonConvert.SerializeObject(dbContext.ProductGroups.Find(m => true).ToList()));
        }

        public void InitUnits()
        {
            dbContext.Units.DeleteMany(new BsonDocument());
            var unitList = dbContext.Products.Distinct<string>("Unit", "{}").ToList();
            foreach (var unit in unitList)
            {
                dbContext.Units.InsertOne(new Unit()
                {
                    Name = unit
                });
            }
            _cache.SetString(Constants.Collection.Units, JsonConvert.SerializeObject(dbContext.Units.Find(m => true).ToList()));
        }

        public void InitLocations()
        {
            dbContext.Locations.DeleteMany(new BsonDocument());
            var locationList = dbContext.Products.Distinct<string>("Location", "{}").ToList();
            foreach (var location in locationList)
            {
                if (!string.IsNullOrEmpty(location) || location != "0" || location.ToUpper() == "HẾT")
                {
                    dbContext.Locations.InsertOne(new Location()
                    {
                        Name = location
                    });
                }
            }
            _cache.SetString(Constants.Collection.Locations, JsonConvert.SerializeObject(dbContext.Locations.Find(m => true).ToList()));
        }

        public void InitLeaveTypes()
        {
            dbContext.LeaveTypes.DeleteMany(new BsonDocument());
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Phép năm",
                YearMax = 12,
                MonthMax = 0
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Phép không hưởng lương",
                YearMax = 3,
                MonthMax = 0,
                SalaryPay = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Nghỉ hưởng lương",
                YearMax = 0,
                MonthMax = 0
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Nghỉ bù",
                YearMax = 0,
                MonthMax = 0
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Nguyên Đán dương lịch",
                YearMax = 1,
                MonthMax = 0,
                Display = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Nguyên Đán âm lịch",
                YearMax = 4,
                MonthMax = 0,
                Display = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Ngày Giỗ tổ Hùng Vương",
                YearMax = 1,
                MonthMax = 0,
                Display = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Ngày Thống nhất đất nước",
                YearMax = 1,
                MonthMax = 0,
                Display = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = "Ngày Quốc tế lao động",
                YearMax = 1,
                MonthMax = 0,
                Display = false
            });
            dbContext.LeaveTypes.InsertOne(new LeaveType()
            {
                Name = " Ngày Quốc khánh",
                YearMax = 1,
                MonthMax = 0,
                Display = false
            });

            foreach (var item in dbContext.LeaveTypes.Find(m => true).ToList())
            {
                var filter = Builders<LeaveType>.Filter.Eq(m => m.Id, item.Id);

                var update = Builders<LeaveType>.Update
                    .Set(m => m.Alias, Utility.AliasConvert(item.Name));

                dbContext.LeaveTypes.UpdateOne(filter, update);
            }
        }
    }
}
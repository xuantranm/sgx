using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Data;
using Models;
using Common.Utilities;
using ViewModels;
using OfficeOpenXml;
using Common.Enums;
using Microsoft.AspNetCore.Authorization;
using MimeKit;
using MimeKit.Text;
using Services;

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
        public ActionResult Email(string Status, string Id, string MaNv, string ToEmail, int Page, int Size)
        {
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

            #region Sort
            var sortBuilder = Builders<ScheduleEmail>.Sort.Descending(m => m.UpdatedOn);
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
                list = dbContext.ScheduleEmails.Find(filter).Sort(sortBuilder).Skip((Page - 1) * Size).Limit(Size).ToList();
            }
            else
            {
                list = dbContext.ScheduleEmails.Find(filter).Sort(sortBuilder).ToList();
            }

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && !string.IsNullOrEmpty(m.Email)).ToList();
            var viewModel = new MailViewModel
            {
                Employees = employees,
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

                InitTrangThai();

                //InitWorkingTime();

                //InitContracts();
                //InitSettings();

                //InitHospital();

                #region Init Data
                //InitLeaveTypes();
                //InitEmployeesSys();
                //InitFunctions();
                //InitFunctionsUsers();
                //InitLanguages();
                //InitTexts();
                //InitCompanies();
                //InitBrands();
                //InitProductGroups();
                //InitProducts();
                //InitUnits();
                //InitLocations();

                //InitSalaryContentTypes();
                //InitSalaryContents();
                //InitBanks();
                //InitSalaryMonthlyTypes();
                #endregion
            }

            //if (setting == "factory")
            //{
            //    InitShift();
            //    InitTruckType();
            //}

            return Json(true);
        }
        #endregion

        #region Factory
        private void InitTrangThai()
        {
            int code = 1;
            var name = "Báo động";
            dbContext.TrangThais.InsertOne(new TrangThai()
            {
                Code = "TT" + code,
                Name = name,
                Alias = Utility.AliasConvert(name),
            });
            code++;

            name = "An toàn";
            dbContext.TrangThais.InsertOne(new TrangThai()
            {
                Code = "TT" + code,
                Name = name,
                Alias = Utility.AliasConvert(name),
            });
            code++;

            name = "Vượt an toàn";
            dbContext.TrangThais.InsertOne(new TrangThai()
            {
                Code = "TT" + code,
                Name = name,
                Alias = Utility.AliasConvert(name),
            });
        }

        public void InitShift()
        {
            dbContext.FactoryShifts.DeleteMany(new BsonDocument());
            dbContext.FactoryShifts.InsertOne(new FactoryShift
            {
                Name = "Ca 1",
                Alias = "ca-1"
            });
            dbContext.FactoryShifts.InsertOne(new FactoryShift
            {
                Name = "Ca 2",
                Alias = "ca-2"
            });
        }

        // Xe cuốc 	 Xe ben 	Xe ủi	Xe xúc
        public void InitTruckType()
        {
            dbContext.FactoryTruckTypes.DeleteMany(new BsonDocument());
            dbContext.FactoryTruckTypes.InsertOne(new FactoryTruckType
            {
                Name = "Xe cuốc",
                Alias = "xe-cuoc",
                Code = "Cuốc",
                CodeAlias = "cuoc"
            });
            dbContext.FactoryTruckTypes.InsertOne(new FactoryTruckType
            {
                TypeAlias = "cuoc",
                Name = "Xe cuốc 03",
                Alias = "xe-cuoc-03",
                Code = "Cuốc 03",
                CodeAlias = "cuoc-03"
            });
            dbContext.FactoryTruckTypes.InsertOne(new FactoryTruckType
            {
                TypeAlias = "cuoc",
                Name = "Xe cuốc 05",
                Alias = "xe-cuoc-05",
                Code = "Cuốc 05",
                CodeAlias = "cuoc-05"
            });
            dbContext.FactoryTruckTypes.InsertOne(new FactoryTruckType
            {
                TypeAlias = "cuoc",
                Name = "Xe cuốc 07",
                Alias = "xe-cuoc-07",
                Code = "Cuốc 07",
                CodeAlias = "cuoc-07"
            });
            dbContext.FactoryTruckTypes.InsertOne(new FactoryTruckType
            {
                Name = "Xe ben",
                Alias = "xe-ben",
                Code = "Ben",
                CodeAlias = "ben"
            });
            dbContext.FactoryTruckTypes.InsertOne(new FactoryTruckType
            {
                Name = "Xe ủi",
                Alias = "xe-ui",
                Code = "Ủi",
                CodeAlias = "ui"
            });
            dbContext.FactoryTruckTypes.InsertOne(new FactoryTruckType
            {
                Name = "Xe xúc",
                Alias = "xe-xuc",
                Code = "Xúc",
                CodeAlias = "xuc"
            });
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

        public void InitSettings()
        {
            dbContext.Settings.DeleteMany(new BsonDocument());
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "verion",
                Value = "1.0",
                Title = "Phiên bản hệ thống",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "environment",
                Value = "test",
                Title = "Môi trường",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "cache",
                Value = "true",
                Title = "Bộ nhớ đệm",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "company",
                Value = "Công ty TNHH CNSH SÀI GÒN XANH",
                Title = "Thông tin công ty",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "images",
                Value = "images",
                Title = "Thư mục hình ảnh",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "documents",
                Value = "documents",
                Title = "Thư mục lưu tài liệu",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "pageSize",
                Value = "50",
                Title = "Dòng dữ liệu",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "excelBackGround",
                Value = "#e6e6fa",
                Title = "Màu tiêu đề tài liệu excel",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "employeeCodeFirst",
                Value = "VIP",
                Title = "Mục đầu mã nhân viên",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "employeeCodeLength",
                Value = "6",
                Title = "[xxx] + Chiều dài mã nhân viên",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "identityCardExpired",
                Value = "15",
                Title = "Thời hạn cmnd (năm)",
                Language = Constants.Languages.Vietnamese
            });

            #region API
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "google-api-key-1",
                Value = "AIzaSyD_ZdqnbEXnZppElDMy7YvbEECaDUFGT-I",
                Title = "Google API Key",
                Content = "xiao.sg1988@gmail.com create on 23-05-2018"
            });
            dbContext.Settings.InsertOne(new Setting()
            {
                Key = "google-api-key-2",
                Value = "AIzaSyCN05d8x_gF14D9jTLoo418WtVFdu6CK6s",
                Title = "Google API Key",
                Content = "sg.xiao1988@gmail.com create on 23-05-2018"
            });
            #endregion

            // _cache.SetString(Constants.Collection.Settings, JsonConvert.SerializeObject(dbContext.Settings.Find(m => true).ToList()));
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

        public void InitTexts()
        {
            var User = dbContext.Employees.Find(m => m.UserName.Equals(Constants.System.account)).First().Id;
            dbContext.Texts.DeleteMany(new BsonDocument());
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 1,
                Content = "Code missing. Add again",
                ContentPlainText = "Code missing. Add again",
                ToolTip = "Code missing. Add again",
                Seo = "code-missing-.-add-again",
                Type = Constants.Type.Text,
                Language = Constants.Languages.English,
                Enable = true,
                ModifiedDate = DateTime.Now
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 1,
                Content = "Không thấy mã. Tạo lại",
                ContentPlainText = "Không thấy mã. Tạo lại",
                ToolTip = "Không thấy mã. Tạo lại",
                Seo = "khong-thay-ma-.-tao-lai",
                Type = Constants.Type.Text,
                Language = Constants.Languages.Vietnamese,
                Enable = true,
                ModifiedDate = DateTime.Now
            });
            _cache.SetString(Constants.Collection.Texts, JsonConvert.SerializeObject(dbContext.Texts.Find(m => true).ToList()));
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
                Department = Constants.System.department,
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

        //public void InitProducts()
        //{
        //    var User = dbContext.Employees.Find(m => m.UserName.Equals(Constants.System.account)).First().Id;
        //    dbContext.Products.DeleteMany(new BsonDocument());
        //    // Read excel
        //    string sWebRootFolder = _env.WebRootPath;
        //    string sFileName = @"baocaotong.xlsx";
        //    FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
        //    try
        //    {
        //        using (ExcelPackage package = new ExcelPackage(file))
        //        {
        //            ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
        //            int rowCount = worksheet.Dimension.Rows;
        //            int ColCount = worksheet.Dimension.Columns;
        //            for (int row = 4; row <= rowCount; row++)
        //            {
        //                try
        //                {
        //                    var product = new Product()
        //                    {
        //                        CodeAccountant = worksheet.Cells[row, 2].Value != null ? worksheet.Cells[row, 2].Value.ToString() : string.Empty,
        //                        Code = worksheet.Cells[row, 3].Value != null ? worksheet.Cells[row, 3].Value.ToString() : string.Empty,
        //                        Name = worksheet.Cells[row, 4].Value != null ? worksheet.Cells[row, 4].Value.ToString() : string.Empty,
        //                        Unit = worksheet.Cells[row, 5].Value != null ? Utility.ToTitleCase(worksheet.Cells[row, 5].Value.ToString().Trim()) : string.Empty,
        //                        Group = worksheet.Cells[row, 6].Value != null ? worksheet.Cells[row, 6].Value.ToString() : string.Empty,
        //                        GroupDevide = worksheet.Cells[row, 7].Value != null ? worksheet.Cells[row, 7].Value.ToString() : string.Empty,
        //                        Location = worksheet.Cells[row, 8].Value != null ? Utility.ToTitleCase(worksheet.Cells[row, 8].Value.ToString().Trim()) : string.Empty,
        //                        Note = worksheet.Cells[row, 9].Value != null ? worksheet.Cells[row, 9].Value.ToString() : string.Empty,
        //                        QuantityInStoreSafe = worksheet.Cells[row, 10].Value != null ? Decimal.Parse(worksheet.Cells[row, 10].Value.ToString()) : 0,
        //                        QuantityInStoreSafeQ4 = worksheet.Cells[row, 11].Value != null ? Decimal.Parse(worksheet.Cells[row, 11].Value.ToString()) : 0,
        //                        CreatedBy = User,
        //                        UpdatedBy = User,
        //                        ApprovedBy = User,
        //                        Status = "Avg"
        //                    };

        //                    if (!string.IsNullOrEmpty(product.Code))
        //                    {
        //                        dbContext.Products.InsertOne(product);
        //                        #region Activities
        //                        var activity = new TrackingUser
        //                        {
        //                            UserId = User,
        //                            Function = Constants.Collection.Stores,
        //                            Action = "create",
        //                            Value = product.Code,
        //                            Content = "create" + " " + Constants.Collection.Stores + " " + product.Code,
        //                            Created = DateTime.Now,
        //                            Link = "/hang-hoa/chi-tiet/" + product.Code
        //                        };
        //                        dbContext.TrackingUsers.InsertOne(activity);
        //                        #endregion
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    _logger.LogWarning(LoggingEvents.GetItemNotFound, ex, "GetById({ID}) NOT FOUND", User);
        //                    var mess = ex.ToString();
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // IMPLEMENT LOG
        //        var mess = ex.ToString();
        //    }
        //    _cache.SetString(Constants.Collection.Products, JsonConvert.SerializeObject(dbContext.Products.Find(m => true).ToList()));
        //}

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

        #region Texts
        private void CacheReLoadText()
        {
            _cache.SetString(Constants.Collection.Texts + Constants.FlagCacheKey, Constants.String_N);
        }

        [HttpGet]
        [Route("/text/cache")]
        public void TextCache()
        {
            _cache.SetString(Constants.Collection.Texts, JsonConvert.SerializeObject(dbContext.Texts.Find(m => true).ToList()));
            _cache.SetString(Constants.Collection.Texts + Constants.FlagCacheKey, Constants.String_Y);
        }

        // GET: Setting
        [Route("/text/")]
        public ActionResult TextIndex(int? code, int? trang)
        {
            var user = User.Identity.Name;
            var results = from e in dbContext.Texts.AsQueryable()
                          select e;
            var viewModel = new TextViewModel
            {
                //Texts = PaginatedList<Text>.Create(results.AsNoTracking(), trang ?? 1, pageSize)
            };
            return View(viewModel);
        }

        [Route("/text/detail/")]
        public ActionResult TextDetails(int id)
        {
            var data = JsonConvert.DeserializeObject<IEnumerable<Text>>(_cache.GetString(Constants.Collection.Texts));
            var entity = data.Where(m => m.Code == id).ToList();
            return View(entity);
        }

        [Route("/text/create/")]
        public ActionResult TextCreate()
        {

            return View();
        }

        // Setting: Setting/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/text/create/")]
        public ActionResult TextCreate(Text entity)
        {
            try
            {
                var User = string.Empty;
                var now = DateTime.Now;
                // TODO: Add insert logic here
                //entity.Id = Guid.NewGuid();
                dbContext.Texts.InsertOne(entity);
                #region Activities
                var activity = new TrackingUser
                {
                    UserId = User,
                    Function = Constants.Collection.Texts,
                    Action = "create",
                    Value = entity.Id,

                    Content = "create " + entity.Code + " with content: " + entity.Seo,
                    Created = now,
                    Link = "/text/create/" + entity.Id
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion
                return RedirectToAction(nameof(TextIndex));
            }
            catch
            {
                return View();
            }
        }

        // GET: Setting/Edit/5
        [Route("/text/edit/")]
        public ActionResult TextEdit(string id)
        {
            var data = JsonConvert.DeserializeObject<IEnumerable<Text>>(_cache.GetString(Constants.Collection.Texts));
            var entity = data.Where(m => m.Id == id).FirstOrDefault();
            return View(entity);
        }

        // Setting: Setting/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/text/edit/")]
        public ActionResult TextEdit(Text entity)
        {
            try
            {
                var User = string.Empty;
                var now = DateTime.Now;
                // TODO: Add update logic here
                // You can use the UpdateOne to get higher performance if you need.
                // dbContext.Settings.ReplaceOne(m => m.Id == entity.Id, entity);

                #region Activities
                var activity = new TrackingUser
                {
                    UserId = User,
                    Function = Constants.Collection.Texts,
                    Action = "edit",
                    Value = entity.Id,
                    Content = "Update setting key: " + entity.Code.ToString() + " with content: " + entity.Content,
                    Created = now,
                    Link = "/setting/details/" + entity.Id
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion
                return RedirectToAction(nameof(TextIndex));
            }
            catch
            {
                return View();
            }
        }

        // GET: Setting/Delete/5
        [Route("/text/delete")]
        public ActionResult TextDelete(string id)
        {
            var data = JsonConvert.DeserializeObject<IEnumerable<Text>>(_cache.GetString(Constants.Collection.Texts));
            var entity = data.Where(m => m.Id == id).FirstOrDefault();
            return View(entity);
        }

        // Setting: Setting/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/text/delete")]
        public ActionResult TextDelete(string id, Setting entity)
        {
            try
            {
                var User = string.Empty;
                var now = DateTime.Now;
                // TODO: Add delete logic here
                dbContext.Settings.DeleteOne(m => m.Id == id);

                #region Activities
                var activity = new TrackingUser
                {
                    UserId = User,
                    Function = Constants.Collection.Texts,
                    Action = "delete",
                    Value = entity.Id,
                    Content = "delete setting " + entity.Key,
                    Created = now,
                    Link = "/not-found"
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion

                return RedirectToAction(nameof(TextIndex));
            }
            catch
            {
                return View();
            }
        }
        #endregion
    }
}
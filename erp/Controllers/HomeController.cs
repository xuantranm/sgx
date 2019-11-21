using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Data;
using ViewModels;
using Models;
using Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver.Linq;
using MimeKit;
using Services;
using Common.Enums;

namespace Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        readonly IHostingEnvironment _env;
        private readonly IEmailSender _emailSender;
        public IConfiguration Configuration { get; }

        public HomeController(
            IConfiguration configuration,
            IHostingEnvironment env,
            IEmailSender emailSender)
        {
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            #region Login | Right
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            var userInformation = dbContext.Employees.Find(m => m.Id.Equals(login)).FirstOrDefault();
            if (userInformation == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("login", "account");
            }

            var rightHr = Utility.IsRight(login, Constants.Rights.HR, (int)ERights.View);
            ViewData["rightHr"] = rightHr;
            #endregion

            int getItems = 10;

            #region Get Setting Value
            var settings = dbContext.Settings.Find(m => m.Enable.Equals(true)).ToList();
            var pageSize = Constants.PageSize;
            var pageSizeSetting = settings.First(m => m.Key.Equals("pageSize"));
            if (pageSizeSetting != null)
            {
                pageSize = Convert.ToInt32(pageSizeSetting.Value);
            }

            var birthdayNoticeBefore = Constants.PageSize;
            var birthdayNoticeBeforeSetting = settings.FirstOrDefault(m => m.Key.Equals("BirthdayNoticeBefore"));
            if (birthdayNoticeBeforeSetting != null)
            {
                birthdayNoticeBefore = Convert.ToInt32(birthdayNoticeBeforeSetting.Value);
            }

            //var contractDayNoticeBefore = Constants.contractDayNoticeBefore;
            //var contractDayNoticeBeforeSetting = settings.First(m => m.Key.Equals("contractDayNoticeBefore"));
            //if (contractDayNoticeBeforeSetting != null)
            //{
            //    contractDayNoticeBefore = Convert.ToInt32(contractDayNoticeBeforeSetting.Value);
            //}

            #endregion

            #region Notification Birthday
            ViewData["birthdayNoticeBefore"] = birthdayNoticeBefore;

            var nextBirthdays = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.Birthday > Constants.MinDate).ToEnumerable()
                .Where(m => m.RemainingBirthDays <= birthdayNoticeBefore).OrderBy(m => m.RemainingBirthDays).Take(6).ToList();
            #endregion

            //#region Notification Contract
            //var sortContract = Builders<Employee>.Sort.Ascending(m => m.).Descending(m => m.Code);
            //var birthdays = dbContext.Employees.Find(m => m.Enable.Equals(true)).Sort(sortBirthday).Limit(getItems).ToList();
            //#endregion
            var sortNotification = Builders<Notification>.Sort.Ascending(m => m.CreatedOn).Descending(m => m.CreatedOn);
            var builderNotication = Builders<Notification>.Filter;
            var filterNotication = builderNotication.Eq(m => m.Enable, true);

            #region Notification HR
            var filterHr = filterNotication & builderNotication.Eq(m => m.Type, 2);
            if (!rightHr)
            {
                filterHr = filterHr & builderNotication.Eq(m => m.User, login) & builderNotication.Ne(m => m.CreatedBy, login);
            }
            var notificationHRs = await dbContext.Notifications.Find(filterHr).Sort(sortNotification).Limit(getItems).ToListAsync();
            #endregion

            #region Notification Others
            var filterSystem = filterNotication & builderNotication.Eq(m => m.Type, 1);
            var notificationSystems = await dbContext.Notifications.Find(filterSystem).Sort(sortNotification).Limit(getItems).ToListAsync();

            var filterExpires = filterNotication & builderNotication.Eq(m => m.Type, 3);
            var notificationExpires = await dbContext.Notifications.Find(filterExpires).Sort(sortNotification).Limit(getItems).ToListAsync();

            var filterTaskBhxh = filterNotication & builderNotication.Eq(m => m.Type, 4);
            var notificationTaskBHXHs = await dbContext.Notifications.Find(filterTaskBhxh).Sort(sortNotification).Limit(getItems).ToListAsync();

            var filterCompany = filterNotication & builderNotication.Eq(m => m.Type, 5);
            var notificationCompanies = await dbContext.Notifications.Find(filterCompany).Sort(sortNotification).Limit(getItems).ToListAsync();

            var notificationActions = await dbContext.NotificationActions.Find(m => m.UserId.Equals(login)).ToListAsync();
            #endregion

            #region Tracking Other User (check user activities,...)
            var sortTrackingOther = Builders<TrackingUser>.Sort.Descending(m => m.Created);
            var trackingsOther = dbContext.TrackingUsers.Find(m => !m.UserId.Equals(login)).Sort(sortTrackingOther).Limit(getItems).ToList();
            #endregion

            #region My Trackings
            var sortTracking = Builders<TrackingUser>.Sort.Descending(m => m.Created);
            var trackings = dbContext.TrackingUsers.Find(m => m.UserId.Equals(login)).Sort(sortTracking).Limit(getItems).ToList();
            #endregion

            #region Extends (trainning, recruit, news....)
            var sortNews = Builders<News>.Sort.Descending(m => m.ModifiedOn);
            var news = dbContext.News.Find(m => m.Enable.Equals(true)).Sort(sortNews).Limit(getItems).ToList();

            var listTrainningTypes = await dbContext.TrainningTypes.Find(m => m.Enable.Equals(true)).ToListAsync();
            Random rnd = new Random();
            int r = rnd.Next(listTrainningTypes.Count);
            var sortTrainnings = Builders<Trainning>.Sort.Descending(m => m.CreatedOn);
            // Random type result
            var trainningType = listTrainningTypes[r].Alias;
            var trainnings = dbContext.Trainnings.Find(m => m.Enable.Equals(true) && m.Type.Equals(trainningType)).Sort(sortTrainnings).Limit(5).ToList();
            #endregion

            #region Leave Manager
            var leaves = await dbContext.Leaves.Find(m => m.ApproverId.Equals(login) && m.Status.Equals(0)).ToListAsync();
            #endregion

            #region Times Manager
            var timeKeepers = dbContext.EmployeeWorkTimeLogs.Find(m => m.ConfirmId.Equals(login) && m.Status.Equals(2)).ToList();
            var timers = new List<TimeKeeperDisplay>();
            if (timeKeepers != null && timeKeepers.Count > 0)
            {
                foreach (var time in timeKeepers)
                {
                    var enrollNumber = string.Empty;
                    var chucvuName = string.Empty;
                    var employee = dbContext.Employees.Find(m => m.Id.Equals(time.EmployeeId)).FirstOrDefault();
                    if (!string.IsNullOrEmpty(employee.ChucVu))
                    {
                        var cvE = dbContext.ChucVus.Find(m => m.Id.Equals(employee.ChucVu)).FirstOrDefault();
                        if (cvE != null)
                        {
                            chucvuName = cvE.Name;
                        }
                    }

                    var employeeDisplay = new TimeKeeperDisplay()
                    {
                        EmployeeWorkTimeLogs = new List<EmployeeWorkTimeLog>() {
                        time
                    },
                        Code = employee.Code + "(" + employee.CodeOld + ")",
                        FullName = employee.FullName,
                        ChucVu = chucvuName
                    };
                    timers.Add(employeeDisplay);
                }
            }
            #endregion

            #region My Activities
            //public IList<Leave> MyLeaves { get; set; }
            //public IList<EmployeeWorkTimeLog> MyWorkTimeLogs { get; set; }
            var sortMyLeave = Builders<Leave>.Sort.Descending(m => m.UpdatedOn);
            var builderMyLeave = Builders<Leave>.Filter;
            var filterMyLeave = builderMyLeave.Eq(m => m.Enable, true)
                & builderMyLeave.Eq(m => m.EmployeeId, login);
            var myLeaves = await dbContext.Leaves.Find(filterMyLeave).Sort(sortMyLeave).Limit(5).ToListAsync();

            var sortMyWorkTime = Builders<EmployeeWorkTimeLog>.Sort.Descending(m => m.Date);
            var builderMyWorkTime = Builders<EmployeeWorkTimeLog>.Filter;
            var filterMyWorkTime = builderMyWorkTime.Eq(m => m.Enable, true) & builderMyWorkTime.Lt(m => m.Date, DateTime.Now.Date) & builderMyWorkTime.Ne(m => m.Status, 1)
                & builderMyWorkTime.Eq(m => m.EmployeeId, login);
            var myWorkTimes = await dbContext.EmployeeWorkTimeLogs.Find(filterMyWorkTime).Sort(sortMyWorkTime).Limit(5).ToListAsync();

            #endregion


            var viewModel = new HomeErpViewModel()
            {
                UserInformation = userInformation,
                NotificationSystems = notificationSystems,
                NotificationCompanies = notificationCompanies,
                NotificationHRs = notificationHRs,
                NotificationExpires = notificationExpires,
                NotificationTaskBhxhs = notificationTaskBHXHs,
                NotificationActions = notificationActions,
                Trackings = trackings,
                TrackingsOther = trackingsOther,
                News = news,
                Birthdays = nextBirthdays,
                Leaves = leaves,
                TimeKeepers = timers,
                // My activities
                MyLeaves = myLeaves,
                MyWorkTimeLogs = myWorkTimes,
                // Training
                Trainnings = trainnings
            };

            return View(viewModel);
        }

        // ALL COMMON CONTENT, CATEGORY
        [Route("{url}")]
        public async Task<IActionResult> Content(string url)
        {
            // CATEGORY | CONTENT
            var mode = (int)EModeDirect.Content;
            var contents = new List<Content>(); // Base Category
            var contentE = new Content(); // Base Content

            var categoryE = dbContext.Categories.Find(m => m.Alias.Equals(url)).FirstOrDefault();
            if (categoryE != null)
            {
                mode = (int)EModeDirect.Category;
                contents = dbContext.Contents.Find(m => m.CategoryId.Equals(categoryE.Id)).ToList();
                if (categoryE.Seo != null)
                {
                    SeoInit(categoryE.Seo);
                }
            }
            else
            {
                mode = (int)EModeDirect.Content;
                contentE = dbContext.Contents.Find(m => m.Enable.Equals(true) && m.Alias.Equals(url)).FirstOrDefault();
                if (contentE != null && contentE.Seo != null)
                {
                    SeoInit(contentE.Seo);
                }
            }

            // Redirect Index if error data
            if (categoryE == null && contentE == null)
            {
                return RedirectToAction("Index");
            }

            ViewData[Constants.Texts.ModeDirect] = mode;

            var viewModel = new HomeViewModel()
            {
                Category = categoryE,
                Contents = contents,
                Content = contentE
            };
            return View(viewModel);
        }

        #region SUB:API,...
        [Route("/tai-lieu/{type}")]
        public IActionResult Document(string type)
        {
            ViewData["Type"] = type;
            if (type == "update-category-21")
            {
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.Gender));
                var genders = new List<NameValue>
                {
                    new NameValue()
                    {
                        Name = "Nam",
                        Value = "Nam"
                    },
                    new NameValue()
                    {
                        Name = "Nữ",
                        Value = "Nữ"
                    },
                    new NameValue()
                    {
                        Name = "Khác",
                        Value = "Khác"
                    }
                };
                var iGender = 1;
                foreach (var item in genders)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Type = (int)ECategory.Gender,
                        Name = item.Name,
                        Alias = Utility.AliasConvert(item.Name),
                        Value = item.Value,
                        Description = string.Empty,
                        Code = iGender.ToString(),
                        CodeInt = iGender
                    });
                    iGender++;
                }
            }
            if (type == "update-category-22")
            {
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.Probation));
                var probations = new List<NameValue>
                {
                    new NameValue()
                    {
                        Name = "06 ngày",
                        Value = "6"
                    },
                    new NameValue()
                    {
                        Name = "30 ngày",
                        Value = "30"
                    },
                    new NameValue()
                    {
                        Name = "60 ngày",
                        Value = "60"
                    }
                };
                var iProbation = 1;
                foreach (var item in probations)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Type = (int)ECategory.Probation,
                        Name = item.Name,
                        Alias = Utility.AliasConvert(item.Name),
                        Value = item.Value,
                        Description = string.Empty,
                        Code = iProbation.ToString(),
                        CodeInt = iProbation
                    });
                    iProbation++;
                }
            }
            if (type == "update-category-time")
            {
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.TimeWork));
                var times = new List<NameValue>
                {
                    new NameValue()
                    {
                        Name = "07:30-16:30",
                        Value = "07:30-16:30"
                    },
                    new NameValue()
                    {
                        Name = "08:00-17:00",
                        Value = "08:00-17:00"
                    },
                };
                var iTime = 1;
                foreach (var item in times)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Type = (int)ECategory.TimeWork,
                        Name = item.Name,
                        Alias = Utility.AliasConvert(item.Name),
                        Value = item.Value,
                        Description = string.Empty,
                        Code = iTime.ToString(),
                        CodeInt = iTime
                    });
                    iTime++;
                }
            }

            if (type == "update-setting")
            {
                var settings = dbContext.SettingsTemp.Find(m => true).ToList();
                dbContext.Settings.DeleteMany(m => true);
                foreach (var item in settings)
                {
                    dbContext.Settings.InsertOne(new Setting()
                    {
                        Type = item.Type,
                        Key = item.Key,
                        Value = item.Value
                    });
                }
            }
            if (type == "update-role")
            {
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.Role));
                var roles = dbContext.Roles.Find(m => true).ToList();
                foreach (var item in roles)
                {
                    if (!string.IsNullOrEmpty(item.Object))
                    {
                        var existCate = dbContext.Categories.CountDocuments(m => m.Name.Equals(item.Object));
                        if (existCate == 0)
                        {
                            dbContext.Categories.InsertOne(new Category()
                            {
                                Type = (int)ECategory.Role,
                                Name = item.Object,
                                Alias = Utility.AliasConvert(item.Object),
                                Description = item.Description,
                                ModeData = (int)EModeData.Merge
                            });
                        }
                    }
                }

                dbContext.Rights.DeleteMany(m => m.Type.Equals((int)ECategory.Role));
                var olds = dbContext.RoleUsers.Find(m => true).ToList();
                foreach (var item in olds)
                {
                    if (!string.IsNullOrEmpty(item.Role))
                    {
                        var roleE = dbContext.Categories.Find(m => m.Alias.Equals(item.Role) && m.Type.Equals((int)ECategory.Role)).FirstOrDefault();
                        if (roleE != null)
                        {
                            dbContext.Rights.InsertOne(new Right()
                            {
                                RoleId = roleE.Id,
                                ObjectId = item.User,
                                Action = item.Action,
                                Start = item.Start,
                                Expired = item.Expired,
                                ModeData = (int)EModeData.Merge
                            });
                        }
                    }
                }
            }
            if (type == "update-category")
            {
                var congtychinhanhs = dbContext.CongTyChiNhanhs.Find(m => m.Enable.Equals(true)).ToList();
                var khoichucnangs = dbContext.KhoiChucNangs.Find(m => m.Enable.Equals(true)).ToList();
                var phongbans = dbContext.PhongBans.Find(m => m.Enable.Equals(true) && !string.IsNullOrEmpty(m.KhoiChucNangId)).ToList();
                var bophans = dbContext.BoPhans.Find(m => m.Enable.Equals(true) && string.IsNullOrEmpty(m.Parent)).ToList();
                var chucvus = dbContext.ChucVus.Find(m => m.Enable.Equals(true)).ToList();
                var hospitals = dbContext.BHYTHospitals.Find(m => m.Enable.Equals(true)).ToList();
                var contracts = dbContext.ContractTypes.Find(m => m.Enable.Equals(true)).ToList();
                var workTimeTypes = dbContext.WorkTimeTypes.Find(m => m.Enable.Equals(true)).ToList();
                var banks = dbContext.Banks.Find(m => m.Enable.Equals(true)).ToList();

                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.Company));
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.KhoiChucNang));
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.PhongBan));
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.BoPhan));
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.ChucVu));
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.Hospital));
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.Contract));
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.TimeWork));
                dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.Bank));
                var iCompany = 1;
                foreach (var item in congtychinhanhs)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Id = item.Id,
                        Type = (int)ECategory.Company,
                        Name = item.Name,
                        Alias = item.Alias,
                        Description = item.Description,
                        Code = item.Code,
                        CodeInt = iCompany
                    });
                    iCompany++;
                }
                foreach (var item in khoichucnangs)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Id = item.Id,
                        Type = (int)ECategory.KhoiChucNang,
                        Name = item.Name,
                        Alias = item.Alias,
                        Description = item.Description,
                        ParentId = item.CongTyChiNhanhId
                    });
                }
                foreach (var item in phongbans)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Id = item.Id,
                        Type = (int)ECategory.PhongBan,
                        Name = item.Name,
                        Alias = item.Alias,
                        Description = item.Description,
                        ParentId = item.KhoiChucNangId
                    });
                }
                foreach (var item in bophans)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Id = item.Id,
                        Type = (int)ECategory.BoPhan,
                        Name = item.Name,
                        Alias = item.Alias,
                        Description = item.Description,
                        ParentId = item.PhongBanId
                    });
                }
                foreach (var item in chucvus)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Id = item.Id,
                        Type = (int)ECategory.ChucVu,
                        Name = item.Name,
                        Alias = item.Alias,
                        Description = item.Description,
                        ParentId = item.KhoiChucNangId
                    });
                }
                foreach (var item in hospitals)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Id = item.Id,
                        Type = (int)ECategory.Hospital,
                        Name = item.Name,
                        Alias = item.Alias
                    });
                }
                foreach (var item in contracts)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Id = item.Id,
                        Type = (int)ECategory.Contract,
                        Name = item.Name,
                        Alias = Utility.AliasConvert(item.Name)
                    });
                }
                foreach (var item in workTimeTypes)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Id = item.Id,
                        Type = (int)ECategory.TimeWork,
                        Name = item.Start.ToString(@"hh\:mm") + "-" + item.End.ToString(@"hh\:mm")
                    });
                }
                foreach (var item in banks)
                {
                    dbContext.Categories.InsertOne(new Category()
                    {
                        Id = item.Id,
                        Type = (int)ECategory.Bank,
                        Name = item.Name,
                        Alias = item.Alias
                    });
                }
            }
            if (type == "update-employee")
            {
                var employees = dbContext.Employees.Find(m => true).ToList();
                
                foreach(var entity in employees)
                {
                    var imgs = new List<Img>();
                    var avatar = entity.Avatar;
                    var cover = entity.Cover;
                    if (!string.IsNullOrEmpty(entity.ManagerId))
                    {
                        var managerE = dbContext.Employees.Find(m => m.ChucVu.Equals(entity.ManagerId) && m.Enable.Equals(true) && m.Leave.Equals(false)).FirstOrDefault();
                        entity.ManagerEmployeeId = managerE != null ? managerE.Id : string.Empty;
                        entity.ManagerInformation = managerE != null ? managerE.ChucVuName + " - " +managerE.FullName : string.Empty;
                    }
                    if (!string.IsNullOrEmpty(entity.CongTyChiNhanh))
                    {
                        var companyE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.Company) && m.Id.Equals(entity.CongTyChiNhanh)).FirstOrDefault();
                        entity.CongTyChiNhanhName = companyE != null ? companyE.Name : string.Empty;
                    }
                    if (!string.IsNullOrEmpty(entity.KhoiChucNang))
                    {
                        var khoichucnangE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.KhoiChucNang) && m.Id.Equals(entity.KhoiChucNang)).FirstOrDefault();
                        entity.KhoiChucNangName = khoichucnangE != null ? khoichucnangE.Name : string.Empty;
                    }
                    if (!string.IsNullOrEmpty(entity.PhongBan))
                    {
                        var phongbanE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.PhongBan) && m.Id.Equals(entity.PhongBan)).FirstOrDefault();
                        entity.PhongBanName = phongbanE != null ? phongbanE.Name : string.Empty;
                    }
                    if (!string.IsNullOrEmpty(entity.BoPhan))
                    {
                        var bophanE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.BoPhan) && m.Id.Equals(entity.BoPhan)).FirstOrDefault();
                        entity.BoPhanName = bophanE != null ? bophanE.Name : string.Empty;
                    }
                    if (!string.IsNullOrEmpty(entity.ChucVu))
                    {
                        try
                        {
                            var chucVuE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.ChucVu) && m.Id.Equals(entity.ChucVu)).FirstOrDefault();
                            entity.ChucVuName = chucVuE != null ? chucVuE.Name : string.Empty;
                        }
                        catch (Exception ex)
                        {
                            entity.ChucVu = string.Empty;
                            entity.ChucVuName = string.Empty;
                        }
                    }
                    if (avatar != null)
                    {
                        var r1 = avatar.Path.Substring(1);
                        var newPath = r1.Remove(r1.Length - 1, 1);
                        imgs.Add(new Img()
                        {
                            Type = (int)EImageSize.Avatar,
                            Path = newPath,
                            FileName = avatar.FileName,
                            Title = avatar.Title,
                            Main = true,
                            Orginal = avatar.OrginalName
                        });
                    }
                    if (cover != null)
                    {
                        var r1 = cover.Path.Substring(1);
                        var newPath = r1.Remove(r1.Length - 1, 1);
                        imgs.Add(new Img()
                        {
                            Type = (int)EImageSize.Cover,
                            Path = newPath,
                            FileName = cover.FileName,
                            Title = cover.Title,
                            Main = true,
                            Orginal = cover.OrginalName
                        });
                    }
                    imgs = imgs.Count == 0 ? null : imgs;
                    var isTimeKeeper = entity.IsTimeKeeper ? false : true;
                    var workplaces = isTimeKeeper ? entity.Workplaces : null;

                    if (isTimeKeeper)
                    {
                        if (entity.Workplaces == null)
                        {
                            isTimeKeeper = false;
                        }
                        else
                        {
                            var places = entity.Workplaces.Where(m => !string.IsNullOrEmpty(m.Fingerprint)).ToList();
                            if (places == null)
                            {
                                isTimeKeeper = false;
                            }
                        }
                    }

                    var filter = Builders<Employee>.Filter.Eq(m => m.Id, entity.Id);
                    var update = Builders<Employee>.Update
                        .Set(m => m.ManagerEmployeeId, entity.ManagerEmployeeId)
                        .Set(m => m.ManagerInformation, entity.ManagerInformation)
                        .Set(m => m.CongTyChiNhanhName, entity.CongTyChiNhanhName)
                        .Set(m => m.KhoiChucNangName, entity.KhoiChucNangName)
                        .Set(m => m.PhongBanName, entity.PhongBanName)
                        .Set(m => m.BoPhanName, entity.BoPhanName)
                        .Set(m => m.ChucVu, entity.ChucVu)
                        .Set(m => m.ChucVuName, entity.ChucVuName)
                        .Set(m => m.Images, imgs)
                        .Set(m => m.IsTimeKeeper, isTimeKeeper)
                        .Set(m => m.Workplaces, workplaces);

                    dbContext.Employees.UpdateOne(filter, update);
                    // UPDATE HISTORY
                    var filterH = Builders<Employee>.Filter.Eq(m => m.EmployeeId, entity.Id);
                    var updateH = Builders<Employee>.Update
                        .Set(m => m.ManagerEmployeeId, entity.ManagerEmployeeId)
                        .Set(m => m.ManagerInformation, entity.ManagerInformation)
                        .Set(m => m.CongTyChiNhanhName, entity.CongTyChiNhanhName)
                        .Set(m => m.KhoiChucNangName, entity.KhoiChucNangName)
                        .Set(m => m.PhongBanName, entity.PhongBanName)
                        .Set(m => m.BoPhanName, entity.BoPhanName)
                        .Set(m => m.ChucVuName, entity.ChucVuName);
                    dbContext.EmployeeHistories.UpdateMany(filterH, updateH);
                }
            }
            if (type == "update-employee-leave")
            {
                var Den = Utility.GetToDate(string.Empty);
                var year = Den.Year;
                var month = Den.Month;

                var builder = Builders<Employee>.Filter;
                var filter = !builder.Eq(i => i.UserName, Constants.System.account)
                    & builder.Eq(m => m.Enable, true) & builder.Eq(m => m.Leave, true);
                var employees = dbContext.Employees.Find(filter).ToList();
                var fields = Builders<Employee>.Projection.Include(p => p.Id);
                var employeeIds = dbContext.Employees.Find(filter).Project<Employee>(fields).ToList().Select(m => m.Id).ToList();

                var builderT = Builders<EmployeeWorkTimeLog>.Filter;
                var filterT = builderT.Eq(m => m.Enable, true)
                            & builderT.Eq(m => m.Month, month)
                            & builderT.Eq(m => m.Year, year);
                if (employeeIds != null && employeeIds.Count > 0)
                {
                    filterT &= builderT.Where(m => employeeIds.Contains(m.EmployeeId));
                }
                var times = dbContext.EmployeeWorkTimeLogs.Find(filterT).SortBy(m => m.Date).ToList();

                foreach (var entity in employees)
                {
                    var employeeWorkTimeLogs = times.Where(m => m.EmployeeId.Equals(entity.Id)).ToList();
                    if (employeeWorkTimeLogs != null || employeeWorkTimeLogs.Count > 0)
                    {
                        var filterU = Builders<Employee>.Filter.Eq(m => m.Id, entity.Id);
                        var update = Builders<Employee>.Update
                            .Set(m => m.IsOnline, false);
                        dbContext.Employees.UpdateOne(filterU, update);
                    }
                }
            }
            return Json(new { result = true, message = "Cập nhật thành công." });
        }

        [Route("/email/welcome/")]
        public async Task<IActionResult> SendMail()
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

            var employees = dbContext.Employees.Find(filter).ToList();
            var password = string.Empty;
            foreach (var employee in employees)
            {
                if (!string.IsNullOrEmpty(employee.Email))
                {
                    // Update password
                    password = Guid.NewGuid().ToString("N").Substring(0, 6);
                    var sysPassword = Helpers.Helper.HashedPassword(password);

                    var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                    var update = Builders<Employee>.Update
                        .Set(m => m.Password, sysPassword);
                    dbContext.Employees.UpdateOne(filterUpdate, update);
                    SendMailRegister(employee, password);
                }
            }

            return Json(new { result = true, source = "sendmail", message = "Gửi mail thành công" });
        }

        [Route("/email/send-miss-v100/")]
        public async Task<IActionResult> SendMailMissV100()
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
            var listboss = new List<string>
            {
                "C.01",
                "C.02"
            };
            // remove list sent
            var listused = new List<string>
            {
                "Nguyễn Thành Đạt",
                "Nguyễn Thái Bình",
                "Phương Bình"
            };
            #endregion

            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true)
                                                    && !m.UserName.Equals(Constants.System.account)
                                                    && !string.IsNullOrEmpty(m.Email)
                                                    && !listboss.Contains(m.NgachLuongCode)
                                                    && !listused.Contains(m.FullName)).ToList();
            var password = string.Empty;
            foreach (var employee in employees)
            {
                if (!string.IsNullOrEmpty(employee.Email))
                {
                    password = Guid.NewGuid().ToString("N").Substring(0, 6);
                    var sysPassword = Helpers.Helper.HashedPassword(password);

                    var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                    var update = Builders<Employee>.Update
                        .Set(m => m.Password, sysPassword);
                    dbContext.Employees.UpdateOne(filterUpdate, update);
                    SendMailRegister2(employee, password);
                }
            }

            return Json(new { result = true, source = "sendmail", message = "Gửi mail thành công" });
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
                entity.FullName,
                url,
                entity.UserName,
                pwd,
                entity.Email);

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                Subject = subject,
                BodyContent = messageBody,
                Type = "register"
            };

            _emailSender.SendEmail(emailMessage);
        }

        public void SendMailRegister2(Employee entity, string pwd)
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
                    + "Confirm_Account_Registration_2.html";
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
                Type = "register"
            };

            _emailSender.SendEmail(emailMessage);
        }
        #endregion
    }
}
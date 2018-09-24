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
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Common.Enums;

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.LinkSalary.Main)]
    public class SalaryController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalaryController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalaryController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        [Route(Constants.LinkSalary.Index)]
        public IActionResult Index()
        {
            var loginId = User.Identity.Name;
            // get information user
            var employee = dbContext.Employees.Find(m => m.Id.Equals(loginId)).FirstOrDefault();
            var viewModel = new SalaryViewModel
            {
                Employee = employee
            };
            return View(viewModel);
        }

        [Route(Constants.LinkSalary.ThangBangLuong)]
        public async Task<IActionResult> ThangBangLuong()
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                SalaryThangBangLuongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Law.Equals(false)).ToListAsync(),
                SalaryThangBangPhuCapPhucLoisReal = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false)).ToListAsync()
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.ThangBangLuongLaw)]
        public async Task<IActionResult> ThangBangLuongLaw()
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                SalaryThangBangLuongLaws = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false) & m.Law.Equals(true)).ToListAsync(),
                SalaryThangBangPhuCapPhucLois = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false)).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.UpdateThangBangLuongLaw)]
        public async Task<IActionResult> UpdateThangBangLuongLaw(ThangBangLuongViewModel viewModel)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Leave.Equals(false) && m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            try
            {
                var now = DateTime.Now;

                #region ToiThieuVung
                var salaryMucLuongVung = viewModel.SalaryMucLuongVung;
                var builderSalaryMucLuongVung = Builders<SalaryMucLuongVung>.Filter;
                var filterSalaryMucLuongVung = builderSalaryMucLuongVung.Eq(m => m.Id, salaryMucLuongVung.Id);
                var updateSalaryMucLuongVung = Builders<SalaryMucLuongVung>.Update
                    .Set(m => m.ToiThieuVungQuiDinh, salaryMucLuongVung.ToiThieuVungQuiDinh)
                    .Set(m => m.ToiThieuVungDoanhNghiepApDung, salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung)
                    .Set(m => m.UpdatedOn, now);
                dbContext.SalaryMucLuongVungs.UpdateOne(filterSalaryMucLuongVung, updateSalaryMucLuongVung);
                #endregion

                #region SalaryThangBangLuongLaws
                decimal salaryMin = salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung;
                var salaryThangBangLuongLaws = viewModel.SalaryThangBangLuongLaws;
                var groups = (from a in salaryThangBangLuongLaws
                              group a by new
                              {
                                  a.MaSo
                              }
                                                    into b
                              select new
                              {
                                  MaSoName = b.Key.MaSo,
                                  Salaries = b.ToList(),
                              }).ToList();

                foreach (var group in groups)
                {
                    // reset salaryDeclareTax;
                    var salaryDeclareTax = salaryMin;
                    foreach (var level in group.Salaries)
                    {
                        salaryDeclareTax = level.HeSo * salaryDeclareTax;
                        var builderSalaryThangBangLuong = Builders<SalaryThangBangLuong>.Filter;
                        var filterSalaryThangBangLuong = builderSalaryThangBangLuong.Eq(m => m.Id, level.Id);
                        var updateSalaryThangBangLuong = Builders<SalaryThangBangLuong>.Update
                            .Set(m => m.MucLuong, salaryDeclareTax)
                            .Set(m => m.HeSo, level.HeSo)
                            .Set(m => m.UpdatedOn, now);
                        dbContext.SalaryThangBangLuongs.UpdateOne(filterSalaryThangBangLuong, updateSalaryThangBangLuong);
                    }
                }
                #endregion

                #region SalaryThangBangPhuCapPhucLois

                foreach (var phucap in viewModel.SalaryThangBangPhuCapPhucLois)
                {
                    // Update if id not null
                    if (!string.IsNullOrEmpty(phucap.Id))
                    {
                        var builderSalaryThangBangPhuCapPhucLoi = Builders<SalaryThangBangPhuCapPhucLoi>.Filter;
                        var filterSalaryThangBangPhuCapPhucLoi = builderSalaryThangBangPhuCapPhucLoi.Eq(m => m.Id, phucap.Id);
                        var updateSalaryThangBangPhuCapPhucLoi = Builders<SalaryThangBangPhuCapPhucLoi>.Update
                            .Set(m => m.Money, phucap.Money)
                            .Set(m => m.UpdatedOn, now);
                        dbContext.SalaryThangBangPhuCapPhucLois.UpdateOne(filterSalaryThangBangPhuCapPhucLoi, updateSalaryThangBangPhuCapPhucLoi);
                    }
                    else
                    {
                        var phucapInformation = dbContext.SalaryPhuCapPhucLois.Find(m => m.Code.Equals(phucap.Code)).FirstOrDefault();
                        if (phucapInformation != null)
                        {
                            phucap.Name = phucapInformation.Name;
                        }
                        dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(phucap);
                    }
                }
                #endregion

                // PROCCESSING

                #region Activities
                // Update multi, insert multi
                string s = JsonConvert.SerializeObject(viewModel.SalaryThangBangLuongLaws);
                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.SalaryThangBangLuong,
                    Action = Constants.Action.Create,
                    Value = s,
                    Content = Constants.Action.Create,
                };
                await dbContext.TrackingUsers.InsertOneAsync(activity);
                #endregion

                return Json(new { result = true, source = "create", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "create", id = string.Empty, message = ex.Message });
            }
        }

        #region Sub
        [Route(Constants.LinkSalary.CalculatorThangBangLuongLaw)]
        public IActionResult CalculatorThangBangLuongLaw(decimal money, decimal heso, string id)
        {
            var list = new List<IdMoney>();
            decimal salaryMin = dbContext.SalaryMucLuongVungs.Find(m=>m.Enable.Equals(true)).First().ToiThieuVungDoanhNghiepApDung; // use reset
            if (money > 0)
            {
                salaryMin = money;
            }

            // if id null: calculator all.
            // else: get information by id=> calculator from [Bac] and return by group.
            if (!string.IsNullOrEmpty(id))
            {
                var currentLevel = dbContext.SalaryThangBangLuongs.Find(m => m.Id.Equals(id)).FirstOrDefault();
                if (currentLevel != null)
                {
                    var bac = currentLevel.Bac;
                    var maso = currentLevel.MaSo;
                    if (heso == 0)
                    {
                        heso = currentLevel.HeSo;
                    }
                    var salaryDeclareTax = heso * salaryMin;
                    if (bac > 1)
                    {
                        var previousBac = bac - 1;
                        var previousBacEntity = dbContext.SalaryThangBangLuongs.Find(m => m.MaSo.Equals(maso) & m.Bac.Equals(previousBac)).FirstOrDefault();
                        if (previousBacEntity != null)
                        {
                            salaryDeclareTax = heso * previousBacEntity.MucLuong;
                        }
                    }
                    // Add current change
                    list.Add(new IdMoney
                    {
                        Id = currentLevel.Id,
                        Money = salaryDeclareTax,
                        Rate = heso
                    });
                    var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(true) & m.MaSo.Equals(maso)).ToList();
                    
                    foreach (var level in levels)
                    {
                        if (level.Bac > bac)
                        {
                            salaryDeclareTax = level.HeSo * salaryDeclareTax;
                            list.Add(new IdMoney
                            {
                                Id = level.Id,
                                Money = salaryDeclareTax,
                                Rate = level.HeSo
                            });
                        }
                    }
                }
            }
            else
            {
                var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(true)).ToList();
                // group by MaSo
                var groups = (from s in levels
                              group s by new
                              {
                                  s.MaSo
                              }
                                                    into l
                              select new
                              {
                                  MaSoName = l.Key.MaSo,
                                  Salaries = l.ToList(),
                              }).ToList();

                foreach (var group in groups)
                {
                    // reset salaryDeclareTax;
                    var salaryDeclareTax = salaryMin;
                    foreach (var level in group.Salaries)
                    {
                        salaryDeclareTax = level.HeSo * salaryDeclareTax;
                        list.Add(new IdMoney
                        {
                            Id = level.Id,
                            Money = salaryDeclareTax,
                            Rate = level.HeSo
                        });
                    }
                }
            }

            return Json(new { result = true, list });
        }

        [Route(Constants.LinkSalary.CalculatorThangBangLuongReal)]
        public IActionResult CalculatorThangBangLuongReal(string id, decimal heso, decimal money)
        {
            if (heso != 0 || money != 0)
            {
                var list = new List<IdMoney>();
                decimal salaryMin = dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).First().ToiThieuVungDoanhNghiepApDung; // use reset
                if (money > 0)
                {
                    salaryMin = money;
                }

                // if id null: calculator all.
                // else: get information by id=> calculator from [Bac] and return by group.
                if (!string.IsNullOrEmpty(id))
                {
                    var currentLevel = dbContext.SalaryThangBangLuongs.Find(m => m.Id.Equals(id)).FirstOrDefault();
                    if (currentLevel != null)
                    {
                        var bac = currentLevel.Bac;
                        var vitriCode = currentLevel.ViTriCode;
                        if (heso == 0)
                        {
                            heso = currentLevel.HeSo;
                        }
                        // Rule bac 1 =  muc tham chieu
                        var salaryDeclareTax = Math.Round(salaryMin, 0); 
                        if (bac > 1)
                        {
                            var previousBac = bac - 1;
                            var previousBacEntity = dbContext.SalaryThangBangLuongs.Find(m => m.ViTriCode.Equals(vitriCode) & m.Bac.Equals(previousBac)).FirstOrDefault();
                            if (previousBacEntity != null)
                            {
                                salaryDeclareTax = heso * previousBacEntity.MucLuong;
                            }
                        }
                        // Add current change
                        list.Add(new IdMoney
                        {
                            Id = currentLevel.Id,
                            Money = salaryDeclareTax,
                            Rate = heso
                        });
                        var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(false) & m.ViTriCode.Equals(vitriCode)).ToList();

                        foreach (var level in levels)
                        {
                            if (level.Bac > bac)
                            {
                                salaryDeclareTax = Math.Round(heso * salaryDeclareTax, 0);
                                list.Add(new IdMoney
                                {
                                    Id = level.Id,
                                    Money = salaryDeclareTax,
                                    Rate = heso
                                });
                            }
                        }
                    }
                }
                else
                {
                    // Ap dung nếu hệ số bậc là 1 + Muc Luong is min.
                    var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Bac.Equals(1) & m.HeSo.Equals(1) & m.MucLuong.Equals(salaryMin)).ToList();

                    // group by VITRI
                    var groups = (from s in levels
                                  group s by new
                                  {
                                      s.ViTriCode
                                  }
                                                        into l
                                  select new
                                  {
                                      l.Key.ViTriCode,
                                      Salaries = l.ToList(),
                                  }).ToList();

                    foreach (var group in groups)
                    {
                        // reset salaryDeclareTax;
                        var salaryDeclareTax = salaryMin;
                        foreach (var level in group.Salaries)
                        {
                            salaryDeclareTax = level.HeSo * salaryDeclareTax;
                            list.Add(new IdMoney
                            {
                                Id = level.Id,
                                Money = salaryDeclareTax,
                                Rate = level.HeSo
                            });
                        }
                    }
                }
                return Json(new { result = true, list });
            }

            return Json(new { result = false });
        }

        [Route(Constants.LinkSalary.UpdateData)]
        public IActionResult UpdateData()
        {
            InitLuongToiThieuVung();
            InitLuongFeeLaw();
            InitThangBangLuong();
            InitSalaryPhuCapPhucLoi();
            InitSalaryThangBangPhuCapPhucLoi();
            InitChucDanhCongViec();

            return Json(new { result = true });
        }

        private void InitLuongToiThieuVung()
        {
            dbContext.SalaryMucLuongVungs.DeleteMany(new BsonDocument());
            dbContext.SalaryMucLuongVungs.InsertOne(new SalaryMucLuongVung()
            {
                ToiThieuVungQuiDinh =  3980000,
                TiLeMucDoanhNghiepApDung = (decimal)1.07,
                ToiThieuVungDoanhNghiepApDung = 3980000*(decimal)1.07
            });
        }

        private void InitLuongFeeLaw()
        {
            dbContext.SalaryFeeLaws.DeleteMany(new BsonDocument());
            dbContext.SalaryFeeLaws.InsertOne(new SalaryFeeLaw()
            {
                Name = "Bảo hiểm xã hội",
                NameAlias = Utility.AliasConvert("Bảo hiểm xã hội"),
                TiLeDong = (decimal)0.105,
                Description = "Bao gồm: BHXH (8%), BHYT(1.5%), Thất nghiệp (1%). Theo Quyết định 595/QĐ-BHXH."
            });
        }

        private void InitThangBangLuong()
        {
            dbContext.SalaryThangBangLuongs.DeleteMany(mbox=>mbox.FlagReal.Equals(false));
            // default muc luong = toi thieu, HR update later.
            decimal salaryMin = 3980000 * (decimal)1.07; // use reset
            decimal salaryDeclareTax = salaryMin;
            // Company no use now. sử dụng từng vị trí đặc thù. Hi vong tương lai áp dụng.
            decimal salaryReal = salaryDeclareTax; // First set real salary default, update later

            var name = string.Empty;
            var nameAlias = string.Empty;
            var maso = string.Empty;
            var typeRole = string.Empty;
            var typeRoleAlias = string.Empty;
            var typeRoleCode = string.Empty;

            #region 1- BẢNG LƯƠNG CHỨC VỤ QUẢN LÝ DOANH NGHIỆP
            typeRole = "CHỨC VỤ QUẢN LÝ DOANH NGHIỆP";
            typeRoleAlias = Utility.AliasConvert(typeRole);
            typeRoleCode = "C";
            // 01- TỔNG GIÁM ĐỐC 
            name = "TỔNG GIÁM ĐỐC";
            maso = "C.01";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 1; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 02-GIÁM ĐỐC/TRƯỞNG BAN
            name = "GIÁM ĐỐC/TRƯỞNG BAN";
            maso = "C.02";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 1; i <= 10; i++)
            {
                decimal heso = (decimal)1.8;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 03- KẾ TOÁN TRƯỞNG/ PHÓ GIÁM ĐỐC
            name = "KẾ TOÁN TRƯỞNG/ PHÓ GIÁM ĐỐC";
            maso = "C.03";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 1; i <= 10; i++)
            {
                decimal heso = (decimal)1.7;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            #endregion

            #region 2- BẢNG LƯƠNG VIÊN CHỨC CHUYÊN MÔN, THỪA HÀNH, PHỤC VỤ
            typeRole = "VIÊN CHỨC CHUYÊN MÔN, THỪA HÀNH, PHỤC VỤ";
            typeRoleAlias = Utility.AliasConvert(typeRole);
            typeRoleCode = "D";
            // 01- TRƯỞNG BP THIẾT KẾ, TRƯỞNG BP GS KT, KẾ TOÁN TỔNG HỢP, QUẢN LÝ THUẾ…
            name = "TRƯỞNG BP THIẾT KẾ, TRƯỞNG BP GS KT, KẾ TOÁN TỔNG HỢP, QUẢN LÝ THUẾ…";
            maso = "D.01";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 1; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 02- TRƯỞNG SALE, NHÂN VIÊN NHÂN SỰ, CV THU MUA, CV PHÁP CHẾ, CV KẾ HOẠCH TỔNG HỢP, CV MÔI TRƯỜNG, CV NC KHCN, CV PHÒNG TN….
            name = "TRƯỞNG SALE, NHÂN VIÊN NHÂN SỰ, CV THU MUA, CV PHÁP CHẾ, CV KẾ HOẠCH TỔNG HỢP, CV MÔI TRƯỜNG, CV NC KHCN, CV PHÒNG TN…";
            maso = "D.02";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 1; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            // 03- NV KINH DOANH, SALE ADMIN, NV HÀNH CHÍNH, NV KẾ TOÁN, THỦ QUỸ, NV DỰ ÁN, NV KỸ THUẬT, …
            name = "NV KINH DOANH, SALE ADMIN, NV HÀNH CHÍNH, NV KẾ TOÁN, THỦ QUỸ, NV DỰ ÁN, NV KỸ THUẬT,…";
            maso = "D.03";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 1; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            #endregion

            #region 3- BẢNG LƯƠNG NHÂN VIÊN TRỰC TIẾP SẢN XUẤT KINH DOANH VÀ PHỤC VỤ
            typeRole = "NHÂN VIÊN TRỰC TIẾP SẢN XUẤT KINH DOANH VÀ PHỤC VỤ";
            typeRoleAlias = Utility.AliasConvert(typeRole);
            typeRoleCode = "B";
            // 01- TRƯỞNG BP -NM…
            name = "TRƯỞNG BP -NM…";
            maso = "B.01";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 1; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 02- TỔ TRƯỞNG NM, TỔ PHÓ NM, TỔ TRƯỞNG LOGISTICS, QUẢN LÝ TRẠM CÂN…
            name = "TỔ TRƯỞNG NM, TỔ PHÓ NM, TỔ TRƯỞNG LOGISTICS, QUẢN LÝ TRẠM CÂN…";
            maso = "B.02";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 1; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 03- TÀI XẾ, NV KHO, NV ĐIỀU ĐỘ, NV CẢNH QUAN, NV BẢO TRÌ, NV TRẠM CÂN…
            name = "TÀI XẾ, NV KHO, NV ĐIỀU ĐỘ, NV CẢNH QUAN, NV BẢO TRÌ, NV TRẠM CÂN…";
            maso = "B.03";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 1; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 04- GIAO NHẬN, PHỤ XE, BẢO VỆ, CÔNG NHÂN…
            name = "GIAO NHẬN, PHỤ XE, BẢO VỆ, CÔNG NHÂN…";
            maso = "B.04";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 1; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            #endregion
        }

        private void InitSalaryPhuCapPhucLoi()
        {
            dbContext.SalaryPhuCapPhucLois.DeleteMany(new BsonDocument());

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            #region Phu Cap
            int type = 1; // phu-cap
            var name = string.Empty;
            int i = 1;

            name = "NẶNG NHỌC ĐỘC HẠI";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") +"-"+ i.ToString("000"),
            });
            i++;
            name = "TRÁCH NHIỆM";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "THÂM NIÊN";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "THU HÚT";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;
            #endregion

            #region Phuc Loi
            type = 2; // phuc-loi
            i = 1;
            
            name = "XĂNG";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "ĐIỆN THOẠI";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "CƠM";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "Kiêm nhiệm";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "BHYT ĐẶC BIỆT";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });

            name = "VỊ TRÍ CẦN KN NHIỀU NĂM";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });

            name = "Vị trí đặc thù";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });

            name = "Nhà ở";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;
            #endregion
        }

        private void InitSalaryThangBangPhuCapPhucLoi()
        {
            dbContext.SalaryThangBangPhuCapPhucLois.DeleteMany(new BsonDocument());
            #region TGD
            // Trach nhiem 01-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.01",
                Money = 500000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.01",
                Money = 500000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.01",
                Money = 500000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.01",
                Money = 500000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.01",
                Money = 0
            });
            #endregion

            #region GĐ/PGĐ
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.02",
                Money = 300000
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.02",
                Money = 400000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.02",
                Money = 300000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.02",
                Money = 400000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.02",
                Money = 0
            });
            #endregion

            #region KT trưởng
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.03",
                Money = 300000
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.03",
                Money = 400000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.03",
                Money = 300000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.03",
                Money = 400000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.03",
                Money = 0
            });
            #endregion

            #region Trưởng BP
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "D.01",
                Money = 200000
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "D.01",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "D.01",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "D.01",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.01",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.01",
                Money = 200000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "B.01",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "B.01",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "B.01",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.01",
                Money = 0
            });
            #endregion

            #region Tổ trưởng
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "D.02",
                Money = 100000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "D.02",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "D.02",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "D.02",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.02",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.02",
                Money = 100000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "B.02",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "B.02",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "B.02",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.02",
                Money = 0
            });
            #endregion

            #region Tổ phó
            // B.02
            #endregion

            #region Others
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "D.03",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.03",
                Money = 200000
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.03",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.03",
                Money = 200000
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.04",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.04",
                Money = 200000
            });
            #endregion
        }

        // Base on ThangBangLuong. Do later
        private void InitChucDanhCongViec()
        {
            dbContext.ChucDanhCongViecs.DeleteMany(new BsonDocument());
            var listTemp = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) && m.Law.Equals(true)).ToList();
            foreach(var item in listTemp)
            {
                if (!(dbContext.ChucDanhCongViecs.CountDocuments(m=>m.Code.Equals(item.MaSo)) > 0)){
                    dbContext.ChucDanhCongViecs.InsertOne(new ChucDanhCongViec()
                    {
                        Name = item.Name,
                        Alias = item.NameAlias,
                        Code = item.MaSo,
                        Type = item.TypeRole,
                        TypeAlias = item.TypeRoleAlias,
                        TypeCode = item.TypeRoleCode
                    });
                }
            }
        }

        #endregion

    }
}
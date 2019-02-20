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
using NPOI.HSSF.Util;
using NPOI.SS.Util;

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.LinkSalary.Main)]
    public class SalarySettingController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalarySettingController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalarySettingController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _logger = logger;
        }

        [Route(Constants.LinkSalary.Setting)]
        public async Task<IActionResult> Setting()
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

            var viewModel = new BangLuongViewModel
            {
                SalarySettings = await dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToListAsync()
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.Setting + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SettingUpdate()
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

            var viewModel = new BangLuongViewModel
            {
                SalarySettings = await dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.Setting + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SettingUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.SalarySettings)
            {
                var builder = Builders<SalarySetting>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<SalarySetting>.Update
                    .Set(m => m.Value, item.Value)
                    .Set(m => m.Description, item.Description)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalarySettings.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        #region UPDATE COMMON DATA, USE FOR ALL SALARIES
        [Route(Constants.LinkSalary.Init)]
        public IActionResult Init()
        {
            InitCaiDat();
            //// Run 1 times, runned on 28 Nov 2018
            //InitNgachLuong();
            //InitNgachLuongLaw();
            //InitCapNhatTitleNhanSu();
            //dbContext.SalaryThangBangPhuCapPhucLois.DeleteMany(m => true);
            //InitThangBangPhuCapPhucLoiLaw();
            //// Run continute
            // 1. Tai lieu thang luong
            // 2. Tai lieu bang luong

            // 29Nov2018.
            // Cap nhat khoi tinh luong + ngach luong + he so luong cho nhan vien
            // 1. Tai lieu nhan vien nha may
            // 2. Tai lieu nhan vien san xuat

            return Json(new { result = true });
        }

        #region Auto
        private void InitCaiDat()
        {
            dbContext.SalarySettings.DeleteMany(new BsonDocument());
            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "mau-so-lam-viec",
                Value = "26",
                Title = "Ngày làm việc"
            });

            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "mau-so-khac",
                Value = "27",
                Title = "Ngày làm việc"
            });
            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "mau-so-bao-ve",
                Value = "30",
                Title = "Ngày làm việc"
            });

            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "ty-le-dong-bh",
                Value = "0.105",
                Title = "Tỷ lệ đóng BH"
            });

            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "mau-so-chuyen-can",
                Value = "300000",
                Title = "Muc thuong chuyen can"
            });

            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "mau-so-tien-com",
                Value = "22000",
                Title = "Tiền cơm"
            });

            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "ti-le-bhxh",
                Value = "8",
                Title = "Tỉ lệ đóng bhxh"
            });

            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "ti-le-bhyt",
                Value = "1.5",
                Title = "Tỉ lệ đóng bhyt"
            });

            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "ti-le-bhtn",
                Value = "1",
                Title = "Tỉ lệ đóng bhtn"
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

        private void InitChucDanhCongViec()
        {
            dbContext.ChucDanhCongViecs.DeleteMany(new BsonDocument());
            var listTemp = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) && m.Law.Equals(true)).ToList();
            foreach (var item in listTemp)
            {
                if (!(dbContext.ChucDanhCongViecs.CountDocuments(m => m.Code.Equals(item.MaSo)) > 0))
                {
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

        private void InitNgachLuong()
        {
            dbContext.NgachLuongs.DeleteMany(m => m.Law.Equals(false));
            decimal salaryMin = 4012500; // use reset
            dbContext.NgachLuongs.DeleteMany(m => true);
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
            decimal heso = (decimal)6.5;
            decimal tiLe = (decimal)1.5;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }

            // 02. PHÓ TỔNG GIÁM ĐỐC/ GĐ ĐIỀU HÀNH
            name = "PHÓ TỔNG GIÁM ĐỐC/ GĐ ĐIỀU HÀNH";
            maso = "C.02";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)3.5;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }

            // 03- KẾ TOÁN TRƯỞNG/ Q.GIÁM ĐỐC / TRƯỞNG BAN
            name = "KẾ TOÁN TRƯỞNG/ Q.GIÁM ĐỐC / TRƯỞNG BAN";
            maso = "C.03";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)2.5;
            tiLe = 1;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }
            #endregion

            #region 2- BẢNG LƯƠNG VIÊN CHỨC CHUYÊN MÔN, THỪA HÀNH, PHỤC VỤ
            typeRole = "VIÊN CHỨC CHUYÊN MÔN, THỪA HÀNH, PHỤC VỤ";
            typeRoleAlias = Utility.AliasConvert(typeRole);
            typeRoleCode = "D";
            // 01- CHUYÊN GIA CAO CẤP/ NGHỆ NHÂN
            name = "CHUYÊN GIA CAO CẤP/ NGHỆ NHÂN";
            maso = "D.01";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)2.5;
            tiLe = (decimal)0.8;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }

            // 02- TRƯỞNG BỘ PHẬN, KẾ TOÁN TỔNG HỢP, CHUYÊN VIÊN
            name = "TRƯỞNG BỘ PHẬN, KẾ TOÁN TỔNG HỢP, CHUYÊN VIÊN";
            maso = "D.02";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.6;
            tiLe = (decimal)0.2;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }
            // 03- KẾ TOÁN VIÊN, NV HCNS, THỦ QUỸ, NV KỸ THUẬT ...
            name = "KẾ TOÁN VIÊN, NV HCNS, THỦ QUỸ, NV KỸ THUẬT ...";
            maso = "D.03";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.3;
            tiLe = (decimal)0.1;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }
            // 04- NV KINH DOANH/ SALE ADMIN
            name = "NV KINH DOANH/ SALE ADMIN";
            maso = "D.04";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.2;
            tiLe = (decimal)0.2;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }
            // 05- VĂN THƯ, LỄ TÂN
            name = "VĂN THƯ, LỄ TÂN";
            maso = "D.05";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1;
            tiLe = (decimal)0.1;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }
            #endregion

            #region 3- BẢNG LƯƠNG NHÂN VIÊN TRỰC TIẾP SẢN XUẤT KINH DOANH VÀ PHỤC VỤ
            typeRole = "NHÂN VIÊN TRỰC TIẾP SẢN XUẤT KINH DOANH VÀ PHỤC VỤ";
            typeRoleAlias = Utility.AliasConvert(typeRole);
            typeRoleCode = "B";
            // 01- TỔ TRƯỞNG, NV KHO, GIÁM SÁT NHÀ MÁY
            name = "TỔ TRƯỞNG, NV KHO, GIÁM SÁT NHÀ MÁY";
            maso = "B.01";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.17;
            tiLe = (decimal)(1.22 - 1.17);
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }

            // 02- TÀI XẾ XE CƠ GIỚI, CÔNG NHÂN KT
            name = "TÀI XẾ XE CƠ GIỚI, CÔNG NHÂN KT";
            maso = "B.02";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.1;
            tiLe = (decimal)(1.17 - 1.1);
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }
            // 03- TÀI XẾ PHỤC VỤ KINH DOANH
            name = "TÀI XẾ PHỤC VỤ KINH DOANH";
            maso = "B.03";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.2;
            tiLe = (decimal)0.1;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }
            // 04- PHỤ XE CÓ TAY NGHỀ (Lái xe được)
            name = "PHỤ XE CÓ TAY NGHỀ";
            maso = "B.04";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.05;
            tiLe = (decimal)(1.1 - 1.05);
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }
            // 05- TẠP VỤ, BẢO VỆ; CÔNG NHÂN SX
            name = "TẠP VỤ, BẢO VỆ; CÔNG NHÂN SX";
            maso = "B.05";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.05;
            tiLe = (decimal)(1.12 - 1.05);
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }
            // 06- BỐC XẾP, PHỤ XE KINH DOANH
            name = "BỐC XẾP, PHỤ XE KINH DOANH";
            maso = "B.06";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1;
            tiLe = (decimal)0.05;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
            }
            #endregion
        }

        private void InitNgachLuongLaw()
        {
            dbContext.NgachLuongs.DeleteMany(m => m.Law.Equals(true));
            // default muc luong = toi thieu, HR update later.
            decimal salaryMin = 3980000 * (decimal)1.07; // use reset

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
            decimal heso = (decimal)2.5;
            decimal tiLe = (decimal)0.05;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = true
                });
            }

            // 02-GIÁM ĐỐC/TRƯỞNG BAN
            name = "GIÁM ĐỐC/TRƯỞNG BAN";
            maso = "C.02";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.8;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = true
                });
            }

            // 03- KẾ TOÁN TRƯỞNG/ PHÓ GIÁM ĐỐC
            name = "KẾ TOÁN TRƯỞNG/ PHÓ GIÁM ĐỐC";
            maso = "C.03";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.7;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = true
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
            heso = (decimal)1.0;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = true
                });
            }

            // 02- TRƯỞNG SALE, NHÂN VIÊN NHÂN SỰ, CV THU MUA, CV PHÁP CHẾ, CV KẾ HOẠCH TỔNG HỢP, CV MÔI TRƯỜNG, CV NC KHCN, CV PHÒNG TN….
            name = "TRƯỞNG SALE, NHÂN VIÊN NHÂN SỰ, CV THU MUA, CV PHÁP CHẾ, CV KẾ HOẠCH TỔNG HỢP, CV MÔI TRƯỜNG, CV NC KHCN, CV PHÒNG TN…";
            maso = "D.02";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.0;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = true
                });
            }

            // 03- NV KINH DOANH, SALE ADMIN, NV HÀNH CHÍNH, NV KẾ TOÁN, THỦ QUỸ, NV DỰ ÁN, NV KỸ THUẬT, …
            name = "NV KINH DOANH, SALE ADMIN, NV HÀNH CHÍNH, NV KẾ TOÁN, THỦ QUỸ, NV DỰ ÁN, NV KỸ THUẬT,…";
            maso = "D.03";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.0;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = true
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
            heso = (decimal)1.0;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = true
                });
            }

            // 02- TỔ TRƯỞNG NM, TỔ PHÓ NM, TỔ TRƯỞNG LOGISTICS, QUẢN LÝ TRẠM CÂN…
            name = "TỔ TRƯỞNG NM, TỔ PHÓ NM, TỔ TRƯỞNG LOGISTICS, QUẢN LÝ TRẠM CÂN…";
            maso = "B.02";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.0;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = true
                });
            }

            // 03- TÀI XẾ, NV KHO, NV ĐIỀU ĐỘ, NV CẢNH QUAN, NV BẢO TRÌ, NV TRẠM CÂN…
            name = "TÀI XẾ, NV KHO, NV ĐIỀU ĐỘ, NV CẢNH QUAN, NV BẢO TRÌ, NV TRẠM CÂN…";
            maso = "B.03";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.0;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = true
                });
            }

            // 04- GIAO NHẬN, PHỤ XE, BẢO VỆ, CÔNG NHÂN…
            name = "GIAO NHẬN, PHỤ XE, BẢO VỆ, CÔNG NHÂN…";
            maso = "B.04";
            nameAlias = Utility.AliasConvert(name);
            heso = (decimal)1.0;
            for (int i = 1; i <= 10; i++)
            {
                if (i > 1)
                {
                    heso += tiLe;
                }
                dbContext.NgachLuongs.InsertOne(new NgachLuong()
                {
                    ChucDanhCongViec = name,
                    Alias = nameAlias,
                    MaSo = maso,
                    TiLe = tiLe,
                    HeSo = heso,
                    Bac = i,
                    MucLuongThang = heso * salaryMin,
                    MucLuongNgay = (heso * salaryMin) / 26,
                    Order = 1,
                    TypeRole = typeRole,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = true
                });
            }
            #endregion
        }

        private void InitLuongToiThieuVung()
        {
            dbContext.SalaryMucLuongVungs.DeleteMany(new BsonDocument());
            dbContext.SalaryMucLuongVungs.InsertOne(new SalaryMucLuongVung()
            {
                ToiThieuVungQuiDinh = 3980000,
                TiLeMucDoanhNghiepApDung = (decimal)1.07,
                ToiThieuVungDoanhNghiepApDung = 3980000 * (decimal)1.07,
                Month = 8,
                Year = 2018
            });
        }

        private void InitCapNhatTitleNhanSu()
        {
            var employees = dbContext.Employees.Find(m => true).ToList();
            foreach (var employee in employees)
            {
                if (!string.IsNullOrEmpty(employee.SalaryChucVuViTriCode))
                {
                    var viTriE = dbContext.SalaryThangBangLuongs.Find(m => m.ViTriCode.Equals(employee.SalaryChucVuViTriCode)).SortByDescending(m => m.UpdatedOn).FirstOrDefault();
                    if (viTriE != null)
                    {
                        employee.Title = viTriE.ViTri;
                        employee.SalaryChucVu = viTriE.ViTri;
                    }
                }

                var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.Title, employee.Title)
                    .Set(m => m.SalaryChucVu, employee.SalaryChucVu);
                dbContext.Employees.UpdateOne(filter, update);
            }
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
                Code = type.ToString("00") + "-" + i.ToString("000"),
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

        private void InitThangBangPhuCapPhucLoiLaw()
        {
            dbContext.SalaryThangBangPhuCapPhucLois.DeleteMany(m => m.Law.Equals(true));
            #region TGD
            // Trach nhiem 01-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.01",
                Money = 500000,
                Law = true
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.01",
                Money = 500000,
                Law = true
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.01",
                Money = 500000,
                Law = true
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.01",
                Money = 500000,
                Law = true
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.01",
                Money = 0,
                Law = true
            });
            #endregion

            #region GĐ/PGĐ
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.02",
                Money = 300000,
                Law = true
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.02",
                Money = 400000,
                Law = true
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.02",
                Money = 300000,
                Law = true
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.02",
                Money = 400000,
                Law = true
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.02",
                Money = 0,
                Law = true
            });
            #endregion

            #region KT trưởng
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.03",
                Money = 300000,
                Law = true
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.03",
                Money = 400000,
                Law = true
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.03",
                Money = 300000,
                Law = true
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.03",
                Money = 400000,
                Law = true
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.03",
                Money = 0,
                Law = true
            });
            #endregion

            #region Trưởng BP
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "D.01",
                Money = 200000,
                Law = true
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "D.01",
                Money = 300000,
                Law = true
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "D.01",
                Money = 200000,
                Law = true
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "D.01",
                Money = 300000,
                Law = true
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.01",
                Money = 0,
                Law = true
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.01",
                Money = 200000,
                Law = true
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "B.01",
                Money = 300000,
                Law = true
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "B.01",
                Money = 200000,
                Law = true
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "B.01",
                Money = 300000,
                Law = true
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.01",
                Money = 0,
                Law = true
            });
            #endregion

            #region Tổ trưởng
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "D.02",
                Money = 100000,
                Law = true
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "D.02",
                Money = 300000,
                Law = true
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "D.02",
                Money = 200000,
                Law = true
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "D.02",
                Money = 300000,
                Law = true
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.02",
                Money = 0,
                Law = true
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.02",
                Money = 100000,
                Law = true
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "B.02",
                Money = 300000,
                Law = true
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "B.02",
                Money = 200000,
                Law = true
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "B.02",
                Money = 300000,
                Law = true
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.02",
                Money = 0,
                Law = true
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
                Money = 0,
                Law = true
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.03",
                Money = 200000,
                Law = true
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.03",
                Money = 0,
                Law = true
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.03",
                Money = 200000,
                Law = true
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.04",
                Money = 0,
                Law = true
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.04",
                Money = 200000,
                Law = true
            });
            #endregion
        }
        #endregion

        [Route(Constants.LinkSalary.VanPhong + "/" + Constants.LinkSalary.ThangLuong + "/" +Constants.LinkSalary.Document)]
        public IActionResult ThangLuongVP()
        {
            return View();
        }

        [Route(Constants.LinkSalary.VanPhong + "/" + Constants.LinkSalary.ThangLuong + "/" + Constants.LinkSalary.Document + "/" + Constants.ActionLink.Update)]
        [HttpPost]
        public ActionResult ThangLuongVPImport()
        {
            InitLuongToiThieuVung();
            // Cause file luong error format. break into multi file.
            var date = DateTime.Now;
            IFormFile file = Request.Form.Files[0];
            string folderName = Constants.Storage.Factories;
            string webRootPath = _env.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            if (file.Length > 0)
            {
                string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                string fullPath = Path.Combine(newPath, file.FileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Position = 0;
                    int headerCal = 0;
                    ISheet sheet0;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet0 = hssfwb.GetSheetAt(0);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet0 = hssfwb.GetSheetAt(0);
                    }

                    #region Sheet 0: Thang Bang Luong
                    dbContext.SalaryThangBangLuongs.DeleteMany(m => m.Law.Equals(false));
                    // Get min salary
                    var salaryMucLuongVung = dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefault();
                    decimal salaryMin = 0;
                    if (salaryMucLuongVung != null)
                    {
                        salaryMin = salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung;
                    }
                    headerCal = 4;
                    int viTriCode = 1;
                    for (int i = headerCal; i <= sheet0.LastRowNum; i++)
                    {
                        IRow row = sheet0.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var vitri = Utility.GetFormattedCellValue(row.GetCell(1)).Trim();
                        if (!string.IsNullOrEmpty(vitri))
                        {
                            var vitriFullCode = Constants.System.viTriCodeTBLuong + viTriCode.ToString("000");
                            var hesobac = (decimal)Utility.GetNumbericCellValue(row.GetCell(13));
                            // Min default each VITRI
                            var money = (decimal)Utility.GetNumbericCellValue(row.GetCell(14));
                            if (money == 0)
                            {
                                money = salaryMin;
                            }
                            else
                            {
                                money = money * 1000;
                            }
                            var vitriAlias = Utility.AliasConvert(vitri);

                            var exist = dbContext.SalaryThangBangLuongs.CountDocuments(m => m.ViTriAlias.Equals(vitriAlias) & m.Law.Equals(false));
                            if (exist == 0)
                            {
                                for (int lv = 1; lv <= 10; lv++)
                                {
                                    if (lv > 1)
                                    {
                                        money = hesobac * money;
                                    }
                                    dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                                    {
                                        Month = 8,
                                        Year = 2018,
                                        ViTri = vitri,
                                        Bac = lv,
                                        HeSo = hesobac,
                                        MucLuong = Math.Round(money, 0),
                                        ViTriCode = vitriFullCode,
                                        ViTriAlias = vitriAlias,
                                        Law = false
                                    });
                                }
                                viTriCode++;
                            }
                        }
                    }
                    #endregion
                }
            }
            return Json(new { url = "/" });
        }

        [Route(Constants.LinkSalary.VanPhong + "/" + Constants.LinkSalary.BangLuong + "/" + Constants.LinkSalary.Document)]
        public IActionResult LuongNhanVien()
        {
            return View();
        }

        [Route(Constants.LinkSalary.VanPhong + "/" + Constants.LinkSalary.BangLuong + "/" + Constants.LinkSalary.Document + "/" + Constants.ActionLink.Update)]
        [HttpPost]
        public ActionResult LuongNhanVienImport()
        {
            // Cause file luong error format. break into multi file.
            var date = DateTime.Now;
            IFormFile file = Request.Form.Files[0];
            string folderName = Constants.Storage.Factories;
            string webRootPath = _env.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            if (file.Length > 0)
            {
                string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                string fullPath = Path.Combine(newPath, file.FileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Position = 0;
                    int headerCal = 0;
                    ISheet sheet0;
                    //ISheet sheet1;
                    //ISheet sheet2;
                    //ISheet sheet3;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet0 = hssfwb.GetSheetAt(0);
                        //sheet1 = hssfwb.GetSheetAt(1);
                        //sheet2 = hssfwb.GetSheetAt(2);
                        //sheet3 = hssfwb.GetSheetAt(3);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet0 = hssfwb.GetSheetAt(0);
                        //sheet1 = hssfwb.GetSheetAt(1);
                        //sheet2 = hssfwb.GetSheetAt(2);
                        //sheet3 = hssfwb.GetSheetAt(3);
                    }

                    #region Sheet 0: Luong Nhan Vien
                    // Cap nhat thang bang luong cho nhan vien
                    // dbContext.SalaryThangBacLuongEmployees.DeleteMany(m => m.Law.Equals(false));
                    // Cap nhat phuc lơi cho nhan vien
                    dbContext.SalaryThangBangPhuCapPhucLois.DeleteMany(m => m.Law.Equals(false));
                    // Du lieu lương tháng 8
                    dbContext.SalaryEmployeeMonths.DeleteMany(m => true);

                    headerCal = 7;
                    var location = Constants.Location((int)EKhoiLamViec.NM);
                    int locationOrder = 0;
                    int groupChucDanhOrder = 0;
                    var groupChucDanh = string.Empty;
                    int year = 2018;
                    int month = 8;
                    var codePc = string.Empty;
                    var namePc = string.Empty;
                    var mauso = 26;
                    decimal moneyPc = 0;
                    string[] c02 = { "giam-doc", "truong-ban" };
                    string[] c03 = { "pho-giam-doc", "ke-toan-truong" };
                    string[] d01 = "TRƯỞNG BP THIẾT KẾ, TRƯỞNG BP GS KT, KẾ TOÁN TỔNG HỢP, QUẢN LÝ THUẾ, Trưởng BP GS kỹ thuật".Split(',');
                    string[] d02 = "TRƯỞNG SALE, NHÂN VIÊN NHÂN SỰ, CV THU MUA, CV PHÁP CHẾ, CV KẾ HOẠCH TỔNG HỢP, CV MÔI TRƯỜNG, CV NC KHCN, CV PHÒNG TN, Nhân viên hành chính/ HCNS NM, Chuyên viên pháp chế, CV thu mua vật tư, CV nghiên cứu ứng dụng SP".Split(',');
                    string[] d03 = "NV KINH DOANH, SALE ADMIN, NV HÀNH CHÍNH, NV KẾ TOÁN, THỦ QUỸ, NV DỰ ÁN, NV KỸ THUẬT, Kế toán quản trị, Kế toán nội bộ, NV sale, Admin điều vận, NV triển khai thực hiện dự án, NV nghiệm thu thanh toán".Split(',');

                    string[] b01 = "TRƯỞNG BP -NM, Trưởng BP kế hoạch & thống kê".Split(',');
                    string[] b02 = "TỔ TRƯỞNG NM, TỔ PHÓ NM, TỔ TRƯỞNG LOGISTICS, QUẢN LÝ TRẠM CÂN,Trưởng phòng điều độ nhân lực, Trưởng phòng điều độ cơ giới, Trưởng đội vận chuyển, Tổ trưởng GS môi trường,Tổ trưởng điều độ sản xuất, Trưởng phòng quản lý chất lượng sản phẩm,Tổ trưởng bảo trì cơ điện,Tổ trưởng vận hành máy,Tổ phó bảo trì ".Split(',');
                    string[] b03 = "TÀI XẾ, NV KHO, NV ĐIỀU ĐỘ, NV CẢNH QUAN, NV BẢO TRÌ, NV TRẠM CÂN, CV giám sát thi công,Giám sát kho,Nhân viên thống kê vận hành, NV QC inline ĐS - PB, NV thống kê ĐS - PB nguyên liệu, Nhân viên vận hành máy,Nhân viên bảo trì cơ khí".Split(',');
                    string[] b04 = "GIAO NHẬN, PHỤ XE, BẢO VỆ, CÔNG NHÂN".Split(',');

                    for (int i = headerCal; i <= sheet0.LastRowNum; i++)
                    {
                        IRow row = sheet0.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        //var style = row.GetCell(1).CellStyle.FillForegroundColorColor;
                        //if (style != null)
                        //{
                        //    var rgb = style.RGB;
                        //}

                        var locationTemp = Utility.GetFormattedCellValue(row.GetCell(1));
                        var groupChucDanhTemp = Utility.GetFormattedCellValue(row.GetCell(2));
                        if (string.IsNullOrEmpty(locationTemp) && string.IsNullOrEmpty(groupChucDanhTemp))
                        {
                            continue;
                        }

                        // Fix ma nv (bang luong vs bang nhan su khac nhau. Update theo bang luong. BASE Email)
                        var maNV = Utility.GetFormattedCellValue(row.GetCell(1)).Trim();
                        var fullName = Utility.GetFormattedCellValue(row.GetCell(2)).Trim();
                        var email = Utility.EmailConvert(fullName);
                        if (fullName == "Ngô Pa Ri")
                        {
                            email = "ngopari@tribat.vn";
                        }
                        var chucVu = Utility.GetFormattedCellValue(row.GetCell(3)).Trim();
                        if (!string.IsNullOrEmpty(chucVu))
                        {
                            var chucVuCode = string.Empty;
                            var joinDate = Utility.GetDateCellValue(row.GetCell(4));
                            var employeeId = string.Empty;
                            if (!string.IsNullOrEmpty(email))
                            {
                                var emp = dbContext.Employees.Find(m => m.Email.Equals(email)).FirstOrDefault();
                                if (emp == null)
                                {
                                    emp = dbContext.Employees.Find(m => m.FullName.Equals(fullName)).FirstOrDefault();
                                    if (emp == null)
                                    {
                                        emp = dbContext.Employees.Find(m => m.CodeOld.Equals(maNV)).FirstOrDefault();
                                        if (emp != null)
                                        {
                                            employeeId = emp.Id;
                                        }
                                    }
                                    else
                                    {
                                        employeeId = emp.Id;
                                    }
                                }
                                else
                                {
                                    employeeId = emp.Id;
                                }
                            }
                            var dateSpan = DateTimeSpan.CompareDates(joinDate, DateTime.Now);
                            var bac = Convert.ToInt32(Utility.GetNumbericCellValue(row.GetCell(5)));
                            var luongCB = Utility.GetNumbericCellValue(row.GetCell(9)) * 1000;
                            if (!string.IsNullOrEmpty(chucVu))
                            {
                                var vitriEntity = dbContext.SalaryThangBangLuongs.Find(m => m.Law.Equals(false) & m.ViTri.Equals(chucVu) & m.Bac.Equals(bac)).FirstOrDefault();
                                if (vitriEntity != null)
                                {
                                    chucVuCode = vitriEntity.ViTriCode;
                                    luongCB = (double)vitriEntity.MucLuong;
                                }
                            }
                            var nangNhoc = Utility.GetNumbericCellValue(row.GetCell(10)) * 1000;
                            var trachNhiem = Utility.GetNumbericCellValue(row.GetCell(11)) * 1000;
                            var thamNiem = Utility.GetNumbericCellValue(row.GetCell(12)) * 1000;
                            if (thamNiem == 0)
                            {
                                //IF(ThamNienLamViecYear>=3,LuongCanBan*(0.03+(ThamNienLamViecYear-3)*0.01),0)
                                if (dateSpan.Years >= 3)
                                {
                                    thamNiem = luongCB * (0.03 + (dateSpan.Years - 3) * 0.01);
                                }
                            }
                            var thuHut = Utility.GetNumbericCellValue(row.GetCell(13)) * 1000;
                            var Xang = Utility.GetNumbericCellValue(row.GetCell(14)) * 1000;
                            var DienThoai = Utility.GetNumbericCellValue(row.GetCell(15)) * 1000;
                            var Com = Utility.GetNumbericCellValue(row.GetCell(16)) * 1000;
                            var KiemNhiem = Utility.GetNumbericCellValue(row.GetCell(17)) * 1000;
                            var BHYTDacBiet = Utility.GetNumbericCellValue(row.GetCell(18)) * 1000;
                            var viTriCanNhieuNamKinhNghiem = Utility.GetNumbericCellValue(row.GetCell(19)) * 1000;
                            var viTriDacThu = Utility.GetNumbericCellValue(row.GetCell(20)) * 1000;
                            var luongCanBanBaoGomPhuCap = luongCB + nangNhoc + trachNhiem + thamNiem + thuHut + Xang + DienThoai + Com + KiemNhiem + BHYTDacBiet + viTriCanNhieuNamKinhNghiem + viTriDacThu;
                            var ngayConglamViec = Utility.GetNumbericCellValue(row.GetCell(22));
                            var ngayNghiPhepHuongLuong = Utility.GetNumbericCellValue(row.GetCell(23));
                            var ngayNghiLeTetHuongLuong = Utility.GetNumbericCellValue(row.GetCell(24));
                            var congCNGio = Utility.GetNumbericCellValue(row.GetCell(25));
                            var congTangCaNgayThuongGio = Utility.GetNumbericCellValue(row.GetCell(26));
                            var congLeTet = Utility.GetNumbericCellValue(row.GetCell(27));
                            var congTacXa = Utility.GetNumbericCellValue(row.GetCell(28));
                            var mucDatTrongThang = Utility.GetNumbericCellValue(row.GetCell(29));
                            var luongTheoDoanhThuDoanhSo = Utility.GetNumbericCellValue(row.GetCell(30)) * 1000;
                            var tongBunBoc = Utility.GetNumbericCellValue(row.GetCell(31));
                            var thanhTienBunBoc = Utility.GetNumbericCellValue(row.GetCell(32)) * 1000;
                            var luongKhac = Utility.GetNumbericCellValue(row.GetCell(33)) * 1000;
                            var thiDua = Utility.GetNumbericCellValue(row.GetCell(34)) * 1000;
                            var hoTroNgoaiLuong = Utility.GetNumbericCellValue(row.GetCell(35)) * 1000;
                            var tongThuNhap = Utility.GetNumbericCellValue(row.GetCell(36)) * 1000;

                            double mauSo = 26;
                            double thunhapbydate = luongCanBanBaoGomPhuCap / mauSo;
                            double thunhapbyminute = thunhapbydate / 8 / 60;

                            double phutconglamviec = ngayConglamViec * 8 * 60;
                            double phutcongCN = congCNGio * 60;
                            double phutcongTangCaNgayThuong = congTangCaNgayThuongGio * 60;
                            double phutcongLeTet = congLeTet * 8 * 60;

                            double tongthunhapminute = thunhapbyminute * (phutconglamviec + (phutcongCN * 2) + (phutcongTangCaNgayThuong * (double)1.5) + (phutcongLeTet * 3))
                                                + luongCB / mauSo * (ngayNghiPhepHuongLuong + ngayNghiLeTetHuongLuong)
                                                + congTacXa + luongTheoDoanhThuDoanhSo + thanhTienBunBoc + luongKhac + thiDua + hoTroNgoaiLuong;

                            var bHXHBHYT = Utility.GetNumbericCellValue(row.GetCell(37)) * 1000;
                            var luongThamGiaBHXH = Utility.GetNumbericCellValue(row.GetCell(38)) * 1000;
                            var tamUng = Utility.GetNumbericCellValue(row.GetCell(39)) * 1000;
                            var thuongLeTet = Utility.GetNumbericCellValue(row.GetCell(40)) * 1000;
                            var thucLanh = Utility.GetNumbericCellValue(row.GetCell(41)) * 1000;
                            if (thucLanh == 0)
                            {
                                thucLanh = 0;
                            }
                            double thuclanhminute = tongthunhapminute - bHXHBHYT - tamUng + thuongLeTet;

                            #region SalaryEmployeeMonth
                            dbContext.SalaryEmployeeMonths.InsertOne(new SalaryEmployeeMonth()
                            {
                                Year = year,
                                Month = month,
                                EmployeeId = employeeId,
                                MaNhanVien = maNV,
                                FullName = fullName,
                                NoiLamViec = location,
                                PhongBan = groupChucDanh,
                                ChucVu = chucVu,
                                ViTriCode = chucVuCode,
                                ThamNienLamViec = joinDate,
                                ThamNienYear = dateSpan.Years,
                                ThamNienMonth = dateSpan.Months,
                                ThamNienDay = dateSpan.Days,
                                Bac = (int)bac,
                                LuongCanBan = (decimal)luongCB,
                                #region Phu Cap
                                NangNhocDocHai = (decimal)nangNhoc,
                                TrachNhiem = (decimal)trachNhiem,
                                ThamNien = (decimal)thamNiem,
                                ThuHut = (decimal)thuHut,
                                #endregion
                                #region PHUC LOI KHAC
                                Xang = (decimal)Xang,
                                DienThoai = (decimal)DienThoai,
                                Com = (decimal)Com,
                                NhaO = 0,
                                KiemNhiem = (decimal)KiemNhiem,
                                BhytDacBiet = (decimal)BHYTDacBiet,
                                ViTriCanKnNhieuNam = (decimal)viTriCanNhieuNamKinhNghiem,
                                ViTriDacThu = (decimal)viTriDacThu,
                                #endregion
                                LuongCoBanBaoGomPhuCap = (decimal)luongCanBanBaoGomPhuCap,
                                NgayCongLamViec = ngayConglamViec,
                                PhutCongLamViec = phutconglamviec,
                                NgayNghiPhepHuongLuong = ngayNghiPhepHuongLuong,
                                NgayNghiLeTetHuongLuong = ngayNghiLeTetHuongLuong,
                                CongCNGio = congCNGio,
                                CongCNPhut = phutcongCN,
                                CongTangCaNgayThuongGio = congTangCaNgayThuongGio,
                                CongTangCaNgayThuongPhut = phutcongTangCaNgayThuong,
                                CongLeTet = congLeTet,
                                CongLeTetPhut = phutcongLeTet,
                                CongTacXa = (decimal)congTacXa,
                                MucDatTrongThang = (decimal)mucDatTrongThang,
                                LuongTheoDoanhThuDoanhSo = (decimal)luongTheoDoanhThuDoanhSo,
                                TongBunBoc = tongBunBoc,
                                ThanhTienBunBoc = (decimal)thanhTienBunBoc,
                                LuongKhac = (decimal)luongKhac,
                                ThiDua = (decimal)thiDua,
                                HoTroNgoaiLuong = (decimal)hoTroNgoaiLuong,
                                ThuNhapByMinute = (decimal)thunhapbyminute,
                                ThuNhapByDate = (decimal)thunhapbydate,
                                TongThuNhap = (decimal)tongThuNhap,
                                TongThuNhapMinute = (decimal)tongthunhapminute,
                                BHXHBHYT = (decimal)bHXHBHYT,
                                LuongThamGiaBHXH = (decimal)luongThamGiaBHXH,
                                TamUng = (decimal)tamUng,
                                ThuongLeTet = (decimal)thuongLeTet,
                                ThucLanh = (decimal)thucLanh,
                                ThucLanhMinute = (decimal)thuclanhminute,
                                MauSo = mauso
                            });
                            #endregion

                            // First Time
                            // Quan ly theo thoi diem,...
                            #region SalaryThangBacLuongEmployee
                            //dbContext.SalaryThangBacLuongEmployees.InsertOne(new SalaryThangBacLuongEmployee()
                            //{
                            //    Year = year,
                            //    Month = month,
                            //    EmployeeId = employeeId,
                            //    ViTriCode = chucVuCode,
                            //    Bac = bac,
                            //    MucLuong = (decimal)luongCB
                            //});
                            #endregion

                            #region SalaryThangBangPhuCapPhucLoiEmployee
                            codePc = "01-001";
                            namePc = "Nặng Nhọc Độc Hại";
                            moneyPc = (decimal)nangNhoc;
                            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
                            {
                                Year = year,
                                Month = month,
                                EmployeeId = employeeId,
                                Code = codePc,
                                Name = namePc,
                                ViTri = chucVuCode,
                                Money = moneyPc
                            });

                            codePc = "01-002";
                            namePc = "Trách nhiệm";
                            moneyPc = (decimal)trachNhiem;
                            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
                            {
                                Year = year,
                                Month = month,
                                EmployeeId = employeeId,
                                Code = codePc,
                                Name = namePc,
                                ViTri = chucVuCode,
                                Money = moneyPc
                            });

                            codePc = "01-004";
                            namePc = "Thu Hút";
                            moneyPc = (decimal)thuHut;
                            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
                            {
                                Year = year,
                                Month = month,
                                EmployeeId = employeeId,
                                Code = codePc,
                                Name = namePc,
                                ViTri = chucVuCode,
                                Money = moneyPc,
                                Law = false
                            });

                            codePc = "02-001";
                            namePc = "Xăng";
                            moneyPc = (decimal)Xang;
                            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
                            {
                                Year = year,
                                Month = month,
                                EmployeeId = employeeId,
                                Code = codePc,
                                Name = namePc,
                                ViTri = chucVuCode,
                                Money = moneyPc,
                                Law = false
                            });

                            codePc = "02-002";
                            namePc = "Điện thoại";
                            moneyPc = (decimal)Xang;
                            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
                            {
                                Year = year,
                                Month = month,
                                EmployeeId = employeeId,
                                Code = codePc,
                                Name = namePc,
                                ViTri = chucVuCode,
                                Money = moneyPc,
                                Law = false
                            });

                            codePc = "02-003";
                            namePc = "Cơm";
                            moneyPc = (decimal)Com;
                            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
                            {
                                Year = year,
                                Month = month,
                                EmployeeId = employeeId,
                                Code = codePc,
                                Name = namePc,
                                ViTri = chucVuCode,
                                Money = moneyPc,
                                Law = false
                            });

                            codePc = "02-004";
                            namePc = "Kiêm nhiệm";
                            moneyPc = (decimal)KiemNhiem;
                            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
                            {
                                Year = year,
                                Month = month,
                                EmployeeId = employeeId,
                                Code = codePc,
                                Name = namePc,
                                ViTri = chucVuCode,
                                Money = moneyPc,
                                Law = false
                            });

                            codePc = "02-005";
                            namePc = "Bhyt Đặc Biệt";
                            moneyPc = (decimal)BHYTDacBiet;
                            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
                            {
                                Year = year,
                                Month = month,
                                EmployeeId = employeeId,
                                Code = codePc,
                                Name = namePc,
                                ViTri = chucVuCode,
                                Money = moneyPc,
                                Law = false
                            });

                            codePc = "02-006";
                            namePc = "Vị Trí Cần Kn Nhiều Năm";
                            moneyPc = (decimal)viTriCanNhieuNamKinhNghiem;
                            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
                            {
                                Year = year,
                                Month = month,
                                EmployeeId = employeeId,
                                Code = codePc,
                                Name = namePc,
                                ViTri = chucVuCode,
                                Money = moneyPc,
                                Law = false
                            });

                            codePc = "02-007";
                            namePc = "Vị Trí Đặc Thù";
                            moneyPc = (decimal)viTriDacThu;
                            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
                            {
                                Year = year,
                                Month = month,
                                EmployeeId = employeeId,
                                Code = codePc,
                                Name = namePc,
                                ViTri = chucVuCode,
                                Money = moneyPc,
                                Law = false
                            });
                            #endregion

                            #region Update Employee Information
                            // Fix ma nv (bang luong vs bang nhan su khac nhau. Update theo bang luong. BASE Email)
                            // Analytics xet chuc danh cong viec by chuc vu
                            // Update new code for vietnam. use LD + 4 so thu tu
                            if (!string.IsNullOrEmpty(employeeId))
                            {
                                // debug
                                if (employeeId == "5b6bb22fe73a301f941c5884")
                                {
                                    //var a = "aa";
                                }
                                var chucdanhCode = string.Empty;
                                var chucVuAlias = Utility.AliasConvert(chucVu);
                                foreach (string item in c02)
                                {
                                    if (chucVuAlias.Contains(Utility.AliasConvert(item)))
                                    {
                                        chucdanhCode = "C.02";
                                    }
                                }
                                foreach (string item in c03)
                                {
                                    if (chucVuAlias.Contains(Utility.AliasConvert(item)))
                                    {
                                        chucdanhCode = "C.03";
                                    }
                                }
                                foreach (string item in d01)
                                {
                                    if (chucVuAlias.Contains(Utility.AliasConvert(item)))
                                    {
                                        chucdanhCode = "D.01";
                                    }
                                }
                                foreach (string item in d02)
                                {
                                    if (chucVuAlias.Contains(Utility.AliasConvert(item)))
                                    {
                                        chucdanhCode = "D.02";
                                    }
                                }
                                foreach (string item in d03)
                                {
                                    if (chucVuAlias.Contains(Utility.AliasConvert(item)))
                                    {
                                        chucdanhCode = "D.03";
                                    }
                                }
                                foreach (string item in b01)
                                {
                                    if (chucVuAlias.Contains(Utility.AliasConvert(item)))
                                    {
                                        chucdanhCode = "B.01";
                                    }
                                }
                                foreach (string item in b02)
                                {
                                    if (chucVuAlias.Contains(Utility.AliasConvert(item)))
                                    {
                                        chucdanhCode = "B.03";
                                    }
                                }
                                foreach (string item in b03)
                                {
                                    if (chucVuAlias.Contains(Utility.AliasConvert(item)))
                                    {
                                        chucdanhCode = "B.03";
                                    }
                                }
                                foreach (string item in b04)
                                {
                                    if (chucVuAlias.Contains(Utility.AliasConvert(item)))
                                    {
                                        chucdanhCode = "B.04";
                                    }
                                }
                                var builder = Builders<Employee>.Filter;
                                var filter = builder.Eq(m => m.Id, employeeId);
                                var update = Builders<Employee>.Update
                                    .Set(m => m.CodeOld, maNV)
                                    .Set(m => m.SalaryChucVu, chucVu)
                                    .Set(m => m.SalaryChucVuViTriCode, chucVuCode)
                                    .Set(m => m.NgachLuong, chucdanhCode)
                                    .Set(m => m.Joinday, joinDate)
                                    .Set(m => m.SalaryMauSo, mauso)
                                    .Set(m => m.SalaryNoiLamViecOrder, locationOrder)
                                    .Set(m => m.SalaryPhongBanOrder, groupChucDanhOrder)
                                    .Set(m => m.UpdatedOn, DateTime.Now);
                                dbContext.Employees.UpdateOne(filter, update);
                            }
                            #endregion
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(locationTemp))
                            {
                                location = locationTemp;
                                locationOrder++;
                            }
                            if (!string.IsNullOrEmpty(groupChucDanhTemp))
                            {
                                groupChucDanh = groupChucDanhTemp;
                                groupChucDanhOrder++;
                            }
                        }
                    }
                    // Update TGD, CT
                    var listTGD = "LĐ01,LĐ02".Split(',');
                    foreach (var item in listTGD)
                    {
                        var builder = Builders<Employee>.Filter;
                        var filter = builder.Eq(m => m.CodeOld, item);
                        var update = Builders<Employee>.Update
                            .Set(m => m.NgachLuong, "C.01")
                            .Set(m => m.UpdatedOn, DateTime.Now);
                        dbContext.Employees.UpdateOne(filter, update);
                    }
                    // Update mauso 30, co 3 nguoi
                    var list30 = "Lê Hoàng Tuấn,Nguyễn Hùng Dũng,Danh Thủy".Split(',');
                    foreach (var item in list30)
                    {
                        var builder = Builders<Employee>.Filter;
                        var filter = builder.Eq(m => m.FullName, item);
                        var update = Builders<Employee>.Update
                            .Set(m => m.SalaryMauSo, 30)
                            .Set(m => m.UpdatedOn, DateTime.Now);
                        dbContext.Employees.UpdateOne(filter, update);
                    }
                    #endregion
                }
            }
            return Json(new { url = "/" });
        }

        [Route(Constants.LinkSalary.NhanVienKhoiNhaMay + "/" + Constants.LinkSalary.Document)]
        public IActionResult NhanVienKhoiNhaMay()
        {
            return View();
        }

        [Route(Constants.LinkSalary.NhanVienKhoiNhaMay + "/" + Constants.LinkSalary.Document + "/" + Constants.LinkSalary.Update)]
        [HttpPost]
        public ActionResult NhanVienKhoiNhaMayImport()
        {
            // Cập nhật khối tính lương, ngạch lương, hệ số lương [Employees]
            // Cause file luong error format. break into multi file.
            var date = DateTime.Now;
            IFormFile file = Request.Form.Files[0];
            string folderName = Constants.Storage.Factories;
            string webRootPath = _env.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            if (file.Length > 0)
            {
                string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                string fullPath = Path.Combine(newPath, file.FileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Position = 0;
                    int headerCal = 0;
                    ISheet sheet0;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet0 = hssfwb.GetSheetAt(0);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet0 = hssfwb.GetSheetAt(0);
                    }

                    #region Sheet 0
                    headerCal = 7;
                    int month = 7;
                    int year = 2018;
                    for (int i = headerCal; i <= sheet0.LastRowNum; i++)
                    {
                        IRow row = sheet0.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var fullName = Utility.GetFormattedCellValue(row.GetCell(2)).Trim();
                        var title = Utility.GetFormattedCellValue(row.GetCell(3)).Trim();
                        // check TITLE.NO DATA
                        if (string.IsNullOrEmpty(title))
                        {
                            continue;
                        }

                        var ngachLuong = Utility.GetFormattedCellValue(row.GetCell(8)).Trim();
                        var hesoLuong = Utility.GetNumbericCellValue(row.GetCell(9));
                        var luong = Utility.GetNumbericCellValue(row.GetCell(11));
                        var truTamUng = Utility.GetNumbericCellValue(row.GetCell(27));
                        var luongBHXH = Utility.GetNumbericCellValue(row.GetCell(29));

                        // base fullname because code wrong
                        if (!string.IsNullOrEmpty(fullName))
                        {
                            var employee = dbContext.Employees.Find(m => m.FullName.Equals(fullName)).FirstOrDefault();
                            if (employee != null)
                            {
                                // proccess
                                var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                                var update = Builders<Employee>.Update
                                    .Set(m => m.SalaryType, (int)EKhoiLamViec.NM)
                                    .Set(m => m.NgachLuong, ngachLuong)
                                    .Set(m => m.SalaryLevel, hesoLuong)
                                    .Set(m => m.Salary, (decimal)luong)
                                    .Set(m => m.Credit, (decimal)truTamUng)
                                    .Set(m => m.LuongBHXH, (decimal)luongBHXH);
                                dbContext.Employees.UpdateOne(filter, update);

                                // CreditEmployees thang 7.
                                // check exist to update
                                var existEntity = dbContext.CreditEmployees.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                                if (existEntity != null)
                                {
                                    var builderC = Builders<CreditEmployee>.Filter;
                                    var filterC = builderC.Eq(m => m.Id, existEntity.Id);
                                    var updateC = Builders<CreditEmployee>.Update
                                        .Set(m => m.EmployeeCode, employee.Code)
                                        .Set(m => m.FullName, employee.FullName)
                                        .Set(m => m.EmployeeTitle, employee.Title)
                                        .Set(m => m.EmployeeDepartment, employee.DepartmentId)
                                        .Set(m => m.EmployeePart, employee.PartId)
                                        .Set(m => m.Money, (decimal)truTamUng)
                                        .Set(m => m.UpdatedOn, DateTime.Now);

                                    dbContext.CreditEmployees.UpdateOne(filterC, updateC);
                                }
                                else
                                {
                                    var newItem = new CreditEmployee
                                    {
                                        Year = year,
                                        Month = month,
                                        EmployeeId = employee.Id,
                                        EmployeeCode = employee.Code,
                                        FullName = employee.FullName,
                                        EmployeeTitle = employee.Title,
                                        EmployeeDepartment = employee.DepartmentId,
                                        EmployeePart = employee.PartId,
                                        Type = (int)ECredit.UngLuong,
                                        Money = (decimal)truTamUng
                                    };
                                    dbContext.CreditEmployees.InsertOne(newItem);
                                }
                            }
                            else
                            {
                                // Update miss data
                                dbContext.Misss.InsertOne(new Miss
                                {
                                    Type = "no-data-employee",
                                    Object = fullName,
                                    Error = "No get data",
                                    DateTime = date.ToString("dd/MM/yyyy HH:mm:ss")
                                });
                            }
                        }
                    }
                    #endregion
                }
            }
            return Json(new { url = "/" });
        }

        [Route(Constants.LinkSalary.NhanVienKhoiSanXuat + "/" + Constants.LinkSalary.Document)]
        public IActionResult NhanVienKhoiSanXuat()
        {
            return View();
        }

        [Route(Constants.LinkSalary.NhanVienKhoiSanXuat + "/" + Constants.LinkSalary.Document + "/" + Constants.LinkSalary.Update)]
        [HttpPost]
        public ActionResult NhanVienKhoiSanXuatImport()
        {
            // Cập nhật khối tính lương, ngạch lương, hệ số lương [Employees]
            // Cause file luong error format. break into multi file.
            var date = DateTime.Now;
            IFormFile file = Request.Form.Files[0];
            string folderName = Constants.Storage.Factories;
            string webRootPath = _env.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            if (file.Length > 0)
            {
                string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                string fullPath = Path.Combine(newPath, file.FileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Position = 0;
                    int headerCal = 0;
                    ISheet sheet0;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet0 = hssfwb.GetSheetAt(0);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet0 = hssfwb.GetSheetAt(0);
                    }

                    #region Sheet 0
                    headerCal = 12;
                    for (int i = headerCal; i <= sheet0.LastRowNum; i++)
                    {
                        IRow row = sheet0.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var fullName = Utility.GetFormattedCellValue(row.GetCell(1)).Trim();
                        var title = Utility.GetFormattedCellValue(row.GetCell(2)).Trim();
                        // check TITLE.NO DATA
                        if (string.IsNullOrEmpty(title))
                        {
                            continue;
                        }
                        var ngachLuong = Utility.GetFormattedCellValue(row.GetCell(6)).Trim();
                        var hesoLuong = Utility.GetNumbericCellValue(row.GetCell(7));

                        // base fullname because code wrong
                        if (!string.IsNullOrEmpty(fullName))
                        {
                            var employee = dbContext.Employees.Find(m => m.FullName.Equals(fullName)).FirstOrDefault();
                            if (employee != null)
                            {
                                // proccess
                                var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                                var update = Builders<Employee>.Update
                                    .Set(m => m.SalaryType, (int)EKhoiLamViec.SX)
                                    .Set(m => m.NgachLuong, ngachLuong)
                                    .Set(m => m.SalaryLevel, hesoLuong);
                                dbContext.Employees.UpdateOne(filter, update);
                            }
                            else
                            {
                                // Update miss data
                                dbContext.Misss.InsertOne(new Miss
                                {
                                    Type = "no-data-employee",
                                    Object = fullName,
                                    Error = "No get data",
                                    DateTime = date.ToString("dd/MM/yyyy HH:mm:ss")
                                });
                            }
                        }
                    }
                    #endregion
                }
            }
            return Json(new { url = "/" });
        }

        [Route(Constants.LinkSalary.ThangLuong + "/" + Constants.LinkSalary.Document)]
        public IActionResult ThangLuongTaiLieu()
        {
            return View();
        }

        [Route(Constants.LinkSalary.ThangLuong + "/" + Constants.LinkSalary.Document + "/" + Constants.ActionLink.Update)]
        [HttpPost]
        public ActionResult ThangLuongImport()
        {
            InitLuongToiThieuVung();
            // Cause file luong error format. break into multi file.
            var date = DateTime.Now;
            IFormFile file = Request.Form.Files[0];
            string folderName = Constants.Storage.Factories;
            string webRootPath = _env.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            if (file.Length > 0)
            {
                string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                string fullPath = Path.Combine(newPath, file.FileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Position = 0;
                    int headerCal = 0;
                    ISheet sheet0;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet0 = hssfwb.GetSheetAt(0);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet0 = hssfwb.GetSheetAt(0);
                    }

                    #region Sheet 0: Thang Bang Luong
                    dbContext.SalaryThangBangLuongs.DeleteMany(m => m.Law.Equals(false));
                    // Get min salary
                    var salaryMucLuongVung = dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefault();
                    decimal salaryMin = 0;
                    if (salaryMucLuongVung != null)
                    {
                        salaryMin = salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung;
                    }
                    headerCal = 4;
                    int viTriCode = 1;
                    for (int i = headerCal; i <= sheet0.LastRowNum; i++)
                    {
                        IRow row = sheet0.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var vitri = Utility.GetFormattedCellValue(row.GetCell(1)).Trim();
                        if (!string.IsNullOrEmpty(vitri))
                        {
                            var vitriFullCode = Constants.System.viTriCodeTBLuong + viTriCode.ToString("000");
                            var hesobac = (decimal)Utility.GetNumbericCellValue(row.GetCell(13));
                            // Min default each VITRI
                            var money = (decimal)Utility.GetNumbericCellValue(row.GetCell(14));
                            if (money == 0)
                            {
                                money = salaryMin;
                            }
                            else
                            {
                                money = money * 1000;
                            }
                            var vitriAlias = Utility.AliasConvert(vitri);

                            var exist = dbContext.SalaryThangBangLuongs.CountDocuments(m => m.ViTriAlias.Equals(vitriAlias) & m.Law.Equals(false));
                            if (exist == 0)
                            {
                                for (int lv = 1; lv <= 10; lv++)
                                {
                                    if (lv > 1)
                                    {
                                        money = hesobac * money;
                                    }
                                    dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                                    {
                                        Month = 8,
                                        Year = 2018,
                                        ViTri = vitri,
                                        Bac = lv,
                                        HeSo = hesobac,
                                        MucLuong = Math.Round(money, 0),
                                        ViTriCode = vitriFullCode,
                                        ViTriAlias = vitriAlias,
                                        Law = false
                                    });
                                }
                                viTriCode++;
                            }
                        }
                    }
                    #endregion
                }
            }
            return Json(new { url = "/" });
        }
        #endregion
    }
}
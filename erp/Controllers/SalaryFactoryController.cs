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
    [Route(Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Factory)]
    public class SalaryFactoryController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalaryFactoryController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalaryFactoryController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _logger = logger;
        }

        [Route(Constants.LinkSalary.BangLuong)]
        public async Task<IActionResult> BangLuong(string Thang, string Id, string KhoiChucNang, string PhongBan, string BoPhan)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongNM, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL
            var congtychinhanhs = dbContext.CongTyChiNhanhs.Find(m => m.Enable.Equals(true)).ToList();
            var ctcnvp = congtychinhanhs.First(x => x.Code.Equals("CT1"));
            var ctcnnm = congtychinhanhs.First(x => x.Code.Equals("CT2"));

            var sortTimes = Utility.DllMonths();
            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true) && m.CongTyChiNhanh.Equals(ctcnnm.Id) 
                        && !m.ChucVu.Equals("5c88d09bd59d56225c4324de") && !m.UserName.Equals(Constants.System.account))
                        .SortBy(m => m.FullName).ToListAsync();
            #endregion

            #region Times
            var toDate = Utility.GetSalaryToDate(Thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            Thang = string.IsNullOrEmpty(Thang) ? month + "-" + year : Thang;
            #endregion

            // MOI THANG SE CO 1 DANH SACH TAI THOI DIEM DO
            // TRANH NGUOI MOI CO TRONG BANG LUONG CU
            Utility.AutoInitSalary((int)ESalaryType.NM, month, year);

            #region Filter
            var builder = Builders<SalaryEmployeeMonth>.Filter;
            var filter = builder.Eq(m => m.Year, year) & builder.Eq(m => m.Month, month)
                        & builder.Eq(m => m.CongTyChiNhanhId, ctcnnm.Id)
                        & !builder.Eq(m => m.ChucVuId, "5c88d09bd59d56225c4324de");
            if (!string.IsNullOrEmpty(KhoiChucNang))
            {
                filter = filter & builder.Eq(x => x.KhoiChucNangId, KhoiChucNang);
            }
            if (!string.IsNullOrEmpty(PhongBan))
            {
                filter = filter & builder.Eq(x => x.PhongBanId, PhongBan);
            }
            if (!string.IsNullOrEmpty(BoPhan))
            {
                filter = filter & builder.Eq(x => x.BoPhanId, BoPhan);
            }
            if (!string.IsNullOrEmpty(Id))
            {
                filter = filter & builder.Eq(x => x.Id, Id.Trim());
            }
            #endregion

            var salaryEmployeeMonths = dbContext.SalaryEmployeeMonths.Find(filter).ToList();

            #region FILL DATA
            var results = new List<SalaryEmployeeMonth>();
            foreach (var salary in salaryEmployeeMonths)
            {
                var salaryFull = Utility.SalaryEmployeeMonthFillData(salary);
                results.Add(salaryFull);
            }
            #endregion

            #region SORT: do later

            #endregion

            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = salaryEmployeeMonths,
                MonthYears = sortTimes,
                Thang = Thang,
                Employees = employees,
                Id = Id,
                KhoiChucNang = KhoiChucNang,
                PhongBan = PhongBan,
                BoPhan = BoPhan
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.BangLuong + "/" + Constants.ActionLink.Update)]
        public async Task<IActionResult> BangLuongUpdate(BangLuongViewModel viewModel)
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

            // For security calculator again => update
            // For demo: do later
            try
            {
                #region Times
                var now = DateTime.Now;
                var thang = viewModel.Thang;
                var toDate = Utility.WorkingMonthToDate(thang);
                var fromDate = toDate.AddMonths(-1).AddDays(1);
                if (string.IsNullOrEmpty(thang))
                {
                    toDate = DateTime.Now;
                    fromDate = toDate.Day > 25 ? new DateTime(toDate.Year, toDate.Month, 26) : new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
                }
                var year = toDate.Day > 25 ? toDate.AddMonths(1).Year : toDate.Year;
                var month = toDate.Day > 25 ? toDate.AddMonths(1).Month : toDate.Month;
                thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;
                #endregion

                var models = viewModel.SalaryEmployeeMonths;
                foreach (var item in models)
                {
                    var builder = Builders<SalaryEmployeeMonth>.Filter;
                    var filter = builder.Eq(m => m.Id, item.Id);
                    var update = Builders<SalaryEmployeeMonth>.Update
                        .Set(m => m.NgachLuongLevel, item.NgachLuongLevel)
                        .Set(m => m.PhuCapKhac, item.PhuCapKhac)
                        .Set(m => m.ThuongLeTet, item.ThuongLeTet)
                        .Set(m => m.LuongThamGiaBHXH, item.LuongThamGiaBHXH)
                        .Set(m => m.HoTroNgoaiLuong, item.HoTroNgoaiLuong)
                        .Set(m => m.UpdatedOn, now);
                    dbContext.SalaryEmployeeMonths.UpdateOne(filter, update);
                }
                return Json(new { result = true, source = "update", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "update", id = string.Empty, message = ex.Message });
            }
        }

        #region THE LUONG
        [Route(Constants.LinkSalary.TheLuong)]
        public IActionResult TheLuong(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }
        #endregion

        #region CONG TONG, UPDATE ONLY. INSERT IN FILE IMPORT
        [Route(Constants.LinkSalary.CongTong)]
        public async Task<IActionResult> CongTong(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongNM, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            #endregion

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 02, 01);
            var endDate = DateTime.Now;
            while (date.Year < endDate.Year || (date.Year == endDate.Year && date.Month <= endDate.Month))
            {
                monthYears.Add(new MonthYear
                {
                    Month = date.Month,
                    Year = date.Year
                });
                date = date.AddMonths(1);
            }
            if (endDate.Day > 25)
            {
                monthYears.Add(new MonthYear
                {
                    Month = endDate.AddMonths(1).Month,
                    Year = endDate.AddMonths(1).Year
                });
            }
            var sortTimes = monthYears.OrderByDescending(x => x.Month).OrderByDescending(x => x.Year).ToList();
            #endregion

            #region Times
            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = toDate.Day > 25 ? new DateTime(toDate.Year, toDate.Month, 26) : new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Day > 25 ? toDate.AddMonths(1).Year : toDate.Year;
            var month = toDate.Day > 25 ? toDate.AddMonths(1).Month : toDate.Month;
            thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;
            #endregion


            var salaryNhaMayCongs = GetCongs(month, year);

            var viewModel = new BangLuongViewModel
            {
                Congs = salaryNhaMayCongs,
                MonthYears = sortTimes,
                Thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.CongTong + "/" + Constants.ActionLink.Update)]
        public async Task<IActionResult> CongTongUpdate(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongNM, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            #endregion

            #region DDL
            var monthYears = new List<MonthYear>();
            var date = new DateTime(2018, 02, 01);
            var endDate = DateTime.Now;
            while (date.Year < endDate.Year || (date.Year == endDate.Year && date.Month <= endDate.Month))
            {
                monthYears.Add(new MonthYear
                {
                    Month = date.Month,
                    Year = date.Year
                });
                date = date.AddMonths(1);
            }
            if (endDate.Day > 25)
            {
                monthYears.Add(new MonthYear
                {
                    Month = endDate.AddMonths(1).Month,
                    Year = endDate.AddMonths(1).Year
                });
            }
            var sortTimes = monthYears.OrderByDescending(x => x.Month).OrderByDescending(x => x.Year).ToList();
            #endregion

            #region Times
            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = toDate.Day > 25 ? new DateTime(toDate.Year, toDate.Month, 26) : new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Day > 25 ? toDate.AddMonths(1).Year : toDate.Year;
            var month = toDate.Day > 25 ? toDate.AddMonths(1).Month : toDate.Month;
            thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;
            #endregion


            var salaryNhaMayCongs = GetCongs(month, year);

            var viewModel = new BangLuongViewModel
            {
                Congs = salaryNhaMayCongs,
                MonthYears = sortTimes,
                Thang = thang
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.CongTong + "/" + Constants.ActionLink.Update)]
        public async Task<IActionResult> CongTongUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.Congs)
            {
                var builder = Builders<EmployeeCong>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<EmployeeCong>.Update
                    .Set(m => m.CongTong, item.CongTong)
                    .Set(m => m.ComSX, item.ComSX)
                    .Set(m => m.ComKD, item.ComKD)
                    .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                dbContext.EmployeeCongs.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        private List<EmployeeCong> GetCongs(int month, int year)
        {
            var results = dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).ToList();
            return results;
        }
        #endregion

        #region TAMUNG | THUONG, BHXH, DIEU CHINH NGAY CONG
        [Route(Constants.LinkSalary.SanXuatTamUngTemplate)]
        public async Task<IActionResult> SanXuatTamUngTemplate(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.BangChamCong, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var toDate = Utility.GetToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            var thangFormat = month.ToString("00") + "-" + year.ToString("0000");
            #endregion

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"" + thangFormat + "-tam-ung";
            sFileName += "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var font = workbook.CreateFont();
                font.FontHeightInPoints = 8;
                font.FontName = "Arial";

                var fontSmall = workbook.CreateFont();
                fontSmall.FontHeightInPoints = 5;
                fontSmall.FontName = "Arial";

                var fontbold = workbook.CreateFont();
                fontbold.FontHeightInPoints = 8;
                fontbold.FontName = "Arial";
                fontbold.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold8 = workbook.CreateFont();
                fontbold8.FontHeightInPoints = 8;
                fontbold8.FontName = "Arial";
                fontbold8.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10 = workbook.CreateFont();
                fontbold10.FontHeightInPoints = 10;
                fontbold10.FontName = "Arial";
                fontbold10.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11 = workbook.CreateFont();
                fontbold11.FontHeightInPoints = 11;
                fontbold11.FontName = "Arial";
                fontbold11.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold12 = workbook.CreateFont();
                fontbold12.FontHeightInPoints = 12;
                fontbold12.FontName = "Arial";
                fontbold12.Boldweight = (short)FontBoldWeight.Bold;

                var styleBorder = workbook.CreateCellStyle();
                styleBorder.BorderBottom = BorderStyle.Thin;
                styleBorder.BorderLeft = BorderStyle.Thin;
                styleBorder.BorderRight = BorderStyle.Thin;
                styleBorder.BorderTop = BorderStyle.Thin;

                var styleBorderDot = workbook.CreateCellStyle();
                styleBorderDot.BorderBottom = BorderStyle.Dotted;
                styleBorderDot.BorderLeft = BorderStyle.Thin;
                styleBorderDot.BorderRight = BorderStyle.Thin;
                styleBorderDot.BorderTop = BorderStyle.Thin;

                var styleCenter = workbook.CreateCellStyle();
                styleCenter.Alignment = HorizontalAlignment.Center;
                styleCenter.VerticalAlignment = VerticalAlignment.Center;

                var styleCenterBorder = workbook.CreateCellStyle();
                styleCenterBorder.CloneStyleFrom(styleBorder);
                styleCenterBorder.Alignment = HorizontalAlignment.Center;
                styleCenterBorder.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleBorderAndColorLightGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellStyleBorderAndColorLightGreen.WrapText = true;
                cellStyleBorderAndColorLightGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorLightGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 146, 208, 80 }));
                cellStyleBorderAndColorLightGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorGreen.SetFont(fontbold11);
                cellStyleBorderAndColorGreen.WrapText = true;
                cellStyleBorderAndColorGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 176, 80 }));
                cellStyleBorderAndColorGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorBlue.SetFont(fontbold11);
                cellStyleBorderAndColorBlue.WrapText = true;
                cellStyleBorderAndColorBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 112, 192 }));
                cellStyleBorderAndColorBlue.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorDarkBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorDarkBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorDarkBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 32, 96 }));

                var cellM3 = workbook.CreateCellStyle();
                cellM3.CloneStyleFrom(styleBorder);
                cellM3.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellM3.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellM3).SetFillForegroundColor(new XSSFColor(new byte[] { 248, 203, 173 }));
                cellM3.BorderBottom = BorderStyle.Thin;
                cellM3.BottomBorderColor = IndexedColors.Black.Index;
                cellM3.BorderTop = BorderStyle.Thin;
                cellM3.TopBorderColor = IndexedColors.Black.Index;
                cellM3.BorderLeft = BorderStyle.Thin;
                cellM3.LeftBorderColor = IndexedColors.Black.Index;
                cellM3.BorderRight = BorderStyle.Thin;
                cellM3.RightBorderColor = IndexedColors.Black.Index;

                var styleDedault = workbook.CreateCellStyle();
                styleDedault.CloneStyleFrom(styleBorder);
                styleDedault.SetFont(font);

                var styleDot = workbook.CreateCellStyle();
                styleDot.CloneStyleFrom(styleBorderDot);
                styleDot.SetFont(font);

                var styleTitle = workbook.CreateCellStyle();
                styleTitle.CloneStyleFrom(styleCenter);
                styleTitle.SetFont(fontbold12);

                var styleSubTitle = workbook.CreateCellStyle();
                styleSubTitle.CloneStyleFrom(styleCenter);
                styleSubTitle.SetFont(fontbold8);

                var styleHeader = workbook.CreateCellStyle();
                styleHeader.CloneStyleFrom(styleCenterBorder);
                styleHeader.SetFont(fontbold8);

                var styleDedaultMerge = workbook.CreateCellStyle();
                styleDedaultMerge.CloneStyleFrom(styleCenter);
                styleDedaultMerge.SetFont(font);

                var styleBold = workbook.CreateCellStyle();
                styleBold.SetFont(fontbold8);

                var styleSmall = workbook.CreateCellStyle();
                styleSmall.CloneStyleFrom(styleBorder);
                styleSmall.SetFont(fontSmall);

                var cellStyleHeader = workbook.CreateCellStyle();
                cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                cellStyleHeader.FillPattern = FillPattern.SolidForeground;

                var fontbold18TimesNewRoman = workbook.CreateFont();
                fontbold18TimesNewRoman.FontHeightInPoints = 18;
                fontbold18TimesNewRoman.FontName = "Times New Roman";
                fontbold18TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11TimesNewRoman = workbook.CreateFont();
                fontbold11TimesNewRoman.FontHeightInPoints = 11;
                fontbold11TimesNewRoman.FontName = "Times New Roman";
                fontbold11TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10TimesNewRoman = workbook.CreateFont();
                fontbold10TimesNewRoman.FontHeightInPoints = 10;
                fontbold10TimesNewRoman.FontName = "Times New Roman";
                fontbold10TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold9TimesNewRoman = workbook.CreateFont();
                fontbold9TimesNewRoman.FontHeightInPoints = 9;
                fontbold9TimesNewRoman.FontName = "Times New Roman";
                fontbold9TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var styleRow0 = workbook.CreateCellStyle();
                styleRow0.SetFont(fontbold18TimesNewRoman);
                styleRow0.Alignment = HorizontalAlignment.Center;
                styleRow0.VerticalAlignment = VerticalAlignment.Center;
                styleRow0.BorderBottom = BorderStyle.Thin;
                styleRow0.BorderTop = BorderStyle.Thin;
                styleRow0.BorderLeft = BorderStyle.Thin;
                styleRow0.BorderRight = BorderStyle.Thin;

                var styleBorderBold11Background = workbook.CreateCellStyle();
                styleBorderBold11Background.SetFont(fontbold11TimesNewRoman);
                styleBorderBold11Background.Alignment = HorizontalAlignment.Center;
                styleBorderBold11Background.VerticalAlignment = VerticalAlignment.Center;
                styleBorderBold11Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold11Background.BorderTop = BorderStyle.Thin;
                styleBorderBold11Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold11Background.BorderRight = BorderStyle.Thin;
                styleBorderBold11Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold11Background.FillPattern = FillPattern.SolidForeground;

                var styleBorderBold10Background = workbook.CreateCellStyle();
                styleBorderBold10Background.SetFont(fontbold10TimesNewRoman);
                styleBorderBold10Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold10Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold10Background.BorderRight = BorderStyle.Thin;
                styleBorderBold10Background.BorderTop = BorderStyle.Thin;
                styleBorderBold10Background.Alignment = HorizontalAlignment.Center;
                styleBorderBold10Background.VerticalAlignment = VerticalAlignment.Center;
                styleBorderBold10Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold10Background.FillPattern = FillPattern.SolidForeground;

                var styleBorderBold9Background = workbook.CreateCellStyle();
                styleBorderBold9Background.SetFont(fontbold9TimesNewRoman);
                styleBorderBold9Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold9Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold9Background.BorderRight = BorderStyle.Thin;
                styleBorderBold9Background.BorderTop = BorderStyle.Thin;
                styleBorderBold9Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold9Background.FillPattern = FillPattern.SolidForeground;
                #endregion

                ISheet sheet1 = workbook.CreateSheet(month.ToString("00") + year.ToString("0000") + "-TAM UNG");

                #region Title
                var rowIndex = 0;
                var columnIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                var cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Công ty TNHH CNSH SÀI GÒN XANH");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Địa chỉ: 127 Nguyễn Trọng Tuyển - P.15 - Q.Phú Nhuận - Tp HCM");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Điện thoại: (08)-39971869 - 38442457 - Fax: 08-39971869");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MST: 0302519810");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;
                #endregion

                #region Thang
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Thời gian:");
                cell.CellStyle = styleTitle;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(thangFormat);
                cell.CellStyle = styleTitle;
                rowIndex++;
                #endregion

                row = sheet1.CreateRow(rowIndex);
                row.Height = 1000;
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Tên NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ứng lương");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;
                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.SanXuatTamUngPost)]
        [HttpPost]
        public ActionResult SanXuatTamUngPost()
        {
            IFormFile file = Request.Form.Files[0];
            string folderName = Constants.Storage.Hr;
            string webRootPath = _env.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            if (file.Length > 0)
            {
                string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                ISheet sheet;
                string fullPath = Path.Combine(newPath, file.FileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Position = 0;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet = hssfwb.GetSheetAt(0);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
                    }

                    var timeRow = sheet.GetRow(5);
                    var time = Utility.GetFormattedCellValue(timeRow.GetCell(1));
                    var endMonthDate = Utility.GetToDate(time);
                    var month = endMonthDate.Month;
                    var year = endMonthDate.Year;
                    var toDate = new DateTime(year, month, 25);
                    var fromDate = toDate.AddMonths(-1).AddDays(1);
                    var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);


                    for (int i = 7; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var ma = string.Empty;
                        var ten = string.Empty;
                        var chucvu = string.Empty;
                        var ngayvaolam = string.Empty;
                        decimal tamung = 0;

                        int columnIndex = 0;
                        ma = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        ten = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tamung = (decimal)Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;

                        if (string.IsNullOrEmpty(ten))
                        {
                            continue;
                        }
                        var alias = Utility.AliasConvert(ten);
                        var existEmployee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                        if (existEmployee != null)
                        {
                            TamUngSanXuat(month, year, tamung, existEmployee);
                        }
                        else
                        {
                            InsertNewEmployee(ten, ma, chucvu, ngayvaolam);
                            var employee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                            TamUngSanXuat(month, year, tamung, employee);
                        }
                    }
                }
            }
            return Json(new { result = true });
        }

        [Route(Constants.LinkSalary.SanXuatNgayCongThuongBHXHTemplate)]
        public async Task<IActionResult> SanXuatNgayCongThuongBHXHTemplate(string thang)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.BangChamCong, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var toDate = Utility.GetToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            var thangFormat = month.ToString("00") + "-" + year.ToString("0000");
            #endregion

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"" + thangFormat + "-ngay-cong-thuong-bhxh";
            sFileName += "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var font = workbook.CreateFont();
                font.FontHeightInPoints = 8;
                font.FontName = "Arial";

                var fontSmall = workbook.CreateFont();
                fontSmall.FontHeightInPoints = 5;
                fontSmall.FontName = "Arial";

                var fontbold = workbook.CreateFont();
                fontbold.FontHeightInPoints = 8;
                fontbold.FontName = "Arial";
                fontbold.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold8 = workbook.CreateFont();
                fontbold8.FontHeightInPoints = 8;
                fontbold8.FontName = "Arial";
                fontbold8.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10 = workbook.CreateFont();
                fontbold10.FontHeightInPoints = 10;
                fontbold10.FontName = "Arial";
                fontbold10.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11 = workbook.CreateFont();
                fontbold11.FontHeightInPoints = 11;
                fontbold11.FontName = "Arial";
                fontbold11.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold12 = workbook.CreateFont();
                fontbold12.FontHeightInPoints = 12;
                fontbold12.FontName = "Arial";
                fontbold12.Boldweight = (short)FontBoldWeight.Bold;

                var styleBorder = workbook.CreateCellStyle();
                styleBorder.BorderBottom = BorderStyle.Thin;
                styleBorder.BorderLeft = BorderStyle.Thin;
                styleBorder.BorderRight = BorderStyle.Thin;
                styleBorder.BorderTop = BorderStyle.Thin;

                var styleBorderDot = workbook.CreateCellStyle();
                styleBorderDot.BorderBottom = BorderStyle.Dotted;
                styleBorderDot.BorderLeft = BorderStyle.Thin;
                styleBorderDot.BorderRight = BorderStyle.Thin;
                styleBorderDot.BorderTop = BorderStyle.Thin;

                var styleCenter = workbook.CreateCellStyle();
                styleCenter.Alignment = HorizontalAlignment.Center;
                styleCenter.VerticalAlignment = VerticalAlignment.Center;

                var styleCenterBorder = workbook.CreateCellStyle();
                styleCenterBorder.CloneStyleFrom(styleBorder);
                styleCenterBorder.Alignment = HorizontalAlignment.Center;
                styleCenterBorder.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleBorderAndColorLightGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellStyleBorderAndColorLightGreen.WrapText = true;
                cellStyleBorderAndColorLightGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorLightGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 146, 208, 80 }));
                cellStyleBorderAndColorLightGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorLightGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorLightGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorGreen = workbook.CreateCellStyle();
                //cellStyleBorderAndColorGreen.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorGreen.SetFont(fontbold11);
                cellStyleBorderAndColorGreen.WrapText = true;
                cellStyleBorderAndColorGreen.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorGreen).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 176, 80 }));
                cellStyleBorderAndColorGreen.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorGreen.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorGreen.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorBlue.SetFont(fontbold11);
                cellStyleBorderAndColorBlue.WrapText = true;
                cellStyleBorderAndColorBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 112, 192 }));
                cellStyleBorderAndColorBlue.BorderBottom = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.BottomBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderTop = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.TopBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderLeft = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.LeftBorderColor = IndexedColors.Black.Index;
                cellStyleBorderAndColorBlue.BorderRight = BorderStyle.Thin;
                cellStyleBorderAndColorBlue.RightBorderColor = IndexedColors.Black.Index;

                var cellStyleBorderAndColorDarkBlue = workbook.CreateCellStyle();
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleBorder);
                cellStyleBorderAndColorDarkBlue.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorDarkBlue.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellStyleBorderAndColorDarkBlue).SetFillForegroundColor(new XSSFColor(new byte[] { 0, 32, 96 }));

                var cellM3 = workbook.CreateCellStyle();
                cellM3.CloneStyleFrom(styleBorder);
                cellM3.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                cellM3.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)cellM3).SetFillForegroundColor(new XSSFColor(new byte[] { 248, 203, 173 }));
                cellM3.BorderBottom = BorderStyle.Thin;
                cellM3.BottomBorderColor = IndexedColors.Black.Index;
                cellM3.BorderTop = BorderStyle.Thin;
                cellM3.TopBorderColor = IndexedColors.Black.Index;
                cellM3.BorderLeft = BorderStyle.Thin;
                cellM3.LeftBorderColor = IndexedColors.Black.Index;
                cellM3.BorderRight = BorderStyle.Thin;
                cellM3.RightBorderColor = IndexedColors.Black.Index;

                var styleDedault = workbook.CreateCellStyle();
                styleDedault.CloneStyleFrom(styleBorder);
                styleDedault.SetFont(font);

                var styleDot = workbook.CreateCellStyle();
                styleDot.CloneStyleFrom(styleBorderDot);
                styleDot.SetFont(font);

                var styleTitle = workbook.CreateCellStyle();
                styleTitle.CloneStyleFrom(styleCenter);
                styleTitle.SetFont(fontbold12);

                var styleSubTitle = workbook.CreateCellStyle();
                styleSubTitle.CloneStyleFrom(styleCenter);
                styleSubTitle.SetFont(fontbold8);

                var styleHeader = workbook.CreateCellStyle();
                styleHeader.CloneStyleFrom(styleCenterBorder);
                styleHeader.SetFont(fontbold8);

                var styleDedaultMerge = workbook.CreateCellStyle();
                styleDedaultMerge.CloneStyleFrom(styleCenter);
                styleDedaultMerge.SetFont(font);

                var styleBold = workbook.CreateCellStyle();
                styleBold.SetFont(fontbold8);

                var styleSmall = workbook.CreateCellStyle();
                styleSmall.CloneStyleFrom(styleBorder);
                styleSmall.SetFont(fontSmall);

                var cellStyleHeader = workbook.CreateCellStyle();
                cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                cellStyleHeader.FillPattern = FillPattern.SolidForeground;

                var fontbold18TimesNewRoman = workbook.CreateFont();
                fontbold18TimesNewRoman.FontHeightInPoints = 18;
                fontbold18TimesNewRoman.FontName = "Times New Roman";
                fontbold18TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold11TimesNewRoman = workbook.CreateFont();
                fontbold11TimesNewRoman.FontHeightInPoints = 11;
                fontbold11TimesNewRoman.FontName = "Times New Roman";
                fontbold11TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold10TimesNewRoman = workbook.CreateFont();
                fontbold10TimesNewRoman.FontHeightInPoints = 10;
                fontbold10TimesNewRoman.FontName = "Times New Roman";
                fontbold10TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var fontbold9TimesNewRoman = workbook.CreateFont();
                fontbold9TimesNewRoman.FontHeightInPoints = 9;
                fontbold9TimesNewRoman.FontName = "Times New Roman";
                fontbold9TimesNewRoman.Boldweight = (short)FontBoldWeight.Bold;

                var styleRow0 = workbook.CreateCellStyle();
                styleRow0.SetFont(fontbold18TimesNewRoman);
                styleRow0.Alignment = HorizontalAlignment.Center;
                styleRow0.VerticalAlignment = VerticalAlignment.Center;
                styleRow0.BorderBottom = BorderStyle.Thin;
                styleRow0.BorderTop = BorderStyle.Thin;
                styleRow0.BorderLeft = BorderStyle.Thin;
                styleRow0.BorderRight = BorderStyle.Thin;

                var styleBorderBold11Background = workbook.CreateCellStyle();
                styleBorderBold11Background.SetFont(fontbold11TimesNewRoman);
                styleBorderBold11Background.Alignment = HorizontalAlignment.Center;
                styleBorderBold11Background.VerticalAlignment = VerticalAlignment.Center;
                styleBorderBold11Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold11Background.BorderTop = BorderStyle.Thin;
                styleBorderBold11Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold11Background.BorderRight = BorderStyle.Thin;
                styleBorderBold11Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold11Background.FillPattern = FillPattern.SolidForeground;

                var styleBorderBold10Background = workbook.CreateCellStyle();
                styleBorderBold10Background.SetFont(fontbold10TimesNewRoman);
                styleBorderBold10Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold10Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold10Background.BorderRight = BorderStyle.Thin;
                styleBorderBold10Background.BorderTop = BorderStyle.Thin;
                styleBorderBold10Background.Alignment = HorizontalAlignment.Center;
                styleBorderBold10Background.VerticalAlignment = VerticalAlignment.Center;
                styleBorderBold10Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold10Background.FillPattern = FillPattern.SolidForeground;

                var styleBorderBold9Background = workbook.CreateCellStyle();
                styleBorderBold9Background.SetFont(fontbold9TimesNewRoman);
                styleBorderBold9Background.BorderBottom = BorderStyle.Thin;
                styleBorderBold9Background.BorderLeft = BorderStyle.Thin;
                styleBorderBold9Background.BorderRight = BorderStyle.Thin;
                styleBorderBold9Background.BorderTop = BorderStyle.Thin;
                styleBorderBold9Background.FillForegroundColor = HSSFColor.Grey50Percent.Index;
                styleBorderBold9Background.FillPattern = FillPattern.SolidForeground;
                #endregion

                ISheet sheet1 = workbook.CreateSheet(month.ToString("00") + year.ToString("0000") + "-NGAYCONG-THUONG-BHXH ");

                #region Title
                var rowIndex = 0;
                var columnIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                var cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Công ty TNHH CNSH SÀI GÒN XANH");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Địa chỉ: 127 Nguyễn Trọng Tuyển - P.15 - Q.Phú Nhuận - Tp HCM");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Điện thoại: (08)-39971869 - 38442457 - Fax: 08-39971869");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MST: 0302519810");
                cell.CellStyle = styleBold;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;
                #endregion

                #region Thang
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Thời gian:");
                cell.CellStyle = styleTitle;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(thangFormat);
                cell.CellStyle = styleTitle;
                rowIndex++;
                #endregion

                row = sheet1.CreateRow(rowIndex);
                row.Height = 1000;
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Tên NV");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Chức vụ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày vào làm");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày làm việc");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Lễ tết");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Phép năm");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Thưởng lễ, tết");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Lương đóng BHXH");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;
                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.SanXuatNgayCongThuongBHXHPost)]
        [HttpPost]
        public ActionResult SanXuatNgayCongThuongBHXHPost()
        {
            IFormFile file = Request.Form.Files[0];
            string folderName = Constants.Storage.Hr;
            string webRootPath = _env.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            if (file.Length > 0)
            {
                string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                ISheet sheet;
                string fullPath = Path.Combine(newPath, file.FileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Position = 0;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet = hssfwb.GetSheetAt(0);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
                    }

                    var timeRow = sheet.GetRow(5);
                    var time = Utility.GetFormattedCellValue(timeRow.GetCell(1));
                    var endMonthDate = Utility.GetToDate(time);
                    var month = endMonthDate.Month;
                    var year = endMonthDate.Year;
                    var toDate = new DateTime(year, month, 25);
                    var fromDate = toDate.AddMonths(-1).AddDays(1);
                    var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);


                    for (int i = 7; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var ma = string.Empty;
                        var ten = string.Empty;
                        var chucvu = string.Empty;
                        var ngayvaolam = string.Empty;
                        double ngaylamviec = 0;
                        double phepnam = 0;
                        double letet = 0;
                        decimal thuongletet = 0;
                        decimal bhxh = 0;

                        int columnIndex = 0;
                        ma = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        ten = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        chucvu = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        ngayvaolam = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        ngaylamviec = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        phepnam = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        letet = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        thuongletet = (decimal)Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        bhxh = (decimal)Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;

                        if (string.IsNullOrEmpty(ten))
                        {
                            continue;
                        }
                        var alias = Utility.AliasConvert(ten);
                        var existEmployee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                        if (existEmployee != null)
                        {
                            DataSanXuat(month, year, ngaylamviec, phepnam, letet, thuongletet, bhxh, existEmployee);
                        }
                        else
                        {
                            InsertNewEmployee(ten, ma, chucvu, ngayvaolam);
                            var employee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                            DataSanXuat(month, year, ngaylamviec, phepnam, letet, thuongletet, bhxh, employee);
                        }
                    }
                }
            }
            return Json(new { result = true });
        }

        private void DataSanXuat(int month, int year, double ngaylamviec, double phepnam, double letet, decimal thuongletet, decimal bhxh, Employee existEmployee)
        {
            // Salary base month-year
            var existSalary = dbContext.SalaryEmployeeMonths.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(existEmployee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (existSalary != null)
            {
                var builderSalary = Builders<SalaryEmployeeMonth>.Filter;
                var filterSalary = builderSalary.Eq(m => m.Id, existSalary.Id);
                var updateSalary = Builders<SalaryEmployeeMonth>.Update
                    .Set(m => m.NgayCongLamViec, ngaylamviec)
                    .Set(m => m.NgayNghiLeTetHuongLuong, letet)
                    .Set(m => m.NgayNghiPhepNam, phepnam)
                    .Set(m => m.LuongThamGiaBHXH, bhxh)
                    .Set(m => m.ThuongLeTet, thuongletet)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalaryEmployeeMonths.UpdateOne(filterSalary, updateSalary);
            }
            else
            {
                dbContext.SalaryEmployeeMonths.InsertOne(new SalaryEmployeeMonth
                {
                    Month = month,
                    Year = year,
                    EmployeeId = existEmployee.Id,
                    EmployeeCode = existEmployee.CodeOld,
                    NgayCongLamViec = ngaylamviec,
                    NgayNghiPhepNam = phepnam,
                    NgayNghiLeTetHuongLuong = letet,
                    ThuongLeTet = thuongletet,
                    LuongThamGiaBHXH = bhxh
                });
            }

            // EmployeeWorkTimeMonthLog
            var existTimes = dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(existEmployee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (existTimes != null)
            {
                var filterTime = Builders<EmployeeWorkTimeMonthLog>.Filter.Eq(m => m.Id, existTimes.Id);
                var updateTime = Builders<EmployeeWorkTimeMonthLog>.Update
                    .Set(m => m.NgayLamViecChinhTay, ngaylamviec)
                    .Set(m => m.PhepNamChinhTay, phepnam)
                    .Set(m => m.LeTetChinhTay, letet);
                dbContext.EmployeeWorkTimeMonthLogs.UpdateOne(filterTime, updateTime);
            }
            else
            {
                dbContext.EmployeeWorkTimeMonthLogs.InsertOne(new EmployeeWorkTimeMonthLog
                {
                    Year = year,
                    Month = month,
                    EmployeeId = existEmployee.Id,
                    EmployeeName = existEmployee.FullName,
                    NgayLamViecChinhTay = ngaylamviec,
                    LeTetChinhTay = letet,
                    PhepNamChinhTay = phepnam
                });
            }
        }

        private void TamUngSanXuat(int month, int year, decimal tamung, Employee existEmployee)
        {
            // Salary base month-year
            var existSalary = dbContext.SalaryEmployeeMonths.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(existEmployee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (existSalary != null)
            {
                var builderSalary = Builders<SalaryEmployeeMonth>.Filter;
                var filterSalary = builderSalary.Eq(m => m.Id, existSalary.Id);
                var updateSalary = Builders<SalaryEmployeeMonth>.Update
                    .Set(m => m.TamUng, tamung)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalaryEmployeeMonths.UpdateOne(filterSalary, updateSalary);
            }
            else
            {
                dbContext.SalaryEmployeeMonths.InsertOne(new SalaryEmployeeMonth
                {
                    Month = month,
                    Year = year,
                    EmployeeId = existEmployee.Id,
                    EmployeeCode = existEmployee.CodeOld,
                    NgachLuongCode = "B.05",
                    TamUng = tamung
                });
            }

            // CreditEmployee
            if (tamung > 0)
            {
                var existCredit = dbContext.CreditEmployees.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECredit.UngLuong) && m.EmployeeId.Equals(existEmployee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                if (existCredit != null)
                {
                    var filterCredit = Builders<CreditEmployee>.Filter.Eq(m => m.Id, existCredit.Id);
                    var updateCredit = Builders<CreditEmployee>.Update
                        .Set(m => m.Money, tamung);
                    dbContext.CreditEmployees.UpdateOne(filterCredit, updateCredit);
                }
                else
                {
                    dbContext.CreditEmployees.InsertOne(new CreditEmployee
                    {
                        Year = year,
                        Month = month,
                        EmployeeId = existEmployee.Id,
                        EmployeeCode = existEmployee.CodeOld,
                        FullName = existEmployee.FullName,
                        Type = (int)ECredit.UngLuong,
                        Money = tamung,
                        DateCredit = new DateTime(year, month, 1),
                        DateFirstPay = new DateTime(year, month, 5).AddMonths(1)
                    });
                }
            }
        }

        private void InsertNewEmployee(string fullname, string oldcode, string chucvu, string ngayvaolam)
        {
            chucvu = string.IsNullOrEmpty(chucvu) ? "CÔNG NHÂN" : chucvu.ToUpper();
            DateTime joinday = string.IsNullOrEmpty(ngayvaolam) ? DateTime.Now : DateTime.FromOADate(Convert.ToDouble(ngayvaolam));

            var entity = new Employee
            {
                FullName = fullname,
                CodeOld = oldcode,
                SalaryType = (int)EKhoiLamViec.SX,
                Joinday = joinday
            };

            #region System Generate
            var pwdrandom = Guid.NewGuid().ToString("N").Substring(0, 6);
            var settings = dbContext.Settings.Find(m => true).ToList();
            // always have value
            var employeeCodeFirst = settings.Where(m => m.Key.Equals("employeeCodeFirst")).First().Value;
            var employeeCodeLength = settings.Where(m => m.Key.Equals("employeeCodeLength")).First().Value;
            var lastEntity = dbContext.Employees.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Id).Limit(1).First();
            var x = 1;
            if (lastEntity != null && lastEntity.Code != null)
            {
                x = Convert.ToInt32(lastEntity.Code.Replace(employeeCodeFirst, string.Empty)) + 1;
            }
            var sysCode = employeeCodeFirst + x.ToString($"D{employeeCodeLength}");
            #endregion

            entity.Code = sysCode;
            entity.Password = pwdrandom;
            entity.AliasFullName = Utility.AliasConvert(entity.FullName);
            dbContext.Employees.InsertOne(entity);

            var newUserId = entity.Id;
            var hisEntity = entity;
            hisEntity.EmployeeId = newUserId;
            dbContext.EmployeeHistories.InsertOne(hisEntity);
        }

        #endregion

        #region SETTINGS
        [Route(Constants.LinkSalary.Setting)]
        public IActionResult Setting(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }
        #endregion

        #region IMPORT DATA
        [Route(Constants.LinkSalary.NhaMayTemplate)]
        public async Task<IActionResult> NhaMayTemplate(string fileName)
        {
            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"du-lieu-nha-may-thang-" + DateTime.Now.Month + "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                #region Styling
                var cellStyleBorder = workbook.CreateCellStyle();
                cellStyleBorder.BorderBottom = BorderStyle.Thin;
                cellStyleBorder.BorderLeft = BorderStyle.Thin;
                cellStyleBorder.BorderRight = BorderStyle.Thin;
                cellStyleBorder.BorderTop = BorderStyle.Thin;
                cellStyleBorder.Alignment = HorizontalAlignment.Center;
                cellStyleBorder.VerticalAlignment = VerticalAlignment.Center;

                var cellStyleHeader = workbook.CreateCellStyle();
                //cellStyleHeader.CloneStyleFrom(cellStyleBorder);
                //cellStyleHeader.FillForegroundColor = HSSFColor.Blue.Index2;
                cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                cellStyleHeader.FillPattern = FillPattern.SolidForeground;

                var font = workbook.CreateFont();
                font.FontHeightInPoints = 11;
                //font.FontName = "Calibri";
                font.Boldweight = (short)FontBoldWeight.Bold;
                #endregion

                ISheet sheet1 = workbook.CreateSheet("NMT" + DateTime.Now.Month.ToString("00"));
                //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 3, 7));
                //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 8, 13));

                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("SỐ LIỆU NHÀ MÁY");
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("Tháng");
                row.CreateCell(1, CellType.Numeric).SetCellValue(DateTime.Now.Month);
                row.CreateCell(2, CellType.String).SetCellValue("Năm");
                row.CreateCell(3, CellType.Numeric).SetCellValue(DateTime.Now.Year);
                // Set style
                for (int i = 0; i <= 3; i++)
                {
                    row.Cells[i].CellStyle = cellStyleHeader;
                }
                row.Cells[1].CellStyle.SetFont(font);
                row.Cells[3].CellStyle.SetFont(font);
                rowIndex++;
                //https://stackoverflow.com/questions/51681846/rowspan-and-colspan-in-apache-poi

                row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("#");
                row.CreateCell(1, CellType.String).SetCellValue("Mã nhân viên");
                row.CreateCell(2, CellType.String).SetCellValue("Họ tên");
                row.CreateCell(3, CellType.String).SetCellValue("Chức vụ");
                row.CreateCell(4, CellType.String).SetCellValue("Công tổng");
                row.CreateCell(5, CellType.String).SetCellValue("Cơm SX");
                row.CreateCell(6, CellType.String).SetCellValue("Cơm KD");
                row.CreateCell(7, CellType.String).SetCellValue("Giờ tăng ca");
                row.CreateCell(8, CellType.String).SetCellValue("Giờ làm việc CN");
                row.CreateCell(9, CellType.String).SetCellValue("Giờ làm việc Lễ/Tết");
                // Set style
                for (int i = 0; i <= 9; i++)
                {
                    row.Cells[i].CellStyle = cellStyleHeader;
                }


                for (int i = 1; i <= 50; i++)
                {
                    row = sheet1.CreateRow(rowIndex + 1);
                    //row.CreateCell(0, CellType.Numeric).SetCellValue(i);
                    row.CreateCell(1, CellType.String).SetCellValue(string.Empty);
                    row.CreateCell(2, CellType.String).SetCellValue(string.Empty);
                    row.CreateCell(3, CellType.String).SetCellValue(string.Empty);
                    row.CreateCell(4, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(5, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(6, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(7, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(8, CellType.Numeric).SetCellValue(string.Empty);
                    row.CreateCell(9, CellType.Numeric).SetCellValue(string.Empty);
                    rowIndex++;
                }

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.NhaMayImport)]
        [HttpPost]
        public ActionResult NhaMayImport()
        {
            IFormFile file = Request.Form.Files[0];
            string folderName = Constants.Storage.Hr;
            string webRootPath = _env.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            if (file.Length > 0)
            {
                string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                ISheet sheet;
                string fullPath = Path.Combine(newPath, file.FileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Position = 0;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet = hssfwb.GetSheetAt(0);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
                    }

                    var timeRow = sheet.GetRow(1);
                    var month = Convert.ToInt32(Utility.GetNumbericCellValue(timeRow.GetCell(1)));
                    var year = Convert.ToInt32(Utility.GetNumbericCellValue(timeRow.GetCell(3)));
                    if (month == 0)
                    {
                        month = DateTime.Now.Month;
                    }
                    if (year == 0)
                    {
                        year = DateTime.Now.Year;
                    }

                    for (int i = 3; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var code = Utility.GetFormattedCellValue(row.GetCell(1));
                        var fullName = Utility.GetFormattedCellValue(row.GetCell(2));
                        var alias = Utility.AliasConvert(fullName);
                        var title = Utility.GetFormattedCellValue(row.GetCell(3));

                        decimal congtong = (decimal)Utility.GetNumbericCellValue(row.GetCell(4));
                        var comsx = Utility.GetNumbericCellValue(row.GetCell(5));
                        var comkd = Utility.GetNumbericCellValue(row.GetCell(6));
                        decimal giotangca = (decimal)Utility.GetNumbericCellValue(row.GetCell(7));
                        decimal giolamvieccn = (decimal)Utility.GetNumbericCellValue(row.GetCell(8));
                        decimal giolamviecletet = (decimal)Utility.GetNumbericCellValue(row.GetCell(9));

                        var employee = new Employee();
                        if (!string.IsNullOrEmpty(alias))
                        {
                            employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.AliasFullName.Equals(alias)).FirstOrDefault();
                        }
                        if (employee == null)
                        {
                            if (!string.IsNullOrEmpty(fullName))
                            {
                                employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.FullName.Equals(fullName)).FirstOrDefault();
                            }
                        }
                        if (employee == null)
                        {
                            if (!string.IsNullOrEmpty(code))
                            {
                                employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.CodeOld.Equals(code)).FirstOrDefault();
                            }
                        }
                        if (employee != null)
                        {
                            // check exist to update
                            var existEntity = dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (existEntity != null)
                            {
                                var builder = Builders<EmployeeCong>.Filter;
                                var filter = builder.Eq(m => m.Id, existEntity.Id);
                                var update = Builders<EmployeeCong>.Update
                                    .Set(m => m.EmployeeCode, employee.Code)
                                    .Set(m => m.EmployeeName, employee.FullName)
                                    .Set(m => m.CongTong, congtong)
                                    .Set(m => m.ComKD, comkd)
                                    .Set(m => m.ComSX, comsx)
                                    .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                                dbContext.EmployeeCongs.UpdateOne(filter, update);
                            }
                            else
                            {
                                var newItem = new EmployeeCong
                                {
                                    Year = year,
                                    Month = month,
                                    EmployeeId = employee.Id,
                                    EmployeeCode = employee.Code,
                                    EmployeeName = employee.FullName,
                                    CongTong = congtong,
                                    ComKD = comkd,
                                    ComSX = comsx
                                };
                                dbContext.EmployeeCongs.InsertOne(newItem);
                            }
                        }
                        else
                        {
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "nha-may-import",
                                Object = "time: " + month + "-" + year + ", code: " +code + "-" + fullName + "-" + title + " ,dòng " + i,
                                Error = "No import data",
                                DateTime = DateTime.Now.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Factory + "/" + Constants.LinkSalary.CongTong });
        }
        #endregion

        #region Extension
        public SalaryEmployeeMonth GetSalaryEmployeeMonth(int year, int month, string employeeId, DateTime thamnienlamviec, string ngachLuong, int bac, double mauSo, decimal mauSoChuyenCan, decimal mauSoTienCom, decimal bhxh, double tyledongbh, SalaryEmployeeMonth newSalary, bool save)
        {
            var today = DateTime.Now.Date;
            int todayMonth = today.Day > 25 ? today.AddMonths(1).Month : today.Month;
            var endDateMonth = new DateTime(year, month, 25);
            var startDateMonth = endDateMonth.AddMonths(-1).AddDays(1);

            decimal luongCB = GetLuongCB(ngachLuong, bac);

            var currentSalary = new SalaryEmployeeMonth();
            var existSalary = dbContext.SalaryEmployeeMonths.CountDocuments(m => m.EmployeeId.Equals(employeeId) & m.Year.Equals(year) & m.Month.Equals(month)) > 0 ? true : false;
            // debug
            existSalary = false;
            // end debug
            if (existSalary)
            {
                currentSalary = dbContext.SalaryEmployeeMonths.Find(m => m.EmployeeId.Equals(employeeId) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefault();
            }
            else
            {
                var lastestSalary = dbContext.SalaryEmployeeMonths.Find(m => m.EmployeeId.Equals(employeeId)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
                var ngaythamnien = (endDateMonth - thamnienlamviec).TotalDays;
                double thangthamnien = Math.Round(ngaythamnien / 30, 0);
                double namthamnien = Math.Round(thangthamnien / 12, 0);
                var hesothamnien = 0;
                // 3 năm đầu ko tăng, bắt đầu năm thứ 4 sẽ có thâm niên 3%, thêm 1 năm tăng 1%
                if (namthamnien >= 4)
                {
                    hesothamnien = 3;
                    for (int i = 5; i <= namthamnien; i++)
                    {
                        hesothamnien++;
                    }
                }
                currentSalary.ThamNienMonth = (int)thangthamnien;
                currentSalary.ThamNienYear = (int)namthamnien;
                currentSalary.HeSoThamNien = hesothamnien;
                currentSalary.ThamNien = luongCB * hesothamnien / 100;
                currentSalary.LuongThamGiaBHXH = lastestSalary != null ? lastestSalary.LuongThamGiaBHXH : bhxh;
            }

            if (newSalary != null)
            {
                currentSalary.NgachLuongLevel = newSalary.NgachLuongLevel;
                currentSalary.LuongKhac = newSalary.LuongKhac;
                currentSalary.HoTroNgoaiLuong = newSalary.HoTroNgoaiLuong;
                currentSalary.ThuongLeTet = newSalary.ThuongLeTet;
                currentSalary.LuongThamGiaBHXH = newSalary.LuongThamGiaBHXH;
            }

            double ngayLamViec = 0;
            double ngayNghiPhepNam = 0;
            double ngayNghiLeTet = 0;
            double nghikhongphep = 0;
            double nghiviecrieng = 0;
            double nghibenh = 0;
            int trukhongphep = 0;
            int truviecrieng = 0;
            int trubenh = 0;
            int chuyencanchiso = 0;

            double tangCaNgayThuong = 0;
            double tangCaCN = 0;
            double tangCaLeTet = 0;

            var chamCongs = dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(employeeId) & m.Year.Equals(year) & m.Month.Equals(month)).ToList();
            if (chamCongs != null & chamCongs.Count > 0)
            {
                foreach (var chamCong in chamCongs)
                {
                    ngayLamViec += chamCong.NgayLamViecChinhTay;
                    ngayNghiPhepNam += chamCong.PhepNamChinhTay;
                    ngayNghiLeTet += chamCong.LeTetChinhTay;
                    nghikhongphep += chamCong.NghiKhongPhep;
                    nghiviecrieng += chamCong.NghiViecRieng;
                    nghibenh += chamCong.NghiBenh;
                }
                if (nghikhongphep >= 1)
                {
                    trukhongphep = 100;
                }
                if (nghiviecrieng >= 3)
                {
                    truviecrieng = 100;
                }
                else if (nghiviecrieng > 1 && nghiviecrieng < 3)
                {
                    truviecrieng = 75;
                }
                else if (nghiviecrieng == 1)
                {
                    truviecrieng = 50;
                }
                if (nghibenh >= 4)
                {
                    trubenh = 100;
                }
                else if (nghibenh > 2 && nghibenh <= 3)
                {
                    trubenh = 75;
                }
                if (nghibenh > 1 && nghibenh <= 2)
                {
                    trubenh = 50;
                }
                if (nghibenh == 1)
                {
                    trubenh = 25;
                }
                chuyencanchiso = (trukhongphep + truviecrieng + trubenh) >= 100 ? 100 : (trukhongphep + truviecrieng + trubenh);
            }

            decimal luongDinhMuc = Convert.ToDecimal(ngayLamViec * (double)luongCB / mauSo);

            decimal congTong = 0;
            // RESOLVE CONG TONG. UPDATE TO DB.
            // Query base employee, time.
            // Manage in collection [SalaryNhaMayCongs]
            var dataCong = dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (dataCong != null)
            {
                congTong = dataCong.CongTong;
            }

            decimal luongVuotDinhMuc = congTong - luongDinhMuc;
            if (luongVuotDinhMuc < 0)
            {
                luongVuotDinhMuc = 0;
            }

            decimal thamnien = currentSalary.ThamNien;
            decimal phucapchuyencan = Convert.ToDecimal(((100 - chuyencanchiso) / 100) * (double)mauSoChuyenCan);

            decimal phucapkhac = currentSalary.HoTroNgoaiLuong;
            decimal tongphucap = thamnien + phucapchuyencan + phucapkhac;

            decimal tongthunhap = tongphucap + luongVuotDinhMuc + luongCB;

            decimal tamung = 0;
            var credits = dbContext.CreditEmployees.Find(m => m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)).ToList();
            if (credits != null && credits.Count > 0)
            {
                foreach (var credit in credits)
                {
                    tamung += credit.Money;
                }
            }

            decimal luongthamgiabhxh = currentSalary.LuongThamGiaBHXH;
            decimal dongbhxh = Convert.ToDecimal((double)luongthamgiabhxh * tyledongbh);

            decimal thuongletet = currentSalary.ThuongLeTet;

            decimal thuclanh = tongthunhap - tamung - dongbhxh;

            decimal comSX = 0;
            decimal comKD = 0;
            var dataSX = dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employeeId) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (dataSX != null)
            {
                comSX = Convert.ToDecimal(dataSX.ComSX * (double)mauSoTienCom);
                comKD = Convert.ToDecimal(dataSX.ComKD * (double)mauSoTienCom);
            }

            decimal hotrothem = 0;

            decimal thuclanhlamtron = (Math.Round(thuclanh / 1000) * 1000) + comSX + comKD + hotrothem;



            #region update field to currentSalary
            currentSalary.LuongCanBan = luongCB;
            currentSalary.ThamNien = thamnien;
            currentSalary.NgayCongLamViec = ngayLamViec;
            currentSalary.NgayNghiPhepNam = ngayNghiPhepNam;
            currentSalary.CongTangCaNgayThuongGio = tangCaNgayThuong;
            currentSalary.CongCNGio = tangCaCN;
            currentSalary.CongLeTet = tangCaLeTet;

            currentSalary.MauSo = mauSo;
            currentSalary.LuongDinhMuc = luongDinhMuc;
            currentSalary.LuongVuotDinhMuc = luongVuotDinhMuc;
            currentSalary.PhuCapChuyenCan = phucapchuyencan;
            currentSalary.TongPhuCap = tongphucap;

            currentSalary.TongThuNhap = tongthunhap;

            currentSalary.BHXHBHYT = bhxh;

            currentSalary.TamUng = tamung;

            currentSalary.ThucLanh = thuclanh;
            currentSalary.ComSX = comSX;
            currentSalary.ComKD = comKD;
            currentSalary.ThucLanhTronSo = thuclanhlamtron;
            #endregion

            // Save common information
            if (save)
            {
                if (existSalary)
                {
                    var builder = Builders<SalaryEmployeeMonth>.Filter;
                    var filter = builder.Eq(m => m.Id, currentSalary.Id);
                    var update = Builders<SalaryEmployeeMonth>.Update
                        .Set(m => m.NgachLuongLevel, currentSalary.NgachLuongLevel)
                        .Set(m => m.LuongCanBan, currentSalary.LuongCanBan)
                        .Set(m => m.NangNhocDocHai, currentSalary.NangNhocDocHai)
                        .Set(m => m.TrachNhiem, currentSalary.TrachNhiem)
                        .Set(m => m.ThuHut, currentSalary.ThuHut)
                        .Set(m => m.Xang, currentSalary.Xang)
                        .Set(m => m.DienThoai, currentSalary.DienThoai)
                        .Set(m => m.Com, currentSalary.Com)
                        .Set(m => m.NhaO, currentSalary.NhaO)
                        .Set(m => m.KiemNhiem, currentSalary.KiemNhiem)
                        .Set(m => m.BhytDacBiet, currentSalary.BhytDacBiet)
                        .Set(m => m.ViTriCanKnNhieuNam, currentSalary.ViTriCanKnNhieuNam)
                        .Set(m => m.ViTriDacThu, currentSalary.ViTriDacThu)
                        .Set(m => m.LuongCoBanBaoGomPhuCap, currentSalary.LuongCoBanBaoGomPhuCap)
                        .Set(m => m.NgayCongLamViec, currentSalary.NgayCongLamViec)
                        .Set(m => m.NgayNghiPhepNam, currentSalary.NgayNghiPhepNam)
                        .Set(m => m.NgayNghiPhepHuongLuong, currentSalary.NgayNghiPhepHuongLuong)
                        .Set(m => m.NgayNghiLeTetHuongLuong, currentSalary.NgayNghiLeTetHuongLuong)
                        .Set(m => m.CongCNGio, currentSalary.CongCNGio)
                        .Set(m => m.CongTangCaNgayThuongGio, currentSalary.CongTangCaNgayThuongGio)
                        .Set(m => m.CongLeTet, currentSalary.CongLeTet)
                        .Set(m => m.CongTacXa, currentSalary.CongTacXa)
                        .Set(m => m.MucDatTrongThang, currentSalary.MucDatTrongThang)
                        .Set(m => m.LuongTheoDoanhThuDoanhSo, currentSalary.LuongTheoDoanhThuDoanhSo)
                        .Set(m => m.TongBunBoc, currentSalary.TongBunBoc)
                        .Set(m => m.ThanhTienBunBoc, currentSalary.ThanhTienBunBoc)
                        .Set(m => m.LuongKhac, currentSalary.LuongKhac)
                        .Set(m => m.ThiDua, currentSalary.ThiDua)
                        .Set(m => m.HoTroNgoaiLuong, currentSalary.HoTroNgoaiLuong)
                        .Set(m => m.TongThuNhap, currentSalary.TongThuNhap)
                        .Set(m => m.BHXHBHYT, currentSalary.BHXHBHYT)
                        .Set(m => m.LuongThamGiaBHXH, currentSalary.LuongThamGiaBHXH)
                        .Set(m => m.TamUng, currentSalary.TamUng)
                        .Set(m => m.ThuongLeTet, currentSalary.ThuongLeTet)
                        .Set(m => m.ThucLanh, currentSalary.ThucLanh)
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    dbContext.SalaryEmployeeMonths.UpdateOne(filter, update);
                }
                else
                {
                    dbContext.SalaryEmployeeMonths.InsertOne(currentSalary);
                }
            }

            return currentSalary;
        }

        public decimal GetLuongCB(string maSo, int bac)
        {
            decimal luongCB = 0;

            // NHA MAY & SANXUAT => GET DIRECT [NgachLuongs] base maSo vs bac
            var lastNgachLuong = dbContext.NgachLuongs.Find(m => m.Enable.Equals(true) && m.Law.Equals(false) && m.MaSo.Equals(maSo) && m.Bac.Equals(bac)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
            if (lastNgachLuong != null)
            {
                luongCB = lastNgachLuong.MucLuongThang;
            }

            return luongCB;
        }
        #endregion
    }
}
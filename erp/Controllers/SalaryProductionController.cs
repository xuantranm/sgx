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
    [Route(Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Production)]
    public class SalaryProductionController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalaryProductionController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalaryProductionController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _logger = logger;
        }

        [Route(Constants.LinkSalary.BangLuong)]
        public async Task<IActionResult> BangLuong(string Thang, string Id)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongSX, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL
            var sortTimes = Utility.DllMonths();
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false)
                                && m.ChucVu.Equals("5c88d09bd59d56225c4324de") && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToList();
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
            Utility.AutoInitSalary((int)ESalaryType.SX, month, year);

            #region Filter
            var builder = Builders<SalaryEmployeeMonth>.Filter;
            var filter = builder.Eq(m => m.Year, year) & builder.Eq(m => m.Month, month)
                        & builder.Eq(m => m.ChucVuId, "5c88d09bd59d56225c4324de");
            if (!string.IsNullOrEmpty(Id))
            {
                filter = filter & builder.Eq(x => x.Id, Id.Trim());
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<SalaryEmployeeMonth>.Sort.Ascending(m => m.EmployeeCode);
            #endregion

            var salaryEmployeeMonths = dbContext.SalaryEmployeeMonths.Find(filter).Sort(sortBuilder).ToList();

            #region FILL DATA
            var results = new List<SalaryEmployeeMonth>();
            foreach (var salary in salaryEmployeeMonths)
            {
                var salaryFull = Utility.SalaryEmployeeMonthFillData(salary);
                results.Add(salaryFull);
            }
            #endregion

            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = results,
                MonthYears = sortTimes,
                Thang = Thang,
                Employees = employees,
                Id = Id,
                ThamSoTinhLuong = Utility.BusinessDaysUntil(fromDate, toDate)
            };

            return View(viewModel);
        }
        
        #region PHU CAP & TANG CA
        [Route(Constants.LinkSalary.PhuCap)]
        public IActionResult PhuCap(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.SanXuatNgoaiGioPhuCapTemplate)]
        public async Task<IActionResult> SanXuatNgoaiGioPhuCapTemplate(string thang)
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
            var toDate = Utility.GetSalaryToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            var thangFormat = month.ToString("00") + "-" + year.ToString("0000");
            #endregion

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"" + thangFormat + "-tang-ca-phu-cap";
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

                ISheet sheet1 = workbook.CreateSheet("TANG CA - PHU CAP " + DateTime.Now.Month.ToString("00"));

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

                var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 2;
                for (DateTime date = fromDate; date <= toDate; date = date.AddDays(1.0))
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(date.Day);
                    cell.CellStyle.SetFont(font);
                    columnIndex++;
                }
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleBorderBold11Background;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleBorderBold11Background;
                columnIndex++;

                var cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + fromToNum);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Giờ tăng ca trong tháng".ToUpper());
                cell.CellStyle = styleBorderBold11Background;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Họ và Tên");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                for (DateTime date = fromDate; date <= toDate; date = date.AddDays(1.0))
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(Constants.DayOfWeekT2(date));
                    cell.CellStyle = styleBorderBold10Background;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày thường");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("CN");
                cell.CellStyle = styleBorderBold10Background;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("CƠM");
                cell.CellStyle = styleBorderBold10Background;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, (columnIndex + 5 + fromToNum));
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("KẾ HOẠCH SẢN XUẤT");
                cell.CellStyle = styleBorderBold9Background;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.SanXuatNgoaiGioPhuCapPost)]
        [HttpPost]
        public ActionResult SanXuatNgoaiGioPhuCapPost()
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
                    var endMonthDate = Utility.GetSalaryToDate(time);
                    var month = endMonthDate.Month;
                    var year = endMonthDate.Year;
                    var toDate = new DateTime(year, month, 25);
                    var fromDate = toDate.AddMonths(-1).AddDays(1);
                    var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);

                    var dayRow = sheet.GetRow(7);
                    //var nameDayRow = sheet.GetRow(9);

                    for (int i = 11; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        int columnIndex = 0;
                        var ma = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var ten = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var alias = Utility.AliasConvert(ten);

                        if (string.IsNullOrEmpty(ten))
                        {
                            continue;
                        }

                        var values = new List<string>();
                        for (DateTime date = fromDate; date <= toDate; date = date.AddDays(1.0))
                        {
                            values.Add(date + ";" + Utility.GetNumbericCellValue(row.GetCell(columnIndex)));
                            columnIndex++;
                        }
                        double ngaythuong = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        double cn = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var com = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        //columnIndex++;

                        var existEmployee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                        if (existEmployee != null)
                        {
                            DataTangCaPhuCap(month, year, existEmployee, values, ngaythuong, cn, com);
                        }
                        else
                        {
                            InsertNewEmployee(ten, ma, string.Empty, string.Empty);
                            var employee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                            DataTangCaPhuCap(month, year, existEmployee, values, ngaythuong, cn, com);
                        }
                    }
                }
            }
            return Json(new { result = true });
        }

        private void DataTangCaPhuCap(int month, int year, Employee employee, List<string> values, double ngaythuong, double cn, double com)
        {
            #region Times
            foreach (var valueTime in values)
            {
                if (valueTime.Split(';').Count() > 1)
                {
                    var ngaytangca = DateTime.Parse(valueTime.Split(';')[0]);
                    var giotangca = TimeSpan.FromHours(Convert.ToDouble(valueTime.Split(';')[1]));
                    var time = dbContext.EmployeeWorkTimeLogs.Find(m => m.EmployeeId.Equals(employee.Id) && m.Date.Equals(ngaytangca)).FirstOrDefault();
                    if (time != null)
                    {
                        var filterTime = Builders<EmployeeWorkTimeLog>.Filter.Eq(m => m.Id, time.Id);
                        var updateTime = Builders<EmployeeWorkTimeLog>.Update
                            .Set(m => m.StatusTangCa, (int)ETangCa.DongY)
                            .Set(m => m.TangCaDaXacNhan, giotangca)
                            .Set(m => m.UpdatedOn, DateTime.Now);
                        dbContext.EmployeeWorkTimeLogs.UpdateOne(filterTime, updateTime);
                    }
                    else
                    {
                        // Update rule later.
                        // dbContext.EmployeeWorkTimeLogs.InsertOne
                    }
                }
            }
            #endregion

            #region EmployeeCongs
            var existCongTong = dbContext.EmployeeCongs.Find(m => m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            if (existCongTong != null)
            {
                var filterCongTong = Builders<EmployeeCong>.Filter.Eq(m => m.Id, existCongTong.Id);
                var updateCongTong = Builders<EmployeeCong>.Update
                    .Set(m => m.Com, com)
                    .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                dbContext.EmployeeCongs.UpdateOne(filterCongTong, updateCongTong);
            }
            else
            {
                dbContext.EmployeeCongs.InsertOne(new EmployeeCong()
                {
                    Month = month,
                    Year = year,
                    EmployeeId = employee.Id,
                    EmployeeCode = employee.CodeOld,
                    EmployeeName = employee.FullName,
                    Com = com
                });
            }
            #endregion
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
            var toDate = Utility.GetSalaryToDate(thang);
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
                    var endMonthDate = Utility.GetSalaryToDate(time);
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
            var toDate = Utility.GetSalaryToDate(thang);
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
                    var endMonthDate = Utility.GetSalaryToDate(time);
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
                    NgachLuongCode = "B.05",
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

        #region TONG HOP
        [Route(Constants.LinkSalary.TongHopTrongGio)]
        public async Task<IActionResult> TongHopTrongGio(string thang, string id)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongSX, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL
            var employeeDdl = await dbContext.Employees.Find(m => m.Enable.Equals(true) && m.SalaryType.Equals((int)EKhoiLamViec.SX) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
            #endregion

            #region Times
            var toDate = Utility.GetSalaryToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;
            #endregion

            #region Setting
            var mauSo = 26;
            var mauSoBaoVe = 27;
            var mauSoChuyenCan = 1;
            var mauSoTienCom = 1;
            decimal ibhxh = 8;
            decimal ibhyt = (decimal)1.5;
            decimal ibhtn = 1;
            #endregion

            #region Filter
            var builder = Builders<FactoryProductCongTheoThang>.Filter;
            var filter = builder.Eq(m => m.Mode, (int)EMode.TrongGio) & builder.Eq(m => m.Month, month) & builder.Eq(m => m.Year, year);
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.EmployeeId, id.Trim());
            }
            #endregion

            #region Sort
            //var sortBuilder = Builders<FactoryProductCongTheoThang>.Sort.Ascending(m => m.Code);
            //var congMs = await dbContext.FactoryProductCongTheoThangs.Find(filter).Sort(sortBuilder).ToListAsync();
            #endregion

            var congMs = await dbContext.FactoryProductCongTheoThangs.Find(filter).ToListAsync();

            var thanhphams = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToListAsync();
            var congviecs = await dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true) && m.Main.Equals(false)).SortBy(m => m.Sort).ToListAsync();
            var dongiaDMs = await dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToListAsync();
            var dongiaM3 = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            if (dongiaM3 == null)
            {
                dongiaM3 = new FactoryProductDonGiaM3
                {
                    Month = month,
                    Year = year,
                    Type = (int)EDinhMuc.BocVac,
                    Price = 11300
                };
                dbContext.FactoryProductDonGiaM3s.InsertOne(dongiaM3);
            }
            var congs = await dbContext.EmployeeCongs.Find(m => m.Enable.Equals(true) && m.Year.Equals(year) && m.Month.Equals(month)).ToListAsync();

            var viewModel = new BangLuongViewModel
            {
                MCongs = congMs,
                MonthYears = Utility.DllMonths(),
                Thang = thang,
                Employees = employeeDdl,
                Id = id,
                ThanhPhams = thanhphams,
                CongViecs = congviecs,
                DonGiaDMs = dongiaDMs,
                DonGiaM3 = dongiaM3,
                Congs = congs
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.SanXuatTongHopTrongGioTemplate)]
        public async Task<IActionResult> SanXuatTongHopTrongGioTemplate(string fileName, string thang)
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
            var toDate = Utility.GetSalaryToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            var thangFormat = month.ToString("00") + "-" + year.ToString("0000");
            #endregion

            var thanhphams = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToListAsync();
            var congviecs = await dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true) && m.Main.Equals(false)).SortBy(m => m.Sort).ToListAsync();
            var dongiathanhphans = await dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToListAsync();
            var dongiaM3entity = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            var dongiaM3 = dongiaM3entity != null ? dongiaM3entity.Price : 11300;
            int thanhphamCount = thanhphams.Count();
            int congviecCount = congviecs.Count();

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"" + thangFormat + "-tong-hop-trong-gio";
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
                #endregion

                ISheet sheet1 = workbook.CreateSheet("Tổng hợp trong giờ");

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

                #region Header
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 3;
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + thanhphamCount);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP CÔNG ĐÓNG BAO");
                cell.CellStyle = styleTitle;
                columnIndex = columnIndex + thanhphamCount;
                columnIndex++;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP BỐC HÀNG");
                cell.CellStyle = styleTitle;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + congviecCount);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP CÔNG VIỆC KHÁC");
                cell.CellStyle = styleTitle;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;
                #endregion

                #region Row 10
                int totalColumn = 3 + thanhphamCount + 1 + 1 + 1 + congviecCount;
                columnIndex = 0;
                for (var i = 1; i <= totalColumn; i++)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(i);
                    cell.CellStyle = styleHeader;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 11
                row = sheet1.CreateRow(rowIndex);
                row.Height = 2000;
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TÊN SẢN PHẨM");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in thanhphams)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(thanhpham.Name);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleHeader;
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Số M3");
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleHeader;
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var congviec in congviecs)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(congviec.Name);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 12
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MÃ SẢN PHẨM");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in thanhphams)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(thanhpham.Code);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                for (var i12 = columnIndex; i12 < totalColumn; i12++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 13
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("ĐƠN GIÁ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in dongiathanhphans)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue((double)Math.Round(thanhpham.DonGiaDieuChinh,0));
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.Numeric);
                cell.SetCellValue((double)Math.Round(dongiaM3,0));
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;
                foreach (var congviec in congviecs)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue((double)Math.Round(congviec.Price,0));
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 14
                row = sheet1.CreateRow(rowIndex);
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
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                var toTP = columnIndex + thanhphamCount;
                for (var i14 = columnIndex; i14 < toTP; i14++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                for (var i14 = columnIndex; i14 < totalColumn; i14++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.SanXuatTongHopTrongGioPost)]
        [HttpPost]
        public ActionResult SanXuatTongHopTrongGioPost()
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
                    var endMonthDate = Utility.GetSalaryToDate(time);
                    var month = endMonthDate.Month;
                    var year = endMonthDate.Year;

                    var priceRow = sheet.GetRow(11);

                    var thanhphams = dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToList();
                    var congviecs = dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true)).ToList();
                    var congvieckhacs = congviecs.Where(m => m.Main.Equals(false)).OrderBy(m => m.Sort).ToList();
                    var congviecbochang = congviecs.Find(m => m.Main.Equals(true) && m.Alias.Equals("boc-hang"));

                    var dongiathanhphans = dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToList();
                    var dongiaM3entity = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
                    var dongiaM3 = dongiaM3entity != null ? dongiaM3entity.Price : 11300;
                    int thanhphamCount = thanhphams.Count();
                    int congviecCount = congviecs.Count();

                    for (int i = 13; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var columnIndex = 0;
                        var code = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var fullName = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        columnIndex++;

                        if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(fullName))
                        {
                            continue;
                        }

                        var employee = !string.IsNullOrEmpty(fullName) ? dbContext.Employees.Find(m => m.Enable.Equals(true) & m.FullName.Equals(fullName.Trim())).FirstOrDefault() : null;
                        if (employee == null)
                        {
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "tong-hop-trong-gio",
                                Object = "Code: " + code + " | Name: " + fullName + " | Line " + i,
                                Error = "Khong tim thay thong tin",
                                DateTime = DateTime.Now.ToString()
                            });
                            continue;
                        }

                        double tongthoigiancongvieckhac = 0;
                        decimal tongcongcongvieckhac = 0;
                        decimal thanhtienthanhpham = 0;
                        decimal thanhtienbochang = 0;
                        decimal thanhtientronggio = 0;

                        #region THANH PHAM
                        foreach (var thanhpham in thanhphams)
                        {
                            var dongiathanhphamE = dongiathanhphans.Where(m => m.ProductId.Equals(thanhpham.Id)).FirstOrDefault();
                            // Get value
                            decimal objectPrice = dongiathanhphamE !=null ? dongiathanhphamE.DonGiaDieuChinh : 0;
                            double objectValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                            decimal objectAmount = Convert.ToDecimal(objectValue * (double)objectPrice);
                            thanhtienthanhpham += objectAmount;
                            // Insert db
                            var exist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(thanhpham.Code) && m.Type.Equals((int)EDinhMuc.DongGoi) && m.Mode.Equals((int)EMode.TrongGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (exist != null)
                            {
                                var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, exist.Id);
                                var update = Builders<FactoryProductCongTheoThang>.Update
                                    .Set(m => m.ObjectPrice, objectPrice)
                                    .Set(m => m.Value, objectValue)
                                    .Set(m => m.Amount, objectAmount);
                                dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                            }
                            else
                            {
                                dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                                {
                                    Month = month,
                                    Year = year,
                                    Type = (int)EDinhMuc.DongGoi,
                                    Mode = (int)EMode.TrongGio,
                                    EmployeeId = employee.Id,
                                    EmployeeCode = employee.CodeOld,
                                    EmployeeName = employee.FullName,
                                    EmployeeAlias = employee.AliasFullName,
                                    ObjectId = thanhpham.Id,
                                    ObjectCode = thanhpham.Code,
                                    ObjectName = thanhpham.Name,
                                    ObjectAlias = thanhpham.Alias,
                                    ObjectSort = thanhpham.Sort,
                                    ObjectPrice = objectPrice,
                                    Value = objectValue,
                                    Amount = objectAmount
                                });
                            }
                            columnIndex++;
                        }
                        #endregion
                        columnIndex++;

                        #region BOC HANG
                        decimal bochangPrice = congviecbochang.Price;
                        double bochangValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        decimal bochangAmount = Convert.ToDecimal(bochangValue * (double)bochangPrice);
                        thanhtienbochang += bochangAmount;

                        // Insert db
                        var bochangexist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(congviecbochang.Code) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Mode.Equals((int)EMode.TrongGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                        if (bochangexist != null)
                        {
                            var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, bochangexist.Id);
                            var update = Builders<FactoryProductCongTheoThang>.Update
                                .Set(m => m.ObjectPrice, bochangPrice)
                                .Set(m => m.Value, bochangValue)
                                .Set(m => m.Amount, bochangAmount);
                            dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                        }
                        else
                        {
                            dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                            {
                                Month = month,
                                Year = year,
                                Type = (int)EDinhMuc.BocVac,
                                Mode = (int)EMode.TrongGio,
                                EmployeeId = employee.Id,
                                EmployeeCode = employee.CodeOld,
                                EmployeeName = employee.FullName,
                                EmployeeAlias = employee.AliasFullName,
                                ObjectId = congviecbochang.Id,
                                ObjectCode = congviecbochang.Code,
                                ObjectName = congviecbochang.Name,
                                ObjectAlias = congviecbochang.Alias,
                                ObjectSort = congviecbochang.Sort,
                                ObjectPrice = bochangPrice,
                                Value = bochangValue,
                                Amount = bochangAmount
                            });
                        }
                        columnIndex++;
                        #endregion

                        columnIndex++;
                        #region CONG VIEC KHAC
                        foreach (var congvieckhac in congvieckhacs)
                        {
                            // Get value
                            decimal objectPrice = congvieckhac.Price;
                            double objectValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                            tongthoigiancongvieckhac += objectValue;
                            decimal objectAmount = Convert.ToDecimal(objectValue * (double)objectPrice);
                            tongcongcongvieckhac += objectAmount;

                            // Insert db
                            var exist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(congvieckhac.Code) && m.Type.Equals((int)EDinhMuc.CongViecKhac) && m.Mode.Equals((int)EMode.TrongGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (exist != null)
                            {
                                var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, exist.Id);
                                var update = Builders<FactoryProductCongTheoThang>.Update
                                    .Set(m => m.ObjectPrice, objectPrice)
                                    .Set(m => m.Value, objectValue)
                                    .Set(m => m.Amount, objectAmount);
                                dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                            }
                            else
                            {
                                dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                                {
                                    Month = month,
                                    Year = year,
                                    Type = (int)EDinhMuc.CongViecKhac,
                                    Mode = (int)EMode.TrongGio,
                                    EmployeeId = employee.Id,
                                    EmployeeCode = employee.CodeOld,
                                    EmployeeName = employee.FullName,
                                    EmployeeAlias = employee.AliasFullName,
                                    ObjectId = congvieckhac.Id,
                                    ObjectCode = congvieckhac.Code,
                                    ObjectName = congvieckhac.Name,
                                    ObjectAlias = congvieckhac.Alias,
                                    ObjectSort = congvieckhac.Sort,
                                    ObjectPrice = objectPrice,
                                    Value = objectValue,
                                    Amount = objectAmount
                                });
                            }
                            columnIndex++;
                        }
                        #endregion

                        thanhtientronggio = thanhtienthanhpham + thanhtienbochang + Convert.ToDecimal((double)tongcongcongvieckhac / 60);

                        #region Update to CONG TONG
                        var existCongTong = dbContext.EmployeeCongs.Find(m => m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                        if (existCongTong != null)
                        {
                            var filterCongTong = Builders<EmployeeCong>.Filter.Eq(m => m.Id, existCongTong.Id);
                            var updateCongTong = Builders<EmployeeCong>.Update
                                .Set(m => m.TongThoiGianCongViecKhacTrongGio, tongthoigiancongvieckhac / 60)
                                .Set(m => m.TongCongCongViecKhacTrongGio, tongcongcongvieckhac)
                                .Set(m => m.ThanhTienTrongGio, thanhtientronggio)
                                .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                            dbContext.EmployeeCongs.UpdateOne(filterCongTong, updateCongTong);
                        }
                        else
                        {
                            dbContext.EmployeeCongs.InsertOne(new EmployeeCong()
                            {
                                Month = month,
                                Year = year,
                                EmployeeId = employee.Id,
                                EmployeeCode = employee.CodeOld,
                                EmployeeName = employee.FullName,
                                TongThoiGianCongViecKhacTrongGio = tongthoigiancongvieckhac / 60,
                                TongCongCongViecKhacTrongGio = tongcongcongvieckhac,
                                ThanhTienTrongGio = thanhtientronggio
                            });
                        }
                        #endregion
                    }

                    return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Production + "/" + Constants.LinkSalary.TongHopTrongGio + "?thang=" + month + "-" + year });
                }
            }
            else
            {
                return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Production + "/" + Constants.LinkSalary.TongHopTrongGio });
            }
        }

        [Route(Constants.LinkSalary.TongHopNgoaiGio)]
        public async Task<IActionResult> TongHopNgoaiGio(string thang, string id)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongSX, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL
            var employeeDdl = await dbContext.Employees.Find(m => m.Enable.Equals(true) && m.SalaryType.Equals((int)EKhoiLamViec.SX) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
            #endregion

            #region Times
            var toDate = Utility.GetSalaryToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            thang = string.IsNullOrEmpty(thang) ? month + "-" + year : thang;
            #endregion

            #region Setting
            var mauSo = 26;
            var mauSoBaoVe = 27;
            var mauSoChuyenCan = 1;
            var mauSoTienCom = 1;
            decimal ibhxh = 8;
            decimal ibhyt = (decimal)1.5;
            decimal ibhtn = 1;
            #endregion

            #region Filter
            var builder = Builders<FactoryProductCongTheoThang>.Filter;
            var filter = builder.Eq(m => m.Mode, (int)EMode.NgoaiGio) & builder.Eq(m => m.Month, month) & builder.Eq(m => m.Year, year);
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.EmployeeId, id.Trim());
            }
            #endregion

            #region Sort
            //var sortBuilder = Builders<FactoryProductCongTheoThang>.Sort.Ascending(m => m.Code);
            //var congMs = await dbContext.FactoryProductCongTheoThangs.Find(filter).Sort(sortBuilder).ToListAsync();
            #endregion

            var congMs = await dbContext.FactoryProductCongTheoThangs.Find(filter).ToListAsync();

            var thanhphams = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToListAsync();
            var congviecs = await dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true) && m.Main.Equals(false)).SortBy(m => m.Sort).ToListAsync();
            var dongiaDMs = await dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToListAsync();
            var dongiaM3 = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            if (dongiaM3 == null)
            {
                dongiaM3 = new FactoryProductDonGiaM3
                {
                    Month = month,
                    Year = year,
                    Type = (int)EDinhMuc.BocVac,
                    Price = 11300
                };
                dbContext.FactoryProductDonGiaM3s.InsertOne(dongiaM3);
            }
            var viewModel = new BangLuongViewModel
            {
                MCongs = congMs,
                MonthYears = Utility.DllMonths(),
                Thang = thang,
                Employees = employeeDdl,
                Id = id,
                ThanhPhams = thanhphams,
                CongViecs = congviecs,
                DonGiaDMs = dongiaDMs,
                DonGiaM3 = dongiaM3
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.SanXuatTongHopNgoaiGioTemplate)]
        public async Task<IActionResult> SanXuatTongHopNgoaiGioTemplate(string fileName, string thang)
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
            var toDate = Utility.GetSalaryToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            var thangFormat = month.ToString("00") + "-" + year.ToString("0000");
            #endregion

            var thanhphams = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToListAsync();
            var congviecs = await dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true) && m.Main.Equals(false)).SortBy(m => m.Sort).ToListAsync();
            var dongiathanhphans = await dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToListAsync();
            var dongiaM3entity = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
            var dongiaM3 = dongiaM3entity != null ? dongiaM3entity.Price : 11300;

            var tangcaentity = dbContext.FactoryProductDinhMucTangCas.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
            double tangcatile = tangcaentity != null ? (1 + tangcaentity.PhanTramTangCa / 100) : 1.1;
            var dongiaM3TangCa = Convert.ToDecimal((double)dongiaM3 * tangcatile);

            int thanhphamCount = thanhphams.Count();
            int congviecCount = congviecs.Count();

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"" + thangFormat + "-tong-hop-ngoai-gio";
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
                #endregion

                ISheet sheet1 = workbook.CreateSheet("Tổng hợp ngoài giờ");

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

                #region Header
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 3;
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + thanhphamCount);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP CÔNG ĐÓNG BAO");
                cell.CellStyle = styleTitle;
                columnIndex = columnIndex + thanhphamCount;
                columnIndex++;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP BỐC HÀNG");
                cell.CellStyle = styleTitle;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + congviecCount);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG HỢP CÔNG VIỆC KHÁC");
                cell.CellStyle = styleTitle;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                rowIndex++;
                #endregion

                #region Row 10
                int totalColumn = 3 + thanhphamCount + 1 + 1 + 1 + congviecCount;
                columnIndex = 0;
                for (var i = 1; i <= totalColumn; i++)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(i);
                    cell.CellStyle = styleHeader;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 11
                row = sheet1.CreateRow(rowIndex);
                row.Height = 2000;
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TÊN SẢN PHẨM");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in thanhphams)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(thanhpham.Name);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleHeader;
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Số M3");
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleHeader;
                sheet1.SetColumnWidth(columnIndex, 400);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var congviec in congviecs)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(congviec.Name);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 12
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MÃ SẢN PHẨM");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in thanhphams)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(thanhpham.Code);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                for (var i12 = columnIndex; i12 < totalColumn; i12++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 13
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("ĐƠN GIÁ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex = columnIndex + 1;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                foreach (var thanhpham in dongiathanhphans)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue((double)Math.Round(thanhpham.DonGiaTangCa, 0));
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.Numeric);
                cell.SetCellValue((double)Math.Round(dongiaM3TangCa,0));
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;
                foreach (var congviec in congviecs)
                {
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue((double)Math.Round(((double)congviec.Price * tangcatile), 0));
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                #region Row 14
                row = sheet1.CreateRow(rowIndex);
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
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                var toTP = columnIndex + thanhphamCount;
                for (var i14 = columnIndex; i14 < toTP; i14++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorGreen;
                    columnIndex++;
                }
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellM3;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = cellStyleBorderAndColorDarkBlue;
                columnIndex++;

                for (var i14 = columnIndex; i14 < totalColumn; i14++)
                {
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(string.Empty);
                    cell.CellStyle = cellStyleBorderAndColorBlue;
                    columnIndex++;
                }
                rowIndex++;
                #endregion

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkSalary.SanXuatTongHopNgoaiGioPost)]
        [HttpPost]
        public ActionResult SanXuatTongHopNgoaiGioPost()
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
                    var endMonthDate = Utility.GetSalaryToDate(time);
                    var month = endMonthDate.Month;
                    var year = endMonthDate.Year;

                    var priceRow = sheet.GetRow(11);

                    var thanhphams = dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToList();
                    var congviecs = dbContext.FactoryCongViecs.Find(m => m.Enable.Equals(true)).ToList();
                    var congvieckhacs = congviecs.Where(m => m.Main.Equals(false)).OrderBy(m => m.Sort).ToList();
                    var congviecbochang = congviecs.Find(m => m.Main.Equals(true) && m.Alias.Equals("boc-hang"));

                    var dongiathanhphans = dbContext.FactoryProductDinhMucs.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EProductType.TP) && m.Year.Equals(year) && m.Month.Equals(month)).SortBy(m => m.Sort).ToList();
                    var dongiaM3entity = dbContext.FactoryProductDonGiaM3s.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Year.Equals(year) && m.Month.Equals(month)).FirstOrDefault();
                    var dongiaM3 = dongiaM3entity != null ? dongiaM3entity.Price : 11300;

                    var tangcaentity = dbContext.FactoryProductDinhMucTangCas.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                    double tangcatile = tangcaentity != null ? (1 + tangcaentity.PhanTramTangCa / 100) : 1.1;
                    var dongiaM3TangCa = Convert.ToDecimal((double)dongiaM3 * tangcatile);

                    int thanhphamCount = thanhphams.Count();
                    int congviecCount = congviecs.Count();

                    for (int i = 13; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var columnIndex = 0;
                        var code = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        var fullName = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        columnIndex++;

                        if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(fullName))
                        {
                            continue;
                        }

                        var employee = !string.IsNullOrEmpty(fullName) ? dbContext.Employees.Find(m => m.Enable.Equals(true) & m.FullName.Equals(fullName)).FirstOrDefault() : null;
                        //if (employee == null)
                        //{
                        //    employee = !string.IsNullOrEmpty(code) ? dbContext.Employees.Find(m => m.Enable.Equals(true) & m.CodeOld.Equals(code)).FirstOrDefault() : null;
                        //}
                        if (employee == null)
                        {
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "tong-hop-ngoai-gio",
                                Object = "Code: " + code + " | Name: " + fullName + " | Line " + i,
                                Error = "Khong tim thay thong tin",
                                DateTime = DateTime.Now.ToString()
                            });
                            continue;
                        }

                        double tongthoigiancongvieckhac = 0;
                        decimal thanhtientongcongcongvieckhac = 0;
                        decimal thanhtienthanhpham = 0;
                        decimal thanhtienbochang = 0;
                        decimal thanhtienngoaigio = 0;

                        #region THANH PHAM
                        foreach (var thanhpham in thanhphams)
                        {
                            var dongiathanhphamE = dongiathanhphans.Where(m => m.ProductId.Equals(thanhpham.Id)).FirstOrDefault();
                            // Get value
                            decimal objectPrice = dongiathanhphamE != null ? dongiathanhphamE.DonGiaTangCa : 0;
                            double objectValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                            decimal objectAmount = Convert.ToDecimal(objectValue * (double)objectPrice);
                            thanhtienthanhpham += objectAmount;
                            // Insert db
                            var exist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(thanhpham.Code) && m.Type.Equals((int)EDinhMuc.DongGoi) && m.Mode.Equals((int)EMode.NgoaiGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (exist != null)
                            {
                                var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, exist.Id);
                                var update = Builders<FactoryProductCongTheoThang>.Update
                                    .Set(m => m.ObjectPrice, objectPrice)
                                    .Set(m => m.Value, objectValue)
                                    .Set(m => m.Amount, objectAmount);
                                dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                            }
                            else
                            {
                                dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                                {
                                    Month = month,
                                    Year = year,
                                    Type = (int)EDinhMuc.DongGoi,
                                    Mode = (int)EMode.NgoaiGio,
                                    EmployeeId = employee.Id,
                                    EmployeeCode = employee.CodeOld,
                                    EmployeeName = employee.FullName,
                                    EmployeeAlias = employee.AliasFullName,
                                    ObjectId = thanhpham.Id,
                                    ObjectCode = thanhpham.Code,
                                    ObjectName = thanhpham.Name,
                                    ObjectAlias = thanhpham.Alias,
                                    ObjectSort = thanhpham.Sort,
                                    ObjectPrice = objectPrice,
                                    Value = objectValue,
                                    Amount = objectAmount
                                });
                            }
                            columnIndex++;
                        }
                        #endregion
                        columnIndex++;

                        #region BOC HANG
                        decimal bochangPrice = Convert.ToDecimal((double)congviecbochang.Price * 1.1);
                        double bochangValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        decimal bochangAmount = Convert.ToDecimal(bochangValue * (double)bochangPrice);
                        thanhtienbochang += bochangAmount;

                        // Insert db
                        var bochangexist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(congviecbochang.Code) && m.Type.Equals((int)EDinhMuc.BocVac) && m.Mode.Equals((int)EMode.NgoaiGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                        if (bochangexist != null)
                        {
                            var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, bochangexist.Id);
                            var update = Builders<FactoryProductCongTheoThang>.Update
                                .Set(m => m.ObjectPrice, bochangPrice)
                                .Set(m => m.Value, bochangValue)
                                .Set(m => m.Amount, bochangAmount);
                            dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                        }
                        else
                        {
                            dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                            {
                                Month = month,
                                Year = year,
                                Type = (int)EDinhMuc.BocVac,
                                Mode = (int)EMode.NgoaiGio,
                                EmployeeId = employee.Id,
                                EmployeeCode = employee.CodeOld,
                                EmployeeName = employee.FullName,
                                EmployeeAlias = employee.AliasFullName,
                                ObjectId = congviecbochang.Id,
                                ObjectCode = congviecbochang.Code,
                                ObjectName = congviecbochang.Name,
                                ObjectAlias = congviecbochang.Alias,
                                ObjectSort = congviecbochang.Sort,
                                ObjectPrice = bochangPrice,
                                Value = bochangValue,
                                Amount = bochangAmount
                            });
                        }
                        columnIndex++;
                        #endregion

                        columnIndex++;
                        #region CONG VIEC KHAC
                        foreach (var congvieckhac in congvieckhacs)
                        {
                            // Get value
                            decimal objectPrice = Convert.ToDecimal((double)congvieckhac.Price * 1.1);
                            double objectValue = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                            tongthoigiancongvieckhac += objectValue;
                            decimal objectAmount = Convert.ToDecimal(objectValue * (double)objectPrice);
                            thanhtientongcongcongvieckhac += objectAmount;

                            // Insert db
                            var exist = dbContext.FactoryProductCongTheoThangs.Find(m => m.EmployeeId.Equals(employee.Id) && m.ObjectCode.Equals(congvieckhac.Code) && m.Type.Equals((int)EDinhMuc.CongViecKhac) && m.Mode.Equals((int)EMode.NgoaiGio) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                            if (exist != null)
                            {
                                var filter = Builders<FactoryProductCongTheoThang>.Filter.Eq(m => m.Id, exist.Id);
                                var update = Builders<FactoryProductCongTheoThang>.Update
                                    .Set(m => m.ObjectPrice, objectPrice)
                                    .Set(m => m.Value, objectValue)
                                    .Set(m => m.Amount, objectAmount);
                                dbContext.FactoryProductCongTheoThangs.UpdateOne(filter, update);
                            }
                            else
                            {
                                dbContext.FactoryProductCongTheoThangs.InsertOne(new FactoryProductCongTheoThang()
                                {
                                    Month = month,
                                    Year = year,
                                    Type = (int)EDinhMuc.CongViecKhac,
                                    Mode = (int)EMode.NgoaiGio,
                                    EmployeeId = employee.Id,
                                    EmployeeCode = employee.CodeOld,
                                    EmployeeName = employee.FullName,
                                    EmployeeAlias = employee.AliasFullName,
                                    ObjectId = congvieckhac.Id,
                                    ObjectCode = congvieckhac.Code,
                                    ObjectName = congvieckhac.Name,
                                    ObjectAlias = congvieckhac.Alias,
                                    ObjectSort = congvieckhac.Sort,
                                    ObjectPrice = objectPrice,
                                    Value = objectValue,
                                    Amount = objectAmount
                                });
                            }
                            columnIndex++;
                        }
                        #endregion

                        // Round here if true.
                        thanhtienngoaigio = thanhtienthanhpham + thanhtienbochang + Convert.ToDecimal((double)thanhtientongcongcongvieckhac / 60);

                        #region Update to CONG TONG
                        var existCongTong = dbContext.EmployeeCongs.Find(m => m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                        if (existCongTong != null)
                        {
                            var filterCongTong = Builders<EmployeeCong>.Filter.Eq(m => m.Id, existCongTong.Id);
                            var updateCongTong = Builders<EmployeeCong>.Update
                                .Set(m => m.TongThoiGianCongViecKhacNgoaiGio, tongthoigiancongvieckhac / 60)
                                .Set(m => m.TongCongCongViecKhacNgoaiGio, thanhtientongcongcongvieckhac)
                                .Set(m => m.ThanhTienNgoaiGio, thanhtienngoaigio)
                                .Set(m => m.UpdatedOn, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                            dbContext.EmployeeCongs.UpdateOne(filterCongTong, updateCongTong);
                        }
                        else
                        {
                            dbContext.EmployeeCongs.InsertOne(new EmployeeCong()
                            {
                                Month = month,
                                Year = year,
                                EmployeeId = employee.Id,
                                EmployeeCode = employee.CodeOld,
                                EmployeeName = employee.FullName,
                                TongThoiGianCongViecKhacNgoaiGio = tongthoigiancongvieckhac / 60,
                                TongCongCongViecKhacNgoaiGio = thanhtientongcongcongvieckhac,
                                ThanhTienNgoaiGio = thanhtienngoaigio
                            });
                        }
                        #endregion
                    }

                    return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Production + "/" + Constants.LinkSalary.TongHopNgoaiGio + "?thang=" + month + "-" + year });
                }
            }
            else
            {
                return Json(new { url = "/" + Constants.LinkSalary.Main + "/" + Constants.LinkSalary.Production + "/" + Constants.LinkSalary.TongHopNgoaiGio });
            }
        }
        #endregion

        #region DINH MUC : In datafix InitFactoryProductDinhMucTangCa, ...InitFactoryCongViec
        [Route(Constants.LinkSalary.DinhMuc)]
        public async Task<IActionResult> DinhMuc(string thang, string id)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.LuongSX, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region DDL
            var productsDdl = await dbContext.FactoryProducts.Find(m => m.Enable.Equals(true)).SortBy(m => m.Sort).ToListAsync();
            #endregion

            #region Times
            var toDate = Utility.GetSalaryToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            #endregion

            #region Filter
            var builder = Builders<FactoryProductDinhMuc>.Filter;
            var filter = builder.Eq(m => m.Month, month) & builder.Eq(m => m.Year, year);
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.ProductCode, id.Trim());
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<FactoryProductDinhMuc>.Sort.Ascending(m => m.Sort);
            #endregion

            var donGiaDinhMucs = await dbContext.FactoryProductDinhMucs.Find(filter).ToListAsync();
            var donGiaDinhMucFulls = new List<FactoryProductDinhMucViewModel>();
            foreach (var dongia in donGiaDinhMucs)
            {
                var product = dbContext.FactoryProducts.Find(m => m.Id.Equals(dongia.ProductId)).FirstOrDefault();
                if (product != null)
                {
                    donGiaDinhMucFulls.Add(new FactoryProductDinhMucViewModel()
                    {
                        Id = dongia.Id,
                        Month = dongia.Month,
                        Year = dongia.Year,
                        Type = dongia.Type,
                        ProductId = dongia.ProductId,
                        ProductCode = dongia.ProductCode,
                        ProductName = product.Name,
                        ProductGroup = product.Group,
                        ProductUnit = product.Unit,
                        Sort = dongia.Sort,
                        SoBaoNhomNgay = dongia.SoBaoNhomNgay,
                        DinhMucTheoNgay = dongia.DinhMucTheoNgay,
                        DinhMucGioQuiDinh = dongia.DinhMucGioQuiDinh,
                        DinhMucTheoGio = dongia.DinhMucTheoGio,
                        DonGia = dongia.DonGia,
                        DonGiaDieuChinh = dongia.DonGiaDieuChinh,
                        DonGiaTangCaPhanTram = dongia.DonGiaTangCaPhanTram,
                        DonGiaTangCa = dongia.DonGiaTangCa,
                        DonGiaM3 = dongia.DonGiaM3,
                        DonGiaTangCaM3 = dongia.DonGiaTangCaM3
                    });
                }
            }
            var tiLeDinhMucs = await dbContext.FactoryProductDinhMucTiLes.Find(m => m.Month.Equals(month) && m.Year.Equals(year)).SortBy(m => m.TiLe).ToListAsync();
            var viewModel = new BangLuongViewModel
            {
                DonGiaDMFulls = donGiaDinhMucFulls,
                TiLeDMs = tiLeDinhMucs,
                MonthYears = Utility.DllMonths(),
                Thang = thang,
                ThanhPhams = productsDdl,
                Id = id
            };

            return View(viewModel);
        }
        #endregion

        #region DONG GOI
        [Route(Constants.LinkSalary.DongGoiTrongGio)]
        public IActionResult DongGoiTrongGio(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.DongGoiNgoaiGio)]
        public IActionResult DongGoiNgoaiGio(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }
        #endregion

        #region BOC HANG
        [Route(Constants.LinkSalary.BocHangTrongGio)]
        public IActionResult BocHangTrongGio(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.BocHangNgoaiGio)]
        public IActionResult BocHangNgoaiGio(string thang)
        {
            var viewModel = new BangLuongViewModel
            {
                Thang = thang
            };

            return View(viewModel);
        }
        #endregion
    }
}
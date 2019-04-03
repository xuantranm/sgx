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
using NPOI.HSSF.Util;
using NPOI.SS.Util;

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.KeHoachTongHopLink.Main)]
    public class KeHoachTongHopController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        readonly IHostingEnvironment _env;

        private readonly ILogger _logger;

        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public IConfiguration Configuration { get; }

        public KeHoachTongHopController(
            IConfiguration configuration,
            IHostingEnvironment env,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILogger<KeHoachTongHopController> logger)
        {
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region KHO NGUYEN LIEU
        [Route(Constants.KeHoachTongHopLink.DuLieuKhoNguyenLieu)]
        public async Task<IActionResult> DuLieuKhoNguyenLieu(DateTime Tu, DateTime Den, string Hang, string TrangThai, int Trang, string SapXep, string ThuTu)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoNguyenLieuDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            var linkCurrent = string.Empty;

            #region Times
            var today = DateTime.Now.Date;
            if (Tu < Constants.MinDate)
            {
                Tu = today;
            }
            if (Den < Constants.MinDate)
            {
                Den = today;
            }
            if (Den < Tu)
            {
                Tu = Den;
            }
            linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
            linkCurrent += "Tu=" + Tu.ToString("MM-dd-yyyy") + "&Den=" + Den.ToString("MM-dd-yyyy");
            #endregion

            var type = (int)EKho.NguyenLieu;
            var trangthais = dbContext.TrangThais.Find(m => m.Enable.Equals(true) && m.TypeId.Equals((int)ETrangThai.Kho)).ToList();
            var products = dbContext.Products.Find(m => m.Enable.Equals(true) && m.TypeId.Equals(type)).ToList();

            #region Filter
            var builder = Builders<KhoNguyenLieu>.Filter;
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);
            if (!string.IsNullOrEmpty(Hang))
            {
                filter = filter & builder.Eq(m => m.ProductId, Hang);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Hang=" + Hang;
            }
            if (!string.IsNullOrEmpty(TrangThai))
            {
                filter = filter & builder.Eq(m => m.TrangThai, TrangThai);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "TrangThai=" + Hang;
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<KhoNguyenLieu>.Sort.Ascending(m => m.Date);
            SapXep = string.IsNullOrEmpty(SapXep) ? "ngay" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "san-pham":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoNguyenLieu>.Sort.Ascending(m => m.ProductAlias) : Builders<KhoNguyenLieu>.Sort.Descending(m => m.ProductAlias);
                    break;
                case "ma-san-pham":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoNguyenLieu>.Sort.Ascending(m => m.ProductCode) : Builders<KhoNguyenLieu>.Sort.Descending(m => m.ProductCode);
                    break;
                case "ton":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoNguyenLieu>.Sort.Ascending(m => m.TonCuoi) : Builders<KhoNguyenLieu>.Sort.Descending(m => m.TonCuoi);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<KhoNguyenLieu>.Sort.Ascending(m => m.Date) : Builders<KhoNguyenLieu>.Sort.Descending(m => m.Date);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            var settingPage = dbContext.Settings.Find(m => m.Type.Equals("system") && m.Key.Equals("page-size-khth")).FirstOrDefault();
            int Size = settingPage != null ? Convert.ToInt32(settingPage.Value) : 100;
            int pages = 1;
            var records = dbContext.KhoNguyenLieus.CountDocuments(filter);

            #region Get lastest data
            if (records == 0)
            {
                var lastestE = dbContext.KhoNguyenLieus.Find(m => true).SortByDescending(m => m.Date).Limit(1).FirstOrDefault();
                if (lastestE != null)
                {
                    Tu = lastestE.Date;
                    Den = lastestE.Date;
                    linkCurrent = "?Tu=" + Tu.ToString("MM-dd-yyyy") + "&Den=" + Den.ToString("MM-dd-yyyy");
                    filter = builder.Eq(m => m.Date, lastestE.Date);
                    records = dbContext.KhoNguyenLieus.CountDocuments(filter);
                }
            }
            #endregion

            var enablePage = false;
            if (records > 0 && records > Size)
            {
                enablePage = true;
                pages = (int)Math.Ceiling((double)records / (double)Size);
                if (Trang > pages)
                {
                    Trang = 1;
                }
            }

            var list = new List<KhoNguyenLieu>();
            if (enablePage)
            {
                list = dbContext.KhoNguyenLieus.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * Size).Limit(Size).ToList();
            }
            else
            {
                list = dbContext.KhoNguyenLieus.Find(filter).Sort(sortBuilder).ToList();
            }

            var viewModel = new KhoViewModel()
            {
                Name = "Kho nguyên liệu",
                KhoNguyenLieus = list,
                TrangThais = trangthais,
                Products = products,
                Tu = Tu,
                Den = Den,
                Hang = Hang,
                TrangThai = TrangThai,
                LinkCurrent = linkCurrent,
                ThuTu = ThuTu,
                SapXep = SapXep,
                Records = (int)records,
                PageSize = Size,
                SoTrang = pages,
                Trang = Trang
            };
            return View(viewModel);
        }

        [Route(Constants.KeHoachTongHopLink.DuLieuKhoNguyenLieu + "/" + Constants.ActionLink.Export)]
        public async Task<IActionResult> DuLieuKhoNguyenLieuExport(DateTime Tu, DateTime Den, string Hang, string TrangThai)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoNguyenLieuDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var today = DateTime.Now.Date;
            if (Tu < Constants.MinDate)
            {
                Tu = today;
            }
            if (Den < Constants.MinDate || Den < Tu)
            {
                Den = Tu;
            }
            #endregion

            string sFileName = @"kho-nguyen-lieu";

            var builder = Builders<KhoNguyenLieu>.Filter;
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);
            if (!string.IsNullOrEmpty(Hang))
            {
                filter = filter & builder.Eq(m => m.ProductId, Hang);
                sFileName += "-" + Hang;
            }
            if (!string.IsNullOrEmpty(TrangThai))
            {
                filter = filter & builder.Eq(m => m.TrangThai, TrangThai);
                sFileName += "-" + TrangThai;
            }
            var khonguyenlieus = dbContext.KhoNguyenLieus.Find(filter).SortBy(m => m.Date).SortBy(m => m.ProductName).ToList();

            var duration = Tu.ToString("ddMMyyyy") + "-" + Den.ToString("ddMMyyyy");
            sFileName += "-" + duration;
            sFileName += "-v" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx";

            string exportFolder = Path.Combine(_env.WebRootPath, "exports", "kho", "kho-nguyen-lieu", today.ToString("yyyyMMdd"));

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            file.Directory.Create(); // If the directory already exists, this method does nothing.

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
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                //cellStyleBorderAndColorLightGreen.WrapText = false;
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
                //styleSmall.CloneStyleFrom(styleBorder);
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

                ISheet sheet1 = workbook.CreateSheet("DATA KHO NVL");

                #region Introduce
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
                #endregion

                row = sheet1.CreateRow(rowIndex);
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("DỮ LIỆU KHO NGUYÊN LIỆU");
                cell.CellStyle = styleTitle;
                rowIndex++;

                #region Title
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NGÀY");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TÊN SẢN PHẨM (SP)");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MÃ SP");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("ĐVT");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỒN ĐẦU");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NHẬP TỪ NCC");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NHẬP TỪ SX");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NHẬP HAO HỤT");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NHẬP CHUYỂN MÃ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG NHẬP");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XUẤT TRẢ NCC");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XUẤT CHO NHÀ MÁY");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XUẤT LOGISTICS");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XUẤT HAO HỤT");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XUẤT CHUYỂN MÃ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỔNG XUẤT");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỒN CUỐI");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỒN AN TOÀN");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("GHI CHÚ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;
                rowIndex++;
                #endregion

                #region Content
                foreach (var item in khonguyenlieus)
                {
                    row = sheet1.CreateRow(rowIndex);
                    columnIndex = 0;
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Date.ToString("dd/MM/yyyy"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ProductName);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ProductCode);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ProductUnit);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TonDau);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.NhapTuNCC);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.NhapTuSanXuat);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.NhapHaoHut);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.NhapChuyenMa);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TongNhap);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuatTraNCC);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuatChoNhaMay);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuatLogistics);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuatHaoHut);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuatChuyenMa);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TongXuat);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TonCuoi);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TonAnToan);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Note);
                    cell.CellStyle = styleDedault;

                    rowIndex++;
                }
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

        [Route(Constants.KeHoachTongHopLink.DuLieuKhoNguyenLieu + "/" + Constants.ActionLink.Template)]
        public async Task<IActionResult> DuLieuKhoNguyenLieuTemplate(DateTime Date)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoNguyenLieuDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            Date = Date.Year > 1990 ? Date : DateTime.Now;
            string exportFolder = Path.Combine(_env.WebRootPath, "exports", "kho", "nguyen-lieu");
            string sFileName = @"nguyen-lieu-" + Date.ToString("dd-MM-yyyy") + "-v" + Date.ToString("HHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            file.Directory.Create(); // If the directory already exists, this method does nothing.

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
            //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
            cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
            //cellStyleBorderAndColorLightGreen.WrapText = false;
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
            //styleSmall.CloneStyleFrom(styleBorder);
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

            ISheet sheet1 = workbook.CreateSheet("DATA KHO NVL");

            #region Introduce
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
            #endregion

            #region Notice
            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Thông tin được cập nhật từ dòng số " + (rowIndex + 5));
            cell.CellStyle = styleSmall;
            rowIndex++;

            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Vì tính chất quan trọng, dữ liệu sau khi được đưa lên không được chỉnh sửa.");
            cell.CellStyle = styleSmall;
            rowIndex++;

            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Thay đổi dữ liệu ở cấp bậc được phép. Theo cách thức tạo dữ liệu mới bù trừ với dữ liệu cần thay đổi.");
            cell.CellStyle = styleSmall;
            rowIndex++;
            #endregion

            row = sheet1.CreateRow(rowIndex);
            CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
            sheet1.AddMergedRegion(cellRangeAddress);
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("DỮ LIỆU KHO NGUYÊN LIỆU");
            cell.CellStyle = styleTitle;
            rowIndex++;

            #region Title
            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NGÀY");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TÊN SẢN PHẨM (SP)");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("MÃ SP");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("ĐVT");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỒN ĐẦU");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NHẬP TỪ NCC");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NHẬP TỪ SX");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NHẬP HAO HỤT");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NHẬP CHUYỂN MÃ");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỔNG NHẬP");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XUẤT TRẢ NCC");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XUẤT CHO NHÀ MÁY");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XUẤT LOGISTICS");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XUẤT HAO HỤT");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XUẤT CHUYỂN MÃ");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỔNG XUẤT");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỒN CUỐI");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỒN AN TOÀN");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("GHI CHÚ");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;
            #endregion

            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [HttpPost]
        [Route(Constants.KeHoachTongHopLink.DuLieuKhoNguyenLieu + "/" + Constants.ActionLink.Template + "/" + Constants.ActionLink.Post)]
        public IActionResult DuLieuKhoNguyenLieuTemplatePost()
        {
            var Errors = new List<string>();
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

                    for (int i = 9; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var productname = string.Empty;
                        var productcode = string.Empty;
                        var productunit = string.Empty;
                        double tondau = 0;
                        double nhaptuncc = 0;
                        double nhaptusx = 0;
                        double nhaphaohut = 0;
                        double nhapchuyenma = 0;
                        double tongnhap = 0;
                        double xuattrancc = 0;
                        double xuatchonhamay = 0;
                        double xuatlogistics = 0;
                        double xuathaohut = 0;
                        double xuatchuyenma = 0;
                        double tongxuat = 0;
                        double toncuoi = 0;
                        double tonantoan = 0;
                        var note = string.Empty;

                        int columnIndex = 0;
                        var ngay = Utility.GetDateCellValue2(row.GetCell(columnIndex));
                        columnIndex++;
                        productname = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        productcode = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        productunit = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tondau = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nhaptuncc = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nhaptusx = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nhaphaohut = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nhapchuyenma = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tongnhap = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xuattrancc = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xuatchonhamay = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xuatlogistics = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xuathaohut = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xuatchuyenma = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tongxuat = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        toncuoi = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tonantoan = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        note = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;

                        if (!ngay.HasValue || string.IsNullOrEmpty(productname))
                        {
                            Errors.Add("Thiếu thông tin [Ngày] và [Tên] ở dòng: " + (i + 1));
                            continue;
                        }
                        var existDataNguyenLieu = dbContext.KhoNguyenLieus.Find(m => m.Date.Equals(ngay) && m.ProductName.Equals(productname)).FirstOrDefault();
                        if (existDataNguyenLieu == null)
                        {
                            InitProduct((int)EKho.NguyenLieu, productname, productcode, productunit, out string alias, out string productid);
                            var nguyenlieu = new KhoNguyenLieu()
                            {
                                Date = ngay.Value,
                                ProductId = productid,
                                ProductName = productname,
                                ProductCode = productcode,
                                ProductAlias = alias,
                                ProductUnit = productunit,
                                TonDau = tondau,
                                NhapTuNCC = nhaptuncc,
                                NhapTuSanXuat = nhaptusx,
                                NhapHaoHut = nhaphaohut,
                                NhapChuyenMa = nhapchuyenma,
                                TongNhap = tongnhap,
                                XuatTraNCC = xuattrancc,
                                XuatChoNhaMay = xuatchonhamay,
                                XuatLogistics = xuatlogistics,
                                XuatHaoHut = xuathaohut,
                                XuatChuyenMa = xuatchuyenma,
                                TongXuat = tongxuat,
                                TonCuoi = toncuoi,
                                TonAnToan = tonantoan,
                                Note = note
                            };
                            dbContext.KhoNguyenLieus.InsertOne(nguyenlieu);
                        }
                        else
                        {
                            Errors.Add("Dữ liệu đã tồn tại. Không thể cập nhật dữ liệu mới ở dòng: " + (i + 1));
                        }
                    }
                }
            }

            return Json(new { result = true, Errors });
        }
        #endregion

        #region KHO THANH PHAM
        [Route(Constants.KeHoachTongHopLink.DuLieuKhoThanhPham)]
        public async Task<IActionResult> DuLieuKhoThanhPham(DateTime Tu, DateTime Den, string Hang, string TrangThai, int Trang, string SapXep, string ThuTu)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoThanhPhamDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            var linkCurrent = string.Empty;

            #region Times
            var today = DateTime.Now.Date;
            if (Tu < Constants.MinDate)
            {
                Tu = today;
            }
            if (Den < Constants.MinDate || Den < Tu)
            {
                Den = Tu;
            }
            linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
            linkCurrent += "Tu=" + Tu.ToString("MM-dd-yyyy") + "&Den=" + Den.ToString("MM-dd-yyyy");
            #endregion

            var type = (int)EKho.ThanhPham;
            var trangthais = dbContext.TrangThais.Find(m => m.Enable.Equals(true) && m.TypeId.Equals((int)ETrangThai.Kho)).ToList();
            var products = dbContext.Products.Find(m => m.Enable.Equals(true) && m.TypeId.Equals(type)).ToList();

            #region Filter
            var builder = Builders<KhoThanhPham>.Filter;
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);
            if (!string.IsNullOrEmpty(Hang))
            {
                filter = filter & builder.Eq(m => m.ProductId, Hang);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Hang=" + Hang;
            }
            if (!string.IsNullOrEmpty(TrangThai))
            {
                filter = filter & builder.Eq(m => m.TrangThai, TrangThai);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "TrangThai=" + Hang;
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<KhoThanhPham>.Sort.Ascending(m => m.Date);
            SapXep = string.IsNullOrEmpty(SapXep) ? "ngay" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "san-pham":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoThanhPham>.Sort.Ascending(m => m.ProductAlias) : Builders<KhoThanhPham>.Sort.Descending(m => m.ProductAlias);
                    break;
                case "ma-san-pham":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoThanhPham>.Sort.Ascending(m => m.ProductCode) : Builders<KhoThanhPham>.Sort.Descending(m => m.ProductCode);
                    break;
                case "ton":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoThanhPham>.Sort.Ascending(m => m.TonCuoi) : Builders<KhoThanhPham>.Sort.Descending(m => m.TonCuoi);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<KhoThanhPham>.Sort.Ascending(m => m.Date) : Builders<KhoThanhPham>.Sort.Descending(m => m.Date);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            var settingPage = dbContext.Settings.Find(m => m.Type.Equals("system") && m.Key.Equals("page-size-khth")).FirstOrDefault();
            int Size = settingPage != null ? Convert.ToInt32(settingPage.Value) : 100;
            int pages = 1;

            var records = dbContext.KhoThanhPhams.CountDocuments(filter);

            #region Get lastest data
            if (records == 0)
            {
                var lastestE = dbContext.KhoThanhPhams.Find(m => true).SortByDescending(m => m.Date).Limit(1).FirstOrDefault();
                if (lastestE != null)
                {
                    Tu = lastestE.Date;
                    Den = lastestE.Date;
                    linkCurrent = "?Tu=" + Tu.ToString("MM-dd-yyyy") + "&Den=" + Den.ToString("MM-dd-yyyy");
                    filter = builder.Eq(m => m.Date, lastestE.Date);
                    records = dbContext.KhoThanhPhams.CountDocuments(filter);
                }
            }
            #endregion

            var enablePage = false;
            if (records > 0 && records > Size)
            {
                enablePage = true;
                pages = (int)Math.Ceiling((double)records / (double)Size);
                if (Trang > pages)
                {
                    Trang = 1;
                }
            }

            var list = new List<KhoThanhPham>();
            if (enablePage)
            {
                list = dbContext.KhoThanhPhams.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * Size).Limit(Size).ToList();
            }
            else
            {
                list = dbContext.KhoThanhPhams.Find(filter).Sort(sortBuilder).ToList();
            }

            var viewModel = new KhoViewModel()
            {
                Name = "Kho thành phẩm",
                KhoThanhPhams = list,
                TrangThais = trangthais,
                Products = products,
                Tu = Tu,
                Den = Den,
                Hang = Hang,
                TrangThai = TrangThai,
                LinkCurrent = linkCurrent,
                ThuTu = ThuTu,
                SapXep = SapXep,
                Records = (int)records,
                PageSize = Size,
                SoTrang = pages,
                Trang = Trang
            };
            return View(viewModel);
        }

        [Route(Constants.KeHoachTongHopLink.DuLieuKhoThanhPham + "/" + Constants.ActionLink.Export)]
        public async Task<IActionResult> DuLieuKhoThanhPhamExport(DateTime Tu, DateTime Den, string Hang, string TrangThai)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoThanhPhamDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var today = DateTime.Now.Date;
            if (Tu < Constants.MinDate)
            {
                Tu = today;
            }
            if (Den < Constants.MinDate || Den < Tu)
            {
                Den = Tu;
            }
            #endregion

            string sFileName = @"kho-thanh-pham";

            var builder = Builders<KhoThanhPham>.Filter;
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);
            if (!string.IsNullOrEmpty(Hang))
            {
                filter = filter & builder.Eq(m => m.ProductId, Hang);
                sFileName += "-" + Hang;
            }
            if (!string.IsNullOrEmpty(TrangThai))
            {
                filter = filter & builder.Eq(m => m.TrangThai, TrangThai);
                sFileName += "-" + TrangThai;
            }
            var khothanhphams = dbContext.KhoThanhPhams.Find(filter).SortBy(m => m.Date).SortBy(m => m.ProductName).ToList();

            var duration = Tu.ToString("ddMMyyyy") + "-" + Den.ToString("ddMMyyyy");
            sFileName += "-" + duration;
            sFileName += "-v" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx";

            string exportFolder = Path.Combine(_env.WebRootPath, "exports", "kho", "kho-thanh-pham", today.ToString("yyyyMMdd"));

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            file.Directory.Create(); // If the directory already exists, this method does nothing.

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
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                //cellStyleBorderAndColorLightGreen.WrapText = false;
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
                //styleSmall.CloneStyleFrom(styleBorder);
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

                ISheet sheet1 = workbook.CreateSheet("DATA KHO THANH PHAM");

                #region Introduce
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
                #endregion

                row = sheet1.CreateRow(rowIndex);
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("DỮ LIỆU KHO THÀNH PHẨM");
                cell.CellStyle = styleTitle;
                rowIndex++;

                #region Title
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NGÀY");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MÃ HÀNG");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TÊN HÀNG");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("ĐVT");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỒN ĐẦU");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NHẬP TỪ SX");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NHẬP TỪ KHO XL");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NHẬP KHÁC");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XUẤT BÁN");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XUẤT KHO XỬ LÝ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XUẤT KHÁC");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỒN CUỐI");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỒN AN TOÀN");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("GHI CHÚ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;
                rowIndex++;
                #endregion

                #region Content
                foreach (var item in khothanhphams)
                {
                    row = sheet1.CreateRow(rowIndex);
                    columnIndex = 0;
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Date.ToString("dd/MM/yyyy"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ProductCode);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ProductName);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ProductUnit);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TonDau);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.NhapTuSanXuat);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.NhapTuKhoXuLy);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.NhapKhac);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuatBan);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuatKhoXuLy);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuatKhac);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TonCuoi);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TonAnToan);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Note);
                    cell.CellStyle = styleDedault;

                    rowIndex++;
                }
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

        [Route(Constants.KeHoachTongHopLink.DuLieuKhoThanhPham + "/" + Constants.ActionLink.Template)]
        public async Task<IActionResult> DuLieuKhoThanhPhamTemplate(DateTime Date)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoThanhPhamDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            Date = Date.Year > 1990 ? Date : DateTime.Now;
            string exportFolder = Path.Combine(_env.WebRootPath, "exports", "kho", "thanh-pham");
            string sFileName = @"thanh-pham-" + Date.ToString("dd-MM-yyyy") + "-v" + Date.ToString("HHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            file.Directory.Create(); // If the directory already exists, this method does nothing.

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
            //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
            cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
            //cellStyleBorderAndColorLightGreen.WrapText = false;
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
            //styleSmall.CloneStyleFrom(styleBorder);
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

            ISheet sheet1 = workbook.CreateSheet("DATA KHO THANH PHAM");

            #region Introduce
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
            #endregion

            #region Notice
            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Thông tin được cập nhật từ dòng số " + (rowIndex + 5));
            cell.CellStyle = styleSmall;
            rowIndex++;

            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Vì tính chất quan trọng, dữ liệu sau khi được đưa lên không được chỉnh sửa.");
            cell.CellStyle = styleSmall;
            rowIndex++;

            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Thay đổi dữ liệu ở cấp bậc được phép. Theo cách thức tạo dữ liệu mới bù trừ với dữ liệu cần thay đổi.");
            cell.CellStyle = styleSmall;
            rowIndex++;
            #endregion

            row = sheet1.CreateRow(rowIndex);
            CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
            sheet1.AddMergedRegion(cellRangeAddress);
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("DỮ LIỆU KHO THÀNH PHẨM");
            cell.CellStyle = styleTitle;
            rowIndex++;

            #region Title
            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NGÀY");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("MÃ HÀNG");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TÊN HÀNG");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("ĐVT");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỒN ĐẦU");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NHẬP TỪ SX");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NHẬP TỪ KHO XL");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NHẬP KHÁC");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XUẤT BÁN");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XUẤT KHO XỬ LÝ");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XUẤT KHÁC");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỒN CUỐI");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỒN AN TOÀN");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("GHI CHÚ");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;
            rowIndex++;
            #endregion

            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [HttpPost]
        [Route(Constants.KeHoachTongHopLink.DuLieuKhoThanhPham + "/" + Constants.ActionLink.Template + "/" + Constants.ActionLink.Post)]
        public IActionResult DuLieuKhoThanhPhamTemplatePost()
        {
            var Errors = new List<string>();
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

                    for (int i = 9; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var productcode = string.Empty;
                        var productname = string.Empty;
                        var productunit = string.Empty;
                        double tondau = 0;
                        double nhaptusx = 0;
                        double nhaptukhoxl = 0;
                        double nhapkhac = 0;
                        double xuattrancc = 0;
                        double xuatban = 0;
                        double xuatkhoxuly = 0;
                        double xuatkhac = 0;
                        double toncuoi = 0;
                        double tonantoan = 0;
                        var note = string.Empty;

                        int columnIndex = 0;
                        var ngay = Utility.GetDateCellValue2(row.GetCell(columnIndex));
                        columnIndex++;
                        productcode = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        productname = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        productunit = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tondau = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nhaptusx = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nhaptukhoxl = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nhapkhac = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xuatban = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xuatkhoxuly = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xuatkhac = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        toncuoi = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tonantoan = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        note = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;

                        if (!ngay.HasValue || string.IsNullOrEmpty(productname))
                        {
                            Errors.Add("Thiếu thông tin [Ngày] và [Tên] ở dòng: " + (i + 1));
                            continue;
                        }
                        var existDataThanhPham = dbContext.KhoThanhPhams.Find(m => m.Date.Equals(ngay) && m.ProductName.Equals(productname)).FirstOrDefault();
                        if (existDataThanhPham == null)
                        {
                            InitProduct((int)EKho.ThanhPham, productname, productcode, productunit, out string alias, out string productid);
                            var thanhpham = new KhoThanhPham()
                            {
                                Date = ngay.Value,
                                ProductId = productid,
                                ProductName = productname,
                                ProductCode = productcode,
                                ProductAlias = alias,
                                ProductUnit = productunit,
                                TonDau = tondau,
                                NhapTuSanXuat = nhaptusx,
                                NhapTuKhoXuLy = nhaptukhoxl,
                                NhapKhac = nhapkhac,
                                XuatBan = xuatban,
                                XuatKhoXuLy = xuatkhoxuly,
                                XuatKhac = xuatkhac,
                                TonCuoi = toncuoi,
                                TonAnToan = tonantoan,
                                Note = note
                            };
                            dbContext.KhoThanhPhams.InsertOne(thanhpham);
                        }
                        else
                        {
                            Errors.Add("Dữ liệu đã tồn tại. Không thể cập nhật dữ liệu mới ở dòng: " + (i + 1));
                        }
                    }
                }
            }

            return Json(new { result = true, Errors });
        }
        #endregion

        #region KHO BUN
        [Route(Constants.KeHoachTongHopLink.DuLieuKhoBun)]
        public async Task<IActionResult> DuLieuKhoBun(DateTime Tu, DateTime Den, string Hang, string TrangThai, int Trang, string SapXep, string ThuTu)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoBunDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            var linkCurrent = string.Empty;

            #region Times
            var today = DateTime.Now.Date;
            if (Tu < Constants.MinDate)
            {
                Tu = today;
            }
            if (Den < Constants.MinDate || Den < Tu)
            {
                Den = Tu;
            }
            linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
            linkCurrent += "Tu=" + Tu.ToString("MM-dd-yyyy") + "&Den=" + Den.ToString("MM-dd-yyyy");
            #endregion

            var type = (int)EKho.Bun;
            var trangthais = dbContext.TrangThais.Find(m => m.Enable.Equals(true) && m.TypeId.Equals((int)ETrangThai.Kho)).ToList();
            var products = dbContext.Products.Find(m => m.Enable.Equals(true) && m.TypeId.Equals(type)).ToList();

            #region Filter
            var builder = Builders<KhoBun>.Filter;
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);
            if (!string.IsNullOrEmpty(Hang))
            {
                filter = filter & builder.Eq(m => m.ProductId, Hang);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Hang=" + Hang;
            }
            if (!string.IsNullOrEmpty(TrangThai))
            {
                filter = filter & builder.Eq(m => m.TrangThai, TrangThai);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "TrangThai=" + Hang;
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<KhoBun>.Sort.Ascending(m => m.Date);
            SapXep = string.IsNullOrEmpty(SapXep) ? "ngay" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "san-pham":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoBun>.Sort.Ascending(m => m.ProductAlias) : Builders<KhoBun>.Sort.Descending(m => m.ProductAlias);
                    break;
                case "khach-hang":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoBun>.Sort.Ascending(m => m.CustomerAlias) : Builders<KhoBun>.Sort.Descending(m => m.CustomerAlias);
                    break;
                case "ho-chua":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoBun>.Sort.Ascending(m => m.HoChuaAlias) : Builders<KhoBun>.Sort.Descending(m => m.HoChuaAlias);
                    break;
                case "ton":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoBun>.Sort.Ascending(m => m.TonKho) : Builders<KhoBun>.Sort.Descending(m => m.TonKho);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<KhoBun>.Sort.Ascending(m => m.Date) : Builders<KhoBun>.Sort.Descending(m => m.Date);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            var settingPage = dbContext.Settings.Find(m => m.Type.Equals("system") && m.Key.Equals("page-size-khth")).FirstOrDefault();
            int Size = settingPage != null ? Convert.ToInt32(settingPage.Value) : 100;
            int pages = 1;
            var records = dbContext.KhoBuns.CountDocuments(filter);

            #region Get lastest data
            if (records == 0)
            {
                var lastestE = dbContext.KhoBuns.Find(m => true).SortByDescending(m => m.Date).Limit(1).FirstOrDefault();
                if (lastestE != null)
                {
                    Tu = lastestE.Date;
                    Den = lastestE.Date;
                    linkCurrent = "?Tu=" + Tu.ToString("MM-dd-yyyy") + "&Den=" + Den.ToString("MM-dd-yyyy");
                    filter = builder.Eq(m => m.Date, lastestE.Date);
                    records = dbContext.KhoBuns.CountDocuments(filter);
                }
            }
            #endregion

            var enablePage = false;
            if (records > 0 && records > Size)
            {
                enablePage = true;
                pages = (int)Math.Ceiling((double)records / (double)Size);
                if (Trang > pages)
                {
                    Trang = 1;
                }
            }

            var list = new List<KhoBun>();
            if (enablePage)
            {
                list = dbContext.KhoBuns.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * Size).Limit(Size).ToList();
            }
            else
            {
                list = dbContext.KhoBuns.Find(filter).Sort(sortBuilder).ToList();
            }

            var viewModel = new KhoViewModel()
            {
                Name = "Kho bùn",
                KhoBuns = list,
                TrangThais = trangthais,
                Products = products,
                Tu = Tu,
                Den = Den,
                Hang = Hang,
                TrangThai = TrangThai,
                LinkCurrent = linkCurrent,
                ThuTu = ThuTu,
                SapXep = SapXep,
                Records = (int)records,
                PageSize = Size,
                SoTrang = pages,
                Trang = Trang
            };
            return View(viewModel);
        }

        [Route(Constants.KeHoachTongHopLink.DuLieuKhoBun + "/" + Constants.ActionLink.Export)]
        public async Task<IActionResult> DuLieuKhoBunExport(DateTime Tu, DateTime Den, string Hang, string TrangThai)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoBunDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var today = DateTime.Now.Date;
            if (Tu < Constants.MinDate)
            {
                Tu = today;
            }
            if (Den < Constants.MinDate || Den < Tu)
            {
                Den = Tu;
            }
            #endregion

            string sFileName = @"kho-bun";

            var builder = Builders<KhoBun>.Filter;
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);
            if (!string.IsNullOrEmpty(Hang))
            {
                filter = filter & builder.Eq(m => m.ProductId, Hang);
                sFileName += "-" + Hang;
            }
            if (!string.IsNullOrEmpty(TrangThai))
            {
                filter = filter & builder.Eq(m => m.TrangThai, TrangThai);
                sFileName += "-" + TrangThai;
            }
            var khobuns = dbContext.KhoBuns.Find(filter).SortBy(m => m.Date).SortBy(m => m.ProductName).ToList();

            var duration = Tu.ToString("ddMMyyyy") + "-" + Den.ToString("ddMMyyyy");
            sFileName += "-" + duration;
            sFileName += "-v" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx";

            string exportFolder = Path.Combine(_env.WebRootPath, "exports", "kho", "kho-bun", today.ToString("yyyyMMdd"));

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            file.Directory.Create(); // If the directory already exists, this method does nothing.

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
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                //cellStyleBorderAndColorLightGreen.WrapText = false;
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
                //styleSmall.CloneStyleFrom(styleBorder);
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

                ISheet sheet1 = workbook.CreateSheet("DATA KHO BUN");

                #region Introduce
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
                #endregion

                row = sheet1.CreateRow(rowIndex);
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("DỮ LIỆU KHO BÙN");
                cell.CellStyle = styleTitle;
                rowIndex++;

                #region Title
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NGÀY");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TÊN BÙN");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TÊN KHÁCH HÀNG");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("HỒ CHỨA");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("ĐVT");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỒN ĐẦU");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NHẬP KHO");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XỬ LÝ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XỬ LÝ BAO");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XỬ LÝ KHÁC");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("HAO HỤT");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỒN KHO");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("GHI CHÚ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;

                rowIndex++;
                #endregion

                #region Content
                foreach (var item in khobuns)
                {
                    row = sheet1.CreateRow(rowIndex);
                    columnIndex = 0;
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Date.ToString("dd/MM/yyyy"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ProductName);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Customer);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.HoChua);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.DVT);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TonDau);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.NhapKho);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuLy);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuLyBao);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuLyKhac);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.HaoHut);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TonKho);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Note);
                    cell.CellStyle = styleDedault;

                    rowIndex++;
                }
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

        [Route(Constants.KeHoachTongHopLink.DuLieuKhoBun + "/" + Constants.ActionLink.Template)]
        public async Task<IActionResult> DuLieuKhoBunTemplate(DateTime Date)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoBunDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            Date = Date.Year > 1990 ? Date : DateTime.Now;
            string exportFolder = Path.Combine(_env.WebRootPath, "exports", "kho", "bun");
            string sFileName = @"bun-" + Date.ToString("dd-MM-yyyy") + "-v" + Date.ToString("HHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            file.Directory.Create(); // If the directory already exists, this method does nothing.

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
            //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
            cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
            //cellStyleBorderAndColorLightGreen.WrapText = false;
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
            //styleSmall.CloneStyleFrom(styleBorder);
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

            ISheet sheet1 = workbook.CreateSheet("DATA KHO BUN");

            #region Introduce
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
            #endregion

            #region Notice
            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Thông tin được cập nhật từ dòng số " + (rowIndex + 5));
            cell.CellStyle = styleSmall;
            rowIndex++;

            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Vì tính chất quan trọng, dữ liệu sau khi được đưa lên không được chỉnh sửa.");
            cell.CellStyle = styleSmall;
            rowIndex++;

            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Thay đổi dữ liệu ở cấp bậc được phép. Theo cách thức tạo dữ liệu mới bù trừ với dữ liệu cần thay đổi.");
            cell.CellStyle = styleSmall;
            rowIndex++;
            #endregion

            row = sheet1.CreateRow(rowIndex);
            CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
            sheet1.AddMergedRegion(cellRangeAddress);
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("DỮ LIỆU KHO BÙN");
            cell.CellStyle = styleTitle;
            rowIndex++;

            #region Title
            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NGÀY");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TÊN BÙN");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TÊN KHÁCH HÀNG");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("HỒ CHỨA");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("ĐVT");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỒN ĐẦU");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NHẬP KHO");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XỬ LÝ");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XỬ LÝ BAO");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XỬ LÝ KHÁC");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("HAO HỤT");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỒN KHO");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("GHI CHÚ");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;
            rowIndex++;
            #endregion

            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [HttpPost]
        [Route(Constants.KeHoachTongHopLink.DuLieuKhoBun + "/" + Constants.ActionLink.Template + "/" + Constants.ActionLink.Post)]
        public IActionResult DuLieuKhoBunTemplatePost()
        {
            var Errors = new List<string>();
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

                    for (int i = 9; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var tenbun = string.Empty;
                        var tenkhachhang = string.Empty;
                        var hochua = string.Empty;
                        var dvt = string.Empty;
                        double tondau = 0;
                        double nhapkho = 0;
                        double xuly = 0;
                        double xulybao = 0;
                        double xulykhac = 0;
                        double haohut = 0;
                        double tonkho = 0;
                        var note = string.Empty;

                        int columnIndex = 0;
                        var ngay = Utility.GetDateCellValue2(row.GetCell(columnIndex));
                        columnIndex++;
                        tenbun = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tenkhachhang = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        hochua = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        dvt = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tondau = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nhapkho = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xuly = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xulybao = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xulykhac = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        haohut = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tonkho = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        note = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;

                        if (!ngay.HasValue || string.IsNullOrEmpty(tenbun))
                        {
                            Errors.Add("Thiếu thông tin [Ngày] và [Tên Bùn] ở dòng: " + (i + 1));
                            continue;
                        }
                        var existDataBun = dbContext.KhoBuns.Find(m => m.Date.Equals(ngay)
                                            && m.ProductName.Equals(tenbun)
                                            && m.Customer.Equals(tenkhachhang)
                                            && m.HoChua.Equals(hochua)
                                            && m.TonDau.Equals(tondau)
                                            && m.NhapKho.Equals(nhapkho)
                                            && m.XuLy.Equals(xuly)).FirstOrDefault();
                        if (existDataBun == null)
                        {
                            InitProduct((int)EKho.Bun, tenbun, string.Empty, dvt, out string alias, out string productid);

                            InitCustomer((int)ECustomer.Bun, tenkhachhang, out string customeralias, out string customerid);

                            InitHoChua(hochua, out string hochuaalias);

                            var bun = new KhoBun()
                            {
                                Date = ngay.Value,
                                ProductId = productid,
                                ProductName = tenbun,
                                ProductCode = string.Empty,
                                ProductAlias = alias,
                                ProductUnit = dvt,
                                Customer = tenkhachhang,
                                CustomerAlias = customeralias,
                                CustomerId = customerid,
                                HoChua = hochua,
                                HoChuaAlias = hochuaalias,
                                DVT = dvt,
                                TonDau = tondau,
                                NhapKho = nhapkho,
                                XuLy = xuly,
                                XuLyBao = xulybao,
                                XuLyKhac = xulykhac,
                                HaoHut = haohut,
                                TonKho = tonkho,
                                Note = note
                            };
                            dbContext.KhoBuns.InsertOne(bun);
                        }
                        else
                        {
                            Errors.Add("Dữ liệu đã tồn tại. Không thể cập nhật dữ liệu mới ở dòng: " + (i + 1));
                        }
                    }
                }
            }

            return Json(new { result = true, Errors });
        }
        #endregion

        #region KHO XU LY
        [Route(Constants.KeHoachTongHopLink.DuLieuKhoXuLy)]
        public async Task<IActionResult> DuLieuKhoXuLy(DateTime Tu, DateTime Den, string Hang, string TrangThai, int Trang, string SapXep, string ThuTu)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoXuLyBunDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            var linkCurrent = string.Empty;

            #region Times
            var today = DateTime.Now.Date;
            if (Tu < Constants.MinDate)
            {
                Tu = today;
            }
            if (Den < Constants.MinDate || Den < Tu)
            {
                Den = Tu;
            }
            linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
            linkCurrent += "Tu=" + Tu.ToString("MM-dd-yyyy") + "&Den=" + Den.ToString("MM-dd-yyyy");
            #endregion

            var type = (int)EKho.XuLy;
            var trangthais = dbContext.TrangThais.Find(m => m.Enable.Equals(true) && m.TypeId.Equals((int)ETrangThai.Kho)).ToList();
            var products = dbContext.Products.Find(m => m.Enable.Equals(true) && m.TypeId.Equals(type)).ToList();

            #region Filter
            var builder = Builders<KhoXuLy>.Filter;
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);
            if (!string.IsNullOrEmpty(Hang))
            {
                filter = filter & builder.Eq(m => m.ProductId, Hang);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "Hang=" + Hang;
            }
            if (!string.IsNullOrEmpty(TrangThai))
            {
                filter = filter & builder.Eq(m => m.TrangThai, TrangThai);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "TrangThai=" + Hang;
            }
            #endregion


            #region Sort
            var sortBuilder = Builders<KhoXuLy>.Sort.Ascending(m => m.Date);
            SapXep = string.IsNullOrEmpty(SapXep) ? "ngay" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "san-pham":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoXuLy>.Sort.Ascending(m => m.ProductAlias) : Builders<KhoXuLy>.Sort.Descending(m => m.ProductAlias);
                    break;
                case "ma-san-pham":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoXuLy>.Sort.Ascending(m => m.ProductCode) : Builders<KhoXuLy>.Sort.Descending(m => m.ProductCode);
                    break;
                case "ton":
                    sortBuilder = ThuTu == "asc" ? Builders<KhoXuLy>.Sort.Ascending(m => m.TonCuoi) : Builders<KhoXuLy>.Sort.Descending(m => m.TonCuoi);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<KhoXuLy>.Sort.Ascending(m => m.Date) : Builders<KhoXuLy>.Sort.Descending(m => m.Date);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            var settingPage = dbContext.Settings.Find(m => m.Type.Equals("system") && m.Key.Equals("page-size-khth")).FirstOrDefault();
            int Size = settingPage != null ? Convert.ToInt32(settingPage.Value) : 100;
            int pages = 1;
            var records = dbContext.KhoXuLys.CountDocuments(filter);

            #region Get lastest data
            if (records == 0)
            {
                var lastestE = dbContext.KhoXuLys.Find(m => true).SortByDescending(m => m.Date).Limit(1).FirstOrDefault();
                if (lastestE != null)
                {
                    Tu = lastestE.Date;
                    Den = lastestE.Date;
                    linkCurrent = "?Tu=" + Tu.ToString("MM-dd-yyyy") + "&Den=" + Den.ToString("MM-dd-yyyy");
                    filter = builder.Eq(m => m.Date, lastestE.Date);
                    records = dbContext.KhoXuLys.CountDocuments(filter);
                }
            }
            #endregion

            var enablePage = false;
            if (records > 0 && records > Size)
            {
                enablePage = true;
                pages = (int)Math.Ceiling((double)records / (double)Size);
                if (Trang > pages)
                {
                    Trang = 1;
                }
            }

            var list = new List<KhoXuLy>();
            if (enablePage)
            {
                list = dbContext.KhoXuLys.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * Size).Limit(Size).ToList();
            }
            else
            {
                list = dbContext.KhoXuLys.Find(filter).Sort(sortBuilder).ToList();
            }

            var viewModel = new KhoViewModel()
            {
                Name = "Kho xử lý",
                KhoXuLys = list,
                TrangThais = trangthais,
                Products = products,
                Tu = Tu,
                Den = Den,
                Hang = Hang,
                TrangThai = TrangThai,
                LinkCurrent = linkCurrent,
                ThuTu = ThuTu,
                SapXep = SapXep,
                Records = (int)records,
                PageSize = Size,
                SoTrang = pages,
                Trang = Trang
            };
            return View(viewModel);
        }

        [Route(Constants.KeHoachTongHopLink.DuLieuKhoXuLy + "/" + Constants.ActionLink.Export)]
        public async Task<IActionResult> DuLieuKhoXuLyExport(DateTime Tu, DateTime Den, string Hang, string TrangThai)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoXuLyBunDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var today = DateTime.Now.Date;
            if (Tu < Constants.MinDate)
            {
                Tu = today;
            }
            if (Den < Constants.MinDate || Den < Tu)
            {
                Den = Tu;
            }
            #endregion

            string sFileName = @"kho-xu-ly";

            var builder = Builders<KhoXuLy>.Filter;
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);
            if (!string.IsNullOrEmpty(Hang))
            {
                filter = filter & builder.Eq(m => m.ProductId, Hang);
                sFileName += "-" + Hang;
            }
            if (!string.IsNullOrEmpty(TrangThai))
            {
                filter = filter & builder.Eq(m => m.TrangThai, TrangThai);
                sFileName += "-" + TrangThai;
            }
            var khoxulys = dbContext.KhoXuLys.Find(filter).SortBy(m => m.Date).SortBy(m => m.ProductName).ToList();

            var duration = Tu.ToString("ddMMyyyy") + "-" + Den.ToString("ddMMyyyy");
            sFileName += "-" + duration;
            sFileName += "-v" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx";

            string exportFolder = Path.Combine(_env.WebRootPath, "exports", "kho", "kho-xu-ly", today.ToString("yyyyMMdd"));

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            file.Directory.Create(); // If the directory already exists, this method does nothing.

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
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                //cellStyleBorderAndColorLightGreen.WrapText = false;
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
                //styleSmall.CloneStyleFrom(styleBorder);
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

                ISheet sheet1 = workbook.CreateSheet("DATA KHO XU LY");

                #region Introduce
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
                #endregion

                row = sheet1.CreateRow(rowIndex);
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("DỮ LIỆU KHO XỬ LÝ");
                cell.CellStyle = styleTitle;
                rowIndex++;

                #region Title
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NGÀY");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("MÃ HÀNG");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TÊN HÀNG");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("ĐVT");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỒN ĐẦU");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NHẬP TỪ KHO THÀNH PHẨM");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NHẬP TỪ KINH DOANH");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XUẤT CHUYỂN KHO THÀNH PHẨM");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("XUẤT XỬ LÝ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("TỒN CUỐI");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                rowIndex++;
                #endregion

                #region Content
                foreach (var item in khoxulys)
                {
                    row = sheet1.CreateRow(rowIndex);
                    columnIndex = 0;
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Date.ToString("dd/MM/yyyy"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ProductCode);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ProductName);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.ProductUnit);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TonDau);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.NhapTuKhoThanhPham);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.NhapTuKinhDoanh);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuatChuyenKhoThanhPham);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.XuatXuLy);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TonCuoi);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    rowIndex++;
                }
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

        [Route(Constants.KeHoachTongHopLink.DuLieuKhoXuLy + "/" + Constants.ActionLink.Template)]
        public async Task<IActionResult> DuLieuKhoXuLyTemplate(DateTime Date)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.KhoXuLyBunDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            Date = Date.Year > 1990 ? Date : DateTime.Now;
            string exportFolder = Path.Combine(_env.WebRootPath, "exports", "kho", "xu-ly");
            string sFileName = @"xu-ly-" + Date.ToString("dd-MM-yyyy") + "-v" + Date.ToString("HHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            file.Directory.Create(); // If the directory already exists, this method does nothing.

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
            //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
            cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
            //cellStyleBorderAndColorLightGreen.WrapText = false;
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
            //styleSmall.CloneStyleFrom(styleBorder);
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

            ISheet sheet1 = workbook.CreateSheet("DATA KHO XU LY");

            #region Introduce
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
            #endregion

            #region Notice
            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Thông tin được cập nhật từ dòng số " + (rowIndex + 5));
            cell.CellStyle = styleSmall;
            rowIndex++;

            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Vì tính chất quan trọng, dữ liệu sau khi được đưa lên không được chỉnh sửa.");
            cell.CellStyle = styleSmall;
            rowIndex++;

            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Thay đổi dữ liệu ở cấp bậc được phép. Theo cách thức tạo dữ liệu mới bù trừ với dữ liệu cần thay đổi.");
            cell.CellStyle = styleSmall;
            rowIndex++;
            #endregion

            row = sheet1.CreateRow(rowIndex);
            CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
            sheet1.AddMergedRegion(cellRangeAddress);
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("DỮ LIỆU KHO XỬ LÝ");
            cell.CellStyle = styleTitle;
            rowIndex++;

            #region Title
            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NGÀY");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("MÃ HÀNG");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TÊN HÀNG");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("ĐVT");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỒN ĐẦU");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NHẬP TỪ KHO THÀNH PHẨM");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NHẬP TỪ KINH DOANH");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XUẤT CHUYỂN KHO THÀNH PHẨM");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("XUẤT XỬ LÝ");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("TỒN CUỐI");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            rowIndex++;
            #endregion

            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [HttpPost]
        [Route(Constants.KeHoachTongHopLink.DuLieuKhoXuLy + "/" + Constants.ActionLink.Template + "/" + Constants.ActionLink.Post)]
        public IActionResult DuLieuKhoXuLyTemplatePost()
        {
            var Errors = new List<string>();
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

                    for (int i = 9; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var productcode = string.Empty;
                        var productname = string.Empty;
                        var productunit = string.Empty;
                        double tondau = 0;
                        double nhaptukhothanhpham = 0;
                        double nhaptukinhdoanh = 0;
                        double xuatchuyenkhothanhpham = 0;
                        double xuatxuly = 0;
                        double toncuoi = 0;

                        int columnIndex = 0;
                        var ngay = Utility.GetDateCellValue2(row.GetCell(columnIndex));
                        columnIndex++;
                        productcode = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        productname = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        productunit = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tondau = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nhaptukhothanhpham = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nhaptukinhdoanh = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xuatchuyenkhothanhpham = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        xuatxuly = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        toncuoi = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;

                        if (!ngay.HasValue || string.IsNullOrEmpty(productname))
                        {
                            Errors.Add("Thiếu thông tin [Ngày] và [Tên] ở dòng: " + (i + 1));
                            continue;
                        }
                        var existDataXuLy = dbContext.KhoXuLys.Find(m => m.Date.Equals(ngay) && m.ProductName.Equals(productname)).FirstOrDefault();
                        if (existDataXuLy == null)
                        {
                            InitProduct((int)EKho.XuLy, productname, productcode, productunit, out string alias, out string productid);
                            var xuly = new KhoXuLy()
                            {
                                Date = ngay.Value,
                                ProductId = productid,
                                ProductName = productname,
                                ProductCode = productcode,
                                ProductAlias = alias,
                                ProductUnit = productunit,
                                TonDau = tondau,
                                NhapTuKhoThanhPham = nhaptukhothanhpham,
                                NhapTuKinhDoanh = nhaptukinhdoanh,
                                XuatChuyenKhoThanhPham = xuatchuyenkhothanhpham,
                                XuatXuLy = xuatxuly,
                                TonCuoi = toncuoi
                            };
                            dbContext.KhoXuLys.InsertOne(xuly);
                        }
                        else
                        {
                            Errors.Add("Dữ liệu đã tồn tại. Không thể cập nhật dữ liệu mới ở dòng: " + (i + 1));
                        }
                    }
                }
            }

            return Json(new { result = true, Errors });
        }
        #endregion

        #region TIEP NHAN XU LY BUN
        [Route(Constants.KeHoachTongHopLink.DuLieuTiepNhanXuLyBun)]
        public async Task<IActionResult> DuLieuTiepNhanXuLyBun(DateTime Tu, DateTime Den, int? SoPhieu, int Trang, string SapXep, string ThuTu)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.TiepNhanXuLyDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            var linkCurrent = string.Empty;

            #region Times
            var today = DateTime.Now.Date;
            if (Tu < Constants.MinDate)
            {
                Tu = today;
            }
            if (Den < Constants.MinDate || Den < Tu)
            {
                Den = Tu;
            }
            linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
            linkCurrent += "Tu=" + Tu.ToString("MM-dd-yyyy") + "&Den=" + Den.ToString("MM-dd-yyyy");
            #endregion

            var type = (int)EKho.TiepNhanXuLy;
            var trangthais = dbContext.TrangThais.Find(m => m.Enable.Equals(true) && m.TypeId.Equals((int)ETrangThai.Kho)).ToList();
            var products = dbContext.Products.Find(m => m.Enable.Equals(true) && m.TypeId.Equals(type)).ToList();

            #region Filter
            var builder = Builders<TiepNhanXuLy>.Filter;
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);
            if (SoPhieu.HasValue)
            {
                filter = filter & builder.Eq(m => m.SoPhieu, SoPhieu);
                linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
                linkCurrent += "SoPhieu=" + SoPhieu;
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<TiepNhanXuLy>.Sort.Ascending(m => m.Date);
            SapXep = string.IsNullOrEmpty(SapXep) ? "ngay" : SapXep;
            ThuTu = string.IsNullOrEmpty(ThuTu) ? "asc" : ThuTu;
            switch (SapXep)
            {
                case "so-phieu":
                    sortBuilder = ThuTu == "asc" ? Builders<TiepNhanXuLy>.Sort.Ascending(m => m.SoPhieu) : Builders<TiepNhanXuLy>.Sort.Descending(m => m.SoPhieu);
                    break;
                case "trong-luong":
                    sortBuilder = ThuTu == "asc" ? Builders<TiepNhanXuLy>.Sort.Ascending(m => m.TrongLuong) : Builders<TiepNhanXuLy>.Sort.Descending(m => m.TrongLuong);
                    break;
                case "nguoi-can":
                    sortBuilder = ThuTu == "asc" ? Builders<TiepNhanXuLy>.Sort.Ascending(m => m.NguoiCan) : Builders<TiepNhanXuLy>.Sort.Descending(m => m.NguoiCan);
                    break;
                default:
                    sortBuilder = ThuTu == "asc" ? Builders<TiepNhanXuLy>.Sort.Ascending(m => m.Date) : Builders<TiepNhanXuLy>.Sort.Descending(m => m.Date);
                    break;
            }
            #endregion

            Trang = Trang == 0 ? 1 : Trang;
            var settingPage = dbContext.Settings.Find(m => m.Type.Equals("system") && m.Key.Equals("page-size-khth")).FirstOrDefault();
            int Size = settingPage != null ? Convert.ToInt32(settingPage.Value) : 100;
            int pages = 1;
            var records = dbContext.TiepNhanXuLys.CountDocuments(filter);

            #region Get lastest data
            if (records == 0)
            {
                var lastestE = dbContext.TiepNhanXuLys.Find(m => true).SortByDescending(m => m.Date).Limit(1).FirstOrDefault();
                if (lastestE != null)
                {
                    Tu = lastestE.Date;
                    Den = lastestE.Date;
                    linkCurrent = "?Tu=" + Tu.ToString("MM-dd-yyyy") + "&Den=" + Den.ToString("MM-dd-yyyy");
                    filter = builder.Eq(m => m.Date, lastestE.Date);
                    records = dbContext.TiepNhanXuLys.CountDocuments(filter);
                }
            }
            #endregion

            var enablePage = false;
            if (records > 0 && records > Size)
            {
                enablePage = true;
                pages = (int)Math.Ceiling((double)records / (double)Size);
                if (Trang > pages)
                {
                    Trang = 1;
                }
            }

            var list = new List<TiepNhanXuLy>();
            if (enablePage)
            {
                list = dbContext.TiepNhanXuLys.Find(filter).Sort(sortBuilder).Skip((Trang - 1) * Size).Limit(Size).ToList();
            }
            else
            {
                list = dbContext.TiepNhanXuLys.Find(filter).Sort(sortBuilder).ToList();
            }

            var viewModel = new KhoViewModel()
            {
                Name = "Tiếp nhận xử lý",
                TiepNhanXuLys = list,
                TrangThais = trangthais,
                Products = products,
                Tu = Tu,
                Den = Den,
                SoPhieu = SoPhieu,
                LinkCurrent = linkCurrent,
                ThuTu = ThuTu,
                SapXep = SapXep,
                Records = (int)records,
                PageSize = Size,
                SoTrang = pages,
                Trang = Trang
            };
            return View(viewModel);
        }

        [Route(Constants.KeHoachTongHopLink.DuLieuTiepNhanXuLyBun + "/" + Constants.ActionLink.Export)]
        public async Task<IActionResult> DuLieuTiepNhanXuLyBunExport(DateTime Tu, DateTime Den, int? SoPhieu)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.TiepNhanXuLyDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            #region Times
            var today = DateTime.Now.Date;
            if (Tu < Constants.MinDate)
            {
                Tu = today;
            }
            if (Den < Constants.MinDate || Den < Tu)
            {
                Den = Tu;
            }
            #endregion

            string sFileName = @"tiep-nhan-xu-ly";

            var builder = Builders<TiepNhanXuLy>.Filter;
            var filter = builder.Eq(m => m.Enable, true)
                        & builder.Gte(m => m.Date, Tu)
                        & builder.Lte(m => m.Date, Den);
            if (SoPhieu.HasValue)
            {
                filter = filter & builder.Eq(m => m.SoPhieu, SoPhieu);
                sFileName += "-" + SoPhieu;
            }
            var tiepnhanxulys = dbContext.TiepNhanXuLys.Find(filter).SortBy(m => m.SoPhieu).ToList();

            var duration = Tu.ToString("ddMMyyyy") + "-" + Den.ToString("ddMMyyyy");
            sFileName += "-" + duration;
            sFileName += "-v" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx";

            string exportFolder = Path.Combine(_env.WebRootPath, "exports", "kho", "tiep-nhan-xu-ly", today.ToString("yyyyMMdd"));

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            file.Directory.Create(); // If the directory already exists, this method does nothing.

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
                //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
                cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
                //cellStyleBorderAndColorLightGreen.WrapText = false;
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
                //styleSmall.CloneStyleFrom(styleBorder);
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

                ISheet sheet1 = workbook.CreateSheet("DATA TIEP NHAN XU LY");

                #region Introduce
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
                #endregion

                row = sheet1.CreateRow(rowIndex);
                CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("DỮ LIỆU TIẾP NHẬN XỬ LÝ");
                cell.CellStyle = styleTitle;
                rowIndex++;

                #region Title
                row = sheet1.CreateRow(rowIndex);
                columnIndex = 0;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("SỐ PHIẾU");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NGÀY GIỜ L1");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NGÀY GIỜ L2");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("BIỂN SỐ 1");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("BIỂN SỐ 2");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("T.LƯỢNG L1");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("T.LƯỢNG L2");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("T.LƯỢNG");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("KHÁCH HÀNG");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("KHÁCH HÀNG CON");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("ĐƠN VỊ VẬN CHUYỂN");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("LOẠI HÀNG");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("KHO HÀNG");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NHẬP/XUẤT");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("NGƯỜI CÂN");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("GHI CHÚ");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("GIỜ CÂN L1");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("GIỜ CÂN L2");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("PHÂN LOẠI");
                cell.CellStyle = cellStyleBorderAndColorLightGreen;

                rowIndex++;
                #endregion

                #region Content
                foreach (var item in tiepnhanxulys)
                {
                    row = sheet1.CreateRow(rowIndex);
                    columnIndex = 0;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.SoPhieu);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.NgayGioL1.ToString("dd/MM/yyyy"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.NgayGioL2.ToString("dd/MM/yyyy"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.BienSo1);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.BienSo2);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TrongLuongL1);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TrongLuongL2);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(item.TrongLuong);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Customer);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.CustomerChild);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.DonViVanChuyen);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.LoaiHang);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.KhoHang);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.NhapXuat);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.NguoiCan);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.Note);
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.GioCan1.ToString("HH:mm:ss"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.GioCan2.ToString("HH:mm:ss"));
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(item.PhanLoai);
                    cell.CellStyle = styleDedault;

                    rowIndex++;
                }
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

        [Route(Constants.KeHoachTongHopLink.DuLieuTiepNhanXuLyBun + "/" + Constants.ActionLink.Template)]
        public async Task<IActionResult> DuLieuTiepNhanXuLyBunTemplate(DateTime Date)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.TiepNhanXuLyDuLieu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            Date = Date.Year > 1990 ? Date : DateTime.Now;
            string exportFolder = Path.Combine(_env.WebRootPath, "exports", "kho", "tiep-nhan-xu-ly");
            string sFileName = @"tiep-nhan-xu-ly-" + Date.ToString("dd-MM-yyyy") + "-v" + Date.ToString("HHmmss") + ".xlsx";

            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            file.Directory.Create(); // If the directory already exists, this method does nothing.

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
            //cellStyleBorderAndColorLightGreen.CloneStyleFrom(styleCenter);
            cellStyleBorderAndColorLightGreen.SetFont(fontbold11);
            //cellStyleBorderAndColorLightGreen.WrapText = false;
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
            //styleSmall.CloneStyleFrom(styleBorder);
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

            ISheet sheet1 = workbook.CreateSheet("DATA TIEP NHAN XU LY");

            #region Introduce
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
            #endregion

            #region Notice
            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Thông tin được cập nhật từ dòng số " + (rowIndex + 5));
            cell.CellStyle = styleSmall;
            rowIndex++;

            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Vì tính chất quan trọng, dữ liệu sau khi được đưa lên không được chỉnh sửa.");
            cell.CellStyle = styleSmall;
            rowIndex++;

            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("Thay đổi dữ liệu ở cấp bậc được phép. Theo cách thức tạo dữ liệu mới bù trừ với dữ liệu cần thay đổi.");
            cell.CellStyle = styleSmall;
            rowIndex++;
            #endregion

            row = sheet1.CreateRow(rowIndex);
            CellRangeAddress cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
            sheet1.AddMergedRegion(cellRangeAddress);
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("DỮ LIỆU TIẾP NHẬN XỬ LÝ");
            cell.CellStyle = styleTitle;
            rowIndex++;

            #region Title
            row = sheet1.CreateRow(rowIndex);
            columnIndex = 0;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("SỐ PHIẾU");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NGÀY GIỜ L1");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NGÀY GIỜ L2");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("BIỂN SỐ 1");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("BIỂN SỐ 2");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("T.LƯỢNG L1");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("T.LƯỢNG L2");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("T.LƯỢNG");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("KHÁCH HÀNG");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("KHÁCH HÀNG CON");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("ĐƠN VỊ VẬN CHUYỂN");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;
            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("LOẠI HÀNG");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("KHO HÀNG");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NHẬP/XUẤT");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("NGƯỜI CÂN");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("GHI CHÚ");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("GIỜ CÂN L1");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("GIỜ CÂN L2");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;
            columnIndex++;

            cell = row.CreateCell(columnIndex, CellType.String);
            cell.SetCellValue("PHÂN LOẠI");
            cell.CellStyle = cellStyleBorderAndColorLightGreen;

            rowIndex++;
            #endregion

            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [HttpPost]
        [Route(Constants.KeHoachTongHopLink.DuLieuTiepNhanXuLyBun + "/" + Constants.ActionLink.Template + "/" + Constants.ActionLink.Post)]
        public IActionResult DuLieuTiepNhanXuLyBunTemplatePost()
        {
            var Errors = new List<string>();
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

                    for (int i = 9; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        int sophieu = 0;
                        DateTime ngaygio1 = DateTime.Now;
                        DateTime ngaygio2 = DateTime.Now;
                        var bienso1 = string.Empty;
                        var bienso2 = string.Empty;
                        double tluong1 = 0;
                        double tluong2 = 0;
                        double tluong = 0;
                        var khachhang = string.Empty;
                        var khachhangcon = string.Empty;
                        var donvivanchuyen = string.Empty;
                        var loaihang = string.Empty;
                        var khohang = string.Empty;
                        var nhapxuat = string.Empty;
                        var nguoican = string.Empty;
                        var note = string.Empty;
                        DateTime giocan1 = DateTime.Now;
                        DateTime giocan2 = DateTime.Now;
                        var phanloai = string.Empty;

                        int columnIndex = 0;
                        try
                        {
                            sophieu = Convert.ToInt32(Utility.GetFormattedCellValue(row.GetCell(columnIndex)));
                        }
                        catch (Exception ex)
                        {
                            Errors.Add("Sai thông tin [Số phiếu] ở dòng: " + (i + 1));
                            continue;
                        }

                        columnIndex++;
                        ngaygio1 = Utility.GetDateCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        ngaygio2 = Utility.GetDateCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        bienso1 = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        bienso2 = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tluong1 = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tluong2 = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        tluong = Utility.GetNumbericCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        khachhang = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        khachhangcon = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        donvivanchuyen = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        loaihang = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        khohang = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nhapxuat = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        nguoican = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        note = Utility.GetFormattedCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        giocan1 = Utility.GetDateCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        giocan2 = Utility.GetDateCellValue(row.GetCell(columnIndex));
                        columnIndex++;
                        phanloai = Utility.GetFormattedCellValue(row.GetCell(columnIndex));

                        var existDataTiepNhanXuLy = dbContext.TiepNhanXuLys.Find(m => m.SoPhieu.Equals(sophieu) && m.Date.Equals(ngaygio1)).FirstOrDefault();
                        if (existDataTiepNhanXuLy == null)
                        {
                            InitCustomer((int)ECustomer.Bun, khachhang, out string customeralias, out string customerid);

                            InitCustomerChild((int)ECustomer.Bun, khachhangcon, khachhang, out string customechildralias, out string customerchildid);

                            var tiepnhanxuly = new TiepNhanXuLy()
                            {
                                Date = ngaygio1,
                                SoPhieu = sophieu,
                                NgayGioL1 = ngaygio1,
                                NgayGioL2 = ngaygio2,
                                BienSo1 = bienso1,
                                BienSo2 = bienso2,
                                TrongLuongL1 = tluong1,
                                TrongLuongL2 = tluong2,
                                TrongLuong = tluong,
                                Customer = khachhang,
                                CustomerId = customerid,
                                CustomerChild = khachhangcon,
                                CustomerChildId = customerchildid,
                                DonViVanChuyen = donvivanchuyen,
                                LoaiHang = loaihang,
                                KhoHang = khohang,
                                NhapXuat = nhapxuat,
                                NguoiCan = nguoican,
                                Note = note,
                                GioCan1 = giocan1,
                                GioCan2 = giocan2,
                                PhanLoai = phanloai
                            };
                            dbContext.TiepNhanXuLys.InsertOne(tiepnhanxuly);
                        }
                        else
                        {
                            Errors.Add("Dữ liệu đã tồn tại. Không thể cập nhật dữ liệu mới ở dòng: " + (i + 1));
                        }
                    }
                }
            }

            return Json(new { result = true, Errors });
        }
        #endregion

        #region DU LIEU DU AN CONG
        [Route(Constants.KeHoachTongHopLink.DuLieuDuAnCong)]
        public IActionResult DuLieuDuAnCong()
        {
            var viewModel = new KhoViewModel()
            {
                Name = "Dự án công"
            };
            return View(viewModel);
        }

        #endregion

        #region SUB
        private void InitProduct(int type, string productname, string productcode, string productunit, out string alias, out string productid)
        {
            alias = Utility.AliasConvert(productname);
            productid = string.Empty;
            var existProduct = dbContext.Products.Find(m => m.Enable.Equals(true) && m.Name.Equals(productname) && m.TypeId.Equals(type)).FirstOrDefault();
            if (existProduct != null)
            {
                productid = existProduct.Id;
            }
            else
            {
                var unitId = string.Empty;
                var existUnit = dbContext.Units.Find(m => m.Enable.Equals(true) && m.Name.Equals(productunit) && m.Type.Equals(type)).FirstOrDefault();
                if (existUnit != null)
                {
                    unitId = existUnit.Id;
                }
                else
                {
                    var unitE = new Unit()
                    {
                        Type = type.ToString(),
                        Name = productunit,
                        Alias = Utility.AliasConvert(productunit)
                    };
                    dbContext.Units.InsertOne(unitE);
                    unitId = unitE.Id;
                }
                var product = new Product()
                {
                    TypeId = type,
                    Code = productcode,
                    Name = productname,
                    Alias = alias,
                    UnitId = unitId
                };
                dbContext.Products.InsertOne(product);
                productid = product.Id;
            }
        }

        private void InitCustomer(int type, string name, out string customeralias, out string customerid)
        {
            customeralias = Utility.AliasConvert(name);
            customerid = string.Empty;
            var existE = dbContext.Customers.Find(m => m.Enable.Equals(true) && m.Name.Equals(name) && m.Type.Equals(type)).FirstOrDefault();
            if (existE != null)
            {
                customerid = existE.Id;
            }
            else
            {
                var entity = new Customer()
                {
                    Type = type,
                    Name = name,
                    Alias = customeralias
                };
                dbContext.Customers.InsertOne(entity);
                customerid = entity.Id;
            }
        }

        private void InitCustomerChild(int type, string name, string parentname, out string customerchildalias, out string customerchildid)
        {
            customerchildalias = Utility.AliasConvert(name);
            customerchildid = string.Empty;
            var existE = dbContext.Customers.Find(m => m.Enable.Equals(true) && m.Name.Equals(name) && m.Type.Equals(type)).FirstOrDefault();
            if (existE != null)
            {
                customerchildid = existE.Id;
            }
            else
            {
                var parentE = dbContext.Customers.Find(m => m.Enable.Equals(true) && m.Name.Equals(parentname) && m.Type.Equals(type)).FirstOrDefault();
                var parentId = string.Empty;
                if (parentE != null)
                {
                    parentId = parentE.Id;
                }
                else
                {
                    parentE = new Customer()
                    {
                        Type = type,
                        Name = parentname,
                        Alias = Utility.AliasConvert(parentname)
                    };
                    dbContext.Customers.InsertOne(parentE);
                    parentId = parentE.Id;
                }

                var entity = new Customer()
                {
                    Type = type,
                    Name = name,
                    Alias = customerchildalias,
                    ParentId = parentId
                };
                dbContext.Customers.InsertOne(entity);
                customerchildid = entity.Id;
            }
        }

        private void InitHoChua(string name, out string hochuaalias)
        {
            hochuaalias = Utility.AliasConvert(name);
            var existE = dbContext.HoChuas.Find(m => m.Enable.Equals(true) && m.Name.Equals(name)).FirstOrDefault();
            if (existE == null)
            {
                var entity = new HoChua()
                {
                    Name = name,
                    Alias = hochuaalias
                };
                dbContext.HoChuas.InsertOne(entity);
            }
        }
        #endregion
    }
}
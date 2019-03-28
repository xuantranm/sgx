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
        public async Task<IActionResult> DuLieuKhoNguyenLieu(DateTime Tu, DateTime Den, string Hang, string TrangThai)
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
            if (Den < Constants.MinDate || Den < Tu)
            {
                Den = Tu;
            }
            linkCurrent += !string.IsNullOrEmpty(linkCurrent) ? "&" : "?";
            linkCurrent += "Tu=" + Tu.ToString("MM-dd-yyyy") + "&Den=" + Den.ToString("MM-dd-yyyy");
            #endregion

            var type = (int)EKho.NguyenLieu;
            var trangthais = dbContext.TrangThais.Find(m => m.Enable.Equals(true) && m.TypeId.Equals((int)ETrangThai.Kho)).ToList();
            var products = dbContext.Products.Find(m => m.Enable.Equals(true) && m.TypeId.Equals(type)).ToList();

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
            var khonguyenlieus = dbContext.KhoNguyenLieus.Find(filter).SortBy(m => m.Date).SortBy(m => m.ProductName).ToList();
            var viewModel = new KhoViewModel()
            {
                Name = "Kho nguyên liệu",
                KhoNguyenLieus = khonguyenlieus,
                TrangThais = trangthais,
                Products = products,
                Tu = Tu,
                Den = Den,
                Hang = Hang,
                TrangThai = TrangThai,
                LinkCurrent = linkCurrent
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.BangChamCong, (int)ERights.View)))
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
            cell.SetCellValue("Thông tin được cập nhật từ dòng số " + (rowIndex + 4));
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

            int lastColumNum = sheet1.GetRow(rowIndex).LastCellNum;
            for (int i = 0; i <= lastColumNum; i++)
            {
                sheet1.AutoSizeColumn(i);
                GC.Collect();
            }

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

                    var timeRow = sheet.GetRow(5);
                    var time = Utility.GetFormattedCellValue(timeRow.GetCell(1));
                    var endMonthDate = Utility.GetSalaryToDate(time);
                    var month = endMonthDate.Month;
                    var year = endMonthDate.Year;
                    var toDate = new DateTime(year, month, 25);
                    var fromDate = toDate.AddMonths(-1).AddDays(1);
                    var fromToNum = Convert.ToInt32((toDate - fromDate).TotalDays);

                    for (int i = 8; i <= sheet.LastRowNum; i++)
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
                            InitProduct(productname, productcode, productunit, out string alias, out string productid);
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
        #endregion

        #region KHO BUN
        #endregion

        #region KHO XU LY
        #endregion

        #region TIEP NHAN XU LY BUN

        #endregion

        #region DU LIEU DU AN CONG
        [Route(Constants.KeHoachTongHopLink.DuLieuDuAnCong)]
        public IActionResult DuLieuDuAnCong()
        {
            return View();
        }

        #endregion

        #region SUB
        private void InitProduct(string productname, string productcode, string productunit, out string alias, out string productid)
        {
            alias = Utility.AliasConvert(productname);
            var type = (int)EKho.NguyenLieu;
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
        #endregion
    }
}
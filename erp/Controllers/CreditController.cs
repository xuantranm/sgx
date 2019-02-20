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
    [Route(Constants.LinkCredit.Main)]
    public class CreditController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public CreditController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<CreditController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _logger = logger;
        }

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

        [Route(Constants.LinkCredit.Credits)]
        public async Task<IActionResult> Credit(string thang, string khoi, string id)
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

            #region DDL
            var sortTimes = Utility.DllMonths();
            #endregion

            #region Times
            var toDate = Utility.GetToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            var year = toDate.Year;
            var month = toDate.Month;
            #endregion

            #region Filter
            var builder = Builders<CreditEmployee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!string.IsNullOrEmpty(thang))
            {
                filter = filter & builder.Eq(m => m.Month, month) & builder.Eq(m => m.Year, year);
            }
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.EmployeeId, id.Trim());
            }
            if (!string.IsNullOrEmpty(khoi))
            {
                var intKhoi = (int)EKhoiLamViec.VP;
                switch (khoi)
                {
                    case "SX":
                        intKhoi = (int)EKhoiLamViec.SX;
                        break;
                    case "NM":
                        intKhoi = (int)EKhoiLamViec.NM;
                        break;
                }
                filter = filter & builder.Eq(x => x.EmployeeKhoi, intKhoi);
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<CreditEmployee>.Sort.Ascending(m => m.UpdatedOn);
            #endregion

            var credits = await dbContext.CreditEmployees.Find(filter).Sort(sortBuilder).ToListAsync();
            var viewModel = new BangLuongViewModel
            {
                Credits = credits,
                MonthYears = sortTimes,
                Thang = thang
            };

            return View(viewModel);
        }

        
        [Route(Constants.LinkCredit.Credits + "/" + Constants.ActionLink.Update)]
        public async Task<IActionResult> CreditUpdate()
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

            var credits = new List<CreditEmployee>();
            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true) & !m.UserName.Equals(Constants.System.account)).ToListAsync();
            foreach (var employee in employees)
            {
                decimal mucthanhtoanhangthang = 0;
                var credit = await dbContext.CreditEmployees.Find(m => m.EmployeeId.Equals(employee.Id)).FirstOrDefaultAsync();
                if (credit != null)
                {
                    mucthanhtoanhangthang = credit.MucThanhToanHangThang;
                    credits.Add(new CreditEmployee
                    {
                        Id = credit.Id,
                        EmployeeId = employee.Id,
                        EmployeeCode = employee.CodeOld,
                        FullName = employee.FullName,
                        EmployeeTitle = employee.SalaryChucVu,
                        MucThanhToanHangThang = mucthanhtoanhangthang
                    });
                }
                else
                {
                    var creditItem = new CreditEmployee
                    {
                        EmployeeId = employee.Id,
                        EmployeeCode = employee.CodeOld,
                        FullName = employee.FullName,
                        EmployeeTitle = employee.SalaryChucVu,
                        MucThanhToanHangThang = mucthanhtoanhangthang
                    };
                    credits.Add(creditItem);
                    dbContext.CreditEmployees.InsertOne(creditItem);
                }
            }

            var viewModel = new BangLuongViewModel
            {
                Credits = credits
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.Credits + "/" + Constants.ActionLink.Update)]
        public async Task<IActionResult> CreditUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.Credits)
            {
                var builder = Builders<CreditEmployee>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<CreditEmployee>.Update
                    .Set(m => m.MucThanhToanHangThang, item.MucThanhToanHangThang)
                    .Set(m => m.Status, item.Status)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.CreditEmployees.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        #region TEMPLATES
        //[Route(Constants.LinkCredit.Template)]
        //public async Task<IActionResult> Template(string fileName)
        //{
        //    string exportFolder = Path.Combine(_env.WebRootPath, "exports");
        //    string sFileName = @"du-lieu-tam-ung-thang-" + DateTime.Now.Month +"-"+ DateTime.Now.Year + "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
        //    string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
        //    FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
        //    var memory = new MemoryStream();
        //    using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
        //    {
        //        IWorkbook workbook = new XSSFWorkbook();
        //        #region Styling
        //        var cellStyleBorder = workbook.CreateCellStyle();
        //        cellStyleBorder.BorderBottom = BorderStyle.Thin;
        //        cellStyleBorder.BorderLeft = BorderStyle.Thin;
        //        cellStyleBorder.BorderRight = BorderStyle.Thin;
        //        cellStyleBorder.BorderTop = BorderStyle.Thin;
        //        cellStyleBorder.Alignment = HorizontalAlignment.Center;
        //        cellStyleBorder.VerticalAlignment = VerticalAlignment.Center;

        //        var cellStyleHeader = workbook.CreateCellStyle();
        //        //cellStyleHeader.CloneStyleFrom(cellStyleBorder);
        //        //cellStyleHeader.FillForegroundColor = HSSFColor.Blue.Index2;
        //        cellStyleHeader.FillForegroundColor = HSSFColor.Grey25Percent.Index;
        //        cellStyleHeader.FillPattern = FillPattern.SolidForeground;

        //        var font = workbook.CreateFont();
        //        font.FontHeightInPoints = 11;
        //        //font.FontName = "Calibri";
        //        font.Boldweight = (short)FontBoldWeight.Bold;
        //        #endregion

        //        ISheet sheet1 = workbook.CreateSheet("TAMUNG" + DateTime.Now.Month.ToString("00"));
        //        //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 3, 7));
        //        //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 8, 13));

        //        var rowIndex = 0;
        //        IRow row = sheet1.CreateRow(rowIndex);
        //        row.CreateCell(0, CellType.String).SetCellValue("TẠM ỨNG NHÀ MÁY");
        //        rowIndex++;

        //        row = sheet1.CreateRow(rowIndex);
        //        row.CreateCell(0, CellType.String).SetCellValue("Tháng");
        //        row.CreateCell(1, CellType.Numeric).SetCellValue(DateTime.Now.Month);
        //        row.CreateCell(2, CellType.String).SetCellValue("Năm");
        //        row.CreateCell(3, CellType.Numeric).SetCellValue(DateTime.Now.Year);
        //        // Set style
        //        for (int i = 0; i <= 3; i++)
        //        {
        //            row.Cells[i].CellStyle = cellStyleHeader;
        //        }
        //        row.Cells[1].CellStyle.SetFont(font);
        //        row.Cells[3].CellStyle.SetFont(font);
        //        rowIndex++;
        //        //https://stackoverflow.com/questions/51681846/rowspan-and-colspan-in-apache-poi

        //        row = sheet1.CreateRow(rowIndex);
        //        row.CreateCell(0, CellType.String).SetCellValue("#");
        //        row.CreateCell(1, CellType.String).SetCellValue("Mã nhân viên");
        //        row.CreateCell(2, CellType.String).SetCellValue("Họ tên");
        //        row.CreateCell(3, CellType.String).SetCellValue("Chức vụ");
        //        row.CreateCell(4, CellType.String).SetCellValue("Ứng lương");
        //        // Set style
        //        for (int i = 0; i <= 4; i++)
        //        {
        //            row.Cells[i].CellStyle = cellStyleHeader;
        //        }


        //        for (int i = 1; i <= 50; i++)
        //        {
        //            row = sheet1.CreateRow(rowIndex + 1);
        //            //row.CreateCell(0, CellType.Numeric).SetCellValue(i);
        //            row.CreateCell(1, CellType.String).SetCellValue(string.Empty);
        //            row.CreateCell(2, CellType.String).SetCellValue(string.Empty);
        //            row.CreateCell(3, CellType.String).SetCellValue(string.Empty);
        //            row.CreateCell(4, CellType.Numeric).SetCellValue(string.Empty);
        //            rowIndex++;
        //        }

        //        workbook.Write(fs);
        //    }
        //    using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
        //    {
        //        await stream.CopyToAsync(memory);
        //    }
        //    memory.Position = 0;
        //    return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        //}

        //[Route(Constants.LinkCredit.CreditImport)]
        //[HttpPost]
        //public ActionResult CreditImport()
        //{
        //    IFormFile file = Request.Form.Files[0];
        //    string folderName = Constants.Storage.Hr;
        //    string webRootPath = _env.WebRootPath;
        //    string newPath = Path.Combine(webRootPath, folderName);
        //    if (!Directory.Exists(newPath))
        //    {
        //        Directory.CreateDirectory(newPath);
        //    }
        //    if (file.Length > 0)
        //    {
        //        string sFileExtension = Path.GetExtension(file.FileName).ToLower();
        //        ISheet sheet;
        //        string fullPath = Path.Combine(newPath, file.FileName);
        //        using (var stream = new FileStream(fullPath, FileMode.Create))
        //        {
        //            file.CopyTo(stream);
        //            stream.Position = 0;
        //            if (sFileExtension == ".xls")
        //            {
        //                HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
        //                sheet = hssfwb.GetSheetAt(0);
        //            }
        //            else
        //            {
        //                XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
        //                sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
        //            }

        //            var timeRow = sheet.GetRow(1);
        //            var month = Convert.ToInt32(Utility.GetNumbericCellValue(timeRow.GetCell(1)));
        //            var year = Convert.ToInt32(Utility.GetNumbericCellValue(timeRow.GetCell(3)));
        //            if (month == 0)
        //            {
        //                month = DateTime.Now.Month;
        //            }
        //            if (year == 0)
        //            {
        //                year = DateTime.Now.Year;
        //            }

        //            for (int i = 3; i <= sheet.LastRowNum; i++)
        //            {
        //                IRow row = sheet.GetRow(i);
        //                if (row == null) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

        //                var code = Utility.GetFormattedCellValue(row.GetCell(1));
        //                var fullName = Utility.GetFormattedCellValue(row.GetCell(2));
        //                var alias = Utility.AliasConvert(fullName);
        //                var title = Utility.GetFormattedCellValue(row.GetCell(3));

        //                decimal ungluong = (decimal)Utility.GetNumbericCellValue(row.GetCell(4));
                       
        //                var employee = new Employee();
        //                if (!string.IsNullOrEmpty(alias))
        //                {
        //                    employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.AliasFullName.Equals(alias)).FirstOrDefault();
        //                }
        //                if (employee == null)
        //                {
        //                    if (!string.IsNullOrEmpty(fullName))
        //                    {
        //                        employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.FullName.Equals(fullName)).FirstOrDefault();
        //                    }
        //                }
        //                if (employee == null)
        //                {
        //                    if (!string.IsNullOrEmpty(code))
        //                    {
        //                        employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.CodeOld.Equals(code)).FirstOrDefault();
        //                    }
        //                }
        //                if (employee != null)
        //                {
        //                    if (ungluong > 0)
        //                    {
        //                        // check exist to update
        //                        var existEntity = dbContext.CreditEmployees.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
        //                        if (existEntity != null)
        //                        {
        //                            var builder = Builders<CreditEmployee>.Filter;
        //                            var filter = builder.Eq(m => m.Id, existEntity.Id);
        //                            var update = Builders<CreditEmployee>.Update
        //                                .Set(m => m.EmployeeCode, employee.Code)
        //                                .Set(m => m.FullName, employee.FullName)
        //                                .Set(m => m.EmployeeTitle, employee.Title)
        //                                .Set(m => m.EmployeeDepartment, employee.DepartmentId)
        //                                .Set(m => m.EmployeePart, employee.PartId)
        //                                .Set(m => m.Money, ungluong)
        //                                .Set(m => m.UpdatedOn, DateTime.Now);

        //                            dbContext.CreditEmployees.UpdateOne(filter, update);
        //                        }
        //                        else
        //                        {
        //                            var newItem = new CreditEmployee
        //                            {
        //                                Year = year,
        //                                Month = month,
        //                                EmployeeId = employee.Id,
        //                                EmployeeCode = employee.Code,
        //                                FullName = employee.FullName,
        //                                EmployeeTitle = employee.Title,
        //                                EmployeePart = employee.PartId,
        //                                EmployeeDepartment = employee.DepartmentId,
        //                                Type = (int)ECredit.UngLuong,
        //                                Money = ungluong
        //                            };
        //                            dbContext.CreditEmployees.InsertOne(newItem);
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    dbContext.Misss.InsertOne(new Miss
        //                    {
        //                        Type = "nha-may-credit-import",
        //                        Object = "time: " + month + "-" + year + ", code: " + code + "-" + fullName + "-" + title + " ,dòng " + i,
        //                        Error = "No import data",
        //                        DateTime = DateTime.Now.ToString()
        //                    });
        //                }
        //            }
        //        }
        //    }
        //    return Json(new { url = "/" + Constants.LinkCredit.Main + "/" + Constants.LinkCredit.CreditsNM });
        //}



        [Route(Constants.LinkCredit.Template)]
        public async Task<IActionResult> Template(string thang)
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
                cell.SetCellValue("Trừ tạm ứng");
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

        [Route(Constants.LinkCredit.CreditImport)]
        [HttpPost]
        public ActionResult CreditImport()
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


                    for (int i = 11; i <= sheet.LastRowNum; i++)
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
                        decimal tamung = 0;
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
                        tamung = (decimal)Utility.GetNumbericCellValue(row.GetCell(columnIndex));
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
                            //DataSanXuat(month, year, ngaylamviec, phepnam, letet, tamung, thuongletet, bhxh, existEmployee);
                        }
                        else
                        {
                            //InsertNewEmployee(ten, ma, chucvu, ngayvaolam);
                            var employee = dbContext.Employees.Find(m => m.FullName.Equals(ten)).FirstOrDefault();
                            //DataSanXuat(month, year, ngaylamviec, phepnam, letet, tamung, thuongletet, bhxh, employee);
                        }
                    }
                }
            }
            return Json(new { result = true });
        }
        #endregion
    }
}
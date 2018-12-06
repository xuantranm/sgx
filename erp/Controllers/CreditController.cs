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
        public async Task<IActionResult> Credit(string thang)
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
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

            var credits = await dbContext.SalaryCredits.Find(m => m.Enable.Equals(true) && m.MucThanhToanHangThang > 0).ToListAsync();
            var viewModel = new BangLuongViewModel
            {
                SalaryCredits = credits,
                MonthYears = sortTimes,
                thang = thang
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

            var credits = new List<SalaryCredit>();
            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true) & !m.UserName.Equals(Constants.System.account)).ToListAsync();
            foreach (var employee in employees)
            {
                decimal mucthanhtoanhangthang = 0;
                var credit = await dbContext.SalaryCredits.Find(m => m.EmployeeId.Equals(employee.Id)).FirstOrDefaultAsync();
                if (credit != null)
                {
                    mucthanhtoanhangthang = credit.MucThanhToanHangThang;
                    credits.Add(new SalaryCredit
                    {
                        Id = credit.Id,
                        EmployeeId = employee.Id,
                        MaNhanVien = employee.CodeOld,
                        FullName = employee.FullName,
                        ChucVu = employee.SalaryChucVu,
                        MucThanhToanHangThang = mucthanhtoanhangthang
                    });
                }
                else
                {
                    var creditItem = new SalaryCredit
                    {
                        EmployeeId = employee.Id,
                        MaNhanVien = employee.CodeOld,
                        FullName = employee.FullName,
                        ChucVu = employee.SalaryChucVu,
                        MucThanhToanHangThang = mucthanhtoanhangthang
                    };
                    credits.Add(creditItem);
                    dbContext.SalaryCredits.InsertOne(creditItem);
                }
            }

            var viewModel = new BangLuongViewModel
            {
                SalaryCredits = credits
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

            foreach (var item in viewModel.SalaryCredits)
            {
                var builder = Builders<SalaryCredit>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<SalaryCredit>.Update
                    .Set(m => m.MucThanhToanHangThang, item.MucThanhToanHangThang)
                    .Set(m => m.Status, item.Status)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalaryCredits.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        #region NHA MAY
        [Route(Constants.LinkCredit.CreditsNM)]
        public async Task<IActionResult> CreditNM(string thang, string id)
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
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

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true) & builder.Eq(m => m.SalaryType, (int)ESalaryType.NM) & !builder.Eq(m => m.UserName, Constants.System.account);
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.Id, id.Trim());
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Employee>.Sort.Ascending(m => m.Code);
            #endregion

            var employees = await dbContext.Employees.Find(filter).Sort(sortBuilder).ToListAsync();

            var credits = await dbContext.SalaryCredits.Find(m => m.Enable.Equals(true) && m.Month.Equals(month) && m.Year.Equals(year) && m.UngLuong > 0).ToListAsync();

            var creditsNM = credits.Where(c => !employees.Any(e => e.EmployeeId == c.EmployeeId)).ToList();
            var viewModel = new BangLuongViewModel
            {
                SalaryCredits = creditsNM,
                MonthYears = sortTimes,
                thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkCredit.CreditsNM + "/" + Constants.ActionLink.Update)]
        public async Task<IActionResult> CreditNMUpdate(string thang, string id)
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
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

            var credits = new List<SalaryCredit>();

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true) & builder.Eq(m => m.SalaryType, (int)ESalaryType.NM) & !builder.Eq(m => m.UserName, Constants.System.account);
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.Id, id.Trim());
            }
            #endregion

            #region Sort
            var sortBuilder = Builders<Employee>.Sort.Ascending(m => m.Code);
            #endregion

            var employees = await dbContext.Employees.Find(filter).Sort(sortBuilder).ToListAsync();
            foreach (var employee in employees)
            {
                decimal ungluong = 0;
                var credit = await dbContext.SalaryCredits.Find(m => m.EmployeeId.Equals(employee.Id)).FirstOrDefaultAsync();
                if (credit != null)
                {
                    ungluong = credit.UngLuong;
                    credits.Add(new SalaryCredit
                    {
                        Id = credit.Id,
                        EmployeeId = employee.Id,
                        MaNhanVien = employee.CodeOld,
                        FullName = employee.FullName,
                        ChucVu = employee.SalaryChucVu,
                        UngLuong = ungluong
                    });
                }
                else
                {
                    var creditItem = new SalaryCredit
                    {
                        EmployeeId = employee.Id,
                        MaNhanVien = employee.CodeOld,
                        FullName = employee.FullName,
                        ChucVu = employee.SalaryChucVu,
                        UngLuong = ungluong
                    };
                    credits.Add(creditItem);
                    dbContext.SalaryCredits.InsertOne(creditItem);
                }
            }

            var viewModel = new BangLuongViewModel
            {
                SalaryCredits = credits,
                MonthYears = sortTimes,
                thang = thang
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkCredit.CreditsNM + "/" + Constants.ActionLink.Update)]
        public async Task<IActionResult> CreditNMUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.SalaryCredits)
            {
                var builder = Builders<SalaryCredit>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<SalaryCredit>.Update
                    .Set(m => m.UngLuong, item.UngLuong)
                    .Set(m => m.Status, item.Status)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalaryCredits.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }
        #endregion

        #region TEMPLATES
        [Route(Constants.LinkCredit.Template)]
        public async Task<IActionResult> Template(string fileName)
        {
            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"du-lieu-tam-ung-thang-" + DateTime.Now.Month +"-"+ DateTime.Now.Year + "-V" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
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

                ISheet sheet1 = workbook.CreateSheet("TAMUNG" + DateTime.Now.Month.ToString("00"));
                //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 3, 7));
                //sheet1.AddMergedRegion(new CellRangeAddress(2, 2, 8, 13));

                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("TẠM ỨNG NHÀ MÁY");
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
                row.CreateCell(4, CellType.String).SetCellValue("Ứng lương");
                // Set style
                for (int i = 0; i <= 4; i++)
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

                        decimal ungluong = (decimal)Utility.GetNumbericCellValue(row.GetCell(4));
                       
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
                            if (ungluong > 0)
                            {
                                // check exist to update
                                var existEntity = dbContext.SalaryCredits.Find(m => m.Enable.Equals(true) && m.EmployeeId.Equals(employee.Id) && m.Month.Equals(month) && m.Year.Equals(year)).FirstOrDefault();
                                if (existEntity != null)
                                {
                                    var builder = Builders<SalaryCredit>.Filter;
                                    var filter = builder.Eq(m => m.Id, existEntity.Id);
                                    var update = Builders<SalaryCredit>.Update
                                        .Set(m => m.MaNhanVien, employee.Code)
                                        .Set(m => m.FullName, employee.FullName)
                                        .Set(m => m.ChucVu, employee.Title)
                                        .Set(m => m.PhongBan, employee.Department)
                                        .Set(m => m.UngLuong, ungluong)
                                        .Set(m => m.UpdatedOn, DateTime.Now);

                                    dbContext.SalaryCredits.UpdateOne(filter, update);
                                }
                                else
                                {
                                    var newItem = new SalaryCredit
                                    {
                                        Year = year,
                                        Month = month,
                                        EmployeeId = employee.Id,
                                        MaNhanVien = employee.Code,
                                        FullName = employee.FullName,
                                        ChucVu = employee.Title,
                                        PhongBan = employee.Department,
                                        UngLuong = ungluong
                                    };
                                    dbContext.SalaryCredits.InsertOne(newItem);
                                }
                            }
                        }
                        else
                        {
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "nha-may-credit-import",
                                Object = "time: " + month + "-" + year + ", code: " + code + "-" + fullName + "-" + title + " ,dòng " + i,
                                Error = "No import data",
                                DateTime = DateTime.Now.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { url = "/" + Constants.LinkCredit.Main + "/" + Constants.LinkCredit.CreditsNM });
        }

        #endregion
    }
}
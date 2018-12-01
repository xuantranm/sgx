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
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using Microsoft.AspNetCore.Authorization;
using MimeKit;
using Services;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading;
using Common.Enums;
using MongoDB.Bson;
using NPOI.HSSF.Util;
using MimeKit.Text;

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.LinkHr.Main)]
    public class EmployeeController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private bool bhxh;

        public EmployeeController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, IEmailSender emailSender,
            ISmsSender smsSender, ILogger<EmployeeController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        // No paging this, small data
        [Route(Constants.LinkHr.Human)]
        public async Task<IActionResult> Index(string id, string ten, string code, string finger, string nl, /*int? page, int? size,*/ string sortBy)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.NhanSu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region Get Setting Value
            var settings = dbContext.Settings.Find(m => m.Enable.Equals(true)).ToList();
            bhxh = false;
            var bhxhSetting = settings.First(m => m.Key.Equals("NoBHXH"));
            if (bhxhSetting != null)
            {
                bhxh = bhxhSetting.Value == "true" ? false : true;
            }
            #endregion

            #region Dropdownlist
            var sortDepartments = Builders<Department>.Sort.Ascending(m => m.Order);
            var departments = dbContext.Departments.Find(m => m.Enable.Equals(true)).Sort(sortDepartments).ToList();
            ViewData["Departments"] = departments;

            var sortParts = Builders<Part>.Sort.Ascending(m => m.Order);
            var parts = dbContext.Parts.Find(m => m.Enable.Equals(true)).Sort(sortParts).ToList();
            ViewData["Parts"] = parts;

            var employeeDdl = await dbContext.Employees.Find(m => m.Enable.Equals(true) && !m.UserName.Equals(Constants.System.account)).SortBy(m => m.FullName).ToListAsync();
            #endregion

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!string.IsNullOrEmpty(ten))
            {
                filter = filter & (builder.Eq(x => x.Email, ten.Trim()) | builder.Regex(x => x.FullName, ten.Trim()));
            }
            if (!string.IsNullOrEmpty(id))
            {
                filter = filter & builder.Eq(x => x.Id, id.Trim());
            }
            if (!string.IsNullOrEmpty(code))
            {
                filter = filter & builder.Regex(m => m.Code, code.Trim());
            }
            if (!string.IsNullOrEmpty(finger))
            {
                filter = filter & builder.Where(m => m.Workplaces.Any(c => c.Fingerprint == finger.Trim()));
                // Eq("Related._id", "b125");
            }
            if (!string.IsNullOrEmpty(nl))
            {
                filter = filter & builder.Where(m => m.Workplaces.Any(c => c.Code == nl.Trim()));
                // Eq("Related._id", "b125");
            }
            if (bhxh)
            {
                filter = filter & builder.Eq(m => m.BhxhEnable, bhxh);
            }
            filter = filter & !builder.Eq(i => i.UserName, Constants.System.account);
            #endregion

            #region Sort
            var sortBuilder = Builders<Employee>.Sort.Ascending(m => m.Code);
            if (!string.IsNullOrEmpty(sortBy))
            {
                var sortField = sortBy.Split("-")[0];
                var sort = sortBy.Split("-")[1];
                switch (sortField)
                {
                    case "code":
                        sortBuilder = sort == "asc" ? Builders<Employee>.Sort.Ascending(m => m.Code) : Builders<Employee>.Sort.Descending(m => m.Code);
                        break;
                    case "department":
                        sortBuilder = sort == "asc" ? Builders<Employee>.Sort.Ascending(m => m.Department) : Builders<Employee>.Sort.Descending(m => m.Department);
                        break;
                    case "name":
                        sortBuilder = sort == "asc" ? Builders<Employee>.Sort.Ascending(m => m.FullName) : Builders<Employee>.Sort.Descending(m => m.FullName);
                        break;
                    default:
                        sortBuilder = sort == "asc" ? Builders<Employee>.Sort.Ascending(m => m.Code) : Builders<Employee>.Sort.Descending(m => m.Code);
                        break;
                }
            }
            #endregion

            var records = await dbContext.Employees.CountDocumentsAsync(filter);
            var results = await dbContext.Employees.Find(filter).Sort(sortBuilder).ToListAsync();

            // leave data
            var leaves = await dbContext.Employees.Find(m => m.Enable.Equals(false)).ToListAsync();

            var departmentsFilter = new List<Department>();

            foreach (var employee in results)
            {
                if (!string.IsNullOrEmpty(employee.Department))
                {
                    var match = departmentsFilter.FirstOrDefault(m => m.Name.Contains(employee.Department));
                    if (match == null)
                    {
                        var department = departments.Find(m => m.Name.Equals(employee.Department));
                        if (department != null)
                        {
                            departmentsFilter.Add(department);
                        }
                    }
                }
            }

            var viewModel = new EmployeeViewModel
            {
                EmployeesDdl = employeeDdl,
                Employees = results,
                Departments = departmentsFilter,
                EmployeesDisable = leaves,
                Records = (int)records,
                id = id,
                ten = ten,
                code = code,
                finger = finger,
                nl = nl
            };

            return View(viewModel);
        }

        [Route(Constants.LinkHr.Human + "/" + Constants.LinkHr.List + "/" + Constants.LinkHr.Export)]
        public async Task<IActionResult> Export(string ten, string code, string finger, string nl, /*int? page, int? size,*/ string sortBy)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.NhanSu, (int)ERights.View)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #endregion

            #region Get Setting Value
            var settings = dbContext.Settings.Find(m => m.Enable.Equals(true)).ToList();
            bhxh = false;
            var bhxhSetting = settings.First(m => m.Key.Equals("NoBHXH"));
            if (bhxhSetting != null)
            {
                bhxh = bhxhSetting.Value == "true" ? false : true;
            }
            #endregion

            #region Dropdownlist
            var sortDepartments = Builders<Department>.Sort.Ascending(m => m.Order);
            var departments = dbContext.Departments.Find(m => m.Enable.Equals(true)).Sort(sortDepartments).ToList();
            ViewData["Departments"] = departments;

            var sortParts = Builders<Part>.Sort.Ascending(m => m.Order);
            var parts = dbContext.Parts.Find(m => m.Enable.Equals(true)).Sort(sortParts).ToList();
            ViewData["Parts"] = parts;
            #endregion

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            if (!String.IsNullOrEmpty(ten))
            {
                filter = filter & (builder.Eq(x => x.Email, ten) | builder.Regex(x => x.FullName, ten));
            }
            if (!String.IsNullOrEmpty(code))
            {
                filter = filter & builder.Regex(m => m.Code, code);
            }
            if (!String.IsNullOrEmpty(finger))
            {
                filter = filter & builder.Where(m => m.Workplaces.Any(c => c.Fingerprint == finger));
                // Eq("Related._id", "b125");
            }
            if (!String.IsNullOrEmpty(nl))
            {
                filter = filter & builder.Where(m => m.Workplaces.Any(c => c.Code == nl));
                // Eq("Related._id", "b125");
            }
            if (bhxh)
            {
                filter = filter & builder.Eq(m => m.BhxhEnable, bhxh);
            }
            filter = filter & !builder.Eq(i => i.UserName, Constants.System.account);
            #endregion

            #region Sort
            var sortBuilder = Builders<Employee>.Sort.Ascending(m => m.Code);
            if (!string.IsNullOrEmpty(sortBy))
            {
                var sortField = sortBy.Split("-")[0];
                var sort = sortBy.Split("-")[1];
                switch (sortField)
                {
                    case "code":
                        sortBuilder = sort == "asc" ? Builders<Employee>.Sort.Ascending(m => m.Code) : Builders<Employee>.Sort.Descending(m => m.Code);
                        break;
                    case "department":
                        sortBuilder = sort == "asc" ? Builders<Employee>.Sort.Ascending(m => m.Department) : Builders<Employee>.Sort.Descending(m => m.Department);
                        break;
                    case "name":
                        sortBuilder = sort == "asc" ? Builders<Employee>.Sort.Ascending(m => m.FullName) : Builders<Employee>.Sort.Descending(m => m.FullName);
                        break;
                    default:
                        sortBuilder = sort == "asc" ? Builders<Employee>.Sort.Ascending(m => m.Code) : Builders<Employee>.Sort.Descending(m => m.Code);
                        break;
                }
            }
            #endregion

            var records = await dbContext.Employees.CountDocumentsAsync(filter);
            var results = await dbContext.Employees.Find(filter).Sort(sortBuilder).ToListAsync();

            // leave data
            var leaves = await dbContext.Employees.Find(m => m.Enable.Equals(false)).ToListAsync();

            var departmentsFilter = new List<Department>();

            foreach (var employee in results)
            {
                if (!string.IsNullOrEmpty(employee.Department))
                {
                    var match = departmentsFilter.FirstOrDefault(m => m.Name.Contains(employee.Department));
                    if (match == null)
                    {
                        var department = departments.Find(m => m.Name.Equals(employee.Department));
                        departmentsFilter.Add(department);
                    }
                }
            }

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"hanh-chinh-nhan-su-" + DateTime.Now.ToString("ddMMyyyyhhmm") + ".xlsx";
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
                #endregion

                ISheet sheet1 = workbook.CreateSheet("Danh-sach");

                //sheet1.AddMergedRegion(new CellRangeAddress(0, 0, 0, 10));
                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("STT");
                row.CreateCell(1, CellType.String).SetCellValue("Mã");
                row.CreateCell(2, CellType.String).SetCellValue("Họ tên");
                row.CreateCell(3, CellType.String).SetCellValue("Email");
                row.CreateCell(4, CellType.String).SetCellValue("Điện thoại bàn");
                row.CreateCell(5, CellType.String).SetCellValue("Điện thoại");
                row.CreateCell(6, CellType.String).SetCellValue("Nơi làm việc");
                row.CreateCell(7, CellType.String).SetCellValue("Chấm công");
                row.CreateCell(8, CellType.String).SetCellValue("Mã chấm công");
                row.CreateCell(9, CellType.String).SetCellValue("Thời gian làm việc");
                row.CreateCell(10, CellType.String).SetCellValue("Mức phép năm");
                row.CreateCell(11, CellType.String).SetCellValue("Ngày sinh");
                row.CreateCell(12, CellType.String).SetCellValue("Giới tính");
                row.CreateCell(13, CellType.String).SetCellValue("Ngày vào làm");
                row.CreateCell(14, CellType.String).SetCellValue("Nguyên quán");
                row.CreateCell(15, CellType.String).SetCellValue("Thường trú");
                row.CreateCell(16, CellType.String).SetCellValue("Tạm trú");
                row.CreateCell(17, CellType.String).SetCellValue("Bộ phận");
                row.CreateCell(18, CellType.String).SetCellValue("Phòng ban");
                row.CreateCell(19, CellType.String).SetCellValue("Chức vụ");
                row.CreateCell(20, CellType.String).SetCellValue("CMND");
                row.CreateCell(21, CellType.String).SetCellValue("Ngày cấp");
                row.CreateCell(22, CellType.String).SetCellValue("Nơi cấp");
                row.CreateCell(23, CellType.String).SetCellValue("Số Hộ khẩu");
                row.CreateCell(24, CellType.String).SetCellValue("Chủ hộ");
                row.CreateCell(25, CellType.String).SetCellValue("Dân tộc");
                row.CreateCell(26, CellType.String).SetCellValue("Tôn giáo");
                row.CreateCell(27, CellType.String).SetCellValue("Số xổ BHXH");
                row.CreateCell(28, CellType.String).SetCellValue("Mã số BHXH");
                row.CreateCell(29, CellType.String).SetCellValue("Nơi KCB ban đầu");
                row.CreateCell(30, CellType.String).SetCellValue("Cơ quan BHXH");
                row.CreateCell(31, CellType.String).SetCellValue("Mã số BHYT");
                row.CreateCell(32, CellType.String).SetCellValue("Quản lý");
                // Set style
                for (int i = 0; i <= 31; i++)
                {
                    row.Cells[i].CellStyle = cellStyleHeader;
                }
                rowIndex++;

                foreach (var data in results)
                {
                    row = sheet1.CreateRow(rowIndex);
                    row.CreateCell(0, CellType.Numeric).SetCellValue(rowIndex);
                    row.CreateCell(1, CellType.String).SetCellValue(data.CodeOld + " (" + data.Code + ")");
                    row.CreateCell(2, CellType.String).SetCellValue(data.FullName);
                    row.CreateCell(3, CellType.String).SetCellValue(data.Email);
                    row.CreateCell(4, CellType.String).SetCellValue(data.Tel);
                    var mobiles = string.Empty;
                    if (data.Mobiles != null && data.Mobiles.Count > 0)
                    {
                        foreach (var mobile in data.Mobiles)
                        {
                            if (!string.IsNullOrEmpty(mobiles))
                            {
                                mobiles += " - ";
                            }
                            mobiles += mobile.Number;
                        }
                    }
                    row.CreateCell(5, CellType.String).SetCellValue(mobiles);

                    var workplaces = string.Empty;
                    var chamcongs = string.Empty;
                    var thoigianlamviec = string.Empty;
                    if (data.Workplaces != null && data.Workplaces.Count > 0)
                    {
                        foreach (var workplace in data.Workplaces)
                        {
                            if (!string.IsNullOrEmpty(workplace.Name))
                            {
                                if (!string.IsNullOrEmpty(workplaces))
                                {
                                    workplaces += " - ";
                                }
                                workplaces += workplace.Name;
                            }
                            if (!string.IsNullOrEmpty(workplace.Fingerprint))
                            {
                                if (!string.IsNullOrEmpty(chamcongs))
                                {
                                    chamcongs += " - ";
                                }
                                chamcongs += workplace.Fingerprint;
                            }
                            if (!string.IsNullOrEmpty(workplace.WorkingScheduleTime))
                            {
                                if (!string.IsNullOrEmpty(thoigianlamviec))
                                {
                                    thoigianlamviec += " - ";
                                }
                                thoigianlamviec += workplace.WorkingScheduleTime;
                            }
                        }
                    }
                    row.CreateCell(6, CellType.String).SetCellValue(workplaces);
                    row.CreateCell(7, CellType.String).SetCellValue(data.IsTimeKeeper ? "Không" : "Có");
                    row.CreateCell(8, CellType.String).SetCellValue(chamcongs);
                    row.CreateCell(9, CellType.String).SetCellValue(thoigianlamviec);

                    row.CreateCell(10, CellType.String).SetCellValue(data.LeaveLevelYear.ToString());
                    row.CreateCell(11, CellType.String).SetCellValue(data.Birthday.ToString("dd/MM/yyyy"));
                    row.CreateCell(12, CellType.String).SetCellValue(data.Gender);
                    row.CreateCell(13, CellType.String).SetCellValue(data.Joinday.ToString("dd/MM/yyyy"));
                    row.CreateCell(14, CellType.String).SetCellValue(data.Bornplace);
                    row.CreateCell(15, CellType.String).SetCellValue(data.AddressResident);
                    row.CreateCell(16, CellType.String).SetCellValue(data.AddressTemporary);

                    row.CreateCell(17, CellType.String).SetCellValue(data.Part);
                    row.CreateCell(18, CellType.String).SetCellValue(data.Department);
                    row.CreateCell(19, CellType.String).SetCellValue(data.Title);
                    row.CreateCell(20, CellType.String).SetCellValue(data.IdentityCard);
                    row.CreateCell(21, CellType.String).SetCellValue(data.IdentityCardDate.HasValue ? data.IdentityCardDate.Value.ToString("dd/MM/yyyy") : string.Empty);
                    row.CreateCell(22, CellType.String).SetCellValue(data.IdentityCardPlace);
                    row.CreateCell(23, CellType.String).SetCellValue(data.HouseHold);
                    row.CreateCell(24, CellType.String).SetCellValue(data.HouseHoldOwner);
                    row.CreateCell(25, CellType.String).SetCellValue(data.Nation);
                    row.CreateCell(26, CellType.String).SetCellValue(data.Religion);
                    row.CreateCell(27, CellType.String).SetCellValue(data.BhxhBookNo);
                    row.CreateCell(28, CellType.String).SetCellValue(data.BhxhCode);
                    row.CreateCell(29, CellType.String).SetCellValue(data.BhxhHospital);
                    row.CreateCell(30, CellType.String).SetCellValue(data.BhxhLocation);
                    row.CreateCell(31, CellType.String).SetCellValue(data.BhytCode);
                    var manage = string.Empty;
                    if (!string.IsNullOrEmpty(data.ManagerId))
                    {
                        var managerEntity = dbContext.Employees.Find(m => m.Id.Equals(data.ManagerId)).FirstOrDefault();
                        if (managerEntity != null)
                        {
                            manage = managerEntity.FullName;
                        }
                    }
                    row.CreateCell(31, CellType.String).SetCellValue(manage);
                    //sheet1.AutoSizeColumn(0);
                    rowIndex++;
                }

                var sheet2 = workbook.CreateSheet("Phong-ban");
                var style1 = workbook.CreateCellStyle();
                style1.FillForegroundColor = HSSFColor.Blue.Index2;
                style1.FillPattern = FillPattern.SolidForeground;

                var style2 = workbook.CreateCellStyle();
                style2.FillForegroundColor = HSSFColor.Yellow.Index2;
                style2.FillPattern = FillPattern.SolidForeground;

                var cell2 = sheet2.CreateRow(0).CreateCell(0);
                cell2.CellStyle = style1;
                cell2.SetCellValue(0);

                cell2 = sheet2.CreateRow(1).CreateCell(0);
                cell2.CellStyle = style2;
                cell2.SetCellValue(1);

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        [Route(Constants.LinkHr.Birthday + "/" + Constants.LinkHr.List)]
        public async Task<IActionResult> Birthday()
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

            //if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
            //{
            //    return RedirectToAction("AccessDenied", "Account");
            //}

            #endregion

            var birthdays = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Birthday > Constants.MinDate).ToEnumerable()
                .OrderBy(m => m.RemainingBirthDays).ToList();

            var viewModel = new BirthdayViewModel()
            {
                Employees = birthdays
            };
            return View(viewModel);
        }

        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        [Route("/sys/login-as/")]
        public async Task<IActionResult> Login(string userName)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (true)
            {
                var result = dbContext.Employees.Find(m => m.Enable.Equals(true)
                                                        && m.IsOnline.Equals(true)
                                                        && m.UserName.Equals(userName))
                                                        .FirstOrDefault();
                // Write log, perfomance...
                if (result != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim("UserName", result.UserName),
                        new Claim(ClaimTypes.Name, result.Id),
                        new Claim(ClaimTypes.Email, string.IsNullOrEmpty(result.Email)? string.Empty : result.Email),
                        new Claim("FullName", result.FullName),
                        new Claim(ClaimTypes.AuthenticationMethod, "sys")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        //AllowRefresh = <bool>,
                        // Refreshing the authentication session should be allowed.

                        //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                        // The time at which the authentication ticket expires. A 
                        // value set here overrides the ExpireTimeSpan option of 
                        // CookieAuthenticationOptions set with AddCookie.

                        //IsPersistent = true,
                        // Whether the authentication session is persisted across 
                        // multiple requests. Required when setting the 
                        // ExpireTimeSpan option of CookieAuthenticationOptions 
                        // set with AddCookie. Also required when setting 
                        // ExpiresUtc.

                        //IssuedUtc = <DateTimeOffset>,
                        // The time at which the authentication ticket was issued.

                        //RedirectUri = <string>
                        // The full path or absolute URI to be used as an http 
                        // redirect response value.
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Index");
                }
            }

            return RedirectToAction("Index");
        }

        // GET: Users/Details/5
        [Route(Constants.LinkHr.Human + "/" + Constants.LinkHr.Information + "/" + "{id}")]
        public async Task<ActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            // Check owner
            if (id != login)
            {
                if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.NhanSu, (int)ERights.View)))
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            #region Check salary right
            var salaryRight = (int)ERights.None;
            if (id != login)
            {
                if (loginUserName == Constants.System.account)
                {
                    salaryRight = (int)ERights.History;
                }
                else
                {
                    if (Utility.IsRight(login, "luong", (int)ERights.Edit))
                    {
                        salaryRight = (int)ERights.Edit;
                    }
                    else if (Utility.IsRight(login, "luong", (int)ERights.View))
                    {
                        salaryRight = (int)ERights.View;
                    }
                }
            }
            else
            {
                salaryRight = (int)ERights.View;
            }
            ViewData["SalatyRight"] = salaryRight;
            #endregion

            #endregion

            var entity = await dbContext.Employees
                .Find(m => m.Id == id).FirstOrDefaultAsync();

            if (entity == null)
            {
                return NotFound();
            }

            #region Dropdownlist
            var parts = dbContext.Parts.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Parts"] = parts;
            var departments = dbContext.Departments.Find(m => m.Enable.Equals(true) && !m.Name.Equals(Constants.System.department)).ToList();
            ViewData["Departments"] = departments;
            var titles = dbContext.Titles.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Titles"] = titles;
            var roles = dbContext.Roles.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Roles"] = roles;
            var hospitals = dbContext.BHYTHospitals.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Hospitals"] = hospitals;
            var contracts = dbContext.ContractTypes.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Contracts"] = contracts;
            var workTimeTypes = dbContext.WorkTimeTypes.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["WorkTimeTypes"] = workTimeTypes;
            #endregion

            var sortEmployee = Builders<Employee>.Sort.Ascending(m => m.FullName);
            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true)).Sort(sortEmployee).ToListAsync();

            var manager = new Employee();
            if (!string.IsNullOrEmpty(entity.ManagerId))
            {
                manager = dbContext.Employees.Find(m => m.Id.Equals(entity.ManagerId)).FirstOrDefault();
            }

            var employeeChanged = await dbContext.EmployeeHistories.Find(m => m.EmployeeId.Equals(id)).SortByDescending(m => m.UpdatedOn).Limit(1).FirstOrDefaultAsync();
            var statusChange = false;
            if (employeeChanged != null && employeeChanged.UpdatedOn > entity.UpdatedOn)
            {
                // if in changed data is not HR
                // Get list hr
                // check with list hr
                var listHr = new List<string>();
                var hrs = dbContext.RoleUsers.Find(m => m.Role.Equals(Constants.Rights.NhanSu)).ToList();
                foreach (var hr in hrs)
                {
                    listHr.Add(hr.User);
                }
                if (!listHr.Contains(employeeChanged.UpdatedBy))
                {
                    statusChange = true;
                }
            }
            var viewModel = new EmployeeDataViewModel()
            {
                Employee = entity,
                EmployeeChance = employeeChanged,
                StatusChange = statusChange,
                Employees = employees,
                Manager = manager
            };
            return View(viewModel);
        }

        // GET: Users/Create
        [Route("nhan-su/tao-moi")]
        public IActionResult Create()
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, Constants.Rights.NhanSu, (int)ERights.Add);

            // sys account override
            if (loginUserName == Constants.System.account)
            {
                right = true;
            }

            if (!right)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            #region Check salary right
            var salaryRight = (int)ERights.None;
            if (loginUserName == Constants.System.account)
            {
                salaryRight = (int)ERights.History;
            }
            else
            {
                if (Utility.IsRight(login, "luong", (int)ERights.Edit))
                {
                    salaryRight = (int)ERights.Edit;
                }
                else if (Utility.IsRight(login, "luong", (int)ERights.View))
                {
                    salaryRight = (int)ERights.View;
                }
            }
            ViewData["SalatyRight"] = salaryRight;
            #endregion

            #endregion

            #region Dropdownlist
            var parts = dbContext.Parts.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Parts"] = parts;
            var departments = dbContext.Departments.Find(m => m.Enable.Equals(true) && !m.Name.Equals(Constants.System.department)).ToList();
            ViewData["Departments"] = departments;
            var titles = dbContext.Titles.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Titles"] = titles;
            var roles = dbContext.Roles.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Roles"] = roles;
            var hospitals = dbContext.BHYTHospitals.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Hospitals"] = hospitals;
            var contracts = dbContext.ContractTypes.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Contracts"] = contracts;
            var workTimeTypes = dbContext.WorkTimeTypes.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["WorkTimeTypes"] = workTimeTypes;
            var banks = dbContext.Banks.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Banks"] = banks;
            #endregion
            var employee = new Employee
            {
                Joinday = DateTime.Now
            };

            var sortEmployee = Builders<Employee>.Sort.Ascending(m => m.FullName);
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true)).Sort(sortEmployee).ToList();

            var viewModel = new EmployeeDataViewModel()
            {
                Employee = employee,
                Employees = employees
            };
            return View(viewModel);
        }

        // POST: Users/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("nhan-su/tao-moi")]
        public async Task<ActionResult> CreateAsync(EmployeeDataViewModel viewModel, string checkUserName, string checkEmail)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            var loginInformation = dbContext.Employees.Find(m => m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion

                return RedirectToAction("login", "account");
            }

            bool right = Utility.IsRight(login, Constants.Rights.NhanSu, (int)ERights.Add);
            // sys account override
            if (loginUserName == Constants.System.account)
            {
                right = true;
            }

            if (!right)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            #endregion

            var entity = viewModel.Employee;
            if (checkUserName != entity.UserName && !CheckExist(entity))
            {
                return Json(new { result = false, source = "user", id = string.Empty, message = "Tên đăng nhập đã có trong hệ thống." });
            }

            if (!string.IsNullOrEmpty(entity.Email))
            {
                if (checkEmail != entity.Email && !CheckEmail(entity))
                {
                    return Json(new { result = false, source = "email", id = string.Empty, message = "Email đã có trong hệ thống." });
                }
            }

            #region Update missing field
            if (entity.Contracts != null)
            {
                if (entity.Contracts.Count > 0)
                {
                    for (int i = entity.Contracts.Count - 1; i >= 0; i--)
                    {
                        if (string.IsNullOrEmpty(entity.Contracts[i].Code))
                        {
                            entity.Contracts.RemoveAt(i);
                        }
                    }
                }
                entity.Contracts = entity.Contracts.Count == 0 ? null : entity.Contracts;
            }

            if (string.IsNullOrEmpty(entity.Department))
            {
                entity.Department = string.Empty;
            }
            if (string.IsNullOrEmpty(entity.Part))
            {
                entity.Part = string.Empty;
            }
            if (string.IsNullOrEmpty(entity.Title))
            {
                entity.Title = string.Empty;
            }
            #endregion

            try
            {
                #region Settings
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-CA");
                var now = DateTime.Now;
                entity.CreatedBy = login;
                entity.UpdatedBy = login;
                entity.CheckedBy = login;
                entity.ApprovedBy = login;

                var settings = dbContext.Settings.Find(m => true).ToList();
                var sendMail = true;
                var emailEntity = settings.Where(m => m.Key.Equals("email-tao-nhan-vien")).FirstOrDefault();
                if (emailEntity != null)
                {
                    sendMail = emailEntity.Value == "true" ? true : false;
                }
                // always have value
                var identityCardExpired = Convert.ToInt32(settings.Where(m => m.Key.Equals("identityCardExpired")).First().Value);
                var employeeCodeFirst = settings.Where(m => m.Key.Equals("employeeCodeFirst")).First().Value;
                var employeeCodeLength = settings.Where(m => m.Key.Equals("employeeCodeLength")).First().Value;
                var enableActivity = false;
                var enableSendMail = false;
                var leaveDayAvailable = 12;
                var salary = 0;
                #endregion

                #region System Generate
                var pwdrandom = Guid.NewGuid().ToString("N").Substring(0, 6);
                if (!string.IsNullOrEmpty(entity.Password))
                {
                    pwdrandom = entity.Password;
                }
                var sysPassword = Helpers.Helper.HashedPassword(pwdrandom);

                var lastEntity = dbContext.Employees.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Id).Limit(1).First();
                var x = 1;
                if (lastEntity != null && lastEntity.Code != null)
                {
                    x = Convert.ToInt32(lastEntity.Code.Replace(employeeCodeFirst, string.Empty)) + 1;
                }
                var sysCode = employeeCodeFirst + x.ToString($"D{employeeCodeLength}");
                #endregion

                #region Images, each product 1 folder. (return images)
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    var images = new List<Image>();
                    //var mapFolder = "images\\" + Constants.Link.Employee + "\\" + sysCode;
                    var mapFolder = Path.Combine("images", Constants.Link.Employee, sysCode);
                    var uploads = Path.Combine(_env.WebRootPath, mapFolder);
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }


                    foreach (var Image in files)
                    {
                        // only save images in input name [files-entity]
                        if (Image != null && Image.Length > 0 && Image.Name == "avatar")
                        {
                            var file = Image;
                            //There is an error here
                            if (file.Length > 0)
                            {
                                var fileName = Guid.NewGuid().ToString().Replace("-", "") + Path.GetExtension(file.FileName);
                                using (var fileStream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                    var currentImage = new Image
                                    {
                                        Path = "\\" + mapFolder + "\\",
                                        FileName = fileName,
                                        OrginalName = file.FileName
                                    };
                                    images.Add(currentImage);
                                    entity.Avatar = currentImage;
                                }
                            }
                        }
                        else if (Image != null && Image.Length > 0 && Image.Name == "cover")
                        {
                            var file = Image;
                            //There is an error here

                            if (file.Length > 0)
                            {
                                var fileName = Guid.NewGuid().ToString().Replace("-", "") + Path.GetExtension(file.FileName);
                                using (var fileStream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                    //emp.BookPic = fileName;
                                    var currentImage = new Image
                                    {
                                        Path = "\\" + mapFolder + "\\",
                                        FileName = fileName,
                                        OrginalName = file.FileName
                                    };
                                    images.Add(currentImage);
                                    entity.Cover = currentImage;
                                }
                            }
                        }
                    }
                }
                #endregion

                #region Trim Data
                entity.Email = !string.IsNullOrEmpty(entity.Email) ? entity.Email.Trim() : string.Empty;
                entity.FullName = !string.IsNullOrEmpty(entity.FullName) ? entity.FullName.Trim() : string.Empty;
                entity.Bornplace = !string.IsNullOrEmpty(entity.Bornplace) ? entity.Bornplace.Trim() : string.Empty;
                entity.AddressResident = !string.IsNullOrEmpty(entity.AddressResident) ? entity.AddressResident.Trim() : string.Empty;
                entity.AddressTemporary = !string.IsNullOrEmpty(entity.AddressTemporary) ? entity.AddressTemporary.Trim() : string.Empty;
                entity.EmailPersonal = !string.IsNullOrEmpty(entity.EmailPersonal) ? entity.EmailPersonal.Trim() : string.Empty;
                entity.IdentityCard = !string.IsNullOrEmpty(entity.IdentityCard) ? entity.IdentityCard.Trim() : string.Empty;
                entity.Passport = !string.IsNullOrEmpty(entity.Passport) ? entity.Passport.Trim() : string.Empty;
                entity.PassportCode = !string.IsNullOrEmpty(entity.PassportCode) ? entity.PassportCode.Trim() : string.Empty;
                entity.PassportPlace = !string.IsNullOrEmpty(entity.PassportPlace) ? entity.PassportPlace.Trim() : string.Empty;
                entity.HouseHold = !string.IsNullOrEmpty(entity.HouseHold) ? entity.HouseHold.Trim() : string.Empty;
                entity.HouseHoldOwner = !string.IsNullOrEmpty(entity.HouseHoldOwner) ? entity.HouseHoldOwner.Trim() : string.Empty;
                #endregion

                entity.Code = sysCode;
                entity.Password = sysPassword;
                entity.AliasFullName = Utility.AliasConvert(entity.FullName);
                dbContext.Employees.InsertOne(entity);
                var newUserId = entity.Id;

                var hisEntity = entity;
                hisEntity.EmployeeId = newUserId;
                dbContext.EmployeeHistories.InsertOne(hisEntity);

                #region Notification
                var notificationImages = new List<Image>();
                if (entity.Avatar != null && !string.IsNullOrEmpty(entity.Avatar.FileName))
                {
                    notificationImages.Add(entity.Avatar);
                }
                if (entity.Cover != null && !string.IsNullOrEmpty(entity.Cover.FileName))
                {
                    notificationImages.Add(entity.Cover);
                }

                var notification = new Notification
                {
                    Type = Constants.Notification.HR,
                    Title = Constants.Notification.CreateHR,
                    Content = entity.FullName,
                    Link = Constants.LinkHr.Main + "/" + Constants.LinkHr.Human + "/" + Constants.LinkHr.Information + "/" + entity.Id,
                    Images = notificationImages.Count > 0 ? notificationImages : null,
                    UserId = newUserId,
                    CreatedBy = login,
                    CreatedByName = loginInformation.FullName
                };
                dbContext.Notifications.InsertOne(notification);
                #endregion

                #region Activities
                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.Employees,
                    Action = Constants.Action.Create,
                    Value = newUserId,
                    Content = JsonConvert.SerializeObject(entity),
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion

                #region Send mail
                if (sendMail)
                {
                    SendMailNewUser(entity, pwdrandom);
                }
                #endregion

                return Json(new { result = true, source = "create", id = newUserId, message = "Khởi tạo thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "create", id = string.Empty, message = ex.Message });
            }
        }

        // GET: Users/Edit/5
        [Route("nhan-su/cap-nhat/{id}")]
        public async Task<ActionResult> Edit(string id)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var loginInformation = dbContext.Employees.Find(m => m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            var rightHr = false;
            // Check owner
            if (id != login)
            {
                if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.NhanSu, (int)ERights.Edit)))
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
                rightHr = true;
            }

            #region Check salary right
            var salaryRight = (int)ERights.None;
            if (id != login)
            {
                if (loginUserName == Constants.System.account)
                {
                    salaryRight = (int)ERights.History;
                }
                else
                {
                    if (Utility.IsRight(login, "luong", (int)ERights.Edit))
                    {
                        salaryRight = (int)ERights.Edit;
                    }
                    else if (Utility.IsRight(login, "luong", (int)ERights.View))
                    {
                        salaryRight = (int)ERights.View;
                    }
                }
            }
            else
            {
                salaryRight = (int)ERights.View;
            }
            ViewData["SalatyRight"] = salaryRight;
            #endregion

            #endregion

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var entity = dbContext.Employees
                .Find(m => m.Id == id).FirstOrDefault();
            if (entity == null)
            {
                return NotFound();
            }

            #region Dropdownlist
            var parts = dbContext.Parts.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Parts"] = parts;
            var departments = dbContext.Departments.Find(m => m.Enable.Equals(true) && !m.Name.Equals(Constants.System.department)).ToList();
            ViewData["Departments"] = departments;
            var titles = dbContext.Titles.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Titles"] = titles;
            var roles = dbContext.Roles.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Roles"] = roles;
            var hospitals = dbContext.BHYTHospitals.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Hospitals"] = hospitals;
            var contracts = dbContext.ContractTypes.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Contracts"] = contracts;
            var workTimeTypes = dbContext.WorkTimeTypes.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["WorkTimeTypes"] = workTimeTypes;
            var banks = dbContext.Banks.Find(m => m.Enable.Equals(true)).ToList();
            ViewData["Banks"] = banks;

            var lastNgachLuongs = dbContext.NgachLuongs.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
            var monthNgachLuong = lastNgachLuongs.Month;
            var yearNgachLuong = lastNgachLuongs.Year;
            var ngachluongs = dbContext.NgachLuongs.Find(m => m.Enable.Equals(true) && m.Bac.Equals(1) && m.Month.Equals(monthNgachLuong) && m.Year.Equals(yearNgachLuong)).ToList();

            var lastThang = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
            var monthThang = lastThang.Month;
            var yearThang = lastThang.Year;
            var thangbangluongs = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) && m.Bac.Equals(1) && m.Law.Equals(false) && m.Month.Equals(monthThang) && m.Year.Equals(yearThang)).ToList();
            #endregion

            var sortEmployee = Builders<Employee>.Sort.Ascending(m => m.FullName);
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true)).Sort(sortEmployee).ToList();

            var manager = new Employee();
            if (!string.IsNullOrEmpty(entity.ManagerId))
            {
                manager = dbContext.Employees.Find(m => m.Id.Equals(entity.ManagerId)).FirstOrDefault();
            }

            var employeeChanged = await dbContext.EmployeeHistories.Find(m => m.EmployeeId.Equals(id)).SortByDescending(m => m.UpdatedOn).Limit(1).FirstOrDefaultAsync();
            var statusChange = false;
            if (employeeChanged != null && employeeChanged.UpdatedOn > entity.UpdatedOn)
            {
                // if in changed data is not HR
                // Get list hr
                // check with list hr
                var listHr = new List<string>();
                var hrs = dbContext.RoleUsers.Find(m => m.Role.Equals(Constants.Rights.NhanSu)).ToList();
                foreach (var hr in hrs)
                {
                    listHr.Add(hr.User);
                }
                if (!listHr.Contains(employeeChanged.UpdatedBy))
                {
                    statusChange = true;
                }
            }

            var viewModel = new EmployeeDataViewModel()
            {
                Employee = entity,
                EmployeeChance = employeeChanged,
                Employees = employees,
                Manager = manager,
                StatusChange = statusChange,
                NgachLuongs = ngachluongs,
                ThangBangLuongs = thangbangluongs
            };
            return View(viewModel);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("nhan-su/cap-nhat/{id}")]
        public async Task<ActionResult> EditAsync(EmployeeDataViewModel viewModel)
        {
            var entity = viewModel.Employee;
            var isLeave = dbContext.Employees.CountDocuments(m => m.Enable.Equals(false) && m.EmployeeId.Equals(entity.Id));
            var userId = entity.Id;

            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            var loginInformation = dbContext.Employees.Find(m => m.Id.Equals(login)).FirstOrDefault();
            if (loginInformation == null)
            {
                #region snippet1
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion

                return RedirectToAction("login", "account");
            }

            // Check owner
            if (entity.Id != login)
            {
                if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, Constants.Rights.NhanSu, (int)ERights.Edit)))
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }
            var rightHr = Utility.IsRight(login, Constants.Rights.NhanSu, (int)ERights.Edit);
            // System
            if (loginUserName == Constants.System.account)
            {
                rightHr = true;
            }
            #endregion

            #region Setting
            var settings = dbContext.Settings.Find(m => true).ToList();
            var sendMail = true;
            var sendMailLeave = true;
            var emailEntitySetting = settings.Where(m => m.Key.Equals("email-cap-nhat-nhan-vien")).FirstOrDefault();
            if (emailEntitySetting != null)
            {
                sendMail = emailEntitySetting.Value == "true" ? true : false;
            }
            var emailLeaveEntitySetting = settings.Where(m => m.Key.Equals("email-nhan-vien-nghi")).FirstOrDefault();
            if (emailLeaveEntitySetting != null)
            {
                sendMailLeave = emailLeaveEntitySetting.Value == "true" ? true : false;
            }
            #endregion

            #region Update missing field
            if (entity.Contracts != null)
            {
                if (entity.Contracts.Count > 0)
                {
                    for (int i = entity.Contracts.Count - 1; i >= 0; i--)
                    {
                        if (string.IsNullOrEmpty(entity.Contracts[i].Code))
                        {
                            entity.Contracts.RemoveAt(i);
                        }
                    }
                }
                entity.Contracts = entity.Contracts.Count == 0 ? null : entity.Contracts;
            }
            if (string.IsNullOrEmpty(entity.Department))
            {
                entity.Department = string.Empty;
            }
            if (string.IsNullOrEmpty(entity.Part))
            {
                entity.Part = string.Empty;
            }
            if (string.IsNullOrEmpty(entity.Title))
            {
                entity.Title = string.Empty;
            }
            #endregion

            try
            {
                var now = DateTime.Now;
                #region Images, each product 1 folder. (return images)
                var avatarImage = new Image();
                var coverImage = new Image();
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    var images = new List<Image>();
                    var mapFolder = "images\\" + Constants.Link.Employee + "\\" + entity.Code;
                    var uploads = Path.Combine(_env.WebRootPath, mapFolder);
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    foreach (var Image in files)
                    {
                        // only save images in input name [files-entity]
                        if (Image != null && Image.Length > 0 && Image.Name == "avatar")
                        {
                            var file = Image;
                            //There is an error here

                            if (file.Length > 0)
                            {
                                var fileName = Guid.NewGuid().ToString().Replace("-", "") + Path.GetExtension(file.FileName);
                                using (var fileStream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                    //emp.BookPic = fileName;
                                    var currentImage = new Image
                                    {
                                        Path = "\\" + mapFolder + "\\",
                                        FileName = fileName,
                                        OrginalName = file.FileName
                                    };
                                    images.Add(currentImage);
                                    avatarImage = currentImage;
                                }
                            }
                        }

                        else if (Image != null && Image.Length > 0 && Image.Name == "cover")
                        {
                            var file = Image;
                            //There is an error here

                            if (file.Length > 0)
                            {
                                var fileName = Guid.NewGuid().ToString().Replace("-", "") + Path.GetExtension(file.FileName);
                                using (var fileStream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                    //emp.BookPic = fileName;
                                    var currentImage = new Image
                                    {
                                        Path = "\\" + mapFolder + "\\",
                                        FileName = fileName,
                                        OrginalName = file.FileName
                                    };
                                    images.Add(currentImage);
                                    coverImage = currentImage;
                                }
                            }
                        }
                    }
                }
                #endregion

                entity.AliasFullName = Utility.AliasConvert(entity.FullName);
                entity.UpdatedBy = login;
                entity.UpdatedOn = now;
                if (avatarImage != null && !string.IsNullOrEmpty(avatarImage.FileName))
                {
                    entity.Avatar = avatarImage;
                }
                if (coverImage != null && !string.IsNullOrEmpty(coverImage.FileName))
                {
                    entity.Cover = coverImage;
                }

                #region Update Data
                // Rule if user have right Nhan Su => direct
                // If no. update temp. Then Nhan Su update.
                var messageResult = "Cập nhật thành công";
                #region Trim Data
                entity.Email = !string.IsNullOrEmpty(entity.Email) ? entity.Email.Trim() : string.Empty;
                entity.FullName = !string.IsNullOrEmpty(entity.FullName) ? entity.FullName.Trim() : string.Empty;
                entity.Bornplace = !string.IsNullOrEmpty(entity.Bornplace) ? entity.Bornplace.Trim() : string.Empty;
                entity.AddressResident = !string.IsNullOrEmpty(entity.AddressResident) ? entity.AddressResident.Trim() : string.Empty;
                entity.AddressTemporary = !string.IsNullOrEmpty(entity.AddressTemporary) ? entity.AddressTemporary.Trim() : string.Empty;
                entity.EmailPersonal = !string.IsNullOrEmpty(entity.EmailPersonal) ? entity.EmailPersonal.Trim() : string.Empty;
                entity.IdentityCard = !string.IsNullOrEmpty(entity.IdentityCard) ? entity.IdentityCard.Trim() : string.Empty;
                entity.Passport = !string.IsNullOrEmpty(entity.Passport) ? entity.Passport.Trim() : string.Empty;
                entity.PassportCode = !string.IsNullOrEmpty(entity.PassportCode) ? entity.PassportCode.Trim() : string.Empty;
                entity.PassportPlace = !string.IsNullOrEmpty(entity.PassportPlace) ? entity.PassportPlace.Trim() : string.Empty;
                entity.HouseHold = !string.IsNullOrEmpty(entity.HouseHold) ? entity.HouseHold.Trim() : string.Empty;
                entity.HouseHoldOwner = !string.IsNullOrEmpty(entity.HouseHoldOwner) ? entity.HouseHoldOwner.Trim() : string.Empty;
                #endregion

                var linkInformation = Constants.LinkHr.Main + "/" + Constants.LinkHr.Human + "/" + Constants.LinkHr.Information + "/" + entity.Id;
                if (rightHr)
                {
                    if (!string.IsNullOrEmpty(entity.SalaryChucVuViTriCode))
                    {
                        var viTriE = dbContext.SalaryThangBangLuongs.Find(m => m.ViTriCode.Equals(entity.SalaryChucVuViTriCode)).SortByDescending(m => m.UpdatedOn).FirstOrDefault();
                        if (viTriE != null)
                        {
                            entity.Title = viTriE.ViTri;
                            entity.SalaryChucVu = viTriE.ViTri;
                        }
                    }

                    var filter = Builders<Employee>.Filter.Eq(m => m.Id, entity.Id);
                    var update = Builders<Employee>.Update
                        .Set(m => m.UpdatedBy, login)
                        .Set(m => m.UpdatedOn, now)
                        .Set(m => m.Workplaces, entity.Workplaces)
                        .Set(m => m.IsTimeKeeper, entity.IsTimeKeeper)
                        .Set(m => m.LeaveLevelYear, entity.LeaveLevelYear)
                        .Set(m => m.LeaveDayAvailable, entity.LeaveDayAvailable)
                        .Set(m => m.Email, entity.Email)
                        .Set(m => m.FullName, entity.FullName)
                        .Set(m => m.FirstName, entity.FirstName)
                        .Set(m => m.LastName, entity.LastName)
                        .Set(m => m.AliasFullName, entity.AliasFullName)
                        .Set(m => m.Birthday, entity.Birthday)
                        .Set(m => m.Bornplace, entity.Bornplace)
                        .Set(m => m.Gender, entity.Gender)
                        .Set(m => m.Joinday, entity.Joinday)
                        .Set(m => m.Contractday, entity.Contractday)
                        .Set(m => m.Enable, entity.Enable)
                        .Set(m => m.Leaveday, entity.Leaveday)
                        .Set(m => m.LeaveReason, entity.LeaveReason)
                        .Set(m => m.AddressResident, entity.AddressResident)
                        .Set(m => m.AddressTemporary, entity.AddressTemporary)
                        .Set(m => m.EmailPersonal, entity.EmailPersonal)
                        .Set(m => m.Intro, entity.Intro)
                        .Set(m => m.Part, entity.Part)
                        .Set(m => m.Department, entity.Department)
                        .Set(m => m.ManagerId, entity.ManagerId)
                        .Set(m => m.Title, entity.Title)
                        .Set(m => m.Tel, entity.Tel)
                        .Set(m => m.Mobiles, entity.Mobiles)
                        .Set(m => m.IsOnline, entity.IsOnline)
                        .Set(m => m.IdentityCard, entity.IdentityCard)
                        .Set(m => m.IdentityCardDate, entity.IdentityCardDate)
                        .Set(m => m.IdentityCardPlace, entity.IdentityCardPlace)
                        .Set(m => m.PassportEnable, entity.PassportEnable)
                        .Set(m => m.Passport, entity.Passport)
                        .Set(m => m.PassportType, entity.PassportType)
                        .Set(m => m.PassportCode, entity.PassportCode)
                        .Set(m => m.PassportDate, entity.PassportDate)
                        .Set(m => m.PassportExpireDate, entity.PassportExpireDate)
                        .Set(m => m.PassportPlace, entity.PassportPlace)
                        .Set(m => m.HouseHold, entity.HouseHold)
                        .Set(m => m.HouseHoldOwner, entity.HouseHoldOwner)
                        .Set(m => m.StatusMarital, entity.StatusMarital)
                        .Set(m => m.Nation, entity.Nation)
                        .Set(m => m.Religion, entity.Religion)
                        .Set(m => m.Certificates, entity.Certificates)
                        .Set(m => m.Cards, entity.Cards)
                        .Set(m => m.Contracts, entity.Contracts)
                        .Set(m => m.StorePapers, entity.StorePapers)
                        .Set(m => m.BhxhEnable, entity.BhxhEnable)
                        .Set(m => m.BhxhStart, entity.BhxhStart)
                        .Set(m => m.BhxhEnd, entity.BhxhEnd)
                        .Set(m => m.BhxhBookNo, entity.BhxhBookNo)
                        .Set(m => m.BhxhCode, entity.BhxhCode)
                        .Set(m => m.BhxhStatus, entity.BhxhStatus)
                        .Set(m => m.BhxhHospital, entity.BhxhHospital)
                        .Set(m => m.BhxhLocation, entity.BhxhLocation)
                        .Set(m => m.BhytCode, entity.BhytCode)
                        .Set(m => m.BhytStart, entity.BhytStart)
                        .Set(m => m.BhytEnd, entity.BhytEnd)
                        .Set(m => m.BhxhHistories, entity.BhxhHistories)
                        .Set(m => m.EmployeeFamilys, entity.EmployeeFamilys)
                        .Set(m => m.Contracts, entity.Contracts)
                        .Set(m => m.EmployeeEducations, entity.EmployeeEducations)
                        .Set(m => m.SalaryType, entity.SalaryType)
                        .Set(m => m.SalaryPayMethod, entity.SalaryPayMethod)
                        .Set(m => m.NgachLuong, entity.NgachLuong)
                        .Set(m => m.SalaryChucVuViTriCode, entity.SalaryChucVuViTriCode);

                    if (entity.Avatar != null && !string.IsNullOrEmpty(entity.Avatar.FileName))
                    {
                        update = update.Set(m => m.Avatar, entity.Avatar);
                    }
                    if (entity.Cover != null && !string.IsNullOrEmpty(entity.Cover.FileName))
                    {
                        update = update.Set(m => m.Cover, entity.Cover);
                    }

                    if (avatarImage != null && !string.IsNullOrEmpty(avatarImage.FileName))
                    {
                        update = update.Set(m => m.Avatar, avatarImage);
                    }
                    if (coverImage != null && !string.IsNullOrEmpty(coverImage.FileName))
                    {
                        update = update.Set(m => m.Cover, coverImage);
                    }

                    dbContext.Employees.UpdateOne(filter, update);
                    entity.EmployeeId = userId;
                    entity.Id = null;
                    dbContext.EmployeeHistories.InsertOne(entity);

                    #region Send email to user changed
                    if (sendMail)
                    {
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
                                + "HrChangeInformation.html";
                        var subject = "Thay đổi thông tin nhân sự.";
                        var requester = entity.FullName;
                        var hrChanged = loginInformation.FullName;
                        if (!string.IsNullOrEmpty(loginInformation.Title))
                        {
                            hrChanged += " - " + loginInformation.Title;
                        }
                        if (!string.IsNullOrEmpty(loginInformation.Email))
                        {
                            hrChanged += " - email: " + loginInformation.Email;
                        }
                        var linkDomain = Constants.System.domain;
                        var fullLinkInformation = linkDomain + "/" + linkInformation;
                        var bodyBuilder = new BodyBuilder();
                        using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                        {
                            bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                        }
                        string messageBody = string.Format(bodyBuilder.HtmlBody,
                            subject,
                            requester,
                            hrChanged,
                            entity.UpdatedOn.ToString("dd/MM/yyyy"),
                            fullLinkInformation,
                            linkDomain
                            );
                        var emailMessage = new EmailMessage()
                        {
                            ToAddresses = tos,
                            Subject = subject,
                            BodyContent = messageBody,
                            Type = "hr-edit-information"
                        };

                        // For faster. Add to schedule.
                        // Send later
                        var scheduleEmail = new ScheduleEmail
                        {
                            Status = (int)EEmailStatus.Schedule,
                            //From = emailMessage.FromAddresses,
                            To = emailMessage.ToAddresses,
                            CC = emailMessage.CCAddresses,
                            BCC = emailMessage.BCCAddresses,
                            Type = emailMessage.Type,
                            Title = emailMessage.Subject,
                            Content = emailMessage.BodyContent
                        };
                        dbContext.ScheduleEmails.InsertOne(scheduleEmail);

                        //_emailSender.SendEmail(emailMessage);
                    }
                    #endregion

                    #region Send email leave
                    if (isLeave == 0 && entity.Leaveday.HasValue)
                    {
                        if (sendMailLeave)
                        {
                            SendMailLeaveUser(entity);
                        }
                    }
                    #endregion
                }
                else
                {
                    entity.EmployeeId = userId;
                    entity.Id = null;
                    dbContext.EmployeeHistories.InsertOne(entity);

                    messageResult = "Thông tin đã được gửi và cập nhật bởi bộ phận Nhân sự.";
                    #region Send email to Hr
                    if (sendMail)
                    {
                        var tos = new List<EmailAddress>();
                        var ccs = new List<EmailAddress>();
                        var listHrRoles = dbContext.RoleUsers.Find(m => m.Role.Equals(Constants.Rights.NhanSu) && (m.Expired.Equals(null) || m.Expired > DateTime.Now)).ToList();
                        if (listHrRoles != null && listHrRoles.Count > 0)
                        {
                            foreach (var item in listHrRoles)
                            {
                                if (item.Action == 3)
                                {
                                    var fields = Builders<Employee>.Projection.Include(p => p.Email).Include(p => p.FullName);
                                    var emailEntity = dbContext.Employees.Find(m => m.Id.Equals(item.User)).Project<Employee>(fields).FirstOrDefault();
                                    if (emailEntity != null)
                                    {
                                        tos.Add(new EmailAddress { Name = emailEntity.FullName, Address = emailEntity.Email });
                                    }
                                }
                                else if (item.Action >= 4)
                                {
                                    var fields = Builders<Employee>.Projection.Include(p => p.Email).Include(p => p.FullName);
                                    var emailEntity = dbContext.Employees.Find(m => m.Id.Equals(item.User) && !m.NgachLuong.Equals("C.01")).Project<Employee>(fields).FirstOrDefault();
                                    if (emailEntity != null)
                                    {
                                        ccs.Add(new EmailAddress { Name = emailEntity.FullName, Address = emailEntity.Email });
                                    }
                                }
                            }
                        }

                        #region UAT
                        var uat = dbContext.Settings.Find(m => m.Key.Equals("UAT")).FirstOrDefault();
                        if (uat != null && uat.Value == "true")
                        {
                            tos = new List<EmailAddress>
                        {
                            new EmailAddress { Name = "Xuan", Address = "xuan.tm1988@gmail.com" }
                        };

                            ccs = new List<EmailAddress>
                        {
                            new EmailAddress { Name = "Xuan CC", Address = "xuantranm@gmail.com" }
                        };
                        }
                        #endregion

                        var webRoot = Environment.CurrentDirectory;
                        var pathToFile = _env.WebRootPath
                                + Path.DirectorySeparatorChar.ToString()
                                + "Templates"
                                + Path.DirectorySeparatorChar.ToString()
                                + "EmailTemplate"
                                + Path.DirectorySeparatorChar.ToString()
                                + "EmployeeChangeInformation.html";
                        var subject = "Thay đổi thông tin nhân sự.";
                        var requester = "Bộ phận nhân sự.";
                        var userTitle = entity.FullName;
                        if (!string.IsNullOrEmpty(entity.Title))
                        {
                            userTitle += " - " + entity.Title;
                        }
                        var linkDomain = Constants.System.domain;
                        var fullLinkInformation = linkDomain + "/" + linkInformation;
                        var bodyBuilder = new BodyBuilder();
                        using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                        {
                            bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                        }
                        string messageBody = string.Format(bodyBuilder.HtmlBody,
                            subject,
                            requester,
                            userTitle,
                            entity.UpdatedOn.ToString("dd/MM/yyyy"),
                            fullLinkInformation,
                            linkDomain
                            );
                        var emailMessage = new EmailMessage()
                        {
                            ToAddresses = tos,
                            CCAddresses = ccs,
                            Subject = subject,
                            BodyContent = messageBody,
                            Type = "edit-information"
                        };
                        _emailSender.SendEmail(emailMessage);
                    }
                    #endregion
                }

                #endregion

                #region Notification
                var notificationImages = new List<Image>();
                if (entity.Avatar != null && !string.IsNullOrEmpty(entity.Avatar.FileName))
                {
                    notificationImages.Add(entity.Avatar);
                }
                if (entity.Cover != null && !string.IsNullOrEmpty(entity.Cover.FileName))
                {
                    notificationImages.Add(entity.Cover);
                }
                //var userData = new UserData {UserId = 0};
                //var userDataString = JsonConvert.SerializeObject(userData);
                //var userData = JsonConvert.DeserializeObject<UserData>(userDataString);
                var notification = new Notification
                {
                    Type = Constants.Notification.HR,
                    Title = Constants.Notification.UpdateHR,
                    Content = entity.FullName,
                    Link = linkInformation,
                    Images = notificationImages.Count > 0 ? notificationImages : null,
                    UserId = userId,
                    CreatedBy = login,
                    CreatedByName = loginInformation.FullName
                };
                dbContext.Notifications.InsertOne(notification);
                #endregion

                #region Activities
                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.NhanViens,
                    Action = Constants.Action.Edit,
                    Value = userId,
                    Content = JsonConvert.SerializeObject(entity)
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion

                return Json(new { result = true, source = "update", id = userId, message = messageResult });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "update", id = userId, message = ex.Message });
            }
        }

        // POST: Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("nhan-su/xoa/{id}")]
        public IActionResult Delete(EmployeeDataViewModel viewModel, string id)
        {
            var userId = User.Identity.Name;
            var entity = viewModel.Employee;
            try
            {
                // TODO: Add disable logic here
                if (CheckDelete(entity))
                {
                    dbContext.Settings.DeleteOne(m => m.Id == entity.Id);

                    #region Activities
                    var activity = new TrackingUser
                    {
                        UserId = userId,
                        Function = Constants.Collection.Employees,
                        Action = Constants.Action.Delete,
                        Value = entity.UserName,
                        Content = entity.UserName + Constants.Flag + entity.FullName
                    };
                    dbContext.TrackingUsers.InsertOne(activity);
                    #endregion

                    return Json(new { entity, result = true, message = "Delete successfull." });
                }
                else
                {
                    return Json(new { entity, result = false, message = entity.UserName + " updated by another. Try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { entity, result = false, message = ex.Message });
            }
        }

        [Route(Constants.LinkHr.ChildrenReport)]
        public async Task<ActionResult> ChildrenReport()
        {
            // update true data
            var employees = dbContext.Employees.Find(m => true).ToList();
            foreach (var employee in employees)
            {
                if (employee.EmployeeFamilys != null)
                {
                    if (employee.EmployeeFamilys.Count > 0)
                    {
                        for (int i = employee.EmployeeFamilys.Count - 1; i >= 0; i--)
                        {
                            if (employee.EmployeeFamilys[i].Birthday.HasValue && employee.EmployeeFamilys[i].Birthday < Constants.MinDate.AddYears(2))
                            {
                                employee.EmployeeFamilys[i].Birthday = null;
                            }
                            if (string.IsNullOrEmpty(employee.EmployeeFamilys[i].FullName) || employee.EmployeeFamilys[i].FullName.Length < 2)
                            {
                                employee.EmployeeFamilys.RemoveAt(i);
                            }
                        }
                    }
                    employee.EmployeeFamilys = employee.EmployeeFamilys.Count == 0 ? null : employee.EmployeeFamilys;

                    var filterUpdate = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                    var update = Builders<Employee>.Update
                        .Set(m => m.EmployeeFamilys, employee.EmployeeFamilys);
                    dbContext.Employees.UpdateOne(filterUpdate, update);
                }
            }

            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq("EmployeeFamilys.Relation", 3);

            //var projection = Builders<Employee>.Projection.Include("EmployeeFamilys.$");
            //var result = await dbContext.Employees.Find(filter).Project(projection).ToListAsync();
            var result = await dbContext.Employees.Find(filter).ToListAsync();

            return View(result);
        }

        [Route(Constants.LinkHr.ChildrenReport + "/" + Constants.LinkHr.Export)]
        public async Task<IActionResult> ChildrenReportExport(string fileName)
        {
            var builder = Builders<Employee>.Filter;
            var filter = builder.Eq(m => m.Enable, true);
            filter = filter & !builder.Eq(i => i.UserName, Constants.System.account);
            filter = filter & builder.Eq("EmployeeFamilys.Relation", 3);
            var results = await dbContext.Employees.Find(filter).ToListAsync();

            string exportFolder = Path.Combine(_env.WebRootPath, "exports");
            string sFileName = @"danh-sach-con-" + DateTime.Now.ToString("ddMMyyyyhhmm") + ".xlsx";
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
                #endregion

                ISheet sheet1 = workbook.CreateSheet("Danh-sach-con");

                //sheet1.AddMergedRegion(new CellRangeAddress(0, 0, 0, 10));
                var rowIndex = 0;
                IRow row = sheet1.CreateRow(rowIndex);
                row.CreateCell(0, CellType.String).SetCellValue("STT");
                row.CreateCell(1, CellType.String).SetCellValue("Mã");
                row.CreateCell(2, CellType.String).SetCellValue("Họ tên");
                row.CreateCell(3, CellType.String).SetCellValue("Email");
                row.CreateCell(4, CellType.String).SetCellValue("Điện thoại bàn");
                row.CreateCell(5, CellType.String).SetCellValue("Điện thoại");
                row.CreateCell(6, CellType.String).SetCellValue("Nơi làm việc");
                row.CreateCell(7, CellType.String).SetCellValue("Bộ phận");
                row.CreateCell(8, CellType.String).SetCellValue("Phòng ban");
                row.CreateCell(9, CellType.String).SetCellValue("Chức vụ");
                row.CreateCell(10, CellType.String).SetCellValue("Thông tin con");
                row.CreateCell(11, CellType.String).SetCellValue("Tổng số");
                // Set style
                for (int i = 0; i <= 11; i++)
                {
                    row.Cells[i].CellStyle = cellStyleHeader;
                }
                rowIndex++;

                foreach (var data in results)
                {
                    row = sheet1.CreateRow(rowIndex);
                    row.CreateCell(0, CellType.Numeric).SetCellValue(rowIndex);
                    row.CreateCell(1, CellType.String).SetCellValue(data.CodeOld + " (" + data.Code + ")");
                    row.CreateCell(2, CellType.String).SetCellValue(data.FullName);
                    row.CreateCell(3, CellType.String).SetCellValue(data.Email);
                    row.CreateCell(4, CellType.String).SetCellValue(data.Tel);
                    var mobiles = string.Empty;
                    if (data.Mobiles != null && data.Mobiles.Count > 0)
                    {
                        foreach (var mobile in data.Mobiles)
                        {
                            if (!string.IsNullOrEmpty(mobiles))
                            {
                                mobiles += " - ";
                            }
                            mobiles += mobile.Number;
                        }
                    }
                    row.CreateCell(5, CellType.String).SetCellValue(mobiles);
                    var workplaces = string.Empty;
                    if (data.Workplaces != null && data.Workplaces.Count > 0)
                    {
                        foreach (var workplace in data.Workplaces)
                        {
                            if (!string.IsNullOrEmpty(workplaces))
                            {
                                workplaces += " - ";
                            }
                            workplaces += workplace.Name;
                            if (!string.IsNullOrEmpty(workplace.Fingerprint))
                            {
                                workplaces += "(" + workplace.Fingerprint + " - " + workplace.WorkingScheduleTime + ")";
                            }
                        }
                    }
                    row.CreateCell(6, CellType.String).SetCellValue(workplaces);
                    row.CreateCell(7, CellType.String).SetCellValue(data.Part);
                    row.CreateCell(8, CellType.String).SetCellValue(data.Department);
                    row.CreateCell(9, CellType.String).SetCellValue(data.Title);
                    var thongtincon = string.Empty;
                    int socon = 0;
                    foreach (var children in data.EmployeeFamilys)
                    {
                        if (children.Relation == 3)
                        {
                            if (!string.IsNullOrEmpty(thongtincon))
                            {
                                thongtincon += " - ";
                            }
                            thongtincon += children.FullName;
                            if (children.Birthday.HasValue)
                            {
                                thongtincon += " (" + children.Birthday.Value.ToString("dd/MM/yyyy") + ")";
                            }
                            socon++;
                        }
                    }
                    row.CreateCell(10, CellType.String).SetCellValue(thongtincon);
                    row.CreateCell(11, CellType.Numeric).SetCellValue(socon);
                    //sheet1.AutoSizeColumn(0);
                    rowIndex++;
                }

                var sheet2 = workbook.CreateSheet("Phong-ban");
                var style1 = workbook.CreateCellStyle();
                style1.FillForegroundColor = HSSFColor.Blue.Index2;
                style1.FillPattern = FillPattern.SolidForeground;

                var style2 = workbook.CreateCellStyle();
                style2.FillForegroundColor = HSSFColor.Yellow.Index2;
                style2.FillPattern = FillPattern.SolidForeground;

                var cell2 = sheet2.CreateRow(0).CreateCell(0);
                cell2.CellStyle = style1;
                cell2.SetCellValue(0);

                cell2 = sheet2.CreateRow(1).CreateCell(0);
                cell2.CellStyle = style2;
                cell2.SetCellValue(1);

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        public string GeneralEmail(string input)
        {
            return Utility.EmailConvert(input);
        }

        public void SendMailNewUser(Employee entity, string pwd)
        {
            // 1. Notication
            //      cc: cấp cao
            //      to: employee have email
            // 2. Send to IT setup email,...
            var url = Constants.System.domain;
            var contact = string.Empty;
            if (entity.Mobiles != null && entity.Mobiles.Count > 0)
            {
                contact = entity.Mobiles[0].Number;
            }
            var subject = "THÔNG BÁO NHÂN SỰ MỚI.";
            var nhansumoi = string.Empty;
            nhansumoi += "<table class='MsoNormalTable' border='0 cellspacing='0' cellpadding='0' width='738' style='width: 553.6pt; margin-left: -1.15pt; border-collapse: collapse;'>";
            nhansumoi += "<tbody>";
            nhansumoi += "<tr style='height: 15.75pt'>";
            nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>STT</b></td>";
            nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>HỌ VÀ TÊN</b></td>";
            nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>SỐ LIÊN HỆ</b></td>";
            nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>EMAIL</b></td>";
            nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>NGÀY NHẬN VIỆC</b></td>";
            nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>CHỨC VỤ</b></td>";
            nhansumoi += "</tr>";

            nhansumoi += "<tr style='height: 12.75pt'>";
            nhansumoi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-top: none; padding: 0cm 5.4pt 0cm 5.4pt;'>" + "01" + "</td>";
            nhansumoi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + entity.FullName + "</td>";
            nhansumoi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + contact + "</td>";
            nhansumoi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + entity.Email + "</td>";
            nhansumoi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + entity.Joinday.ToString("dd/MM/yyyy") + "</td>";
            nhansumoi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + entity.Title + "</td>";
            nhansumoi += "</tr>";
            nhansumoi += "</tbody>";
            nhansumoi += "</table>";
            var tos = new List<EmailAddress>();
            var ccs = new List<EmailAddress>();
            var listboss = new List<string>
            {
                "C.01",
                "C.02"
            };
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && !string.IsNullOrEmpty(m.Email)).ToList();
            foreach (var employee in employees)
            {
                if (!string.IsNullOrEmpty(employee.NgachLuong) && listboss.Any(str => str.Contains(employee.NgachLuong)))
                {
                    ccs.Add(new EmailAddress
                    {
                        Name = employee.FullName,
                        Address = employee.Email,
                    });
                }
                else
                {
                    tos.Add(new EmailAddress
                    {
                        Name = employee.FullName,
                        Address = employee.Email,
                    });
                }
            }

            #region UAT
            var uat = dbContext.Settings.Find(m => m.Key.Equals("UAT")).FirstOrDefault();
            if (uat != null && uat.Value == "true")
            {
                tos = new List<EmailAddress>
                        {
                            new EmailAddress { Name = "Xuan", Address = "xuan.tm1988@gmail.com" }
                        };

                ccs = new List<EmailAddress>
                        {
                            new EmailAddress { Name = "Xuan CC", Address = "xuantranm@gmail.com" }
                        };
            }
            #endregion

            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "NhanSuMoi.html";
            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }
            string messageBody = string.Format(builder.HtmlBody,
                subject,
                "tất cả thành viên",
                nhansumoi,
                url);

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                CCAddresses = ccs,
                Subject = subject,
                BodyContent = messageBody,
                Type = "nhan-su-moi"
            };

            // For faster update.
            var scheduleEmail = new ScheduleEmail
            {
                Status = (int)EEmailStatus.ScheduleASAP,
                To = emailMessage.ToAddresses,
                CC = emailMessage.CCAddresses,
                BCC = emailMessage.BCCAddresses,
                Type = emailMessage.Type,
                Title = emailMessage.Subject,
                Content = emailMessage.BodyContent
            };
            dbContext.ScheduleEmails.InsertOne(scheduleEmail);
            //_emailSender.SendEmail(emailMessage);
        }

        public void SendMailLeaveUser(Employee entity)
        {
            // 1. Notication
            //      cc: cấp cao
            //      to: employee have email
            // 2. Send to IT setup email,...
            var url = Constants.System.domain;
            var contact = string.Empty;
            if (entity.Mobiles != null && entity.Mobiles.Count > 0)
            {
                contact = entity.Mobiles[0].Number;
            }
            var subject = "THÔNG BÁO NHÂN SỰ NGHỈ VIỆC.";
            var nhansunghi = string.Empty;
            nhansunghi += "<table class='MsoNormalTable' border='0 cellspacing='0' cellpadding='0' width='738' style='width: 553.6pt; margin-left: -1.15pt; border-collapse: collapse;'>";
            nhansunghi += "<tbody>";
            nhansunghi += "<tr style='height: 15.75pt'>";
            nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>STT</b></td>";
            nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>PHÒNG/BAN</b></td>";
            nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>HỌ VÀ TÊN</b></td>";
            nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>CHỨC VỤ</b></td>";
            nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>NGÀY NGHỈ</b></td>";
            nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-left: none; background: #76923C; padding: 0cm 5.4pt 0cm 5.4pt;'><b>SỐ LIÊN HỆ</b></td>";
            nhansunghi += "</tr>";

            nhansunghi += "<tr style='height: 12.75pt'>";
            nhansunghi += "<td nowrap='nowrap' style='border: solid windowtext 1.0pt; border-top: none; padding: 0cm 5.4pt 0cm 5.4pt;'>" + "01" + "</td>";
            nhansunghi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + entity.Department + "</td>";
            nhansunghi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + entity.FullName + "</td>";
            nhansunghi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + entity.Title + "</td>";
            nhansunghi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + entity.Leaveday.Value.ToString("dd/MM/yyyy") + "</td>";
            nhansunghi += "<td nowrap='nowrap' style='border-top: none; border-left: none; border-bottom: solid windowtext 1.0pt; border-right: solid windowtext 1.0pt; padding: 0cm 5.4pt 0cm 5.4pt;'>" + contact + "</td>";
            nhansunghi += "</tr>";
            nhansunghi += "</tbody>";
            nhansunghi += "</table>";
            var tos = new List<EmailAddress>();
            var ccs = new List<EmailAddress>();
            var listboss = new List<string>
            {
                "C.01",
                "C.02"
            };
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true) && !string.IsNullOrEmpty(m.Email) && !m.UserName.Equals(Constants.System.account)).ToList();
            foreach (var employee in employees)
            {
                if (!string.IsNullOrEmpty(employee.NgachLuong) && listboss.Any(str => str.Contains(employee.NgachLuong)))
                {
                    ccs.Add(new EmailAddress
                    {
                        Name = employee.FullName,
                        Address = employee.Email,
                    });
                }
                else
                {
                    tos.Add(new EmailAddress
                    {
                        Name = employee.FullName,
                        Address = employee.Email,
                    });
                }
            }

            #region UAT
            //tos = new List<EmailAddress>
            //            {
            //                new EmailAddress { Name = "Xuan", Address = "xuan.tm1988@gmail.com" }
            //            };

            //ccs = new List<EmailAddress>
            //            {
            //                new EmailAddress { Name = "Xuan CC", Address = "xuantranm@gmail.com" }
            //            };
            #endregion

            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "NhanSuNghi.html";
            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }
            string messageBody = string.Format(builder.HtmlBody,
                subject,
                "tất cả thành viên",
                nhansunghi,
                url);

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                CCAddresses = ccs,
                Subject = subject,
                BodyContent = messageBody,
                Type = "nhan-su-nghi"
            };

            // For faster update.
            var scheduleEmail = new ScheduleEmail
            {
                Status = (int)EEmailStatus.ScheduleASAP,
                To = emailMessage.ToAddresses,
                CC = emailMessage.CCAddresses,
                BCC = emailMessage.BCCAddresses,
                Type = emailMessage.Type,
                Title = emailMessage.Subject,
                Content = emailMessage.BodyContent
            };
            dbContext.ScheduleEmails.InsertOne(scheduleEmail);
            //_emailSender.SendEmail(emailMessage);
        }

        private bool EntityExists(string id)
        {
            return dbContext.Employees.Count(m => m.Id == id) > 0;
        }

        public bool CheckExist(Employee entity)
        {
            return dbContext.Employees.Find(m => m.Enable.Equals(true) && m.UserName.Equals(entity.UserName)).Count() > 0 ? false : true;
        }

        public bool CheckEmail(Employee entity)
        {
            return dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Email.Equals(entity.Email)).Count() > 0 ? false : true;
        }

        public bool CheckUpdate(Employee entity)
        {
            var db = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.UserName.Equals(entity.UserName)).First();
            if (db.UserName != entity.UserName)
            {
                if (CheckExist(entity))
                {
                    return db.Timestamp == entity.Timestamp ? true : false;
                }
            }
            return db.Timestamp == entity.Timestamp ? true : false;
        }

        public bool CheckDisable(Employee entity)
        {
            return entity.Usage > 0 ? false : true;
        }

        public bool CheckActive(Employee entity)
        {
            return dbContext.Employees.Find(m => m.Enable.Equals(true) && m.UserName.Equals(entity.UserName)).Count() > 0 ? false : true;
        }

        public bool CheckDelete(Employee entity)
        {
            if (entity.NoDelete)
            {
                return false;
            }
            return entity.Usage > 0 ? false : true;
        }

        #region Sub data
        [HttpPost]
        [Route(Constants.LinkHr.Department + " / " + Constants.ActionLink.Update)]
        public IActionResult Department(Department entity)
        {
            entity.Alias = Utility.AliasConvert(entity.Name);
            bool exist = dbContext.Departments.CountDocuments(m => m.Alias.Equals(entity.Alias)) > 0;
            if (!exist)
            {
                dbContext.Departments.InsertOne(entity);
                return Json(new { result = true, source = "create", entity, message = "Tạo mới thành công" });
            }
            return Json(new { result = false, source = "create", entity, message = "Phòng/ban đã có. Không thể tạo 2 dữ liệu cùng tên." });
        }

        [HttpPost]
        [Route(Constants.LinkHr.Part + " / " + Constants.ActionLink.Update)]
        public IActionResult Part(Part entity)
        {
            entity.Alias = Utility.AliasConvert(entity.Name);
            bool exist = dbContext.Parts.CountDocuments(m => m.Alias.Equals(entity.Alias)) > 0;
            if (!exist)
            {
                dbContext.Parts.InsertOne(entity);
                return Json(new { result = true, source = "create", entity, message = "Tạo mới thành công" });
            }
            return Json(new { result = false, source = "create", entity, message = "Dữ liệu tồn tại. Không thể tạo 2 dữ liệu cùng tên." });
        }

        [HttpPost]
        //[Route(Constants.LinkHr.Title + " / " + Constants.ActionLink.Update)]
        public IActionResult Title(Title entity)
        {
            entity.Alias = Utility.AliasConvert(entity.Name);
            bool exist = dbContext.Titles.CountDocuments(m => m.Alias.Equals(entity.Alias)) > 0;
            if (!exist)
            {
                var lastest = dbContext.Titles.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Order).FirstOrDefault();
                var newNo = lastest.Order + 1;
                var newCode = Constants.System.viTriCodeTBLuong + newNo.ToString("000");
                entity.Code = newCode;
                entity.Order = newNo;
                dbContext.Titles.InsertOne(entity);
                return Json(new { result = true, source = "create", entity, message = "Tạo mới thành công" });
            }
            return Json(new { result = false, source = "create", entity, message = "Dữ liệu tồn tại. Không thể tạo 2 dữ liệu cùng tên." });
        }

        [HttpPost]
        [Route(Constants.LinkHr.Hospital + " / " + Constants.ActionLink.Update)]
        public IActionResult Hospital(BHYTHospital entity)
        {
            entity.Alias = Utility.AliasConvert(entity.Name);
            bool exist = dbContext.BHYTHospitals.CountDocuments(m => m.Alias.Equals(entity.Alias)) > 0;
            if (!exist)
            {
                dbContext.BHYTHospitals.InsertOne(entity);
                return Json(new { result = true, source = "create", entity, message = "Tạo mới thành công" });
            }
            return Json(new { result = false, source = "create", entity, message = "Bệnh viện đã có. Không thể tạo 2 dữ liệu cùng tên." });
        }
        #endregion
    }
}
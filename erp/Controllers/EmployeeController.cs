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
        public async Task<IActionResult> Index(string ten, string code, string finger, string nl, /*int? page, int? size,*/ string sortBy)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
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

            var viewModel = new EmployeeViewModel
            {
                Employees = results,
                Departments = departmentsFilter,
                EmployeesDisable = leaves,
                Records = (int)records,
                ten = ten,
                code = code,
                finger = finger,
                nl = nl
            };

            return View(viewModel);
        }

        [Route(Constants.LinkHr.Human+"/"+ Constants.LinkHr.Export +"/" + Constants.LinkHr.List)]
        public async Task<IActionResult> Export(string ten, string code, string finger, string nl, /*int? page, int? size,*/ string sortBy)
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

            if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
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

            string exportFolder = Path.Combine(_env.WebRootPath,"exports");
            string sFileName = @"hanh-chinh-nhan-su-" + DateTime.Now.ToString("ddMMyyyyhhmm") +".xlsx";
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
                row.CreateCell(4, CellType.String).SetCellValue("Phòng ban");
                row.CreateCell(5, CellType.String).SetCellValue("Số ngày phép còn lại");
                // Set style
                for(int i = 0; i <= 5; i++)
                {
                    row.Cells[i].CellStyle = cellStyleHeader;
                }
                rowIndex++;

                foreach (var data in results)
                {
                    row = sheet1.CreateRow(rowIndex);
                    row.CreateCell(0, CellType.Numeric).SetCellValue(rowIndex);
                    row.CreateCell(1, CellType.String).SetCellValue(data.CodeOld);
                    row.CreateCell(2, CellType.String).SetCellValue(data.FullName);
                    row.CreateCell(3, CellType.String).SetCellValue(data.Email);
                    row.CreateCell(4, CellType.String).SetCellValue(data.Department);
                    row.CreateCell(5, CellType.Numeric).SetCellValue((double)data.LeaveDayAvailable);
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

            //var viewModel = new EmployeeViewModel
            //{
            //    Employees = results,
            //    Departments = departmentsFilter,
            //    EmployeesDisable = leaves,
            //    Records = (int)records,
            //    ten = ten,
            //    code = code,
            //    finger = finger,
            //    nl = nl
            //};
            //return View(viewModel);
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
                        new Claim(ClaimTypes.Email, result.Email),
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
        [Route("nhan-su/thong-tin/{id}")]
        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            ViewData["LoginUserName"] = loginUserName;

            var userInformation = dbContext.Employees.Find(m => m.Id.Equals(login)).FirstOrDefault();
            if (userInformation == null)
            {
                #region snippet1
                HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                #endregion
                return RedirectToAction("login", "account");
            }

            // Check owner
            if (id != login)
            {
                if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.View)))
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
            #endregion

            var sortEmployee = Builders<Employee>.Sort.Ascending(m => m.FullName);
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true)).Sort(sortEmployee).ToList();

            var manager = new Employee();
            if (!string.IsNullOrEmpty(entity.ManagerId))
            {
                manager = dbContext.Employees.Find(m => m.Id.Equals(entity.ManagerId)).FirstOrDefault();
            }

            var viewModel = new EmployeeDataViewModel()
            {
                Employee = entity,
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
            bool right = Utility.IsRight(login, "nhan-su", (int)ERights.Add);

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
            var salaries = new List<Salary>();
            var salariesContent = dbContext.SalaryContents.Find(m => m.Enable.Equals(true)).SortBy(m => m.Order).ToList();
            foreach (var salaryContent in salariesContent)
            {
                salaries.Add(new Salary
                {
                    Type = salaryContent.SalaryType,
                    Title = salaryContent.Name,
                    Money = 0,
                    Order = salaryContent.Order
                });
            }

            var employee = new Employee
            {
                //Birthday = DateTime.Now.AddYears(-18),
                Joinday = DateTime.Now,
                Salaries = salaries
                //IdentityCardDate = DateTime.Now.AddYears(-18)
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
            bool right = Utility.IsRight(login, "nhan-su", (int)ERights.Add);

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

            var user = viewModel.Employee;
            if (checkUserName != user.UserName && !CheckExist(user))
            {
                return Json(new { result = false, source = "user", id = string.Empty, message = "Tên đăng nhập đã có trong hệ thống." });
            }

            if (checkEmail != user.Email && !CheckEmail(user))
            {
                return Json(new { result = false, source = "email", id = string.Empty, message = "Email đã có trong hệ thống." });
            }

            #region Update missing field
            if (user.Contracts != null)
            {
                if (user.Contracts.Count > 0)
                {
                    for (int i = user.Contracts.Count - 1; i >= 0; i--)
                    {
                        if (string.IsNullOrEmpty(user.Contracts[i].Code))
                        {
                            user.Contracts.RemoveAt(i);
                        }
                    }
                }
                user.Contracts = user.Contracts.Count == 0 ? null : user.Contracts;
            }

            if (string.IsNullOrEmpty(user.Department))
            {
                user.Department = string.Empty;
            }
            if (string.IsNullOrEmpty(user.Part))
            {
                user.Part = string.Empty;
            }
            if (string.IsNullOrEmpty(user.Title))
            {
                user.Title = string.Empty;
            }
            #endregion

            try
            {
                #region Settings
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-CA");
                var now = DateTime.Now;
                user.CreatedBy = login;
                user.UpdatedBy = login;
                user.CheckedBy = login;
                user.ApprovedBy = login;

                var settings = dbContext.Settings.Find(m => true).ToList();
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
                var pwdrandom = Guid.NewGuid().ToString("N");
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
                                    user.Avatar = currentImage;
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
                                    user.Cover = currentImage;
                                }
                            }
                        }
                    }
                }
                #endregion

                user.Code = sysCode;
                user.Password = sysPassword;
                user.AliasFullName = Utility.AliasConvert(user.FullName);
                decimal totalSalary = 0;
                if (user.Salaries != null && user.Salaries.Count > 0)
                {
                    foreach (var salaryItem in user.Salaries)
                    {
                        totalSalary += salaryItem.Money;
                    }
                }
                user.Salary = totalSalary;
                dbContext.Employees.InsertOne(user);

                var newUserId = user.Id;

                #region Send mail to IT setup email

                #endregion

                #region Notification
                var notificationImages = new List<Image>();
                if (user.Avatar != null && !string.IsNullOrEmpty(user.Avatar.FileName))
                {
                    notificationImages.Add(user.Avatar);
                }
                if (user.Cover != null && !string.IsNullOrEmpty(user.Cover.FileName))
                {
                    notificationImages.Add(user.Cover);
                }
                var notification = new Notification
                {
                    Type = Constants.Notification.HR,
                    Title = Constants.Notification.CreateHR,
                    Content = user.FullName,
                    Link = "/hr/nhan-su/thong-tin/" + user.Id,
                    Images = notificationImages.Count > 0 ? notificationImages : null,
                    UserId = string.Empty,
                    CreatedBy = loginUserName
                };
                dbContext.Notifications.InsertOne(notification);
                #endregion

                #region Activities
                var objectId = newUserId.ToString();
                var objectName = user.UserName;

                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.Employees,
                    Action = Constants.Action.Create,
                    Value = objectId,
                    Description = Constants.Action.Create + " " + Constants.Collection.Employees + " " + objectName,
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion

                return Json(new { result = true, source = "create", id = user.Id, message = "Khởi tạo thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "create", id = string.Empty, message = ex.Message });
            }
        }

        // GET: Users/Edit/5
        [Route("nhan-su/cap-nhat/{id}")]
        public ActionResult Edit(string id)
        {
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;

            // Check owner
            if (id != login)
            {
                if (!(loginUserName == Constants.System.account ? true : Utility.IsRight(login, "nhan-su", (int)ERights.Edit)))
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
            #endregion

            var sortEmployee = Builders<Employee>.Sort.Ascending(m => m.FullName);
            var employees = dbContext.Employees.Find(m => m.Enable.Equals(true)).Sort(sortEmployee).ToList();

            var currentSalaries = new List<Salary>();
            var currentSalariesString = new List<string>();
            if (entity.Salaries != null && entity.Salaries.Count > 0)
            {
                currentSalaries = entity.Salaries.ToList();
                foreach (var currentSalary in currentSalaries)
                {
                    if (!string.IsNullOrEmpty(currentSalary.Title))
                    {
                        currentSalariesString.Add(currentSalary.Title);
                    }
                }
            }
            var salariesContent = dbContext.SalaryContents.Find(m => m.Enable.Equals(true) && !currentSalariesString.Contains(m.Name)).SortBy(m => m.Order).ToList();
            foreach (var salaryContent in salariesContent)
            {
                currentSalaries.Add(new Salary
                {
                    Type = salaryContent.SalaryType,
                    Title = salaryContent.Name,
                    Money = 0,
                    Order = salaryContent.Order
                });
            }
            entity.Salaries = currentSalaries;

            var viewModel = new EmployeeDataViewModel()
            {
                Employee = entity,
                Employees = employees
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
            #region Authorization
            var login = User.Identity.Name;
            var loginUserName = User.Claims.Where(m => m.Type.Equals("UserName")).FirstOrDefault().Value;
            bool right = Utility.IsRight(login, "nhan-su", (int)ERights.Edit);

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

                #region Salary
                decimal totalSalary = 0;
                if (entity.Salaries != null && entity.Salaries.Count > 0)
                {
                    foreach (var salaryItem in entity.Salaries)
                    {
                        totalSalary += salaryItem.Money;
                    }
                }
                entity.Salary = totalSalary;
                #endregion

                #region Update Data
                var filter = Builders<Employee>.Filter.Eq(m => m.Id, entity.Id);
                var update = Builders<Employee>.Update
                    .Set(m => m.UpdatedBy, login)
                    .Set(m => m.UpdatedOn, now)
                    .Set(m => m.Workplaces, entity.Workplaces)
                    .Set(m => m.IsTimeKeeper, entity.IsTimeKeeper)
                    .Set(m => m.LeaveLevelYear, entity.LeaveLevelYear)
                    .Set(m => m.LeaveDayAvailable, entity.LeaveDayAvailable)
                    .Set(m => m.FullName, entity.FullName)
                    .Set(m => m.FirstName, entity.FirstName)
                    .Set(m => m.LastName, entity.LastName)
                    .Set(m => m.AliasFullName, Utility.AliasConvert(entity.FullName))
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
                    .Set(m => m.Passport, entity.IdentityCard)
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
                    .Set(m => m.Salaries, entity.Salaries)
                    .Set(m => m.Salary, entity.Salary)
                    // BHXH
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
                    // FAMILY
                    .Set(m => m.EmployeeFamilys, entity.EmployeeFamilys)
                    // CONTRACT
                    .Set(m => m.Contracts, entity.Contracts)
                    .Set(m => m.EmployeeEducations, entity.EmployeeEducations);

                if (avatarImage != null && !string.IsNullOrEmpty(avatarImage.FileName))
                {
                    update = update.Set(m => m.Avatar, avatarImage);
                }

                if (coverImage != null && !string.IsNullOrEmpty(coverImage.FileName))
                {
                    update = update.Set(m => m.Cover, coverImage);
                }

                dbContext.Employees.UpdateOne(filter, update);
                #endregion

                var userId = entity.Id;

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
                    Title = Constants.Notification.UpdateHR,
                    Content = entity.FullName,
                    Link = "/hr/nhan-su/thong-tin/" + entity.Id,
                    Images = notificationImages.Count > 0 ? notificationImages : null,
                    UserId = string.Empty,
                    CreatedBy = loginUserName
                };
                dbContext.Notifications.InsertOne(notification);
                #endregion


                #region Activities
                var activity = new TrackingUser
                {
                    UserId = userId,
                    Function = Constants.Collection.NhanViens,
                    Action = Constants.Action.Edit,
                    Value = userId,
                    Description = Constants.Action.Edit + " " + Constants.Collection.Employees + " " + entity.UserName
                };
                dbContext.TrackingUsers.InsertOne(activity);
                #endregion
                return Json(new { result = true, source = "update", id = entity.Id, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "update", id = entity.Id, message = ex.Message });
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
                        Description = entity.UserName + Constants.Flag + entity.FullName
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
            var employees = dbContext.Employees.Find(m=>true).ToList();
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

        [Route(Constants.LinkHr.ChildrenReport+ "/"+ Constants.LinkHr.Export)]
        public async Task<IActionResult> ChildrenReportExport(string fileName)
        {
            string sWebRootFolder = _env.WebRootPath;
            fileName = @"demo.xlsx";
            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, fileName);
            FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, fileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(sWebRootFolder, fileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Demo");
                IRow row = excelSheet.CreateRow(0);

                row.CreateCell(0).SetCellValue("ID");
                row.CreateCell(1).SetCellValue("Name");
                row.CreateCell(2).SetCellValue("Age");

                row = excelSheet.CreateRow(1);
                row.CreateCell(0).SetCellValue(1);
                row.CreateCell(1).SetCellValue("Kane Williamson");
                row.CreateCell(2).SetCellValue(29);

                row = excelSheet.CreateRow(2);
                row.CreateCell(0).SetCellValue(2);
                row.CreateCell(1).SetCellValue("Martin Guptil");
                row.CreateCell(2).SetCellValue(33);

                row = excelSheet.CreateRow(3);
                row.CreateCell(0).SetCellValue(3);
                row.CreateCell(1).SetCellValue("Colin Munro");
                row.CreateCell(2).SetCellValue(23);

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(sWebRootFolder, fileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        #region Sub data
        [HttpPost]
        [Route("nhan-su/phong-ban/tao-moi/")]
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
        [Route("nhan-su/bo-phan/tao-moi/")]
        public IActionResult Part(Part entity)
        {
            entity.Alias = Utility.AliasConvert(entity.Name);
            bool exist = dbContext.Parts.CountDocuments(m => m.Alias.Equals(entity.Alias)) > 0;
            if (!exist)
            {
                dbContext.Parts.InsertOne(entity);
                return Json(new { result = true, source = "create", entity, message = "Tạo mới thành công" });
            }
            return Json(new { result = false, source = "create", entity, message = "Bộ phận đã có. Không thể tạo 2 dữ liệu cùng tên." });
        }

        [HttpPost]
        [Route("nhan-su/cong-viec/tao-moi/")]
        public IActionResult Title(Title entity)
        {
            entity.Alias = Utility.AliasConvert(entity.Name);
            bool exist = dbContext.Titles.CountDocuments(m => m.Alias.Equals(entity.Alias)) > 0;
            if (!exist)
            {
                dbContext.Titles.InsertOne(entity);
                return Json(new { result = true, source = "create", entity, message = "Tạo mới thành công" });
            }
            return Json(new { result = false, source = "create", entity, message = "Công việc đã có. Không thể tạo 2 dữ liệu cùng tên." });
        }

        [HttpPost]
        [Route("nhan-su/benh-vien/tao-moi/")]
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

        public string GeneralEmail(string input)
        {
            return Utility.EmailConvert(input);
        }

        public void SendMailNewUser(Employee entity, string pwd)
        {
            // Send it setup email
            // Send Hr reception (base location) information
            // If user not location, send all reception information
            //      1. Information of user
            //      2. erp account (cause it not create acc, not sent data)


            var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = "IT Helpdesk", Address = "" }
            };

            // Send an email with this link
            //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            //var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
            //Email from Email Template
            var callbackUrl = "/";
            string Message = "[erp-tribat] Nhân sự mới - Setup máy, email, phần mềm liên quan.";
            // string body;

            var webRoot = _env.WebRootPath; //get wwwroot Folder

            //Get TemplateFile located at wwwroot/Templates/EmailTemplate/Register_EmailTemplate.html
            var pathToFile = _env.WebRootPath
                    + Path.DirectorySeparatorChar.ToString()
                    + "Templates"
                    + Path.DirectorySeparatorChar.ToString()
                    + "EmailTemplate"
                    + Path.DirectorySeparatorChar.ToString()
                    + "Confirm_Account_Registration.html";

            var subject = "Thông tin đăng nhập hệ thống TRIBAT - ERP.";

            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }
            //{0} : Subject
            //{1} : DateTime
            //{2} : Email
            //{3} : Username
            //{4} : Password
            //{5} : Message
            //{6} : callbackURL

            string messageBody = string.Format(builder.HtmlBody,
                subject,
                String.Format("{0:dddd, d MMMM yyyy}", DateTime.Now),
                entity.FullName,
                entity.UserName,
                pwd,
                Message,
                callbackUrl
                );

            var emailMessage = new EmailMessage()
            {
                ToAddresses = tos,
                Subject = subject,
                BodyContent = messageBody
            };
            _emailSender.SendEmailAsync(emailMessage);

            ViewData["Message"] = $"Please confirm your account by clicking this link: <a href='{callbackUrl}' class='btn btn-primary'>Confirmation Link</a>";
            ViewData["MessageValue"] = "1";

            _logger.LogInformation(3, "User created a new account with password.");
        }

        private bool EntityExists(string id)
        {
            return dbContext.Employees.Count(m => m.Id == id) > 0;
        }

        [Route("nhan-su/import")]
        public IActionResult Import()
        {
            return View();
        }

        public async Task<IActionResult> OnPostExport()
        {
            string sWebRootFolder = _env.WebRootPath;
            string sFileName = @"demo.xlsx";
            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(sWebRootFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Demo");
                IRow row = excelSheet.CreateRow(0);

                row.CreateCell(0).SetCellValue("ID");
                row.CreateCell(1).SetCellValue("Name");
                row.CreateCell(2).SetCellValue("Age");

                row = excelSheet.CreateRow(1);
                row.CreateCell(0).SetCellValue(1);
                row.CreateCell(1).SetCellValue("Kane Williamson");
                row.CreateCell(2).SetCellValue(29);

                row = excelSheet.CreateRow(2);
                row.CreateCell(0).SetCellValue(2);
                row.CreateCell(1).SetCellValue("Martin Guptil");
                row.CreateCell(2).SetCellValue(33);

                row = excelSheet.CreateRow(3);
                row.CreateCell(0).SetCellValue(3);
                row.CreateCell(1).SetCellValue("Colin Munro");
                row.CreateCell(2).SetCellValue(23);

                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(sWebRootFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }

        public ActionResult OnPostImport()
        {
            IFormFile file = Request.Form.Files[0];
            string folderName = "Upload";
            string webRootPath = _env.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            StringBuilder sb = new StringBuilder();
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
                        sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook  
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
                    }
                    IRow headerRow = sheet.GetRow(0); //Get Header Row
                    int cellCount = headerRow.LastCellNum;
                    sb.Append("<table class='table'><tr>");
                    for (int j = 0; j < cellCount; j++)
                    {
                        NPOI.SS.UserModel.ICell cell = headerRow.GetCell(j);
                        if (cell == null || string.IsNullOrWhiteSpace(cell.ToString())) continue;
                        sb.Append("<th>" + cell.ToString() + "</th>");
                    }
                    sb.Append("</tr>");
                    sb.AppendLine("<tr>");
                    for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        for (int j = row.FirstCellNum; j < cellCount; j++)
                        {
                            if (row.GetCell(j) != null)
                                sb.Append("<td>" + row.GetCell(j).ToString() + "</td>");
                        }
                        sb.AppendLine("</tr>");
                    }
                    sb.Append("</table>");
                }
            }
            return this.Content(sb.ToString());
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
    }
}
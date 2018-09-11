using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Data;
using Models;
using Common.Utilities;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using MongoDB.Bson;
using System.Globalization;
using MimeKit;
using Services;
using Microsoft.AspNetCore.Authorization;

namespace erp.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _env;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public FileController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, IEmailSender emailSender,
            ISmsSender smsSender, ILogger<FileController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _env = env;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        [Route("/tai-lieu/")]
        public IActionResult Index()
        {
            return View();
        }

        #region HCNS-Docs
        [Route("/tai-lieu/co-so-kham-chua-benh-ban-dau/")]
        public IActionResult Kcb()
        {
            return View();
        }

        [Route("/tai-lieu/co-so-kham-chua-benh-ban-dau/import/")]
        [HttpPost]
        public ActionResult KcbImport()
        {
            dbContext.BHYTHospitals.DeleteMany(m => true);

            int sheetCal = 0;
            // X is 4. guest is 3
            int headerCal = 7;
            if (!String.IsNullOrEmpty(Request.Form["sheetCal"]))
            {
                sheetCal = Convert.ToInt32(Request.Form["sheetCal"]);
            }
            if (!String.IsNullOrEmpty(Request.Form["headerCal"]))
            {
                headerCal = Convert.ToInt32(Request.Form["headerCal"]);
            }

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
                        sheet = hssfwb.GetSheetAt(sheetCal); //get first sheet from workbook  
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet = hssfwb.GetSheetAt(sheetCal); //get first sheet from workbook   
                    }
                    IRow headerRow = sheet.GetRow(headerCal); //Get Header Row
                    int cellCount = headerRow.LastCellNum;

                    var x = 1;
                    var systemDate = Constants.MinDate;
                    /*for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) *///Read Excel File
                    for (int i = (headerCal + 1); i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var hospital = new BHYTHospital()
                        {
                            Code = GetFormattedCellValue(row.GetCell(3)),
                            Local = GetFormattedCellValue(row.GetCell(2)),
                            City = "Hồ Chí Minh",
                            Name = GetFormattedCellValue(row.GetCell(1)),
                            Address = GetFormattedCellValue(row.GetCell(4)),
                            Condition = GetFormattedCellValue(row.GetCell(5)),
                            Note = GetFormattedCellValue(row.GetCell(6))
                        };
                        dbContext.BHYTHospitals.InsertOne(hospital);
                        x++;
                    }
                }
            }
            return Json(new { url = "/hr/nhan-su" });
        }
        #endregion

        #region HCNS
        [Route("/tai-lieu/nhan-vien/")]
        public IActionResult NhanVien()
        {
            return View();
        }

        [Route("/tai-lieu/nhan-vien/export/")]
        public async Task<IActionResult> NhanVienExport()
        {
            string sWebRootFolder = _env.WebRootPath + Constants.FlagCacheKey;
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

        // Da update bang tay. khong active
        //[Route("/tai-lieu/nhan-vien/import/")]
        //[HttpPost]
        //public ActionResult NhanVienImport()
        //{
        //    dbContext.Employees.DeleteMany(m => !m.UserName.Equals(Constants.System.account));

        //    #region Settings
        //    System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-CA");

        //    var settings = dbContext.Settings.Find(m => true).ToList();
        //    // always have value
        //    var identityCardExpired = Convert.ToInt32(settings.Where(m => m.Key.Equals("identityCardExpired")).First().Value);
        //    var employeeCodeFirst = settings.Where(m => m.Key.Equals("employeeCodeFirst")).First().Value;
        //    var employeeCodeLength = settings.Where(m => m.Key.Equals("employeeCodeLength")).First().Value;
        //    var enableActivity = false;
        //    var enableSendMail = false;
        //    var leaveDayAvailable = 12;
        //    #endregion

        //    int sheetCal = 1;
        //    // X is 4. guest is 3
        //    int headerCal = 4;
        //    if (!String.IsNullOrEmpty(Request.Form["sheetCal"]))
        //    {
        //        sheetCal = Convert.ToInt32(Request.Form["sheetCal"]);
        //    }
        //    if (!String.IsNullOrEmpty(Request.Form["headerCal"]))
        //    {
        //        headerCal = Convert.ToInt32(Request.Form["headerCal"]);
        //    }

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
        //                sheet = hssfwb.GetSheetAt(sheetCal); //get first sheet from workbook  
        //            }
        //            else
        //            {
        //                XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
        //                sheet = hssfwb.GetSheetAt(sheetCal); //get first sheet from workbook   
        //            }
        //            IRow headerRow = sheet.GetRow(headerCal); //Get Header Row
        //            int cellCount = headerRow.LastCellNum;

        //            var x = 1;
        //            var systemDate = Constants.MinDate;
        //            /*for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) *///Read Excel File
        //            for (int i = (headerCal + 1); i <= sheet.LastRowNum; i++)
        //            {
        //                IRow row = sheet.GetRow(i);
        //                if (row == null) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

        //                #region Check Data Import
        //                //for (int ck = 1; ck <= 109; ck++)
        //                //{
        //                //    Console.WriteLine(GetFormattedCellValue(row.GetCell(ck)));
        //                //}
        //                #endregion

        //                // STT
        //                if (i == 180) continue;
        //                if (i > 180)
        //                {
        //                    #region System Generate
        //                    var fullName = GetFormattedCellValue(row.GetCell(1));
        //                    var email = GetFormattedCellValue(row.GetCell(1));
        //                    if (string.IsNullOrEmpty(email))
        //                    {
        //                        email = Utility.EmailConvert(fullName);
        //                    }
        //                    var sysUserName = email.Replace(Constants.MailExtension, string.Empty);
        //                    // use send mail to user.
        //                    var pwdrandom = Guid.NewGuid().ToString("N");
        //                    var sysPassword = Helpers.Helper.HashedPassword(pwdrandom);
        //                    var sysCode = employeeCodeFirst + x.ToString($"D{employeeCodeLength}");
        //                    #endregion

        //                    #region Data fix
        //                    var workplaces = new List<Workplace>
        //                    {
        //                        new Workplace
        //                        {
        //                            Code = "NM",
        //                            Name = "Nhà máy",
        //                            //Fingerprint = GetFormattedCellValue(row.GetCell(6)),
        //                            WorkingScheduleTime = "8:00-17:00"
        //                        }
        //                    };
        //                    var birthday = systemDate;
        //                    var joinday = systemDate;
        //                    var title = GetFormattedCellValue(row.GetCell(3));
        //                    var department = GetFormattedCellValue(row.GetCell(4));
        //                    #endregion
        //                    var nhanvien = new Employee()
        //                    {
        //                        UserName = sysUserName,
        //                        Email = email,
        //                        FullName = fullName,
        //                        AliasFullName = Utility.AliasConvert(fullName),
        //                        Workplaces = workplaces,
        //                        Password = sysPassword,
        //                        Code = sysCode,
        //                        Department = department,
        //                        Title = title,
        //                        Birthday = birthday,
        //                        Joinday = joinday,
        //                        LeaveDayAvailable = 12
        //                    };
        //                    dbContext.Employees.InsertOne(nhanvien);
        //                    #region Activities
        //                    if (enableActivity)
        //                    {
        //                        var activity = new TrackingUser
        //                        {
        //                            UserId = "",
        //                            Function = Constants.Collection.NhanViens,
        //                            Action = "create",
        //                            Value = nhanvien.Code,
        //                            Description = "create" + " " + Constants.Collection.NhanViens + " " + nhanvien.Code,
        //                            Created = DateTime.Now,
        //                            Link = "/nhan-vien/chi-tiet/" + nhanvien.Code
        //                        };
        //                        dbContext.TrackingUsers.InsertOne(activity);
        //                    }
        //                    #endregion
        //                }
        //                else
        //                {
        //                    #region System Generate
        //                    var fullName = GetFormattedCellValue(row.GetCell(5));
        //                    var email = GetFormattedCellValue(row.GetCell(7));
        //                    if (string.IsNullOrEmpty(email))
        //                    {
        //                        email = Utility.EmailConvert(fullName);
        //                    }
        //                    var sysUserName = email.Replace(Constants.MailExtension, string.Empty);
        //                    // use send mail to user.
        //                    var pwdrandom = Guid.NewGuid().ToString("N");
        //                    var sysPassword = Helpers.Helper.HashedPassword(pwdrandom);
        //                    var sysCode = employeeCodeFirst + x.ToString($"D{employeeCodeLength}");
        //                    #endregion

        //                    #region Data fix
        //                    var workplaces = new List<Workplace>
        //                    {
        //                        new Workplace
        //                        {
        //                            Code = GetFormattedCellValue(row.GetCell(8)),
        //                            Name = GetFormattedCellValue(row.GetCell(8)) == "NM" ? "Nhà máy" : "Văn phòng",
        //                            Fingerprint = GetFormattedCellValue(row.GetCell(6)),
        //                            WorkingScheduleTime = GetFormattedCellValue(row.GetCell(8)) == "NM" ? "7:00-16:00" : "8:00-17:00",
        //                        }
        //                    };
        //                    var birthday = systemDate;
        //                    // Cause format multi
        //                    var databirthday = GetFormattedCellValue(row.GetCell(9));
        //                    if (!String.IsNullOrEmpty(databirthday))
        //                    {
        //                        try
        //                        {
        //                            birthday = DateTime.SpecifyKind((DateTime.ParseExact(databirthday, "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                        }
        //                        catch
        //                        {
        //                            try
        //                            {
        //                                birthday = DateTime.SpecifyKind((DateTime.ParseExact(databirthday, "MM/dd/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                            }
        //                            catch
        //                            {
        //                            }
        //                        }
        //                    }

        //                    var joinday = systemDate;
        //                    try
        //                    {
        //                        string s = GetFormattedCellValue(row.GetCell(11));
        //                        if (!string.IsNullOrEmpty(s))
        //                        {
        //                            int idx = s.LastIndexOf('-');
        //                            if (idx != -1)
        //                            {
        //                                if (s.Substring(idx + 1).Length < 3)
        //                                {
        //                                    var s1 = s.Substring(0, idx);
        //                                    var s2 = "-20" + s.Substring(idx + 1);
        //                                    s = s1 + s2;
        //                                }
        //                            }
        //                            joinday = DateTime.SpecifyKind((DateTime.ParseExact(s, "dd-MM-yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 11 at row {} : " + ex, row);
        //                    }
        //                    var identityCardDate = systemDate;
        //                    try
        //                    {
        //                        identityCardDate = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(19))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(19)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 19 at row {} : " + ex, row);
        //                    }
        //                    var mobiles = new List<EmployeeMobile>();
        //                    foreach (var item in GetFormattedCellValue(row.GetCell(27)).Split(';'))
        //                    {
        //                        mobiles.Add(new EmployeeMobile()
        //                        {
        //                            Type = "Work",
        //                            Number = item
        //                        });
        //                    }
        //                    var bangtukhaiungvien = 0;
        //                    try
        //                    {
        //                        bangtukhaiungvien = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(28))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(28)));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 28 at row {} : " + ex, row);
        //                    }
        //                    var donungtuyen = 0;
        //                    try
        //                    {
        //                        donungtuyen = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(29))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(29)));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 29 at row {} : " + ex, row);
        //                    }
        //                    var soyeulylich = 0;
        //                    try
        //                    {
        //                        soyeulylich = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(30))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(30)));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 30 at row {} : " + ex, row);
        //                    }
        //                    var giaykhaisinh = 0;
        //                    try
        //                    {
        //                        giaykhaisinh = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(31))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(31)));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 31 at row {} : " + ex, row);
        //                    }
        //                    var giaychungminh = 0;
        //                    try
        //                    {
        //                        giaychungminh = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(32))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(32)));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 32 at row {} : " + ex, row);
        //                    }
        //                    var bangtotnghiep = 0;
        //                    try
        //                    {
        //                        bangtotnghiep = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(33))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(33)));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 33 at row {} : " + ex, row);
        //                    }
        //                    var bangdiemhocba = 0;
        //                    try
        //                    {
        //                        bangdiemhocba = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(34))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(34)));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 34 at row {} : " + ex, row);
        //                    }
        //                    var hokhau = 0;
        //                    try
        //                    {
        //                        hokhau = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(35))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(35)));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 35 at row {} : " + ex, row);
        //                    }
        //                    var xacnhannhansu = 0;
        //                    try
        //                    {
        //                        xacnhannhansu = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(36))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(36)));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 36 at row {} : " + ex, row);
        //                    }
        //                    var anh = 0;
        //                    try
        //                    {
        //                        anh = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(37))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(37)));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 31 at row {} : " + ex, row);
        //                    }

        //                    var probationStart = systemDate;
        //                    try
        //                    {
        //                        probationStart = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(39))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(39)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 39 at row {} : " + ex, row);
        //                    }
        //                    var probationEnd = systemDate;
        //                    try
        //                    {
        //                        probationEnd = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(40))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(40)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 40 at row {} : " + ex, row);
        //                    }
        //                    var thoivuStart = systemDate;
        //                    try
        //                    {
        //                        thoivuStart = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(42))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(42)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 42 at row {} : " + ex, row);
        //                    }
        //                    var thoivuEnd = systemDate;
        //                    try
        //                    {
        //                        thoivuEnd = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(43))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(43)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 43 at row {} : " + ex, row);
        //                    }

        //                    var thoihanlan1So = GetFormattedCellValue(row.GetCell(44));
        //                    var thoihanlan1Count = 0;
        //                    try
        //                    {
        //                        thoihanlan1Count = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(45))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(45)).Replace(" năm", ""));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 45 at row {} : " + ex, row);
        //                    }
        //                    var thoihanlan1Start = systemDate;
        //                    try
        //                    {
        //                        thoihanlan1Start = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(46))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(46)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc); ;
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 46 at row {} : " + ex, row);
        //                    }
        //                    var thoihanlan1End = systemDate;
        //                    try
        //                    {
        //                        thoihanlan1End = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(47))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(47)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc); ;
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 47 at row {} : " + ex, row);
        //                    }
        //                    var giahanlan1So = GetFormattedCellValue(row.GetCell(48));
        //                    var giahanlan1Count = 0;
        //                    try
        //                    {
        //                        giahanlan1Count = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(49))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(49)).Replace(" năm", ""));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 49 at row {} : " + ex, row);
        //                    }
        //                    var giahanlan1Start = systemDate;
        //                    try
        //                    {
        //                        giahanlan1Start = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(50))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(50)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 50 at row {} : " + ex, row);
        //                    }
        //                    var giahanlan1End = systemDate;
        //                    try
        //                    {
        //                        giahanlan1End = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(51))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(51)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 47 at row {} : " + ex, row);
        //                    }

        //                    var thoihanlan2So = GetFormattedCellValue(row.GetCell(52));
        //                    var thoihanlan2Count = 0;
        //                    try
        //                    {
        //                        thoihanlan2Count = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(53))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(53)).Replace(" năm", ""));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 53 at row {} : " + ex, row);
        //                    }
        //                    var thoihanlan2Start = systemDate;
        //                    try
        //                    {
        //                        thoihanlan2Start = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(54))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(54)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 54 at row {} : " + ex, row);
        //                    }
        //                    var thoihanlan2End = systemDate;
        //                    try
        //                    {
        //                        thoihanlan2End = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(55))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(55)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 55 at row {} : " + ex, row);
        //                    }
        //                    var giahanlan2So = GetFormattedCellValue(row.GetCell(56));
        //                    var giahanlan2Count = 0;
        //                    try
        //                    {
        //                        giahanlan2Count = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(57))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(57)).Replace(" năm", ""));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 57 at row {} : " + ex, row);
        //                    }
        //                    var giahanlan2Start = systemDate;
        //                    try
        //                    {
        //                        giahanlan2Start = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(58))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(58)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 58 at row {} : " + ex, row);
        //                    }
        //                    var giahanlan2End = systemDate;
        //                    try
        //                    {
        //                        giahanlan2End = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(59))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(59)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 59 at row {} : " + ex, row);
        //                    }

        //                    var thoihanlan3So = GetFormattedCellValue(row.GetCell(60));
        //                    var thoihanlan3Count = 0;
        //                    try
        //                    {
        //                        thoihanlan3Count = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(61))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(61)).Replace(" năm", ""));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 61 at row {} : " + ex, row);
        //                    }
        //                    var thoihanlan3Start = systemDate;
        //                    try
        //                    {
        //                        thoihanlan3Start = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(62))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(62)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 62 at row {} : " + ex, row);
        //                    }
        //                    var thoihanlan3End = systemDate;
        //                    try
        //                    {
        //                        thoihanlan3End = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(63))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(63)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 63 at row {} : " + ex, row);
        //                    }

        //                    var khongthoihanSo = GetFormattedCellValue(row.GetCell(64));
        //                    var khongthoihanStart = systemDate;
        //                    try
        //                    {
        //                        khongthoihanStart = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(65))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(65)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 65 at row {} : " + ex, row);
        //                    }
        //                    var bhxhEnable = false;
        //                    if (GetFormattedCellValue(row.GetCell(67)) == "x")
        //                    {
        //                        bhxhEnable = true;
        //                    }
        //                    var bhxhStart = systemDate;
        //                    try
        //                    {
        //                        bhxhStart = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(68))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(68)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 68 at row {} : " + ex, row);
        //                    }
        //                    var birthdayVo = systemDate;
        //                    try
        //                    {
        //                        birthdayVo = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(97))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(97)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 97 at row {} : " + ex, row);
        //                    }
        //                    var birthdayCon1 = systemDate;
        //                    try
        //                    {
        //                        birthdayCon1 = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(99))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(99)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 99 at row {} : " + ex, row);
        //                    }
        //                    var birthdayCon2 = systemDate;
        //                    try
        //                    {
        //                        birthdayCon2 = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(101))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(101)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 101 at row {} : " + ex, row);
        //                    }
        //                    var birthdayCon3 = systemDate;
        //                    try
        //                    {
        //                        birthdayCon3 = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(103))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(103)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 103 at row {} : " + ex, row);
        //                    }
        //                    var birthdayCon4 = systemDate;
        //                    try
        //                    {
        //                        birthdayCon4 = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(105))) ? systemDate : DateTime.SpecifyKind((DateTime.ParseExact(GetFormattedCellValue(row.GetCell(105)), "dd/MM/yyyy", CultureInfo.InvariantCulture)), DateTimeKind.Utc);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogError(LoggingEvents.FormatType, "Get data cell 105 at row {} : " + ex, row);
        //                    }
        //                    #endregion

        //                    var nhanvien = new Employee()
        //                    {
        //                        UserName = sysUserName,
        //                        Password = sysPassword,
        //                        Code = sysCode,
        //                        Department = GetFormattedCellValue(row.GetCell(1)),
        //                        Part = GetFormattedCellValue(row.GetCell(2)),
        //                        CodeOld = GetFormattedCellValue(row.GetCell(4)),
        //                        FullName = fullName,
        //                        AliasFullName = Utility.AliasConvert(fullName),
        //                        Workplaces = workplaces,
        //                        Email = email,
        //                        LeaveDayAvailable = leaveDayAvailable,
        //                        Birthday = birthday,
        //                        Bornplace = GetFormattedCellValue(row.GetCell(10)),
        //                        Joinday = joinday,
        //                        Title = GetFormattedCellValue(row.GetCell(13)),
        //                        // CONG VIEC
        //                        Function = GetFormattedCellValue(row.GetCell(12)),
        //                        AddressResident = GetFormattedCellValue(row.GetCell(14)),
        //                        AddressTemporary = GetFormattedCellValue(row.GetCell(15)),
        //                        Gender = GetFormattedCellValue(row.GetCell(16)),
        //                        //Gender = GetFormattedCellValue(row.GetCell(17)),
        //                        IdentityCard = GetFormattedCellValue(row.GetCell(18)),
        //                        IdentityCardDate = identityCardDate,
        //                        IdentityCardPlace = GetFormattedCellValue(row.GetCell(20)),
        //                        Cards = new List<Card>()
        //                    {
        //                        new Card()
        //                        {
        //                            Type = "CMND",
        //                            Code = GetFormattedCellValue(row.GetCell(18)),
        //                            Created = identityCardDate,
        //                            Location = GetFormattedCellValue(row.GetCell(20)),
        //                            Expired = identityCardDate.AddYears(identityCardExpired)
        //                        }
        //                    },
        //                        StatusMarital = GetFormattedCellValue(row.GetCell(22)),
        //                        Nation = GetFormattedCellValue(row.GetCell(23)),
        //                        Religion = GetFormattedCellValue(row.GetCell(24)),
        //                        // Child
        //                        Certificates = new List<Certificate>()
        //                    {
        //                        new Certificate()
        //                        {
        //                            Type = GetFormattedCellValue(row.GetCell(25)),
        //                            Location = GetFormattedCellValue(row.GetCell(26))
        //                        }
        //                    },
        //                        Mobiles = mobiles,
        //                        StorePapers = new List<StorePaper>()
        //                    {
        //                        new StorePaper()
        //                        {
        //                            Type = "Bản tự khai ứng viên",
        //                            Description = string.Empty,
        //                            Count = bangtukhaiungvien,
        //                            Unit = string.Empty
        //                        },
        //                        new StorePaper()
        //                        {
        //                            Type = "Đơn ứng tuyển",
        //                            Description = string.Empty,
        //                            Count = donungtuyen,
        //                            Unit = string.Empty
        //                        },
        //                        new StorePaper()
        //                        {
        //                            Type = "Sơ yếu lý lịch",
        //                            Description = string.Empty,
        //                            Count = soyeulylich,
        //                            Unit = string.Empty
        //                        },
        //                        new StorePaper()
        //                        {
        //                            Type = "Giấy khai sinh",
        //                            Description = string.Empty,
        //                            Count = giaykhaisinh,
        //                            Unit = string.Empty
        //                        },
        //                        new StorePaper()
        //                        {
        //                            Type = "Chứng minh thư",
        //                            Description = string.Empty,
        //                            Count = giaychungminh,
        //                            Unit = string.Empty
        //                        },
        //                        new StorePaper()
        //                        {
        //                            Type = "Bằng/ chứng nhận tốt nghiệp",
        //                            Description = string.Empty,
        //                            Count = bangtotnghiep,
        //                            Unit = string.Empty
        //                        },
        //                        new StorePaper()
        //                        {
        //                            Type = "Bảng điểm hoặc học bạ",
        //                            Description = string.Empty,
        //                            Count = bangdiemhocba,
        //                            Unit = string.Empty
        //                        },
        //                        new StorePaper()
        //                        {
        //                            Type = "Hộ khẩu",
        //                            Description = string.Empty,
        //                            Count = hokhau,
        //                            Unit = string.Empty
        //                        },
        //                        new StorePaper()
        //                        {
        //                            Type = "Xác nhận nhân sự",
        //                            Description = string.Empty,
        //                            Count = xacnhannhansu,
        //                            Unit = string.Empty
        //                        },
        //                        new StorePaper()
        //                        {
        //                            Type = "Ảnh",
        //                            Description = string.Empty,
        //                            Count = anh,
        //                            Unit = string.Empty
        //                        }
        //                    },
        //                        Contracts = new List<Contract>()
        //                    {
        //                        new Contract()
        //                        {
        //                            Type = "THỬ VIỆC",
        //                            Code = GetFormattedCellValue(row.GetCell(38)),
        //                            Start = probationStart,
        //                            End = probationEnd
        //                        },
        //                        new Contract()
        //                        {
        //                            Type = "THỜI VỤ",
        //                            Code = GetFormattedCellValue(row.GetCell(41)),
        //                            Start = thoivuStart,
        //                            End = thoivuEnd
        //                        },
        //                        new Contract()
        //                        {
        //                            Type = "HĐ XÁC ĐỊNH THỜI HẠN LẦN 1",
        //                            Code = thoihanlan1So,
        //                            Duration = thoihanlan1Count,
        //                            Start = thoihanlan1Start,
        //                            End = thoihanlan1End
        //                        },
        //                        new Contract()
        //                        {
        //                            Type = "PHỤ LỤC GIA HẠN HĐ LẦN 1",
        //                            Code = giahanlan1So,
        //                            Duration = giahanlan1Count,
        //                            Start = giahanlan1Start,
        //                            End = giahanlan1End
        //                        },
        //                         new Contract()
        //                        {
        //                            Type = "HĐ XÁC ĐỊNH THỜI HẠN LẦN 2",
        //                            Code = thoihanlan2So,
        //                            Duration = thoihanlan2Count,
        //                            Start = thoihanlan2Start,
        //                            End = thoihanlan2End
        //                        },
        //                        new Contract()
        //                        {
        //                            Type = "PHỤ LỤC GIA HẠN HĐ LẦN 2",
        //                            Code = giahanlan2So,
        //                            Duration = giahanlan2Count,
        //                            Start = giahanlan2Start,
        //                            End = giahanlan2End
        //                        },
        //                         new Contract()
        //                        {
        //                            Type = "HĐ XÁC ĐỊNH THỜI HẠN LẦN 3",
        //                            Code = thoihanlan3So,
        //                            Duration = thoihanlan3Count,
        //                            Start = thoihanlan3Start,
        //                            End = thoihanlan3End
        //                        },
        //                         new Contract()
        //                        {
        //                            Type = "HĐ KHÔNG XÁC ĐỊNH THỜI HẠN",
        //                            Code = khongthoihanSo,
        //                            Start = khongthoihanStart
        //                        }
        //                    },
        //                        Salaries = new List<Salary>()
        //                    {
        //                        new Salary()
        //                        {
        //                            Code = GetFormattedCellValue(row.GetCell(66))
        //                        }
        //                    },
        //                        BhxhEnable = bhxhEnable,
        //                        BhxhStart = bhxhStart,
        //                        BhxhYear = GetFormattedCellValue(row.GetCell(69)),
        //                        BhxhBookNo = GetFormattedCellValue(row.GetCell(70)),
        //                        BhxhCode = GetFormattedCellValue(row.GetCell(71)),
        //                        BhxhStatus = GetFormattedCellValue(row.GetCell(72)),
        //                        BhxhHospital = GetFormattedCellValue(row.GetCell(73)),
        //                        EmployeeEducations = new List<EmployeeEducation>()
        //                    {
        //                        new EmployeeEducation()
        //                        {
        //                            No = 1,
        //                            Content = GetFormattedCellValue(row.GetCell(76))
        //                        },
        //                        new EmployeeEducation()
        //                        {
        //                            No = 2,
        //                            Content = GetFormattedCellValue(row.GetCell(77))
        //                        },
        //                        new EmployeeEducation()
        //                        {
        //                            No = 3,
        //                            Content = GetFormattedCellValue(row.GetCell(78))
        //                        }
        //                        ,new EmployeeEducation()
        //                        {
        //                            No = 4,
        //                            Content = GetFormattedCellValue(row.GetCell(79))
        //                        }
        //                    },
        //                        EmployeeMovements = new List<EmployeeMovement>()
        //                    {
        //                        new EmployeeMovement
        //                        {
        //                            No = 1,
        //                            Content = GetFormattedCellValue(row.GetCell(80))
        //                        },
        //                         new EmployeeMovement
        //                        {
        //                            No = 2,
        //                            Content = GetFormattedCellValue(row.GetCell(81))
        //                        },
        //                          new EmployeeMovement
        //                        {
        //                            No = 3,
        //                            Content = GetFormattedCellValue(row.GetCell(82))
        //                        },
        //                           new EmployeeMovement
        //                        {
        //                            No = 4,
        //                            Content = GetFormattedCellValue(row.GetCell(83))
        //                        }
        //                    },
        //                        EmployeeAwards = new List<EmployeeAward>()
        //                    {
        //                        new EmployeeAward()
        //                        {
        //                            No = 1,
        //                            Content = GetFormattedCellValue(row.GetCell(84))
        //                        },
        //                        new EmployeeAward()
        //                        {
        //                            No = 2,
        //                            Content = GetFormattedCellValue(row.GetCell(85))
        //                        },
        //                        new EmployeeAward()
        //                        {
        //                            No = 3,
        //                            Content = GetFormattedCellValue(row.GetCell(86))
        //                        },
        //                        new EmployeeAward()
        //                        {
        //                            No = 4,
        //                            Content = GetFormattedCellValue(row.GetCell(87))
        //                        }

        //                    },
        //                        EmployeeDisciplines = new List<EmployeeDiscipline>()
        //                    {
        //                        new EmployeeDiscipline()
        //                        {
        //                            No = 1,
        //                            Content = GetFormattedCellValue(row.GetCell(88))
        //                        },
        //                        new EmployeeDiscipline()
        //                        {
        //                            No = 2,
        //                            Content = GetFormattedCellValue(row.GetCell(89))
        //                        },
        //                        new EmployeeDiscipline()
        //                        {
        //                            No = 3,
        //                            Content = GetFormattedCellValue(row.GetCell(90))
        //                        },
        //                        new EmployeeDiscipline()
        //                        {
        //                            No = 4,
        //                            Content = GetFormattedCellValue(row.GetCell(91))
        //                        }

        //                    },
        //                        EmployeePowers = new List<EmployeePower>()
        //                    {
        //                        new EmployeePower()
        //                        {
        //                            Year = 2015,
        //                            Value = GetFormattedCellValue(row.GetCell(92))
        //                        },
        //                        new EmployeePower()
        //                        {
        //                            Year = 2016,
        //                            Value = GetFormattedCellValue(row.GetCell(93))
        //                        },
        //                        new EmployeePower()
        //                        {
        //                            Year = 2017,
        //                            Value = GetFormattedCellValue(row.GetCell(94))
        //                        },
        //                        new EmployeePower()
        //                        {
        //                            Year = 2018,
        //                            Value = GetFormattedCellValue(row.GetCell(95))
        //                        }
        //                    },
        //                        EmployeeFamilys = new List<EmployeeFamily>()
        //                    {
        //                        new EmployeeFamily()
        //                        {
        //                            Relation = 2,
        //                            FullName = GetFormattedCellValue(row.GetCell(96)),
        //                            Birthday = birthdayVo,
        //                        },
        //                         new EmployeeFamily()
        //                        {
        //                            Relation = 3,
        //                            FullName = GetFormattedCellValue(row.GetCell(98)),
        //                            Birthday = birthdayCon1,
        //                        },
        //                         new EmployeeFamily()
        //                        {
        //                            Relation = 3,
        //                            FullName = GetFormattedCellValue(row.GetCell(100)),
        //                            Birthday = birthdayCon2,
        //                        },
        //                         new EmployeeFamily()
        //                        {
        //                            Relation = 3,
        //                            FullName = GetFormattedCellValue(row.GetCell(102)),
        //                            Birthday = birthdayCon3,
        //                        },
        //                         new EmployeeFamily()
        //                        {
        //                            Relation = 3,
        //                            FullName = GetFormattedCellValue(row.GetCell(104)),
        //                            Birthday = birthdayCon4,
        //                        },
        //                    },
        //                        EmployeeContactRelates = new List<EmployeeContactRelate>()
        //                    {
        //                        new EmployeeContactRelate()
        //                        {
        //                            No = 1,
        //                            Mobile = GetFormattedCellValue(row.GetCell(106))
        //                        },
        //                         new EmployeeContactRelate()
        //                        {
        //                            No = 2,
        //                            Mobile = GetFormattedCellValue(row.GetCell(107))
        //                        }
        //                    },
        //                        EmployeeDocuments = new List<EmployeeDocument>()
        //                    {
        //                        new EmployeeDocument()
        //                        {
        //                            Name = GetFormattedCellValue(row.GetCell(108))
        //                        }
        //                    },
        //                        EmployeeChecks = new List<EmployeeCheck>()
        //                    {
        //                        new EmployeeCheck()
        //                        {
        //                            No = 1,
        //                            EmployeeCode = GetFormattedCellValue(row.GetCell(109))
        //                        }
        //                    }
        //                    };

        //                    if (!string.IsNullOrEmpty(nhanvien.FullName))
        //                    {
        //                        dbContext.Employees.InsertOne(nhanvien);
        //                        #region Activities
        //                        if (enableActivity)
        //                        {
        //                            var activity = new TrackingUser
        //                            {
        //                                UserId = "",
        //                                Function = Constants.Collection.NhanViens,
        //                                Action = "create",
        //                                Value = nhanvien.Code,
        //                                Description = "create" + " " + Constants.Collection.NhanViens + " " + nhanvien.Code,
        //                                Created = DateTime.Now,
        //                                Link = "/nhan-vien/chi-tiet/" + nhanvien.Code
        //                            };
        //                            dbContext.TrackingUsers.InsertOne(activity);
        //                        }
        //                        #endregion
        //                    }
        //                }
        //                x++;
        //            }
        //        }
        //    }

        //    #region Insert Parts, Departments, Functions
        //    UpdateLeave();
        //    InitParts();
        //    InitDepartments();
        //    InitTitles();
        //    UpdatePwd();
        //    #endregion
        //    return Json(new { url = "/hr/nhan-su" });
        //    //return this.Content("true");
        //}

        [Route("/tai-lieu/nhan-vien/update/")]
        [HttpPost]
        public ActionResult NhanVienUpdate()
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
                        sheet = hssfwb.GetSheetAt(1);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet = hssfwb.GetSheetAt(1); //get first sheet from workbook   
                    }

                    for (int i = 5; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var systemDate = Constants.MinDate;
                        if (i < 180)
                        {
                            var fullName = GetFormattedCellValue(row.GetCell(5));
                            var employeeEntity = dbContext.Employees.Find(m => m.FullName.Equals(fullName)).FirstOrDefault();
                            if (employeeEntity != null)
                            {
                                var statusMarital = GetFormattedCellValue(row.GetCell(22));
                                var nation = GetFormattedCellValue(row.GetCell(23));
                                var religion = GetFormattedCellValue(row.GetCell(24));

                                #region Certificates
                                var certificates = new List<Certificate>();
                                var hocvan = GetFormattedCellValue(row.GetCell(25));
                                var description = GetFormattedCellValue(row.GetCell(26));
                                if (!string.IsNullOrEmpty(hocvan))
                                {
                                    certificates.Add(
                                                new Certificate()
                                                {
                                                    Type = hocvan,
                                                    Description = description
                                                });
                                }
                                #endregion

                                #region StorePaper
                                var storePapers = new List<StorePaper>()
                                {
                                    new StorePaper()
                                    {
                                        Type = "Bản tự khai ứng viên",
                                        Description = string.Empty,
                                        Count = (int)GetNumbericCellValue(row.GetCell(28)),
                                        Unit = string.Empty
                                    },
                                    new StorePaper()
                                    {
                                        Type = "Đơn ứng tuyển",
                                        Description = string.Empty,
                                        Count = (int)GetNumbericCellValue(row.GetCell(29)),
                                        Unit = string.Empty
                                    },
                                    new StorePaper()
                                    {
                                        Type = "Sơ yếu lý lịch",
                                        Description = string.Empty,
                                        Count = (int)GetNumbericCellValue(row.GetCell(30)),
                                        Unit = string.Empty
                                    },
                                    new StorePaper()
                                    {
                                        Type = "Giấy khai sinh",
                                        Description = string.Empty,
                                        Count = (int)GetNumbericCellValue(row.GetCell(31)),
                                        Unit = string.Empty
                                    },
                                    new StorePaper()
                                    {
                                        Type = "Chứng minh thư",
                                        Description = string.Empty,
                                        Count = (int)GetNumbericCellValue(row.GetCell(32)),
                                        Unit = string.Empty
                                    },
                                    new StorePaper()
                                    {
                                        Type = "Bằng/ chứng nhận tốt nghiệp",
                                        Description = string.Empty,
                                        Count = (int)GetNumbericCellValue(row.GetCell(33)),
                                        Unit = string.Empty
                                    },
                                    new StorePaper()
                                    {
                                        Type = "Bảng điểm hoặc học bạ",
                                        Description = string.Empty,
                                        Count = (int)GetNumbericCellValue(row.GetCell(34)),
                                        Unit = string.Empty
                                    },
                                    new StorePaper()
                                    {
                                        Type = "Hộ khẩu",
                                        Description = string.Empty,
                                        Count = (int)GetNumbericCellValue(row.GetCell(35)),
                                        Unit = string.Empty
                                    },
                                    new StorePaper()
                                    {
                                        Type = "Xác nhận nhân sự",
                                        Description = string.Empty,
                                        Count = (int)GetNumbericCellValue(row.GetCell(36)),
                                        Unit = string.Empty
                                    },
                                    new StorePaper()
                                    {
                                        Type = "Ảnh",
                                        Description = string.Empty,
                                        Count = (int)GetNumbericCellValue(row.GetCell(37)),
                                        Unit = string.Empty
                                    }
                                };
                                #endregion

                                #region Eduction
                                var employeeEducations = new List<EmployeeEducation>()
                                {
                                    new EmployeeEducation()
                                    {
                                        No = 1,
                                        Content = GetFormattedCellValue(row.GetCell(76))
                                    },
                                    new EmployeeEducation()
                                    {
                                        No = 2,
                                        Content = GetFormattedCellValue(row.GetCell(77))
                                    },
                                    new EmployeeEducation()
                                    {
                                        No = 3,
                                        Content = GetFormattedCellValue(row.GetCell(78))
                                    }
                                    ,new EmployeeEducation()
                                    {
                                        No = 4,
                                        Content = GetFormattedCellValue(row.GetCell(79))
                                    }
                                };
                                #endregion

                                var builder = Builders<Employee>.Filter;
                                var filter = Builders<Employee>.Filter.Eq(m => m.FullName, fullName);
                                var update = Builders<Employee>.Update
                                    .Set(m => m.StatusMarital, statusMarital)
                                    .Set(m => m.Nation, nation)
                                    .Set(m => m.Religion, religion)
                                    .Set(m => m.Certificates, certificates)
                                    .Set(m => m.StorePapers, storePapers)
                                    .Set(m => m.EmployeeEducations, employeeEducations);

                                dbContext.Employees.UpdateOne(filter, update);
                            }
                        }
                    }
                }
            }
            return Json(new { url = "/hr/nhan-su" });
        }

        [Route("/tai-lieu/nhan-vien/update-contract/")]
        [HttpPost]
        public ActionResult NhanVienUpdateContract()
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
                        sheet = hssfwb.GetSheetAt(1);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet = hssfwb.GetSheetAt(1); //get first sheet from workbook   
                    }

                    for (int i = 5; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var systemDate = Constants.MinDate;
                        if (i < 180)
                        {
                            var fullName = GetFormattedCellValue(row.GetCell(5));
                            var employeeEntity = dbContext.Employees.Find(m => m.FullName.Equals(fullName)).FirstOrDefault();
                            if (employeeEntity != null)
                            {
                                #region Declare
                                DateTime? probationStart = GetDateCellValue2(row.GetCell(39));
                                DateTime? probationEnd = GetDateCellValue2(row.GetCell(40));
                                DateTime? thoivuStart = GetDateCellValue2(row.GetCell(42));
                                DateTime? thoivuEnd = GetDateCellValue2(row.GetCell(43));

                                var thoihanlan1So = GetFormattedCellValue(row.GetCell(44)).Split(" ")[0];
                                var thoihanlan1Count = 0;
                                try
                                {
                                    thoihanlan1Count = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(45))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(45)).Replace(" năm", ""));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(LoggingEvents.FormatType, "Get data cell 45 at row {} : " + ex, row);
                                }

                                DateTime? thoihanlan1Start = GetDateCellValue2(row.GetCell(46));
                                DateTime? thoihanlan1End = GetDateCellValue2(row.GetCell(47));

                                var giahanlan1So = GetFormattedCellValue(row.GetCell(48));
                                var giahanlan1Count = 0;
                                try
                                {
                                    giahanlan1Count = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(49))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(49)).Replace(" năm", ""));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(LoggingEvents.FormatType, "Get data cell 49 at row {} : " + ex, row);
                                }

                                DateTime? giahanlan1Start = GetDateCellValue2(row.GetCell(50));
                                DateTime? giahanlan1End = GetDateCellValue2(row.GetCell(51));

                                var thoihanlan2So = GetFormattedCellValue(row.GetCell(52));
                                var thoihanlan2Count = 0;
                                try
                                {
                                    thoihanlan2Count = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(53))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(53)).Replace(" năm", ""));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(LoggingEvents.FormatType, "Get data cell 53 at row {} : " + ex, row);
                                }

                                DateTime? thoihanlan2Start = GetDateCellValue2(row.GetCell(54));
                                DateTime? thoihanlan2End = GetDateCellValue2(row.GetCell(55));

                                var giahanlan2So = GetFormattedCellValue(row.GetCell(56));
                                var giahanlan2Count = 0;
                                try
                                {
                                    giahanlan2Count = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(57))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(57)).Replace(" năm", ""));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(LoggingEvents.FormatType, "Get data cell 57 at row {} : " + ex, row);
                                }

                                DateTime? giahanlan2Start = GetDateCellValue2(row.GetCell(58));
                                DateTime? giahanlan2End = GetDateCellValue2(row.GetCell(59));

                                var thoihanlan3So = GetFormattedCellValue(row.GetCell(60));
                                var thoihanlan3Count = 0;
                                try
                                {
                                    thoihanlan3Count = String.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(61))) ? 0 : Convert.ToInt32(GetFormattedCellValue(row.GetCell(61)).Replace(" năm", ""));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(LoggingEvents.FormatType, "Get data cell 61 at row {} : " + ex, row);
                                }

                                DateTime? thoihanlan3Start = GetDateCellValue2(row.GetCell(62));
                                DateTime? thoihanlan3End = GetDateCellValue2(row.GetCell(63));

                                var code = GetFormattedCellValue(row.GetCell(64)).Replace("'", "");
                                var description = GetFormattedCellValue(row.GetCell(65));
                                var phulucdieuchinhluong = GetFormattedCellValue(row.GetCell(66));
                                DateTime? khongthoihanStart = null;
                                #endregion

                                #region Contracts
                                var contracts = new List<Contract>();

                                var soHdThuViec = GetFormattedCellValue(row.GetCell(38));
                                if (!string.IsNullOrEmpty(soHdThuViec))
                                {
                                    contracts.Add(
                                                new Contract()
                                                {
                                                    Type = "5b3c7221d44df83d946dbdd8",
                                                    TypeName = "THỬ VIỆC",
                                                    Code = soHdThuViec,
                                                    Start = probationStart,
                                                    End = probationEnd
                                                });
                                }
                                var soHdThoiVu = GetFormattedCellValue(row.GetCell(41));
                                if (!string.IsNullOrEmpty(soHdThoiVu))
                                {
                                    contracts.Add(
                                                new Contract()
                                                {
                                                    Type = "5b3c7221d44df83d946dbdd9",
                                                    TypeName = "THỜI VỤ",
                                                    Code = soHdThoiVu,
                                                    Start = thoivuStart,
                                                    End = thoivuEnd
                                                });
                                }

                                if (!string.IsNullOrEmpty(thoihanlan1So))
                                {
                                    contracts.Add(
                                                new Contract()
                                                {
                                                    Type = "5b3c7221d44df83d946dbdda",
                                                    TypeName = "HĐ XÁC ĐỊNH THỜI HẠN LẦN 1",
                                                    Code = thoihanlan1So,
                                                    Duration = thoihanlan1Count,
                                                    Start = thoihanlan1Start,
                                                    End = thoihanlan1End
                                                });
                                }
                                if (!string.IsNullOrEmpty(giahanlan1So))
                                {
                                    contracts.Add(
                                               new Contract()
                                               {
                                                   Type = "5b3c7221d44df83d946dbddb",
                                                   TypeName = "PHỤ LỤC GIA HẠN HĐ LẦN 1",
                                                   Code = giahanlan1So,
                                                   Duration = giahanlan1Count,
                                                   Start = giahanlan1Start,
                                                   End = giahanlan1End
                                               });
                                }
                                if (!string.IsNullOrEmpty(thoihanlan2So))
                                {
                                    contracts.Add(
                                               new Contract()
                                               {
                                                   Type = "5b3c7221d44df83d946dbddc",
                                                   TypeName = "HĐ XÁC ĐỊNH THỜI HẠN LẦN 2",
                                                   Code = thoihanlan2So,
                                                   Duration = thoihanlan2Count,
                                                   Start = thoihanlan2Start,
                                                   End = thoihanlan2End
                                               });
                                }
                                if (!string.IsNullOrEmpty(giahanlan2So))
                                {
                                    contracts.Add(
                                               new Contract()
                                               {
                                                   Type = "5b3c7221d44df83d946dbddd",
                                                   TypeName = "PHỤ LỤC GIA HẠN HĐ LẦN 2",
                                                   Code = giahanlan2So,
                                                   Duration = giahanlan2Count,
                                                   Start = giahanlan2Start,
                                                   End = giahanlan2End
                                               });
                                }
                                if (!string.IsNullOrEmpty(thoihanlan3So))
                                {
                                    contracts.Add(
                                                new Contract()
                                                {
                                                    Type = "5b3c7221d44df83d946dbdde",
                                                    TypeName = "HĐ XÁC ĐỊNH THỜI HẠN LẦN 3",
                                                    Code = thoihanlan3So,
                                                    Duration = thoihanlan3Count,
                                                    Start = thoihanlan3Start,
                                                    End = thoihanlan3End
                                                });
                                }
                                if (!string.IsNullOrEmpty(code) && code != "0")
                                {
                                    contracts.Add(
                                                new Contract()
                                                {
                                                    Type = "5b3c7221d44df83d946dbddf",
                                                    TypeName = "HĐ KHÔNG XÁC ĐỊNH THỜI HẠN",
                                                    Code = code,
                                                    Start = khongthoihanStart,
                                                    Description = description,
                                                    PhuLucDieuChinhLuong = phulucdieuchinhluong
                                                });
                                }
                                #endregion

                                var builder = Builders<Employee>.Filter;
                                var filter = Builders<Employee>.Filter.Eq(m => m.FullName, fullName);
                                var update = Builders<Employee>.Update
                                    .Set(m => m.Contracts, contracts);

                                dbContext.Employees.UpdateOne(filter, update);
                            }

                            #region Comment
                            // Update Hop dong khong xac dinh thoi gian
                            //if (!string.IsNullOrEmpty(phulucdieuchinhluong))
                            //{
                            //    var fullName = GetFormattedCellValue(row.GetCell(5));
                            //    var employeeEntity = dbContext.Employees.Find(m => m.FullName.Equals(fullName)).FirstOrDefault();
                            //    if (employeeEntity != null)
                            //    {
                            //        if (employeeEntity.Contracts != null && employeeEntity.Contracts.Count > 0)
                            //        {
                            //            foreach (var contract in employeeEntity.Contracts)
                            //            {
                            //                if (contract.Type == "HĐ KHÔNG XÁC ĐỊNH THỜI HẠN")
                            //                {
                            //                    var builder = Builders<Employee>.Filter;
                            //                    var filter = Builders<Employee>.Filter.Eq(m => m.FullName, fullName);
                            //                    filter = filter & builder.ElemMatch("Contracts", Builders<Contract>.Filter.Eq("Type", "HĐ KHÔNG XÁC ĐỊNH THỜI HẠN"));
                            //                    var update = Builders<Employee>.Update
                            //                        .Set("Contracts.$.Code", code)
                            //                        .Set("Contracts.$.Description", description)
                            //                        .Set("Contracts.$.PhuLucDieuChinhLuong", phulucdieuchinhluong);
                            //                    dbContext.Employees.UpdateOne(filter, update);
                            //                }
                            //            }
                            //        }
                            //        else
                            //        {
                            //            var contracts = new List<Contract>();
                            //            if (employeeEntity.Contracts != null && employeeEntity.Contracts.Count > 0)
                            //            {
                            //                contracts = employeeEntity.Contracts.ToList();
                            //            }
                            //            var contract = new Contract
                            //            {
                            //                Type = "HĐ KHÔNG XÁC ĐỊNH THỜI HẠN",
                            //                Code = code,
                            //                Start = khongthoihanStart ?? null,
                            //                PhuLucDieuChinhLuong = phulucdieuchinhluong,
                            //                Description = description
                            //            };
                            //            contracts.Add(contract);

                            //            var builder = Builders<Employee>.Filter;
                            //            var filter = Builders<Employee>.Filter.Eq(m => m.FullName, fullName);
                            //            var update = Builders<Employee>.Update
                            //                .Set(m=>m.Contracts, contracts);
                            //            dbContext.Employees.UpdateOne(filter, update);
                            //        }
                            //    }
                            //}
                            #endregion
                        }
                    }
                }
            }
            return Json(new { url = "/hr/nhan-su" });
        }

        public void UpdateLeave()
        {
            var list = dbContext.Employees.Find(m => m.Function.Equals("Nghỉ việc")).ToList();
            foreach (var item in list)
            {
                var filter = Builders<Employee>.Filter.Eq(m => m.Id, item.Id);
                var update = Builders<Employee>.Update
                    //.Set(m => m.Leave, true)
                    .Set(m => m.Enable, false)
                    .Set(m => m.UpdatedOn, DateTime.Now)
                    .Set(m => m.UpdatedBy, Constants.System.account);
                dbContext.Employees.UpdateOne(filter, update);
            }
        }

        public void UpdatePwd()
        {
            var filter = Builders<Employee>.Filter.Eq(m => m.Email, "phuong.ndq@tribat.vn");
            var update = Builders<Employee>.Update
                .Set(m => m.Password, "oc/Ln+7CbJGe3GUccIN95+Fx1wBUHKEr2its652Rwbs=");
            dbContext.Employees.UpdateOne(filter, update);

            filter = Builders<Employee>.Filter.Eq(m => m.Email, "anh.ndq@tribat.vn");
            update = Builders<Employee>.Update
                .Set(m => m.Password, "FoYtepW5yOsrfdTxuzlBUMjRkbToANyhJPEIEDZ/lsw=");
            dbContext.Employees.UpdateOne(filter, update);

            filter = Builders<Employee>.Filter.Eq(m => m.Email, "thanh.dnt@tribat.vn");
            update = Builders<Employee>.Update
                .Set(m => m.Password, "DY5d9djFim2AwagyXCak/GHprYvNcnTaVlWnCj2ljOk=");
            dbContext.Employees.UpdateOne(filter, update);

            filter = Builders<Employee>.Filter.Eq(m => m.Email, "thoa.ctm@tribat.vn");
            update = Builders<Employee>.Update
                .Set(m => m.Password, "hOBU+oQGIY3LzYCRGVVLu5hCtP/K5DR0ydfe+x7zUYE=");
            dbContext.Employees.UpdateOne(filter, update);
        }

        public void InitParts()
        {
            dbContext.Parts.DeleteMany(new BsonDocument());
            var list = dbContext.Employees.Distinct<string>("Part", "{}").ToList();
            var i = 1;
            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    dbContext.Parts.InsertOne(new Part()
                    {
                        Name = item,
                        Alias = Utility.AliasConvert(item),
                        Order = i
                    });
                    i++;
                }
            }
            //_cache.SetString(Constants.Collection.Parts, JsonConvert.SerializeObject(dbContext.Parts.Find(m => true).ToList()));
        }

        public void InitDepartments()
        {
            dbContext.Departments.DeleteMany(new BsonDocument());
            var list = dbContext.Employees.Distinct<string>("Department", "{}").ToList();
            var i = 1;
            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    dbContext.Departments.InsertOne(new Department()
                    {
                        Name = item,
                        Alias = Utility.AliasConvert(item),
                        Order = i
                    });
                }
            }
        }

        public void InitTitles()
        {
            dbContext.Titles.DeleteMany(new BsonDocument());
            var list = dbContext.Employees.Distinct<string>("Title", "{}").ToList();
            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    dbContext.Titles.InsertOne(new Title()
                    {
                        Name = item
                    });
                }
            }
            //_cache.SetString(Constants.Collection.Parts, JsonConvert.SerializeObject(dbContext.Parts.Find(m => true).ToList()));
        }

        public void SendMailRegister(Employee entity, string pwd)
        {
            var tos = new List<EmailAddress>
            {
                new EmailAddress { Name = entity.FullName, Address = entity.Email }
            };

            // Send an email with this link
            //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            //var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
            //Email from Email Template
            var callbackUrl = "/";
            string Message = "Đăng nhập TRIBAT - ERP <a href=\"" + callbackUrl + "\">here</a>";
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
        #endregion

        #region Factory
        [Route("/tai-lieu/nha-may/thong-ke/")]
        public IActionResult FactoryReport()
        {
            return View();
        }

        [Route("/tai-lieu/nha-may/thong-ke/bao-cao-tk")]
        [HttpPost]
        public ActionResult FactoryReportImport()
        {
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
                    ISheet sheet1;
                    ISheet sheet2;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet0 = hssfwb.GetSheetAt(0); //get first sheet from workbook  
                        sheet1 = hssfwb.GetSheetAt(1);
                        sheet2 = hssfwb.GetSheetAt(2);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet0 = hssfwb.GetSheetAt(0); //get first sheet from workbook   
                        sheet1 = hssfwb.GetSheetAt(1);
                        sheet2 = hssfwb.GetSheetAt(2);
                    }
                    #region Read & Insert Data

                    #region Sheet 0 Ma so
                    dbContext.FactoryProducts.DeleteMany(m => true);
                    dbContext.FactoryShifts.DeleteMany(m => true);
                    dbContext.FactoryStages.DeleteMany(m => true);
                    dbContext.FactoryTruckTypes.DeleteMany(m => true);
                    dbContext.FactoryMotorVehicles.DeleteMany(m => true);
                    dbContext.FactoryWorks.DeleteMany(m => true);
                    headerCal = 2;
                    for (int i = headerCal; i <= sheet0.LastRowNum; i++)
                    {
                        IRow row = sheet0.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var ca = GetFormattedCellValue(row.GetCell(0));
                        var mangcongviec = GetFormattedCellValue(row.GetCell(1));
                        var congdoan = GetFormattedCellValue(row.GetCell(2));
                        var xe = GetFormattedCellValue(row.GetCell(3));
                        var loaixe = GetFormattedCellValue(row.GetCell(4));
                        var nvl = GetFormattedCellValue(row.GetCell(5));
                        

                        if (!string.IsNullOrEmpty(ca))
                        {
                            dbContext.FactoryShifts.InsertOne(new FactoryShift
                            {
                                Name = ca,
                                Alias = Utility.AliasConvert(ca)
                            });
                        }

                        if (!string.IsNullOrEmpty(mangcongviec))
                        {
                            dbContext.FactoryWorks.InsertOne(new FactoryWork
                            {
                                Name = mangcongviec,
                                Alias = Utility.AliasConvert(mangcongviec)
                            });
                        }

                        if (!string.IsNullOrEmpty(congdoan))
                        {
                            dbContext.FactoryStages.InsertOne(new FactoryStage
                            {
                                Name = congdoan,
                                Alias = Utility.AliasConvert(congdoan)
                            });
                        }

                        if (!string.IsNullOrEmpty(xe))
                        {
                            dbContext.FactoryMotorVehicles.InsertOne(new FactoryMotorVehicle
                            {
                                Name = xe,
                                Alias = Utility.AliasConvert(xe),
                                Type = loaixe,
                                TypeAlias = Utility.AliasConvert(loaixe)
                            });
                        }

                        if (!string.IsNullOrEmpty(nvl))
                        {
                            dbContext.FactoryProducts.InsertOne(new FactoryProduct
                            {
                                Name = nvl,
                                Alias = Utility.AliasConvert(nvl)
                            });
                        }
                    }
                    #endregion

                    #region Sheet 1 DATA ton SX
                    dbContext.FactoryTonSXs.DeleteMany(m => true);
                    headerCal = 2;
                    for (int i = headerCal; i <= sheet1.LastRowNum; i++)
                    {
                        IRow row = sheet1.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var date = GetDateCellValue(row.GetCell(1));
                        var product = GetFormattedCellValue(row.GetCell(2));
                        var unit = GetFormattedCellValue(row.GetCell(3));
                        var productAlias = Utility.AliasConvert(product);
                        var productId = string.Empty;
                        var productEntity = dbContext.FactoryProducts.Find(m => m.Alias.Equals(productAlias)).FirstOrDefault();
                        if (productEntity != null)
                        {
                            productId = productEntity.Id;
                        }
                        else
                        {
                            var newProduct = new FactoryProduct
                            {
                                Name = product,
                                Alias = productAlias,
                                Unit = unit
                            };
                            dbContext.FactoryProducts.InsertOne(newProduct);
                            productId = newProduct.Id;
                        }
                        
                        var lot = GetFormattedCellValue(row.GetCell(4));
                        decimal tondaungay = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(5))))
                        {
                            tondaungay = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(5)));
                        }
                        decimal nhaptusanxuat = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(6))))
                        {
                            nhaptusanxuat = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(6)));
                        }
                        decimal nhaptukho = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(7))))
                        {
                            nhaptukho = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(7)));
                        }
                        decimal xuatchokho = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(8))))
                        {
                            xuatchokho = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(8)));
                        }
                        decimal xuatchosanxuat = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(9))))
                        {
                            xuatchosanxuat = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(9)));
                        }
                        decimal xuathaohut = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(10))))
                        {
                            xuathaohut = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(10)));
                        }
                        decimal toncuoingay = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(11))))
                        {
                            toncuoingay = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(11)));
                        }

                        dbContext.FactoryTonSXs.InsertOne(new FactoryTonSX
                        {
                            Year = date.Year,
                            Month = date.Month,
                            Week = Utility.GetIso8601WeekOfYear(date),
                            Day = date.Day,
                            Date = date,
                            ProductId = productId,
                            Product = product,
                            ProductAlias = Utility.AliasConvert(product),
                            Unit = unit,
                            LOT = lot,
                            TonDauNgay = tondaungay,
                            NhapTuSanXuat = nhaptusanxuat,
                            XuatChoSanXuat = xuatchosanxuat,
                            NhapTuKho = nhaptukho,
                            XuatChoKho = xuatchokho,
                            XuatHaoHut = xuathaohut,
                            TonCuoiNgay = toncuoingay
                        });

                        // Update Quantity Product
                        var builderUpdateQuantityProduct = Builders<FactoryProduct>.Filter;
                        var filterUpdateQuantityProduct = builderUpdateQuantityProduct.Eq(m => m.Id, productId);
                        var updateQuantityProduct = Builders<FactoryProduct>.Update
                            .Set(m => m.Quantity, toncuoingay);
                        dbContext.FactoryProducts.UpdateOne(filterUpdateQuantityProduct, updateQuantityProduct);
                    }

                    // Update Unit Product & create Unit collections
                    var tonsxs = dbContext.FactoryTonSXs.Find(m => true).ToList();
                    var groups = (from p in tonsxs
                                  group p by new
                                  {
                                      p.ProductAlias,
                                      p.Unit
                                  }
                              into d
                                  select new
                                  {
                                      Product = d.Key.ProductAlias,
                                      d.Key.Unit
                                  }).ToList();
                    foreach (var group in groups)
                    {
                        var builderUpdateProduct = Builders<FactoryProduct>.Filter;
                        var filterUpdateProduct = builderUpdateProduct.Eq(m => m.Alias, group.Product);
                        var updateProduct = Builders<FactoryProduct>.Update
                            .Set(m => m.Unit, group.Unit);
                        dbContext.FactoryProducts.UpdateOne(filterUpdateProduct, updateProduct);

                        var aliasUnit = Utility.AliasConvert(group.Unit);
                        if (dbContext.Units.CountDocuments(m =>m.Type.Equals(Constants.UnitType.Factory) & m.Alias.Equals(aliasUnit)) == 0)
                        {
                            dbContext.Units.InsertOne(new Unit
                            {
                                Type = Constants.UnitType.Factory,
                                Name = group.Unit,
                                Alias = aliasUnit
                            });
                        }
                    }

                    #endregion

                    #region Sheet 2 DATA Van Hanh
                    dbContext.FactoryVanHanhs.DeleteMany(m => true);
                    headerCal = 2;
                    for (int i = headerCal; i <= sheet2.LastRowNum; i++)
                    {
                        IRow row = sheet2.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var date = GetDateCellValue(row.GetCell(1));
                        var ca = GetFormattedCellValue(row.GetCell(2));
                        var mangcongviec = GetFormattedCellValue(row.GetCell(3));
                        var congdoan = GetFormattedCellValue(row.GetCell(4));
                        if (string.IsNullOrEmpty(mangcongviec) && string.IsNullOrEmpty(congdoan)) continue;
                        var lot = GetFormattedCellValue(row.GetCell(5));
                        var xecogioiMay = GetFormattedCellValue(row.GetCell(6));
                        var xecogioiMayAlias = Utility.AliasConvert(GetFormattedCellValue(row.GetCell(6)));
                        var product = GetFormattedCellValue(row.GetCell(7));
                        var productAlias = Utility.AliasConvert(product);
                        var productId = string.Empty;
                        var productEntity = dbContext.FactoryProducts.Find(m => m.Alias.Equals(productAlias)).FirstOrDefault();
                        if (productEntity != null)
                        {
                            productId = productEntity.Id;
                        }
                        else
                        {
                            var newProduct = new FactoryProduct
                            {
                                Name = product,
                                Alias = productAlias
                            };
                            dbContext.FactoryProducts.InsertOne(newProduct);
                            productId = newProduct.Id;
                        }

                        int slNhanCong = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(8))))
                        {
                            slNhanCong = Convert.ToInt32(GetFormattedCellValue(row.GetCell(8)));
                        }
                        var calamviec = GetFormattedCellValue(row.GetCell(9));

                        var thoigianbatdau = DateTime.FromOADate(row.GetCell(10).NumericCellValue).TimeOfDay;
                        var thoigianketthuc = DateTime.FromOADate(row.GetCell(11).NumericCellValue).TimeOfDay;
                        var thoigianbttq = DateTime.FromOADate(row.GetCell(12).NumericCellValue).TimeOfDay;
                        var thoigianxehu = DateTime.FromOADate(row.GetCell(13).NumericCellValue).TimeOfDay;
                        var thoigiannghi = DateTime.FromOADate(row.GetCell(14).NumericCellValue).TimeOfDay;
                        var thoigiancongvieckhac = DateTime.FromOADate(row.GetCell(15).NumericCellValue).TimeOfDay;
                        var thoigiandaymobac = DateTime.FromOADate(row.GetCell(16).NumericCellValue).TimeOfDay;
                        var thoigianbochang = DateTime.FromOADate(row.GetCell(17).NumericCellValue).TimeOfDay;
                        var thoigiankhautru = thoigianbttq.Add(thoigianxehu).Add(thoigiannghi).Add(thoigiancongvieckhac).Add(thoigiandaymobac).Add(thoigianbochang);
                        var thoigianlamviec = thoigianketthuc.Subtract(thoigianbatdau).Subtract(thoigiankhautru);
                        decimal soluongthuchien = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(19))))
                        {
                            try
                            {
                                soluongthuchien = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(19)));
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                        decimal soluongdonggoi = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(20))))
                        {
                            soluongdonggoi = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(20)));
                        }
                        decimal soluongbochang = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(21))))
                        {
                            soluongbochang = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(21)));
                        }
                        decimal dau = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(22))))
                        {
                            dau = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(22)));
                        }
                        decimal nhot10 = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(23))))
                        {
                            nhot10 = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(23)));
                        }
                        decimal nhot50 = 0;
                        if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(24))))
                        {
                            try
                            {
                                nhot50 = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(24)));
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                        var nhot90 = GetNumbericCellValue(row.GetCell(25));
                        var nhot140 = GetNumbericCellValue(row.GetCell(26));
                        var nguyennhan = GetFormattedCellValue(row.GetCell(27));
                        var tongthoigianbochang = thoigianbochang.TotalSeconds * slNhanCong;
                        var tongthoigiandonggoi = thoigianlamviec.TotalSeconds * slNhanCong;
                        var tongthoigiancongvieckhac = thoigiancongvieckhac.TotalSeconds * slNhanCong;
                        var tongthoigiandaymobac = thoigiandaymobac.TotalSeconds * slNhanCong;

                        var phieuInCa = Utility.NoPhieuInCa(date, xecogioiMayAlias);

                        dbContext.FactoryVanHanhs.InsertOne(new FactoryVanHanh
                        {
                            Year = date.Year,
                            Month = date.Month,
                            Week = Utility.GetIso8601WeekOfYear(date),
                            Day = date.Day,
                            Date = date,
                            Ca = ca,
                            CaAlias = Utility.AliasConvert(ca),
                            MangCongViec = mangcongviec,
                            MangCongViecAlias = Utility.AliasConvert(mangcongviec),
                            CongDoan = congdoan,
                            CongDoanAlias = Utility.AliasConvert(congdoan),
                            LOT = lot,
                            XeCoGioiMay = xecogioiMay,
                            XeCoGioiMayAlias = xecogioiMayAlias,
                            ProductId = productId,
                            NVLTP = product,
                            NVLTPAlias = productAlias,
                            SLNhanCong = slNhanCong,
                            Start = thoigianbatdau,
                            End = thoigianketthuc,
                            ThoiGianBTTQ = thoigianbttq,
                            ThoiGianXeHu = thoigianxehu,
                            ThoiGianNghi = thoigiannghi,
                            ThoiGianCVKhac = thoigiancongvieckhac,
                            ThoiGianDayMoBat = thoigiandaymobac,
                            ThoiGianBocHang = thoigianbochang,
                            ThoiGianLamViec = thoigianlamviec,
                            SoLuongThucHien = soluongthuchien,
                            SoLuongDongGoi = soluongdonggoi,
                            SoLuongBocHang = soluongbochang,
                            Dau = dau,
                            Nhot10 = nhot10,
                            Nhot50 = nhot50,
                            NguyenNhan = nguyennhan,
                            TongThoiGianBocHang = tongthoigianbochang,
                            TongThoiGianDongGoi = tongthoigiandonggoi,
                            TongThoiGianCVKhac = tongthoigiancongvieckhac,
                            TongThoiGianDayMoBat = tongthoigiandaymobac,
                            PhieuInCa = phieuInCa
                        });
                    }
                    #endregion

                    #region Sheet 3 BC ton SX
                    // GET DIRECT FROM FactoryTonSX
                    //dbContext.FactoryReportTonSXs.DeleteMany(m => true);
                    //var groupReportTonSxs = (from p in tonsxs
                    //                         group p by new
                    //                         {
                    //                             p.Date,
                    //                             p.Product
                    //                         }
                    //          into d
                    //                         select new
                    //                         {
                    //                             Date = d.Key.Date,
                    //                             d.Key.Product
                    //                         }).ToList();
                    //foreach (var group in groupReportTonSxs)
                    //{

                    //}

                    #endregion

                    #region Sheet 4 BC XCG

                    #endregion

                    #region Sheet 5 BC DG

                    #endregion

                    #region Sheet 6 BC Boc Hang

                    #endregion

                    #region Sheet 6 BC Van Hanh May

                    #endregion

                    #endregion
                }
            }
            return Json(new { url = "/"+ Constants.LinkFactory.Main + "/" + Constants.LinkFactory.TonSx });
        }

        [Route("/tai-lieu/nha-may/thong-ke/tuan")]
        [HttpPost]
        public ActionResult FactoryWeekImport()
        {
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
                    ISheet sheet2;
                    ISheet sheet3;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet0 = hssfwb.GetSheetAt(0);
                        sheet2 = hssfwb.GetSheetAt(2);
                        sheet3 = hssfwb.GetSheetAt(3);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet0 = hssfwb.GetSheetAt(0);
                        sheet2 = hssfwb.GetSheetAt(2);
                        sheet3 = hssfwb.GetSheetAt(3);
                    }

                    #region Read & Insert Data

                    #region Sheet 2 Dinh Muc
                    dbContext.FactoryDinhMucs.DeleteMany(m => true);
                    headerCal = 3;
                    for (int i = headerCal; i <= sheet2.LastRowNum; i++)
                    {
                        IRow row = sheet2.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var congdoan = GetFormattedCellValue(row.GetCell(0));

                        var diemCongDoanVanHanh = new DinhMucDiemCongDoanVanHanh
                        {
                            XeCuoc = (decimal)GetNumbericCellValue(row.GetCell(1)),
                            XeBen = (decimal)GetNumbericCellValue(row.GetCell(2)),
                            XeUi = (decimal)GetNumbericCellValue(row.GetCell(3)),
                            XeXuc = (decimal)GetNumbericCellValue(row.GetCell(4))
                        };
                        var diemNangSuan1h = new DinhMucDiemNangSuan1h
                        {
                            XeCuoc = (decimal)GetNumbericCellValue(row.GetCell(5)),
                            XeBen = (decimal)GetNumbericCellValue(row.GetCell(6)),
                            XeUi = (decimal)GetNumbericCellValue(row.GetCell(7)),
                            XeXuc = (decimal)GetNumbericCellValue(row.GetCell(8))
                        };
                        var dinhMucChiPhi = new DinhMucChiPhi
                        {
                            XeCuoc07 = (decimal)GetNumbericCellValue(row.GetCell(9)),
                            XeCuoc05 = (decimal)GetNumbericCellValue(row.GetCell(10)),
                            XeCuoc03 = (decimal)GetNumbericCellValue(row.GetCell(11)),
                            XeBen = (decimal)GetNumbericCellValue(row.GetCell(12)),
                            XeUi = (decimal)GetNumbericCellValue(row.GetCell(13)),
                            XeXuc = (decimal)GetNumbericCellValue(row.GetCell(14))
                        };
                        dbContext.FactoryDinhMucs.InsertOne(new FactoryDinhMuc
                        {
                            Year = date.Year,
                            Month = date.Month,
                            Week = Utility.GetIso8601WeekOfYear(date),
                            Day = date.Day,
                            CongDoan = congdoan,
                            Alias = Utility.AliasConvert(congdoan),
                            DiemCongDoanVanHanh = diemCongDoanVanHanh,
                            DiemNangSuan1h = diemNangSuan1h,
                            DinhMucChiPhi = dinhMucChiPhi
                        });
                    }
                    #endregion

                    #region Sheet 0 Danh Gia XCG
                    dbContext.FactoryDanhGiaXCGs.DeleteMany(m => true);
                    headerCal = 4;
                    var chungloaixe = string.Empty;
                    for (int i = headerCal; i <= sheet0.LastRowNum; i++)
                    {
                        IRow row = sheet0.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var tempchungloaixe = GetFormattedCellValue(row.GetCell(0));
                        if (!string.IsNullOrEmpty(tempchungloaixe)){
                            chungloaixe = tempchungloaixe;
                        }
                        var congviec = GetFormattedCellValue(row.GetCell(1));
                        if (!string.IsNullOrEmpty(congviec))
                        {
                            var thoigianlamviec = DateTime.FromOADate(row.GetCell(2).NumericCellValue).TimeOfDay;
                            decimal chuyen = (decimal)GetNumbericCellValue(row.GetCell(3));
                            decimal oil = (decimal)GetNumbericCellValue(row.GetCell(4));
                           
                            var congdoanvanhanh = new DanhGiaXCGCongDoanVanHanh
                            {
                                TrongSoDanhGia = (decimal)GetNumbericCellValue(row.GetCell(5)),
                                DiemCongDoan = (decimal)GetNumbericCellValue(row.GetCell(6)),
                                TiTrongCongDoan = (decimal)GetNumbericCellValue(row.GetCell(7)),
                                DiemDanhGiaCongDoan = (decimal)GetNumbericCellValue(row.GetCell(8))
                            };

                            var nangsuat = new DanhGiaXCGNangSuat
                            {
                                TrongSoDanhGia = (decimal)GetNumbericCellValue(row.GetCell(9)),
                                TieuChuanCongDoan = (decimal)GetNumbericCellValue(row.GetCell(10)),
                                ThucTe = (decimal)GetNumbericCellValue(row.GetCell(11)),
                                DiemDanhGiaCongDoan = (decimal)GetNumbericCellValue(row.GetCell(12))
                            };

                            var tieuHaoDau = new DanhGiaXCGTieuHaoDau
                            {
                                TrongSoDanhGia = (decimal)GetNumbericCellValue(row.GetCell(13)),
                                TieuChuanCongDoan = (decimal)GetNumbericCellValue(row.GetCell(14)),
                                ThucTe = (decimal)GetNumbericCellValue(row.GetCell(15)),
                                DiemDanhGiaCongDoan = (decimal)GetNumbericCellValue(row.GetCell(16))
                            };

                            var vanHanh = new DanhGiaXCGVanHanh
                            {
                                DiemVanHanhXCG = (decimal)GetNumbericCellValue(row.GetCell(17)),
                                TrongSo = (decimal)GetNumbericCellValue(row.GetCell(18))
                            };

                            var chiPhi = new DanhGiaXCGChiPhi
                            {
                                TieuChuan = (decimal)GetNumbericCellValue(row.GetCell(19)),
                                ThucTe = (decimal)GetNumbericCellValue(row.GetCell(20)),
                                DiemChiPhiXCG = (decimal)GetNumbericCellValue(row.GetCell(21)),
                                TrongSo = (decimal)GetNumbericCellValue(row.GetCell(22))
                            };

                            decimal danhGiaTongThe = (decimal)GetNumbericCellValue(row.GetCell(23));
                            var xepHangXCG = GetFormattedCellValue(row.GetCell(24));

                            dbContext.FactoryDanhGiaXCGs.InsertOne(new FactoryDanhGiaXCG
                            {
                                Year = date.Year,
                                Month = date.Month,
                                Week = Utility.GetIso8601WeekOfYear(date),
                                Day = date.Day,
                                ChungLoaiXe = chungloaixe,
                                CongViec = congviec,
                                ThoiGianLamViec = thoigianlamviec,
                                Chuyen = chuyen,
                                Oil = oil,
                                CongDoanVanHanh = congdoanvanhanh,
                                NangSuat = nangsuat,
                                TieuHaoDau = tieuHaoDau,
                                VanHanh = vanHanh,
                                ChiPhi = chiPhi,
                                DanhGiaTongThe = danhGiaTongThe,
                                XepHangXCG = xepHangXCG
                            });
                        }
                    }
                    #endregion

                    #region Sheet 3 Chi Phi XCG
                    dbContext.FactoryChiPhiXCGs.DeleteMany(m => true);
                    headerCal = 2;
                    for (int i = headerCal; i <= sheet3.LastRowNum; i++)
                    {
                        IRow row = sheet3.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var chungLoaiXe = GetFormattedCellValue(row.GetCell(0));
                        if (!string.IsNullOrEmpty(chungloaixe))
                        {
                            double chiPhiThang = GetNumbericCellValue(row.GetCell(1));
                            dbContext.FactoryChiPhiXCGs.InsertOne(new FactoryChiPhiXCG
                            {
                                Year = date.Year,
                                Month = date.Month,
                                Week = Utility.GetIso8601WeekOfYear(date),
                                Day = date.Day,
                                ChungLoaiXe = chungLoaiXe,
                                ChungLoaiXeAlias = Utility.AliasConvert(chungLoaiXe),
                                ChiPhiThang = (decimal)chiPhiThang,
                                ChiPhi1H = (decimal)chiPhiThang / 26 / 7
                            });
                        }
                    }
                    #endregion

                    #endregion
                }
            }
            return Json(new { url = "/" + Constants.LinkFactory.Main + "/" + Constants.LinkFactory.DanhGiaXCG });
        }


        [Route("/tai-lieu/nha-may/nha-thau")]
        [HttpPost]
        public ActionResult FactoryNhaThau()
        {
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

                    #region Read & Insert Data
                    dbContext.FactoryNhaThaus.DeleteMany(m => true);
                    headerCal = 1;
                    for (int i = headerCal; i <= sheet0.LastRowNum; i++)
                    {
                        IRow row = sheet0.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;


                        var xe = GetFormattedCellValue(row.GetCell(0));
                        var chungloai = GetFormattedCellValue(row.GetCell(1));
                        var nhathau = GetFormattedCellValue(row.GetCell(2));
                        var mangcongviec = GetFormattedCellValue(row.GetCell(3));

                        dbContext.FactoryNhaThaus.InsertOne(new FactoryNhaThau
                        {
                            Code = i.ToString(),
                            Xe = xe,
                            XeAlias = Utility.AliasConvert(xe),
                            ChungLoaiXe = chungloai,
                            ChungLoaiXeAlias = Utility.AliasConvert(chungloai),
                            NhaThau = nhathau,
                            NhaThauALias = Utility.AliasConvert(nhathau),
                            MangCongViec = mangcongviec,
                            MangCongViecAlias = Utility.AliasConvert(mangcongviec)
                        });
                    }
                    #endregion
                }
            }
            return Json(new { url = "/" + Constants.LinkFactory.Main + "/" + Constants.LinkFactory.DanhGiaXCG });
        }
        #endregion

        [Route("/tai-lieu/dia-diem/")]
        public IActionResult Location()
        {
            return View();
        }

        #region Reference code
        //[Route("/tai-lieu/nhan-vien/import/")]
        //public ActionResult NhanVienImport()
        //{
        //    int sheetCal = 0;
        //    int headerCal = 0;
        //    if (!String.IsNullOrEmpty(Request.Form["sheetCal"]))
        //    {
        //        sheetCal = Convert.ToInt32(Request.Form["sheetCal"]);
        //    }
        //    if (!String.IsNullOrEmpty(Request.Form["headerCal"]))
        //    {
        //        headerCal = Convert.ToInt32(Request.Form["headerCal"]);
        //    }

        //    IFormFile file = Request.Form.Files[0];
        //    string folderName = Constants.Storage.Hr;
        //    string webRootPath = _env.WebRootPath;
        //    string newPath = Path.Combine(webRootPath, folderName);
        //    StringBuilder sb = new StringBuilder();
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
        //                sheet = hssfwb.GetSheetAt(sheetCal); //get first sheet from workbook  
        //            }
        //            else
        //            {
        //                XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
        //                sheet = hssfwb.GetSheetAt(sheetCal); //get first sheet from workbook   
        //            }
        //            IRow headerRow = sheet.GetRow(headerCal); //Get Header Row
        //            int cellCount = headerRow.LastCellNum;
        //            sb.Append("<table class='table'><tr>");
        //            for (int j = 0; j < cellCount; j++)
        //            {
        //                NPOI.SS.UserModel.ICell cell = headerRow.GetCell(j);
        //                if (cell == null || string.IsNullOrWhiteSpace(cell.ToString())) continue;
        //                sb.Append("<th>" + cell.ToString() + "</th>");
        //            }
        //            sb.Append("</tr>");
        //            sb.AppendLine("<tr>");
        //            /*for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) *///Read Excel File
        //            for (int i = (headerCal + 1); i <= sheet.LastRowNum; i++)
        //            {
        //                IRow row = sheet.GetRow(i);
        //                if (row == null) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;
        //                for (int j = row.FirstCellNum; j < cellCount; j++)
        //                {
        //                    if (row.GetCell(j) != null)
        //                    {
        //                        sb.Append("<td><input type='text' value='" + GetFormattedCellValue(row.GetCell(j)) + "' class='form-control' /></td>");
        //                        //sb.Append("<td>" + row.GetCell(j).ToString() + "</td>");
        //                    }
        //                }
        //                sb.AppendLine("</tr>");
        //            }
        //            sb.Append("</table>");
        //        }
        //    }
        //    return this.Content(sb.ToString());
        //}
        #endregion

        protected string GetFormattedCellValue(ICell cell)
        {
            if (cell != null)
            {
                switch (cell.CellType)
                {
                    case CellType.String:
                        return cell.StringCellValue;

                    case CellType.Numeric:
                        if (DateUtil.IsCellDateFormatted(cell))
                        {
                            //DateTime date = cell.DateCellValue;
                            //ICellStyle style = cell.CellStyle;
                            //// Excel uses lowercase m for month whereas .Net uses uppercase
                            //string format = style.GetDataFormatString().Replace('m', 'M');
                            //string format = "dd/MM/yyyy hh:mm:ss";
                            //return date.ToString(format);
                            return cell.DateCellValue.ToString();
                        }
                        else
                        {
                            return cell.NumericCellValue.ToString();
                        }

                    case CellType.Boolean:
                        return cell.BooleanCellValue ? "TRUE" : "FALSE";

                    case CellType.Formula:
                        switch (cell.CachedFormulaResultType)
                        {
                            case CellType.String:
                                return cell.StringCellValue;
                            case CellType.Boolean:
                                return cell.BooleanCellValue ? "TRUE" : "FALSE";
                            case CellType.Numeric:
                                if (DateUtil.IsCellDateFormatted(cell))
                                {
                                    DateTime date = cell.DateCellValue;
                                    ICellStyle style = cell.CellStyle;
                                    // Excel uses lowercase m for month whereas .Net uses uppercase
                                    string format = style.GetDataFormatString().Replace('m', 'M');
                                    return date.ToString(format);
                                }
                                else
                                {
                                    return cell.NumericCellValue.ToString();
                                }
                        }
                        return cell.CellFormula;

                        //case CellType.Error:
                        //    return FormulaError.ForInt(cell.ErrorCellValue).String;
                }
            }
            // null or blank cell, or unknown cell type
            return string.Empty;
        }

        protected string GetFormattedCellValue2(ICell cell, string format)
        {
            if (cell != null)
            {
                switch (cell.CellType)
                {
                    case CellType.String:
                        return cell.StringCellValue;

                    case CellType.Numeric:
                        if (DateUtil.IsCellDateFormatted(cell))
                        {
                            //DateTime date = cell.DateCellValue;
                            //ICellStyle style = cell.CellStyle;
                            //// Excel uses lowercase m for month whereas .Net uses uppercase
                            //string format = style.GetDataFormatString().Replace('m', 'M');
                            //string format = "dd/MM/yyyy hh:mm:ss";
                            //return date.ToString(format);
                            return cell.DateCellValue.ToString(format);
                        }
                        else
                        {
                            return cell.NumericCellValue.ToString();
                        }

                    case CellType.Boolean:
                        return cell.BooleanCellValue ? "TRUE" : "FALSE";

                    case CellType.Formula:
                        switch (cell.CachedFormulaResultType)
                        {
                            case CellType.String:
                                return cell.StringCellValue;
                            case CellType.Boolean:
                                return cell.BooleanCellValue ? "TRUE" : "FALSE";
                            case CellType.Numeric:
                                if (DateUtil.IsCellDateFormatted(cell))
                                {
                                    DateTime date = cell.DateCellValue;
                                    ICellStyle style = cell.CellStyle;
                                    // Excel uses lowercase m for month whereas .Net uses uppercase
                                    format = style.GetDataFormatString().Replace('m', 'M');
                                    return date.ToString(format);
                                }
                                else
                                {
                                    return cell.NumericCellValue.ToString();
                                }
                        }
                        return cell.CellFormula;

                        //case CellType.Error:
                        //    return FormulaError.ForInt(cell.ErrorCellValue).String;
                }
            }
            // null or blank cell, or unknown cell type
            return string.Empty;
        }

        protected DateTime GetDateCellValue(ICell cell)
        {
            if (cell != null)
            {
                if (cell.CellType == CellType.Numeric)
                {
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.DateCellValue;
                    }
                }
                else if (cell.CellType == CellType.Formula)
                {
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.DateCellValue;
                    }
                }
            }
            // null or blank cell, or unknown cell type
            return DateTime.Now;
        }

        protected DateTime? GetDateCellValue2(ICell cell)
        {
            if (cell != null)
            {
                if (cell.CellType == CellType.Numeric)
                {
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.DateCellValue;
                    }
                }
            }
            return null;
        }

        protected double GetNumbericCellValue(ICell cell)
        {
            if (cell != null)
            {
                if (cell.CellType == CellType.Numeric)
                {
                    return cell.NumericCellValue;
                }
                if (cell.CellType == CellType.Formula)
                {
                    return cell.NumericCellValue;
                }
            }
            return 0;
        }

        public DateTime ParseExcelDate(string date)
        {
            DateTime dt;
            if (DateTime.TryParse(date, out dt))
            {
                return dt;
            }

            double oaDate;
            if (double.TryParse(date, out oaDate))
            {
                return DateTime.FromOADate(oaDate);
            }

            return DateTime.MinValue;
        }
    }
}
﻿using System;
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
using NPOI.SS.Util;
using MongoDB.Bson;
using System.Globalization;
using MimeKit;
using Services;
using Microsoft.AspNetCore.Authorization;
using System.Drawing;
using Common.Enums;
using Helpers;

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

        [Route("tai-lieu")]
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
                        sheet = hssfwb.GetSheetAt(0);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
                    }

                    #region Settings
                    var source = Constants.ActionLink.Create;
                    bool isEdit = false;
                    var result = true;
                    var message = Constants.Texts.Success;
                    var settings = dbContext.Settings.Find(m => m.Enable.Equals(true)).ToList();
                    var identityCardExpired = Convert.ToInt32(settings.Where(m => m.Key.Equals("identityCardExpired")).First().Value);
                    var employeeCodeFirst = settings.Where(m => m.Key.Equals("employeeCodeFirst")).First().Value;
                    var employeeCodeLength = settings.Where(m => m.Key.Equals("employeeCodeLength")).First().Value;
                    #endregion

                    dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.KhoiChucNang));
                    dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.PhongBan));
                    dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.BoPhan));
                    dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.ChucVu));

                    var filterA = Builders<Employee>.Filter.Eq(m => m.Enable, true);
                    var updateA = Builders<Employee>.Update
                        .Set(m => m.KhoiChucNang, null)
                        .Set(m => m.KhoiChucNangName, null)
                        .Set(m => m.PhongBan, null)
                        .Set(m => m.PhongBanName, null)
                        .Set(m => m.BoPhan, null)
                        .Set(m => m.BoPhanName, null)
                        .Set(m => m.ChucVu, null)
                        .Set(m => m.ChucVuName, null);
                    dbContext.Employees.UpdateMany(filterA, updateA);
                    var phongbanE = new Category()
                    {
                        Type = (int)ECategory.PhongBan,
                        Name = "Ban lãnh đạo",
                        Alias = Utility.AliasConvert("Ban lãnh đạo")
                    };
                    dbContext.Categories.InsertOne(phongbanE);

                    var systemDate = Constants.MinDate;
                    var kcn = string.Empty;
                    var phongban = string.Empty;
                    var bophan = string.Empty;
                    var kcnE = new Category();
                    var bophanE = new Category();
                    int iKcn = 1;
                    int iPb = 1;
                    int iBp = 1;

                    for (int i = 5; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var col0 = GetFormattedCellValue(row.GetCell(0));
                        var col1 = GetFormattedCellValue(row.GetCell(1));
                        var col2 = GetFormattedCellValue(row.GetCell(2));
                        var col3 = GetFormattedCellValue(row.GetCell(3));
                        var col4 = GetDateCellValue(row.GetCell(4));

                        if (string.IsNullOrEmpty(col1) && string.IsNullOrEmpty(col2) && string.IsNullOrEmpty(col3))
                        {
                            kcn = col0.Trim();
                            phongban = string.Empty;
                            bophan = string.Empty;
                            phongbanE = new Category();
                            bophanE = new Category();
                            if (!string.IsNullOrEmpty(kcn))
                            {
                                kcnE = dbContext.Categories.Find(m => m.Name.Equals(kcn) && m.Type.Equals((int)ECategory.KhoiChucNang)).FirstOrDefault();
                                if (kcnE == null)
                                {
                                    kcnE = new Category()
                                    {
                                        Type = (int)ECategory.KhoiChucNang,
                                        Name = kcn,
                                        Alias = Utility.AliasConvert(kcn),
                                        CodeInt = iKcn,
                                        Code = iKcn.ToString()
                                    };
                                    dbContext.Categories.InsertOne(kcnE);
                                    iKcn++;
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(col0) && string.IsNullOrEmpty(col1) && !string.IsNullOrEmpty(col2))
                        {
                            bophan = string.Empty;
                            bophanE = new Category();
                            phongban = col2.Trim();
                            phongbanE = dbContext.Categories.Find(m => m.Name.Equals(phongban) && m.Type.Equals((int)ECategory.PhongBan)).FirstOrDefault();
                            if (phongbanE == null)
                            {
                                phongbanE = new Category()
                                {
                                    Type = (int)ECategory.PhongBan,
                                    Name = phongban,
                                    Alias = Utility.AliasConvert(phongban),
                                    ParentId = kcnE.Id,
                                    CodeInt = iPb,
                                    Code = iPb.ToString()
                                };
                                dbContext.Categories.InsertOne(phongbanE);
                                iPb++;
                            }
                        }
                        if (string.IsNullOrEmpty(col0) && string.IsNullOrEmpty(col1) && string.IsNullOrEmpty(col2))
                        {
                            bophan = col3.Trim();
                            bophanE = dbContext.Categories.Find(m => m.Name.Equals(bophan) && m.Type.Equals((int)ECategory.BoPhan)).FirstOrDefault();
                            if (bophanE == null)
                            {
                                bophanE = new Category()
                                {
                                    Type = (int)ECategory.BoPhan,
                                    Name = bophan,
                                    Alias = Utility.AliasConvert(bophan),
                                    ParentId = phongbanE.Id,
                                    CodeInt = iBp,
                                    Code = iBp.ToString()
                                };
                                dbContext.Categories.InsertOne(bophanE);
                                iBp++;
                            }
                        }
                        if (!string.IsNullOrEmpty(col0) && !string.IsNullOrEmpty(col1) && !string.IsNullOrEmpty(col2))
                        {
                            var maNV = col1.Trim();
                            var hoten = col2.Trim();
                            var hotenalias = Utility.AliasConvert(hoten);
                            var chucvu = col3.Trim();
                            var thamnien = col4;

                            var chucvuE = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Name.Equals(chucvu) && m.Type.Equals((int)ECategory.ChucVu)).FirstOrDefault();
                            if (chucvuE == null)
                            {
                                var parentId = string.Empty;
                                if (bophanE != null && !string.IsNullOrEmpty(bophanE.Id))
                                {
                                    parentId = bophanE.Id;
                                }
                                else if (phongbanE != null && !string.IsNullOrEmpty(phongbanE.Id))
                                {
                                    parentId = phongbanE.Id;
                                }
                                else
                                {
                                    parentId = kcnE.Id;
                                }
                                chucvuE = new Category()
                                {
                                    Type = (int)ECategory.ChucVu,
                                    Name = chucvu,
                                    Alias = Utility.AliasConvert(chucvu),
                                    ParentId = parentId
                                };
                                dbContext.Categories.InsertOne(chucvuE);
                            }

                            var employeeE = dbContext.Employees.Find(m => m.AliasFullName.Equals(hotenalias) && m.Enable.Equals(true)).FirstOrDefault();
                            if (employeeE != null)
                            {
                                var builder = Builders<Employee>.Filter;
                                var filter = Builders<Employee>.Filter.Eq(m => m.Id, employeeE.Id);
                                var update = Builders<Employee>.Update
                                    .Set(m => m.KhoiChucNang, kcnE.Id)
                                    .Set(m => m.KhoiChucNangName, kcnE.Name)
                                    .Set(m => m.PhongBan, phongbanE.Id)
                                    .Set(m => m.PhongBanName, phongbanE.Name)
                                    .Set(m => m.BoPhan, bophanE.Id)
                                    .Set(m => m.BoPhanName, bophanE.Name)
                                    .Set(m => m.ChucVu, chucvuE.Id)
                                    .Set(m => m.ChucVuName, chucvuE.Name);
                                //.Set(m => m.Joinday, thamnien);
                                dbContext.Employees.UpdateOne(filter, update);
                            }
                            else
                            {
                                var pwdrandom = Guid.NewGuid().ToString("N").Substring(0, 6);
                                var sysPassword = Helper.HashedPassword(pwdrandom);
                                var lastEntity = dbContext.Employees.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Id).Limit(1).First();
                                var x = 1;
                                if (lastEntity != null && lastEntity.Code != null)
                                {
                                    x = Convert.ToInt32(lastEntity.Code.Replace(employeeCodeFirst, string.Empty)) + 1;
                                }
                                var sysCode = employeeCodeFirst + x.ToString($"D{employeeCodeLength}");

                                dbContext.Employees.InsertOne(new Employee()
                                {
                                    Code = sysCode,
                                    Password = sysPassword,
                                    FullName = hoten,
                                    AliasFullName = hotenalias,
                                    KhoiChucNang = kcnE.Id,
                                    KhoiChucNangName = kcnE.Name,
                                    PhongBan = phongbanE.Id,
                                    PhongBanName = phongbanE.Name,
                                    BoPhan = bophanE.Id,
                                    BoPhanName = bophanE.Name,
                                    ChucVu = chucvuE.Id,
                                    ChucVuName = chucvuE.Name,
                                    Joinday = thamnien,
                                    CodeOld = maNV,
                                    IsTimeKeeper = true,
                                    Official = false,
                                    Nation = "Việt Nam",
                                    Religion = "Kinh",
                                    BhxhEnable = false
                                });
                            }
                        }
                    }
                }
            }
            return Json(new { result = true, url = Constants.LinkHr.Main + "/" + Constants.LinkHr.List });
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

        [Route("/tai-lieu/ngay-phep/")]
        public IActionResult NgayPhep()
        {
            return View();
        }

        [Route("/tai-lieu/ngay-phep/update/")]
        [HttpPost]
        public ActionResult NgayPhepUpdate()
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

                    var typeLeave = dbContext.LeaveTypes.Find(m => m.Display.Equals(true) && m.SalaryPay.Equals(true) && m.Alias.Equals("phep-nam")).FirstOrDefault();
                    for (int i = 2; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var code = GetFormattedCellValue(row.GetCell(1));
                        var fullName = GetFormattedCellValue(row.GetCell(2));
                        var alias = Utility.AliasConvert(fullName);
                        var email = GetFormattedCellValue(row.GetCell(3));
                        if (!string.IsNullOrEmpty(email))
                        {
                            email = Utility.EmailConvert(fullName);
                        }
                        var phepcon = GetNumbericCellValue(row.GetCell(6));
                        // get employee by code -> email (from fullname) -> fullname
                        var employee = new Employee();
                        if (!string.IsNullOrEmpty(alias))
                        {
                            employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.AliasFullName.Equals(alias)).FirstOrDefault();
                        }
                        if (employee == null)
                        {
                            if (!string.IsNullOrEmpty(email))
                            {
                                employee = dbContext.Employees.Find(m => m.Enable.Equals(true) & m.Email.Equals(email)).FirstOrDefault();
                            }
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
                            if (employee.Id == "5b6bfc463ee8461ee48cbbea")
                            {
                                var phoo = phepcon;
                            }
                            if (dbContext.LeaveEmployees.CountDocuments(m => m.EmployeeId.Equals(employee.Id) & m.LeaveTypeId.Equals(typeLeave.Id)) > 0)
                            {
                                var filter = Builders<LeaveEmployee>.Filter.Eq(m => m.EmployeeId, employee.Id);
                                filter = filter & Builders<LeaveEmployee>.Filter.Eq(m => m.LeaveTypeId, typeLeave.Id);
                                var update = Builders<LeaveEmployee>.Update
                                    .Set(m => m.Number, phepcon);
                                dbContext.LeaveEmployees.UpdateOne(filter, update);
                            }
                            else
                            {
                                dbContext.LeaveEmployees.InsertOne(new LeaveEmployee
                                {
                                    LeaveTypeId = typeLeave.Id,
                                    EmployeeId = employee.Id,
                                    LeaveTypeName = typeLeave.Name,
                                    EmployeeName = employee.FullName,
                                    Number = phepcon
                                });
                            }
                        }
                        else
                        {
                            // Insert log
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "leavedate",
                                Object = fullName + " - " + email,
                                Error = "No get data",
                                DateTime = DateTime.Now.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { url = "/hr/nhan-su" });
        }

        [Route("/tai-lieu/ma-cham-cong/")]
        public IActionResult MaChamCong()
        {
            return View();
        }

        [Route("/tai-lieu/ma-cham-cong/update/")]
        [HttpPost]
        public ActionResult MaChamCongUpdate()
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

                    for (int i = 1; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var code = Convert.ToInt32(GetFormattedCellValue(row.GetCell(0)));
                        var fullName = GetFormattedCellValue(row.GetCell(1));
                        var alias = Utility.AliasConvert(fullName);
                        var email = Utility.EmailConvert(fullName);
                        var employee = dbContext.Employees.Find(m => m.Email.Equals(email)).FirstOrDefault();
                        if (employee == null)
                        {
                            employee = dbContext.Employees.Find(m => m.AliasFullName.Equals(alias)).FirstOrDefault();
                        }

                        if (employee != null)
                        {
                            // Update fingercode code.ToString("000")
                            var workPlaces = employee.Workplaces;
                            foreach (var workplace in workPlaces)
                            {
                                if (workplace.Code == "NM")
                                {
                                    workplace.Fingerprint = code.ToString("000");
                                }
                            }

                            var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                            var update = Builders<Employee>.Update
                                .Set(m => m.Workplaces, workPlaces);
                            dbContext.Employees.UpdateOne(filter, update);
                        }
                        else
                        {
                            // Insert log
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "fingercode",
                                Object = fullName,
                                Error = "No get data",
                                DateTime = DateTime.Now.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { url = "/hr/nhan-su" });
        }

        public void UpdateLeave()
        {
            var list = dbContext.Employees.Find(m => m.Enable.Equals(true)).ToList();
            foreach (var item in list)
            {
                var filter = Builders<Employee>.Filter.Eq(m => m.Id, item.Id);
                var update = Builders<Employee>.Update
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
        [Route("/tai-lieu/nha-may")]
        public IActionResult Factory()
        {
            return View();
        }

        [Route("/tai-lieu/nha-may/ma-so-van-hanh")]
        [HttpPost]
        public ActionResult FactoryMaSoVanHanh()
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
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet0 = hssfwb.GetSheetAt(0); //get first sheet from workbook  
                        sheet1 = hssfwb.GetSheetAt(1);
                        //sheet2 = hssfwb.GetSheetAt(2);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet0 = hssfwb.GetSheetAt(0); //get first sheet from workbook   
                        sheet1 = hssfwb.GetSheetAt(1);
                        //sheet2 = hssfwb.GetSheetAt(2);
                    }
                    #region Read & Insert Data

                    #region Sheet 0 Ma so
                    var vanHanh = "Vận hành";
                    var vanHanhE = dbContext.Categories.Find(m => m.Name.Equals(vanHanh)).FirstOrDefault();
                    if (vanHanhE == null)
                    {
                        vanHanhE = new Category()
                        {
                            Name = "Vận hành",
                            Alias = "van-hanh",
                            ModeData = (int)EModeData.File
                        };
                        dbContext.Categories.InsertOne(vanHanhE);
                    }
                    var parentId = vanHanhE.Id;

                    dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.Ca));
                    dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.CongDoan));
                    dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.XeCoGioivsMayMoc));
                    dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.PhanLoaiXe));
                    dbContext.Categories.DeleteMany(m => m.Type.Equals((int)ECategory.NVLvsBTPvsTP));
                    headerCal = 2;
                    int codeInt = 1;
                    for (int i = headerCal; i <= sheet0.LastRowNum; i++)
                    {
                        IRow row = sheet0.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var ca = GetFormattedCellValue(row.GetCell(0));
                        var macongdoan = GetFormattedCellValue(row.GetCell(1));
                        var congdoan = GetFormattedCellValue(row.GetCell(2));
                        var noidungcongdoan = GetFormattedCellValue(row.GetCell(3));
                        var xecogioivsmaymoc = GetFormattedCellValue(row.GetCell(4));
                        var phanloaixe = GetFormattedCellValue(row.GetCell(5));
                        var chungloaixe = GetFormattedCellValue(row.GetCell(6));
                        var nhathau = GetFormattedCellValue(row.GetCell(7));
                        var mangcongviec = GetFormattedCellValue(row.GetCell(8));
                        var maxe = GetFormattedCellValue(row.GetCell(9));

                        var nvlvsbtpvstp = GetFormattedCellValue(row.GetCell(10));

                        if (!string.IsNullOrEmpty(ca))
                        {
                            dbContext.Categories.InsertOne(new Category()
                            {
                                Type = (int)ECategory.Ca,
                                Name = ca,
                                Alias = Utility.AliasConvert(ca),
                                ParentId = parentId,
                                ModeData = (int)EModeData.File
                            });
                        }

                        if (!string.IsNullOrEmpty(macongdoan))
                        {
                            dbContext.Categories.InsertOne(new Category()
                            {
                                Type = (int)ECategory.CongDoan,
                                Code = macongdoan,
                                Name = congdoan,
                                Alias = Utility.AliasConvert(congdoan),
                                //Content = noidungcongdoan,
                                ParentId = parentId,
                                ModeData = (int)EModeData.File
                            });
                        }

                        if (!string.IsNullOrEmpty(xecogioivsmaymoc))
                        {
                            var phanloaixeId = string.Empty;
                            var chungloaiId = string.Empty;
                            var nhathauId = string.Empty;
                            var mangcongviecId = string.Empty;
                            if (!string.IsNullOrEmpty(phanloaixe))
                            {
                                var phanloaiE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.PhanLoaiXe) && m.Name.Equals(phanloaixe)).FirstOrDefault();
                                if (phanloaiE == null)
                                {
                                    phanloaiE = new Category
                                    {
                                        Type = (int)ECategory.PhanLoaiXe,
                                        Name = phanloaixe,
                                        Alias = Utility.AliasConvert(phanloaixe),
                                        ParentId = parentId,
                                        ModeData = (int)EModeData.File
                                    };
                                    dbContext.Categories.InsertOne(phanloaiE);
                                }
                                phanloaixeId = phanloaiE.Id;
                            }
                            if (!string.IsNullOrEmpty(chungloaixe))
                            {
                                var chungloaiE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.ChungLoaiXe) && m.Name.Equals(chungloaixe)).FirstOrDefault();
                                if (chungloaiE == null)
                                {
                                    chungloaiE = new Category
                                    {
                                        Type = (int)ECategory.ChungLoaiXe,
                                        Name = chungloaixe,
                                        Alias = Utility.AliasConvert(chungloaixe),
                                        ParentId = parentId,
                                        ModeData = (int)EModeData.File
                                    };
                                    dbContext.Categories.InsertOne(chungloaiE);
                                }
                                chungloaiId = chungloaiE.Id;
                            }
                            if (!string.IsNullOrEmpty(nhathau))
                            {
                                var nhathauE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.NhaThau) && m.Name.Equals(nhathau)).FirstOrDefault();
                                if (nhathauE == null)
                                {
                                    nhathauE = new Category
                                    {
                                        Type = (int)ECategory.NhaThau,
                                        Name = nhathau,
                                        Alias = Utility.AliasConvert(nhathau),
                                        ParentId = parentId,
                                        ModeData = (int)EModeData.File
                                    };
                                    dbContext.Categories.InsertOne(nhathauE);
                                }
                                nhathauId = nhathauE.Id;
                            }
                            if (!string.IsNullOrEmpty(mangcongviec))
                            {
                                var mangcongviecE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.MangCongViec) && m.Name.Equals(mangcongviec)).FirstOrDefault();
                                if (mangcongviecE == null)
                                {
                                    mangcongviecE = new Category
                                    {
                                        Type = (int)ECategory.MangCongViec,
                                        Name = mangcongviec,
                                        Alias = Utility.AliasConvert(mangcongviec),
                                        ParentId = parentId,
                                        ModeData = (int)EModeData.File
                                    };
                                    dbContext.Categories.InsertOne(mangcongviecE);
                                }
                                mangcongviecId = mangcongviecE.Id;
                            }

                            if (!string.IsNullOrEmpty(maxe))
                            {
                                codeInt = Convert.ToInt32(maxe.Substring(maxe.Length - 2));
                            }
                            dbContext.Categories.InsertOne(new Category
                            {
                                Type = (int)ECategory.XeCoGioivsMayMoc,
                                Code = maxe,
                                CodeInt = codeInt,
                                Name = xecogioivsmaymoc,
                                Alias = Utility.AliasConvert(xecogioivsmaymoc),
                                ParentId = parentId,
                                //ChungLoaiId = chungloaiId,
                                //NhaThauId = nhathauId,
                                //MangCongViecId = mangcongviecId,
                                ModeData = (int)EModeData.File
                            });
                        }

                        if (!string.IsNullOrEmpty(nvlvsbtpvstp))
                        {
                            dbContext.Categories.InsertOne(new Category
                            {
                                Type = (int)ECategory.NVLvsBTPvsTP,
                                Name = nvlvsbtpvstp,
                                Alias = Utility.AliasConvert(nvlvsbtpvstp),
                                ParentId = parentId,
                                ModeData = (int)EModeData.File
                            });
                        }

                        codeInt++;
                    }
                    #endregion

                    #region Sheet 1 DATA Van Hanh
                    dbContext.FactoryVanHanhs.DeleteMany(m => true);
                    headerCal = 2;
                    for (int i = headerCal; i <= sheet1.LastRowNum; i++)
                    {
                        IRow row = sheet1.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var month = GetNumbericCellValue(row.GetCell(0));
                        var date = GetDateCellValue(row.GetCell(1));
                        var ca = GetFormattedCellValue(row.GetCell(2));
                        var congdoan = GetFormattedCellValue(row.GetCell(3));
                        var noidungcongdoan = GetFormattedCellValue(row.GetCell(4));
                        var macongdoan = GetFormattedCellValue(row.GetCell(5));
                        var lot = GetFormattedCellValue(row.GetCell(6));
                        var xecogioiMay = GetFormattedCellValue(row.GetCell(7));
                        var nvlTp = GetFormattedCellValue(row.GetCell(8));
                        var tenCongNhan = GetFormattedCellValue(row.GetCell(9));
                        var caLamViec = GetFormattedCellValue(row.GetCell(10));
                        var thoigianbatdau = row.GetCell(11) != null ? DateTime.FromOADate(row.GetCell(11).NumericCellValue).TimeOfDay : TimeSpan.Zero;
                        var thoigianketthuc = row.GetCell(12) != null ? DateTime.FromOADate(row.GetCell(12).NumericCellValue).TimeOfDay : TimeSpan.Zero;
                        var thoigianbttq = row.GetCell(13) != null ? DateTime.FromOADate(row.GetCell(13).NumericCellValue).TimeOfDay : TimeSpan.Zero;
                        var thoigianxehu = row.GetCell(14) != null ? DateTime.FromOADate(row.GetCell(14).NumericCellValue).TimeOfDay : TimeSpan.Zero;
                        var thoigiannghi = row.GetCell(15) != null ? DateTime.FromOADate(row.GetCell(15).NumericCellValue).TimeOfDay : TimeSpan.Zero;
                        var thoigiancongvieckhac = row.GetCell(16) != null ? DateTime.FromOADate(row.GetCell(16).NumericCellValue).TimeOfDay : TimeSpan.Zero;
                        var thoigianlamviec = thoigianketthuc.Subtract(thoigianbatdau).Subtract(thoigianbttq).Subtract(thoigianxehu).Subtract(thoigiannghi).Subtract(thoigiancongvieckhac);
                        double soluongthuchien = GetNumbericCellValue(row.GetCell(18));
                        double dau = GetNumbericCellValue(row.GetCell(19));
                        double nhot10 = GetNumbericCellValue(row.GetCell(20));
                        double nhot50 = GetNumbericCellValue(row.GetCell(21));
                        double nhot90 = GetNumbericCellValue(row.GetCell(22));
                        double nhot140 = GetNumbericCellValue(row.GetCell(23));
                        var nguyennhan = GetFormattedCellValue(row.GetCell(24));

                        var caId = string.Empty;
                        var caAlias = string.Empty;
                        if (!string.IsNullOrEmpty(ca))
                        {
                            var caE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.Ca) && m.Name.Equals(ca)).FirstOrDefault();
                            if (caE == null)
                            {
                                caE = new Category()
                                {
                                    Type = (int)ECategory.Ca,
                                    Name = ca,
                                    Alias = Utility.AliasConvert(ca),
                                    ParentId = parentId,
                                    ModeData = (int)EModeData.File
                                };
                                dbContext.Categories.InsertOne(caE);
                            }
                            caId = caE.Id;
                            caAlias = caE.Alias;
                        }
                        var congDoanId = string.Empty;
                        var congDoanAlias = string.Empty;
                        if (!string.IsNullOrEmpty(macongdoan))
                        {
                            var congdoanE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.CongDoan) && m.Code.Equals(macongdoan)).FirstOrDefault();
                            if (congdoanE == null)
                            {
                                congdoanE = new Category()
                                {
                                    Type = (int)ECategory.CongDoan,
                                    Code = macongdoan,
                                    Name = congdoan,
                                    Alias = Utility.AliasConvert(congdoan),
                                    //Content = noidungcongdoan,
                                    ParentId = parentId,
                                    ModeData = (int)EModeData.File
                                };
                                dbContext.Categories.InsertOne(congdoanE);
                            }
                            congDoanId = congdoanE.Id;
                            congDoanAlias = congdoanE.Alias;
                            congdoan = congdoanE.Name;
                            //noidungcongdoan = congdoanE.Content;
                        }

                        var xeCoGioiMayId = string.Empty;
                        var xeCoGioiMayCode = string.Empty;
                        var xeCoGioiMayAlias = string.Empty;
                        if (!string.IsNullOrEmpty(xecogioiMay))
                        {
                            var xeCoGioiMayE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.XeCoGioivsMayMoc) && m.Name.Equals(xecogioiMay)).FirstOrDefault();
                            if (xeCoGioiMayE == null)
                            {
                                var code = "NON01";
                                var codeInt2 = 1;
                                var lastestE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.XeCoGioivsMayMoc)).SortByDescending(m => m.CodeInt).FirstOrDefault();
                                if (lastestE != null)
                                {
                                    codeInt2 = lastestE.CodeInt + 1;
                                    code = "NON" + codeInt2.ToString("D2");
                                }
                                xeCoGioiMayE = new Category
                                {
                                    Type = (int)ECategory.XeCoGioivsMayMoc,
                                    Code = code,
                                    CodeInt = codeInt2,
                                    Name = xecogioiMay,
                                    Alias = Utility.AliasConvert(xecogioiMay),
                                    ParentId = parentId,
                                    ModeData = (int)EModeData.File
                                };
                                dbContext.Categories.InsertOne(xeCoGioiMayE);
                            }
                            xeCoGioiMayId = xeCoGioiMayE.Id;
                            xeCoGioiMayCode = xeCoGioiMayE.Code;
                            xeCoGioiMayAlias = xeCoGioiMayE.Alias;
                        }

                        var productId = string.Empty;
                        var productAlias = string.Empty;
                        if (!string.IsNullOrEmpty(nvlTp))
                        {
                            var nvlE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.NVLvsBTPvsTP) && m.Name.Equals(nvlTp)).FirstOrDefault();
                            if (nvlE == null)
                            {
                                nvlE = new Category
                                {
                                    Type = (int)ECategory.NVLvsBTPvsTP,
                                    Name = nvlTp,
                                    Alias = Utility.AliasConvert(nvlTp),
                                    ParentId = parentId,
                                    ModeData = (int)EModeData.File
                                };
                                dbContext.Categories.InsertOne(nvlE);
                            }
                            productId = nvlE.Id;
                            productAlias = nvlE.Alias;
                        }

                        var employeeId = string.Empty;
                        var employeeAlias = string.Empty;
                        if (!string.IsNullOrEmpty(tenCongNhan))
                        {
                            var employeeE = dbContext.Employees.Find(m => m.FullName.Equals(tenCongNhan)).FirstOrDefault();
                            if (employeeE != null)
                            {
                                employeeId = employeeE.Id;
                                employeeAlias = employeeE.AliasFullName;
                            }
                        }

                        var caLamViecId = string.Empty;
                        var caLamViecAlias = string.Empty;
                        if (!string.IsNullOrEmpty(caLamViec))
                        {
                            var caLamViecE = dbContext.Categories.Find(m => m.Type.Equals((int)ECategory.CaLamViec) && m.Name.Equals(caLamViec)).FirstOrDefault();
                            if (caLamViecE == null)
                            {
                                caLamViecE = new Category
                                {
                                    Type = (int)ECategory.CaLamViec,
                                    Name = caLamViec,
                                    Alias = Utility.AliasConvert(caLamViec),
                                    ParentId = parentId,
                                    ModeData = (int)EModeData.File
                                };
                                dbContext.Categories.InsertOne(caLamViecE);
                            }
                            caLamViecId = caLamViecE.Id;
                            caLamViecAlias = caLamViecE.Alias;
                        }

                        var phieuInCa = Utility.NoPhieuInCa(date, xeCoGioiMayCode);

                        dbContext.FactoryVanHanhs.InsertOne(new FactoryVanHanh
                        {
                            Year = date.Year,
                            Month = date.Month,
                            Week = Utility.GetIso8601WeekOfYear(date),
                            Day = date.Day,
                            Date = date,
                            Ca = ca,
                            CaId = caId,
                            CaAlias = caAlias,
                            CongDoanId = congDoanId,
                            CongDoanCode = macongdoan,
                            CongDoanName = congdoan,
                            CongDoanAlias = congDoanAlias,
                            CongDoanNoiDung = noidungcongdoan,
                            LOT = lot,
                            XeCoGioiMayId = xeCoGioiMayId,
                            XeCoGioiMayCode = xeCoGioiMayCode,
                            XeCoGioiMayName = xecogioiMay,
                            XeCoGioiMayAlias = xeCoGioiMayAlias,
                            ProductId = productId,
                            ProductName = nvlTp,
                            ProductAlias = productAlias,
                            Employee = tenCongNhan,
                            EmployeeId = employeeId,
                            EmployeeAlias = employeeAlias,
                            CaLamViec = caLamViec,
                            CaLamViecId = caLamViecId,
                            CaLamViecAlias = caLamViecAlias,
                            StartVH = thoigianbatdau,
                            End = thoigianketthuc,
                            ThoiGianBTTQ = thoigianbttq,
                            ThoiGianXeHu = thoigianxehu,
                            ThoiGianNghi = thoigiannghi,
                            ThoiGianCVKhac = thoigiancongvieckhac,
                            ThoiGianLamViec = thoigianlamviec,
                            SoLuongThucHien = soluongthuchien,
                            Dau = dau,
                            Nhot10 = nhot10,
                            Nhot50 = nhot50,
                            Nhot90 = nhot90,
                            Nhot140 = nhot140,
                            NguyenNhan = nguyennhan,
                            PhieuInCa = phieuInCa,
                            ModeData = (int)EModeData.File
                        });
                    }
                    #endregion

                    #region Sheet 2 DATA ton SX
                    //dbContext.FactoryTonSXs.DeleteMany(m => true);
                    //headerCal = 2;
                    //for (int i = headerCal; i <= sheet1.LastRowNum; i++)
                    //{
                    //    IRow row = sheet1.GetRow(i);
                    //    if (row == null) continue;
                    //    if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                    //    if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                    //    if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                    //    var date = GetDateCellValue(row.GetCell(1));
                    //    var product = GetFormattedCellValue(row.GetCell(2));
                    //    var unit = GetFormattedCellValue(row.GetCell(3));
                    //    var productAlias = Utility.AliasConvert(product);
                    //    var productId = string.Empty;
                    //    var productEntity = dbContext.FactoryProducts.Find(m => m.Alias.Equals(productAlias)).FirstOrDefault();
                    //    if (productEntity != null)
                    //    {
                    //        productId = productEntity.Id;
                    //    }
                    //    else
                    //    {
                    //        var newProduct = new FactoryProduct
                    //        {
                    //            Name = product,
                    //            Alias = productAlias,
                    //            Unit = unit
                    //        };
                    //        dbContext.FactoryProducts.InsertOne(newProduct);
                    //        productId = newProduct.Id;
                    //    }

                    //    var lot = GetFormattedCellValue(row.GetCell(4));
                    //    decimal tondaungay = 0;
                    //    if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(5))))
                    //    {
                    //        tondaungay = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(5)));
                    //    }
                    //    decimal nhaptusanxuat = 0;
                    //    if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(6))))
                    //    {
                    //        nhaptusanxuat = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(6)));
                    //    }
                    //    decimal nhaptukho = 0;
                    //    if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(7))))
                    //    {
                    //        nhaptukho = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(7)));
                    //    }
                    //    decimal xuatchokho = 0;
                    //    if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(8))))
                    //    {
                    //        xuatchokho = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(8)));
                    //    }
                    //    decimal xuatchosanxuat = 0;
                    //    if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(9))))
                    //    {
                    //        xuatchosanxuat = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(9)));
                    //    }
                    //    decimal xuathaohut = 0;
                    //    if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(10))))
                    //    {
                    //        xuathaohut = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(10)));
                    //    }
                    //    decimal toncuoingay = 0;
                    //    if (!string.IsNullOrEmpty(GetFormattedCellValue(row.GetCell(11))))
                    //    {
                    //        toncuoingay = Convert.ToDecimal(GetFormattedCellValue(row.GetCell(11)));
                    //    }

                    //    dbContext.FactoryTonSXs.InsertOne(new FactoryTonSX
                    //    {
                    //        Year = date.Year,
                    //        Month = date.Month,
                    //        Week = Utility.GetIso8601WeekOfYear(date),
                    //        Day = date.Day,
                    //        Date = date,
                    //        ProductId = productId,
                    //        Product = product,
                    //        ProductAlias = Utility.AliasConvert(product),
                    //        Unit = unit,
                    //        LOT = lot,
                    //        TonDauNgay = tondaungay,
                    //        NhapTuSanXuat = nhaptusanxuat,
                    //        XuatChoSanXuat = xuatchosanxuat,
                    //        NhapTuKho = nhaptukho,
                    //        XuatChoKho = xuatchokho,
                    //        XuatHaoHut = xuathaohut,
                    //        TonCuoiNgay = toncuoingay
                    //    });

                    //    // Update Quantity Product
                    //    var builderUpdateQuantityProduct = Builders<FactoryProduct>.Filter;
                    //    var filterUpdateQuantityProduct = builderUpdateQuantityProduct.Eq(m => m.Id, productId);
                    //    var updateQuantityProduct = Builders<FactoryProduct>.Update
                    //        .Set(m => m.Quantity, toncuoingay);
                    //    dbContext.FactoryProducts.UpdateOne(filterUpdateQuantityProduct, updateQuantityProduct);
                    //}

                    //// Update Unit Product & create Unit collections
                    //var tonsxs = dbContext.FactoryTonSXs.Find(m => true).ToList();
                    //var groups = (from p in tonsxs
                    //              group p by new
                    //              {
                    //                  p.ProductAlias,
                    //                  p.Unit
                    //              }
                    //          into d
                    //              select new
                    //              {
                    //                  Product = d.Key.ProductAlias,
                    //                  d.Key.Unit
                    //              }).ToList();
                    //foreach (var group in groups)
                    //{
                    //    var builderUpdateProduct = Builders<FactoryProduct>.Filter;
                    //    var filterUpdateProduct = builderUpdateProduct.Eq(m => m.Alias, group.Product);
                    //    var updateProduct = Builders<FactoryProduct>.Update
                    //        .Set(m => m.Unit, group.Unit);
                    //    dbContext.FactoryProducts.UpdateOne(filterUpdateProduct, updateProduct);

                    //    var aliasUnit = Utility.AliasConvert(group.Unit);
                    //    if (dbContext.Units.CountDocuments(m => m.Type.Equals(Constants.UnitType.Factory) & m.Alias.Equals(aliasUnit)) == 0)
                    //    {
                    //        dbContext.Units.InsertOne(new Unit
                    //        {
                    //            Type = Constants.UnitType.Factory,
                    //            Name = group.Unit,
                    //            Alias = aliasUnit
                    //        });
                    //    }
                    //}

                    #endregion
                    #region Sheet 3 BC ton SX

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
            return Json(new { url = "/" + Constants.LinkFactory.VanHanh });
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
                        if (!string.IsNullOrEmpty(tempchungloaixe))
                        {
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

        #region SALE
        //sale-chuc-vu
        [Route("/tai-lieu/sale-chuc-vu/")]
        public IActionResult SaleChucVu()
        {
            return View();
        }

        [Route("/tai-lieu/sale-chuc-vu/update/")]
        [HttpPost]
        public ActionResult SaleChucVuUpdate()
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

                    var typeLeave = dbContext.LeaveTypes.Find(m => m.Display.Equals(true) && m.SalaryPay.Equals(true) && m.Alias.Equals("phep-nam")).FirstOrDefault();
                    for (int i = 1; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
                        if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

                        var code = GetFormattedCellValue(row.GetCell(0));
                        var fullName = GetFormattedCellValue(row.GetCell(1));
                        var alias = Utility.AliasConvert(fullName);
                        var title = GetFormattedCellValue(row.GetCell(2));
                        // get employee by code -> fullname
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
                            var filter = Builders<Employee>.Filter.Eq(m => m.Id, employee.Id);
                            var update = Builders<Employee>.Update
                                .Set(m => m.SaleChucVu, title);
                            dbContext.Employees.UpdateOne(filter, update);
                        }
                        else
                        {
                            dbContext.Misss.InsertOne(new Miss
                            {
                                Type = "sale-title",
                                Object = code + "-" + fullName + "-" + title,
                                Error = "No get data",
                                DateTime = DateTime.Now.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { url = "/hr/nhan-su" });
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

        //[Route("/tai-lieu/nhan-vien/update/")]
        //[HttpPost]
        //public ActionResult NhanVienUpdate()
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
        //                sheet = hssfwb.GetSheetAt(1);
        //            }
        //            else
        //            {
        //                XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
        //                sheet = hssfwb.GetSheetAt(1); //get first sheet from workbook   
        //            }

        //            for (int i = 5; i <= sheet.LastRowNum; i++)
        //            {
        //                IRow row = sheet.GetRow(i);
        //                if (row == null) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Error)) continue;
        //                if (row.Cells.All(d => d.CellType == CellType.Unknown)) continue;

        //                var systemDate = Constants.MinDate;
        //                if (i < 180)
        //                {
        //                    var fullName = GetFormattedCellValue(row.GetCell(5));
        //                    var employeeEntity = dbContext.Employees.Find(m => m.FullName.Equals(fullName)).FirstOrDefault();
        //                    if (employeeEntity != null)
        //                    {
        //                        var statusMarital = GetFormattedCellValue(row.GetCell(22));
        //                        var nation = GetFormattedCellValue(row.GetCell(23));
        //                        var religion = GetFormattedCellValue(row.GetCell(24));

        //                        #region Certificates
        //                        var certificates = new List<Certificate>();
        //                        var hocvan = GetFormattedCellValue(row.GetCell(25));
        //                        var description = GetFormattedCellValue(row.GetCell(26));
        //                        if (!string.IsNullOrEmpty(hocvan))
        //                        {
        //                            certificates.Add(
        //                                        new Certificate()
        //                                        {
        //                                            Type = hocvan,
        //                                            Description = description
        //                                        });
        //                        }
        //                        #endregion

        //                        #region StorePaper
        //                        var storePapers = new List<StorePaper>()
        //                        {
        //                            new StorePaper()
        //                            {
        //                                Type = "Bản tự khai ứng viên",
        //                                Description = string.Empty,
        //                                Count = (int)GetNumbericCellValue(row.GetCell(28)),
        //                                Unit = string.Empty
        //                            },
        //                            new StorePaper()
        //                            {
        //                                Type = "Đơn ứng tuyển",
        //                                Description = string.Empty,
        //                                Count = (int)GetNumbericCellValue(row.GetCell(29)),
        //                                Unit = string.Empty
        //                            },
        //                            new StorePaper()
        //                            {
        //                                Type = "Sơ yếu lý lịch",
        //                                Description = string.Empty,
        //                                Count = (int)GetNumbericCellValue(row.GetCell(30)),
        //                                Unit = string.Empty
        //                            },
        //                            new StorePaper()
        //                            {
        //                                Type = "Giấy khai sinh",
        //                                Description = string.Empty,
        //                                Count = (int)GetNumbericCellValue(row.GetCell(31)),
        //                                Unit = string.Empty
        //                            },
        //                            new StorePaper()
        //                            {
        //                                Type = "Chứng minh thư",
        //                                Description = string.Empty,
        //                                Count = (int)GetNumbericCellValue(row.GetCell(32)),
        //                                Unit = string.Empty
        //                            },
        //                            new StorePaper()
        //                            {
        //                                Type = "Bằng/ chứng nhận tốt nghiệp",
        //                                Description = string.Empty,
        //                                Count = (int)GetNumbericCellValue(row.GetCell(33)),
        //                                Unit = string.Empty
        //                            },
        //                            new StorePaper()
        //                            {
        //                                Type = "Bảng điểm hoặc học bạ",
        //                                Description = string.Empty,
        //                                Count = (int)GetNumbericCellValue(row.GetCell(34)),
        //                                Unit = string.Empty
        //                            },
        //                            new StorePaper()
        //                            {
        //                                Type = "Hộ khẩu",
        //                                Description = string.Empty,
        //                                Count = (int)GetNumbericCellValue(row.GetCell(35)),
        //                                Unit = string.Empty
        //                            },
        //                            new StorePaper()
        //                            {
        //                                Type = "Xác nhận nhân sự",
        //                                Description = string.Empty,
        //                                Count = (int)GetNumbericCellValue(row.GetCell(36)),
        //                                Unit = string.Empty
        //                            },
        //                            new StorePaper()
        //                            {
        //                                Type = "Ảnh",
        //                                Description = string.Empty,
        //                                Count = (int)GetNumbericCellValue(row.GetCell(37)),
        //                                Unit = string.Empty
        //                            }
        //                        };
        //                        #endregion

        //                        #region Eduction
        //                        var employeeEducations = new List<EmployeeEducation>()
        //                        {
        //                            new EmployeeEducation()
        //                            {
        //                                No = 1,
        //                                Content = GetFormattedCellValue(row.GetCell(76))
        //                            },
        //                            new EmployeeEducation()
        //                            {
        //                                No = 2,
        //                                Content = GetFormattedCellValue(row.GetCell(77))
        //                            },
        //                            new EmployeeEducation()
        //                            {
        //                                No = 3,
        //                                Content = GetFormattedCellValue(row.GetCell(78))
        //                            }
        //                            ,new EmployeeEducation()
        //                            {
        //                                No = 4,
        //                                Content = GetFormattedCellValue(row.GetCell(79))
        //                            }
        //                        };
        //                        #endregion

        //                        var builder = Builders<Employee>.Filter;
        //                        var filter = Builders<Employee>.Filter.Eq(m => m.FullName, fullName);
        //                        var update = Builders<Employee>.Update
        //                            .Set(m => m.StatusMarital, statusMarital)
        //                            .Set(m => m.Nation, nation)
        //                            .Set(m => m.Religion, religion)
        //                            .Set(m => m.Certificates, certificates)
        //                            .Set(m => m.StorePapers, storePapers)
        //                            .Set(m => m.EmployeeEducations, employeeEducations);

        //                        dbContext.Employees.UpdateOne(filter, update);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return Json(new { url = "/hr/nhan-su" });
        //}
        #endregion

        protected string GetFormattedCellValue(ICell cell)
        {
            if (cell != null)
            {
                switch (cell.CellType)
                {
                    case CellType.String:
                        return cell.StringCellValue.Trim();

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
                            return cell.NumericCellValue.ToString().Trim();
                        }

                    case CellType.Boolean:
                        return cell.BooleanCellValue ? "TRUE" : "FALSE";

                    case CellType.Formula:
                        switch (cell.CachedFormulaResultType)
                        {
                            case CellType.String:
                                return cell.StringCellValue.Trim();
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
                                    return cell.NumericCellValue.ToString().Trim();
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
                    if (cell.DateCellValue != null)
                    {
                        return cell.DateCellValue;
                    }
                    else if (DateUtil.IsCellDateFormatted(cell))
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
            if (DateTime.TryParse(date, out DateTime dt))
            {
                return dt;
            }

            return double.TryParse(date, out double oaDate) ? DateTime.FromOADate(oaDate) : DateTime.MinValue;
        }
    }
}
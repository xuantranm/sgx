using Common.Enums;
using Common.Utilities;
using Data;
using Models;
using ViewModels;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Configuration;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;
using MimeKit;


namespace xmailtimer
{
    class Program
    {
        static void Main(string[] args)
        {
            #region setting
            var location = ConfigurationSettings.AppSettings.Get("location").ToString();
            var debug = ConfigurationSettings.AppSettings.Get("debugString").ToString();
            var connection = ConfigurationSettings.AppSettings.Get("connection").ToString();
            var database = ConfigurationSettings.AppSettings.Get("database").ToString();
            #endregion

            SendTimeKeeper(location, connection, database, debug);
        }

        static void SendTimeKeeper(string location, string connection, string database, string debug)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            var url = Constants.System.domain;
            #endregion

            var congtychinhanhs = dbContext.Categories.Find(m => m.Enable.Equals(true) && m.Type.Equals((int)ECategory.Company)).ToList();
            var vanPhong = congtychinhanhs.Where(m => m.CodeInt.Equals(1)).FirstOrDefault();
            var nhaMay = congtychinhanhs.Where(m => m.CodeInt.Equals(2)).FirstOrDefault();

            #region Times : end of month
            // Default end of month. Setting in db
            var today = DateTime.Now.Date;
            int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var crawlStart = 1; // start date of month
            var crawlEnd = daysInMonth;
            var timerMonthCalculator = 0; // current base End Day of Month.
            var crawlStartE = dbContext.Settings.Find(m => m.Key.Equals("crawl-day-start")).FirstOrDefault();
            var crawlEndE = dbContext.Settings.Find(m => m.Key.Equals("crawl-day-end")).FirstOrDefault();
            var timerMonthCalculatorE = dbContext.Settings.Find(m => m.Key.Equals("timer-month-calculator")).FirstOrDefault(); // value:0,-1
            if (crawlStartE != null && !string.IsNullOrEmpty(crawlStartE.Value))
            {
                crawlStart = Convert.ToInt32(crawlStartE.Value);
            }
            if (crawlEndE != null && !string.IsNullOrEmpty(crawlStartE.Value))
            {
                crawlEnd = Convert.ToInt32(crawlEndE.Value);
            }
            if (timerMonthCalculatorE != null)
            {
                timerMonthCalculator = Convert.ToInt32(timerMonthCalculatorE.Value);
            }

            int month = today.Month;
            int year = today.Year;
            var Tu = crawlStart > crawlEnd ? new DateTime(year, month, crawlStart).AddMonths(-1) : new DateTime(year, month, crawlStart);
            var Den = crawlStart > crawlEnd ? new DateTime(Tu.AddMonths(1).Year, Tu.AddMonths(1).Month, crawlEnd) : new DateTime(Tu.Year, Tu.Month, crawlEnd);
            var ngaychot = Den.AddDays(1);
            #endregion

            #region Filter
            var builder = Builders<Employee>.Filter;
            var filter = !builder.Eq(i => i.UserName, Constants.System.account)
                        & builder.Eq(m => m.Enable, true) & builder.Eq(m => m.IsOnline, true)
                        & builder.Eq(m => m.IsTimeKeeper, true);
            //if (!string.IsNullOrEmpty(location))
            //{
            //    var locationId = location == "1" ? vanPhong.Id : nhaMay.Id;
            //    filter &= builder.Eq(m => m.CongTyChiNhanh, locationId);
            //}
            if (!string.IsNullOrEmpty(debug))
            {
                filter &= builder.Eq(m => m.Id, debug);
            }

            var fields = Builders<Employee>.Projection.Include(p => p.Id);

            var employees = dbContext.Employees.Find(filter).ToList();
            var employeesId = dbContext.Employees.Find(filter).Project<Employee>(fields).ToList().Select(m => m.Id).ToList();

            var builderT = Builders<EmployeeWorkTimeLog>.Filter;
            var filterT = builderT.Eq(m => m.Enable, true)
                        & builderT.Gte(m => m.Date, Tu)
                        & builderT.Lte(m => m.Date, Den);
            if (employeesId != null && employeesId.Count > 0)
            {
                filterT &= builderT.Where(m => employeesId.Contains(m.EmployeeId));
            }
            #endregion

            var times = dbContext.EmployeeWorkTimeLogs.Find(filterT).SortBy(m => m.Date).ToList();

            var results = new List<TimeKeeperDisplay>();

            foreach (var employee in employees)
            {
                var employeeWorkTimeLogs = times.Where(m => m.EmployeeId.Equals(employee.Id)).ToList();
                if (employeeWorkTimeLogs == null || employeeWorkTimeLogs.Count == 0) continue;

                var enrollNumber = string.Empty;
                if (employee.Workplaces != null && employee.Workplaces.Count > 0)
                {
                    foreach (var workplace in employee.Workplaces)
                    {
                        if (!string.IsNullOrEmpty(workplace.Fingerprint))
                        {
                            if (!string.IsNullOrEmpty(enrollNumber))
                            {
                                enrollNumber += ";";
                            }
                            enrollNumber += workplace.Code + ":" + workplace.Fingerprint;
                        }
                    }
                }
                results.Add(new TimeKeeperDisplay()
                {
                    EmployeeWorkTimeLogs = employeeWorkTimeLogs,
                    Id = employee.Id,
                    Code = employee.CodeOld,
                    EnrollNumber = enrollNumber,
                    FullName = employee.FullName,
                    CongTyChiNhanh = employee.CongTyChiNhanhName,
                    KhoiChucNang = employee.KhoiChucNangName,
                    PhongBan = employee.PhongBanName,
                    BoPhan = employee.BoPhanName,
                    ChucVu = employee.ChucVuName,
                    Alias = employee.AliasFullName,
                    Email = employee.Email,
                    ManageId = employee.ManagerEmployeeId
                });
            }

            // UAT : Check data
            //var nhamayCount = results.Count(m => m.CongTyChiNhanh.Equals("Nhà máy Xử lý bùn thải Sài Gòn Xanh"));

            #region Get Hrs: For case empty Email of user and manager
            var hrsCongTy = new List<Employee>();
            var employeeHr = dbContext.Employees.Find(m => m.Id.Equals("5b6bb22fe73a301f941c5887")).FirstOrDefault(); // Anh
            hrsCongTy.Add(employeeHr);
            #endregion

            #region Step 1: Employee have email
            var listEmail = results.Where(m => !string.IsNullOrEmpty(m.Email)).ToList();
            foreach (var employee in listEmail)
            {
                string sFileName = @"bang-cham-cong";
                sFileName += "-" + month + "-" + year;
                sFileName += "-" + employee.Alias + ".xlsx";

                var timers = employee.EmployeeWorkTimeLogs;
                if (timers == null || timers.Count == 0) continue;

                var displayItem = new List<TimeKeeperDisplay> { employee };
                var excelViewModel = RenderExcel(Tu, Den, ngaychot, sFileName, 1, displayItem);

                #region SCHEDULE MAIL
                Console.WriteLine("Send email to:" + employee.Email);
                var subject = "CHẤM CÔNG THÁNG " + month + "-" + year;
                var note = string.Empty;
                var owner = string.Empty;
                var to2 = employee.FullName;
                var tos = new List<EmailAddress>
                {
                    new EmailAddress { Name = employee.FullName, Address = employee.Email }
                };

                var ccs = new List<EmailAddress>();
                foreach (var item in hrsCongTy)
                {
                    ccs.Add(new EmailAddress() { Name = item.FullName, Address = item.Email });
                }

                // Because some case have enrollNumber but no time manage.
                // If ngaycongNT = 0: gui nhan su
                if (excelViewModel.NgayCongNT == 0)
                {
                    ccs = new List<EmailAddress>();
                    subject = "CHẤM CÔNG THÁNG " + month + "-" + year + " của " + employee.FullName.ToUpper();
                    to2 = string.Empty;
                    owner = string.Empty;
                    tos = new List<EmailAddress>();
                    foreach (var item in hrsCongTy)
                    {
                        tos.Add(new EmailAddress() { Name = item.FullName, Address = item.Email });
                    }
                    note = "<b>Anh/chị nhận được email này do nhân viên " + employee.FullName + " không có dữ liệu chấm công. Xin lỗi về bất tiện này và vui lòng kiểm tra lại.</b><br />";
                }

                var attachments = new List<string>
                {
                    excelViewModel.FileNameFullPath
                };

                var pathToFile = @"C:\Projects\App.Schedule\Templates\bangchamcong.html";
                var bodyBuilder = new BodyBuilder();
                using (StreamReader SourceReader = File.OpenText(pathToFile))
                {
                    bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                }
                string messageBody = string.Format(bodyBuilder.HtmlBody,
                    subject,
                    note,
                    " anh/chị " + to2,
                    month + "-" + year + owner,
                    "28/" + month.ToString("00") + "/" + year,
                    excelViewModel.NgayCongNT,
                    excelViewModel.NgayNghiP,
                    url + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Index,
                    url + "/" + Constants.LinkLeave.Main + "/" + Constants.LinkLeave.Index,
                    url);

                var emailMessage = new EmailMessage()
                {
                    ToAddresses = tos,
                    CCAddresses = ccs,
                    Subject = subject,
                    BodyContent = messageBody,
                    Type = "bang-cham-cong",
                    EmployeeId = employee.Id,
                    Attachments = attachments
                };

                ScheduleMail(connection, database, emailMessage);
                #endregion
            }
            #endregion

            #region Step 2: Employee miss email: Have manager or none.
            var listEmptyEmail = results.Where(m => string.IsNullOrEmpty(m.Email)).ToList();
            var groupManager = (from s in listEmptyEmail
                                group s by new
                                {
                                    s.ManageId
                                }
                                                    into l
                                select new
                                {
                                    l.Key.ManageId,
                                    list = l.ToList(),
                                }).ToList();
            foreach (var manager in groupManager)
            {
                var listControl = manager.list;

                if (!string.IsNullOrEmpty(manager.ManageId))
                {
                    var managerE = dbContext.Employees.Find(m => m.Enable.Equals(true) && m.Leave.Equals(false) && m.Id.Equals(manager.ManageId)).FirstOrDefault();
                    if (managerE != null && !string.IsNullOrEmpty(managerE.Email))
                    {
                        string sFileName = @"bang-cham-cong";
                        sFileName += "-" + month + "-" + year;
                        sFileName += "-nhan-vien-cua-" + managerE.AliasFullName + ".xlsx";

                        var excelViewModel = RenderExcel(Tu, Den, ngaychot, sFileName, 2, listControl);

                        Console.WriteLine("Send email to:" + managerE.Email);
                        var subject = "CHẤM CÔNG THÁNG " + month + "-" + year + " CỦA NHÂN VIÊN ĐANG QUẢN LÝ";
                        var note = "<b>Xin lỗi về bất tiện này. Anh/chị nhận được email này do đang quản lý danh sách nhân viên (chưa có thông tin email). Vui lòng xem công và liên hệ nhân viên liên quan.</b><br />";
                        var to2 = managerE.FullName;
                        var tos = new List<EmailAddress>
                        {
                            new EmailAddress { Name = managerE.FullName, Address = managerE.Email }
                        };

                        var ccs = new List<EmailAddress>();
                        ccs = new List<EmailAddress>();
                        foreach (var item in hrsCongTy)
                        {
                            ccs.Add(new EmailAddress() { Name = item.FullName, Address = item.Email });
                        }

                        var attachments = new List<string>
                        {
                            excelViewModel.FileNameFullPath
                        };

                        var pathToFile = @"C:\Projects\App.Schedule\Templates\bangchamcongnhom.html";
                        var bodyBuilder = new BodyBuilder();
                        using (StreamReader SourceReader = File.OpenText(pathToFile))
                        {
                            bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                        }
                        string messageBody = string.Format(bodyBuilder.HtmlBody,
                            subject,
                            note,
                            " anh/chị " + to2,
                            month + "-" + year,
                            "28/" + month.ToString("00") + "/" + year,
                            url + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Index,
                            url + "/" + Constants.LinkLeave.Main + "/" + Constants.LinkLeave.Index,
                            url);

                        var emailMessage = new EmailMessage()
                        {
                            ToAddresses = tos,
                            CCAddresses = ccs,
                            Subject = subject,
                            BodyContent = messageBody,
                            Type = "bang-cham-cong-nhom",
                            EmployeeId = managerE.Id,
                            Attachments = attachments
                        };

                        ScheduleMail(connection, database, emailMessage);
                    }
                }
                else
                {
                    // Gui HR
                    var sFileName = @"bang-cham-cong";
                    sFileName += "-" + month + "-" + year;
                    sFileName += "-nhan-vien-khac.xlsx";
                    var subject = "CHẤM CÔNG THÁNG " + month + "-" + year;
                    subject += " CỦA NHÂN VIÊN KHÁC";
                    var note = "<b>Xin lỗi về bất tiện này. Anh/chị nhận được email này do đang quản lý danh sách nhân viên (chưa có thông tin email và chưa có người quản lý trực tiếp). Vui lòng xem công và liên hệ nhân viên liên quan.</b><br />";
                    var to2 = string.Empty;
                    var tos = new List<EmailAddress>();
                    foreach (var hrEmployee in hrsCongTy)
                    {
                        to2 += string.IsNullOrEmpty(to2) ? hrEmployee.FullName : ", " + hrEmployee.FullName;
                        tos.Add(new EmailAddress { Name = hrEmployee.FullName, Address = hrEmployee.Email });
                    }

                    var excelViewModel = RenderExcel(Tu, Den, ngaychot, sFileName, 2, listControl);

                    var attachments = new List<string>
                    {
                        excelViewModel.FileNameFullPath
                    };

                    var pathToFile = @"C:\Projects\App.Schedule\Templates\bangchamcongnhom.html";
                    var bodyBuilder = new BodyBuilder();
                    using (StreamReader SourceReader = File.OpenText(pathToFile))
                    {
                        bodyBuilder.HtmlBody = SourceReader.ReadToEnd();
                    }
                    string messageBody = string.Format(bodyBuilder.HtmlBody,
                        subject,
                        note,
                        " anh/chị " + to2,
                        month + "-" + year,
                        "28/" + month.ToString("00") + "/" + year,
                        url + "/" + Constants.LinkTimeKeeper.Main + "/" + Constants.LinkTimeKeeper.Index,
                        url + "/" + Constants.LinkLeave.Main + "/" + Constants.LinkLeave.Index,
                        url);

                    var emailMessage = new EmailMessage()
                    {
                        ToAddresses = tos,
                        Subject = subject,
                        BodyContent = messageBody,
                        Type = "bang-cham-cong-nhom",
                        EmployeeId = string.Empty,
                        Attachments = attachments
                    };

                    ScheduleMail(connection, database, emailMessage);
                }
            }
            #endregion
        }

        static ExcelViewModel RenderExcel(DateTime Tu, DateTime Den, DateTime ngaychot, string sFileName, int mode, List<TimeKeeperDisplay> list)
        {
            double ngayCongNT = 0;
            double ngayNghiP = 0;
            var root = @"C:\Projects\App.Schedule";
            string exportFolder = Path.Combine(root, "exports", "timers", ngaychot.ToString("yyyyMMdd"));
            FileInfo file = new FileInfo(Path.Combine(exportFolder, sFileName));
            file.Directory.Create(); // If the directory already exists, this method does nothing.
            using (var fs = new FileStream(Path.Combine(exportFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();

                #region Styling
                Utility.StyleExcel(workbook, out ICellStyle styleDedault, out ICellStyle styleDot, out ICellStyle styleTitle, out ICellStyle styleSubTitle, out ICellStyle styleHeader, out ICellStyle styleDedaultMerge, out ICellStyle styleBold);
                #endregion

                ISheet sheet1 = workbook.CreateSheet("Cong");

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
                cell.SetCellValue("BẢNG THỐNG KÊ CHẤM CÔNG");
                cell.CellStyle = styleTitle;
                rowIndex++;

                #region Header
                row = sheet1.CreateRow(rowIndex);
                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 9);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Từ ngày " + Tu.ToString("dd/MM/yyyy") + " đến ngày " + Den.ToString("dd/MM/yyyy"));
                cell.CellStyle = styleSubTitle;
                rowIndex++;
                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("STT");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã NV");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Họ tên");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Chức vụ");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Mã chấm công");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue(string.Empty);
                cell.CellStyle = styleHeader;
                columnIndex++;

                for (DateTime date = Tu; date <= Den; date = date.AddDays(1))
                {
                    cell = row.CreateCell(columnIndex);
                    cell.SetCellValue(date.Day);
                    cell.CellStyle = styleHeader;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày công");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Lễ tết");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Vào trễ");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex = columnIndex + 1;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ra sớm");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex = columnIndex + 1;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 2);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Tăng ca (giờ)");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                columnIndex = columnIndex + 2;
                columnIndex++;

                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex, columnIndex, columnIndex + 1);
                sheet1.AddMergedRegion(cellRangeAddress);
                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("Ngày nghỉ");
                cell.CellStyle = styleHeader;
                RegionUtil.SetBorderTop((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderRight((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);
                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, cellRangeAddress, sheet1, workbook);

                rowIndex++;

                row = sheet1.CreateRow(rowIndex);
                columnIndex = 6;
                for (DateTime date = Tu; date <= Den; date = date.AddDays(1.0))
                {
                    cell = row.CreateCell(columnIndex); // cell B1
                    cell.SetCellValue(Constants.DayOfWeekT2(date));
                    cell.CellStyle = styleHeader;
                    columnIndex++;
                }

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("");
                columnIndex++;

                cell = row.CreateCell(columnIndex, CellType.String);
                cell.SetCellValue("");
                columnIndex++;

                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Lần");
                cell.CellStyle = styleHeader;
                columnIndex++;
                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Phút");
                cell.CellStyle = styleHeader;
                columnIndex++;
                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Lần");
                cell.CellStyle = styleHeader;
                columnIndex++;
                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Phút");
                cell.CellStyle = styleHeader;
                columnIndex++;
                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Ngày thường");
                cell.CellStyle = styleHeader;
                columnIndex++;
                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Chủ nhật");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("Lễ tết");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("P");
                cell.CellStyle = styleHeader;
                columnIndex++;

                cell = row.CreateCell(columnIndex);
                cell.SetCellValue("KP");
                cell.CellStyle = styleHeader;
                columnIndex++;

                rowIndex++;
                #endregion

                var order = 1;
                foreach (var employee in list)
                {
                    var timesSort = employee.EmployeeWorkTimeLogs.OrderBy(m => m.Date).ToList();
                    var vaoTreLan = 0;
                    int vaoTrePhut = 0;
                    var raSomLan = 0;
                    int raSomPhut = 0;
                    double otNormalReal = 0;
                    double otSundayReal = 0;
                    double otHolidayReal = 0;
                    double tangCaNgayThuong = 0;
                    double tangCaChuNhat = 0;
                    double tangCaLeTet = 0;
                    double vangKP = 0;
                    double letet = 0;

                    var rowEF = rowIndex;
                    var rowET = rowIndex + 6;

                    row = sheet1.CreateRow(rowIndex);

                    columnIndex = 0;
                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(order);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(employee.Code);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(employee.FullName);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(employee.ChucVu);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue(employee.EnrollNumber);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cell = row.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Vào 1");
                    cell.CellStyle = styleDedault;

                    var rowout1 = sheet1.CreateRow(rowIndex + 1);
                    var rowin2 = sheet1.CreateRow(rowIndex + 2);
                    var rowout2 = sheet1.CreateRow(rowIndex + 3);
                    var rowlate = sheet1.CreateRow(rowIndex + 4);
                    var rowearly = sheet1.CreateRow(rowIndex + 5);
                    var rowreason = sheet1.CreateRow(rowIndex + 6);

                    cell = rowout1.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Ra 1");
                    cell.CellStyle = styleDedault;

                    cell = rowin2.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Vào 2");
                    cell.CellStyle = styleDedault;

                    cell = rowout2.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Ra 2");
                    cell.CellStyle = styleDedault;

                    cell = rowlate.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Trễ (phút)");
                    cell.CellStyle = styleDedault;

                    cell = rowearly.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Sớm (phút)");
                    cell.CellStyle = styleDedault;

                    cell = rowreason.CreateCell(columnIndex, CellType.String);
                    cell.SetCellValue("Công|Xác nhận-Lý do");
                    cell.CellStyle = styleDedault;
                    columnIndex++;

                    for (DateTime date = Tu; date <= Den; date = date.AddDays(1.0))
                    {
                        var item = timesSort.Where(m => m.Date.Equals(date)).FirstOrDefault();
                        if (item != null)
                        {
                            var analytic = Utility.TimerAnalytics(item, true);
                            var dayString = string.Empty;
                            var displayInOut = string.Empty;
                            var noilamviec = !string.IsNullOrEmpty(item.WorkplaceCode) ? item.WorkplaceCode : string.Empty;
                            var reason = !string.IsNullOrEmpty(item.Reason) ? item.Reason : string.Empty;
                            var detail = !string.IsNullOrEmpty(item.ReasonDetail) ? item.ReasonDetail : string.Empty;
                            var statusBag = item.StatusTangCa == (int)ETangCa.TuChoi ? "badge-pill" : "badge-info";
                            var giotangcathucte = Math.Round(item.OtThucTeD, 2);
                            var phuttangcathucte = Math.Round(giotangcathucte * 60, 0);
                            var giotangcaxacnhan = Math.Round(item.OtXacNhanD, 2);
                            int late = 0;
                            int early = 0;

                            var isMiss = analytic.Miss;
                            item.WorkDay = analytic.Workday;
                            late = analytic.Late;
                            early = analytic.Early;
                            displayInOut = analytic.DisplayInOut;

                            ngayCongNT += analytic.Workday;
                            ngayNghiP += analytic.NgayNghiP;
                            letet += analytic.LeTet;
                            vaoTreLan += analytic.VaoTreLan;
                            vaoTrePhut += analytic.Late;
                            raSomLan += analytic.RaSomLan;
                            raSomPhut += analytic.Early;
                            otNormalReal += analytic.OtNormalReal;
                            otSundayReal += analytic.OtSundayReal;
                            otHolidayReal += analytic.OtHolidayReal;
                            tangCaNgayThuong += analytic.TangCaNgayThuong;
                            tangCaChuNhat += analytic.TangCaChuNhat;
                            tangCaLeTet += analytic.TangCaLeTet;

                            dayString = item.WorkDay + " ngày";
                            if (item.Mode == (int)ETimeWork.Sunday || item.Mode == (int)ETimeWork.Holiday)
                            {
                                if (item.WorkTime.TotalHours > 0)
                                {
                                    dayString = Math.Round(item.WorkTime.TotalHours, 2) + " giờ";
                                }
                            }

                            var displayLate = string.Empty;
                            var displayEarly = string.Empty;

                            if (item.Logs == null)
                            {
                                var text = item.Reason;
                                if (item.Mode == (int)ETimeWork.Normal)
                                {
                                    text += string.IsNullOrEmpty(text) ? Constants.TimeKeeper(item.Status) : ";" + Constants.TimeKeeper(item.Status);
                                    text += string.IsNullOrEmpty(text) ? item.ReasonDetail : ";" + item.ReasonDetail;
                                }
                                cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                                sheet1.AddMergedRegion(cellRangeAddress);
                                cell = row.CreateCell(columnIndex, CellType.String);
                                cell.SetCellValue(text);
                                cell.CellStyle = styleDedaultMerge;
                                var rowCellRangeAddress = new CellRangeAddress(rowEF, rowET, columnIndex, columnIndex);
                                sheet1.AddMergedRegion(rowCellRangeAddress);
                                RegionUtil.SetBorderTop((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                                RegionUtil.SetBorderLeft((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                                RegionUtil.SetBorderRight((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                                RegionUtil.SetBorderBottom((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                            }
                            else
                            {
                                var displayIn1 = string.Empty;
                                var displayIn2 = string.Empty;
                                var displayOut1 = string.Empty;
                                var displayOut2 = string.Empty;
                                var displayReason = string.Empty;
                                if (item.In.HasValue)
                                {
                                    displayIn1 = item.In.ToString();
                                }
                                if (item.Out.HasValue)
                                {
                                    displayOut1 = item.Out.ToString();
                                }

                                cell = row.CreateCell(columnIndex, CellType.String);
                                cell.SetCellValue(displayIn1);
                                cell.CellStyle = styleDot;

                                cell = rowout1.CreateCell(columnIndex, CellType.String);
                                cell.SetCellValue(displayOut1);
                                cell.CellStyle = styleDedault;

                                cell = rowin2.CreateCell(columnIndex, CellType.String);
                                cell.SetCellValue(displayIn2);
                                if (!string.IsNullOrEmpty(displayIn2))
                                {
                                    cell.CellStyle = styleDot;
                                }
                                else
                                {
                                    cell.CellStyle = styleDedault;
                                }

                                cell = rowout2.CreateCell(columnIndex, CellType.String);
                                cell.SetCellValue(displayOut2);
                                cell.CellStyle = styleDedault;

                                cell = rowlate.CreateCell(columnIndex, CellType.String);
                                if (late > 0)
                                {
                                    displayLate = late.ToString();
                                }
                                cell.SetCellValue(displayLate);
                                cell.CellStyle = styleDedault;

                                cell = rowearly.CreateCell(columnIndex, CellType.String);
                                if (early > 0)
                                {
                                    displayEarly = early.ToString();
                                }
                                cell.SetCellValue(displayEarly);
                                cell.CellStyle = styleDedault;

                                cell = rowreason.CreateCell(columnIndex, CellType.String);
                                var detailText = dayString;
                                if (item.Status > (int)EStatusWork.DuCong)
                                {
                                    detailText += ";" + Constants.TimeKeeper(item.Status);
                                }
                                if (item.Mode != (int)ETimeWork.Normal && item.WorkDay < 1)
                                {
                                    detailText += ";" + Constants.WorkTimeMode(item.Mode);
                                    detailText += ":" + item.SoNgayNghi;
                                }
                                if (!string.IsNullOrEmpty(detail))
                                {
                                    detailText += ";" + detail;
                                }
                                cell.SetCellValue(detailText);
                                cell.CellStyle = styleDedault;
                            }

                            columnIndex++;
                        }
                        else
                        {
                            cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                            sheet1.AddMergedRegion(cellRangeAddress);
                            cell = row.CreateCell(columnIndex, CellType.String);
                            cell.SetCellValue(Constants.NA);
                            cell.CellStyle = styleDedaultMerge;
                            var rowCellRangeAddress = new CellRangeAddress(rowEF, rowET, columnIndex, columnIndex);
                            sheet1.AddMergedRegion(rowCellRangeAddress);
                            RegionUtil.SetBorderTop((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                            RegionUtil.SetBorderLeft((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                            RegionUtil.SetBorderRight((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                            RegionUtil.SetBorderBottom((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                            columnIndex++;
                        }
                    }

                    var columnIndexF = columnIndex;
                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(Math.Round(ngayCongNT, 2));
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(letet);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(vaoTreLan);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(vaoTrePhut);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(raSomLan);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(raSomPhut);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(Math.Round(tangCaNgayThuong, 2));
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(Math.Round(tangCaChuNhat, 2));
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(Math.Round(tangCaLeTet, 2));
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(ngayNghiP);
                    cell.CellStyle = styleDedaultMerge;
                    columnIndex++;

                    cellRangeAddress = new CellRangeAddress(rowIndex, rowIndex + 6, columnIndex, columnIndex);
                    sheet1.AddMergedRegion(cellRangeAddress);
                    cell = row.CreateCell(columnIndex, CellType.Numeric);
                    cell.SetCellValue(vangKP);
                    cell.CellStyle = styleDedaultMerge;

                    var columnIndexT = columnIndex;
                    columnIndex++;

                    rowIndex += 6;
                    rowIndex++;
                    order++;
                    #region fix border
                    for (var i = 0; i < 5; i++)
                    {
                        var rowCellRangeAddress = new CellRangeAddress(rowEF, rowET, i, i);
                        sheet1.AddMergedRegion(rowCellRangeAddress);
                        RegionUtil.SetBorderTop((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderLeft((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderRight((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderBottom((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                    }
                    for (var y = columnIndexF; y <= columnIndexT; y++)
                    {
                        var rowCellRangeAddress = new CellRangeAddress(rowEF, rowET, y, y);
                        sheet1.AddMergedRegion(rowCellRangeAddress);
                        RegionUtil.SetBorderTop((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderLeft((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderRight((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                        RegionUtil.SetBorderBottom((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                    }
                    #endregion
                }

                #region fix border
                var rowF = 7;
                var rowT = 8;
                for (var i = 0; i < 6; i++)
                {
                    var rowCellRangeAddress = new CellRangeAddress(rowF, rowT, i, i);
                    sheet1.AddMergedRegion(rowCellRangeAddress);
                    RegionUtil.SetBorderTop((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                    RegionUtil.SetBorderLeft((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                    RegionUtil.SetBorderRight((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                    RegionUtil.SetBorderBottom((int)BorderStyle.Thin, rowCellRangeAddress, sheet1, workbook);
                }
                #endregion

                workbook.Write(fs);
            }

            return new ExcelViewModel()
            {
                FileNameFullPath = Path.Combine(exportFolder, sFileName),
                NgayCongNT = ngayCongNT,
                NgayNghiP = ngayNghiP
            };
        }

        static void ScheduleMail(string connection, string database, EmailMessage emailMessage)
        {
            #region Connection, Setting & Filter
            MongoDBContext.ConnectionString = connection;
            MongoDBContext.DatabaseName = database;
            MongoDBContext.IsSSL = true;
            MongoDBContext dbContext = new MongoDBContext();
            #endregion

            var scheduleEmail = new ScheduleEmail
            {
                Status = (int)EEmailStatus.TimerMonth,
                To = emailMessage.ToAddresses,
                CC = emailMessage.CCAddresses,
                BCC = emailMessage.BCCAddresses,
                Type = emailMessage.Type,
                Title = emailMessage.Subject,
                Content = emailMessage.BodyContent,
                Attachments = emailMessage.Attachments,
                EmployeeId = emailMessage.EmployeeId
            };

            dbContext.ScheduleEmails.InsertOne(scheduleEmail);
        }
    }
}

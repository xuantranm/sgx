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

namespace erp.Controllers
{
    [Authorize]
    [Route(Constants.LinkSalary.Main)]
    public class SalaryController : Controller
    {
        readonly MongoDBContext dbContext = new MongoDBContext();
        private readonly IDistributedCache _cache;
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SalaryController(IDistributedCache cache, IConfiguration configuration, IHostingEnvironment env, ILogger<SalaryController> logger)
        {
            _cache = cache;
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        // The luong cua nhan vien
        [Route(Constants.LinkSalary.Index)]
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

        // Current month: base employee. Because if new employee will apply.
        //      Save data () each month by Hr salary people.
        //          Save dynamic information.
        // If previous month. use data in collection "SalaryEmployeeMonths"
        [Route(Constants.LinkSalary.BangLuongReal)]
        public async Task<IActionResult> BangLuongReal(string thang)
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            // override times if null
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Year;
            var month = toDate.Month;

            var thamsoEntity = dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToList();
            var mauSo = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-lam-viec")).Value);
            var mauSoBaoVe = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-bao-ve")).Value);
            var tyledongbh = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ty-le-dong-bh")).Value);
            decimal ngayConglamViec = Utility.BusinessDaysUntil(fromDate, toDate);
            decimal phutLamViec = ngayConglamViec * 8 * 60;

            var calBHXH = true;
            if (DateTime.Now.Day < 26 && DateTime.Now.Month == month)
            {
                calBHXH = false;
            }

            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true) & !m.UserName.Equals(Constants.System.account)).ToListAsync();
            var salaryEmployeeMonths = new List<SalaryEmployeeMonth>();
            foreach (var employee in employees)
            {
                var currentSalary = await dbContext.SalaryEmployeeMonths.Find(m => m.FlagReal.Equals(true) & m.EmployeeId.Equals(employee.Id) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefaultAsync();
                var existInformation = currentSalary != null ? true : false;

                var thamnienlamviec = employee.Joinday;
                var dateSpan = DateTimeSpan.CompareDates(thamnienlamviec, DateTime.Now);
                int bac = 1;
                decimal luongCB = 0;
                if (!existInformation)
                {
                    // Nếu [SalaryThangBangLuong] thay đổi. (chưa tạo lịch sử).
                    // Cập nhật [SalaryThangBacLuongEmployees]
                    // Mỗi tháng 1 record [SalaryThangBacLuongEmployees]
                    // Get lastest information base year, month.
                    var level = dbContext.SalaryThangBacLuongEmployees
                        .Find(m => m.EmployeeId.Equals(employee.Id) & m.FlagReal.Equals(true) & m.Enable.Equals(true) 
                        & m.Year.Equals(year) & m.Month.Equals(month))
                        .FirstOrDefault();
                    if (level != null)
                    {
                        bac = level.Bac;
                        luongCB = level.MucLuong;
                    }
                    else
                    {
                        // Get lastest
                        var lastLevel = await dbContext.SalaryThangBacLuongEmployees
                        .Find(m => m.EmployeeId.Equals(employee.Id) & m.FlagReal.Equals(true) & m.Enable.Equals(true))
                        .SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();

                        if (lastLevel != null)
                        {
                            var salaryThangBangLuong = await dbContext.SalaryThangBangLuongs.Find(m => m.ViTriCode.Equals(lastLevel.ViTriCode) & m.Bac.Equals(lastLevel.Bac)).FirstOrDefaultAsync();
                            if (salaryThangBangLuong != null)
                            {
                                dbContext.SalaryThangBacLuongEmployees.InsertOne(new SalaryThangBacLuongEmployee()
                                {
                                    Year = year,
                                    Month = month,
                                    EmployeeId = employee.Id,
                                    ViTriCode = salaryThangBangLuong.ViTriCode,
                                    Bac = salaryThangBangLuong.Bac,
                                    MucLuong = salaryThangBangLuong.MucLuong
                                });

                                bac = salaryThangBangLuong.Bac;
                                luongCB = salaryThangBangLuong.MucLuong;
                            }
                        }
                    }
                }
                else
                {
                    bac = currentSalary.Bac;
                    luongCB = currentSalary.LuongCanBan;
                }

                decimal nangnhoc = 0;
                decimal trachnhiem = 0;
                decimal thamnien = 0;
                decimal thuhut = 0;
                decimal dienthoai = 0;
                decimal xang = 0;
                decimal com = 0;
                decimal nhao = 0;
                decimal kiemnhiem = 0;
                decimal bhytdacbiet = 0;
                decimal vitricanknnhieunam = 0;
                decimal vitridacthu = 0;
                decimal luongcbbaogomphucap = 0;
                if (!existInformation)
                {
                    var phucapphuclois = dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.EmployeeId.Equals(employee.Id) & m.Law.Equals(false)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).ToList();
                    if (phucapphuclois.Find(m => m.Code.Equals("01-001")) != null)
                    {
                        nangnhoc = phucapphuclois.Find(m => m.Code.Equals("01-001")).Money;
                    }
                    if (phucapphuclois.Find(m => m.Code.Equals("01-002")) != null)
                    {
                        trachnhiem = phucapphuclois.Find(m => m.Code.Equals("01-002")).Money;
                    }
                    if (phucapphuclois.Find(m => m.Code.Equals("01-004")) != null)
                    {
                        thuhut = phucapphuclois.Find(m => m.Code.Equals("01-004")).Money;
                    }
                    if (phucapphuclois.Find(m => m.Code.Equals("02-001")) != null)
                    {
                        xang = phucapphuclois.Find(m => m.Code.Equals("02-001")).Money;
                    }
                    if (phucapphuclois.Find(m => m.Code.Equals("02-002")) != null)
                    {
                        dienthoai = phucapphuclois.Find(m => m.Code.Equals("02-002")).Money;
                    }
                    if (phucapphuclois.Find(m => m.Code.Equals("02-003")) != null)
                    {
                        com = phucapphuclois.Find(m => m.Code.Equals("02-003")).Money;
                    }
                    if (phucapphuclois.Find(m => m.Code.Equals("02-004")) != null)
                    {
                        kiemnhiem = phucapphuclois.Find(m => m.Code.Equals("02-004")).Money;
                    }
                    if (phucapphuclois.Find(m => m.Code.Equals("02-005")) != null)
                    {
                        bhytdacbiet = phucapphuclois.Find(m => m.Code.Equals("02-005")).Money;
                    }
                    if (phucapphuclois.Find(m => m.Code.Equals("02-006")) != null)
                    {
                        vitricanknnhieunam = phucapphuclois.Find(m => m.Code.Equals("02-006")).Money;
                    }
                    if (phucapphuclois.Find(m => m.Code.Equals("02-007")) != null)
                    {
                        vitridacthu = phucapphuclois.Find(m => m.Code.Equals("02-007")).Money;
                    }
                    if (phucapphuclois.Find(m => m.Code.Equals("02-008")) != null)
                    {
                        nhao = phucapphuclois.Find(m => m.Code.Equals("02-008")).Money;
                    }
                }
                else
                {
                    nangnhoc = currentSalary.NangNhocDocHai;
                    trachnhiem = currentSalary.TrachNhiem;
                    thuhut = currentSalary.ThuHut;
                    dienthoai = currentSalary.DienThoai;
                    xang = currentSalary.Xang;
                    com = currentSalary.Com;
                    nhao = currentSalary.NhaO;
                    kiemnhiem = currentSalary.KiemNhiem;
                    bhytdacbiet = currentSalary.BhytDacBiet;
                    vitricanknnhieunam = currentSalary.ViTriCanKnNhieuNam;
                    vitridacthu = currentSalary.ViTriDacThu;
                }
                if (dateSpan.Years >= 3)
                {
                    thamnien = luongCB * Convert.ToDecimal(0.03 + (dateSpan.Years - 3) * 0.01);
                }
                luongcbbaogomphucap = luongCB + nangnhoc + trachnhiem + thamnien + thuhut + dienthoai + xang + com + nhao + kiemnhiem + bhytdacbiet + vitricanknnhieunam + vitridacthu;
                decimal ngayNghiPhepHuongLuong = 0;
                decimal ngayNghiLeTetHuongLuong = 0;
                decimal congCNGio = 0;
                decimal congTangCaNgayThuongGio = 0;
                decimal congLeTet = 0;
                var chamCong = await dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(employee.Id) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefaultAsync();
                if (chamCong != null)
                {
                    ngayConglamViec = (decimal)chamCong.Workday;
                    phutLamViec = (decimal)chamCong.WorkTime;
                    ngayNghiPhepHuongLuong = (decimal)chamCong.NgayNghiHuongLuong;
                    ngayNghiLeTetHuongLuong = (decimal)chamCong.NgayNghiLeTetHuongLuong;
                    congCNGio = (decimal)chamCong.CongCNGio;
                    congTangCaNgayThuongGio = (decimal)chamCong.CongTangCaNgayThuongGio;
                    congLeTet = (decimal)chamCong.CongLeTet;
                }

                decimal congTacXa = 0;
                decimal tongBunBoc = 0;
                decimal thanhTienBunBoc = 0;
                decimal mucDatTrongThang = 0;
                decimal luongTheoDoanhThuDoanhSo = 0;
                var logisticData = await dbContext.SalaryLogisticDatas.Find(m => m.EmployeeId.Equals(employee.Id) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefaultAsync();
                var saleData = await dbContext.SalarySaleKPIs.Find(m => m.EmployeeId.Equals(employee.Id) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefaultAsync();
                if (logisticData != null)
                {
                    congTacXa = logisticData.CongTacXa;
                    tongBunBoc = logisticData.KhoiLuongBun;
                    thanhTienBunBoc = logisticData.ThanhTienBun;
                    mucDatTrongThang = logisticData.TongSoChuyen;
                    luongTheoDoanhThuDoanhSo = logisticData.TienChuyen;
                }
                if (saleData != null)
                {
                    luongTheoDoanhThuDoanhSo += saleData.TongThuong;
                }

                decimal luongKhac = 0;
                decimal thiDua = 0;
                decimal hoTroNgoaiLuong = 0;
                decimal luongthamgiabhxh = 0;
                decimal thuongletet = 0;
                if (existInformation)
                {
                    luongKhac = currentSalary.LuongKhac;
                    thiDua = currentSalary.ThiDua;
                    hoTroNgoaiLuong = currentSalary.HoTroNgoaiLuong;
                    luongthamgiabhxh = currentSalary.LuongThamGiaBHXH;
                    thuongletet = currentSalary.ThuongLeTet;
                }
                else
                {
                    var lastBhxh = await dbContext.SalaryEmployeeMonths.Find(m => m.FlagReal.Equals(true) & m.EmployeeId.Equals(employee.Id)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
                    if (lastBhxh != null)
                    {
                        luongthamgiabhxh = lastBhxh.LuongThamGiaBHXH;
                    }
                }

                mauSo = employee.SalaryMauSo != 26 ? 30 : 26;
                decimal tongthunhap = luongcbbaogomphucap / mauSo * (ngayConglamViec + congCNGio / 8 * 2 + congTangCaNgayThuongGio / 8 * (decimal)1.5 + congLeTet * 3)
                                    + luongCB / mauSo * (ngayNghiPhepHuongLuong + ngayNghiLeTetHuongLuong)
                                    + congTacXa + luongTheoDoanhThuDoanhSo + thanhTienBunBoc + luongKhac + thiDua + hoTroNgoaiLuong;

                decimal bhxhbhyt = luongthamgiabhxh * tyledongbh;
                // Du thang moi dong bh
                if (!calBHXH)
                {
                    bhxhbhyt = 0;
                }
                
                decimal tamung = 0;
                var creditData = await dbContext.SalaryCredits.Find(m => m.EmployeeId.Equals(employee.Id) & !m.Status.Equals(2)).FirstOrDefaultAsync();
                if (creditData != null)
                {
                    tamung = creditData.MucThanhToanHangThang;
                }
                decimal thuclanh = tongthunhap - bhxhbhyt - tamung + thuongletet;

                var salary = new SalaryEmployeeMonth()
                {
                    Year = year,
                    Month = month,
                    EmployeeId = employee.Id,
                    MaNhanVien = employee.CodeOld,
                    FullName = employee.FullName,
                    NoiLamViec = employee.SalaryNoiLamViec,
                    PhongBan = employee.SalaryPhongBan,
                    ChucVu = employee.SalaryChucVu,
                    ViTriCode = employee.SalaryChucVuViTriCode,
                    ThamNienLamViec = thamnienlamviec,
                    ThamNienYear = dateSpan.Years,
                    ThamNienMonth = dateSpan.Months,
                    ThamNienDay = dateSpan.Days,
                    Bac = bac,
                    LuongCanBan = luongCB,
                    NangNhocDocHai = nangnhoc,
                    TrachNhiem = trachnhiem,
                    ThamNien = thamnien,
                    ThuHut = thuhut,
                    Xang = xang,
                    DienThoai = dienthoai,
                    Com = com,
                    NhaO = nhao,
                    KiemNhiem = kiemnhiem,
                    BhytDacBiet = bhytdacbiet,
                    ViTriCanKnNhieuNam = vitricanknnhieunam,
                    ViTriDacThu = vitridacthu,
                    LuongCoBanBaoGomPhuCap = luongcbbaogomphucap,
                    NgayCongLamViec = ngayConglamViec,
                    NgayNghiPhepHuongLuong = ngayNghiPhepHuongLuong,
                    NgayNghiLeTetHuongLuong = ngayNghiLeTetHuongLuong,
                    CongCNGio = congCNGio,
                    CongTangCaNgayThuongGio = congTangCaNgayThuongGio,
                    CongLeTet = congLeTet,
                    CongTacXa = congTacXa,
                    MucDatTrongThang = mucDatTrongThang,
                    LuongTheoDoanhThuDoanhSo = luongTheoDoanhThuDoanhSo,
                    TongBunBoc = tongBunBoc,
                    ThanhTienBunBoc = thanhTienBunBoc,
                    LuongKhac = luongKhac,
                    ThiDua = thiDua,
                    HoTroNgoaiLuong = hoTroNgoaiLuong,
                    TongThuNhap = tongthunhap,
                    BHXHBHYT = bhxhbhyt,
                    LuongThamGiaBHXH = luongthamgiabhxh,
                    TamUng = tamung,
                    ThuongLeTet = thuongletet,
                    ThucLanh = thuclanh,
                    MauSo = mauSo,
                    FlagReal = true,
                    NoiLamViecOrder = employee.SalaryNoiLamViecOrder,
                    PhongBanOrder = employee.SalaryPhongBanOrder,
                    ChucVuOrder = employee.SalaryChucVuOrder
                };
                salaryEmployeeMonths.Add(salary);
                // Save automatic. For access faster,...
                // No update because dynamic information
                //  update in Update Form
                if (!existInformation)
                {
                    dbContext.SalaryEmployeeMonths.InsertOne(salary);
                }
            }

            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = salaryEmployeeMonths,
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                MonthYears = sortTimes,
                thang = thang,
                calBHXH = calBHXH
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.BangLuongReal + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> BangLuongRealUpdate(string thang)
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
            var date = new DateTime(2018, 08, 01);
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            // If exist, update, no => create 1 month
            var dataTime = sortTimes[0];
            if (!string.IsNullOrEmpty(thang))
            {
                dataTime = new MonthYear
                {
                    Month = Convert.ToInt32(thang.Split("-")[0]),
                    Year = Convert.ToInt32(thang.Split("-")[1]),
                };
            }

            var employeeMonths = new List<SalaryEmployeeMonth>();

            var employeeMonthsTemp = dbContext.SalaryEmployeeMonths.Find(m => m.Year.Equals(dataTime.Year) & m.Month.Equals(dataTime.Month) & m.Enable.Equals(true) & m.FlagReal.Equals(true)).ToList();
            var thamsoEntity = await dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToListAsync();
            var mauSo = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-lam-viec")).Value);
            var mauSoBaoVe = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-bao-ve")).Value);
            var tyledongbh = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ty-le-dong-bh")).Value);

            if (employeeMonthsTemp.Count == 0)
            {
                // Create new data
                var employess = await dbContext.Employees.Find(m => m.Enable.Equals(true) & !m.UserName.Equals(Constants.System.account)).ToListAsync();
                foreach (var employee in employess)
                {
                    var employeeId = employee.Id;
                    var dateSpan = DateTimeSpan.CompareDates(employee.Joinday, DateTime.Now);
                    var bacEntity = await dbContext.SalaryThangBacLuongEmployees.Find(m => m.Enable.Equals(true) & m.ViTriCode.Equals(employee.SalaryChucVuViTriCode)).FirstOrDefaultAsync();
                    var phucapEntity = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.EmployeeId.Equals(employeeId)).ToListAsync();
                    var chamCong = await dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(employeeId) & m.Year.Equals(dataTime.Year) & m.Month.Equals(dateSpan.Months)).FirstOrDefaultAsync();
                    // Get lastest data
                    var logisticData = await dbContext.SalaryLogisticDatas.Find(m => m.EmployeeId.Equals(employeeId)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();

                    // Get lastest data
                    var saleData = await dbContext.SalarySaleKPIs.Find(m => m.EmployeeId.Equals(employeeId)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();

                    // Get lastest data
                    var creditData = await dbContext.SalaryCredits.Find(m => m.EmployeeId.Equals(employeeId) & !m.Status.Equals(2)).FirstOrDefaultAsync(); ;

                    decimal luongCB = 0;
                    if (bacEntity != null)
                    {
                        luongCB = bacEntity.MucLuong;
                    }

                    var chucVuCode = employee.SalaryChucVuViTriCode;
                    decimal nangNhoc = 0;
                    if (phucapEntity.Find(m => m.Code.Equals("01-001")) != null)
                    {
                        nangNhoc = phucapEntity.Find(m => m.Code.Equals("01-001")).Money;
                    }
                    decimal trachNhiem = 0;
                    if (phucapEntity.Find(m => m.Code.Equals("01-002")) != null)
                    {
                        trachNhiem = phucapEntity.Find(m => m.Code.Equals("01-002")).Money;
                    }
                    decimal thamNiem = 0;
                    if (dateSpan.Years >= 3)
                    {
                        thamNiem = luongCB * Convert.ToDecimal(0.03 + (dateSpan.Years - 3) * 0.01);
                    }
                    decimal thuHut = 0;
                    if (phucapEntity.Find(m => m.Code.Equals("01-004")) != null)
                    {
                        thuHut = phucapEntity.Find(m => m.Code.Equals("01-004")).Money;
                    }
                    decimal Xang = 0;
                    if (phucapEntity.Find(m => m.Code.Equals("02-001")) != null)
                    {
                        Xang = phucapEntity.Find(m => m.Code.Equals("02-001")).Money;
                    }
                    decimal DienThoai = 0;
                    if (phucapEntity.Find(m => m.Code.Equals("02-002")) != null)
                    {
                        DienThoai = phucapEntity.Find(m => m.Code.Equals("02-002")).Money;
                    }
                    decimal Com = 0;
                    if (phucapEntity.Find(m => m.Code.Equals("02-003")) != null)
                    {
                        Com = phucapEntity.Find(m => m.Code.Equals("02-003")).Money;
                    }
                    decimal KiemNhiem = 0;
                    if (phucapEntity.Find(m => m.Code.Equals("02-004")) != null)
                    {
                        KiemNhiem = phucapEntity.Find(m => m.Code.Equals("02-004")).Money;
                    }
                    decimal BHYTDacBiet = 0;
                    if (phucapEntity.Find(m => m.Code.Equals("02-005")) != null)
                    {
                        BHYTDacBiet = phucapEntity.Find(m => m.Code.Equals("02-005")).Money;
                    }
                    decimal viTriCanNhieuNamKinhNghiem = 0;
                    if (phucapEntity.Find(m => m.Code.Equals("02-006")) != null)
                    {
                        viTriCanNhieuNamKinhNghiem = phucapEntity.Find(m => m.Code.Equals("02-006")).Money;
                    }
                    decimal viTriDacThu = 0;
                    if (phucapEntity.Find(m => m.Code.Equals("02-007")) != null)
                    {
                        viTriDacThu = phucapEntity.Find(m => m.Code.Equals("02-007")).Money;
                    }

                    decimal luongCanBanBaoGomPhuCap = luongCB + nangNhoc + trachNhiem + thamNiem + thuHut + Xang + DienThoai + Com + KiemNhiem + BHYTDacBiet + viTriCanNhieuNamKinhNghiem + viTriDacThu;

                    // get ngay cong tu cham cong, neu ko thuoc cham cong, lay ngay lam viec.
                    var toDate = Utility.WorkingMonthToDate(string.Empty);
                    var fromDate = toDate.AddMonths(-1).AddDays(1);
                    decimal ngayConglamViec = Utility.BusinessDaysUntil(fromDate, toDate);
                    decimal ngayNghiPhepHuongLuong = 0;
                    decimal ngayNghiLeTetHuongLuong = 0;
                    decimal congCNGio = 0;
                    decimal congTangCaNgayThuongGio = 0;
                    decimal congLeTet = 0;
                    if (chamCong != null)
                    {
                        ngayConglamViec = (decimal)chamCong.Workday;
                        ngayNghiPhepHuongLuong = (decimal)chamCong.NgayNghiHuongLuong;
                        ngayNghiLeTetHuongLuong = (decimal)chamCong.NgayNghiLeTetHuongLuong;
                    }

                    decimal congTacXa = 0;
                    decimal tongBunBoc = 0;
                    decimal thanhTienBunBoc = 0;
                    decimal mucDatTrongThang = 0;
                    decimal luongTheoDoanhThuDoanhSo = 0;
                    if (logisticData != null)
                    {
                        // Logistic & Sale
                        congTacXa = logisticData.CongTacXa;
                        tongBunBoc = logisticData.KhoiLuongBun;
                        thanhTienBunBoc = logisticData.ThanhTienBun;
                        mucDatTrongThang = logisticData.TongSoChuyen;
                        luongTheoDoanhThuDoanhSo = logisticData.TienChuyen;
                    }

                    if (saleData != null)
                    {
                        // Sale
                        luongTheoDoanhThuDoanhSo = saleData.TongThuong;
                    }

                    decimal luongKhac = 0;
                    // If Logistics top doanh thu + 1t. Update later
                    decimal thiDua = 0;
                    decimal hoTroNgoaiLuong = 0;
                    //luongcbbaogomphucap/thamso* (ngayconglamviec+congCNgio/8*2+congtangcangaythuonggio/8*1.5+congletet*3)
                    //+luongCB/thamso *(ngaynghiphephuongluong+NgàynghỉLễTếthưởnglương)
                    //+congtacxa+doanhthu+thanhtienbocbun+luongkhac+thidua+hotrongoailuong
                    decimal tongThuNhap = luongCanBanBaoGomPhuCap / mauSo * (ngayConglamViec + congCNGio / 8 * 2 + congTangCaNgayThuongGio / 8 * (decimal)1.5 + congLeTet * 3)
                                        + luongCB / mauSo * (ngayNghiLeTetHuongLuong + ngayNghiLeTetHuongLuong)
                                        + congTacXa + luongTheoDoanhThuDoanhSo + thanhTienBunBoc + luongKhac + thiDua + hoTroNgoaiLuong;

                    decimal luongThamGiaBHXH = 0;
                    decimal bHXHBHYT = luongThamGiaBHXH * tyledongbh;

                    decimal tamUng = 0;
                    if (creditData != null)
                    {
                        tamUng = creditData.MucThanhToanHangThang;
                    }
                    decimal thuongLeTet = 0;
                    //=ROUND(AK18-AL18-AN18+AO18,-1)
                    decimal thucLanh = tongThuNhap - bHXHBHYT - tamUng + thuongLeTet;

                    // Get luong, phu cap, phuc loi....
                    try
                    {
                        var newSalaryEmployeeMonth = new SalaryEmployeeMonth
                        {
                            Year = dataTime.Year,
                            Month = dataTime.Month,
                            EmployeeId = employee.Id,
                            MaNhanVien = employee.CodeOld,
                            FullName = employee.FullName,
                            NoiLamViec = employee.SalaryNoiLamViec,
                            PhongBan = employee.SalaryPhongBan,
                            ChucVu = employee.SalaryChucVu,
                            ViTriCode = employee.SalaryChucVuViTriCode,
                            ThamNienLamViec = employee.Joinday,
                            ThamNienYear = dateSpan.Years,
                            ThamNienMonth = dateSpan.Months,
                            ThamNienDay = dateSpan.Days,
                            Bac = bacEntity.Bac,
                            LuongCanBan = bacEntity.MucLuong,
                            NangNhocDocHai = nangNhoc,
                            TrachNhiem = trachNhiem,
                            ThamNien = thamNiem,
                            ThuHut = thuHut,
                            Xang = Xang,
                            DienThoai = DienThoai,
                            Com = Com,
                            NhaO = 0,
                            KiemNhiem = KiemNhiem,
                            BhytDacBiet = BHYTDacBiet,
                            ViTriCanKnNhieuNam = viTriCanNhieuNamKinhNghiem,
                            ViTriDacThu = viTriDacThu,
                            LuongCoBanBaoGomPhuCap = luongCanBanBaoGomPhuCap,
                            NgayCongLamViec = ngayConglamViec,
                            NgayNghiPhepHuongLuong = ngayNghiPhepHuongLuong,
                            NgayNghiLeTetHuongLuong = ngayNghiLeTetHuongLuong,
                            CongCNGio = congCNGio,
                            CongTangCaNgayThuongGio = congTangCaNgayThuongGio,
                            CongLeTet = congLeTet,
                            CongTacXa = congTacXa,
                            MucDatTrongThang = mucDatTrongThang,
                            LuongTheoDoanhThuDoanhSo = luongTheoDoanhThuDoanhSo,
                            TongBunBoc = tongBunBoc,
                            ThanhTienBunBoc = thanhTienBunBoc,
                            LuongKhac = luongKhac,
                            ThiDua = thiDua,
                            HoTroNgoaiLuong = hoTroNgoaiLuong,
                            TongThuNhap = tongThuNhap,
                            BHXHBHYT = bHXHBHYT,
                            LuongThamGiaBHXH = luongThamGiaBHXH,
                            TamUng = tamUng,
                            ThuongLeTet = thuongLeTet,
                            ThucLanh = thucLanh,
                            MauSo = 26
                        };
                        employeeMonths.Add(newSalaryEmployeeMonth);
                    }
                    catch (Exception ex)
                    {

                    }

                }

                dbContext.SalaryEmployeeMonths.InsertMany(employeeMonths);
            }
            else
            {
                employeeMonths = employeeMonthsTemp;
            }

            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = employeeMonths,
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                SalaryThangBangLuongReals = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Law.Equals(false)).ToListAsync(),
                SalaryThangBangPhuCapPhucLoisReal = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false)).ToListAsync(),
                MonthYears = sortTimes
            };

            return View(viewModel);
        }

        // Only update, create new at Init
        [HttpPost]
        [Route(Constants.LinkSalary.BangLuongReal + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> BangLuongRealUpdate(BangLuongViewModel viewModel)
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

            try
            {
                var now = DateTime.Now;
                var models = viewModel.SalaryEmployeeMonths;
                foreach (var item in models)
                {
                    var dateSpan = DateTimeSpan.CompareDates(item.ThamNienLamViec, DateTime.Now);

                    var builder = Builders<SalaryEmployeeMonth>.Filter;
                    var filter = builder.Eq(m => m.Id, item.Id);
                    var update = Builders<SalaryEmployeeMonth>.Update
                        .Set(m => m.ThamNienLamViec, item.ThamNienLamViec)
                        .Set(m => m.ThamNienYear, dateSpan.Years)
                        .Set(m => m.ThamNienMonth, dateSpan.Months)
                        .Set(m => m.ThamNienDay, dateSpan.Days)
                        //.Set(m => m.Bac, bacEntity.Bac)
                        .Set(m => m.LuongCanBan, item.LuongCanBan)
                        .Set(m => m.NangNhocDocHai, item.NangNhocDocHai)
                        .Set(m => m.TrachNhiem, item.TrachNhiem)
                        .Set(m => m.ThamNien, item.ThamNien)
                        .Set(m => m.ThuHut, item.ThuHut)
                        .Set(m => m.Xang, item.Xang)
                        .Set(m => m.DienThoai, item.DienThoai)
                        .Set(m => m.Com, item.Com)
                        .Set(m => m.NhaO, 0)
                        .Set(m => m.KiemNhiem, item.KiemNhiem)
                        .Set(m => m.BhytDacBiet, item.BhytDacBiet)
                        .Set(m => m.ViTriCanKnNhieuNam, item.ViTriCanKnNhieuNam)
                        .Set(m => m.ViTriDacThu, item.ViTriDacThu)
                        .Set(m => m.LuongCoBanBaoGomPhuCap, item.LuongCoBanBaoGomPhuCap)
                        .Set(m => m.NgayCongLamViec, item.NgayCongLamViec)
                        .Set(m => m.NgayNghiPhepHuongLuong, item.NgayNghiPhepHuongLuong)
                        .Set(m => m.NgayNghiLeTetHuongLuong, item.NgayNghiLeTetHuongLuong)
                        .Set(m => m.CongCNGio, item.CongCNGio)
                        .Set(m => m.CongTangCaNgayThuongGio, item.CongTangCaNgayThuongGio)
                        .Set(m => m.CongLeTet, item.CongLeTet)
                        .Set(m => m.CongTacXa, item.CongTacXa)
                        .Set(m => m.MucDatTrongThang, item.MucDatTrongThang)
                        .Set(m => m.LuongTheoDoanhThuDoanhSo, item.LuongTheoDoanhThuDoanhSo)
                        .Set(m => m.TongBunBoc, item.TongBunBoc)
                        .Set(m => m.ThanhTienBunBoc, item.ThanhTienBunBoc)
                        .Set(m => m.LuongKhac, item.LuongKhac)
                        .Set(m => m.ThiDua, item.ThiDua)
                        .Set(m => m.HoTroNgoaiLuong, item.HoTroNgoaiLuong)
                        .Set(m => m.TongThuNhap, item.TongThuNhap)
                        .Set(m => m.BHXHBHYT, item.BHXHBHYT)
                        .Set(m => m.LuongThamGiaBHXH, item.LuongThamGiaBHXH)
                        .Set(m => m.TamUng, item.TamUng)
                        .Set(m => m.ThuongLeTet, item.ThuongLeTet)
                        .Set(m => m.ThucLanh, item.ThucLanh)
                        .Set(m => m.MauSo, 26)
                        .Set(m => m.UpdatedOn, now);
                    dbContext.SalaryEmployeeMonths.UpdateOne(filter, update);
                }
                return Json(new { result = true, source = "update", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "update", id = string.Empty, message = ex.Message });
            }
        }

        [Route(Constants.LinkSalary.TheLuong)]
        public async Task<IActionResult> TheLuong(string thang)
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

            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = await dbContext.SalaryEmployeeMonths.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true)).ToListAsync(),
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                SalaryThangBangLuongReals = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Law.Equals(false)).ToListAsync(),
                SalaryThangBangPhuCapPhucLoisReal = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false)).ToListAsync()
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.ThangBangLuongReal)]
        public async Task<IActionResult> ThangBangLuongReal()
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

            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                SalaryThangBangLuongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Law.Equals(false)).ToListAsync(),
                SalaryThangBangPhuCapPhucLoisReal = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false)).ToListAsync()
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.ThangBangLuongReal + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> ThangBangLuongRealUpdate()
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

            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                SalaryThangBangLuongs = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Law.Equals(false)).ToListAsync(),
                SalaryThangBangPhuCapPhucLoisReal = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false)).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.ThangBangLuongReal + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> ThangBangLuongRealUpdate(ThangBangLuongViewModel viewModel)
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

            try
            {
                var now = DateTime.Now;

                #region ToiThieuVung
                var salaryMucLuongVung = viewModel.SalaryMucLuongVung;
                var builderSalaryMucLuongVung = Builders<SalaryMucLuongVung>.Filter;
                var filterSalaryMucLuongVung = builderSalaryMucLuongVung.Eq(m => m.Id, salaryMucLuongVung.Id);
                var updateSalaryMucLuongVung = Builders<SalaryMucLuongVung>.Update
                    //.Set(m => m.ToiThieuVungQuiDinh, salaryMucLuongVung.ToiThieuVungQuiDinh)
                    .Set(m => m.ToiThieuVungDoanhNghiepApDung, salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung)
                    .Set(m => m.UpdatedOn, now);
                dbContext.SalaryMucLuongVungs.UpdateOne(filterSalaryMucLuongVung, updateSalaryMucLuongVung);
                #endregion

                #region SalaryThangBangLuongReal
                decimal salaryMin = salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung;
                var salaryThangBangLuongs = viewModel.SalaryThangBangLuongs;
                var groups = (from a in salaryThangBangLuongs
                              group a by new
                              {
                                  a.ViTriCode
                              }
                                                    into b
                              select new
                              {
                                  b.Key.ViTriCode,
                                  Salaries = b.ToList()
                              }).ToList();

                int maxLevel = 10;
                foreach (var group in groups)
                {
                    var id = group.Salaries[0].Id;
                    if (!string.IsNullOrEmpty(id))
                    {
                        var vitriCode = group.ViTriCode;
                        var vitri = group.Salaries[0].ViTri;
                        var vitriAlias = group.Salaries[0].ViTriAlias;
                        var salaryDeclareTax = group.Salaries[0].MucLuong;
                        var heso = group.Salaries[0].HeSo;
                        if (salaryDeclareTax == 0)
                        {
                            salaryDeclareTax = salaryMin;
                        }
                        for (int lv = 0; lv <= maxLevel; lv++)
                        {
                            if (lv > 1)
                            {
                                salaryDeclareTax = heso * salaryDeclareTax;
                            }
                            var exist = dbContext.SalaryThangBangLuongs.CountDocuments(m => m.ViTriCode.Equals(vitriCode) & m.Bac.Equals(lv) & m.FlagReal.Equals(true));
                            if (exist > 0)
                            {
                                var builderSalaryThangBangLuong = Builders<SalaryThangBangLuong>.Filter;
                                var filterSalaryThangBangLuong = builderSalaryThangBangLuong.Eq(m => m.ViTriCode, vitriCode);
                                filterSalaryThangBangLuong = filterSalaryThangBangLuong & builderSalaryThangBangLuong.Eq(m => m.Bac, lv);
                                var updateSalaryThangBangLuong = Builders<SalaryThangBangLuong>.Update
                                    .Set(m => m.MucLuong, salaryDeclareTax)
                                    .Set(m => m.HeSo, heso)
                                    .Set(m => m.UpdatedOn, now);
                                dbContext.SalaryThangBangLuongs.UpdateOne(filterSalaryThangBangLuong, updateSalaryThangBangLuong);
                            }
                            else
                            {
                                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                                {
                                    ViTri = vitriCode,
                                    Bac = lv,
                                    HeSo = heso,
                                    MucLuong = salaryDeclareTax,
                                    ViTriCode = vitriCode,
                                    ViTriAlias = vitriAlias,
                                    Law = false,
                                    FlagReal = true
                                });
                            }
                        }
                    }
                    else
                    {
                        // Insert NEW VI TRI
                        var vitri = group.Salaries[0].ViTri;
                        if (!string.IsNullOrEmpty(vitri))
                        {
                            var vitriAlias = Utility.AliasConvert(group.Salaries[0].ViTri);
                            string vitriLastCode = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true)).SortByDescending(m => m.ViTriCode).FirstOrDefault().ViTriCode;
                            int newCode = Convert.ToInt32(vitriLastCode.Split('-')[1]) + 1;
                            string newCodeFull = Constants.System.viTriCodeTBLuong + newCode.ToString("000");
                            var salaryDeclareTax = group.Salaries[0].MucLuong;
                            var heso = group.Salaries[0].HeSo;

                            if (salaryDeclareTax == 0)
                            {
                                salaryDeclareTax = salaryMin;
                            }
                            for (int lv = 1; lv <= 10; lv++)
                            {
                                if (lv > 1)
                                {
                                    salaryDeclareTax = heso * salaryDeclareTax;
                                }
                                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                                {
                                    ViTri = vitri,
                                    Bac = lv,
                                    HeSo = heso,
                                    MucLuong = salaryDeclareTax,
                                    ViTriCode = newCodeFull,
                                    ViTriAlias = vitriAlias,
                                    Law = false,
                                    FlagReal = true
                                });
                            }
                        }
                    }
                }
                #endregion

                return Json(new { result = true, source = "update", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "update", id = string.Empty, message = ex.Message });
            }
        }

        // LAWS - BAO CAO THUE
        // AUTOMATIC DATA, BASE EMPLOYEES
        [Route(Constants.LinkSalary.BangLuongLaw)]
        public async Task<IActionResult> BangLuongLaw(string thang)
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            var toDate = Utility.WorkingMonthToDate(thang);
            var fromDate = toDate.AddMonths(-1).AddDays(1);
            // override times if null
            if (string.IsNullOrEmpty(thang))
            {
                toDate = DateTime.Now;
                fromDate = new DateTime(toDate.AddMonths(-1).Year, toDate.AddMonths(-1).Month, 26);
            }
            var year = toDate.Year;
            var month = toDate.Month;

            var thamsoEntity = dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToList();
            var mauSo = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-lam-viec")).Value);
            var mauSoBaoVe = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-bao-ve")).Value);
            var tyledongbh = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("ty-le-dong-bh")).Value);
            decimal ngayConglamViec = Utility.BusinessDaysUntil(fromDate, toDate);
            decimal phutLamViec = ngayConglamViec * 8 * 60;

            var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true) & !m.UserName.Equals(Constants.System.account)).ToListAsync();
            var salaryEmployeeMonths = new List<SalaryEmployeeMonth>();
            foreach (var employee in employees)
            {
                var currentSalary = await dbContext.SalaryEmployeeMonths.Find(m => m.FlagReal.Equals(false) & m.EmployeeId.Equals(employee.Id) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefaultAsync();
                var existInformation = currentSalary != null ? true : false;

                var thamnienlamviec = employee.Joinday;
                var dateSpan = DateTimeSpan.CompareDates(thamnienlamviec, DateTime.Now);
                int bac = 1;
                decimal luongCB = 0;
                if (!existInformation)
                {
                    // Get lastest information base year, month.
                    var level = dbContext.SalaryEmployeeMonths.Find(m => m.EmployeeId.Equals(employee.Id) & m.FlagReal.Equals(true) & m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefault();
                    if (level != null)
                    {
                        bac = level.Bac;
                        luongCB = level.LuongThamGiaBHXH - level.TrachNhiem;
                    }
                }
                else
                {
                    bac = currentSalary.Bac;
                    luongCB = currentSalary.LuongCanBan;
                }
                // debug
                if (employee.Id == "5b6bb22fe73a301f941c5884")
                {
                    var a = "aa";
                }
                if (luongCB > 0)
                {
                    decimal nangnhoc = 0;
                    decimal trachnhiem = 0;
                    decimal thamnien = 0;
                    decimal thuhut = 0;
                    decimal dienthoai = 0;
                    decimal xang = 0;
                    decimal com = 0;
                    decimal nhao = 0;
                    decimal kiemnhiem = 0;
                    decimal bhytdacbiet = 0;
                    decimal vitricanknnhieunam = 0;
                    decimal vitridacthu = 0;
                    decimal luongcbbaogomphucap = 0;
                    if (!existInformation)
                    {
                        var phucapphuclois = dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.MaSo.Equals(employee.SalaryMaSoChucDanhCongViec)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).ToList();

                        if (phucapphuclois.Find(m => m.Code.Equals("01-001")) != null)
                        {
                            nangnhoc = phucapphuclois.Find(m => m.Code.Equals("01-001")).Money;
                        }
                        if (phucapphuclois.Find(m => m.Code.Equals("01-002")) != null)
                        {
                            trachnhiem = phucapphuclois.Find(m => m.Code.Equals("01-002")).Money;
                        }
                        if (phucapphuclois.Find(m => m.Code.Equals("01-004")) != null)
                        {
                            thuhut = phucapphuclois.Find(m => m.Code.Equals("01-004")).Money;
                        }
                        if (phucapphuclois.Find(m => m.Code.Equals("02-001")) != null)
                        {
                            xang = phucapphuclois.Find(m => m.Code.Equals("02-001")).Money;
                        }
                        if (phucapphuclois.Find(m => m.Code.Equals("02-002")) != null)
                        {
                            dienthoai = phucapphuclois.Find(m => m.Code.Equals("02-002")).Money;
                        }
                        if (phucapphuclois.Find(m => m.Code.Equals("02-003")) != null)
                        {
                            com = phucapphuclois.Find(m => m.Code.Equals("02-003")).Money;
                        }
                        if (phucapphuclois.Find(m => m.Code.Equals("02-004")) != null)
                        {
                            kiemnhiem = phucapphuclois.Find(m => m.Code.Equals("02-004")).Money;
                        }
                        if (phucapphuclois.Find(m => m.Code.Equals("02-005")) != null)
                        {
                            bhytdacbiet = phucapphuclois.Find(m => m.Code.Equals("02-005")).Money;
                        }
                        if (phucapphuclois.Find(m => m.Code.Equals("02-006")) != null)
                        {
                            vitricanknnhieunam = phucapphuclois.Find(m => m.Code.Equals("02-006")).Money;
                        }
                        if (phucapphuclois.Find(m => m.Code.Equals("02-007")) != null)
                        {
                            vitridacthu = phucapphuclois.Find(m => m.Code.Equals("02-007")).Money;
                        }
                        if (phucapphuclois.Find(m => m.Code.Equals("02-008")) != null)
                        {
                            nhao = phucapphuclois.Find(m => m.Code.Equals("02-008")).Money;
                        }
                    }
                    else
                    {
                        nangnhoc = currentSalary.NangNhocDocHai;
                        trachnhiem = currentSalary.TrachNhiem;
                        thuhut = currentSalary.ThuHut;
                        dienthoai = currentSalary.DienThoai;
                        xang = currentSalary.Xang;
                        com = currentSalary.Com;
                        nhao = currentSalary.NhaO;
                        kiemnhiem = currentSalary.KiemNhiem;
                        bhytdacbiet = currentSalary.BhytDacBiet;
                        vitricanknnhieunam = currentSalary.ViTriCanKnNhieuNam;
                        vitridacthu = currentSalary.ViTriDacThu;
                    }
                    //if (dateSpan.Years >= 3)
                    //{
                    //    thamnien = luongCB * Convert.ToDecimal(0.03 + (dateSpan.Years - 3) * 0.01);
                    //}

                    luongcbbaogomphucap = luongCB + nangnhoc + trachnhiem + thamnien + thuhut + dienthoai + xang + com + nhao + kiemnhiem + bhytdacbiet + vitricanknnhieunam + vitridacthu;
                    decimal ngayNghiPhepHuongLuong = 0;
                    decimal ngayNghiLeTetHuongLuong = 0;
                    decimal congCNGio = 0;
                    decimal congTangCaNgayThuongGio = 0;
                    decimal congLeTet = 0;
                    var chamCong = await dbContext.EmployeeWorkTimeMonthLogs.Find(m => m.EmployeeId.Equals(employee.Id) & m.Year.Equals(year) & m.Month.Equals(month)).FirstOrDefaultAsync();
                    if (chamCong != null)
                    {
                        ngayConglamViec = (decimal)chamCong.Workday;
                        phutLamViec = (decimal)chamCong.WorkTime;
                        ngayNghiPhepHuongLuong = (decimal)chamCong.NgayNghiHuongLuong;
                        ngayNghiLeTetHuongLuong = (decimal)chamCong.NgayNghiLeTetHuongLuong;
                        congCNGio = (decimal)chamCong.CongCNGio;
                        congTangCaNgayThuongGio = (decimal)chamCong.CongTangCaNgayThuongGio;
                        congLeTet = (decimal)chamCong.CongLeTet;
                    }

                    decimal congTacXa = 0;
                    decimal tongBunBoc = 0;
                    decimal thanhTienBunBoc = 0;
                    decimal mucDatTrongThang = 0;
                    decimal luongTheoDoanhThuDoanhSo = 0;
                    decimal luongKhac = 0;
                    decimal thiDua = 0;
                    decimal hoTroNgoaiLuong = 0;
                    decimal luongthamgiabhxh = luongCB;
                    decimal thuongletet = 0;
                    if (existInformation)
                    {
                        luongKhac = currentSalary.LuongKhac;
                        thiDua = currentSalary.ThiDua;
                        hoTroNgoaiLuong = currentSalary.HoTroNgoaiLuong;
                        //luongthamgiabhxh = luongCB;
                        thuongletet = currentSalary.ThuongLeTet;
                    }

                    mauSo = employee.SalaryMauSo != 26 ? 30 : 26;
                    decimal tongthunhap = luongcbbaogomphucap / mauSo * (ngayConglamViec + congCNGio / 8 * 2 + congTangCaNgayThuongGio / 8 * (decimal)1.5 + congLeTet * 3)
                                        + luongCB / mauSo * (ngayNghiPhepHuongLuong + ngayNghiLeTetHuongLuong)
                                        + congTacXa + luongTheoDoanhThuDoanhSo + thanhTienBunBoc + luongKhac + thiDua + hoTroNgoaiLuong;


                    decimal bhxhbhyt = luongthamgiabhxh * tyledongbh;
                    // Du thang moi dong bh
                    //if (DateTime.Now.Day < 26 && DateTime.Now.Month == month)
                    //{
                    //    bhxhbhyt = 0;
                    //}

                    decimal tamung = 0;
                    decimal thuclanh = tongthunhap - bhxhbhyt - tamung + thuongletet;

                    var salary = new SalaryEmployeeMonth()
                    {
                        Year = year,
                        Month = month,
                        EmployeeId = employee.Id,
                        MaNhanVien = employee.CodeOld,
                        FullName = employee.FullName,
                        NoiLamViec = employee.SalaryNoiLamViec,
                        PhongBan = employee.SalaryPhongBan,
                        ChucVu = employee.SalaryChucVu,
                        ViTriCode = employee.SalaryChucVuViTriCode,
                        ThamNienLamViec = thamnienlamviec,
                        ThamNienYear = dateSpan.Years,
                        ThamNienMonth = dateSpan.Months,
                        ThamNienDay = dateSpan.Days,
                        Bac = bac,
                        LuongCanBan = luongCB,
                        NangNhocDocHai = nangnhoc,
                        TrachNhiem = trachnhiem,
                        ThamNien = thamnien,
                        ThuHut = thuhut,
                        Xang = xang,
                        DienThoai = dienthoai,
                        Com = com,
                        NhaO = nhao,
                        KiemNhiem = kiemnhiem,
                        BhytDacBiet = bhytdacbiet,
                        ViTriCanKnNhieuNam = vitricanknnhieunam,
                        ViTriDacThu = vitridacthu,
                        LuongCoBanBaoGomPhuCap = luongcbbaogomphucap,
                        NgayCongLamViec = ngayConglamViec,
                        NgayNghiPhepHuongLuong = ngayNghiPhepHuongLuong,
                        NgayNghiLeTetHuongLuong = ngayNghiLeTetHuongLuong,
                        CongCNGio = congCNGio,
                        CongTangCaNgayThuongGio = congTangCaNgayThuongGio,
                        CongLeTet = congLeTet,
                        CongTacXa = congTacXa,
                        MucDatTrongThang = mucDatTrongThang,
                        LuongTheoDoanhThuDoanhSo = luongTheoDoanhThuDoanhSo,
                        TongBunBoc = tongBunBoc,
                        ThanhTienBunBoc = thanhTienBunBoc,
                        LuongKhac = luongKhac,
                        ThiDua = thiDua,
                        HoTroNgoaiLuong = hoTroNgoaiLuong,
                        TongThuNhap = tongthunhap,
                        BHXHBHYT = bhxhbhyt,
                        LuongThamGiaBHXH = luongthamgiabhxh,
                        TamUng = tamung,
                        ThuongLeTet = thuongletet,
                        ThucLanh = thuclanh,
                        MauSo = mauSo,
                        FlagReal = false
                    };
                    salaryEmployeeMonths.Add(salary);
                    // Save automatic. For access faster,...
                    // No update because dynamic information
                    //  update in Update Form
                    if (!existInformation)
                    {
                        dbContext.SalaryEmployeeMonths.InsertOne(salary);
                    }
                }
            }

            var viewModel = new BangLuongViewModel
            {
                SalaryEmployeeMonths = salaryEmployeeMonths,
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                MonthYears = sortTimes,
                thang = thang
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.ThangBangLuongLaw)]
        public async Task<IActionResult> ThangBangLuongLaw()
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

            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                SalaryThangBangLuongLaws = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false) & m.Law.Equals(true)).ToListAsync(),
                SalaryThangBangPhuCapPhucLois = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false)).ToListAsync()
            };

            return View(viewModel);
        }

        [Route(Constants.LinkSalary.ThangBangLuongLaw + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> ThangBangLuongLawUpdate()
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

            var viewModel = new ThangBangLuongViewModel
            {
                SalaryMucLuongVung = await dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).FirstOrDefaultAsync(),
                SalaryThangBangLuongLaws = await dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false) & m.Law.Equals(true)).ToListAsync(),
                SalaryThangBangPhuCapPhucLois = await dbContext.SalaryThangBangPhuCapPhucLois.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(false)).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.ThangBangLuongLaw + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> ThangBangLuongLawUpdate(ThangBangLuongViewModel viewModel)
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

            try
            {
                var now = DateTime.Now;

                #region ToiThieuVung
                var salaryMucLuongVung = viewModel.SalaryMucLuongVung;
                var builderSalaryMucLuongVung = Builders<SalaryMucLuongVung>.Filter;
                var filterSalaryMucLuongVung = builderSalaryMucLuongVung.Eq(m => m.Id, salaryMucLuongVung.Id);
                var updateSalaryMucLuongVung = Builders<SalaryMucLuongVung>.Update
                    .Set(m => m.ToiThieuVungQuiDinh, salaryMucLuongVung.ToiThieuVungQuiDinh)
                    .Set(m => m.ToiThieuVungDoanhNghiepApDung, salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung)
                    .Set(m => m.UpdatedOn, now);
                dbContext.SalaryMucLuongVungs.UpdateOne(filterSalaryMucLuongVung, updateSalaryMucLuongVung);
                #endregion

                #region SalaryThangBangLuongLaws
                decimal salaryMin = salaryMucLuongVung.ToiThieuVungDoanhNghiepApDung;
                var salaryThangBangLuongLaws = viewModel.SalaryThangBangLuongLaws;
                var groups = (from a in salaryThangBangLuongLaws
                              group a by new
                              {
                                  a.MaSo
                              }
                                                    into b
                              select new
                              {
                                  MaSoName = b.Key.MaSo,
                                  Salaries = b.ToList(),
                              }).ToList();

                foreach (var group in groups)
                {
                    // reset salaryDeclareTax;
                    var salaryDeclareTax = salaryMin;
                    if (group.Salaries[0].MucLuong > 0)
                    {
                        salaryDeclareTax = group.Salaries[0].MucLuong;
                    }
                    foreach (var level in group.Salaries)
                    {
                        // bac 1 set manual
                        if (level.Bac > 1)
                        {
                            salaryDeclareTax = level.HeSo * salaryDeclareTax;
                        }
                        var builderSalaryThangBangLuong = Builders<SalaryThangBangLuong>.Filter;
                        var filterSalaryThangBangLuong = builderSalaryThangBangLuong.Eq(m => m.Id, level.Id);
                        var updateSalaryThangBangLuong = Builders<SalaryThangBangLuong>.Update
                            .Set(m => m.MucLuong, salaryDeclareTax)
                            .Set(m => m.HeSo, level.HeSo)
                            .Set(m => m.UpdatedOn, now);
                        dbContext.SalaryThangBangLuongs.UpdateOne(filterSalaryThangBangLuong, updateSalaryThangBangLuong);
                    }
                }
                #endregion

                #region SalaryThangBangPhuCapPhucLois

                foreach (var phucap in viewModel.SalaryThangBangPhuCapPhucLois)
                {
                    // Update if id not null
                    if (!string.IsNullOrEmpty(phucap.Id))
                    {
                        var builderSalaryThangBangPhuCapPhucLoi = Builders<SalaryThangBangPhuCapPhucLoi>.Filter;
                        var filterSalaryThangBangPhuCapPhucLoi = builderSalaryThangBangPhuCapPhucLoi.Eq(m => m.Id, phucap.Id);
                        var updateSalaryThangBangPhuCapPhucLoi = Builders<SalaryThangBangPhuCapPhucLoi>.Update
                            .Set(m => m.Money, phucap.Money)
                            .Set(m => m.UpdatedOn, now);
                        dbContext.SalaryThangBangPhuCapPhucLois.UpdateOne(filterSalaryThangBangPhuCapPhucLoi, updateSalaryThangBangPhuCapPhucLoi);
                    }
                    else
                    {
                        var phucapInformation = dbContext.SalaryPhuCapPhucLois.Find(m => m.Code.Equals(phucap.Code)).FirstOrDefault();
                        if (phucapInformation != null)
                        {
                            phucap.Name = phucapInformation.Name;
                        }
                        dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(phucap);
                    }
                }
                #endregion

                // PROCCESSING

                #region Activities
                // Update multi, insert multi
                string s = JsonConvert.SerializeObject(viewModel.SalaryThangBangLuongLaws);
                var activity = new TrackingUser
                {
                    UserId = login,
                    Function = Constants.Collection.SalaryThangBangLuong,
                    Action = Constants.Action.Create,
                    Value = s,
                    Content = Constants.Action.Create,
                };
                await dbContext.TrackingUsers.InsertOneAsync(activity);
                #endregion

                return Json(new { result = true, source = "create", message = "Thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, source = "create", id = string.Empty, message = ex.Message });
            }
        }

        #region NHA MAY

        #endregion

        #region SUB DATA (SALES, LOGISTICS,...)
        [Route(Constants.LinkSalary.Setting + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SettingUpdate()
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

            var viewModel = new BangLuongViewModel
            {
                SalarySettings = await dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.Credits + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SettingUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.SalarySettings)
            {
                var builder = Builders<SalarySetting>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<SalarySetting>.Update
                    .Set(m => m.Value, item.Value)
                    .Set(m => m.Description, item.Description)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalarySettings.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        [Route(Constants.LinkSalary.Credits + "/" + Constants.LinkSalary.Update)]
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
                if (credit != null && credit.MucThanhToanHangThang > 0)
                {
                    mucthanhtoanhangthang = credit.MucThanhToanHangThang;
                }
                try
                {
                    credits.Add(new SalaryCredit
                    {
                        EmployeeId = employee.Id,
                        MaNhanVien = employee.CodeOld,
                        FullName = employee.FullName,
                        ChucVu = employee.SalaryChucVu,
                        MucThanhToanHangThang = mucthanhtoanhangthang
                    });
                }
                catch (Exception ex)
                {

                }

            }
            dbContext.SalaryCredits.InsertMany(credits);

            var viewModel = new BangLuongViewModel
            {
                SalaryCredits = credits
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.Setting + "/" + Constants.LinkSalary.Update)]
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

        [Route(Constants.LinkSalary.LogisticDatas + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> LogisticDataUpdate(string thang)
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
            var date = new DateTime(2018, 08, 01);
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            // If exist, update, no => create 1 month
            var dataTime = sortTimes[0];
            if (!string.IsNullOrEmpty(thang))
            {
                dataTime = new MonthYear
                {
                    Month = Convert.ToInt32(thang.Split("-")[0]),
                    Year = Convert.ToInt32(thang.Split("-")[1]),
                };
            }

            var logisticsData = new List<SalaryLogisticData>();
            var logisticsDataTemp = dbContext.SalaryLogisticDatas.Find(m => m.Year.Equals(dataTime.Year) & m.Month.Equals(dataTime.Month) & m.Enable.Equals(true)).ToList();
            if (logisticsDataTemp != null && logisticsDataTemp.Count > 0)
            {
                logisticsData = logisticsDataTemp;
            }
            else
            {
                var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true)
                & !m.UserName.Equals(Constants.System.account)
                & (m.CodeOld.Contains("KDG")
                    || m.CodeOld.Contains("KDPX")
                    || m.CodeOld.Contains("KDX")
                    || m.CodeOld.Contains("KDS"))
                ).ToListAsync();
                foreach (var employee in employees)
                {
                    try
                    {
                        logisticsData.Add(new SalaryLogisticData
                        {
                            Year = dataTime.Year,
                            Month = dataTime.Month,
                            EmployeeId = employee.Id,
                            MaNhanVien = employee.CodeOld,
                            FullName = employee.FullName,
                            ChucVu = employee.SalaryChucVu
                        });
                    }
                    catch (Exception ex)
                    {

                    }

                }
                dbContext.SalaryLogisticDatas.InsertMany(logisticsData);
            }

            var viewModel = new BangLuongViewModel
            {
                SalaryLogisticDatas = logisticsData
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.LogisticDatas + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> LogisticDataUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.SalaryLogisticDatas)
            {
                var builder = Builders<SalaryLogisticData>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<SalaryLogisticData>.Update
                    //
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalaryLogisticDatas.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        [Route(Constants.LinkSalary.SaleKPIs + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SaleKPIUpdate(string thang)
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
            var date = new DateTime(2018, 08, 01);
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
            var sortTimes = monthYears.OrderByDescending(x => x.Year).OrderByDescending(x => x.Month).ToList();
            #endregion

            // If exist, update, no => create 1 month
            var dataTime = sortTimes[0];
            if (!string.IsNullOrEmpty(thang))
            {
                dataTime = new MonthYear
                {
                    Month = Convert.ToInt32(thang.Split("-")[0]),
                    Year = Convert.ToInt32(thang.Split("-")[1]),
                };
            }

            var sales = new List<SalarySaleKPI>();
            var salesTemp = dbContext.SalarySaleKPIs.Find(m => m.Year.Equals(dataTime.Year) & m.Month.Equals(dataTime.Month) & m.Enable.Equals(true)).ToList();
            if (salesTemp != null && salesTemp.Count > 0)
            {
                sales = salesTemp;
            }
            else
            {
                var employees = await dbContext.Employees.Find(m => m.Enable.Equals(true)
                 & !m.UserName.Equals(Constants.System.account)
                 & (m.CodeOld.Contains("KDS")
                     || m.CodeOld.Contains("KDV"))
                 ).ToListAsync();
                foreach (var employee in employees)
                {
                    try
                    {
                        sales.Add(new SalarySaleKPI
                        {
                            Year = dataTime.Year,
                            Month = dataTime.Month,
                            EmployeeId = employee.Id,
                            MaNhanVien = employee.CodeOld,
                            FullName = employee.FullName,
                            ChucVu = employee.SalaryChucVu
                        });
                    }
                    catch (Exception ex) { }

                }
                dbContext.SalarySaleKPIs.InsertMany(sales);
            }

            var viewModel = new BangLuongViewModel
            {
                SalarySaleKPIs = sales
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.SaleKPIs + "/" + Constants.LinkSalary.Update)]
        public async Task<IActionResult> SaleKPIUpdate(BangLuongViewModel viewModel)
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

            foreach (var item in viewModel.SalarySaleKPIs)
            {
                var builder = Builders<SalarySaleKPI>.Filter;
                var filter = builder.Eq(m => m.Id, item.Id);
                var update = Builders<SalarySaleKPI>.Update
                    .Set(m => m.UpdatedOn, DateTime.Now)
                    .Set(m => m.UpdatedOn, DateTime.Now)
                    .Set(m => m.UpdatedOn, DateTime.Now);
                dbContext.SalarySaleKPIs.UpdateOne(filter, update);
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        [Route(Constants.LinkSalary.KPIMonth)]
        public async Task<IActionResult> KPIMonth()
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

            // Get KPI lastest month
            var kpiLastest = await dbContext.SaleKPIs.Find(m => m.Enable.Equals(true)).SortByDescending(m => m.Year).SortByDescending(m => m.Month).FirstOrDefaultAsync();
            var lastMonth = kpiLastest.Month;
            var lastYear = kpiLastest.Year;

            var viewModel = new SalarySaleViewModel()
            {
                SaleKPIs = await dbContext.SaleKPIs.Find(m => m.Enable.Equals(true) & m.Year.Equals(lastYear) & m.Month.Equals(lastMonth)).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route(Constants.LinkSalary.KPIMonth)]
        public async Task<IActionResult> KPIMonth(SalarySaleViewModel viewModel)
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

            foreach (var item in viewModel.SaleKPIs)
            {
                if (!string.IsNullOrEmpty(item.Id))
                {
                    var builder = Builders<SaleKPI>.Filter;
                    var filter = builder.Eq(m => m.Id, item.Id);
                    var update = Builders<SaleKPI>.Update
                        .Set(m => m.Value, item.Value)
                        .Set(m => m.UpdatedOn, DateTime.Now);
                    dbContext.SaleKPIs.UpdateOne(filter, update);
                }
                else
                {
                    // create new kpi
                }
            }

            return Json(new { result = true, source = "update", message = "Thành công" });
        }

        #endregion

        #region Sub
        [Route(Constants.LinkSalary.TongThuNhapCalculator)]
        public IActionResult ThangBangLuongLawCalculator(decimal luongCanBanBaoGomPhuCap, string id)
        {
            var thamsoEntity = dbContext.SalarySettings.Find(m => m.Enable.Equals(true)).ToList();
            var mauSo = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-lam-viec")).Value);
            var mauSoBaoVe = Convert.ToDecimal(thamsoEntity.Find(m => m.Key.Equals("mau-so-bao-ve")).Value);

            //luongcbbaogomphucap/thamso* (ngayconglamviec+congCNgio/8*2+congtangcangaythuonggio/8*1.5+congletet*3)
            //+luongCB/thamso *(ngaynghiphephuongluong+NgàynghỉLễTếthưởnglương)
            //+congtacxa+doanhthu+thanhtienbocbun+luongkhac+thidua+hotrongoailuong
            //decimal tongthunhap = luongCanBanBaoGomPhuCap / mauSo * (ngayConglamViec + congCNGio / 8 * 2 + congTangCaNgayThuongGio / 8 * (decimal)1.5 + congLeTet * 3)
            //                    + luongCB / mauSo * (ngayNghiLeTetHuongLuong + ngayNghiLeTetHuongLuong)
            //                    + congTacXa + luongTheoDoanhThuDoanhSo + thanhTienBunBoc + luongKhac + thiDua + hoTroNgoaiLuong;
            decimal tongthunhap = luongCanBanBaoGomPhuCap;
            decimal bHXHBHYT = 0;
            decimal luongThamGiaBHXH = 0;

            decimal tamUng = 0;

            decimal thuongLeTet = 0;
            //=ROUND(AK18-AL18-AN18+AO18,-1)
            decimal thuclanh = tongthunhap - bHXHBHYT - tamUng + thuongLeTet;

            return Json(new { tongthunhap, thuclanh });
        }

        [Route(Constants.LinkSalary.ThangBangLuongLawCalculator)]
        public IActionResult ThangBangLuongLawCalculator(decimal money, decimal heso, string id)
        {
            var list = new List<IdMoney>();
            decimal salaryMin = dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).First().ToiThieuVungDoanhNghiepApDung; // use reset
            if (money > 0)
            {
                salaryMin = money;
            }

            // if id null: calculator all.
            // else: get information by id=> calculator from [Bac] and return by group.
            if (!string.IsNullOrEmpty(id))
            {
                var currentLevel = dbContext.SalaryThangBangLuongs.Find(m => m.Id.Equals(id)).FirstOrDefault();
                if (currentLevel != null)
                {
                    var bac = currentLevel.Bac;
                    var maso = currentLevel.MaSo;
                    if (heso == 0)
                    {
                        heso = currentLevel.HeSo;
                    }
                    var salaryDeclareTax = heso * salaryMin;
                    if (bac > 1)
                    {
                        var previousBac = bac - 1;
                        var previousBacEntity = dbContext.SalaryThangBangLuongs.Find(m => m.MaSo.Equals(maso) & m.Bac.Equals(previousBac)).FirstOrDefault();
                        if (previousBacEntity != null)
                        {
                            salaryDeclareTax = heso * previousBacEntity.MucLuong;
                        }
                    }
                    // Add current change
                    list.Add(new IdMoney
                    {
                        Id = currentLevel.Id,
                        Money = salaryDeclareTax,
                        Rate = heso
                    });
                    var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(true) & m.MaSo.Equals(maso)).ToList();

                    foreach (var level in levels)
                    {
                        if (level.Bac > bac)
                        {
                            salaryDeclareTax = level.HeSo * salaryDeclareTax;
                            list.Add(new IdMoney
                            {
                                Id = level.Id,
                                Money = salaryDeclareTax,
                                Rate = level.HeSo
                            });
                        }
                    }
                }
            }
            else
            {
                var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(true)).ToList();
                // group by MaSo
                var groups = (from s in levels
                              group s by new
                              {
                                  s.MaSo
                              }
                                                    into l
                              select new
                              {
                                  MaSoName = l.Key.MaSo,
                                  Salaries = l.ToList(),
                              }).ToList();

                foreach (var group in groups)
                {
                    // reset salaryDeclareTax;
                    var salaryDeclareTax = salaryMin;
                    foreach (var level in group.Salaries)
                    {
                        salaryDeclareTax = level.HeSo * salaryDeclareTax;
                        list.Add(new IdMoney
                        {
                            Id = level.Id,
                            Money = salaryDeclareTax,
                            Rate = level.HeSo
                        });
                    }
                }
            }

            return Json(new { result = true, list });
        }

        [Route(Constants.LinkSalary.ThangBangLuongRealCalculator)]
        public IActionResult ThangBangLuongRealCalculator(string id, decimal heso, decimal money)
        {
            var list = new List<IdMoney>();
            decimal salaryMin = dbContext.SalaryMucLuongVungs.Find(m => m.Enable.Equals(true)).First().ToiThieuVungDoanhNghiepApDung; // use reset
            var salaryMinApDung = salaryMin;
            if (money > 0)
            {
                salaryMin = money;
            }
            if (!string.IsNullOrEmpty(id))
            {
                if (id != "new")
                {
                    var currentLevel = dbContext.SalaryThangBangLuongs.Find(m => m.Id.Equals(id)).FirstOrDefault();
                    if (currentLevel != null)
                    {
                        var bac = currentLevel.Bac;
                        var vitriCode = currentLevel.ViTriCode;
                        if (heso == 0)
                        {
                            heso = currentLevel.HeSo;
                        }
                        var salaryDeclareTax = Math.Round(salaryMin, 0);
                        var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.Law.Equals(false) & m.ViTriCode.Equals(vitriCode)).ToList();
                        foreach (var level in levels)
                        {
                            if (level.Bac > bac)
                            {
                                // Rule bac 1 =  muc tham chieu
                                if (level.Bac > 1)
                                {
                                    salaryDeclareTax = Math.Round(heso * salaryDeclareTax, 0);
                                }
                                list.Add(new IdMoney
                                {
                                    Id = level.Id,
                                    Money = salaryDeclareTax,
                                    Rate = heso
                                });
                            }
                        }
                    }
                }
                else
                {
                    heso = heso == 0 ? 1 : heso;
                    var salaryDeclareTax = Math.Round(salaryMin, 0);
                    list.Add(new IdMoney
                    {
                        Id = "new-1",
                        Money = salaryDeclareTax,
                        Rate = heso
                    });
                    for (var i = 2; i <= 10; i++)
                    {
                        salaryDeclareTax = Math.Round(heso * salaryDeclareTax, 0);
                        list.Add(new IdMoney
                        {
                            Id = "new-" + i,
                            Money = salaryDeclareTax,
                            Rate = heso
                        });
                    }
                }
            }
            else
            {
                // Ap dung nếu hệ số bậc là 1 + Muc Luong is min.
                var levels = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) & m.FlagReal.Equals(true) & m.Bac.Equals(1) & m.MucLuong.Equals(salaryMinApDung)).ToList();

                // group by VITRI
                var groups = (from s in levels
                              group s by new
                              {
                                  s.ViTriCode
                              }
                                                    into l
                              select new
                              {
                                  l.Key.ViTriCode,
                                  Salaries = l.ToList(),
                              }).ToList();

                foreach (var group in groups)
                {
                    // reset salaryDeclareTax;
                    var salaryDeclareTax = salaryMin;
                    foreach (var level in group.Salaries)
                    {
                        //Rule level 1 = muc
                        if (level.Bac > 1)
                        {
                            salaryDeclareTax = level.HeSo * salaryDeclareTax;
                        }
                        list.Add(new IdMoney
                        {
                            Id = level.Id,
                            Money = salaryDeclareTax,
                            Rate = level.HeSo
                        });
                    }
                }
            }
            return Json(new { result = true, list });
        }

        [Route(Constants.LinkSalary.UpdateData)]
        public IActionResult UpdateData()
        {
            InitCaiDat();
            InitChucVuSale();
            InitKPI();



            InitLuongToiThieuVung();
            InitLuongFeeLaw();
            InitThangBangLuong();
            InitSalaryPhuCapPhucLoi();
            InitSalaryThangBangPhuCapPhucLoi();
            InitChucDanhCongViec();

            return Json(new { result = true });
        }

        private void InitCaiDat()
        {
            dbContext.SalarySettings.DeleteMany(new BsonDocument());
            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "mau-so-lam-viec",
                Value = "26",
                Title = "Ngày làm việc"
            });

            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "mau-so-khac",
                Value = "27",
                Title = "Ngày làm việc"
            });
            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "mau-so-bao-ve",
                Value = "30",
                Title = "Ngày làm việc"
            });

            dbContext.SalarySettings.InsertOne(new SalarySetting()
            {
                Key = "ty-le-dong-bh",
                Value = "0.105",
                Title = "Tỷ lệ đóng BH"
            });
        }

        // Init sale chuc vu
        private void InitChucVuSale()
        {
            dbContext.ChucVuSales.DeleteMany(new BsonDocument());
            var chucvu = "ĐDKD HCM";
            int i = 1;
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "TKD HCM";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "ĐDKD TỈNH";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "TKD TỈNH";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "ADMIN";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "ĐDKD BÙN";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;

            chucvu = "TKD BÙN";
            dbContext.ChucVuSales.InsertOne(new ChucVuSale()
            {
                Name = chucvu,
                Alias = Utility.AliasConvert(chucvu),
                Code = Constants.System.chucVuSaleCode + i.ToString("00")
            });
            i++;
        }

        private void InitKPI()
        {
            dbContext.SaleKPIs.DeleteMany(new BsonDocument());
            var typeKHM = "KH Mới";
            var typeKHMAlias = Utility.AliasConvert(typeKHM);
            var typeKHMCode = Constants.System.kPITypeCode + 1.ToString("00");
            var conditionKHM = string.Empty;
            var conditionKHMValue = string.Empty;

            var typeDP = "Độ phủ";
            var typeDPAlias = Utility.AliasConvert(typeDP);
            var typeDPCode = Constants.System.kPITypeCode + 2.ToString("00");
            var conditionDP = "Trên 80%";
            var conditionDPValue = "80";

            var typeNH = "Ngành hàng";
            var typeNHAlias = Utility.AliasConvert(typeNH);
            var typeNHCode = Constants.System.kPITypeCode + 3.ToString("00");
            var conditionNH = "Đạt 70% 4 ngành";
            var conditionNHValue = "70";

            var typeDT = "Doanh thu";
            var typeDTAlias = Utility.AliasConvert(typeDT);
            var typeDTCode = Constants.System.kPITypeCode + 4.ToString("00");
            var conditionDT1 = "80%-99%";
            var conditionDT1Value = "80-99";
            var conditionDT2 = "Trên 100%";
            var conditionDT2Value = "100";


            var typeDS = "Doanh số";
            var typeDSAlias = Utility.AliasConvert(typeDS);
            var typeDSCode = Constants.System.kPITypeCode + 5.ToString("00");
            var conditionDS1 = "80%-99%";
            var conditionDS1Value = "80-99";
            var conditionDS2 = "Trên 100%";
            var conditionDS2Value = "100-119";
            var conditionDS3 = "Trên 120%";
            var conditionDS3Value = "120";


            var chucvus = dbContext.ChucVuSales.Find(m => m.Enable.Equals(true)).ToList();
            // Update value later.
            foreach (var item in chucvus)
            {
                // KH Mới
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeKHM,
                    TypeAlias = typeKHMAlias,
                    TypeCode = typeKHMCode,
                    Condition = conditionKHM,
                    ConditionValue = conditionKHMValue,
                    Value = "500"
                });

                // DP
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDP,
                    TypeAlias = typeDPAlias,
                    TypeCode = typeDPCode,
                    Condition = conditionDP,
                    ConditionValue = conditionDPValue,
                    Value = "1000"
                });

                // Ngành hàng
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeNH,
                    TypeAlias = typeNHAlias,
                    TypeCode = typeNHCode,
                    Condition = conditionNH,
                    ConditionValue = conditionNHValue,
                    Value = "500"
                });

                // Doanh thu
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDT,
                    TypeAlias = typeDTAlias,
                    TypeCode = typeDTCode,
                    Condition = conditionDT1,
                    ConditionValue = conditionDT1Value,
                    Value = "1000"
                });

                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDT,
                    TypeAlias = typeDTAlias,
                    TypeCode = typeDTCode,
                    Condition = conditionDT2,
                    ConditionValue = conditionDT2Value,
                    Value = "2000"
                });

                // DS
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDS,
                    TypeAlias = typeDSAlias,
                    TypeCode = typeDPCode,
                    Condition = conditionDS1,
                    ConditionValue = conditionDS1Value,
                    Value = "1000"
                });
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDS,
                    TypeAlias = typeDSAlias,
                    TypeCode = typeDPCode,
                    Condition = conditionDS2,
                    ConditionValue = conditionDS2Value,
                    Value = "3000"
                });
                dbContext.SaleKPIs.InsertOne(new SaleKPI()
                {
                    Year = 2018,
                    Month = 6,
                    ChucVu = item.Name,
                    ChucVuAlias = item.Alias,
                    ChucVuCode = item.Code,
                    Type = typeDS,
                    TypeAlias = typeDSAlias,
                    TypeCode = typeDPCode,
                    Condition = conditionDS3,
                    ConditionValue = conditionDS3Value,
                    Value = "4000"
                });
            }
        }

        private void InitLogistics()
        {
            // CityXeGiaoNhan
            dbContext.CityGiaoNhans.DeleteMany(new BsonDocument());
            var listLocationGiaoNhan = new List<string>
            {
                "TP.HCM",
                "Bình Dương",
                "Biên Hòa",
                "Vũng Tàu",
                "BìnhThuận",
                "Cần Thơ"
                ,"Vĩnh Long"
                ,"Long An"
                ,"Tiền Giang"
                ,"Đồng Nai"
            };
            // Code update later...
            foreach (var item in listLocationGiaoNhan)
            {
                dbContext.CityGiaoNhans.InsertOne(new CityGiaoNhan
                {
                    City = item
                });
            }
            var xes = new List<string>()
            {
                "Xe nhỏ",
                "Xe lớn"
            };
            // Don gia chuyen xe
            dbContext.DonGiaChuyenXes.DeleteMany(new BsonDocument());

            foreach (var item in listLocationGiaoNhan)
            {
                if (item == "TP.HCM")
                {
                    var xe2s = new List<string>()
                    {
                        "Xe nhỏ 1.7 tấn",
                        "Xe lớn ben và 8 tấn"
                    };
                    foreach (var xe in xe2s)
                    {
                        for (var i = 1; i <= 5; i++)
                        {
                            dbContext.DonGiaChuyenXes.InsertOne(new DonGiaChuyenXe
                            {
                                Year = 2018,
                                Month = 8

                            });
                        }
                    }
                }
                else if (item == "Bình Dương" || item == "Biên Hòa")
                {
                    foreach (var xe in xes)
                    {
                        for (var i = 1; i <= 5; i++)
                        {

                        }
                    }
                }
                else
                {
                    foreach (var xe in xes)
                    {

                    }
                }
            }


            // Ho tro cong tac xa
            dbContext.HoTroCongTacXas.DeleteMany(new BsonDocument());
        }

        private void InitLuongToiThieuVung()
        {
            dbContext.SalaryMucLuongVungs.DeleteMany(new BsonDocument());
            dbContext.SalaryMucLuongVungs.InsertOne(new SalaryMucLuongVung()
            {
                ToiThieuVungQuiDinh = 3980000,
                TiLeMucDoanhNghiepApDung = (decimal)1.07,
                ToiThieuVungDoanhNghiepApDung = 3980000 * (decimal)1.07
            });
        }

        private void InitLuongFeeLaw()
        {
            dbContext.SalaryFeeLaws.DeleteMany(new BsonDocument());
            dbContext.SalaryFeeLaws.InsertOne(new SalaryFeeLaw()
            {
                Name = "Bảo hiểm xã hội",
                NameAlias = Utility.AliasConvert("Bảo hiểm xã hội"),
                TiLeDong = (decimal)0.105,
                Description = "Bao gồm: BHXH (8%), BHYT(1.5%), Thất nghiệp (1%). Theo Quyết định 595/QĐ-BHXH."
            });
        }

        private void InitThangBangLuong()
        {
            dbContext.SalaryThangBangLuongs.DeleteMany(mbox => mbox.FlagReal.Equals(false));
            // default muc luong = toi thieu, HR update later.
            decimal salaryMin = 3980000 * (decimal)1.07; // use reset
            decimal salaryDeclareTax = salaryMin;
            // Company no use now. sử dụng từng vị trí đặc thù. Hi vong tương lai áp dụng.
            decimal salaryReal = salaryDeclareTax; // First set real salary default, update later

            var name = string.Empty;
            var nameAlias = string.Empty;
            var maso = string.Empty;
            var typeRole = string.Empty;
            var typeRoleAlias = string.Empty;
            var typeRoleCode = string.Empty;

            #region 1- BẢNG LƯƠNG CHỨC VỤ QUẢN LÝ DOANH NGHIỆP
            typeRole = "CHỨC VỤ QUẢN LÝ DOANH NGHIỆP";
            typeRoleAlias = Utility.AliasConvert(typeRole);
            typeRoleCode = "C";
            // 01- TỔNG GIÁM ĐỐC 
            name = "TỔNG GIÁM ĐỐC";
            maso = "C.01";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 02-GIÁM ĐỐC/TRƯỞNG BAN
            name = "GIÁM ĐỐC/TRƯỞNG BAN";
            maso = "C.02";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 0)
                {
                    heso = (decimal)1.05;
                    if (i == 1)
                    {
                        heso = (decimal)1.8;
                    }
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 03- KẾ TOÁN TRƯỞNG/ PHÓ GIÁM ĐỐC
            name = "KẾ TOÁN TRƯỞNG/ PHÓ GIÁM ĐỐC";
            maso = "C.03";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 0)
                {
                    heso = (decimal)1.05;
                    if (i == 1)
                    {
                        heso = (decimal)1.7;
                    }
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            #endregion

            #region 2- BẢNG LƯƠNG VIÊN CHỨC CHUYÊN MÔN, THỪA HÀNH, PHỤC VỤ
            typeRole = "VIÊN CHỨC CHUYÊN MÔN, THỪA HÀNH, PHỤC VỤ";
            typeRoleAlias = Utility.AliasConvert(typeRole);
            typeRoleCode = "D";
            // 01- TRƯỞNG BP THIẾT KẾ, TRƯỞNG BP GS KT, KẾ TOÁN TỔNG HỢP, QUẢN LÝ THUẾ…
            name = "TRƯỞNG BP THIẾT KẾ, TRƯỞNG BP GS KT, KẾ TOÁN TỔNG HỢP, QUẢN LÝ THUẾ…";
            maso = "D.01";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 02- TRƯỞNG SALE, NHÂN VIÊN NHÂN SỰ, CV THU MUA, CV PHÁP CHẾ, CV KẾ HOẠCH TỔNG HỢP, CV MÔI TRƯỜNG, CV NC KHCN, CV PHÒNG TN….
            name = "TRƯỞNG SALE, NHÂN VIÊN NHÂN SỰ, CV THU MUA, CV PHÁP CHẾ, CV KẾ HOẠCH TỔNG HỢP, CV MÔI TRƯỜNG, CV NC KHCN, CV PHÒNG TN…";
            maso = "D.02";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            // 03- NV KINH DOANH, SALE ADMIN, NV HÀNH CHÍNH, NV KẾ TOÁN, THỦ QUỸ, NV DỰ ÁN, NV KỸ THUẬT, …
            name = "NV KINH DOANH, SALE ADMIN, NV HÀNH CHÍNH, NV KẾ TOÁN, THỦ QUỸ, NV DỰ ÁN, NV KỸ THUẬT,…";
            maso = "D.03";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            #endregion

            #region 3- BẢNG LƯƠNG NHÂN VIÊN TRỰC TIẾP SẢN XUẤT KINH DOANH VÀ PHỤC VỤ
            typeRole = "NHÂN VIÊN TRỰC TIẾP SẢN XUẤT KINH DOANH VÀ PHỤC VỤ";
            typeRoleAlias = Utility.AliasConvert(typeRole);
            typeRoleCode = "B";
            // 01- TRƯỞNG BP -NM…
            name = "TRƯỞNG BP -NM…";
            maso = "B.01";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 02- TỔ TRƯỞNG NM, TỔ PHÓ NM, TỔ TRƯỞNG LOGISTICS, QUẢN LÝ TRẠM CÂN…
            name = "TỔ TRƯỞNG NM, TỔ PHÓ NM, TỔ TRƯỞNG LOGISTICS, QUẢN LÝ TRẠM CÂN…";
            maso = "B.02";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 03- TÀI XẾ, NV KHO, NV ĐIỀU ĐỘ, NV CẢNH QUAN, NV BẢO TRÌ, NV TRẠM CÂN…
            name = "TÀI XẾ, NV KHO, NV ĐIỀU ĐỘ, NV CẢNH QUAN, NV BẢO TRÌ, NV TRẠM CÂN…";
            maso = "B.03";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }

            // 04- GIAO NHẬN, PHỤ XE, BẢO VỆ, CÔNG NHÂN…
            name = "GIAO NHẬN, PHỤ XE, BẢO VỆ, CÔNG NHÂN…";
            maso = "B.04";
            nameAlias = Utility.AliasConvert(name);
            salaryDeclareTax = salaryMin;
            salaryReal = salaryMin;
            for (int i = 0; i <= 10; i++)
            {
                decimal heso = 1;
                if (i > 1)
                {
                    heso = (decimal)1.05;
                }
                salaryDeclareTax = salaryDeclareTax * heso;
                salaryReal = salaryReal * heso;
                // Theo thuế
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryDeclareTax,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode
                });
                // Thuc te
                dbContext.SalaryThangBangLuongs.InsertOne(new SalaryThangBangLuong()
                {
                    Name = name,
                    MaSo = maso,
                    TypeRole = typeRole,
                    Bac = i,
                    HeSo = heso,
                    MucLuong = salaryReal,
                    NameAlias = nameAlias,
                    TypeRoleAlias = typeRoleAlias,
                    TypeRoleCode = typeRoleCode,
                    Law = false
                });
            }
            #endregion
        }

        private void InitSalaryPhuCapPhucLoi()
        {
            dbContext.SalaryPhuCapPhucLois.DeleteMany(new BsonDocument());

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            #region Phu Cap
            int type = 1; // phu-cap
            var name = string.Empty;
            int i = 1;

            name = "NẶNG NHỌC ĐỘC HẠI";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;
            name = "TRÁCH NHIỆM";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "THÂM NIÊN";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "THU HÚT";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;
            #endregion

            #region Phuc Loi
            type = 2; // phuc-loi
            i = 1;

            name = "XĂNG";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "ĐIỆN THOẠI";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "CƠM";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "Kiêm nhiệm";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;

            name = "BHYT ĐẶC BIỆT";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });

            name = "VỊ TRÍ CẦN KN NHIỀU NĂM";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });

            name = "Vị trí đặc thù";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });

            name = "Nhà ở";
            dbContext.SalaryPhuCapPhucLois.InsertOne(new SalaryPhuCapPhucLoi()
            {
                Type = type,
                Order = i,
                Name = textInfo.ToTitleCase(name.ToLower()),
                NameAlias = Utility.AliasConvert(name),
                Code = type.ToString("00") + "-" + i.ToString("000"),
            });
            i++;
            #endregion
        }

        private void InitSalaryThangBangPhuCapPhucLoi()
        {
            dbContext.SalaryThangBangPhuCapPhucLois.DeleteMany(m => m.FlagReal.Equals(false));
            #region TGD
            // Trach nhiem 01-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.01",
                Money = 500000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.01",
                Money = 500000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.01",
                Money = 500000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.01",
                Money = 500000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.01",
                Money = 0
            });
            #endregion

            #region GĐ/PGĐ
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.02",
                Money = 300000
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.02",
                Money = 400000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.02",
                Money = 300000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.02",
                Money = 400000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.02",
                Money = 0
            });
            #endregion

            #region KT trưởng
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "C.03",
                Money = 300000
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "C.03",
                Money = 400000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "C.03",
                Money = 300000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "C.03",
                Money = 400000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "C.03",
                Money = 0
            });
            #endregion

            #region Trưởng BP
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "D.01",
                Money = 200000
            });

            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "D.01",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "D.01",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "D.01",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.01",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.01",
                Money = 200000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "B.01",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "B.01",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "B.01",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.01",
                Money = 0
            });
            #endregion

            #region Tổ trưởng
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "D.02",
                Money = 100000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "D.02",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "D.02",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "D.02",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.02",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.02",
                Money = 100000
            });
            // Dien thoai 02-002
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-002",
                Name = "Điện thoại",
                MaSo = "B.02",
                Money = 300000
            });
            // Xang 02-001
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-001",
                Name = "Xăng",
                MaSo = "B.02",
                Money = 200000
            });
            // Com 02-003
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-003",
                Name = "Cơm",
                MaSo = "B.02",
                Money = 300000
            });
            // Nha o 02-008
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.02",
                Money = 0
            });
            #endregion

            #region Tổ phó
            // B.02
            #endregion

            #region Others
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "D.03",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "D.03",
                Money = 200000
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.03",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.03",
                Money = 200000
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "01-002",
                Name = "Trách nhiệm",
                MaSo = "B.04",
                Money = 0
            });
            dbContext.SalaryThangBangPhuCapPhucLois.InsertOne(new SalaryThangBangPhuCapPhucLoi()
            {
                Code = "02-008",
                Name = "Nhà ở",
                MaSo = "B.04",
                Money = 200000
            });
            #endregion
        }

        // Base on ThangBangLuong. Do later
        private void InitChucDanhCongViec()
        {
            dbContext.ChucDanhCongViecs.DeleteMany(new BsonDocument());
            var listTemp = dbContext.SalaryThangBangLuongs.Find(m => m.Enable.Equals(true) && m.Law.Equals(true)).ToList();
            foreach (var item in listTemp)
            {
                if (!(dbContext.ChucDanhCongViecs.CountDocuments(m => m.Code.Equals(item.MaSo)) > 0))
                {
                    dbContext.ChucDanhCongViecs.InsertOne(new ChucDanhCongViec()
                    {
                        Name = item.Name,
                        Alias = item.NameAlias,
                        Code = item.MaSo,
                        Type = item.TypeRole,
                        TypeAlias = item.TypeRoleAlias,
                        TypeCode = item.TypeRoleCode
                    });
                }
            }
        }

        #endregion

    }
}
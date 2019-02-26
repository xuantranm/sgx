using Common.Enums;
using System;

namespace Common.Utilities
{
    public static class Constants
    {
        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }

        public static class ActionLink
        {
            public const string Init = "init";
            public const string Export = "xuat-tai-lieu";
            public const string Add = "nhan-su";
            public const string Edit = "thong-tin";
            public const string Update = "cap-nhat";
            public const string Delete = "danh-sach";
            public const string Disable = "nhap-lieu";
            public const string Post = "post";
        }

        public static string Location(int loc)
        {
            switch (loc)
            {
                case (int)EKhoiLamViec.NM:
                    return "Nhà Máy";
                case (int)EKhoiLamViec.SX:
                    return "Nhà Máy";
                default:
                    return "Văn Phòng";
            }
        }

        public const string ImgMissLink = "http://via.placeholder.com/";

        public const string WaitData = "Chờ lấy dữ liệu";

        public const string NewDataSuccess = "Tạo mới dữ liệu thành công";

        public const string DataDuplicate = "Dữ liệu đã tồn tại. Không thể tạo nhiều dữ liệu cùng tên.";

        public const string NA = "N/A";

        // System not store db. highest right
        public static class System
        {
            public const string account = "sysadmin";
            public const string accountId = "5b4e9e8f4fbc4d13582de168";
            public const string department = "system";
            public const int expire = 30;
            public const bool enable = true;
            public const string domain = "http://erp.tribat.vn:8090";
            //public const string domain = "https://localhost:44366";
            public const string login = "/account/login/";
            public const string emailErp = "test-erp@tribat.vn";
            public const string emailErpPwd = "Kh0ngbiet@123";
            public const string emailErpName = "HCNS";

            public const string emailHr = "app.hcns@tribat.vn";
            public const string emailHrPwd = "Tr1b@t";
            public const string emailHrName = "APP.HCNS";

            public const string emailNM = "test-erp@tribat.vn";

            public const string viTriCodeTBLuong = "SVT-";

            public const string chucVuSaleCode = "SCV-";

            public const string kPITypeCode = "KPITYPE-";
        }

        public static class Rights
        {
            public const string System = "system";
            public const string HR = "hr";
            public const string NhanSu = "nhan-su";
            public const string HanhChanh = "hanh-chanh";
            public const string Luong = "luong";
            public const string LuongNM = "luong-nha-may";
            public const string LuongSX = "luong-san-xuat";
            public const string LuongVP = "luong-van-phong";
            
            public const string XinNghiPhepDum = "xin-nghi-phep-dum";
            public const string XacNhanNghiPhep = "xac-nhan-nghi-phep";

            public const string ChamCong = "cham-cong";
            public const string XacNhanCongDum = "xac-nhan-cong-dum";
            public const string XacNhanCong = "xac-nhan-cong";
            public const string BangChamCong = "bang-cham-cong";

            public const string NhaMay = "nha-may";
            public const string TonSX = "ton-sx";
        }

        public static class ContactType
        {
            public const string personal = "personal";
            public const string company = "company";
        }

        public static class UnitType
        {
            public const string Factory = "factory";
            public const string Logistics = "logistics";
            public const string Hr = "hr";
        }

        public static class Status
        {
            public const string Open = "Open";
            public const string Complete = "Complete";
            public const string Order = "Open";
            public const string Completes = "Complete";
        }

        public static class Seo
        {
            public const string indexFollow = "index, follow";
            public const string indexNoFollow = "index, nofollow";
            public const string noIndexFollow = "noindex, follow";
            public const string noIndexNoFollow = "noindex, nofollow";
        }

        public static class LinkHr
        {
            public const string Main = "hr";
            public const string Human = "nhan-su";
            public const string Information = "thong-tin";
            public const string List = "danh-sach";
            public const string Create = "nhap-lieu";
            public const string Edit = "chinh-sua";

            public const string ChildrenReport = "danh-sach-con-cua-nhan-vien";
            public const string Export = "xuat";
            public const string Department = "phong-ban";
            public const string Children = "con-cai";

            public const string NewProduct = "them-san-pham";
            public const string NewUnit = "them-dvt";
            public const string Birthday = "sinh-nhat";

            public const string Title = "chuc-vu";
            public const string Part = "bo-phan";
            public const string Hospital = "benh-vien";
        }

        public static class LinkFactory
        {
            public const string Main = "nm";
            public const string List = "danh-sach";
            public const string TonSx = "ton-sx";
            public const string ReportTonSx = "bao-cao-ton-san-xuat";
            public const string ReportXCG = "bao-cao-xe-co-gioi";
            public const string ReportDG = "bao-cao-dong-goi";
            public const string ReportBH = "bao-cao-boc-hang";
            public const string ReportVanHanh = "bao-cao-van-hanh";
            public const string VanHanh = "van-hanh";
            public const string DanhGiaXCG = "danh-gia-xe-co-goi";
            public const string BieuDoXCG = "bieu-do-danh-gia-xe-co-goi";
            public const string DinhMucXCG = "dinh-muc-xe-co-goi";
            public const string ChiPhiXCG = "chi-phi-xe-co-goi";
            public const string Create = "nhap-lieu";
            public const string Edit = "chinh-sua";
            public const string PhieuInCa = "phieu-in-ca";

            public const string NewProduct = "them-san-pham";
            public const string NewUnit = "them-dvt";

        }
        // fg
        public static class LinkTimeKeeper
        {
            public const string Main = "fg";
            public const string Index = "cham-cong";
            public const string Manage = "quan-ly-cham-cong";
            public const string Request = "yeu-cau-xac-nhan-cong";
            public const string Aprrove = "xac-nhan-cham-cong";
            public const string AprrovePost = "post-xac-nhan-cham-cong";
            public const string Item = "chi-tiet-cham-cong";
            // API
            //public const string Approve = "duyet-phep";
            public const string HelpTime = "cong-nhan-vien-khac";

            public const string ReasonRule = "ly-do-xac-nhan-cong";

            public const string Approvement = "lich-su-duyet-cong";

            public const string Timer = "bang-cham-cong";

            public const string XacNhanTangCa = "xac-nhan-tang-ca";

            public const string OvertimeTemplate = "bang-tang-ca-mau";

            public const string OvertimeTemplateFull = "bang-tang-ca-mau-full";
        }

        public static class LinkSalary
        {
            public const string Main = "lg";
            public const string VanPhong = "van-phong";
            public const string Setting = "cai-dat";
            public const string Factory = "nha-may";
            public const string Production = "san-xuat";
            public const string ThangLuongTrue = "tl";

            public const string ThangLuong = "thang-luong";
            public const string BangLuong = "bang-luong";
            public const string TheLuong = "the-luong";

            public const string SaleKPI = "kinh-doanh-kpi-thang";
            public const string SaleKPICalculator = "tinh-toan-kinh-doanh-kpi-thang";
            public const string SaleKPIImport = "kinh-doanh-kpi-thang-import";
            public const string SaleKPITemplate = "kinh-doanh-kpi-thang-mau";

            public const string SaleKPIEmployee = "kinh-doanh-so-lieu";
            public const string SaleKPIEmployeeImport = "kinh-doanh-so-lieu-import";
            public const string SaleKPIEmployeeTemplate = "kinh-doanh-so-lieu-mau";
            public const string SaleKPIEmployeeCalculator = "tinh-toan-so-lieu-kinh-doanh";

            public const string LogisticGiaChuyenXe = "logistic-gia-chuyen-xe";
            public const string LogisticGiaChuyenXeCalculator = "tinh-toan-logistic-gia-chuyen-xe";
            public const string LogisticGiaChuyenXeImport = "logistic-gia-chuyen-xe-import";
            public const string LogisticGiaChuyenXeTemplate = "logistic-gia-chuyen-xe-mau";

            public const string LogisticGiaBunPost = "logistic-gia-bun-post";

            public const string LogisticEmployeeCong = "logistics-cong-nhan-vien";
            public const string LogisticEmployeeCongCalculator = "tinh-toan-cong-nhan-vien-logistic";
            public const string LogisticEmployeeImport = "logistics-so-lieu-import";
            public const string LogisticEmployeeTemplate = "logistics-so-lieu-mau";

            public const string Credits = "so-lieu-tam-ung";

            public const string Export = "xuat-tai-lieu";
            public const string Update = "cap-nhat";
            public const string Calculator = "tinh-toan";
            public const string CalculatorThucLanh = "tinh-toan-thuc-lanh";

            public const string Init = "init";
            public const string Document = "tai-lieu";

            public const string NhanVienKhoiNhaMay = "nhan-vien-khoi-nha-may";
            public const string NhanVienKhoiSanXuat = "nhan-vien-khoi-san-xuat";

            // TEMPLATES
            public const string CongTong = "cong-tong";
            public const string NhaMayTemplate = "nha-may-so-lieu-mau";
            public const string NhaMayImport = "nha-may-so-lieu-import";

            public const string SanXuatTemplate = "san-xuat-so-lieu-mau";
            public const string SanXuatImport = "san-xuat-so-lieu-import";

            public const string SanXuatTongHopTrongGioTemplate = "tong-hop-trong-gio-mau";
            public const string SanXuatTongHopTrongGioPost = "tong-hop-trong-gio-post";

            public const string SanXuatTongHopNgoaiGioTemplate = "tong-hop-ngoai-gio-mau";
            public const string SanXuatTongHopNgoaiGioPost = "tong-hop-ngoai-gio-post";

            public const string Timer = "bang-cong";

            public const string PhuCap = "phu-cap";

            public const string UngLuong = "ung-luong";

            public const string DongGoiTrongGio = "dong-goi-trong-gio";

            public const string DongGoiNgoaiGio = "dong-goi-ngoai-gio";

            public const string BocHangTrongGio = "boc-hang-trong-gio";

            public const string BocHangNgoaiGio = "boc-hang-ngoai-gio";

            public const string TongHopTrongGio = "tong-hop-trong-gio";

            public const string TongHopNgoaiGio = "tong-hop-ngoai-gio";

            public const string DinhMuc = "dinh-muc";

            public const string SanXuatTamUngTemplate = "tam-ung-mau";
            public const string SanXuatTamUngPost = "tam-ung-post";
            public const string SanXuatNgayCongThuongBHXHTemplate = "ngay-cong-thuong-bhxh-mau";
            public const string SanXuatNgayCongThuongBHXHPost = "ngay-cong-thuong-bhxh-post";
            public const string SanXuatNgoaiGioPhuCapTemplate = "ngoai-gio-phu-cap-mau";
            public const string SanXuatNgoaiGioPhuCapPost = "ngoai-gio-phu-cap-post";
        }

        public static class LinkCredit
        {
            public const string Main = "cd";
            public const string Credits = "so-lieu-tam-ung";
            public const string CreditsNM = "ung-luong";

            public const string Template = "mau-tam-ung";
            public const string CreditImport = "tam-ung-import";
        }

        public static class LinkTraining
        {
            public const string Main = "tn";
            public const string Index = "dao-tao";
            public const string Create = "tao";
            public const string CalculatorDate = "calculator-date";
            // API
            public const string Approve = "duyet-phep";
        }

        public static class NewsLink
        {
            public const string Main = "nws";
            public const string Index = "tin-tuc";

            public const string List = "danh-sach";
            public const string Create = "tao";
            public const string CalculatorDate = "calculator-date";
            // API
            public const string Approve = "duyet-phep";
        }

        public static class LinkNotication
        {
            public const string Main = "nt";
            public const string Index = "thong-bao";

            public const string List = "danh-sach";
            public const string Create = "tao";
            public const string CalculatorDate = "calculator-date";
            // API
            public const string Approve = "duyet-phep";
        }

        public static class LinkSurvey
        {
            public const string Main = "ks";
            public const string Index = "khao-sat";
            public const string Create = "tao";
            public const string CalculatorDate = "calculator-date";
            // API
            public const string Approve = "duyet-phep";
        }

        public static class LinkEvaluate
        {
            public const string Main = "dg";
            public const string Index = "danh-gia";
            public const string Create = "tao";
            public const string CalculatorDate = "calculator-date";
            // API
            public const string Approve = "duyet-phep";
        }

        public static class LinkRecruitment
        {
            public const string Main = "td";
            public const string Index = "tuyen-dung";
            public const string Create = "tao";
            public const string CalculatorDate = "calculator-date";
            // API
            public const string Approve = "duyet-phep";
        }

        public static class LinkRole
        {
            public const string Role = "r";
            public const string RoleUser = "r-u";
            public const string Index = "phan-quyen";
            public const string Detail = "chi-tiet";
            public const string Create = "tao-moi";
            public const string Edit = "chinh-sua";
            public const string Disable = "vo-hieu";
            public const string Active = "khoi-phuc";
            public const string Delete = "xoa";

            public const string HelpLeave = "nghi-phep-nhan-vien-khac";
            public const string UpdateLeaveDay = "cap-nhat-ngay-nghi";
            public const string CalculatorDate = "calculator-date";
            public const string ApprovePost = "post-duyet-phep";
            // API
            public const string Approve = "duyet-phep";
        }

        public static class LinkLeave
        {
            public const string Main = "lm";
            public const string Index = "nghi-phep";
            public const string HelpLeave = "nghi-phep-nhan-vien-khac";
            public const string UpdateLeaveDay = "cap-nhat-ngay-nghi";
            public const string Create = "create";
            public const string CalculatorDate = "calculator-date";
            public const string ApprovePost = "post-duyet-phep";
            // API
            public const string Approve = "duyet-phep";

            public const string Manage = "quan-ly";

            public const string Approvement = "quan-ly-phep";
        }

        public static class LinkSystem
        {
            public const string Main = "system";
            public const string Mail = "mail";
            public const string Resend = "gui-lai";
            public const string Item = "chi-tiet";
        }

        public static class Link
        {
            public const string Product = "p";
            public const string News = "n";
            public const string Job = "j";
            public const string Content = "c";
            public const string Employee = "hr";
            public const string Youtube = "https://www.youtube.com/embed/";
        }

        public static class ImgSize
        {
            public const string desktop = "desktop";
            public const string tablet = "tablet";
            public const string mobile = "mobile";
            public const string thumb = "thumb";
        }


        public static class LinkDocument
        {
            public const string Main = "tai-lieu";
            public const string GettingStarted = "bat-dau";
            public const string Information = "thong-tin-ca-nhan";
            public const string HR = "hanh-chinh";
            public const string Leave = "nghi-phep";
            public const string Timer = "cham-cong";
            public const string Salary = "luong";
        }

        /// <summary>
        /// cache all text on application
        /// </summary>
        public static string CachingText = "system_text";

        /// <summary>
        /// caching time (1 year) for CachingText
        /// </summary>
        public static int CachingTime = 525949;

        public static int PageSize = 50;

        public static int SmallPageSize = 5;

        public static int birthDayNoticeBefore = 7;

        public static int contractDayNoticeBefore = 7;

        public static string DateFormat = "dd/MM/yyyy";
        public static string DateFormatMonthYear = "MM/yyyy";
        public static DateTime MinDate = new DateTime(1900, 01, 01);

        public static string MissTitle = "Đang cập nhật";

        public static string CurrentText = "Dữ liệu hiện tại";

        public static string Godmode = "godmode";

        public static string String_Y = "Y";

        public static string String_N = "N";

        public static string String_D = "D";

        public static string String_R = "R";

        public static string String_A = "A";

        public static string String_S = "S";

        public static string IsSubmitted = "A";

        public static long BigFileSize = 20971520; // 20 MB

        public static string Readonly = "Readonly";

        public static string Modify = "Modify";

        public static string Cache = "Cache.";

        public static string MailExtension = "@tribat.vn";

        public static string CountCacheKey = ".Count";

        public static string FlagCacheKey = "-flag-";

        public static string Flag = "-";

        public static string Max = "max";

        public static class Languages
        {
            public const string English = "en-US";
            public const string Vietnamese = "vi-VN";
        }

        public static class Type
        {
            public const string Text = "Text";
            public const string Label = "Label";
        }

        public static class Currency
        {
            public const string English = "usd";
            public const string Vietnamese = "vnđ";
        }

        public static class Storage
        {
            public const string Uploads = "uploads";
            public const string Hr = "hr";
            public const string Account = "account";
            public const string Logistics = "logistics";
            public const string Store = "store";
            public const string NhaMay = "nha-may";
            public const string Factories = "factories";
        }

        public static class Collection
        {
            public const string SalaryThangBangLuong = "SalaryThangBangLuong";

            public const string Notifications = "Notifications";
            public const string Settings = "Settings";
            public const string Texts = "Texts";
            public const string Links = "Links";
            public const string Lookups = "Lookups";
            public const string Activities = "Activities";
            public const string Countries = "Countries";
            public const string Languages = "Languages";
            public const string Parts = "Parts";
            public const string Departments = "Departments";
            public const string Functions = "Functions";
            public const string Roles = "Roles";
            public const string RoleUsers = "RoleUsers";
            public const string NhanViens = "NhanViens";
            public const string ProductGroups = "ProductGroups";
            public const string Products = "Products";
            public const string Units = "Units";
            public const string Locations = "Locations";
            public const string Stores = "Stores";
            public const string Requisitions = "Requisitions";
            public const string Orders = "Orders";
            public const string Inputs = "Inputs";
            public const string Outputs = "Outputs";
            public const string Returns = "Returns";
            public const string NA = "N/A";
            public const string Employees = "Employees";
            public const string BHYTHospitals = "BHYTHospitals";

            // FACTORY
            public const string FactoryTonSx = "FactoryTonSxs";
            public const string FactoryVanHanh = "FactoryVanHanhs";
            public const string FactoryDanhGia = "FactoryDanhGias";
            public const string FactoryDinhMuc = "FactoryDinhMucs";
        }

        public static class Action
        {
            public const string Read = "1";
            public const string Create = "2";
            public const string Edit = "3";
            public const string Disable = "4";
            public const string Delete = "5";
            public const string Active = "6";
        }

        public static class Notification
        {
            public const int System = 1;
            public const int HR = 2;
            public const int ExpireDoc = 3;
            public const int BHXH = 4;
            public const int Company = 5;
            public const string CreateHR = "Tạo mới tài khoản";
            public const string UpdateHR = "Chỉnh sửa thông tin tài khoản";
        }

        public static string SalaryPaymentMethod(int method)
        {
            switch (method)
            {
                case 1:
                    return "Chuyển khoản";
                default:
                    return "Tiền mặt";
            }
        }

        // 0: send | 1: ok | 2: fail, 3: make resend
        public static string EmailStatus(int status)
        {
            switch (status)
            {
                case 0:
                    return "Gửi";
                case 1:
                    return "Thành công";
                case 2:
                    return "Lỗi";
                case 3:
                    return "Gửi lại";
                default:
                    return string.Empty;
            }
        }

        public static string TimeKeeper(int status)
        {
            switch (status)
            {
                case 0:
                    return "Xác nhận công";
                case 2:
                    return "Chờ xác nhận";
                case 3:
                    return "Đã xác nhận";
                case 4:
                    return "Không xác nhận";
                case 5:
                    return "Đang lấy dữ liệu";
                default:
                    return string.Empty;
            }
        }

        public static string StringTangCa(int status)
        {
            switch (status)
            {
                case 1:
                    return "Cần xác nhận";
                case 2:
                    return "Chờ xác nhận";
                case 3:
                    return "Đã xác nhận";
                case 4:
                    return "Không xác nhận";
                default:
                    return string.Empty;
            }
        }

        public static string Married(string status)
        {
            switch (status)
            {
                case "M":
                    return "Kết hôn";
                case "S":
                    return "Độc thân";
                default:
                    return string.Empty;
            }
        }

        public static string Relation(int status)
        {
            switch (status)
            {
                case 3:
                    return "Con";
                case 2:
                    return "Vợ/chồng";
                case 1:
                    return "Cha/mẹ";
                default:
                    return string.Empty;
            }
        }

        public static string TaskStatusBhxh(int status)
        {
            switch (status)
            {
                case 1:
                    return "Hoàn thành";
                case 2:
                    return "Không xác định";
                default:
                    return "Chờ";
            }
        }

        public static string TaskBhxh(string status)
        {
            switch (status)
            {
                case "giam-bhxh":
                    return "Giảm BHXH";
                case "the-bhyt":
                    return "Thẻ BHYT";
                case "ghi-che-do-thai-san":
                    return "Ghi chế độ thai sản";
                case "ghi-che-do-om-dau":
                    return "Ghi chế độ ốm đau";
                case "ghi-che-do-tai-nan-lao-dong":
                    return "Ghi chế độ tai nan lao động";
                default:
                    return "Tăng BHXH";
            }
        }

        public static string StatusLeave(int status)
        {
            switch (status)
            {
                case 1:
                    return "Đồng ý";
                case 2:
                    return "Không duyệt";
                default:
                    return "Chờ duyệt";
            }
        }

        public static string GetHHMMSSFromSecond(double input)
        {
            if (input > 0)
            {
                int mySeconds = Convert.ToInt32(input);
                int myHours = mySeconds / 3600; //3600 Seconds in 1 hour
                mySeconds %= 3600;
                int myMinutes = mySeconds / 60; //60 Seconds in a minute
                mySeconds %= 60;
                string mySec = mySeconds.ToString(),
                myMin = myMinutes.ToString(),
                myHou = myHours.ToString();
                if (myHours < 10) { myHou = myHou.Insert(0, "0"); }
                if (myMinutes < 10) { myMin = myMin.Insert(0, "0"); }
                if (mySeconds < 10) { mySec = mySec.Insert(0, "0"); }
                if (mySec == "0") mySec = "00";
                return myHou + ":" + myMin + ":" + mySec;
            }
            else
            {
                return "0";
            }
        }

        public static string GetHHMMFromSecond(double input)
        {
            if (input > 0)
            {
                int mySeconds = Convert.ToInt32(input);
                int myHours = mySeconds / 3600; //3600 Seconds in 1 hour
                mySeconds %= 3600;
                int myMinutes = mySeconds / 60; //60 Seconds in a minute
                mySeconds %= 60;
                string mySec = mySeconds.ToString(),
                myMin = myMinutes.ToString(),
                myHou = myHours.ToString();
                if (myHours < 10) { myHou = myHou.Insert(0, "0"); }
                if (myMinutes < 10) { myMin = myMin.Insert(0, "0"); }
                return myHou + ":" + myMin;
            }
            else
            {
                return "00:00";
            }
        }

        public static string DayOfWeekT2(DateTime date)
        {
            int today = (int)date.DayOfWeek;
            switch (today)
            {
                case (int)DayOfWeek.Monday:
                    return "T2";
                case (int)DayOfWeek.Tuesday:
                    return "T3";
                case (int)DayOfWeek.Wednesday:
                    return "T4";
                case (int)DayOfWeek.Thursday:
                    return "T5";
                case (int)DayOfWeek.Friday:
                    return "T6";
                case (int)DayOfWeek.Saturday:
                    return "T7";
                default:
                    return "CN";
            }
        }

        public static string CTSTimeWork(int input)
        {
            switch (input)
            {
                case (int)ETimeWork.Sunday:
                    return "Chủ nhật";
                case (int)ETimeWork.Holiday:
                    return "Lễ tết";
                default:
                    return "Ngày thường";
            }
        }
    }
}

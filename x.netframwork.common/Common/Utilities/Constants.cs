using System;

namespace Common.Utilities
{
    public static class Constants
    {
        public const string ImgMissLink = "http://via.placeholder.com/";

        // System not store db. highest right
        public static class System
        {
            public const string account = "sysadmin";
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
        }

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

        }

        public static class ContactType
        {
            public const string personal = "personal";
            public const string company = "company";
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

        public static class Link
        {
            public const string Product = "p";
            public const string News = "n";
            public const string Job = "j";
            public const string Content = "c";
            public const string Employee = "hr";
        }

        public static class ImgSize
        {
            public const string desktop = "desktop";
            public const string tablet = "tablet";
            public const string mobile = "mobile";
            public const string thumb = "thumb";
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
        }

        public static class Collection
        {
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
            public const string NA = "NA";
            public const string Employees = "Employees";
            public const string BHYTHospitals = "BHYTHospitals";
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
                default:
                    return string.Empty;
            }
        }

    }
}

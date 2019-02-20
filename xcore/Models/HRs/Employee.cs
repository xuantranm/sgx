using Common.Utilities;
using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Models
{
    public class Employee : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Use store history
        public string EmployeeId { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Mã hệ thống")]
        public string Code { get; set; }

        [Display(Name = "Mã nhân viên")]
        public string CodeOld { get; set; }

        public IList<Workplace> Workplaces { get; set; }

        // true: not chấm công.
        public bool IsTimeKeeper { get; set; } = false;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LeaveLevelYear { get; set; } = 12;

        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        public string AliasFullName { get; set; }

        [Display(Name = "Họ và tên đệm")]
        public string FirstName { get; set; }

        [Display(Name = "Tên")]
        public string LastName { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Birthday { get; set; }

        [Display(Name = "Nguyên quán")]
        public string Bornplace { get; set; }

        [Display(Name = "Giới tính")]
        public string Gender { get; set; }

        [Display(Name = "Ngày vào làm")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Joinday { get; set; }

        // Last contract date
        [Display(Name = "Ngày hợp đồng")]
        [DataType(DataType.Date)]
        public DateTime Contractday { get; set; }

        // No use, use Enable
        [Display(Name = "Nghỉ việc")]
        public bool Leave { get; set; } = false;

        [Display(Name = "Ngày nghỉ việc")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Leaveday { get; set; }

        [Display(Name = "Lý do nghỉ việc")]
        public string LeaveReason { get; set; }

        // Nội dung bàn giao
        public string LeaveHandover { get; set; }

        [Display(Name = "Thường trú")]
        public string AddressResident { get; set; }

        [Display(Name = "Phường/Xã")]
        public string WardResident { get; set; }

        [Display(Name = "Quận/Huyện")]
        public string DistrictResident { get; set; }

        [Display(Name = "Tỉnh/Thành phố")]
        public string CityResident { get; set; }

        [Display(Name = "Nước")]
        public string CountryResident { get; set; }

        [Display(Name = "Tạm trú")]
        public string AddressTemporary { get; set; }

        [Display(Name = "Phường/Xã")]
        public string WardTemporary { get; set; }

        [Display(Name = "Quận/Huyện")]
        public string DistrictTemporary { get; set; }

        [Display(Name = "Tỉnh/Thành phố")]
        public string CityTemporary { get; set; }

        [Display(Name = "Nước")]
        public string CountryTemporary { get; set; }

        [Display(Name = "Phòng/ban")]
        public string Department { get; set; }

        public string DepartmentId { get; set; }

        public string DepartmentAlias { get; set; }

        [Display(Name = "Bộ phận")]
        public string Part { get; set; }

        public string PartId { get; set; }

        public string PartAlias { get; set; }

        public string ManagerId { get; set; }

        [Display(Name = "ĐT bàn")]
        [DataType(DataType.PhoneNumber)]
        public string Tel { get; set; }

        [Display(Name = "Di động")]
        public IList<EmployeeMobile> Mobiles { get; set; }

        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Display(Name = "Email cá nhân")]
        [DataType(DataType.EmailAddress)]
        public string EmailPersonal { get; set; }

        public bool ConfirmEmail { get; set; } = true;

        // Fast get data
        // data on LeaveEmployees
        [Display(Name = "Ngày phép sẵn có")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LeaveDayAvailable { get; set; } = 0;

        // Text
        [Display(Name = "Công việc")]
        public string Title { get; set; }

        public string TitleId { get; set; }

        public string TitleAlias { get; set; }

        // No use now. use title
        [Display(Name = "Chức vụ")]
        public string Function { get; set; }

        // Checked - Approved
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        [Display(Name = "Quyền truy cập")]
        public bool IsOnline { get; set; } = true;

        [Display(Name = "CMND")]
        public string IdentityCard { get; set; }

        [Display(Name = "Ngày cấp")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? IdentityCardDate { get; set; }

        [Display(Name = "Nơi cấp")]
        public string IdentityCardPlace { get; set; }

        public bool PassportEnable { get; set; } = false;

        [Display(Name = "Số Hộ chiếu")]
        public string Passport { get; set; }

        [Display(Name = "Loại Hộ chiếu")]
        public string PassportType { get; set; }

        [Display(Name = "Mã số Hộ chiếu")]
        public string PassportCode { get; set; }

        [Display(Name = "Ngày cấp")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? PassportDate { get; set; }

        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? PassportExpireDate { get; set; }

        [Display(Name = "Nơi cấp")]
        public string PassportPlace { get; set; }

        [Display(Name = "Số Hộ khẩu")]
        public string HouseHold { get; set; }

        [Display(Name = "Chủ hộ")]
        public string HouseHoldOwner { get; set; }

        [Display(Name = "Hôn nhân")]
        public string StatusMarital { get; set; }

        [Display(Name = "Dân tộc")]
        public string Nation { get; set; }

        [Display(Name = "Tôn giáo")]
        public string Religion { get; set; }

        [Display(Name = "Bằng cấp")]
        public IList<Certificate> Certificates { get; set; }

        [Display(Name = "Giấy tờ cá nhân")]
        public IList<Card> Cards { get; set; }
        
        // Work  manage in Work. No relationship. It big data.

        public IList<Contract> Contracts { get; set; }

        public IList<StorePaper> StorePapers { get; set; }

        #region BHXH
        public bool BhxhEnable { get; set; } = true;

        // Ngày bắt đầu đóng bhxh ở công ty tribat
        [Display(Name = "Ngày đóng BHXH")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? BhxhStart { get; set; }

        [Display(Name = "Ngày dừng đóng BHXH")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? BhxhEnd { get; set; }

        [Display(Name = "Số xổ BHXH")]
        public string BhxhBookNo { get; set; }

        [Display(Name = "Mã số BHXH")]
        public string BhxhCode { get; set; }

        [Display(Name = "Trạng thái")]
        public string BhxhStatus { get; set; }

        [Display(Name = "Nơi KCB ban đầu")]
        public string BhxhHospital { get; set; }

        [Display(Name = "Cơ quan BHXH")]
        public string BhxhLocation { get; set; }

        [Display(Name = "Mã số BHYT")]
        public string BhytCode { get; set; }

        [Display(Name = "Hiệu lực BHYT")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? BhytStart { get; set; }

        [Display(Name = "Hết hạn BHYT")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? BhytEnd { get; set; }

        [Display(Name = "Số tháng đóng bảo hiểm")]
        [BsonRepresentation(BsonType.Decimal128)]
        [DisplayFormat(DataFormatString = "{0:n0}")]
        public decimal BhxhMonth { get; set; }

        [DisplayFormat(DataFormatString = "{0:n0}")]
        public string BhxhYear { get; set; }

        // Ms.Thoa
        public IList<BhxhHistory> BhxhHistories { get; set; }
        #endregion

        public IList<EmployeeEducation> EmployeeEducations { get; set; }

        public IList<EmployeeMovement> EmployeeMovements { get; set; }

        public IList<EmployeeAward> EmployeeAwards { get; set; }

        public IList<EmployeeDiscipline> EmployeeDisciplines { get; set; }

        public IList<EmployeePower> EmployeePowers { get; set; }

        [Display(Name = "Số người phụ thuộc")]
        public int BhxhDependecy { get; set; } = 0;

        public IList<EmployeeFamily> EmployeeFamilys { get; set; }

        public IList<EmployeeContactRelate> EmployeeContactRelates { get; set; }

        public IList<EmployeeDocument> EmployeeDocuments { get; set; }

        public IList<EmployeeCheck> EmployeeChecks { get; set; }

        public IList<Image> Images { get; set; }

        public Image Avatar { get; set; }

        public Image Cover { get; set; }

        [Display(Name = "Giới thiệu")]
        public string Intro { get; set; }

        public EmployeeBank EmployeeBank { get; set; }
        // For fast data access, divide other collection
        //public IList<Notification> Notifications { get; set; }

        #region SALARIES
        /// <summary>
        /// Because chance employee. Manager here.
        /// Base [SalaryType]
        /// </summary>
        public int SalaryType { get; set; } = (int)EKhoiLamViec.VP;

        // Update on future: 0: hand, 1: bank
        public int SalaryPayMethod { get; set; } = 0;

        // Get newest from [SalaryEmployeeMonth]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Salary { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongBHXH { get; set; } = 0;

        // Tổng dư nợ hiện tại.
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Credit { get; set; } = 0;

        // Use Title.
        public string SalaryChucVu { get; set; }

        public string SalaryChucVuViTriCode { get; set; }

        public int SalaryNoiLamViecOrder { get; set; } = 0;

        public int SalaryPhongBanOrder { get; set; } = 0;

        public int SalaryChucVuOrder { get; set; } = 0;

        // nhóm vào chức vụ
        public string NgachLuong { get; set; }

        // Faster get data. base newest from [SalaryEmployeeMonth]
        public int SalaryLevel { get; set; } = 1;

        public double SalaryMauSo { get; set; } = 26;
        #endregion

        #region SALES
        public string SaleChucVu { get; set; }
        #endregion

        #region LOGISTICS
        public string LogisticChucVu { get; set; }
        #endregion

        #region Get
        public int Age
        {
            get
            {
                DateTime today = DateTime.Today;
                int age = today.Year - Birthday.Year;
                if (today < Birthday.AddYears(age))
                    age--;
                return age;
            }
        }

        public int RemainingBirthDays
        {
            get
            {
                DateTime today = DateTime.Today;
                DateTime next = Birthday.AddYears(today.Year - Birthday.Year);

                if (next < today)
                    next = next.AddYears(1);

                int numDays = (next - today).Days;

                return numDays;
            }
        }

        public int AgeBirthday
        {
            get
            {
                if (RemainingBirthDays == 0)
                {
                    // Today is birthday
                    return Age;
                }
                return Age + 1;
            }
        }

        public DateTime NextBirthDays
        {
            get
            {
                if (RemainingBirthDays == 0)
                {
                    // Today is birthday
                    return Birthday.AddYears(Age);
                }
                return Birthday.AddYears(Age + 1);
            }
        }

        public string BirthdayOfWeek
        {
            get
            {
                var culture = new CultureInfo("vi");
                return culture.DateTimeFormat.GetDayName(NextBirthDays.DayOfWeek);
            }
        }

        public int WeekBirthdayNumber
        {
            get
            {
                var culture = new CultureInfo("vi");
                return culture.Calendar.GetWeekOfYear(NextBirthDays, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            }
        }
        #endregion
    }
}

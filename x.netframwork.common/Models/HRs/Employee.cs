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
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string EmployeeId { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string Code { get; set; }

        public string CodeOld { get; set; }

        public IList<Workplace> Workplaces { get; set; }

        // true: not chấm công.
        public bool IsTimeKeeper { get; set; } = false;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LeaveLevelYear { get; set; } = 12;

        public string FullName { get; set; }

        public string AliasFullName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Birthday { get; set; }

        public string Bornplace { get; set; }

        public string Gender { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Joinday { get; set; }

        public int ProbationMonth { get; set; } = 2;

        public bool Official { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Contractday { get; set; }

        public int RemainingBhxh
        {
            get
            {
                DateTime today = DateTime.Today;
                DateTime next = Joinday.AddMonths(6);
                return (next - today).Days;
            }
        }

        public int ProbationAlert
        {
            get
            {
                DateTime today = DateTime.Today;
                DateTime next = Joinday.AddMonths(ProbationMonth);
                return (next - today).Days;
            }
        }

        public bool Leave { get; set; } = false;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Leaveday { get; set; }

        public string LeaveReason { get; set; }

        // Nội dung bàn giao
        public string LeaveHandover { get; set; }

        public string AddressResident { get; set; }

        public string WardResident { get; set; }

        public string DistrictResident { get; set; }

        public string CityResident { get; set; }

        public string CountryResident { get; set; }

        public string AddressTemporary { get; set; }

        public string WardTemporary { get; set; }

        public string DistrictTemporary { get; set; }

        public string CityTemporary { get; set; }

        public string CountryTemporary { get; set; }

        #region New 08.03.2019. Store Id. Search alias convert to id | Query convert to viewModel, future thay the Deparment, part,...
        public string CongTyChiNhanh { get; set; }

        public string KhoiChucNang { get; set; }

        public string PhongBan { get; set; }

        public string BoPhan { get; set; }

        public string BoPhanCon { get; set; }

        public string ChucVu { get; set; }

        public string CongTyChiNhanhName { get; set; }

        public string KhoiChucNangName { get; set; }

        public string PhongBanName { get; set; }

        public string BoPhanName { get; set; }

        public string BoPhanConName { get; set; }

        public string ChucVuName { get; set; }

        public string GhiChu { get; set; }
        #endregion

        public string ManagerId { get; set; }

        public string ManagerInformation { get; set; }

        public string Tel { get; set; }

        public IList<EmployeeMobile> Mobiles { get; set; }

        public string Email { get; set; }

        public string EmailPersonal { get; set; }

        public bool ConfirmEmail { get; set; } = true;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LeaveDayAvailable { get; set; } = 0;

        public string Status { get; set; }

        public bool IsOnline { get; set; } = true;

        public string IdentityCard { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? IdentityCardDate { get; set; }

        public string IdentityCardPlace { get; set; }

        public bool PassportEnable { get; set; } = false;

        public string Passport { get; set; }

        public string PassportType { get; set; }

        public string PassportCode { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? PassportDate { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? PassportExpireDate { get; set; }

        public string PassportPlace { get; set; }

        public string HouseHold { get; set; }

        public string HouseHoldOwner { get; set; }

        public string StatusMarital { get; set; }

        public string Nation { get; set; }

        public string Religion { get; set; }

        public IList<Certificate> Certificates { get; set; }

        public IList<Card> Cards { get; set; }
        
        public IList<Contract> Contracts { get; set; }

        public IList<StorePaper> StorePapers { get; set; }

        #region BHXH
        public bool BhxhEnable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? BhxhStart { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? BhxhEnd { get; set; }

        public string BhxhBookNo { get; set; }

        public string BhxhCode { get; set; }

        public string BhxhStatus { get; set; }

        public string BhxhHospital { get; set; }

        public string BhxhLocation { get; set; }

        public string BhytCode { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? BhytStart { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? BhytEnd { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BhxhMonth { get; set; }

        public string BhxhYear { get; set; }

        // Ms.Thoa
        public IList<BhxhHistory> BhxhHistories { get; set; }
        #endregion

        public IList<EmployeeEducation> EmployeeEducations { get; set; }

        public IList<EmployeeMovement> EmployeeMovements { get; set; }

        public IList<EmployeeAward> EmployeeAwards { get; set; }

        public IList<EmployeeDiscipline> EmployeeDisciplines { get; set; }

        public IList<EmployeePower> EmployeePowers { get; set; }

        public int BhxhDependecy { get; set; } = 0;

        public IList<EmployeeFamily> EmployeeFamilys { get; set; }

        public IList<EmployeeContactRelate> EmployeeContactRelates { get; set; }

        public IList<EmployeeDocument> EmployeeDocuments { get; set; }

        public IList<EmployeeCheck> EmployeeChecks { get; set; }

        public IList<Image> Images { get; set; }

        public Image Avatar { get; set; }

        public Image Cover { get; set; }

        public string Intro { get; set; }

        public EmployeeBank EmployeeBank { get; set; }

        public bool IsWelcomeEmail { get; set; } = true;

        public bool IsLeaveEmail { get; set; } = true;

        #region SALARIES
        public int SalaryType { get; set; } = (int)EKhoiLamViec.VP;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongBHXH { get; set; } = 0;

        public int NgachLuongLevel { get; set; } = 1; // bậc lương thang bang luong

        public string NgachLuongCode { get; set; } // Ma so

        public double ThamSoTinhLuong { get; set; } = 26; // bv:27

        // Get newest from [SalaryEmployeeMonth]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Salary { get; set; } = 0;

        // Tổng dư nợ hiện tại.
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Credit { get; set; } = 0;

        // Update on future: 0: hand, 1: bank
        public int SalaryPayMethod { get; set; } = 0;

        public string SalaryChucVu { get; set; }

        public string SalaryChucVuViTriCode { get; set; }
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

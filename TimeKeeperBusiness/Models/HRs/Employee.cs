using Common.Utilities;
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

        public string UserName { get; set; }

        public string Password { get; set; }

        public string Code { get; set; }

        public string CodeOld { get; set; }

        public IList<Workplace> Workplaces { get; set; }

        // true: not chấm công.
        public bool IsTimeKeeper { get; set; } = false;

        public string LeaveLevelYear { get; set; }

        public string FullName { get; set; }

        public string AliasFullName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Birthday { get; set; }

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

        public int AgeBirthday
        {
            get
            {
                DateTime today = DateTime.Today;
                int age = today.Year - Birthday.Year;
                if (today < Birthday.AddYears(age))
                    age--;
                return age+1;
            }
        }

        public DateTime NextBirthDays
        {
            get
            {
                return Birthday.AddYears(Age + 1);
            }
        }

        public int RemainingBirthDays
        {
            get
            {
                DateTime today = DateTime.Today;
                DateTime nextBirthday = Birthday.AddYears(Age + 1);

                TimeSpan difference = nextBirthday - DateTime.Today;

                return Convert.ToInt32(difference.TotalDays);
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

        public string Bornplace { get; set; }

        public string Gender { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Joinday { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Contractday { get; set; }

        public bool Leave { get; set; } = false;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Leaveday { get; set; }

        public string LeaveReason { get; set; }

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

        public string Part { get; set; }

        public string Department { get; set; }

        public string ManagerId { get; set; }

        public string Tel { get; set; }

        public IList<EmployeeMobile> Mobiles { get; set; }

        public string Email { get; set; }

        public string EmailPersonal { get; set; }

        public bool ConfirmEmail { get; set; } = true;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LeaveDayAvailable { get; set; } = 0;

        public string Title { get; set; }

        public string Function { get; set; }

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

        public int LevelSalary { get; set; } = 0;

        public int SalaryPayMethod { get; set; } = 0;

        public IList<Salary> Salaries { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Salary { get; set; } = 0;

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
    }

    public class EmployeeBank
    {
        public string BankAccount { get; set; }
        public string BankHolder { get; set; }
        public string BankName { get; set; }
        public string BankLocation { get; set; }
        public bool Enable { get; set; }
    }

    public class BhxhHistory
    {
        public string Task { get; set; }

        public string TaskDisplay { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DateAction { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DateResult { get; set; }

        public int Status { get; set; }
    }

    public class Workplace
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Fingerprint { get; set; }
        public string WorkingScheduleTime { get; set; }
        public bool Enable { get; set; } = true;
        public string Language { get; set; } = Constants.Languages.Vietnamese;
    }

    public class EmployeeRole
    {
        public string Function { get; set; }
        public int Right { get; set; }
    }

    public class EmployeeCheck
    {
        public int No { get; set; }
        public string EmployeeCode { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class EmployeeDocument
    {
        public string Name { get; set; }
        public string Content { get;set; }
    }

    public class EmployeeContactRelate
    {
        public int No { get; set; }
        public string FullName { get; set; }
        public string Mobile { get; set; }
    }

    public class EmployeeFamily
    {
        public int? Relation { get; set; }
        public string FullName { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Birthday { get; set; }
    }

    public class EmployeePower
    {
        public int Year { get; set; }
        public string Value { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class EmployeeDiscipline
    {
        public int No { get; set; }
        public string Content { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class EmployeeAward
    {
        public int No { get; set; }
        public string Content { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class EmployeeMovement
    {
        public int No { get; set; }
        public string Content { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class EmployeeEducation
    {
        public int No { get; set; }
        public string Content { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class Salary
    {
        public string Code { get; set; }

        public string Type { get; set; }

        public string Title { get; set; }

        public decimal Money { get; set; } = 0;

        public int Order { get; set; } = 0;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class StorePaper
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public int Count { get; set; }
        public string Unit { get; set; }
    }

    public class EmployeeMobile
    {
        public string Type { get; set; }

        public string Number { get; set; }
    }

    public class Card
    {
        public string Type { get; set; }

        public string Code { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Created { get; set; }

        public string Location { get; set; }

        public string Description { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Expired { get; set; }

        public IList<Image> Images { get; set; }

        public bool Enable { get; set; } = true;
    }

    public class Certificate
    {
        public string Type { get; set; }

        public string Location { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Created { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Expired { get; set; }

        public IList<Image> Images { get; set; }

        public bool Enable { get; set; } = true;
    }

    public class Contract
    {
        public string Type { get; set; }

        public string TypeName { get; set; }

        public string Code { get; set; }

        public string PLHD { get; set; }

        public string PhuLucDieuChinhLuong { get; set; }

        public string Description { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Start { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? End { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? Duration { get; set; }

        public IList<Image> Images { get; set; }

        public bool Enable { get; set; } = true;

        public DateTime NextContract
        {
            get
            {
                return End.HasValue ? End.Value.AddDays(1) : Constants.MinDate;
            }
        }

        public int RemainingContract
        {
            get
            {
                if (End.HasValue)
                {
                    DateTime today = DateTime.Today;
                    DateTime next = End.Value;

                    TimeSpan difference = next - DateTime.Today;

                    return Convert.ToInt32(difference.TotalDays);
                }
                else
                {
                    return 0;
                }
            }
        }
    }

    public class Children
    {
        public string Type { get; set; }

        public string FullName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Birthday { get; set; }

        public IList<Image> Images { get; set; }

        public bool Enable { get; set; } = true;
    }
}

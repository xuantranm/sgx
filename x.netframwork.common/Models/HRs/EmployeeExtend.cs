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

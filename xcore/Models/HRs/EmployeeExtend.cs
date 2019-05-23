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
    public class PhuCapPhucLoi
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NangNhocDocHai { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TrachNhiem { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuHut { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Xang { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DienThoai { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Com { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NhaO { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal KiemNhiem { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BhytDacBiet { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ViTriCanKnNhieuNam { get; set; } = 0;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ViTriDacThu { get; set; } = 0;
    }

    public class EmployeeBank
    {
        [Display(Name="Số tài khoản")]
        public string BankAccount { get; set; }
        [Display(Name = "Tên người hưởng")]
        public string BankHolder { get; set; }
        [Display(Name = "Tên ngân hàng")]
        public string BankName { get; set; }
        [Display(Name = "Chi nhánh")]
        public string BankLocation { get; set; }
        public bool Enable { get; set; }
    }

    public class BhxhHistory
    {
        [Display(Name = "Tác vụ")]
        public string Task { get; set; }

        public string TaskDisplay { get; set; }

        [Display(Name = "Ngày thực hiện")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DateAction { get; set; }

        [Display(Name = "Ngày trả kết quả")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DateResult { get; set; }

        [Display(Name = "Trạng thái")]
        // 0: Moi, 1: Cho, 2: Hoan thanh,...
        public int Status { get; set; }
    }

    public class Workplace
    {
        // Example: NM, VP
        [Display(Name="Mã")]
        public string Code { get; set; }

        [Display(Name = "Tên")]
        public string Name { get; set; }

        [Display(Name = "Mã chấm công")]
        public string Fingerprint { get; set; }

        [Display(Name = "Thời gian làm việc")]
        public string WorkingScheduleTime { get; set; }

        public bool Enable { get; set; } = true;
        public string Language { get; set; } = Constants.Languages.Vietnamese;
    }

    public class EmployeeRole
    {
        public string Function { get; set; }
        // 0: None; 1:Read ; 2:Create ; 3:Edit ; 4:Disable; 5:Delete; 6:Max ; 7: xxx; 8: sys
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
        // Chong:1, Vo:2, Con:3
        public int? Relation { get; set; }
        public string FullName { get; set; }

        [DataType(DataType.Date)]
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
        // Multi languages use texts
    }

    public class EmployeeMobile
    {
        [Display(Name = "Loại")]
        public string Type { get; set; }

        [Display(Name ="Số điện thoại")]
        [DataType(DataType.PhoneNumber)]
        public string Number { get; set; }
    }

    public class Card
    {
        [Display(Name = "Loại giấy tờ")]
        [Required]
        public string Type { get; set; }

        [Display(Name = "Số")]
        [Required]
        public string Code { get; set; }

        [Display(Name = "Ngày cấp")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Created { get; set; }

        [Display(Name = "Nơi cấp")]
        public string Location { get; set; }

        [Display(Name = "Chi tiết")]
        public string Description { get; set; }

        // if null => Vô thời hạn
        [Display(Name = "Thời hạn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Expired { get; set; }

        [Display(Name = "Hình ảnh")]
        // fullpath and each image divide ';'
        public string Images { get; set; }

        public bool Enable { get; set; } = true;
    }

    public class Certificate
    {
        [Display(Name = "Loại bằng cấp")]
        public string Type { get; set; }

        [Display(Name = "Nơi cấp")]
        public string Location { get; set; }

        [Display(Name = "Số")]
        public string Code { get; set; }

        [Display(Name = "Chi tiết")]
        public string Description { get; set; }

        [Display(Name = "Ngày cấp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Created { get; set; }

        // if null => Vô thời hạn
        [Display(Name = "Thời hạn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Expired { get; set; }

        [Display(Name = "Hình ảnh")]
        // fullpath and each image divide ';'
        public string Images { get; set; }

        public bool Enable { get; set; } = true;
    }

    public class Contract
    {
        [Display(Name = "Loại hợp đồng")]
        [Required]
        public string Type { get; set; }

        public string TypeName { get; set; }

        [Display(Name = "Số")]
        [Required]
        public string Code { get; set; }

        [Display(Name = "PLHĐ")]
        public string PLHD { get; set; }

        [Display(Name = " Phụ lục điều chỉnh lương")]
        public string PhuLucDieuChinhLuong { get; set; }

        public string Description { get; set; }

        [Display(Name = "Ngày hiệu lực")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Start { get; set; }

        // if null => Vô thời hạn
        [Display(Name = "Ngày hết hiệu lực")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? End { get; set; }

        [Display(Name = "Số năm")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? Duration { get; set; }

        [Display(Name = "Hình ảnh")]
        // fullpath and each image divide ';'
        public string Images { get; set; }

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
        [Display(Name = "Mối quan hệ")]
        [Required]
        public string Type { get; set; }

        [Display(Name = "Họ và tên")]
        [Required]
        public string FullName { get; set; }

        [Display(Name = "Ngày sinh")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Birthday { get; set; }

        [Display(Name = "Hình ảnh")]
        // fullpath and each image divide ';'
        public string Images { get; set; }

        public bool Enable { get; set; } = true;
    }
}

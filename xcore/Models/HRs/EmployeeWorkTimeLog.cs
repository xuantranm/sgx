using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class EmployeeWorkTimeLog: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string EmployeeId { get; set; }

        public string EmployeeName { get; set; }

        public string EmployeeTitle { get; set; }

        [Display(Name = "Mã chấm công")]
        public string EnrollNumber { get; set; }

        public int Year { get; set; } = DateTime.Now.Year;

        public int Month { get; set; } = DateTime.Now.Month;

        [Display(Name = "Loại chấm công")]
        public string VerifyMode { get; set; }

        public string WorkplaceCode { get; set; }

        [Display(Name = "Ngày chấm công")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        [Display(Name = "Giờ vô")]
        public TimeSpan? In { get; set; }

        [Display(Name = "Giờ ra")]
        public TimeSpan? Out { get; set; }

        [Display(Name = "Bắt đầu ca")]
        public TimeSpan Start { get; set; }

        [Display(Name = "Kết thúc ca")]
        public TimeSpan End { get; set; }

        [Display(Name = "Ngày công")]
        public TimeSpan WorkTime { get; set; }

        [Display(Name = "Ngày công")]
        public double WorkDay { get; set; } = 0;

        [Display(Name = "Trễ")]
        public TimeSpan Late { get; set; }

        [Display(Name = "Về sớm")]
        public TimeSpan Early { get; set; }

        public int StatusLate { get; set; } = 1;

        public int StatusEarly { get; set; } = 1;

        // 0: cần xác nhận công; 1: đủ ngày công ; 2: đã gửi xác nhận công, 3: đồng ý; 4: từ chối  
        [Display(Name = "Trạng thái")]
        public int Status { get; set; } = 1;

        // Save History
        public IList<AttLog> Logs { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [DataType(DataType.Date)]
        public DateTime DateOnlyRecord
        {
            get
            {
                return Date.Date;
            }
        }

        public TimeSpan TimeOnlyRecord
        {
            get
            {
                return Date.TimeOfDay;
            }
        }

        // XÁC NHẬN
        // User request, current use login user.
        public string Request { get; set; }

        [Display(Name ="Ngày gửi xác nhận công")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? RequestDate { get; set; }

        [Display(Name = "Lý do")]
        public string Reason { get; set; }

        [Display(Name ="Người xác nhận")]
        public string ConfirmId { get; set; }

        [Display(Name = "Người xác nhận")]
        public string ConfirmName { get; set; }

        [Display(Name = "Ngày xác nhận công")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? ConfirmDate { get; set; }

        // Use if 1: leave. ... define later
        public string InOutMode { get; set; }

        // Use define: Nghỉ, Chủ nhật, Không chấm công
        [Display(Name = "Work code")]
        public string Workcode { get; set; }

        public string SecureCode { get; set; }
    }
}

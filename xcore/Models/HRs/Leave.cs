using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// 1 day, 1 record
    /// Group by Code base date created
    /// Form, To is same date.
    /// </summary>
    public class Leave : Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [Display(Name = "Loại phép")]
        public string TypeId { get; set; }

        public string TypeName { get; set; }

        // 0 tru luong, 1 phép (năm, bù,...) khong tru luong.
        public bool Salary { get; set; }

        [Display(Name = "Mã nhân viên")]
        [Required]
        public string EmployeeId { get; set; }

        [Display(Name = "Tên nhân viên")]
        [Required]
        public string EmployeeName { get; set; }

        public string EmployeeTitle { get; set; }

        public string EmployeePart { get; set; }

        public string EmployeeDepartment { get; set; }

        // 1 cấp xác nhận (trường hợp 2 cấp. ;2.Id)
        // Format 1.Id
        public string ApproverId { get; set; }

        [Display(Name = "Người duyệt")]
        // Format 1.Name
        public string ApproverName { get; set; }

        public string ApproverEmail { get; set; }

        public string ApproverId2 { get; set; }

        [Display(Name = "Người duyệt")]
        // Format 1.Name
        public string ApproverName2 { get; set; }

        public string ApproverEmail2 { get; set; }

        // 0: new , 1: accept, 2: cancel, 3: pending,...
        [Display(Name = "Tình trạng")]
        public int Status { get; set; } = 0;

        [Display(Name = "Từ")]
        [DataType(DataType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime From { get; set; } = DateTime.Now;

        [Display(Name = "Thời gian bắt đầu")]
        public TimeSpan Start { get; set; }

        [Display(Name = "Đến")]
        [DataType(DataType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime To { get; set; } = DateTime.Now;

        [Display(Name = "Thời gian kết thúc")]
        public TimeSpan End { get; set; }

        [Display(Name = "Số ngày")]
        public double Number { get; set; }

        [Display(Name = "Lý do")]
        public string Reason { get; set; }

        [Display(Name = "Điện thoại liên hệ")]
        public string Phone { get; set; }

        [Display(Name = "Bình luận")]
        public string Comment { get; set; }

        public string WorkingScheduleTime { get; set; }

        public string GroupCode { get; set; } = DateTime.Now.ToString("ddMMyyyy");

        // Use link email.
        public string SecureCode { get; set; }

        public int Month { get; set; } = DateTime.Now.Month;

        public int Year { get; set; } = DateTime.Now.Year;
    }
}

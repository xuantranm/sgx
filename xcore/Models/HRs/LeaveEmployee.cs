using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Quản lý số ngày nghỉ phép còn lại (nghỉ phép, các loại nghỉ bù,...)
    // Khong quan ly nghi phep huong luong (thai san, dam cuoi,..)
    public class LeaveEmployee : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [Display(Name = "Loại phép")]
        public string LeaveTypeId { get; set; }

        [Display(Name = "Mã nhân viên")]
        [Required]
        public string EmployeeId { get; set; }

        public string LeaveTypeName { get; set; }

        [Display(Name = "Tên nhân viên")]
        public string EmployeeName { get; set; }

        [Display(Name = "Số ngày")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Number { get; set; } = 0;
    }
}

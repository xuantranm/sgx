using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // employee <=> employeesalaryMonth (1 <=> n)
    // Tính lương tháng
    public class EmployeeSalaryMonth : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; } = DateTime.Now.Year;

        public int Month { get; set; } = DateTime.Now.Month;

        public string UserId { get; set; }

        [Display(Name = "Mã nhân viên")]
        public string EmployeeCode { get; set; }

        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Display(Name = "Chức vụ")]
        public string Title { get; set; }

        [Display(Name = "Bậc lương")]
        public int LevelSalary { get; set; } = 0;

        public IList<SalaryInMonth> Salaries { get; set; }

        // Chưa tính giảm trừ
        [Display(Name = "Tổng thu nhập")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Total { get; set; }

        [Display(Name = "Làm tròn")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalRound { get; set; }

        [Display(Name = "Thực lãnh")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Summary { get; set; }
    }

    public class SalaryInMonth {
        // Thu nhập | Thu nhập khác | Các khoản giảm trừ
        public string Type { get; set; }

        // Số công ngày lương,...
        [Display(Name = "Nội dung lương")]
        public string Content { get; set; }

        [Display(Name = "ĐVT")]
        // Ngày | giờ
        public string Unit { get; set; }

        [Display(Name = "SL")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Quantity { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Total { get; set; }

        // 0: cố định, 1: theo ngày làm việc, 2: thời gian, 3 ...
        [Display(Name = "Cách tính")]
        public int MethodCalculator { get; set; } = 0;

        public int Order { get; set; } = 0;
    }
}

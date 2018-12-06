using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Data từng tháng,
    /// </summary>
    public class SalaryNhaMayCong
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; } = DateTime.Now.Year;

        public int Month { get; set; } = DateTime.Now.Month;

        public string EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeName { get; set; }

        public string EmployeeChucVu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CongTong { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ComSX { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ComKD { get; set; } = 0;

        public bool Enable { get; set; } = true;

        public string CreatedOn { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        public string UpdatedOn { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Data từng tháng,
    /// </summary>
    public class SalarySanXuatCong
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

        #region First time, analytics later.
        // Because Tang Ca, CN, Le must be accept leader.
        // Put here for fast
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal GioTangCa { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal GioLamViecCN { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal GioLamViecLeTet { get; set; } = 0;
        #endregion

        public bool Enable { get; set; } = true;

        public string CreatedOn { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        public string UpdatedOn { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

    }
}

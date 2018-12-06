using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SalaryPhuCapPhucLoi
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Type { get; set; } = (int)EPCPL.PC;

        public int Order { get; set; } = 1;

        public string Name { get; set; }

        public string Code { get; set; }

        public string NameAlias { get; set; }

        // Theo luat VN, false theo cong ty
        public bool Law { get; set; } = true;

        /// <summary>
        /// Thoi gian ap dung.
        /// Dung de cap nhat thang luong moi, lich sử,...
        /// Get lastest base Month + Year
        /// </summary>
        public int Month { get; set; } = DateTime.Now.Month;

        public int Year { get; set; } = DateTime.Now.Year;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
    }
}

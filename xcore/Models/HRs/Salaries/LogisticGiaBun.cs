using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// KPI từng tháng,
    /// Gọi chịu khó groupby Type and Condition.
    /// Mục đích mở rộng về sau.
    /// Hay mỗi type 1 field...
    /// Mở rộng hay thêm bớt thêm field data.
    /// </summary>
    public class LogisticGiaBun
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string Name { get; set; }

        public string Alias { get; set; }

        public string Code { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HoTroTienComTinh { get; set; } = 0;

        public bool Enable { get; set; } = true;

        public string CreatedOn { get; set; } = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");

        public string UpdatedOn { get; set; } = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");

    }
}

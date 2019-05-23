using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class LeaveType : Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }

        public double YearMax { get; set; } = 0;
 
        public double? MonthMax { get; set; } = 0;

        public double? MaxOnce { get; set; } = 0;

        public bool SalaryPay { get; set; } = true;

        public bool Display { get; set; } = true;
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class TruckCheck:Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string XeCode { get; set; }

        [Required]
        public string Content { get; set; }

        // Xác nhận/ Không xác nhận,...
        public string Status { get; set; }
    }
}

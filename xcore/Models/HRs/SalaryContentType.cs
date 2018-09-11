using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Tính ra lương nhân viên (chưa tính tháng)
    public class SalaryContentType: Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        
        [Required]
        public string Name { get; set; }

        public string Alias { get; set; }
        
        public string Description { get; set; }

        // sort 1->....
        public int Order { get; set; } = 1;
    }
}

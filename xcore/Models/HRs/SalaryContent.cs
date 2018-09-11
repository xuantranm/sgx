using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SalaryContent: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string SalaryId { get; set; }
        public string SalaryAlias { get; set; }
        public string SalaryType { get; set; }

        [Required]
        public string Name { get; set; }

        public string Alias { get; set; }
        
        public string Description { get; set; }

        // sort 1->....
        public int Order { get; set; } = 1;
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Part belong Department
    /// </summary>
    // Bộ phận
    public class Part
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Code { get; set; }

        [Required]
        public string Name { get; set; }

        public string Alias { get; set; }
        
        public string Description { get; set; }

        public string Images { get; set; }

        // sort 1->....
        public int Order { get; set; } = 1;

        public string ParentCode { get; set; }

        public string DepartmentCode { get; set; }

        public string DepartmentName { get; set; }

        public bool Enable { get; set; } = true;
    }
}

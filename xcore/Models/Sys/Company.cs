using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Company
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Code { get; set; }

        public string Parent { get; set; }

        [Required]
        public string Name { get; set; }

        public string Telephone { get; set; }

        public string Telephone2 { get; set; }

        public string Hotline { get; set; }

        public string Fax { get; set; }

        public string Tax { get; set; }

        public string Email { get; set; }

        public IList<CompanyLocation> CompanyLocations { get; set; }

        public string Language { get; set; }

        public bool Enable { get; set; } = true;
    }

    public class CompanyLocation
    {
        public string Address { get; set; }
        public bool Enable { get; set; } = true;
    }
}

using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Information only
    /// </summary>
    public class Customer : Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Type { get; set; } = (int)ECustomer.Client;

        [Required]
        public string Name { get; set; }

        public string Alias { get; set; }

        public string Code { get; set; }

        public string Address { get; set; }

        public string District { get; set; }

        public string City { get; set; }

        public string Mobile { get; set; }

        public string ParentId { get; set; }
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Bank: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }

        public string Shorten { get; set; }

        public string Alias { get; set; }

        public string Location { get; set; }
        
        public string Type { get; set; }

        public Image Image { get; set; }
    }
}

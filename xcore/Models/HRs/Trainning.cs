using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Trainning : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }

        // Excel,....
        public string Type { get; set; }

        public string Link {get;set;}

        // 1: Youtube/ 2: vimeo
        public int Source { get; set; } = 1;

        public int View { get; set; }

    }
}

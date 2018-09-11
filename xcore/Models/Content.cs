using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Models
{
    // Manage all: news, jobs,...
    public class Content : Extension
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Key, example: home | about | product
        public string Code { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Body { get; set; }

        // link base 
        // link base alias
        public string Name { get; set; }

        public string Alias { get; set; }

        public IList<Image> Images { get; set; }

        public bool Enable { get; set; } = true;

        public string Language { get; set; } = Constants.Languages.Vietnamese;
    }
}

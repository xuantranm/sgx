using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class SEO
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Key, example: home | about | product
        public string Code { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string MetaOwner { get; set; }

        public string Canonical { get; set; }

        public string KeyWords { get; set; }

        public string OgUrl { get; set; }

        public string OgTitle { get; set; }

        public string OgDescription{ get; set; }

        // link base 
        // link base alias
        public string Name { get; set; }

        public string Alias { get; set; }

        public string Language { get; set; } = Constants.Languages.Vietnamese;
    }
}

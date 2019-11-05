using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Models
{
    // Move to Img
    public class Image
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // each item 1 folder by id
        public string Path { get; set; }

        // sizes: + _desktop,_tablet,_mobile,_thumb,...
        public string FileName { get; set; }

        public string OrginalName { get; set; }

        public string Title { get; set; }

        public int Order { get; set; }

        public bool Main { get; set; }

        // ex: avatar, cover, product,....
        public string Type { get; set; }
    }
}

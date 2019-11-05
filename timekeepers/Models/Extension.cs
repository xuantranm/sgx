using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Models
{
    public class Extension
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Code { get; set; } // For multi languages

        public int CodeInt { get; set; }

        public Seo Seo { get; set; }

        public IList<Tag> Tags { get; set; }

        public long Timestamp { get; set; } = DateTime.Now.Ticks;

        public bool Publish { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime PublishOn { get; set; } = DateTime.Now;

        public bool Enable { get; set; } = true;

        public int Usage { get; set; } = 0;

        public bool NoDelete { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ModifiedOn { get; set; } = DateTime.Now;

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }

        public string Language { get; set; }

        public string Domain { get; set; }

        public int ModeData { get; set; }

        public DateTime Start { get; set; } = DateTime.Now;

        public DateTime? Expired { get; set; }
    }

    public class Tag
    {
        public string Name { get; set; }

        public string Link { get; set; }
    }
}

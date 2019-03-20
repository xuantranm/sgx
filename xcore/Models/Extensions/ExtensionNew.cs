using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Models
{
    // Apply new entity.
    // If edit current, more times.
    public class ExtensionNew
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // For multi update, general by system
        public string Timestamp { get; set; } = DateTime.Now.ToString("yyyyMMddHHmmssfff");

        public bool Enable { get; set; } = true;

        public int Usage { get; set; } = 0;

        public bool NoDelete { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public string CreatedBy { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ModifiedOn { get; set; } = DateTime.Now;

        public string ModifiedBy { get; set; }


        #region SEO
        public string SeoTitle { get; set; }
        public string KeyWords { get; set; }
        public string MetaOwner { get; set; }
        public string Canonical { get; set; }
        public string OgUrl { get; set; }
        public string OgTitle { get; set; }
        public string OgDescription { get; set; }
        public string SeoFooter { get; set; }
        public string Robots { get; set; } = Constants.Seo.indexFollow;
        public string RelationshipCategory { get; set; }
        public string RelationshipItem { get; set; }
        #endregion
    }
}

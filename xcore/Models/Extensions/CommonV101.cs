using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Models
{
    public class CommonV101
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public bool NoDelete { get; set; } = false;
        // For check use?
        public int Usage { get; set; } = 0;
        // For multi update, general by system
        public string Timestamp { get; set; } = DateTime.Now.ToString("yyyyMMddHHmmssfff");

        public string Language { get; set; } = Constants.Languages.Vietnamese;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CheckedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ApprovedOn { get; set; } = DateTime.Now;

        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string CheckedBy { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;

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

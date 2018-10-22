using Common.Utilities;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Models
{
    public class Common
    {
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
    }
}

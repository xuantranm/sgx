using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Notification: CommonV101
    {
        public int Type { get; set; } = (int)ENotification.None;

        public string Title { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }

        public string Content { get; set; }

        // No use
        public string Link { get; set; }

        public IList<Image> Images { get; set; }

        public IList<Document> Documents { get; set; }

        // Thông báo cho từng user| null theo type.
        public string User { get; set; }
    }
}

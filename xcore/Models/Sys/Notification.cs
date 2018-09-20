using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Store data user
    public class Notification
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // 1: system, 2.nhan-su-change, 3: expire-document, 4: task-bhxh, 5: company, 6:...
        public int Type { get; set; } = 0;

        public string Title { get; set; }

        public string Content { get; set; }

        public string Link { get; set; }

        public IList<Image> Images { get; set; }

        // Thông báo cho từng user| null theo type.
        public string UserId { get; set; }

        public string CreatedBy { get; set; }

        public string CreatedByName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public bool Enable { get; set; } = true;
    }
}

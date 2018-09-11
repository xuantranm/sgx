using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Store data user
    public class NotificationAction
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }

        public string NotificationId { get; set; }

        // 0: None, 1: Viewed, 2: Deleted , ...
        // Rule: ex: 1 not show in notification of user. ...
        public int Action { get; set; } = 0;

        public DateTime UpdatedOn { get; set; } = DateTime.Now;
    }
}

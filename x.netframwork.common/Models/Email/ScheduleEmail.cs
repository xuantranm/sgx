using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Models
{
    public class ScheduleEmail
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public List<EmailAddress> From { get; set; }

        public List<EmailAddress> To { get; set; }

        public List<EmailAddress> CC { get; set; }

        public List<EmailAddress> BCC { get; set; }

        public string Type { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        // if 4: schedule (use template base [type]) , after sent update status normal.
        public int Status { get; set; } = (int)EEmailStatus.Send;

        public string Error { get; set; }

        // Số lần gửi lỗi
        public int ErrorCount { get; set; } = 0;

        public string CreatedOn { get; set; } = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");

        public string UpdatedOn { get; set; } = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");
    }
}

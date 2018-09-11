using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace timekeepers.Models
{
    public class EmployeeWorkTimeLog
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string EnrollNumber { get; set; }

        public string VerifyMode { get; set; }

        public string InOutMode { get; set; }

        public string Workcode { get; set; }

        public string WorkplaceCode { get; set; }

        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }

        // Rule by times
        public TimeSpan WorkTime { get; set; }

        // Rule by day
        public decimal WorkDay { get; set; } = 0;

        public TimeSpan Late { get; set; }

        public TimeSpan Early { get; set; }

        // 0: cần xác nhận công; 1: đủ ngày công ; 2: đã gửi xác nhận công, 3: đồng ý; 4: từ chối  
        public int Status { get; set; } = 1;

        public string Request { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        public string ConfirmId { get; set; }

        public string ConfirmName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ConfirmDate { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }
    }
}

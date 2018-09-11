using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class EmployeeWorkTimeLog: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string EnrollNumber { get; set; }

        public string VerifyMode { get; set; }

        public string WorkplaceCode { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        public TimeSpan? In { get; set; }

        public TimeSpan? Out { get; set; }

        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }

        public TimeSpan WorkTime { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal WorkDay { get; set; } = 0;

        public TimeSpan Late { get; set; }

        public TimeSpan Early { get; set; }

        public int Status { get; set; } = 1;

        // Save History
        public IList<AttLog> Logs { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime DateOnlyRecord
        {
            get
            {
                return Date.Date;
            }
        }

        public TimeSpan TimeOnlyRecord
        {
            get
            {
                return Date.TimeOfDay;
            }
        }

        // XÁC NHẬN
        public string Request { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? RequestDate { get; set; }

        public string ConfirmId { get; set; }

        public string ConfirmName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? ConfirmDate { get; set; }

        // No use
        public string InOutMode { get; set; }

        public string Workcode { get; set; }
    }
}

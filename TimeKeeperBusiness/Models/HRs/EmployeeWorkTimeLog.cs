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

        public string EmployeeId { get; set; }

        public string EmployeeName { get; set; }

        public string EmployeeTitle { get; set; }

        public string EnrollNumber { get; set; }

        public int Year { get; set; } = DateTime.Now.Year;

        public int Month { get; set; } = DateTime.Now.Month;

        public string VerifyMode { get; set; }

        public string WorkplaceCode { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        public TimeSpan? In { get; set; }

        public TimeSpan? Out { get; set; }

        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }

        public TimeSpan WorkTime { get; set; }

        public double WorkDay { get; set; } = 0;

        public TimeSpan Late { get; set; }

        public TimeSpan Early { get; set; }

        public int StatusLate { get; set; } = 1;

        public int StatusEarly { get; set; } = 1;

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

        public string Reason { get; set; }

        public string ConfirmId { get; set; }

        public string ConfirmName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? ConfirmDate { get; set; }

        // Use if 1: leave. ... define later
        public string InOutMode { get; set; }

        public string Workcode { get; set; }
    }
}

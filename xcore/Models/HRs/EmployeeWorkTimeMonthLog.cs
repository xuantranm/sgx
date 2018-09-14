using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Mục đích lấy dữ liệu nhanh, phục vụ tính lương, danh sách, report,...
    // Mỗi tháng 1 records.
    public class EmployeeWorkTimeMonthLog
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string EmployeeId { get; set; }

        public string EnrollNumber { get; set; }

        public int Year { get; set; } = DateTime.Now.Year;

        public int Month { get; set; } = DateTime.Now.Month;

        public double Workday { get; set; }

        public double WorkTime { get; set; }
        
        public double Late { get; set; }

        public double Early { get; set; }

        // số lần cho phép trể trong tháng
        public int LateCountAllow { get; set; }

        // số phút cho phép trể 1 lần
        public int LateMinuteAllow { get; set; }

        // số lần dùng cho phép trể trong tháng
        public int LateCountAllowUsed { get; set; }

        // Số lần đi trễ trong tháng
        public int LateCount { get; set; }

        // số lần cho phép về sớm trong tháng
        public int EarlyCountAllow { get; set; }

        // số phút cho phép về sớm 1 lần
        public int EarlyMinuteAllow { get; set; }

        // số lần dùng cho phép về sớm trong tháng
        public int EarlyCountAllowUsed { get; set; }

        // Số lần về sớm trong tháng
        public int EarlyCount { get; set; }

        public double Sunday { get; set; }

        public double Holiday { get; set; }

        public double LeaveDate { get; set; }

        public double LeaveDateNotApprove { get; set; }

        public double LeaveDateApproved { get; set; }

        // Rule from 26
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime From { get; set; }

        // Last update data timekeeper
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime To { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}

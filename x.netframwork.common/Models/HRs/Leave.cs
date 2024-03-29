﻿using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using Common.Enums;

namespace Models
{
    /// <summary>
    /// 1 day, 1 record
    /// Group by Code base date created
    /// Form, To is same date.
    /// </summary>
    public class Leave : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string TypeId { get; set; }

        public string TypeName { get; set; }

        // 0 tru luong, 1 phép (năm, bù,...) khong tru luong.
        public bool Salary { get; set; }

        public string EmployeeId { get; set; }

        public string EmployeeName { get; set; }

        public string EmployeeTitle { get; set; }

        public string EmployeePart { get; set; }

        public string EmployeeDepartment { get; set; }

        // 1 cấp xác nhận (trường hợp 2 cấp. ;2.Id)
        // Format 1.Id
        public string ApproverId { get; set; }

        public string ApproverName { get; set; }

        public string ApproverEmail { get; set; }

        public string ApproverId2 { get; set; }

        public string ApproverName2 { get; set; }

        public string ApproverEmail2 { get; set; }

        public int Status { get; set; } = (int)StatusLeave.New;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime From { get; set; } = DateTime.Now;

        public TimeSpan Start { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime To { get; set; } = DateTime.Now;

        public TimeSpan End { get; set; }

        public double Number { get; set; }

        public string Reason { get; set; }

        public string Phone { get; set; }

        public string Comment { get; set; }

        public string WorkingScheduleTime { get; set; }

        public string GroupCode { get; set; } = DateTime.Now.ToString("ddMMyyyy");

        // Use link email.
        public string SecureCode { get; set; }

        public int Month { get; set; } = DateTime.Now.Month;

        public int Year { get; set; } = DateTime.Now.Year;
    }
}

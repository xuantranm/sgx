using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Quản lý số ngày nghỉ phép còn lại (nghỉ phép, các loại nghỉ bù,...)
    // Khong quan ly nghi phep huong luong (thai san, dam cuoi,..)
    public class LeaveEmployee : Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string LeaveTypeId { get; set; }

        public string EmployeeId { get; set; }

        public string LeaveTypeName { get; set; }

        public string EmployeeName { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Number { get; set; } = 0;

        public string Department { get; set; }

        public string Part { get; set; }

        public string Title { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LeaveLevel { get; set; } = 12;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NumberUsed { get; set; } = 0;

        // Define probation. Probation no use leave phep-nam.
        // UseFlag = false if probation
        public bool UseFlag { get; set; } = true;
    }
}

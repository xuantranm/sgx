using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Get full data in viewmodel, same employee
    /// </summary>
    public class EmployeeWorkTimeLog: Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        #region Employee
        public string EmployeeId { get; set; }

        public string EmployeeName { get; set; }
        
        public string Department { get; set; }

        public string DepartmentId { get; set; }

        public string DepartmentAlias { get; set; }

        public string Part { get; set; }

        public string PartId { get; set; }

        public string PartAlias { get; set; }

        public string EmployeeTitleId { get; set; }

        public string EmployeeTitle { get; set; }

        public string EmployeeTitleAlias { get; set; }

        public string EnrollNumber { get; set; }
        #endregion

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

        public TimeSpan Lunch { get; set; } = new TimeSpan(1, 0, 0);

        public TimeSpan OtherRelax { get; set; } = new TimeSpan(0, 0, 0);

        public TimeSpan WorkTime { get; set; }

        // Giờ tăng ca thực tế
        public TimeSpan TangCaThucTe { get; set; }

        public int StatusTangCa { get; set; } = (int)ETangCa.None;

        // Sau khi xác nhận
        public TimeSpan TangCaDaXacNhan { get; set; }

        public double OtThucTeD { get; set; }

        public double OtXacNhanD { get; set; }

        public int Status { get; set; } = (int)EStatusWork.DuCong;

        // Save History
        public IList<AttLog> Logs { get; set; }

        // XÁC NHẬN
        public string Request { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? RequestDate { get; set; }

        public string Reason { get; set; }

        public string ReasonDetail { get; set; }

        public string ConfirmId { get; set; }

        public string ConfirmName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? ConfirmDate { get; set; }

        public string InOutMode { get; set; }
        
        public int Mode { get; set; } = (int)ETimeWork.Normal;

        #region Leave
        public double SoNgayNghi { get; set; } = 0;
        #endregion

        // use define salary location
        // base luong [SalaryType] : VP, NM, SX
        public int Workcode { get; set; }

        public string SecureCode { get; set; }

        public bool IsSendMail { get; set; } = false;

        #region Automactic
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
        #endregion

        #region No Use
        public double WorkDay { get; set; } = 0;
        public TimeSpan Late { get; set; }
        public TimeSpan Early { get; set; }
        public int StatusLate { get; set; } = (int)EStatusWork.DuCong;
        public int StatusEarly { get; set; } = (int)EStatusWork.DuCong;
        #endregion
    }
}

using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models
{
    public class OvertimeEmployee: Extension
    {
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        public TimeSpan StartOvertime { get; set; }

        public TimeSpan EndOvertime { get; set; }

        public double Hour { get; set; }

        public TimeSpan StartSecurity { get; set; }

        public TimeSpan EndSecurity { get; set; }

        public double HourSecurity { get; set; }

        public int Food { get; set; } = 0;

        public string Description { get; set; }

        public bool Agreement { get; set; } = true;

        public int Status{ get; set; } = (int)EOvertime.Create;

        public int Type { get; set; } = (int)EDateType.Normal;

        public int Year { get; set; } = DateTime.Now.Year;

        public int Month { get; set; } = DateTime.Now.Month;

        // Not use, use UI only
        public bool CheckOnUI { get; set; } = true;

        public bool IsDelete { get; set; } = false;

        public string Document
        {
            get
            {
                var sFileName = "bang-tang-ca-code-" + Code + ".xlsx";
                return Path.Combine("documents", "overtimes", Date.ToString("yyyyMMdd"), sFileName);
            }
        }

        public string ApprovedBy { get; set; } = string.Empty;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ApprovedOn { get; set; } = DateTime.Now;

        #region Employee
        public string ManagerId { get; set; }

        public string ManagerInfo { get; set; }

        public string EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeName { get; set; }

        public string EmployeeAlias { get; set; }

        public string ChucVuId { get; set; }

        public string ChucVuName { get; set; }

        public string ChucVuAlias { get; set; }

        public string BoPhanId { get; set; }

        public string BoPhanName { get; set; }

        public string BoPhanAlias { get; set; }

        public string PhongBanId { get; set; }

        public string PhongBanName { get; set; }

        public string PhongBanAlias { get; set; }

        public string KhoiChucNangId { get; set; }

        public string KhoiChucNangName { get; set; }

        public string KhoiChucNangAlias { get; set; }

        public string CongTyChiNhanhId { get; set; }

        public string CongTyChiNhanhName { get; set; }

        public string CongTyChiNhanhAlias { get; set; }
        #endregion
    }
}

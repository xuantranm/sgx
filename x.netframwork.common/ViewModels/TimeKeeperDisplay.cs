using Common.Utilities;
using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Models;

namespace ViewModels
{
    public class TimeKeeperDisplay
    {
        public IList<EmployeeWorkTimeLog> EmployeeWorkTimeLogs { get; set; }

        //public EmployeeWorkTimeLog EmployeeWorkTimeLog { get; set; }

        public string Id { get; set; }

        public string Code { get; set; }

        public string EnrollNumber { get; set; }

        public string FullName { get; set; }

        public string CongTyChiNhanh { get; set; }

        public string KhoiChucNang { get; set; }

        public string PhongBan { get; set; }

        public string BoPhan { get; set; }

        public string BoPhanCon { get; set; }

        public string ChucVu { get; set; }

        public string Alias { get; set; }

        public string Email { get; set; }

        public string ManageId { get; set; }
    }
}

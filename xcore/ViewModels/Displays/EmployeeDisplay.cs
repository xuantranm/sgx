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
    public class EmployeeDisplay
    {
        public Employee Employee { get; set; }

        // Get name
        public string CongTyChiNhanh { get; set; }

        public string KhoiChucNang { get; set; }

        public string PhongBan { get; set; }

        public string BoPhan { get; set; }

        public string BoPhanCon { get; set; }

        public string ChucVu { get; set; }

        public string NguoiQuanLy { get; set; }

        public string NguoiQuanLyChucVu { get; set; }
    }
}

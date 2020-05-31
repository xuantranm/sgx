using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class TimerAnalytic
    {
        public bool Miss { get; set; } = false;

        public double Workday { get; set; } = 0;

        public double NgayNghiP { get; set; } = 0;

        public double LeTet { get; set; } = 0;
        public int Late { get; set; } = 0;
        public int VaoTreLan { get; set; } = 0;
        public int Early { get; set; } = 0;
        public int RaSomLan { get; set; } = 0;
        public string DisplayInOut { get; set; } = string.Empty;
        public double OtNormalReal { get; set; } = 0;
        public double OtSundayReal { get; set; } = 0;
        public double OtHolidayReal { get; set; } = 0;
        public double TangCaNgayThuong { get; set; } = 0;
        public double TangCaChuNhat { get; set; } = 0;
        public double TangCaLeTet { get; set; } = 0;
        public double VangKP { get; set; } = 0;
    }
}

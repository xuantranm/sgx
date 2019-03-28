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
    public class ExcelViewModel
    {
        public string FileNameFullPath { get; set; }

        public double NgayCongNT { get; set; }

        public double NgayNghiP { get; set; }
    }
}

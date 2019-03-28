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
    public class KhoNguyenLieuDisplay
    {
        public KhoNguyenLieu KhoNguyenLieu { get; set; }

        public string TenSanPham { get; set; }

        public string MaSanPham { get; set; }

        public string DVT { get; set; }


        public string Type { get; set; }

        // Continute...
    }
}

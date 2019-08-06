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
    public class PhongBanBoPhanDisplay
    {
        public PhongBan PhongBanBoPhan { get; set; }

        // Get name
        public string KhoiChucNangId { get; set; }

        public string KhoiChucNangName { get; set; }

        public int KhoiChucNangOrder { get; set; }
    }
}

using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Role quan ly quyen => Move to Categories : Type Role
    /// Right quan ly quyen theo location (NM,VP,...),chuc vu, nguoi dung
    /// </summary>
    public class Right : Extension
    {
        public string RoleId { get; set; }
        public int Type { get; set; } = (int)ERightType.User;
        public string ObjectId { get; set; }
        public int Action { get; set; } = (int)ERights.View;
    }
}

using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Role quan ly quyen
    /// Right quan ly quyen theo nguoi dung, chuc vu 
    /// </summary>
    public class Right : Extension
    {
        public string RoleId { get; set; }
        public int Type { get; set; } = (int)ERightType.User;
        public string ObjectId { get; set; }
        public int Action { get; set; } = (int)ERights.View;
        public DateTime Start { get; set; } = DateTime.Now;
        public DateTime? End { get; set; }
    }
}

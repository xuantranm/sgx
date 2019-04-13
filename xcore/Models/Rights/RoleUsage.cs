using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Quan ly quyen
    /// Role => RoleUsage => User | GroupPolicy
    /// If GroupPolicy : users or titles
    /// </summary>
    public class RoleUsage : CommonV101
    {
        public int Type { get; set; } = (int)ERoleControl.User;

        public string ObjectId { get; set; } // User Id or GroupPolicy Id

        public string ObjectAlias { get; set; }

        public string RoleId { get; set; }

        public string RoleAlias { get; set; }

        public int Right { get; set; } = (int)ERights.None;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Start { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Expired { get; set; }
    }
}

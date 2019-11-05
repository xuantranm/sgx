using Attributes;
using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // 10 first code use notice system.
    public class Text : Extension
    {
        public int Group { get; set; } = (int)EText.System;

        public string Value { get; set; }

        public string Alias { get; set; }

        public string ToolTip { get; set; }
    }
}

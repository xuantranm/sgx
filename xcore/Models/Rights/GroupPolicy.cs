using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Assign chức vụ into group
    /// Assign employeeId into group
    /// </summary>
    public class GroupPolicy : CommonV101
    {
        public int Type { get; set; } = (int)EGroupPolicy.Title;

        public string Name { get; set; }

        public string Objects { get; set; } // more divide by ;

        public string Alias { get; set; }

        public string Description { get; set; }
    }
}

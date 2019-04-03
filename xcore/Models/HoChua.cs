using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Information only
    /// </summary>
    public class HoChua : CommonV101
    {
        public string Name { get; set; }

        public string Alias { get; set; }

        public string Code { get; set; }

        public string Note { get; set; }
    }
}

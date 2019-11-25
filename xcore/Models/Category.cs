using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// All small data store here: Ca, Cong Doan, Xe, Phan Loại Xe,...
    /// If category belong another category, use [ParentId] (Id of parent category)
    /// Ex: HR: First time, Category Company => ... (parent => child)
    /// </summary>
    public class Category : Extension
    {
        public int Type { get; set; } = (int)ECategory.Role;

        [Required]
        public string Name { get; set; }

        public string Alias { get; set; }

        public string Value { get; set; } // Probation...

        public string Description { get; set; }

        public IList<Property> Properties { get; set; }

        public IList<ContentIn> Contents { get; set; }

        public string ParentId { get; set; }
    }
}

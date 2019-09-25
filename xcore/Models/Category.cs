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
    /// </summary>
    public class Category : Extension
    {
        public int Type { get; set; } = (int)ECategory.Ca;

        public string Name { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }

        public string Content { get; set; } // Value

        public IList<Img> Images { get; set; }

        public IList<Document> Documents { get; set; }

        public string ParentId { get; set; }
    }
}

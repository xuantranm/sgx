using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // 10 first code use notice system.
    public class ProductCategorySale : Extension
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        public int Code { get; set; }

        #region Parent

        public int ParentCode { get; set; }

        public string ParentName { get; set; }

        public string ParentAlias { get; set; }

        #endregion

        [Required]
        public string Name { get; set; }

        // Use link base alias
        public string Alias { get; set; }

        public string Description { get; set; }

        public int Order { get; set; } = 1;

        [Required]
        public string Language { get; set; } = Constants.Languages.Vietnamese;

        public bool Enable { get; set; } = true;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string ModifiedUserId { get; set; }
    }
}

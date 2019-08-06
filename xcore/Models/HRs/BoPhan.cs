using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Dung cho Co cau Nha May
    /// </summary>
    public class BoPhan
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Bo Phan Con thi PhongBanId nullorempty
        public string PhongBanId { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Alias { get; set; }
        
        public string Description { get; set; }

        public IList<Image> Images { get; set; }

        public int Order { get; set; } = 1;

        public string Parent { get; set; }

        public string Language { get; set; } = Constants.Languages.Vietnamese;

        public bool Enable { get; set; } = true;
    }
}

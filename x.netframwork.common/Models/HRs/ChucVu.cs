using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Tuong tu BoPhan
    /// </summary>
    public class ChucVu
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }

        public string CongTyChiNhanhId { get; set; }

        public string KhoiChucNangId { get; set; }

        public string PhongBanId { get; set; }

        public string BoPhanId { get; set; }

        public string BoPhanConId { get; set; }

        public IList<Image> Images { get; set; }

        // Chia theo level
        // High level is lowest number
        public int Level { get; set; } = 1;

        public int Order { get; set; } = 1;

        public string Parent { get; set; }

        // Update current employee in chucvu
        public string Employee { get; set; }

        public string Language { get; set; } = Constants.Languages.Vietnamese;

        public bool Enable { get; set; } = true;
    }
}

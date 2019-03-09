using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Moi Khoi thuoc CongTyChiNhanh
    /// 2 CongTyChiNhanh có 2 khoi ten giong thì tạo 2 khoi. KHONG DUNG CHUNG
    /// </summary>
    public class KhoiChucNang
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string CongTyChiNhanhId { get; set; }

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

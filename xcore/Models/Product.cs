using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Only control product information
    /// Quantity is control in other place. Such as: ...
    /// </summary>
    public class Product: CommonV101
    {
        // Hiện tại: Mỗi kho mỗi mã
        // Tạo sản phẩm từng kho
        public int TypeId { get; set; } = (int)EKho.NguyenVatLieu;

        public string Code { get; set; }

        public string Code1 { get; set; }

        public string Code2 { get; set; }

        public string Name { get; set; }

        public string Alias { get; set; }

        public string UnitId { get; set; }

        public string Note { get; set; }

        public IList<Document> Documents { get; set; }

        public int Status { get; set; } = (int)EProductStatus.New;
    }
}

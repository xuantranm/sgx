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
    public class TrangThai: CommonV101
    {
        // Tạo sản phẩm từng kho
        public int TypeId { get; set; } = (int)ETrangThai.Kho;

        public string Code { get; set; }

        public string Name { get; set; }

        public string Alias { get; set; }

        public string Note { get; set; }
    }
}

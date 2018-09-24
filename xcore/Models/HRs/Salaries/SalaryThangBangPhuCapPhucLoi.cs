using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Current:
    /// Phuc Loi theo chức danh công việc (báo cáo thuế, tham chiếu) dùng: field [MaSo] bên collection [SalaryThangBangLuong]
    /// Future:
    /// Phuc Loi theo từng vị trí: dùng field [ViTri] bên collection [SalaryThangBangLuong] (1)
    /// Phuc Loi theo từng nhân viên (tính lương thực tế) (2)
    /// </summary>
    public class SalaryThangBangPhuCapPhucLoi
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Display(Name = "Mã phụ cấp")]
        public string Code { get; set; }

        [Display(Name = "Tên phụ cấp")]
        public string Name { get; set; }

        // Update In Future
        [Display(Name = "Vị trí")]
        public string ViTri { get; set; }

        [Display(Name = "Mã số")]
        public string MaSo { get; set; }

        [Display(Name = "Số tiền")]
        public decimal Money { get; set; }

        // muc luong theo luat VN, false theo cong ty
        public bool Law { get; set; } = true;

        public bool FlagReal = false;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Start { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}

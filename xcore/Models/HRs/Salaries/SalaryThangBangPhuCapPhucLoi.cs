﻿using MongoDB.Bson;
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

        public int Year { get; set; }

        public int Month { get; set; }

        // Link SalaryPhuCapPhuLoi
        [Display(Name = "Mã phụ cấp")]
        public string Code { get; set; }

        [Display(Name = "Tên phụ cấp")]
        public string Name { get; set; }

        // Ma so không dùng song song với employeeID,
        // bao cao thuế: dựa vào bảng employee => lấy SalaryMaSoChucDanhCongViec => lấy phụ cấp, trợ cấp.
        [Display(Name = "Mã số")]
        public string MaSo { get; set; }

        #region Phuc Loi theo vi tri, nhan vien
        public string EmployeeId { get; set; }

        // Update In Future
        [Display(Name = "Vị trí")]
        public string ViTri { get; set; }
        #endregion

        [Display(Name = "Số tiền")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Money { get; set; }

        public bool Law { get; set; } = false;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}

using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Quan ly tháng tính sales, logistics
    /// Update SalaryEmployeeMonth
    /// If each employee can change itseft
    /// </summary>
    public class SalaryDuration: CommonV101
    {
        public int SalaryYear { get; set; }

        public int SalaryMonth { get; set; }

        public int SaleYear { get; set; }

        public int SaleMonth { get; set; }

        public int LogisticYear { get; set; }

        public int LogisticMonth { get; set; }
    }
}

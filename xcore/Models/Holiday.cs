using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Manager holiday
    // Manger email collection [HolidayExtension]
    public class Holiday : ExtensionNew
    {
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        public string Name { get; set; }

        public string Detail { get; set; }

        #region Automatic
        public int Month
        {
            get
            {
                return Date.Day > 25 ? Date.AddMonths(1).Month : Date.Month;
            }
        }

        public int Year
        {
            get
            {
                return Date.Day > 25 ? Date.AddMonths(1).Year : Date.Year;
            }
        }
        #endregion
    }
}

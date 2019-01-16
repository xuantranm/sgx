using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Manager holiday
    public class Holiday: ExtensionNew
    {
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

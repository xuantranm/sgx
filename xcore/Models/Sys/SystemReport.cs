using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    // Quan ly toan bo he thong bao cao
    // If track Error, view Error for detail
    public class SystemReport
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Loi | email-wrong|....
        public string Type { get; set; }

        public string Detail { get; set; }

        public string CreatedOn { get; set; } = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");

        public string UpdatedOn { get; set; } = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");
    }
}

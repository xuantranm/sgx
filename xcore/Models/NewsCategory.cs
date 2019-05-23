using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class NewsCategory : ExtensionNew
    {
        public int Code { get; set; }

        public int ParentCode { get; set; } = 0; // no parent

        public string Name { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }
    }
}

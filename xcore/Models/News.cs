using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Tin tức Only
    /// ...
    /// each language 1 record. group by code
    /// Create/Edit 1 language 1 time
    /// </summary>
    public class News: ExtensionNew
    {
        public int Code { get; set; }

        public string CategoryId { get; set; }

        public string CategoryCode { get; set; }

        public string CategoryAlias { get; set; }

        public string CategoryName { get; set; }

        public string Name { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }

        public string Content { get; set; }

        public IList<Image> Images { get; set; }

        public IList<Document> Documents { get; set; }

        public string Source { get; set; }

        public string SourceLink { get; set; }

        public bool HomePage { get; set; } = false;
    }
}
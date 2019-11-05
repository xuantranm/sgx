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
    /// [NO DELETE ANY FIELD. IT'S DANGEROUS.]
    /// Control all data.
    /// Category such as: news, products, promotion,...
    /// </summary>
    public class Content : Extension
    {
        #region Category
        public string CategoryId { get; set; }

        public string CategoryName { get; set; }

        public string CategoryAlias { get; set; }
        #endregion

        public string Name { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }

        public int Position { get; set; } = (int)EPosition.Normal; // Get fastest

        public IList<Img> Imgs { get; set; } // Use display, home title ...

        public IList<ContentIn> Contents { get; set; }

        public IList<Setting> Properties { get; set; }
    }
}
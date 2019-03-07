using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class ExProductSale
    {
        public ProductSale Product { get; set; }

        public string CategoryAlias { get; set; }
    }
}

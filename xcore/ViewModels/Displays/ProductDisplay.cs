using Common.Utilities;
using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Models;

namespace ViewModels
{
    public class ProductDisplay
    {
        public Product Product { get; set; }

        public string Unit { get; set; }

        public string Type { get; set; }

        // Continute...
    }
}

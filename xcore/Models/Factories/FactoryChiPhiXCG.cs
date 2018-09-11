using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class FactoryChiPhiXCG : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; } = 0;

        public int Month { get; set; } = 0;

        public int Week { get; set; } = 0;

        public int Day { get; set; } = 0;

        public string ChungLoaiXe { get; set; }

        public string ChungLoaiXeAlias { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiPhiThang { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiPhi1H { get; set; } = 0;
    }
}

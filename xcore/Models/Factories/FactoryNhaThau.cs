using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Công đoạn
    public class FactoryNhaThau : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Code { get; set; }

        // model WorkFactory
        public string Xe { get; set; }

        public string XeAlias { get; set; }

        public string ChungLoaiXe { get; set; }

        public string ChungLoaiXeAlias { get; set; }

        public string NhaThau { get; set; }

        public string NhaThauALias { get; set; }

        public string MangCongViec { get; set; }

        public string MangCongViecAlias { get; set; }
    }
}

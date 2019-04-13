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
    public class EmployeeSelectList
    {
        public string Id { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string ChucVu { get; set; }
    }
}

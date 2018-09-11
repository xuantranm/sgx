using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ViewModels
{
    public class ExtensionViewModel
    {
        [Display(Name = "Từ")]
        public DateTime? From { get; set; }

        [Display(Name = "Đến")]
        public DateTime? To { get; set; }

        [Display(Name = "Trạng thái")]
        public IList<EnableModel> Enable { get; set; } = new List<EnableModel>() {
            new EnableModel()
            {
                Value = true,
                Text = "Active",
            },
            new EnableModel()
            {
                Value = false,
                Text = "Disable"
            }
        };

        // not search, use show hide extend
        public bool Extend { get; set; } = true;
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ViewModels
{
    public class CommonViewModel
    {
        public string LinkCurrent { get; set; }

        public string ThuTu { get; set; }

        public string SapXep { get; set; }

        // Phan trang
        public int Records { get; set; }

        public int PageSize { get; set; }

        public int SoTrang { get; set; } // PageCount

        public int Trang { get; set; } // Page
    }
}

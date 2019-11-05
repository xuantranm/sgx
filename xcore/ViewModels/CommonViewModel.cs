using Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ViewModels
{
    public class CommonViewModel
    {
        // Link
        public IList<Link> Links { get; set; }

        public string LinkCurrent { get; set; } // use fb share, export

        public string ThuTu { get; set; }

        public string SapXep { get; set; }

        // Phan trang
        public int Records { get; set; }

        public int PageSize { get; set; }

        public int PageTotal { get; set; }

        public int PageCurrent { get; set; }

        public int Trang { get; set; } // Temp

        public int SoTrang { get; set; } // Temp

        public IList<Breadcrumb> Breadcrumbs { get; set; }

        public Seo Seo { get; set; }

        public IList<Domain> Domains { get; set; }
    }
}

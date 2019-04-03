using Models;
using System;
using System.Collections.Generic;

namespace ViewModels
{
    public class KhoViewModel: CommonViewModel
    {
        public string Name { get; set;}

        public DateTime Tu { get; set; }

        public DateTime Den { get; set; }

        public string Hang { get; set; }

        public int? SoPhieu { get; set; }

        public string TrangThai { get; set; }

        public IList<TrangThai> TrangThais { get; set; }

        public IList<Product> Products { get; set; }

        public IList<KhoNguyenLieu> KhoNguyenLieus { get; set; }

        public IList<KhoThanhPham> KhoThanhPhams { get; set; }

        public IList<KhoXuLy> KhoXuLys { get; set; }

        public IList<KhoBun> KhoBuns { get; set; }

        public IList<TiepNhanXuLy> TiepNhanXuLys { get; set; }
    }
}

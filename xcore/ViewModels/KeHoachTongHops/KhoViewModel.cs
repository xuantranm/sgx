using Models;
using System;
using System.Collections.Generic;

namespace ViewModels
{
    public class KhoViewModel: CommonViewModel
    {
        public string Name { get; set;}

        public string LOT { get; set; }

        public int? Nam { get; set; }

        public int? Thang { get; set; }

        public int? Tuan { get; set; }

        public DateTime Tu { get; set; }

        public DateTime Den { get; set; }

        public string Hang { get; set; }

        public int? SoPhieu { get; set; }

        public string ChuDauTu { get; set; }

        public string ThiCong { get; set; }

        public string GiamSat { get; set; }

        public string BienSo { get; set; }

        public string TrangThai { get; set; }

        public IList<TrangThai> TrangThais { get; set; }

        public IList<int> Nams { get; set; }

        public IList<int> Thangs { get; set; }

        public IList<int> Tuans { get; set; }

        public IList<Product> Products { get; set; }

        public IList<KhoNguyenVatLieu> KhoNguyenVatLieus { get; set; }

        public IList<KhoThanhPham> KhoThanhPhams { get; set; }

        public IList<KhoHangTraVe> KhoHangTraVes { get; set; }

        public IList<KhoBun> KhoBuns { get; set; }

        public IList<TramCan> TramCans { get; set; }

        //public IList<DuAnCong> DuAnCongs { get; set; }

        public IList<Customer> ThiCongs { get; set; }

        public IList<Customer> GiamSats { get; set; }

        public IList<Customer> ChuDauTus { get; set; }
    }
}

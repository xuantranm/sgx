using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class ContentViewModel: PagingExtension
    {
        #region Menu
        public MenuViewModel Menu { get; set; }
        #endregion

        public IList<Breadcrumb> Breadcrumbs { get; set; }

        public Content Entity { get; set; }
        public IList<Link> Links { get; set; }

        public string linkGioiThieu { get; set; }
        public string textGioiThieu { get; set; }
        public string linkDoiTac { get; set; }
        public string textDoiTac { get; set; }
        public string linkCoCauToChuc { get; set; }
        public string textCoCauToChuc { get; set; }
        public string linkTamNhinSuMang { get; set; }
        public string textTamNhinSuMang { get; set; }
        public string linkVanBanThamKhao { get; set; }
        public string textVanBanThamKhao { get; set; }
        public string linkCoSoPhapLy { get; set; }
        public string textCoSoPhapLy { get; set; }
        public string linkLichSuHinhThanh { get; set; }
        public string textLichSuHinhThanh { get; set; }
        public string linkCongNgheXuLy { get; set; }
        public string textCongNgheXuLy { get; set; }
        public string linkCongSuatXuLy { get; set; }
        public string textCongSuatXuLy { get; set; }

        public string linkKhachHangViTriDiaLy { get; set; }
        public string textKhachHangViTriDiaLy { get; set; }
        public string linkKhachHangXuLyChatThai { get; set; }
        public string textKhachHangXuLyChatThai { get; set; }

    }
}

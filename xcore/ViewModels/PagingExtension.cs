using Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ViewModels
{
    public class PagingExtension
    {
        [Display(Name = "Trang")]
        public int PageIndex { get; set; } = 1;

        [Display(Name = "Số dòng")]
        public int PageSize { get; set; } = 50;

        [Display(Name = "Tổng số dòng")]
        public int TotalRecords { get; set; } = 0;

        [Display(Name = "Tổng số trang")]
        public int TotalPages { get; set; } = 1;

        public bool HasPreviousPage
        {
            get
            {
                return (PageIndex > 1);
            }
        }

        public bool HasNextPage
        {
            get
            {
                return (PageIndex < TotalPages);
            }
        }

        [Display(Name = "Sắp xếp")]
        public string Sort { get; set; } = "asc";

        [Display(Name = "Trường")]
        public string SortBy { get; set; }
    }
}

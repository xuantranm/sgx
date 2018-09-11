using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class ErpViewModel
    {
        public OwnerViewModel OwnerInformation { get; set; }
        public Employee UserInformation { get; set; }
        public IList<TrackingUser> TrackingUser { get; set; }
        public IList<Menu> Menus { get; set; }
    }

    public class OwnerViewModel
    {
        public Employee Main { get; set; }
        // relation
        public int NotificationCount { get; set; } = 0;
    }
}

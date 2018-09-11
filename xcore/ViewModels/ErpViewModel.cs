using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class ErpViewModel
    {
        public Employee OwnerInformation { get; set; }
        public int NotificationCount { get; set; } = 0;
        public Employee UserInformation { get; set; }
        public IList<TrackingUser> TrackingUser { get; set; }
        public IList<Menu> Menus { get; set; }
    }
}

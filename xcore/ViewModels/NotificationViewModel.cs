using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class NotificationViewModel : CommonViewModel
    {
        public IList<Notification> Notifications { get; set; }

        public IList<NotificationAction> NotificationActions { get; set; }

        public Notification Notification { get; set; }
    }
}

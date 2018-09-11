using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class HomeErpViewModel
    {
        // Notifcation from HR
        public IList<Employee> Birthdays { get; set; }

        public IList<Employee> ExpiresContract { get; set; }

        public IList<Notification> NotificationSystems { get; set; }

        public IList<Notification> NotificationCompanies { get; set; }

        public IList<Notification> NotificationHRs { get; set; }

        public IList<Notification> NotificationExpires { get; set; }

        public IList<Notification> NotificationTaskBhxhs { get; set; }

        public IList<NotificationAction> NotificationActions { get; set; }

        public Employee UserInformation { get; set; }

        public IList<TrackingUser> Trackings { get; set; }

        // Load activities user
        public IList<TrackingUser> TrackingsOther { get; set; }

        // Load news...
        public IList<News> News { get; set; }
    }
}

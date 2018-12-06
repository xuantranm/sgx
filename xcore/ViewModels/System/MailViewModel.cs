using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class MailViewModel
    {
        public IList<ScheduleEmail> ScheduleEmails { get; set; }

        public ScheduleEmail ScheduleEmail { get; set; }

        public int? status { get; set; }

        public string id { get; set; }

        public string toemail { get; set; }

        public int page { get; set; }

        public int size { get; set; }

        public int Records { get; set; }

        public int Pages { get; set; }

        // For resend
        public string fromEmail { get; set; }

        public string toEmail { get; set; }

        public string ccEmail { get; set; }
    }
}

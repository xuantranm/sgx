﻿using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class MailViewModel
    {
        // Search email by phongban
        public IList<PhongBan> PhongBans { get; set; }

        public IList<Employee> Employees { get; set; }

        public IList<EmailAddress> TOs { get; set; }
        public IList<EmailAddress> CCs { get; set; }

        public IList<ScheduleEmail> ScheduleEmails { get; set; }

        public ScheduleEmail ScheduleEmail { get; set; }

        public string Status { get; set; }

        public string Id { get; set; }

        public string PhongBan { get; set; }

        public string MaNv { get; set; }

        public string ToEmail { get; set; }

        public int Page { get; set; }

        public int Size { get; set; }

        public int Records { get; set; }

        public int Pages { get; set; }

        // For resend
        public string FromEmail { get; set; }

        public string CcEmail { get; set; }
    }
}

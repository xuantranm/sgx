using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class EmailMessage
    {
        public EmailMessage()
        {
            ToAddresses = new List<EmailAddress>();
            FromAddresses = new List<EmailAddress>();
            CCAddresses = new List<EmailAddress>();
            BCCAddresses = new List<EmailAddress>();
        }

        public List<EmailAddress> ToAddresses { get; set; }
        public List<EmailAddress> FromAddresses { get; set; }
        public List<EmailAddress> CCAddresses { get; set; }
        public List<EmailAddress> BCCAddresses { get; set; }
        public string Subject { get; set; }
        public string BodyContent { get; set; }
        public string Type { get; set; }
        public string EmployeeId { get; set; }

        public List<string> Attachments { get; set; }
    }
}

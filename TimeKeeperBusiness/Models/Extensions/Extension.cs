using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class Extension
    {
        #region SEO
        public string SeoTitle { get; set; }
        public string KeyWords { get; set; }
        public string MetaOwner { get; set; }
        public string Canonical { get; set; }
        public string OgUrl { get; set; }
        public string OgTitle { get; set; }
        public string OgDescription { get; set; }
        public string SeoFooter { get; set; }
        public string Robots { get; set; }

        public string RelationshipCategory { get; set; }

        public string RelationshipItem { get; set; }
        #endregion
        
        //...
    }
}

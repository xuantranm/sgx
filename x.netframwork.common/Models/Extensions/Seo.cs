using Common.Utilities;
using System;
using System.Collections.Generic;

namespace Models
{
    public class Seo
    {
        public string Title { get; set; }

        public string KeyWords { get; set; } // divide by ","

        public string Description { get; set; }

        public string Robots { get; set; } = Constants.Seo.indexFollow;

        public string Author { get; set; } // if null get domain

        public string Type { get; set; } = Constants.GoogleSearchType.WebSite;

        public string Url { get; set; }

        public string Canonical { get; set; } // Use Url

        public DateTime? DatePublished { get; set; }

        public DateTime? DateModified { get; set; }

        public string Footer { get; set; }

        // Fb vs Google use w-h same same 720*480
        public string Image { get; set; }

        public string ImageW { get; set; } 

        public string ImageH { get; set; } 

        #region Google Search Meta ApplicationLdJson
        public string NameApplicationLdJsonGoogleMeta { get; set; } // if null get domain

        public string TypeGGS { get; set; } = Constants.GoogleSearchType.WebSite;

        public string ImageGG { get; set; }

        public string ImageGGW { get; set; } 

        public string ImageGGH { get; set; } 

        #endregion

        #region Facebook
        // Example: <meta content="VnExpress" property="og:site_name"/>
        // og:site_name
        // og:url
        public string TypeFB { get; set; } // og:type
        // og:title
        // og:image
        // og:description
        public string TagsFB { get; set; } // only fb

        public string ImageFB { get; set; }

        public string ImageFBW { get; set; } 

        public string ImageFBH { get; set; } 

        #endregion

        #region Twitter Card: Implement later
        // Examle: <meta name="twitter:card" value="summary">
        public string TwitterCard { get; set; } = Constants.TwitterCard.Summary; // twitter:card
        // twitter:url
        // twitter:title
        // twitter:description
        // twitter:image
        public string TwitterSite { get; set; }  // twitter:site
        public string TwitterCreator { get; set; } // twitter:creator
        #endregion
    }
}

using MongoDB.Driver;
using System.Linq;
using System.Threading;
using Common.Utilities;
using Data;

namespace Helpers
{
    public static class TextHelper
    {
        private static MongoDBContext dbContext = new MongoDBContext();

        //private static readonly IHttpContextAccessor _httpContextAccessor;
        //private static ISession _session => _httpContextAccessor.HttpContext.Session;

        static TextHelper()
        {
        }

        
        public static string GetText(int code, string language, bool plainText = false, bool noGodMode = false)
        {
            // Some time cookie not found language.

            if (string.IsNullOrEmpty(language))
            {
                language = Thread.CurrentThread.CurrentUICulture.Name;
            }

            // Implete cache later...
            var text = dbContext.Texts.Find(m => m.Code.Equals(code) && m.Language.Equals(language)).FirstOrDefault();
            //var cacheTexts = JsonConvert.DeserializeObject<IEnumerable<Text>>(cacheManager.GetString(Constants.Collection.Texts));
            // End cache area 

            if (text == null)
            {
                //var aa = dbContext.Texts.Find(m => m.Code.Equals(1) && m.Language.Equals(language)).First();
                return "Missing code";
            }
            text.ToolTip = Utility.HtmlToPlainText(text.ToolTip);

            if (noGodMode)
            {
                return text.Content;
            }

            var html = plainText
                           ? text.ContentPlainText
                           : string.IsNullOrEmpty(text.ToolTip)
                                 ? text.Content
                                 : string.Format("<span title='{0}'>{1}</span>",
                                                 text.ToolTip,
                                                 text.Content);

            // use session

            return html;
        }
    }
}

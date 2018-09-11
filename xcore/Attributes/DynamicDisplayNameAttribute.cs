using System.ComponentModel;

namespace Attributes
{
    public class DynamicDisplayNameAttribute : DisplayNameAttribute
    {
        private readonly int _code;
        public DynamicDisplayNameAttribute(int code)
        {
            _code = code;
        }

        public override string DisplayName
        {
            get
            {
                // Get localized text
                return string.Empty;
                //return TextHelper.GetText(_code, null);
            }
        }
    }
}
using System;
using KeePassLib.Security;

namespace CefBrowserControl.Resources
{
    [Serializable]
    public class Text : Resource
    {
        public ProtectedString Value;

        public Text() : base()
        {

        }

        public Text(ProtectedString value)
        {
            Value = value;
        }
    }
}

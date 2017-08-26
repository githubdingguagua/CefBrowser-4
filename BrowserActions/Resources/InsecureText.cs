using System;

namespace CefBrowserControl.Resources
{
    [Serializable]
    public class InsecureText : Resource
    {
        public string Value = "";

        public InsecureText() : base()
        {

        }

        public InsecureText(string value)
        {
            Value = value;
        }
    }
}

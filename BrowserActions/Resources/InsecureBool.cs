using System;
using System.Collections.Generic;
using System.Text;

namespace CefBrowserControl.Resources
{
    [Serializable]
    public class InsecureBool : Resource
    {
        public bool Value = false;

        public InsecureBool() : base()
        {

        }

        public InsecureBool(bool value)
        {
            Value = value;
        }
    }
}

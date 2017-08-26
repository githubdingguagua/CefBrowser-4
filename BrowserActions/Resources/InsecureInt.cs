using System;
using System.Collections.Generic;
using System.Text;

namespace CefBrowserControl.Resources
{
    [Serializable]
    public class InsecureInt : Resource
    {
        public int? Value = 0;

        public InsecureInt() : base()
        {

        }

        public InsecureInt(int? value)
        {
            Value = value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using CefBrowserControl.BrowserActions.Elements;

namespace CefBrowserControl.Resources
{

    [Serializable]
    public class InsecureHttpAuthSchemaType : Resource
    {
        public GetHttpAuth.SchemaTypes Value = GetHttpAuth.SchemaTypes.Nope;

        public InsecureHttpAuthSchemaType() : base()
        {

        }

        public InsecureHttpAuthSchemaType(GetHttpAuth.SchemaTypes value)
        {
            Value = value;
        }
    }
}

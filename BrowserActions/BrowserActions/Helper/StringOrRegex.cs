using System;
using System.Collections.Generic;
using System.Text;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Helper
{
    public class StringOrRegex
    {
        public InsecureText Value { get; set; } = new InsecureText();

        public InsecureBool IsRegex { get; set; } = new InsecureBool();
    }
}

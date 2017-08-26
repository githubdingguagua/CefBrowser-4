using System;
using System.Collections.Generic;
using System.Text;
using CefBrowserControl.BrowserActions.Elements;

namespace CefBrowserControl.Resources
{
    [Serializable]
    public class InsecureDialogType : Resource
    {
        public GetJsPrompt.DialogTypes Value = GetJsPrompt.DialogTypes.Nope;

        public InsecureDialogType() : base()
        {

        }

        public InsecureDialogType(GetJsPrompt.DialogTypes value)
        {
            Value = value;
        }
    }
}

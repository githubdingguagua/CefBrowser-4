using System;
using CefSharp;

namespace CefBrowser.Handler
{
    [Serializable]
    public class JsDialog
    {
        public string OriginUrl;
        public CefJsDialogType DialogType;
        public string MessageText;
        public string DefaultPromptText;
        public IJsDialogCallback Callback;
        public string UID;
        public bool SucessfullyHandled;

        public JsDialog()
        {
            UID = HashingEx.Hashing.GetSha512Hash(DateTime.Now.ToString() + Guid.NewGuid());
        }
    }
}

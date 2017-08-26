using System;

namespace CefBrowserControl.BrowserCommands
{
    [Serializable]
    public class Open : BrowserCommand
    {
        public string CachePath;

        public Open() : base()
        {
            
        }

        public Open(string uid, string cachePath = "") : this()
        {
            UID = uid;
            CachePath = cachePath;
        }
    }
}

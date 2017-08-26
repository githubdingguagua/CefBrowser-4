using System;

namespace CefBrowserControl
{
    public class Options
    {
        public const int LockTimeOut = 100;
        public const bool IsDebug = false;
        public const int MaxBrowserInstances = 3;
        public const bool WindowsNormallyVisible = true;
        public static TimeSpan DefaultCefBrowserActionOrCommandTimeoutMsec = new TimeSpan(0, 0, 0,30);
        public const string DefaultUrl = "https://duckduckgo.com";
        //Keepass DB X Password Changer Exported Template
        public const string PlaceholderPre = "@@{{", PlaceholderPost = "}}@@";
    }
}

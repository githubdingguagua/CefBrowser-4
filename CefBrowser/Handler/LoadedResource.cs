using System;

namespace CefBrowser.Handler
{
    public class LoadedResource
    {
        public string Url { get; set; }

        public DateTime DateTime { get; set; }

        public string FrameName { get; set; }

        public bool IsMainFrame { get; set; }
    }
}

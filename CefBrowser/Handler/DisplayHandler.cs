using System.Collections.Generic;
using CefSharp;

namespace CefBrowser.Handler
{
    public class DisplayHandler: IDisplayHandler
    {
        private readonly WebBrowserEx _webBrowser;

        public DisplayHandler(WebBrowserEx webBrowser)
        {
            _webBrowser = webBrowser;
        }

        public void OnAddressChanged(IWebBrowser browserControl, AddressChangedEventArgs addressChangedArgs)
        {
            _webBrowser.Dispatcher.Invoke(() =>
            {
                _webBrowser.Title = addressChangedArgs.Address.ToString();
            });
            _webBrowser.RequestHandler.LoadedResources.Clear();
        }

        public void OnTitleChanged(IWebBrowser browserControl, TitleChangedEventArgs titleChangedArgs)
        {
        }

        public void OnFaviconUrlChange(IWebBrowser browserControl, IBrowser browser, IList<string> urls)
        {
        }

        public void OnFullscreenModeChange(IWebBrowser browserControl, IBrowser browser, bool fullscreen)
        {
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IDisplayHandler_OnTooltipChanged.htm
        //To handle the display of the tooltip yourself return true otherwise return false to allow the browser to display the tooltip.
        public bool OnTooltipChanged(IWebBrowser browserControl, string text)
        {
            return false;
        }

        public void OnStatusMessage(IWebBrowser browserControl, StatusMessageEventArgs statusMessageArgs)
        {
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IDisplayHandler_OnConsoleMessage.htm
        //Return true to stop the message from being output to the console.
        public bool OnConsoleMessage(IWebBrowser browserControl, ConsoleMessageEventArgs consoleMessageArgs)
        {
            return true;
        }
    }
}

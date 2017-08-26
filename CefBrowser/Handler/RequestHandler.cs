using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using CefBrowser.Gateway;
using CefBrowserControl.BrowserActions.Elements;
using CefSharp;

namespace CefBrowser.Handler
{
    public class RequestHandler : IRequestHandler
    {
        readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        private readonly List<LoadedResource> _loadedResources = new List<LoadedResource>();

        public List<HttpAuth> HandledHttpAuths = new List<HttpAuth>();
        public List<GatewayAction.SetHttpAuth> PreparedHttpAuths = new List<GatewayAction.SetHttpAuth>();

        public List<LoadedResource> LoadedResources
        {
            get
            {
                _lockSlim.EnterReadLock();
                try
                {
                    return _loadedResources;
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }
            }
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnBeforeBrowse.htm
        //Return true to cancel the navigation or false to allow the navigation to proceed.
        public bool OnBeforeBrowse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool isRedirect)
        {
            return false;
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnOpenUrlFromTab.htm
        //Return true to cancel the navigation or false to allow the navigation to proceed in the source browser's top-level frame.
        public bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl,
            WindowOpenDisposition targetDisposition, bool userGesture)
        {
            return false;
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnCertificateError.htm
        //Return false to cancel the request immediately. Return true and use IRequestCallback to execute in an async fashion.
        public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl,
            ISslInfo sslInfo, IRequestCallback callback)
        {
            return false;
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnPluginCrashed.htm
        public void OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath)
        {

        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnBeforeResourceLoad.htm
        //To cancel loading of the resource return Cancel or Continue to allow the resource to load normally. For async return ContinueAsync
        public CefReturnValue OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request,
            IRequestCallback callback)
        {
            return CefReturnValue.Continue;
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_GetAuthCredentials.htm
        //Return true to continue the request and call CefAuthCallback::Continue() when the authentication information is available. Return false to cancel the request.
        public bool GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port,
            string realm, string scheme, IAuthCallback callback)
        {
            bool success = false;
            foreach (var preparedHttpAuth in PreparedHttpAuths)
            {
                if (preparedHttpAuth.ExpectedSchemaType == GetHttpAuth.SchemaTypes.Nope ||
                    (preparedHttpAuth.ExpectedSchemaType.ToString() == scheme))
                {
                    if (preparedHttpAuth.ExpectedHost.Value.Value == "" ||
                        (preparedHttpAuth.ExpectedHost.IsRegex.Value &&
                         Regex.IsMatch(host, preparedHttpAuth.ExpectedHost.Value.Value) ||
                         !preparedHttpAuth.ExpectedHost.IsRegex.Value &&
                         host == preparedHttpAuth.ExpectedHost.Value.Value))
                    {
                        if (preparedHttpAuth.ExpectedRealm.Value.Value == "" ||
                        (preparedHttpAuth.ExpectedRealm.IsRegex.Value &&
                         Regex.IsMatch(realm, preparedHttpAuth.ExpectedRealm.Value.Value) ||
                         !preparedHttpAuth.ExpectedRealm.IsRegex.Value &&
                         realm == preparedHttpAuth.ExpectedRealm.Value.Value))
                        {
                            if (preparedHttpAuth.ExpectedPort == null ||
                                (preparedHttpAuth.ExpectedPort == port))
                            {
                                if (callback.IsDisposed)
                                    break;
                                if(preparedHttpAuth.Cancel)
                                    callback.Cancel();
                                else
                                    callback.Continue(preparedHttpAuth.Username/*.ReadString()*/, preparedHttpAuth.Password/*.ReadString()*/);

                                HandledHttpAuths.Add(new HttpAuth()
                                {
                                    Host = host,
                                    Port =  port,
                                    Realm = realm,
                                    SuccessfullyHandled = true,
                                    Scheme = scheme,
                                });
                                return true;
                            }
                        }
                    }
                }
            }
            HandledHttpAuths.Add(new HttpAuth()
            {
                Host = host,
                Port = port,
                Realm = realm,
                Scheme = scheme,
                SuccessfullyHandled = false,
            });
            return false;
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnRenderProcessTerminated.htm
        public void OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status)
        {

        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnQuotaRequest.htm
        //Return false to cancel the request immediately. Return true to continue the request and call Continue(Boolean) either in this method or at a later time to grant or deny the request.
        public bool OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize,
            IRequestCallback callback)
        {
            return false;
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnResourceRedirect.htm
        public void OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, ref string newUrl)
        {

        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnProtocolExecution.htm
        //return to true to attempt execution via the registered OS protocol handler, if any. Otherwise return false.
        public bool OnProtocolExecution(IWebBrowser browserControl, IBrowser browser, string url)
        {
            return false;
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnRenderViewReady.htm
        public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
        {

        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnResourceResponse.htm
        //To allow the resource to load normally return false. To redirect or retry the resource modify request (url, headers or post body) and return true.
        public bool OnResourceResponse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            return false;
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_GetResourceResponseFilter.htm
        //Return an IResponseFilter to intercept this response, otherwise return null
        public IResponseFilter GetResourceResponseFilter(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request,
            IResponse response)
        {
            return null;
        }

        //http://cefsharp.github.io/api/51.0.0/html/M_CefSharp_IRequestHandler_OnResourceLoadComplete.htm
        public void OnResourceLoadComplete(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request,
            IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _loadedResources.Add(new LoadedResource()
                {
                    DateTime = DateTime.Now,
                    Url = request.Url,
                    FrameName = frame.Name,
                    IsMainFrame = frame.IsMain,
                });
                //if (request.ResourceType == ResourceType.Image)
                //{
                //    MemoryStreamResponseFilter filter;
                //    if (responseDictionary.TryGetValue(request.Identifier, out filter))
                //    {
                //        //TODO: Do something with the data here
                //        var data = filter.Data;
                //        var dataLength = filter.Data.Length;
                //        //NOTE: You may need to use a different encoding depending on the request
                //        var dataAsUtf8String = Encoding.UTF8.GetString(data);
                //    }
                //}
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        //https://github.com/cefsharp/CefSharp/blob/master/CefSharp.Example/RequestHandler.cs
        bool IRequestHandler.OnSelectClientCertificate(IWebBrowser browserControl, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
        {
            //NOTE: If you do not wish to implement this method returning false is the default behaviour
            // We also suggest you explicitly Dispose of the callback as it wraps an unmanaged resource.

            return OnSelectClientCertificate(browserControl, browser, isProxy, host, port, certificates, callback);
        }

        //https://github.com/cefsharp/CefSharp/blob/master/CefSharp.Example/RequestHandler.cs
        protected virtual bool OnSelectClientCertificate(IWebBrowser browserControl, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
        {
            callback.Dispose();
            return false;
        }

        //https://github.com/cefsharp/CefSharp/blob/master/CefSharp.Example/RequestHandler.cs
        void IRequestHandler.OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
        {
            //Example of how to redirect - need to check `newUrl` in the second pass
            //if (request.Url.StartsWith("https://www.google.com", StringComparison.OrdinalIgnoreCase) && !newUrl.Contains("github"))
            //{
            //    newUrl = "https://github.com";
            //}
            //newUrl = request.Url;
        }
    }
}
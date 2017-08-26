using System;
using CefSharp;

namespace CefBrowser.Handler
{
    [Serializable]
    public class HttpAuth
    {
        public string Host;
        public int Port;
        public string Realm;
        public string Scheme;
        public string UID;
        public bool SuccessfullyHandled;

        public HttpAuth()
        {
            UID = HashingEx.Hashing.GetSha512Hash(DateTime.Now.ToString() + Guid.NewGuid());
        }
    }
}

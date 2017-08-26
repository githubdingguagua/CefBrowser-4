using System;

namespace CefBrowserControl.Resources
{
    public class Resource
    {
        //Uniuue Resource ID
        public string URID;

        public Resource()
        {
            URID = HashingEx.Hashing.GetSha512Hash(DateTime.Now.ToString());
        }
    }
}

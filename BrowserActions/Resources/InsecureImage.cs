using System;
using System.Drawing;

namespace CefBrowserControl.Resources
{
    [Serializable]
    public class InsecureImage : Resource
    {
        public string Base64EncodedImage = "";

        public InsecureImage() : base()
        {
            
        }

        public InsecureImage(string base64String)
        {
            Base64EncodedImage = base64String;
        }
    }
}

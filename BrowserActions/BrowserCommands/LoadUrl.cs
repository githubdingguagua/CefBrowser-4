using System;
using System.Collections.Generic;
using System.Text;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserCommands
{
    [Serializable]
    public class LoadUrl : BrowserCommand, IInstanciateInputParameters
    {
        public InsecureText Url = new InsecureText(Options.DefaultUrl);

        public LoadUrl() : base()
        {
            
        }

        public LoadUrl(string uid, string url) : this()
        {
            UID = uid;
            Url.Value = url;
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Url", Url),
            };
            InputParameterRequired = new List<string>()
            {
                "Url"
            };
            Description =
                "Instructs the browser to load the specified url";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Url")
                    Url = (InsecureText)inputParameter.Value;
            }
        }
    }
}

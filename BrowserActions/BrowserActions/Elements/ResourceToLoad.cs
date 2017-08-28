using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.BrowserActions.Helper;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(StringOrRegex))]
    [Serializable]
    public class ResourceToLoad : BaseObject, IInstanciateInputParameters
    {
        public StringOrRegex ExpectedResourceUrl = new StringOrRegex();

        public StringOrRegex ExpectedFrameName = new StringOrRegex();

        public string ResourceUrl;
        public DateTime LoadedAt;

        public enum KeyList
        {
            ResourceUrl,
            LoadedAt
        }

        public ResourceToLoad()
        {
            
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.ResourceUrl.ToString());
            ReturnedOutputKeysList.Add(KeyList.LoadedAt.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("ExpectedResourceUrl", ExpectedResourceUrl),
                new KeyValuePairEx<string, object>("ExpectedFrameName", ExpectedFrameName),
            };
            InputParameterRequired = new List<string>()
            {
                "ExpectedResourceUrl",
            };
            Description =
                "Instructs the browser to check if a resource has been loaded and wait for it. POSSIBLE DEADLOCK AHEAD!";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "ExpectedResourceUrl")
                    ExpectedResourceUrl = (StringOrRegex)inputParameter.Value;
                else if (inputParameter.Key == "ExpectedFrameName")
                    ExpectedFrameName = (StringOrRegex)inputParameter.Value;
            }
        }
    }
}
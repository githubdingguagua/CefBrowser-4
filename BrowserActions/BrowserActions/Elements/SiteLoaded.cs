using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.BrowserActions.Helper;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(StringOrRegex))]
    [Serializable]
    public class SiteLoaded : BaseObject, IInstanciateInputParameters
    {
        public StringOrRegex ExpectedSiteToLoad = new StringOrRegex();

        public string SiteLoadedUrl;

        public enum KeyList
        {
            SiteLoadedUrl
        }

        public SiteLoaded()
        {
           
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.SiteLoadedUrl.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("ExpectedSiteToLoad", ExpectedSiteToLoad),
            };
            InputParameterSet = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("ExpectedSiteToLoad", ExpectedSiteToLoad),
            };
            InputParameterRequired = new List<string>()
            {
                "ExpectedSiteToLoad"
            };
            Description =
                "Lets the browser wait until the specified url has loaded. POSSIBLE DEADLOCK AHEAD!";
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "ExpectedSiteToLoad")
                    ExpectedSiteToLoad = (StringOrRegex)inputParameter.Value;
            }
        }
    }
}
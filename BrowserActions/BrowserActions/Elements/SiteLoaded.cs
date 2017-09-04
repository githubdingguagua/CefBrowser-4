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

           SetAvailableInputParameters();
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
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "ExpectedSiteToLoad")
                    ExpectedSiteToLoad = (StringOrRegex)inputParameter.Value;
            }
            if (InputParameterAvailable.Count != 1)
                NewInstance();
        }

        public new void SetAvailableInputParameters()
        {
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("ExpectedSiteToLoad", ExpectedSiteToLoad),
            };
        }
    }
}
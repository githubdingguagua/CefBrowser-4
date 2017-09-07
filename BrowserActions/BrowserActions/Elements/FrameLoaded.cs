using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.BrowserActions.Helper;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(StringOrRegex))]
    [Serializable]
    public class FrameLoaded : BaseObject, IInstanciateInputParameters
    {
        public StringOrRegex ExpectedFrameName = new StringOrRegex();

        public FrameLoaded() : base()
        {
            
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            SetAvailableInputParameters();
            InputParameterRequired = new List<string>()
            {
                "ExpectedFrameName"
            };
            Description =
                "Instructs the browser to wait until one or more frames are loaded completely. POSSIBLE DEADLOCK AHED!";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "ExpectedFrameName")
                    ExpectedFrameName = (StringOrRegex)inputParameter.Value;
            }
            if (InputParameterAvailable.Count != 1)
                NewInstance();
        }

        public new void SetAvailableInputParameters()
        {
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("ExpectedFrameName", ExpectedFrameName),
            };
        }
    }
}
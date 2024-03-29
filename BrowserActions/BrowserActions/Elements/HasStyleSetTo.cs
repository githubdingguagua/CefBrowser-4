using System;
using System.Collections.Generic;
using CefBrowserControl.BrowserActions.Helper;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [Serializable]
    public class HasStyleSetTo : HasAttributeSetTo
    {
        public HasStyleSetTo()
        {
          
        }

        public new void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
                new KeyValuePairEx<string, object>("AttributeName", AttributeName),
                new KeyValuePairEx<string, object>("ExpectedValue", ExpectedValue),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
                "AttributeName",
                "ExpectedValue",
            };
            Description =
                 "Checks if one or more elements have set the css style to a specified value. Action = true if case becomes true. attributename is CSS Style AttributeName!";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Selector")
                    Selector = (Selector)inputParameter.Value;
                else if (inputParameter.Key == "AttributeName")
                    AttributeName = (InsecureText)inputParameter.Value;
                else if (inputParameter.Key == "ExpectedValue")
                    ExpectedValue = (StringOrRegex)inputParameter.Value;
            }
            if (InputParameterAvailable.Count != 3)
                NewInstance();
        }
    }
}
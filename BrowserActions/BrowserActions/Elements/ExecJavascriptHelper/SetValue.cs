using System;
using System.Collections.Generic;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements.ExecJavascriptHelper
{
    [Serializable]
    public class SetValue : SetAttribute
    {
        public SetValue()
        {

        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            AttributeName.Value = "Value";
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
                new KeyValuePairEx<string, object>("ValueToSet", ValueToSet),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
                "AttributeName",
                "ValueToSet",
            };
            Description =
                "Instructs the browser set the attribute value of the one or more elements.";
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
                else if (inputParameter.Key == "ValueToSet")
                    ValueToSet = (InsecureText)inputParameter.Value;
            }
        }
    }
}

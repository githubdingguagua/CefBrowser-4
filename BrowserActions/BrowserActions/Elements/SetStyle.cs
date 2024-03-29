using System;
using System.Collections.Generic;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [Serializable]
    public class SetStyle : SetAttribute
    {
        public SetStyle() : base()
        {
          
        }


        public new void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
           SetAvailableInputParameters();
            InputParameterRequired = new List<string>()
            {
                "Selector",
                "AttributeName",
                "ValueToSet",
            };
            Description = "Sets an css attribute value of one or more elements. AttributeName = Css Style Name!";
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
            if (InputParameterAvailable.Count != 3)
                NewInstance();
        }
        public new void SetAvailableInputParameters()
        {
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
                new KeyValuePairEx<string, object>("AttributeName", AttributeName),
                new KeyValuePairEx<string, object>("ValueToSet", ValueToSet),
            };
        }
    }
}
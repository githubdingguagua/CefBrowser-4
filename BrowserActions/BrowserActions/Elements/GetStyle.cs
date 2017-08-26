using System;
using System.Collections.Generic;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [Serializable]
    public class GetStyle : GetAttribute
    {
        public GetStyle()
        {

        }

        public new void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.Value.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
                new KeyValuePairEx<string, object>("AttributeName", AttributeName),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
                "AttributeName",
            };
            Description =
                "Gets the style of an element. The AttributeName will be the css property!";
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Selector")
                    Selector = (Selector)inputParameter.Value;
                else if (inputParameter.Key == "AttributeName")
                    AttributeName = (InsecureText)inputParameter.Value;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace CefBrowserControl.BrowserActions.Elements.ExecJavascriptHelper
{
    [Serializable]
    public class GetInnerHtml : JavascriptToExecute
    {
        public GetInnerHtml()
        {

        }

        public new void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.ExecutionResult.ToString());
            Javascript.Value = ".innerHTML";
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
            };
            Description =
               "gets the inner HTML of an element";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Selector")
                    Selector = (Selector)inputParameter.Value;
            }
        }
    }
}

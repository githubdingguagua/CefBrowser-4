using System;
using System.Collections.Generic;
using System.Text;

namespace CefBrowserControl.BrowserActions.Elements
{
    [Serializable]
    public class InvokeSubmit : JavascriptToExecute
    {
        public InvokeSubmit()
        {

        }

        public new void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.ExecutionResult.ToString());
            Javascript.Value = ".submit()";
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
            };
            Description =
               "Invokes submit event on the selector";
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

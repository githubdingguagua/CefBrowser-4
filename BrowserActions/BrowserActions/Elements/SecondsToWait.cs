using System;
using System.Collections.Generic;

namespace CefBrowserControl.BrowserActions.Elements
{
    [Serializable]
    public class SecondsToWait :BaseObject, IInstanciateInputParameters //Can be used for await and action
    {
        public TimeSpan SecondsToWaitInt = TimeSpan.Zero;

        public SecondsToWait()
        {
            
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("SecondsToWaitInt", SecondsToWaitInt),
            };
            InputParameterRequired = new List<string>()
            {
                "SecondsToWaitInt"
            };
            Description =
                "Instructs the browser to wait for a number of seconds";
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "SecondsToWaitInt")
                    SecondsToWaitInt = (TimeSpan)inputParameter.Value;
            }
        }
    }
}
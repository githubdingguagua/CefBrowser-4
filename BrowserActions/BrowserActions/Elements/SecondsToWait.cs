using System;
using System.Collections.Generic;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [Serializable]
    public class SecondsToWait :BaseObject, IInstanciateInputParameters //Can be used for await and action
    {
        public InsecureInt Seconds = new InsecureInt();

        public SecondsToWait()
        {
            
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;

            Seconds = new InsecureInt(1);

           SetAvailableInputParameters();
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
                if (inputParameter.Key == "Seconds")
                    Seconds.Value = ((InsecureInt) inputParameter.Value).Value;
            }
            if (InputParameterAvailable.Count != 1)
                NewInstance();
        }

        public new void SetAvailableInputParameters()
        {
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Seconds", Seconds),
            };
        }
    }
}
using System;
using System.Collections.Generic;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserCommands
{
    [Serializable]
    public class SwitchUserInputEnabling : BrowserCommand, IInstanciateInputParameters
    {
        public InsecureBool Enabled = new InsecureBool(true);

        public SwitchUserInputEnabling() : base()
        {
           
        }

        public SwitchUserInputEnabling(string uid, bool enabled) : this()
        {
            UID = uid;
            Enabled.Value = enabled;
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
                "Enabled"
            };
            Description =
                "Let the user enable to use the mouse and keyboard in the browser";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Enabled")
                    Enabled = (InsecureBool)inputParameter.Value;
            }
            if (InputParameterAvailable.Count != 1)
                NewInstance();
        }

        public new void SetAvailableInputParameters()
        {
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Enabled", Enabled),
            };
        }
    }
}

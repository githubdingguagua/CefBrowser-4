using System;
using System.Collections.Generic;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserCommands
{
    [Serializable]
    public class SwitchWindowVisibility : BrowserCommand, IInstanciateInputParameters
    {
        public InsecureBool Visible = new InsecureBool(true);

        public SwitchWindowVisibility() : base()
        {
            
        }

        public SwitchWindowVisibility(string uid, bool visible) : this()
        {
            UID = uid;
            Visible.Value = visible;
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Visible", Visible),
            };
            InputParameterRequired = new List<string>()
            {
                "Visible"
            };
            Description =
                "Shows or hides the browser for the user";
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Visible")
                    Visible = (InsecureBool)inputParameter.Value;
            }
        }
    }
}

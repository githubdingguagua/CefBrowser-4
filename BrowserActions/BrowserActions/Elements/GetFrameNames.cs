using System;
using System.Collections.Generic;

namespace CefBrowserControl.BrowserActions.Elements
{
    [Serializable]
    public class GetFrameNames : BaseObject, IInstanciateInputParameters
    {
        public List<KeyValuePairEx<string, bool>> FrameNames { get; set; }

        public enum KeyList
        {
            FrameName,
        }

        public GetFrameNames()
        {
           
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.FrameName.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
            };
            InputParameterRequired = new List<string>()
            {
            };
            Description =
                "Gets all the framenames from the browser.";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {

        }

        public new void SetAvailableInputParameters()
        {

        }
    }
}

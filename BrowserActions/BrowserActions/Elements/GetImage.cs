//https://stackoverflow.com/questions/22172604/convert-image-url-to-base64/22172860#22172860

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(Selector))]
    [Serializable]
    public class GetImage : BaseObject, IInstanciateInputParameters
    {
        public Selector Selector = new Selector();

        public string Base64String;

        public enum KeyList
        {
            Base64String
        }

        public GetImage()
        {
           
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.Base64String.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector"
            };
            Description =
                "Extracts an image from the DOM in base64 format. POSSIBLE DEADLOCK AHED when the selector fails and no image was found!";
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

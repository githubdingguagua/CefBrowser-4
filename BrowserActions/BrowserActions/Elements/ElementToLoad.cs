using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(Selector))]
    [Serializable]
    public class ElementToLoad : BaseObject, IInstanciateInputParameters
    {
        public enum KeyList
        {
            NumberOfFoundElements
        }

        public Selector Selector = new Selector();

        public ElementToLoad()
        {
           
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.NumberOfFoundElements.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector"
            };
            Description =
                "Instructs the browser to wait until one or many elements are available in the DOM. POSSIBLE DEADLOCK AHED!";
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
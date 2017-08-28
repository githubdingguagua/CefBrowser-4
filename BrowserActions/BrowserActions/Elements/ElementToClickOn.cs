using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(Selector))]
    [Serializable]
    public class ElementToClickOn : BaseObject, IInstanciateInputParameters
    {
        public Selector Selector  = new Selector();
        public InsecureBool DoubleClick = new InsecureBool();

        public ElementToClickOn() : base()
        {
           
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            Description = "Click on an element in the DOM, do not use this function, as it does not work at the moment";
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
                new KeyValuePairEx<string, object>("DoubleClick", DoubleClick),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
            };
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Selector")
                    Selector = (Selector)inputParameter.Value;
                else if (inputParameter.Key == "DoubleClick")
                    DoubleClick = (InsecureBool) inputParameter.Value;
            }
        }
    }
}
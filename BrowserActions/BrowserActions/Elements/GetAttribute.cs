using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(Selector))]
    [Serializable]
    public class GetAttribute : BaseObject, IInstanciateInputParameters
    {
        public Selector Selector = new Selector();
        public InsecureText AttributeName = new InsecureText();
        public String Value { get; set; }

        public enum KeyList
        {
            Value
        }

        public GetAttribute()
        {
           
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.Value.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
                new KeyValuePairEx<string, object>("AttributeName", AttributeName),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
                "AttributeName",
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
                else if (inputParameter.Key == "AttributeName")
                    AttributeName = (InsecureText)inputParameter.Value;
            }
        }
    }
}
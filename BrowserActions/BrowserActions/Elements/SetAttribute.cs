using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(Selector))]
    [Serializable]
    public class SetAttribute : BaseObject, IInstanciateInputParameters
    {
        public Selector Selector = new Selector();
        public InsecureText AttributeName = new InsecureText();
        public InsecureText ValueToSet = new InsecureText();

        public SetAttribute() : base()
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
                new KeyValuePairEx<string, object>("Selector", Selector),
                new KeyValuePairEx<string, object>("AttributeName", AttributeName),
                new KeyValuePairEx<string, object>("ValueToSet", ValueToSet),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
                "AttributeName",
                "ValueToSet",
            };
            Description =
                "Instructs the browser set the attribute value of the one or more elements.";
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Selector")
                    Selector = (Selector)inputParameter.Value;
                else if (inputParameter.Key == "AttributeName")
                    AttributeName = (InsecureText)inputParameter.Value;
                else if (inputParameter.Key == "ValueToSet")
                    ValueToSet = (InsecureText)inputParameter.Value;
            }
        }
    }
}
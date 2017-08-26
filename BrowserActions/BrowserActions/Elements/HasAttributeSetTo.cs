using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.BrowserActions.Helper;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(Selector))]
    [XmlInclude(typeof(StringOrRegex))]
    [Serializable]
    public class HasAttributeSetTo : BaseObject, IInstanciateInputParameters
    {
        public Selector Selector = new Selector();
        public InsecureText AttributeName = new InsecureText();
        public StringOrRegex ExpectedValue = new StringOrRegex();

        public HasAttributeSetTo()
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
                new KeyValuePairEx<string, object>("ExpectedValue", ExpectedValue),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
                "AttributeName",
                "ExpectedValue",
            };
            Description =
                "Checks if one or more elements have set the value to the specified expected value. If that is the case, this object will set successfull to true!";
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Selector")
                    Selector = (Selector)inputParameter.Value;
                else if (inputParameter.Key == "AttributeName")
                    AttributeName = (InsecureText)inputParameter.Value;
                else if (inputParameter.Key == "ExpectedValue")
                    ExpectedValue = (StringOrRegex)inputParameter.Value;
            }
        }
    }
}
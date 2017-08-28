using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(Selector))]
    [Serializable]
    public class TextToTypeIn : BaseObject, IInstanciateInputParameters
    {
        public Selector Selector = new Selector();
        public InsecureText Text = new InsecureText();
        public InsecureBool PressEnterAfterwards = new InsecureBool(true);

        public TextToTypeIn() : base()
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
                new KeyValuePairEx<string, object>("Text", Text),
                new KeyValuePairEx<string, object>("PressEnterAfterwards", PressEnterAfterwards),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
                "Text",
            };
            Description =
                "Instructs the browser to simulate the text typing for the specified element. DO NOT USE, THIS IS NOT IMPLEMENTED!";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Selector")
                    Selector = (Selector)inputParameter.Value;
                else if (inputParameter.Key == "Text")
                    Text = (InsecureText)inputParameter.Value;
                else if (inputParameter.Key == "PressEnterAfterwards")
                    PressEnterAfterwards = (InsecureBool)inputParameter.Value;
            }
        }
    }
}
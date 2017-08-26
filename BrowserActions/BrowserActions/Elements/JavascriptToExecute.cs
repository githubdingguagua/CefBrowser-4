using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(JavascriptToExecute))]
    [Serializable]
    public class JavascriptToExecute : BaseObject, IInstanciateInputParameters
    {
        public Selector Selector = new Selector();
        public InsecureText Javascript = new InsecureText();

        public JavascriptToExecute NextJavascriptToExecute { get; set; }

        public void SetNextJavascriptToExecute(JavascriptToExecute js)
        {
            NextJavascriptToExecute = js;
        }

        public string ExecutionResult { get; set; }

        public enum KeyList
        {
            ExecutionResult,
        }

        public JavascriptToExecute()
        {
           
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.ExecutionResult.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
                new KeyValuePairEx<string, object>("Javascript", Javascript),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
                "Javascript",
            };
            Description =
                "Executes Javascript. If Selector is set, the javascript is tried to be executed on it";
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Selector")
                    Selector = (Selector)inputParameter.Value;
                else if (inputParameter.Key == "Javascript")
                    Javascript = (InsecureText)inputParameter.Value;
            }
        }
    }
}
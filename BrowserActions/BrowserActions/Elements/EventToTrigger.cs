using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(Selector))]
    [Serializable]
    public class EventToTrigger : BaseObject, IInstanciateInputParameters
    {
        //https://developer.mozilla.org/en-US/docs/Web/Guide/Events/Creating_and_triggering_events
        //Section Triggering built-in events
        //First one is selector, second one is event with attributes, may has to be changed to another class
        public Selector Selector  = new Selector();
        public InsecureText EventScriptBlock = new InsecureText();
        public string Result { get; set; }

        public enum KeyList
        {
            Result
        }

        public EventToTrigger()
        {
            
        }

        public new void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.Result.ToString());

            SetAvailableInputParameters();
            InputParameterRequired = new List<string>()
            {
                "Selector",
                "EventScriptBlock",
            };
            Description =
                "Invoke Events from https://developer.mozilla.org/en-US/docs/Web/Guide/Events/Creating_and_triggering_events";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Selector")
                    Selector = (Selector)inputParameter.Value;
                else if (inputParameter.Key == "EventScriptBlock")
                    EventScriptBlock = (InsecureText)inputParameter.Value;
            }
            if (InputParameterAvailable.Count != 2)
                NewInstance();
        }

        public new void SetAvailableInputParameters()
        {
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
                new KeyValuePairEx<string, object>("EventScriptBlock", EventScriptBlock),
            };
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace CefBrowserControl.BrowserActions.Elements.EventTypes
{
    [Serializable]
    public class InvokeMouseClick : EventToTrigger
    {
        public InvokeMouseClick() : base()
        {
           
        }

        public new void ReadInputParameters()
        {
            try
            {
                List<string> parametersSet = new List<string>();
                foreach (var parameterAvailableKeyValuePairEx in InputParameterAvailable)
                {
                    foreach (var parameterSetKeyValuePairEx in InputParameterSet)
                    {
                        if (parameterSetKeyValuePairEx.Key == parameterAvailableKeyValuePairEx.Key)
                        {
                            switch (parameterSetKeyValuePairEx.Key)
                            {
                                case "Selector":
                                    Selector = (Selector) parameterSetKeyValuePairEx.Value;
                                    break;
                                default:
                                    ExceptionHandling.Handling.GetException("Unexpected",
                                        new Exception("This parameter was not handlet in readinputparameters!"));
                                    break;
                            }
                            parametersSet.Add(parameterSetKeyValuePairEx.Key);
                        }
                    }
                }
                foreach (var requiredParameter in InputParameterRequired)
                {
                    if (!parametersSet.Contains(requiredParameter))
                        ExceptionHandling.Handling.GetException("Unexpected",
                            new Exception("Not all parameters have been set!"));
                }
            }
            catch (Exception ex)
            {
                ExceptionHandling.Handling.GetException("Unexpected", ex);
            }
        }

        public new void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.Result.ToString());
            EventScriptBlock.Value = @"new MouseEvent('click', {
    'view': window,
    'bubbles': true,
    'cancelable': true
  });";
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector",
            };
            Description =
               @"Invoke mouse event from https://developer.mozilla.org/en-US/docs/Web/Guide/Events/Creating_and_triggering_events directly on elements";
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

using System;
using System.Collections.Generic;
using System.Text;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements.EventTypes
{
    [Serializable]
    public class InvokeFullKeyboardEvent : EventToTrigger
    {
        public InsecureText KeyCode = new InsecureText();

        public InvokeFullKeyboardEvent() : base()
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
                                case "KeyCode":
                                    KeyCode.Value = ((InsecureText)parameterSetKeyValuePairEx.Value).Value;
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
            EventScriptBlock.Value = "";
            KeyCode.Value = "ArrowLeft";
            SetAvailableInputParameters();
            InputParameterRequired = new List<string>()
            {
                "Selector",
                "KeyCode",
            };
            Description =
               @"Invokes keydown keypress and keyup event of the given keyboard code of https://developer.mozilla.org/de/docs/Web/API/KeyboardEvent/key/Key_Values like ArrowLeft, ArrowRight etc. or a, test this on https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/code";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void SetAvailableInputParameters()
        {
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
                new KeyValuePairEx<string, object>("KeyCode", KeyCode),
            };
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "Selector")
                    Selector = (Selector)inputParameter.Value;
                if (inputParameter.Key == "KeyCode")
                    KeyCode.Value = ((InsecureText)inputParameter.Value).Value;
            }
            if (InputParameterAvailable.Count != 2)
                NewInstance();
        }

    }
}

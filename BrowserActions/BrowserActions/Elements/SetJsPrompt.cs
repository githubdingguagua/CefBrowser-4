using System;
using System.Collections.Generic;
using System.Text;
using CefBrowserControl.BrowserActions.Helper;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    public class SetJsPrompt : GetJsPrompt
    {
        public InsecureBool SetSuccess = new InsecureBool(true);
        public InsecureBool SetText = new InsecureBool(false);
        public InsecureText Text = new InsecureText();

        public SetJsPrompt() : base()
        {

        }

        public new void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("ExpectedMessageText", ExpectedMessageText),
                new KeyValuePairEx<string, object>("ExpectedDefaultPromptValue", ExpectedDefaultPromptValue),
                new KeyValuePairEx<string, object>("ExpectedDialogType", ExpectedDialogType),
                new KeyValuePairEx<string, object>("SetSuccess", SetSuccess),
                new KeyValuePairEx<string, object>("SetText", SetText),
                new KeyValuePairEx<string, object>("Text", Text)
            };
            InputParameterRequired = new List<string>()
            {
                "SetSuccess",
            };
            Description =
               "Instructs the browser react for an specific js prompt. This has to be done BEFORE the auth, otherwise it gets cancelled!";
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "ExpectedMessageText")
                    ExpectedMessageText = (StringOrRegex)inputParameter.Value;
                else if (inputParameter.Key == "ExpectedDefaultPromptValue")
                    ExpectedDefaultPromptValue = (StringOrRegex)inputParameter.Value;
                else if (inputParameter.Key == "ExpectedDialogType")
                    ExpectedDialogType = (InsecureDialogType)inputParameter.Value;
                else if (inputParameter.Key == "SetSuccess")
                    SetSuccess = (InsecureBool)inputParameter.Value;
                else if (inputParameter.Key == "SetText")
                    SetText = (InsecureBool)inputParameter.Value;
                else if (inputParameter.Key == "Text")
                    Text = (InsecureText)inputParameter.Value;
            }
        }
    }

}

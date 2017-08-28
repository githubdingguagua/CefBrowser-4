using System;
using System.Collections.Generic;
using CefBrowserControl.BrowserActions.Helper;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [Serializable]
    public class GetJsPrompt : BaseObject, IInstanciateInputParameters
    {
        public enum KeyList
        {
            MessageText,
            DefaultPromptValue,
            DialogType,
        }

        public enum DialogTypes
        {
            Alert,
            Confirm,
            Prompt,
            Nope
        }

        public StringOrRegex ExpectedMessageText = new StringOrRegex() { IsRegex = new InsecureBool() { Value = true } };
        public StringOrRegex ExpectedDefaultPromptValue = new StringOrRegex() { IsRegex = new InsecureBool() { Value = true } };
        public InsecureDialogType ExpectedDialogType = new InsecureDialogType();
        public string MessageText = "";
        public string DefaultPromptValue = "";
        public DialogTypes DialogType = DialogTypes.Nope;

        public GetJsPrompt()
        {
            
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;

            ReturnedOutputKeysList.Add(KeyList.MessageText.ToString());
            ReturnedOutputKeysList.Add(KeyList.DefaultPromptValue.ToString());
            ReturnedOutputKeysList.Add(KeyList.DialogType.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("ExpectedMessageText", ExpectedMessageText),
                new KeyValuePairEx<string, object>("ExpectedDefaultPromptValue", ExpectedDefaultPromptValue),
                new KeyValuePairEx<string, object>("ExpectedDialogType", ExpectedDialogType),
            };
            InputParameterRequired = new List<string>()
            {
            };
            Description =
                "Gets an normally showed js prompt. Note that the prompt has to be set first, or the browser will ignore it! POSSIBLE DEADLOCK AHED when no prompt has shown, because Browser will wait for it!";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
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
            }
        }
    }
}

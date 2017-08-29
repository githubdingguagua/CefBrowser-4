using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using CefBrowserControl.BrowserActions.Helper;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [Serializable]
    public class SetHttpAuth : GetHttpAuth
    {
        public InsecureBool Cancel = new InsecureBool();
        public InsecureText Username = new InsecureText();
        public InsecureText Password = new InsecureText();

        public SetHttpAuth() : base()
        {

        }

        public new void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;

            Cancel.Value = false;

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("ExpectedSchemaType", ExpectedSchemaType),
                new KeyValuePairEx<string, object>("ExpectedHost", ExpectedHost),
                new KeyValuePairEx<string, object>("ExpectedPort", ExpectedPort),
                new KeyValuePairEx<string, object>("ExpectedRealm", ExpectedRealm),
                new KeyValuePairEx<string, object>("Cancel", Cancel),
                new KeyValuePairEx<string, object>("Username", Username),
                new KeyValuePairEx<string, object>("Password", Password),

            };
            InputParameterRequired = new List<string>()
            {
                "Cancel",
                "Username",
                "Password",
            };
            Description =
                "Instructs the browser to cancel an http auth or set username and password for it. This has to be done BEFORE the auth, otherwise it gets cancelled!";
            TimeoutInSec = Options.DefaultTimeoutSeconds;
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "ExpectedSchemaType")
                    ExpectedSchemaType = (InsecureHttpAuthSchemaType)inputParameter.Value;
                else if (inputParameter.Key == "ExpectedHost")
                    ExpectedHost = (StringOrRegex)inputParameter.Value;
                else if (inputParameter.Key == "ExpectedPort")
                    ExpectedPort = (InsecureInt)inputParameter.Value;
                else if (inputParameter.Key == "ExpectedRealm")
                    ExpectedRealm = (StringOrRegex)inputParameter.Value;
                else if (inputParameter.Key == "Cancel")
                    Cancel = (InsecureBool)inputParameter.Value;
                else if (inputParameter.Key == "Username")
                    Username = (InsecureText)inputParameter.Value;
                else if (inputParameter.Key == "Password")
                    Password = (InsecureText)inputParameter.Value;
            }
        }
    }

}

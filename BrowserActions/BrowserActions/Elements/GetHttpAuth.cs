using System;
using System.Collections.Generic;
using CefBrowserControl.BrowserActions.Helper;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserActions.Elements
{
    [Serializable]
    public class GetHttpAuth : BaseObject, IInstanciateInputParameters
    {
        public enum SchemaTypes
        {
            basic,
            digest,
            Nope
        }

        public InsecureHttpAuthSchemaType ExpectedSchemaType = new InsecureHttpAuthSchemaType();
        public StringOrRegex ExpectedHost = new StringOrRegex() {IsRegex = new InsecureBool() {Value = true} };
        public InsecureInt ExpectedPort = new InsecureInt(null);
        public StringOrRegex ExpectedRealm = new StringOrRegex() { IsRegex = new InsecureBool() { Value = true } };
        public string Host = "";
        public int Port = 0;
        public string Realm = "";
        public SchemaTypes Scheme = SchemaTypes.Nope;

        public enum KeyList
        {
            Host,
            Port,
            Realm,
            Scheme
        }

        public GetHttpAuth()
        {
            
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;

            ReturnedOutputKeysList.Add(KeyList.Host.ToString());
            ReturnedOutputKeysList.Add(KeyList.Port.ToString());
            ReturnedOutputKeysList.Add(KeyList.Scheme.ToString());
            ReturnedOutputKeysList.Add(KeyList.Realm.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("ExpectedSchemaType", ExpectedSchemaType),
                new KeyValuePairEx<string, object>("ExpectedHost", ExpectedHost),
                new KeyValuePairEx<string, object>("ExpectedPort", ExpectedPort),
                new KeyValuePairEx<string, object>("ExpectedRealm", ExpectedRealm),
            };
            InputParameterRequired = new List<string>()
            {
            };
            Description =
                "Gets the http auth infos of an elapsed authentication. SetHttpAuth has to be set before, or browser will loop!";
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
            }
        }
    }
}

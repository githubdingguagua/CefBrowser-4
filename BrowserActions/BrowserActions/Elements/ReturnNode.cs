using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CefBrowserControl.BrowserActions.Elements
{
    [XmlInclude(typeof(Selector))]
    [XmlInclude(typeof(ReturnNode))]
    [Serializable]
    public class ReturnNode : BaseObject, IInstanciateInputParameters
    {
        //int = id which the code wants to see, string is the selector
        public Selector Selector  = new Selector();

        public string SerializedNode { get; set; }

        public void SetSerializedNode(string node)
        {
            SerializedNode = node;
        }

        public ReturnNode NextReturnNode { set; get; }

        public enum KeyList
        {
            SerializedNode
        }

        public  ReturnNode():base()
        {
            
        }

        public void SetNextReturnNode(ReturnNode node)
        {
            NextReturnNode = node;

           
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.SerializedNode.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("Selector", Selector),
            };
            InputParameterRequired = new List<string>()
            {
                "Selector"
            };
            Description =
                "Returns the full Node of an element in xml serialized format";
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
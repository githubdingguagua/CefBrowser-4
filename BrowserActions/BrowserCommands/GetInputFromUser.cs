﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using CefBrowserControl.Resources;

namespace CefBrowserControl.BrowserCommands
{
    [Serializable]
    [XmlInclude(typeof(InsecureText))]
    [XmlInclude(typeof(InsecureImage))]
    public class GetInputFromUser : BrowserCommand, IInstanciateInputParameters
    {
        //Only text and base64 encoded images
        public List<object> InsecureDisplayObjects = new List<object>();

        public InsecureBool InputNeeded = new InsecureBool(true);

        public string UserInputResult;

        public enum KeyList
        {
            UserInputResult
        }

        public GetInputFromUser()
        {
           
        }

        public void NewInstance()
        {
            if (!HaveRequirementsBeenSet)
                HaveRequirementsBeenSet = true;
            else
                return;
            ReturnedOutputKeysList.Add(KeyList.UserInputResult.ToString());

            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("InsecureDisplayObjects", InsecureDisplayObjects),
                new KeyValuePairEx<string, object>("InputNeeded", InputNeeded),
            };
            InputParameterRequired = new List<string>()
            {
                "InsecureDisplayObjects",
                "InputNeeded",
            };
            Description =
                "Gets input from user";
        }

        public new void ReadAvailableInputParameters()
        {
            foreach (var inputParameter in InputParameterAvailable)
            {
                if (inputParameter.Key == "InsecureDisplayObjects")
                    InsecureDisplayObjects = (List<object>) inputParameter.Value;
                if (inputParameter.Key == "InputNeeded")
                    InputNeeded = (InsecureBool)inputParameter.Value;
            }
        }
    }
}
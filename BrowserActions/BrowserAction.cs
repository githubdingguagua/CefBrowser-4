using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CefBrowserControl.BrowserActions.Elements;
using CefBrowserControl.BrowserActions.Elements.EventTypes;
using CefBrowserControl.BrowserActions.Elements.ExecJavascriptHelper;
using CefBrowserControl.BrowserActions.Helper;
using CefBrowserControl.Resources;

namespace CefBrowserControl
{
    [XmlInclude(typeof(ElementToClickOn))]
    [XmlInclude(typeof(ElementToLoad))]
    [XmlInclude(typeof(EventToTrigger))]
    [XmlInclude(typeof(FrameLoaded))]
    [XmlInclude(typeof(GetAttribute))]
    [XmlInclude(typeof(GetFrameNames))]
    [XmlInclude(typeof(GetStyle))]
    [XmlInclude(typeof(HasAttributeSetTo))]
    [XmlInclude(typeof(HasStyleSetTo))]
    [XmlInclude(typeof(JavascriptToExecute))]
    [XmlInclude(typeof(ResourceToLoad))]
    [XmlInclude(typeof(ReturnNode))]
    [XmlInclude(typeof(SecondsToWait))]
    [XmlInclude(typeof(SetAttribute))]
    [XmlInclude(typeof(SetStyle))]
    [XmlInclude(typeof(SiteLoaded))]
    [XmlInclude(typeof(TextToTypeIn))]
    [XmlInclude(typeof(GetJsPrompt))]
    [XmlInclude(typeof(SetJsPrompt))]
    [XmlInclude(typeof(GetHttpAuth))]
    [XmlInclude(typeof(SetHttpAuth))]
    [XmlInclude(typeof(BaseObject))]
    [XmlInclude(typeof(GetImage))]
    [XmlInclude(typeof(KeyValuePairEx<string, string>))]
    [XmlInclude(typeof(KeyValuePairEx<ActionState, string>))]
    [XmlInclude(typeof(InvokeSubmit))]
    [XmlInclude(typeof(InvokeMouseClick))]
    [XmlInclude(typeof(GetInnerText))]
    [XmlInclude(typeof(GetInnerHtml))]
    [XmlInclude(typeof(SetValue))]
    [XmlInclude(typeof(InvokeFullKeyboardEvent))]



    [Serializable]
    public class BrowserAction : BaseObject
    {
        private static int counter = 0;

        public  string UID;
        public  string UCID;

      
        public BrowserAction()
        {
            GenerateNewUCID();

            Description =
                @"A 'Browser Action' can be sent to the browser and interacts with the DOM. You can nearly interact with everything you can interact in a normal browser.";
            InputParameterAvailable = new List<KeyValuePairEx<string, object>>()
            {
                new KeyValuePairEx<string, object>("ActionObject", ActionObject),
                new KeyValuePairEx<string, object>("ActionFrameName", ActionFrameName),
            };
            InputParameterRequired = new List<string>
            {
                "ActionObject",
            };
        }

        public BrowserAction(string uid): this()
        {
            UID = uid;
        }

        private void GenerateNewUCID()
        {
            UCID = HashingEx.Hashing.GetSha1Hash(DateTime.Now.ToString() + " " + counter++ + Guid.NewGuid());
        }

        public void GenerateNewUCID(string additional)
        {
            UCID = HashingEx.Hashing.GetSha1Hash(DateTime.Now.ToString() + " " + counter++ + additional + Guid.NewGuid());
        }

        public enum ExecuteActionOn
        {
            Id,
            Name,
            TagName,
            Class,
            Xpath,
        }

        public bool CanBeExecutedParallel { get; set; } = false;
        public bool ReturnResult { get; set; } = true;

        public object ActionObject { get; set; }

        public string SerializedActionObject { get; set; } = "";

        public string SerializedActionObjectType { get; set; }

        public StringOrRegex ActionFrameName { get; set; } = null;

    }



    public class Selector
    {
        public string SelectorString { get; set; } = "";

        //public bool MultipleElementsAllowed { get; set; } = false;

        public int FoundElements { get; set; }

        public void SetFoundElements(int numberOfElements)
        {
            FoundElements = numberOfElements;
        }

        public InsecureInt ExpectedNumberOfElements = new InsecureInt(1);

        public BrowserAction.ExecuteActionOn SelectorExecuteActionOn { get; set; }
    }
}


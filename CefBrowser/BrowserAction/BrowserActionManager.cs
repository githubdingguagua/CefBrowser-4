using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Drawing;
using System.Linq;
using CefBrowser.Gateway;
using CefBrowserControl;
using CefBrowserControl.BrowserActions.Elements;
using CefBrowserControl.BrowserActions.Elements.EventTypes;
using CefBrowserControl.BrowserActions.Helper;
using CefBrowserControl.Resources;
using Rectangle = CefBrowserControl.BrowserActions.Helper.Rectangle;
using Timer = System.Timers.Timer;

namespace CefBrowser.BrowserAction
{
    public class BrowserActionManager
    {
        private static readonly ReaderWriterLockSlim _actionListLockSlim = new ReaderWriterLockSlim();

        private readonly Timer _timer;
        public bool ActionsEnabled { get; set; }

        public bool ExecutingActions { get; private set; }

        public static Queue<object> BrowserActions { get; } = new Queue<object>();
        public static Queue<object> BrowserActionsCompleted { get; } = new Queue<object>();

        private readonly Gateway.Gateway _gateway;

        private int _threadSleepTime = 100;

        //private readonly WebBrowser _webBrowser;

        public BrowserActionManager(Gateway.Gateway gateway /*, WebBrowser webBrowser*/)
        {
            //_webBrowser = webBrowser;
            _gateway = gateway;
            _timer = new Timer {Interval = 100};
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (ActionsEnabled)
            {
                _timer.Stop();

                HandleActions();

                if (ActionsEnabled)
                    _timer.Start();
            }
        }

        private void HandleActions()
        {
            ExecutingActions = true;
            ExecuteBrowserActions();
            //_webBrowser.Dispatcher.Invoke(ExecuteBrowserActions);
            ExecutingActions = false;
        }

        private static string EscapeJavascript(string script)
        {
            string sanitized = script.Replace(@"\", @"\\").Replace(@"'", @"\'").Replace("\"", "\\\"");
            //string sanitized = "atob('" + EncodingEx.Base64.Encoder.EncodeString(Encoding.UTF8, script) + "')";

            return sanitized;
        }

        public void AddBrowserActions(object browserAction)
        {
            _actionListLockSlim.EnterWriteLock();
            BrowserActions.Enqueue(browserAction);
            _actionListLockSlim.ExitWriteLock();
        }

        public object GetCompletedBrowserAction()
        {
            object obj = null;
            _actionListLockSlim.EnterWriteLock();
            if(BrowserActionsCompleted.Count > 0)
                obj =  BrowserActionsCompleted.Dequeue();
            _actionListLockSlim.ExitWriteLock();
            return obj;
        }

        public static string GenerateElementExistString(Selector selector, bool selectLastElement = false)
        {
            string baseSelector = BuildExecuteOnSelector(
                selector.SelectorString,
                selector.SelectorExecuteActionOn,
                selector.ExpectedNumberOfElements.Value, selectLastElement);
            bool nonSingleReturningElements =
                selector.SelectorExecuteActionOn !=
                CefBrowserControl.BrowserAction.ExecuteActionOn.Id;
            string extensionString = nonSingleReturningElements
                ? (selector.ExpectedNumberOfElements.Value > 1
                    ? (selector.SelectorExecuteActionOn ==
                       CefBrowserControl.BrowserAction.ExecuteActionOn.Xpath
                        ? ""
                        : ".length > 0")
                    : "")
                : "";
            return $"(function(){{if({baseSelector + extensionString}){{return true;}}else{{return false;}}}})();";
        }

        public GatewayAction.EvaluateJavascript GetAttribute(GetAttribute element, StringOrRegex frameName, Type actionType, int iterationNr)
        {
            string script = BuildExecuteOnSelector(element.Selector.SelectorString,
                                                                element.Selector.SelectorExecuteActionOn, iterationNr, true) +
                                                            (actionType==
                                                             typeof(GetAttribute)
                                                                ? ".getAttribute('" +
                                                                  element.AttributeName + "')"
                                                                : ".style" + element.AttributeName);
            GatewayAction.EvaluateJavascript evaluateJavascript = new GatewayAction.
                EvaluateJavascript()
            {
                FrameName = frameName,
                Timeout = element.Timeout,
                Script = script,
            };
            _gateway.AddGatewayAction(evaluateJavascript);
            while (!evaluateJavascript.Completed)
                Thread.Sleep(_threadSleepTime);
            return evaluateJavascript;
        }


        public void ExecuteBrowserActions()
        {
            GatewayAction.IsBrowserInitiated initializedAction = new GatewayAction.IsBrowserInitiated();
            _gateway.AddGatewayAction(initializedAction);
            while (!initializedAction.Completed)
            {
                Thread.Sleep(_threadSleepTime);
            }
            if (!initializedAction.Result)
                return;

            while (BrowserActions.Count > 0)
            {
                #region actions

                _actionListLockSlim.EnterWriteLock();
                var oList = BrowserActions.Dequeue();
                _actionListLockSlim.ExitWriteLock();

                var actionList = WebBrowserEx.ConvertObjectToObjectList(oList);
                foreach (object actionObject in actionList)
                {
                    var action = (CefBrowserControl.BrowserAction) actionObject;
                    //var actionFrame = _webBrowser.GetAccordingFrame(action.ActionFrameName);

                    if (action.ActionObject != null)
                    {
                        if (action.ActionObject.GetType() == typeof(GetImage))
                        {
                            bool failDueTimeout = false;
                            bool success = true;
                            while (!failDueTimeout)
                            {
                                List<object> imagesToGet =
                                    WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                                foreach (var imageToGet in imagesToGet)
                                {
                                    GetImage getImage = (GetImage) imageToGet;
                                    GatewayAction.EvaluateJavascript javascript = new GatewayAction.
                                        EvaluateJavascript();
                                    string scriptblock = @"(function(){
var img = ";
                                    scriptblock += BuildExecuteOnSelector(getImage.Selector.SelectorString,
                                        getImage.Selector.SelectorExecuteActionOn);
                                    scriptblock += @";
var canvas = document.createElement('canvas');
canvas.width = img.width;
canvas.height = img.height;
var ctx = canvas.getContext('2d');
ctx.drawImage(img, 0, 0);
var dataURL = canvas.toDataURL('image/png');
return dataURL.replace(/^ data:image\/ (png | jpg); base64,/, '');})(); ";
                                    javascript.Script =
                                        action.TranslatePlaceholderStringToSingleString(scriptblock);
                                    _gateway.AddGatewayAction(javascript);
                                    while (true)
                                    {
                                        if (javascript.Completed)
                                            break;
                                        if (ShouldBreakDueToTimeout(getImage))
                                        {
                                            failDueTimeout = true;
                                            break;
                                        }
                                        Thread.Sleep(100);
                                    }
                                    if (javascript.Completed && javascript.Success)
                                    {
                                        getImage.Base64String = (string) javascript.Response.Result;
                                        getImage.ReturnedOutput.Add(
                                            new KeyValuePairEx<string, string>(
                                                GetImage.KeyList.Base64String.ToString(),
                                                (string) javascript.Response.Result));
                                        getImage.Successful = true;
                                    }
                                    if (!javascript.Success)
                                    {
                                        success = false;
                                    }
                                    getImage.Completed = true;
                                }
                                if (success)
                                    break;
                            }
                            action.Successful = success;
                        }
                        else if (action.ActionObject.GetType() == typeof(GetHttpAuth))
                        {
                            List<object> httpAuthsToGet =
                                WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            bool failedOnce = false;
                            foreach (var o in httpAuthsToGet)
                            {
                                while (!failedOnce)
                                {
                                    GetHttpAuth getHttpAuth = (GetHttpAuth)o;
                                    GatewayAction.GetHttpAuth getHttpAuthGw = new GatewayAction.GetHttpAuth()
                                    {
                                        ExpectedRealm = getHttpAuth.ExpectedRealm,
                                        ExpectedPort = getHttpAuth.ExpectedPort.Value,
                                        ExpectedSchemaType = getHttpAuth.ExpectedSchemaType.Value,
                                        ExpectedHost = getHttpAuth.ExpectedHost,
                                    };
                                    _gateway.AddGatewayAction(getHttpAuthGw);
                                    while (!getHttpAuthGw.Completed)
                                    {
                                        if (ShouldBreakDueToTimeout(getHttpAuth))
                                        {
                                            failedOnce = true;
                                            break;
                                        }
                                        Thread.Sleep(100);
                                    }
                                    if (getHttpAuthGw.Success)
                                    {
                                        getHttpAuth.Successful = true;
                                        getHttpAuth.Completed = true;
                                        getHttpAuth.Host = getHttpAuthGw.Host;
                                        getHttpAuth.Port = getHttpAuthGw.Port;
                                        getHttpAuth.Realm = getHttpAuthGw.Realm;
                                        getHttpAuth.Scheme = getHttpAuthGw.Scheme;
                                        getHttpAuth.ReturnedOutput.Add(
                                            new KeyValuePairEx<string, string>(
                                                GetHttpAuth.KeyList.Host.ToString(),
                                                getHttpAuthGw.Host));
                                        getHttpAuth.ReturnedOutput.Add(
                                            new KeyValuePairEx<string, string>(
                                                GetHttpAuth.KeyList.Port.ToString(),
                                                getHttpAuthGw.Port.ToString()));
                                        getHttpAuth.ReturnedOutput.Add(
                                            new KeyValuePairEx<string, string>(
                                                GetHttpAuth.KeyList.Realm.ToString(),
                                                getHttpAuthGw.Realm));
                                        getHttpAuth.ReturnedOutput.Add(
                                            new KeyValuePairEx<string, string>(
                                                GetHttpAuth.KeyList.Scheme.ToString(),
                                                getHttpAuthGw.Scheme.ToString()));
                                        break;
                                    }
                                    else
                                    {
                                        //failedOnce = true;
                                        Thread.Sleep(500);
                                    }
                                }
                            }
                            if (!failedOnce)
                                action.Successful = true;
                        }
                        else if (action.ActionObject.GetType() == typeof(SetHttpAuth))
                        {
                            List<object> setHttpAuths =
                                WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            bool failedOnce = false;
                            foreach (var o in setHttpAuths)
                            {
                                while (true)
                                {
                                    SetHttpAuth setHttpAuth = (SetHttpAuth)o;
                                    GatewayAction.SetHttpAuth setHttpAuthGw = new GatewayAction.SetHttpAuth()
                                    {
                                        ExpectedRealm = action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(setHttpAuth.ExpectedRealm),
                                        ExpectedPort = setHttpAuth.ExpectedPort.Value,
                                        ExpectedSchemaType = setHttpAuth.ExpectedSchemaType.Value,
                                        ExpectedHost = action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(setHttpAuth.ExpectedHost),
                                        Cancel = setHttpAuth.Cancel.Value,
                                        Username = action.TranslatePlaceholderStringToSingleString(setHttpAuth.Username.Value),
                                        Password = action.TranslatePlaceholderStringToSingleString(setHttpAuth.Password.Value),
                                    };
                                    _gateway.AddGatewayAction(setHttpAuthGw);
                                    while (!setHttpAuthGw.Completed)
                                    {
                                        if (ShouldBreakDueToTimeout(setHttpAuth))
                                        {
                                            failedOnce = true;
                                            break;
                                        }
                                        Thread.Sleep(100);
                                    }
                                    if (setHttpAuthGw.Success)
                                    {
                                        action.Successful = true;
                                        setHttpAuth.Successful = true;
                                        setHttpAuth.Completed = true;
                                        break;
                                    }
                                    else
                                    {
                                        Thread.Sleep(500);
                                    }
                                }
                            }
                            if (!failedOnce)
                                action.Successful = true;
                        }
                        else if (action.ActionObject.GetType() == typeof(GetJsPrompt))
                        {
                            List<object> jsDialogsToGet =
                                WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            bool failedOnce = false;
                            foreach (var o in jsDialogsToGet)
                            {
                                while (!failedOnce)
                                {
                                    GetJsPrompt getPrompt = (GetJsPrompt) o;
                                    GatewayAction.GetJsDialog getPromptGw = new GatewayAction.GetJsDialog()
                                    {
                                        ExpectedDialogType = getPrompt.ExpectedDialogType.Value,
                                        ExpectedMessageText =
                                            action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(
                                                getPrompt.ExpectedMessageText),
                                        ExpectedDefaultPromptValue =
                                            action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(
                                                getPrompt.ExpectedDefaultPromptValue),
                                    };
                                    _gateway.AddGatewayAction(getPromptGw);
                                    while (!getPromptGw.Completed)
                                    {
                                        if (ShouldBreakDueToTimeout(getPrompt))
                                        {
                                            failedOnce = true;
                                            break;
                                        }
                                        Thread.Sleep(100);
                                    }
                                    if (getPromptGw.Success)
                                    {
                                        getPrompt.Successful = true;
                                        getPrompt.Completed = true;
                                        getPrompt.DialogType = getPromptGw.DialogType;
                                        getPrompt.MessageText = getPromptGw.MessageText;
                                        getPrompt.DefaultPromptValue = getPromptGw.DefaultPromptValue;
                                        getPrompt.ReturnedOutput.Add(
                                            new KeyValuePairEx<string, string>(
                                                GetJsPrompt.KeyList.DialogType.ToString(),
                                                getPromptGw.DialogType.ToString()));
                                        getPrompt.ReturnedOutput.Add(
                                            new KeyValuePairEx<string, string>(
                                                GetJsPrompt.KeyList.MessageText.ToString(),
                                                getPromptGw.MessageText));
                                        getPrompt.ReturnedOutput.Add(
                                            new KeyValuePairEx<string, string>(
                                                GetJsPrompt.KeyList.DefaultPromptValue.ToString(),
                                                getPromptGw.DefaultPromptValue));
                                        break;
                                    }
                                    else
                                    {
                                        //failedOnce = true;
                                        Thread.Sleep(500);
                                    }
                                }
                            }
                            if (!failedOnce)
                                action.Successful = true;
                        }
                        else if (action.ActionObject.GetType() == typeof(SetJsPrompt))
                        {
                            List<object> jsDialogsToGet =
                                WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            bool failedOnce = false;
                            foreach (var o in jsDialogsToGet)
                            {
                                while (true)
                                {
                                    SetJsPrompt setPrompt = (SetJsPrompt) o;
                                    GatewayAction.SetJsDialog setPromptGw = new GatewayAction.SetJsDialog()
                                    {
                                        ExpectedDialogType = setPrompt.ExpectedDialogType.Value,
                                        ExpectedMessageText =
                                            action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(
                                                setPrompt.ExpectedMessageText),
                                        ExpectedDefaultPromptValue =
                                            action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(
                                                setPrompt.ExpectedDefaultPromptValue),
                                        SetText = setPrompt.SetText.Value,
                                        SetSuccess = setPrompt.SetSuccess.Value,
                                        Text = action.TranslatePlaceholderStringToSingleString(setPrompt.Text.Value),
                                    };
                                    _gateway.AddGatewayAction(setPromptGw);
                                    while (!setPromptGw.Completed)
                                    {
                                        if (ShouldBreakDueToTimeout(setPrompt))
                                        {
                                            failedOnce = true;
                                            break;
                                        }
                                        Thread.Sleep(100);
                                    }
                                    if (setPromptGw.Success)
                                    {
                                        action.Successful = true;
                                        setPrompt.Successful = true;
                                        setPrompt.Completed = true;
                                        break;
                                    }
                                    else
                                    {
                                        Thread.Sleep(500);
                                    }
                                }
                            }
                            if (!failedOnce)
                                action.Successful = true;
                        }
                        else if (action.ActionObject.GetType() == typeof(GetFrameNames))
                            //TODO Create isMain Frame Method
                        {
                            List<object> frameNamesToGet =
                                WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            foreach (var o in frameNamesToGet)
                            {
                                var frame = (GetFrameNames) o;
                                GatewayAction.GetFrameNames frameNames = new GatewayAction.GetFrameNames();
                                _gateway.AddGatewayAction(frameNames);
                                while (!frameNames.Completed)
                                {
                                    if (ShouldBreakDueToTimeout(frame))
                                    {
                                        break;
                                    }
                                    Thread.Sleep(100);
                                }
                                frame.FrameNames = new List<KeyValuePairEx<string, bool>>();
                                foreach (var frameName in frameNames.Frames)
                                {
                                    frame.FrameNames.Add(new KeyValuePairEx<string, bool>(frameName.Key,
                                        frameName.Value));
                                }
                                //Bool = isMainFrame
                                foreach (KeyValuePair<string, bool> keyValuePair in frameNames.Frames)
                                {
                                    frame.ReturnedOutput.Add(
                                        new KeyValuePairEx<string, string>(
                                            GetFrameNames.KeyList.FrameName.ToString(), keyValuePair.Key));
                                }
                                frame.Successful = true;
                                frame.Completed = true;
                                action.Successful = true;
                            }
                        }
                        else if (action.ActionObject.GetType() == typeof(ElementToLoad))
                        {
                            List<object> elementsToLoad = WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            var found = false;
                            bool brokeTimeout = false;
                            while (!found && !brokeTimeout)
                            {
                                found = true;
                                foreach (var selector in elementsToLoad)
                                {
                                    ElementToLoad element = (ElementToLoad) selector;
                                    if (ShouldBreakDueToTimeout(element))
                                    {
                                        brokeTimeout = true;
                                        break;
                                    }

                                    string testString =
                                        GenerateElementExistString(element.Selector);

                                    GatewayAction.EvaluateJavascript evaluateJavascript = new GatewayAction.
                                        EvaluateJavascript()
                                        {
                                            Script = action.TranslatePlaceholderStringToSingleString(testString),
                                            FrameName =
                                                action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(
                                                    action.ActionFrameName),
                                            Timeout = element.Timeout,
                                        };
                                    _gateway.AddGatewayAction(evaluateJavascript);
                                    while (!evaluateJavascript.Completed)
                                    {
                                        if (ShouldBreakDueToTimeout(element))
                                        {
                                            break;
                                        }
                                        Thread.Sleep(_threadSleepTime);
                                    }
                                    //var task = await actionFrame.EvaluateScriptAsync(testString, element.Timeout);
                                    if (evaluateJavascript.Response.Success)
                                    {
                                        var response = evaluateJavascript.Response.Result;
                                        {
                                            try
                                            {
                                                var foundInThis = Convert.ToBoolean(response);
                                                if (!foundInThis)
                                                    found = false;
                                                else
                                                {
                                                    KeyValuePairEx<int, string> numberOfElements =
                                                        FindElements(element.Selector, element.Timeout);
                                                    if (numberOfElements.Key == 0 &&
                                                        element.Selector.ExpectedNumberOfElements.Value > 0 ||
                                                        numberOfElements.Key !=
                                                        element.Selector.ExpectedNumberOfElements.Value &&
                                                        element.Selector.ExpectedNumberOfElements.Value > 0)
                                                    {
                                                        found = false;
                                                    }
                                                    element.ReturnedOutput.Add(
                                                        new KeyValuePairEx<string, string>(
                                                            ElementToLoad.KeyList.NumberOfFoundElements.ToString(),
                                                            numberOfElements.Value));
                                                    element.Successful = true;
                                                    element.Completed = true;
                                                }
                                            }
                                            catch (FormatException)
                                            {
                                                found = false;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }
                            }
                            if (found)
                            {
                                action.Successful = true;
                            }
                        }
                        else if (action.ActionObject.GetType() == typeof(ResourceToLoad))
                        {
                            List<object> resourcesToLoad =
                                WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            foreach (Object o in resourcesToLoad)
                            {
                                ResourceToLoad resource = (ResourceToLoad) o;
                                bool found = false;
                                bool brokeTimeout = false;
                                while (!found && !brokeTimeout)
                                {
                                    if (ShouldBreakDueToTimeout(resource))
                                    {
                                        brokeTimeout = true;
                                        break;
                                    }
                                    GatewayAction.ResourceLoaded resourceLoaded = new GatewayAction.
                                        ResourceLoaded()
                                        {
                                            ResourceUrl =
                                                action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(
                                                    resource.ExpectedResourceUrl),
                                            FrameName =
                                                action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(
                                                    resource.ExpectedFrameName),
                                        };
                                    _gateway.AddGatewayAction(resourceLoaded);
                                    while (!resourceLoaded.Completed)
                                    {
                                        if (ShouldBreakDueToTimeout(resource))
                                            break;
                                        Thread.Sleep(_threadSleepTime);

                                    }
                                    if (resourceLoaded.Success)
                                    {
                                        resource.ResourceUrl = resource.ExpectedResourceUrl.Value.Value;
                                        resource.LoadedAt = resourceLoaded.DateTime;
                                        resource.ReturnedOutput.Add(
                                            new KeyValuePairEx<string, string>(
                                                ResourceToLoad.KeyList.ResourceUrl.ToString(),
                                                resource.ExpectedResourceUrl.Value.Value));
                                        resource.ReturnedOutput.Add(
                                            new KeyValuePairEx<string, string>(
                                                ResourceToLoad.KeyList.LoadedAt.ToString(),
                                                resourceLoaded.DateTime.ToString()));
                                        found = true;
                                        resource.Successful = true;
                                        resource.Completed = true;
                                    }
                                }
                            }
                            action.Successful = true;
                        }
                        else if (action.ActionObject.GetType() == typeof(SecondsToWait))
                        {
                            var secondsToSleep = (int) action.ActionObject;
                            Thread.Sleep(secondsToSleep * 1000);
                            action.Successful = true;
                        }
                        else if (action.ActionObject.GetType() == typeof(SiteLoaded))
                            //https://developer.mozilla.org/en-US/docs/Web/Events/DOMContentLoaded
                        {
                            bool shouldBreakDueTimeout = false;
                            while (!shouldBreakDueTimeout)
                            {
                                var siteLoaded = (SiteLoaded) action.ActionObject;
                                if (siteLoaded != null)
                                {
                                    if (ShouldBreakDueToTimeout(siteLoaded))
                                    {
                                        shouldBreakDueTimeout = true;
                                        break;
                                    }
                                }
                                GatewayAction.SiteLoaded siteLoadedAction = new GatewayAction.SiteLoaded()
                                {
                                    Timeout = siteLoaded.Timeout,
                                };
                                _gateway.AddGatewayAction(siteLoadedAction);
                                while (!siteLoadedAction.Completed)
                                {
                                    if (ShouldBreakDueToTimeout(siteLoaded))
                                    {
                                        shouldBreakDueTimeout = true;
                                        break;
                                    }
                                    Thread.Sleep(_threadSleepTime);
                                }
                                if (siteLoadedAction.Success && siteLoadedAction.Address != null)
                                {
                                    siteLoaded.ExpectedSiteToLoad =
                                        action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(
                                            siteLoaded.ExpectedSiteToLoad);
                                    if (siteLoaded.ExpectedSiteToLoad != null)
                                        if (siteLoaded.ExpectedSiteToLoad.IsRegex.Value
                                            ? !Regex.IsMatch(siteLoadedAction.Address,
                                                siteLoaded.ExpectedSiteToLoad.Value.Value)
                                            : siteLoaded.ExpectedSiteToLoad.Value.Value != siteLoadedAction.Address)
                                            action.Successful = false;
                                        else
                                        {
                                            siteLoaded.SiteLoadedUrl = siteLoadedAction.Address;
                                            siteLoaded.ReturnedOutput.Add(
                                                new KeyValuePairEx<string, string>(
                                                    SiteLoaded.KeyList.SiteLoadedUrl.ToString(),
                                                    siteLoadedAction.Address));
                                            action.Successful = true;
                                            siteLoaded.Successful = true;
                                            siteLoaded.Completed = true;
                                            break;
                                        }
                                }
                            }
                        }
                        else if (action.ActionObject.GetType() == typeof(FrameLoaded))
                            //https://developer.mozilla.org/en-US/docs/Web/Events/load
                        {
                            List<object> framesToLoad = WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            foreach (Object o in framesToLoad)
                            {
                                FrameLoaded frame = (FrameLoaded) o;
                                bool brokeTimeout = false;
                                while (!brokeTimeout)
                                {
                                    GatewayAction.FrameLoaded frameLoaded = new GatewayAction.FrameLoaded()
                                    {
                                        FrameName =
                                            action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(
                                                frame.ExpectedFrameName),
                                        Timeout = frame.Timeout,
                                    };
                                    _gateway.AddGatewayAction(frameLoaded);
                                    while (!frameLoaded.Completed)
                                    {
                                        if (ShouldBreakDueToTimeout(frame))
                                        {
                                            brokeTimeout = true;
                                            break;
                                        }
                                        Thread.Sleep(_threadSleepTime);
                                    }
                                    if (frameLoaded.Success)
                                    {
                                        frame.Successful = true;
                                        break;
                                    }
                                }
                                frame.Completed = true;
                                action.Successful = true;
                            }
                        }
                        else if (action.ActionObject.GetType() == typeof(HasStyleSetTo) ||
                                 action.ActionObject.GetType() == typeof(HasAttributeSetTo))
                        {
                            List<object> attributesSetTo =
                                WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            foreach (object o in attributesSetTo)
                            {
                                HasAttributeSetTo element = (HasAttributeSetTo) o;
                                bool found = false;
                                bool brokeTimeout = false;
                                while (!found && !brokeTimeout)
                                {
                                    found = true;
                                    if (ShouldBreakDueToTimeout(element))
                                    {
                                        brokeTimeout = true;
                                        break;
                                    }
                                    KeyValuePairEx<int, string> numberOfElements =
                                        FindElements(element.Selector, element.Timeout);
                                    for (int i = 0; i < numberOfElements.Key; i++)
                                    {
                                        string script = BuildExecuteOnSelector(element.Selector.SelectorString,
                                                            element.Selector.SelectorExecuteActionOn, i + 1, true) +
                                                        (action.ActionObject.GetType() ==
                                                         typeof(HasAttributeSetTo)
                                                            ? ".getAttribute('" +
                                                              element.AttributeName.Value + "')"
                                                            : ".style" + element.AttributeName.Value);
                                        script = action.TranslatePlaceholderStringToSingleString(script);
                                        GatewayAction.EvaluateJavascript evaluateJavascript = new GatewayAction.
                                            EvaluateJavascript()
                                            {
                                                Timeout = element.Timeout,
                                                Script = script,
                                                FrameName = action.ActionFrameName,
                                            };
                                        _gateway.AddGatewayAction(evaluateJavascript);
                                        while (!evaluateJavascript.Completed)
                                        {
                                            if (ShouldBreakDueToTimeout(element))
                                            {
                                                brokeTimeout = true;
                                                break;
                                            }
                                            Thread.Sleep(_threadSleepTime);

                                        }
                                        if (evaluateJavascript.Response.Success)
                                        {
                                            if (evaluateJavascript.Response.Result != null)
                                            {
                                                element.ExpectedValue =
                                                    action.TranslatePlaceholderStringOrRegexToSingleStringOrRegex(
                                                        element.ExpectedValue);
                                                if (element.ExpectedValue.IsRegex.Value
                                                    ? Regex.IsMatch(evaluateJavascript.Response.Result.ToString(),
                                                        element.ExpectedValue.Value.Value)
                                                    : evaluateJavascript.Response.Result.ToString() ==
                                                      element.ExpectedValue.Value.Value)
                                                {
                                                    element.Successful = true;
                                                    element.Completed = true;
                                                    continue;
                                                }
                                                found = false;
                                            }
                                            //ELSE RESULT IS NULL!
                                        }
                                        //ELSE RESULT HAS ERROR MESSAGE IN RESPONSE!
                                    }
                                    if (numberOfElements.Key == 0)
                                        found = false;
                                }
                                action.Successful = found;
                            }
                        }
                        else if (action.ActionObject.GetType() == typeof(InvokeSubmit) ||
                                 action.ActionObject.GetType() == typeof(JavascriptToExecute))
                        {
                            var js = WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);

                            foreach (var preElement in js)
                            {
                                var element = (JavascriptToExecute) preElement;
                                element.Javascript =
                                    new InsecureText(
                                        action.TranslatePlaceholderStringToSingleString(element.Javascript.Value));
                                element.Selector.SelectorString =
                                    action.TranslatePlaceholderStringToSingleString(element.Selector.SelectorString);
                                bool brokeTimeout = false;
                                while (!brokeTimeout)
                                {
                                    if (ShouldBreakDueToTimeout(element))
                                    {
                                        brokeTimeout = true;
                                        break;
                                    }
                                    KeyValuePairEx<int, string> count = element.Selector != null
                                        ? FindElements(element.Selector, element.Timeout)
                                        : new KeyValuePairEx<int, string>(1, "Pass");
                                    if (count.Key == 0)
                                        break;
                                    if (count.Value == "Pass" ||
                                        count.Key == element.Selector.ExpectedNumberOfElements.Value ||
                                        count.Key > 0 && element.Selector.ExpectedNumberOfElements.Value == 0)
                                    {
                                        for (int i = 0; i < count.Key; i++)
                                        {
                                            string script = (element.Selector != null
                                                                ? BuildExecuteOnSelector(
                                                                    element.Selector.SelectorString,
                                                                    element.Selector.SelectorExecuteActionOn,
                                                                    i + 1, true)
                                                                : "") + element.Javascript.Value;
                                            GatewayAction.EvaluateJavascript evaluateJavascript = new GatewayAction
                                                .EvaluateJavascript()
                                                {
                                                    Timeout = element.Timeout,
                                                    Script = script,
                                                    FrameName = action.ActionFrameName,
                                                };
                                            _gateway.AddGatewayAction(evaluateJavascript);
                                            while (!evaluateJavascript.Completed)
                                                Thread.Sleep(_threadSleepTime);
                                            if (evaluateJavascript.Response.Result == null)
                                            {
                                                evaluateJavascript.Response.Result = "Outcome: " +
                                                                                     evaluateJavascript.Response
                                                                                         .Success;
                                                evaluateJavascript.Response.Message = "Outcome: " +
                                                                                      evaluateJavascript
                                                                                          .Response
                                                                                          .Success;
                                            }
                                            if (i == 0)
                                            {
                                                element.ExecutionResult =
                                                    evaluateJavascript.Response.Result.ToString();
                                                element.ReturnedOutput.Add(
                                                    new KeyValuePairEx<string, string>(
                                                        JavascriptToExecute.KeyList.ExecutionResult.ToString(),
                                                        evaluateJavascript.Response.Result.ToString()));
                                                element.ReturnedOutput.Add(
                                                    new KeyValuePairEx<string, string>(
                                                        JavascriptToExecute.KeyList.ExecutedJavascript.ToString(),
                                                        script));
                                                element.Successful = evaluateJavascript.Response.Success;
                                                element.Completed = true;
                                                element.ExecutedJavascript = script;
                                            }
                                            else
                                            {
                                                var newElement = new JavascriptToExecute()
                                                {
                                                    Selector = element.Selector,
                                                    Javascript = element.Javascript,
                                                    Timeout = element.Timeout
                                                };
                                                newElement.ExecutionResult = evaluateJavascript.Response.Message;
                                                newElement.ReturnedOutput.Add(
                                                    new KeyValuePairEx<string, string>(
                                                        JavascriptToExecute.KeyList.ExecutionResult.ToString(),
                                                        evaluateJavascript.Response.Result.ToString()));
                                                newElement.ReturnedOutput.Add(
                                                   new KeyValuePairEx<string, string>(
                                                       JavascriptToExecute.KeyList.ExecutedJavascript.ToString(),
                                                       script));
                                                newElement.Successful = evaluateJavascript.Response.Success;
                                                newElement.Completed = true;
                                                newElement.ExecutedJavascript = script;

                                                element.SetNextJavascriptToExecute(newElement);
                                            }
                                            var text = script + Environment.NewLine + Environment.NewLine +
                                                       "JS exec result: " +
                                                       (evaluateJavascript.Response.Success
                                                           ? "Success: "
                                                           : "Failure: ") + evaluateJavascript.Response.Result +
                                                       evaluateJavascript.Response.Message;
                                            action.Successful = evaluateJavascript.Response.Success;
                                        }
                                    }
                                    else
                                    {
                                        //Not Executing because Number of found js elements do not match!
                                        action.Successful = false;
                                    }
                                    break;
                                }
                            }
                        }
                        else if (action.ActionObject.GetType() == typeof(ReturnNode))
                            // https://github.com/cefsharp/CefSharp/issues/830
                        {
                            var js = WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            bool success = true;
                            foreach (var preElement in js)
                            {
                                var element = (ReturnNode) preElement;
                                element.Selector.SelectorString =
                                    action.TranslatePlaceholderStringToSingleString(element.Selector.SelectorString);
                                bool brokeTimeout = false;
                                while (!brokeTimeout)
                                {
                                    if (ShouldBreakDueToTimeout(element))
                                    {
                                        brokeTimeout = true;
                                        break;
                                    }
                                    KeyValuePairEx<int, string> count = element.Selector != null
                                        ? FindElements(element.Selector, element.Timeout)
                                        : new KeyValuePairEx<int, string>(1, "Pass");
                                    if (count.Key == 0)
                                        break;
                                    if (count.Value == "Pass" ||
                                        count.Key == element.Selector.ExpectedNumberOfElements.Value ||
                                        count.Key > 0 && element.Selector.ExpectedNumberOfElements.Value == 0)
                                    {
                                        for (int i = 0; i < count.Key; i++)
                                        {
                                            string script =
                                                "(function(){ return new XMLSerializer().serializeToString(" +
                                                (element.Selector != null
                                                    ? BuildExecuteOnSelector(element.Selector.SelectorString,
                                                        element.Selector.SelectorExecuteActionOn,
                                                        i + 1, true)
                                                    : "") + ");})();";
                                            GatewayAction.EvaluateJavascript evaluateJavascript = new GatewayAction
                                                .EvaluateJavascript()
                                                {
                                                    Timeout = element.Timeout,
                                                    Script = script,
                                                    FrameName = action.ActionFrameName,
                                                };
                                            _gateway.AddGatewayAction(evaluateJavascript);
                                            while (!evaluateJavascript.Completed)
                                                Thread.Sleep(_threadSleepTime);
                                            if (evaluateJavascript.Response.Success)
                                            {
                                                if (i == 0)
                                                    element.SetSerializedNode(
                                                        evaluateJavascript.Response.Message);
                                                else
                                                {
                                                    var newElement = new ReturnNode()
                                                    {
                                                        Selector = element.Selector,
                                                        Timeout = element.Timeout
                                                    };
                                                    newElement.SetSerializedNode(
                                                        evaluateJavascript.Response.Result.ToString());
                                                    newElement.ReturnedOutput.Add(
                                                        new KeyValuePairEx<string, string>(
                                                            ReturnNode.KeyList.SerializedNode.ToString(),
                                                            evaluateJavascript.Response.Result.ToString()));
                                                    element.SetNextReturnNode(newElement);
                                                }
                                            }
                                            element.Successful = evaluateJavascript.Response.Success;
                                            element.Completed = true;
                                            if (success == true && element.Successful == false)
                                                success = false;
                                        }

                                    }
                                    else
                                    {
                                        //HASNT RETURNED ANY RESULT ON SELECTOR STRING, because number of found elements did not match
                                    }
                                    break;
                                }
                            }
                            action.Successful = success;
                        }
                        else if (action.ActionObject.GetType() == typeof(SetStyle) ||
                                 action.ActionObject.GetType() == typeof(SetAttribute))
                        {
                            List<object> objects = WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            bool success = true;
                            foreach (object o in objects)
                            {
                                bool found = false;
                                bool brokeTimeout = false;
                                while (!found && !brokeTimeout)
                                {
                                    found = true;
                                    SetAttribute element = (SetAttribute) o;
                                    element.AttributeName =
                                        new InsecureText(
                                            action.TranslatePlaceholderStringToSingleString(element.AttributeName.Value));
                                    element.ValueToSet = new InsecureText(
                                        action.TranslatePlaceholderStringToSingleString(element.ValueToSet.Value));
                                    element.ValueToSet.Value = EscapeJavascript(element.ValueToSet.Value);
                                    if (ShouldBreakDueToTimeout(element))
                                    {
                                        brokeTimeout = true;
                                        break;
                                    }
                                    KeyValuePairEx<int, string> numberOfElements =
                                        FindElements(element.Selector, element.Timeout);
                                    for (int i = 0; i < numberOfElements.Key; i++)
                                    {
                                        string script = BuildExecuteOnSelector(element.Selector.SelectorString,
                                                            element.Selector.SelectorExecuteActionOn, i + 1, true) +
                                                        (action.ActionObject.GetType() ==
                                                         typeof(SetAttribute)
                                                            ? ".setAttribute('" + element.AttributeName.Value + "', '" +
                                                              element.ValueToSet.Value +
                                                              "')"
                                                            : ".style" + element.AttributeName.Value + " = '" +
                                                              element.ValueToSet.Value + "'");
                                        GatewayAction.EvaluateJavascript evaluateJavascript = new GatewayAction.
                                            EvaluateJavascript()
                                            {
                                                Timeout = element.Timeout,
                                                Script = script,
                                                FrameName = action.ActionFrameName,
                                            };
                                        _gateway.AddGatewayAction(evaluateJavascript);
                                        while (!evaluateJavascript.Completed)
                                            Thread.Sleep(_threadSleepTime);
                                        if (evaluateJavascript.Response.Success)
                                        {
                                            element.Successful = true;
                                        }
                                        else
                                        {
                                            if (success == true)
                                                success = false;
                                            found = false;
                                        }
                                        element.Completed = true;
                                    }
                                }
                            }
                            action.Successful = success;
                        }
                        else if (action.ActionObject.GetType() == typeof(GetStyle) ||
                                 action.ActionObject.GetType() == typeof(GetAttribute))
                        {
                            List<object> getAttribute = WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            foreach (object o in getAttribute)
                            {
                                GetAttribute element = (GetAttribute) o;
                                element.AttributeName =
                                    new InsecureText(
                                        action.TranslatePlaceholderStringToSingleString(element.AttributeName.Value));
                                element.Selector.SelectorString =
                                    action.TranslatePlaceholderStringToSingleString(element.Selector.SelectorString);
                                bool found = false;
                                bool brokeTimeout = false;
                                while (!found && !brokeTimeout)
                                {
                                    found = true;
                                    if (ShouldBreakDueToTimeout(element))
                                    {
                                        brokeTimeout = true;
                                        break;
                                    }
                                    KeyValuePairEx<int, string> numberOfElements = FindElements(element.Selector,
                                        element.Timeout);
                                    for (int i = 0; i < numberOfElements.Key; i++)
                                    {
                                        GatewayAction.EvaluateJavascript evaluateJavascript = GetAttribute(element,
                                            action.ActionFrameName, action.ActionObject.GetType(), i + 1);

                                        if (evaluateJavascript.Response.Success)
                                        {
                                            element.Value = evaluateJavascript.Response.Message;
                                            element.ReturnedOutput.Add(
                                                new KeyValuePairEx<string, string>(
                                                    CefBrowserControl.BrowserActions.Elements.GetAttribute.KeyList
                                                        .Value.ToString(), evaluateJavascript.Response.Message));
                                            element.Successful = true;
                                        }
                                        else
                                        {
                                            //NOT SUCCESSFUL! ERROR MESSAGE IN RESONSE MESSAGE
                                        }
                                        element.Completed = true;
                                    }
                                    if (numberOfElements.Key == 0)
                                        found = false;
                                }
                                action.Successful = found;
                            }
                        }
                        else if (action.ActionObject.GetType() == typeof(InvokeMouseClick) ||
                                 action.ActionObject.GetType() == typeof(EventToTrigger))
                        {
                            bool success = true;
                            List<object> events = WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            foreach (object o in events)
                            {
                                EventToTrigger element = (EventToTrigger) o;
                                element.EventScriptBlock = new InsecureText(
                                    action.TranslatePlaceholderStringToSingleString(element.EventScriptBlock.Value));
                                element.Selector.SelectorString =
                                    action.TranslatePlaceholderStringToSingleString(element.Selector.SelectorString);
                                bool found = false;
                                bool brokeTimeout = false;
                                while (!found && !brokeTimeout)
                                {
                                    found = true;
                                    if (ShouldBreakDueToTimeout(element))
                                    {
                                        brokeTimeout = true;
                                        break;
                                    }
                                    KeyValuePairEx<int, string> numberOfElements = FindElements(element.Selector,
                                        element.Timeout);
                                    for (int i = 0; i < numberOfElements.Key; i++)
                                    {
                                        string script = @"(function(){var event = " + element.EventScriptBlock.Value + @"
        var cb = " + BuildExecuteOnSelector(element.Selector.SelectorString,
                                                            element.Selector.SelectorExecuteActionOn, i + 1, true) +
                                                        @";
        var cancelled = ! cb.dispatchEvent(event);
        if(cancelled)
            return 'Got canceled';
        else
            return 'Success';
        })();
        ";
                                        GatewayAction.EvaluateJavascript evaluateJavascript = new GatewayAction.
                                            EvaluateJavascript()
                                            {
                                                Timeout = element.Timeout,
                                                Script = script,
                                                FrameName = action.ActionFrameName,
                                            };
                                        _gateway.AddGatewayAction(evaluateJavascript);
                                        while (!evaluateJavascript.Completed)
                                            Thread.Sleep(_threadSleepTime);
                                        element.Successful = evaluateJavascript.Response.Success;
                                        element.Result = evaluateJavascript.Response.Message;
                                        element.ReturnedOutput.Add(
                                            new KeyValuePairEx<string, string>(
                                                EventToTrigger.KeyList.Result.ToString(),
                                                evaluateJavascript.Response.Message));
                                        if (success == true && evaluateJavascript.Response.Success == false)
                                            success = false;
                                        element.Completed = true;
                                    }
                                    if (numberOfElements.Key == 0)
                                        found = false;
                                }
                            }
                            action.Successful = success;
                        }
                        else if (action.ActionObject.GetType() == typeof(TextToTypeIn))
                        {


                            //Browser.GetBrowser().GetHost().SendMouseClickEvent();
                        }
                        else if (action.ActionObject.GetType() == typeof(ElementToClickOn))
                        {
                            #region click

                            //TODO check if is in frame and then calculate new position
                            //TODO scroll if needed
                            List<object> events = WebBrowserEx.ConvertObjectToObjectList(action.ActionObject);
                            foreach (object o in events)
                            {
                                ElementToClickOn element = (ElementToClickOn) o;
                                element.Selector.SelectorString =
                                    action.TranslatePlaceholderStringToSingleString(element.Selector.SelectorString);
                                KeyValuePairEx<int, string> numberOfElements = FindElements(element.Selector,
                                    element.Timeout);
                                for (int i = 0; i < numberOfElements.Key; i++)
                                {
                                    Selector evaluationSelector = new Selector()
                                    {
                                        SelectorString = element.Selector.SelectorString,
                                        SelectorExecuteActionOn = element.Selector.SelectorExecuteActionOn,
                                        ExpectedNumberOfElements = new InsecureInt(i + 1),
                                    };
                                    string evaluationScript = GenerateElementExistString(evaluationSelector,
                                        true);
                                    GatewayAction.FindElementsFrame findFrame = new GatewayAction.
                                        FindElementsFrame()
                                        {
                                            EvaluationScript = evaluationScript,
                                            Timeout = element.Timeout,
                                        };
                                    _gateway.AddGatewayAction(findFrame);
                                    while (!findFrame.Completed)
                                    {
                                        if (ShouldBreakDueToTimeout(element))
                                        {
                                            break;
                                        }
                                        Thread.Sleep(_threadSleepTime);

                                    }
                                    if (!findFrame.Success)
                                        throw new CannotFindSingleElementsFrame("Script " + evaluationScript +
                                                                                " funktioniert nicht.");
                                    GatewayAction.GetParentFrames parentFrames = new GatewayAction.
                                        GetParentFrames()
                                        {
                                            StartingFrame = new StringOrRegex()
                                            {
                                                Value = new InsecureText(findFrame.FrameName),
                                            }
                                        };
                                    _gateway.AddGatewayAction(parentFrames);
                                    while (!parentFrames.Completed)
                                    {
                                        if (ShouldBreakDueToTimeout(element))
                                        {
                                            break;
                                        }
                                        Thread.Sleep(_threadSleepTime);
                                    }
                                    //Going from the inner side to the outer side
                                    for (int index = 0; index < parentFrames.RecursiveFrameList.Count - 1; index++)
                                    {
                                        FrameDetails frameDetail = parentFrames.RecursiveFrameList[index];
                                        KeyValuePairEx<int, string> numberOfElementsFrame =
                                            FindElements(
                                                new Selector()
                                                {
                                                    SelectorString = "iframe",
                                                    SelectorExecuteActionOn =
                                                        CefBrowserControl.BrowserAction.ExecuteActionOn.TagName
                                                },
                                                element.Timeout,
                                                parentFrames.RecursiveFrameList[index + 1].FrameName);
                                        for (int i2 = 0; i2 < numberOfElementsFrame.Key; i2++)
                                        {
                                            GetAttribute frameElement = new GetAttribute()
                                            {
                                                Selector = new Selector()
                                                {
                                                    ExpectedNumberOfElements = new InsecureInt(i2 + 1),
                                                    SelectorString = "iframe",
                                                    SelectorExecuteActionOn =
                                                        CefBrowserControl.BrowserAction.ExecuteActionOn.TagName,
                                                },
                                                AttributeName = new InsecureText("src"),
                                            };
                                            GatewayAction.EvaluateJavascript evaluateJavascript2 =
                                                GetAttribute(frameElement,
                                                    new StringOrRegex()
                                                    {
                                                        Value =
                                                            new InsecureText(
                                                                parentFrames.RecursiveFrameList[index + 1].FrameName)
                                                    },
                                                    action.ActionObject.GetType(), i2 + 1);

                                            if (evaluateJavascript2.Response.Success)
                                            {
                                                if (Regex.IsMatch(frameDetail.Url,
                                                    (string) evaluateJavascript2.Response.Result))
                                                    frameDetail.SrcAttributeNr = i2;
                                            }
                                            else
                                            {
                                                throw new Exception("fuck");
                                            }
                                        }
                                    }


                                    if (parentFrames.RecursiveFrameList.Count == 0)
                                        throw new CannotFindRecursiveFrameList();

                                    ObjectLocation location =
                                        GetObjectsLocation(
                                            new StringOrRegex() {Value = new InsecureText(findFrame.FrameName)},
                                            element.Selector, timeout: element.Timeout);

                                    //int increasingCoordinatesX = 0, increasingCoordinatesY = 0;
                                    /*
                                     * //Position parameters used for drawing the rectangle
var x = 210;
var y = 210;
var width = 10;
var height = 10;

var canvas = document.createElement('canvas'); //Create a canvas element
//Set canvas width/height
canvas.style.width='100%';
canvas.style.height='100%';
//Set canvas drawing area width/height
canvas.width = window.innerWidth;
canvas.height = window.innerHeight;
//Position canvas
canvas.style.position='absolute';
canvas.style.left=0;
canvas.style.top=0;
canvas.style.zIndex=100000;
canvas.style.pointerEvents='none'; //Make sure you can click 'through' the canvas
document.body.appendChild(canvas); //Append canvas to body element
var context = canvas.getContext('2d');
//Draw rectangle
context.rect(x, y, width, height);
context.fillStyle = 'red';
context.fill();
                                     * */

                                    //from inner to outer, setting locations
                                    for (int index = 0; index < parentFrames.RecursiveFrameList.Count; index++)
                                    {
                                        FrameDetails details = parentFrames.RecursiveFrameList[index];

                                        FrameDetails currentFrameDimensions =
                                            GetFrameDimensions(
                                                new StringOrRegex() {Value = new InsecureText(details.FrameName)},
                                                element.Timeout);

                                        KeyValuePairEx<int, int> currentScrollPosition =
                                            GetScrollPosition(
                                                new StringOrRegex() {Value = new InsecureText(details.FrameName)},
                                                element.Timeout);
                                        details.ScrollOffsetX = currentScrollPosition.Key;
                                        details.ScrollOffsetY = currentScrollPosition.Value;
                                        details.ClientWidth = currentFrameDimensions.ClientWidth;
                                        details.ClientHeight = currentFrameDimensions.ClientHeight;
                                        details.ScrollWidth = currentFrameDimensions.ScrollWidth;
                                        details.ScrollHeight = currentFrameDimensions.ScrollHeight;
                                        if (index > 0)
                                        {
                                            Selector frameSelector = new Selector()
                                            {
                                                SelectorString =
                                                    "iframe",
                                                ExpectedNumberOfElements = new InsecureInt(
                                                    parentFrames.RecursiveFrameList[index - 1].SrcAttributeNr + 1),
                                                SelectorExecuteActionOn =
                                                    CefBrowserControl.BrowserAction.ExecuteActionOn.TagName,
                                            };
                                            details.NextTargetLocation = GetObjectsLocation(
                                                new StringOrRegex()
                                                {
                                                    Value =
                                                        new InsecureText(
                                                            parentFrames.RecursiveFrameList[index].FrameName)
                                                }, frameSelector);
                                            parentFrames.RecursiveFrameList[index - 1].FrameLocation =
                                                details.NextTargetLocation;
                                            details.FrameLocation = new ObjectLocation()
                                            {
                                                X = 0,
                                                Y = 0,
                                            };
                                            //TODO:Make central part here
                                            //TODO: Take outer width
                                            //details.NextTargetLocation.X += details.NextTargetLocation.DocumentOffsetWidth / 2;
                                            //details.NextTargetLocation.Y += details.NextTargetLocation.DocumentOffsetHeight / 2;
                                        }
                                        else
                                        {
                                            details.NextTargetLocation = location;

                                        }


                                        //muss noch frames scrollen, am besten in die mitte,

                                        //TODO: add centration technique
                                    }
                                    //Maximized and normal viewing rectange are not the same
                                    for (int index = 0; index < parentFrames.RecursiveFrameList.Count; index++)
                                    {
                                        FrameDetails details = parentFrames.RecursiveFrameList[index];
                                        var rect = index > 0
                                            ? new Rectangle(
                                                new Point(
                                                    details.NextTargetLocation.X +
                                                    parentFrames.RecursiveFrameList[index].ClientWidth / 2,
                                                    details.NextTargetLocation.Y +
                                                    parentFrames.RecursiveFrameList[index].ClientHeight / 2),
                                                details.ClientWidth,
                                                details.ClientHeight)
                                            : new Rectangle(
                                                new Point(location.X,
                                                    location.Y),
                                                details.ClientWidth,
                                                details.ClientHeight);
                                        if (rect.LeftTop.X < 0)
                                        {
                                            int deltaX = -rect.LeftTop.X;
                                            rect.Move(deltaX);
                                        }
                                        if (rect.LeftTop.Y < 0)
                                        {
                                            int deltaY = -rect.LeftTop.Y;
                                            rect.Move(0, deltaY);
                                        }
                                        if (rect.RightTop.X > details.ScrollWidth)
                                        {
                                            int deltaX = -(rect.RightTop.X - details.ScrollWidth);
                                            rect.Move(deltaX);
                                        }
                                        if (rect.LeftBottom.Y > details.ScrollHeight)
                                        {
                                            int deltaY = -(rect.LeftBottom.Y - details.ScrollHeight);
                                            rect.Move(0, deltaY);
                                        }
                                        details.ViewingRectangle = rect;
                                    }

                                    Random random = new Random(Guid.NewGuid().GetHashCode());
                                    //TODO: was gemacht wird, wenn parent iframe kleiner child frame ist
                                    int incrX = 0, incrY = 0;

                                    //Going from outer to inner
                                    for (int index = parentFrames.RecursiveFrameList.Count - 1;
                                        index >= 0;
                                        index--)
                                    {
                                        double multiplicatorX = 1, multiplicatorY = 1;
                                        int scrolledX = 0, scrolledY = 0;
                                        while (true)
                                        {
                                            //if (index <= parentFrames.RecursiveFrameList.Count - 2)
                                            //{
                                            //    for (int recIndex = index + 1;
                                            //        recIndex < parentFrames.RecursiveFrameList.Count;
                                            //        recIndex++)
                                            //    {
                                            //        FrameDetails recDetails = parentFrames.RecursiveFrameList[recIndex];
                                            //        incrX += (int)recDetails.ViewingRectangle.LeftTop.X - (index == 0 ? 0 : recDetails.ClientWidth / -
                                            //                    recDetails.MousheWheelAction.DeltaWheelAction.Key);
                                            //        incrY += (int)recDetails.ViewingRectangle.LeftTop.Y - (index == 0 ? 0 : recDetails.ClientHeight / 2 -
                                            //                    recDetails.MousheWheelAction.DeltaWheelAction.ExpectedValue);
                                            //    }
                                            //}

                                            //Genaues scrollen in Angriff nehmen
                                            FrameDetails details = parentFrames.RecursiveFrameList[index];

                                            int scrollX = (int) details.ViewingRectangle.LeftTop.X,
                                                scrollY = (int) details.ViewingRectangle.LeftTop.Y;
                                            if (scrollX > details.ScrollWidth - details.ClientWidth)
                                                scrollX = details.ScrollWidth - details.ClientWidth;
                                            scrollX -= details.ScrollOffsetX;
                                            scrollX = (int) (scrollX * multiplicatorX) - scrolledX;
                                            if (scrollY > details.ScrollHeight - details.ClientHeight)
                                                scrollY = details.ScrollHeight - details.ClientHeight;
                                            scrollY -= details.ScrollOffsetY;
                                            scrollY = (int) (scrollY * multiplicatorX) - scrolledY;


                                            KeyValuePairEx<int, int> deltaMouseWheel =
                                                new KeyValuePairEx<int, int>(scrollX,
                                                    scrollY);

                                            //    KeyValuePairEx<int, int> newMouseWheel = new KeyValuePairEx<int, int>(scrollX, scrollY);

                                            //KeyValuePairEx<int, int> deltaMouseWheel = new KeyValuePairEx<int, int>(
                                            //    ((int) details.ViewingRectangle.LeftTop.X - details.ScrollOffsetX) >
                                            //    (details.ScrollWidth - details.ClientWidth)
                                            //        ? (details.ScrollWidth - details.ClientWidth)
                                            //        : (int) details.ViewingRectangle.LeftTop.X - details.ScrollOffsetX,
                                            //    ((int) details.ViewingRectangle.LeftTop.Y - details.ScrollOffsetY) >
                                            //    (details.ScrollHeight - details.ClientHeight)
                                            //        ? details.ScrollHeight - details.ClientHeight
                                            //        : (int) details.ViewingRectangle.LeftTop.Y - details.ScrollOffsetY);
                                            int offset = 2;
                                            int wheelLocX = (int) details.FrameLocation.X + offset - incrX,
                                                wheelLocY = (int) details.FrameLocation.Y + offset - incrY;
                                            if (wheelLocX < offset)
                                                wheelLocX = offset;
                                            if (wheelLocY < offset)
                                                wheelLocY = offset;
                                            GatewayAction.SendMouseWheel mouseWheelX = new GatewayAction.
                                                SendMouseWheel()
                                                {
                                                    //TODO: Problem, what if already scrolled over element/iframe?
                                                    //TODO: Problem with increasing when already over
                                                    WhellLocation =
                                                        new Point(wheelLocX, wheelLocY),
                                                    //new Point(
                                                    //            random.Next((int)details.ViewingRectangle.LeftTop.X, (int) details.ViewingRectangle.RightTop.X),
                                                    //            random.Next((int)details.ViewingRectangle.LeftTop.Y, (int)details.ViewingRectangle.LeftBottom.Y)),
                                                    DeltaWheelAction =
                                                        new KeyValuePairEx<int, int>(deltaMouseWheel.Key,
                                                            0),

                                                    FrameName =
                                                        new StringOrRegex()
                                                        {
                                                            Value = new InsecureText(details.FrameName)
                                                        },
                                                };
                                            GatewayAction.SendMouseWheel mouseWheelY = new GatewayAction.
                                                SendMouseWheel()
                                                {
                                                    //TODO: Problem, what if already scrolled over element/iframe?
                                                    //TODO: Problem with increasing when already over
                                                    WhellLocation =
                                                        new Point(wheelLocX, wheelLocY),
                                                    //new Point(
                                                    //            random.Next((int)details.ViewingRectangle.LeftTop.X, (int) details.ViewingRectangle.RightTop.X),
                                                    //            random.Next((int)details.ViewingRectangle.LeftTop.Y, (int)details.ViewingRectangle.LeftBottom.Y)),
                                                    DeltaWheelAction =
                                                        new KeyValuePairEx<int, int>(0, deltaMouseWheel.Value),

                                                    FrameName =
                                                        new StringOrRegex()
                                                        {
                                                            Value = new InsecureText(details.FrameName)
                                                        },
                                                };
                                            //increasingCoordinatesX += details.NextTargetLocation.X - details.ScrollOffsetX - (int) mouseWheel.WhellLocation.X;
                                            //increasingCoordinatesY += details.NextTargetLocation.Y - details.ScrollOffsetY - (int) mouseWheel.WhellLocation.Y;
                                            //TODO: Reactivate below comment!
                                            // details.MousheWheelAction = mouseWheelX;
                                            incrX += scrollX;
                                            incrY += scrollY;
                                            _gateway.AddGatewayAction(mouseWheelX);
                                            while (!mouseWheelX.Completed)
                                                Thread.Sleep(_threadSleepTime);
                                            Thread.Sleep(1000);
                                            _gateway.AddGatewayAction(mouseWheelY);
                                            while (!mouseWheelY.Completed)
                                                Thread.Sleep(_threadSleepTime);
                                            Thread.Sleep(1000);
                                            GatewayAction.EvaluateJavascript evaluateScrollPosition = new GatewayAction
                                                .
                                                EvaluateJavascript()
                                                {
                                                    FrameName = new StringOrRegex()
                                                    {
                                                        Value = new InsecureText(details.FrameName),
                                                    },
                                                    Script =
                                                        "(function(){return '' + document.body.scrollLeft + ' ' + document.body.scrollTop;})()",
                                                };
                                            _gateway.AddGatewayAction(evaluateScrollPosition);
                                            while (!evaluateScrollPosition.Completed)
                                                Thread.Sleep(100);
                                            string[] splittedValues =
                                                evaluateScrollPosition.Response.Result.ToString().Split(' ');
                                            scrolledX =
                                                Convert.ToInt32(Convert.ToDouble(splittedValues[0].Replace('.', ',')));
                                            scrolledY =
                                                Convert.ToInt32(Convert.ToDouble(splittedValues[1].Replace('.', ',')));
                                            multiplicatorX = 1 + ((Double) scrolledX / (Double) deltaMouseWheel.Key);
                                            if (multiplicatorX.Equals(2))
                                                multiplicatorX = 1;
                                            multiplicatorY = 1 +
                                                             ((Double) scrolledY / (Double) deltaMouseWheel.Value);
                                            if (multiplicatorY.Equals(2))
                                                multiplicatorY = 1;
                                            Thread.Sleep(1000);

                                            //ToDo last thing i did: decommented this and the maximized version had failed to locate the next frame
                                            //See you next time
                                            if (Double.IsNaN(multiplicatorX))
                                                break;
                                            if (Double.IsNaN(multiplicatorY))
                                                break;
                                            //GatewayAction.SendMouseWheel mouseWheelResetX = new GatewayAction.
                                            //    SendMouseWheel()
                                            //{
                                            //    //TODO: Problem, what if already scrolled over element/iframe?
                                            //    //TODO: Problem with increasing when already over
                                            //    WhellLocation =
                                            //            new Point(wheelLocX, wheelLocY),
                                            //    //new Point(
                                            //    //            random.Next((int)details.ViewingRectangle.LeftTop.X, (int) details.ViewingRectangle.RightTop.X),
                                            //    //            random.Next((int)details.ViewingRectangle.LeftTop.Y, (int)details.ViewingRectangle.LeftBottom.Y)),
                                            //    DeltaWheelAction =
                                            //            new KeyValuePairEx<int, int>(deltaMouseWheel.Key, 0),

                                            //    ExpectedFrameName = new StringOrRegex() { ExpectedValue = details.ExpectedFrameName },
                                            //};
                                            //_gateway.AddGatewayAction(mouseWheelResetX);
                                            //while (!mouseWheelResetX.Completed)
                                            //    Thread.Sleep(100);
                                            //GatewayAction.SendMouseWheel mouseWheelResetY = new GatewayAction.
                                            //    SendMouseWheel()
                                            //{
                                            //    //TODO: Problem, what if already scrolled over element/iframe?
                                            //    //TODO: Problem with increasing when already over
                                            //    WhellLocation =
                                            //            new Point(wheelLocX, wheelLocY),
                                            //    //new Point(
                                            //    //            random.Next((int)details.ViewingRectangle.LeftTop.X, (int) details.ViewingRectangle.RightTop.X),
                                            //    //            random.Next((int)details.ViewingRectangle.LeftTop.Y, (int)details.ViewingRectangle.LeftBottom.Y)),
                                            //    DeltaWheelAction =
                                            //            new KeyValuePairEx<int, int>(0, deltaMouseWheel.ExpectedValue),

                                            //    ExpectedFrameName = new StringOrRegex() { ExpectedValue = details.ExpectedFrameName },
                                            //};
                                            //_gateway.AddGatewayAction(mouseWheelResetY);
                                            //while (!mouseWheelResetY.Completed)
                                            //    Thread.Sleep(100);
                                            Thread.Sleep(1000);
                                        }
                                    }



                                    //TODO: Make this loop create a rectangel and focus on the clicking element
                                    //for (int index = parentFrames.RecursiveFrameList.Count - 1; i >= 0; i--)
                                    //{
                                    //    GatewayAction.SendMouseWheel mouseWheel = new GatewayAction.
                                    //       SendMouseWheel()
                                    //    {
                                    //        WhellLocation = new Point(increasingCoordinatesX, increasingCoordinatesY),
                                    //        DeltaWheelAction =
                                    //               new KeyValuePairEx<int, int>(newScrollingOffsetWidth,
                                    //                   newScrollingOffsetHeight),
                                    //        ExpectedFrameName = action.ActionFrameName,
                                    //    };
                                    //    _gateway.AddGatewayAction(mouseWheel);
                                    //    while (!mouseWheel.Completed)
                                    //}
                                }

                                //        KeyValuePairEx<int, int> currentScrollPosition = GetScrollPosition(action.ActionFrameName, element.Timeout);








                                //bool found = false;
                                ////while (!found)
                                //{
                                //    found = true;
                                //    if (ShouldBreakDueToTimeout(element, action.ResultsList,
                                //        element.Selector.SelectorString))
                                //        break;
                                //    for (int i = 0; i < numberOfElements.Key; i++)
                                //    {
                                //        Random random = new Random(Guid.NewGuid().GetHashCode());
                                //        NextTargetLocation location = GetObjectsLocation(action.ActionFrameName,
                                //            element.Selector, timeout: element.Timeout);
                                //        KeyValuePairEx<int, int> clickLocation = location.GetRandomLocation();
                                //        //Todo: remember current mouse position
                                //        KeyValuePairEx<int, int> currentScrollPosition = GetScrollPosition(action.ActionFrameName, element.Timeout);
                                //        int scrolledX = currentScrollPosition.Key,
                                //            scrolledY = currentScrollPosition.ExpectedValue;

                                //        GatewayAction.GetSize size = new GatewayAction.GetSize();
                                //        _gateway.AddGatewayAction(size);
                                //        while(!size.Completed)
                                //            Thread.Sleep(_threadSleepTime);
                                //        if (!size.Success)
                                //            throw new CannotGetBrowserSize();

                                //        int maxWidthUnits = (location.DocumentClientWidth / size.Width) - 1;
                                //        int width = size.Width;
                                //        int neededWidthUnits = (clickLocation.Key / size.Width) - 1;
                                //        int maxHeightUnits = (location.DocumentClientHeight / size.Height) - 1;
                                //        int height = size.Height;
                                //        int neededHeightUnits = (clickLocation.ExpectedValue / size.Height) - 1;

                                //        if (neededWidthUnits > maxWidthUnits || neededHeightUnits > maxHeightUnits)
                                //            throw new Exception("WTF?");
                                //        int newScrollingOffsetWidth = neededWidthUnits*width - scrolledX;
                                //        int newScrollingOffsetHeight = neededHeightUnits * height - scrolledY;
                                //        GatewayAction.SendMouseWheel mouseWheel = new GatewayAction.
                                //            SendMouseWheel()
                                //            {
                                //                WhellLocation = new Point(0, 0),
                                //                DeltaWheelAction =
                                //                    new KeyValuePairEx<int, int>(newScrollingOffsetWidth,
                                //                        newScrollingOffsetHeight),
                                //                ExpectedFrameName = action.ActionFrameName,
                                //            };
                                //        _gateway.AddGatewayAction(mouseWheel);
                                //        while (!mouseWheel.Completed) 
                                //            Thread.Sleep(_threadSleepTime);


                                //        //------------

                                //        //while (scrolledX < clickLocation.Key)
                                //        //{
                                //        //    scrolledX += size.Width;
                                //        //}
                                //        //scrolledX -= size.Width;

                                //        //_webBrowser.Browser.GetBrowser()
                                //        //    .GetHost()
                                //        //    .SendMouseWheelEvent(0, 0,
                                //        //        -scrolledX, 0, CefEventFlags.None);
                                //        //while (scrolledY < clickLocation.ExpectedValue)
                                //        //{
                                //        //    scrolledY += (int)_webBrowser.Browser.ActualHeight;
                                //        //}
                                //        //scrolledY -= (int)_webBrowser.Browser.ActualHeight;
                                //        //_webBrowser.Browser.GetBrowser()
                                //        //    .GetHost()
                                //        //    .SendMouseWheelEvent(0, 0,
                                //        //        0, -scrolledY, CefEventFlags.None);


                                //        //Browser.GetBrowser().GetHost().SendMouseWheelEvent(0, 0, -500, 0, CefEventFlags.None);
                                //        //Browser.GetBrowser().GetHost().SendMouseWheelEvent(0,0,0,-500,CefEventFlags.None);
                                //        //KeyValuePairEx<int, int> newClickLocation =
                                //        //    new KeyValuePairEx<int, int>(clickLocation.Key - scrolledX,
                                //        //        clickLocation.ExpectedValue - scrolledY);

                                //        //_webBrowser.Browser.GetBrowser()
                                //        //    .GetHost()
                                //        //    .SendMouseClickEvent(newClickLocation.Key, newClickLocation.ExpectedValue,
                                //        //        MouseButtonType.Left, false, element.DoubleClick ? 2 : 1,
                                //        //        CefEventFlags.None);
                                //        //Thread.Sleep(random.Next(200));
                                //        //_webBrowser.Browser.GetBrowser()
                                //        //    .GetHost()
                                //        //    .SendMouseClickEvent(newClickLocation.Key, newClickLocation.ExpectedValue,
                                //        //        MouseButtonType.Left, true, element.DoubleClick ? 2 : 1,
                                //        //        CefEventFlags.None);
                                //        //action.ResultsList.Add(
                                //        //    new KeyValuePairEx<BrowserAction.ActionState, string>(
                                //        //        BrowserAction.ActionState.Successfull,
                                //        //        "Clicked at X:" + newClickLocation.Key + " Y:" +
                                //        //        newClickLocation.ExpectedValue));
                                //        //Rectangle rect = new Rectangle()
                                //        //{
                                //        //    Width = 10,
                                //        //    Height = 10,
                                //        //    Fill = Brushes.Red,
                                //        //    Stroke = Brushes.DarkRed,

                                //        //};
                                //        //Canvas.SetLeft(rect, newClickLocation.Key);
                                //        //Canvas.SetTop(rect, newClickLocation.ExpectedValue);


                                //        //_webBrowser.testvas.Children.Add(rect);

                                //    }
                                //    if (numberOfElements.Key == 0)
                                //        found = false;
                                //}
                            }

                            #endregion
                        }
                        else if (action.ActionObject.GetType() == typeof(SecondsToWait))
                        {

                        }
                    }

                    foreach (var element in ((BaseObject) action.ActionObject).ReturnedOutput)
                    {
                        action.ReturnedOutput.Add(element);
                    }

                    //Was prior after exitwritelock!
                    action.SetFinished(true);

                    _actionListLockSlim.EnterWriteLock();
                    BrowserActionsCompleted.Enqueue(action);
                    _actionListLockSlim.ExitWriteLock();
                }

                #endregion
            }
        }

        private static string BuildExecuteOnSelector(string selectionString,
            CefBrowserControl.BrowserAction.ExecuteActionOn executeActionOn,
            int? exptectedElements = 1, bool selectLastElement = false)
        {
            if (exptectedElements == null)
                exptectedElements = 0;
            if (String.IsNullOrEmpty(selectionString))
                throw new NoSelectionStringSpecified("Please define a selection string!");
            switch (executeActionOn)
            {
                case CefBrowserControl.BrowserAction.ExecuteActionOn.Class:
                    return
                        $"document.getElementsByClassName('{selectionString}'){(exptectedElements == 1 || selectLastElement ? "[" + (selectLastElement ? (exptectedElements - 1).ToString() : "0") + "]" : "")}";
                case CefBrowserControl.BrowserAction.ExecuteActionOn.Id:
                    return $"document.getElementById('{selectionString}')";
                case CefBrowserControl.BrowserAction.ExecuteActionOn.Name:
                    return
                        $"document.getElementsByName('{selectionString}'){(exptectedElements == 1 || selectLastElement ? "[" + (selectLastElement ? (exptectedElements - 1).ToString() : "0") + "]" : "")}";
                case CefBrowserControl.BrowserAction.ExecuteActionOn.TagName:
                    return
                        $"document.getElementsByTagName('{selectionString}'){(exptectedElements == 1 || selectLastElement ? "[" + (selectLastElement ? (exptectedElements - 1).ToString() : "0") + "]" : "")}";
                case CefBrowserControl.BrowserAction.ExecuteActionOn.Xpath:
                {
                    string iterator = "";
                    for (int i = 0; i < exptectedElements - 1; i++)
                    {
                        iterator += "result.iterateNext(); ";
                    }
                    string script = $"(function(){{var result = document.evaluate('{EscapeJavascript(selectionString)}', document, null, XPathResult.ANY_TYPE, null ); " +
                        iterator + "return result.iterateNext();})()";
                    return script;
                }
            }
            throw new NoSelectionTypeSpecified("Please define a selection type");
        }

        private static bool ShouldBreakDueToTimeout(BaseObject baseObject)
        {
            if (baseObject.Timeout != null)
            {
                if (baseObject.FirstAccess == null)
                {
                    baseObject.FirstAccess = DateTime.Now;
                }
                if (DateTime.Now > baseObject.FirstAccess.Value + baseObject.Timeout.Value)
                {
                    baseObject.TimedOut = true;
                    return true;
                }
            }
            return false;
        }

        private FrameDetails GetFrameDimensions(StringOrRegex frame, TimeSpan? timeout = null)
        {
            string script = @"(function getDimensions() {
        	return [document.documentElement.clientWidth, document.documentElement.clientHeight, document.body.scrollWidth, document.body.scrollHeight];
        })();";
            while (true)
            {
                GatewayAction.EvaluateJavascript evaluateJavascript = new GatewayAction.EvaluateJavascript()
                {
                    Timeout = timeout,
                    FrameName = frame,
                    Script = script,
                };
                _gateway.AddGatewayAction(evaluateJavascript);
                while (!evaluateJavascript.Completed)
                    Thread.Sleep(_threadSleepTime);
                if (evaluateJavascript.Response.Success)
                {
                    List<object> arrayResult = (List<object>) evaluateJavascript.Response.Result;
                    return new FrameDetails()
                    {
                        ClientWidth = (int) arrayResult[0],
                        ClientHeight = (int) arrayResult[1],
                        ScrollWidth = (int) arrayResult[2],
                        ScrollHeight = (int) arrayResult[3],
                    };
                }
                throw new CannotGetObjectLocation(evaluateJavascript.Response.Message);
            }
        }

        private ObjectLocation GetObjectsLocation(StringOrRegex frameName, Selector selector, int numberOfElement = 1,
            TimeSpan? timeout = null)
        {
            //http://www.quirksmode.org/js/findpos.html

            string script = @"(function findPos() {
            var obj = " +
                            BuildExecuteOnSelector(selector.SelectorString, selector.SelectorExecuteActionOn,
                                numberOfElement, true) +
                            @";
        	var curleft = curtop = 0;
        	if (obj.offsetParent) {
        		do {
        			curleft += obj.offsetLeft;
        			curtop += obj.offsetTop;
        			} 
        		while (obj = obj.offsetParent);
        	}
            var width = " +
                            BuildExecuteOnSelector(selector.SelectorString, selector.SelectorExecuteActionOn,
                                numberOfElement, true) +
                            @".clientWidth;
            var height = " +
                            BuildExecuteOnSelector(selector.SelectorString, selector.SelectorExecuteActionOn,
                                numberOfElement, true) +
                            @".clientHeight;
        	return [curleft,curtop, width, height, document.documentElement.clientWidth, document.documentElement.clientHeight, document.body.scrollWidth, document.body.scrollHeight, document.documentElement.offsetWidth, document.documentElement.offsetHeight];
        })();";
            while (true)
            {
                GatewayAction.EvaluateJavascript evaluateJavascript = new GatewayAction.EvaluateJavascript()
                {
                    Timeout = timeout,
                    FrameName = frameName,
                    Script = script,
                };
                _gateway.AddGatewayAction(evaluateJavascript);
                while (!evaluateJavascript.Completed)
                    Thread.Sleep(_threadSleepTime);
                if (evaluateJavascript.Response.Success)
                {
                    List<object> arrayResult = (List<object>) evaluateJavascript.Response.Result;
                    return new ObjectLocation()
                    {
                        X = (int) arrayResult[0],
                        Y = (int) arrayResult[1],
                        Width = (int) arrayResult[2],
                        Height = (int) arrayResult[3],
                        DocumentClientWidth = (int) arrayResult[4],
                        DocumentClientHeight = (int) arrayResult[5],
                        DocumentScrollWidth = (int) arrayResult[6],
                        DocumentScrollHeight = (int) arrayResult[7],
                        DocumentOffsetWidth = (int)arrayResult[8],
                        DocumentOffsetHeight = (int)arrayResult[9],

                    };
                }
                throw new CannotGetObjectLocation(evaluateJavascript.Response.Message);
            }
        }

        public KeyValuePairEx<int, int> GetScrollPosition(StringOrRegex frameName, TimeSpan? timeout = null)
        {
            string script = "(function(){return [document.body.scrollLeft, document.body.scrollTop];})();";
            while (true)
            {
                GatewayAction.EvaluateJavascript javascript = new GatewayAction.EvaluateJavascript()
                {
                    Script = script,
                    Timeout = timeout,
                    FrameName = frameName,
                };
                _gateway.AddGatewayAction(javascript);
                while (!javascript.Completed)
                    Thread.Sleep(_threadSleepTime);
                if (javascript.Success)
                {
                    if (javascript.Response.Success)
                    {
                        List<object> arrayResult = (List<object>) javascript.Response.Result;
                        return new KeyValuePairEx<int, int>((int) arrayResult[0], (int) arrayResult[1]);
                    }
                }
                throw new CannotGetScrollOffset(javascript.Response.Message);
            }
        }

        public KeyValuePairEx<int, string> FindElements(Selector selector, TimeSpan? timeout, String frameName = null)
        {
            Dictionary<string, bool> frames = new Dictionary<string, bool>();
            if (frameName != null)
                frames.Add(frameName, true);
            else
            {
                GatewayAction.GetFrameNames frameNames = new GatewayAction.GetFrameNames();
                _gateway.AddGatewayAction(frameNames);
                while (!frameNames.Completed)
                    Thread.Sleep(_threadSleepTime);
                frames = frameNames.Frames;
            }
            int numberOfElements = 0;

            foreach (KeyValuePair<string, bool> framename in frames)
            {
                while (true)
                {
                    //IFrame frame = _webBrowser.GetAccordingFrame(framename);
                    string multipleFilter = selector.SelectorExecuteActionOn == CefBrowserControl.BrowserAction.ExecuteActionOn.Id
                        ? "document.getElementById('" + selector.SelectorString + "') != null ? 1 : 0"
                        : selector.SelectorExecuteActionOn ==
                          CefBrowserControl.BrowserAction.ExecuteActionOn.Xpath
                            ? "document.evaluate(\"count(" + EscapeJavascript(selector.SelectorString) +
                              ")\", document, null, XPathResult.ANY_TYPE, null )['numberValue'];"
                            : BuildExecuteOnSelector(selector.SelectorString,
                                  selector.SelectorExecuteActionOn, 2) + ".length";
                    string checkScript =
                        $"(function(){{return {multipleFilter} }})();";
                    GatewayAction.EvaluateJavascript findJavascript = new GatewayAction.EvaluateJavascript()
                    {
                        FrameName = new StringOrRegex() {Value = new InsecureText(framename.Key)},
                        Script = checkScript,
                        Timeout = timeout,
                    };
                    _gateway.AddGatewayAction(findJavascript);
                    while (!findJavascript.Completed)
                    {
                        Thread.Sleep(_threadSleepTime);
                    }
                    //var resultFoundItems =
                    //    await frame.EvaluateScriptAsync(checkScript, timeout);
                    if (findJavascript.Response.Success)
                    {
                        try
                        {
                            numberOfElements += Convert.ToInt32(findJavascript.Response.Result);
                            selector.SetFoundElements(selector.FoundElements + numberOfElements);
                            break;

                        }
                        catch (FormatException ex)
                        {
                            selector.SetFoundElements(0);
                            return new KeyValuePairEx<int, string>(0,
                                "Exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
                        }

                    }
                    else
                    {
                        selector.SetFoundElements(0);
                        return new KeyValuePairEx<int, string>(0, "");
                    }
                }

            }
            if (numberOfElements == 0)
                return new KeyValuePairEx<int, string>(numberOfElements,
                    "Could not find any elements of " +
                    selector.SelectorString);
            if (selector.ExpectedNumberOfElements.Value == numberOfElements ||
                selector.ExpectedNumberOfElements.Value == 0)
                return new KeyValuePairEx<int, string>(numberOfElements,
                    "There are " + numberOfElements +
                    " of " + selector.SelectorString +
                    " Elements ");
            return new KeyValuePairEx<int, string>(numberOfElements, "There were " + numberOfElements +
                                                                   " instead of the expected " +
                                                                   selector.ExpectedNumberOfElements +
                                                                   " of " +
                                                                   selector.ExpectedNumberOfElements);
        }

    }

    public class CannotGetBrowserSize : Exception
        {
        }

        public class CannotFindSingleElementsFrame : Exception
        {
            public CannotFindSingleElementsFrame(string message) : base(message)
            {

            }
        }

        public class CannotFindRecursiveFrameList : Exception
        {
            public CannotFindRecursiveFrameList() : base("Die Liste kann nicht erstellt werden")
            {

            }
        }

        public class CannotFindItemByUrl : Exception
        {
            public CannotFindItemByUrl(string message) : base(message)
            {

            }
        }
    }

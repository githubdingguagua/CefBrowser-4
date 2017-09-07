using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CefBrowser.Handler;
using CefBrowserControl.BrowserActions.Elements;
using CefBrowserControl.BrowserActions.Helper;
using CefSharp;
using Rectangle = System.Windows.Shapes.Rectangle;
using Timer = System.Timers.Timer;

namespace CefBrowser.Gateway
{
    public class Gateway
    {
        private WebBrowserEx _webBrowser;

        private readonly ReaderWriterLockSlim _actionListLockSlim = new ReaderWriterLockSlim();

        public bool EnableVisualDrawing { get; set; } = true;
        private double _internalZoomLevel = 1;

        private bool _gatewayEnabled = true;
        public bool GatewayEnabled
        {
            get { return _gatewayEnabled; }
            set
            {
                _gatewayEnabled = value;
                if(value)
                    _gatewayTimer.Start();
            }
        }

        private readonly Queue<object> _gatewayActions = new Queue<object>();

        private readonly Timer _gatewayTimer = new Timer(100);

        public Gateway(WebBrowserEx webBrowser)
        {
            _webBrowser = webBrowser;
            _gatewayTimer.Elapsed += _gatewayTimer_Elapsed;
            _gatewayTimer.Start();
        }

        private void DrawPoint(int x, int y)
        {
            if (EnableVisualDrawing)
            {
                _webBrowser.Dispatcher.Invoke(() =>
                {
                    int dimensions = 10;
                    Rectangle rect = new Rectangle()
                    {
                        Width = dimensions,
                        Height = dimensions,
                        Fill = Brushes.Red,
                        Stroke = Brushes.DarkRed,

                    };
                    Canvas.SetLeft(rect, x - dimensions/2);
                    Canvas.SetTop(rect, y - dimensions/2);


                    _webBrowser.testvas.Children.Add(rect);
                });
            }
        }

        public IFrame GetAccordingFrame(string frameName)
        {
            return frameName == null
                ? _webBrowser.Browser.GetMainFrame()
                : _webBrowser.Browser.GetBrowser().GetFrame(frameName);
        }

        public bool HandleJavascriptEvaluateResultMessage(string message, ref IFrame waitFrame, string frameName)
        {
            if (message.Contains("Frame") &&
                message.Contains("is no longer available") ||
                message.Contains("Uncaught TypeError: Cannot read property 'length' of null\n@ undefined"))
            {
                waitFrame = GetAccordingFrame(frameName);
                return true;
            }
            return false;
        }

        private void _gatewayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _gatewayTimer.Stop();
            if (!GatewayEnabled)
                return;
            for (object actionObject; (actionObject = GetGatewayAction()) != null;)
            {
                GatewayAction.BaseObject action = (GatewayAction.BaseObject) actionObject;
                switch (action.ActionType)
                {
                    case GatewayAction.GatewayActionType.GetParentFrameNames:
                    {
                        GatewayAction.GetParentFrames frameAction = (GatewayAction.GetParentFrames) action;
                        _webBrowser.Dispatcher.Invoke(() =>
                        {
                            IFrame frame = null;
                            foreach (string frameName in  _webBrowser.Browser.GetBrowser().GetFrameNames())
                            {
                                if (frameAction.StartingFrame.IsRegex.Value &&
                                    Regex.IsMatch(frameName, frameAction.StartingFrame.Value.Value))
                                    frame = _webBrowser.Browser.GetBrowser().GetFrame(frameName);
                                else if (!frameAction.StartingFrame.IsRegex.Value && frameName == frameAction.StartingFrame.Value.Value)
                                    frame = _webBrowser.Browser.GetBrowser().GetFrame(frameName);
                            }
                            if (frame != null)
                            {
                                //frameAction.RecursiveFrameList.Add(new FrameDetails() { ExpectedFrameName = frame.AttributeName, Url = frame.Url });
                                while (true)
                                {
                                    frameAction.RecursiveFrameList.Add(new FrameDetails() { FrameName = frame.Name, Url = frame.Url });
                                    if (frame.Parent != null)
                                    {
                                        frame = frame.Parent;
                                    }
                                    else
                                        break;
                                }
                                frameAction.Success = true;
                            }
                        });
                            frameAction.Completed = true;

                        }
                        break;
                        case GatewayAction.GatewayActionType.FindElementsFrame:
                    {
                        GatewayAction.FindElementsFrame findingAction = (GatewayAction.FindElementsFrame) action;
                        _webBrowser.Dispatcher.Invoke(async() =>
                        {
                            foreach (string frameName in _webBrowser.Browser.GetBrowser().GetFrameNames())
                            {
                                IFrame frame = _webBrowser.Browser.GetBrowser().GetFrame(frameName);
                                var result = await frame.EvaluateScriptAsync(findingAction.EvaluationScript);
                                if (result.Success)
                                {
                                    if ((bool) result.Result)
                                    {
                                        findingAction.FrameName = frameName;
                                        findingAction.Success = true;
                                        break;
                                    }
                                }
                            }
                        });
                        findingAction.Completed = true;
                    }
                        break;
                    case GatewayAction.GatewayActionType.EvaluateJavascript:
                    {
                        GatewayAction.EvaluateJavascript evaluationAction = (GatewayAction.EvaluateJavascript) action;
                        _webBrowser.Dispatcher.Invoke(async () =>
                        {
                            while (true)
                            {
                                IFrame frame = null;
                                if (evaluationAction.FrameName == null)
                                    frame = _webBrowser.Browser.GetMainFrame();
                                else if (evaluationAction.FrameName.IsRegex.Value)
                                {
                                    foreach (var frameName in _webBrowser.Browser.GetBrowser().GetFrameNames())
                                    {
                                        if (Regex.IsMatch(frameName, evaluationAction.FrameName.Value.Value))
                                        {
                                            frame = _webBrowser.Browser.GetBrowser().GetFrame(frameName);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    frame = _webBrowser.Browser.GetBrowser().GetFrame(evaluationAction.FrameName.Value.Value);
                                }
                                if (frame == null)
                                {
                                    evaluationAction.Success = false;
                                    evaluationAction.Response = new JavascriptResponse()
                                    {
                                        Message = "Frame not found",
                                        Result = false,
                                        Success = false,
                                    };
                                    evaluationAction.Completed = true;
                                    break;
                                }
                                JavascriptResponse result =
                                    await
                                        frame.EvaluateScriptAsync(evaluationAction.Script,
                                            timeout: evaluationAction.Timeout);
                                if (!result.Success)
                                {
                                    if (HandleJavascriptEvaluateResultMessage(result.Message, ref frame,
                                        evaluationAction.FrameName.Value.Value))
                                        continue;
                                    else
                                        evaluationAction.Success = false;
                                }
                                else
                                    evaluationAction.Success = true;
                                evaluationAction.Response = result;
                                evaluationAction.Completed = true;
                                break;
                            }
                        });
                    }
                        break;
                    case GatewayAction.GatewayActionType.GetHttpAuth:
                        {
                            GatewayAction.GetHttpAuth getHttpAuth = (GatewayAction.GetHttpAuth)action;
                            bool success = false;
                            HttpAuth removingAuth = null;
                            foreach (var handledHttpAuth in _webBrowser.RequestHandler.HandledHttpAuths)
                            {
                                if (getHttpAuth.ExpectedHost.Value.Value == "" ||
                                    (getHttpAuth.ExpectedHost.IsRegex.Value &&
                                     Regex.IsMatch(handledHttpAuth.Host, getHttpAuth.ExpectedHost.Value.Value) ||
                                     !getHttpAuth.ExpectedHost.IsRegex.Value &&
                                     handledHttpAuth.Host == getHttpAuth.ExpectedHost.Value.Value))
                                {
                                    if (getHttpAuth.ExpectedRealm.Value.Value == "" ||
                                        (getHttpAuth.ExpectedRealm.IsRegex.Value &&
                                         Regex.IsMatch(handledHttpAuth.Realm, getHttpAuth.ExpectedRealm.Value.Value) ||
                                         !getHttpAuth.ExpectedRealm.IsRegex.Value &&
                                         handledHttpAuth.Realm == getHttpAuth.ExpectedRealm.Value.Value))
                                    {
                                        if (getHttpAuth.ExpectedSchemaType == GetHttpAuth.SchemaTypes.Nope ||
                                            (getHttpAuth.ExpectedSchemaType.ToString() == handledHttpAuth.Scheme.ToString()))
                                        {
                                            if (getHttpAuth.ExpectedPort == null ||
                                                ((int) getHttpAuth.ExpectedPort == handledHttpAuth.Port))
                                            {
                                                success = handledHttpAuth.SuccessfullyHandled;
                                                getHttpAuth.Host = handledHttpAuth.Host;
                                                getHttpAuth.Port = handledHttpAuth.Port;
                                                getHttpAuth.Realm = handledHttpAuth.Realm;
                                                foreach (var value in Enum.GetValues(typeof(GetHttpAuth.SchemaTypes)))
                                                {
                                                    if (value.ToString() == handledHttpAuth.Scheme)
                                                    {
                                                        getHttpAuth.Scheme = (GetHttpAuth.SchemaTypes) Enum.Parse(typeof(GetHttpAuth.SchemaTypes), value.ToString());
                                                        break;
                                                    }
                                                }
                                                removingAuth = handledHttpAuth;
                                            }
                                        }
                                    }
                                }
                            }
                            if(removingAuth != null)
                                _webBrowser.RequestHandler.HandledHttpAuths.Remove(removingAuth);
                            getHttpAuth.Success = success;
                            getHttpAuth.Completed = true;
                        }
                        break;
                    case GatewayAction.GatewayActionType.SetHttpAuth:
                        {
                            GatewayAction.SetHttpAuth httpAuth = (GatewayAction.SetHttpAuth)action;
                            _webBrowser.RequestHandler.PreparedHttpAuths.Add(httpAuth);
                            action.Success = true;
                            action.Completed = true;
                        }
                        break;
                    case GatewayAction.GatewayActionType.GetJsDialog:
                    {
                            GatewayAction.GetJsDialog jsAction = (GatewayAction.GetJsDialog)action;
                        bool success = false;
                        JsDialog removingJsDialog = null;
                        foreach (var handledJsDialog in _webBrowser.JsDialogHandler.HandledJsDialogs)
                        {
                            if (jsAction.ExpectedDefaultPromptValue == null ||
                                (jsAction.ExpectedDefaultPromptValue.IsRegex.Value &&
                                 Regex.IsMatch(handledJsDialog.DefaultPromptText, jsAction.ExpectedDefaultPromptValue.Value.Value) ||
                                 !jsAction.ExpectedDefaultPromptValue.IsRegex.Value &&
                                 handledJsDialog.DefaultPromptText == jsAction.ExpectedDefaultPromptValue.Value.Value))
                            {
                                if (jsAction.ExpectedMessageText == null ||
                                    (jsAction.ExpectedMessageText.IsRegex.Value &&
                                     Regex.IsMatch(handledJsDialog.MessageText, jsAction.ExpectedMessageText.Value.Value) ||
                                     !jsAction.ExpectedMessageText.IsRegex.Value &&
                                     handledJsDialog.MessageText == jsAction.ExpectedMessageText.Value.Value))
                                {
                                    if (jsAction.ExpectedDialogType == GetJsPrompt.DialogTypes.Nope ||
                                        (jsAction.ExpectedDialogType.ToString() == handledJsDialog.DialogType.ToString()))
                                    {
                                        success = handledJsDialog.SucessfullyHandled;
                                        jsAction.MessageText = handledJsDialog.MessageText;
                                        jsAction.DefaultPromptValue = handledJsDialog.DefaultPromptText;
                                            foreach (var value in Enum.GetValues(typeof(GetJsPrompt.DialogTypes)))
                                            {
                                                if (value.ToString() ==  handledJsDialog.DialogType.ToString())
                                                {
                                                    jsAction.DialogType = (GetJsPrompt.DialogTypes)Enum.Parse(typeof(GetJsPrompt.DialogTypes), value.ToString());
                                                    break;
                                                }
                                            }
                                        removingJsDialog = handledJsDialog;
                                    }
                                }
                            }
                        }
                        if (removingJsDialog != null)
                            _webBrowser.JsDialogHandler.HandledJsDialogs.Remove(removingJsDialog);
                        jsAction.Success = success;
                        jsAction.Completed = true;
                    }
                        break;
                    case GatewayAction.GatewayActionType.SetJsDialog:
                        {
                            GatewayAction.SetJsDialog jsAction = (GatewayAction.SetJsDialog)action;
                            _webBrowser.JsDialogHandler.PreparedDialogActions.Add(jsAction);
                            action.Success = true;
                            action.Completed = true;
                        }
                        break;
                    case GatewayAction.GatewayActionType.ExecuteJavascript:
                    {
                        GatewayAction.ExecuteJavascript executeJavascript = (GatewayAction.ExecuteJavascript) action;
                        _webBrowser.Dispatcher.Invoke(() =>
                        {
                            IFrame frame = null;
                            if (executeJavascript.FrameName == null)
                                frame = _webBrowser.Browser.GetMainFrame();
                            else if (executeJavascript.FrameName.IsRegex.Value)
                            {
                                foreach (var frameName in _webBrowser.Browser.GetBrowser().GetFrameNames())
                                {
                                    if (Regex.IsMatch(frameName, executeJavascript.FrameName.Value.Value))
                                    {
                                        frame = _webBrowser.Browser.GetBrowser().GetFrame(frameName);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                frame = _webBrowser.Browser.GetBrowser().GetFrame(executeJavascript.FrameName.Value.Value);
                            }
                            if (frame == null)
                            {
                                executeJavascript.Success = false;
                            }
                            else
                            {
                                frame.ExecuteJavaScriptAsync(executeJavascript.Script);

                                executeJavascript.Success = true;
                            }
                            executeJavascript.Completed = true;
                        });
                    }
                        break;
                    case GatewayAction.GatewayActionType.GetSize:
                    {
                        GatewayAction.GetSize getSize = (GatewayAction.GetSize) action;
                        _webBrowser.Dispatcher.Invoke(() =>
                        {
                            getSize.Width = (int) _webBrowser.ActualWidth;
                            getSize.Height = (int) _webBrowser.ActualHeight;
                        });
                        getSize.Success = true;
                        getSize.Completed = true;
                    }
                        break;
                    case GatewayAction.GatewayActionType.IsBrowserInitiated:
                    {
                        GatewayAction.IsBrowserInitiated initiatedAction = (GatewayAction.IsBrowserInitiated) action;
                        _webBrowser.Dispatcher.Invoke(() =>
                        {
                            initiatedAction.Result = _webBrowser.Browser.IsBrowserInitialized;
                        });
                        initiatedAction.Completed = true;
                    }
                        break;
                    case GatewayAction.GatewayActionType.SendKeyBoard:
                    {

                    }
                        break;
                    case GatewayAction.GatewayActionType.SendMouseClick:
                    {

                    }
                        break;
                    case GatewayAction.GatewayActionType.SendMouseWheel:
                    {
                        var mouseAction = (GatewayAction.SendMouseWheel) action;
                        _webBrowser.Dispatcher.Invoke(() =>
                        {
                            //_webBrowser.Browser.SendMouseWheelEvent();
                            //_webBrowser.Browser.SendMouseWheelEvent((int)mouseAction.WhellLocation.X, (int)mouseAction.WhellLocation.Y, mouseAction.DeltaWheelAction.Key, -mouseAction.DeltaWheelAction.ExpectedValue, CefEventFlags.None);
                            if (mouseAction.DeltaWheelAction.Key != 0)
                                _webBrowser.Browser.SendMouseWheelEvent((int)(mouseAction.WhellLocation.X), (int)(mouseAction.WhellLocation.Y), (int)(-mouseAction.DeltaWheelAction.Key * _internalZoomLevel), 0, CefEventFlags.None);
                            else
                                _webBrowser.Browser.SendMouseWheelEvent((int)(mouseAction.WhellLocation.X), (int)(mouseAction.WhellLocation.Y), 0, (int)(-mouseAction.DeltaWheelAction.Value * _internalZoomLevel), CefEventFlags.None);
                            //MessageBox.Show(_webBrowser, _webBrowser.Browser.ZoomLevel.ToString());
                        });
                        DrawPoint((int) (mouseAction.WhellLocation.X), (int) (mouseAction.WhellLocation.Y));
                        mouseAction.Success = true;
                        mouseAction.Completed = true;
                    }
                        break;
                    case GatewayAction.GatewayActionType.SiteLoaded:
                    {
                        var site = (GatewayAction.SiteLoaded) action;
                        _webBrowser.Dispatcher.Invoke(() =>
                        {
                            site.Address = _webBrowser.Browser.Address;
                            site.Success = !_webBrowser.Browser.IsLoading;
                        });
                        site.Completed = true;
                    }
                        break;
                    case GatewayAction.GatewayActionType.ResourceLoaded:
                    {
                        var resource = (GatewayAction.ResourceLoaded) action;
                        _webBrowser.Dispatcher.Invoke(() =>
                        {
                            foreach (var loadedResource in _webBrowser.RequestHandler.LoadedResources
                            )
                            {
                                if (resource.FrameName == null && loadedResource.IsMainFrame ||
                                    resource.FrameName != null &&
                                    (resource.FrameName.IsRegex.Value
                                        ? Regex.IsMatch(loadedResource.FrameName, resource.FrameName.Value.Value)
                                        : loadedResource.FrameName == resource.FrameName.Value.Value))
                                {
                                    if (resource.ResourceUrl.IsRegex.Value
                                        ? Regex.IsMatch(loadedResource.Url, resource.ResourceUrl.Value.Value)
                                        : (loadedResource.Url == resource.ResourceUrl.Value.Value))
                                    {
                                        resource.Success = true;
                                        resource.DateTime = loadedResource.DateTime;
                                    }
                                }
                            }

                        });
                        resource.Completed = true;
                    }
                        break;
                    case GatewayAction.GatewayActionType.FrameLoaded:
                    {
                        var frame = (GatewayAction.FrameLoaded) action;
                        _webBrowser.Dispatcher.Invoke(() =>
                        {
                            List<FrameLoadingState> frameLoadingStates = _webBrowser.BrowsingState.FrameLoadingStates;
                            foreach (FrameLoadingState state in frameLoadingStates)
                            {
                                if (state.IsMainFrame && frame.FrameName == null || frame.FrameName != null &&
                                    (frame.FrameName.IsRegex.Value
                                        ? Regex.IsMatch(state.FrameName, frame.FrameName.Value.Value)
                                        : frame.FrameName != null &&
                                          state.FrameName == frame.FrameName.Value.Value))
                                {
                                    frame.Success = !state.IsLoading;
                                    break;
                                }
                            }
                        });
                        frame.Completed = true;
                    }
                        break;
                        case GatewayAction.GatewayActionType.ShowMessageBox:
                    {
                        GatewayAction.ShowMessageBox messageBox = (GatewayAction.ShowMessageBox) action;
                        _webBrowser.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(_webBrowser, messageBox.Text);
                        });
                        messageBox.Completed = true;

                    }
                        break;
                        case GatewayAction.GatewayActionType.GetFrameNames:
                    {
                        var getFrameNames = (GatewayAction.GetFrameNames) action;
                        _webBrowser.Dispatcher.Invoke(() =>
                        {
                            IFrame mainFrame = _webBrowser.Browser.GetMainFrame();
                            foreach (string frameName in _webBrowser.Browser.GetBrowser().GetFrameNames())
                            {
                                getFrameNames.Frames.Add(frameName, mainFrame.Name == frameName);
                            }
                            getFrameNames.Success = true;
                        });
                        action.Completed = true;
                    }
                        break;
                }
            }
            _gatewayTimer.Start();
        }

        private object GetGatewayAction()
        {
            _actionListLockSlim.EnterReadLock();
            try
            {
                if (_gatewayActions.Count == 0)
                    return null;
                return _gatewayActions.Dequeue();
            }
            finally
            {
                _actionListLockSlim.ExitReadLock();
            }
        }

        public void AddGatewayAction(object action)
        {
            if (action == null) return;
            try
            {
                _actionListLockSlim.EnterWriteLock();
                _gatewayActions.Enqueue(action);
            }
            finally
            {
                _actionListLockSlim.ExitWriteLock();
            }
        }
    }
}

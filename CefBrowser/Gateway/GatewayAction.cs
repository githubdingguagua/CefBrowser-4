using System;
using System.Collections.Generic;
using System.Windows;
using CefBrowserControl;
using CefBrowserControl.BrowserActions.Elements;
using CefBrowserControl.BrowserActions.Helper;
using CefSharp;

namespace CefBrowser.Gateway
{
    public class GatewayAction
    {
        public enum GatewayActionType
        {
            GetFrameNames,
            GetParentFrameNames,
            FindElementsFrame,
            ExecuteJavascript,
            EvaluateJavascript,
            IsBrowserInitiated,
            GetSize,
            SendMouseClick,
            SendMouseWheel,
            SendKeyBoard,
            ResourceLoaded,
            SiteLoaded,
            FrameLoaded,
            ShowMessageBox,
            GetJsDialog,
            SetJsDialog,
            GetHttpAuth,
            SetHttpAuth,
        }

        public abstract class BaseObject
        {
            public GatewayActionType ActionType { get; set; }

            public bool Completed { get; set; }

            public bool Success { get; set; }

            public TimeSpan? Timeout { get; set; }

            protected BaseObject()
            {
            }
        }

        public class GetHttpAuth : BaseObject
        {
            public GetHttpAuth()
            {
                ActionType = GatewayActionType.GetHttpAuth;
            }

            public CefBrowserControl.BrowserActions.Elements.GetHttpAuth.SchemaTypes ExpectedSchemaType;
            public StringOrRegex ExpectedHost;
            public int? ExpectedPort = null;
            public StringOrRegex ExpectedRealm;
            public string Host;
            public int Port;
            public string Realm;
            public CefBrowserControl.BrowserActions.Elements.GetHttpAuth.SchemaTypes Scheme;
        }

        public class SetHttpAuth : GetHttpAuth
        {
            public SetHttpAuth()
            {
                ActionType = GatewayActionType.SetHttpAuth;
            }

            public bool Cancel = true;
            public string Username;
            public string Password;
        }

        public class GetJsDialog : BaseObject
        {
            public GetJsDialog()
            {
                ActionType = GatewayActionType.GetJsDialog;
            }

            public StringOrRegex ExpectedMessageText;
            public StringOrRegex ExpectedDefaultPromptValue;
            public string DefaultPromptValue;
            public string MessageText;
            public GetJsPrompt.DialogTypes DialogType;
            public GetJsPrompt.DialogTypes ExpectedDialogType;
        }

        public class SetJsDialog : GetJsDialog
        {
            public SetJsDialog()
            {
                ActionType = GatewayActionType.SetJsDialog;
            }

            public bool SetSuccess;
            public bool SetText;
            public string Text = "";
        }

        public class GetParentFrames : BaseObject
        {
            public GetParentFrames()
            {
                ActionType = GatewayActionType.GetParentFrameNames;
            }

            public StringOrRegex StartingFrame { get; set; }

            public List<FrameDetails> RecursiveFrameList { get; set; } = new List<FrameDetails>();
        }

        public class FindElementsFrame : BaseObject
        {
            public FindElementsFrame()
            {
                ActionType = GatewayActionType.FindElementsFrame;
            }

            public string EvaluationScript { get; set; }

            public string FrameName { get; set; }

        }

        public class ExecuteJavascript : BaseObject
        {
            public ExecuteJavascript()
            {
                ActionType = GatewayActionType.ExecuteJavascript;
            }

            public string Script { get; set; }

            public StringOrRegex FrameName { get; set; }
        }

        public class EvaluateJavascript : ExecuteJavascript
        {
            public EvaluateJavascript()
            {
                ActionType = GatewayActionType.EvaluateJavascript;
            }

            public JavascriptResponse Response { get; set; }
        }

        public class IsBrowserInitiated : BaseObject
        {
            public IsBrowserInitiated()
            {
                ActionType = GatewayActionType.IsBrowserInitiated;
            }

            public bool Result { get; set; }
        }

        public class GetSize : BaseObject
        {
            public GetSize()
            {
                ActionType = GatewayActionType.GetSize;
            }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public class SendMouseClick : BaseObject
        {
            public SendMouseClick()
            {
                ActionType = GatewayActionType.SendMouseClick;
            }
            public Point ClickLocation { get; set; }

            public int? Duration { get; set; } = null;

            public MouseButtonType ButtonType { get; set; } = MouseButtonType.Left;

            public bool DoubleClick { get; set; }
        }

        public class SendMouseWheel : BaseObject
        {
            public SendMouseWheel()
            {
                ActionType = GatewayActionType.SendMouseWheel;
            }
            public System.Drawing.Point WhellLocation { get; set; }

            public KeyValuePairEx<int,int> DeltaWheelAction { get; set; }

            public bool SeperateVerticalAndHorizontalMovement { get; set; } = true;

            public int? Duration { get; set; } = null;

            public StringOrRegex FrameName { get; set; }
        }

        public class SendKeyBoard : BaseObject
        {
            public SendKeyBoard()
            {
                ActionType = GatewayActionType.SendKeyBoard;
            }

            public string Text { get; set; } = "";

            public int? Duration { get; set; } = null;
        }

        public class ResourceLoaded : BaseObject
        {
            public ResourceLoaded()
            {
                ActionType = GatewayActionType.ResourceLoaded;
            }

            public StringOrRegex ResourceUrl { get; set; }

            public StringOrRegex FrameName { get; set; }

            public DateTime DateTime { get; set; }
        }

        public class SiteLoaded : BaseObject
        {
            public SiteLoaded()
            {
                ActionType = GatewayActionType.SiteLoaded;
            }

            public string Address { get; set; }


        }

        public class FrameLoaded : BaseObject
        {
            public FrameLoaded()
            {
                ActionType = GatewayActionType.FrameLoaded;
            }

            public StringOrRegex FrameName { get; set; }
        }

        public class GetFrameNames : BaseObject
        {
            public GetFrameNames()
            {
                ActionType = GatewayActionType.GetFrameNames;
            }

            //AttributeName, is MainFrame
            public Dictionary<string, bool> Frames = new Dictionary<string, bool>();
        }

        public class ShowMessageBox : BaseObject
        {
            public ShowMessageBox()
            {
                ActionType = GatewayActionType.ShowMessageBox;
            }
            public string Text { get; set; }
        }

    }
}

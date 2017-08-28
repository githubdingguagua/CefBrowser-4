using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CefBrowser.Gateway;
using CefBrowserControl.BrowserActions.Elements;
using CefSharp;

namespace CefBrowser.Handler
{
    public class JsDialogHandler : IJsDialogHandler
    {
        public List<JsDialog> HandledJsDialogs = new List<JsDialog>();
        public List<GatewayAction.SetJsDialog> PreparedDialogActions = new List<GatewayAction.SetJsDialog>();

        public bool OnJSDialog(IWebBrowser browserControl, IBrowser browser, string originUrl, CefJsDialogType dialogType,
            string messageText, string defaultPromptText, IJsDialogCallback callback, ref bool suppressMessage)
        {
            bool success = false;
            foreach (var preparedDialog  in PreparedDialogActions)
            {
                if (preparedDialog.ExpectedDefaultPromptValue == null ||
                    (preparedDialog.ExpectedDefaultPromptValue.IsRegex.Value &&
                     Regex.IsMatch(defaultPromptText, preparedDialog.ExpectedDefaultPromptValue.Value.Value) ||
                     !preparedDialog.ExpectedDefaultPromptValue.IsRegex.Value &&
                     defaultPromptText == preparedDialog.ExpectedDefaultPromptValue.Value.Value))
                {
                    if (preparedDialog.ExpectedMessageText == null ||
                        (preparedDialog.ExpectedMessageText.IsRegex.Value &&
                         Regex.IsMatch(messageText, preparedDialog.ExpectedMessageText.Value.Value) ||
                         !preparedDialog.ExpectedMessageText.IsRegex.Value &&
                         messageText == preparedDialog.ExpectedMessageText.Value.Value))
                    {
                        if (preparedDialog.ExpectedDialogType == GetJsPrompt.DialogTypes.Nope ||
                            (preparedDialog.ExpectedDialogType.ToString() == dialogType.ToString()))
                        {
                            if (callback.IsDisposed)
                                break;
                            if (preparedDialog.SetText)
                                callback.Continue(preparedDialog.SetSuccess, preparedDialog.Text);
                            else
                                callback.Continue(preparedDialog.SetSuccess);
                            HandledJsDialogs.Add(new JsDialog()
                            {
                                DefaultPromptText = defaultPromptText,
                                DialogType = dialogType,
                                MessageText = messageText,
                                OriginUrl = originUrl,
                                Callback = callback,
                                SucessfullyHandled = true,
                            });
                            return true;
                        }
                    }
                }
            }
            HandledJsDialogs.Add(new JsDialog()
            {
                DefaultPromptText = defaultPromptText,
                DialogType = dialogType,
                MessageText = messageText,
                OriginUrl = originUrl,
                Callback = callback,
                SucessfullyHandled = false
        });
            suppressMessage = true;
            return false;
            /*
            Different types of dialogs:
            Prompt: https://www.w3schools.com/js/tryit.asp?filename=tryjs_prompt
            Confirm: https://www.w3schools.com/js/tryit.asp?filename=tryjs_confirm
            Alert: https://www.w3schools.com/js/tryit.asp?filename=tryjs_alert
            */
        }

        public bool OnJSBeforeUnload(IWebBrowser browserControl, IBrowser browser, string message, bool isReload,
            IJsDialogCallback callback)
        {
            //surpressing Is it OK to leave/reload this page?
            if (!callback.IsDisposed)
                callback.Continue(true);
            return true;
        }

        public void OnResetDialogState(IWebBrowser browserControl, IBrowser browser)
        {

        }

        public void OnDialogClosed(IWebBrowser browserControl, IBrowser browser)
        {

        }
    }
}

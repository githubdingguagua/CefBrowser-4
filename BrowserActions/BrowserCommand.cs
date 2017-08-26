using System;
using System.Xml.Serialization;
using CefBrowserControl.BrowserCommands;
using CefBrowserControl.Resources;
using KeePassLib.Security;

namespace CefBrowserControl
{
    [XmlInclude(typeof(LoadUrl))]
    [XmlInclude(typeof(Open))]
    [XmlInclude(typeof(Quit))]
    [XmlInclude(typeof(SwitchUserInputEnabling))]
    [XmlInclude(typeof(SwitchWindowVisibility))]
    [XmlInclude(typeof(GetInputFromUser))]
    [Serializable]
    public class BrowserCommand : BaseObject
    {
        private static int counter = 0;
        //Unique ID(BrowserTabs and so on)
        public string UID;

        //Unique Command ID
        public string UCID;

      

        public BrowserCommand()
        {
            GenerateNewUcid();
        }

        private void GenerateNewUcid()
        {
            UCID = HashingEx.Hashing.GetSha1Hash(DateTime.Now.ToString() + " " + counter++ + Guid.NewGuid());
        }

        public void GenerateNewUcid(string additional)
        {
            UCID = HashingEx.Hashing.GetSha1Hash(DateTime.Now.ToString() + " " + counter++ + additional + Guid.NewGuid());
        }

        public event EventHandler CommandFinishedEventHandler;

        protected virtual void OnActionFinished(EventArgs e)
        {
            EventHandler handler = CommandFinishedEventHandler;
            handler?.Invoke(this, e);
        }

        public void SetCompleted(bool finished)
        {
            Completed = finished;
            OnActionFinished(new EventArgs());
        }

        public bool ExecuteEventHandler { get; set; } = false;
    }
}

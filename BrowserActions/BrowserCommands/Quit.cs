using System;
using System.Collections.Generic;
using System.Text;

namespace CefBrowserControl.BrowserCommands
{
    [Serializable]
    public class Quit : BrowserCommand
    {
        public bool All;

        public Quit() : base()
        {
            
        }

        public Quit(string uid) : this()
        {
            UID = uid;
        }

        public Quit(string uid, bool all) : this()
        {
            UID = uid;
            All = all;
        }
    }
}

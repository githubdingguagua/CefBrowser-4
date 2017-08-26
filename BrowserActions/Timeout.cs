using System;
using System.Collections.Generic;
using System.Text;

namespace CefBrowserControl
{
    public static class Timeout
    {
        public static bool ShouldBreakDueTimeout(BaseObject baseObject)
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
    }
}

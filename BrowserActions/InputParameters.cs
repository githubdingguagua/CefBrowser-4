using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;

namespace CefBrowserControl
{
    public abstract class InputParameters
    {
        public bool HaveRequirementsBeenSet;

        public List<KeyValuePairEx<string, object>> InputParameterAvailable;

        public List<KeyValuePairEx<string, object>> InputParameterSet;

        public static List<string> InputParameterRequired;
    }

    public interface IInstanciateInputParameters
    {
        void NewInstance();

        void ReadAvailableInputParameters();
    }
    
}

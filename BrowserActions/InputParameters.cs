using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CefBrowserControl
{
    public abstract class InputParameters
    {
        [XmlIgnore]
        public bool HaveRequirementsBeenSet;

        public List<KeyValuePairEx<string, object>> InputParameterAvailable;

        [XmlIgnore]
        public List<KeyValuePairEx<string, object>> InputParameterSet;

        public static List<string> InputParameterRequired;
    }

    public interface IInstanciateInputParameters
    {
        void NewInstance();

        void ReadAvailableInputParameters();
    }
    
}

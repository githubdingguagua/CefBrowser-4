//https://blogs.msdn.microsoft.com/seshadripv/2005/11/02/serializing-an-object-of-the-keyvaluepair-generic-class/
//https://stackoverflow.com/questions/83232/is-there-a-serializable-generic-key-value-pair-class-in-net
using System;
using System.Collections.Generic;
using System.Text;

namespace CefBrowserControl
{
    [Serializable]
    public struct KeyValuePairEx<K, V>
    {
        public K Key { get; set; }
        public V Value { get; set; }

        public KeyValuePairEx(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }
}

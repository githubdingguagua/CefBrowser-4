using System;
using System.Collections.Generic;
using System.Text;
using SerializationDotNet2.Xml;

namespace CefBrowserControl.Conversion
{
    public class CefDecoding
    {
        public static CefDecodeResult Decode(string plainText)
        {
            SerializeContainer container =
                (SerializeContainer)
                Deserializer.DeserializeObjectFromString(
                    plainText, typeof(SerializeContainer));
            string serializedChild = EncodingEx.Base64.Decoder.DecodeString(System.Text.Encoding.UTF8,
                container.SerializedEncodedChild);
            Type t = Type.GetType(container.ChildType);
            object obj =
                Deserializer.DeserializeObjectFromString(
                    serializedChild, t);

            if (obj is BrowserAction)
            {
                BrowserAction action = (BrowserAction)obj;
                action.ActionObject = Deserializer.DeserializeObjectFromString(EncodingEx.Base64.Decoder.DecodeString(Encoding.UTF8, action.SerializedActionObject),
                    Type.GetType(action.SerializedActionObjectType));
                action.SerializedActionObject = "";
            }

            return new CefDecodeResult(obj, t, container.UCID);
        }
    }

    public class CefDecodeResult
    {
        public object DecodedObject;
        public Type DecodedType;
        public string UCID;

        public CefDecodeResult(object obj, Type t, string ucid)
        {
            DecodedObject = obj;
            DecodedType = t;
            UCID = ucid;
        }
    }
}

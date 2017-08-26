using System;
using System.Collections.Generic;
using System.Text;
using SerializationDotNet2.Xml;

namespace CefBrowserControl.Conversion
{
    public class CefEncoding
    {
        public static string Encode(string ucid, object obj)
        {
            try
            {
                if (obj is BrowserAction)
                {
                    BrowserAction action = (BrowserAction) obj;
                    action.SerializedActionObjectType = action.ActionObject.GetType().AssemblyQualifiedName;
                    string serializedObject = Serializer.SerializeObjectToString(action.ActionObject,
                        action.ActionObject.GetType());
                    action.SerializedActionObject = EncodingEx.Base64.Encoder.EncodeString(Encoding.UTF8, serializedObject
                        );
                    action.ActionObject = null;
                }

                string serialized =
                    Serializer.SerializeObjectToString(obj,
                        obj.GetType());
                SerializeContainer container = new SerializeContainer();
                container.UCID = ucid;
                container.SerializedEncodedChild = EncodingEx.Base64.Encoder.EncodeString(System.Text.Encoding.UTF8,
                    serialized);
                container.ChildType = obj.GetType().AssemblyQualifiedName;
                string serializedCommand =
                    Serializer.SerializeObjectToString(container,
                        container.GetType());
                string msg = EncodingEx.Base64.Encoder.EncodeString(System.Text.Encoding.UTF8, serializedCommand
                );
                return msg;
            }
            catch (Exception ex)
            {
                ExceptionHandling.Handling.GetException("Unexpected",ex);
                return null;
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace CefBrowserControl.BrowserActions.Helper
{
    public class ObjectLocation
    {
        //https://developer.mozilla.org/en-US/docs/Web/API/CSS_Object_Model/Determining_the_dimensions_of_elements
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int DocumentClientWidth { get; set; }
        public int DocumentClientHeight { get; set; }
        public int DocumentScrollWidth { get; set; }
        public int DocumentScrollHeight { get; set; }
        public int DocumentOffsetWidth { get; set; }
        public int DocumentOffsetHeight { get; set; }

        public KeyValuePairEx<int,int> GetRandomLocation()
        {
            Random random= new Random(Guid.NewGuid().GetHashCode());
            int newX = random.Next(X, X+Width + 1);
            int newY = random.Next(Y, Y+Height + 1);
            return new KeyValuePairEx<int, int>(newX, newY);
        }
    }
}

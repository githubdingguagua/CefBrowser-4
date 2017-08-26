namespace CefBrowserControl.BrowserActions.Helper
{
    public class FrameDetails
    {
        public string FrameName { get; set; }

        public int ScrollOffsetX { get; set; }
        public int ClientWidth { get; set; }
        public int ScrollWidth { get; set; }

        public int ScrollOffsetY { get; set; }
        public int ClientHeight { get; set; }
        public int ScrollHeight { get; set; }

        public ObjectLocation NextTargetLocation { get; set; }

        public ObjectLocation FrameLocation { get; set; }

        //look@me im desperate
        //public GatewayAction.SendMouseWheel MousheWheelAction { get; set; }

        public string Url { get; set; }

        public int SrcAttributeNr { get; set; }

        public Rectangle ViewingRectangle { get; set; }
    }
}

using System.Drawing;

namespace CefBrowserControl.BrowserActions.Helper
{
    public class Rectangle
    {
        private Point _leftTop, _leftBottom, _rightTop, _rightBottom;

        public Point LeftTop => _leftTop;
        public Point LeftBottom => _leftBottom;
        public Point RightTop => _rightTop;
        public Point RightBottom => _rightBottom;


        public Rectangle(Point leftTopPoint, int width, int height)
        {
            _leftTop = _leftBottom = _rightTop = _rightBottom = leftTopPoint;
            _leftBottom.Y += height;
            _rightTop.X += width;
            _rightBottom.X += width;
            _rightBottom.Y += height;
        }

        //MayBeTheFehlerBeHere
        public void Move(int deltaX = 0, int deltaY = 0)
        {
            _leftTop.X += deltaX;
            _leftTop.Y += deltaY;
            _leftBottom.X += deltaX;
            _leftBottom.Y += deltaY;
            _rightTop.X += deltaX;
            _rightTop.Y += deltaY;
            _rightBottom.X += deltaX;
            _rightBottom.Y += deltaY;

        }
    }
}

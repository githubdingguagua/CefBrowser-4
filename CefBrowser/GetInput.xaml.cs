using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CefBrowserControl;
using CefBrowserControl.BrowserCommands;
using CefBrowserControl.Resources;
using Image = System.Windows.Controls.Image;
using Point = System.Drawing.Point;
using SystemColors = System.Drawing.SystemColors;
using Timer = System.Timers.Timer;

namespace CefBrowser
{
    /// <summary>
    /// Interaktionslogik für GetInput.xaml
    /// </summary>
    public partial class GetInput : Window
    {
        private GetInputFromUser GetInputFromUser;
        Timer timer = new Timer();
        private bool alreadyBrougthToFront = false;

        [DllImport("User32.dll")]
        public static extern Int32 SetForegroundWindow(int hWnd);

        public static BitmapImage DecodeImageToBitmapImage(string base64String)
        {
            if (base64String.IndexOf(',') >= 0)
                base64String = base64String.Substring(base64String.IndexOf(',') + 1);

            // Convert base 64 string to byte[]
            byte[] binaryData = Convert.FromBase64String(base64String);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(binaryData);
            bitmapImage.EndInit();

            return bitmapImage;
        }

        public GetInput(GetInputFromUser getInputFromUser)
        {
            InitializeComponent();

            GetInputFromUser = getInputFromUser;

            Timer timeOutTimer = new Timer();
            timeOutTimer.Interval = 100;
            timeOutTimer.Elapsed += delegate(object sender, ElapsedEventArgs args)
            {
                timeOutTimer.Stop();
                bool startAgain = true;
                if (CefBrowserControl.Timeout.ShouldBreakDueTimeout(((BaseObject) getInputFromUser)))
                {
                    try
                    {
                        //this.BeginInvoke((MethodInvoker)delegate ()
                        //{
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            timer.Stop();
                            Close();
                        }));
                        
                       
                        //});
                        startAgain = false;
                    }
                    catch (Exception)
                    {
                        startAgain = false;
                    }
                }
                if (startAgain)
                    timeOutTimer.Start();
            };
            timeOutTimer.Start();

            StackPanelInsecureObjects.Children.Clear();

            foreach (var displayObject in GetInputFromUser.InsecureDisplayObjects)
            {
                if (displayObject is CefBrowserControl.Resources.InsecureText)
                {
                    CefBrowserControl.Resources.InsecureText text = (InsecureText)displayObject;

                    //<TextBox Text="asdfasd&#xA;asdfasdf&#xA;dasdfasdf&#xA;asdsadfasdf&#xD;&#xA;asdfasd" Width="350" Margin="0,5" IsReadOnly="True" />

                    //ScrollViewer scrollViewer = new ScrollViewer();
                    //scrollViewer.Margin = new Thickness(0, 5, 0, 5);
                    //scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    //scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

                    TextBox textBox = new TextBox();
                    textBox.Text = GetInputFromUser.TranslatePlaceholderStringToSingleString(text.Value);
                    textBox.Width = 350;
                    textBox.Height = 80;
                    textBox.IsReadOnly = true;
                    textBox.TextWrapping = TextWrapping.Wrap;
                    textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;

                    //scrollViewer.Content = textBox;
                    StackPanelInsecureObjects.Children.Add(textBox);
                }
                else if (displayObject is CefBrowserControl.Resources.InsecureImage)
                {
                    CefBrowserControl.Resources.InsecureImage encodedImage = (InsecureImage)displayObject;

                    //< Image Margin = "0,5" Height = "80" Width = "350" Source = "earth.ico" />

                    string base64String = GetInputFromUser.TranslatePlaceholderStringToSingleString(encodedImage.Base64EncodedImage);
                    System.Drawing.Image imageObject = EncodingEx.Base64.Decoder.DecodeImage(base64String);

                    Image image = new Image();
                    image.Margin = new Thickness(0,5,0,5);
                    image.Width = 350;
                    image.Height = 80;
                    image.Source = DecodeImageToBitmapImage(base64String);

                    StackPanelInsecureObjects.Children.Add(image);
                }
                else
                {
                    ExceptionHandling.Handling.GetException("Unexpected", new Exception("Element does not exist!"));
                }
            }

            if (GetInputFromUser.InputNeeded.Value)
            {
                //groupBoxInput.Location = new System.Drawing.Point(groupBoxInput.Location.X, _currentY);
                ////groupBoxInput.Width = Width - 2 * HorizontalSpace;
                //_currentY += groupBoxInput.Height + VerticalSpace;
            }
            else
            {
                StackPanelInput.Visibility = Visibility.Hidden;
            }

            //Height = _currentY;

            timer.Interval = 100;
            timer.AutoReset = false;
            timer.Elapsed += delegate (object sender, ElapsedEventArgs args)
            {
                timer.Stop();
                try
                {
                    if (!GetInputFromUser.KeepInFront.Value && !alreadyBrougthToFront)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            Activate();
                            Focus();
                            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                            //SetForegroundWindow(windowHandle);
                        }));
                        //this.BeginInvoke((MethodInvoker)delegate ()
                        //{
                        
                        //});
                    }
                    if (!GetInputFromUser.KeepInFront.Value)
                        alreadyBrougthToFront = true;
                }
                catch (Exception)
                {

                }
                timer.Start();
            };
            timer.Start();

            Show();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            FinishSuccessfully();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            GetInputFromUser.Completed = true;
            Close();
        }

        private void FinishSuccessfully()
        {
            timer.Stop();
            GetInputFromUser.Successful = true;
            if (GetInputFromUser.InputNeeded.Value)
            {
                GetInputFromUser.UserInputResult = TextBoxUserInput.Text;
                GetInputFromUser.ReturnedOutput.Add(
                    new KeyValuePairEx<string, string>(GetInputFromUser.KeyList.UserInputResult.ToString(),
                        TextBoxUserInput.Text));
            }
            GetInputFromUser.Completed = true;
            Close();
        }

        private void TextBoxUserInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                FinishSuccessfully();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CefBrowser.BrowserAction;
using CefBrowser.Gateway;
using CefBrowser.Handler;
using CefBrowserControl;
using CefBrowserControl.BrowserActions.Elements;
using CefBrowserControl.BrowserActions.Helper;
using CefBrowserControl.BrowserCommands;
using CefBrowserControl.Conversion;
using CefSharp;
using Timer = System.Timers.Timer;

namespace CefBrowser
{
    /// <summary>
    ///     Interaktionslogik für WebBrowser.xaml
    /// </summary>
    public partial class WebBrowserEx
    {
        public static string RcpServerName = "CefBAAS-1";

        private static readonly BrowsingState _browsingState = new BrowsingState();

        public readonly RequestHandler RequestHandler;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        public readonly DisplayHandler DisplayHandler;
        public readonly DialogHandler DialogHandler;
        public readonly JsDialogHandler JsDialogHandler;
        public static bool ready = false;

        public static BrowserActionManager ActionManager;
        public static Gateway.Gateway ActionBrowserGateway;

        public static ReaderWriterLock MessageLock= new ReaderWriterLock() , CefListLock = new ReaderWriterLock(), TimerMessageHandlingLock = new ReaderWriterLock(), TimerCefBrowserLock = new ReaderWriterLock();

        public static Dictionary<string, object> BrowserCommandsCompleted = new Dictionary<string, object>(),
            BrowserActionsCompleted = new Dictionary<string, object>(),
            BrowserCommandsList = new Dictionary<string, object>(),
            BrowserActionsList = new Dictionary<string, object>(),
            BrowserActionsInTransit = new Dictionary<string, object>();

        public static List<KeyValuePairEx<string, string>> MessagesPending = new List<KeyValuePairEx<string, string>>();
        public static List<string> MessagesReceived = new List<string>();

        private static RpcReaderWriter _rpcReaderWriter;
        private static Thread rpcReaderThread;

        //messagehandling puts from messagelist into commandslist
        //these commands get pulled from browserhandling
        //which executes them and puts it finally back into completed
        private static Timer _timerMessageHandling, _timerBrowserHandling;

        private static string UID = "";

        public WebBrowserEx()
        {
            InitializeComponent();
            //DO NOT USE BROWSER GATEWAY WITHOUT BROWSERACTIONMANAGER!!!!!
            ActionBrowserGateway = new Gateway.Gateway(this);
            ActionManager = new BrowserActionManager(ActionBrowserGateway);

            testvas.Background = null;
            CefSettings settings = new CefSettings();
            
            settings.SetOffScreenRenderingBestPerformanceArgs();
            //settings.CefCommandLineArgs.Add("disable-gpu", "1");
            //settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1");
            //settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");
            //settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1"); //Disable Vsync
            if (!Cef.IsInitialized)
                Cef.Initialize(settings);
            //if (!Cef.IsInitialized)
            //    Cef.Initialize();
            Browser.LoadingStateChanged += BrowserOnLoadingStateChanged;
            Browser.FrameLoadStart += BrowserOnFrameLoadStart;
            Browser.FrameLoadEnd += BrowserOnFrameLoadEnd;
            //_dialogHandler = new DialogHandler();
            //Browser.DialogHandler = _dialogHandler;
            //Cef.EnableHighDPISupport();
            RequestHandler = new RequestHandler();
            Browser.RequestHandler = RequestHandler;
            DisplayHandler = new DisplayHandler(this);
            Browser.DisplayHandler = DisplayHandler;
            JsDialogHandler = new JsDialogHandler();
            Browser.JsDialogHandler = JsDialogHandler;
            Browser.IsHitTestVisible = true;
            //Browser.LoadHtml("<h1>This was a triumph. I'm making a note here: huge success.</h1>", "http://localhost");

        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //ScrollFactoring scrollSettings = ScrollFactoring.Default;
            //if ((int)ActualWidth != scrollSettings.lastWidth || (int)ActualHeight != scrollSettings.lastHeight)
            //{

            //    BrowserAction.BrowserAction action = new BrowserAction.BrowserAction();
            //    action.ActionObject = new SiteLoaded();
            //    action.ActionType = BrowserAction.BrowserAction.Action.SiteLoaded;
            //    action.ActionFinishedEventHandler += ActionOnActionFinishedEventHandler;
            //    ActionManager.AddBrowserActions(action);
            //    ActionManager.ActionsEnabled = true;
            //    string testingWindow = "<body style=\"background-color:black;\"><div style=\"width=" + ActualWidth * 2 + ";height=" + ActualHeight * 2 + "\"></div></body>";
            //    Browser.WebBrowser.LoadHtml(testingWindow, "testing.local", CefEncoding.UTF8);
            //    //Browser.LoadString(testingWindow, "testing.local");
            //}

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                UID = args[1];

                if (_rpcReaderWriter == null || !(rpcReaderThread != null && rpcReaderThread.IsAlive))
                {
                    _rpcReaderWriter =
                        new RpcReaderWriter(MessagesPending, MessagesReceived, MessageLock, UID,
                            RcpServerName);
                    rpcReaderThread = new Thread(new ThreadStart(_rpcReaderWriter.Listen));
                    rpcReaderThread.Start();
                }

                _timerBrowserHandling = new Timer();
                _timerBrowserHandling.Interval = 100;
                _timerBrowserHandling.AutoReset = false;
                _timerBrowserHandling.Elapsed += _timerBrowserHandling_Elapsed;
                _timerBrowserHandling.Start();

                _timerMessageHandling = new Timer();
                _timerMessageHandling.Interval = 100;
                _timerMessageHandling.Elapsed += _timerMessageHandling_Elapsed;
                _timerMessageHandling.AutoReset = false;
                _timerMessageHandling.Start();

              

                ActionManager.ActionsEnabled = true;
            }
            else
            {
                Browser.Load(Options.DefaultUrl);
            }
        }

        private void _timerBrowserHandling_Elapsed(object sender, ElapsedEventArgs e)
        {
                _timerBrowserHandling.Stop();
            try
            {
                TimerCefBrowserLock.AcquireWriterLock(Options.LockTimeOut);
                try
                {
                    CefListLock.AcquireWriterLock(Options.LockTimeOut);
                    try
                    {
                        List<string> forRemoving = new List<string>();
                        foreach (var ucidToBrowsercommand in BrowserCommandsList)
                        {
                            try
                            {
                                string commandType = ucidToBrowsercommand.Value.GetType().Name;
                                //Console.WriteLine(commandType);
                                switch (commandType)
                                {
                                    case "Open":
                                        Open open =
                                            (Open) ucidToBrowsercommand.Value;
                                        open.Successful = true;
                                        open.Completed = true;
                                        forRemoving.Add(open.UCID);
                                        break;
                                    case "SwitchWindowVisibility":
                                        SwitchWindowVisibility visibility =
                                            (SwitchWindowVisibility) ucidToBrowsercommand.Value;
                                        Dispatcher.Invoke(() =>
                                        {
                                            Visibility = visibility.Visible.Value ? Visibility.Visible : Visibility.Hidden;
                                            ShowInTaskbar = visibility.Visible.Value;

                                        });
                                        visibility.Successful = true;
                                        visibility.Completed = true;
                                        forRemoving.Add(visibility.UCID);
                                        break;
                                    case "LoadUrl":
                                        LoadUrl loadUrl = (LoadUrl) ucidToBrowsercommand.Value;
                                        Dispatcher.Invoke(() =>
                                        {
                                            Browser.Load(loadUrl.Url.Value);
                                            Title = loadUrl.Url.Value;
                                        });
                                        loadUrl.Successful = true;
                                        loadUrl.Completed = true;
                                        forRemoving.Add(loadUrl.UCID);
                                        break;
                                    case "SwitchUserInputEnabling":
                                        SwitchUserInputEnabling enabling =
                                            (SwitchUserInputEnabling) ucidToBrowsercommand.Value;
                                        throw new Exception("This method is not fully implemented by CefBrowser");
                                        // ReSharper disable once HeuristicUnreachableCode
                                        visibility.Completed = true;
                                        forRemoving.Add(visibility.UCID);
                                        break;
                                    default:
                                        throw new Exception(
                                            "This browser command method is not implemented by CefBrowser");
                                }
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandling.Handling.GetException("Unexpected", ex);

                            }
                        }
                        foreach (string ucid in forRemoving)
                        {
                            BrowserCommandsCompleted.Add(ucid, BrowserCommandsList[ucid]);
                            BrowserCommandsList.Remove(ucid);
                        }
                        //----BrowserActions
                        forRemoving.Clear();
                        foreach (var ucidToBrowserAction in BrowserActionsList)
                        {
                            try
                            {
                                CefBrowserControl.BrowserAction browserAction =
                                    (CefBrowserControl.BrowserAction) ucidToBrowserAction.Value;
                                ActionManager.AddBrowserActions(browserAction);
                                forRemoving.Add(ucidToBrowserAction.Key);
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandling.Handling.GetException("Unexpected", ex);

                            }
                        }
                        foreach (string ucid in forRemoving)
                        {
                            //BrowserActionsCompleted.Add(ucid, BrowserActionsCompleted[ucid]);
                            //BrowserActionsCompleted.Remove(ucid);
                            BrowserActionsInTransit.Add(ucid, BrowserActionsList[ucid]);
                            BrowserActionsList.Remove(ucid);
                        }
                        //get all completed actions from actionmanager
                        forRemoving.Clear();
                        if (BrowserActionManager.BrowserActionsCompleted.Count > 0)
                        {
                            for (object obj = ActionManager.GetCompletedBrowserAction();
                                obj != null;
                                obj = ActionManager.GetCompletedBrowserAction())
                            {
                                CefBrowserControl.BrowserAction browserAction = (CefBrowserControl.BrowserAction) obj;
                                forRemoving.Add(browserAction.UCID);
                            }
                            foreach (string ucid in forRemoving)
                            {
                                BrowserActionsCompleted.Add(ucid, BrowserActionsInTransit[ucid]);
                                BrowserActionsInTransit.Remove(ucid);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandling.Handling.GetException("Unexpected", ex);

                    }

                }
                catch (ApplicationException ex1)
                {
                    ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);
                }
                finally
                {
                    if (CefListLock.IsWriterLockHeld)
                    {
                        CefListLock.ReleaseWriterLock();
                    }
                }
            }
            catch (ApplicationException ex1)
            {
                ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);
            }
            finally
            {
                if (TimerCefBrowserLock.IsWriterLockHeld)
                {
                    TimerCefBrowserLock.ReleaseWriterLock();
                }
                _timerBrowserHandling.Start();
            }
        }

        private void _timerMessageHandling_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timerMessageHandling.Stop();
            try
            {
                TimerMessageHandlingLock.AcquireWriterLock(Options.LockTimeOut);
                try
                {
                    MessageLock.AcquireWriterLock(Options.LockTimeOut);
                    try
                    {
                        if (MessagesReceived.Count > 0)
                        {
                            try
                            {
                                CefListLock.AcquireWriterLock(Options.LockTimeOut);
                                foreach (var message in MessagesReceived)
                                {
                                    //DEBUG
                                    //Console.WriteLine(message);
                                    string plain = EncodingEx.Base64.Decoder.DecodeString(Encoding.UTF8, message);
                                    try
                                    {
                                        CefDecodeResult cefDecodeResult = CefDecoding.Decode(plain);
                                        if (cefDecodeResult.DecodedObject is CefBrowserControl.BrowserAction)
                                        {
                                            BrowserActionsList.Add(cefDecodeResult.UCID, cefDecodeResult.DecodedObject);
                                        }
                                        else if (cefDecodeResult.DecodedObject is BrowserCommand)
                                        {
                                            BrowserCommandsList.Add(cefDecodeResult.UCID, cefDecodeResult.DecodedObject);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ExceptionHandling.Handling.GetException("Unexpected", ex);

                                    }
                                }
                                MessagesReceived.Clear();
                            }
                            catch (ApplicationException ex1)
                            {
                                ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);
                            }
                            finally
                            {
                                if (CefListLock.IsWriterLockHeld)
                                    CefListLock.ReleaseWriterLock();
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        ExceptionHandling.Handling.GetException("Unexpected", ex);

                    }
                }
                catch (ApplicationException ex1)
                {
                    ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);

                }
                finally
                {
                    if (MessageLock.IsWriterLockHeld)
                        MessageLock.ReleaseWriterLock();
                }
                try
                {
                    CefListLock.AcquireWriterLock(Options.LockTimeOut);
                    try
                    {
                        if (BrowserCommandsCompleted.Count > 0)
                        {
                            try
                            {
                                MessageLock.AcquireWriterLock(Options.LockTimeOut);
                                try
                                {
                                    foreach (var ucidToCommand in BrowserCommandsCompleted)
                                    {
                                        BrowserCommand cmd = (BrowserCommand) ucidToCommand.Value;
                                        MessagesPending.Add(new KeyValuePairEx<string, string>(RcpServerName,
                                            CefEncoding.Encode(cmd.UCID, cmd)));
                                    }
                                    BrowserCommandsCompleted.Clear();
                                }
                                catch (Exception ex)
                                {
                                    ExceptionHandling.Handling.GetException("Unexpected", ex);

                                }
                            }
                            catch (ApplicationException ex1)
                            {
                                ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);
                            }
                            finally
                            {
                                if (MessageLock.IsWriterLockHeld)
                                    MessageLock.ReleaseWriterLock();
                            }
                        }
                        if (BrowserActionsCompleted.Count > 0)
                        {
                            try
                            {
                                MessageLock.AcquireWriterLock(Options.LockTimeOut);
                                try
                                {
                                    foreach (var ucidToAction in BrowserActionsCompleted)
                                    {
                                        CefBrowserControl.BrowserAction action = (CefBrowserControl.BrowserAction)ucidToAction.Value;
                                        MessagesPending.Add(new KeyValuePairEx<string, string>(RcpServerName,
                                            CefEncoding.Encode(action.UCID, action)));
                                    }
                                    BrowserActionsCompleted.Clear();
                                }
                                catch (Exception ex)
                                {
                                    ExceptionHandling.Handling.GetException("Unexpected", ex);

                                }
                            }
                            catch (ApplicationException ex1)
                            {
                                ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);
                            }
                            finally
                            {
                                if (MessageLock.IsWriterLockHeld)
                                    MessageLock.ReleaseWriterLock();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandling.Handling.GetException("Unexpected", ex);

                    }
                }
                catch (ApplicationException ex1)
                {
                    ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);

                }
                finally
                {
                    if (CefListLock.IsWriterLockHeld)
                    {
                        CefListLock.ReleaseWriterLock();
                    }
                }

            }
            catch (ApplicationException ex1)
            {
                ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);

            }
            finally
            {
                if (TimerMessageHandlingLock.IsWriterLockHeld)
                {
                    TimerMessageHandlingLock.ReleaseWriterLock();
                }
            }
            _timerMessageHandling.Start();
        }

        private void BrowserOnFrameLoadEnd(object sender, FrameLoadEndEventArgs args)
        {
            foreach (FrameLoadingState existingState in _browsingState.FrameLoadingStates)
            {
                if (existingState.FrameName == args.Frame.Name )
                {
                    if (existingState.IsLoading)
                        existingState.IsLoading = false;
                    if (existingState.Address != args.Url)
                        existingState.Address = args.Url;
                    return;
                }
            }
            var state = new FrameLoadingState()
            {
                FrameName = args.Frame.Name,
                IsLoading = false,
                IsMainFrame = args.Frame.IsMain,
                Address = args.Url,

            };
            _browsingState.FrameLoadingStates.Add(state);
            Browser.ZoomLevel = 1;

        }

        private void BrowserOnFrameLoadStart(object sender, FrameLoadStartEventArgs args)
        {
            if (
                !_browsingState.FrameLoadingStates.Contains(new FrameLoadingState()
                {
                    FrameName = args.Frame.Name
                }))
            {
                var state = new FrameLoadingState()
                {
                    FrameName = args.Frame.Name,
                    IsLoading = true,
                    IsMainFrame = args.Frame.IsMain,
                    Address = args.Url,
                    
                };
                _browsingState.FrameLoadingStates.Add(state);
            }

        }

        private void BrowserOnLoadingStateChanged(object sender, LoadingStateChangedEventArgs loadingStateChangedEventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                Browser.ZoomLevel = 1;

            });
            if (loadingStateChangedEventArgs.IsLoading != _browsingState.IsLoading)
                _browsingState.IsLoading = loadingStateChangedEventArgs.IsLoading;

            IFrame mainFrame = loadingStateChangedEventArgs.Browser.MainFrame;
            if (mainFrame?.Url != null)
                if (_browsingState.Address != mainFrame.Url)
                    _browsingState.Address = mainFrame.Url;
        }
        
        public bool ExecutingBrowserActions { get; private set; }

        public BrowsingState BrowsingState
        {
            get { return _browsingState; }
        }

        public bool Ready
        {
            get { return ready; }
        }


        public static List<object> ConvertObjectToObjectList(object o)
        {
            List<object> elementsToLoad;
            if (!(o is IList && o.GetType().IsGenericType))
            {
                elementsToLoad = new List<object> {o};
            }
            else
                elementsToLoad = (List<object>)o;
            return elementsToLoad;
        }

        



        private void ActionOnActionFinishedEventHandler(object sender, EventArgs eventArgs)
        {
            MessageBox.Show("seas");

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(ActionManager != null)
            ActionManager.ActionsEnabled = false;
            if(Browser != null)
            Browser.Stop();
            Cef.Shutdown();

            Application.Current.Shutdown();

        }
    }

    public class NoSelectionStringSpecified : Exception
    {
        public NoSelectionStringSpecified(string message) : base(message)
        {
        }
    }

    public class NoSelectionTypeSpecified : Exception
    {
        public NoSelectionTypeSpecified(string message) : base(message)
        {
        }
    }

    public class CannotGetObjectLocation : Exception
    {
        public CannotGetObjectLocation(string message) : base(message)
        {
            
        }
    }

    public class CannotGetScrollOffset : Exception
    {
        public CannotGetScrollOffset(string message): base(message)
        {
            
        }
    }
}
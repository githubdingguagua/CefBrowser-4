//Multiple App Domains
//https://stackoverflow.com/questions/3413807/how-can-i-run-a-wpf-application-in-a-new-appdomain-executeassembly-fails#3414124
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CefBrowserControl;
using CefBrowserControl.BrowserCommands;
using CefBrowserControl.Conversion;
using Timer = System.Timers.Timer;

namespace CefBrowser
{
    public class ControlInterface
    {
        private Timer processingTimerFromBridge, processingTimerToCefBrowser;
        private int intervalProcessingTimer = 100;

        public static ReaderWriterLock BridgeListsLock, CefBrowserListsLock = new ReaderWriterLock();

        public static Dictionary<string, object> BrowserCommandsInTransit,
            BrowserActionsInTransit,
            BrowserCommandsCompleted,
            BrowserActionsCompleted;

        private static Dictionary<string, Dictionary<string, object>> _browserCommandsList = new Dictionary<string, Dictionary<string, object>>(),
            _browserActionsList = new Dictionary<string, Dictionary<string, object>>();

        public static ReaderWriterLock MessagesLock = new ReaderWriterLock();
        public static Dictionary<string, string> PendingMessagesList;

        private static RpcReaderWriter _rpcReaderWriter;

        public ControlInterface(ReaderWriterLock bridgeListsLock, Dictionary<string, object> browserCommandsInTransit,
            Dictionary<string, object> browserActionsInTransit, Dictionary<string, object> browserCommandsCompleted,
            Dictionary<string, object> browserActionsCompleted, RpcReaderWriter rpcReaderWriter, Dictionary<string, string> pendingMessagesList)
        {
            _rpcReaderWriter = rpcReaderWriter;
            PendingMessagesList = pendingMessagesList;

            BridgeListsLock = bridgeListsLock;
            BrowserCommandsInTransit = browserCommandsInTransit;
            BrowserActionsInTransit = browserActionsInTransit;
            BrowserCommandsCompleted = browserCommandsCompleted;
            BrowserActionsCompleted = browserActionsCompleted;

            processingTimerFromBridge = new Timer();
            processingTimerFromBridge.Interval = intervalProcessingTimer;
            processingTimerFromBridge.Elapsed += ProcessingTimerFromBridgeElapsed;
            processingTimerFromBridge.Enabled = true;

            processingTimerToCefBrowser = new Timer();
            processingTimerToCefBrowser.Interval = intervalProcessingTimer;
            processingTimerToCefBrowser.Elapsed += ProcessingTimerToCefBrowser_Elapsed;
            processingTimerToCefBrowser.Enabled = true;

        }

        private static Dictionary<string, Process> CefBrowserSessions = new Dictionary<string, Process>();

        private void ProcessingTimerToCefBrowser_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            processingTimerToCefBrowser.Stop();
            try
            {
                CefBrowserListsLock.AcquireWriterLock(Options.LockTimeOut);

                Dictionary<string, string> forDeletion = new Dictionary<string, string>();
                //each browser session
                foreach (KeyValuePair<string, Dictionary<string, object>> browserCommandsUcidToObjectList in _browserCommandsList)
                {
                    string uid = browserCommandsUcidToObjectList.Key;
                    //commands for each browser [
                    foreach (KeyValuePair<string, object> browserUcidToCommand in browserCommandsUcidToObjectList.Value)
                    {

                        string commandType = browserUcidToCommand.Value.GetType().ToString();
                        //Console.WriteLine(browserUcidToCommand.ExpectedValue.GetType().ToString());
                        switch (commandType)
                        {
                            case "CefBrowserControl.BrowserCommands.Open":
                                Open open = (Open)browserUcidToCommand.Value;
                                forDeletion.Add(open.UID, open.UCID);
                                if (!CefBrowserSessions.ContainsKey(open.UID))
                                {
                                    //Thread thread = new Thread(startBrowser);
                                    //thread.AttributeName = open.UID;
                                    //thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                                    //thread.Start();
                                    ////Todo: Clean Shutdown
                                    ////thread.Join(); //Wait for the thread to end
                                    Process p = Process.Start("CefBrowser.exe", open.UID + " --debug");

                                    CefBrowserSessions.Add(open.UID, p);
                                    //TODO: Add code that checks thread
                                    open.Successful = true;
                                }
                                while (true)
                                {
                                    try
                                    {
                                        MessagesLock.AcquireWriterLock(Options.LockTimeOut);
                                        _rpcReaderWriter.AddClient(open.UID);
                                        break;
                                    }
                                    catch (ApplicationException ex1)
                                    {
                                        ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);
                                    }
                                    finally
                                    {
                                        if (MessagesLock.IsWriterLockHeld)
                                            MessagesLock.ReleaseWriterLock();
                                    }
                                }
                                open.Completed = true;
                                break;
                        }
                    }
                }
                foreach (KeyValuePair<string, string> uidAndUcid in forDeletion)
                {
                    object obj = _browserCommandsList[uidAndUcid.Key][uidAndUcid.Value];
                    BrowserCommandsCompleted.Add(uidAndUcid.Key, obj);
                    _browserCommandsList[uidAndUcid.Key].Remove(uidAndUcid.Value);
                }
                try
                {
                    MessagesLock.AcquireWriterLock(Options.LockTimeOut);
                    try
                    {
                        //Send rest of pending commands and actions to cef
                        foreach (KeyValuePair<string, Dictionary<string, object>> browserCommandsUcidToObjectList in _browserCommandsList)
                        {
                            string uid = browserCommandsUcidToObjectList.Key; //=client for rcp
                                                                              //commands for each browser [
                            foreach (KeyValuePair<string, object> browserUcidToCommand in browserCommandsUcidToObjectList.Value)
                            {
                                PendingMessagesList.Add(uid, CefEncoding.Encode(browserUcidToCommand.Key, browserUcidToCommand.Value));
                            }
                        }
                        foreach (var uidToCommand in _browserCommandsList)
                        {
                            uidToCommand.Value.Clear();
                        }
                        foreach (KeyValuePair<string, Dictionary<string, object>> browserActionsUcidToObjectList in _browserActionsList)
                        {
                            string uid = browserActionsUcidToObjectList.Key; //=client for rcp
                                                                             //commands for each browser [
                            foreach (KeyValuePair<string, object> browserUcidToAction in browserActionsUcidToObjectList.Value)
                            {
                                PendingMessagesList.Add(uid, CefEncoding.Encode(browserUcidToAction.Key, browserUcidToAction.Value));
                            }
                        }
                        foreach (var uidToAction in _browserActionsList)
                        {
                            uidToAction.Value.Clear();
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
                    if (MessagesLock.IsWriterLockHeld)
                        MessagesLock.ReleaseWriterLock();
                }
            }
            catch (ApplicationException ex1)
            {
                ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);
            }
            finally
            {
                if (CefBrowserListsLock.IsWriterLockHeld)
                    CefBrowserListsLock.ReleaseWriterLock();
            }
            processingTimerToCefBrowser.Start();
        }

        private void ProcessingTimerFromBridgeElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            processingTimerFromBridge.Stop();

            //Get Elements from CefBridge to CefBrowser
            try
            {
                BridgeListsLock.AcquireWriterLock(Options.LockTimeOut);
                try
                {
                    if (BrowserActionsInTransit.Count > 0 || BrowserCommandsInTransit.Count > 0)
                    {
                        try
                        {
                            CefBrowserListsLock.AcquireWriterLock(Options.LockTimeOut);
                            try
                            {
                                foreach (var ucidToBrowsercommand in BrowserCommandsInTransit)
                                {
                                    BrowserCommand browserCommand = (BrowserCommand) ucidToBrowsercommand.Value;
                                    if (!_browserCommandsList.ContainsKey(browserCommand.UID))
                                        _browserCommandsList.Add(browserCommand.UID, new Dictionary<string, object>());
                                    _browserCommandsList[browserCommand.UID].Add(browserCommand.UCID, ucidToBrowsercommand.Value);
                                }
                                BrowserCommandsInTransit.Clear();
                                foreach (var ucidTobrowserAction in BrowserActionsInTransit)
                                {
                                    CefBrowserControl.BrowserAction browserAction = (CefBrowserControl.BrowserAction)ucidTobrowserAction.Value;
                                    if (!_browserActionsList.ContainsKey(browserAction.UID))
                                        _browserActionsList.Add(browserAction.UID, new Dictionary<string, object>());
                                    _browserActionsList[browserAction.UID].Add(browserAction.UCID, ucidTobrowserAction.Value);
                                }
                                BrowserCommandsInTransit.Clear();
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
                            if (CefBrowserListsLock.IsWriterLockHeld)
                                CefBrowserListsLock.ReleaseWriterLock();
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
                if (BridgeListsLock.IsWriterLockHeld)
                    BridgeListsLock.ReleaseWriterLock();
            }
            processingTimerFromBridge.Start();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using CSharpTest.Net.RpcLibrary;
using RPC_Communication;

namespace CefBrowserControl
{
    public class RpcReaderWriter
    {
        private static int maxTries = 3;
        private static int timeAfterFailForRetry = 2000;

        public static Server Server;
        private static System.Timers.Timer _timer;

        private static int standardTimeout = 100;

        private static List<KeyValuePairEx<string, string>> messagesPending;
        private static List<string> messagesReceived;
        private static ReaderWriterLock messagesLock;

        private static Dictionary<string, Client> otherRcpServers = new Dictionary<string, Client>();

        //SELF
        private string ThisRcpServerName;

        //CLIENT RPC = SELF FOR OTHERS
        //SERVER RPC = OTHERS FOR CLIENT
        //Single Server only
        public RpcReaderWriter(List<KeyValuePairEx<string, string>> pending, List<string> received, ReaderWriterLock listsLock, string thisRcpServerName, string otherRcpServer)
        {
            ThisRcpServerName = thisRcpServerName;

            messagesPending = pending;
            messagesReceived = received;
            messagesLock = listsLock;

            otherRcpServers.Add(otherRcpServer, new Client(otherRcpServer));
        }

        public RpcReaderWriter(List<KeyValuePairEx<string, string>> pending, List<string> received, ReaderWriterLock listsLock, string thisRcpServerName)
        {
            ThisRcpServerName = thisRcpServerName;

            messagesPending = pending;
            messagesReceived = received;
            messagesLock = listsLock;
        }

        public void AddClient(string serverRcpName)
        {
            while (true)
            {
                try
                {
                    //messagesLock.AcquireWriterLock(UnifiedLockTimeout);
                    otherRcpServers.Add(serverRcpName, new Client(serverRcpName));
                    //messagesLock.ReleaseLock();
                    break;
                }
                catch (ApplicationException ex1)
                {
                    ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);

                }
            }
        }

        public void RemoveClient(string serverRcpName)
        {
            while (true)
            {
                try
                {
                    //messagesLock.AcquireWriterLock(UnifiedLockTimeout);
                    otherRcpServers.Remove(serverRcpName);
                    //messagesLock.ReleaseLock();
                    break;
                }
                catch (ApplicationException ex1)
                {
                                            ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);
                }
            }
        }

        public void Listen()
        {
            Server = new Server(ThisRcpServerName);

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = standardTimeout;
            _timer.Start();

            Server.Listen();
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();

            try
            {
                messagesLock.AcquireWriterLock(Options.LockTimeOut);
                try
                {
                    foreach (var ClientIdTomessage in messagesPending)
                    {
                        if (otherRcpServers.ContainsKey(ClientIdTomessage.Key))
                        {
                            while (true)
                            {
                                try
                                {
                                    otherRcpServers[ClientIdTomessage.Key].Send(ClientIdTomessage.Value);
                                    break;
                                }
                                catch (RpcException exception)
                                {
                                    if (exception.RpcError == RpcError.RPC_S_SERVER_UNAVAILABLE)
                                    {
                                        Thread.Sleep(500);
                                    }
                                    else if (exception.RpcError == RpcError.RPC_S_SERVER_TOO_BUSY)
                                    {
                                        Thread.Sleep(1000);
                                    }
                                    else
                                    {
                                        ExceptionHandling.Handling.GetException("Unexpected", exception);

                                    }
                                }
                                catch (Exception ex)
                                {
                                    ExceptionHandling.Handling.GetException("Unexpected", ex);
                                }
                            }
                            //for (int tries = 0; tries < maxTries || 1 == 1; tries++)
                            //{
                            //    try
                            //    {
                            //        otherRcpServers[ClientIdTomessage.Key].Send(ClientIdTomessage.ExpectedValue);
                            //        break;
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        string name = ex.GetType().AttributeName;
                            //        //TODO REACTIVATE
                            //        //if (name == "RpcException" && tries < 2)
                            //        {
                            //            Thread.Sleep(timeAfterFailForRetry);
                            //        }
                            //        //TODO: TimeOutIfWindowDoenstStart?
                            //        //else
                            //        //{
                            //        //    throw new Exception(ex.Message);
                            //        //}
                            //    }
                            //}
                        }
                        else
                        {
                            ExceptionHandling.Handling.GetException("Unexpected", new Exception("w000t"));
                        }
                    }
                    messagesPending.Clear();
                }
                catch (Exception ex)
                {
                    //if (ex.GetType().AttributeName == "CSharpTest.Net.RpcLibrary.RpcException" && maxTries-- > 0)
                    //{
                    //    Thread.Sleep(timeAfterFailForRetry);

                    //}
                    ExceptionHandling.Handling.GetException("Unexpected", ex);

                }
            }
            catch (ApplicationException ex1)
            {
                ExceptionHandling.Handling.GetException("ReaderWriterLock", ex1);
            }
            finally
            {
                if(messagesLock.IsWriterLockHeld)
                    messagesLock.ReleaseWriterLock();
            }

            try
            {
                Server.Lock.AcquireWriterLock(Options.LockTimeOut);
                try
                {
                    if (Server.MessageList.Count > 0)
                    {
                        try
                        {
                            messagesLock.AcquireWriterLock(Options.LockTimeOut);
                            try
                            {
                                foreach (var message in Server.MessageList)
                                    messagesReceived.Add(message);
                                Server.MessageList.Clear();
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
                            if (messagesLock.IsWriterLockHeld)
                                messagesLock.ReleaseWriterLock();
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
                if (Server.Lock.IsWriterLockHeld)
                    Server.Lock.ReleaseWriterLock();
            }
            _timer.Start();
        }
    }
}

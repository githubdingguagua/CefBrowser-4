using System;
using System.Collections.Generic;
using System.Threading;

namespace CefBrowser.Handler
{
    public class BrowsingState
    {
        public ReaderWriterLockSlim LockSlim = new ReaderWriterLockSlim();

        private readonly List<AutoResetEvent> _frameWaitHandles = new List<AutoResetEvent>();
        public List<AutoResetEvent> FrameWaitHandles
        {
            get
            {
                LockSlim.EnterWriteLock();
                try
                {
                    return _frameWaitHandles;
                }
                finally
                {
                    LockSlim.ExitWriteLock();
                }
            }
        }

        private readonly List<AutoResetEvent> _addressWaitHandles = new List<AutoResetEvent>();
        public List<AutoResetEvent> AddressWaitHandles
        {
            get
            {
                LockSlim.EnterWriteLock();
                try
                {
                    return _addressWaitHandles;
                }
                finally
                {
                    LockSlim.ExitWriteLock();
                }
            }
        }

        private readonly List<AutoResetEvent> _loadingWaitHandles = new List<AutoResetEvent>();
        public List<AutoResetEvent> LoadingWaitHandles
        {
            get
            {
                LockSlim.EnterWriteLock();
                try
                {
                    return _loadingWaitHandles;
                }
                finally
                {
                    foreach (AutoResetEvent waitHandle in _loadingWaitHandles)
                    {
                        waitHandle.Set();
                    }
                    LockSlim.ExitWriteLock();
                }
            }
        }

        private string _address;
        public string Address
        {
            get
            {
                LockSlim.EnterReadLock();
                try
                {
                    return _address;
                }
                finally
                {
                    LockSlim.ExitReadLock();
                }
            }
            set
            {
                LockSlim.EnterWriteLock();
                try
                {
                    _address = value;
                }
                finally
                {
                    foreach (AutoResetEvent waitHandle in _addressWaitHandles)
                    {
                        waitHandle.Set();
                    }
                    LockSlim.ExitWriteLock();
                }
            } 
        }

        private bool _isLoading;

        public bool IsLoading
        {
            get
            {
                LockSlim.EnterReadLock();
                try
                {
                    return _isLoading;
                }
                finally
                {
                    LockSlim.ExitReadLock();
                }
            }
            set
            {
                LockSlim.EnterWriteLock();
                try
                {
                    _isLoading = value;
                }
                finally
                {
                    foreach (AutoResetEvent waitHandle in _loadingWaitHandles)
                    {
                        waitHandle.Set();
                    }
                    LockSlim.ExitWriteLock();
                }
            }
        }

        private readonly List<FrameLoadingState> _frameLoadingStates = new List<FrameLoadingState>();

        public List<FrameLoadingState> FrameLoadingStates
        {
            get
            {
                LockSlim.EnterWriteLock();
                try
                {
                    return _frameLoadingStates;
                }
                finally
                {
                    foreach (AutoResetEvent waitHandle in _frameWaitHandles)
                    {
                        waitHandle.Set();
                    }
                    LockSlim.ExitWriteLock();
                }
            }
        }
    }

    public class FrameLoadingState : IEquatable<FrameLoadingState>
    {
        readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
        private string _frameName;
        private bool _isMainFrame;
        private bool _isLoading;
        private string _address;

        public string FrameName
        {
            get
            {
                    return _frameName;
            }
            set
            {
                _lockSlim.EnterWriteLock();
                try
                {
                    _frameName = value;
                }
                finally
                {
                    _lockSlim.ExitWriteLock();
                }
            }
        }

        public bool IsMainFrame
        {
            get
            {
                    return _isMainFrame;
            }
            set
            {
                _lockSlim.EnterWriteLock();
                try
                {
                    _isMainFrame = value;
                }
                finally
                {
                    _lockSlim.ExitWriteLock();
                }
            }
        }

        public bool IsLoading
        {
            get
            {
                _lockSlim.EnterReadLock();
                try
                {

                    return _isLoading;
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }
            }
            set
            {
                _lockSlim.EnterWriteLock();
                try
                {
                    _isLoading = value;
                }
                finally
                {
                    _lockSlim.ExitWriteLock();
                }

            }
        }

        public string Address
        {
            get
            {
                _lockSlim.EnterReadLock();
                try
                {
                    return _address;
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }

            }
            set
            {
                _lockSlim.EnterWriteLock();
                try
                {
                    _address = value;
                }
                finally
                {
                    _lockSlim.ExitWriteLock();
                }

            }
        }

        public override string ToString()
        {

            _lockSlim.EnterReadLock();
            try
            {
                return FrameName;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public bool Equals(FrameLoadingState obj)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (obj == null)
                    return false;
                return FrameName == obj.FrameName;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

        }
    }
}

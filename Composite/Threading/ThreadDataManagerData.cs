﻿using System.Threading;
using Composite.Collections.Generic;
using System;


namespace Composite.Threading
{
    public sealed class ThreadDataManagerData: IDisposable
    {
        private ThreadDataManagerData Parrent { get; set; }
        private Hashtable<object, object> Data { get; set; }
        private bool _disposed = false;


        public ThreadDataManagerData()
            : this(null)
        {
        }

        public delegate void OnThreadDataDisposedDelegate();

        public event ThreadStart OnDispose;

        public ThreadDataManagerData(ThreadDataManagerData parentThreadData)
        {
            this.Parrent = parentThreadData;
            this.Data = new Hashtable<object, object>();
        }



        /// <summary>
        /// This method will find the first one that contains the key and return the value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool TryGetParentValue(object key, out object value)
        {
            CheckNotDisposed();

            ThreadDataManagerData current = this;
            while ((current != null) && (current.Data.ContainsKey(key) == false))
            {
                current = current.Parrent;
            }

            if (current == null)
            {
                value = null;
                return false;
            }

            value = current.Data[key];
            return true;
        }



        public void SetValue(object key, object value)
        {
            CheckNotDisposed();

            if (this.Data.ContainsKey(key) == false)
            {
                this.Data.Add(key, value);
            }
            else
            {
                this.Data[key] = value;
            }
        }

        public object GetValue(object key)
        {
            CheckNotDisposed();

            return Data[key];
        }

        public bool HasValue(object key)
        {
            CheckNotDisposed();

            return this.Data.ContainsKey(key);
        }

        public object this[object key]
        {
            get { return GetValue(key); }
        }

        public void CheckNotDisposed()
        {
            if(_disposed) throw new ObjectDisposedException("TheadDataManagerData");
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (OnDispose != null)
            {
                OnDispose();
            }
            _disposed = true;
        }

        #endregion
    }
}

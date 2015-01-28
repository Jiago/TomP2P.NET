﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Netty
{
    public abstract class BaseChannel : IChannel
    {
        public event ClosedEventHandler Closed;

        protected Pipeline _pipeline;

        public void SetPipeline(Pipeline pipeline)
        {
            _pipeline = pipeline;
        }

        /// <summary>
        /// A Close() method that notfies the subscribed events.
        /// </summary>
        public void Close()
        {
            DoClose();
            OnClosed();
        }

        protected abstract void DoClose();

        protected void OnClosed()
        {
            if (Closed != null)
            {
                Closed(this);
            }
        }

        public abstract Socket Socket { get; }

        public Pipeline Pipeline
        {
            get { return _pipeline; }
        }

        public abstract bool IsUdp { get; }

        public abstract bool IsTcp { get; }
    }
}
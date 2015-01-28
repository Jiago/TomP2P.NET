﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace TomP2P.Connection.Windows.Netty
{
    /// <summary>
    /// Equivalent to Java Netty's ChannelPipeline. Represents a chain of inbound and outbound handlers.
    /// Only the required parts are implemented.
    /// </summary>
    public class Pipeline
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public delegate void PipelineEventHandler(Pipeline pipeline, byte[] bytes);
        public event PipelineEventHandler OutboundFinished;

        private readonly IDictionary<string, HandlerItem> _name2Item;
        private readonly LinkedList<HandlerItem> _handlers;

        private readonly IChannel _channel;
        private readonly ChannelHandlerContext _ctx;
        private IOutboundHandler _currentOutbound = null;

        public Pipeline(IChannel channel)
        {
            _name2Item = new Dictionary<string, HandlerItem>();
            _handlers = new LinkedList<HandlerItem>();

            _channel = channel;
            _ctx = new ChannelHandlerContext(channel, this);
        }

        public void Write(object msg) // TODO called from API and ctx, problematic?
        {
            if (msg == null)
            {
                throw new NullReferenceException("msg");
            }
            // find next outbound handler
            while (GetNextOutbound() != null)
            {
                Logger.Debug("Processing outbound handler '{0}'.", _currentOutbound);
                _currentOutbound.Write(_ctx, msg);
            }
            var bytes = ConnectionHelper.ExtractBytes(msg); // TODO check if correct
            OnOutboundFinished(bytes);
        }

        private IOutboundHandler GetNextOutbound()
        {
            var outbounds = CurrentOutboundHandlers;
            if (outbounds.Count != 0)
            {
                if (_currentOutbound == null)
                {
                    _currentOutbound = outbounds.First.Value;
                }
                else
                {
                    var node = outbounds.Find(_currentOutbound);
                    if (node != null && node.Next != null)
                    {
                        _currentOutbound = node.Next.Value;
                    }
                    return null;
                }
                return _currentOutbound;
            }
            return null;
        }

        protected void OnOutboundFinished(byte[] bytes)
        {
            if (OutboundFinished != null)
            {
                OutboundFinished(this, bytes);
            }
        }

        /// <summary>
        /// Inserts a handler at the first position of this pipeline.
        /// </summary>
        /// <returns></returns>
        public Pipeline AddFirst(string name, IChannelHandler handler)
        {
            CheckDuplicateName(name);
            var item = new HandlerItem(name, handler);
            _name2Item.Add(name, item);

            _handlers.AddFirst(item);
            return this;
        }

        /// <summary>
        /// Appends a handler at the last position of this pipeline.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Pipeline AddLast(string name, IChannelHandler handler)
        {
            CheckDuplicateName(name);
            var item = new HandlerItem(name, handler);
            _name2Item.Add(name, item);

            _handlers.AddLast(item);
            return this;
        }

        /// <summary>
        /// Inserts a handler before an existing handler of this pipeline.
        /// </summary>
        /// <param name="baseName"></param>
        /// <param name="name"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Pipeline AddBefore(string baseName, string name, IChannelHandler handler)
        {
            // TODO check if works correctly!
            HandlerItem baseItem;
            if (!_name2Item.TryGetValue(baseName, out baseItem))
            {
                throw new ArgumentException("The requested base item does not exist in this pipeline.");
            }
            
            CheckDuplicateName(name);
            var item = new HandlerItem(name, handler);
            _name2Item.Add(name, item);

            var nBase = new LinkedListNode<HandlerItem>(baseItem);
            var nNew = new LinkedListNode<HandlerItem>(item);

            _handlers.AddBefore(nBase, nNew);
            return this;
        }

        /// <summary>
        /// Replaces the specified handler with a new handler in this pipeline.
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <param name="newHandler"></param>
        /// <returns></returns>
        public Pipeline Replace(string oldName, string newName, IChannelHandler newHandler)
        {
            // TODO check if works correctly!
            HandlerItem oldItem;
            if (!_name2Item.TryGetValue(oldName, out oldItem))
            {
                throw new ArgumentException("The requested base item does not exist in this pipeline.");
            }
            CheckDuplicateName(newName);

            oldItem.Name = newName;
            oldItem.Handler = newHandler;
            return this;
        }

        private void CheckDuplicateName(string name)
        {
            if (_name2Item.ContainsKey(name))
            {
                throw new ArgumentException("Duplicate handler name: " + name);
            }
        }

        private LinkedList<IOutboundHandler> CurrentOutboundHandlers
        {
            // TODO check if works
            get
            {
                var outbounds = _handlers.
                    Select(item => item.Handler).
                    Where(handler => handler is IOutboundHandler).
                    Cast<IOutboundHandler>();
                return new LinkedList<IOutboundHandler>(outbounds);
            }
        }

        private struct HandlerItem
        {
            public string Name { get; set; }
            public IChannelHandler Handler { get; set; }

            public HandlerItem(string name, IChannelHandler handler) : this()
            {
                Name = name;
                Handler = handler;
            }
        }
    }
}
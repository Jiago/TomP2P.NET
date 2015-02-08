﻿using System;
using System.Threading;
using System.Threading.Tasks;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Connection
{
    public class IdleStateHandlerTomP2P : BaseChannelHandler, IDuplexHandler
    {
        public int AllIdleTimeMillis { get; private set; }

        private VolatileLong _lastReadTime = new VolatileLong(0);

        private VolatileLong _lastWriteTime = new VolatileLong(0);

        private volatile Task _allIdleTimeoutTask;
        private volatile CancellationTokenSource _cts;

        private volatile int _state; // 0 - none, 1 - initialized, 2- destroyed

        /// <summary>
        /// Creates a new instance firing IdleStateEvents.
        /// </summary>
        /// <param name="allIdleTimeSeconds">An IdleStateEvent whose state is AllIdle will be triggered
        /// when neither read nor write was performed for the specified period of time.
        /// Specify 0 to disable.</param>
        public IdleStateHandlerTomP2P(int allIdleTimeSeconds)
        {
            if (allIdleTimeSeconds <= 0)
            {
                AllIdleTimeMillis = 0;
            }
            else
            {
                AllIdleTimeMillis = TimeSpan.FromSeconds(allIdleTimeSeconds).Milliseconds;
            }
        }

        public override void HandlerAdded(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.IsActive)
            {
                Initialize(ctx);
            }
        }

        public override void HandlerRemoved(ChannelHandlerContext ctx)
        {
            Destroy();
        }

        public override void ChannelActive(ChannelHandlerContext ctx)
        {
            Initialize(ctx);
        }

        public override void ChannelInactive(ChannelHandlerContext ctx)
        {
            Destroy();
        }

        public void Read(ChannelHandlerContext ctx, object msg)
        {
            _lastReadTime.Set(Convenient.CurrentTimeMillis());
            ctx.FireRead(msg);
        }

        public void Write(ChannelHandlerContext ctx, object msg)
        {
            ctx.Channel.WriteCompleted += (channel) =>
            {
                _lastWriteTime.Set(Convenient.CurrentTimeMillis());
            };
            //ctx.FireWrite(msg); // TODO needed?
        }

        private void Initialize(ChannelHandlerContext ctx)
        {
            switch (_state)
            {
                case 1:
                    return;
                case 2:
                    return;
            }
            _state = 1;

            // .NET-specific:
            var currentMillis = Convenient.CurrentTimeMillis();
            _lastReadTime.Set(currentMillis);
            _lastWriteTime.Set(currentMillis);

            if (AllIdleTimeMillis > 0)
            {
                // one-shot task
                StartAllIdleTimeoutTask(ctx, AllIdleTimeMillis);
            }
        }

        // use "async void" because it's an event-like task
        private async void StartAllIdleTimeoutTask(ChannelHandlerContext ctx, int delayMillis)
        {
            _cts = new CancellationTokenSource();
            _allIdleTimeoutTask = Task.Delay(delayMillis, _cts.Token);

            await _allIdleTimeoutTask;

            // continue with "AllIdleTimeoutTask"
            if (!ctx.Channel.IsOpen)
            {
                return;
            }
            var currentTime = Convenient.CurrentTimeMillis();
            var lastIoTime = Math.Max(_lastReadTime.Get(), _lastWriteTime.Get());
            var nextDelay = (int)(AllIdleTimeMillis - (currentTime - lastIoTime)); // TODO bad cast, possible data loss
            if (nextDelay <= 0)
            {
                // both reader and writer are idle
                // --> set a new timeout and notify the callback
                StartAllIdleTimeoutTask(ctx, AllIdleTimeMillis);
                try
                {
                    ctx.FireUserEventTriggered(this);
                }
                catch (Exception ex)
                {
                    ctx.FireExceptionCaught(ex);
                }
            }
            else
            {
                // either read or write occurred before the timeout
                // --> set a new timeout with shorter delayMillis
                StartAllIdleTimeoutTask(ctx, nextDelay);
            }
        }

        private void Destroy()
        {
            _state = 2;
            if (_allIdleTimeoutTask != null)
            {
                _cts.Cancel();
                _allIdleTimeoutTask = null;
                _cts = null;
            }
        }

        public void UserEventTriggered(ChannelHandlerContext ctx, object evt)
        {
            // nothing to do here
        }
    }
}

﻿using System;
using NLog;
using TomP2P.Connection;
using TomP2P.Connection.Windows.Netty;

namespace TomP2P.Message
{
    public class TomP2PSinglePacketUdp : BaseChannelHandler, IInboundHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ISignatureFactory _signatureFactory;

        public TomP2PSinglePacketUdp(ISignatureFactory signatureFactory)
        {
            _signatureFactory = signatureFactory;
        }

        /// <summary>
        /// .NET-specific decoding handler for incoming UDP messages.
        /// </summary>
        public void Read(ChannelHandlerContext ctx, object msg)
        {
            var dgram = msg as DatagramPacket;
            if (dgram == null)
            {
                ctx.FireRead(msg);
                return;
            }

            var buf = dgram.Content;
            var sender = dgram.Sender;
            var recipient = dgram.Recipient;

            try
            {
                var decoder = new Decoder(_signatureFactory);
                bool finished = decoder.Decode(ctx, buf, recipient, sender);
                if (finished)
                {
                    // prepare finish
                    ctx.FireRead(decoder.PrepareFinish());
                }
                else
                {
                    Logger.Warn("Did not get the complete packet!");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in UDP decoding.", ex);
                throw;
            }
        }
    }
}

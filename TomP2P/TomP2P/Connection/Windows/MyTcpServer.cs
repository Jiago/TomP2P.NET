﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Message;

namespace TomP2P.Connection.Windows
{
    public class MyTcpServer : BaseChannel, ITcpChannel
    {
        // wrapped member
        private readonly TcpListener _tcpServer;

        private volatile bool _isStopped; // volatile!

        public MyTcpServer(IPEndPoint localEndPoint)
        {
            // local endpoint
            _tcpServer = new TcpListener(localEndPoint);
        }

        public void Start()
        {
            _tcpServer.Start();

            // accept MaxNrOfClients simultaneous connections
            var maxNrOfClients = Utils.Utils.GetMaxNrOfClients();
            for (int i = 0; i < maxNrOfClients; i++)
            {
                ServiceLoopAsync();
            }
            _isStopped = false;
        }

        public void Stop()
        {
            Close();
        }

        protected override void DoClose()
        {
            _tcpServer.Stop();
            // TODO notify async wait in service loop (CancellationToken)
            _isStopped = true;
        }

        protected async Task ServiceLoopAsync()
        {
            // buffers
            var recvBuffer = new byte[256];
            var sendBuffer = new byte[256];

            while (!_isStopped)
            {
                // accept a client connection
                var client = await _tcpServer.AcceptTcpClientAsync();
                
                // get stream for reading and writing
                var stream = client.GetStream();

                // loop to receive all content sent by the client
                while (await stream.ReadAsync(recvBuffer, 0, recvBuffer.Length) != 0)
                {
                    // process content
                    // TODO implement
                    throw new NotImplementedException();

                    // send back
                    await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);
                }
            }
        }

        private byte[] TcpPipeline(byte[] recvBytes, IPEndPoint recipient, IPEndPoint sender)
        {
            // TODO implement a pipeline config somewhat similar to Java's ChannelServer.handlers()
            throw new NotImplementedException();
            /*// 1. decode incoming message
            // 2. hand it to the Dispatcher
            // 3. encode outgoing message
            var recvMessage = _decoder.Read(recvBytes, recipient, sender);

            // null means that no response is sent back
            // TODO does this mean that we can close channel?
            var responseMessage = _dispatcher.RequestMessageReceived(recvMessage, true, _udpServerSocket.Client);

            // TODO channel might have been closed, check

            var buffer = _encoder.Write(responseMessage);
            var sendBytes = ConnectionHelper.ExtractBytes(buffer);
            return sendBytes;*/
        }

        public override Socket Socket
        {
            get { return _tcpServer.Server; }
        }

        public bool IsActive
        {
            // from Java Netty: "Return true if the Channel is active and so connected."
            get { return IsOpen && !_isStopped; }
        }

        public override bool IsUdp
        {
            get { return false; }
        }

        public override bool IsTcp
        {
            get { return true; }
        }
    }
}

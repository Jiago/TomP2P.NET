﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Sockets
{
    /// <summary>
    /// Synchronous server socket that suspends execution of the server application
    /// while waiting for a connection from a client.
    /// </summary>
    public class SyncServer
    {
        private int _bufferSize = 1024;
        private string _hostName = "localhost"; // or IPAddress 127.0.0.1
        private short _serverPort = 5150;

        // TODO make server protocol-generic
        private SocketType _socketType; // TCP: Stream, UDP: Dgram
        private ProtocolType _protocolType; // TCP: Tcp, UDP: Udp

        public byte[] SendBuffer { get; set; }
        public byte[] RecvBuffer { get; set; }

        public void StartTcp()
        {
            try
            {

                // establish the local endpoint for the socket
                IPHostEntry ipHostInfo = Dns.GetHostEntry(_hostName);
                IPAddress localAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEp = new IPEndPoint(localAddress, _serverPort);

                // create a TCP/IP server socket
                Socket listener = new Socket(localAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Socket handler = null;

                // BINDING
                try
                {
                    listener.Bind(localEp);
                }
                catch (Exception)
                {
                    throw new Exception("Binding failed.");
                }

                // LISTENING
                try
                {
                    // TCP only
                    listener.Listen(10); // TODO find appropriate backlog
                }
                catch (Exception)
                {
                    throw new Exception("Listening failed.");
                }

                // ACCEPTING (RECEIVING)
                try
                {
                    handler = listener.Accept(); // blocking

                    while (true)
                    {
                        int bytesRecv = handler.Receive(RecvBuffer);

                        if (bytesRecv == 0)
                        {
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    throw new Exception("Accepting/Receiving failed.");
                }

                // Manipulate Data
                SendBuffer = RecvBuffer;

                // SENDING
                try
                {
                    handler.Send(SendBuffer);

                    // shutdown handler/client-connection
                    handler.Shutdown(SocketShutdown.Send);
                    handler.Close();
                }
                catch (Exception)
                {
                    throw new Exception("Sending failed.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Net;
using NLog;
using TomP2P.Connection.Windows;
using TomP2P.Message;

namespace TomP2P.Connection
{
    /// <summary>
    /// The "server" part that accepts connections.
    /// </summary>
    public sealed class ChannelServer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private MyUdpServer _udpServer;
        private TcpServerSocket _tcpServer;

        // setup
        private readonly Bindings _interfaceBindings;

        /// <summary>
        /// The channel server configuration.
        /// </summary>
        public ChannelServerConfiguration ChannelServerConfiguration { get; private set; }
        private readonly Dispatcher _dispatcher;
        private readonly IList<IPeerStatusListener> _peerStatusListeners;

        // TODO DropConnectionInboundHandlers needed?
        private readonly TomP2PSinglePacketUDP _udpDecoderHandler;

        // .NET
        private readonly TomP2POutbound _udpEncoderHandler;

        /// <summary>
        /// Sets parameters and starts network device discovery.
        /// </summary>
        /// <param name="channelServerConfiguration">The server configuration that contains e.g. the handlers</param>
        /// <param name="dispatcher">The shared dispatcher.</param>
        /// <param name="peerStatusListeners">The status listeners for offline peers.</param>
        public ChannelServer(ChannelServerConfiguration channelServerConfiguration, Dispatcher dispatcher,
            IList<IPeerStatusListener> peerStatusListeners)
        {
            ChannelServerConfiguration = channelServerConfiguration;
            _interfaceBindings = channelServerConfiguration.BindingsIncoming;
            _dispatcher = dispatcher;
            _peerStatusListeners = peerStatusListeners;
            string status = DiscoverNetworks.DiscoverInterfaces(_interfaceBindings);
            Logger.Info("Status of interface search: {0}.", status);

            // TODO DropConnectionInboundHandlers needed?
            _udpDecoderHandler = new TomP2PSinglePacketUDP(channelServerConfiguration.SignatureFactory);
            _udpEncoderHandler = new TomP2POutbound(false, channelServerConfiguration.SignatureFactory);
        }

        /// <summary>
        /// Starts to listen to UDP and TCP ports.
        /// </summary>
        /// <returns></returns>
        public bool Startup()
        {
            if (!ChannelServerConfiguration.IsDisableBind)
            {
                if (_interfaceBindings.IsListenAll)
                {
                    Logger.Info("Listening for broadcasts on UDP port {0} and TCP port {1}.",
                        ChannelServerConfiguration.Ports.UdpPort,
                        ChannelServerConfiguration.Ports.TcpPort);
                    if (!StartupTcp(new IPEndPoint(IPAddress.Any, ChannelServerConfiguration.Ports.TcpPort))
                        || !StartupUdp(new IPEndPoint(IPAddress.Any, ChannelServerConfiguration.Ports.UdpPort)))
                    {
                        Logger.Warn("Cannot bind TCP or UDP.");
                        return false;
                    }
                }
                else
                {
                    foreach (IPAddress address in _interfaceBindings.FoundAddresses)
                    {
                        Logger.Info("Listening on address {0}, UDP port {1}, TCP port {2}.", address,
                            ChannelServerConfiguration.Ports.UdpPort,
                            ChannelServerConfiguration.Ports.TcpPort);
                        if (!StartupTcp(new IPEndPoint(address, ChannelServerConfiguration.Ports.TcpPort))
                            || !StartupUdp(new IPEndPoint(address, ChannelServerConfiguration.Ports.UdpPort)))
                        {
                            Logger.Warn("Cannot bind TCP or UDP.");
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Starts to listen on a UDP port.
        /// </summary>
        /// <param name="listenAddress">The address to listen to.</param>
        /// <returns>True, if startup was successful.</returns>
        private bool StartupUdp(IPEndPoint listenAddress)
        {
            // pipeline is implemented in MyUdpServer.UdpPipeline
            // TODO configure UDP server
            try
            {
                // TODO find appropriate maxNrOfClients
                _udpServer = new MyUdpServer(listenAddress, 10, _udpDecoderHandler, _udpEncoderHandler, _dispatcher);
                _udpServer.Start();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn("An exception occured when starting up UDP server.", ex);
                return false;
            }
        }

        /// <summary>
        /// Starts tp listen on a TCP port.
        /// </summary>
        /// <param name="listenAddress">The address to listen to.</param>
        /// <returns>True, if startup was successful.</returns>
        private bool StartupTcp(IPEndPoint listenAddress)
        {
            return true;
            // TODO implement
            // TODO configure TCP server
            // TODO configure a server-side pipeline
            try
            {
                _tcpServer = new TcpServerSocket(listenAddress, 10, 10 * 1024); // TODO move configs to separate config file
                // binding is done in Start()
                _tcpServer.Start();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn("An exception occured when starting up TCP server. {0}", ex);
                return false;
            }
        }

        /// <summary>
        /// Shuts down the server.
        /// </summary>
        public void Shutdown()
        {
            // in Java, this method is async

            // shutdown both UDP and TCP server sockets
            if (_udpServer != null)
            {
                _udpServer.Stop();
            }
            if (_tcpServer != null)
            {
                _tcpServer.Stop();
            }
        }
    }
}
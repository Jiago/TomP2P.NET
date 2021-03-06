﻿using System;
using System.Collections.Generic;
using System.Net;
using TomP2P.Extensions;

namespace TomP2P.Core.Peers
{
    // TODO in Java: make subnetting configurable, IPv4 and IPv6.
    // Beeing too strict does not mean to harm the network. Other peers will have the 
    // information about the peer even if you excluded it.

    /// <summary>
    /// Filter peers if the IP is the same.
    /// </summary>
    public class PeerIpFilter : IPeerFilter
    {
        private readonly int _mask4;
        private readonly int _mask6;

        public PeerIpFilter(int mask4, int mask6)
        {
            _mask4 = mask4;
            _mask6 = mask6;
        }

        public bool Reject(PeerAddress peerAddress, ICollection<PeerAddress> all, Number160 target)
        {
            if (peerAddress.InetAddress.IsIPv4())
            {
                var ipv4 = IPv4.FromInetAddress(peerAddress.InetAddress);
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var paddr in all)
                {
                    if (paddr.InetAddress.IsIPv4())
                    {
                        var ipv4Test = IPv4.FromInetAddress(paddr.InetAddress);
                        if (ipv4.MaskWithNetworkMask(_mask4).Equals(ipv4Test.MaskWithNetworkMask(_mask4)))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                var ipv6 = IPv6.FromInetAddress(peerAddress.InetAddress);
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var paddr in all)
                {
                    if (paddr.InetAddress.IsIPv6())
                    {
                        var ipv6Test = IPv6.FromInetAddress(paddr.InetAddress);
                        if (ipv6.MaskWithNetworkMask(_mask6).Equals(ipv6Test.MaskWithNetworkMask(_mask6)))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // ReSharper disable once InconsistentNaming
        private class IPv4 : IEquatable<IPv4>
        {
            private readonly int _bits;

            private IPv4(int bits)
            {
                _bits = bits;
            }

            public IPv4 MaskWithNetworkMask(int networkMask)
            {
                if (networkMask == 32)
                {
                    return this;
                }
                // TODO works?
                return new IPv4((int)(_bits & (0xFFFFFFFF << (32 - networkMask))));
            }

            public static IPv4 FromInetAddress(IPAddress inetAddress)
            {
                if (inetAddress == null)
                {
                    throw new ArgumentException("Cannot construct from null.");
                }
                if (!inetAddress.IsIPv4())
                {
                    throw new ArgumentException("Must be IPv4.");
                }
                byte[] buf = inetAddress.GetAddressBytes();

                int ip = ((buf[0] & 0xFF) << 24) | ((buf[1] & 0xFF) << 16) | ((buf[2] & 0xFF) << 8) | ((buf[3] & 0xFF) << 0);

                return new IPv4(ip);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(obj, null))
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (GetType() != obj.GetType())
                {
                    return false;
                }
                return Equals(obj as IPv4);
            }

            public bool Equals(IPv4 other)
            {
                return _bits == other._bits;
            }

            public override int GetHashCode()
            {
                return _bits;
            }
        }

        // ReSharper disable once InconsistentNaming
        private class IPv6 : IEquatable<IPv6>
        {
            private readonly long _highBits;
            private readonly long _lowBits;

            private IPv6(long hightBits, long lowBits)
            {
                _highBits = hightBits;
                _lowBits = lowBits;
            }

            public IPv6 MaskWithNetworkMask(int networkMask)
            {
                if (networkMask == 128)
                {
                    return this;
                }
                if (networkMask == 64)
                {
                    return new IPv6(_highBits, 0);
                }
                if (networkMask > 64)
                {
                    int remainingPrefixLength = networkMask - 64;
                    // TODO works?
                    return new IPv6(_highBits, _lowBits & (long)(0xFFFFFFFFFFFFFFFFL << (64 - remainingPrefixLength)));
                }
                return new IPv6(_highBits & (long)(0xFFFFFFFFFFFFFFFFL << (64 - networkMask)), 0);
            }

            public static IPv6 FromInetAddress(IPAddress inetAddress)
            {
                if (inetAddress == null)
                {
                    throw new ArgumentException("Cannot construct from null.");
                }
                if (!inetAddress.IsIPv6())
                {
                    throw new ArgumentException("Must be IPv6.");
                }
                byte[] buf = inetAddress.GetAddressBytes();

                long highBits = ((buf[0] & 0xFFL) << 56) | ((buf[1] & 0xFFL) << 48) | ((buf[2] & 0xFFL) << 40)
                    | ((buf[3] & 0xFFL) << 32) | ((buf[4] & 0xFFL) << 24) | ((buf[5] & 0xFFL) << 16)
                    | ((buf[6] & 0xFFL) << 8) | ((buf[7] & 0xFFL) << 0);

                long lowBits = ((buf[8] & 0xFFL) << 56) | ((buf[9] & 0xFFL) << 48) | ((buf[10] & 0xFFL) << 40)
                        | ((buf[11] & 0xFFL) << 32) | ((buf[12] & 0xFFL) << 24) | ((buf[13] & 0xFFL) << 16)
                        | ((buf[14] & 0xFFL) << 8) | ((buf[15] & 0xFFL) << 0);

                return new IPv6(highBits, lowBits);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(obj, null))
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (GetType() != obj.GetType())
                {
                    return false;
                }
                return Equals(obj as IPv6);
            }

            public bool Equals(IPv6 other)
            {
                return _highBits == other._highBits && _lowBits == other._lowBits;
            }

            public override int GetHashCode()
            {
                return (int)(_highBits ^ _lowBits);
            }
        }
    }
}

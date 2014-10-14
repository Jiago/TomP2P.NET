﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Peers;

namespace TomP2P.Message
{
    // TODO introduce header and payload regions

    public class Message
    {
        // used for creating random message id
        private static readonly Random Random = new Random();

        public const int ContentTypeLength = 8;

        /// <summary>
        /// 8 x 4 bit.
        /// </summary>
        public enum Content
        {
            Undefined, // required as default enum value
            Empty, Key, MapKey640Data, MapKey640Keys, SetKey640, SetNeighbors, ByteBuffer,
            Long, Integer, PublicKeySignature, SetTrackerData, BloomFilter, MapKey640Byte,
            PublicKey, SetPeerSocket, User1
        }

        /// <summary>
        /// 4 x 1 bit.
        /// </summary>
        public enum MessageType
        {
            // REQUEST_1 is the normal request
            // REQUEST_2 for GET returns the extended digest (hashes of all stored data)
            // REQUEST_3 for GET returns a Bloom filter
            // REQUEST_4 for GET returns a range (min/max)
            // REQUEST_2 for PUT/ADD/COMPARE_PUT means protect domain
            // REQUEST_3 for PUT means put if absent
            // REQUEST_3 for COMPARE_PUT means partial (partial means that put those data that match compare, ignore others)
            // REQUEST_4 for PUT means protect domain and put if absent
            // REQUEST_4 for COMPARE_PUT means partial and protect domain
            // REQUEST_2 for REMOVE means send back results
            // REQUEST_2 for RAW_DATA means serialize object
            // *** NEIGHBORS has four different cases
            // REQUEST_1 for NEIGHBORS means check for put (no digest) for tracker and storage
            // REQUEST_2 for NEIGHBORS means check for get (with digest) for storage
            // REQUEST_3 for NEIGHBORS means check for get (with digest) for tracker
            // REQUEST_4 for NEIGHBORS means check for put (with digest) for task
            // REQUEST_FF_1 for PEX means fire and forget, coming from mesh
            // REQUEST_FF_1 for PEX means fire and forget, coming from primary
            // REQUEST_1 for TASK is submit new task
            // REQUEST_2 for TASK is status
            // REQUEST_3 for TASK is send back result
            Request1, Request2, Request3, Request4, RequestFf1, RequestFf2, Ok,
            PartiallyOk, NotFound, Denied, UnknownId, Exception, Cancel, User1, User2
        }

        // *** HEADER ***

        /// <summary>
        /// Randomly generated message ID.
        /// </summary>
        public int MessageId { get; private set; }

        /// <summary>
        /// Returns the version, which is 32bit. Each application can choose a version to not interfere with other applications.
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Determines if its a request or command reply and what kind of reply (error, warning states).
        /// </summary>
        public MessageType Type { get; private set; }

        /* commands so far:
         *  0: PING
         *  1: PUT
         *  2: GET
         *  3: ADD
         *  4: REMOVE
         *  5: NEIGHBORS
         *  6: QUIT
         *  7: DIRECT_DATA
         *  8: TRACKER_ADD
         *  9: TRACKER_GET
         * 10: PEX
         * 11: TASK
         * 12: BROADCAST_DATA*/

        /// <summary>
        /// Command of the message, such as GET, PING, etc.
        /// </summary>
        public byte Command { get; private set; }

        /// <summary>
        /// The ID of the sender. Note that the IP is set via the socket.
        /// </summary>
        public PeerAddress Sender { get; private set; }

        /// <summary>
        /// The ID of the recipient. Note that the IP is set via the socket.
        /// </summary>
        public PeerAddress Recipient { get; private set; }

        public PeerAddress RecipientRelay { get; private set; }

        private int _options = 0;

        // *** PAYLOAD ***

        // we can send 8 types

        /// <summary>
        /// The content types. Can be empty if not set.
        /// </summary>
        public Content[] ContentTypes { get; private set; }

        private readonly Queue<NumberType> _contentReferences = new Queue<NumberType>();

        // following the payload objects:
        // content lists:
        private List<NeighborSet> _neighborsList = null;

        // TODO add all further lists

        /// <summary>
        /// Creates a message with a random message ID.
        /// </summary>
        public Message()
        {
            MessageId = Random.Next();
            ContentTypes = new Content[ContentTypeLength];
        }

        /// <summary>
        /// For deserialization, the ID needs to be set.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <returns>This class.</returns>
        public Message SetMessageId(int messageId)
        {
            MessageId = messageId;
            return this;
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        /// <param name="version">The 24bit version.</param>
        /// <returns>This class.</returns>
        public Message SetVersion(int version)
        {
            Version = version;
            return this;
        }

        /// <summary>
        /// Set the message type. Either its a request or reply (with error and warning codes).
        /// </summary>
        /// <param name="type">The type of the message.</param>
        /// <returns>This class.</returns>
        public Message SetMessageType(MessageType type)
        {
            Type = type;
            return this;
        }

        /// <summary>
        /// Command of the message, such as GET, PING, etc.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>This class.</returns>
        public Message SetCommand(byte command)
        {
            Command = command;
            return this;
        }

        /// <summary>
        /// Set the ID of the sender. The IP of the sender will NOT be transferred, as this information is in the IP packet.
        /// </summary>
        /// <param name="sender">The ID of the sender.</param>
        /// <returns>This class.</returns>
        public Message SetSender(PeerAddress sender)
        {
            Sender = sender;
            return this;
        }

        /// <summary>
        /// Set the ID of the recipient. The IP is used to connect to the recipient, but the IP is NOT transferred.
        /// </summary>
        /// <param name="recipient">The ID of the recipient.</param>
        /// <returns>This class.</returns>
        public Message SetRecipient(PeerAddress recipient)
        {
            Recipient = recipient;
            return this;
        }

        public Message SetRecipientRelay(PeerAddress recipientRelay)
        {
            RecipientRelay = recipientRelay;
            return this;
        }

        /// <summary>
        /// Convenient method to set the content type.
        /// Set first content type 1, if this is set (not empty), then set the second, etc. 
        /// </summary>
        /// <param name="contentType">The content type to set.</param>
        /// <returns>This class.</returns>
        public Message SetContentType(Content contentType)
        {
            for (int i = 0, reference = 0; i < ContentTypeLength; i++)
            {
                if (ContentTypes[i] == Content.Undefined) // TODO check expression
                {
                    if (contentType == Content.PublicKeySignature && i != 0)
                    {
                        throw new InvalidOperationException("The public key needs to be the first to be set.");
                    }
                    ContentTypes[i] = contentType;
                    _contentReferences.Enqueue(new NumberType(reference, contentType));
                    return this;
                }
                if (ContentTypes[i] == contentType)
                {
                    reference++;
                }
                else if (ContentTypes[i] == Content.PublicKeySignature || ContentTypes[i] == Content.PublicKey)
                {
                    // special handling for public key, as we store both in the same list
                    if (contentType == Content.PublicKeySignature || contentType == Content.PublicKey)
                    {
                        reference++;
                    }
                }
            }
            throw new InvalidOperationException("Already set 8 content types.");
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    public class CompositeByteBuf : AbstractByteBuf
    {
        private readonly IByteBufAllocator _alloc;
        private readonly bool _direct;
        private readonly IList<Component> _components = new List<Component>();
        private readonly int _maxNumComponents;

        private sealed class Component
        {
            public readonly ByteBuf _buf;
            public readonly int _length;
            public int _offset;
            public int _endOffset;

            private Component(ByteBuf buf)
            {
                _buf = buf;
                _length = buf.ReadableBytes;
            }
        }

        public CompositeByteBuf(IByteBufAllocator alloc, bool direct, int maxNumComponents, params ByteBuf[] buffers)
            : base(Int32.MaxValue)
        {
            if (alloc == null)
            {
                throw new NullReferenceException("alloc");
            }
            _alloc = alloc;
            _direct = direct;
            _maxNumComponents = maxNumComponents;
            // TODO leak detector needed?
        }

        public override int Capacity
        {
            get
            {
                if (_components.Count == 0)
                {
                    return 0;
                }
                return _components[_components.Count - 1]._endOffset;

            }
        }

        public override int NioBufferCount()
        {
            if (_components.Count == 1)
            {
                return _components[0]._buf.NioBufferCount();
            }
            else
            {
                int count = 0;
                int componentsCount = _components.Count;
                for (int i = 0; i < componentsCount; i++)
                {
                    var c = _components[i];
                    count += c._buf.NioBufferCount();
                }
                return count;
            }
        }

        public override MemoryStream NioBuffer(int index, int length)
        {
            if (_components.Count == 1)
            {
                ByteBuf buf = _components[0]._buf;
                if (buf.NioBufferCount() == 1)
                {
                    return _components[0]._buf.NioBuffer(index, length);
                }
            }
            MemoryStream merged = Convenient.Allocate(length); // little-endian
            MemoryStream[] buffers = NioBuffers(index, length);

            for (int i = 0; i < buffers.Length; i++)
            {
                merged.Put(buffers[i]);
            }

            merged.Flip();
            return merged;
        }

        public override MemoryStream[] NioBuffers(int index, int length)
        {
            CheckIndex(index, length);
            if (length == 0)
            {
                return new MemoryStream[0]; // EMPTY_BYTE_BUFFERS<
            }

            IList<MemoryStream> buffers = new List<MemoryStream>(_components.Count);
            int i = ToComponentIndex(index);
            while (length > 0)
            {
                Component c = _components[i];
                ByteBuf s = c._buf;
                int adjustment = c._offset;
                int localLength = Math.Min(length, s.Capacity - (index - adjustment));
                switch (s.NioBufferCount())
                {
                    case 0:
                        throw new InvalidOperationException();
                    case 1:
                        buffers.Add(s.NioBuffer(index - adjustment, localLength));
                        break;
                    default:
                        // TODO implement
                        Collections.addAll(buffers, s.NioBuffers(index - adjustment, localLength));
                }

                index += localLength;
                length -= localLength;
                i++;
            }

            return buffers.ToArray();
        }

        public int ToComponentIndex(int offset)
        {
            CheckIndex(offset);

            for (int low = 0, high = _components.Count; low <= high;)
            {
                int mid = low + high >> 1;
                Component c = _components[mid];
                if (offset >= c._endOffset) {
                    low = mid + 1;
                } else if (offset < c._offset) {
                    high = mid - 1;
                } else {
                    return mid;
                }
            }

            throw new Exception("should not reach here");
        }

        public override MemoryStream[] NioBuffers()
        {
            return NioBuffers(ReaderIndex, ReadableBytes);
        }

        // TODO implement deallocate?

        public override ByteBuf Unwrap()
        {
            return null;
        }
    }
}

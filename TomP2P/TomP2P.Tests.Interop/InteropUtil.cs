﻿using TomP2P.Core.Storage;
using TomP2P.Extensions;

namespace TomP2P.Tests.Interop
{
    public class InteropUtil
    {
        public static byte[] ExtractBytes(AlternativeCompositeByteBuf buf)
        {
            var buffer = buf.NioBuffer();
            buffer.Position = 0;

            var bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes, 0, bytes.Length);
            return bytes;
        }

        public static sbyte[] ReadJavaBytes(byte[] bytes)
        {
            AlternativeCompositeByteBuf buf = AlternativeCompositeByteBuf.CompBuffer();
            buf.WriteBytes(bytes.ToSByteArray());
            return ExtractBytes(buf).ToSByteArray();
        }
    }
}

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Utility
{
    internal static class BitConversion
    {
        public static uint ReadUInt(ReadOnlySpan<byte> bytes, int startIndex)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian
                (bytes.Slice(startIndex, sizeof(uint)));
        }

        public static int ReadInt(ReadOnlySpan<byte> bytes, int startIndex)
        {
            return BinaryPrimitives.ReadInt32LittleEndian
                (bytes.Slice(startIndex, sizeof(int)));
        }

        public static ushort ReadUShort(ReadOnlySpan<byte> bytes, int startIndex)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian
                (bytes.Slice(startIndex, sizeof(ushort)));
        }

        public static void WriteUInt(Span<byte> span, uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        }

        public static void WriteInt(Span<byte> span, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
        }

        public static void WriteUShort(Span<byte> span, ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        }
    }
}

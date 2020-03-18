// This code is copied from
// https://github.com/topas/VarintBitConverter
//
// VarintConverter was created and is maintained by
// Tomáš Pastorek (https://github.com/topas).
// It was released under BSD license
using System.Buffers;

namespace System
{
    internal class VarintBitConverter
    {
        /// <summary>
        /// Returns the specified byte value as varint encoded array of bytes.   
        /// </summary>
        /// <param name="value">Byte value</param>
        /// <returns>Varint array of bytes.</returns>
        public static byte[] GetVarintBytes(byte value)
        {
            return GetVarintBytes((ulong)value);
        }

        /// <summary>
        /// Returns the specified 16-bit signed value as varint encoded array of bytes.   
        /// </summary>
        /// <param name="value">16-bit signed value</param>
        /// <returns>Varint array of bytes.</returns>
        public static byte[] GetVarintBytes(short value)
        {
            var zigzag = EncodeZigZag(value, 16);
            return GetVarintBytes((ulong)zigzag);
        }

        /// <summary>
        /// Returns the specified 16-bit unsigned value as varint encoded array of bytes.   
        /// </summary>
        /// <param name="value">16-bit unsigned value</param>
        /// <returns>Varint array of bytes.</returns>
        public static byte[] GetVarintBytes(ushort value)
        {
            return GetVarintBytes((ulong)value);
        }

        /// <summary>
        /// Returns the specified 32-bit signed value as varint encoded array of bytes.   
        /// </summary>
        /// <param name="value">32-bit signed value</param>
        /// <returns>Varint array of bytes.</returns>
        public static byte[] GetVarintBytes(int value)
        {
            var zigzag = EncodeZigZag(value, 32);
            return GetVarintBytes((ulong)zigzag);
        }

        /// <summary>
        /// Returns the specified 32-bit unsigned value as varint encoded array of bytes.   
        /// </summary>
        /// <param name="value">32-bit unsigned value</param>
        /// <returns>Varint array of bytes.</returns>
        public static byte[] GetVarintBytes(uint value)
        {
            return GetVarintBytes((ulong)value);
        }

        /// <summary>
        /// Returns the specified 64-bit signed value as varint encoded array of bytes.   
        /// </summary>
        /// <param name="value">64-bit signed value</param>
        /// <returns>Varint array of bytes.</returns>
        public static byte[] GetVarintBytes(long value)
        {
            var zigzag = EncodeZigZag(value, 64);
            return GetVarintBytes((ulong)zigzag);
        }

        /// <summary>
        /// Returns the specified 64-bit unsigned value as varint encoded array of bytes.   
        /// </summary>
        /// <param name="value">64-bit unsigned value</param>
        /// <returns>Varint array of bytes.</returns>
        public static byte[] GetVarintBytes(ulong value)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(10);
            var pos = 0;
            do
            {
                var byteVal = value & 0x7f;
                value >>= 7;

                if (value != 0)
                {
                    byteVal |= 0x80;
                }

                buffer[pos++] = (byte)byteVal;

            } while (value != 0);

            var result = new byte[pos];
            Buffer.BlockCopy(buffer, 0, result, 0, pos);
            ArrayPool<byte>.Shared.Return(buffer);

            return result;
        }

        /// <summary>
        /// Writes the specified 64-bit value as varint encoded array of bytes using <paramref name="writer"/>.   
        /// </summary>
        /// <param name="writer">A binary writer</param>
        /// <param name="value">64-bit unsigned value</param>
        /// <returns>Number of bytes written.</returns>
        public static void WriteVarintBytesToBuffer(IO.BinaryWriter writer, long value)
        {
            long zigzag = EncodeZigZag(value, 64);
            WriteVarintBytesToBuffer(writer, (ulong)zigzag);
        }

        /// <summary>
        /// Writes the specified 64-bit unsigned value as varint encoded array of bytes using <paramref name="writer"/>.   
        /// </summary>
        /// <param name="writer">A binary writer</param>
        /// <param name="value">64-bit unsigned value</param>
        /// <returns>Number of bytes written.</returns>
        public static void WriteVarintBytesToBuffer(IO.BinaryWriter writer, ulong value)
        {
            do
            {
                var byteVal = value & 0x7f;
                value >>= 7;

                if (value != 0)
                {
                    byteVal |= 0x80;
                }
                writer.Write((byte)byteVal);

            } while (value != 0);
        }

        /// <summary>
        /// Returns 64-bit signed value from varint encoded array of bytes.
        /// </summary>
        /// <param name="bytes">Varint encoded array of bytes.</param>
        /// <returns>64-bit signed value</returns>
        public static long ToInt64(ReadOnlySpan<byte> bytes)
        {
            var zigzag = ToTarget(bytes, 64);
            return DecodeZigZag(zigzag);
        }

        /// <summary>
        /// Returns 64-bit unsigned value from varint encoded array of bytes.
        /// </summary>
        /// <param name="bytes">Varint encoded array of bytes.</param>
        /// <returns>64-bit unsigned value</returns>
        public static ulong ToUInt64(ReadOnlySpan<byte> bytes)
        {
            return ToTarget(bytes, 64);
        }

        internal static long EncodeZigZag(long value, int bitLength)
        {
            return (value << 1) ^ (value >> (bitLength - 1));
        }

        internal static long DecodeZigZag(ulong value)
        {
            if ((value & 0x1) == 0x1)
            {
                return (-1 * ((long)(value >> 1) + 1));
            }

            return (long)(value >> 1);
        }

        private static ulong ToTarget(ReadOnlySpan<byte> bytes, int sizeBites)
        {
            int shift = 0;
            ulong result = 0;

            foreach (ulong byteValue in bytes)
            {
                ulong tmp = byteValue & 0x7f;
                result |= tmp << shift;

                if (shift > sizeBites)
                {
                    throw new ArgumentOutOfRangeException("bytes", "Byte array is too large.");
                }

                if ((byteValue & 0x80) != 0x80)
                {
                    return result;
                }

                shift += 7;
            }

            throw new ArgumentException("Cannot decode varint from byte array.", "bytes");
        }
    }
}

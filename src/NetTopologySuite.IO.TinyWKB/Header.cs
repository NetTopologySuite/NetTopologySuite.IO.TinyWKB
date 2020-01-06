using System;

namespace NetTopologySuite.IO
{
    [Obsolete("Use TinyWkbHeader", true)]
    internal struct Header
    {
        private readonly byte _h;

        public Header(byte h)
        {
            _h = h;
        }

        public Header(TinyWkbGeometryType geometryType, int precision)
        {
            long p = VarintBitConverter.EncodeZigZag((long) precision, 4);
            _h = (byte) ((byte) geometryType | (byte) (p << 4));
        }

        public TinyWkbGeometryType GeometryType => (TinyWkbGeometryType) (_h & 0x0f);

        public int Precision => (int) VarintBitConverter.DecodeZigZag((ulong) ((_h & 0xf0) >> 4));

        public double Scale { get { int precision = Precision; return Math.Pow(10, precision); } }

        public double Descale { get { int precision = Precision; return  Math.Pow(10, -precision); } }

        public byte Value { get => _h; }
    }
}

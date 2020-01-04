using System;

namespace NetTopologySuite.IO
{
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

        public double Scale => Math.Pow(10, Precision);

        public double Descale => Math.Pow(10, -Precision);
    }

    public enum TinyWkbGeometryType : byte
    {
        Point = 1,
        LineString = 2,
        Polygon = 3,
        MultiPoint = 4,
        MultiLineString = 5,
        MultiPolygon = 6,
        GeometryCollection = 7
    }
}

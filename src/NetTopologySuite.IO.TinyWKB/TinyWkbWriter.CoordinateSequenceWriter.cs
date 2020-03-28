using System;
using System.IO;

using NetTopologySuite.DataStructures;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO
{
    public partial class TinyWkbWriter
    {
        private class CoordinateSequenceWriter
        {
            private readonly double[] _scales;

            private readonly long[] _prevCoordinate;

            public CoordinateSequenceWriter(TinyWkbHeader header)
            {
                if (!header.HasExtendedPrecisionInformation || !(header.HasZ | header.HasM))
                {
                    _scales = new[] { header.ScaleX(), header.ScaleY() };
                }
                else if (header.HasZ && !header.HasM)
                {
                    _scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleZ() };
                }
                else if (!header.HasZ && header.HasM)
                {
                    _scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleM() };
                }
                else
                {
                    _scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleZ(), header.ScaleM() };
                }

                _prevCoordinate = new long[_scales.Length];
            }

            public int Dimension => _scales.Length;

            public void Write(BinaryWriter writer, CoordinateSequence sequence, bool skipLastCoordinate = false, bool writeCount = false)
            {
                if (sequence == null || sequence.Count == 0)
                    return;

                int count = sequence.Count - (skipLastCoordinate ? 1 : 0);

                if (writeCount)
                    writer.Write(VarintBitConverter.GetVarintBytes((uint)count));

                ReadOnlySpan<double> scales = _scales;
                Span<long> prevCoordinate = _prevCoordinate;

                for (int i = 0; i < count; i++)
                {
                    // Encode ordinate values
                    for (int dim = 0; dim < scales.Length; dim++)
                    {
                        double val = sequence.GetOrdinate(i, dim);
                        long enc = EncodeOrdinate(val, scales[dim], ref prevCoordinate[dim]);
                        writer.Write(VarintBitConverter.GetVarintBytes(enc));
                    }
                }
            }

            public void WriteIntervals(BinaryWriter writer, ReadOnlySpan<Interval> intervals)
            {
                for (int i = 0; i < intervals.Length; i++)
                {
                    long prev = 0;
                    writer.Write(VarintBitConverter.GetVarintBytes(EncodeOrdinate(intervals[i].Min, _scales[i], ref prev)));
                    writer.Write(VarintBitConverter.GetVarintBytes(EncodeOrdinate(intervals[i].Max, _scales[i], ref prev)));
                }
            }

            private static long EncodeOrdinate(double value, double scale, ref long lastScaledValue)
            {
                long lngValue = (long)Math.Round(value * scale, 0, MidpointRounding.AwayFromZero);
                long valueEnc = lngValue - lastScaledValue;
                lastScaledValue = lngValue;
                return valueEnc;
            }
        }
    }
}

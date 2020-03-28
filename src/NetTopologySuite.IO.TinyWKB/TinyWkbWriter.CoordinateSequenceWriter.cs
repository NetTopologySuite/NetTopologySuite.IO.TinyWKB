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
            private static readonly Ordinate[] OrdinatesXY = new Ordinate[] { Ordinate.X, Ordinate.Y };

            private static readonly Ordinate[] OrdinatesXYZ = new Ordinate[] { Ordinate.X, Ordinate.Y, Ordinate.Z };

            private static readonly Ordinate[] OrdinatesXYM = new Ordinate[] { Ordinate.X, Ordinate.Y, Ordinate.M };

            private static readonly Ordinate[] OrdinatesXYZM = new Ordinate[] { Ordinate.X, Ordinate.Y, Ordinate.Z, Ordinate.M };

            private readonly long[] _prevCoordinate;

            private readonly double[] _scales;

            public CoordinateSequenceWriter(TinyWkbHeader header)
            {
                if (!header.HasExtendedPrecisionInformation || !(header.HasZ | header.HasM))
                {
                    _scales = new[] { header.ScaleX(), header.ScaleY() };
                    OutputOrdinates = OrdinatesXY;
                }
                else if (header.HasZ && !header.HasM)
                {
                    _scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleZ() };
                    OutputOrdinates = OrdinatesXYZ;
                }
                else if (!header.HasZ && header.HasM)
                {
                    _scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleM() };
                    OutputOrdinates = OrdinatesXYM;
                }
                else
                {
                    _scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleZ(), header.ScaleM() };
                    OutputOrdinates = OrdinatesXYZM;
                }

                _prevCoordinate = new long[OutputOrdinates.Length];
            }

            public Ordinate[] OutputOrdinates { get; }

            public void Write(BinaryWriter writer, CoordinateSequence sequence, bool skipLastCoordinate = false, bool writeCount = false)
            {
                if (sequence == null || sequence.Count == 0)
                    return;

                int count = sequence.Count - (skipLastCoordinate ? 1 : 0);

                if (writeCount)
                    writer.Write(VarintBitConverter.GetVarintBytes((uint)count));

                ReadOnlySpan<double> scales = _scales;
                Span<long> prevCoordinate = _prevCoordinate;

                Span<int> ordinateIndexes = stackalloc int[OutputOrdinates.Length];
                for (int i = 0; i < OutputOrdinates.Length; i++)
                {
                    sequence.TryGetOrdinateIndex(OutputOrdinates[i], out ordinateIndexes[i]);
                }

                for (int i = 0; i < count; i++)
                {
                    // Encode ordinate values
                    for (int dim = 0; dim < ordinateIndexes.Length; dim++)
                    {
                        double val = ordinateIndexes[dim] == -1
                            ? double.NaN
                            : sequence.GetOrdinate(i, ordinateIndexes[dim]);
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

using System;
using System.IO;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO
{
    public partial class TinyWkbWriter
    {
        private class CoordinateSequenceWriter
        {
            private readonly long[] _prevCoordinate;

            public CoordinateSequenceWriter(TinyWkbHeader header)
            {
                if (!header.HasExtendedPrecisionInformation || !(header.HasZ | header.HasM))
                {
                    Ordinates = new[] { Ordinate.X, Ordinate.Y };
                    Scales = new[] { header.ScaleX(), header.ScaleY() };
                }
                else if (header.HasZ && !header.HasM)
                {
                    Ordinates = new[] { Ordinate.X, Ordinate.Y, Ordinate.Z };
                    Scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleZ() };
                }
                else if (!header.HasZ && header.HasM)
                {
                    Ordinates = new[] { Ordinate.X, Ordinate.Y, Ordinate.M };
                    Scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleM() };
                    Measures = 1;
                }
                else
                {
                    Ordinates = new[] { Ordinate.X, Ordinate.Y, Ordinate.Z, Ordinate.M };
                    Scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleZ(), header.ScaleM() };
                    Measures = 1;
                }

                _prevCoordinate = new long[Dimension];
            }

            /// <summary>
            /// Gets the number of dimensions to write
            /// </summary>
            public int Dimension
            {
                get => Scales.Length;
            }

            /// <summary>
            /// Gets the number of measure values to write
            /// </summary>
            public int Measures { get; }

            /// <summary>
            /// Gets a vector containing the scale factors for each ordinate
            /// </summary>
            public double[] Scales { get; }

            /// <summary>
            /// Gets a vector containing the scale factors for each ordinate
            /// </summary>
            public Ordinate[] Ordinates { get; }

            public void Write(BinaryWriter writer, CoordinateSequence sequence, bool skipLast)
            {
                if (sequence == null || sequence.Count == 0)
                    return;

                int count = skipLast ? sequence.Count - 1 : sequence.Count;
                if (count > 1)
                    writer.Write(VarintBitConverter.GetVarintBytes((uint)count));

                long[] prevCoordinate = _prevCoordinate;
                Span<int> ordinateIndices = stackalloc int[] {0, 1, -1, -1};
                for (int i = 2; i < Dimension; i++)
                {
                    if (!sequence.TryGetOrdinateIndex(Ordinates[i], out ordinateIndices[i]))
                        ordinateIndices[i] = -1;
                }

                for (int i = 0; i < count; i++)
                {
                    // Encode ordinate values
                    for (int dim = 0; dim < Dimension; dim++)
                    {
                        double value = ordinateIndices[dim] < 0 ? 0d : sequence.GetOrdinate(i, ordinateIndices[dim]);
                        long encOrdinate = EncodeOrdinate(value, Scales[dim], ref prevCoordinate[dim]);
                        writer.Write(VarintBitConverter.GetVarintBytes(encOrdinate));
                    }
                }

            }

            public static long EncodeOrdinate(double value, double scale, ref long lastScaledValue)
            {
                long lngValue = (long)Math.Round(value * scale, 0, MidpointRounding.AwayFromZero);
                long valueEnc = lngValue - lastScaledValue;
                lastScaledValue = lngValue;
                return valueEnc;
            }
        }
    }
}

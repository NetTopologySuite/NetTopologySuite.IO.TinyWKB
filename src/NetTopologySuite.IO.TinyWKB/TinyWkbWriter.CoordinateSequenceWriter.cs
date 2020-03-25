using System;
using System.Buffers;
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
                    Scales = new[] { header.ScaleX(), header.ScaleY() };
                    Dimension = 2;
                }
                else if (header.HasZ && !header.HasM)
                {
                    Scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleZ() };
                    Dimension = 3;
                }
                else if (!header.HasZ && header.HasM)
                {
                    Scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleM() };
                    Dimension = 3;
                    Measures = 1;
                }
                else
                {
                    Scales = new[] { header.ScaleX(), header.ScaleY(), header.ScaleZ(), header.ScaleM() };
                    Dimension = 4;
                    Measures = 1;
                }

                _prevCoordinate = new long[Dimension];
            }

            /// <summary>
            /// Gets the number of dimensions to write
            /// </summary>
            public int Dimension { get; }

            /// <summary>
            /// Gets the number of measure values to write
            /// </summary>
            public int Measures { get; }

            /// <summary>
            /// Gets a vector containing the scale factors for each ordinate
            /// </summary>
            public double[] Scales { get; }

            public void Write(BinaryWriter writer, CoordinateSequence sequence, int omit, int coordinatesRequired)
            {
                if (sequence == null || sequence.Count == 0)
                    return;

                int count = sequence.Count - omit;
                long[] prevCoordinate = _prevCoordinate;
                Span<long> encCoordinate = stackalloc long[Dimension];

                // Get an array of longs to store ordinate data in
                long[] ordinatesToWrite = ArrayPool<long>.Shared.Rent(count * Dimension);
                int coordinatesToWrite = 0;
                for (int i = 0; i < count; i++)
                {
                    // Assume we don't need to write the coordinate
                    bool write = false;

                    // Encode ordinate values
                    for (int dim = 0; dim < Dimension; dim++)
                    {
                        encCoordinate[dim] = EncodeOrdinate(sequence.GetOrdinate(i, dim), Scales[dim], ref prevCoordinate[dim]);
                        write |= encCoordinate[dim] != 0;
                    }

                    // This coordinate is different from the last.
                    if (write)
                    {
                        for (int dim = 0; dim < Dimension; dim++)
                            ordinatesToWrite[coordinatesToWrite * Dimension + dim] = encCoordinate[dim];
                        coordinatesToWrite++;
                    }
                }

                // Write 0's for required coordinates
                if (coordinatesToWrite < coordinatesRequired)
                    coordinatesToWrite = coordinatesRequired;

                // If we have more than one coordinate to write, report that!
                if (coordinatesToWrite > 1)
                    writer.Write(VarintBitConverter.GetVarintBytes((uint)coordinatesToWrite));

                // Write ordinate values
                coordinatesToWrite *= Dimension;
                for(int i = 0; i < coordinatesToWrite; i++)
                    writer.Write(VarintBitConverter.GetVarintBytes(ordinatesToWrite[i]));

                // return ordinate longs array
                ArrayPool<long>.Shared.Return(ordinatesToWrite);
            }

            public static long EncodeOrdinate(double value, double scale, ref long lastScaledValue)
            {
                long lngValue = (long)Math.Round(value * scale, 0, MidpointRounding.AwayFromZero);
                long valueEnc = lngValue - lastScaledValue;
                lastScaledValue = lngValue;
                return valueEnc;
            }

            /// <summary>
            /// Method to initialize the previous coordinate
            /// </summary>
            public void InitPrevCoordinate()
            {
                for (int i = 0; i < Dimension; i++)
                    _prevCoordinate[i] = 0;
            }
        }
    }
}

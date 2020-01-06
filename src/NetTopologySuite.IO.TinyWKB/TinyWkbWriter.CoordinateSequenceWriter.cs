using System;
using System.Buffers;
using System.IO;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO
{
    public partial class TinyWkbWriter
    {
        private class CoordinateSequenceWriter {

            private delegate void WriteVarintsFn(BinaryWriter writer, CoordinateSequence sequence, int index, double[] last);
            private readonly int _dimension;
            private readonly int _measures;

            private readonly WriteVarintsFn _writeVarintsFn;

            public CoordinateSequenceWriter(TinyWkbHeader header)
            {
                int dimension = 2;
                int measures = 0;
                double[] scales = ArrayPool<double>.Shared.Rent(4);
                scales[0] = scales[1] = header.ScaleX();
                if (header.HasExtendedPrecisionInformation)
                {
                    if (header.HasZ)
                    {
                        scales[dimension++] = header.ScaleZ();
                    }
                    if (header.HasM)
                    {
                        scales[dimension++] = header.ScaleM();
                        measures = 1;
                        if (dimension == 3)
                            _writeVarintsFn = WriteVarints2DM;
                        else
                            _writeVarintsFn = WriteVarints3DM;
                    }
                    else
                        _writeVarintsFn = WriteVarints3D;

                }
                else
                    _writeVarintsFn = WriteVarints2D;

                _dimension = dimension;
                _measures = measures;
                Scales = new ReadOnlySpan<double>(scales, 0, dimension).ToArray();

                ArrayPool<double>.Shared.Return(scales, true);
            }

            /// <summary>
            /// Gets an array to pass to <see cref="Write"/> function as parameter <c>last</c>.
            /// </summary>
            public int Dimension => _dimension;

            public int Measures => _measures;

            public double[] Scales { get; }

            public void Write(BinaryWriter writer, CoordinateSequence sequence, double[] last, int omit)
            {
                if (sequence == null || sequence.Count == 0)
                    return;

                int count = sequence.Count - omit;
                if (count > 1)
                    writer.Write(VarintBitConverter.GetVarintBytes((uint)count));
                for(int i = 0; i < count; i++)
                    _writeVarintsFn(writer, sequence, i,  last);
            }

            private void WriteVarints2D(BinaryWriter writer, CoordinateSequence sequence, int index, double[] last)
            {
                WriteOrdinate(writer, sequence.GetX(index), Scales[0], ref last[0]);
                WriteOrdinate(writer, sequence.GetY(index), Scales[1], ref last[1]);
            }

            private void WriteVarints2DM(BinaryWriter writer, CoordinateSequence sequence, int index, double[] last)
            {
                WriteOrdinate(writer, sequence.GetX(index), Scales[0], ref last[0]);
                WriteOrdinate(writer, sequence.GetY(index), Scales[1], ref last[1]);
                WriteOrdinate(writer, sequence.GetM(index), Scales[2], ref last[2]);
            }

            private void WriteVarints3D(BinaryWriter writer, CoordinateSequence sequence, int index, double[] last)
            {
                WriteOrdinate(writer, sequence.GetX(index), Scales[0], ref last[0]);
                WriteOrdinate(writer, sequence.GetY(index), Scales[1], ref last[1]);
                WriteOrdinate(writer, sequence.GetZ(index), Scales[2], ref last[2]);
            }

            private void WriteVarints3DM(BinaryWriter writer, CoordinateSequence sequence, int index, double[] last)
            {
                WriteOrdinate(writer, sequence.GetX(index), Scales[0], ref last[0]);
                WriteOrdinate(writer, sequence.GetY(index), Scales[1], ref last[1]);
                WriteOrdinate(writer, sequence.GetZ(index), Scales[2], ref last[2]);
                WriteOrdinate(writer, sequence.GetM(index), Scales[3], ref last[3]);
            }

            public static void WriteOrdinate(BinaryWriter writer, double value, double scale, ref double lastScaledValue)
            {
                value = Math.Round(value * scale, 0, MidpointRounding.AwayFromZero);
                double valueEnc = value - lastScaledValue;
                lastScaledValue = value;
                VarintBitConverter.WriteVarintBytesToBuffer(writer, (long)valueEnc);
            }
        }
    }
}

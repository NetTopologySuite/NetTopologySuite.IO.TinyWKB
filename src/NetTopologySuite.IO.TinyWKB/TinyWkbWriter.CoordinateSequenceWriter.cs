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
            private readonly double _scale12, _scale3, _scale4;
            private readonly int _dimension;
            private readonly WriteVarintsFn _writeVarintsFn;
            private TinyWkbWriter tinyWkbWriter;
            private Geometry geometry;

            public CoordinateSequenceWriter(TinyWkbWriter tinyWkbWriter, Geometry geometry)
            {
                this.tinyWkbWriter = tinyWkbWriter;
                this.geometry = geometry;
            }

            /// <summary>
            /// Gets an array to pass to <see cref="Write"/> function as parameter <c>last</c>.
            /// </summary>
            public double[] InitalLast => new double[_dimension];

            public void Write(BinaryWriter writer, CoordinateSequence sequence, double[] last, int omit)
            {
                if (sequence == null || sequence.Count == 0)
                    return;

                int count = sequence.Count - omit;
                for(int i = 0; i < count; i++)
                    _writeVarintsFn(writer, sequence, i,  last);
            }

            private void WriteVarints2D(BinaryWriter writer, CoordinateSequence sequence, int index, double[] last)
            {
                WriteOrdinate(writer, sequence.GetX(index), _scale12, ref last[0]);
                WriteOrdinate(writer, sequence.GetY(index), _scale12, ref last[1]);
            }

            private void WriteVarints2DM(BinaryWriter writer, CoordinateSequence sequence, int index, double[] last)
            {
                WriteOrdinate(writer, sequence.GetX(index), _scale12, ref last[0]);
                WriteOrdinate(writer, sequence.GetY(index), _scale12, ref last[1]);
                WriteOrdinate(writer, sequence.GetM(index), _scale3, ref last[2]);
            }

            private void WriteVarints3D(BinaryWriter writer, CoordinateSequence sequence, int index, double[] last)
            {
                WriteOrdinate(writer, sequence.GetX(index), _scale12, ref last[0]);
                WriteOrdinate(writer, sequence.GetY(index), _scale12, ref last[1]);
                WriteOrdinate(writer, sequence.GetZ(index), _scale3, ref last[2]);
            }

            private void WriteVarints3DM(BinaryWriter writer, CoordinateSequence sequence, int index, double[] last)
            {
                WriteOrdinate(writer, sequence.GetX(index), _scale12, ref last[0]);
                WriteOrdinate(writer, sequence.GetY(index), _scale12, ref last[1]);
                WriteOrdinate(writer, sequence.GetZ(index), _scale3, ref last[2]);
                WriteOrdinate(writer, sequence.GetM(index), _scale4, ref last[3]);
            }

            private static void WriteOrdinate(BinaryWriter writer, double value, double scale, ref double lastScaledValue)
            {
                value = Math.Round(value * scale, 0, MidpointRounding.AwayFromZero);
                var valueEnc = value - lastScaledValue;
                lastScaledValue = value;
                VarintBitConverter.WriteVarintBytesToBuffer(writer, (long)valueEnc);
            }
        }
    }
}

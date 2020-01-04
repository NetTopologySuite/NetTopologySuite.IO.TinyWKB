
using System;
using System.IO;
using System.Runtime.CompilerServices;
using NetTopologySuite.Geometries;

[assembly: InternalsVisibleTo("NetTopologySuite.IO.TinyWKB.Test")]

namespace NetTopologySuite.IO
{
    public partial class TinyWkbReader
    {
        private class CoordinateSequenceReader
        {
            private delegate double[] CoordinateReaderFn(BinaryReader reader, double descale12, double descale3, double descale4, double[] last);
            private delegate void CoordinateAddFn(CoordinateSequence sequence, int index, double[] coords);

            private readonly CoordinateSequenceFactory _coordinateSequenceFactory;
            private readonly CoordinateReaderFn _coordinateReaderFn;
            private readonly CoordinateAddFn _coordinateAddFn;
            private readonly double _descale12, _descale3, _descale4;
            private readonly int _dimension, _measures;

            public CoordinateSequenceReader(CoordinateSequenceFactory coordinateSequenceFactory, Header header, MetadataHeader mdhFlags, ExtendedPrecisionInformation epInfo)
            {
                _coordinateSequenceFactory = coordinateSequenceFactory;
                _descale12 = header.Descale;
                if (!mdhFlags.HasExtendedPrecisionInformation || !(epInfo.HasZ | epInfo.HasM))
                {
                    _coordinateReaderFn = ReadCoordinate2;
                    _dimension = 2;
                    _coordinateAddFn = AddXY;
                }
                else if (epInfo.HasZ && !epInfo.HasM)
                {
                    _descale3 = Math.Pow(10, -epInfo.PrecisionZ);
                    _coordinateReaderFn = ReadCoordinate3;
                    _dimension = 3;
                    _coordinateAddFn = AddXYZ;
                }
                else if (!epInfo.HasZ && epInfo.HasM)
                {
                    _descale3 = Math.Pow(10, -epInfo.PrecisionM);
                    _coordinateReaderFn = ReadCoordinate3;
                    _dimension = 3;
                    _measures = 1;
                    _coordinateAddFn = AddXYM;
                }
                else
                {
                    _descale3 = Math.Pow(10, -epInfo.PrecisionZ);
                    _descale4 = Math.Pow(10, -epInfo.PrecisionM);
                    _coordinateReaderFn = ReadCoordinate4;
                    _dimension = 4;
                    _measures = 1;
                    _coordinateAddFn = AddXYZM;
                }

            }


            public CoordinateSequence Read(BinaryReader reader, int count, ref double[] last, int buffer = 0)
            {
                var res = _coordinateSequenceFactory.Create(count + buffer, _dimension, _measures);
                for (int i = 0; i < count; i++)
                {
                    last = _coordinateReaderFn(reader, _descale12, _descale3, _descale4, last);
                    _coordinateAddFn(res, i, last);
                }

                return res;
            }


            private static double[] ReadCoordinate2(BinaryReader reader,
                double descale12, double descale3, double descale4,
                double[] lastRead)
            {
                double[] res = {
                    ToDouble(ReadVarint(reader), descale12),
                    ToDouble(ReadVarint(reader), descale12) };

                if (lastRead != null)
                {
                    res[0] += lastRead[0];
                    res[1] += lastRead[1];
                }

                return res;
            }
            private static double[] ReadCoordinate3(BinaryReader reader,
                double descale12, double descale3, double descale4,
                double[] lastRead)
            {
                double[] res = {
                    ToDouble(ReadVarint(reader), descale12),
                    ToDouble(ReadVarint(reader), descale12),
                    ToDouble(ReadVarint(reader), descale3) };

                if (lastRead != null)
                {
                    res[0] += lastRead[0];
                    res[1] += lastRead[1];
                    res[2] += lastRead[2];
                }

                return res;
            }
            private static double[] ReadCoordinate4(BinaryReader reader,
                double descale12, double descale3, double descale4,
                double[] lastRead)
            {
                double[] res = {
                    ToDouble(ReadVarint(reader), descale12),
                    ToDouble(ReadVarint(reader), descale12),
                    ToDouble(ReadVarint(reader), descale3),
                    ToDouble(ReadVarint(reader), descale4) };

                if (lastRead != null)
                {
                    res[0] += lastRead[0];
                    res[1] += lastRead[1];
                    res[2] += lastRead[2];
                    res[3] += lastRead[3];
                }

                return res;
            }

            private static void AddXY(CoordinateSequence sequence, int index, double[] ordinateValues)
            {
                sequence.SetX(index, ordinateValues[0]);
                sequence.SetY(index, ordinateValues[1]);
            }
            private static void AddXYZ(CoordinateSequence sequence, int index, double[] ordinateValues)
            {
                sequence.SetX(index, ordinateValues[0]);
                sequence.SetY(index, ordinateValues[1]);
                if (sequence.HasZ) sequence.SetZ(index, ordinateValues[2]);
            }
            private static void AddXYM(CoordinateSequence sequence, int index, double[] ordinateValues)
            {
                sequence.SetX(index, ordinateValues[0]);
                sequence.SetY(index, ordinateValues[1]);
                if (sequence.HasM) sequence.SetM(index, ordinateValues[2]);
            }
            private static void AddXYZM(CoordinateSequence sequence, int index, double[] ordinateValues)
            {
                sequence.SetX(index, ordinateValues[0]);
                sequence.SetY(index, ordinateValues[1]);
                if (sequence.HasZ) sequence.SetZ(index, ordinateValues[2]);
                if (sequence.HasM) sequence.SetM(index, ordinateValues[3]);
            }
        }
    }
}

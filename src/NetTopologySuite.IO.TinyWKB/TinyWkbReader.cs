
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NetTopologySuite.DataStructures;
using NetTopologySuite.Geometries;

[assembly: InternalsVisibleTo("NetTopologySuite.IO.TinyWKB.Test")]

namespace NetTopologySuite.IO
{
    /// <summary>
    /// A TWKB (Tiny Well-Known-Binary) format reader
    /// </summary>
    public partial class TinyWkbReader
    {
        private readonly GeometryFactory _factory;

        /// <summary>
        /// Creates an instance of this class using <see cref="GeometryFactory.Default"/> geometry factory.
        /// </summary>
        public TinyWkbReader()
            :this(GeometryFactory.Default)
        {
            
        }

        /// <summary>
        /// Creates an instance of this class using the provided <see cref="GeometryFactory"/>.
        /// </summary>
        /// <param name="factory">The geometry factory</param>
        public TinyWkbReader(GeometryFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Method to read a <see cref="Geometry"/> from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The input stream</param>
        /// <returns>A geometry</returns>
        public Geometry Read(Stream stream)
        {
            using (var br = new BinaryReader(stream, Encoding.UTF8, true))
                return Read(br);
        }

        /// <summary>
        /// Method to read a <see cref="Geometry"/> using a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The reader</param>
        /// <returns>A geometry</returns>
        public Geometry Read(BinaryReader reader)
        {
            // Read the common, extended header information
            var h = new Header(reader.ReadByte());
            var mdhFlags = new MetadataHeader(reader.ReadByte());
            var epInfo = mdhFlags.HasExtendedPrecisionInformation
                ? new ExtendedPrecisionInformation(reader.ReadByte())
                : new ExtendedPrecisionInformation();

            ulong size = mdhFlags.HasSize ? ReadUVarint(reader) : 0;
            if (mdhFlags.HasBoundingBox)
            {
                ReadBoundingBox(reader, h, mdhFlags, epInfo,
                    out _, out _, out _);
            }

            // Based on header information, build a coordinate sequence reader
            var coordinateReader = new CoordinateSequenceReader(_factory.CoordinateSequenceFactory, h, mdhFlags, epInfo);
            double[] last = null;
            switch (h.GeometryType)
            {
                case TinyWkbGeometryType.Point:
                    return ReadPoint(reader, coordinateReader);
                case TinyWkbGeometryType.LineString:
                    return ReadLineString(reader, coordinateReader, ref last);
                case TinyWkbGeometryType.Polygon:
                    return ReadPolygon(reader, coordinateReader, ref last);
                case TinyWkbGeometryType.MultiPoint:
                    return ReadMultiPoint(reader, mdhFlags, coordinateReader);
                case TinyWkbGeometryType.MultiLineString:
                    return ReadMultiLineString(reader, mdhFlags, coordinateReader);
                case TinyWkbGeometryType.MultiPolygon:
                    return ReadMultiPolygon(reader, mdhFlags, coordinateReader);
                case TinyWkbGeometryType.GeometryCollection:
                    return ReadGeometryCollection(reader, mdhFlags);
            }

            throw new NotSupportedException();
        }

        private Point ReadPoint(BinaryReader reader, CoordinateSequenceReader csReader)
        {
            double[] last = null;
            var sequence = csReader.Read(reader, 1, ref last);
            return _factory.CreatePoint(sequence);
        }
        private LineString ReadLineString(BinaryReader reader, CoordinateSequenceReader csReader, ref double[] last, int buffer = 0)
        {
            int numPoints = (int) ReadUVarint(reader);
            var sequence = csReader.Read(reader, numPoints, ref last, buffer);
            return _factory.CreateLineString(sequence);
        }

        private Polygon ReadPolygon(BinaryReader reader, CoordinateSequenceReader csReader, ref double[] last)
        {
            int numRings = (int)ReadUVarint(reader);
            if (numRings == 0) return _factory.CreatePolygon(null, null);

            int numPoints = (int)ReadUVarint(reader);
            var sequence = csReader.Read(reader, numPoints, ref last, 1);
            CoordinateSequences.CopyCoord(sequence, 0, sequence, numPoints);
            var shell = _factory.CreateLinearRing(sequence);
            var holes = new LinearRing[numRings - 1];
            if (numRings > 1)
            {
                for (int i = 0; i < holes.Length; i++)
                {
                    numPoints = (int)ReadUVarint(reader);
                    sequence = csReader.Read(reader, numPoints, ref last, 1);
                    CoordinateSequences.CopyCoord(sequence, 0, sequence, numPoints);
                    holes[i] = _factory.CreateLinearRing(sequence);
                }
            }

            return _factory.CreatePolygon(shell, holes);
        }

        private MultiPoint ReadMultiPoint(BinaryReader reader, MetadataHeader mdhFlags, CoordinateSequenceReader csReader)
        {
            int numPoints = (int) ReadUVarint(reader);
            long[] idList = ReadIdList(reader, mdhFlags, numPoints);
            double[] last = null;
            var sequence = csReader.Read(reader, numPoints, ref last);
            var res = _factory.CreateMultiPoint(sequence);
            for (int i = 0; i < numPoints; i++)
                res.GetGeometryN(i).UserData = idList[i];
            return res;
        }

        private MultiLineString ReadMultiLineString(BinaryReader reader, MetadataHeader mdhFlags, CoordinateSequenceReader csReader)
        {
            int numLineStrings = (int)ReadUVarint(reader);
            long[] idList = ReadIdList(reader, mdhFlags, numLineStrings);
            var lineStrings = new LineString[numLineStrings];
            double[] last = null;
            for (int i = 0; i < numLineStrings; i++)
            {
                lineStrings[i] = ReadLineString(reader, csReader, ref last);
                lineStrings[i].UserData = idList[i];
            }

            return _factory.CreateMultiLineString(lineStrings);
        }
        private MultiPolygon ReadMultiPolygon(BinaryReader reader, MetadataHeader mdhFlags, CoordinateSequenceReader csReader)
        {
            int numPolygons = (int)ReadUVarint(reader);
            long[] idList = ReadIdList(reader, mdhFlags, numPolygons);
            var polygons = new Polygon[numPolygons];

            double[] last = null;
            for (int i = 0; i < numPolygons; i++)
            {
                polygons[i] = ReadPolygon(reader, csReader, ref last);
                polygons[i].UserData = idList[i];
            }

            return _factory.CreateMultiPolygon(polygons);
        }

        private GeometryCollection ReadGeometryCollection(BinaryReader reader, MetadataHeader mdhFlags)
        {
            int numGeometries = (int) ReadUVarint(reader);
            var geometries = new Geometry[numGeometries];
            long[] idList = ReadIdList(reader, mdhFlags, numGeometries);
            for (int i = 0; i < numGeometries; i++)
            {
                geometries[i] = Read(reader);
                geometries[i].UserData = idList[i];
            }

            return _factory.CreateGeometryCollection(geometries);
        }

        private static void ReadBoundingBox(BinaryReader reader, Header h,
            MetadataHeader mdhFlags, ExtendedPrecisionInformation epInfo,
            out Envelope envelope, out Interval zInterval, out Interval mInterval)
        {
            envelope = ReadEnvelope(reader, h.Precision);
            zInterval = mdhFlags.HasExtendedPrecisionInformation && epInfo.HasZ
                ? ReadInterval(reader, epInfo.PrecisionZ)
                : Interval.Create();

            mInterval = mdhFlags.HasExtendedPrecisionInformation && epInfo.HasM
                ? ReadInterval(reader, epInfo.PrecisionM)
                : Interval.Create();
        }

        private static Envelope ReadEnvelope(BinaryReader reader, double scale)
        {
            double xmin = ToDouble(ReadVarint(reader), scale);
            double xmax = xmin += ToDouble(ReadVarint(reader), scale);
            double ymin = ToDouble(ReadVarint(reader), scale);
            double ymax = ymin += ToDouble(ReadVarint(reader), scale);

            return new Envelope(xmin, xmax, ymin, ymax);
        }

        private long[] ReadIdList(BinaryReader reader, MetadataHeader mdhFlags, int numGeometries)
        {
            long[] res = new long[numGeometries];
            for (int i = 0; i < numGeometries; i++)
                res[i] = mdhFlags.HasIdList ? ReadVarint(reader) : i;
            return res;
        }

        private static Interval ReadInterval(BinaryReader reader, double scale)
        {
            double min = ToDouble(ReadVarint(reader), scale);
            double max = min += ToDouble(ReadVarint(reader), scale);

            return Interval.Create(min, max);
        }

        private static double ToDouble(long value, double descale)
        {
            return value * descale;
        }

        private static long ReadVarint(BinaryReader reader)
        {
            byte[] data = ReadVarintData(reader);
            return VarintBitConverter.ToInt64(data);
        }
        private static ulong ReadUVarint(BinaryReader reader)
        {
            byte[] data = ReadVarintData(reader);
            return VarintBitConverter.ToUInt64(data);
        }

        private static byte[] ReadVarintData(BinaryReader reader)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(10);
            int i = 0;
            while (i < 10)
            {
                if (((buffer[i++] = reader.ReadByte()) & 0x80) == 0)
                    break;
            }

            if (i >= 10)
                throw new InvalidDataException();

            byte[] res = new ReadOnlySpan<byte>(buffer, 0, i).ToArray();
            ArrayPool<byte>.Shared.Return(buffer);

            return res;
        }
    }
}

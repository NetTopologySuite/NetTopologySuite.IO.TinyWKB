
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using NetTopologySuite.DataStructures;
using NetTopologySuite.Geometries;

//[assembly: InternalsVisibleTo("NetTopologySuite.IO.TinyWKB.Test")]

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
        /// Method to read a <see cref="Geometry"/> from a <paramref name="buffer"/>. Optionally a starting position can be supplied.
        /// </summary>
        /// <param name="buffer">The input stream</param>
        /// <param name="position">The starting position</param>
        /// <param name="seekOrigin">A value indicating how to interpret <paramref name="position"/></param>
        /// <returns>A geometry</returns>
        public Geometry Read(byte[] buffer, long position = 0, SeekOrigin seekOrigin = SeekOrigin.Begin)
        {
            using (var ms = new MemoryStream(buffer))
            {
                if (position != 0)
                    ms.Seek(position, seekOrigin);
                return Read(ms);
            }
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
            var header = TinyWkbHeader.Read(reader);

            // Since we don't do anything with size just drop it
            if (header.HasSize) ReadUVarint(reader);

            // If is empty bail out
            if (header.HasEmptyGeometry)
                return CreateEmpty(header);

            // If we did sth. with size this would be correct.
            /*ulong size = header.HasSize ? ReadUVarint(reader) : 0;*/

            if (header.HasBoundingBox)
            {
                ReadBoundingBox(reader, header, out _, out _, out _);
            }

            // Based on header information, build a coordinate sequence reader
            var coordinateReader = new CoordinateSequenceReader(_factory.CoordinateSequenceFactory, header);
            switch (header.GeometryType)
            {
                case TinyWkbGeometryType.Point:
                    return ReadPoint(reader, coordinateReader);
                case TinyWkbGeometryType.LineString:
                    return ReadLineString(reader, coordinateReader);
                case TinyWkbGeometryType.Polygon:
                    return ReadPolygon(reader, coordinateReader);
                case TinyWkbGeometryType.MultiPoint:
                    return ReadMultiPoint(reader, header, coordinateReader);
                case TinyWkbGeometryType.MultiLineString:
                    return ReadMultiLineString(reader, header, coordinateReader);
                case TinyWkbGeometryType.MultiPolygon:
                    return ReadMultiPolygon(reader, header, coordinateReader);
                case TinyWkbGeometryType.GeometryCollection:
                    return ReadGeometryCollection(reader, header);
            }

            throw new NotSupportedException();
        }

        private Geometry CreateEmpty(TinyWkbHeader header)
        {
            switch (header.GeometryType)
            {
                case TinyWkbGeometryType.Point:
                    return _factory.CreatePoint((CoordinateSequence) null);
                case TinyWkbGeometryType.LineString:
                    return _factory.CreateLineString((CoordinateSequence)null);
                case TinyWkbGeometryType.Polygon:
                    return _factory.CreatePolygon((CoordinateSequence)null);
                case TinyWkbGeometryType.MultiPoint:
                    return _factory.CreateMultiPoint((CoordinateSequence)null);
                case TinyWkbGeometryType.MultiLineString:
                    return _factory.CreateMultiLineString(null);
                case TinyWkbGeometryType.MultiPolygon:
                    return _factory.CreateMultiPolygon(null);
                case TinyWkbGeometryType.GeometryCollection:
                    return _factory.CreateGeometryCollection(null);
            }
            throw new ArgumentException("Invalid geometry type specified in header", nameof(header));
        }

        private Point ReadPoint(BinaryReader reader, CoordinateSequenceReader csReader)
        {
            var sequence = csReader.Read(reader, 1);
            return _factory.CreatePoint(sequence);
        }
        private LineString ReadLineString(BinaryReader reader, CoordinateSequenceReader csReader)
        {
            int numPoints = (int) ReadUVarint(reader);
            var sequence = csReader.Read(reader, numPoints);
            return _factory.CreateLineString(sequence);
        }

        private Polygon ReadPolygon(BinaryReader reader, CoordinateSequenceReader csReader)
        {
            int numRings = (int)ReadUVarint(reader);
            if (numRings == 0) return _factory.CreatePolygon(null, null);

            int numPoints = (int)ReadUVarint(reader);
            var sequence = csReader.Read(reader, numPoints, closeRing: true);
            CoordinateSequences.CopyCoord(sequence, 0, sequence, numPoints);
            var shell = _factory.CreateLinearRing(sequence);
            var holes = new LinearRing[numRings - 1];
            if (numRings > 1)
            {
                for (int i = 0; i < holes.Length; i++)
                {
                    numPoints = (int)ReadUVarint(reader);
                    sequence = csReader.Read(reader, numPoints, closeRing: true);
                    CoordinateSequences.CopyCoord(sequence, 0, sequence, numPoints);
                    holes[i] = _factory.CreateLinearRing(sequence);
                }
            }

            return _factory.CreatePolygon(shell, holes);
        }

        private MultiPoint ReadMultiPoint(BinaryReader reader, TinyWkbHeader header, CoordinateSequenceReader csReader)
        {
            int numPoints = (int) ReadUVarint(reader);
            long[] idList = ReadIdList(reader, header, numPoints);
            var sequence = csReader.Read(reader, numPoints);
            var res = _factory.CreateMultiPoint(sequence);
            for (int i = 0; i < numPoints; i++)
                res.GetGeometryN(i).UserData = idList[i];
            return res;
        }

        private MultiLineString ReadMultiLineString(BinaryReader reader, TinyWkbHeader header, CoordinateSequenceReader csReader)
        {
            int numLineStrings = (int)ReadUVarint(reader);
            long[] idList = ReadIdList(reader, header, numLineStrings);
            var lineStrings = new LineString[numLineStrings];
            for (int i = 0; i < numLineStrings; i++)
            {
                lineStrings[i] = ReadLineString(reader, csReader);
                lineStrings[i].UserData = idList[i];
            }

            return _factory.CreateMultiLineString(lineStrings);
        }
        private MultiPolygon ReadMultiPolygon(BinaryReader reader, TinyWkbHeader header, CoordinateSequenceReader csReader)
        {
            int numPolygons = (int)ReadUVarint(reader);
            long[] idList = ReadIdList(reader, header, numPolygons);
            var polygons = new Polygon[numPolygons];

            for (int i = 0; i < numPolygons; i++)
            {
                polygons[i] = ReadPolygon(reader, csReader);
                polygons[i].UserData = idList[i];
            }

            return _factory.CreateMultiPolygon(polygons);
        }

        private GeometryCollection ReadGeometryCollection(BinaryReader reader, TinyWkbHeader header)
        {
            int numGeometries = (int) ReadUVarint(reader);
            var geometries = new Geometry[numGeometries];
            long[] idList = ReadIdList(reader, header, numGeometries);
            for (int i = 0; i < numGeometries; i++)
            {
                geometries[i] = Read(reader);
                geometries[i].UserData = idList[i];
            }

            return _factory.CreateGeometryCollection(geometries);
        }

        private static void ReadBoundingBox(BinaryReader reader, TinyWkbHeader header,
            out Envelope envelope, out Interval zInterval, out Interval mInterval)
        {
            envelope = ReadEnvelope(reader, header.DescaleX());
            zInterval = header.HasExtendedPrecisionInformation && header.HasZ
                ? ReadInterval(reader, header.DescaleZ())
                : Interval.Create();

            mInterval = header.HasExtendedPrecisionInformation && header.HasM
                ? ReadInterval(reader, header.DescaleM())
                : Interval.Create();
        }

        private static Envelope ReadEnvelope(BinaryReader reader, double scale)
        {
            double xmin = ToDouble(ReadVarint(reader), scale);
            double xmax = xmin + ToDouble(ReadVarint(reader), scale);
            double ymin = ToDouble(ReadVarint(reader), scale);
            double ymax = ymin + ToDouble(ReadVarint(reader), scale);

            return new Envelope(xmin, xmax, ymin, ymax);
        }

        private long[] ReadIdList(BinaryReader reader, TinyWkbHeader header, int numGeometries)
        {
            long[] res = new long[numGeometries];
            for (int i = 0; i < numGeometries; i++)
                res[i] = header.HasIdList ? ReadVarint(reader) : i;
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
            Span<byte> buffer = stackalloc byte[9];
            return VarintBitConverter.ToInt64(ReadVarintData(reader, buffer));
        }
        private static ulong ReadUVarint(BinaryReader reader)
        {
            Span<byte> buffer = stackalloc byte[9];
            return VarintBitConverter.ToUInt64(ReadVarintData(reader, buffer));
        }

        private static ReadOnlySpan<byte> ReadVarintData(BinaryReader reader, Span<byte> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (((buffer[i] = reader.ReadByte()) & 0x80) == 0)
                {
                    return buffer.Slice(0, i + 1);
                }
            }

            throw new InvalidDataException();
        }
    }
}

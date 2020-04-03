using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NetTopologySuite.DataStructures;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;

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
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                    return Read(br);
            }
        }

        /// <summary>
        /// Method to read a <see cref="Geometry"/> from a <paramref name="buffer"/>. Optionally a starting position can be supplied.
        /// </summary>
        /// <param name="buffer">The input stream</param>
        /// <param name="idList"></param>
        /// <param name="position">The starting position</param>
        /// <param name="seekOrigin">A value indicating how to interpret <paramref name="position"/></param>
        /// <returns>A geometry</returns>
        public Geometry Read(byte[] buffer, out IList<long> idList, long position = 0, SeekOrigin seekOrigin = SeekOrigin.Begin)
        {
            using (var ms = new MemoryStream(buffer))
            {
                if (position != 0)
                    ms.Seek(position, seekOrigin);
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                    return Read(br, out idList, true);
            }
        }
        /// <summary>
        /// Method to read a <see cref="Geometry"/> from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The input stream</param>
        /// <returns>A geometry</returns>
        public Geometry Read(Stream stream)
        {
            return Read(stream, out _);
        }

        /// <summary>
        /// Method to read a <see cref="Geometry"/> from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The input stream</param>
        /// <param name="idList">An array of identifiers if present</param>
        /// <returns>A geometry</returns>
        public Geometry Read(Stream stream, out IList<long> idList)
        {
            using (var br = new BinaryReader(stream, Encoding.UTF8, true))
                return Read(br, out idList, true);
        }

        /// <summary>
        /// Method to read a <see cref="Geometry"/> using a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The reader</param>
        /// <returns>A geometry</returns>
        public Geometry Read(BinaryReader reader)
        {
            return Read(reader, out _);
        }

        /// <summary>
        /// Method to read a <see cref="Geometry"/> using a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The reader</param>
        /// <param name="idList">An array of identifiers if present</param>
        /// <param name="exportIdList">A flag indicating if the list of ids should be exported.</param>
        /// <returns>A geometry</returns>
        private Geometry Read(BinaryReader reader, out IList<long> idList, bool exportIdList = false)
        {
            idList = null;

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
            GeometryCollection gc = null;
            switch (header.GeometryType)
            {
                case TinyWkbGeometryType.Point:
                    return ReadPoint(reader, coordinateReader);
                case TinyWkbGeometryType.LineString:
                    return ReadLineString(reader, coordinateReader);
                case TinyWkbGeometryType.Polygon:
                    return ReadPolygon(reader, coordinateReader);
                case TinyWkbGeometryType.MultiPoint:
                    gc = ReadMultiPoint(reader, header, coordinateReader, out idList);
                    break;
                case TinyWkbGeometryType.MultiLineString:
                    gc = ReadMultiLineString(reader, header, coordinateReader, out idList);
                    break;
                case TinyWkbGeometryType.MultiPolygon:
                    gc = ReadMultiPolygon(reader, header, coordinateReader, out idList);
                    break;
                case TinyWkbGeometryType.GeometryCollection:
                    gc =  ReadGeometryCollection(reader, header, out idList);
                    break;
            }

            if (gc == null)
                Assert.ShouldNeverReachHere();

            // If we have an idList and don't want to export it, use alternatives
            if (idList != null && !exportIdList)
            {
                var handler = IdentifiersProvided;
                if (handler != null)
                {
                    handler.Invoke(this, new IdentifiersEventArgs(gc, idList));
                }
                else
                {
                    for (int i = 0; i < gc.NumGeometries; i++)
                        gc.GetGeometryN(i).UserData = idList[i];
                }
            }

            // invalidate idList if we don't want to export it.
            if (!exportIdList)
                idList = null;

            return gc;
        }

        private Geometry CreateEmpty(TinyWkbHeader header)
        {
            switch (header.GeometryType)
            {
                case TinyWkbGeometryType.Point:
                    return _factory.CreatePoint((CoordinateSequence)null);
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
            var shell = _factory.CreateLinearRing(sequence);
            var holes = new LinearRing[numRings - 1];
            if (numRings > 1)
            {
                for (int i = 0; i < holes.Length; i++)
                {
                    numPoints = (int)ReadUVarint(reader);
                    sequence = csReader.Read(reader, numPoints, closeRing: true);
                    holes[i] = _factory.CreateLinearRing(sequence);
                }
            }

            return _factory.CreatePolygon(shell, holes);
        }

        /// <summary>
        /// Event raised when a list of ids was provided.
        /// </summary>
        public event EventHandler<IdentifiersEventArgs> IdentifiersProvided;

        private MultiPoint ReadMultiPoint(BinaryReader reader, TinyWkbHeader header, CoordinateSequenceReader csReader, out IList<long> idList)
        {
            int numPoints = (int) ReadUVarint(reader);
            idList = ReadIdList(reader, header, numPoints);
            var sequence = csReader.Read(reader, numPoints);
            var res = _factory.CreateMultiPoint(sequence);

            return res;
        }

        private MultiLineString ReadMultiLineString(BinaryReader reader, TinyWkbHeader header,
            CoordinateSequenceReader csReader, out IList<long> idList)
        {
            int numLineStrings = (int)ReadUVarint(reader);
            idList = ReadIdList(reader, header, numLineStrings);
            var lineStrings = new LineString[numLineStrings];
            for (int i = 0; i < numLineStrings; i++)
                lineStrings[i] = ReadLineString(reader, csReader);

            return _factory.CreateMultiLineString(lineStrings);
        }
        private MultiPolygon ReadMultiPolygon(BinaryReader reader, TinyWkbHeader header,
            CoordinateSequenceReader csReader, out IList<long> idList)
        {
            int numPolygons = (int)ReadUVarint(reader);
            idList = ReadIdList(reader, header, numPolygons);
            var polygons = new Polygon[numPolygons];

            for (int i = 0; i < numPolygons; i++)
                polygons[i] = ReadPolygon(reader, csReader);

            return _factory.CreateMultiPolygon(polygons);
        }

        private GeometryCollection ReadGeometryCollection(BinaryReader reader, TinyWkbHeader header, out IList<long> idList)
        {
            int numGeometries = (int) ReadUVarint(reader);
            var geometries = new Geometry[numGeometries];
            idList = ReadIdList(reader, header, numGeometries);
            for (int i = 0; i < numGeometries; i++)
                geometries[i] = Read(reader);

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
            if (!header.HasIdList) return null;

            long[] res = new long[numGeometries];
            for (int i = 0; i < numGeometries; i++)
                res[i] = ReadVarint(reader);
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

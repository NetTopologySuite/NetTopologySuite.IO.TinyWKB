using System;
using System.Globalization;
using System.IO;
using System.Text;
using NetTopologySuite.DataStructures;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO
{
    public partial class TinyWkbWriter
    {
        /// <summary>
        /// Creates an instance of this writer using the provided arguments
        /// </summary>
        /// <param name="precisionXY"></param>
        /// <param name="emitZ"></param>
        /// <param name="precisionZ"></param>
        /// <param name="emitM"></param>
        /// <param name="precisionM"></param>
        /// <param name="emitSize"></param>
        /// <param name="emitBoundingBox"></param>
        /// <param name="emitIdList"></param>
        public TinyWkbWriter(int precisionXY = 7, 
            bool emitZ = false, int precisionZ = 3, bool emitM = false, int precisionM = 3, 
            bool emitSize = false, bool emitBoundingBox = true, bool emitIdList = false)
        {
            PrecisionXY = precisionXY;
            EmitZ = emitZ;
            PrecisionZ = precisionZ;
            EmitM = emitM;
            PrecisionM = precisionM;
            EmitSize = emitSize;
            EmitBoundingBox = emitBoundingBox;
            EmitIdList = emitIdList;
        }

        /// <summary>
        /// Gets a value indicating the precision of the x- and y-ordinate values.
        /// </summary>
        public int PrecisionXY { get; }

        /// <summary>
        /// Gets a value indicating the precision of the z-ordinate values. 
        /// </summary>
        /// <remarks>This value is only meaningful if <see cref="EmitZ"/> is <c>true</c>.</remarks>
        public int PrecisionZ { get; }

        /// <summary>
        /// Gets a value indicating the precision of the m-ordinate values. 
        /// </summary>
        /// <remarks>This value is only meaningful if <see cref="EmitM"/> is <c>true</c>.</remarks>
        public int PrecisionM { get; }

        /// <summary>
        /// Gets a value indicating if the writer should write bounding box information.
        /// </summary>
        public bool EmitBoundingBox { get; }

        /// <summary>
        /// Gets a value if z-ordinate values should be written, if present.
        /// </summary>
        public bool EmitZ { get; }

        /// <summary>
        /// Gets a value if m-ordinate values should be written, if present.
        /// </summary>
        public bool EmitM { get; }

        /// <summary>
        /// Gets a value indicating if size information should be written.
        /// </summary>
        public bool EmitSize { get; }

        /// <summary>
        /// Gets a value indicating if an id-list for multi-part geometries should be written.
        /// </summary>
        public bool EmitIdList { get; }

        public byte[] Write(Geometry geometry)
        {
            using (var ms = new MemoryStream())
            {
                Write(ms, geometry);
                return ms.ToArray();
            }
        }

        public void Write(Stream stream, Geometry geometry)
        {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                Write(writer, geometry);
        }

        private void Write(BinaryWriter writer, Geometry geometry)
        {
            var header = ToHeader(geometry.OgcGeometryType);
            var mdhFlags = ToMetadataHeader(geometry, out var epi);

            writer.Write(header.Value);
            writer.Write(mdhFlags.Value);
            if (mdhFlags.HasExtendedPrecisionInformation) writer.Write(epi.Value);

            // If geometry is empty, no more information needs to be written.
            if (geometry.IsEmpty) return;

            // If we need to emit size, write geometry information to memory stream
            if (EmitSize)
            {
                using (var ms = new MemoryStream())
                {
                    using (var geomWriter = new BinaryWriter(ms, Encoding.UTF8, true))
                    {
                        WriteGeometry(geomWriter, header, mdhFlags, epi, geometry);
                    }
                    writer.Write(VarintBitConverter.GetVarintBytes(ms.Length));
                    writer.Write(ms.GetBuffer(), 0, (int)ms.Position);
                }
            } 
            // If not, just write geometry
            else
            {
                WriteGeometry(writer, header, mdhFlags, epi, geometry);
            }
        }

        private void WriteGeometry(BinaryWriter writer, Header header, MetadataHeader mdhFlags, ExtendedPrecisionInformation epi, Geometry geometry)
        {
            if (geometry.OgcGeometryType == OgcGeometryType.GeometryCollection)
            {
                WriteGeometryCollection(writer, header, mdhFlags, epi, (GeometryCollection)geometry);
                return;
            }

            var csWriter = new CoordinateSequenceWriter(header, mdhFlags, epi);
            double[] initialLast = new double[csWriter.Dimension];//System.Buffers.ArrayPool<double>.Shared.Rent(csWriter.Dimension);
            switch (geometry.OgcGeometryType)
            {
                case OgcGeometryType.Point:
                    WritePoint(writer, csWriter, (Point)geometry, initialLast);
                    break;
                case OgcGeometryType.LineString:
                    WriteLineString(writer, csWriter, (LineString)geometry, initialLast);
                    break;
                case OgcGeometryType.Polygon:
                    WritePolygon(writer, csWriter, (Polygon)geometry, initialLast);
                    break;
                case OgcGeometryType.MultiPoint:
                    WriteMultiPoint(writer, csWriter, (MultiPoint)geometry, initialLast);
                    break;
                case OgcGeometryType.MultiLineString:
                    WriteMultiLineString(writer, csWriter, (MultiLineString)geometry, initialLast);
                    break;
                case OgcGeometryType.MultiPolygon:
                    WriteMultiPolygon(writer, csWriter, (MultiPolygon)geometry, initialLast);
                    break;
                default:
                    throw new ArgumentException(nameof(geometry));
            }

            //System.Buffers.ArrayPool<double>.Shared.Return(initialLast, true);
        }

        private void WritePoint(BinaryWriter writer, CoordinateSequenceWriter csWriter, Point point, double[] last)
        {
            // We don't write bounding boxes for points. Period.
            csWriter.Write(writer, point.CoordinateSequence, last, 0);
        }

        private void WriteLineString(BinaryWriter writer, CoordinateSequenceWriter csWriter, LineString lineString, double[] last)
        {
            WriteBoundingBox(writer, csWriter, lineString);
            csWriter.Write(writer, lineString.CoordinateSequence, last, 0);
        }

        private void WritePolygon(BinaryWriter writer, CoordinateSequenceWriter csWriter, Polygon polygon, double[] last, bool omitBoundingBox = false)
        {
            if (!omitBoundingBox) WriteBoundingBox(writer, csWriter, polygon);
            writer.Write(VarintBitConverter.GetVarintBytes(1 + polygon.NumInteriorRings));
            csWriter.Write(writer, polygon.Shell.CoordinateSequence, last, 1);
            for (int i = 0; i < polygon.NumInteriorRings; i++)
                csWriter.Write(writer, polygon.GetInteriorRingN(i).CoordinateSequence, last, 1);
        }

        private void WriteMultiPoint(BinaryWriter writer, CoordinateSequenceWriter csWriter, MultiPoint multiPoint, double[] last)
        {
            WriteBoundingBox(writer, csWriter, multiPoint);
            writer.Write(VarintBitConverter.GetVarintBytes(multiPoint.NumGeometries));
            WriteIdList(writer, multiPoint);
            for (int i = 0; i < multiPoint.NumGeometries; i++)
                csWriter.Write(writer, ((Point)multiPoint.GetGeometryN(i)).CoordinateSequence, last, 0);
        }

        private void WriteMultiLineString(BinaryWriter writer, CoordinateSequenceWriter csWriter, MultiLineString multiLineString, double[] last)
        {
            WriteBoundingBox(writer, csWriter, multiLineString);
            writer.Write(VarintBitConverter.GetVarintBytes(multiLineString.NumGeometries));
            WriteIdList(writer, multiLineString);
            for (int i = 0; i < multiLineString.NumGeometries; i++)
                csWriter.Write(writer, ((LineString)multiLineString.GetGeometryN(i)).CoordinateSequence, last, 0);
        }

        private void WriteMultiPolygon(BinaryWriter writer, CoordinateSequenceWriter csWriter, MultiPolygon multiPolygon, double[] last)
        {
            WriteBoundingBox(writer, csWriter, multiPolygon);
            writer.Write(VarintBitConverter.GetVarintBytes(multiPolygon.NumGeometries));
            WriteIdList(writer, multiPolygon);
            for (int i = 0; i < multiPolygon.NumGeometries; i++)
                WritePolygon(writer, csWriter, (Polygon)multiPolygon.GetGeometryN(i), last, true);
        }

        private void WriteGeometryCollection(BinaryWriter writer, Header header, MetadataHeader mdhFlags, ExtendedPrecisionInformation epi, GeometryCollection gc)
        {
            var csWriter = new CoordinateSequenceWriter(header, mdhFlags, epi);
            WriteBoundingBox(writer, csWriter, gc);
            writer.Write(VarintBitConverter.GetVarintBytes(gc.NumGeometries));
            WriteIdList(writer, gc);
            for (int i = 0; i < gc.NumGeometries; i++)
                WriteGeometry(writer, header, mdhFlags, epi, gc.GetGeometryN(i));
        }

        private void WriteBoundingBox(BinaryWriter writer, CoordinateSequenceWriter csWriter, Geometry geometry)
        {
            if (!EmitBoundingBox) return;

            var minMaxFilter = new MinMaxFilter(csWriter.Dimension, csWriter.Measures);
            geometry.Apply(minMaxFilter);

            for (int i = 0; i < csWriter.Dimension; i++)
                WriteInterval(writer, minMaxFilter[i], csWriter.Scales[i]);

        }

        private static long _nextId = 1;

        private static long GetNextId()
        {
            return System.Threading.Interlocked.Increment(ref _nextId);
        }

        private void WriteIdList(BinaryWriter writer, GeometryCollection gc)
        {
            if (!EmitIdList) return;

            long[] idList = new long[gc.Count];
            try
            {
                for (int i = 0; i < gc.Count; i++)
                {
                    var userData = gc.GetGeometryN(i)?.UserData;
                    if (userData is IConvertible convertibleUserData)
                        idList[i] = convertibleUserData.ToInt64(NumberFormatInfo.InvariantInfo);
                    else
                        throw new Exception("No Id in UserData");
                }
            } 
            catch
            {
                for (int i = 0; i < gc.Count; i++)
                    idList[i] = GetNextId();
            }

            for (int i = 0; i < gc.Count; i++)
                writer.Write(VarintBitConverter.GetVarintBytes(idList[i]));
        }

        private void WriteInterval(BinaryWriter writer, Interval interval, double scale)
        {
            double last = 0d;
            CoordinateSequenceWriter.WriteOrdinate(writer, interval.Min, scale, ref last);
            CoordinateSequenceWriter.WriteOrdinate(writer, interval.Max, scale, ref last);
        }

        private class MinMaxFilter : ICoordinateSequenceFilter
        {
            private readonly int _dimension, _measureIndex;
            private readonly Interval[] _intervals;

            public MinMaxFilter(int dimension, int measures)
            {
                _dimension = dimension - measures;
                _measureIndex = measures > 0 ? _dimension : -1;
                _intervals = new Interval[dimension];
                for (int i = 0; i < dimension; i++)
                    _intervals[i] = Interval.Create();
            }

            public bool Done => false;

            public bool GeometryChanged => false;

            public void Filter(CoordinateSequence seq, int i)
            {
                for (int j = 0; j < _dimension; j++)
                    _intervals[j] = _intervals[j].ExpandedByValue(seq.GetOrdinate(i, j));
                if (_measureIndex >= 0)
                    _intervals[_measureIndex] = _intervals[_measureIndex].ExpandedByValue(seq.GetM(i));
            }

            public Interval this[int dimension] { get { return _intervals[dimension]; } }
        }


        private Header ToHeader(OgcGeometryType type)
        {
            TinyWkbGeometryType res;
            switch (type)
            {
                case OgcGeometryType.Point:
                    res = TinyWkbGeometryType.Point;
                    break;
                case OgcGeometryType.LineString:
                    res = TinyWkbGeometryType.LineString;
                    break;
                case OgcGeometryType.Polygon:
                    res = TinyWkbGeometryType.Polygon;
                    break;
                case OgcGeometryType.MultiPoint:
                    res = TinyWkbGeometryType.MultiPoint;
                    break;
                case OgcGeometryType.MultiLineString:
                    res = TinyWkbGeometryType.MultiLineString;
                    break;
                case OgcGeometryType.MultiPolygon:
                    res = TinyWkbGeometryType.MultiPolygon;
                    break;
                case OgcGeometryType.GeometryCollection:
                    res = TinyWkbGeometryType.GeometryCollection;
                    break;
                default:
                    throw new NotSupportedException();
            }
            return new Header(res, PrecisionXY);
        }

        private MetadataHeader ToMetadataHeader(Geometry geometry, out ExtendedPrecisionInformation epi)
        {
            Coordinate c = geometry.Coordinate;
            bool hasZ = false, hasM = false;
            int dimension = Coordinates.Dimension(c);
            if (dimension > 2)
            {
                int measures = Coordinates.Measures(c);
                if (dimension == 3 && measures == 0)
                    hasZ = true;
                else if (dimension == 3 && measures == 1)
                    hasM = true;
                else {
                    hasZ = true;
                    hasM = true;
                }
            }

            hasZ &= EmitZ;
            hasM &= EmitM;
            
            epi = (hasZ | hasM) ? new ExtendedPrecisionInformation(hasZ, PrecisionZ, hasM, PrecisionM) : new ExtendedPrecisionInformation();

            return new MetadataHeader(
                geometry.OgcGeometryType != OgcGeometryType.Point,
                EmitSize, EmitIdList,
                (hasZ | hasM),
                geometry.IsEmpty);
        }
    }
}

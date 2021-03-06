using System;
using System.Globalization;
using System.IO;
using System.Text;
using NetTopologySuite.DataStructures;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO
{
    /// <summary>
    /// A writer class to encode <see cref="Geometry"/> into TinyWKB format.
    /// </summary>
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
            bool emitZ = true, int precisionZ = 7, bool emitM = true, int precisionM = 7, 
            bool emitSize = false, bool emitBoundingBox = true, bool emitIdList = false)
        {
            if (precisionXY < -7 || 7 < precisionXY)
                throw new ArgumentOutOfRangeException(nameof(precisionXY), precisionXY, "Geometry precision must be in the range [-7, 7]");

            if (precisionZ < 0 || 7 < precisionZ)
                throw new ArgumentOutOfRangeException(nameof(precisionZ), precisionZ, "Z-ordinate precision must be in the range [0, 7]");

            if (precisionM < 0 || 7 < precisionM)
                throw new ArgumentOutOfRangeException(nameof(precisionM), precisionM, "M-ordinate precision must be in the range [0, 7]");

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

        /// <summary>
        /// Function to write <paramref name="geometry"/> to an array of bytes.
        /// </summary>
        /// <param name="geometry">A geometry</param>
        /// <returns><paramref name="geometry"/> encoded in an array of bytes</returns>
        public byte[] Write(Geometry geometry)
        {
            return Write(geometry, null);
        }

        /// <summary>
        /// Function to write <paramref name="geometry"/> to an array of bytes.
        /// </summary>
        /// <param name="geometry">A geometry</param>
        /// <param name="idList">A list of ids for geometries an a collection. Not used for atomic geometries</param>
        /// <returns><paramref name="geometry"/> encoded in an array of bytes</returns>
        public byte[] Write(Geometry geometry, long[] idList)
        {
            using (var ms = new MemoryStream())
            {
                Write(ms, geometry, idList);
                return ms.ToArray();
            }
        }
        /// <summary>
        /// Method to write <paramref name="geometry"/> TinyWKB encoded to a stream.
        /// </summary>
        /// <param name="stream">A stream</param>
        /// <param name="geometry">A geometry</param>
        public void Write(Stream stream, Geometry geometry)
        {
            Write(stream, geometry, null);
        }

        /// <summary>
        /// Method to write <paramref name="geometry"/> TinyWKB encoded to a stream.
        /// </summary>
        /// <param name="stream">A stream</param>
        /// <param name="geometry">A geometry</param>
        /// <param name="idList">A list of ids for geometries an a collection. Not used for atomic geometries</param>
        public void Write(Stream stream, Geometry geometry, long[] idList)
        {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                Write(writer, geometry, idList);
        }

        /// <summary>
        /// Method to write <paramref name="geometry"/> TinyWKB encoded using a binary writer.
        /// </summary>
        /// <param name="writer">A binary writer</param>
        /// <param name="geometry">A geometry</param>
        public void Write(BinaryWriter writer, Geometry geometry)
        {
            Write(writer, geometry, null);
        }
        /// <summary>
        /// Method to write <paramref name="geometry"/> TinyWKB encoded using a binary writer.
        /// </summary>
        /// <param name="writer">A binary writer</param>
        /// <param name="geometry">A geometry</param>
        /// <param name="idList">A list of ids for geometries an a collection. Not used for atomic geometries</param>
        public void Write(BinaryWriter writer, Geometry geometry, long[] idList)
        {
            // Create and write header
            idList = GetIdList(geometry, idList);
            var header = ToHeader(geometry, idList != null);
            TinyWkbHeader.Write(writer, header);

            // If we need to emit size, write geometry information to memory stream
            if (header.HasSize)
            {
                using (var ms = new MemoryStream())
                {
                    using (var geomWriter = new BinaryWriter(ms, Encoding.UTF8, true))
                    {
                        WriteGeometry(geomWriter, header, geometry, idList);
                    }
                    writer.Write(VarintBitConverter.GetVarintBytes((ulong)ms.Length));
                    writer.Write(ms.GetBuffer(), 0, (int)ms.Position);
                }
            } 
            // If not, just write geometry
            else
            {
                WriteGeometry(writer, header, geometry, idList);
            }
        }

        private void WriteGeometry(BinaryWriter writer, TinyWkbHeader header, Geometry geometry, long[] idList)
        {
            // If geometry is empty, no more information needs to be written.
            if (geometry.IsEmpty) return;

            // if we have a geometry collection
            if (geometry.OgcGeometryType == OgcGeometryType.GeometryCollection)
            {
                WriteGeometryCollection(writer, header, (GeometryCollection)geometry, idList);
                return;
            }

            var csWriter = new CoordinateSequenceWriter(header);
            switch (geometry.OgcGeometryType)
            {
                case OgcGeometryType.Point:
                    WritePoint(writer, csWriter, (Point)geometry);
                    break;
                case OgcGeometryType.LineString:
                    WriteLineString(writer, csWriter, (LineString)geometry);
                    break;
                case OgcGeometryType.Polygon:
                    WritePolygon(writer, csWriter, (Polygon)geometry);
                    break;
                case OgcGeometryType.MultiPoint:
                    WriteMultiPoint(writer, csWriter, (MultiPoint)geometry, idList);
                    break;
                case OgcGeometryType.MultiLineString:
                    WriteMultiLineString(writer, csWriter, (MultiLineString)geometry, idList);
                    break;
                case OgcGeometryType.MultiPolygon:
                    WriteMultiPolygon(writer, csWriter, (MultiPolygon)geometry, idList);
                    break;
                default:
                    throw new ArgumentException(nameof(geometry));
            }

        }

        private void WritePoint(BinaryWriter writer, CoordinateSequenceWriter csWriter, Point point)
        {
            // We don't write bounding boxes for points. Period.
            csWriter.Write(writer, point.CoordinateSequence, false);
        }

        private void WriteLineString(BinaryWriter writer, CoordinateSequenceWriter csWriter, LineString lineString)
        {
            WriteBoundingBox(writer, csWriter, lineString);
            csWriter.Write(writer, lineString.CoordinateSequence, false);
        }

        private void WritePolygon(BinaryWriter writer, CoordinateSequenceWriter csWriter, Polygon polygon, bool omitBoundingBox = false)
        {
            if (!omitBoundingBox) WriteBoundingBox(writer, csWriter, polygon);
            writer.Write(VarintBitConverter.GetVarintBytes((uint)(1 + polygon.NumInteriorRings)));
            csWriter.Write(writer, polygon.Shell.CoordinateSequence, true);
            for (int i = 0; i < polygon.NumInteriorRings; i++)
                csWriter.Write(writer, polygon.GetInteriorRingN(i).CoordinateSequence, true);
        }

        private void WriteMultiPoint(BinaryWriter writer, CoordinateSequenceWriter csWriter, MultiPoint multiPoint, long[] idList)
        {
            WriteBoundingBox(writer, csWriter, multiPoint);
            writer.Write(VarintBitConverter.GetVarintBytes((uint)multiPoint.NumGeometries));
            WriteIdList(writer, multiPoint, idList);
            for (int i = 0; i < multiPoint.NumGeometries; i++)
                csWriter.Write(writer, ((Point)multiPoint.GetGeometryN(i)).CoordinateSequence, false);
        }

        private void WriteMultiLineString(BinaryWriter writer, CoordinateSequenceWriter csWriter,
            MultiLineString multiLineString, long[] idList)
        {
            WriteBoundingBox(writer, csWriter, multiLineString);
            writer.Write(VarintBitConverter.GetVarintBytes((uint)multiLineString.NumGeometries));
            WriteIdList(writer, multiLineString, idList);
            for (int i = 0; i < multiLineString.NumGeometries; i++)
                csWriter.Write(writer, ((LineString)multiLineString.GetGeometryN(i)).CoordinateSequence, false);
        }

        private void WriteMultiPolygon(BinaryWriter writer, CoordinateSequenceWriter csWriter,
            MultiPolygon multiPolygon, long[] idList)
        {
            WriteBoundingBox(writer, csWriter, multiPolygon);
            writer.Write(VarintBitConverter.GetVarintBytes((uint)multiPolygon.NumGeometries));
            WriteIdList(writer, multiPolygon, idList);
            for (int i = 0; i < multiPolygon.NumGeometries; i++)
                WritePolygon(writer, csWriter, (Polygon)multiPolygon.GetGeometryN(i), true);
        }

        private void WriteGeometryCollection(BinaryWriter writer, TinyWkbHeader header, GeometryCollection gc,
            long[] idList)
        {
            var csWriter = new CoordinateSequenceWriter(header);
            WriteBoundingBox(writer, csWriter, gc);
            writer.Write(VarintBitConverter.GetVarintBytes((uint)gc.NumGeometries));
            WriteIdList(writer, gc, idList);
            for (int i = 0; i < gc.NumGeometries; i++)
                Write(writer, gc.GetGeometryN(i));
        }

        private void WriteBoundingBox(BinaryWriter writer, CoordinateSequenceWriter csWriter, Geometry geometry)
        {
            if (!EmitBoundingBox) return;

            var minMaxFilter = new MinMaxFilter(csWriter.Ordinates);
            geometry.Apply(minMaxFilter);

            for (int i = 0; i < csWriter.Dimension; i++)
                WriteInterval(writer, minMaxFilter[i], csWriter.Scales[i]);

        }

        private static long _nextId = 1;

        private static long GetNextId()
        {
            return System.Threading.Interlocked.Increment(ref _nextId);
        }

        /// <summary>
        /// Event raised when a list of id values is required
        /// </summary>
        public event EventHandler<IdentifiersEventArgs> IdentifiersRequired;

        private void WriteIdList(BinaryWriter writer, GeometryCollection gc, long[] idList)
        {
            if (!EmitIdList) return;

            for (int i = 0; i < gc.Count; i++)
                writer.Write(VarintBitConverter.GetVarintBytes(idList[i]));
        }

        /// <summary>
        /// Checks if an idList is provided for a geometry collection
        /// </summary>
        /// <param name="geom">The geometry to test</param>
        /// <param name="idList">The provided idList</param>
        /// <returns>The idList</returns>
        private long[] GetIdList(Geometry geom, long[] idList)
        {
            var gc = geom as GeometryCollection;
            if (gc == null || !EmitIdList) return null;

            if (idList == null || idList.Length != gc.NumGeometries)
            {
                idList = new long[gc.Count];
                var h = IdentifiersRequired;
                if (h != null)
                {
                    var args = new IdentifiersEventArgs(gc, idList);
                    h(this, args);
                }
                else
                {
                    bool fallback = false;
                    for (int i = 0; i < idList.Length; i++)
                    {
                        object userData = gc.GetGeometryN(i).UserData;
                        if (userData is null)
                        {
                            fallback = true;
                            break;
                        }

                        try
                        {
                            idList[i] = Convert.ToInt64(userData, CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            fallback = true;
                            break;
                        }
                    }

                    if (fallback)
                    {
                        for (int i = 0; i < idList.Length; i++)
                            idList[i] = GetNextId();
                    }
                }
            }

            return idList;
        }

        private void WriteInterval(BinaryWriter writer, Interval interval, double scale)
        {
            long last = 0;
            long encValue = CoordinateSequenceWriter.EncodeOrdinate(interval.Min, scale, ref last);
            writer.Write(VarintBitConverter.GetVarintBytes(encValue));
            encValue = CoordinateSequenceWriter.EncodeOrdinate(interval.Max, scale, ref last);
            writer.Write(VarintBitConverter.GetVarintBytes(encValue));
        }

        private class MinMaxFilter : ICoordinateSequenceFilter
        {
            //private readonly int _dimension, _measureIndex;
            private readonly Ordinate[] _ordinates;
            private readonly Interval[] _intervals;

            public MinMaxFilter(Ordinate[] ordinates)
            {
                _ordinates = ordinates;
                _intervals = new Interval[_ordinates.Length];
                for (int i = 0; i < _ordinates.Length; i++)
                    _intervals[i] = Interval.Create();
            }

            public int Dimension => _ordinates.Length;

            public bool Done { get; private set; }

            public bool GeometryChanged => false;

            public void Filter(CoordinateSequence sequence, int i)
            {
                if (i > 0) return;

                Span<int> ordinateIndices = stackalloc int[] {0, 1, -1, -1};
                for (int j = 2; j < Dimension; j++)
                {
                    if (!sequence.TryGetOrdinateIndex(_ordinates[j], out ordinateIndices[j]))
                        ordinateIndices[j] = -1;
                }

                for (int k = 0; k < sequence.Count; k++)
                {
                    for (int j = 0; j < Dimension; j++)
                    {
                        if (ordinateIndices[j] < 0) continue;
                        _intervals[j] = _intervals[j]
                            .ExpandedByValue(sequence.GetOrdinate(k, ordinateIndices[j]));
                    }
                }
            }
        
            public Interval this[int dimension] { get { return _intervals[dimension]; } }
        }


        private TinyWkbHeader ToHeader(Geometry geometry, bool hasIdList)
        {
            TinyWkbGeometryType type;
            switch (geometry.OgcGeometryType)
            {
                case OgcGeometryType.Point:
                    type = TinyWkbGeometryType.Point;
                    hasIdList = false;
                    break;
                case OgcGeometryType.LineString:
                    type = TinyWkbGeometryType.LineString;
                    hasIdList = false;
                    break;
                case OgcGeometryType.Polygon:
                    type = TinyWkbGeometryType.Polygon;
                    hasIdList = false;
                    break;
                case OgcGeometryType.MultiPoint:
                    type = TinyWkbGeometryType.MultiPoint;
                    break;
                case OgcGeometryType.MultiLineString:
                    type = TinyWkbGeometryType.MultiLineString;
                    break;
                case OgcGeometryType.MultiPolygon:
                    type = TinyWkbGeometryType.MultiPolygon;
                    break;
                case OgcGeometryType.GeometryCollection:
                    type = TinyWkbGeometryType.GeometryCollection;
                    break;
                default:
                    throw new NotSupportedException();
            }

            // Test if we have z- or m-ordinate values
            var c = geometry.Coordinate;
            bool hasZ = c is CoordinateZ && !double.IsNaN(c.Z);
            bool hasM = c is CoordinateM || c is CoordinateZM;

            return new TinyWkbHeader(type, PrecisionXY, geometry.IsEmpty,
                EmitBoundingBox, EmitSize, EmitIdList & hasIdList,
                EmitZ & hasZ, PrecisionZ,
                EmitM & hasM, PrecisionM);
        }
    }
}

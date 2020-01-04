using System;
using System.IO;
using System.Text;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO
{
    public partial class TinyWkbWriter
    {

        public int PrecisionXY { get; set; }

        public int PrecisionZ { get; set; }

        public int PrecisionM { get; set; }

        public bool EmitBoundingBox { get; }

        public bool EmitZ { get; }
        
        public bool EmitM { get; }

        public byte[] Write(Geometry geometry)
        {
            using (var ms = new MemoryStream())
            {
                Write(geometry, ms);
                return ms.ToArray();
            }
        }

        public void Write(Geometry geometry, Stream stream)
        {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                Write(geometry, writer);
        }

        private void Write(Geometry geometry, BinaryWriter writer)
        {
            writer.Write(ToHeader(geometry.OgcGeometryType));
            writer.Write(ToMetadataHeader(geometry));
            if (EmitZ || EmitM)
                writer.Write(ToExtPrecInfo(geometry));

            if (/*size*/ false)
            {
                //Write size
            }

            var csWriter = new CoordinateSequenceWriter(this, geometry);

        }

        private byte ToHeader(OgcGeometryType type)
        {
            byte res;
            switch (type)
            {
                case OgcGeometryType.Point:
                    res = (byte)TinyWkbGeometryType.Point;
                    break;
                case OgcGeometryType.LineString:
                    res = (byte)TinyWkbGeometryType.LineString;
                    break;
                case OgcGeometryType.Polygon:
                    res = (byte)TinyWkbGeometryType.Polygon;
                    break;
                case OgcGeometryType.MultiPoint:
                    res = (byte)TinyWkbGeometryType.MultiPoint;
                    break;
                case OgcGeometryType.MultiLineString:
                    res = (byte)TinyWkbGeometryType.MultiLineString;
                    break;
                case OgcGeometryType.MultiPolygon:
                    res = (byte)TinyWkbGeometryType.MultiPolygon;
                    break;
                case OgcGeometryType.GeometryCollection:
                    res = (byte)TinyWkbGeometryType.GeometryCollection;
                    break;
                default:
                    throw new NotSupportedException();
            }
            return (byte)(res | (PrecisionXY << 4));
        }

        private byte ToMetadataHeader(Geometry geometry)
        {
            return new MetadataHeader(
                geometry.OgcGeometryType != OgcGeometryType.Point,
                false, false,
                EmitZ | EmitM,
                geometry.IsEmpty).Value;
        }

        private byte ToExtPrecInfo(Geometry geometry)
        {
            throw new NotImplementedException();
        }
    }
}

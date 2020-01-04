using System;
using System.IO;
using System.Text;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO
{
    public class TinyWkbWriter
    {
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
            writer.Write((byte)ToTinyWkbType(geometry.OgcGeometryType));

        }

        private TinyWkbGeometryType ToTinyWkbType(OgcGeometryType type)
        {
            switch (type)
            {
                case OgcGeometryType.Point:
                    return TinyWkbGeometryType.Point;
                case OgcGeometryType.LineString:
                    return TinyWkbGeometryType.LineString;
                case OgcGeometryType.Polygon:
                    return TinyWkbGeometryType.Polygon;
                case OgcGeometryType.MultiPoint:
                    return TinyWkbGeometryType.MultiPoint;
                case OgcGeometryType.MultiLineString:
                    return TinyWkbGeometryType.MultiLineString;
                case OgcGeometryType.MultiPolygon:
                    return TinyWkbGeometryType.MultiPolygon;
                case OgcGeometryType.GeometryCollection:
                    return TinyWkbGeometryType.GeometryCollection;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

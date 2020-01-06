using System;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("NetTopologySuite.IO.TinyWKB.Test")]

namespace NetTopologySuite.IO
{
    /// <summary>
    /// Enumeration of valid geometry types in Tiny WKB format
    /// </summary>
    internal enum TinyWkbGeometryType : byte
    {
        /// <summary>
        /// A point geometry
        /// </summary>
        Point = 1,

        /// <summary>
        /// A line string geometry
        /// </summary>
        LineString = 2,

        /// <summary>
        /// A polygon geometry
        /// </summary>
        Polygon = 3,

        /// <summary>
        /// A geometry made up of multiple <see cref="Point"/>s.
        /// </summary>
        MultiPoint = 4,

        /// <summary>
        /// A geometry made up of multiple <see cref="LineString"/>s.
        /// </summary>
        MultiLineString = 5,

        /// <summary>
        /// A geometry made up of multiple <see cref="Polygon"/>s.
        /// </summary>
        MultiPolygon = 6,

        /// <summary>
        /// A geometry made up of multiple single instance geometries of different kind.
        /// </summary>
        GeometryCollection = 7
    }

    internal static class TinyWkbHeaderEx
    {
        public static double ScaleX(this TinyWkbHeader self) => Math.Pow(10, self.PrecisionXY);
        public static double ScaleY(this TinyWkbHeader self) => Math.Pow(10, self.PrecisionXY);
        public static double ScaleZ(this TinyWkbHeader self) => Math.Pow(10, self.PrecisionZ);
        public static double ScaleM(this TinyWkbHeader self) => Math.Pow(10, self.PrecisionM);

        public static double DescaleX(this TinyWkbHeader self) => Math.Pow(10, -self.PrecisionXY);
        public static double DescaleY(this TinyWkbHeader self) => Math.Pow(10, -self.PrecisionXY);
        public static double DescaleZ(this TinyWkbHeader self) => Math.Pow(10, -self.PrecisionZ);
        public static double DescaleM(this TinyWkbHeader self) => Math.Pow(10, -self.PrecisionM);

    }
}

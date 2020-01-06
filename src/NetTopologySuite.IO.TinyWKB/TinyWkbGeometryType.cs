using System;
using System.Runtime.CompilerServices;

//[assembly:InternalsVisibleTo("NetTopologySuite.IO.TinyWKB.Test")]

namespace NetTopologySuite.IO
{
    /// <summary>
    /// Enumeration of valid geometry types in Tiny WKB format
    /// </summary>
    public enum TinyWkbGeometryType : byte
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

    /// <summary>
    /// Extension methods for <see cref="TinyWkbHeader"/>.
    /// </summary>
    public static class TinyWkbHeaderEx
    {
        /// <summary>
        /// Computes the scale value for x-ordinate values.
        /// </summary>
        /// <param name="self">A TWKB-header</param>
        /// <returns>The scale value for x-ordinate values</returns>
        public static double ScaleX(this TinyWkbHeader self) => Math.Pow(10, self.PrecisionXY);
        /// <summary>
        /// Computes the scale value for y-ordinate values.
        /// </summary>
        /// <param name="self">A TWKB-header</param>
        /// <returns>The scale value for y-ordinate values</returns>
        public static double ScaleY(this TinyWkbHeader self) => Math.Pow(10, self.PrecisionXY);
        /// <summary>
        /// Computes the scale value for z-ordinate values.
        /// </summary>
        /// <param name="self">A TWKB-header</param>
        /// <returns>The scale value for z-ordinate values</returns>
        public static double ScaleZ(this TinyWkbHeader self) => Math.Pow(10, self.PrecisionZ);
        /// <summary>
        /// Computes the scale value for m-ordinate values.
        /// </summary>
        /// <param name="self">A TWKB-header</param>
        /// <returns>The scale value for m-ordinate values</returns>
        public static double ScaleM(this TinyWkbHeader self) => Math.Pow(10, self.PrecisionM);

        /// <summary>
        /// Computes the de-scale value for x-ordinate values.
        /// </summary>
        /// <param name="self">A TWKB-header</param>
        /// <returns>The de-scale value for x-ordinate values</returns>
        public static double DescaleX(this TinyWkbHeader self) => Math.Pow(10, -self.PrecisionXY);
        /// <summary>
        /// Computes the de-scale value for y-ordinate values.
        /// </summary>
        /// <param name="self">A TWKB-header</param>
        /// <returns>The de-scale value for y-ordinate values</returns>
        public static double DescaleY(this TinyWkbHeader self) => Math.Pow(10, -self.PrecisionXY);
        /// <summary>
        /// Computes the de-scale value for z-ordinate values.
        /// </summary>
        /// <param name="self">A TWKB-header</param>
        /// <returns>The de-scale value for z-ordinate values</returns>
        public static double DescaleZ(this TinyWkbHeader self) => Math.Pow(10, -self.PrecisionZ);
        /// <summary>
        /// Computes the de-scale value for m-ordinate values.
        /// </summary>
        /// <param name="self">A TWKB-header</param>
        /// <returns>The de-scale value for m-ordinate values</returns>
        public static double DescaleM(this TinyWkbHeader self) => Math.Pow(10, -self.PrecisionM);

    }
}

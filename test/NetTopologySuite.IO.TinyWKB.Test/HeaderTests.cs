using NUnit.Framework;

namespace NetTopologySuite.IO.Test
{
    public class HeaderTests
    {
        [TestCase(TinyWkbGeometryType.Point, 0, 1d)]
        [TestCase(TinyWkbGeometryType.Point, 3, 0.001)]
        [TestCase(TinyWkbGeometryType.Point, -1, 10d)]
        [TestCase(TinyWkbGeometryType.LineString, 3, 0.001)]
        [TestCase(TinyWkbGeometryType.Polygon, -1, 10d)]
        [TestCase(TinyWkbGeometryType.MultiPoint, 3, 0.001)]
        [TestCase(TinyWkbGeometryType.MultiLineString, -1, 10d)]
        [TestCase(TinyWkbGeometryType.MultiPolygon, -1, 10d)]
        [TestCase(TinyWkbGeometryType.GeometryCollection, 4, 0.0001)]
        public void Test(byte type, int precision, double descale)
        {
            var h = new TinyWkbHeader((TinyWkbGeometryType)type, precision);

            Assert.That(h.GeometryType, Is.EqualTo((TinyWkbGeometryType)type));
            Assert.That(h.PrecisionXY, Is.EqualTo(precision));
            Assert.That(h.DescaleX(), Is.EqualTo(descale));
            Assert.That(h.DescaleY(), Is.EqualTo(descale));
        }
    }
}

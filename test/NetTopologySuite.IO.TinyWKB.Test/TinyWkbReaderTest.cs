using System;
using System.IO;
using NetTopologySuite.Geometries;
using NUnit.Framework;

namespace NetTopologySuite.IO.Test
{
    public class TinyWkbReaderTest
    {
        private TinyWkbReader _reader;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _reader = new TinyWkbReader();
        }

        [TestCase("01000204", OgcGeometryType.Point)]
        [TestCase("02000202020808", OgcGeometryType.LineString)]
        [TestCase("03031b000400040205000004000004030000030500000002020000010100", OgcGeometryType.Polygon)]
        [TestCase("04070b0004020402000200020404", OgcGeometryType.MultiPoint)]
        [TestCase("020309020802080202020808", OgcGeometryType.LineString)]
        [TestCase("05030f020802080202020208080207050404", OgcGeometryType.MultiLineString)]
        [TestCase("070402000201000002020002080a0404", OgcGeometryType.GeometryCollection)]
        public void TestsFromTwkbJs(string hexString, OgcGeometryType type)
        {
            Geometry g = null;
            using (var ms = new MemoryStream(WKBReader.HexToBytes(hexString)))
            {
                ms.Seek(0, SeekOrigin.Begin);
                Assert.That(() => g = _reader.Read(ms), Throws.Nothing);
                Assert.That(g.OgcGeometryType, Is.EqualTo(type));
            }
            
            Console.WriteLine($"Read '{g.AsText()}' from '{hexString}'.");
        }
    }
}

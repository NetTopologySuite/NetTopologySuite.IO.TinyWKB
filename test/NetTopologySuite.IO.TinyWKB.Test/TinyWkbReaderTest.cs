using System;
using System.Globalization;
using System.IO;
using System.Text;
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
        //This Polygon has repeated points, which are discarded.
        //[TestCase("03031b000400040205000004000004030000030500000002020000010100", OgcGeometryType.Polygon)]
        [TestCase("0303170004000402040400000403000003040002020000010100", OgcGeometryType.Polygon)]
        [TestCase("04070b0004020402000200020404", OgcGeometryType.MultiPoint)]
        [TestCase("020309020802080202020808", OgcGeometryType.LineString)]
        [TestCase("05030f020802080202020208080207050404", OgcGeometryType.MultiLineString)]
        [TestCase("070402000201000002020002080a0404", OgcGeometryType.GeometryCollection)]
        public void TestsFromTwkbJs(string hexString, OgcGeometryType type)
        {
            byte[] originalBytes = WKBReader.HexToBytes(hexString);
            Geometry g = null;
            TinyWkbHeader header;
            using (var ms = new MemoryStream(originalBytes))
            {
                ms.Seek(0, SeekOrigin.Begin);
                using (var br = new BinaryReader(ms, Encoding.UTF8, false))
                {
                    header = TinyWkbHeader.Read(br);

                    ms.Seek(0, SeekOrigin.Begin);
                    Assert.That(() => g = _reader.Read(br), Throws.Nothing);
                    Assert.That(g.OgcGeometryType, Is.EqualTo(type));
                    Assert.That(ms.Position, Is.EqualTo(ms.Length));
                }
            }

            TestContext.WriteLine();
            TestContext.WriteLine($"Read  '{g.AsText()}' from '{hexString.Substring(2)}'.");

            var wrtr = new TinyWkbWriter(precisionXY: header.PrecisionXY,
                header.HasZ, header.PrecisionZ, header.HasM, header.PrecisionM,
                header.HasSize, header.HasBoundingBox, header.HasIdList);
            byte[] data2 = wrtr.Write(g);
            TestContext.WriteLine($"Write '{g.AsText()}' to '{TinyWkbWriterTest.ToHexString(data2)}'.");
            TestContext.WriteLine(_reader.Read(data2));
            Assert.That(data2, Is.EqualTo(originalBytes));
        }

        [TestCase("POINT EMPTY", "0110")]
        [TestCase("LINESTRING EMPTY", "821200")]
        [TestCase("POLYGON EMPTY", "0310")]
        [TestCase("MULTIPOINT EMPTY", "0410")]
        [TestCase("MULTILINESTRING EMPTY", "0510")]
        [TestCase("MULTIPOLYGON EMPTY", "0610")]
        [TestCase("GEOMETRYCOLLECTION EMPTY", "0710")]
        public void TestsFromPostgis(string wkt, string hexString)
        {
            Geometry g = null;
            using (var ms = new MemoryStream(WKBReader.HexToBytes(hexString)))
            {
                ms.Seek(0, SeekOrigin.Begin);
                using (var br = new BinaryReader(ms, Encoding.UTF8, false))
                {
                    Assert.That(() => g = _reader.Read(br), Throws.Nothing);
                    Assert.That(g.ToText(), Is.EqualTo(wkt));
                    Assert.That(ms.Position, Is.EqualTo(ms.Length));
                }
            }
        }
    }
}

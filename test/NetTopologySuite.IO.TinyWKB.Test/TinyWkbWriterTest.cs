using NetTopologySuite.Geometries;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using NetTopologySuite.Geometries.Utilities;

namespace NetTopologySuite.IO.Test
{
    [TestFixture(0)]
    [TestFixture(1)]
    [TestFixture(2)]
    [TestFixture(3)]
    [TestFixture(3, true, 1, false, 0, true, true, false)]
    [TestFixture(3, false, 0, true, 1, true, true, false)]
    [TestFixture(4, true, 2, true, 3, true, true, false)]
    [TestFixture(4, true, 2, true, 3, true, true, true)]
    [TestFixture(-1)]
    public class TinyWkbWriterTest
    {
        private readonly int _dimension;

        private readonly int _precisionXY;
        private readonly bool _emitZ, _emitM;
        private readonly int _precisionZ, _precisionM;
        private readonly bool _emitSize, _emitBoundingBox, _emitIdList;

        public TinyWkbWriterTest(int precisionXY)
            : this(precisionXY, false)
        {

        }

        public TinyWkbWriterTest(int precisionXY,
            bool emitZ, int precisionZ = 0,
            bool emitM = false, int precisionM = 0,
            bool emitSize = false, bool emitBoundingBox = false, bool emitIdList = false)
        {
            int dimension = 2;
            if (precisionXY < -7 || precisionXY > 7)
                throw new ArgumentOutOfRangeException(nameof(precisionXY));

            _precisionXY = precisionXY;
            if (precisionZ < 0 || precisionZ > 7)
                throw new ArgumentOutOfRangeException(nameof(precisionZ));
            _precisionZ = precisionZ;

            if (precisionM < 0 || precisionM > 7)
                throw new ArgumentOutOfRangeException(nameof(precisionM));
            _precisionM = precisionM;

            _emitZ = emitZ;
            if (emitZ) dimension++;
            _emitM = emitM;
            if (emitM) dimension++;
            _emitSize = emitSize;
            _emitBoundingBox = emitBoundingBox;
            _emitIdList = emitIdList;

            _dimension = dimension;
        }

        private readonly Random Rnd = new Random(13);

        private Coordinate CreateCoordinate(double x, double y)
        {
            double wholex = Math.Round(x, 0, MidpointRounding.AwayFromZero);
            if (Math.Abs(x - wholex) < 1e8)
                x = wholex + Rnd.NextDouble() - 0.5d;
            double wholey = Math.Round(y, 0, MidpointRounding.AwayFromZero);
            if (Math.Abs(y - wholey) < 1e8)
                y = wholey + Rnd.NextDouble() - 0.5d;

            if (_dimension == 2) return new Coordinate(x, y);

            if (_dimension == 3 && _emitZ)
                return new CoordinateZ(x, y, 100 + 100 * Rnd.NextDouble());
            if (_dimension == 3 && !_emitZ)
                return new CoordinateM(x, y, 200 + 100 * Rnd.NextDouble());

            return new CoordinateZM(x, y, 100 + 100 * Rnd.NextDouble(), 200 + 100 * Rnd.NextDouble());
        }

        private TinyWkbWriter CreateWriter()
        {
            return new TinyWkbWriter(_precisionXY,
                _emitZ, _precisionZ,
                _emitM, _precisionM,
                _emitSize, _emitBoundingBox, _emitIdList
                );
        }

        [Test, Order(0)]
        public void TestConstructor()
        {
            TinyWkbWriter twkbWriter = null;
            Assert.That(() => twkbWriter = new TinyWkbWriter(_precisionXY,
                _emitZ, _precisionZ,
                _emitM, _precisionM,
                _emitSize, _emitBoundingBox, _emitIdList
                ), Throws.Nothing);

            Assert.That(twkbWriter, Is.Not.Null);
            Assert.That(twkbWriter.PrecisionXY, Is.EqualTo(_precisionXY));
            Assert.That(twkbWriter.EmitZ, Is.EqualTo(_emitZ));
            Assert.That(twkbWriter.PrecisionZ, Is.EqualTo(_precisionZ));
            Assert.That(twkbWriter.EmitM, Is.EqualTo(_emitM));
            Assert.That(twkbWriter.PrecisionM, Is.EqualTo(_precisionM));
        }

        [Test]
        public void TestPoint() {

            var pt = GeometryFactory.Default.CreatePoint(CreateCoordinate(3, 6));

            byte[] twkbData = TestWrite(pt);
            TestReRead(pt, twkbData);
        }

        [Test]
        public void TestLineString()
        {

            var ls = GeometryFactory.Default.CreateLineString(new[] {
                CreateCoordinate(4, 7), CreateCoordinate(10, 3),
                CreateCoordinate(12, 4),CreateCoordinate(15, 9),
                CreateCoordinate(17, 8) }
            );

            byte[] twkbData = TestWrite(ls);
            TestReRead(ls, twkbData);
        }

        [Test]
        public void TestPolygon()
        {
            var p = GeometryFactory.Default.CreatePolygon(CreateRing(ToCoordinateArray(PolygonOrdinates)));

            byte[] twkbData = TestWrite(p);
            TestReRead(p, twkbData);
        }

        [Test]
        public void TestPolygonWithHoles()
        {
            var shell = CreateRing(ToCoordinateArray(PolygonWithHolesShellOrdinates));
            var holesCoordinates = ToCoordinateArrays(PolygonWithHolesRingsOrdinates);
            var holes = new LinearRing[holesCoordinates.Length];
            for (int i = 0; i < holes.Length; i++)
                holes[i] = CreateRing(holesCoordinates[i]);

            var p = GeometryFactory.Default.CreatePolygon(shell, holes);

            byte[] twkbData = TestWrite(p);
            TestReRead(p, twkbData);
        }

        [Test]
        public void TestMultiPoint()
        {
            var cs = GeometryFactory.Default.CoordinateSequenceFactory.Create(ToCoordinateArray(LineStringOrdinates));
            var mp = GeometryFactory.Default.CreateMultiPoint(cs);
            AddIds(mp);

            byte[] twkbData = TestWrite(mp);
            TestReRead(mp, twkbData);
        }

        [Test]
        public void TestMultiLineString()
        {
            var ls0 = GeometryFactory.Default.CreateLineString(ToCoordinateArray(LineStringOrdinates));
            var ls1 = (LineString) new AffineTransformation(1, 0, 100, 0, 1, 0).Transform(ls0);
            var mls = ls0.Factory.CreateMultiLineString(new[] {ls0, ls1});
            AddIds(mls);

            byte[] twkbData = TestWrite(mls);
            TestReRead(mls, twkbData);
        }

        [Test]
        public void TestMultiPolygon()
        {
            var p0 = GeometryFactory.Default.CreatePolygon(CreateRing(ToCoordinateArray(PolygonOrdinates)));
            var shell = CreateRing(ToCoordinateArray(PolygonWithHolesShellOrdinates));
            var holesCoordinates = ToCoordinateArrays(PolygonWithHolesRingsOrdinates);
            var holes = new LinearRing[holesCoordinates.Length];
            for (int i = 0; i < holes.Length; i++)
                holes[i] = CreateRing(holesCoordinates[i]);
            var p1 = GeometryFactory.Default.CreatePolygon(shell, holes);
            var mp = p0.Factory.CreateMultiPolygon(new[] { p0, p1 });
            AddIds(mp);

            byte[] twkbData = TestWrite(mp);
            TestReRead(mp, twkbData);
        }

        [Test]
        public void TestGeometryCollection()
        {
            var p0 = GeometryFactory.Default.CreatePolygon(CreateRing(ToCoordinateArray(PolygonOrdinates)));
            var shell = CreateRing(ToCoordinateArray(PolygonWithHolesShellOrdinates));
            var holesCoordinates = ToCoordinateArrays(PolygonWithHolesRingsOrdinates);
            var holes = new LinearRing[holesCoordinates.Length];
            for (int i = 0; i < holes.Length; i++)
                holes[i] = CreateRing(holesCoordinates[i]);
            var p1 = GeometryFactory.Default.CreatePolygon(shell, holes);

            var ls = GeometryFactory.Default.CreateLineString(ToCoordinateArray(LineStringOrdinates));
            var p = GeometryFactory.Default.CreatePoint(CreateCoordinate(3, 5));

            var gc = p0.Factory.CreateGeometryCollection(new Geometry[] { p0, p, ls, p1 });
            AddIds(gc);

            byte[] twkbData = TestWrite(gc);
            TestReRead(gc, twkbData);
        }

        [Test]
        public void TestEmpty()
        {
            DoTestEmpty(GeometryFactory.Default.CreatePoint((Coordinate) null));
            DoTestEmpty(GeometryFactory.Default.CreateLineString((Coordinate[])null));
            DoTestEmpty(GeometryFactory.Default.CreatePolygon((Coordinate[])null));
            DoTestEmpty(GeometryFactory.Default.CreateMultiPoint((CoordinateSequence)null));
            DoTestEmpty(GeometryFactory.Default.CreateMultiLineString((LineString[])null));
            DoTestEmpty(GeometryFactory.Default.CreateMultiPolygon((Polygon[])null));
            DoTestEmpty(GeometryFactory.Default.CreateGeometryCollection(null));
        }

        private void DoTestEmpty(Geometry g)
        {
            byte[] twkbData = TestWrite(g);
            TestReRead(g, twkbData);
        }

        private void AddIds(GeometryCollection gc)
        {
            if (!_emitIdList) return;

            for (int i = 0; i < gc.NumGeometries; i++)
                gc.GetGeometryN(i).UserData = (1 + i) * 10;
        }

        private void ChecIds(GeometryCollection gc)
        {
            if (!_emitIdList) return;
            for (int i = 0; i < gc.NumGeometries; i++)
            {
                Assert.That(gc.GetGeometryN(i).UserData, Is.EqualTo((1 + i) * 10));
            }
        }

        private byte[] TestWrite(Geometry geometry)
        {
            var wrtr = CreateWriter();
            byte[] data = null;

            Assert.That(() => data = wrtr.Write(geometry), Throws.Nothing);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Length, Is.GreaterThan(0));

            TestContext.WriteLine(ToHexString(data));

            return data;
        }


        private void TestReRead(Geometry geometry, byte[] twkbData)
        {
            var rdr = new TinyWkbReader();
            Geometry readGeometry = null;

            Assert.That(() => readGeometry = rdr.Read(twkbData), Throws.Nothing);
            Assert.That(readGeometry != null);
            Assert.That(readGeometry.OgcGeometryType, Is.EqualTo(geometry.OgcGeometryType));

            if (readGeometry is GeometryCollection gc)
                ChecIds(gc);

            TestContext.WriteLine();
        }

        internal static string ToHexString(IEnumerable<byte> data)
        {
            var sb = new StringBuilder("0x");
            foreach (byte byt in data)
                sb.Append(byt.ToString("x2"));

            return sb.ToString();
        }

        private Coordinate[] ToCoordinateArray(double[][] ordinateValues)
        {
            var res = new Coordinate[ordinateValues.Length];
            for (int i = 0; i < ordinateValues.Length; i++)
                res[i] = CreateCoordinate(ordinateValues[i][0], ordinateValues[i][1]);
            return res;
        }

        private Coordinate[][] ToCoordinateArrays(double[][][] ordinateValues)
        {
            var res = new Coordinate[ordinateValues.Length][];
            for (int i = 0; i < ordinateValues.Length; i++)
                res[i] = ToCoordinateArray(ordinateValues[i]);
            return res;
        }

        private LinearRing CreateRing(Coordinate[] coordinates)
        {
            coordinates[coordinates.Length - 1] = coordinates[0].Copy();
            return GeometryFactory.Default.CreateLinearRing(coordinates);
        }

        private static double[][] LineStringOrdinates => new[]
            {new[] {4d, 7d}, new[] {10d, 3d}, new[] {12d, 4d}, new[] {15d, 9d}, new[] {17d, 8d}};

        private static double[][] PolygonOrdinates => new[]
            {new[] {5d, 5d}, new[] {5d, 45d}, new[] {45d, 45d}, new[] {45d, 5d}, new[] {5d, 5d}};

        private static double[][] PolygonWithHolesShellOrdinates => new[]
            {new[] {105d, 5d}, new[] {105d, 45d}, new[] {145d, 45d}, new[] {145d, 5d}, new[] {105d, 5d}};

        private static double[][][] PolygonWithHolesRingsOrdinates => new[]
        {
            new[] {new[] {115d, 10d}, new[] {115d, 15d}, new[] {115d, 15d}, new[] {115d, 10d}, new[] {115d, 10d}},
            new[] {new[] {135d, 35d}, new[] {135d, 40d}, new[] {140d, 40d}, new[] {140d, 35d}, new[] {135d, 35d}}

        };
    }
}

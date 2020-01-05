using NetTopologySuite.Geometries;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO.Test
{
    [TestFixture(0)]
    [TestFixture(1)]
    [TestFixture(2)]
    [TestFixture(3)]
    [TestFixture(3, false, 1, true, 1, true, true, false)]
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

            var pt = GeometryFactory.Default.CreatePoint(CreateCoordinate(4, 7));
            var wrtr = CreateWriter();

            byte[] data = null;

            Assert.That(() => data = wrtr.Write(pt), Throws.Nothing);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Length, Is.GreaterThan(0));

            TestContext.WriteLine(ToHexString(data));
        }

        [Test]
        public void TestLineString()
        {

            var ls = GeometryFactory.Default.CreateLineString(new[] {
                CreateCoordinate(4, 7), CreateCoordinate(10, 3),
                CreateCoordinate(12, 4),CreateCoordinate(15, 9),
                CreateCoordinate(17, 8) }
            );
            var wrtr = CreateWriter();

            byte[] data = null;

            Assert.That(() => data = wrtr.Write(ls), Throws.Nothing);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Length, Is.GreaterThan(0));

            TestContext.WriteLine(ToHexString(data));
        }
        private static string ToHexString(IEnumerable<byte> data)
        {
            var sb = new StringBuilder("0x");
            foreach (var byt in data)
                sb.Append(byt.ToString("X2"));
            return sb.ToString();
        }
    }
}

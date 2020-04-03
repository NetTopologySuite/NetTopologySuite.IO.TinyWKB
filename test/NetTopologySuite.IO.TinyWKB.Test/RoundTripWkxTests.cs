using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NetTopologySuite.IO.Test
{
    [TestFixture(CsFactoryType.Array)]
    [TestFixture(CsFactoryType.PackedFloat)]
    [TestFixture(CsFactoryType.PackedDouble)]
    [TestFixture(CsFactoryType.DotSpatialAffine)]
    public class RoundTripWkxTests
    {
        private readonly NtsGeometryServices _ntsGeometryServices;
        private readonly WKBReader _wkbReader;
        private readonly WKTReader _wktReader;
        private readonly WKTWriter _wktWriter = new WKTWriter(4);

        public RoundTripWkxTests(CsFactoryType csFactoryType)
        {
            var pm = new PrecisionModel();
            int srid = 126;
            CoordinateSequenceFactory csFactory;
            switch (csFactoryType)
            {
                default:
                case CsFactoryType.Array:
                    csFactory = CoordinateArraySequenceFactory.Instance;
                    break;
                case CsFactoryType.PackedFloat:
                    csFactory = PackedCoordinateSequenceFactory.FloatFactory;
                    break;
                case CsFactoryType.PackedDouble:
                    csFactory = PackedCoordinateSequenceFactory.DoubleFactory;
                    break;
                case CsFactoryType.DotSpatialAffine:
                    csFactory = new DotSpatialAffineCoordinateSequenceFactory(Ordinates.XY);
                    break;

            }

            _ntsGeometryServices = new NtsGeometryServices(csFactory, pm, srid);
            _wkbReader = new WKBReader(_ntsGeometryServices);
            _wktReader = new WKTReader(_ntsGeometryServices.CreateGeometryFactory());


        }

        [TestCase("POINT (10 10)")]
        [TestCase("LINESTRING (10 10, 15 10, 20 15, 25 15)")]
        [TestCase("POLYGON ((10 10, 15 10, 15 15, 10 15, 10 10))")]
        [TestCase("POLYGON ((10 10, 15 10, 15 15, 10 15, 10 10), (11 11, 11 14, 14 14, 14 11, 11 11))")]
        [TestCase("MULTIPOINT ((10 10), (15 10), (20 15), (25 15))")]
        [TestCase("MULTILINESTRING ((10 10, 15 10, 20 15, 25 15), (9 11, 14 11, 19 16, 24 16))")]
        [TestCase("MULTIPOLYGON (((10 10, 15 10, 15 15, 10 15, 10 10), (11 11, 11 14, 14 14, 14 11, 11 11)), ((30 10, 35 10, 35 15, 30 15, 30 10)))")]
        [TestCase("GEOMETRYCOLLECTION (POINT (10 10), LINESTRING (10 10, 15 10, 20 15, 25 15), POLYGON ((10 10, 15 10, 15 15, 10 15, 10 10), (11 11, 11 14, 14 14, 14 11, 11 11)))")]
        public void TestWkx(string wktOrWkb)
        {

            var geomS = wktOrWkb.StartsWith("0x")
                ? _wkbReader.Read(WKBReader.HexToBytes(wktOrWkb.Substring(2)))
                : _wktReader.Read(wktOrWkb);

            Test2D(geomS);
            Test2DM(geomS);
            Test3D(geomS);
            Test3DM(geomS);
        }

        [Ignore("These tests are known to fail, need to be investigated")]
        //[TestCase("")]
        public void TestWkxFailure(string wktOrWkb)
        {

            var geomS = wktOrWkb.StartsWith("0x")
                ? _wkbReader.Read(WKBReader.HexToBytes(wktOrWkb.Substring(2)))
                : _wktReader.Read(wktOrWkb);

            //Test2DM(geomS);
        }

        private void Test2D(Geometry geomS)
        {
            TestContext.WriteLine(_wktWriter.Write(geomS));
            var twkbWriter = new TinyWkbWriter(emitZ: false, emitM: false);
            byte[] bytes = twkbWriter.Write(geomS);
            CreatePostGisConformanceCheckSql("2D", geomS, bytes);
            var twkbReader = new TinyWkbReader(_ntsGeometryServices.CreateGeometryFactory());
            var geomD = twkbReader.Read(bytes);

            Check(geomS, geomD, Ordinates.XY);
        }

        private void Test2DM(Geometry geomS)
        {
            if (CoordinateArrays.Measures(geomS.Coordinates) == 0)
                geomS = AddOrdinates(geomS, Ordinate.M);

            var twkbWriter = new TinyWkbWriter(emitZ:false);
            byte[] bytes = twkbWriter.Write(geomS);
            CreatePostGisConformanceCheckSql("2DM", geomS, bytes);
            var twkbReader = new TinyWkbReader(_ntsGeometryServices.CreateGeometryFactory());
            var geomD = twkbReader.Read(bytes);

            Check(geomS, geomD, Ordinates.XYM);
        }

        private void Test3D(Geometry geomS)
        {
            if (CoordinateArrays.Measures(geomS.Coordinates) == 0)
                geomS = AddOrdinates(geomS, Ordinate.Z);

            var twkbWriter = new TinyWkbWriter(emitM: false);
            byte[] bytes = twkbWriter.Write(geomS);
            CreatePostGisConformanceCheckSql("3D", geomS, bytes);
            var twkbReader = new TinyWkbReader(_ntsGeometryServices.CreateGeometryFactory());
            var geomD = twkbReader.Read(bytes);

            Check(geomS, geomD, Ordinates.XYZ);
        }
        private void Test3DM(Geometry geomS)
        {
            if (CoordinateArrays.Measures(geomS.Coordinates) == 0)
                geomS = AddOrdinates(geomS, Ordinate.Z, Ordinate.M);

            var twkbWriter = new TinyWkbWriter();
            byte[] bytes = twkbWriter.Write(geomS);
            CreatePostGisConformanceCheckSql("3DM", geomS, bytes);
            var twkbReader = new TinyWkbReader(_ntsGeometryServices.CreateGeometryFactory());
            var geomD = twkbReader.Read(bytes);

            Check(geomS, geomD, Ordinates.XYZ);
        }

        private Geometry AddOrdinates(Geometry geom, params Ordinate[] ordinates)
        {
            var geoms = new Geometry[geom.NumGeometries];
            for (int i = 0; i < geom.NumGeometries; i++)
            {
                var geomI = geom.GetGeometryN(i);
                switch (geomI.OgcGeometryType)
                {
                    case OgcGeometryType.Point:
                        geoms[i] = geom.Factory.CreatePoint(AddOrdinates(((Point) geomI).CoordinateSequence, 1, ordinates));
                        break;
                    case OgcGeometryType.LineString:
                        geoms[i] = geom.Factory.CreateLineString(AddOrdinates(((LineString)geomI).CoordinateSequence, 1, ordinates));
                        break;
                    case OgcGeometryType.Polygon:
                        int offset = 1;
                        var poly = (Polygon) geomI;
                        var shell = geom.Factory.CreateLinearRing(AddOrdinates(poly.ExteriorRing.CoordinateSequence, offset, ordinates));
                        offset += shell.NumPoints;
                        var holes = new LinearRing[poly.NumInteriorRings];
                        for (int j = 0; j < poly.NumInteriorRings; j++)
                        {
                            holes[j] = geom.Factory.CreateLinearRing(AddOrdinates(poly.GetInteriorRingN(j).CoordinateSequence, offset, ordinates));
                            offset += shell.NumPoints;
                        }
                        geoms[i] = geom.Factory.CreatePolygon(shell, holes);
                        break;
                }
            }

            if (geoms.Length == 1)
                return geoms[0];
            return geom.Factory.BuildGeometry(geoms);
        }

        private CoordinateSequence AddOrdinates(CoordinateSequence sequence, int offset, params Ordinate[] ordinates)
        {
            if (ordinates.Length == 0)
                return sequence;

            int dimension = sequence.Dimension;
            int measures = sequence.Measures;
            for (int i = 0; i < ordinates.Length; i++)
            {
                if (HasOrdinate(sequence, ordinates[i]))
                    continue;
                dimension += 1;
                if (ordinates[i] == Ordinate.M) measures = 1;
            }

            // Create a sequence with required dimensions
            var res = _ntsGeometryServices.DefaultCoordinateSequenceFactory.Create(sequence.Count,
                dimension, measures);

            // Copy existant values
            var srcOrdinates = GetOrdinates(sequence);
            int[] srcIndices = GetOrdinateIndices(sequence);

            for (int i = 0; i < srcIndices.Length; i++)
            {
                if (!res.TryGetOrdinateIndex(srcOrdinates[i], out int tgtIndex))
                    continue;

                for (int j = 0; j < sequence.Count; j++)
                    res.SetOrdinate(j, tgtIndex, sequence.GetOrdinate(j, srcIndices[i]));
            }

            for (int i = 0; i < ordinates.Length; i++)
            {
                if (HasMeaningfulOrdinate(sequence, ordinates[i]))
                    continue;

                if (!res.TryGetOrdinateIndex(ordinates[i], out int tgtIndex))
                    throw new InvalidOperationException();

                for (int j = 0; j < sequence.Count; j++)
                    res.SetOrdinate(j, tgtIndex, offset + j + (double)ordinates[i]/100d);

            }

            if (CoordinateSequences.IsRing(res))
                CoordinateSequences.CopyCoord(res, 0, res, res.Count-1);

            return res;
        }

        private static Ordinate[] GetOrdinates(CoordinateSequence sequence)
        {
            var res = new List<Ordinate>(4);
            res.Add(Ordinate.X);
            res.Add(Ordinate.Y);
            if (sequence.HasZ) res.Add(Ordinate.Z);
            if (sequence.HasM) res.Add(Ordinate.M);

            return res.ToArray();
        }

        private static int[] GetOrdinateIndices(CoordinateSequence sequence)
        {
            var res = new List<int>(4);
            res.Add(0);
            res.Add(1);
            if (sequence.TryGetOrdinateIndex(Ordinate.Z, out int indexZ))
                res.Add(indexZ);
            if (sequence.TryGetOrdinateIndex(Ordinate.M, out int indexM))
                res.Add(indexM);

            return res.ToArray();
        }

        private static bool HasOrdinate(CoordinateSequence sequence, Ordinate ordinate)
        {
            if (ordinate == Ordinate.Z)
                return sequence.HasZ;
            if (ordinate == Ordinate.M)
                return sequence.HasM;
            throw new ArgumentException(nameof(ordinate));
        }

        private static bool HasMeaningfulOrdinate(CoordinateSequence sequence, Ordinate ordinate)
        {
            if (ordinate == Ordinate.Z || ordinate == Ordinate.M)
            {
                var interval = CheckInterval(sequence, ordinate);
                return double.IsFinite(interval.Width) && (interval.Width > 0 || interval.Min != 0d) ;
            }
            throw new ArgumentException(nameof(ordinate));
        }

        private (bool has, bool create) CheckOrdinate(CoordinateSequence cs, Ordinate ordinate)
        {
            bool hasOrdinate = false;
            bool initOrdinate = false;
            if (ordinate == Ordinate.Z)
            {
                hasOrdinate = cs.HasZ;
                initOrdinate = !hasOrdinate || CheckInterval(cs, ordinate).Width == 0d;
                return (hasOrdinate, initOrdinate);
            }
            hasOrdinate = cs.HasM;
            initOrdinate = !hasOrdinate || CheckInterval(cs, ordinate).Width == 0d;
            return (hasOrdinate, initOrdinate);
        }

        static DataStructures.Interval CheckInterval(CoordinateSequence cs, Ordinate ordinate)
        {
            var res = DataStructures.Interval.Create();
            for (int i = 0; i < cs.Count; i++)
                res = res.ExpandedByValue(cs.GetOrdinate(i, ordinate));
            return res;
        }

        private void Check(Geometry geomS, Geometry geomD, Ordinates ordinates)
        {
            Assert.That(geomD.OgcGeometryType, Is.EqualTo(geomS.OgcGeometryType));
            Assert.That(geomD.NumGeometries, Is.EqualTo(geomS.NumGeometries));

            for (int i = 0; i < geomS.NumGeometries; i++)
            {
                var geomSi = geomS.GetGeometryN(i);
                var geomDi = geomD.GetGeometryN(i);
                Assert.That(geomDi.OgcGeometryType, Is.EqualTo(geomSi.OgcGeometryType));
                switch (geomSi.OgcGeometryType)
                {
                    case OgcGeometryType.Point:
                        CheckPoint((Point) geomSi, (Point) geomDi, ordinates);
                        break;
                    case OgcGeometryType.LineString:
                        CheckLineString((LineString)geomSi, (LineString)geomDi, ordinates);
                        break;
                    case OgcGeometryType.Polygon:
                        CheckPolygon((Polygon)geomSi, (Polygon)geomDi, ordinates);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        private void CheckPolygon(Polygon geomSi, Polygon geomDi, Ordinates ordinates)
        {
            CheckLineString(geomSi.ExteriorRing, geomDi.ExteriorRing, ordinates);
            for (int i = 0; i < geomSi.NumInteriorRings; i++)
                CheckLineString(geomSi.GetInteriorRingN(i), geomDi.GetInteriorRingN(i), ordinates);
        }

        private void CheckLineString(LineString geomSi, LineString geomDi, Ordinates ordinates)
        {
            CheckCoordinateSequence(geomSi.CoordinateSequence, geomDi.CoordinateSequence, ordinates);
        }

        private void CheckPoint(Point geomSi, Point geomDi, Ordinates ordinates)
        {
            CheckCoordinateSequence(geomSi.CoordinateSequence, geomDi.CoordinateSequence, ordinates);
        }

        private void CheckCoordinateSequence(CoordinateSequence csSi, CoordinateSequence csDi, Ordinates ordinates)
        {
            Assert.That(csSi.Count, Is.EqualTo(csDi.Count));
            for (int i = 0; i < csSi.Count; i++)
            {
                Assert.That(csDi.GetX(i), Is.EqualTo(csSi.GetX(i)).Within(1E-5));
                Assert.That(csDi.GetY(i), Is.EqualTo(csSi.GetY(i)).Within(1E-5));
                if ((ordinates & Ordinates.Z) == Ordinates.Z)
                    Assert.That(csDi.GetZ(i), Is.EqualTo(csSi.GetZ(i)).Within(1E-5));
                if ((ordinates & Ordinates.M) == Ordinates.M)
                    Assert.That(csDi.GetM(i), Is.EqualTo(csSi.GetM(i)).Within(1E-5));

            }
        }

        private void CreatePostGisConformanceCheckSql(string dim, Geometry geometry, byte[] twkb)
        {
            const string union = "UNION ";
            TestContext.WriteLine($"{(dim == "2D" ? string.Empty : union)}SELECT '{dim}' AS dimension, ST_Equals(ST_GeomFromText('{_wktWriter.Write(geometry)}'), ST_GeomFromTWKB(E'\\\\x{WKBWriter.ToHex(twkb)}')) AS conform{(dim != "3DM" ? string.Empty : ";")}");
        }
    }
}

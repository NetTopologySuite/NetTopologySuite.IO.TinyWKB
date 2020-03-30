using System;
using System.ComponentModel.Design;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NUnit.Framework;

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
                    csFactory = DotSpatialAffineCoordinateSequenceFactory.Instance;
                    break;

            }

            _ntsGeometryServices = new NtsGeometryServices(csFactory, pm, srid);
            _wkbReader = new WKBReader(_ntsGeometryServices);
            _wktReader = new WKTReader(_ntsGeometryServices.CreateGeometryFactory());


        }

        [TestCase("POINT (10 10)")]
        [TestCase("LINESTRING (10 10, 15 10, 20 15, 25 20)")]
        [TestCase("POLYGON ((10 10, 15 10, 15 15, 10 15, 10 10))")]
        [TestCase("POLYGON ((10 10, 15 10, 15 15, 10 15, 10 10), (11 11, 11 14, 14 14, 14 11, 11 11))")]
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

        private void Test2D(Geometry geomS)
        {
            var twkbWriter = new TinyWkbWriter();
            byte[] bytes = twkbWriter.Write(geomS);
            var twkbReader = new TinyWkbReader(_ntsGeometryServices.CreateGeometryFactory());
            var geomD = twkbReader.Read(bytes);

            Check(geomS, geomD, Ordinates.XY);
        }

        private void Test2DM(Geometry geomS)
        {
            if (CoordinateArrays.Measures(geomS.Coordinates) == 0)
                geomS = AddOrdinates(geomS, Ordinate.M);

            var twkbWriter = new TinyWkbWriter(emitM: true);
            byte[] bytes = twkbWriter.Write(geomS);
            var twkbReader = new TinyWkbReader(_ntsGeometryServices.CreateGeometryFactory());
            var geomD = twkbReader.Read(bytes);

            Check(geomS, geomD, Ordinates.XYM);
        }

        private void Test3D(Geometry geomS)
        {
            if (CoordinateArrays.Measures(geomS.Coordinates) == 0)
                geomS = AddOrdinates(geomS, Ordinate.Z);

            var twkbWriter = new TinyWkbWriter(emitZ: true);
            byte[] bytes = twkbWriter.Write(geomS);
            var twkbReader = new TinyWkbReader(_ntsGeometryServices.CreateGeometryFactory());
            var geomD = twkbReader.Read(bytes);

            Check(geomS, geomD, Ordinates.XYZ);
        }
        private void Test3DM(Geometry geomS)
        {
            if (CoordinateArrays.Measures(geomS.Coordinates) == 0)
                geomS = AddOrdinates(geomS, Ordinate.Z, Ordinate.M);

            var twkbWriter = new TinyWkbWriter(emitZ: true, emitM:true);
            byte[] bytes = twkbWriter.Write(geomS);
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

            int measures = ordinates.Contains(Ordinate.M) ? 1 : 0;
            var res = _ntsGeometryServices.DefaultCoordinateSequenceFactory.Create(sequence.Count,
                sequence.Dimension + ordinates.Length, measures);

            for (int i = 0; i < sequence.Count; i++)
            {
                res.SetX(i, sequence.GetX(i));
                res.SetY(i, sequence.GetY(i));
                for (int j = 0; j < ordinates.Length; j++)
                    res.SetOrdinate(i, ordinates[j], (100d * offset+i) + (double)ordinates[j]);
            }

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
                Assert.That(csSi.GetX(i), Is.EqualTo(csDi.GetX(i)).Within(1E-5));
                Assert.That(csSi.GetY(i), Is.EqualTo(csDi.GetY(i)).Within(1E-5));
                if ((ordinates & Ordinates.Z) == Ordinates.Z)
                    Assert.That(csSi.GetX(i), Is.EqualTo(csDi.GetX(i)).Within(1E-5));
                if ((ordinates & Ordinates.M) == Ordinates.M)
                    Assert.That(csSi.GetX(i), Is.EqualTo(csDi.GetX(i)).Within(1E-5));

            }
        }
    }
}

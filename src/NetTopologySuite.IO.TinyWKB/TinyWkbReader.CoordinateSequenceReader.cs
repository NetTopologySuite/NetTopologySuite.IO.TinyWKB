
using System.IO;
using System.Runtime.CompilerServices;
using NetTopologySuite.Geometries;

//[assembly: InternalsVisibleTo("NetTopologySuite.IO.TinyWKB.Test")]

namespace NetTopologySuite.IO
{
    public partial class TinyWkbReader
    {
        private class CoordinateSequenceReader
        {
            private readonly CoordinateSequenceFactory _coordinateSequenceFactory;
            private readonly double[] _descales;
            private readonly long[] _prevCoordinate;
            private readonly int _dimension, _measures;

            public CoordinateSequenceReader(CoordinateSequenceFactory coordinateSequenceFactory, TinyWkbHeader header)
            {
                _coordinateSequenceFactory = coordinateSequenceFactory;
                if (!header.HasExtendedPrecisionInformation || !(header.HasZ | header.HasM))
                {
                    _descales = new[] { header.DescaleX(), header.DescaleY() };
                    _dimension = 2;
                }
                else if (header.HasZ && !header.HasM)
                {
                    _descales = new[] { header.DescaleX(), header.DescaleY(), header.DescaleZ() };
                    _dimension = 3;
                }
                else if (!header.HasZ && header.HasM)
                {
                    _descales = new[] { header.DescaleX(), header.DescaleY(), header.DescaleM() };
                    _dimension = 3;
                    _measures = 1;
                }
                else
                {
                    _descales = new[] { header.DescaleX(), header.DescaleY(), header.DescaleZ(), header.DescaleM() };
                    _dimension = 4;
                    _measures = 1;
                }

                _prevCoordinate = new long[_dimension];
            }


            public CoordinateSequence Read(BinaryReader reader, int count, bool closeRing = false)
            {
                var res = _coordinateSequenceFactory.Create(count + (closeRing ? 1 : 0), _dimension, _measures);

                // copy these array references to local variables to help the JIT optimize this loop
                long[] prevCoordinate = _prevCoordinate;
                double[] descales = _descales;

                for (int i = 0; i < count; i++)
                {
                    for (int dim = 0; dim < prevCoordinate.Length; dim++)
                    {
                        prevCoordinate[dim] += ReadVarint(reader);
                        res.SetOrdinate(i, dim, ToDouble(prevCoordinate[dim], descales[dim]));
                    }
                }

                if (closeRing)
                {
                    for (int dim = 0; dim < res.Dimension; dim++)
                    {
                        res.SetOrdinate(count, dim, res.GetOrdinate(0, dim));
                    }
                }

                return res;
            }
        }
    }
}

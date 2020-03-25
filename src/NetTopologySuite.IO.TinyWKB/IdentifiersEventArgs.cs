using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO
{
    /// <summary>
    /// Event arguments for <see cref="TinyWkbWriter.IdentifiersRequired"/>
    /// </summary>
    public class IdentifiersEventArgs : EventArgs
    {
        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        /// <param name="gc">A geometry collection</param>
        /// <param name="idList"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public IdentifiersEventArgs(GeometryCollection gc, IList<long> idList = null)
        {
            Geometry = gc ?? throw new ArgumentNullException(nameof(gc));

            if (idList != null && idList.Count != gc.Count)
                throw new ArgumentException(nameof(idList));
            IdList = idList ?? new long[gc.Count];
        }

        /// <summary>
        /// Gets the geometry for the
        /// </summary>
        public GeometryCollection Geometry { get; }

        /// <summary>
        /// Gets the number of identifiers in <see cref="IdList"/>
        /// </summary>
        public int Count { get => Geometry.Count; }

        /// <summary>
        /// Gets or sets a value indicating the list of identifiers
        /// </summary>
        public IList<long> IdList { get; }
    }
}

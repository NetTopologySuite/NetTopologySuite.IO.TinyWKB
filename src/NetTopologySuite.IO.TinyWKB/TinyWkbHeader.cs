using System;
using System.IO;

namespace NetTopologySuite.IO
{
    /// <summary>
    /// TinyWKB header
    /// </summary>
    public struct TinyWkbHeader
    {
        /// <summary>
        /// Flag indicating that
        /// </summary>
        [Flags]
        private enum Metadata
        {
            HasBoundingBox = 1 << 8,
            HasSize = 1 << 9,
            HasIdList = 1 << 10,
            HasExtendedPrecisionInformation = 1 << 11,
            HasEmptyGeometry = 1 << 12,
            HasZ = 1 << 16,
            HasM = 1 << 17
        }



        private readonly int _value;

        /// <summary>
        /// Utility function to read the header using a binary reader
        /// </summary>
        /// <param name="reader">The binary reader to use</param>
        /// <returns>The header</returns>
        public static TinyWkbHeader Read(BinaryReader reader)
        {
            byte header = reader.ReadByte();
            byte metadata = reader.ReadByte();
            return ((metadata << 8) & (int)Metadata.HasExtendedPrecisionInformation) != 0
                ? new TinyWkbHeader(header, metadata, reader.ReadByte())
                : new TinyWkbHeader(header, metadata);
        }

        /// <summary>
        /// Utility method to write a header using a binary writer
        /// </summary>
        /// <param name="writer">The binary writer to use</param>
        /// <param name="header">The header</param>
        public static void Write(BinaryWriter writer, TinyWkbHeader header)
        {
            writer.Write((ushort)(0xffff & header._value));
            if (header.HasExtendedPrecisionInformation)
                writer.Write((byte)(0xFF & (header._value >> 16)));
        }

        /// <summary>
        /// Creates an instance of this header using the provided arguments
        /// </summary>
        /// <param name="header">The base header</param>
        /// <param name="metadata">The metadata</param>
        /// <param name="epi">The extended precision information</param>
        private TinyWkbHeader(byte header, byte metadata, byte epi = 0) : this()
        {
            _value = header | metadata << 8 | epi << 16;
        }

        /// <summary>
        /// Creates an instance of this class using the provided data.
        /// </summary>
        /// <param name="geometryType">The geometry type</param>
        /// <param name="precisionXY">The number of decimal places for x- and y-ordinate values. A negative value is allowed</param>
        /// <param name="hasEmptyGeometry">A flag indicating that the geometry is empty. If <c>true</c> some input parameters are overridden.</param>
        /// <param name="hasBoundingBox">A flag indicating that the bounding box information should be written.</param>
        /// <param name="hasSize">A flag indicating that the size of the geometry data is part of the header.</param>
        /// <param name="hasIdList">A flag indicating that an id-list is written. This applies to multi-geometries only.</param>
        /// <param name="hasZ">A flag indicating that z-ordinates are present</param>
        /// <param name="precisionZ">The number of decimal places for z-ordinate values</param>
        /// <param name="hasM">A flag indicating that m-ordinates are present</param>
        /// <param name="precisionM">The number of decimal places for m-ordinate values</param>
        public TinyWkbHeader(TinyWkbGeometryType geometryType, int precisionXY = 7,
            bool hasEmptyGeometry = false,
            bool hasBoundingBox = true, bool hasSize = false, bool hasIdList = false,
            bool hasZ = false, int precisionZ = 7, bool hasM = false, int precisionM = 7)
            : this()
        {
            if (precisionXY < -7 || 7 < precisionXY)
                throw new ArgumentOutOfRangeException(nameof(precisionXY));

            if (precisionZ < 0 || 7 < precisionXY)
                throw new ArgumentOutOfRangeException(nameof(precisionZ));
            if (precisionM < 0 || 7 < precisionXY)
                throw new ArgumentOutOfRangeException(nameof(precisionM));

            // encode xy precision.
            int p = (int) VarintBitConverter.EncodeZigZag(precisionXY, 4);

            // We don't write bounding boxes for points.
            if (geometryType == TinyWkbGeometryType.Point)
                hasBoundingBox = false;

            // Remove if empty
            if (hasEmptyGeometry)
            {
                hasBoundingBox = false;
                hasSize = false;
                hasZ = false;
                hasM = false;
            }

            // No idlists, for single instance geometries
            if (geometryType < TinyWkbGeometryType.MultiPoint)
                hasIdList = false;

            int metadata = 0;
            if (hasBoundingBox) metadata |= (int) Metadata.HasBoundingBox;
            if (hasSize) metadata |= (int)Metadata.HasSize;
            if (hasIdList) metadata |= (int)Metadata.HasIdList;
            if (hasZ | hasM) metadata |= (int)Metadata.HasExtendedPrecisionInformation;
            if (hasEmptyGeometry) metadata |= (int)Metadata.HasEmptyGeometry;
            if (hasZ) {
                metadata |= (int)Metadata.HasZ;
                metadata |= (0x07 & precisionZ) << 18;
            }
            if (hasM) {
                metadata |= (int)Metadata.HasM;
                metadata |= (0x07 & precisionZ) << 21;
            }

            _value = (int) geometryType | (p << 4) | metadata;

        }

        /// <summary>
        /// Gets a value indicating the geometry type.
        /// </summary>
        public TinyWkbGeometryType GeometryType => (TinyWkbGeometryType)(_value & 0x0f);

        /// <summary>
        /// Gets a value indicating the number of decimal places for x- and y-ordinate values.
        /// </summary>
        public int PrecisionXY => (int)VarintBitConverter.DecodeZigZag((ulong)((_value & 0xf0) >> 4));

        /// <summary>
        /// Gets a value indicating that bounding box information is present
        /// </summary>
        public bool HasBoundingBox => (_value & (int)Metadata.HasBoundingBox) != 0;

        /// <summary>
        /// Gets a value indicating that the number of bytes required for the geometry definition is present.
        /// </summary>
        public bool HasSize => (_value & (int)Metadata.HasSize) != 0;

        /// <summary>
        /// Gets a value indicating that the an id-list is present.
        /// </summary>
        public bool HasIdList => (_value & (int)Metadata.HasIdList) != 0;

        /// <summary>
        /// Gets a value indicating that extended precision information for z- and/or m-ordinates is present.
        /// </summary>
        public bool HasExtendedPrecisionInformation => (_value & (int)Metadata.HasExtendedPrecisionInformation) != 0;

        /// <summary>
        /// Gets a value indicating that the geometry is empty
        /// </summary>
        public bool HasEmptyGeometry => (_value & (int)Metadata.HasEmptyGeometry) != 0;

        /// <summary>
        /// Gets a value indicating that z-ordinates are present.
        /// </summary>
        public bool HasZ => (_value & (int)Metadata.HasZ) != 0;

        /// <summary>
        /// Gets a value indicating that m-ordinates are present.
        /// </summary>
        public bool HasM => (_value & (int)Metadata.HasM) != 0;

        /// <summary>
        /// Gets a value indicating the number of decimal places for z-ordinates.
        /// </summary>
        public int PrecisionZ => 0x07 & (_value >> 18);

        /// <summary>
        /// Gets a value indicating the number of decimal places for m-ordinates.
        /// </summary>
        public int PrecisionM => 0x07 & (_value >> 21);
    }
}

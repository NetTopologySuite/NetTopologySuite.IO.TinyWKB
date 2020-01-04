namespace NetTopologySuite.IO
{
    internal struct MetadataHeader
    {
        private readonly byte _mh;

        public MetadataHeader(byte mh)
        {
            _mh = mh;
        }

        public MetadataHeader(bool hasBoundingBox, bool hasSize, bool hasIdList, bool hasExtendedPrecisionInformation, bool hasEmptyGeometry)
        {
            // Note: writing IdList and Size is not supported
            byte mh = (byte) (hasBoundingBox ? 1 : 0);
            mh |= (byte)(/*hasSize*/false ? 2 : 0);
            mh |= (byte)(/*hasIdList*/false ? 4 : 0);
            mh |= (byte)(hasExtendedPrecisionInformation ? 8 : 0);
            mh |= (byte)(hasEmptyGeometry ? 16 : 0);

            _mh = mh;
        }

        public byte Value => _mh;

        public bool HasBoundingBox => (_mh & 1) != 0;
        public bool HasSize => (_mh & 2) != 0;
        public bool HasIdList => (_mh & 4) != 0;
        public bool HasExtendedPrecisionInformation => (_mh & 8) != 0;
        public bool HasEmptyGeometry => (_mh & 16) != 0;
        public bool Unused6 => (_mh & 32) != 0;
        public bool Unused7 => (_mh & 64) != 0;
        public bool Unused8 => (_mh & 128) != 0;
    }
}

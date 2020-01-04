namespace NetTopologySuite.IO
{
    internal struct MetadataHeader
    {
        private readonly byte _mh;

        public MetadataHeader(byte mh)
        {
            _mh = mh;
        }

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

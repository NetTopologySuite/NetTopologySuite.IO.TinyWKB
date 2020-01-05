namespace NetTopologySuite.IO
{
    internal struct ExtendedPrecisionInformation
    {
        private readonly byte _epi;

        public ExtendedPrecisionInformation(byte epi)
        {
            _epi = epi;
        }

        public ExtendedPrecisionInformation(bool hasZ, int precisionZ, bool hasM, int precisionM) 
            : this()
        {
            byte epi = (byte)(hasZ ? 1 : 0);
            epi |= (byte)(hasM ? 2 : 0);
            epi |= (byte)((precisionZ & 7) << 2);
            epi |= (byte)((precisionM & 7) << 5);

            _epi = epi;
        }

        public bool HasZ => (_epi & 0x01) != 0;
        public bool HasM => (_epi & 0x02) != 0;

        public int PrecisionZ => (_epi & 0x1C) >> 2;
        public int PrecisionM => (_epi & 0xE0) >> 5;

        public byte Value => _epi;
    }
}

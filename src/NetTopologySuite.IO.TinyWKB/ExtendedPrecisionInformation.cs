namespace NetTopologySuite.IO
{
    internal struct ExtendedPrecisionInformation
    {
        private readonly byte _epi;

        public ExtendedPrecisionInformation(byte epi)
        {
            _epi = epi;
        }
        public bool HasZ => (_epi & 0x01) != 0;
        public bool HasM => (_epi & 0x02) != 0;

        public int PrecisionZ => (_epi & 0x1C) >> 2;
        public int PrecisionM => (_epi & 0xE0) >> 5;

    }
}

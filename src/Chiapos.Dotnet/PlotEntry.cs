using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class PlotEntry
    {
        public ulong y;
        public ulong pos;
        public ulong offset;
        public UInt128 left_metadata;  // We only use left_metadata, unless metadata does not
        public UInt128 right_metadata; // fit in 128 bits.
        public bool used;                 // Whether the entry was used in the next table of matches
        public ulong read_posoffset;   // The combined pos and offset that this entry points to
    }
}
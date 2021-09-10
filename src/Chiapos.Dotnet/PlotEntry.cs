using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class PlotEntry
    {
        public ulong y;
        public ulong pos;
        public Bits2 left_metadata;
        public bool used;                 // Whether the entry was used in the next table of matches
        public ulong read_posoffset;   // The combined pos and offset that this entry points to
    }
}
using System;

namespace chiapos_dotnet
{
    [Flags]
    public enum PhaseFlags : byte
    {
        EnableBitfield = 1 << 0,
        ShowProgress = 1 << 1
    }
}
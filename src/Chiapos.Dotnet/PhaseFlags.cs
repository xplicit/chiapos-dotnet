using System;

namespace chiapos_dotnet
{
    [Flags]
    public enum PhaseFlags : byte
    {
        None = 0,
        EnableBitfield = 1 << 0,
        ShowProgress = 1 << 1
    }
}
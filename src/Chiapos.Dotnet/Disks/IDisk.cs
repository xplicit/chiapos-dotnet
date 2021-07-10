using System;

namespace Chiapos.Dotnet.Disks
{
    public interface IDisk : IDisposable
    {
        ReadOnlySpan<byte> Read(ulong begin, ulong length);
        void Write(ulong begin, ReadOnlySpan<byte> buffer);
        void Truncate(ulong new_size);
        void FreeMemory();
        string GetFileName();
    }
}
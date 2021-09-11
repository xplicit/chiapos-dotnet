using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Chiapos.Dotnet
{
    public class Bitfield
    {
        private ulong[] m_array;
        private ulong m_length;
        
        private const int BitsPerInt64 = 64;
        private const int BitsPerByte = 8;

        private const int BitShiftPerInt64 = 6;
        private const int BitShiftPerByte = 3;

        public ulong Length => (ulong)m_length;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetInt64ArrayLengthFromBitLength(ulong n)
        {
            Debug.Assert(n >= 0);
            return (int)((n - 1UL + (1UL << BitShiftPerInt64)) >> BitShiftPerInt64);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetByteArrayLengthFromBitLength(int n)
        {
            Debug.Assert(n >= 0);
            return (int)((uint)(n - 1 + (1 << BitShiftPerByte)) >> BitShiftPerByte);
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Div64Rem(int number, out int remainder)
        {
            uint quotient = (uint)number / 64;
            remainder = number & (64 - 1);
            return (int)quotient;
        }
        
        public Bitfield(ulong length)
        {
            m_array = new ulong[(int)GetInt64ArrayLengthFromBitLength(length)];
            m_length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(ulong index)
        {
            ulong num = 1UL << (int)(index & (BitsPerInt64 - 1));
            ref ulong local = ref this.m_array[index >> BitShiftPerInt64];
            local |= num;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(ulong index)
        {
            ulong num = 1UL << (int)(index & (BitsPerInt64 - 1));
            ref ulong local = ref this.m_array[index >> BitShiftPerInt64];
            return (local & num) > 0;
        }

        public ulong Count(ulong startBit, ulong endBit)
        {
            int startIdx = (int)(startBit >> BitShiftPerInt64);
            int endIdx = (int)(endBit >> BitShiftPerInt64);

            ulong result = 0;

            while (startIdx != endIdx)
            {
                result += (ulong)BitOperations.PopCount(m_array[startIdx]);
                startIdx++;
            }

            int tail = (int)(endBit & (BitsPerInt64 - 1));
            if (tail > 0)
            {
                ulong mask = (1UL << tail) - 1;
                result += (ulong)BitOperations.PopCount(m_array[endIdx] & mask);
            }

            return result;
        }

        public void Clear()
        {
            Span<ulong> span = m_array.AsSpan(0, GetInt64ArrayLengthFromBitLength(m_length));
            span.Clear();
        }
        
        public static void Swap(ref Bitfield lhs, ref Bitfield rhs)
        {
            (rhs, lhs) = (lhs, rhs);
        }

        public void FreeMemory()
        {
        }
    }
}
using System.Collections;

namespace Chiapos.Dotnet
{
    public class BitfieldOld
    {
        private BitArray bitArray;

        public BitfieldOld(long size) : this((ulong)size) {}
        public BitfieldOld(ulong size)
        {
            bitArray = new BitArray((int) size);
        }

        public bool Get(ulong index)
        {
            return bitArray.Get((int) index);
        }

        public void Set(ulong index)
        {
            bitArray.Set((int)index, true);
        }

        public void Clear()
        {
            bitArray.SetAll(false);
        }

        public ulong Length => (ulong)bitArray.Length;

        public void Swap(ref BitfieldOld rhs)
        {
            var tmp = rhs.bitArray;
            rhs.bitArray = this.bitArray;
            this.bitArray = tmp;
        }

        public ulong Count(ulong startBit, ulong endBit)
        {
            ulong result = 0;
            for (int i = (int)startBit; i < (int)endBit; i++)
            {
                result += bitArray[i] ? 1U : 0U;
            }

            return result;
        }

        public void FreeMemory()
        {
            bitArray = null;
        }
        
    }
}

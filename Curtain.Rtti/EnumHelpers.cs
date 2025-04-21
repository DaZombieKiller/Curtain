using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Curtain.Rtti;

internal static class EnumHelpers
{
    public static unsafe TEnum GetFlagsMask<TEnum>()
        where TEnum : unmanaged, Enum
    {
        return EnumInfo<TEnum>.FlagsMask;
    }

    private static class EnumInfo<TEnum>
        where TEnum : unmanaged, Enum
    {
        public static readonly TEnum FlagsMask = GetFlagsMask();

        private static unsafe TEnum GetFlagsMask()
        {
            if (sizeof(TEnum) == sizeof(byte))
                return GetFlagsMask<byte>();
            else if (sizeof(TEnum) == sizeof(ushort))
                return GetFlagsMask<ushort>();
            else if (sizeof(TEnum) == sizeof(uint))
                return GetFlagsMask<uint>();
            else if (sizeof(TEnum) == sizeof(ulong))
                return GetFlagsMask<ulong>();

            static TEnum GetFlagsMask<T>()
                where T : unmanaged, IBinaryInteger<T>
            {
                var mask = T.Zero;

                foreach (T value in MemoryMarshal.Cast<TEnum, T>(Enum.GetValues<TEnum>()))
                    mask |= value;

                return Unsafe.BitCast<T, TEnum>(mask);
            }

            throw new NotSupportedException();
        }
    }
}

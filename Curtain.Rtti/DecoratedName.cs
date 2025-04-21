using AsmResolver;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Curtain.Rtti;

/// <summary>Provides methods for operating on decorated symbol names.</summary>
public static unsafe class DecoratedName
{
    /// <summary>Converts the provided decorated symbol name to its un-decorated form.</summary>
    public static string UnDecorate(Utf8String name, UnDecorateFlags flags = UnDecorateFlags.Complete)
    {
        return UnDecorate(name.AsSpan(), flags);
    }

    /// <summary>Converts the provided decorated symbol name to its un-decorated form.</summary>
    public static string UnDecorate(ReadOnlySpan<byte> name, UnDecorateFlags flags = UnDecorateFlags.Complete)
    {
        if (name.StartsWith((byte)'.'))
        {
            name = name[1..];
            flags |= UnDecorateFlags.Decode32Bit | UnDecorateFlags.TypeOnly;
        }

        [DllImport("DbgHelp", ExactSpelling = true, SetLastError = true)]
        static extern uint UnDecorateSymbolName(sbyte* name, sbyte* outputString, uint maxStringLength, uint flags);

        return name.IsEmpty ? string.Empty : UnDecorate(MemoryMarshal.Cast<byte, sbyte>(name), flags, &UnDecorateSymbolName);
    }

    /// <summary>Converts the provided decorated symbol name to its un-decorated form.</summary>
    public static string UnDecorate(ReadOnlySpan<char> name, UnDecorateFlags flags = UnDecorateFlags.Complete)
    {
        if (name.StartsWith('.'))
        {
            name = name[1..];
            flags |= UnDecorateFlags.Decode32Bit | UnDecorateFlags.TypeOnly;
        }

        [DllImport("DbgHelp", ExactSpelling = true, SetLastError = true)]
        static extern uint UnDecorateSymbolNameW(char* name, char* outputString, uint maxStringLength, uint flags);

        return name.IsEmpty ? string.Empty : UnDecorate(name, flags, &UnDecorateSymbolNameW);
    }

    /// <summary>Converts the provided decorated symbol name to its un-decorated form.</summary>
    private static string UnDecorate<T>(ReadOnlySpan<T> name, UnDecorateFlags flags, delegate*<T*, T*, uint, uint, uint> function)
        where T : unmanaged
    {
        uint written;
        string result;
        T[]? buffer = null;

        fixed (T* pName = name)
        {
            int length = name.Length;

            do
            {
                if (buffer != null)
                    ArrayPool<T>.Shared.Return(buffer);

                length *= 2;
                buffer = ArrayPool<T>.Shared.Rent(length);

                fixed (T* pBuffer = buffer)
                    written = function(pName, pBuffer, (uint)buffer.Length, (uint)flags);

                // UnDecorateSymbolName name returns 0 on failure, and maxStringLength-2 on truncation.
                if (written == 0)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            } while (written == buffer.Length - 2);
        }

        if (typeof(T) == typeof(char))
            result = MemoryMarshal.Cast<T, char>(buffer.AsSpan(0, (int)written)).ToString();
        else
        {
            fixed (T* pBuffer = buffer)
            {
                result = new string((sbyte*)pBuffer, 0, (int)written);
            }
        }

        ArrayPool<T>.Shared.Return(buffer);
        return result;
    }
}

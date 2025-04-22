using System.Buffers;
using System.Runtime.InteropServices;

namespace Curtain.Rtti;

/// <summary>Provides methods for operating on decorated symbol names.</summary>
public static unsafe class DecoratedName
{
    /// <summary>Converts the provided decorated symbol name to its un-decorated form.</summary>
    public static string UnDecorate(ReadOnlySpan<char> name, UnDecorateFlags flags = UnDecorateFlags.Complete)
    {
        if (name.StartsWith('.'))
        {
            name = name[1..];
            flags |= UnDecorateFlags.Decode32Bit | UnDecorateFlags.TypeOnly;
        }

        if (name.IsEmpty)
            return string.Empty;

        uint written;
        char[]? buffer = null;

        fixed (char* pName = name)
        {
            int length = name.Length;

            do
            {
                if (buffer != null)
                    ArrayPool<char>.Shared.Return(buffer);

                length *= 2;
                buffer = ArrayPool<char>.Shared.Rent(length);

                [DllImport("DbgHelp", ExactSpelling = true, SetLastError = true)]
                static extern uint UnDecorateSymbolNameW(char* name, char* outputString, uint maxStringLength, uint flags);

                fixed (char* pBuffer = buffer)
                    written = UnDecorateSymbolNameW(pName, pBuffer, (uint)buffer.Length, (uint)flags);

                // UnDecorateSymbolName name returns 0 on failure, and maxStringLength-2 on truncation.
                if (written == 0)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            } while (written == buffer.Length - 2);
        }

        string result = buffer.AsSpan(0, (int)written).ToString();
        ArrayPool<char>.Shared.Return(buffer);
        return result;
    }
}

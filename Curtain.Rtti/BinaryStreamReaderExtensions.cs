using AsmResolver;
using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver.PE.File;
using AsmResolver.PE.Platforms;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Curtain.Rtti;

/// <summary>Provides extensions for <see cref="BinaryStreamReader"/>.</summary>
internal static class BinaryStreamReaderExtensions
{
    public static uint ReadRttiPtr(ref this BinaryStreamReader reader, RttiModule module)
    {
        if (module.Platform.Is32Bit)
            return module.PEFile.AddressToRva(reader.ReadUInt32());

        return reader.ReadUInt32();
    }

    /// <inheritdoc cref="BinaryStreamReader.ReadNativeInt"/>
    public static ulong ReadNativeInt(ref this BinaryStreamReader reader, PEImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        return reader.ReadNativeInt(Platform.Get(image.MachineType).Is32Bit);
    }

    /// <inheritdoc cref="BinaryStreamReader.ReadNativeInt"/>
    public static ulong ReadNativeInt(ref this BinaryStreamReader reader, PEFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return reader.ReadNativeInt(Platform.Get(file.FileHeader.Machine).Is32Bit);
    }

    public static bool IsNext(ref this BinaryStreamReader reader, ReadOnlySpan<byte> next, bool advancePast = false)
    {
        if (reader.TryGetRemainingSpan(out ReadOnlySpan<byte> remaining))
        {
            if (!remaining.StartsWith(next))
                return false;

            if (advancePast)
                reader.Offset += (uint)next.Length;

            return true;
        }

        if (!reader.CanRead((uint)next.Length))
            return false;

        var offset = reader.Offset;
        var buffer = ArrayPool<byte>.Shared.Rent(next.Length);
        reader.ReadBytes(buffer);
        bool isNext = buffer.AsSpan(0, next.Length).SequenceEqual(next);
        ArrayPool<byte>.Shared.Return(buffer);

        if (!isNext || !advancePast)
            reader.Offset = offset;

        return isNext;
    }

    /// <inheritdoc cref="BinaryStreamReader.AdvanceUntil"/>
    public static bool FastAdvanceUntil(ref this BinaryStreamReader reader, byte delimiter, bool consumeDelimiter)
    {
        if (TryGetRemainingSpan(ref reader, out ReadOnlySpan<byte> remaining))
            return FastAdvanceUntil(ref reader, remaining, delimiter, consumeDelimiter);

        return reader.AdvanceUntil(delimiter, consumeDelimiter);
    }

    public static Utf8String FastReadUtf8String(this scoped ref BinaryStreamReader reader)
    {
        if (!TryGetRemainingSpan(ref reader, out ReadOnlySpan<byte> remaining))
            return reader.ReadUtf8String();

        int length = remaining.IndexOf((byte)0);
        var result = length == 0 ? Utf8String.Empty : new Utf8String(length == -1 ? remaining : remaining[..length]);

        if (length == -1)
            reader.Offset += reader.Length;
        else
            reader.Offset += (uint)length + 1;

        return result;
    }

    /// <summary>Advances the reader until the provided delimiter is reached.</summary>
    /// <param name="delimiter">The delimiter to stop at.</param>
    /// <param name="consumeDelimiter"><c>true</c> if the final delimiter should be consumed if available, <c>false</c> otherwise.</param>
    /// <returns><c>true</c> if the delimiter was found and consumed, <c>false</c> otherwise.</returns>
    public static bool AdvanceUntil(ref this BinaryStreamReader reader, ReadOnlySpan<byte> delimiter, bool consumeDelimiter)
    {
        ArgumentOutOfRangeException.ThrowIfZero(delimiter.Length);

        if (delimiter.Length == 1)
            return FastAdvanceUntil(ref reader, delimiter[0], consumeDelimiter);

        if (TryGetRemainingSpan(ref reader, out ReadOnlySpan<byte> remaining))
            return FastAdvanceUntil(ref reader, remaining, delimiter, consumeDelimiter);

        while (reader.CanRead((uint)delimiter.Length))
        {
            uint offset = reader.RelativeOffset;

            for (int i = 0; i <= delimiter.Length; i++)
            {
                if (i == delimiter.Length)
                    return true;

                if (reader.ReadByte() != delimiter[i])
                {
                    reader.RelativeOffset = offset + 1;
                    break;
                }
            }
        }

        reader.RelativeOffset = reader.Length;
        return false;
    }

    public static unsafe bool TryGetRemainingSpan(ref this BinaryStreamReader reader, out ReadOnlySpan<byte> remaining)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_data")]
        static extern ref byte[] GetByteArray(ByteArrayDataSource source);

        if (reader.RemainingLength > int.MaxValue)
        {
            remaining = default;
            return false;
        }

        if (reader.DataSource is ByteArrayDataSource array)
        {
            remaining = GetByteArray(array).AsSpan((int)(uint)reader.Offset, (int)reader.RemainingLength);
            return true;
        }

        if (reader.DataSource is UnmanagedDataSource unmanaged)
        {
            remaining = new ReadOnlySpan<byte>((byte*)unmanaged.BaseAddress + reader.Offset, (int)reader.RemainingLength);
            return true;
        }

        remaining = default;
        return false;
    }

    private static bool FastAdvanceUntil(ref BinaryStreamReader reader, ReadOnlySpan<byte> remaining, byte delimiter, bool consumeDelimiter)
    {
        int index = remaining.IndexOf(delimiter);

        if (index == -1)
        {
            reader.RelativeOffset = reader.Length;
            return false;
        }

        reader.RelativeOffset += (uint)index + (consumeDelimiter ? 1u : 0u);
        return true;
    }

    private static bool FastAdvanceUntil(ref BinaryStreamReader reader, ReadOnlySpan<byte> remaining, ReadOnlySpan<byte> delimiter, bool consumeDelimiter)
    {
        int index = remaining.IndexOf(delimiter);

        if (index == -1)
        {
            reader.RelativeOffset = reader.Length;
            return false;
        }

        reader.RelativeOffset += (uint)index + (consumeDelimiter ? (uint)delimiter.Length : 0u);
        return true;
    }
}

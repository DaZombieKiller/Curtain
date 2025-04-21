using System.Collections.Immutable;

namespace Curtain.Rtti;

public sealed class VirtualFunctionTable : RttiObject, IRttiObject<VirtualFunctionTable>
{
    public RttiCompleteObjectLocator CompleteObjectLocator { get; }

    public ImmutableArray<ulong> FunctionPointers { get; }

    internal VirtualFunctionTable(RttiModule module, uint rva)
        : base(module, rva)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rva, (uint)module.Platform.PointerSize);
        var reader = module.PEFile.CreateReaderAtRva(rva - (uint)module.Platform.PointerSize);
        var colRva = module.PEFile.AddressToRva(reader.ReadNativeInt(module.Platform.Is32Bit));
        CompleteObjectLocator = module.GetOrAddObject<RttiCompleteObjectLocator>(colRva) ?? throw new ArgumentException(null, nameof(rva));
        var builder = ImmutableArray.CreateBuilder<ulong>();

        while (reader.CanRead((uint)module.Platform.PointerSize))
        {
            var pointer = reader.ReadNativeInt(module.Platform.Is32Bit);

            if (!module.IsValidFunctionPointer(pointer))
                break;

            builder.Add(pointer);
        }

        FunctionPointers = builder.DrainToImmutable();
        CompleteObjectLocator.VTable = this;
    }

    /// <inheritdoc/>
    static VirtualFunctionTable IRttiObject<VirtualFunctionTable>.CreateInstance(RttiModule module, uint rva)
    {
        return new VirtualFunctionTable(module, rva);
    }
}

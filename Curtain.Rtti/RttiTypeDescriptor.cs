using AsmResolver;
using System.Collections.ObjectModel;

namespace Curtain.Rtti;

public sealed class RttiTypeDescriptor : RttiObject, IRttiObject<RttiTypeDescriptor>
{
    /// <summary>Gets the mangled name of the type.</summary>
    public Utf8String Name { get; }

    /// <summary>Gets the internal storage for <see cref="CompleteObjectLocators"/>.</summary>
    internal List<RttiCompleteObjectLocator> CompleteObjectLocatorsList { get; } = [];

    /// <summary>Gets the <see cref="RttiCompleteObjectLocator"/>s that reference this <see cref="RttiTypeDescriptor"/>.</summary>
    public ReadOnlyCollection<RttiCompleteObjectLocator> CompleteObjectLocators { get; }

    /// <summary>Gets the <see cref="RttiTypeKind"/> of the type.</summary>
    public RttiTypeKind Kind => Name[3] switch
    {
        'T' => RttiTypeKind.Union,
        'U' => RttiTypeKind.Struct,
        'V' => RttiTypeKind.Class,
        'W' => RttiTypeKind.Enum,
        _ => RttiTypeKind.Invalid,
    };

    internal RttiTypeDescriptor(RttiModule module, uint rva)
        : base(module, rva)
    {
        var reader = module.PEFile.CreateReaderAtRva(rva);
        CompleteObjectLocators = CompleteObjectLocatorsList.AsReadOnly();
        
        if (reader.ReadNativeInt(module.Platform.Is32Bit) != module.TypeInfoVPtr)
            throw new ArgumentException(null, nameof(rva));

        if (reader.ReadNativeInt(module.Platform.Is32Bit) != 0)
            throw new ArgumentException(null, nameof(rva));

        if (!reader.IsNext(".?A"u8))
            throw new ArgumentException(null, nameof(rva));

        Name = reader.FastReadUtf8String();
    }

    /// <inheritdoc/>
    static RttiTypeDescriptor IRttiObject<RttiTypeDescriptor>.CreateInstance(RttiModule module, uint rva)
    {
        return new RttiTypeDescriptor(module, rva);
    }
}

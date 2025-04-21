namespace Curtain.Rtti;

public sealed class RttiCompleteObjectLocator : RttiObject, IRttiObject<RttiCompleteObjectLocator>
{
    public uint Offset { get; }

    public uint ConstructionDisplacement { get; }

    public RttiTypeDescriptor TypeDescriptor { get; }

    public RttiClassHierarchyDescriptor ClassDescriptor { get; }

    public VirtualFunctionTable VTable { get; internal set; } = null!;

    internal RttiCompleteObjectLocator(RttiModule module, uint rva)
        : base(module, rva)
    {
        var reader = module.PEFile.CreateReaderAtRva(rva);
        
        if (reader.ReadUInt32() != module.CompleteObjectLocatorSignature)
            throw new ArgumentException(null, nameof(rva));

        Offset = reader.ReadUInt32();
        ConstructionDisplacement = reader.ReadUInt32();
        var typeDescriptorPtr = reader.ReadRttiPtr(module);
        var classDescriptorPtr = reader.ReadRttiPtr(module);

        if (module.Platform.Is64Bit && reader.ReadUInt32() != rva)
            throw new ArgumentException(null, nameof(rva));

        TypeDescriptor = module.GetOrAddObject<RttiTypeDescriptor>(typeDescriptorPtr) ?? throw new ArgumentException(null, nameof(rva));
        ClassDescriptor = module.GetOrAddObject<RttiClassHierarchyDescriptor>(classDescriptorPtr) ?? throw new ArgumentException(null, nameof(rva));
    }

    /// <inheritdoc/>
    static RttiCompleteObjectLocator IRttiObject<RttiCompleteObjectLocator>.CreateInstance(RttiModule module, uint rva)
    {
        return new RttiCompleteObjectLocator(module, rva);
    }
}

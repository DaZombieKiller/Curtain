namespace Curtain.Rtti;

public sealed class RttiBaseClassDescriptor : RttiObject, IRttiObject<RttiBaseClassDescriptor>
{
    public RttiTypeDescriptor TypeDescriptor { get; }

    public uint BaseCount { get; }

    // TODO: Better names for these

    public uint MDisplacement { get; }

    public uint PDisplacement { get; }

    public uint VDisplacement { get; }

    public RttiBaseClassFlags Attributes { get; }

    public RttiClassHierarchyDescriptor? ClassDescriptor { get; }

    public bool IsPrivateOrProtected => (Attributes & RttiBaseClassFlags.PrivateOrProtectedMask) != 0;

    public bool IsVirtual => Attributes.HasFlag(RttiBaseClassFlags.VirtualBaseOfContainingObject);

    internal RttiBaseClassDescriptor(RttiModule module, uint rva)
        : base(module, rva)
    {
        var reader = module.PEFile.CreateReaderAtRva(rva);
        var typeDescriptorPtr = reader.ReadRttiPtr(module);
        BaseCount = reader.ReadUInt32();
        MDisplacement = reader.ReadUInt32();
        PDisplacement = reader.ReadUInt32();
        VDisplacement = reader.ReadUInt32();
        Attributes = (RttiBaseClassFlags)reader.ReadUInt32();

        if ((Attributes & ~EnumHelpers.GetFlagsMask<RttiBaseClassFlags>()) != 0)
            throw new ArgumentException(null, nameof(rva));

        if (Attributes.HasFlag(RttiBaseClassFlags.HasClassHierarchy))
            ClassDescriptor = module.GetOrAddObject<RttiClassHierarchyDescriptor>(reader.ReadRttiPtr(module));

        TypeDescriptor = module.GetOrAddObject<RttiTypeDescriptor>(typeDescriptorPtr) ?? throw new ArgumentException(null, nameof(rva));
    }

    /// <inheritdoc/>
    static RttiBaseClassDescriptor IRttiObject<RttiBaseClassDescriptor>.CreateInstance(RttiModule module, uint rva)
    {
        return new RttiBaseClassDescriptor(module, rva);
    }
}

namespace Curtain.Rtti;

public sealed class RttiClassHierarchyDescriptor : RttiObject, IRttiObject<RttiClassHierarchyDescriptor>
{
    public RttiClassHierarchyFlags Attributes { get; }

    public RttiBaseClassArray BaseClassArray { get; }

    internal RttiClassHierarchyDescriptor(RttiModule module, uint rva)
        : base(module, rva)
    {
        var reader = module.PEFile.CreateReaderAtRva(rva);

        if (reader.ReadUInt32() != module.ClassHierarchySignature)
            throw new ArgumentException(null, nameof(rva));

        Attributes = (RttiClassHierarchyFlags)reader.ReadUInt32();

        if ((Attributes & ~EnumHelpers.GetFlagsMask<RttiClassHierarchyFlags>()) != 0)
            throw new ArgumentException(null, nameof(rva));

        uint baseCount = reader.ReadUInt32();
        BaseClassArray = module.GetOrAddObject<RttiBaseClassArray>(reader.ReadRttiPtr(module)) ?? throw new ArgumentException(null, nameof(rva));

        if (!BaseClassArray.Initialize(baseCount))
        {
            throw new ArgumentException(null, nameof(rva));
        }
    }

    /// <inheritdoc/>
    static RttiClassHierarchyDescriptor IRttiObject<RttiClassHierarchyDescriptor>.CreateInstance(RttiModule module, uint rva)
    {
        return new RttiClassHierarchyDescriptor(module, rva);
    }
}

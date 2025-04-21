using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Curtain.Rtti;

public sealed class RttiBaseClassArray : RttiObject, IRttiObject<RttiBaseClassArray>
{
    public ImmutableArray<RttiBaseClassDescriptor> ClassDescriptors { get; private set; }

    internal RttiBaseClassArray(RttiModule module, uint rva)
        : base(module, rva)
    {
    }

    internal bool Initialize(uint numBases)
    {
        if (!ClassDescriptors.IsDefault)
            return false;

        var reader = Module.PEFile.CreateReaderAtRva(Rva);
        var array = new RttiBaseClassDescriptor[numBases];

        for (uint i = 0; i < numBases; i++)
        {
            var descriptor = Module.GetOrAddObject<RttiBaseClassDescriptor>(reader.ReadRttiPtr(Module));

            if (descriptor == null)
                return false;

            array[i] = descriptor;
        }

        ClassDescriptors = ImmutableCollectionsMarshal.AsImmutableArray(array);
        return true;
    }

    /// <inheritdoc/>
    static RttiBaseClassArray IRttiObject<RttiBaseClassArray>.CreateInstance(RttiModule module, uint rva)
    {
        return new RttiBaseClassArray(module, rva);
    }
}

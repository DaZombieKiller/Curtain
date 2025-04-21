using System.Runtime.InteropServices;

namespace Curtain.Rtti;

public sealed partial class RttiModule
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ClassHierarchyDescriptor
    {
        public const int Size = 4 * sizeof(uint);
        public uint Signature;
        public uint Attributes;
        public uint BaseClassCount;
        public uint BaseClassArray;
    }
}

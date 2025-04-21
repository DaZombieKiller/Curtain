using AsmResolver.IO;
using System.Runtime.InteropServices;

namespace Curtain.Rtti;

public sealed partial class RttiModule
{
    [StructLayout(LayoutKind.Sequential)]
    private struct CompleteObjectLocator
    {
        public const int Size = 6 * sizeof(uint);
        public uint Signature;
        public uint Offset;
        public uint ConstructionDisplacement;
        public uint TypeDescriptor;
        public uint ClassDescriptor;
        public uint Self;

        public CompleteObjectLocator(ref BinaryStreamReader reader)
        {
            Signature = reader.ReadUInt32();
            Offset = reader.ReadUInt32();
            ConstructionDisplacement = reader.ReadUInt32();
            TypeDescriptor = reader.ReadUInt32();
            ClassDescriptor = reader.ReadUInt32();
            Self = reader.ReadUInt32();
        }

        public readonly bool IsValid(RttiModule module, uint rva)
        {
            if (Signature != module.CompleteObjectLocatorSignature)
                return false;

            if (module.Platform.Is64Bit && Self != rva)
                return false;

            uint typeDescRva = TypeDescriptor;
            uint classDescRva = ClassDescriptor;
            ulong imageBase = module.PEFile.OptionalHeader.ImageBase;

            if (module.Platform.Is32Bit)
            {
                if (typeDescRva <= imageBase)
                    return false;

                if (classDescRva <= imageBase)
                    return false;

                typeDescRva -= (uint)imageBase;
                classDescRva -= (uint)imageBase;
            }

            if (!module.PEFile.TryGetSectionContainingRva(typeDescRva, out var section))
                return false;

            if (!section.ContainsFileOffset(section.RvaToFileOffset(typeDescRva) + module.MinSizeOfTypeDescriptor))
                return false;

            if (!module.PEFile.TryGetSectionContainingRva(classDescRva, out section))
                return false;

            if (!section.ContainsFileOffset(section.RvaToFileOffset(classDescRva) + ClassHierarchyDescriptor.Size))
                return false;

            return true;
        }
    }
}

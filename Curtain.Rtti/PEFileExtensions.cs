using AsmResolver.PE.File;

namespace Curtain.Rtti;

/// <summary>Provides extensions for <see cref="PEFile"/>.</summary>
internal static class PEFileExtensions
{
    /// <summary>Converts an absolute virtual address to a relative virtual address.</summary>
    public static uint AddressToRva(this PEFile file, ulong address)
    {
        if (address == 0)
            return 0;

        return checked((uint)(address - file.OptionalHeader.ImageBase));
    }

    /// <summary>Converts a relative virtual address to an absolute virtual address.</summary>
    public static ulong RvaToAddress(this PEFile file, uint rva)
    {
        if (rva == 0)
            return 0;

        return checked(rva + file.OptionalHeader.ImageBase);
    }
}

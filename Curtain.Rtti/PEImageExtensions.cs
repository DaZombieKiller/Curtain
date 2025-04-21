using AsmResolver.PE;

namespace Curtain.Rtti;

/// <summary>Provides extensions for <see cref="PEImage"/>.</summary>
internal static class PEImageExtensions
{
    /// <summary>Converts an absolute virtual address to a relative virtual address.</summary>
    public static uint AddressToRva(this PEImage image, ulong address)
    {
        if (address == 0)
            return 0;

        return checked((uint)(address - image.ImageBase));
    }

    /// <summary>Converts a relative virtual address to an absolute virtual address.</summary>
    public static ulong RvaToAddress(this PEImage image, uint rva)
    {
        if (rva == 0)
            return 0;

        return checked(rva + image.ImageBase);
    }
}

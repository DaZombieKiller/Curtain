namespace Curtain.Rtti;

/// <summary>Base class for all RTTI-related objects.</summary>
public abstract class RttiObject
{
    /// <summary>Gets the relative virtual address of the object in the underlying PE file.</summary>
    public uint Rva { get; }

    /// <summary>Gets the <see cref="RttiModule"/> that this object belongs to.</summary>
    public RttiModule Module { get; }

    private protected RttiObject(RttiModule module, uint rva)
    {
        ArgumentNullException.ThrowIfNull(module);
        Rva = rva;
        Module = module;
        module.RegisterObject(rva, this);
    }
}

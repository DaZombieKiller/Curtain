namespace Curtain.Rtti;

/// <summary>Flags for <see cref="RttiBaseClassDescriptor"/> instances.</summary>
[Flags]
public enum RttiBaseClassFlags : uint
{
    /// <summary>No flags.</summary>
    None = 0,

    /// <summary>Base class is not publicly inherited.</summary>
    NotVisible = 1 << 0,

    /// <summary>Base class is ambiguous.</summary>
    Ambiguous = 1 << 1,

    /// <summary></summary>
    PrivateOrProtectedBase = 1 << 2,

    /// <summary></summary>
    PrivateOrProtectedInCompleteObject = 1 << 3,

    /// <summary></summary>
    VirtualBaseOfContainingObject = 1 << 4,

    /// <summary></summary>
    NonPolymorphic = 1 << 5,

    /// <summary></summary>
    HasClassHierarchy = 1 << 6,

    /// <summary>Mask of all flags that define non-public inheritance.</summary>
    PrivateOrProtectedMask = NotVisible | PrivateOrProtectedBase | PrivateOrProtectedInCompleteObject,
}

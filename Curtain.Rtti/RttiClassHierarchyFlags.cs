namespace Curtain.Rtti;

[Flags]
public enum RttiClassHierarchyFlags : uint
{
    None,

    /// <summary>The class hierarchy contains multiple inheritance.</summary>
    MultipleInheritance = 1 << 0,

    /// <summary>The class hierarchy contains virtual inheritance.</summary>
    VirtualInheritance = 1 << 1,

    /// <summary>The class hierarchy contains ambiguous bases.</summary>
    Ambiguous = 1 << 2,
}

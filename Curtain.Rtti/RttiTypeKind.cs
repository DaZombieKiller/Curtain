using System.ComponentModel;

namespace Curtain.Rtti;

/// <summary>Defines the kind of an <see cref="RttiTypeDescriptor"/>.</summary>
public enum RttiTypeKind
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    Invalid,

    /// <summary>Type is a <c>struct</c>.</summary>
    Struct,

    /// <summary>Type is a <c>class</c>.</summary>
    Class,

    /// <summary>Type is a <c>union</c>.</summary>
    Union,

    /// <summary>Type is an <c>enum</c>.</summary>
    Enum,
}

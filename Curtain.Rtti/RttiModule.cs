using AsmResolver;
using AsmResolver.IO;
using AsmResolver.PE.File;
using AsmResolver.PE.Platforms;
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Curtain.Rtti;

public sealed partial class RttiModule
{
    /// <summary>The underlying PE file.</summary>
    private readonly PEFile _file;

    /// <summary>The platform of the underlying PE file.</summary>
    private readonly Platform _platform;

    /// <summary>Mapping from RVAs to RTTI objects.</summary>
    private readonly Dictionary<uint, RttiObject> _rtti = [];

    /// <summary>The address of the <c>type_info</c> virtual function table.</summary>
    private readonly ulong _typeInfoVPtr;

    /// <summary>Gets the underlying PE file.</summary>
    public PEFile PEFile => _file;

    /// <summary>Gets the platform of the underlying PE file.</summary>
    public Platform Platform => _platform;

    /// <summary>Gets the signature value for an <see cref="RttiCompleteObjectLocator"/>.</summary>
    internal uint CompleteObjectLocatorSignature => _platform.Is32Bit ? 0u : 1u;

    /// <summary>Gets the signature value for an <see cref="RttiClassHierarchyDescriptor"/>.</summary>
    internal uint ClassHierarchySignature => 0u;

    /// <summary>Gets the minimum size of an RTTI type descriptor.</summary>
    internal uint MinSizeOfTypeDescriptor => (uint)(_platform.PointerSize * 2 + ".?ATN@@\0"u8.Length);

    /// <summary>Gets the address of the <c>type_info</c> virtual function table.</summary>
    internal ulong TypeInfoVPtr => _typeInfoVPtr;

    /// <summary>Initializes a new <see cref="RttiModule"/> instance.</summary>
    public RttiModule(string path)
        : this(PEFile.FromFile(path))
    {
    }

    /// <summary>Initializes a new <see cref="RttiModule"/> instance.</summary>
    public RttiModule(PEFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        _file = file;
        _platform = Platform.Get(file.FileHeader.Machine);

        if (!TryGetTypeInfoVTable(out _typeInfoVPtr))
            return;

        ReadRttiObjects();
    }

    /// <summary>Enumerates <see cref="RttiObject"/>s of the specified type in the module.</summary>
    public IEnumerable<T> EnumerateObjects<T>()
        where T : RttiObject
    {
        return _rtti.Values.OfType<T>();
    }

    /// <summary>Gets the address of the <c>type_info</c> virtual function table.</summary>
    private bool TryGetTypeInfoVTable(out ulong address)
    {
        foreach (PESection section in _file.Sections.Where(section => section.IsMemoryRead && !section.IsMemoryExecute))
        {
            BinaryStreamReader reader = section.CreateReader();

            // RTTI type descriptors are comprised of two pointers followed by
            // a null-terminated string containing a mangled name for the type
            // represented by the descriptor. The first pointer is the address
            // of the virtual function table for the type_info class.
            if (reader.AdvanceUntil(".?AVtype_info@@\0"u8, consumeDelimiter: false))
            {
                //
                // struct TypeDescriptor
                // {
                //     void *vftbl;
                //     void *spare;
                //     char name[]; <- You are here
                // };
                //
                reader.Offset -= (uint)_platform.PointerSize * 2;
                var rva = reader.Rva;
                address = reader.ReadNativeInt(_platform.Is32Bit);
                reader.Offset += (uint)_platform.PointerSize + 1;

                if (address < (uint)_platform.PointerSize)
                    continue;

                if (!IsValidPointer(address - (uint)_platform.PointerSize, out _))
                    continue;

                //
                // CompleteObjectLocator *pLocator; <- You are here
                // void *VirtualFunctionTable[];
                //
                ulong addressOfPtrToLocator = address - (uint)_platform.PointerSize;
                BinaryStreamReader vtReader = _file.CreateReaderAtRva(_file.AddressToRva(addressOfPtrToLocator));
                ulong pointerToLocator = vtReader.ReadNativeInt(_platform.Is32Bit);

                if (!IsValidPointer(pointerToLocator, out _))
                    continue;

                //
                // struct CompleteObjectLocator
                // {
                //     uint Signature; <- You are here
                //     uint Offset;
                //     uint ConstructionDisplacement;
                //     uint pTypeDescriptor;
                //     uint pClassDescriptor;
                // };
                //
                vtReader = _file.CreateReaderAtRva(_file.AddressToRva(pointerToLocator));
                var locator = new CompleteObjectLocator(ref vtReader);

                if (_platform.Is32Bit)
                    locator.TypeDescriptor -= (uint)_file.OptionalHeader.ImageBase;

                if (locator.TypeDescriptor != rva)
                    continue;

                GetOrAddObject<VirtualFunctionTable>(_file.AddressToRva(address));
                return true;
            }
        }

        address = 0;
        return false;
    }

    private void ReadRttiObjects()
    {
        var descriptors = new HashSet<int>();
        Span<byte> vptr = stackalloc byte[_platform.PointerSize];

        if (_platform.Is32Bit)
            BinaryPrimitives.WriteUInt32LittleEndian(vptr, (uint)_typeInfoVPtr);
        else
            BinaryPrimitives.WriteUInt64LittleEndian(vptr, _typeInfoVPtr);

        foreach (PESection section in _file.Sections.Where(section => section.IsMemoryRead && !section.IsMemoryExecute))
        {
            BinaryStreamReader reader = section.CreateReader();

            while (reader.AdvanceUntil(vptr, consumeDelimiter: false))
            {
                var rva = reader.Rva;
                reader.ReadNativeInt(_platform.Is32Bit);
                var spare = reader.ReadNativeInt(_platform.Is32Bit);

                if (spare == 0 && reader.IsNext(".?A"u8))
                {
                    uint address = rva;
                    GetOrAddObject<RttiTypeDescriptor>(rva);

                    // On x64, RTTI data pointers are stored as RVAs, instead
                    // of regular addresses. 32-bit RTTI just uses addresses.
                    if (_platform.Is32Bit)
                        address += (uint)_file.OptionalHeader.ImageBase;

                    descriptors.Add(unchecked((int)address));
                }

                reader.Rva = rva + (uint)_platform.PointerSize;
            }
        }

        // Create a new FrozenSet from the descriptor addresses, which will
        // significantly improve the search performance for large binaries.
        var frozenDescriptors = descriptors.ToFrozenSet();

        // Track object locators for later use when scanning for vtables.
        var locators = new Dictionary<ulong, RttiCompleteObjectLocator>();
        
        foreach (PESection section in _file.Sections.Where(section => section.IsMemoryRead && !section.IsMemoryExecute))
        {
            BinaryStreamReader reader = section.CreateReader();

            if (reader.TryGetRemainingSpan(out ReadOnlySpan<byte> span))
            {
                for (int i = 0; i < span.Length - CompleteObjectLocator.Size; i++)
                {
                    var col = MemoryMarshal.Read<CompleteObjectLocator>(span[i..]);

                    if (!col.IsValid(this, reader.StartRva + (uint)i))
                        continue;

                    if (!frozenDescriptors.Contains(unchecked((int)col.TypeDescriptor)))
                        continue;

                    var locator = GetOrAddObject<RttiCompleteObjectLocator>(reader.StartRva + (uint)i);
                    locators.Add(_file.RvaToAddress(locator!.Rva), locator);
                }
            }
            else
            {
                for (uint i = 0; i < reader.Length - CompleteObjectLocator.Size; i++, reader.RelativeOffset = i)
                {
                    var col = new CompleteObjectLocator(ref reader);

                    if (!col.IsValid(this, reader.StartRva + i))
                        continue;

                    if (!frozenDescriptors.Contains(unchecked((int)col.TypeDescriptor)))
                        continue;

                    var locator = GetOrAddObject<RttiCompleteObjectLocator>(reader.StartRva + i);
                    locators.Add(_file.RvaToAddress(locator!.Rva), locator);
                }
            }
        }

        FindVTables(locators.ToFrozenDictionary());
    }

    internal bool IsValidFunctionPointer(ulong pointer)
    {
        if (!IsValidPointer(pointer, out PESection? section))
            return false;

        return section.IsMemoryExecute;
    }

    internal bool IsValidPointer(ulong pointer, [NotNullWhen(true)] out PESection? section)
    {
        section = null;

        if (pointer <= _file.OptionalHeader.ImageBase)
            return false;

        if (pointer - _file.OptionalHeader.ImageBase > uint.MaxValue)
            return false;

        if (!_file.TryGetSectionContainingRva(_file.AddressToRva(pointer), out section))
            return false;

        return true;
    }

    private void FindVTables(FrozenDictionary<ulong, RttiCompleteObjectLocator> locators)
    {
        foreach (PESection section in _file.Sections.Where(section => section.IsMemoryRead && !section.IsMemoryExecute))
        {
            BinaryStreamReader reader = section.CreateReader();

            if (reader.TryGetRemainingSpan(out ReadOnlySpan<byte> span))
            {
                for (int i = 0; i < span.Length - _platform.PointerSize * 2; i++)
                {
                    var pointer = ReadNativeIntLittleEndian(span[i..]);
                    var address = reader.StartRva + (uint)i + (uint)_platform.PointerSize;

                    if (locators.TryGetValue(pointer, out RttiCompleteObjectLocator? locator) &&
                        IsValidFunctionPointer(ReadNativeIntLittleEndian(span[(i + _platform.PointerSize)..])))
                    {
                        GetOrAddObject<VirtualFunctionTable>(address);
                    }
                }
            }
            else
            {
                while (reader.CanRead((uint)_platform.PointerSize * 2))
                {
                    var pointer = reader.ReadNativeInt(_platform.Is32Bit);
                    var address = reader.Rva;

                    if (locators.TryGetValue(pointer, out RttiCompleteObjectLocator? locator) &&
                        IsValidFunctionPointer(reader.ReadNativeInt(_platform.Is32Bit)))
                    {
                        GetOrAddObject<VirtualFunctionTable>(address);
                    }
                    else
                    {
                        // Undo second pointer read
                        reader.Rva = address;
                    }
                }
            }
        }
    }

    private ulong ReadNativeIntLittleEndian(ReadOnlySpan<byte> span)
    {
        return _platform.Is32Bit ? BinaryPrimitives.ReadUInt32LittleEndian(span) : BinaryPrimitives.ReadUInt64LittleEndian(span);
    }

    internal void RegisterObject(uint rva, RttiObject obj)
    {
        _rtti.Add(rva, obj);
    }

    internal T? GetObject<T>(uint rva)
        where T : RttiObject, IRttiObject<T>
    {
        if (rva == 0)
            return null;

        _rtti.TryGetValue(rva, out var obj);
        return obj as T;
    }

    internal T? GetOrAddObject<T>(uint rva)
        where T : RttiObject, IRttiObject<T>
    {
        if (rva == 0)
            return null;

        if (_rtti.TryGetValue(rva, out var obj))
            return (T)obj;

        return T.CreateInstance(this, rva);
    }
}

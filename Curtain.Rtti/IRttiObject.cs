namespace Curtain.Rtti;

internal interface IRttiObject<T>
    where T : RttiObject, IRttiObject<T>
{
    static abstract T CreateInstance(RttiModule module, uint rva);
}

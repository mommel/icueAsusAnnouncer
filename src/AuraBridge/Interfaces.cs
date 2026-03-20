using System;
using System.Collections;
using System.Runtime.InteropServices;


// Confirmed from Interop.AuraServiceLib.dll reflection.
namespace iCueAuraBridge
{
    // ===== OFFICIAL SIGNATURES FROM Interop.AuraServiceLib.dll =====
    // DO NOT add stubs for methods not in this list — extra entries shift vtable slots.

    [ComVisible(true)]
    [Guid("ee69dbae-33ff-4e45-b378-01797a59852d")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IAuraSdk2
    {
        [DispId(1)] IAuraSyncDeviceCollection Enumerate(uint devType);
        [DispId(2)] void SwitchMode();
        [DispId(3)] void ReleaseControl(uint reserve);
    }

    [ComVisible(true)]
    [Guid("87fc56ab-99ca-4fd3-b561-2eedd719da57")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IAuraSyncDeviceCollection
    {
        [DispId(-4)] IEnumerator GetEnumerator();
        [DispId(2)]  int Count { get; }
        [DispId(0)]  IAuraSyncDevice get_Item(int index);
    }

    // IAuraSyncDevice has EXACTLY these 6 methods/properties in this vtable order.
    // SetMode/SetLightColor/Effects/StandbyEffects etc. are only in DERIVED interfaces
    // like IAuraMotherboard, IAuraKeyboard — NOT in IAuraSyncDevice itself.
    [ComVisible(true)]
    [Guid("6a30d789-f5da-4f26-bf09-6dfb9dedf91e")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IAuraSyncDevice
    {
        [DispId(7)]  IAuraRgbLightCollection Lights { get; }
        [DispId(8)]  uint Type { get; }
        [DispId(9)]  string Name { get; }
        [DispId(13)] uint Width { get; }
        [DispId(14)] uint Height { get; }
        [DispId(1)]  void Apply();
    }

    [ComVisible(true)]
    [Guid("003065bc-b562-454e-adcb-a9b70042d486")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IAuraRgbLightCollection
    {
        [DispId(-4)] IEnumerator GetEnumerator();
        [DispId(2)]  int Count { get; }
        [DispId(0)]  IAuraRgbLight get_Item(int index);
    }

    [ComVisible(true)]
    [Guid("9af6260e-4311-417d-b3ef-b85a34cf3244")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IAuraRgbLight
    {
        [DispId(1)] byte Red   { get; set; }
        [DispId(2)] byte Green { get; set; }
        [DispId(3)] byte Blue  { get; set; }
        [DispId(4)] string Name { get; }
        [DispId(5)] uint Color { get; set; }
    }
}

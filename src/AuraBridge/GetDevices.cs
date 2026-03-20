using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using OpenRGB.NET;

namespace iCueAuraBridge
{
    static class Log
    {
        public static void Write(string msg)
        {
            try
            {
                System.Diagnostics.EventLog.WriteEntry("iCueAuraBridge", msg, System.Diagnostics.EventLogEntryType.Error);
            }
            catch { }
        }
    }

    [ComVisible(true)]
    [Guid("af395d26-368e-471e-88da-6645669599eb")]
    [ClassInterface(ClassInterfaceType.None)]
    public class AuraSyncDeviceCollection : IAuraSyncDeviceCollection
    {
        private readonly List<IAuraSyncDevice> _devices = new();

        public AuraSyncDeviceCollection(IEnumerable<IAuraSyncDevice> devices)
        {
            _devices.AddRange(devices);
        }

        public int Count => _devices.Count;

        public IAuraSyncDevice get_Item(int index)
        {
            try { return _devices[index]; }
            catch (Exception ex) { Log.Write($"Collection.get_Item({index}) error: {ex.Message}"); return _devices[0]; }
        }

        public IEnumerator GetEnumerator() => _devices.GetEnumerator();
    }

    [ComVisible(true)]
    [Guid("b9ea91c7-9e90-4b95-baaf-9d66b65eb733")]
    [ClassInterface(ClassInterfaceType.None)]
    public class AuraRgbLightCollection : IAuraRgbLightCollection
    {
        private readonly List<IAuraRgbLight> _lights = new();

        public AuraRgbLightCollection(int count)
        {
            for (int i = 0; i < count; i++) _lights.Add(new AuraRgbLight($"LED {i}"));
        }

        public int Count => _lights.Count;

        public IAuraRgbLight get_Item(int index)
        {
            try { return _lights[index]; }
            catch (Exception ex) { Log.Write($"LightCollection.get_Item({index}) error: {ex.Message}"); return _lights[0]; }
        }

        public IEnumerator GetEnumerator() => _lights.GetEnumerator();
    }

    [ComVisible(true)]
    [Guid("72cddd80-b274-463c-bc16-c2522ce6ecd4")]
    [ClassInterface(ClassInterfaceType.None)]
    public class AuraRgbLight : IAuraRgbLight
    {
        public AuraRgbLight(string name) { Name = name; }
        public byte Red   { get; set; }
        public byte Green { get; set; }
        public byte Blue  { get; set; }
        public string Name { get; }
        public uint Color
        {
            get => (uint)((Red << 16) | (Green << 8) | Blue);
            set { Red = (byte)((value >> 16) & 0xFF); Green = (byte)((value >> 8) & 0xFF); Blue = (byte)(value & 0xFF); }
        }
    }

    // Minimal motherboard — required by iCUE to activate the ASUS plugin layout.
    // IAuraSyncDevice vtable order confirmed from Interop.AuraServiceLib.dll (6 methods only).
    [ComVisible(true)]
    [Guid("2d9532b7-4aac-449a-82e9-5d4a11961ff8")]
    [ClassInterface(ClassInterfaceType.None)]
    public class AuraMotherboard : IAuraSyncDevice
    {
        public AuraMotherboard()
        {
            Lights = new AuraRgbLightCollection(1);
        }

        public IAuraRgbLightCollection Lights { get; }
        public uint Type   => 0x10000; // MB_RGB
        public string Name => "ASUS ROG Motherboard";
        public uint Width  => 1;
        public uint Height => 1;
        public void Apply() { }
    }

    // The GPU device that forwards iCUE color commands to OpenRGB.
    // Name is taken from OpenRGB so it displays the real device name in iCUE.
    [ComVisible(true)]
    [Guid("8d802f33-fa46-496a-9aa5-a34a805783ca")]
    [ClassInterface(ClassInterfaceType.None)]
    public class AuraGPU : IAuraSyncDevice
    {
        private readonly OpenRgbClient? _openRgbClient;
        private readonly Device? _openRgbGpu;
        private readonly string _name;
        private readonly bool _useAuraBridge;
        private int _applyCount = 0;

        public AuraGPU(string name, int ledCount, OpenRgbClient? openRgbClient, Device? openRgbGpu, bool useAuraBridge)
        {
            _name          = name;
            _openRgbClient = openRgbClient;
            _openRgbGpu    = openRgbGpu;
            _useAuraBridge = useAuraBridge;
            Lights         = new AuraRgbLightCollection(ledCount);
        }

        public IAuraRgbLightCollection Lights { get; }
        public uint Type   => 0x20000; // VGA_RGB
        public string Name => _name;
        public uint Width  => (uint)Lights.Count;
        public uint Height => 1;

        public void Apply()
        {
            try
            {
                _applyCount++;
                if (_useAuraBridge)
                {
                    for (int i = 0; i < Lights.Count; i++)
                    {
                        var l = Lights.get_Item(i);
                        iCueAuraBridge.SetColor(i, l.Red, l.Green, l.Blue);
                    }
                    iCueAuraBridge.Apply();

                    if (_applyCount % 250 == 0)
                        Log.Write($"GPU Apply() heartbeat: {_applyCount} frames sent to AuraGpuBridge.");
                }
                else if (_openRgbClient != null && _openRgbGpu != null)
                {
                    var colors = new OpenRGB.NET.Color[_openRgbGpu.Colors.Length];
                    for (int i = 0; i < Lights.Count && i < colors.Length; i++)
                    {
                        var l = Lights.get_Item(i);
                        colors[i] = new OpenRGB.NET.Color(l.Red, l.Green, l.Blue);
                    }
                    _openRgbClient.UpdateLeds(_openRgbGpu.Index, colors);

                    if (_applyCount % 250 == 0)
                        Log.Write($"GPU Apply() heartbeat: {_applyCount} frames sent to OpenRGB.");
                }
            }
            catch (Exception ex) { Log.Write($"GPU Apply() error: {ex.Message}"); }
        }
    }
}

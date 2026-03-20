using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using OpenRGB.NET;

namespace iCueAuraBridge
{
    [ComVisible(true)]
    [Guid("05921124-5057-483e-a037-e9497b523590")] // CoClass GUID — must match what asus_plugin.dll looks up
    [ClassInterface(ClassInterfaceType.None)]
    public class AuraSdkClass : IAuraSdk2
    {
        private AuraMotherboard? _auraMotherboard;
        private AuraGPU? _auraGpu;
        private OpenRgbClient? _openRgbClient;
        private Device? _openRgbGpu;
        
        private void Log(string msg)
        {
            try
            {
                System.Diagnostics.EventLog.WriteEntry("iCueAuraBridge", msg, System.Diagnostics.EventLogEntryType.Information);
            }
            catch { }
        }

        public AuraSdkClass()
        {
            Log("iCueAuraBridge starting...");
            _auraMotherboard = new AuraMotherboard();

            int ledCount = 24;
            string gpuName = "ASUS ROG GPU";
            bool useAuraBridge = false;

            try
            {
                int bridgeCount = iCueAuraBridge.Connect();
                if (bridgeCount > 0)
                {
                    useAuraBridge = true;
                    ledCount = bridgeCount;
                    
                    string bridgeName = iCueAuraBridge.GetName().Trim();
                    if (!string.IsNullOrEmpty(bridgeName))
                    {
                        gpuName = "ASUS " + bridgeName.Replace("_", " ");
                    }
                    else
                    {
                        gpuName = "ASUS ROG GPU (NVAPI)";
                    }

                    Log($"AuraGpuBridge Connected directly to ENE GPU via NVAPI! ({ledCount} LEDs, Name: {gpuName})");
                }
            }
            catch (Exception ex)
            {
                Log($"AuraGpuBridge failed: {ex.Message}");
            }

            if (!useAuraBridge)
            {
                try
                {
                    _openRgbClient = new OpenRgbClient(name: "iCUE-AuraBridge");
                    _openRgbClient.Connect();
                    var devices = _openRgbClient.GetAllControllerData();
                    foreach (var d in devices)
                    {
                        Log($"Found Device: {d.Name} ({d.Type}) {d.Leds.Length} LEDs");
                        // Skip Corsair devices to prevent feedback loops with iCUE
                        if (d.Name.Contains("Corsair", StringComparison.OrdinalIgnoreCase)) continue;

                        if (d.Type == DeviceType.Gpu)
                        {
                            _openRgbGpu = d;
                            ledCount    = d.Leds.Length;
                            gpuName     = d.Name; // Use the real device name so it shows correctly in iCUE
                            Log($"Found GPU: {gpuName} ({ledCount} LEDs)");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"OpenRGB connect failed (GPU will use defaults): {ex.Message}");
                }
            }

            _auraGpu = new AuraGPU(gpuName, ledCount, _openRgbClient, _openRgbGpu, useAuraBridge);
            Log($"Ready. Motherboard + GPU ({gpuName}) registered.");
        }

        public IAuraSyncDeviceCollection Enumerate(uint devType)
        {
            var devices = new List<IAuraSyncDevice>();

            if ((devType & 0x10000) != 0 || devType == 0)  // MB_RGB
                devices.Add(_auraMotherboard!);

            if ((devType & 0x20000) != 0 || devType == 0)  // VGA_RGB
                devices.Add(_auraGpu!);

            Log($"Enumerate(0x{devType:X}) -> {devices.Count} devices");
            return new AuraSyncDeviceCollection(devices);
        }

        public void SwitchMode()
        {
            Log("SwitchMode called — iCUE taking control.");
        }

        public void ReleaseControl(uint reserve)
        {
            Log("ReleaseControl called.");
        }
    }
}

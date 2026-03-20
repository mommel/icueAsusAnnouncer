using System;
using System.Runtime.InteropServices;

namespace iCueAuraBridge
{
    public static class iCueAuraBridge
    {
        // Dynamically load the correct architecture DLL
        private const string DllName64 = "AuraGpuBridge64.dll";
        private const string DllName32 = "AuraGpuBridge32.dll";

        [DllImport(DllName64, EntryPoint = "AuraGpu_Connect", CallingConvention = CallingConvention.Cdecl)]
        private static extern int AuraGpu_Connect64();

        [DllImport(DllName32, EntryPoint = "AuraGpu_Connect", CallingConvention = CallingConvention.Cdecl)]
        private static extern int AuraGpu_Connect32();

        [DllImport(DllName64, EntryPoint = "AuraGpu_SetColor", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AuraGpu_SetColor64(int zone, byte r, byte g, byte b);

        [DllImport(DllName32, EntryPoint = "AuraGpu_SetColor", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AuraGpu_SetColor32(int zone, byte r, byte g, byte b);

        [DllImport(DllName64, EntryPoint = "AuraGpu_Apply", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AuraGpu_Apply64();

        [DllImport(DllName32, EntryPoint = "AuraGpu_Apply", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AuraGpu_Apply32();

        [DllImport(DllName64, EntryPoint = "AuraGpu_GetName", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void AuraGpu_GetName64(System.Text.StringBuilder outName, int maxLen);

        [DllImport(DllName32, EntryPoint = "AuraGpu_GetName", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void AuraGpu_GetName32(System.Text.StringBuilder outName, int maxLen);

        public static int Connect()
        {
            return Environment.Is64BitProcess ? AuraGpu_Connect64() : AuraGpu_Connect32();
        }

        public static string GetName()
        {
            var sb = new System.Text.StringBuilder(32);
            if (Environment.Is64BitProcess) AuraGpu_GetName64(sb, sb.Capacity);
            else AuraGpu_GetName32(sb, sb.Capacity);
            return sb.ToString();
        }

        public static void SetColor(int zone, byte r, byte g, byte b)
        {
            if (Environment.Is64BitProcess) AuraGpu_SetColor64(zone, r, g, b);
            else AuraGpu_SetColor32(zone, r, g, b);
        }

        public static void Apply()
        {
            if (Environment.Is64BitProcess) AuraGpu_Apply64();
            else AuraGpu_Apply32();
        }
    }
}

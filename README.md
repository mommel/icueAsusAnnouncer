# iCUE ASUS Aura Bridge

A standalone bridge that natively allows Corsair iCUE to communicate with your ASUS GPU's RGB controller, completely removing the dependency on ASUS Armoury Crate.

## Description

The *iCUE ASUS Aura Bridge* acts as a C# COM server mock, impersonating the native `IAuraSdk2` and `IAuraSyncDevice` interfaces expected by Corsair iCUE's `asus_plugin.dll` plugin. Instead of relying on Armoury Crate processes, this custom server intercepts iCUE's lighting commands.

It utilizes a lightweight, standalone C++ DLL (`AuraGpuBridge.dll`) capable of securely routing RGB commands directly to supported ENE SMBus GPU controllers using NVIDIA's NVAPI (completely avoiding ring0/driver access requirements).

### Key Features
* **No Armoury Crate dependency**: Drastically reduces background process bloat and installation issues.
* **No kernel-level driver required**: Leverages native, approved space via NVAPI.
* **Auto-Discovery**: Extracts exact GPU model names and dynamically adjusts layout counts through iCUE.
* **Self-Contained Installer**: Fully automated installer gracefully terminates iCUE, drops the DLL payload, handles dual-architecture Windows COM registration (x64 and WoW6432), and restarts iCUE.
* **Windows Event Logging**: Seamlessly logs bridge activity, diagnostics, and component status directly into the Windows Event Viewer under the `iCueAuraBridge` event source.
* **OpenRGB Integration**: Automatically connects and falls back to an active OpenRGB server to control devices if the direct NVAPI bridge is incompatible or unavailable.

## Installation

We provide an automated installation package using **Inno Setup**. 
Just download the release and run `iCueAuraBridge_Installer.exe`.

The installer automatically handles:
1. Identifying if you are running x86/x64 plugin modes.
2. COM Server registry injection for the precise AuraSDK GUID.
3. Automatically bringing iCUE down and up upon install.

To uninstall, use standard Windows **Add / Remove Programs** under `iCUE ASUS Aura Bridge`.

## Development & Building from Source

To build from source directly, open a PowerShell elevated terminal in the repository root and simply execute:
```powershell
.\CreateRelease.ps1
```

The script automatically:
1. Prompts for and installs **Inno Setup 6** if missing via `winget`.
2. Compiles the C++ Native NVAPI Bridges for both architectures (`bin\x64` & `bin\x86`) using local MSVC build tools.
3. Publishes the C# COM server targeting both frameworks natively.
4. Wraps everything into a distributed `.exe` in the `Release\` output folder.

## Architecture

- **`src/AuraGpuBridge` (C++)**: Leverages NVIDIA NVAPI `pNvAPI_I2CWriteEx` commands interacting directly over the ENE SMBus registers for instantaneous RGB applying on ASUS GPUs.
- **`src/AuraBridge` (C#)**: An exposed `ComVisible` `.NET 8` assembly configured with `[ClassInterface(ClassInterfaceType.None)]`. Handles explicit custom COM vtable mappings (`IAuraSyncDeviceCollection` / `IAuraSyncDevice`) matching byte-for-byte with the official API so iCUE recognizes it seamlessly.

## Credits

* **[OpenRGB](https://gitlab.com/CalcProgrammer1/OpenRGB)** (GPLv2): Special thanks to CalcProgrammer1 and contributors. OpenRGB is integrated as a fallback option to ensure extensive hardware compatibility. Their exceptional open-source reverse-engineering efforts provide the foundational RGB control standard.
* **[OpenRGB.NET](https://github.com/diogotr7/OpenRGB.NET)** (MIT License): The C# client library utilized by this project to interface with the OpenRGB SDK server.
* **[Inno Setup](https://jrsoftware.org/isinfo.php)** (zlib/libpng License): The automated installation executable is packaged and built using Inno Setup by Jordan Russell and Martijn Laan.

*Licensing Note: Because this project exclusively communicates with the OpenRGB software over a standard TCP network socket using the MIT-licensed `OpenRGB.NET` wrapper, `iCueAuraBridge` operates as a wholly separate program and is therefore legally isolated from the GPLv2 copyleft requirements of OpenRGB itself.*

## Disclaimer

**ABSOLUTELY NO WARRANTY.** This software is provided "AS IS" and is strictly a **proof of concept**. It intercepts hardware communication and directly writes to system management buses. By using this software, you acknowledge that you do so entirely at your own risk. The authors and contributors are not responsible for any damage to your hardware, operating system, or any other issues that may arise from its use.

### Trademarks & Affiliation
This project is an unofficial, community-driven tool. **It is not affiliated with, endorsed by, or sponsored by ASUS, Corsair, or NVIDIA in any way.** 
"ASUS", "Aura Sync", "ROG", "Corsair", "iCUE", "NVIDIA", and "NVAPI" are trademarks or registered trademarks of their respective owners. All other trademarks, product names, and company names or logos cited herein are the property of their respective owners.

[Setup]
AppName=iCUE ASUS Aura Bridge
AppVersion=1.0
AppPublisher=iCUE ASUS Announcer
DefaultDirName={commonappdata}\iCueAuraBridge
DisableDirPage=yes
DefaultGroupName=iCUE ASUS Aura Bridge
DisableProgramGroupPage=yes
OutputBaseFilename=iCueAuraBridge_Installer
OutputDir=Release
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
SetupLogging=yes

[Files]
Source: "Release\iCueAuraBridge\x64\*"; DestDir: "{app}\x64"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Release\iCueAuraBridge\x86\*"; DestDir: "{app}\x86"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
; Native 64-bit registration
Root: HKLM; Subkey: "SOFTWARE\Classes\CLSID\{{05921124-5057-483e-a037-e9497b523590}"; ValueType: string; ValueName: ""; ValueData: "AuraSdkClass"; Flags: uninsdeletekey; Check: Is64BitInstallMode
Root: HKLM; Subkey: "SOFTWARE\Classes\CLSID\{{05921124-5057-483e-a037-e9497b523590}\InprocServer32"; ValueType: string; ValueName: ""; ValueData: "{app}\x64\iCueAuraBridge.comhost.dll"; Flags: uninsdeletekey; Check: Is64BitInstallMode
Root: HKLM; Subkey: "SOFTWARE\Classes\CLSID\{{05921124-5057-483e-a037-e9497b523590}\InprocServer32"; ValueType: string; ValueName: "ThreadingModel"; ValueData: "Both"; Flags: uninsdeletekey; Check: Is64BitInstallMode

; 32-bit WOW6432Node registration
Root: HKLM; Subkey: "SOFTWARE\Classes\WOW6432Node\CLSID\{{05921124-5057-483e-a037-e9497b523590}"; ValueType: string; ValueName: ""; ValueData: "AuraSdkClass"; Flags: uninsdeletekey; Check: Is64BitInstallMode
Root: HKLM; Subkey: "SOFTWARE\Classes\WOW6432Node\CLSID\{{05921124-5057-483e-a037-e9497b523590}\InprocServer32"; ValueType: string; ValueName: ""; ValueData: "{app}\x86\iCueAuraBridge.comhost.dll"; Flags: uninsdeletekey; Check: Is64BitInstallMode
Root: HKLM; Subkey: "SOFTWARE\Classes\WOW6432Node\CLSID\{{05921124-5057-483e-a037-e9497b523590}\InprocServer32"; ValueType: string; ValueName: "ThreadingModel"; ValueData: "Both"; Flags: uninsdeletekey; Check: Is64BitInstallMode

[Run]
Filename: "sc.exe"; Parameters: "start Corsair"; Flags: runhidden; Description: "Starting Corsair iCUE Service"
Filename: "C:\Program Files\Corsair\Corsair iCUE5 Software\iCUE.exe"; Flags: nowait postinstall; Description: "Launch iCUE"

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop Corsair"; Flags: runhidden; RunOnceId: "StopCorsair"

[Code]
procedure InitializeWizard;
var
  ResultCode: Integer;
begin
  // Stop iCUE and Corsair Service before installing to unlock any files
  Exec('taskkill.exe', '/F /IM iCUE.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('sc.exe', 'stop Corsair', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(2000); // Give the service a moment to fully stop
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    Exec('taskkill.exe', '/F /IM iCUE.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('sc.exe', 'stop Corsair', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(2000);
  end;
  if CurUninstallStep = usPostUninstall then
  begin
    Exec('sc.exe', 'start Corsair', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;

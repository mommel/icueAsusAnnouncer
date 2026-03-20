$ErrorActionPreference = "Stop"

$vcVars64 = "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat"
$vcVars32 = "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars32.bat"

if (-Not (Test-Path $vcVars64)) {
    Write-Error "Could not find vcvars64.bat"
    exit 1
}

# Ensure bin directories exist
$bin64 = "$PSScriptRoot\src\AuraGpuBridge\bin\x64"
$bin32 = "$PSScriptRoot\src\AuraGpuBridge\bin\x86"
$srcFile = "$PSScriptRoot\src\AuraGpuBridge\AuraGpuBridge.cpp"

if (-Not (Test-Path $bin64)) { New-Item -ItemType Directory -Force -Path $bin64 | Out-Null }
if (-Not (Test-Path $bin32)) { New-Item -ItemType Directory -Force -Path $bin32 | Out-Null }

Write-Host "Building AuraGpuBridge (x64)..."
$buildCmd64 = @"
call "$vcVars64"
cd /d "$bin64"
cl.exe /nologo /O2 /LD /EHsc /MD "$srcFile" /link user32.lib /OUT:AuraGpuBridge64.dll
del *.obj *.lib *.exp
"@
Set-Content -Path "$PSScriptRoot\compile64.bat" -Value $buildCmd64
cmd.exe /c "$PSScriptRoot\compile64.bat"
Remove-Item "$PSScriptRoot\compile64.bat" -Force

Write-Host "Building AuraGpuBridge (x86)..."
$buildCmd32 = @"
call "$vcVars32"
cd /d "$bin32"
cl.exe /nologo /O2 /LD /EHsc /MD "$srcFile" /link user32.lib /OUT:AuraGpuBridge32.dll
del *.obj *.lib *.exp
"@
Set-Content -Path "$PSScriptRoot\compile32.bat" -Value $buildCmd32
cmd.exe /c "$PSScriptRoot\compile32.bat"
Remove-Item "$PSScriptRoot\compile32.bat" -Force

Write-Host "AuraGpuBridge build complete!"

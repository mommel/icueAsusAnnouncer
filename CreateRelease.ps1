$ErrorActionPreference = "Stop"
$projectDir = $PSScriptRoot

$iscc = $null
$commandPath = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if ($commandPath) {
    if ($commandPath -is [System.Array]) {
        $iscc = $commandPath[0].Source
    } else {
        $iscc = $commandPath.Source
    }
}

if (-not $iscc) {
    $uninstallKeys = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup*",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup*",
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup*"
    )
    foreach ($key in $uninstallKeys) {
        $installLocation = Get-ItemProperty $key -ErrorAction SilentlyContinue | Select-Object -ExpandProperty InstallLocation -ErrorAction SilentlyContinue
        if ($installLocation) {
            # In case multiple paths are returned, take the first one
            if ($installLocation -is [System.Array]) {
                $installLocation = $installLocation[0]
            }
            $testPath = Join-Path $installLocation "ISCC.exe"
            if (Test-Path $testPath) {
                $iscc = $testPath
                break
            }
        }
    }
}

if (-not $iscc) {
    Write-Error "Inno Setup 6 (ISCC.exe) was not found."
    Write-Error "Inno Setup is required to build the .exe installer. Assuming manual ZIP release only..."
    Write-Information "Maybe you want to use"
    Write-Information "winget install JRSoftware.InnoSetup"
    Write-Information "to install it."
    exit 1
}

Write-Host "======================================"
Write-Host "  Building iCueAuraBridge Release     "
Write-Host "======================================"

$releaseDir = "$projectDir\Release\iCueAuraBridge"

if (Test-Path "$projectDir\Release") {
    Remove-Item "$projectDir\Release" -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

$gpuBridgeBuildDir = "$projectDir\src\AuraGpuBridge\bin"

if (Test-Path "$gpuBridgeBuildDir") {
    Remove-Item "$gpuBridgeBuildDir" -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $gpuBridgeBuildDir | Out-Null

Write-Host "[1/3] Building C++ Native Bridge (x64 and x86)..."

Set-Location $projectDir
& powershell -ExecutionPolicy Bypass -File .\buildAuraGpuBridge.ps1

Write-Host "[2/3] Publishing C# COM Server..."
dotnet publish "$projectDir\src\AuraBridge\iCueAuraBridge.csproj" -r win-x64 -c Release -o "$releaseDir\x64" | Out-Null
dotnet publish "$projectDir\src\AuraBridge\iCueAuraBridge.csproj" -r win-x86 -c Release -o "$releaseDir\x86" | Out-Null

Write-Host "[3/3] Compiling Inno Setup Installer..."

if ($iscc) {
    Write-Host "Found Inno Setup at: $iscc"
    & "$iscc" "$projectDir\installer.iss"
    Write-Host "======================================"
    Write-Host " Release .exe successfully created in "
    Write-Host " $projectDir\Release\iCueAuraBridge_Installer.exe"
    Write-Host "======================================"
} else {
    Write-Warning "Inno Setup (ISCC.exe) not found!"
    Write-Warning "Could not compile the .exe installer. Please install Inno Setup 6:"
    Write-Warning "https://jrsoftware.org/isdl.php"
    Write-Warning ""
    Write-Warning "Alternatively, you can just distribute the Release folder manually."
}

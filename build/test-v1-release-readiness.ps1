param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$OutputPath = "src\Kinovea\bin\x64\Release",
    [string]$PortableZip = "",
    [string]$InstallerPath = "",
    [switch]$SmokeTestInstaller
)

$ErrorActionPreference = 'Stop'

function Assert-True([bool]$condition, [string]$message) {
    if (!$condition) { throw $message }
}

function Assert-Contains([string]$path, [string]$pattern, [string]$message) {
    Assert-True (Test-Path $path) "Release source file was not found: $path"
    $content = Get-Content $path -Raw
    if ($content -notmatch $pattern) { throw $message }
}

Assert-True ($Version -match '^\d+\.\d+\.\d+$') "Release version must use major.minor.patch format. Received: $Version"
$escapedVersion = [regex]::Escape($Version)

Assert-Contains ".github\workflows\build.yml" "VERSION:\s*$escapedVersion(?:\s|$)" "GitHub Actions VERSION does not match $Version."
Assert-Contains "src\Kinovea\Properties\AssemblyInfo.cs" ('AssemblyVersion\("' + $escapedVersion + '\.0"\)') "AssemblyVersion does not match $Version."
Assert-Contains "src\Kinovea\Properties\AssemblyInfo.cs" ('AssemblyFileVersion\("' + $escapedVersion + '\.0"\)') "AssemblyFileVersion does not match $Version."
Assert-Contains "src\Kinovea\Properties\AssemblyInfo.cs" ('AssemblyInformationalVersion\("' + $escapedVersion + '\+kinovea\.') "AssemblyInformationalVersion does not match $Version."
Assert-Contains "src\Kinovea\Workspace\FitSessionReportGenerator.cs" ('ReportVersion\s*=\s*"' + $escapedVersion + '"') "Report generator version does not match $Version."
Assert-Contains "src\Installer\kinovea.nsi" ('!define VERSION "' + $escapedVersion + '"') "Installer fallback version does not match $Version."
Assert-Contains "README.md" ('Current milestone:\s*' + $escapedVersion) "README current milestone does not match $Version."

Assert-True (Test-Path $OutputPath) "Release output folder was not found: $OutputPath"
$exe = Join-Path $OutputPath "CassetteMotionPro.exe"
Assert-True (Test-Path $exe) "CassetteMotionPro.exe was not found in the release output."
$versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Resolve-Path $exe).Path)
Assert-True ($versionInfo.FileVersion -eq "$Version.0") "CassetteMotionPro.exe file version was '$($versionInfo.FileVersion)', expected '$Version.0'."

$requiredFiles = @(
    "CassetteMotionPro.exe",
    "Kinovea.Camera.dll",
    "Kinovea.Camera.DirectShow.dll",
    "Kinovea.Video.dll",
    "Kinovea.Video.FFMpeg.dll"
)
foreach ($file in $requiredFiles) {
    Assert-True (Test-Path (Join-Path $OutputPath $file)) "Required release file is missing: $file"
}

$drawingTools = Join-Path $OutputPath "DrawingTools"
Assert-True (Test-Path $drawingTools) "DrawingTools folder is missing from the release output."
Assert-True (@(Get-ChildItem $drawingTools -File -Recurse -ErrorAction SilentlyContinue).Count -gt 0) "DrawingTools folder is empty."

if (![string]::IsNullOrWhiteSpace($PortableZip)) {
    Assert-True (Test-Path $PortableZip) "Portable ZIP was not found: $PortableZip"
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $PortableZip).Path)
    try {
        $entryNames = @($archive.Entries | ForEach-Object { $_.FullName.Replace('/', '\') })
        Assert-True ($entryNames -contains "CassetteMotionPro.exe") "Portable ZIP does not contain CassetteMotionPro.exe."
        Assert-True (@($entryNames | Where-Object { $_ -like "DrawingTools\*" }).Count -gt 0) "Portable ZIP does not contain DrawingTools."
        Assert-True (@($entryNames | Where-Object { $_ -like "AppData*" }).Count -gt 0) "Portable ZIP does not contain the portable AppData marker."
    }
    finally {
        $archive.Dispose()
    }
}

if (![string]::IsNullOrWhiteSpace($InstallerPath)) {
    Assert-True (Test-Path $InstallerPath) "Installer was expected but not found: $InstallerPath"
    $installer = Get-Item $InstallerPath
    Assert-True ($installer.Length -gt 1MB) "Installer is unexpectedly small: $($installer.Length) bytes."
    $installerVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($installer.FullName)
    Assert-True ($installerVersion.FileVersion -like "$Version*") "Installer file version was '$($installerVersion.FileVersion)', expected '$Version'."
    Assert-True ($installerVersion.ProductVersion -like "$Version*") "Installer product version was '$($installerVersion.ProductVersion)' and file version was '$($installerVersion.FileVersion)', expected '$Version'."

    if ($SmokeTestInstaller) {
        $smokeRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("CassetteMotionPro-Install-Smoke-" + [guid]::NewGuid().ToString('N'))
        try {
            $install = Start-Process -FilePath $installer.FullName -ArgumentList @('/S', "/D=$smokeRoot") -Wait -PassThru
            Assert-True ($install.ExitCode -eq 0) "Silent installer smoke test failed with exit code $($install.ExitCode)."
            Assert-True (Test-Path (Join-Path $smokeRoot "CassetteMotionPro.exe")) "Installed application executable was not found."
            Assert-True (Test-Path (Join-Path $smokeRoot "Kinovea.Video.FFMpeg.dll")) "Installed FFmpeg playback component was not found."
            Assert-True (@(Get-ChildItem (Join-Path $smokeRoot "DrawingTools") -File -Recurse -ErrorAction SilentlyContinue).Count -gt 0) "Installed DrawingTools folder is missing or empty."

            $uninstaller = Join-Path $smokeRoot "Uninstall-CassetteMotionPro.exe"
            Assert-True (Test-Path $uninstaller) "Installed uninstaller was not found."
            $uninstall = Start-Process -FilePath $uninstaller -ArgumentList @('/S', "_?=$smokeRoot") -Wait -PassThru
            Assert-True ($uninstall.ExitCode -eq 0) "Silent uninstaller smoke test failed with exit code $($uninstall.ExitCode)."
            Start-Sleep -Seconds 1
            Assert-True (!(Test-Path (Join-Path $smokeRoot "CassetteMotionPro.exe"))) "Silent uninstaller left the main executable installed."
        }
        finally {
            if (Test-Path $smokeRoot) { Remove-Item $smokeRoot -Recurse -Force -ErrorAction SilentlyContinue }
        }
    }
}

Write-Host "PASS: v$Version source versions, executable metadata, required runtime files, drawing tools, portable ZIP, and available installer artifacts are release-consistent."

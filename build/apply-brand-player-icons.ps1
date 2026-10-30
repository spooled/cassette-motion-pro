# Overlay the Cassette Motion Pro player and tool icons onto the upstream
# Kinovea.ScreenManager resources. Each PNG replaces the upstream file of the same
# name, so the resx references and pixel sizes stay valid.
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"
$source = Join-Path $RepoRoot "branding\player-icons"
$target = Join-Path $RepoRoot "src\Kinovea.ScreenManager\Resources"

if (!(Test-Path $source)) { throw "Brand icon folder not found: $source" }
if (!(Test-Path $target)) { throw "Upstream ScreenManager resources not found: $target" }

Add-Type -AssemblyName System.Drawing
$applied = 0
foreach ($icon in Get-ChildItem $source -Filter *.png) {
    $existing = Join-Path $target $icon.Name
    if (!(Test-Path $existing)) { throw "Upstream has no icon named $($icon.Name); upstream may have changed." }

    $old = [System.Drawing.Image]::FromFile($existing)
    $new = [System.Drawing.Image]::FromFile($icon.FullName)
    try {
        if ($old.Width -ne $new.Width -or $old.Height -ne $new.Height) {
            throw "Size mismatch for $($icon.Name): upstream $($old.Width)x$($old.Height), brand $($new.Width)x$($new.Height)."
        }
    }
    finally {
        $old.Dispose()
        $new.Dispose()
    }
    Copy-Item $icon.FullName $existing -Force
    $applied++
}
Write-Host "Applied $applied brand player icons."

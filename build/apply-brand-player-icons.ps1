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

# Drawing-tool icons (angle, line, pencil, ...) are embedded as base64 inside
# Drawings.resx rather than stored as PNG files, so rewrite those entries.
$drawingSource = Join-Path $RepoRoot "branding\drawing-icons"
$resx = Join-Path $RepoRoot "src\Kinovea.ScreenManager\Properties\Drawings.resx"
if (!(Test-Path $drawingSource)) { throw "Brand drawing icon folder not found: $drawingSource" }
if (!(Test-Path $resx)) { throw "Upstream Drawings.resx not found: $resx" }

$bytes = [System.IO.File]::ReadAllBytes($resx)
$hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
$text = [System.IO.File]::ReadAllText($resx)
$patched = 0
foreach ($icon in Get-ChildItem $drawingSource -Filter *.png) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($icon.Name)
    $pattern = '(?s)(<data name="' + [regex]::Escape($name) + '"[^>]*>\s*<value>)(.*?)(</value>)'
    $match = [regex]::Match($text, $pattern)
    if (!$match.Success) { throw "Drawings.resx has no icon named $name; upstream may have changed." }

    $oldBytes = [Convert]::FromBase64String(($match.Groups[2].Value -replace '\s', ''))
    $oldStream = New-Object System.IO.MemoryStream(,$oldBytes)
    $old = [System.Drawing.Image]::FromStream($oldStream)
    $new = [System.Drawing.Image]::FromFile($icon.FullName)
    try {
        if ($old.Width -ne $new.Width -or $old.Height -ne $new.Height) {
            throw "Size mismatch for embedded $name`: upstream $($old.Width)x$($old.Height), brand $($new.Width)x$($new.Height)."
        }
    }
    finally {
        $old.Dispose(); $oldStream.Dispose(); $new.Dispose()
    }

    $base64 = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($icon.FullName), [Base64FormattingOptions]::InsertLineBreaks)
    $lines = ($base64 -split "\r?\n") | ForEach-Object { "        $_" }
    $replacement = "`r`n" + ($lines -join "`r`n") + "`r`n    "
    $text = $text.Substring(0, $match.Groups[2].Index) + $replacement + $text.Substring($match.Groups[2].Index + $match.Groups[2].Length)
    $patched++
}
[System.IO.File]::WriteAllText($resx, $text, (New-Object System.Text.UTF8Encoding($hasBom)))
Write-Host "Replaced $patched embedded drawing icons in Drawings.resx."

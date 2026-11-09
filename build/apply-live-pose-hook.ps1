# Plug the live joint overlay into the upstream Kinovea live capture view.
# Adds LivePoseHook.cs to Kinovea.ScreenManager and two one-line calls. Fails loudly if upstream changed.
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"
$sm = Join-Path $RepoRoot "src\Kinovea.ScreenManager"
if (!(Test-Path $sm)) { throw "Upstream ScreenManager not found: $sm" }

function Edit-File([string]$path, [string]$anchor, [string]$replacement) {
    $text = [System.IO.File]::ReadAllText($path)
    $first = $text.IndexOf($anchor)
    if ($first -lt 0) { throw "Anchor not found in ${path}: $anchor" }
    if ($text.IndexOf($anchor, $first + 1) -ge 0) { throw "Anchor is not unique in ${path}: $anchor" }
    $text = $text.Substring(0, $first) + $replacement + $text.Substring($first + $anchor.Length)
    [System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($true)))
}

Copy-Item (Join-Path $PSScriptRoot "live-pose\LivePoseHook.cs") (Join-Path $sm "CaptureScreen\LivePoseHook.cs") -Force

Edit-File (Join-Path $sm "Kinovea.ScreenManager.csproj") `
    '<Compile Include="CaptureScreen\CaptureScreen.cs" />' `
    ('<Compile Include="CaptureScreen\LivePoseHook.cs" />' + "`r`n" + '    <Compile Include="CaptureScreen\CaptureScreen.cs" />')

Edit-File (Join-Path $sm "CaptureScreen\CaptureScreen.cs") `
    'viewportController.Bitmap = displayFrame;' `
    ('viewportController.Bitmap = displayFrame;' + "`r`n" + '                LivePoseHook.Submit(displayFrame);')

Edit-File (Join-Path $sm "CaptureScreen\Views\ViewPort.cs") `
    'DrawKVA(e.Graphics);' `
    ('DrawKVA(e.Graphics);' + "`r`n" + '            LivePoseHook.Draw(e.Graphics, displayRectangle, controller.Bitmap.Size);')

Write-Host "Live pose hook applied."

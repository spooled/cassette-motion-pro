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
    ('viewportController.Bitmap = displayFrame;' + "`r`n" + '                LivePoseHook.Submit(viewportController, displayFrame);')

Edit-File (Join-Path $sm "CaptureScreen\Views\ViewPort.cs") `
    'DrawKVA(e.Graphics);' `
    ('DrawKVA(e.Graphics);' + "`r`n" + '            LivePoseHook.Draw(controller, e.Graphics, displayRectangle, controller.Bitmap.Size);')

Edit-File (Join-Path $sm "CaptureScreen\Views\ViewPort.cs") `
    'base.OnMouseDown(e);' `
    ('base.OnMouseDown(e);' + "`r`n" + '            if (LivePoseHook.PointerDown(controller, e.Location, e.Button, displayRectangle)) { Invalidate(); return; }')

Edit-File (Join-Path $sm "CaptureScreen\Views\ViewPort.cs") `
    'this.Focus();' `
    ('this.Focus();' + "`r`n" + '            if (LivePoseHook.PointerMove(controller, e.Location, e.Button, displayRectangle)) { Invalidate(); return; }')

Edit-File (Join-Path $sm "CaptureScreen\Views\ViewPort.cs") `
    'base.OnMouseUp(e);' `
    ('base.OnMouseUp(e);' + "`r`n" + '            if (LivePoseHook.PointerUp(controller, e.Location, e.Button, displayRectangle)) { Invalidate(); return; }')

$player = Join-Path $sm "PlayerScreen\Controls\PlayerScreenUserInterface2.cs"

Edit-File $player `
    'private void SurfaceScreen_Paint(object sender, PaintEventArgs e)' `
    ('// Where the whole video image sits on screen (empty when mirrored or nothing is loaded), for the live joint overlay.' + "`r`n" + '        private Rectangle LivePoseRect()' + "`r`n" + '        {' + "`r`n" + '            try' + "`r`n" + '            {' + "`r`n" + '                if (!m_FrameServer.Loaded || m_FrameServer.Metadata.Mirrored) return Rectangle.Empty;' + "`r`n" + '                return m_FrameServer.ImageTransform.Transform(new RectangleF(PointF.Empty, (SizeF)m_FrameServer.VideoReader.Info.ReferenceSize));' + "`r`n" + '            }' + "`r`n" + '            catch (Exception) { return Rectangle.Empty; }' + "`r`n" + '        }' + "`r`n`r`n" + '        private void SurfaceScreen_Paint(object sender, PaintEventArgs e)')

Edit-File $player `
    'FlushOnGraphics(m_FrameServer.CurrentImage, e.Graphics, m_viewportManipulator.RenderingSize, iKeyFrameIndex, m_iCurrentPosition, m_FrameServer.ImageTransform);' `
    ('FlushOnGraphics(m_FrameServer.CurrentImage, e.Graphics, m_viewportManipulator.RenderingSize, iKeyFrameIndex, m_iCurrentPosition, m_FrameServer.ImageTransform);' + "`r`n" + '                    LivePoseHook.PlayerPaint(pbSurfaceScreen, e.Graphics, m_FrameServer.CurrentImage, m_iCurrentPosition, m_bIsCurrentlyPlaying, LivePoseRect());')

foreach ($name in @(@("MouseDown", "PointerDown"), @("MouseMove", "PointerMove"), @("MouseUp", "PointerUp"))) {
    $method = "SurfaceScreen_" + $name[0]
    $call = "LivePoseHook." + $name[1]
    Edit-File $player `
        ("private void " + $method + "(object sender, MouseEventArgs e)") `
        ("private void " + $method + "(object sender, MouseEventArgs e)" + "`r`n" + '        {' + "`r`n" + "            if ($call(pbSurfaceScreen, e.Location, e.Button, LivePoseRect())) { pbSurfaceScreen.Invalidate(); return; }" + "`r`n" + "            ${method}Core(sender, e);" + "`r`n" + '        }' + "`r`n`r`n" + "        private void ${method}Core(object sender, MouseEventArgs e)")
}

Write-Host "Live pose hook applied."

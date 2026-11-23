# Better first-connection format for cameras the upstream default trips over (GoPro Webcam).
# Upstream connects a never-configured camera with the first media type it lists. The GoPro Webcam virtual camera
# lists several sizes but only delivers the one chosen in the GoPro Webcam app (1080p by default), so the camera
# shows a thumbnail in the list (the thumbnail lets the camera pick) but the live view never gets a frame.
# For GoPro devices this picks 1920x1080 in a format that converts cleanly, logs every media type the camera
# offers, and logs when no frame arrives. Other cameras keep the upstream behaviour. Fails loudly if upstream changed.
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"
$grabber = Join-Path $RepoRoot "src\Kinovea.Camera.DirectShow\FrameGrabber.cs"
if (!(Test-Path $grabber)) { throw "Upstream DirectShow FrameGrabber not found: $grabber" }

function Edit-File([string]$path, [string]$anchor, [string]$replacement) {
    $text = [System.IO.File]::ReadAllText($path)
    $first = $text.IndexOf($anchor)
    if ($first -lt 0) { throw "Anchor not found in ${path}: $anchor" }
    if ($text.IndexOf($anchor, $first + 1) -ge 0) { throw "Anchor is not unique in ${path}: $anchor" }
    $text = $text.Substring(0, $first) + $replacement + $text.Substring($first + $anchor.Length)
    [System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($true)))
}

# 1. Choose the first-connection media type through our picker.
Edit-File $grabber `
    'cap = caps[0];' `
    'cap = CassettePickDefaultMediaType(caps, summary.Alias);'

# 2. Log what the camera offers, every time it is prepared.
Edit-File $grabber `
    'var loadedConfig = ConfigureDevice();' `
    ('CassetteLogMediaTypes(device, summary.Alias);' + "`r`n" + '            var loadedConfig = ConfigureDevice();')

# 3. Note when the camera starts but sends nothing.
Edit-File $grabber `
    'device.Start();' `
    ('device.Start();' + "`r`n" + '            CassetteWatchFirstFrame();')

# 4. The helpers, added before the frame handler.
$helpers = @'
        // ---- Cassette Motion Pro: first-connection format and diagnostics ----
        private System.Threading.Timer cassetteFirstFrameTimer;

        private static bool CassetteIsGoPro(string alias)
        {
            return alias != null && alias.IndexOf("GoPro", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // GoPro Webcam only streams the size chosen in the GoPro Webcam app (1080p by default) and RGB24 / YUY2 / MJPG
        // convert cleanly. Everything else keeps the upstream choice (the first media type the camera lists).
        private static AForge.Video.DirectShow.VideoCapabilities CassettePickDefaultMediaType(AForge.Video.DirectShow.VideoCapabilities[] caps, string alias)
        {
            if (!CassetteIsGoPro(alias))
                return caps[0];

            AForge.Video.DirectShow.VideoCapabilities best = caps[0];
            double bestScore = double.MinValue;
            foreach (AForge.Video.DirectShow.VideoCapabilities cap in caps)
            {
                double score = 0;
                int w = cap.FrameSize.Width, h = cap.FrameSize.Height;
                if (w == 1920 && h == 1080) score += 1000;
                else if (w == 1280 && h == 720) score += 500;
                else score += Math.Min(w * h, 1920 * 1080) / 10000.0;
                string compression = (cap.Compression ?? string.Empty).Trim().ToUpperInvariant();
                if (compression == "RGB24") score += 60;
                else if (compression == "YUY2") score += 50;
                else if (compression == "MJPG") score += 40;
                score -= Math.Abs(cap.AverageFrameRate - 30) * 0.5;
                if (score > bestScore) { bestScore = score; best = cap; }
            }
            log.InfoFormat("GoPro camera {0}: first connection uses index {1} ({2}x{3} @ {4:0.###} fps, {5}).",
                alias, best.Index, best.FrameSize.Width, best.FrameSize.Height, best.AverageFrameRate, best.Compression);
            return best;
        }

        private static void CassetteLogMediaTypes(VideoCaptureDevice device, string alias)
        {
            try
            {
                AForge.Video.DirectShow.VideoCapabilities[] caps = device.VideoCapabilities;
                log.InfoFormat("Camera {0} offers {1} media type(s).", alias, caps.Length);
                foreach (AForge.Video.DirectShow.VideoCapabilities cap in caps)
                    log.InfoFormat("  index {0}: {1}x{2} @ {3:0.###} fps, {4}", cap.Index, cap.FrameSize.Width, cap.FrameSize.Height, cap.AverageFrameRate, cap.Compression);
            }
            catch (Exception e)
            {
                log.ErrorFormat("Could not list media types for {0}: {1}", alias, e.Message);
            }
        }

        private void CassetteWatchFirstFrame()
        {
            if (cassetteFirstFrameTimer != null) cassetteFirstFrameTimer.Dispose();
            cassetteFirstFrameTimer = new System.Threading.Timer(delegate
            {
                if (receivedFirstFrame || !grabbing) return;
                if (CassetteIsGoPro(summary.Alias))
                    log.ErrorFormat("No frames from {0} after 8 s. Check that the GoPro Webcam app shows the camera ready (blue dot), that its preview window is closed, and that the size chosen in the GoPro Webcam app matches the camera settings here.", summary.Alias);
                else
                    log.ErrorFormat("No frames from {0} after 8 s. The selected media type may not be delivered by this camera; try another one in the camera settings.", summary.Alias);
            }, null, 8000, System.Threading.Timeout.Infinite);
        }
        // ---- end Cassette Motion Pro ----

        private void device_NewFrameBuffer(object sender, NewFrameBufferEventArgs e)
'@
Edit-File $grabber `
    'private void device_NewFrameBuffer(object sender, NewFrameBufferEventArgs e)' `
    $helpers.Replace("`n", "`r`n").Replace("`r`r`n", "`r`n")

# Better camera formats than the upstream default (USB global shutter cameras such as the ELP AR0234, webcams).
# Upstream connects a never-configured camera with the first media type it lists. USB cameras often list a raw
# (YUY2) full-size mode first, which USB carries at only a few frames per second, so the picture is choppy, black or
# frozen. This picks 1920x1080 (or the nearest size) at the highest frame rate up to 120 fps, which is MJPG on most
# cameras.
# - A camera already saved in a raw mode that is slow (under 25 fps) or HD size or larger is moved once to a fast
#   MJPG mode when it has one (listed in camera-format-upgrades.txt, so a raw mode chosen again later is kept).
# - A camera that sends no frame within 3 s of starting is restarted, up to twice (the first start after the camera
#   list's thumbnail can stay black; a second start works). Each restart and a final failure are logged as warnings
#   with the camera's media types, so they show in the normal log without debug logging.
# It also logs every media type the camera offers (debug log). Fails loudly if upstream changed.
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
    ('cap = CassettePickDefaultMediaType(caps, summary.Alias);' + "`r`n" + '                CassetteRememberFormatChoice();')

# 2. Log what the camera offers, every time it is prepared.
Edit-File $grabber `
    'var loadedConfig = ConfigureDevice();' `
    ('CassetteLogMediaTypes(device, summary.Alias);' + "`r`n" + '            var loadedConfig = ConfigureDevice();' + "`r`n" + '            loadedConfig = CassetteUpgradeSlowSavedMediaType(loadedConfig);')

# 3. Restart the camera if it starts but sends nothing.
Edit-File $grabber `
    'device.Start();' `
    ('device.Start();' + "`r`n" + '            CassetteWatchFirstFrame();')

# 3b. Stop the first-frame watch before the camera is stopped.
Edit-File $grabber `
    'DeviceHelper.StopDevice(device);' `
    ('CassetteStopWatch();' + "`r`n" + '            DeviceHelper.StopDevice(device);')

# 4. The helpers, added before the frame handler.
$helpers = @'
        // ---- Cassette Motion Pro: first-connection format and diagnostics ----
        private System.Threading.Timer cassetteFirstFrameTimer;
        private readonly object cassetteWatchLock = new object();
        private int cassetteRestarts;
        private bool cassetteStopping;
        private const int CassetteFirstFrameWaitMs = 3000, CassetteMaxRestarts = 2;

        // The fastest full-HD mode (see CassettePickFastMediaType), logged.
        private static AForge.Video.DirectShow.VideoCapabilities CassettePickDefaultMediaType(AForge.Video.DirectShow.VideoCapabilities[] caps, string alias)
        {
            AForge.Video.DirectShow.VideoCapabilities fast = CassettePickFastMediaType(caps);
            log.WarnFormat("Camera {0}: first connection uses index {1} ({2}x{3} @ {4:0.###} fps, {5}).",
                alias, fast.Index, fast.FrameSize.Width, fast.FrameSize.Height, fast.AverageFrameRate, fast.Compression);
            return fast;
        }

        // 1920x1080 (or the nearest size up to 1920x1200) at the highest frame rate up to 120 fps. On USB cameras the
        // fast modes are MJPG; raw YUY2 at full size runs at a few frames per second.
        private static AForge.Video.DirectShow.VideoCapabilities CassettePickFastMediaType(AForge.Video.DirectShow.VideoCapabilities[] caps)
        {
            AForge.Video.DirectShow.VideoCapabilities best = caps[0];
            double bestScore = double.MinValue;
            foreach (AForge.Video.DirectShow.VideoCapabilities cap in caps)
            {
                int w = cap.FrameSize.Width, h = cap.FrameSize.Height;
                double area = (double)w * h, full = 1920.0 * 1080.0;
                double size;
                if (w == 1920 && h == 1080) size = 100;
                else if (w == 1920 && h == 1200) size = 95;
                else if (area <= full) size = 80 * area / full;
                else size = 70;                                   // larger than needed: more data, slower pose
                double fps = Math.Min(cap.AverageFrameRate, 120);
                double score = size + fps * 0.8;
                if (fps < 25) score -= 60;                        // too slow to judge a pedal stroke
                string compression = (cap.Compression ?? string.Empty).Trim().ToUpperInvariant();
                if (compression == "MJPG") score += 5;
                if (score > bestScore) { bestScore = score; best = cap; }
            }
            return best;
        }

        // A camera saved in a raw mode that is slow, or too big for USB to carry smoothly (usually the old upstream
        // default), is moved once to a fast MJPG mode. Cameras already moved are listed in camera-format-upgrades.txt
        // in the settings folder, so a raw mode chosen later in the camera settings is kept.
        private SpecificInfo CassetteUpgradeSlowSavedMediaType(SpecificInfo info)
        {
            if (info == null || string.IsNullOrEmpty(summary.Identifier)) return info;
            try
            {
                string listPath = System.IO.Path.Combine(Software.SettingsDirectory, "camera-format-upgrades.txt");
                if (System.IO.File.Exists(listPath))
                    foreach (string line in System.IO.File.ReadAllLines(listPath))
                        if (string.Equals(line.Trim(), summary.Identifier, StringComparison.OrdinalIgnoreCase)) return info;

                AForge.Video.DirectShow.VideoCapabilities[] caps = device.VideoCapabilities;
                AForge.Video.DirectShow.VideoCapabilities current = caps.FirstOrDefault(c => c.Index == info.MediaTypeIndex);
                if (current == null) return info;
                string compression = (current.Compression ?? string.Empty).Trim().ToUpperInvariant();
                double fps = info.SelectedFramerate > 0 ? info.SelectedFramerate : current.AverageFrameRate;
                bool big = (double)current.FrameSize.Width * current.FrameSize.Height >= 1280.0 * 720.0;
                if (compression == "MJPG" || (fps >= 25 && !big)) return info;
                AForge.Video.DirectShow.VideoCapabilities fast = CassettePickFastMediaType(caps);
                string fastCompression = (fast.Compression ?? string.Empty).Trim().ToUpperInvariant();
                if (fast.Index == current.Index || fastCompression != "MJPG" || fast.AverageFrameRate < 25 || fast.AverageFrameRate < fps) return info;

                device.SetMediaTypeAndFramerate(fast.Index, fast.AverageFrameRate);
                info.MediaTypeIndex = fast.Index;
                info.SelectedFramerate = (float)fast.AverageFrameRate;
                System.IO.File.AppendAllText(listPath, summary.Identifier + Environment.NewLine);
                log.WarnFormat("Camera {0} was saved at {1}x{2} @ {3:0.###} fps ({4}), which USB cannot carry smoothly. Switched once to index {5} ({6}x{7} @ {8:0.###} fps, {9}). A raw mode chosen again in the camera settings is kept.",
                    summary.Alias, current.FrameSize.Width, current.FrameSize.Height, fps, current.Compression,
                    fast.Index, fast.FrameSize.Width, fast.FrameSize.Height, fast.AverageFrameRate, fast.Compression);
            }
            catch (Exception e)
            {
                log.ErrorFormat("Could not check the saved media type for {0}: {1}", summary.Alias, e.Message);
            }
            return info;
        }

        // The first-connection choice counts as the one-time move: a raw mode chosen later is kept.
        private void CassetteRememberFormatChoice()
        {
            if (string.IsNullOrEmpty(summary.Identifier)) return;
            try
            {
                string listPath = System.IO.Path.Combine(Software.SettingsDirectory, "camera-format-upgrades.txt");
                System.IO.File.AppendAllText(listPath, summary.Identifier + Environment.NewLine);
            }
            catch (Exception e)
            {
                log.ErrorFormat("Could not record the camera format choice for {0}: {1}", summary.Alias, e.Message);
            }
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

        // Started by Start(): wait for the first frame, restarting the camera if none comes.
        private void CassetteWatchFirstFrame()
        {
            lock (cassetteWatchLock)
            {
                cassetteStopping = false;
                cassetteRestarts = 0;
                CassetteArmWatch();
            }
        }

        private void CassetteArmWatch()
        {
            if (cassetteFirstFrameTimer != null) cassetteFirstFrameTimer.Dispose();
            cassetteFirstFrameTimer = new System.Threading.Timer(CassetteCheckFirstFrame, null, CassetteFirstFrameWaitMs, System.Threading.Timeout.Infinite);
        }

        private void CassetteCheckFirstFrame(object state)
        {
            lock (cassetteWatchLock)
            {
                if (cassetteStopping || receivedFirstFrame || !grabbing) return;
                string format = CassetteCurrentFormat();
                if (cassetteRestarts < CassetteMaxRestarts)
                {
                    cassetteRestarts++;
                    log.WarnFormat("No frames from {0} ({1}) after {2} s. Restarting the camera ({3} of {4}).",
                        summary.Alias, format, CassetteFirstFrameWaitMs / 1000, cassetteRestarts, CassetteMaxRestarts);
                    try
                    {
                        DeviceHelper.StopDevice(device);
                        if (cassetteStopping) return;
                        device.Start();
                    }
                    catch (Exception e)
                    {
                        log.ErrorFormat("Could not restart {0}: {1}", summary.Alias, e.Message);
                    }
                    CassetteArmWatch();
                    return;
                }
                log.ErrorFormat("No frames from {0} ({1}) after {2} restarts. Use a USB 3 (blue) port directly on the computer, not a hub, close other programs that use the camera, and choose an MJPG 1920x1080 media type in the camera settings.",
                    summary.Alias, format, CassetteMaxRestarts);
                CassetteLogMediaTypesAsWarning();
            }
        }

        // Called by Stop() before the camera is stopped.
        private void CassetteStopWatch()
        {
            lock (cassetteWatchLock)
            {
                cassetteStopping = true;
                if (cassetteFirstFrameTimer != null) cassetteFirstFrameTimer.Dispose();
                cassetteFirstFrameTimer = null;
            }
        }

        private string CassetteCurrentFormat()
        {
            try
            {
                AForge.Video.DirectShow.VideoCapabilities cap = device.VideoResolution;
                if (cap == null) return "format unknown";
                return string.Format("{0}x{1} @ {2:0.###} fps, {3}", cap.FrameSize.Width, cap.FrameSize.Height, resultingFramerate, cap.Compression);
            }
            catch (Exception)
            {
                return "format unknown";
            }
        }

        private void CassetteLogMediaTypesAsWarning()
        {
            try
            {
                AForge.Video.DirectShow.VideoCapabilities[] caps = device.VideoCapabilities;
                log.WarnFormat("{0} offers {1} media type(s):", summary.Alias, caps.Length);
                foreach (AForge.Video.DirectShow.VideoCapabilities cap in caps)
                    log.WarnFormat("  index {0}: {1}x{2} @ {3:0.###} fps, {4}", cap.Index, cap.FrameSize.Width, cap.FrameSize.Height, cap.AverageFrameRate, cap.Compression);
            }
            catch (Exception e)
            {
                log.ErrorFormat("Could not list media types for {0}: {1}", summary.Alias, e.Message);
            }
        }
        // ---- end Cassette Motion Pro ----

        private void device_NewFrameBuffer(object sender, NewFrameBufferEventArgs e)
'@
Edit-File $grabber `
    'private void device_NewFrameBuffer(object sender, NewFrameBufferEventArgs e)' `
    $helpers.Replace("`n", "`r`n").Replace("`r`r`n", "`r`n")

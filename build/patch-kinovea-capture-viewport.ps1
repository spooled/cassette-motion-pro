# Keep the live camera picture on screen.
# Upstream saves where each camera's picture was drawn (CameraSummary.DisplayRectangle) and reuses it on the next
# connection, checking only its aspect ratio. A position saved in another layout, or while the capture screen had no
# size yet, draws the picture outside the visible area, so the screen stays dark until the layout is changed.
# This refits the picture to the screen when the saved position is mostly off screen or tiny, on connection and on
# every resize of the capture view.
# It also watches each newly connected camera: if no picture has been painted after 3 s it refreshes the view, and
# after 6 s it reconnects the camera once (what changing the screen layout did by hand). Each step is logged as a
# warning with the display state, so the cause can be read from the normal log. Fails loudly if upstream changed.
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"
$sm = Join-Path $RepoRoot "src\Kinovea.ScreenManager"
$viewport = Join-Path $sm "CaptureScreen\Views\ViewPort.cs"
$capture = Join-Path $sm "CaptureScreen\CaptureScreen.cs"
foreach ($path in @($viewport, $capture)) { if (!(Test-Path $path)) { throw "Upstream file not found: $path" } }

function Edit-File([string]$path, [string]$anchor, [string]$replacement) {
    $text = [System.IO.File]::ReadAllText($path)
    $first = $text.IndexOf($anchor)
    if ($first -lt 0) { throw "Anchor not found in ${path}: $anchor" }
    if ($text.IndexOf($anchor, $first + 1) -ge 0) { throw "Anchor is not unique in ${path}: $anchor" }
    $text = $text.Substring(0, $first) + $replacement + $text.Substring($first + $anchor.Length)
    [System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($true)))
}

# 1. The check, added to the viewport before OnResize.
$helper = @'
        // ---- Cassette Motion Pro: keep the picture on screen ----
        private static readonly log4net.ILog cassetteLog = log4net.LogManager.GetLogger(typeof(Viewport));

        /// <summary>
        /// Refit the picture to the view when it is drawn mostly outside the visible area or at almost no size.
        /// Returns true when it was refitted.
        /// </summary>
        public int CassettePaintedFrames;   // pictures painted since the camera connected (live view watch)

        public bool CassetteRefitIfLost()
        {
            if (referenceSize.Width <= 0 || referenceSize.Height <= 0 || this.Width < 16 || this.Height < 16)
                return false;

            // Lost: almost no size, or less than a quarter of the picture (or of the view, when zoomed in) is visible.
            Rectangle visible = Rectangle.Intersect(displayRectangle, this.ClientRectangle);
            double shown = (double)visible.Width * visible.Height;
            double drawn = (double)displayRectangle.Width * displayRectangle.Height;
            double view = (double)this.ClientRectangle.Width * this.ClientRectangle.Height;
            bool lost = displayRectangle.Width < 16 || displayRectangle.Height < 16 || shown < 0.25 * Math.Min(drawn, view);
            if (!lost)
                return false;

            Rectangle before = displayRectangle;
            InitializeDisplayRectangle(Rectangle.Empty);
            controller.UpdateDisplayRectangle(displayRectangle);
            ForceZoomValue();
            Invalidate();
            cassetteLog.WarnFormat("Live picture was drawn at {0} in a {1}x{2} view; refitted to {3}.", before, this.Width, this.Height, displayRectangle);
            return true;
        }
        // ---- end Cassette Motion Pro ----

        protected override void OnResize(EventArgs e)
'@
Edit-File $viewport `
    'protected override void OnResize(EventArgs e)' `
    $helper.Replace("`n", "`r`n").Replace("`r`r`n", "`r`n")

# 2. Check again whenever the view changes size (for example when it gets its real size after the camera opened).
Edit-File $viewport `
    'base.OnResize(e);' `
    ('base.OnResize(e);' + "`r`n" + '            CassetteRefitIfLost();')

# 3. Check right after a camera connects with its saved position.
Edit-File $capture `
    'viewportController.InitializeDisplayRectangle(cameraSummary.DisplayRectangle, referenceSize);' `
    ('viewportController.InitializeDisplayRectangle(cameraSummary.DisplayRectangle, referenceSize);' + "`r`n" + '            if (viewportController.View != null) viewportController.View.CassetteRefitIfLost();')

# 4. Count pictures actually painted.
Edit-File $viewport `
    'DrawImage(e.Graphics);' `
    ('DrawImage(e.Graphics);' + "`r`n" + '            CassettePaintedFrames++;')

# 5. Count pictures handed to the view.
Edit-File $capture `
    'viewportController.Bitmap = displayFrame;' `
    ('viewportController.Bitmap = displayFrame;' + "`r`n" + '                cassetteDisplayedFrames++;')

# 6. Start the live view watch when a camera has connected.
Edit-File $capture `
    'cameraConnected = true;' `
    ('cameraConnected = true;' + "`r`n" + '            CassetteStartViewWatch();')

# 7. Stop it when the screen is closed.
Edit-File $capture `
    'displayTimer.Tick -= displayTimer_Tick;' `
    ('displayTimer.Tick -= displayTimer_Tick;' + "`r`n" + '            if (cassetteViewWatch != null) { cassetteViewWatch.Stop(); cassetteViewWatch.Tick -= CassetteViewWatch_Tick; }')

# 8. The watch itself, added before SlowTick.
$watch = @'
        // ---- Cassette Motion Pro: live view watch ----
        private System.Windows.Forms.Timer cassetteViewWatch;
        private readonly Stopwatch cassetteViewClock = new Stopwatch();
        private int cassetteDisplayedFrames, cassetteViewStage, cassetteReconnects;
        private string cassetteWatchedCamera;

        private void CassetteStartViewWatch()
        {
            if (cassetteViewWatch == null)
            {
                cassetteViewWatch = new System.Windows.Forms.Timer();
                cassetteViewWatch.Interval = 500;
                cassetteViewWatch.Tick += CassetteViewWatch_Tick;
            }
            string id = cameraSummary == null ? null : cameraSummary.Identifier;
            if (id != cassetteWatchedCamera) { cassetteWatchedCamera = id; cassetteReconnects = 0; }
            cassetteDisplayedFrames = 0;
            cassetteViewStage = 0;
            if (viewportController.View != null) viewportController.View.CassettePaintedFrames = 0;
            cassetteViewClock.Restart();
            cassetteViewWatch.Start();
        }

        private void CassetteViewWatch_Tick(object sender, EventArgs e)
        {
            try
            {
                Viewport view = viewportController.View;
                if (!cameraLoaded || view == null) { cassetteViewWatch.Stop(); return; }
                if (view.CassettePaintedFrames > 0)
                {
                    if (cassetteViewStage > 0)
                        log.WarnFormat("Live view of {0} shows the picture now (after step {1}). {2}", cameraSummary.Alias, cassetteViewStage, CassetteViewState());
                    cassetteReconnects = 0;
                    cassetteViewWatch.Stop();
                    return;
                }

                long ms = cassetteViewClock.ElapsedMilliseconds;
                if (cassetteViewStage == 0 && ms >= 3000)
                {
                    cassetteViewStage = 1;
                    log.WarnFormat("Live view of {0} has painted no picture after 3 s. {1} Refreshing the view.", cameraSummary.Alias, CassetteViewState());
                    view.CassetteRefitIfLost();
                    if (view.Parent != null) view.Parent.PerformLayout();
                    view.Invalidate();
                    view.Update();
                }
                else if (cassetteViewStage == 1 && ms >= 6000)
                {
                    cassetteViewStage = 2;
                    cassetteViewWatch.Stop();
                    if (cassetteReconnects >= 1)
                    {
                        log.ErrorFormat("Live view of {0} still has no picture after reconnecting. {1}", cameraSummary.Alias, CassetteViewState());
                        return;
                    }
                    cassetteReconnects++;
                    log.WarnFormat("Live view of {0} still has no picture after 6 s. {1} Reconnecting the camera.", cameraSummary.Alias, CassetteViewState());
                    Disconnect();
                    Connect();
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Live view watch failed: {0}", ex.Message);
                if (cassetteViewWatch != null) cassetteViewWatch.Stop();
            }
        }

        private string CassetteViewState()
        {
            try
            {
                Viewport view = viewportController.View;
                return string.Format("State: connected {0}, display timer {1} every {2} ms, pictures handed to the view {3}, painted {4}, delay buffer {5} slots at {6}, mode {7}, delay {8}, image {9}x{10} {11}, view {12}x{13} visible {14} handle {15}, picture at {16}, signal {17:0.0} fps.",
                    cameraConnected, displayTimer.Enabled, displayTimer.Interval, cassetteDisplayedFrames, view == null ? -1 : view.CassettePaintedFrames,
                    delayer.FullCapacity, delayer.CurrentPosition, recordingMode, delay,
                    imageDescriptor.Width, imageDescriptor.Height, imageDescriptor.Format,
                    view == null ? 0 : view.Width, view == null ? 0 : view.Height, view != null && view.Visible, view != null && view.IsHandleCreated,
                    viewportController.DisplayRectangle, pipelineManager.Frequency);
            }
            catch (Exception ex)
            {
                return "State unavailable (" + ex.Message + ").";
            }
        }
        // ---- end Cassette Motion Pro ----

        private void SlowTick()
'@
Edit-File $capture `
    'private void SlowTick()' `
    $watch.Replace("`n", "`r`n").Replace("`r`r`n", "`r`n")

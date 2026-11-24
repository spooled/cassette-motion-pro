# Keep the live camera picture on screen.
# Upstream saves where each camera's picture was drawn (CameraSummary.DisplayRectangle) and reuses it on the next
# connection, checking only its aspect ratio. A position saved in another layout, or while the capture screen had no
# size yet, draws the picture outside the visible area, so the screen stays dark until the layout is changed.
# This refits the picture to the screen when the saved position is mostly off screen or tiny, on connection and on
# every resize of the capture view. Fails loudly if upstream changed.
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

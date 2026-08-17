$screenManagerPath = "src\Kinovea.ScreenManager"
if (!(Test-Path $screenManagerPath)) {
  throw "Kinovea.ScreenManager was not found. Restore upstream project dependencies before applying this patch."
}

$routerPath = Join-Path $screenManagerPath "CassetteReportImageSaveRouter.cs"
@'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Kinovea.Services;

namespace Kinovea.ScreenManager
{
    public static class CassetteReportImageSaveRouter
    {
        public static Func<IWin32Window, Bitmap, string, bool> SaveReportImageRequested;

        public static bool TrySaveReportImage(PlayerScreen player, string suggestedFileName)
        {
            if (SaveReportImageRequested == null || player == null || player.view == null)
                return false;

            try
            {
                using (Bitmap bitmap = CapturePlayer(player))
                {
                    bool handled = SaveReportImageRequested(null, bitmap, suggestedFileName);
                    if (handled)
                        NotificationCenter.RaiseRefreshFileList(false);

                    return handled;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool TrySaveCompositeReportImage(PlayerScreen player1, PlayerScreen player2, string suggestedFileName)
        {
            if (SaveReportImageRequested == null || player1 == null || player2 == null || player1.view == null || player2.view == null)
                return false;

            try
            {
                using (Bitmap left = CapturePlayer(player1))
                using (Bitmap right = CapturePlayer(player2))
                using (Bitmap composite = BuildComposite(left, right))
                {
                    bool handled = SaveReportImageRequested(null, composite, suggestedFileName);
                    if (handled)
                        NotificationCenter.RaiseRefreshFileList(false);

                    return handled;
                }
            }
            catch
            {
                return false;
            }
        }

        private static Bitmap CapturePlayer(PlayerScreen player)
        {
            Bitmap bitmap = null;
            bool prepared = false;

            try
            {
                player.view.BeforeExportVideo();
                prepared = true;
                Size size = player.FrameServer.VideoReader.Info.ReferenceSize;
                bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format24bppRgb);
                player.view.PaintFlushedImage(bitmap);
                return bitmap;
            }
            catch
            {
                if (bitmap != null)
                    bitmap.Dispose();

                throw;
            }
            finally
            {
                if (prepared)
                    player.view.AfterExportVideo();
            }
        }

        private static Bitmap BuildComposite(Bitmap left, Bitmap right)
        {
            int width = left.Width + right.Width;
            int height = Math.Max(left.Height, right.Height);
            Bitmap composite = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            using (Graphics graphics = Graphics.FromImage(composite))
            {
                graphics.Clear(Color.Black);
                graphics.DrawImageUnscaled(left, 0, 0);
                graphics.DrawImageUnscaled(right, left.Width, 0);
            }

            return composite;
        }
    }
}
'@ | Set-Content $routerPath

$screenManagerProject = Join-Path $screenManagerPath "Kinovea.ScreenManager.csproj"
$projectContent = Get-Content $screenManagerProject -Raw
if ($projectContent -notmatch "CassetteReportImageSaveRouter\.cs") {
  $compileMarker = '    <Compile Include="Exporters\Images\ExporterImage.cs" />'
  $compileInsert = '    <Compile Include="CassetteReportImageSaveRouter.cs" />' + "`r`n" + $compileMarker
  $projectContent = $projectContent.Replace($compileMarker, $compileInsert)
  if ($projectContent -notmatch "CassetteReportImageSaveRouter\.cs") {
    throw "Could not add CassetteReportImageSaveRouter.cs to Kinovea.ScreenManager.csproj."
  }
  Set-Content $screenManagerProject $projectContent
}

$imageExporterPath = Join-Path $screenManagerPath "Exporters\Images\ImageExporter.cs"
$imageExporterContent = Get-Content $imageExporterPath -Raw

if ($imageExporterContent -notmatch "CassetteReportImageSaveRouter\.TrySaveReportImage") {
  $pattern = '(?s)(\s+// Immediately get a file name to save to\.\r?\n\s+// Any configuration of the save happens later\.\r?\n\s+SaveFileDialog sfd = new SaveFileDialog\(\);)'
  $replacement = @'

            string suggestedFilename = SuggestFilename(format, player1, player2);
            if (player2 != null && CassetteReportImageSaveRouter.TrySaveCompositeReportImage(player1, player2, suggestedFilename))
            {
                player1.FrameServer.AfterSave();
                return;
            }

            if (format == ImageExportFormat.Image && CassetteReportImageSaveRouter.TrySaveReportImage(player1, suggestedFilename))
            {
                player1.FrameServer.AfterSave();
                return;
            }
$1
'@

  $patched = [System.Text.RegularExpressions.Regex]::Replace($imageExporterContent, $pattern, $replacement, 1)
  if ($patched -eq $imageExporterContent) {
    throw "Could not patch ImageExporter.cs near the SaveFileDialog creation."
  }

  $patched = $patched -replace 'sfd\.FileName = SuggestFilename\(format, player1, player2\);', 'sfd.FileName = suggestedFilename;'
  Set-Content $imageExporterPath $patched
}

Write-Host "Patched Kinovea Save image to offer Cassette Motion Pro Before/After report image saves."

$screenManagerPath = "src\Kinovea.ScreenManager"
if (!(Test-Path $screenManagerPath)) {
  throw "Kinovea.ScreenManager was not found. Restore upstream project dependencies before applying this patch."
}

$routerPath = Join-Path $screenManagerPath "CassetteVideoSaveRouter.cs"
@'
using System;
using System.IO;
using System.Windows.Forms;
using Kinovea.Services;

namespace Kinovea.ScreenManager
{
    public static class CassetteVideoSaveRouter
    {
        public const string CancelSaveToken = "__CASSETTE_VIDEO_SAVE_CANCEL__";

        public static Func<IWin32Window, string, string, string> ChooseVideoSavePathRequested;
        public static Action<string> VideoSaveCompleted;

        private static bool hasPendingSavePath;
        private static string pendingSavePath;

        public static bool PromptForNextVideoSave(IWin32Window owner, string suggestedFileName, string preferredFormat)
        {
            string path = ChooseVideoSavePath(owner, suggestedFileName, preferredFormat);
            if (IsCancelToken(path))
                return false;

            pendingSavePath = path;
            hasPendingSavePath = true;
            return true;
        }

        public static bool TryConsumePendingSavePath(out string path)
        {
            if (!hasPendingSavePath)
            {
                path = null;
                return false;
            }

            path = pendingSavePath;
            pendingSavePath = null;
            hasPendingSavePath = false;
            return true;
        }

        public static string ChooseVideoSavePath(IWin32Window owner, string suggestedFileName, string preferredFormat)
        {
            if (ChooseVideoSavePathRequested == null)
                return null;

            return ChooseVideoSavePathRequested(owner, suggestedFileName, preferredFormat);
        }

        public static bool IsCancelToken(string path)
        {
            return string.Equals(path, CancelSaveToken, StringComparison.Ordinal);
        }

        public static void NotifyVideoSaved(string path)
        {
            if (VideoSaveCompleted == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            VideoSaveCompleted(path);
            NotificationCenter.RaiseRefreshFileList(false);
        }
    }
}
'@ | Set-Content $routerPath

$screenManagerProject = Join-Path $screenManagerPath "Kinovea.ScreenManager.csproj"
$projectContent = Get-Content $screenManagerProject -Raw
if ($projectContent -notmatch "CassetteVideoSaveRouter\.cs") {
  $compileMarker = '    <Compile Include="CassetteReportImageSaveRouter.cs" />'
  $escapedCompileMarker = [regex]::Escape($compileMarker)
  if ($projectContent -match $escapedCompileMarker) {
    $compileInsert = $compileMarker + "`r`n" + '    <Compile Include="CassetteVideoSaveRouter.cs" />'
    $projectContent = $projectContent.Replace($compileMarker, $compileInsert)
  } else {
    $fallbackMarker = '    <Compile Include="Exporters\Video\VideoExporter.cs" />'
    $compileInsert = '    <Compile Include="CassetteVideoSaveRouter.cs" />' + "`r`n" + $fallbackMarker
    $projectContent = $projectContent.Replace($fallbackMarker, $compileInsert)
  }

  if ($projectContent -notmatch "CassetteVideoSaveRouter\.cs") {
    throw "Could not add CassetteVideoSaveRouter.cs to Kinovea.ScreenManager.csproj."
  }

  Set-Content $screenManagerProject $projectContent
}

$videoExporterPath = Join-Path $screenManagerPath "Exporters\Video\VideoExporter.cs"
$videoExporterContent = Get-Content $videoExporterPath -Raw

if ($videoExporterContent -notmatch "CassetteVideoSaveRouter\.ChooseVideoSavePath") {
  $saveDialogPattern = '(?s)(\s+// Immediately get a file name to save to\.\r?\n\s+// Any configuration of the save happens later\.\r?\n\s+SaveFileDialog sfd = new SaveFileDialog\(\);)'
  $saveDialogReplacement = @'

            string suggestedFilename = SuggestFilename(format, player1, player2);
            string cassetteSavePath;
            bool hasCassetteSavePath = CassetteVideoSaveRouter.TryConsumePendingSavePath(out cassetteSavePath);
            if (!hasCassetteSavePath)
            {
                // Cassette Motion Pro: fallback for export paths that do not pass through
                // the toolbar/menu command first.
                cassetteSavePath = CassetteVideoSaveRouter.ChooseVideoSavePath(null, suggestedFilename, PreferencesManager.PlayerPreferences.VideoFormat.ToString());
            }
            if (CassetteVideoSaveRouter.IsCancelToken(cassetteSavePath))
                return;

            bool useCassetteSavePath = !string.IsNullOrEmpty(cassetteSavePath);
$1
'@

  $patched = [System.Text.RegularExpressions.Regex]::Replace($videoExporterContent, $saveDialogPattern, $saveDialogReplacement, 1)
  if ($patched -eq $videoExporterContent) {
    throw "Could not patch VideoExporter.cs near the SaveFileDialog creation."
  }

  $patched = $patched.Replace('sfd.FileName = SuggestFilename(format, player1, player2);', 'sfd.FileName = useCassetteSavePath ? cassetteSavePath : suggestedFilename;')

  $dialogPattern = @'
            if (sfd.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(sfd.FileName))
                return;
'@

  $dialogReplacement = @'
            if (!useCassetteSavePath && (sfd.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(sfd.FileName)))
                return;
'@

  $patched = $patched.Replace($dialogPattern, $dialogReplacement)

  $notifyPattern = '(?s)(\r?\n\s+)\}\r?\n\s+catch \(Exception e\)'
  $notifyReplacement = "`r`n                if (useCassetteSavePath)`r`n                    CassetteVideoSaveRouter.NotifyVideoSaved(sfd.FileName);`r`n            }`r`n            catch (Exception e)"
  $patched = [System.Text.RegularExpressions.Regex]::Replace($patched, $notifyPattern, $notifyReplacement, 1)

  if ($patched -notmatch "CassetteVideoSaveRouter\.NotifyVideoSaved") {
    throw "Could not add video save notification to VideoExporter.cs."
  }

  Set-Content $videoExporterPath $patched
}

$playerScreenPath = Join-Path $screenManagerPath "PlayerScreen\PlayerScreen.cs"
$playerScreenContent = Get-Content $playerScreenPath -Raw
if ($playerScreenContent -notmatch "CassetteVideoSaveRouter\.PromptForNextVideoSave") {
  $playerScreenReplacement = @'
        private void ExportVideo(VideoExportFormat format)
        {
            if (!CassetteVideoSaveRouter.PromptForNextVideoSave(null, FileName, PreferencesManager.PlayerPreferences.VideoFormat.ToString()))
                return;

            VideoExporter exporter = new VideoExporter();
            exporter.Export(format, this, null, null);
        }
'@

  $playerScreenPattern = '(?s)\s+private void ExportVideo\(VideoExportFormat format\)\s+\{\s+VideoExporter exporter = new VideoExporter\(\);\s+exporter\.Export\(format, this, null, null\);\s+\}'
  $playerScreenContent = [System.Text.RegularExpressions.Regex]::Replace($playerScreenContent, $playerScreenPattern, "`r`n" + $playerScreenReplacement, 1)
  if ($playerScreenContent -notmatch "CassetteVideoSaveRouter\.PromptForNextVideoSave") {
    throw "Could not patch PlayerScreen.cs video export command."
  }

  Set-Content $playerScreenPath $playerScreenContent
}

$screenManagerFilePath = Join-Path $screenManagerPath "ScreenManager.cs"
$screenManagerContent = Get-Content $screenManagerFilePath -Raw
if ($screenManagerContent -notmatch "CassetteVideoSaveRouter\.PromptForNextVideoSave") {
  $sideBySideReplacement = @'
                if (!CassetteVideoSaveRouter.PromptForNextVideoSave(null, "Side-by-side video", PreferencesManager.PlayerPreferences.VideoFormat.ToString()))
                    return;

                VideoExporter exporter = new VideoExporter();
                exporter.Export(format, player1, player2, dualPlayer);
'@

  $singleScreenReplacement = @'
                if (!CassetteVideoSaveRouter.PromptForNextVideoSave(null, player.FileName, PreferencesManager.PlayerPreferences.VideoFormat.ToString()))
                    return;

                VideoExporter exporter = new VideoExporter();
                exporter.Export(format, player, null, null);
'@

  $screenManagerContent = [System.Text.RegularExpressions.Regex]::Replace(
    $screenManagerContent,
    '(?s)\s+VideoExporter exporter = new VideoExporter\(\);\s+exporter\.Export\(format, player1, player2, dualPlayer\);',
    "`r`n" + $sideBySideReplacement,
    1)

  $screenManagerContent = [System.Text.RegularExpressions.Regex]::Replace(
    $screenManagerContent,
    '(?s)\s+VideoExporter exporter = new VideoExporter\(\);\s+exporter\.Export\(format, player, null, null\);',
    "`r`n" + $singleScreenReplacement,
    1)

  if ($screenManagerContent -notmatch "CassetteVideoSaveRouter\.PromptForNextVideoSave") {
    throw "Could not patch ScreenManager.cs video export command."
  }

  Set-Content $screenManagerFilePath $screenManagerContent
}

Write-Host "Patched Kinovea video button/export to offer Cassette Motion Pro Before/After client video saves."

$ErrorActionPreference = 'Stop'

$sources = @(
  'src/Kinovea/Clients/ClientRecord.cs',
  'src/Kinovea/Workspace/FitFollowUpEntry.cs',
  'src/Kinovea/Workspace/FitSessionRecord.cs',
  'src/Kinovea/Workspace/FitSessionRepository.cs',
  'src/Kinovea/Workspace/FitSessionRecoveryStore.cs'
)
$usings = "using System;`nusing System.IO;`nusing System.Collections.Generic;`nusing System.Linq;`nusing System.Xml.Serialization;`nusing CassetteMotionPro.Clients;`n"
$source = $usings + (($sources | ForEach-Object { (Get-Content $_ -Raw) -replace '(?m)^using [^;]+;\r?\n', '' }) -join "`n")
$source += @'

public static class FitSessionRecoveryProbe
{
    public static void Run(string sessionsRoot, CassetteMotionPro.Workspace.FitSessionRecord session)
    {
        var recovery = new CassetteMotionPro.Workspace.FitSessionRecoveryStore(sessionsRoot);
        recovery.Save(session, "Report");
        recovery.Save(session, "Measure");
        string autosave = Path.Combine(sessionsRoot, ".fit-day-recovery", "autosave.xml");
        File.WriteAllText(autosave, "<broken");
        CassetteMotionPro.Workspace.FitSessionRecord draft;
        string section;
        DateTime savedUtc;
        if (!recovery.TryLoad(out draft, out section, out savedUtc) || draft.Id != session.Id)
            throw new InvalidOperationException("Autosave backup recovery failed.");
        if (section != string.Empty)
            throw new InvalidOperationException("A backup draft inherited the newer workspace section.");
        recovery.Clear();
        if (File.Exists(autosave) || File.Exists(autosave + ".bak"))
            throw new InvalidOperationException("Recovery cleanup failed.");
    }
}
'@
Add-Type -TypeDefinition $source -Language CSharp

$root = Join-Path ([System.IO.Path]::GetTempPath()) ('cassette-readiness-' + [guid]::NewGuid().ToString('N'))
try {
  $clientFolder = Join-Path $root 'Legacy Client'
  [System.IO.Directory]::CreateDirectory($clientFolder) | Out-Null
  $client = [CassetteMotionPro.Clients.ClientRecord]::new()
  $client.FolderPath = $clientFolder
  $repository = [CassetteMotionPro.Workspace.FitSessionRepository]::new($client)

  # A minimal old manifest must still deserialize with the current model.
  $legacyFolder = Join-Path $repository.RootPath 'legacy-session'
  [System.IO.Directory]::CreateDirectory($legacyFolder) | Out-Null
  $legacyId = [guid]::NewGuid()
  $legacyXml = '<FitSessionRecord><Id>' + $legacyId + '</Id><Title>Legacy fit</Title><SessionDate>2026-01-01T00:00:00</SessionDate><Status>Assessment</Status></FitSessionRecord>'
  [System.IO.File]::WriteAllText((Join-Path $legacyFolder 'session.xml'), $legacyXml)
  $loaded = @($repository.LoadAll() | Where-Object { $_.Id -eq $legacyId })
  if ($loaded.Count -ne 1 -or $loaded[0].Title -ne 'Legacy fit') { throw 'Legacy session migration check failed.' }

  $session = [CassetteMotionPro.Workspace.FitSessionRecord]::new()
  $session.Title = 'Current fit'
  $session.Goals = 'Comfort and stability'
  $repository.Save($session) | Out-Null
  $session.Goals = 'Updated goal'
  $repository.Save($session) | Out-Null
  $manifest = Join-Path $session.FolderPath 'session.xml'
  if (!(Test-Path ($manifest + '.bak'))) { throw 'Session backup was not created.' }
  [System.IO.File]::WriteAllText($manifest, '<broken')
  $restored = @($repository.LoadAll() | Where-Object { $_.Id -eq $session.Id })
  if ($restored.Count -ne 1 -or $restored[0].Goals -ne 'Comfort and stability') { throw 'Session backup recovery failed.' }

  # Storage folder must not move when the session date is edited after the first save.
  $keyBefore = $session.StorageFolderName
  if ([string]::IsNullOrWhiteSpace($session.StorageKey) -or $session.StorageKey -ne $keyBefore) { throw 'Storage key was not assigned on first save.' }
  $session.SessionDate = $session.SessionDate.AddDays(-9)
  if ($session.StorageFolderName -ne $keyBefore) { throw 'Editing the session date moved the storage folder.' }
  $repository.Save($session) | Out-Null
  $reloaded = @($repository.LoadAll() | Where-Object { $_.Id -eq $session.Id })
  if ($reloaded.Count -ne 1 -or $reloaded[0].StorageKey -ne $keyBefore) { throw 'Storage key did not survive a save/load round trip.' }
  if ((Split-Path $reloaded[0].FolderPath -Leaf) -ne $keyBefore) { throw 'Session folder does not match its storage key.' }

  # A draft that has never been saved must get a stable, non-pending folder name.
  $draft = [CassetteMotionPro.Workspace.FitSessionRecord]::new()
  $draft.SessionDate = [datetime]::Today
  $draft.EnsureStorageKey() | Out-Null
  $draftKey = $draft.StorageFolderName
  if ($draftKey -like '*pending*') { throw 'Draft session was given a pending storage folder.' }
  $draft.SessionDate = [datetime]::Today.AddDays(3)
  $repository.Save($draft) | Out-Null
  if ($draft.StorageFolderName -ne $draftKey -or (Split-Path $draft.FolderPath -Leaf) -ne $draftKey) { throw 'Saving a draft changed its storage folder.' }

  # Older manifests without a key keep the folder name the app already used for them.
  $legacyLoaded = @($repository.LoadAll() | Where-Object { $_.Id -eq $legacyId })
  $expectedLegacy = '2026-01-01_' + $legacyId.ToString('N').Substring(0, 8)
  if ($legacyLoaded[0].StorageKey -ne $expectedLegacy) { throw 'Legacy session did not keep its existing storage folder name.' }

  # A hostile or damaged key must never escape the session folder.
  foreach ($bad in @('..', '..\..\Windows', 'C:\temp', 'a/b', '')) {
    if ([CassetteMotionPro.Workspace.FitSessionRecord]::IsSafeStorageKey($bad)) { throw ('Unsafe storage key accepted: ' + $bad) }
  }

  # FindById returns the saved session, and nothing for an unknown id.
  if ($null -eq $repository.FindById($draft.Id)) { throw 'FindById did not return a saved session.' }
  if ($null -ne $repository.FindById([guid]::NewGuid())) { throw 'FindById returned a session for an unknown id.' }

  # A follow-up saved elsewhere (Client Manager) must survive a later save of a stale in-memory copy.
  $stale = $repository.FindById($draft.Id)
  $fresh = $repository.FindById($draft.Id)
  $entry = [CassetteMotionPro.Workspace.FitFollowUpEntry]::new()
  $entry.Id = [guid]::NewGuid()
  $entry.RiderFeedback = 'Saved from Client Manager'
  $fresh.FollowUps = [System.Collections.Generic.List[CassetteMotionPro.Workspace.FitFollowUpEntry]]::new()
  $fresh.FollowUps.Add($entry)
  $repository.Save($fresh) | Out-Null
  $stale.Notes = 'Edited in Fit Day after the follow-up was saved'
  $repository.Save($stale) | Out-Null
  $merged = $repository.FindById($draft.Id)
  if ($null -eq $merged.FollowUps -or @($merged.FollowUps | Where-Object { $_.Id -eq $entry.Id }).Count -ne 1) { throw 'A follow-up saved elsewhere was lost by a later save.' }
  if ($merged.Notes -ne 'Edited in Fit Day after the follow-up was saved') { throw 'Follow-up merge discarded the newer in-memory edit.' }
  $repository.Save($merged) | Out-Null
  if (@($repository.FindById($draft.Id).FollowUps).Count -ne 1) { throw 'Saving twice duplicated a follow-up.' }

  [FitSessionRecoveryProbe]::Run($repository.RootPath, $session)

  Write-Host 'PASS: legacy session load, current session round-trip, stable storage key, follow-up merge, manifest backup recovery, autosave backup recovery, recovery cleanup.'
}
finally {
  if (Test-Path $root) { Remove-Item $root -Recurse -Force }
}

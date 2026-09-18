$ErrorActionPreference = 'Stop'

$sources = @(
  'src/Kinovea/Clients/ClientRecord.cs',
  'src/Kinovea/Workspace/FitFollowUpEntry.cs',
  'src/Kinovea/Workspace/FitSessionRecord.cs',
  'src/Kinovea/Workspace/FitSessionRepository.cs',
  'src/Kinovea/Workspace/FitSessionRecoveryStore.cs'
)
$usings = "using System;`nusing System.IO;`nusing System.Collections.Generic;`nusing System.Linq;`nusing System.Xml.Serialization;`n"
$source = $usings + (($sources | ForEach-Object { (Get-Content $_ -Raw) -replace '(?m)^using [^;]+;\r?\n', '' }) -join "`n")
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

  $recovery = [CassetteMotionPro.Workspace.FitSessionRecoveryStore]::new($repository.RootPath)
  $recovery.Save($session, 'Report')
  $recovery.Save($session, 'Measure')
  $autosave = Join-Path $repository.RootPath '.fit-day-recovery/autosave.xml'
  [System.IO.File]::WriteAllText($autosave, '<broken')
  $draft = $null
  $section = ''
  $savedUtc = [datetime]::MinValue
  if (!$recovery.TryLoad([ref]$draft, [ref]$section, [ref]$savedUtc) -or $draft.Id -ne $session.Id) { throw 'Autosave backup recovery failed.' }
  $recovery.Clear()
  if ((Test-Path $autosave) -or (Test-Path ($autosave + '.bak'))) { throw 'Recovery cleanup failed.' }

  Write-Host 'PASS: legacy session load, current session round-trip, manifest backup recovery, autosave backup recovery, recovery cleanup.'
}
finally {
  if (Test-Path $root) { Remove-Item $root -Recurse -Force }
}

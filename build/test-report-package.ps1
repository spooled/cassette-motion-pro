# Builds a real report package and ZIP with the compiled application and checks that
# nothing fitter-only reaches the client ZIP. Run under Windows PowerShell 5.1 so the
# .NET Framework application assembly loads the same way it does in the app.
param(
    [string]$OutputPath = 'src\Kinovea\bin\x64\Release'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$exe = Join-Path $OutputPath 'CassetteMotionPro.exe'
if (-not (Test-Path $exe)) { throw "CassetteMotionPro.exe was not found in $OutputPath." }
$assembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path $exe).Path)

$internalMarker = 'INTERNAL-ONLY-MARKER-7F3A91'
$fitterNoteMarker = 'FITTER-NOTE-MARKER-2B6C40'
$clientMarker = 'CLIENT-VISIBLE-MARKER-9D15E8'

$root = Join-Path ([System.IO.Path]::GetTempPath()) ('cassette-package-' + [guid]::NewGuid().ToString('N'))
try {
    $clientFolder = Join-Path $root 'Package Client'
    [System.IO.Directory]::CreateDirectory($clientFolder) | Out-Null

    $client = New-Object ($assembly.GetType('CassetteMotionPro.Clients.ClientRecord'))
    $client.FirstName = 'Package'
    $client.LastName = 'Tester'
    $client.FolderPath = $clientFolder

    $session = New-Object ($assembly.GetType('CassetteMotionPro.Workspace.FitSessionRecord'))
    $session.Id = [guid]::NewGuid()
    $session.Title = 'Package contents check'
    $session.SessionDate = [datetime]::Today
    $session.EnsureStorageKey() | Out-Null
    $session.HandoffInternalNotes = $internalMarker
    $session.Notes = $fitterNoteMarker
    $session.HandoffClientMessage = $clientMarker

    # Studio settings normally load from the app's settings folder, which is not set up outside the
    # running app. Use default settings so the report builds the same way for a new studio.
    $settingsRepository = $assembly.GetType('CassetteMotionPro.Workspace.StudioSettingsRepository')
    $defaultSettings = [Activator]::CreateInstance($assembly.GetType('CassetteMotionPro.Workspace.StudioSettings'))
    $settingsRepository.GetField('current', [System.Reflection.BindingFlags]'NonPublic,Static').SetValue($null, $defaultSettings)

    $generator = $assembly.GetType('CassetteMotionPro.Workspace.FitSessionReportGenerator')
    # Pass the raw objects: reflection cannot convert PowerShell's wrapper objects.
    $zipPath = $generator.GetMethod('GeneratePackageZip').Invoke($null, [object[]]@($client.PSObject.BaseObject, $session.PSObject.BaseObject))
    if (-not (Test-Path $zipPath)) { throw 'GeneratePackageZip did not create a ZIP.' }

    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $names = @($zip.Entries | ForEach-Object { $_.FullName })
        if ($names -notcontains 'Bike Fit Report.html') { throw 'Client ZIP is missing Bike Fit Report.html.' }
        if ($names -notcontains 'Client Handoff Notes.txt') { throw 'Client ZIP is missing Client Handoff Notes.txt.' }

        $leaked = @($names | Where-Object { $_ -match '(?i)fitter only' })
        if ($leaked.Count -gt 0) { throw ('Client ZIP contains fitter-only entries: ' + ($leaked -join ', ')) }

        $clientNotesSeen = $false
        foreach ($entry in $zip.Entries) {
            if ($entry.Length -eq 0 -or $entry.FullName -notmatch '(?i)\.(html|txt)$') { continue }
            $reader = New-Object System.IO.StreamReader($entry.Open())
            try { $text = $reader.ReadToEnd() } finally { $reader.Dispose() }
            if ($text.Contains($internalMarker)) { throw ("Internal notes leaked into client ZIP entry '" + $entry.FullName + "'.") }
            if ($text.Contains($fitterNoteMarker)) { throw ("Fitter notes leaked into client ZIP entry '" + $entry.FullName + "'.") }
            if ($text.Contains($root)) { throw ("A local path leaked into client ZIP entry '" + $entry.FullName + "'.") }
            if ($entry.FullName -eq 'Client Handoff Notes.txt' -and $text.Contains($clientMarker)) { $clientNotesSeen = $true }
        }
        if (-not $clientNotesSeen) { throw 'Client Handoff Notes.txt did not contain the client-facing message.' }
    }
    finally {
        $zip.Dispose()
    }

    # The fitter copy must still exist on disk next to the package, with the internal notes in it.
    $internalFile = Get-ChildItem -Path $root -Recurse -Filter 'Handoff Notes - With Internal Notes.txt' | Select-Object -First 1
    if ($null -eq $internalFile) { throw 'The Fitter Only folder with internal handoff notes was not created.' }
    if (-not (Get-Content $internalFile.FullName -Raw).Contains($internalMarker)) { throw 'Fitter-only handoff notes lost the internal notes.' }

    Write-Host 'PASS: report package ZIP is client-safe and the fitter-only copy keeps the internal notes.'
}
catch {
    # Surface the real error as a GitHub annotation so a failed run explains itself.
    $detail = $_.Exception.ToString()
    if ($_.Exception.InnerException) { $detail += ' || INNER: ' + $_.Exception.InnerException.ToString() }
    $detail += ' || AT: ' + $_.InvocationInfo.PositionMessage
    Write-Host ('::error::' + ($detail -replace '\r?\n', ' | '))
    exit 1
}
finally {
    if (Test-Path $root) { Remove-Item $root -Recurse -Force }
}

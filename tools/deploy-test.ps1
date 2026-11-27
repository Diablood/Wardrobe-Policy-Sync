param(
    [string]$RimWorldDir = 'D:/SteamLibrary/steamapps/common/RimWorld',
    [string]$RimWorldModsDir = '',
    [string]$HarmonyPath = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $RimWorldModsDir) { $RimWorldModsDir = Join-Path $RimWorldDir 'Mods' }
$mods = [IO.Path]::GetFullPath($RimWorldModsDir)
$target = [IO.Path]::GetFullPath((Join-Path $mods 'Wardrobe-Policy-Sync'))
if ((Split-Path -Parent $target) -ne $mods) { throw 'Unsafe deployment target' }

function Assert-GameStopped {
    if (Get-Process -ErrorAction SilentlyContinue | Where-Object ProcessName -Match 'RimWorld') {
        throw 'Close RimWorld before deploying the test build.'
    }
}

Assert-GameStopped
if (-not (Test-Path -LiteralPath $mods -PathType Container)) { throw "Missing Mods directory: $mods" }
if (Test-Path -LiteralPath $target) {
    $metadata = New-Object System.Xml.XmlDocument
    $metadata.Load((Join-Path $target 'About/About.xml'))
    if ($metadata.ModMetaData.packageId -ne 'diablood.wardrobepolicysync') {
        throw 'Existing deployment belongs to another mod.'
    }
}

& (Join-Path $root 'build.cmd') $RimWorldDir $HarmonyPath
if ($LASTEXITCODE -ne 0) { throw 'Build failed; deployment unchanged.' }
& (Join-Path $PSScriptRoot 'check-consistency.ps1') -RimWorldDir $RimWorldDir

$stamp = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$stage = Join-Path $root "artifacts/deploy-$stamp"
$backup = $null
$installationStarted = $false
try {
    $null = New-Item -ItemType Directory -Path $stage
    foreach ($folder in @('About', 'Common', '1.6', '1.7')) {
        foreach ($file in (Get-ChildItem -LiteralPath (Join-Path $root $folder) -Recurse -File)) {
            if ($file.Extension -notin @('.dll', '.xml', '.png', '.txt')) { continue }
            $relative = $file.FullName.Substring($root.Length + 1)
            $copy = Join-Path $stage $relative
            $null = New-Item -ItemType Directory -Path (Split-Path -Parent $copy) -Force
            Copy-Item -LiteralPath $file.FullName -Destination $copy
        }
    }
    Copy-Item -LiteralPath (Join-Path $root 'LoadFolders.xml') -Destination $stage
    $publishedId = Join-Path $target 'About/PublishedFileId.txt'
    if (Test-Path -LiteralPath $publishedId) {
        Copy-Item -LiteralPath $publishedId -Destination (Join-Path $stage 'About/PublishedFileId.txt') -Force
    }

    Assert-GameStopped
    if (Test-Path -LiteralPath $target) {
        # Backups live outside Mods so RimWorld cannot load a duplicate package.
        $backupRoot = Join-Path (Split-Path -Parent $mods) 'WPS-test-backups'
        $backup = Join-Path $backupRoot $stamp
        $null = New-Item -ItemType Directory -Path $backupRoot -Force
        Move-Item -LiteralPath $target -Destination $backup
    }
    $installationStarted = $true
    Copy-Item -LiteralPath $stage -Destination $target -Recurse
    $files = @(Get-ChildItem -LiteralPath $stage -Recurse -File)
    foreach ($file in $files) {
        $copy = Join-Path $target $file.FullName.Substring($stage.Length + 1)
        if ((Get-FileHash -LiteralPath $copy).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash) {
            throw "Deployed file mismatch: $copy"
        }
    }
    if (@(Get-ChildItem -LiteralPath $target -Recurse -File).Count -ne $files.Count) {
        throw 'Unexpected files in deployed mod.'
    }
    Write-Output "Deployed and verified $($files.Count) runtime files: $target"
    if ($backup) { Write-Output "Previous installation preserved: $backup" }
} catch {
    if ($installationStarted -and (Test-Path -LiteralPath $target)) {
        if ((Split-Path -Parent ([IO.Path]::GetFullPath($target))) -ne $mods) { throw 'Unsafe rollback target' }
        Remove-Item -LiteralPath $target -Recurse -Force
    }
    if ($backup -and (Test-Path -LiteralPath $backup)) {
        Move-Item -LiteralPath $backup -Destination $target
    }
    throw
} finally {
    $artifacts = [IO.Path]::GetFullPath((Join-Path $root 'artifacts'))
    if ((Split-Path -Parent ([IO.Path]::GetFullPath($stage))) -ne $artifacts) { throw 'Unsafe staging cleanup' }
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}

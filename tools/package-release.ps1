param(
    [string]$RimWorldDir = 'D:/SteamLibrary/steamapps/common/RimWorld',
    [string]$HarmonyPath = '',
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = New-Object System.Xml.XmlDocument
$project.Load((Join-Path $root 'Source/WardrobePolicySync/WardrobePolicySync.csproj'))
$version = $project.SelectSingleNode('/Project/PropertyGroup/Version').InnerText
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid release version' }

if (-not $NoBuild) {
    & (Join-Path $root 'build.cmd') $RimWorldDir $HarmonyPath
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed' }
}
& (Join-Path $PSScriptRoot 'check-consistency.ps1') -RimWorldDir $RimWorldDir
$original = & git -C $root rev-parse '1.0.0:Assemblies/WardrobePolicySync.dll'
if ($LASTEXITCODE -ne 0) { throw 'Cannot read baseline 1.0.0 binary identity' }
$preserved = & git -C $root hash-object '1.6/Assemblies/WardrobePolicySync.dll'
if ($LASTEXITCODE -ne 0 -or $original -ne $preserved) { throw 'The preserved 1.6 binary changed' }

$output = Join-Path $root 'artifacts/releases'
$stage = Join-Path $root ('artifacts/package-' + [guid]::NewGuid().ToString('N'))
$content = Join-Path $stage 'Wardrobe-Policy-Sync'
$null = New-Item -ItemType Directory -Path $content -Force
$null = New-Item -ItemType Directory -Path $output -Force
$archive = Join-Path $output "Wardrobe-Policy-Sync-$version.zip"
$pending = Join-Path $stage 'release.zip'
try {
    foreach ($folder in @('About', 'Common', '1.6', '1.7')) {
        foreach ($file in (Get-ChildItem -LiteralPath (Join-Path $root $folder) -Recurse -File)) {
            if ($file.Extension -notin @('.dll', '.xml', '.png', '.txt')) { continue }
            $relative = $file.FullName.Substring($root.Length + 1)
            $copy = Join-Path $content $relative
            $null = New-Item -ItemType Directory -Path (Split-Path -Parent $copy) -Force
            Copy-Item -LiteralPath $file.FullName -Destination $copy
        }
    }
    foreach ($file in @('LoadFolders.xml', 'README.md', 'CHANGELOG.md')) {
        Copy-Item -LiteralPath (Join-Path $root $file) -Destination $content
    }
    [IO.File]::WriteAllText((Join-Path $content 'Version.txt'), "$version`n")
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($content, $pending, [IO.Compression.CompressionLevel]::Optimal, $true)
    $zip = [IO.Compression.ZipFile]::OpenRead($pending)
    try {
        $files = @(Get-ChildItem -LiteralPath $content -Recurse -File)
        $entries = @($zip.Entries | Where-Object { $_.Name })
        if ($entries.Count -ne $files.Count) { throw 'Archive file count mismatch' }
        foreach ($entry in $entries) {
            $path = Join-Path $stage $entry.FullName
            $stream = $entry.Open()
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $hash = ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '') }
            finally { $stream.Dispose(); $sha.Dispose() }
            if ($hash -ne (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash) {
                throw "Archive content mismatch: $($entry.FullName)"
            }
        }
    } finally { $zip.Dispose() }
    Move-Item -LiteralPath $pending -Destination $archive -Force
    $digest = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText("$archive.sha256", "$digest  $([IO.Path]::GetFileName($archive))`n")
    Write-Output "Prepared $version; verified $($files.Count) files: $archive"
    Write-Output "SHA256: $digest"
} finally {
    $artifacts = [IO.Path]::GetFullPath((Join-Path $root 'artifacts'))
    if ((Split-Path -Parent ([IO.Path]::GetFullPath($stage))) -ne $artifacts) { throw 'Unsafe staging cleanup' }
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}

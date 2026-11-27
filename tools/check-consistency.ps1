param(
    [string]$RimWorldDir = 'D:/SteamLibrary/steamapps/common/RimWorld'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

# Keep a conservative margin under Steam's 1 MB preview limit.
$preview = Get-Item -LiteralPath (Join-Path $root 'About/Preview.png')
if ($preview.Length -ge 1000000) {
    throw "Workshop preview must be under 1,000,000 bytes; got $($preview.Length)"
}

function Read-Xml([string]$path) {
    $document = New-Object System.Xml.XmlDocument
    $document.Load($path)
    return $document
}

$xmlFiles = @('About/About.xml', 'LoadFolders.xml', '1.7/Patches/HAR_OutfitStand.xml',
    'Common/Languages/English/Keyed/WardrobePolicySync.xml',
    'Common/Languages/French/Keyed/WardrobePolicySync.xml')
foreach ($file in $xmlFiles) { $null = Read-Xml (Join-Path $root $file) }

$metadata = Read-Xml (Join-Path $root 'About/About.xml')
$project = Read-Xml (Join-Path $root 'Source/WardrobePolicySync/WardrobePolicySync.csproj')
$releaseVersion = $project.SelectSingleNode('/Project/PropertyGroup/Version').InnerText
if ($metadata.ModMetaData.modVersion.InnerText -ne $releaseVersion) {
    throw 'About.xml modVersion differs from the assembly project version'
}
if ($metadata.ModMetaData.description -notmatch [regex]::Escape("Version $releaseVersion")) {
    throw 'Workshop description version differs from the project version'
}
$folders = Read-Xml (Join-Path $root 'LoadFolders.xml')
foreach ($version in @('1.6', '1.7')) {
    if (-not $metadata.SelectSingleNode("/ModMetaData/supportedVersions/li[text()='$version']")) {
        throw "Missing supported version: $version"
    }
    $paths = @($folders.SelectNodes("/loadFolders/v$version/li") | ForEach-Object InnerText)
    if (($paths -join ',') -ne "Common,$version") { throw "Unexpected load folders: $version" }
    $assemblies = @($paths | ForEach-Object {
        Get-ChildItem -LiteralPath (Join-Path $root $_) -Filter '*.dll' -Recurse
    })
    if ($assemblies.Count -ne 1 -or $assemblies[0].Name -ne 'WardrobePolicySync.dll') {
        throw "Expected exactly one WPS assembly for $version"
    }
}

$english = Read-Xml (Join-Path $root $xmlFiles[3])
$french = Read-Xml (Join-Path $root $xmlFiles[4])
$englishKeys = @($english.DocumentElement.ChildNodes | Where-Object NodeType -eq Element | ForEach-Object Name | Sort-Object)
$frenchKeys = @($french.DocumentElement.ChildNodes | Where-Object NodeType -eq Element | ForEach-Object Name | Sort-Object)
if (Compare-Object $englishKeys $frenchKeys) { throw 'English/French translation keys differ' }
foreach ($key in $englishKeys) {
    $en = @([regex]::Matches($english.DocumentElement[$key].InnerText, '\{\d+\}') | ForEach-Object Value | Sort-Object)
    $fr = @([regex]::Matches($french.DocumentElement[$key].InnerText, '\{\d+\}') | ForEach-Object Value | Sort-Object)
    if (($en -join ',') -ne ($fr -join ',')) { throw "Translation arguments differ: $key" }
}

$defs = Read-Xml (Join-Path $RimWorldDir 'Data/Odyssey/Defs/ThingDefs_Buildings/Buildings_Furniture.xml')
$patch = Read-Xml (Join-Path $root '1.7/Patches/HAR_OutfitStand.xml')
$xpath = $patch.Patch.Operation.match.xpath
$targets = $defs.SelectNodes($xpath)
if ($targets.Count -ne 1) { throw "HAR patch expected one target, got $($targets.Count)" }
$null = $targets[0].ParentNode.RemoveChild($targets[0])
if ($defs.SelectNodes($xpath).Count -ne 0) { throw 'HAR patch simulation failed' }
if (-not $defs.SelectSingleNode('Defs/ThingDef[defName="Building_KidOutfitStand"]/building/fixedStorageSettings/filter/specialFiltersToDisallow/li[text()="AllowAdultOnlyApparel"]')) {
    throw 'HAR patch changed the kid stand restriction'
}

Write-Output "OK: release $releaseVersion metadata; Workshop preview $($preview.Length) bytes; $($xmlFiles.Count) XML files; version folders; one DLL per version; $($englishKeys.Count) EN/FR keys and arguments; HAR XPath simulation."

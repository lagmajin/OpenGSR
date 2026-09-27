<#
.SYNOPSIS
Copies the shared logic package from OpenGSCore into this Unity project.

.DESCRIPTION
OpenGSR does not reference OpenGSCore directly. Unity reads
Packages\com.opengs.logic, which is a copy rather than a link, so a change in
OpenGSCore does not reach the client until something copies it. That gap is
silent: the server picks up a change, the client does not, and the mismatch only
shows up much later as a missing type or a stale rule.

This script makes the copy explicit and verifiable. It syncs the sources and
generates the .meta files Unity needs, so a file added to OpenGSCore shows up in
the client without a human remembering to copy it and its metadata.

Files that only exist in the package are left alone, because the package also
carries Unity specific files the shared repository knows nothing about.

.PARAMETER Source
The OpenGSCore repository. Defaults to the sibling directory of OpenGSR.

.PARAMETER Destination
The Unity package directory. Defaults to Packages\com.opengs.logic.

.PARAMETER Check
Report what would change and exit non zero if anything is out of date, without
writing anything. This is the mode CI uses to catch a forgotten sync.

.EXAMPLE
./tools/sync_core.ps1

.EXAMPLE
./tools/sync_core.ps1 -Check
#>
[CmdletBinding()]
param(
    [string] $Source,
    [string] $Destination,
    [switch] $Check
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $Source) {
    $Source = Join-Path (Split-Path -Parent $repoRoot) 'OpenGSCore'
}
if (-not $Destination) {
    $Destination = Join-Path $repoRoot 'Packages\com.opengs.logic'
}

$sourceRoot = (Resolve-Path $Source).Path
$destinationRoot = (Resolve-Path $Destination).Path

Write-Host "Source:      $sourceRoot"
Write-Host "Destination: $destinationRoot"

if ($sourceRoot -eq $destinationRoot) {
    throw 'Source and destination resolve to the same directory; refusing to copy onto itself.'
}

# Directories that exist only to organise the shared repository and that the
# package must not mirror. The tests are a separate Unity assembly and the build
# output is not source at all.
$excludedDirectories = @('Tests', 'obj', 'bin', '.artifacts', '.git', '.vs')
$excludedFiles = @('OpenGSCore.sln', 'Directory.Build.props', 'FodyWeavers.xml')

function Test-Excluded {
    param([string] $RelativePath)

    $segments = $RelativePath -split '[\\/]'
    foreach ($excluded in $excludedDirectories) {
        if ($segments -contains $excluded) {
            return $true
        }
    }

    $leaf = Split-Path -Leaf $RelativePath
    return $excludedFiles -contains $leaf
}

# Unity identifies every asset by the guid in its .meta file. A copied file with
# no .meta gets one generated on the next import, but the meta has to be written
# here so the sync is complete and a fresh checkout does not churn.
function New-MetaContent {
    param([string] $RelativePath, [string] $Kind)

    $seed = "$($Kind)|$RelativePath|OpenGS"
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($seed)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $hash = $sha.ComputeHash($bytes)

    # A Unity guid is 32 lowercase hex characters.
    $guid = -join ($hash[0..15] | ForEach-Object { $_.ToString('x2') })

    if ($Kind -eq 'folder') {
        $body = "fileFormatVersion: 2`nguid: $guid`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
    }
    else {
        $body = "fileFormatVersion: 2`nguid: $guid`nMonoImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  defaultReferences: []`n  executionOrder: 0`n  icon: {instanceID: 0}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
    }

    return $body
}

$copied = 0
$metasCreated = 0
$outOfDate = New-Object System.Collections.Generic.List[string]
$orphans = New-Object System.Collections.Generic.List[string]

Get-ChildItem -Path $sourceRoot -Recurse -File -Filter '*.cs' | ForEach-Object {
    $relative = $_.FullName.Substring($sourceRoot.Length).TrimStart('\', '/')
    if (Test-Excluded $relative) {
        return
    }

    $target = Join-Path $destinationRoot $relative
    $exists = Test-Path $target
    $differs = $exists -and ((Get-FileHash $_.FullName).Hash -ne (Get-FileHash $target).Hash)

    if (-not $exists) {
        $orphans.Add($relative)
    }

    if (-not $exists -or $differs) {
        $outOfDate.Add($relative)
        if (-not $Check) {
            $targetDir = Split-Path -Parent $target
            if (-not (Test-Path $targetDir)) {
                New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
            }
            Copy-Item $_.FullName $target -Force
            $copied++
        }
    }

    $meta = "$target.meta"
    if (-not (Test-Path $meta) -and -not $Check) {
        [System.IO.File]::WriteAllText($meta, (New-MetaContent $relative 'file'))
        $metasCreated++
    }
}

# A directory that exists in the source but not yet in the package needs a folder
# meta too, otherwise Unity treats it as an empty folder with no identity.
Get-ChildItem -Path $sourceRoot -Recurse -Directory | ForEach-Object {
    $relative = $_.FullName.Substring($sourceRoot.Length).TrimStart('\', '/')
    if (Test-Excluded $relative) {
        return
    }

    $targetDir = Join-Path $destinationRoot $relative
    if (-not (Test-Path $targetDir) -and -not $Check) {
        New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    }

    $meta = "$targetDir.meta"
    if ((Test-Path $targetDir) -and -not (Test-Path $meta) -and -not $Check) {
        [System.IO.File]::WriteAllText($meta, (New-MetaContent $relative 'folder'))
        $metasCreated++
    }
}

Write-Host ''
if ($outOfDate.Count -eq 0) {
    Write-Host 'Package is up to date with OpenGSCore.'
    exit 0
}

Write-Host "Out of date: $($outOfDate.Count)"
foreach ($item in ($outOfDate | Select-Object -First 40)) {
    Write-Host "  $item"
}
if ($outOfDate.Count -gt 40) {
    Write-Host "  ... and $($outOfDate.Count - 40) more"
}

if ($Check) {
    Write-Host ''
    Write-Host 'The shared package is behind OpenGSCore. Run this without -Check to sync.'
    exit 1
}

Write-Host "Copied: $copied, meta files created: $metasCreated"
if ($orphans.Count -gt 0) {
    Write-Host "Files the package had never seen: $($orphans.Count)"
    foreach ($item in ($orphans | Select-Object -First 20)) {
        Write-Host "  $item"
    }
}

Write-Host ''
Write-Host 'Package synced from OpenGSCore.'
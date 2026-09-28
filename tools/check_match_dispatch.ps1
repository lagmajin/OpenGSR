<#
.SYNOPSIS
Checks that no match script takes a message the base match script handles.

.DESCRIPTION
The base match script reads the messages a match is made of: the room state, the
end of the match, the rulings on a claim, a drop, a spend. A mode that handles
one of them takes it, and then the base never sees it for that mode, and
everything else the base does with that message quietly stops happening there.

That is not a hypothetical. The flag mode had its own case for the room state,
so the clock, the health multiplier and the running score were all missing from
it, which is the whole of what the base reads that message for. The death match
had two such cases, one of them doing exactly what the base does. The survival
script had a copy of the handler that took a message and did nothing with it.

This finds those, and finds a mode that overrides the handler without ever
delegating to the base, which is the other way a message stops being read. A mode
is allowed to handle a message the base does not, which is where its own rules
live.

.EXAMPLE
./tools/check_match_dispatch.ps1
#>
[CmdletBinding()]
param(
    [string] $MatchScriptFolder = 'Assets/Scripts/Match',
    [string] $BaseScript = 'AbstractMatchMainScript.cs'
)

$ErrorActionPreference = 'Stop'

$folder = Join-Path (Split-Path -Parent $PSScriptRoot) $MatchScriptFolder
$basePath = Join-Path $folder $BaseScript
if (-not (Test-Path $basePath)) {
    throw "The base match script is not at $basePath"
}

# The cases inside the base's own handler, which is where the shared messages
# are dispatched from.
$baseSource = Get-Content -LiteralPath $basePath -Raw
$handlerStart = $baseSource.IndexOf('protected virtual void OnNetworkDataRecved')
if ($handlerStart -lt 0) {
    throw 'The base match script has no OnNetworkDataRecved to read messages in.'
}
$handler = $baseSource.Substring($handlerStart, 6000)
$baseCases = [regex]::Matches($handler, 'case ([A-Za-z0-9_.]+):') |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique

Write-Host "The base handles $($baseCases.Count) message types."

$problems = New-Object System.Collections.Generic.List[string]

foreach ($file in Get-ChildItem -Path $folder -Filter '*.cs' | Where-Object { $_.Name -ne $BaseScript }) {
    $source = Get-Content -LiteralPath $file.FullName -Raw
    if ($source -notmatch 'OnNetworkDataRecved') { continue }

    $start = $source.IndexOf('OnNetworkDataRecved')
    $body = $source.Substring($start, [Math]::Min(6000, $source.Length - $start))

    if ($body -notmatch 'base\.OnNetworkDataRecved') {
        $problems.Add("$($file.Name) overrides the handler and never delegates to the base, so a message it does not name is not read at all.")
        continue
    }

    $fallthrough = $body.IndexOf('default:')
    if ($fallthrough -lt 0) { $fallthrough = $body.Length }

    $own = [regex]::Matches($body.Substring(0, $fallthrough), 'case ([A-Za-z0-9_.]+):') |
        ForEach-Object { $_.Groups[1].Value } |
        Sort-Object -Unique

    foreach ($shadowed in $own | Where-Object { $baseCases -contains $_ }) {
        $problems.Add("$($file.Name) handles '$shadowed', which the base also handles, so the base never sees it for that mode.")
    }
}

Write-Host ''
if ($problems.Count -gt 0) {
    foreach ($problem in $problems) {
        Write-Host "  $problem"
    }
    Write-Host ''
    Write-Host "A mode is stopping a message from being read. Run this in CI to keep it from being missed."
    exit 1
}

Write-Host 'Every match script leaves the shared messages to the base.'
exit 0

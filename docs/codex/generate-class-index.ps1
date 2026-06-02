param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$OutputPath = (Join-Path $PSScriptRoot "class-index.md")
)

$sourceRoots = @(
    (Join-Path $Root "Assets\Scripts"),
    (Join-Path $Root "Assets\Editor")
)

$items = foreach ($sourceRoot in $sourceRoots) {
    if (-not (Test-Path $sourceRoot)) {
        continue
    }

    Get-ChildItem -Path $sourceRoot -Recurse -Filter *.cs | ForEach-Object {
        $relativePath = $_.FullName.Substring($Root.Length + 1)
        $content = Get-Content $_.FullName
        foreach ($line in $content) {
            if ($line -match '^\s*(?:public|internal|private|protected|sealed|static|partial|\s)*?(class|interface|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)') {
                [pscustomobject]@{
                    Kind = $matches[1]
                    Name = $matches[2]
                    Path = $relativePath
                }
            }
        }
    }
}

$items = $items | Sort-Object Path, Name, Kind -Unique

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Class Index")
$lines.Add("")
$lines.Add("This index is generated from the current `Assets/Scripts` and `Assets/Editor` source tree.")
$lines.Add("It includes `class`, `interface`, `struct`, and `enum` declarations so the repo can be searched from one place.")
$lines.Add("")

foreach ($group in ($items | Group-Object { Split-Path $_.Path -Parent } | Sort-Object Name)) {
    $lines.Add("## $($group.Name)")
    $lines.Add("")
    $lines.Add("| Kind | Name | File |")
    $lines.Add("| --- | --- | --- |")

    foreach ($item in ($group.Group | Sort-Object Name, Kind)) {
        $lines.Add("| ``" + $item.Kind + "`` | ``" + $item.Name + "`` | ``" + ($item.Path.Replace('\', '/')) + "`` |")
    }

    $lines.Add("")
}

Set-Content -Path $OutputPath -Value $lines -Encoding UTF8

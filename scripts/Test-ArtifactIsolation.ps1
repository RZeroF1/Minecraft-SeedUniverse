param(
    [string] $PackageDirectory,
    [string[]] $OutputDirectory = @(),
    [switch] $CheckTracked
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$privatePattern = '(?i)do[ _-]?not[ _-]?submit|(^|/)(agents?\.md|internal[^/]*|research|investigations|screenshots|test-samples|test-results|temp|tmp)(/|$)|\.(zip|7z|tmp|bak)$'
$publicDocs = @('docs/api.md', 'docs/architecture.md', 'docs/nuget.md')
function Assert-PublicPath([string] $path) {
    $path = $path.Replace('\', '/')
    if ($path -match $privatePattern -or ($path -match '^docs/' -and $path -notin $publicDocs)) {
        throw "Private or unreviewed file: $path"
    }
}
if ($CheckTracked) {
    $tracked = @(git -C $root ls-files)
    if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed.' }
    foreach ($path in $tracked) { Assert-PublicPath $path }
    Write-Host "Checked $($tracked.Count) tracked paths."
}
if ($PackageDirectory) {
    Add-Type -AssemblyName System.IO.Compression
    $packages = @(Get-ChildItem -LiteralPath $PackageDirectory -File | Where-Object { $_.Extension -in '.nupkg', '.snupkg' })
    if (-not $packages.Count) { throw 'No packages found.' }
    $payload = @(
        'lib/net10.0/RZeroF1.Minecraft.SeedMap.dll',
        'lib/net10.0/RZeroF1.Minecraft.SeedMap.xml',
        'runtimes/win-x64/native/rzero_seedmap.dll',
        'README.md', 'README.en.md', 'LICENSE', 'THIRD-PARTY-NOTICES.md',
        'docs/api.md', 'docs/architecture.md', 'docs/nuget.md',
        'licenses/cubiomes/LICENSE', 'licenses/cubiomes/SOURCE.md', 'licenses/cubiomes/manifest.json'
    )
    foreach ($package in $packages) {
        $zip = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
        try {
            $entries = @($zip.Entries | Where-Object { $_.Name } | ForEach-Object { [Uri]::UnescapeDataString($_.FullName).Replace('\', '/') })
            $required = if ($package.Extension -eq '.snupkg') { @('lib/net10.0/RZeroF1.Minecraft.SeedMap.pdb') } else { $payload }
            foreach ($entry in $entries) {
                Assert-PublicPath $entry
                $metadata = $entry -match '^(_rels/\.rels|\[Content_Types\]\.xml|RZeroF1\.Minecraft\.SeedMap\.nuspec|package/services/metadata/core-properties/[^/]+\.psmdcp|\.signature\.p7s)$'
                if ($entry -notin $required -and -not $metadata) { throw "Unexpected package entry in $($package.Name): $entry" }
            }
            foreach ($entry in $required) {
                if ($entry -notin $entries) { throw "Missing required entry in $($package.Name): $entry" }
            }
            Write-Host "$($package.FullName):"
            $entries | ForEach-Object { Write-Host "  $_" }
        }
        finally { $zip.Dispose() }
    }
}
foreach ($directory in $OutputDirectory) {
    $resolved = (Resolve-Path -LiteralPath $directory).Path
    $files = @(Get-ChildItem -LiteralPath $resolved -Recurse -File)
    foreach ($file in $files) {
        $relative = [IO.Path]::GetRelativePath($resolved, $file.FullName)
        Assert-PublicPath $relative
        Write-Host "  $relative"
    }
    Write-Host "Checked $($files.Count) output files in $resolved"
}

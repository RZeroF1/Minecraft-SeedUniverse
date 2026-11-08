param(
    [string] $PackagePath,
    [string] $PackageDirectory,
    [string] $PackageVersion,
    [string[]] $OutputDirectory = @(),
    [switch] $CheckTracked
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$privatePattern = '(?i)do[ _-]?not[ _-]?submit|(^|/)(agents?\.md|internal[^/]*|artifacts|tests?|logs|research|investigations|screenshots|test-samples|test-results|temp|tmp)(/|$)|\.(jar|class|zip|7z|tmp|bak)$'
$publicDocs = @('docs/api.md', 'docs/architecture.md', 'docs/nuget.md', 'docs/versions.md')
function Assert-PublicPath([string] $path, [switch] $Source) {
    $path = $path.Replace('\', '/')
    if ($path -match $privatePattern -or ($Source -and $path -match '(?i)\.obj$') -or ($path -match '^docs/' -and $path -notin $publicDocs)) {
        throw 'A private or unreviewed file was detected; its path is omitted from this log.'
    }
}
if ($CheckTracked) {
    $tracked = @(git -C $root ls-files)
    if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed.' }
    foreach ($path in $tracked) { Assert-PublicPath $path -Source }
    $localPathPattern = '(?i)(?<![a-z0-9])[a-z]:[\\/]|(?<!\\)\\\\[a-z0-9_.-]+\\[a-z0-9_$.-]+|\bfile:[/]{2}'
    git -C $root grep --cached -I -q -P -- $localPathPattern
    if ($LASTEXITCODE -eq 0) {
        throw 'A local absolute path was detected in staged content; details are omitted from this log.'
    }
    if ($LASTEXITCODE -ne 1) { throw 'Staged content privacy check failed.' }
    Write-Host "Checked $($tracked.Count) tracked paths."
}
if ($PackagePath -and $PackageDirectory) {
    throw 'Specify only one of -PackagePath or -PackageDirectory.'
}
$packageInput = if ($PackagePath) { $PackagePath } else { $PackageDirectory }
if ($packageInput) {
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $inputItem = Get-Item -LiteralPath $packageInput
    if ($inputItem.PSIsContainer) {
        if (-not $PackageVersion) {
            $projectFile = Join-Path $root 'src/RZeroF1.Minecraft.SeedMap.Core/RZeroF1.Minecraft.SeedMap.Core.csproj'
            if (-not (Test-Path -LiteralPath $projectFile)) {
                throw 'Package directory input requires -PackageVersion when the SDK project file is unavailable.'
            }
            $projectText = Get-Content -LiteralPath $projectFile -Raw
            $versionMatch = [regex]::Match(
                $projectText,
                '(?s)<PackageVersion\b[^>]*>\s*(?<version>[^<\s]+)\s*</PackageVersion>')
            if (-not $versionMatch.Success) {
                throw 'Package directory input requires -PackageVersion because PackageVersion was not found.'
            }
            $PackageVersion = $versionMatch.Groups['version'].Value
        }

        $packagePrefix = 'RZeroF1.Minecraft.SeedMap.'
        $packagePattern = '^' + [regex]::Escape($packagePrefix) +
            [regex]::Escape($PackageVersion) + '\.(nupkg|snupkg)$'
        $packages = @(Get-ChildItem -LiteralPath $inputItem.FullName -File |
            Where-Object { $_.Name -match $packagePattern })
        if (-not $packages.Count) {
            throw "No package for current SDK version was found in the supplied directory."
        }
        Write-Host "Selected package version $PackageVersion."
    }
    else {
        if ($inputItem.Extension -notin '.nupkg', '.snupkg') {
            throw 'PackagePath must point to a .nupkg, .snupkg, or a package output directory.'
        }
        $packages = @($inputItem)
    }

    if (-not $packages.Count) { throw 'No packages found.' }
    $payload = @(
        'lib/net10.0/RZeroF1.Minecraft.SeedMap.dll',
        'lib/net10.0/RZeroF1.Minecraft.SeedMap.xml',
        'runtimes/win-x64/native/rzero_seedmap.dll',
        'README.md', 'README.en.md', 'LICENSE', 'THIRD-PARTY-NOTICES.md',
        'docs/api.md', 'docs/architecture.md', 'docs/nuget.md', 'docs/versions.md',
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
                if ($entry -notin $required -and -not $metadata) { throw 'An unexpected package entry was detected; its path is omitted from this log.' }
            }
            foreach ($entry in $required) {
                if ($entry -notin $entries) { throw "Missing required entry in $($package.Name): $entry" }
            }
            Write-Host "$($package.Name):"
            $entries | ForEach-Object { Write-Host "  $_" }
        }
        finally { $zip.Dispose() }
    }
}
foreach ($directory in $OutputDirectory) {
    $resolved = (Resolve-Path -LiteralPath $directory).Path
    $prefix = $resolved.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $files = @(Get-ChildItem -LiteralPath $resolved -Recurse -File)
    foreach ($file in $files) {
        # Windows PowerShell 5 does not provide [IO.Path]::GetRelativePath.
        $relative = $file.FullName.Substring($prefix.Length)
        Assert-PublicPath $relative
        Write-Host "  $relative"
    }
    Write-Host "Checked $($files.Count) output files."
}
exit 0

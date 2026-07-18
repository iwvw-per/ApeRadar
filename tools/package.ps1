param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$SoftwareDate = (Get-Date -Format "yyyyMMdd"),
    [string]$PackageSuffix = "",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$ProjectPath = Join-Path $RepoRoot "ApeRadar\ApeRadar.csproj"
$ArtifactsRoot = Join-Path $RepoRoot "artifacts"
$PublishDir = Join-Path $ArtifactsRoot "publish\$Configuration-$Runtime"
$StageRoot = Join-Path $ArtifactsRoot "staging"

[xml]$ProjectXml = Get-Content -LiteralPath $ProjectPath
$Version = ($ProjectXml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Version not found in $ProjectPath"
}

$normalizedSuffix = $PackageSuffix.Trim().TrimStart("-")
$PackageName = "ApeRadar-$Version-Build-$SoftwareDate-$Runtime"
if (-not [string]::IsNullOrWhiteSpace($normalizedSuffix)) {
    $PackageName += "-$normalizedSuffix"
}
$PackageRoot = Join-Path $StageRoot $PackageName
$ZipPath = Join-Path $ArtifactsRoot "$PackageName.zip"

function Assert-UnderPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Root
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($Root)
    if (-not $fullRoot.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $fullRoot += [System.IO.Path]::DirectorySeparatorChar
    }

    if (-not $fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
        -not $fullPath.Equals($fullRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar), [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside $fullRoot : $fullPath"
    }
}

function Remove-DirectoryIfExists {
    param([Parameter(Mandatory = $true)][string]$Path)

    Assert-UnderPath -Path $Path -Root $ArtifactsRoot
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Copy-Directory {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        return
    }

    Assert-UnderPath -Path $Destination -Root $PackageRoot
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | Copy-Item -Destination $Destination -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $ArtifactsRoot | Out-Null
Remove-DirectoryIfExists -Path $PublishDir
Remove-DirectoryIfExists -Path $PackageRoot
if (Test-Path -LiteralPath $ZipPath) {
    Assert-UnderPath -Path $ZipPath -Root $ArtifactsRoot
    Remove-Item -LiteralPath $ZipPath -Force
}

$selfContainedValue = $SelfContained.IsPresent.ToString().ToLowerInvariant()
dotnet publish $ProjectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained:$selfContainedValue `
    -o $PublishDir `
    /p:ApeRadarSoftwareDate=$SoftwareDate

New-Item -ItemType Directory -Force -Path $PackageRoot | Out-Null

$allowedTopLevelExtensions = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
@(".exe", ".dll", ".json", ".config", ".txt") | ForEach-Object { [void]$allowedTopLevelExtensions.Add($_) }

$forbiddenTopLevelFiles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
@("WatchList.json", "placement.config") | ForEach-Object { [void]$forbiddenTopLevelFiles.Add($_) }

$forbiddenDirectories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
@("Log", "Screenshot", "Download") | ForEach-Object { [void]$forbiddenDirectories.Add($_) }

Get-ChildItem -LiteralPath $PublishDir -File | ForEach-Object {
    if ($forbiddenTopLevelFiles.Contains($_.Name)) {
        return
    }

    if ($allowedTopLevelExtensions.Contains($_.Extension)) {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $PackageRoot $_.Name) -Force
    }
}

Get-ChildItem -LiteralPath $PublishDir -Directory | ForEach-Object {
    if ($forbiddenDirectories.Contains($_.Name)) {
        return
    }

    if ($_.Name -eq "Resources" -or $_.Name -eq "runtimes") {
        Copy-Directory -Source $_.FullName -Destination (Join-Path $PackageRoot $_.Name)
    }
}

$requiredFiles = @(
    "ApeRadar.exe",
    "README.txt",
    "LICENSE.txt",
    "log4net.config",
    "Resources\Json\ships.json"
)

foreach ($requiredFile in $requiredFiles) {
    $path = Join-Path $PackageRoot $requiredFile
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required package file is missing: $requiredFile"
    }
}

$forbiddenRelativePaths = @(
    "WatchList.json",
    "placement.config",
    "Log\Log.txt",
    "Screenshot",
    "Download"
)

foreach ($forbiddenRelativePath in $forbiddenRelativePaths) {
    if (Test-Path -LiteralPath (Join-Path $PackageRoot $forbiddenRelativePath)) {
        throw "Forbidden runtime file entered package: $forbiddenRelativePath"
    }
}

$dllBytes = [System.IO.File]::ReadAllBytes((Join-Path $PackageRoot "ApeRadar.dll"))
$dllUtf8 = [System.Text.Encoding]::UTF8.GetString($dllBytes)
$dllUnicode = [System.Text.Encoding]::Unicode.GetString($dllBytes)
if (-not ($dllUtf8.Contains($SoftwareDate) -or $dllUnicode.Contains($SoftwareDate))) {
    throw "Build date $SoftwareDate was not found in ApeRadar.dll"
}

Compress-Archive -Path (Join-Path $PackageRoot "*") -DestinationPath $ZipPath -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
try {
    $entries = $zip.Entries | ForEach-Object { $_.FullName.Replace("/", "\") }
    foreach ($requiredFile in $requiredFiles) {
        if (-not ($entries -contains $requiredFile)) {
            throw "Zip missing required file: $requiredFile"
        }
    }

    foreach ($entry in $entries) {
        if ($entry -match '(^|\\)(Log|Screenshot|Download)(\\|$)' -or
            $entry -match '(^|\\)(WatchList\.json|placement\.config)$') {
            throw "Zip contains forbidden runtime entry: $entry"
        }
    }
}
finally {
    $zip.Dispose()
}

Write-Host "Package created: $ZipPath"

<#PSScriptInfo
.VERSION 1.0.0
.GUID 00000000-0000-0000-0000-000000000000
.AUTHOR HermesProxy Team
.DESCRIPTION Build script for WotLK 3.4.3 runtime packaging
#>

[CmdletBinding()]
param (
    [string]$OutputDirectory = "$(Join-Path $PSScriptRoot "..\artifacts\Hermes-WotLK343-patches-1-8-rebuilt-$(Get-Date -Format 'yyyyMMdd')")"
)

# Resolve paths
$ScriptDir = $PSScriptRoot
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")
$ProjectPath = Join-Path $RepoRoot "HermesProxy\HermesProxy.csproj"
$SourceCsvPath = Join-Path $RepoRoot "HermesProxy\CSV"

# Create output directory
if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

Write-Host "Building HermesProxy runtime package..."
Write-Host "Output directory: $OutputDirectory"

# Step 1: Restore (without publish properties to avoid NETSDK1124 on SourceGen)
Write-Host "Restoring project..."
& dotnet restore $ProjectPath -r win-x64
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE"
}

# Step 2: Publish (with --no-restore to use already-restored projects)
Write-Host "Publishing project..."
& dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true -p:PublishTrimmed=true --no-restore -o $OutputDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

# Step 3: Copy CSV files (publish doesn't copy content files by default when using ExcludeFromSingleFile)
# The CSV files are marked as Content with CopyToOutputDirectory in the project
# But we need to ensure they're in the output directory
$PublishCsvPath = Join-Path $OutputDirectory "CSV"
if (-not (Test-Path $PublishCsvPath)) {
    # If CSV wasn't copied by publish, copy it from source
    Write-Host "Copying CSV files to output..."
    Copy-Item -Path $SourceCsvPath -Destination $PublishCsvPath -Recurse -Force
}

# Step 4: Get all files in output directory (excluding runtime-lock.json itself)
Write-Host "Computing file hashes..."
$Files = Get-ChildItem -Path $OutputDirectory -File -Recurse | Where-Object { $_.Name -ne "runtime-lock.json" }

$FileHashes = @{}
foreach ($File in $Files) {
    $RelativePath = $File.FullName.Substring($OutputDirectory.Length + 1) -replace '\\', '/'
    $Hash = (Get-FileHash -Path $File.FullName -Algorithm SHA256).Hash.ToLower()
    $FileHashes[$RelativePath] = $Hash
    Write-Verbose "Hash for $RelativePath : $Hash"
}

# Step 5: Create runtime-lock.json
$RuntimeLock = @{
    SchemaVersion = 2
    SourceCommit = (git rev-parse HEAD)
    IntegrationPatchSetId = "wotlk343-patches-1-8-20260821"
    BuildConfiguration = "Release|win-x64|self-contained|trimmed"
    ModernBuild = "V3_4_3_54261"
    LegacyBuild = "V3_3_5a_12340"
    Capabilities = @("CloseInteraction", "SpellExecuteLog", "PartialValues", "TelemetryV1")
    CreatedUtc = (Get-Date -Format "o")
    Files = $FileHashes
}

$RuntimeLockPath = Join-Path $OutputDirectory "runtime-lock.json"
$RuntimeLock | ConvertTo-Json -Depth 10 | Out-File -FilePath $RuntimeLockPath -Encoding UTF8

Write-Host "Runtime package built successfully!"
Write-Host "Output: $OutputDirectory"
Write-Host "Runtime lock: $RuntimeLockPath"
Write-Host "Total files: $($FileHashes.Count)"

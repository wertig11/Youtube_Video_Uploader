# PowerShell script to package the compiled application and install.ps1 into a release setup zip
# Usage: Run with 'powershell -ExecutionPolicy Bypass -File .\package_installer.ps1'

$ErrorActionPreference = "Stop"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "   YouTube Video Uploader - Package Builder  " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Define paths
$buildsDir = Join-Path $PSScriptRoot "Builds"
$versionDir = Join-Path $buildsDir "v2.0.1"
$installerScript = Join-Path $PSScriptRoot "install.ps1"
$outputZip = Join-Path $buildsDir "YouTubeVideoUploader_v2.0.1_Setup.zip"

# Check if build directory exists
if (-not (Test-Path $versionDir)) {
    Write-Error "Build directory $versionDir does not exist. Please run 'dotnet publish' first."
}

# Check if installer script exists
if (-not (Test-Path $installerScript)) {
    Write-Error "Installer script $installerScript not found at the root directory."
}

# Remove existing zip package if it exists
if (Test-Path $outputZip) {
    Remove-Item -Path $outputZip -Force
    Write-Host "Removed existing zip file: $outputZip" -ForegroundColor Gray
}

# Create temp directory for compression to package installer and files together
$tempDir = Join-Path $buildsDir "TempSetupPackage"
if (Test-Path $tempDir) {
    Remove-Item -Path $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

# Copy publish files into subfolder
$appFilesDest = Join-Path $tempDir "Builds\v2.0.1"
New-Item -ItemType Directory -Path $appFilesDest | Out-Null
Copy-Item -Path "$versionDir\*" -Destination $appFilesDest -Force

# Copy installer script to root of package
Copy-Item -Path $installerScript -Destination $tempDir -Force

Write-Host "Packaging files..." -ForegroundColor Yellow

# Compress the temp folder content to zip
Compress-Archive -Path "$tempDir\*" -DestinationPath $outputZip -Force

# Clean up temp files
Remove-Item -Path $tempDir -Recurse -Force

Write-Host "Package created successfully!" -ForegroundColor Green
Write-Host "Output path: $outputZip" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

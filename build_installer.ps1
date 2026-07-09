# PowerShell script to package the compiled application and compile the standalone GUI installer exe
# Usage: Run with 'powershell -ExecutionPolicy Bypass -File .\build_installer.ps1'

$ErrorActionPreference = "Stop"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "   YouTube Video Uploader - GUI Installer Build" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Define paths
$buildsDir = Join-Path $PSScriptRoot "Builds"
$versionDir = Join-Path $buildsDir "v2.0.1"
$installerProjectDir = Join-Path $PSScriptRoot "src\YouTubeVideoUploader.Installer"
$zipDest = Join-Path $installerProjectDir "app.zip"
$finalInstallerExe = Join-Path $buildsDir "YouTubeVideoUploader_v2.0.1_Setup.exe"

# Check if build directory exists
if (-not (Test-Path $versionDir)) {
    Write-Error "Build directory $versionDir does not exist. Please run 'dotnet publish' first."
}

# Remove existing zip package in the installer directory if it exists
if (Test-Path $zipDest) {
    Remove-Item -Path $zipDest -Force
}

# Create temp directory for compression to filter out PDB files
$tempDir = Join-Path $buildsDir "TempZipPayload"
if (Test-Path $tempDir) {
    Remove-Item -Path $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

# Copy publish files (excluding PDB files)
Write-Host "Filtering and copying payload files..." -ForegroundColor Yellow
Copy-Item -Path "$versionDir\*" -Destination $tempDir -Exclude "*.pdb" -Force

# Compress the temp folder content into app.zip inside the installer folder
Write-Host "Compressing payload into app.zip..." -ForegroundColor Yellow
Compress-Archive -Path "$tempDir\*" -DestinationPath $zipDest -Force

# Clean up temp files
Remove-Item -Path $tempDir -Recurse -Force

Write-Host "app.zip created inside the installer project." -ForegroundColor Green

# Remove existing installer exe if it exists
if (Test-Path $finalInstallerExe) {
    Remove-Item -Path $finalInstallerExe -Force
}

# Compile and publish the installer project as a single-file self-contained executable
Write-Host "Compiling the C# GUI Installer..." -ForegroundColor Yellow

dotnet publish "$installerProjectDir\YouTubeVideoUploader.Installer.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o "$buildsDir\InstallerOutput"

# Move the published installer exe to the main Builds directory with a clean name
$compiledExe = Join-Path "$buildsDir\InstallerOutput" "YouTubeVideoUploader.Installer.exe"
if (Test-Path $compiledExe) {
    Move-Item -Path $compiledExe -Destination $finalInstallerExe -Force
    Write-Host "GUI Setup Installer created successfully!" -ForegroundColor Green
    Write-Host "Output path: $finalInstallerExe" -ForegroundColor Cyan
} else {
    Write-Error "Failed to locate compiled installer executable at $compiledExe."
}

# Clean up temporary installer output folder
if (Test-Path "$buildsDir\InstallerOutput") {
    Remove-Item -Path "$buildsDir\InstallerOutput" -Recurse -Force
}

# Clean up the embedded app.zip from the src directory to keep workspace clean
if (Test-Path $zipDest) {
    Remove-Item -Path $zipDest -Force
}

Write-Host "Cleaned up temporary workspace files." -ForegroundColor Gray
Write-Host "=============================================" -ForegroundColor Cyan

# PowerShell Installer script for YouTube Video Uploader
# Usage: Run with 'powershell -ExecutionPolicy Bypass -File .\install.ps1'

$ErrorActionPreference = "Stop"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "   YouTube Video Uploader - Setup Installer  " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# 1. Locate the latest build executable
$buildsDir = Join-Path $PSScriptRoot "Builds"
if (-not (Test-Path $buildsDir)) {
    Write-Error "Builds folder not found. Please compile the application first (e.g. dotnet publish)."
}

# Find latest versioned subfolder or default exe in Builds
$exeFile = Get-ChildItem -Path $buildsDir -Filter "YouTubeVideoUploader.UI.exe" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $exeFile) {
    Write-Error "YouTubeVideoUploader.UI.exe was not found inside the Builds directory."
}

Write-Host "Located build executable: $($exeFile.FullName)" -ForegroundColor Green

# 2. Select Installation Path
$defaultInstallPath = Join-Path $env:LOCALAPPDATA "Programs\YouTubeVideoUploader"
Write-Host "Default installation directory: $defaultInstallPath"
$userInputPath = Read-Host "Press Enter to install to default, or type a custom path"

$installPath = $defaultInstallPath
if (-not [string]::IsNullOrWhiteSpace($userInputPath)) {
    $installPath = $userInputPath
}

# Create installation directory
if (-not (Test-Path $installPath)) {
    New-Item -ItemType Directory -Path $installPath -Force | Out-Null
    Write-Host "Created folder: $installPath" -ForegroundColor Gray
}

# 3. Copy files to destination
Write-Host "Copying files to installation directory..." -ForegroundColor Yellow
$sourceDir = $exeFile.Directory.FullName
Get-ChildItem -Path $sourceDir | ForEach-Object {
    $destFile = Join-Path $installPath $_.Name
    Copy-Item -Path $_.FullName -Destination $destFile -Force
}
Write-Host "Files copied successfully!" -ForegroundColor Green

# 4. Optional Desktop Shortcut Creation
Write-Host ""
$shortcutPrompt = Read-Host "Would you like to create a Desktop shortcut? (Y/N)"
if ($shortcutPrompt -match "^[yY](es)?$") {
    Write-Host "Creating Desktop shortcut..." -ForegroundColor Yellow
    
    $desktopPath = [System.Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktopPath "YouTube Video Uploader.lnk"
    
    $wshell = New-Object -ComObject WScript.Shell
    $shortcut = $wshell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = Join-Path $installPath "YouTubeVideoUploader.UI.exe"
    $shortcut.WorkingDirectory = $installPath
    $shortcut.Description = "YouTube Video Uploader Application"
    $shortcut.Save()
    
    Write-Host "Desktop shortcut created successfully!" -ForegroundColor Green
} else {
    Write-Host "Skipped creating desktop shortcut." -ForegroundColor Gray
}

Write-Host ""
Write-Host "Installation completed successfully!" -ForegroundColor Green
Write-Host "You can run the application from: $(Join-Path $installPath 'YouTubeVideoUploader.UI.exe')" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

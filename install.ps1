# Install Script for YoutubeMaxxer

$ErrorActionPreference = "Stop"

Write-Host "Starting installation of YoutubeMaxxer..." -ForegroundColor Cyan

# 1. Check for .NET
$dotnetVersion = "10.0"
Write-Host "Checking for .NET $dotnetVersion..."
if (!(Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Error ".NET is not installed. Please install .NET $dotnetVersion from https://dotnet.microsoft.com/"
    exit
}

# 2. Define paths
$sourceDir = $PSScriptRoot
$installDir = Join-Path $env:ProgramFiles "YoutubeMaxxer"

Write-Host "Installing to $installDir..."

# 3. Copy files
if (!(Test-Path $installDir)) {
    New-Item -Path $installDir -ItemType Directory
}

# Copy everything except the installer itself and git/obj/bin folders
$itemsToCopy = Get-ChildItem $sourceDir -Exclude @("install.ps1", "INSTALL.md", ".git", "obj", "bin", "TestApp")
foreach ($item in $itemsToCopy) {
    Copy-Item -Path $item.FullName -Destination $installDir -Recurse -Force
}

Write-Host "Installation complete!" -ForegroundColor Green
Write-Host "You can run the application from: $installDir\YtDlpGui.exe"

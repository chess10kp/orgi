# PowerShell installation script for Orgi on Windows
param(
    [string]$Version = "v0.1.9",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Check if running on Windows
if ($PSVersionTable.Platform -and $PSVersionTable.Platform -ne "Win32NT") {
    Write-Error "This script is for Windows only. Use install.sh for Unix systems."
    exit 1
}

# Define paths
$LocalBinPath = "$env:USERPROFILE\.local\bin"
$OrgiExePath = "$LocalBinPath\orgi.exe"
$TempDir = "$env:TEMP\orgi-install"
$ZipPath = "$TempDir\orgi-win-x64.zip"
$ExtractPath = "$TempDir\orgi-win-x64"

Write-Host "Installing Orgi $Version for Windows..." -ForegroundColor Green

# Create local bin directory if it doesn't exist
if (-not (Test-Path $LocalBinPath)) {
    Write-Host "Creating $LocalBinPath..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $LocalBinPath -Force | Out-Null
}

# Check if orgi already exists
if ((Test-Path $OrgiExePath) -and -not $Force) {
    Write-Warning "Orgi already exists at $OrgiExePath. Use -Force to reinstall."
    exit 0
}

# Create temp directory
if (Test-Path $TempDir) {
    Remove-Item -Path $TempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $TempDir | Out-Null

# Download the Windows release
$DownloadUrl = "https://github.com/chess10kp/orgi/releases/download/$Version/orgi-win-x64.zip"
Write-Host "Downloading Orgi from $DownloadUrl..." -ForegroundColor Yellow

try {
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $ZipPath -UseBasicParsing
}
catch {
    Write-Error "Failed to download Orgi: $_"
    exit 1
}

# Extract the zip
Write-Host "Extracting Orgi..." -ForegroundColor Yellow
try {
    Expand-Archive -Path $ZipPath -DestinationPath $TempDir -Force
}
catch {
    Write-Error "Failed to extract Orgi: $_"
    exit 1
}

# Copy the executable
Write-Host "Installing orgi.exe to $LocalBinPath..." -ForegroundColor Yellow
if (Test-Path "$ExtractPath\orgi.exe") {
    Copy-Item -Path "$ExtractPath\orgi.exe" -Destination $OrgiExePath -Force
}
else {
    Write-Error "orgi.exe not found in extracted archive"
    exit 1
}

# Test the installation
Write-Host "Testing installation..." -ForegroundColor Yellow
try {
    $TestResult = & $OrgiExePath --version
    Write-Host "Installation successful! Orgi version: $TestResult" -ForegroundColor Green
}
catch {
    Write-Error "Installation test failed: $_"
    exit 1
}

# Add to PATH if not already there
$CurrentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if ($CurrentPath -notlike "*$LocalBinPath*") {
    Write-Host "Adding $LocalBinPath to user PATH..." -ForegroundColor Yellow
    $NewPath = $CurrentPath + ";$LocalBinPath"
    [Environment]::SetEnvironmentVariable("PATH", $NewPath, "User")
    Write-Host "PATH updated. You may need to restart your terminal or run 'refreshenv'." -ForegroundColor Yellow
}

# Install PowerShell completion
Write-Host "Installing PowerShell completion..." -ForegroundColor Yellow
try {
    $CompletionScript = & $OrgiExePath completion powershell
    $CompletionDir = "$env:USERPROFILE\Documents\PowerShell\Scripts"
    $CompletionFile = "$CompletionDir\orgi-completion.ps1"
    
    if (-not (Test-Path $CompletionDir)) {
        New-Item -ItemType Directory -Path $CompletionDir -Force | Out-Null
    }
    
    $CompletionScript | Out-File -FilePath $CompletionFile -Encoding UTF8
    
    # Add to PowerShell profile
    $ProfilePath = "$env:USERPROFILE\Documents\PowerShell\Microsoft.PowerShell_profile.ps1"
    $ProfileLine = ". '$CompletionFile'"
    
    if (Test-Path $ProfilePath) {
        $ProfileContent = Get-Content $ProfilePath
        if ($ProfileContent -notlike "*orgi-completion.ps1*") {
            Add-Content -Path $ProfilePath -Value "`n# Orgi completion`n$ProfileLine"
        }
    }
    else {
        New-Item -ItemType File -Path $ProfilePath -Force | Out-Null
        Set-Content -Path $ProfilePath -Value "# Orgi completion`n$ProfileLine"
    }
    
    Write-Host "PowerShell completion installed. Restart PowerShell to enable." -ForegroundColor Green
}
catch {
    Write-Warning "Failed to install PowerShell completion: $_"
}

# Cleanup
Write-Host "Cleaning up temporary files..." -ForegroundColor Yellow
Remove-Item -Path $TempDir -Recurse -Force

Write-Host "Installation complete!" -ForegroundColor Green
Write-Host "Run 'orgi --help' to get started." -ForegroundColor Cyan
Write-Host "If PATH doesn't work, restart your terminal or run: `$env:PATH += `";$LocalBinPath`"" -ForegroundColor Yellow
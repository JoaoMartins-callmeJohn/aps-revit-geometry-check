# Revit Plugin Bundle Packaging Script
# This script packages the Revit plugin into a bundle for deployment

# Define paths
$ProjectRoot = $PSScriptRoot
$DllSource = Join-Path $ProjectRoot "bin\Debug\RevitPlugin.dll"
$BundleFolder = Join-Path $ProjectRoot "GeometryCheck.bundle"
$BundleContents = Join-Path $BundleFolder "Contents"
$BundlesOutputFolder = Join-Path $ProjectRoot "bundles"
$ZipFileName = "GeoExport.bundle.zip"
$ZipFilePath = Join-Path $ProjectRoot $ZipFileName

Write-Host "Starting bundle packaging process..." -ForegroundColor Green

# Step 1: Create Contents folder if it doesn't exist
if (-not (Test-Path $BundleContents)) {
    Write-Host "Creating Contents folder..."
    New-Item -ItemType Directory -Path $BundleContents -Force | Out-Null
}

# Step 2: Copy DLL to bundle Contents folder
if (Test-Path $DllSource) {
    Write-Host "Copying DLL from $DllSource to $BundleContents..."
    Copy-Item -Path $DllSource -Destination $BundleContents -Force
    Write-Host "DLL copied successfully!" -ForegroundColor Green
} else {
    Write-Host "ERROR: DLL not found at $DllSource" -ForegroundColor Red
    Write-Host "Please build the project first." -ForegroundColor Yellow
    exit 1
}

# Step 3: Create ZIP file
Write-Host "Creating ZIP archive..."
if (Test-Path $ZipFilePath) {
    Remove-Item $ZipFilePath -Force
}

# Use .NET compression for better compatibility
Add-Type -Assembly "System.IO.Compression.FileSystem"
[System.IO.Compression.ZipFile]::CreateFromDirectory($BundleFolder, $ZipFilePath)
Write-Host "ZIP file created: $ZipFilePath" -ForegroundColor Green

# Step 4: Create bundles folder if it doesn't exist (remove file if exists)
if (Test-Path $BundlesOutputFolder -PathType Leaf) {
    Write-Host "Removing existing 'bundles' file..."
    Remove-Item $BundlesOutputFolder -Force
}

if (-not (Test-Path $BundlesOutputFolder -PathType Container)) {
    Write-Host "Creating bundles folder..."
    New-Item -ItemType Directory -Path $BundlesOutputFolder -Force | Out-Null
}

# Step 5: Copy ZIP to bundles folder
$FinalZipPath = Join-Path $BundlesOutputFolder $ZipFileName
Write-Host "Copying ZIP to bundles folder..."
Copy-Item -Path $ZipFilePath -Destination $FinalZipPath -Force

# Step 6: Clean up temporary ZIP file
Remove-Item $ZipFilePath -Force

Write-Host "`nBundle packaging completed successfully!" -ForegroundColor Green
Write-Host "Output: $FinalZipPath" -ForegroundColor Cyan

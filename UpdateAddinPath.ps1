param(
    [string]$AddinFile,
    [string]$AssemblyPath
)

Write-Host "Updating $AddinFile with path: $AssemblyPath"

# Read the file content
$content = Get-Content $AddinFile -Raw

# Replace the Assembly element content
$pattern = '<Assembly>.*?</Assembly>'
$replacement = "<Assembly>$AssemblyPath</Assembly>"
$newContent = $content -replace $pattern, $replacement

# Save the updated content
$newContent | Set-Content $AddinFile -Encoding UTF8

Write-Host "Update complete"

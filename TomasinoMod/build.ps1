# Define paths
$projectPath = "f:/SPTarkov-Custom-Mods/TomasinoMod"
$outputPath = "f:/SPTarkov-Custom-Mods/TomasinoMod/bin/Release"
$zipFilePath = "f:/SPTarkov-Custom-Mods/TomasinoMod/PantsMod.zip"
$readmeFilePath = "f:/SPTarkov-Custom-Mods/README.md"

# Clean previous build
if (Test-Path $outputPath) {
    Remove-Item -Recurse -Force $outputPath
}

# Build the project
dotnet build $projectPath -c Release

# Create the ZIP file
if (Test-Path $zipFilePath) {
    Remove-Item -Force $zipFilePath
}
Compress-Archive -Path "$outputPath/*" -DestinationPath $zipFilePath

# Update the README file
$readmeContent = Get-Content $readmeFilePath
$updatedReadmeContent = $readmeContent -replace "\[releases page\]\(#\)", "[releases page](PantsMod.zip)"
Set-Content $readmeFilePath -Value $updatedReadmeContent

Write-Output "Build and packaging complete. README updated."
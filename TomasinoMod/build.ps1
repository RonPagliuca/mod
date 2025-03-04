# Define paths
$projectPath = "f:/SPTarkov-Custom-Mods/TomasinoMod"
$outputPath = "f:/SPTarkov-Custom-Mods/TomasinoMod/bin/Release"
$zipFilePath = "f:/SPTarkov-Custom-Mods/PantsMod.zip"
$readmeFilePath = "f:/SPTarkov-Custom-Mods/README.md"
$tempDir = "f:/SPTarkov-Custom-Mods/TomasinoMod/temp"

# Clean previous build
if (Test-Path $outputPath) {
    Write-Output "Cleaning previous build..."
    Remove-Item -Recurse -Force $outputPath
}

# Ensure no previous PantsMod.dll exists
$existingDlls = Get-ChildItem -Path $projectPath -Recurse -Filter "PantsMod.dll"
if ($existingDlls) {
    Write-Output "Removing previous PantsMod.dll files..."
    $existingDlls | Remove-Item -Force
}

# Build the project
Write-Output "Building the project..."
dotnet build $projectPath -c Release

# Ensure the output directory exists
if (-Not (Test-Path $outputPath)) {
    Write-Error "Build output directory does not exist: $outputPath"
    exit 1
}

# Ensure the PantsMod.dll exists
$dllPath = Join-Path $outputPath "PantsMod.dll"
if (-Not (Test-Path $dllPath)) {
    Write-Error "PantsMod.dll not found in the output directory: $dllPath"
    exit 1
}

# Prepare the temporary directory for the ZIP file
if (Test-Path $tempDir) {
    Write-Output "Cleaning temporary directory..."
    Remove-Item -Recurse -Force $tempDir
}
New-Item -ItemType Directory -Path $tempDir
New-Item -ItemType Directory -Path (Join-Path $tempDir "BepInEx\plugins")

# Copy the DLL to the temporary directory
Copy-Item -Path $dllPath -Destination (Join-Path $tempDir "BepInEx\plugins")

# Create the ZIP file with the correct directory structure
if (Test-Path $zipFilePath) {
    Write-Output "Removing existing ZIP file..."
    Remove-Item -Force $zipFilePath
}
Write-Output "Creating ZIP file..."
Compress-Archive -Path "$tempDir\*" -DestinationPath $zipFilePath

# Verify the ZIP file was created
if (-Not (Test-Path $zipFilePath)) {
    Write-Error "Failed to create ZIP file: $zipFilePath"
    exit 1
}

# Update the README file
Write-Output "Updating README file..."
$readmeContent = Get-Content $readmeFilePath
$updatedReadmeContent = $readmeContent -replace "\[Download Pants Mod\]\(.*\)", "[Download Pants Mod](./PantsMod.zip)"
Set-Content $readmeFilePath -Value $updatedReadmeContent

Write-Output "Build and packaging complete. README updated."
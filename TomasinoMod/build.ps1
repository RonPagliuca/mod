<#
  build.ps1
  Run this script from the TomasinoMod folder (where the .csproj resides).
#>

# The folder containing build.ps1 (and the .csproj).
$projectDir = $PSScriptRoot

# The parent folder, which also contains BepInEx, EscapeFromTarkov_Data, etc.
$rootDir = Split-Path $projectDir -Parent

# Paths
$projectFile    = Join-Path $projectDir "TomasinoMod.csproj"
$outputPath     = Join-Path $projectDir "bin"
$intermediatePath = Join-Path $projectDir "tempObj/"  # Must end with slash
$tempDir        = Join-Path $projectDir "temp"

# The ZIP and README live in the parent folder (so they're siblings with BepInEx).
$zipFilePath    = Join-Path $rootDir "PantsMod.zip"
$readmeFilePath = Join-Path $rootDir "README.md"

# 1. Ensure the .csproj file exists
if (-not (Test-Path $projectFile)) {
    Write-Error "Project file not found: $projectFile"
    exit 1
}

# 2. Clean up old bin and intermediate folders
if (Test-Path $outputPath) {
    Write-Output "Cleaning previous build folder..."
    Remove-Item -Recurse -Force $outputPath
}
if (Test-Path $intermediatePath) {
    Write-Output "Cleaning previous intermediate folder..."
    Remove-Item -Recurse -Force $intermediatePath
}

# 3. Create bin folder if needed
if (-not (Test-Path $outputPath)) {
    Write-Output "Creating bin folder..."
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}

# 4. Remove any leftover PantsMod.dll in the project folder
$existingDlls = Get-ChildItem -Path $projectDir -Recurse -Filter "PantsMod.dll"
if ($existingDlls) {
    Write-Output "Removing old PantsMod.dll files..."
    $existingDlls | Remove-Item -Force
}

# 5. Build the project in Release mode, overriding output to bin,
#    and placing intermediate files in tempObj
Write-Output "Building the project..."
dotnet build $projectFile -c Release -o $outputPath -p:BaseIntermediateOutputPath=$intermediatePath

# 6. Verify that bin folder now exists
if (-not (Test-Path $outputPath)) {
    Write-Error "Build output directory does not exist: $outputPath"
    exit 1
}

# 7. Check if PantsMod.dll was created; if not, try to rename any single DLL found
$dllPath = Join-Path $outputPath "PantsMod.dll"
if (-not (Test-Path $dllPath)) {
    Write-Output "PantsMod.dll not found. Searching for a single DLL in $outputPath..."
    $dllFiles = Get-ChildItem -Path $outputPath -Filter *.dll -File
    if ($dllFiles.Count -eq 1) {
        $found = $dllFiles[0].Name
        Write-Output "Renaming $found to PantsMod.dll..."
        Rename-Item -Path $dllFiles[0].FullName -NewName "PantsMod.dll"
    }
}

if (-not (Test-Path $dllPath)) {
    Write-Error "PantsMod.dll not found in $outputPath"
    exit 1
}

# 8. Remove any extra files (keeping only PantsMod.dll)
Get-ChildItem -Path $outputPath -File | Where-Object { $_.Name -ne "PantsMod.dll" } | ForEach-Object {
    Write-Output "Removing extra file: $($_.Name)"
    Remove-Item $_.FullName -Force
}

# 9. Clean up intermediate folder
if (Test-Path $intermediatePath) {
    Write-Output "Removing intermediate folder..."
    Remove-Item -Recurse -Force $intermediatePath
}

# 10. Prepare the temp folder for packaging
if (Test-Path $tempDir) {
    Write-Output "Removing old temp folder..."
    Remove-Item -Recurse -Force $tempDir
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

# 11. Create the BepInEx\plugins folder structure in temp
$tempPluginsDir = Join-Path $tempDir "BepInEx\plugins"
New-Item -ItemType Directory -Path $tempPluginsDir | Out-Null

# 12. Copy PantsMod.dll into the temp BepInEx\plugins folder
Copy-Item -Path (Join-Path $outputPath "PantsMod.dll") -Destination $tempPluginsDir

# 13. Create or overwrite PantsMod.zip in the parent folder
if (Test-Path $zipFilePath) {
    Write-Output "Removing existing $zipFilePath..."
    Remove-Item -Force $zipFilePath
}
Write-Output "Creating ZIP: $zipFilePath"
Compress-Archive -Path "$tempDir\*" -DestinationPath $zipFilePath

if (-not (Test-Path $zipFilePath)) {
    Write-Error "Failed to create ZIP file: $zipFilePath"
    exit 1
}

# 14. Remove the temp packaging folder
Write-Output "Removing temp folder..."
Remove-Item -Recurse -Force $tempDir

# 15. Update README in the parent folder (if present)
if (Test-Path $readmeFilePath) {
    Write-Output "Updating README link..."
    $readmeContent = Get-Content $readmeFilePath
    $updatedReadmeContent = $readmeContent -replace "\[Download Pants Mod\]\(.*\)", "[Download Pants Mod](./PantsMod.zip)"
    Set-Content $readmeFilePath -Value $updatedReadmeContent
}
else {
    Write-Output "No README found at $readmeFilePath. Skipping update."
}

Write-Output "Build and packaging complete. Your PantsMod.dll is in bin, and PantsMod.zip is in $rootDir."

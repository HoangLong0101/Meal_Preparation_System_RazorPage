param(
    [string]$ProjectPath = "src/Meal_Preparation_System_RazorPage/Meal_Preparation_System_RazorPage.csproj",
    [string]$SecretsFile = "./user-secrets-export.txt",
    [switch]$RunApp
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..")
Set-Location $repoRoot

$projectFullPath = Resolve-Path $ProjectPath
$secretsFullPath = [System.IO.Path]::GetFullPath($SecretsFile)
$importScriptPath = Join-Path $scriptRoot "import-secrets.ps1"

if (-not (Test-Path $importScriptPath)) {
    throw "Missing import script: $importScriptPath"
}

if (Test-Path $secretsFullPath) {
    Write-Host "Importing user-secrets from: $secretsFullPath"
    & $importScriptPath -ProjectPath $projectFullPath -InputFile $secretsFullPath
} else {
    Write-Host "Secrets file not found, skipping import: $secretsFullPath"
    Write-Host "You can provide one with -SecretsFile or set secrets manually."
}

Write-Host "Restoring dependencies..."
dotnet restore $projectFullPath

Write-Host "Building project..."
dotnet build $projectFullPath

if ($RunApp) {
    Write-Host "Starting app..."
    dotnet run --project $projectFullPath
} else {
    Write-Host "Bootstrap completed. Run app with: dotnet run --project $projectFullPath"
}

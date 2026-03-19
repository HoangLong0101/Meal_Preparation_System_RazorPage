param(
    [string]$ProjectPath = "src/Meal_Preparation_System_RazorPage/Meal_Preparation_System_RazorPage.csproj",
    [string]$OutputFile = "./user-secrets-export.txt"
)

$ErrorActionPreference = "Stop"

$projectFullPath = Resolve-Path $ProjectPath
$outputFullPath = [System.IO.Path]::GetFullPath($OutputFile)

dotnet user-secrets list --project $projectFullPath | Set-Content -Path $outputFullPath -Encoding UTF8

Write-Host "Exported user-secrets to: $outputFullPath"
Write-Host "Do not commit this file to git."

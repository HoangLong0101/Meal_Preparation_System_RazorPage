param(
    [string]$ProjectPath = "src/Meal_Preparation_System_RazorPage/Meal_Preparation_System_RazorPage.csproj",
    [string]$InputFile = "./user-secrets-export.txt"
)

$ErrorActionPreference = "Stop"

$projectFullPath = Resolve-Path $ProjectPath
$inputFullPath = [System.IO.Path]::GetFullPath($InputFile)

if (-not (Test-Path $inputFullPath)) {
    throw "Secrets file not found: $inputFullPath"
}

dotnet user-secrets init --project $projectFullPath | Out-Null

$lines = Get-Content -Path $inputFullPath
$imported = 0

foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }

    $separatorIndex = $line.IndexOf("=")
    if ($separatorIndex -le 0) {
        continue
    }

    $key = $line.Substring(0, $separatorIndex).Trim()
    $value = $line.Substring($separatorIndex + 1)

    if ([string]::IsNullOrWhiteSpace($key)) {
        continue
    }

    dotnet user-secrets set $key $value --project $projectFullPath | Out-Null
    $imported++
}

Write-Host "Imported $imported secret(s) into project: $projectFullPath"
Write-Host "Current secrets:"
dotnet user-secrets list --project $projectFullPath

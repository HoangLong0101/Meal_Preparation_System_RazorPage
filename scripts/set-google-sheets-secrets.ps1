param(
    [Parameter(Mandatory = $true)]
    [string]$SpreadsheetId,

    [Parameter(Mandatory = $false)]
    [string]$SheetName = "DashboardReport",

    [Parameter(Mandatory = $false)]
    [string]$ServiceAccountFile,

    [Parameter(Mandatory = $false)]
    [string]$ServiceAccountJson,


    [Parameter(Mandatory = $false)]
    [string]$ProjectPath = "src/Meal_Preparation_System_RazorPage/Meal_Preparation_System_RazorPage.csproj"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ServiceAccountFile) -and [string]::IsNullOrWhiteSpace($ServiceAccountJson)) {
    throw "Provide either -ServiceAccountFile or -ServiceAccountJson."
}

if (-not [string]::IsNullOrWhiteSpace($ServiceAccountFile) -and -not [string]::IsNullOrWhiteSpace($ServiceAccountJson)) {
    throw "Use only one option: -ServiceAccountFile or -ServiceAccountJson."
}

if (-not (Test-Path $ProjectPath)) {
    throw "Project file not found: $ProjectPath"
}

Write-Host "Setting Google Sheets secrets for project: $ProjectPath" -ForegroundColor Cyan

dotnet user-secrets set "GoogleSheets:SpreadsheetId" "$SpreadsheetId" --project "$ProjectPath" | Out-Null
dotnet user-secrets set "GoogleSheets:SheetName" "$SheetName" --project "$ProjectPath" | Out-Null

if (-not [string]::IsNullOrWhiteSpace($ServiceAccountFile)) {
    if (-not (Test-Path $ServiceAccountFile)) {
        throw "Service account file not found: $ServiceAccountFile"
    }

    $fullPath = (Resolve-Path $ServiceAccountFile).Path

    dotnet user-secrets set "GoogleSheets:ServiceAccountFile" "$fullPath" --project "$ProjectPath" | Out-Null
    dotnet user-secrets remove "GoogleSheets:ServiceAccountJson" --project "$ProjectPath" | Out-Null

    Write-Host "Configured service account via file path." -ForegroundColor Green
}
else {
    $normalizedJson = $ServiceAccountJson.Trim()

    if ($normalizedJson.StartsWith("{")) {
        try {
            $normalizedJson = ($normalizedJson | ConvertFrom-Json | ConvertTo-Json -Compress)
        }
        catch {
            throw "ServiceAccountJson is not valid JSON."
        }
    }

    dotnet user-secrets set "GoogleSheets:ServiceAccountJson" "$normalizedJson" --project "$ProjectPath" | Out-Null
    dotnet user-secrets remove "GoogleSheets:ServiceAccountFile" --project "$ProjectPath" | Out-Null

    Write-Host "Configured service account via JSON content." -ForegroundColor Green
}

Write-Host "Google Sheets secrets configured successfully." -ForegroundColor Green

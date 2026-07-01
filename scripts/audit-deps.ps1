param(
    [switch]$FailOnFindings
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$foundIssues = $false

Write-Host "Running npm audit..."
Push-Location (Join-Path $repoRoot "client")
npm audit --audit-level=high
if ($LASTEXITCODE -ne 0) { $foundIssues = $true }
Pop-Location

Write-Host "Running dotnet vulnerable package scan..."
Push-Location $repoRoot
dotnet list package --vulnerable --include-transitive
if ($LASTEXITCODE -ne 0) { $foundIssues = $true }
Pop-Location

if ($foundIssues -and $FailOnFindings) {
    exit 1
}

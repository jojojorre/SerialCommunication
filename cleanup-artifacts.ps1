# Build Artifacts Cleanup Script
# Removes Visual Studio build artifacts and cache from SerialCommunication repository
# Usage: .\cleanup-artifacts.ps1

param(
    [switch]$Force = $false
)

$repoRoot = Split-Path -Parent $PSCommandPath
$artifacts = @(
    @{ Path = "SerialCommunication\bin"; Name = "bin folder" },
    @{ Path = "SerialCommunication\obj"; Name = "obj folder" },
    @{ Path = ".vs"; Name = ".vs cache" },
    @{ Path = "SerialCommunication\TestResults"; Name = "test results" }
)

Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Build Artifacts Cleanup Script       ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝" -ForegroundColor Cyan

if (-not $Force) {
    Write-Host "`nArtifacts to be removed:" -ForegroundColor Yellow
    foreach ($artifact in $artifacts) {
        $fullPath = Join-Path $repoRoot $artifact.Path
        if (Test-Path $fullPath) {
            Write-Host "  ✓ $($artifact.Name)" -ForegroundColor Yellow
        }
    }
    Write-Host "`nTo proceed, run: .\cleanup-artifacts.ps1 -Force`n" -ForegroundColor Gray
    exit 0
}

Write-Host "`nRemoving artifacts..." -ForegroundColor Cyan

$removed = 0
$failed = 0

foreach ($artifact in $artifacts) {
    $fullPath = Join-Path $repoRoot $artifact.Path
    if (Test-Path $fullPath) {
        try {
            Write-Host "Removing: $($artifact.Name)..." -NoNewline
            Remove-Item -Path $fullPath -Recurse -Force -ErrorAction Stop
            Write-Host " ✓" -ForegroundColor Green
            $removed++
        } catch {
            Write-Host " ✗" -ForegroundColor Red
            Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
            $failed++
        }
    }
}

# Remove log files
Write-Host "Removing log files..." -NoNewline
$logCount = 0
Get-ChildItem -Path $repoRoot -Recurse -Filter "*.log" -ErrorAction SilentlyContinue | ForEach-Object {
    Remove-Item -Path $_.FullName -Force -ErrorAction SilentlyContinue
    $logCount++
}
Write-Host " ✓ ($logCount files)" -ForegroundColor Green

Write-Host "`n╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║          Cleanup Complete             ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host "Removed: $removed artifact groups" -ForegroundColor Green
if ($failed -gt 0) {
    Write-Host "Failed: $failed artifact groups (may require manual removal)" -ForegroundColor Yellow
}
Write-Host ""

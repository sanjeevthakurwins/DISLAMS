<#
PowerShell helper to run the seed_data.sql
Usage:
  - Open PowerShell as user with permission to the SQL Server
  - Update $Server if needed
  - ./run-seed.ps1

This script tries Invoke-Sqlcmd (SQLServer module) first; if not present, it falls back to sqlcmd.exe.
#>
param(
    [string]$Server = "Ditsdev346",
    [string]$Database = "DISLAMS_StudentManagement",
    [string]$ScriptPath = "$PSScriptRoot\seed_data.sql"
)

Write-Host "Running seed script against server: $Server, database: $Database" -ForegroundColor Green

# Try Invoke-Sqlcmd
if (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue) {
    Write-Host "Using Invoke-Sqlcmd..." -ForegroundColor Yellow
    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -InputFile $ScriptPath
} else {
    Write-Host "Invoke-Sqlcmd not found, falling back to sqlcmd.exe" -ForegroundColor Yellow
    $sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($null -eq $sqlcmd) {
        Write-Error "sqlcmd.exe not found in PATH. Install SQL Server Command Line Tools or use Invoke-Sqlcmd." -ForegroundColor Red
        exit 1
    }
    & sqlcmd -S $Server -d $Database -i $ScriptPath
}

Write-Host "Seed script execution finished." -ForegroundColor Green

<#
    Ejecuta todos los scripts .sql de esta carpeta, en orden alfabético,
    contra la instancia local de SQL Server Express usando autenticación
    de Windows (Integrated Security).

    Uso:
        powershell -File Run-Scripts.ps1
        powershell -File Run-Scripts.ps1 -Server "localhost\SQLEXPRESS"
#>
param(
    [string]$Server = "localhost\SQLEXPRESS"
)

Add-Type -AssemblyName System.Data

$scriptsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$files = Get-ChildItem -Path $scriptsDir -Filter "*.sql" | Sort-Object Name

# Conexión inicial a master, para poder correr CREATE DATABASE
$connStringMaster = "Server=$Server;Database=master;Integrated Security=True;TrustServerCertificate=True;Connection Timeout=15"

foreach ($file in $files) {
    Write-Host "==> Ejecutando $($file.Name)" -ForegroundColor Cyan
    $sql = Get-Content -Raw -Path $file.FullName -Encoding UTF8
    $batches = $sql -split '(?im)^\s*GO\s*$'

    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = $connStringMaster
    $conn.Open()

    foreach ($batch in $batches) {
        if ($batch.Trim().Length -eq 0) { continue }
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $batch
        $cmd.CommandTimeout = 60
        try {
            $cmd.ExecuteNonQuery() | Out-Null
        } catch {
            Write-Host "ERROR en $($file.Name): $($_.Exception.Message)" -ForegroundColor Red
            $conn.Close()
            exit 1
        }
    }
    $conn.Close()
}

Write-Host "Todos los scripts se ejecutaron correctamente." -ForegroundColor Green

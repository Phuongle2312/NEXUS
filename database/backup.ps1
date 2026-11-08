# Sao luu database NEXUS (task 28, de bai yeu cau sao luu moi ngay).
# Tao file database\backup\NEXUS_yyyyMMdd_HHmm.bak. Tai khoan dich vu SQL Server phai co quyen ghi vao thu muc nay.
#   powershell -ExecutionPolicy Bypass -File database\backup.ps1
param([string]$Server = ".\SQLEXPRESS")

$dir = Join-Path $PSScriptRoot "backup"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$file = Join-Path $dir ("NEXUS_" + (Get-Date -Format "yyyyMMdd_HHmm") + ".bak")

# COPY_ONLY: khong anh huong chuoi sao luu khac; CHECKSUM + VERIFYONLY: phat hien file hong ngay luc tao.
$sql = "BACKUP DATABASE NEXUS TO DISK = N'$file' WITH COPY_ONLY, INIT, CHECKSUM; RESTORE VERIFYONLY FROM DISK = N'$file' WITH CHECKSUM;"
sqlcmd -S $Server -E -C -b -Q $sql
if ($LASTEXITCODE -ne 0) {
    Write-Error "Sao luu that bai."
    exit 1
}
Write-Output "Da sao luu: $file"

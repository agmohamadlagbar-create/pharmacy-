# FTP Upload Script for MonsterASP.net
# Usage: .\ftp_upload.ps1 -FtpPassword (ConvertTo-SecureString "pwd" -AsPlainText -Force) -DbPassword (ConvertTo-SecureString "pwd" -AsPlainText -Force)

param(
    [Parameter(Mandatory=$true)]
    [SecureString]$FtpPassword,
    
    [Parameter(Mandatory=$true)]
    [SecureString]$DbPassword
)

$ErrorActionPreference = "Stop"

# --- Configuration ---
$ftpServer   = "ftp://site69773.siteasp.net"
$ftpUser     = "site69773"
$ftpRootPath = "/wwwroot"
$localPublishPath = "d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\backend\OnlineStore.Api\publish"

# --- Step 1: Update appsettings.Production.json with real DB password ---
Write-Host "=== Updating appsettings.Production.json with DB password ===" -ForegroundColor Cyan
$prodSettingsPath = Join-Path $localPublishPath "appsettings.Production.json"
$content = Get-Content $prodSettingsPath -Raw
$plainDbPassword = ConvertFrom-SecureString $DbPassword -AsPlainText
$content = $content -replace "YOUR_DB_PASSWORD_HERE", $plainDbPassword
Set-Content $prodSettingsPath -Value $content -NoNewline
Write-Host "  Done." -ForegroundColor Green

# --- Step 2: Upload all files via FTP ---
Write-Host "`n=== Starting FTP Upload ===" -ForegroundColor Cyan

# Create .NET FTP credentials — NetworkCredential accepts SecureString directly
$credentials = New-Object System.Net.NetworkCredential($ftpUser, $FtpPassword)

function Send-FtpFile {
    param(
        [string]$LocalFile,
        [string]$RemotePath
    )
    
    $uri = "$ftpServer$RemotePath"
    
    try {
        $request = [System.Net.FtpWebRequest]::Create($uri)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $request.Credentials = $credentials
        $request.UseBinary = $true
        $request.UsePassive = $true
        $request.KeepAlive = $false
        $request.EnableSsl = $false
        
        $fileContent = [System.IO.File]::ReadAllBytes($LocalFile)
        $request.ContentLength = $fileContent.Length
        
        $requestStream = $request.GetRequestStream()
        $requestStream.Write($fileContent, 0, $fileContent.Length)
        $requestStream.Close()
        
        $response = $request.GetResponse()
        $response.Close()
        
        return $true
    }
    catch {
        Write-Host "    ERROR uploading $($RemotePath): $_" -ForegroundColor Red
        return $false
    }
}

function New-FtpDirectory {
    param(
        [string]$RemotePath
    )
    
    $uri = "$ftpServer$RemotePath"
    
    try {
        $request = [System.Net.FtpWebRequest]::Create($uri)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $request.Credentials = $credentials
        $request.UsePassive = $true
        $request.KeepAlive = $false
        $request.EnableSsl = $false
        
        $response = $request.GetResponse()
        $response.Close()
    }
    catch {
        # Directory might already exist, that's OK
    }
}

# --- Step 2.5: Take App Offline ---
Write-Host "  Taking app offline..." -ForegroundColor Yellow
Set-Content -Path "$localPublishPath\app_offline.htm" -Value "<html><body>Site is updating. Be back in a minute!</body></html>"
$offlineSuccess = Send-FtpFile -LocalFile "$localPublishPath\app_offline.htm" -RemotePath "$ftpRootPath/app_offline.htm"
if ($offlineSuccess) {
    Write-Host "  App is offline. Waiting 10 seconds for locks to release..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
}

# Get all files from publish directory (excluding app_offline.htm)
$allFiles = Get-ChildItem -Path $localPublishPath -Recurse -File | Where-Object { $_.Name -ne "app_offline.htm" }
$totalFiles = $allFiles.Count
$currentFile = 0
$failedFiles = @()

Write-Host "  Found $totalFiles files to upload." -ForegroundColor Yellow

# First, create all necessary directories
$allDirs = Get-ChildItem -Path $localPublishPath -Recurse -Directory
foreach ($dir in $allDirs) {
    $relativePath = $dir.FullName.Substring($localPublishPath.Length).Replace("\", "/")
    $remoteDirPath = "$ftpRootPath$relativePath"
    Write-Host "  Creating directory: $remoteDirPath" -ForegroundColor DarkGray
    New-FtpDirectory -RemotePath $remoteDirPath
}

# Then upload all files
foreach ($file in $allFiles) {
    $currentFile++
    $relativePath = $file.FullName.Substring($localPublishPath.Length).Replace("\", "/")
    $remotePath = "$ftpRootPath$relativePath"
    
    $percent = [math]::Round(($currentFile / $totalFiles) * 100, 1)
    Write-Host "  [$currentFile/$totalFiles] ($percent%) Uploading: $relativePath" -ForegroundColor White -NoNewline
    
    $success = Send-FtpFile -LocalFile $file.FullName -RemotePath $remotePath
    if ($success) {
        Write-Host " OK" -ForegroundColor Green
    } else {
        $failedFiles += $relativePath
    }
}

# --- Step 3: Bring App Back Online ---
Write-Host "`n=== Bringing App Back Online ===" -ForegroundColor Cyan
try {
    $request = [System.Net.FtpWebRequest]::Create("$ftpServer$ftpRootPath/app_offline.htm")
    $request.Credentials = $credentials
    $request.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
    $request.GetResponse().Close()
    Write-Host "  App is back online." -ForegroundColor Green
} catch {
    Write-Host "  Could not delete app_offline.htm: $_" -ForegroundColor Red
}
Remove-Item "$localPublishPath\app_offline.htm" -ErrorAction SilentlyContinue

# --- Summary ---
Write-Host "`n=== Upload Complete ===" -ForegroundColor Cyan
Write-Host "  Total files: $totalFiles" -ForegroundColor White
Write-Host "  Successful: $($totalFiles - $failedFiles.Count)" -ForegroundColor Green

if ($failedFiles.Count -gt 0) {
    Write-Host "  Failed: $($failedFiles.Count)" -ForegroundColor Red
    foreach ($f in $failedFiles) {
        Write-Host "    - $f" -ForegroundColor Red
    }
} else {
    Write-Host "`n  All files uploaded successfully!" -ForegroundColor Green
    Write-Host "  Your website should be live at: https://site69773.siteasp.net" -ForegroundColor Yellow
}

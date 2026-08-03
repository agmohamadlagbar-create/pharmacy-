$ErrorActionPreference = "Stop"

# --- Configurations ---
$ftpServer = "ftp://lagbar.runasp.net"
$ftpUser = "site69773"
$ftpPass = "4o+Pf#K39By%"
$ftpRootPath = "/wwwroot"
$localPublishPath = "d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\backend\OnlineStore.Api\publish"

$credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPass)

function Upload-FtpFile {
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
        Write-Host "`n    ERROR uploading $($RemotePath): $_" -ForegroundColor Red
        return $false
    }
}

function Create-FtpDirectory {
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
        
        $response = $request.GetResponse()
        $response.Close()
    }
    catch {
        # Directory might already exist, which is fine
    }
}

# --- Step 1: Take App Offline ---
Write-Host "=== 1. Taking App Offline ===" -ForegroundColor Yellow
Set-Content -Path "$localPublishPath\app_offline.htm" -Value "<html><body>Site is updating. Be back in a minute!</body></html>"
$offlineSuccess = Upload-FtpFile -LocalFile "$localPublishPath\app_offline.htm" -RemotePath "$ftpRootPath/app_offline.htm"
if ($offlineSuccess) {
    Write-Host "  App is offline. Waiting 10 seconds for locks to release..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
}

# --- Step 2: Upload Files ---
Write-Host "`n=== 2. Starting FTP Upload to lagbar.runasp.net ===" -ForegroundColor Cyan

# Find all files and directories
$allFiles = Get-ChildItem -Path $localPublishPath -Recurse -File | Where-Object { 
    $_.Name -ne "app_offline.htm" -and $_.Name -ne "appsettings.Production.json"
}
$allDirs = Get-ChildItem -Path $localPublishPath -Recurse -Directory
$totalFiles = $allFiles.Count
$currentFile = 0
$failedFiles = @()

Write-Host "  Found $totalFiles files to upload." -ForegroundColor Yellow

# Create all necessary subdirectories remotely
foreach ($dir in $allDirs) {
    $relativePath = $dir.FullName.Substring($localPublishPath.Length).Replace("\", "/")
    $remoteDirPath = "$ftpRootPath$relativePath"
    Write-Host "  Creating directory: $remoteDirPath" -ForegroundColor DarkGray
    Create-FtpDirectory -RemotePath $remoteDirPath
}

# Upload files one by one
foreach ($file in $allFiles) {
    $currentFile++
    $relativePath = $file.FullName.Substring($localPublishPath.Length).Replace("\", "/")
    $remotePath = "$ftpRootPath$relativePath"
    
    $percent = [math]::Round(($currentFile / $totalFiles) * 100, 1)
    Write-Host "  [$currentFile/$totalFiles] ($percent%) Uploading: $relativePath..." -NoNewline
    
    $success = Upload-FtpFile -LocalFile $file.FullName -RemotePath $remotePath
    if ($success) {
        Write-Host " OK" -ForegroundColor Green
    }
    else {
        $failedFiles += $relativePath
    }
}

# --- Step 3: Bring App Back Online ---
Write-Host "`n=== 3. Bringing App Back Online ===" -ForegroundColor Yellow
try {
    $request = [System.Net.FtpWebRequest]::Create("$ftpServer$ftpRootPath/app_offline.htm")
    $request.Credentials = $credentials
    $request.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
    $request.GetResponse().Close()
    Write-Host "  App is back online." -ForegroundColor Green
}
catch {
    Write-Host "  Could not delete app_offline.htm: $_" -ForegroundColor Red
}
Remove-Item "$localPublishPath\app_offline.htm" -ErrorAction SilentlyContinue

Write-Host "`n=== Upload Complete ===" -ForegroundColor Cyan
Write-Host "  Total files: $totalFiles" -ForegroundColor White
Write-Host "  Successful: $($totalFiles - $failedFiles.Count)" -ForegroundColor Green

if ($failedFiles.Count -gt 0) {
    Write-Host "  Failed: $($failedFiles.Count)" -ForegroundColor Red
    foreach ($f in $failedFiles) {
        Write-Host "    - $f" -ForegroundColor Red
    }
    exit 1
}
else {
    Write-Host "`n  All files uploaded successfully to lagbar.runasp.net!" -ForegroundColor Green
}

$ftpServer = "ftp://site69773.siteasp.net"
$ftpUser = $env:FTP_USER
$ftpPassword = $env:FTP_PASS
$ftpRootPath = "/wwwroot"
$localPublishPath = "d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\backend\OnlineStore.Api\publish"

if ([string]::IsNullOrWhiteSpace($ftpUser) -or [string]::IsNullOrWhiteSpace($ftpPassword)) {
    throw "Set FTP_USER and FTP_PASS environment variables before running this script."
}

$credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPassword)

function Upload-FtpFile {
    param([string]$LocalFile, [string]$RemotePath)
    try {
        $uri = "$ftpServer$RemotePath"
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
        $request.GetResponse().Close()
        return $true
    } catch {
        Write-Host "ERROR uploading $($LocalFile): $_" -ForegroundColor Red
        return $false
    }
}

Write-Host "=== Taking App Offline ===" -ForegroundColor Yellow
Set-Content -Path "$localPublishPath\app_offline.htm" -Value "<html><body>Site is updating. Be back in a minute!</body></html>"
$offlineSuccess = Upload-FtpFile -LocalFile "$localPublishPath\app_offline.htm" -RemotePath "$ftpRootPath/app_offline.htm"
if ($offlineSuccess) {
    Write-Host "  App is offline. Waiting 10 seconds for locks to release..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
}

Write-Host "`n=== Uploading Modified DLLs ===" -ForegroundColor Cyan
$dllFiles = @("OnlineStore.Api.dll", "OnlineStore.Api.pdb")
foreach ($file in $dllFiles) {
    $localFile = Join-Path $localPublishPath $file
    Write-Host "  Uploading $file..." -NoNewline
    $success = Upload-FtpFile -LocalFile $localFile -RemotePath "$ftpRootPath/$file"
    if ($success) { Write-Host " OK" -ForegroundColor Green }
}

Write-Host "`n=== Uploading Frontend HTML ===" -ForegroundColor Cyan
Write-Host "  Uploading index.html..." -NoNewline
$success = Upload-FtpFile -LocalFile "$localPublishPath\wwwroot\index.html" -RemotePath "$ftpRootPath/wwwroot/index.html"
if ($success) { Write-Host " OK" -ForegroundColor Green }

Write-Host "`n=== Uploading Frontend Assets ===" -ForegroundColor Cyan
$assets = Get-ChildItem -Path "$localPublishPath\wwwroot\assets" -File
foreach ($asset in $assets) {
    Write-Host "  Uploading asset: $($asset.Name)..." -NoNewline
    $success = Upload-FtpFile -LocalFile $asset.FullName -RemotePath "$ftpRootPath/wwwroot/assets/$($asset.Name)"
    if ($success) { Write-Host " OK" -ForegroundColor Green }
}

Write-Host "`n=== Bringing App Back Online ===" -ForegroundColor Yellow
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

Write-Host "`n=== Deployment Complete ===" -ForegroundColor Green

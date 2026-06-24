$ftpServer   = "ftp://site69773.siteasp.net"
$ftpUser     = $env:FTP_USER
$ftpRootPath = "/wwwroot"
$localPublishPath = "d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\backend\OnlineStore.Api\publish"
$FtpPassword = $env:FTP_PASS

if ([string]::IsNullOrWhiteSpace($ftpUser) -or [string]::IsNullOrWhiteSpace($FtpPassword)) {
    throw "Set FTP_USER and FTP_PASS environment variables before running this script."
}

$credentials = New-Object System.Net.NetworkCredential($ftpUser, $FtpPassword)

function Upload-FtpFile {
    param([string]$LocalFile, [string]$RemotePath)
    try {
        $uri = "$ftpServer$RemotePath"
        $request = [System.Net.FtpWebRequest]::Create($uri)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $request.Credentials = $credentials
        $request.UseBinary = $true
        $request.UsePassive = $true
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

Write-Host "Taking app offline..."
Set-Content -Path "$localPublishPath\app_offline.htm" -Value "<html><body>Site is updating. Be back in a minute!</body></html>"
Upload-FtpFile -LocalFile "$localPublishPath\app_offline.htm" -RemotePath "$ftpRootPath/app_offline.htm" | Out-Null
Start-Sleep -Seconds 10 # Wait for IIS to shut down the AppDomain and unlock the DLLs

Write-Host "Uploading DLLs..."
$dlls = Get-ChildItem -Path $localPublishPath -Filter "*.dll"
foreach ($dll in $dlls) {
    Write-Host "Uploading $($dll.Name)..." -NoNewline
    $success = Upload-FtpFile -LocalFile $dll.FullName -RemotePath "$ftpRootPath/$($dll.Name)"
    if ($success) { Write-Host " OK" -ForegroundColor Green }
}

Write-Host "Bringing app back online (deleting app_offline.htm)..."
try {
    $request = [System.Net.FtpWebRequest]::Create("$ftpServer$ftpRootPath/app_offline.htm")
    $request.Credentials = $credentials
    $request.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
    $request.GetResponse().Close()
} catch {
    Write-Host "Could not delete app_offline.htm: $_"
}
Remove-Item "$localPublishPath\app_offline.htm" -ErrorAction SilentlyContinue

Write-Host "Done!"

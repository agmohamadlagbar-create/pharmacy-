$user = $env:FTP_USER
$pass = $env:FTP_PASS

if ([string]::IsNullOrWhiteSpace($user) -or [string]::IsNullOrWhiteSpace($pass)) {
    throw "Set FTP_USER and FTP_PASS environment variables before running this script."
}

$webclient = New-Object System.Net.WebClient
$webclient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)

# 1. Create and upload app_offline.htm
$offlineContent = "<html><body>Site is being updated. Back in a few seconds.</body></html>"
Set-Content -Path ".\app_offline.htm" -Value $offlineContent
Write-Output "Stopping application pool..."
$webclient.UploadFile("ftp://lagbar.runasp.net/wwwroot/app_offline.htm", ".\app_offline.htm")
Start-Sleep -Seconds 5

# 2. Upload the DLLs and PDBs
$publishDir = "d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\backend\publish"
$filesToUpload = @(
    "OnlineStore.Api.dll",
    "OnlineStore.Api.pdb",
    "OnlineStore.Infrastructure.dll",
    "OnlineStore.Infrastructure.pdb"
)

try {
    foreach ($file in $filesToUpload) {
        Write-Output "Uploading $file..."
        $webclient.UploadFile("ftp://lagbar.runasp.net/wwwroot/$file", "$publishDir\$file")
    }
    Write-Output "Upload successful."
} catch {
    Write-Output "Failed to upload files: $_"
}

# 3. Delete app_offline.htm
Write-Output "Restarting application pool..."
$request = [System.Net.FtpWebRequest]::Create("ftp://lagbar.runasp.net/wwwroot/app_offline.htm")
$request.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
$request.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
try {
    $response = $request.GetResponse()
    $response.Close()
    Write-Output "app_offline.htm deleted. Site is online."
} catch {
    Write-Output "Failed to delete app_offline.htm: $_"
}

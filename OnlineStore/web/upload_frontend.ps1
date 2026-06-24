$ErrorActionPreference = "Stop"

# --- Configurations ---
$ftpServer = "ftp://lagbar.runasp.net"
$ftpUser = $env:FTP_USER
$ftpPass = $env:FTP_PASS
$ftpRootPath = "/wwwroot/wwwroot" # This targets the nested wwwroot folder on IIS
$localPath = "d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\backend\OnlineStore.Api\wwwroot"
$webDir = "d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\web\online-store-web"

if ([string]::IsNullOrWhiteSpace($ftpUser) -or [string]::IsNullOrWhiteSpace($ftpPass)) {
    throw "Set FTP_USER and FTP_PASS environment variables before running this script."
}

# Step 1: Build the Vite frontend React application
Write-Host "=== 1. Building Vite React App ===" -ForegroundColor Cyan
Push-Location $webDir
try {
    # Check if npm dependencies exist
    if (-not (Test-Path "node_modules")) {
        Write-Host "  Installing npm dependencies..." -ForegroundColor Yellow
        npm install
    }
    Write-Host "  Running npm run build..." -ForegroundColor Yellow
    npm run build
} finally {
    Pop-Location
}
Write-Host "  Build successful! Output is generated in $localPath" -ForegroundColor Green

# Step 2: Upload compiled static assets via FTP
Write-Host "`n=== 2. Starting FTP Upload to lagbar.runasp.net ===" -ForegroundColor Cyan
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
        $request.EnableSsl = $false
        
        $response = $request.GetResponse()
        $response.Close()
    }
    catch {
        # Directory might already exist, which is fine
    }
}

# Find all files and directories
$allFiles = Get-ChildItem -Path $localPath -Recurse -File
$allDirs = Get-ChildItem -Path $localPath -Recurse -Directory
$totalFiles = $allFiles.Count
$currentFile = 0
$failedFiles = @()

Write-Host "  Found $totalFiles files to upload." -ForegroundColor Yellow

# Create all necessary subdirectories remotely
foreach ($dir in $allDirs) {
    $relativePath = $dir.FullName.Substring($localPath.Length).Replace("\", "/")
    $remoteDirPath = "$ftpRootPath$relativePath"
    Write-Host "  Creating directory: $remoteDirPath" -ForegroundColor DarkGray
    Create-FtpDirectory -RemotePath $remoteDirPath
}

# Upload files one by one
foreach ($file in $allFiles) {
    $currentFile++
    $relativePath = $file.FullName.Substring($localPath.Length).Replace("\", "/")
    $remotePath = "$ftpRootPath$relativePath"
    
    $percent = [math]::Round(($currentFile / $totalFiles) * 100, 1)
    Write-Host "  [$currentFile/$totalFiles] ($percent%) Uploading: $relativePath..." -NoNewline
    
    $success = Upload-FtpFile -LocalFile $file.FullName -RemotePath $remotePath
    if ($success) {
        Write-Host " OK" -ForegroundColor Green
    } else {
        $failedFiles += $relativePath
    }
}

Write-Host "`n=== Upload Complete ===" -ForegroundColor Cyan
Write-Host "  Total files: $totalFiles" -ForegroundColor White
Write-Host "  Successful: $($totalFiles - $failedFiles.Count)" -ForegroundColor Green

if ($failedFiles.Count -gt 0) {
    Write-Host "  Failed: $($failedFiles.Count)" -ForegroundColor Red
    foreach ($f in $failedFiles) {
        Write-Host "    - $f" -ForegroundColor Red
    }
    exit 1
} else {
    Write-Host "`n  All static assets uploaded successfully to lagbar.runasp.net!" -ForegroundColor Green
}

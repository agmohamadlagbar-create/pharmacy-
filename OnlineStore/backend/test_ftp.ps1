$user = $env:FTP_USER
$pass = $env:FTP_PASS

if ([string]::IsNullOrWhiteSpace($user) -or [string]::IsNullOrWhiteSpace($pass)) {
    throw "Set FTP_USER and FTP_PASS environment variables before running this script."
}

$hosts = @("ftp://lagbar.runasp.net/", "ftp://site69773.runasp.net/", "ftp://ftp.runasp.net/")

foreach ($ftpHost in $hosts) {
    Write-Output "Trying $ftpHost ..."
    try {
        $request = [System.Net.FtpWebRequest]::Create($ftpHost)
        $request.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
        $request.Timeout = 5000
        $response = $request.GetResponse()
        $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
        $files = $reader.ReadToEnd()
        $reader.Close()
        $response.Close()
        Write-Output "SUCCESS with $ftpHost"
        Write-Output "Files:"
        Write-Output $files
        exit 0
    } catch {
        Write-Output "Failed: $_"
    }
}
Write-Output "All attempts failed."
exit 1

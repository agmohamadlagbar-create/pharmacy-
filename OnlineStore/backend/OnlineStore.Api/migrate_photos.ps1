#!/usr/bin/env pwsh
# ────────────────────────────────────────────────────────────
#  migrate_photos.ps1 — One-time migration script
#  Moves existing images from wwwroot/uploads/ to storage/
#  and updates the database URLs from /uploads/... to /media/...
# ────────────────────────────────────────────────────────────

param(
    [string]$ContentRoot = "d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\backend\OnlineStore.Api",
    [string]$ConnectionString = "Server=localhost\SQLEXPRESS;Database=LagbarOnlineStore;Trusted_Connection=True;TrustServerCertificate=True"
)

$uploadsRoot = Join-Path $ContentRoot "wwwroot\uploads"
$storageRoot = Join-Path $ContentRoot "storage"

Write-Host "=== Photo Storage Migration Script ===" -ForegroundColor Cyan
Write-Host "Uploads root: $uploadsRoot"
Write-Host "Storage root: $storageRoot"
Write-Host ""

# ── Step 1: Check if uploads folder exists ──
if (-not (Test-Path $uploadsRoot)) {
    Write-Host "No uploads folder found at '$uploadsRoot'. Nothing to migrate." -ForegroundColor Yellow
    exit 0
}

# ── Step 2: Move files from wwwroot/uploads/ to storage/ ──
# Map old folder → new folder
$folderMap = @{
    "products"   = "Unknown\Unknown\products\0\original"
    "profiles"   = "Unknown\Unknown\users"
    "categories" = "Unknown\Unknown\categories"
    "ads"        = "Unknown\Unknown\ads"
    "stores"     = "Unknown\Unknown\stores"
}

$movedFiles = @{}  # Old relative path → New storage key

Write-Host "── Step 1: Copying files ──" -ForegroundColor Green
foreach ($oldSubdir in (Get-ChildItem -Path $uploadsRoot -Directory -ErrorAction SilentlyContinue)) {
    $subName = $oldSubdir.Name  # e.g., "products", "profiles", "categories", "ads"
    $newSubdir = $folderMap[$subName]
    
    if (-not $newSubdir) {
        $newSubdir = "Unknown\Unknown\$subName"
    }

    # Handle profiles specially — they have user-id subdirectories
    if ($subName -eq "profiles") {
        foreach ($userDir in (Get-ChildItem -Path $oldSubdir.FullName -Directory -ErrorAction SilentlyContinue)) {
            $userId = $userDir.Name
            $destDir = Join-Path $storageRoot "Unknown\Unknown\users\$userId\avatar\original"
            if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
            
            foreach ($file in (Get-ChildItem -Path $userDir.FullName -File -ErrorAction SilentlyContinue)) {
                $destFile = Join-Path $destDir $file.Name
                Copy-Item -Path $file.FullName -Destination $destFile -Force
                
                $oldRelPath = "/uploads/profiles/$userId/$($file.Name)"
                $newStorageKey = "Unknown/Unknown/users/$userId/avatar/original/$($file.Name)"
                $movedFiles[$oldRelPath] = $newStorageKey
                Write-Host "  [COPY] $oldRelPath → /media/$newStorageKey"
            }
        }
        continue
    }

    # Handle products — they have regionCode/categoryCode subdirectories
    if ($subName -eq "products") {
        foreach ($regionDir in (Get-ChildItem -Path $oldSubdir.FullName -Directory -ErrorAction SilentlyContinue)) {
            foreach ($catDir in (Get-ChildItem -Path $regionDir.FullName -Directory -ErrorAction SilentlyContinue)) {
                $destDir = Join-Path $storageRoot "Unknown\Unknown\products\0\original"
                if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
                
                foreach ($file in (Get-ChildItem -Path $catDir.FullName -File -ErrorAction SilentlyContinue)) {
                    $destFile = Join-Path $destDir $file.Name
                    Copy-Item -Path $file.FullName -Destination $destFile -Force
                    
                    $oldRelPath = "/uploads/products/$($regionDir.Name)/$($catDir.Name)/$($file.Name)"
                    $newStorageKey = "Unknown/Unknown/products/0/original/$($file.Name)"
                    $movedFiles[$oldRelPath] = $newStorageKey
                    Write-Host "  [COPY] $oldRelPath → /media/$newStorageKey"
                }
            }
            # Also check for files directly in the region dir
            foreach ($file in (Get-ChildItem -Path $regionDir.FullName -File -ErrorAction SilentlyContinue)) {
                $destDir = Join-Path $storageRoot "Unknown\Unknown\products\0\original"
                if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
                
                $destFile = Join-Path $destDir $file.Name
                Copy-Item -Path $file.FullName -Destination $destFile -Force
                
                $oldRelPath = "/uploads/products/$($regionDir.Name)/$($file.Name)"
                $newStorageKey = "Unknown/Unknown/products/0/original/$($file.Name)"
                $movedFiles[$oldRelPath] = $newStorageKey
                Write-Host "  [COPY] $oldRelPath → /media/$newStorageKey"
            }
        }
        continue
    }

    # Default: flat folder (categories, ads, etc.)
    $destDir = Join-Path $storageRoot $newSubdir
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }

    foreach ($file in (Get-ChildItem -Path $oldSubdir.FullName -File -Recurse -ErrorAction SilentlyContinue)) {
        $destFile = Join-Path $destDir $file.Name
        Copy-Item -Path $file.FullName -Destination $destFile -Force
        
        $relativeFromUploads = $file.FullName.Substring($uploadsRoot.Length).Replace('\', '/')
        $oldRelPath = "/uploads$relativeFromUploads"
        $newStorageKey = ($newSubdir.Replace('\', '/')) + "/$($file.Name)"
        $movedFiles[$oldRelPath] = $newStorageKey
        Write-Host "  [COPY] $oldRelPath → /media/$newStorageKey"
    }
}

Write-Host ""
Write-Host "── Step 2: Updating database URLs ──" -ForegroundColor Green
Write-Host "Total files moved: $($movedFiles.Count)"

# ── Step 3: Update DB references from /uploads/... to /media/... ──
# We need to update:
# 1. Products.ImageUrl
# 2. Products.AdditionalImageUrls (JSON array)
# 3. AdBanners.ImageUrl
# 4. Categories.ImageUrl
# 5. AspNetUsers.ProfilePictureUrl

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $conn.Open()
    Write-Host "  Connected to database." -ForegroundColor Green

    # Update Products.ImageUrl
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Id, ImageUrl FROM Products WHERE ImageUrl IS NOT NULL AND ImageUrl LIKE '/uploads/%'"
    $reader = $cmd.ExecuteReader()
    $productUpdates = @()
    while ($reader.Read()) {
        $productUpdates += @{ Id = $reader["Id"]; OldUrl = $reader["ImageUrl"] }
    }
    $reader.Close()

    foreach ($pu in $productUpdates) {
        $newUrl = $null
        foreach ($key in $movedFiles.Keys) {
            if ($pu.OldUrl -eq $key) {
                $newUrl = "/media/$($movedFiles[$key])"
                break
            }
        }
        if (-not $newUrl) {
            # If file not found in map, just convert the path pattern
            $newUrl = $pu.OldUrl -replace "^/uploads/", "/media/Unknown/Unknown/"
        }
        
        $updateCmd = $conn.CreateCommand()
        $updateCmd.CommandText = "UPDATE Products SET ImageUrl = @newUrl WHERE Id = @id"
        $updateCmd.Parameters.AddWithValue("@newUrl", $newUrl) | Out-Null
        $updateCmd.Parameters.AddWithValue("@id", $pu.Id) | Out-Null
        $updateCmd.ExecuteNonQuery() | Out-Null
        Write-Host "  [Products] Id=$($pu.Id): $($pu.OldUrl) → $newUrl"
    }

    # Update Products.AdditionalImageUrls (JSON)
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Id, AdditionalImageUrls FROM Products WHERE AdditionalImageUrls IS NOT NULL AND AdditionalImageUrls LIKE '%/uploads/%'"
    $reader = $cmd.ExecuteReader()
    $additionalUpdates = @()
    while ($reader.Read()) {
        $additionalUpdates += @{ Id = $reader["Id"]; OldJson = $reader["AdditionalImageUrls"] }
    }
    $reader.Close()

    foreach ($au in $additionalUpdates) {
        $newJson = $au.OldJson -replace "/uploads/", "/media/Unknown/Unknown/"
        $updateCmd = $conn.CreateCommand()
        $updateCmd.CommandText = "UPDATE Products SET AdditionalImageUrls = @newJson WHERE Id = @id"
        $updateCmd.Parameters.AddWithValue("@newJson", $newJson) | Out-Null
        $updateCmd.Parameters.AddWithValue("@id", $au.Id) | Out-Null
        $updateCmd.ExecuteNonQuery() | Out-Null
        Write-Host "  [Products.Additional] Id=$($au.Id) updated"
    }

    # Update AdBanners.ImageUrl
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Id, ImageUrl FROM AdBanners WHERE ImageUrl LIKE '/uploads/%'"
    $reader = $cmd.ExecuteReader()
    $adUpdates = @()
    while ($reader.Read()) {
        $adUpdates += @{ Id = $reader["Id"]; OldUrl = $reader["ImageUrl"] }
    }
    $reader.Close()

    foreach ($ad in $adUpdates) {
        $newUrl = $null
        foreach ($key in $movedFiles.Keys) {
            if ($ad.OldUrl -eq $key) {
                $newUrl = "/media/$($movedFiles[$key])"
                break
            }
        }
        if (-not $newUrl) { $newUrl = $ad.OldUrl -replace "^/uploads/", "/media/Unknown/Unknown/" }
        
        $updateCmd = $conn.CreateCommand()
        $updateCmd.CommandText = "UPDATE AdBanners SET ImageUrl = @newUrl WHERE Id = @id"
        $updateCmd.Parameters.AddWithValue("@newUrl", $newUrl) | Out-Null
        $updateCmd.Parameters.AddWithValue("@id", $ad.Id) | Out-Null
        $updateCmd.ExecuteNonQuery() | Out-Null
        Write-Host "  [AdBanners] Id=$($ad.Id): $($ad.OldUrl) → $newUrl"
    }

    # Update Categories.ImageUrl
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Id, ImageUrl FROM Categories WHERE ImageUrl IS NOT NULL AND ImageUrl LIKE '/uploads/%'"
    $reader = $cmd.ExecuteReader()
    $catUpdates = @()
    while ($reader.Read()) {
        $catUpdates += @{ Id = $reader["Id"]; OldUrl = $reader["ImageUrl"] }
    }
    $reader.Close()

    foreach ($cu in $catUpdates) {
        $newUrl = $null
        foreach ($key in $movedFiles.Keys) {
            if ($cu.OldUrl -eq $key) {
                $newUrl = "/media/$($movedFiles[$key])"
                break
            }
        }
        if (-not $newUrl) { $newUrl = $cu.OldUrl -replace "^/uploads/", "/media/Unknown/Unknown/" }
        
        $updateCmd = $conn.CreateCommand()
        $updateCmd.CommandText = "UPDATE Categories SET ImageUrl = @newUrl WHERE Id = @id"
        $updateCmd.Parameters.AddWithValue("@newUrl", $newUrl) | Out-Null
        $updateCmd.Parameters.AddWithValue("@id", $cu.Id) | Out-Null
        $updateCmd.ExecuteNonQuery() | Out-Null
        Write-Host "  [Categories] Id=$($cu.Id): $($cu.OldUrl) → $newUrl"
    }

    # Update AspNetUsers.ProfilePictureUrl
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Id, ProfilePictureUrl FROM AspNetUsers WHERE ProfilePictureUrl IS NOT NULL AND ProfilePictureUrl LIKE '/uploads/%'"
    $reader = $cmd.ExecuteReader()
    $userUpdates = @()
    while ($reader.Read()) {
        $userUpdates += @{ Id = $reader["Id"]; OldUrl = $reader["ProfilePictureUrl"] }
    }
    $reader.Close()

    foreach ($uu in $userUpdates) {
        $newUrl = $null
        foreach ($key in $movedFiles.Keys) {
            if ($uu.OldUrl -eq $key) {
                $newUrl = "/media/$($movedFiles[$key])"
                break
            }
        }
        if (-not $newUrl) { $newUrl = $uu.OldUrl -replace "^/uploads/", "/media/Unknown/Unknown/" }
        
        $updateCmd = $conn.CreateCommand()
        $updateCmd.CommandText = "UPDATE AspNetUsers SET ProfilePictureUrl = @newUrl WHERE Id = @id"
        $updateCmd.Parameters.AddWithValue("@newUrl", $newUrl) | Out-Null
        $updateCmd.Parameters.AddWithValue("@id", $uu.Id) | Out-Null
        $updateCmd.ExecuteNonQuery() | Out-Null
        Write-Host "  [Users] Id=$($uu.Id): $($uu.OldUrl) → $newUrl"
    }

    $conn.Close()
    Write-Host ""
    Write-Host "=== Migration Complete ===" -ForegroundColor Cyan
    Write-Host "Files copied: $($movedFiles.Count)"
    Write-Host "Products updated: $($productUpdates.Count)"
    Write-Host "AdBanners updated: $($adUpdates.Count)"
    Write-Host "Categories updated: $($catUpdates.Count)"
    Write-Host "Users updated: $($userUpdates.Count)"
    Write-Host ""
    Write-Host "NOTE: The old wwwroot/uploads/ folder has been preserved. You can delete it manually after verifying everything works." -ForegroundColor Yellow
} catch {
    Write-Host "ERROR: $_" -ForegroundColor Red
    if ($conn) { $conn.Close() }
    exit 1
}

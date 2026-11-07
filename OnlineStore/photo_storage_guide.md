# Photo Storage Guide
**Project:** Online Store — C# Backend / React Frontend  
**Purpose:** AI-readable instruction set for storing, organizing, serving, backing up, and restoring photos.

---

## 🛡️ IMAGE PIPELINE RULES (Mandatory)

Before any photo is stored, the system **MUST** enforce these processing steps in this exact order:

### STEP 1 — VALIDATION (Synchronous)
- **Magic Bytes**: Verify the file is a real image by checking magic bytes (not just the extension).
- **Accepted MIME types**: `image/jpeg`, `image/png`, `image/webp`, `image/heic`, `image/heif`.
- **Size Limit**: Reject any file exceeding **20MB**.
- **Integrity**: Reject corrupted or unreadable files.
- **Fail Fast**: Return a clear error message to the user if validation fails — do NOT proceed.

### STEP 2 — EXIF STRIPPING (Synchronous)
- **Correction**: Read the EXIF orientation tag FIRST and auto-rotate the image to correct orientation.
- **Privacy**: Strip **ALL** EXIF metadata (GPS, device info, timestamps, etc.) before storage.
- **No exceptions**: No metadata should survive into storage under any circumstance.

### STEP 3 — STORE ORIGINAL (Synchronous)
- **Format Conversion**: If HEIC/HEIF is uploaded, convert to **JPEG** before storing as the original.
- **Structure**: Store the cleaned original using:  
  `{Country}/{City}/{Category}/{ID}/original/{filename_yyyyMMdd_HHmmss.ext}`
- **Database**: Write the `storage_key` (relative path with forward slashes) immediately.

### STEP 4 — ASYNC PROCESSING (Non-blocking)
Trigger a background job to generate processed versions:
- **Formats**: Convert all sizes to **WebP (Quality: 82)**.
- **Sizes**:
  - `thumb`: 200×200px (center crop)
  - `medium`: 800px wide (preserve aspect ratio)
  - `large`: 1600px wide (preserve aspect ratio)
- **Path**: `{Country}/{City}/{Category}/{ID}/processed/{size}_{filename}.webp`
- **Retry**: Log errors and retry up to **3 times**. Do NOT delete the original on failure.

---

## 1. Location Resolution

Extract two values from the user's location (provided via OpenMap API):

| Field     | Example      | Usage             |
|-----------|--------------|-------------------|
| `country` | `USA`        | Top-level folder  |
| `city`    | `NewYork`    | Second-level folder |

### Sanitization Rules
- Remove all spaces: `New York` → `NewYork`
- Remove all special characters: `São Paulo` → `SaoPaulo`
- Enforce PascalCase: `new york` → `NewYork`

---

## 2. Folder Structure

**Storage root:** Configurable in `appsettings.json` as `StorageRoot`.

```
/storage/
  /{Country}/
    /{City}/
      /products/
        /{ID}/
          /original/          ← Raw upload (Cleaned). NEVER delete.
          /processed/         ← Generated WebP sizes.
      /users/
        /{ID}/
          /avatar/
            /original/
            /processed/
```

---

## 3. Database Schema

### Table: `product_images`
| Column | Type | Description |
|--------|------|-------------|
| `image_type` | ENUM | `original`, `thumb`, `medium`, `large` |
| `storage_key` | VARCHAR | Relative path from root (Forward slashes only `/`) |

---

## 4. Serving Images

- **Rule**: Never serve the original file directly if a processed version exists.
- **Standard**: Serve the `large` version for details and `thumb` for lists.
- **URL Pattern**: `BASE_URL + "/" + storage_key`

---

## 5. Security & Privacy Reminders

1. **Magic Bytes**: Never trust the user-provided extension.
2. **Metadata**: Always strip EXIF to prevent location leaks from user photos.
3. **Background**: Step 4 must never block the API response.

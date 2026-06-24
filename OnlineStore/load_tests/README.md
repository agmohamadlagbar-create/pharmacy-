# OnlineStore Load Testing (k6)

This directory contains load testing scripts for the OnlineStore backend and frontend using [k6](https://k6.io/).

## Prerequisites

You can run these scripts either by installing k6 locally, or by using Docker.

### Option 1: Install k6 natively
- **Windows**: `winget install k6` or download the installer from [k6.io](https://k6.io/docs/getting-started/installation/)
- **Mac**: `brew install k6`
- **Linux**: See the [installation guide](https://k6.io/docs/getting-started/installation/)

### Option 2: Use Docker
If you have Docker installed, you don't need to install k6 natively. You can use the `npm run test:docker` command.

## Setup

First, navigate to this directory (`load_tests`) and run:

```bash
npm install
```
This is only necessary to install type definitions for better IDE support.

## Running the tests

### Testing public endpoints only
If you don't provide credentials, the script will only test the public API endpoints (`/api/Products`, `/api/Categories`) and the frontend homepage, and will skip the authenticated portions.

**Native k6:**
```bash
npm run test:local
# Or directly:
k6 run scenarios/browse_and_checkout.js
```

**Docker:**
```bash
npm run test:docker
```

### Testing authenticated endpoints (Login, Profile, Orders)
To test the login flow and authenticated API requests, provide test credentials via environment variables:

**Native k6:**
```bash
# Windows (PowerShell)
$env:TEST_EMAIL="your_test_user@example.com"
$env:TEST_PASSWORD="YourTestPassword123!"
k6 run scenarios/browse_and_checkout.js
```

**Docker:**
```bash
# Windows (PowerShell)
docker run --rm -i -e TEST_EMAIL="your_test_user@example.com" -e TEST_PASSWORD="YourTestPassword123!" grafana/k6 run - < scenarios/browse_and_checkout.js
```

## Overriding the load profile

By default, the script ramps up to 5 users over 10 seconds, stays for 30 seconds, and then ramps down. 
To do a quick smoke test with just 1 user for 5 seconds, use:

```bash
k6 run --vus 1 --duration 5s scenarios/browse_and_checkout.js
```

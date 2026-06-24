# Sharing the Project with Ngrok

To share your local Online Store with external users, follow these steps to set up the two ngrok tunnels.

## 1. Start Your Services
First, ensure your local projects are running:

- **Backend**: Run in `d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\backend\OnlineStore.Api`
- **Web App**: Run in `d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\web\online-store-web`

## 2. Expose with Ngrok
Open two separate terminal windows and run:

**Terminal 1 (Backend API):**
```bash
ngrok http 5000
```
*Take note of the `Forwarding` URL (e.g., `https://abc-123.ngrok-free.dev`).*

**Terminal 2 (Web App):**
```bash
ngrok http 5175
```
*Take note of the `Forwarding` URL.*

## 3. Configure the Frontend
For the Web App to communicate with the Public API URL, you must update the environment configuration:

1. Open `online-store-web/.env`.
2. Update the `VITE_API_BASE_URL` with your **Backend Ngrok URL**:
   ```env
   VITE_API_BASE_URL=https://abc-123.ngrok-free.dev
   ```
3. **Restart the Web App** (Vite) so it picks up the new environment variable.

## 4. Share!
You can now share the **Web App Ngrok URL** with anyone. They will be able to access the site and it will correctly route all API requests to your local machine via ngrok.

> [!IMPORTANT]
> Because you are using the free tier of ngrok, the URLs change every time you restart ngrok. Remember to update the `.env` file whenever you get a new backend URL.

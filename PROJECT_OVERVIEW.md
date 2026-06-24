# OnlineStore Project Overview

## 🏗 Architecture & Stack
This is a modern, multi-platform e-commerce application divided into three main components:

1. **Backend (API)**
   - **Framework:** ASP.NET Core Web API (.NET 8, C#)
   - **Database:** SQL Server
   - **ORM:** Entity Framework Core (Code-First)
   - **Authentication:** ASP.NET Identity + JWT (JSON Web Tokens)
   - **Real-Time Communication:** SignalR (for live location and order status tracking)

2. **Web Frontend**
   - **Framework:** React (built with Vite)
   - **Features:** User and Shop interfaces, login/signup flows, real-time map tracking with Leaflet.

3. **Mobile App**
   - **Framework:** Flutter (Android)
   - **Features:** Native mobile interfaces for customers and delivery agents.

---

## ✨ Key Features

### 1. Authentication & Role Management
- Secure user registration and login using JWT.
- Support for multiple user roles: **Customer**, **Shop Owner**, and **Delivery Agent**.
- Role toggling and specific dashboards depending on the logged-in user's role.

### 2. Real-Time Order Status Tracking
- **Lifecycle Tracking:** Orders transition through a detailed lifecycle:
  - `Pending` (Order placed / Customer paid)
  - `Preparing` (Shop accepts)
  - `Ready for Pickup` (Shop finishes preparation)
  - `Assigned` (Delivery agent accepts job)
  - `Picked Up` (Agent collects the order)
  - `On the Way` (Agent is heading to customer)
  - `Delivered` (Order complete)
- **Live Updates:** Status changes are broadcasted instantly to the customer's web or mobile app using SignalR, requiring no page refreshes. Push notifications are also integrated.

### 3. Real-Time Delivery Location Tracking
- **Live Map:** Customers can watch their delivery agent's real-time movement on a map.
- **Technology:** Uses Leaflet/React-Leaflet on the frontend and SignalR `TrackingHub` on the backend.
- **Agent Tracking:** Delivery agents stream their GPS coordinates (or via simulation mode during testing) directly to the server, which broadcasts it to the specific customer's active session.

### 4. Payments
- Integration capabilities for order checkouts and status notifications from payment to shop preparation.

### 5. Media & Photo Storage
- Support for uploading and storing photos (e.g., product images, user avatars).

### 6. Internationalization
- Includes translation synchronization (`sync_translations.py`) for supporting multiple languages across the platform.

### 7. Load Testing & Infrastructure
- Dedicated `load_tests` scripts to ensure the backend and real-time hubs scale appropriately.
- Support for local testing using tools like `ngrok` for webhooks or external access, and `LocalDB` for SQL Server.

---

## 🚀 Running the Project

### Backend
Navigate to `backend/OnlineStore.Api` and run:
```bash
dotnet restore
dotnet ef database update --project ../OnlineStore.Infrastructure --startup-project .
dotnet run
```
Access Swagger UI at `http://localhost:5000/swagger`.

### Web
Navigate to `web/online-store-web` and run:
```bash
npm install
npm run dev
```

### Mobile
Navigate to `mobile/online_store_mobile` and run:
```bash
flutter pub get
flutter run
```

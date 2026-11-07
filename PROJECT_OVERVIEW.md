# OnlineStore Project Overview

## 🏗 Architecture & Stack
This is a modern e-commerce application divided into two main components:

1. **Backend (API)**
   - **Framework:** ASP.NET Core Web API (.NET 8, C#)
   - **Database:** SQL Server
   - **ORM:** Entity Framework Core (Code-First)
   - **Authentication:** ASP.NET Identity + JWT (JSON Web Tokens)
   - **Real-Time Communication:** SignalR (for notifications and order status updates)

2. **Web Frontend**
   - **Framework:** React (built with Vite)
   - **Features:** User, Shop, Delivery, and Admin interfaces, login/signup flows, and interactive address selection.

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
- **Live Updates:** Status changes are broadcasted instantly to the customer's web app using SignalR, requiring no page refreshes.

### 3. Payments
- Integration capabilities for order checkouts and status notifications from payment to shop preparation.

### 4. Media & Photo Storage
- Support for uploading and storing photos (e.g., product images, user avatars).

### 5. Internationalization
- Includes translation synchronization (`sync_translations.py`) for supporting multiple languages across the platform.

### 6. Load Testing & Infrastructure
- Dedicated `load_tests` scripts to ensure the backend and real-time hubs scale appropriately.
- Support for local testing using `LocalDB` or SQL Server.

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

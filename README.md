# LAGBAR Online Store

A full-stack multi-vendor e-commerce platform featuring role-based dashboards (**Customer**, **Shop Owner**, **Delivery Agent**, and **Admin**), real-time order tracking via **SignalR**, **Stripe** payments, and automated media processing.

---

## 🏗 Architecture & Tech Stack

### 1. Backend (`OnlineStore/backend`)
- **Framework:** ASP.NET Core Web API (.NET 8, C#)
- **Database:** SQL Server (`SQLEXPRESS` / LocalDB) with Entity Framework Core (Code-First)
- **Authentication:** ASP.NET Identity + JWT (Access + Refresh Tokens)
- **Real-Time Communication:** SignalR (`/hubs/notifications`)
- **Payments:** Stripe (`Stripe.net`) + Cash on Delivery (COD)
- **Image Pipeline:** SixLabors ImageSharp + HEIF/WebP background processing
- **Reporting & Logging:** QuestPDF, ClosedXML, Serilog

### 2. Web Frontend (`OnlineStore/web/online-store-web`)
- **Framework:** React 18 + Vite
- **Routing & State:** React Router v7, Axios with automatic JWT refresh interceptor
- **Features:** Multi-language support (`en`, `ar`, `tr` via `i18next`), interactive maps (`Leaflet`), analytics charts (`Recharts`), Stripe Elements, and Lottie animations

---

## 📋 Prerequisites

Before running the project for the first time, ensure you have installed:
1. **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)**
2. **[SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)** (default instance: `localhost\SQLEXPRESS`) or SQL Server LocalDB
3. **[Node.js](https://nodejs.org/)** (v18+ recommended) and `npm`

---

## 🚀 First-Time Setup & Running the Project

### Step 1: Run the Backend API
Open a terminal from the repository root and run:

```bash
cd OnlineStore/backend/OnlineStore.Api
dotnet restore
dotnet run
```

**What happens automatically on first launch:**
- Runs on **`http://localhost:5000`** (`Swagger UI`: `http://localhost:5000/swagger`)
- Automatically creates the `LagbarOnlineStore` database and applies the initial schema migration ([`InitialLagbarStoreSchema.cs`](OnlineStore/backend/OnlineStore.Infrastructure/Migrations/InitialLagbarStoreSchema.cs))
- Seeds the 4 system roles (`User`, `Shop`, `Delivery`, `Admin`)
- Seeds the 6 default product categories (`Food`, `Beverages`, `Cleaning Supplies`, `House Needs`, `Electronics`, `Clothing`)
- Seeds the default **Admin** account in Development mode:
  - **Email:** `admin@lagbar.com`
  - **Password:** `Admin1234!`
- Logs email verification codes directly to the backend console in Development mode (`📧 [DEV EMAIL]`) in addition to sending via SMTP

> **Using a different SQL Server instance?**  
> Update `ConnectionStrings:DefaultConnection` in [`OnlineStore/backend/OnlineStore.Api/appsettings.json`](OnlineStore/backend/OnlineStore.Api/appsettings.json) (for example, `Server=(localdb)\\mssqllocaldb;Database=LagbarOnlineStore;Trusted_Connection=True;TrustServerCertificate=True`).

---

### Step 2: Run the Web Frontend
Open a second terminal from the repository root and run:

```bash
cd OnlineStore/web/online-store-web
npm install
npm run dev
```

- Opens at **`http://localhost:5175`**
- The Vite dev server automatically proxies `/api`, `/hubs`, `/media`, and `/uploads` requests to `http://localhost:5000`.

---

## ✨ Key Workflows

### Order Lifecycle & Real-Time Tracking
Orders transition through the following states with instant SignalR notifications:
1. `Pending` — Order placed by customer
2. `Preparing` — Accepted by Shop Owner
3. `ReadyForPickup` — Prepared by Shop Owner and visible to Delivery Agents
4. `Assigned` — Accepted by a Delivery Agent
5. `PickedUp` — Collected from the shop(s)
6. `OnTheWay` — Out for delivery to the customer
7. `Delivered` — Completed

### Additional Guides
- **Payment Integration:** [`OnlineStore/payment_guide.md`](OnlineStore/payment_guide.md)
- **Photo Storage Pipeline:** [`OnlineStore/photo_storage_guide.md`](OnlineStore/photo_storage_guide.md)
- **Load Testing (k6):** [`OnlineStore/load_tests/README.md`](OnlineStore/load_tests/README.md)

---

## 🧪 Running Tests

### Backend Unit & Integration Tests
```bash
cd OnlineStore/backend
dotnet test
```

### Frontend Unit Tests (Vitest)
```bash
cd OnlineStore/web/online-store-web
npm test
```

# OnlineStore (Login/Signup Starter)

Stack:
- Backend: ASP.NET Core Web API (C#) + EF Core Code-First + SQL Server + ASP.NET Identity + JWT
- Web: React (Vite) UI for login/signup with Shop/User toggle

---

## 1) Backend setup

### Requirements
- .NET 8 SDK
- SQL Server (LocalDB or full SQL Server)

### Configure
Edit:
`backend/OnlineStore.Api/appsettings.json`

1. Set `ConnectionStrings:DefaultConnection` to your SQL Server.
2. Set `Jwt:Key` to a long random secret (32+ chars).

### Run migrations / create DB
From the backend folder:

```bash
cd backend/OnlineStore.Api
dotnet tool install --global dotnet-ef
dotnet restore
dotnet ef migrations add InitialAuth --project ../OnlineStore.Infrastructure --startup-project .
dotnet ef database update --project ../OnlineStore.Infrastructure --startup-project .
dotnet run
```

Swagger will open at:
`http://localhost:5000/swagger`

---

## 2) Web (React)

```bash
cd web/online-store-web
npm install
npm run dev
```

Set API URL (optional):
Create `.env.local` in `web/online-store-web` (or copy `.env.example`):

```
VITE_API_BASE_URL=http://localhost:5000
```

# 📍 Real-Time Location Tracking — Local Testing & Production Guide

**Stack:** ASP.NET Core (C#) · React · SignalR · Leaflet (OpenStreetMap)

---

## 🧠 Overview

This document explains how the real-time location tracking system works, how we tested it locally using a manual simulation, and what needs to change before going to production.

The system is split into two responsibilities:

| Concern | Technology |
|---|---|
| Live GPS location streaming | SignalR (TrackingHub) |
| Order status updates (preparing, ready, delivered) | SignalR (TrackingHub) |
| Map rendering | Leaflet + React-Leaflet |

---

## 🏗️ Architecture

```
[Delivery Agent Browser]
        |
        | clicks map → new coordinates
        |
        ▼
[React: DeliverySimulationMap]
        |
        | hubConnection.invoke("SendLocation", orderId, lat, lng)
        |
        ▼
[ASP.NET Core: TrackingHub.SendLocation()]
        |
        | Clients.Group("order_{id}").SendAsync("location:updated", ...)
        |
        ▼
[React: OrderTrackingCard]
        |
        | trackingConnection.on("location:updated") → setDriverPos()
        |
        ▼
[Customer sees marker move on Leaflet map — no refresh]
```

---

## 🧪 How We Tested Locally (Simulation)

Because there is no real GPS device during development, we built a **manual map simulation** on the delivery agent's side.

### How It Works

1. The delivery agent accepts an order
2. A **modal map pops up** on the delivery agent's screen
3. The agent can **click anywhere on the map** to move their marker to that position
4. Every click immediately calls `SendLocation` on the SignalR hub
5. The customer's map updates **instantly** with no page refresh

### Two Testing Modes

| Mode | How |
|---|---|
| Manual (click to move) | Click anywhere on the delivery map — marker jumps to that point |
| Auto-move | Press "Auto-Move" button — marker moves 10% closer to customer every 2 seconds automatically |

### How to Run a Local Test

1. Open **two browser tabs** — both pointed at `localhost`
2. **Tab 1:** Log in as the **customer** — navigate to the active order page
3. **Tab 2:** Log in as the **delivery agent** — accept an order
4. The simulation map modal opens on Tab 2
5. Click anywhere on the delivery map
6. Watch the marker move on Tab 1 (customer side) instantly

---

## 📁 Key Files

### Backend (C#)

| File | Purpose |
|---|---|
| `Hubs/TrackingHub.cs` | SignalR hub — handles `SendLocation`, `JoinOrderGroup`, `LeaveOrderGroup`, `order:status_updated` |
| `Controllers/DeliveryController.cs` | Delivery job management — injects `IHubContext<TrackingHub>` for status broadcasts |

### Frontend (React)

| File | Purpose |
|---|---|
| `DeliverySimulationMap.jsx` | Simulation modal with click-to-move Leaflet map |
| `OrderTrackingCard.jsx` | Customer-facing map — listens to `location:updated` and `order:status_updated` |
| `DeliveryJobsPage.jsx` | Delivery agent dashboard — manages `simJob` state and renders simulation modal |

---

## ⚙️ SignalR Hub — Key Methods

```csharp
// Delivery agent calls this when marker moves
public async Task SendLocation(int orderId, double latitude, double longitude)
{
    await Clients.Group($"order_{orderId}")
                 .SendAsync("location:updated", new {
                     orderId,
                     latitude,
                     longitude,
                     status = "on_the_way"
                 });
}

// Customer calls this on component mount to subscribe to their order
public async Task JoinOrderGroup(int orderId)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
}

// Customer calls this on component unmount
public async Task LeaveOrderGroup(int orderId)
{
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
}
```

---

## ⚠️ Known Issues Fixed During Development

### 1. Customer Never Joined the Order Group
The `JoinOrderGroup` invoke was guarded by a `state === 'Connected'` check, but SignalR was still in `'Connecting'` state when the component mounted. The fix was a safe async `joinGroup()` helper that polls until connected:

```js
const joinGroup = async () => {
    while (trackingConnection.state === 'Connecting') {
        await new Promise(r => setTimeout(r, 100));
    }
    if (trackingConnection.state === 'Connected') {
        await trackingConnection.invoke('JoinOrderGroup', order.id);
    }
};
```

### 2. PascalCase vs camelCase Mismatch
C# SignalR was sending `{ OrderId, Latitude, Longitude }` (PascalCase) but React was reading `data.orderId` (camelCase). Fixed by adding camelCase JSON serialization in `Program.cs`:

```csharp
builder.Services.AddSignalR()
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });
```

### 3. Stale Closure in Simulation Loop
The `reportLocation` interval was reading stale state values because React closures capture values at the time of creation. Fixed by using refs that mirror state and reading from refs inside the interval function.

---

## 🚀 Production Checklist

When moving from local to production, the following changes are required:

### 1. Replace Manual Simulation with Real GPS

Remove `DeliverySimulationMap.jsx` entirely. Replace with real GPS using the browser Geolocation API on the delivery agent's mobile browser or app:

```js
// In DeliveryJobsPage — replace simulation with this:
useEffect(() => {
    if (!activeJob) return;

    const watchId = navigator.geolocation.watchPosition(
        (pos) => {
            hubConnection.invoke(
                'SendLocation',
                activeJob.orderId,
                pos.coords.latitude,
                pos.coords.longitude
            );
        },
        (err) => console.error('GPS error:', err),
        { enableHighAccuracy: true, maximumAge: 3000, timeout: 10000 }
    );

    return () => navigator.geolocation.clearWatch(watchId);
}, [activeJob]);
```

### 2. Secure the SignalR Hub

Currently the hub accepts any `SendLocation` call from any authenticated user. In production, validate that the caller is actually the delivery agent assigned to that order:

```csharp
public async Task SendLocation(int orderId, double latitude, double longitude)
{
    var userId = Context.UserIdentifier;

    // Validate this user is the assigned delivery agent for this order
    var isAssigned = await _db.DeliveryJobs
        .AnyAsync(j => j.OrderId == orderId && j.DeliveryUserId == userId &&
                       j.Status != DeliveryStatus.Delivered);

    if (!isAssigned) return; // Silently reject unauthorized calls

    await Clients.Group($"order_{orderId}")
                 .SendAsync("location:updated", new { orderId, latitude, longitude, status = "on_the_way" });
}
```

### 3. Persist Location to Database

Currently location is streamed in real time but not saved. For production, save each update to a `DeliveryLocations` table so the customer can see the agent's last known position when they first load the page (before any new update arrives):

```csharp
// In SendLocation, before broadcasting:
await _db.DeliveryLocations.AddAsync(new DeliveryLocation {
    OrderId = orderId,
    Latitude = latitude,
    Longitude = longitude,
    RecordedAt = DateTime.UtcNow
});
await _db.SaveChangesAsync();
```

Add a REST endpoint so the customer loads the last known position on mount:

```csharp
[HttpGet("api/orders/{orderId}/last-location")]
public async Task<IActionResult> GetLastLocation(int orderId)
{
    var loc = await _db.DeliveryLocations
        .Where(l => l.OrderId == orderId)
        .OrderByDescending(l => l.RecordedAt)
        .FirstOrDefaultAsync();

    return loc == null ? NotFound() : Ok(loc);
}
```

### 4. Configure SignalR for Scale (Azure SignalR Service)

Local SignalR runs in-process and only works on a single server. In production with multiple server instances, use **Azure SignalR Service** so all instances share the same connection backplane:

```csharp
// Program.cs
builder.Services.AddSignalR()
    .AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"])
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });
```

Add to `appsettings.Production.json`:
```json
"Azure": {
  "SignalR": {
    "ConnectionString": "YOUR_AZURE_SIGNALR_CONNECTION_STRING"
  }
}
```

### 5. Use HTTPS and WSS Only

SignalR over HTTP uses `ws://` — in production it must use `wss://` (WebSocket Secure). Ensure your production server has an SSL certificate and all SignalR connections go through HTTPS. The React client will automatically use `wss://` when connecting to an `https://` URL.

### 6. Handle Reconnection on the Customer Side

Add a reconnection handler so if the customer loses connection briefly (e.g., phone sleep), they automatically rejoin their order group:

```js
trackingConnection.onreconnected(async () => {
    await trackingConnection.invoke('JoinOrderGroup', order.id);
});
```

### 7. Environment Variables

Move all hardcoded values to environment variables:

| Variable | Local Value | Production |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5000` | `https://yourdomain.com` |
| SignalR Hub URL | `/hubs/tracking` | `/hubs/tracking` (same) |
| CORS allowed origins | `*` | Your exact frontend domain only |

---

## 📋 Production Readiness Summary

| Item | Status |
|---|---|
| SignalR hub set up | ✅ Done |
| Customer joins order group safely | ✅ Fixed |
| camelCase JSON serialization | ✅ Fixed |
| Stale closure bug in simulation | ✅ Fixed |
| Real GPS replacing simulation | ⬜ TODO |
| Hub authorization validation | ⬜ TODO |
| Location persistence to DB | ⬜ TODO |
| Azure SignalR Service (multi-server) | ⬜ TODO |
| HTTPS / WSS enforcement | ⬜ TODO |
| Reconnection handler | ⬜ TODO |
| Production environment variables | ⬜ TODO |

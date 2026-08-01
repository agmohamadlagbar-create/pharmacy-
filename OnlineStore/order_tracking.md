# 📦 Real-Time Order Status Tracking — Architecture & Fix Guide

**Stack:** ASP.NET Core (C#) · React · SignalR · SQL Server

---

## 🧠 Overview

This document covers how order status flows from payment to delivery, what notifications are sent at each step, and how to make the customer's `OrderTrackingCard` update in real time — without any page refresh.

---

## 🔄 Order Lifecycle

```
[Customer Pays]
       |
       | → Notification sent to Shop
       |----------------------------------------------------------\
       ▼                                                           ▼
[Shop: "Preparing"]                                       [Cancellation Flow]
       |                                                    |-- Customer cancels (Pending/Preparing)
       | → Notification sent to Customer                    |   └─ 95% Stripe refund + Stock restored
       ▼                                                    |-- Shop declines (Pending only + Reason)
[Shop: "Ready for Pickup"]                                  |   └─ 100% Stripe refund + Stock restored
       |                                                    ▼
       | → Notification sent to Delivery Agent            [OrderStatus = Cancelled]
       ▼                                                    | → Push notification sent to Customer
[Delivery: "Accepted Job"]                                  | → SignalR broadcast (with Reason & Role)
       |                                                    ▼
       | → Notification sent to Customer                  [DeliveredOrderCleanupService]
       ▼                                                    └─ Auto-purged from DB after 7 days
[Delivery: "Picked Up"]
       |
       | → Notification sent to Customer
       ▼
[Delivery: "On the Way"]
       |
       | → Notification sent to Customer + Location tracking starts
       ▼
[Delivery: "Delivered"]
       |
       | → Notification sent to Customer + Tracking card closes
       ▼
[DeliveredOrderCleanupService]
       └─ Auto-purged from DB after 7 days
```

---

## ❌ The Problem — Why Refresh Is Required

Every time the shop or delivery agent changes the order status, the backend saves to the database and sends a push notification — but it does **not** broadcast the new status via SignalR to the customer's active browser session.

The customer's `OrderTrackingCard` has no live connection to status changes. It only shows what was loaded when the page first rendered. To see an update, the customer must reload the page to re-fetch the order from the API.

**Root cause:** `order:status_updated` SignalR event is either not being sent from the backend on every status transition, or the customer's component is not listening for it correctly.

---

## ✅ The Fix — Broadcast Every Status Change via SignalR

### How It Should Work

```
[Shop changes status to "Preparing"]
         |
         | → DB saved ✅
         | → Push notification sent ✅
         | → SignalR broadcast to order group ← THIS IS MISSING
         |
         ▼
[Customer OrderTrackingCard receives "order:status_updated"]
         |
         | → Status label updates instantly
         | → No refresh needed ✅
```

---

## ⚙️ Backend Fixes (C#)

### Step 1 — Inject IHubContext Into Every Controller That Changes Status

Any controller that changes order or delivery job status must have access to the TrackingHub:

```csharp
private readonly IHubContext<TrackingHub> _trackingHub;

public OrdersController(
    ApplicationDbContext db,
    IHubContext<TrackingHub> trackingHub,
    INotificationService notif)
{
    _db = db;
    _trackingHub = trackingHub;
    _notif = notif;
}
```

### Step 2 — Broadcast After Every Status Change

Add the SignalR broadcast immediately after every `SaveChangesAsync()` call that changes an order or delivery status. Apply this pattern to all of the following transitions:

#### Shop: Order Accepted / Preparing

```csharp
[HttpPut("api/orders/{orderId}/status/preparing")]
public async Task<IActionResult> MarkPreparing(int orderId)
{
    var order = await _db.Orders.FindAsync(orderId);
    if (order == null) return NotFound();

    order.Status = OrderStatus.Preparing;
    await _db.SaveChangesAsync();

    // Send push notification to customer (already exists)
    await _notif.NotifyCustomerAsync(order.UserId, "Your order is being prepared!");

    // ✅ ADD THIS — broadcast status to customer's active session
    await _trackingHub.Clients.Group($"order_{orderId}")
        .SendAsync("order:status_updated", new {
            orderId,
            status = "preparing",
            label = "Preparing your order"
        });

    return Ok();
}
```

#### Shop: Ready for Pickup

```csharp
order.Status = OrderStatus.ReadyForPickup;
await _db.SaveChangesAsync();

await _trackingHub.Clients.Group($"order_{orderId}")
    .SendAsync("order:status_updated", new {
        orderId,
        status = "ready_for_pickup",
        label = "Ready for pickup"
    });
```

#### Delivery: Job Accepted

```csharp
job.Status = DeliveryStatus.Assigned;
await _db.SaveChangesAsync();

await _trackingHub.Clients.Group($"order_{job.OrderId}")
    .SendAsync("order:status_updated", new {
        orderId = job.OrderId,
        status = "assigned",
        label = "Delivery agent assigned"
    });
```

#### Delivery: Picked Up

```csharp
job.Status = DeliveryStatus.PickedUp;
await _db.SaveChangesAsync();

await _trackingHub.Clients.Group($"order_{job.OrderId}")
    .SendAsync("order:status_updated", new {
        orderId = job.OrderId,
        status = "picked_up",
        label = "Order picked up"
    });
```

#### Delivery: On the Way

```csharp
job.Status = DeliveryStatus.OnTheWay;
await _db.SaveChangesAsync();

await _trackingHub.Clients.Group($"order_{job.OrderId}")
    .SendAsync("order:status_updated", new {
        orderId = job.OrderId,
        status = "on_the_way",
        label = "On the way to you"
    });
```

#### Delivery: Delivered

```csharp
job.Status = DeliveryStatus.Delivered;
await _db.SaveChangesAsync();

await _trackingHub.Clients.Group($"order_{job.OrderId}")
    .SendAsync("order:status_updated", new {
        orderId = job.OrderId,
        status = "delivered",
        label = "Delivered!"
    });
```

### Step 3 — Ensure camelCase Serialization in Program.cs

All SignalR payloads must use camelCase so React can read them without issues:

```csharp
builder.Services.AddSignalR()
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });
```

---

## ⚛️ Frontend Fixes (React)

### OrderTrackingCard — Full Fixed useEffect

The component must safely join the order group and listen to both location and status events:

```jsx
export default function OrderTrackingCard({ order, onDismiss, trackingConnection }) {
    const [driverPos, setDriverPos] = useState(null);
    const [trackingStatus, setTrackingStatus] = useState(order.status ?? 'pending');
    const [statusLabel, setStatusLabel] = useState('');

    useEffect(() => {
        if (!trackingConnection) return;

        // Handle incoming live location (from delivery agent moving)
        const handleLocation = (data) => {
            if (data.orderId === order.id) {
                setDriverPos([data.latitude, data.longitude]);
                setTrackingStatus(data.status);
            }
        };

        // ✅ Handle incoming status change (from shop or delivery agent)
        const handleStatus = (data) => {
            if (data.orderId === order.id) {
                setTrackingStatus(data.status);
                setStatusLabel(data.label ?? '');
            }
        };

        trackingConnection.on('location:updated', handleLocation);
        trackingConnection.on('order:status_updated', handleStatus);

        // ✅ Safe group join — waits for Connected state before invoking
        const joinGroup = async () => {
            while (trackingConnection.state === 'Connecting') {
                await new Promise(r => setTimeout(r, 100));
            }
            if (trackingConnection.state === 'Connected') {
                await trackingConnection.invoke('JoinOrderGroup', order.id);
            }
        };
        joinGroup();

        // ✅ Handle reconnection — rejoin group if connection drops and recovers
        const handleReconnected = async () => {
            if (trackingConnection.state === 'Connected') {
                await trackingConnection.invoke('JoinOrderGroup', order.id);
            }
        };
        trackingConnection.onreconnected(handleReconnected);

        return () => {
            trackingConnection.off('location:updated', handleLocation);
            trackingConnection.off('order:status_updated', handleStatus);
            if (trackingConnection.state === 'Connected') {
                trackingConnection.invoke('LeaveOrderGroup', order.id);
            }
        };
    }, [trackingConnection, order.id]);

    // ... render map and status bar
}
```

### Status Bar Component

Show a visual status bar inside `OrderTrackingCard` that updates with each event:

```jsx
const STATUS_STEPS = [
    { key: 'pending',          icon: '🛒', label: 'Order placed'         },
    { key: 'preparing',        icon: '👨‍🍳', label: 'Preparing'            },
    { key: 'ready_for_pickup', icon: '📦', label: 'Ready for pickup'      },
    { key: 'assigned',         icon: '🛵', label: 'Agent assigned'        },
    { key: 'picked_up',        icon: '🏃', label: 'Picked up'             },
    { key: 'on_the_way',       icon: '📍', label: 'On the way'            },
    { key: 'delivered',        icon: '✅', label: 'Delivered!'             },
];

function StatusBar({ currentStatus }) {
    const currentIndex = STATUS_STEPS.findIndex(s => s.key === currentStatus);

    return (
        <div style={{ display: 'flex', justifyContent: 'space-between', padding: '12px 0' }}>
            {STATUS_STEPS.map((step, index) => (
                <div
                    key={step.key}
                    style={{
                        textAlign: 'center',
                        opacity: index <= currentIndex ? 1 : 0.3,
                        fontWeight: index === currentIndex ? 'bold' : 'normal',
                        fontSize: 12,
                        flex: 1
                    }}
                >
                    <div style={{ fontSize: 20 }}>{step.icon}</div>
                    <div>{step.label}</div>
                </div>
            ))}
        </div>
    );
}
```

---

## 🗺️ Where to Add Broadcasts — Quick Reference

Use this as a checklist to find every place in your backend that needs a SignalR broadcast added:

| Action | Controller / Method | Event to Send | Status Value |
|---|---|---|---|
| Customer pays | `OrdersController.Checkout()` | `order:status_updated` | `pending` |
| Shop accepts order | `OrdersController.Accept()` | `order:status_updated` | `preparing` |
| Shop marks ready | `OrdersController.MarkReady()` | `order:status_updated` | `ready_for_pickup` |
| Delivery accepts job | `DeliveryController.AcceptJob()` | `order:status_updated` | `assigned` |
| Delivery picks up | `DeliveryController.PickUp()` | `order:status_updated` | `picked_up` |
| Delivery on the way | `DeliveryController.OnTheWay()` | `order:status_updated` | `on_the_way` |
| Delivery delivered | `DeliveryController.Deliver()` | `order:status_updated` | `delivered` |

---

## 🔍 How to Debug If It Still Does Not Work

Follow these steps in order:

### 1. Confirm the broadcast is being sent
Add a log line right before every `SendAsync` call:

```csharp
Console.WriteLine($"[SignalR] Broadcasting order:status_updated to order_{orderId} → {newStatus}");
await _trackingHub.Clients.Group($"order_{orderId}")
    .SendAsync("order:status_updated", ...);
```

Check your terminal when you change a status — if this line does not appear, the broadcast code is not being reached.

### 2. Confirm the customer is in the group
Add a log in `JoinOrderGroup` in `TrackingHub.cs`:

```csharp
public async Task JoinOrderGroup(int orderId)
{
    Console.WriteLine($"[SignalR] {Context.ConnectionId} joined order_{orderId}");
    await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
}
```

Open the customer tab and check the terminal — you should see this line when `OrderTrackingCard` mounts.

### 3. Confirm the event arrives in the browser
Add a log inside the React listener:

```js
const handleStatus = (data) => {
    console.log('[SignalR] order:status_updated received:', data);
    if (data.orderId === order.id) {
        setTrackingStatus(data.status);
    }
};
```

Open the browser console on the customer tab and change an order status. If the log appears but the UI does not update, the issue is in React state. If the log never appears, the customer is not in the group.

### 4. Check for camelCase mismatch
If `data.orderId` logs as `undefined`, the backend is sending PascalCase (`OrderId`). Apply the camelCase fix in `Program.cs` (see Step 3 in Backend Fixes above).

---

## 🚀 Production Notes

| Item | Note |
|---|---|
| Push notifications | Already working — no change needed |
| SignalR status broadcasts | Add to every status transition as shown above |
| Azure SignalR Service | Required for multi-server deployments — see `location_tracking.md` |
| Group cleanup | `LeaveOrderGroup` is called on component unmount — works correctly |
| Delivered state | Hide the map and show a "Thank you" message when `status === 'delivered'` |
| Customer reconnection | `onreconnected` handler re-joins the group automatically |

---

## 📋 Fix Checklist

- [ ] Add `IHubContext<TrackingHub>` injection to `OrdersController`
- [ ] Add `IHubContext<TrackingHub>` injection to `DeliveryController`
- [ ] Add `SendAsync("order:status_updated", ...)` after every status change in `OrdersController`
- [ ] Add `SendAsync("order:status_updated", ...)` after every status change in `DeliveryController`
- [ ] Confirm camelCase JSON serialization is set in `Program.cs`
- [ ] Add `order:status_updated` listener in `OrderTrackingCard` useEffect
- [ ] Add `onreconnected` handler to re-join group after connection drop
- [ ] Add `StatusBar` component to `OrderTrackingCard` with all 7 steps
- [ ] Test: change order status in shop tab → customer tab updates with no refresh
- [ ] Test: change delivery status → customer tab updates with no refresh
- [ ] Test: close and reopen customer tab → status shows correct current state

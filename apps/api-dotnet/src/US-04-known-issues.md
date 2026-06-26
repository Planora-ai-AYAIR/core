# US-04 — Known Issues (Discovered During Real-time Broadcasting Implementation)

These issues were found while implementing per-type analysis result broadcasting via SignalR.
They are **not** fixed in this change-set to keep scope focused. Schedule them separately.

---

## 1. 🔴 Webhook HMAC Middleware Does Not Run (Security)

**File:** `Planora.Api/Program.cs:51`

```csharp
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/webhook"),
    builder => builder.UseMiddleware<WebhookSignatureMiddleware>());
```

**Problem:** The path check uses `/api/webhook` (singular), but the actual controller route is `[Route("api/webhooks")]` (plural). `StartsWithSegments("/api/webhook")` will **not** match `/api/webhooks/ai-events`, so the HMAC signature middleware is **never invoked**. The `AiWebhookController` is marked `[AllowAnonymous]`, meaning the endpoint is completely unprotected — any unauthenticated request can inject fake analysis results.

**Fix:** Change to `"/api/webhooks"` (plural) to match the controller route.

---

## 2. 🟡 Duplicate / Dead Webhook Configuration

**Files:** `Planora.Infrastructure/Options/WebhookOptions.cs`, `Planora.Api/Middlewares/WebhookSignatureMiddleware.cs`

**Problem:** Two separate config sections exist for webhook secrets:
- `WebhookOptions` reads from `"Webhook"` section, uses header `X-Signature`
- `WebhookSignatureMiddleware` reads `AiOptions.SharedSecret` and uses header `X-Webhook-Signature`

The `WebhookOptions` class and its config section appear unused — the middleware doesn't reference them.
This is confusing and error-prone when rotating secrets.

**Fix:** Remove `WebhookOptions` and its config section, or reconcile both into a single authority.

---

## 3. 🟡 SignalR Group-Name Mismatch (ReportNotifier)

**Files:** `Planora.Api/Hubs/NotificationHub.cs`, `Planora.Api/Hubs/ReportNotifier.cs`

**Problem:** Clients join the group `parcel:{id}` (colon) via `NotificationHub.SubscribeToParcel`:
```csharp
await Groups.AddToGroupAsync(Context.ConnectionId, $"parcel:{parcelId}");
```

But `ReportNotifier` broadcasts to `parcel_{id}` (underscore):
```csharp
var groupName = $"parcel_{parcelId}";
```

These are different group names — `ReportNotifier` messages reach **no one**.

**Fix:** Change `ReportNotifier` to use `$"parcel:{parcelId}"` to match the group clients actually join.

---

## 4. 🟠 RiskCompletedHandler Uses Inline Notification (No Group Broadcast)

**File:** `Planora.Application/Features/Analysis/Commands/RiskCompleted/RiskCompletedHandler.cs`

**Problem:** Every other completion handler delegates to `AnalysisNotificationHelper.PublishCompletionNotificationAsync`, which:
1. Persists the notification
2. Pushes to the user **and** the `parcel:{id}` group

`RiskCompletedHandler` has its own inline `PublishCompletionNotificationAsync` private method that only:
1. Persists the notification
2. Pushes to the user (no group broadcast)

Clients listening on the `parcel:{id}` group never receive the risk completion notification.

**Fix:** Replace the inline method with a call to `AnalysisNotificationHelper.PublishCompletionNotificationAsync`.

---

## 5. 🔵 `bearing.completed` Event Type Declared but Not Routed

**File:** `Planora.Application/Features/Parcels/Dtos/Webhook/AiWebhookEventTypes.cs`

**Problem:** `AiWebhookEventTypes.BearingCompleted` and `BearingFailed` are defined but `AiWebhookController` has no case for them — they fall through to `UnsupportedEventType`. Additionally, there is no `BearingCompletedHandler`. Bearing data is currently stored inside `SoilResult` rows.

**Status:** Intentional — a teammate owns the bearing model/handler/query. Track as a known gap.

# 🎓 حوار بين مُعلم وتلميذه: كيف يشتغل الـ Real-time Analysis Broadcasting في Planora

---

## المقدمة

**التلميذ:** يا أستاذ، أنا سمعت إننا عملنا حاجة جديدة في الـ backend بحيث لما كل نوع analysis يخلص، الـ frontend بيتعرف فورًا ومش لازم يعمل refresh أو يروح يعمل GET تاني. ممكن تشرحلي من الأول لإخر حاجة؟

**المعلم:** يا بني، ده سؤال عظيم. خليني آخدك في رحلة من أول ما الـ User بيضغط "Start Analysis" لحد ما الـ Frontend يستلم النتيجة وهو قاعد مكانه. هنتكلم عن كل Layer، كل Class، وكل Message اللي بيتحرك في السيم. يلا نبدأ.

---

## الفصل الأول: الـ Big Picture — الخريطة الكاملة

**المعلم:** قبل ما ندخل في الكود، لازم تشوف الصورة الكاملة. تخيل إننا عندي Pipeline طويلة:

```
┌─────────────┐     ┌──────────────────┐     ┌────────────────┐     ┌───────────────┐
│   Frontend   │────▶│  .NET API        │────▶│  Python AI     │────▶│  .NET API     │
│  (Angular)   │     │  StartAnalysis   │     │  (التحليلات)    │     │  Webhook      │
│              │◀────│  SignalR Push    │◀────│  per-type      │◀────│  Controller   │
└─────────────┘     └──────────────────┘     └────────────────┘     └───────────────┘
     ▲                                                │
     │                                                │
     └──────── SignalR ──────── parcel:{id} ──────────┘
```

**التلميذ:** يعني الـ Flow بيبقى كده؟

**المعلم:** بالظبط. بس خليني أقسمه لمراحل:

1. **Outbound:** الـ Frontend يطلب Start Analysis → الـ .NET API يبعت الشغل للـ Python AI Service.
2. **Process:** الـ Python بيشغل الـ analysis types واحد واحد (Topography, Soil, Risk, Borehole, PDF).
3. **Inbound:** كل type لما يخلص، الـ Python بيبعت Webhook Event للـ .NET API.
4. **Broadcast:** الـ .NET API يستلم الـ Event، يخزن النتيجة في الـ DB، وبعدين يبعتها Real-time للـ Frontend عن طريق SignalR.

**التلميذ:** طب إيه اللي اتغير بالظبط؟ كانوا بيعملوا إيه قبل كده؟

**المعلم:** قبل كده كان في طريقين:
- **القديم:** Python بيبعت event واحد بس اسمه `analysis.completed` واللي كان بيجمع كل النتائج في payload واحد. الـ Frontend كان لازم يستنى الـ Pipeline كله يخلص وبعدين يعمل GET عشان يشوف النتيجة.
- **الجديد:** Python بيبعت event لكل نوع لوحده (`topography.completed`, `soil.completed`, الخ)... وكل Handler لما يستلم الـ Event ده، بيخزن النتيجة **وبيبعتها فورًا** للـ Frontend عن طريق SignalR. فالـ User يشوف Topography drainage يخلص الأول، بعدين Soil، وهكذا — منغير ما يستنى حاجة.

---

## الفصل الثاني: الـ Outbound — إزاي الشغل بيروح للـ Python

**المعلم:** يلا نبدأ من الأول. الـ User بيضغط "Start Analysis" في الـ Frontend. ده بيعمل POST للـ API.

**التلميذ:** والـ API بيعمل إيه؟

**المعلم:** شوف الـ `StartAnalysisHandler`:

```csharp
// Planora.Application/Features/Analysis/Commands/StartAnalysis/StartAnalysisHandler.cs

public sealed class StartAnalysisHandler(
    IParcelRepository parcelRepository,
    IAnalysisJobRepository analysisJobRepository,
    IProcessAggregatedAnalysisJob processAggregatedAnalysisJob,   // ← ده اللي بيروج للـ Python
    INotificationPublisher notificationPublisher,
    ...) : IRequestHandler<StartAnalysisCommand, Result<StartAnalysisResponse>>
{
    public async Task<Result<StartAnalysisResponse>> Handle(StartAnalysisCommand request, CancellationToken ct)
    {
        // 1. بيدور على الـ Parcel
        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);
        if (parcel is null) return ParcelErrors.NotFound;

        // 2. بيتأكد إن مفيش Job شغال فعلاً
        var hasActiveJob = await analysisJobRepository.HasActiveJobAsync(request.ParcelId, ct);
        if (hasActiveJob) return AnalysisJobErrors.AlreadyRunning;

        // 3. بيعمل AnalysisJob Entity
        var analysisJobResult = AnalysisJob.Create(
            id: Guid.NewGuid(),
            parcelId: parcel.Id,
            pythonJobId: $"pending-{Guid.NewGuid():N}",   // ← مؤقتًا لحد ما Python يرد
            type: AnalysisType.Aggregated,
            options: options);

        await analysisJobRepository.AddAsync(analysisJobResult.Value, ct);

        // 4. بيُدخل الشغل في Hangfire Queue
        var hangfireJobId = processAggregatedAnalysisJob.Enqueue(parcel.Id, analysisJobResult.Value.Id);

        // 5. بيبعت Notification "Analysis Started" عن طريق SignalR
        await AnalysisNotificationHelper.PublishStartedNotificationAsync(
            analysisJobResult.Value, parcelRepository, notificationRepository, notificationPublisher, ct);

        return new StartAnalysisResponse(...);
    }
}
```

**التلميذ:** يعني بيعمل حاجتين مهمين: يودي الشغل Hangfire ويبعت Notification؟

**المعلم:** أيوه. Hangfire ده الـ Background Job Scheduler — بيشتغل في Validation Thread لوحده. والـ Notification ده الـ "بدأنا" الـ bell icon اللي بيظهر في الـ Frontend. بس ده مش موضوعنا النهارده — موضوعنا إيه اللي بيحصل لما الشغل **يخلص**.

### الـ Hangfire Job بيروح للـ Python إزاي؟

**المعلم:** شوف الـ `ProcessAggregatedAnalysisJob`:

```csharp
// Planora.Infrastructure/BackgroundJobs/ProcessAggregatedAnalysisJob.cs

public sealed class ProcessAggregatedAnalysisJob(
    IBackgroundJobClient backgroundJobClient,
    IAiAnalysisService aiAnalysis,
    IAnalysisJobRepository analysisJobRepository,
    IParcelRepository parcelRepository) : IProcessAggregatedAnalysisJob
{
    public async Task<Result<Success>> Execute(Guid parcelId, Guid analysisJobId)
    {
        var parcel = await parcelRepository.GetByIdAsync(parcelId, CancellationToken.None);
        var analysisJob = await analysisJobRepository.GetByIdAsync(analysisJobId, CancellationToken.None);

        // بيبني الـ Request من الـ Parcel Boundary + Options
        var request = new SubmitAiAnalysisJobRequest(...);

        // بيوديه للـ Python AI Service
        AiAnalysisJobResponse response;
        response = await aiAnalysis.SubmitAnalysisJobAsync(request, CancellationToken.None);

        // بيسجل الـ PythonJobId اللي Python رجعه
        analysisJob.SetPythonJobId(response.Data.JobId);
        analysisJob.MarkAsRunning();
        await analysisJobRepository.SaveChangesAsync(CancellationToken.None);

        return Result.Success;
    }
}
```

**التلميذ:** آه فهمت. يعني الـ .NET بيبعت الشغل للـ Python، والـ Python بردًا الـ `JobId` بتاعه. ولما كل analysis type يخلص، الـ Python هيبعت Webhook لينا.

**المعلم:** بالظبط كده. وده اللي بيوصلنا للـ Inbound Side.

---

## الفصل الثالث: الـ Inbound — إزاي الـ Python بيرجع النتايج لينا

### الـ Webhook Envelope

**المعلم:** الـ Python بيبعت POST request للـ endpoint ده:

```
POST /api/webhooks/ai-events
```

وشكل الـ Payload اللي بيبعته كده:

```json
{
  "eventType": "topography.completed",
  "jobId": "py-job-abc123",
  "data": { ... الـ TopographyResultPayload بالكامل ... },
  "timestamp": "2026-06-27T12:00:00Z"
}
```

وده معرف في الكود عندنا كـ `AiWebhookEnvelope`:

```csharp
// Planora.Application/Features/Parcels/Dtos/Webhook/AiWebhookEnvelope.cs

public sealed class AiWebhookEnvelope
{
    public string EventType { get; init; } = string.Empty;
    public string JobId { get; init; } = string.Empty;
    public JsonElement Data { get; init; }
    public DateTime Timestamp { get; init; }
}
```

### الـ Event Types

**التلميذ:** طب إيه الـ Event Types اللي ممكن تيجي؟

**المعلم:** شوف الـ Constants دي:

```csharp
// Planora.Application/Features/Parcels/Dtos/Webhook/AiWebhookEventTypes.cs

public static class AiWebhookEventTypes
{
    public const string TopographyCompleted = "topography.completed";
    public const string TopographyFailed = "topography.failed";
    public const string SoilCompleted = "soil.completed";
    public const string SoilFailed = "soil.failed";
    public const string RiskCompleted = "risk.completed";
    public const string RiskFailed = "risk.failed";
    public const string BoreholeCompleted = "borehole.completed";
    public const string PdfCompleted = "pdf.completed";
    public const string BearingCompleted = "bearing.completed";   // ← لسه مش متعامل معاه
    public const string BearingFailed = "bearing.failed";         // ← لسه مش متعامل معاه
    public const string AnalysisFailed = "analysis.failed";
    public const string AnalysisCompleted = "analysis.completed";  // ← الـ aggregated القديم
}
```

**التلميذ:** يعني في 6 types شغالة (Topography، Soil، Risk، Borehole، PDF) + الـ Aggregated القديم + الـ Bearing اللي لسه صاحبك شغال عليه؟

**المعلم:** بالظبط 100%. دلوقتي الـ Bearing مش at the moment.

### الـ Webhook Controller

**المعلم:** دلوقتي خليك معايا في الـ Controller اللي بيستلم الـ Webhook ده:

```csharp
// Planora.Api/Controllers/AiWebhookController.cs

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public sealed class AiWebhookController(ISender mediator) : BaseApiController
{
    [HttpPost("ai-events")]
    public async Task<IActionResult> Receive([FromBody] AiWebhookEnvelope envelope, CancellationToken ct)
    {
        var result = envelope.EventType switch
        {
            AiWebhookEventTypes.TopographyCompleted => await mediator.Send(
                new TopographyCompletedCommand
                {
                    PythonJobId = envelope.JobId,
                    Payload = envelope.Data.Deserialize<TopographyResultPayload>(JsonOptions)!
                }, ct),

            AiWebhookEventTypes.SoilCompleted => await mediator.Send(
                new SoilCompletedCommand { ... }, ct),

            AiWebhookEventTypes.RiskCompleted => await mediator.Send(
                new RiskCompletedCommand { ... }, ct),

            AiWebhookEventTypes.BoreholeCompleted => await mediator.Send(
                new BoreholeCompletedCommand { ... }, ct),

            AiWebhookEventTypes.PdfCompleted => await mediator.Send(
                new PdfCompletedCommand { ... }, ct),

            AiWebhookEventTypes.AnalysisCompleted => await mediator.Send(
                new AnalysisCompletedCommand { ... }, ct),

            AiWebhookEventTypes.AnalysisFailed => await mediator.Send(
                new AnalysisFailedCommand { ... }, ct),

            _ => AnalysisJobErrors.UnsupportedEventType
        };

        if (result.IsError) return Problem(result.Errors);
        return OkEnvelope(result.Value, "Event Handled");
    }
}
```

**التلميذ:** آه يعني الـ Controller ده بيبقى الـ Entry Point. بياخد الـ Envelope، يشوف الـ EventType، وبعدين يبعت Command لكل Handler عن طريق MediatR.

**المعلم:** إيوه بالظبط. الـ Controller ده ثِقيل — بيبقى thin. مفيشlogic فيه غير routing. كل الشغل الحقيقي بيحصل في الـ Handlers.

---

## الفصل الرابع: الـ Handler — قلب العملية

**المعلم:** يلا ندخل في الـ TopographyCompletedHandler كـ Example. الباقي نفس النمط بالظبط.

```csharp
// Planora.Application/Features/Analysis/Commands/TopographyCompleted/TopographyCompletedHandler.cs

public sealed class TopographyCompletedHandler(
    IAnalysisJobRepository analysisJobRepository,
    ITopographyResultRepository topographyResultRepository,
    IParcelRepository parcelRepository,
    INotificationRepository notificationRepository,
    INotificationPublisher notificationPublisher,    // ← ده الـ Interface البيعمل الـ Push
    IHybridCacheService cacheService,
    ILogger<TopographyCompletedHandler> logger)
    : IRequestHandler<TopographyCompletedCommand, Result<AnalysisJobProcessedResponse>>
{
    public async Task<Result<AnalysisJobProcessedResponse>> Handle(
        TopographyCompletedCommand request, CancellationToken ct)
    {
        // 1. يلاقي الـ AnalysisJob عن طريق الـ PythonJobId
        var analysisJob = await analysisJobRepository.GetByPythonJobIdAsync(request.PythonJobId, ct);
        if (analysisJob is null) return AnalysisJobErrors.NotFound;

        // 2. يتحقق إن الـ Status صح (Running)
        if (analysisJob.Status != AnalysisJobStatus.Running)
            return AnalysisJobErrors.InvalidStateTransition;

        // 3. يغير الـ Status إلى Completed
        var markAsCompletedResult = analysisJob.MarkAsCompleted();
        if (markAsCompletedResult.IsError) return AnalysisJobErrors.FaildStatusUpdate;

        // 4. يخزن الـ TopographyResult في الداتابيز
        var topographyResult = new TopographyResult(
            analysisJob.Id,
            request.Payload.ElevationMin,
            request.Payload.ElevationMax,
            request.Payload.ElevationMean,
            slopeDistributionJson,
            request.Payload.CutVolume,
            request.Payload.FillVolume,
            /* ... باقي الحقول ... */);
        await topographyResultRepository.AddAsync(topographyResult, ct);

        // 5. يخزن كل التعديلات
        await analysisJobRepository.SaveChangesAsync(ct);

        // 6. يمسح الـ Cache
        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        // 7. 🔔 بيبعت Notification عادي (الـ bell icon)
        await AnalysisNotificationHelper.PublishCompletionNotificationAsync(
            analysisJob, parcelRepository, notificationRepository, notificationPublisher, ct);

        // 8. 🆕 بيبعت الـ FULL RESULT عن طريق SignalR للـ Frontend (الجديد اللي عملناه)
        await AnalysisNotificationHelper.PublishAnalysisResultAsync(
            analysisJob, AiWebhookEventTypes.TopographyCompleted, request.Payload, notificationPublisher, ct);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
```

**التلميذ:** يا سلام! يعني Step 7 ده الـ القديم — الـ bell notification اللي بيقول "Topography complete" وبس. وStep 8 ده الـ الجديد اللي بيبعت الـ Result نفسه.

**المعلم:** بالظبط! والفرق مهم:

- **Step 7** (`PublishCompletionNotificationAsync`): بيبعت `NotificationDto` — فيها Title + Message + Link بس. الـ Frontend لو عايز يشوف الأرقام لازم يعمل GET.
- **Step 8** (`PublishAnalysisResultAsync`): بيبعت `AnalysisResultEnvelope` — فيها الـ **Full Payload** بالكامل (ElevationMin, ElevationMax, CutVolume, الخ...). الـ Frontend يقدر يرسم البيانات فورًا من غير أي GET.

**التلميذ:** يعني الاتنين بيعملوا Push في نفس الوقت؟ مش هيحصل Race أو حاجة؟

**المعلم:** أيوه بيحصلوا بشكل متتابع (await...await) بس مش فيهم مشكلة. هم رسالتين مختلفين على نفس الـ Connection — الـ SignalR بيضمن الترتيب. الـ Frontend هيستلم `NotificationReceived` الأول (bell icon) وبعديه `AnalysisResultReceived` (الـ data). ولو الـ bell وصل بس الـ data لسه لأ، الـ UI يقدر يعرض الـ bell وبعدين يملا الـ data أول ما تيجي — عمليًا الفرق مليثانية.

### بقية الـ Handlers نفس النمط

**المعلم:** كل Handler بيعمل نفس الخطوات:
- `SoilCompletedHandler` → `AiWebhookEventTypes.SoilCompleted` + `request.Payload` (SoilResultPayload)
- `RiskCompletedHandler` → `AiWebhookEventTypes.RiskCompleted` + `request.Payload` (RiskResultPayload)
- `BoreholeCompletedHandler` → `AiWebhookEventTypes.BoreholeCompleted` + `request.Payload` (BoreholeResultPayload)
- `PdfCompletedHandler` → `AiWebhookEventTypes.PdfCompleted` + `request.Payload` (PdfResultPayload)
- `AnalysisCompletedHandler` → `AiWebhookEventTypes.AnalysisCompleted` + `request.Payload` (AggregatedAnalysisResultPayload)

كل واحد منهم بيعمل:
```
await AnalysisNotificationHelper.PublishAnalysisResultAsync(
    analysisJob, AiWebhookEventTypes.XXXCompleted, request.Payload, notificationPublisher, ct);
```

---

## الفصل الخامس: الـ AnalysisResultEnvelope — الغلاف اللي بيوشي الـ Data

**المعلم:** دلوقتي خليك معايا في الـ Record اللي احنا عملناه. ده الـ Envelope اللي بيتحرك من الـ Backend للـ Frontend:

```csharp
// Planora.Application/Features/Analysis/Dtos/Realtime/AnalysisResultEnvelope.cs

public sealed record AnalysisResultEnvelope(
    string EventType,       // مثلاً "topography.completed"
    Guid ParcelId,          // الـ Parcel اللي التحليل بتاعه
    Guid AnalysisJobId,     // الـ Job ID
    string AnalysisType,    // "Topography" — job.Type.ToString()
    object Result,          // 🔑 الـ Full Payload (TopographyResultPayload أو غيره)
    DateTime Timestamp);
```

**التلميذ:** طب الـ `Result` ليه `object`؟ مش المفروض يبقى typed؟

**المعلم:** سؤال جامد! الإجابة: لأن الـ Envelope ده **واحد** بس بيشتغل لكل الـ Types. لو عملناه `TopographyResultPayload` هيبقى شغال بس للـ Topography. لكن لما `object`، فالـ Serializer (System.Text.Json اللي SignalR بيستخدمه) بيشوف الـ Runtime Type الفعلي وبيسيريليزه بناءً عليه. يعني:

- لو `Result` = `TopographyResultPayload` → الـ JSON هيطلع كل خصائص الـ Topography
- لو `Result` = `SoilResultPayload` → الـ JSON هيطلع كل خصائص الـ Soil

وكل Payload عنده `[JsonPropertyName]` عشان الـ Python snake_case:

```csharp
public sealed record TopographyResultPayload
{
    [JsonPropertyName("elevationMin")]     // ← الـ Frontend يشوف الاسم ده في الـ JSON
    public double ElevationMin { get; init; }

    [JsonPropertyName("elevationMax")]
    public double ElevationMax { get; init; }
    // ...
}
```

**التلميذ:** يعني الـ Frontend لازم يعرف يعمل switch على الـ `eventType` عشان يعرف الـ shape بتاعة الـ `result`؟

**المعلم:** بالظبط. وده منطقي — لأن كل نوع له حقوله الخاصة. الـ Frontend هيشتغل كده:

```typescript
// في الـ Frontend (TypeScript concept)
switch (envelope.eventType) {
  case "topography.completed":
    const topo = envelope.result as TopographyResultPayload;
    // يعرض الـ Elevation, Cut/Fill, Contour lines...
    break;
  case "soil.completed":
    const soil = envelope.result as SoilResultPayload;
    // يعرض الـ Sand/Silt/Clay%, Bearing Capacity...
    break;
  // ... الخ
}
```

---

## الفصل السادس: الـ Helper — اللي بيبني الـ Envelope

**المعلم:** دلوقتي شوف الـ Helper اللي بنستخدمه في كل Handler:

```csharp
// Planora.Application/Common/Helpers/AnalysisNotificationHelper.cs

public static Task PublishAnalysisResultAsync(
    AnalysisJob job,
    string eventType,
    object resultPayload,
    INotificationPublisher notificationPublisher,
    CancellationToken ct)
{
    var envelope = new AnalysisResultEnvelope(
        EventType: eventType,                    // "topography.completed"
        ParcelId: job.ParcelId,                  // الـ Parcel اللي الشغل بتاعه
        AnalysisJobId: job.Id,                   // الـ Job نفسه
        AnalysisType: job.Type.ToString(),        // "Topography"
        Result: resultPayload,                   // الـ Payload الكامل
        Timestamp: DateTime.UtcNow);

    return notificationPublisher.PublishAnalysisResultAsync(job.ParcelId, envelope, ct);
}
```

**التلميذ:** يعني الـ Helper ده بيبني الـ Envelope بسهولة وبعدين يوديه للـ Publisher؟

**المعلم:** أيوه! لاحظ حاجة مهمة: الـ Helper ده **مش بيعمل Parcel lookup** — مش بيروح يعمل query في الداتابيز عشان الـ Parcel. لأن الـ `AnalysisJob` Entity عنده الـ `ParcelId` أصلاً. ده بيقلل الـ DB Round-trip وبيخلي العملية أسرع.

**التلميذ:** فهمت. يعني كل اللي محتاجه الـ ParcelId موجود في الـ job أصلاً.

**المعلم:** بالظبط. وده فرق عن الـ `PublishCompletionNotificationAsync` اللي لازم يعمل parcel lookup لأنه محتاج الـ `parcel.UserId` عشان يبعت notification شخصي لكل user. لكن في الـ Analysis Result Broadcasting، إحنا بنبعت لكل اللي subscribed للـ parcel group — مش محتاجين نعرف مين الـ User.

---

## الفصل السابع: الـ Publisher Interface — الـ Contract بين الـ Application والـ API

**المعلم:** دلوقتي نيجي للجزء اللي بيربط الـ Application Layer بالـ API Layer. ده الـ Clean Architecture في أحلى صوره:

```csharp
// Planora.Application/Interfaces/Services/INotificationPublisher.cs

public interface INotificationPublisher
{
    // الطرق القديمة — لسه شغالة
    Task PublishAsync(Guid userId, NotificationDto notification, CancellationToken ct);
    Task PublishToGroupAsync(string groupName, NotificationDto notification, CancellationToken ct);

    // 🆕 الطريقة الجديدة — لبتاع الـ Analysis Results
    Task PublishAnalysisResultAsync(Guid parcelId, AnalysisResultEnvelope envelope, CancellationToken ct);
}
```

**التلميذ:** يعني الـ Application Layer بيعرف إن في طريقة اسمها `PublishAnalysisResultAsync` بس مش بيعرف إن تحتها SignalR؟

**المعلم:** بال ظبط! ده الـ Dependency Inversion Principle. الـ Application مش عارف حاجة عن SignalR أو Hubs أو Connection IDs — هو بس عارف إن في Interfaceبيبعت analysis result لـ parcel معين. والـ Implementation الحقيقي في الـ API Layer.

### والـ Implementation الحقيقي:

```csharp
// Planora.Api/Services/SignalRNotificationPublisher.cs

public sealed class SignalRNotificationPublisher(
    IHubContext<NotificationHub, INotificationClient> hub,    // ← ده الـ SignalR Hub Context
    ILogger<SignalRNotificationPublisher> logger)
    : INotificationPublisher
{
    // ... الطرق القديمة ...

    public async Task PublishAnalysisResultAsync(
        Guid parcelId, AnalysisResultEnvelope envelope, CancellationToken ct)
    {
        try
        {
            var groupName = $"parcel:{parcelId}";   // ← اسم الـ Group
            await hub.Clients.Group(groupName)
                       .AnalysisResultReceived(envelope);  // ← الـ Method اللي في INotificationClient
            logger.LogInformation(
                "Pushed analysis result {EventType} for parcel {ParcelId} to group {GroupName}",
                envelope.EventType, parcelId, groupName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to push analysis result {EventType} for parcel {ParcelId}",
                envelope.EventType, parcelId);
        }
    }
}
```

**التلميذ:** آه! يعني `hub.Clients.Group("parcel:{id}").AnalysisResultReceived(envelope)` — ده بيعمل Broadcast لكل اللي في الـ Group ده.

**المعلم:** بالظبط! وتلات حاجات مهمة هنا:

1. **Group Name:** `$"parcel:{parcelId}"` — نفس الـ Convention اللي كل الـ Handlers التانيين بيستخدموه.
2. **Strongly-Typed Client:** مش بنعمل `SendAsync("methodName", ...)` — بنعمل `.AnalysisResultReceived(envelope)` عن طريق الـ `INotificationClient` Interface. ده بيضمن compile-time safety — لو غيرت الاسم في الـ Interface هستا يشتغل.
3. **Try/Catch:** لو الـ SignalR فشل (مثلاً الـ Connection اتقطع)، بنسجل Warning بس مش بنخلي الـ Handler يفشل. لأن التخزين في الداتابيز أهم من الـ Real-time push.

**التلميذ:** طب يعني لو الـ SignalR فشل، النتيجة اتخزنت في الداتابيز بس الـ Frontend مش عرفه؟

**المعلم:** صح! بس ده مقبول — الـ Frontend يقدر يعمل GET في أي وقت يشوف النتيجة. الـ SignalR ده **Best-Effort Enhancement** — مش Critical Path. لو وصل، رائع. لو لأ، الـ Data موجودة في الـ DB.

---

## الفصل الثامن: الـ SignalR Hub — مكان الـ Connections والـ Groups

**المعلم:** دلوقتي نيجي للـ Hub نفسه:

```csharp
// Planora.Api/Hubs/NotificationHub.cs

[Authorize]   // ← لازم يكون عامل Login
public sealed class NotificationHub(ILogger<NotificationHub> _logger) : Hub<INotificationClient>
{
    public override Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "SignalR connection established. UserId={UserId} ConnectionId={ConnectionId}",
            Context.UserIdentifier, Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "SignalR connection closed. UserId={UserId} ConnectionId={ConnectionId}",
            Context.UserIdentifier, Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    // 🔑 ده الـ Method اللي الـ Frontend بيستدعيه عشان ينضم للـ Group
    public async Task SubscribeToParcel(Guid parcelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"parcel:{parcelId}");
        _logger.LogInformation(
            "Connection {ConnectionId} subscribed to parcel group {ParcelId}",
            Context.ConnectionId, parcelId);
    }
}
```

**التلميذ:** يعني الـ Frontend لما يعمل Connection للـ Hub، لازم يعمل `SubscribeToParcel(parcelId)` عشان يسمع الـ Messages بتاعة الـ Parcel ده؟

**المعلم:** بالظبط! وده Pattern مهم اسمه **Pub/Sub via Groups**. الفكرة:
- كل Connection ممكن ينضم لأكتر من Group (أكتر من Parcel)
- لما الـ Backend يعمل Broadcast لـ `parcel:{id}`، كل اللي انضم للـ Group ده هيستلم الرسالة
- الـ Hub بيعرف الـ UserId من الـ JWT Token (عشان `[Authorize]`)

### والـ Client Interface:

```csharp
// Planora.Api/Hubs/INotificationClient.cs

public interface INotificationClient
{
    Task NotificationReceived(NotificationDto notification);    // ← القديم (bell icon)
    Task AnalysisResultReceived(AnalysisResultEnvelope envelope); // ← 🆕 الجديد (full data)
}
```

**التلميذ:** يعني الـ Interface ده بيحدد الـ Methods اللي الـ Server ممكن يناديها على الـ Client. زي Contract.

**المعلم:** بالظبط! لأن الـ Hub معرف كده `Hub<INotificationClient>`، فالـ `IHubContext<NotificationHub, INotificationClient>` يخليك تتكلم مع الـ Clients عن طريق الـ Interface ده بس — compile-time safe.

---

## الفصل التاسع: البرنامج كله بيتسجل إزاي (DI + Pipeline)

**المعلم:** خليني أوريك إزاي كل حاجة بتتسجل في الـ Program.cs:

```csharp
// Planora.Api/Program.cs

var builder = WebApplication.CreateBuilder(args);

// 1. تسجيل كل الـ Services
builder.Services
    .AddApplicationServices()                   // ← الـ Application Layer (MediatR, Validators, الخ)
    .AddInfrastructureServices(builder.Configuration) // ← الـ Infrastructure Layer (DB, Hangfire, الخ)
    .AddPresentationServices(builder.Configuration);  // ← الـ API Layer (Controllers, SignalR, الخ)

builder.Services.AddControllers();
builder.Services.AddSignalR();   // ← 🔑 ده اللي بيفعل الـ SignalR في الـ Pipeline
```

```csharp
// Planora.Api/DependencyInjection.cs

public static IServiceCollection AddPresentationServices(
    this IServiceCollection services, IConfiguration configuration)
{
    // ...
    services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();  // ← تسجيل الـ Implementation
    services.AddScoped<IReportNotifier, ReportNotifier>();
    // ...
}
```

```csharp
// الـ Endpoint Mapping
app.MapHub<NotificationHub>("/hubs/notifications");  // ← 🔑 الـ URL اللي الـ Frontend بيعمل Connect عليه
```

**التلميذ:** يعني الـ Frontend بيعمل connect على `/hubs/notifications` وبعدين ينضم للـ Groups؟

**المعلم:** أيوه. والـ URL ده بيعمل WebSocket Upgrade تلقائي. الـ SignalR بيختار أحسن transport متاح (WebSocket → Server-Sent Events → Long Polling).

---

## الفصل العاشر: الـ JWT + SignalR — إزاي الـ Authentication بيشتغل

**المعلم:** حاجة مهمة — الـ SignalR مش بيستخدم الـ Authorization Header العادي. شوف:

```csharp
// Planora.Api/DependencyInjection.cs → AddJwtAuthentication

.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters { ... };

    // 🔑 الـ Part المهم للـ SignalR:
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var accessToken = ctx.Request.Query["access_token"];  // ← من الـ Query String
            var path = ctx.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs"))                  // ← لو الـ Request على الـ Hub
            {
                ctx.Token = accessToken;                          // ← خد التوكين من الـ QS
            }
            return Task.CompletedTask;
        }
    };
});
```

**التلميذ:** يعني الـ Frontend لازم يبعت الـ JWT Token في الـ Query String لما يعمل Connect للـ Hub؟

**المعلم:** بالظبط! لأن الـ Browser مش بيقدر يضيف Authorization Header في الـ WebSocket Upgrade Request. فالـ Pattern ده بيبقى:

```typescript
// في الـ Frontend
const hubConnection = new HubConnectionBuilder()
  .withUrl("/hubs/notifications?access_token=" + jwtToken, {
    accessTokenFactory: () => jwtToken   // أو الطريقة دي
  })
  .build();

await hubConnection.start();

// بعدين ينضم للـ Parcel Group
await hubConnection.invoke("SubscribeToParcel", parcelId);
```

---

## الفصل الحادي عشر: ملخص الـ Message Flow الكامل

**المعلم:** خليني أوضحلك الـ Flow كله في شكل Timeline:

```
  Frontend                     .NET API                    Python AI
     │                            │                           │
     │── POST /api/analysis ─────▶│                           │
     │                            │── Hangfire: Submit Job ──▶│
     │◀─ SignalR: AnalysisStarted │                           │
     │   (bell: "Analysis started")│                          │
     │                            │                           │
     │                            │                           │ [Topography يخلص]
     │                            │◀─ POST /webhooks/ai-events│
     │                            │   { eventType:              │
     │                            │     "topography.completed"  │
     │                            │     jobId: "py-abc123"      │
     │                            │     data: { elevation... } }│
     │                            │                           │
     │                            │ [TopographyCompletedHandler]
     │                            │   1. يلاقي الـ Job
     │                            │   2. MarkAsCompleted
     │                            │   3. يخزن TopographyResult
     │                            │   4. SaveChanges
     │                            │   5. Cache Invalidation
     │                            │   6. PublishCompletion ─────▶│
     │◀─ SignalR: NotificationReceived (bell)               │
     │   "Topography analysis complete"                       │
     │                            │   7. PublishAnalysisResult ─▶│
     │◀─ SignalR: AnalysisResultReceived ◀──│
     │   { eventType: "topography.completed",│
     │     parcelId: "xxx",                  │
     │     analysisJobId: "yyy",             │
     │     analysisType: "Topography",       │
     │     result: { elevationMin: 1200,    │
     │              elevationMax: 1350, ... },│
     │     timestamp: "2026-06-27T..." }     │
     │                            │                           │
     │  الـ Frontend يعرض الـ     │                           │
     │  Topography Data فورًا     │                           │
     │  (مفيش GET!)              │                           │
     │                            │                           │
     │                            │                           │ [Soil يخلص]
     │                            │◀─ POST /webhooks/ai-events│
     │                            │   { eventType:              │
     │                            │     "soil.completed" ... }  │
     │                            │                           │
     │                            │ [SoilCompletedHandler]     │
     │                            │   ... نفس الخطوات ...      │
     │◀─ SignalR: AnalysisResultReceived ◀──│
     │   { eventType: "soil.completed",      │
     │     result: { sandPercent: 45, ... } }│
     │                            │                           │
     │  الـ Frontend يعرض الـ    │                           │
     │  Soil Data فورًا          │                           │
     │                            │                           │
     │            ... نفس الشيء لـ Risk, Borehole, PDF ...   │
```

**التلميذ:** يا سلام! يعني الـ Frontend يستلم **تيجي** رسالتين لكل analysis type:
1. `NotificationReceived` — الـ bell icon (عادي)
2. `AnalysisResultReceived` — الـ full data (الجديد)

**المعلم:** بالظبط. والاثنين مش بيعملوا conflict — لأنهم على Method مختلفة في الـ `INotificationClient`.

---

## الفصل الثاني عشر: الـ AnalysisResultEnvelope JSON Shape

**المعلم:** خليني أوريك الشكل الفعلي اللي الـ Frontend هيستلمه في الـ JSON:

### Topography مثال:

```json
{
  "eventType": "topography.completed",
  "parcelId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "analysisJobId": "f7e6d5c4-b3a2-1098-7654-3210fedcba98",
  "analysisType": "Topography",
  "result": {
    "pythonJobId": "py-job-abc123",
    "elevationMin": 1200.5,
    "elevationMax": 1350.2,
    "elevationMean": 1275.3,
    "slopeDistribution": [
      { "category": "Flat", "range": "0-5°", "percentage": 40.0, "color": "#4CAF50" }
    ],
    "cutVolume": 15000.0,
    "fillVolume": 8000.0,
    "netVolume": 7000.0,
    "contourInterval": 5.0,
    "contourGeoJsonUrl": "https://s3.../contours.geojson",
    "pondingGeoJsonUrl": "https://s3.../ponding.geojson",
    "pondingZonesCount": 3,
    "pondingTotalArea": 2500.0,
    "elevationTileUrl": "https://s3.../elevation.pmtiles",
    "slopeTileUrl": "https://s3.../slope.pmtiles",
    "demRasterUrl": "https://s3.../dem.tif",
    "slopeRasterUrl": "https://s3.../slope.tif",
    "metadata": {
      "copernicusDemVersion": "v3.0",
      "pixelResolutionMeters": 30,
      "crs": "EPSG:4326",
      "processingTimeSeconds": 45
    }
  },
  "timestamp": "2026-06-27T12:30:00Z"
}
```

### مثال لـ Soil:

```json
{
  "eventType": "soil.completed",
  "parcelId": "a1b2c3d4-...",
  "analysisJobId": "f7e6d5c4-...",
  "analysisType": "Soil",
  "result": {
    "sandPercent": 45.0,
    "siltPercent": 30.0,
    "clayPercent": 25.0,
    "bulkDensity": 1.5,
    "organicCarbon": 2.1,
    "ph": 7.2,
    "bearingCapacityEstimate": 150.0,
    "bearingCapacityCategory": "Medium",
    "primaryType": "Sandy Loam",
    "usdaClass": "SCL",
    "heatmapTileUrl": "https://s3.../soil-heatmap.pmtiles",
    "bearing": {
      "bearingCapacityKpa": 155.0,
      "confidence": 0.87,
      "trafficLight": "green",
      "recommendedFoundation": "Shallow Foundation"
    }
  },
  "timestamp": "2026-06-27T12:35:00Z"
}
```

---

## الفصل الثالث عشر: خطوات التنفيذ — مين بيعمل إيه

**المعلم:** دلوقتي خليني أقلك بالتفصيل: إيه اللي اتعمل في الـ Backend، وإيه المعلومات اللي لازم توصّل للـ Python AI، وإيه اللي لازم يتعمل في الـ Frontend.

### 🟢 PART 1: اللي اتعمل في الـ Backend (خلص ✅)

| # | الخطوة | الملف | الحالة |
|---|--------|-------|--------|
| 1 | عمل `AnalysisResultEnvelope` record | `Planora.Application/Features/Analysis/Dtos/Realtime/AnalysisResultEnvelope.cs` | ✅ |
| 2 | إضافة `PublishAnalysisResultAsync` لـ `INotificationPublisher` | `Planora.Application/Interfaces/Services/INotificationPublisher.cs` | ✅ |
| 3 | إضافة `AnalysisResultReceived` لـ `INotificationClient` | `Planora.Api/Hubs/INotificationClient.cs` | ✅ |
| 4 | تنفيذ `PublishAnalysisResultAsync` في `SignalRNotificationPublisher` | `Planora.Api/Services/SignalRNotificationPublisher.cs` | ✅ |
| 5 | إضافة Helper method `PublishAnalysisResultAsync` | `Planora.Application/Common/Helpers/AnalysisNotificationHelper.cs` | ✅ |
| 6 | توصيل `TopographyCompletedHandler` | `TopographyCompletedHandler.cs` | ✅ |
| 7 | توصيل `SoilCompletedHandler` | `SoilCompletedHandler.cs` | ✅ |
| 8 | توصيل `RiskCompletedHandler` | `RiskCompletedHandler.cs` | ✅ |
| 9 | توصيل `BoreholeCompletedHandler` | `BoreholeCompletedHandler.cs` | ✅ |
| 10 | توصيل `PdfCompletedHandler` | `PdfCompletedHandler.cs` | ✅ |
| 11 | توصيل `AnalysisCompletedHandler` (الـ aggregated القديم) | `AnalysisCompletedHandler.cs` | ✅ |
| 12 | Build ونجح (0 errors) | `dotnet build` | ✅ |

### 🔵 PART 2: المعلومات اللي لازم توصّل لفريق الـ Python AI

**المعلم:** عشان الـ Feature دي تشتغل، الـ Python لازم يبعت **per-type Events** مش aggregated واحد. يعني لازم فريق الـ Python يعرف الحاجات دي:

#### الـ Endpoint المستلم:
```
POST /api/webhooks/ai-events
```

#### الـ Envelope بتاعة الـ Webhook:
```json
{
  "eventType": "topography.completed",   // ← لازم يكون matching مع الـ AiWebhookEventTypes
  "jobId": "py-job-abc123",             // ← الـ Job ID اللي الـ .NET سجله (PythonJobId)
  "data": { ... },
  "timestamp": "2026-06-27T12:00:00Z"
}
```

#### الـ Event Types المطلوبة + الـ Payload Shape:

| Event Type | Payload Type | ملاحظات |
|------------|-------------|---------|
| `topography.completed` | `TopographyResultPayload` | شوف الحقول في `Planora.Application/Features/Analysis/Dtos/TopographyResultPayload.cs` |
| `soil.completed` | `SoilResultPayload` (+ nested `BearingResultPayload` + `SpectralIndicesPayload`) | شوف `SoilResultPayload.cs` |
| `risk.completed` | `RiskResultPayload` (+ nested `Flood/Seismic/ExpansiveSoil/Liquefaction`) | شوف `RiskResultPayload.cs` |
| `borehole.completed` | `BoreholeResultPayload` (+ nested `BoreholePlacementPoint[]`) | شوف `BoreholeResultPayload.cs` |
| `pdf.completed` | `PdfResultPayload` | شوف `PdfResultPayload.cs` |
| `analysis.failed` | `{ reason: string }` | لو أي job فشل |
| `analysis.completed` | `AggregatedAnalysisResultPayload` | الـ legacy path (لو عايزين يفضل شغال) |

**مهم:** الـ `jobId` لازم يبقى **نفس الـ ID** اللي الـ Python رجعه لما الـ Job اتعمل (الـ `PythonJobId`). الـ .NET بيستخدمه عشان يلاقي الـ `AnalysisJob` في الداتابيز`.

### 🟠 PART 3: المعلومات اللي لازم توصّل لفريق الـ Frontend

**المعلم:** الـ Frontend لازم يعرف 4 حاجات أساسية:

#### 1. الـ SignalR Connection URL + Auth

```
URL:  /hubs/notifications
Auth: ?access_token=<jwt>
```

بعد الـ Connection، لازم يعمل:
```typescript
await hubConnection.invoke("SubscribeToParcel", parcelId);
```

ده بينضم للـ Group `parcel:{parcelId}` — وبعدين أي broadcast للـ Group ده هيوصل.

#### 2. الـ New Method اللي لازم يسمعله

| الـ Method Name | اللي بيتحصل | إزاي يستعمله |
|----------------|-------------|--------------|
| `NotificationReceived` | الـ القديم — bell icon notification (title + message + link) | يعرض notification في الـ bell icon / toast |
| `AnalysisResultReceived` | 🆕 **الجديد** — الـ full analysis result data | يرسم الـ data فورًا في الـ UI component المناسب |

#### 3. شكل الـ `AnalysisResultEnvelope` اللي هيستلمه

```typescript
interface AnalysisResultEnvelope {
  eventType: string;           // "topography.completed" | "soil.completed" | ...
  parcelId: string;            // Guid
  analysisJobId: string;       // Guid
  analysisType: string;       // "Topography" | "Soil" | "Risk" | "Borehole" | "Pdf"
  result: unknown;            // الـ Payload — شكلها مختلف حسب الـ eventType
  timestamp: string;          // ISO 8601
}
```

#### 4. الـ `result` shape حسب الـ eventType

```typescript
// لما eventType = "topography.completed"
interface TopographyResultPayload {
  pythonJobId: string;
  elevationMin: number;
  elevationMax: number;
  elevationMean: number;
  slopeDistribution?: SlopeCategoryEntry[];
  cutVolume: number;
  fillVolume: number;
  netVolume: number;
  contourInterval: number;
  contourGeoJsonUrl?: string;
  pondingGeoJsonUrl?: string;
  pondingZonesCount?: number;
  pondingTotalArea?: number;
  elevationTileUrl?: string;
  slopeTileUrl?: string;
  demRasterUrl?: string;
  slopeRasterUrl?: string;
  metadata?: {
    copernicusDemVersion?: string;
    pixelResolutionMeters?: number;
    crs?: string;
    processingTimeSeconds?: number;
  };
}

// لما eventType = "soil.completed"
interface SoilResultPayload {
  sandPercent: number;
  siltPercent: number;
  clayPercent: number;
  bulkDensity: number;
  organicCarbon: number;
  ph: number;
  bearingCapacityEstimate: number;
  bearingCapacityCategory: string;
  primaryType?: string;
  usdaClass?: string;
  aiConfidence?: number;
  heatmapTileUrl?: string;
  soilTypeGeoJsonUrl?: string;
  depthProfileImageUrl?: string;
  bearing?: BearingResultPayload;
  spectralIndices?: { ndviMean?: number; bsiMean?: number; ndmiMean?: number };
  // ... باقي الحقول شوفها في SoilResultPayload.cs
}

// لما eventType = "risk.completed"
interface RiskResultPayload {
  overallRiskScore: number;
  overallRiskLevel?: string;
  floodRiskScore: number;
  seismicRiskScore: number;
  expansiveSoilRisk: number;
  liquefactionRisk: number;
  flood?: { score?: number; level?: string; factors?: any; geoJsonUrl?: string };
  seismic?: { score?: number; level?: string; factors?: any; source?: string; zone?: string };
  expansiveSoil?: { score?: number; level?: string; factors?: any; replacementDepth?: number };
  liquefaction?: { score?: number; level?: string; factors?: any; susceptibility?: string; methodology?: string };
  riskHeatmapTileUrl?: string;
  mitigationSuggestions?: any;
}

// لما eventType = "borehole.completed"
interface BoreholeResultPayload {
  minimumRequired: number;
  optimalCount: number;
  coveragePercentage: number;
  gridSize: number;
  placementStrategy: string;
  placementPoints?: BoreholePlacementPoint[];
  placementGeoJsonUrl?: string;
  traditionalBoreholeCount: number;
  traditionalEstimatedCost: number;
  optimizedBoreholeCount: number;
  optimizedEstimatedCost: number;
  savingsAmount: number;
  savingsPercentage: number;
  currency: string;
}

// لما eventType = "pdf.completed"
interface PdfResultPayload {
  pdfS3Url: string;
  pageCount?: number;
  sizeBytes?: number;
}
```

#### 5. الـ Frontend Code Pattern (مثال بالـ TypeScript)

```typescript
// 1. عمل Connection
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/notifications', {
    accessTokenFactory: () => this.authService.getAccessToken()
  })
  .withAutomaticReconnect()
  .build();

// 2. تسجيل الـ Handlers
connection.on('NotificationReceived', (notification: NotificationDto) => {
  // ده الـ القديم — يعرض الـ bell icon
  this.notificationService.show(notification);
});

connection.on('AnalysisResultReceived', (envelope: AnalysisResultEnvelope) => {
  // 🆕 ده الـ الجديد — يعرض الـ Data فورًا
  switch (envelope.eventType) {
    case 'topography.completed':
      this.topographyService.updateFromRealtime(envelope.result as TopographyResultPayload);
      break;
    case 'soil.completed':
      this.soilService.updateFromRealtime(envelope.result as SoilResultPayload);
      break;
    case 'risk.completed':
      this.riskService.updateFromRealtime(envelope.result as RiskResultPayload);
      break;
    case 'borehole.completed':
      this.boreholeService.updateFromRealtime(envelope.result as BoreholeResultPayload);
      break;
    case 'pdf.completed':
      this.pdfService.updateFromRealtime(envelope.result as PdfResultPayload);
      break;
    case 'analysis.completed':
      // الـ aggregated payload — يحتوي على كل النتايج مع بعض
      this.analysisService.updateAllFromRealtime(envelope.result as AggregatedAnalysisResultPayload);
      break;
  }
});

// 3. Start + Subscribe
await connection.start();
await connection.invoke('SubscribeToParcel', currentParcelId);
```

**التلميذ:** يعني الـ Frontend لازم يعمل 3 حاجات بس:
1. Connect بالـ JWT
2. Subscribe للـ Parcel
3. يسمع على `AnalysisResultReceived` ويعمل switch على الـ eventType

**المعلم:** بالظبط! وممكن يعمل الـ `withAutomaticReconnect()` عشان لو الـ Connection قطر يعيد الاتصال تلقائي.

---

## الفصل الرابع عشر: الـ Known Issues اللي لازم نعملها لاحقًا

**المعلم:** وأنا بشتغل لقيت 5 Bugs في الكود الموجود — كتبتهم في ملف `US-04-known-issues.md`. مش عملتهم عشان الـ scope بس لازم تعملهم قريب:

| # | الخطورة | المشكلة | الملف |
|---|---------|---------|-------|
| 1 | 🔴 Critical | الـ Webhook HMAC Middleware مش بيشتغل! الـ Path غلط: `/api/webhook` (singular) بس الـ Controller route هو `/api/webhooks` (plural). يعني الـ Endpoint مفتوح لأي حد | `Program.cs:51` |
| 2 | 🟡 Medium | يوجد Config section ميت (`Webhook` / `WebhookOptions`) مش مستخدم — الـ Middleware بيستخدم `AiOptions.SharedSecret` | `WebhookOptions.cs` |
| 3 | 🟡 Medium | الـ `ReportNotifier` بيستخدم `parcel_{id}` (underscore) بس الـ Clients بيضموا لـ `parcel:{id}` (colon). يعني الـ Reports مش بتوصل لحد | `ReportNotifier.cs` |
| 4 | 🟠 Low | الـ `RiskCompletedHandler` عنده Inline notification مش بيبعت للـ Group — على عكس كل الـ Handlers التانيين | `RiskCompletedHandler.cs` |
| 5 | 🔵 Info | `bearing.completed` مش متعامل معاه (صاحبك شغال عليه) | `AiWebhookEventTypes.cs` |

**التلميذ:** يا رجل! الأول خطير — الـ Webhook مفتوح من غير أي Authentication!

**المعلم:** أيوه وده Priority واحد. لازم يتعمل بسرعة. بس مش في الـ scope ده.

---

## الفصل الخامس عشر: الخلاصة

**التلميذ:** يا أستاذ، أنا فهمت الدنيا! خليني ألخص:

1. **الـ Backend** عمل Interface جديد في `INotificationPublisher` اسمه `PublishAnalysisResultAsync` اللي بيعمل broadcast لـ `parcel:{id}` group عن طريق SignalR.

2. **الـ Envelope** اللي بيتحرك اسمه `AnalysisResultEnvelope` وبيحمل: الـ eventType، parcelId، analysisJobId، analysisType، الـ **full result payload**، وtimestamp.

3. **كل Handler** من الـ 5 (Topography, Soil, Risk, Borehole, PDF) + الـ Aggregated — بيعمل call لـ `AnalysisNotificationHelper.PublishAnalysisResultAsync` بعد ما يخزن النتيجة في الـ DB.

4. **الـ Frontend** لازم يعمل Connect على `/hubs/notifications?access_token=<jwt>`، ينضم للـ Group عن طريق `SubscribeToParcel`، ويسمع على method اسمها `AnalysisResultReceived`.

5. **الـ Python** لازم يبعت per-type events (كل type لوحده) مش aggregated واحد.

6. **في Bugs** لازم تتعمل (خاصة الـ HMAC middleware اللي مش بيشتغل).

**المعلم:** الله يبارك فيك! فهمت كويس. يلا نروح نعمل الـ Bugs دي بقى!

---

*تم الكتابة يوم 2026-06-27 — عبدالرحمن 🇪🇬*

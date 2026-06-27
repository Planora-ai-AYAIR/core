# المشاكل الحرجة في التكامل بين .NET Backend و AI Python Engine

> ملف توضيحي بالمصري عن كل المشاكل اللي محتاجين نحلها قبل أي حاجة تانية
> المصدر: ردود فريق Ai Python على أسئلة التكامل
> التاريخ: 2026-06-27

---

## نظرة عامة سريعة (TL;DR)

في **10 مشاكل حرجة** هتمنع التكامل من إنه يشتغل أصلاً. لازم نحلهم بالترتيب ده قبل ما نكمل أي حاجة تانية. الـ Job اللي عالق دلوقتي للـ parcel `cb00eec8...` سببه واحد أو أكتر من المشاكل دي.

---

## 🔴 المشاكل الحرجة (Critical) — لازم تتحل دلوقتي

### 1️⃣ شكل الـ Response غلط تماماً (Response Shape Mismatch)

**المشكلة:**
- إحنا في الـ Refit client بنفك الـ response على إنه `string` عادي (يعني الـ jobId كده مباشرة).
- بايثون بترجع **JSON Envelope** كامل، الـ jobId جوه `data.pythonJobId`.

**اللي بايثون بترجعه فعلاً:**
```json
{
  "statusCode": 202,
  "message": "Python topography job queued",
  "errors": null,
  "data": {
    "pythonJobId": "pyjob_topo_a1b2c3d4e5f6",
    "status": "queued",
    "acceptedAt": "2026-06-27T12:00:00.000000+00:00"
  }
}
```

**التأثير:** كل request بيبعت بيفشل في الـ parsing → الـ job ميتسجلش صح في الـ DB.

**الحل المطلوب:**
- نعمل DTO جديد للـ envelope: `AiResponseEnvelope<T>` فيه `statusCode`, `message`, `errors`, `data`.
- نعدل كل الـ Refit interfaces عشان ترجع الـ envelope بدل الـ string.
- نطلع `pythonJobId` من `response.Data.PythonJobId`.

---

### 2️⃣ اسم الفيلد غلط: `pythonJobId` مش `jobId`

**المشكلة:** في الـ §3 endpoints (اللي إحنا بنستخدمها)، الفيلد اسمه `pythonJobId` مش `jobId`.

**الحل:** في الـ DTO نسمي الـ property `PythonJobId` ونعمله `[JsonPropertyName("pythonJobId")]`.

---

### 3️⃣ الـ Request Body شكله غلط 100% — هيرجع 422 على طول

**اللي إحنا بنبعته دلوقتي (غلط):**
```json
{
  "parcelId": "...",
  "boundaryGeoJson": "...",
  "areaHectares": 5.2,
  "centroidLatitude": 30.1,
  "centroidLongitude": 31.2
}
```

**اللي بايثون عايزاه (صح):**
```json
{
  "jobId": "your-dotnet-job-id-guid",
  "parcelId": "...",
  "geoJson": {
    "type": "Polygon",
    "coordinates": [[[lon, lat], ...]]
  },
  "bbox": { "minX": ..., "minY": ..., "maxX": ..., "maxY": ... }
}
```

**الفروقات الحرجة:**
| إحنا بنبعت | بايثون عايزة | الحالة |
|------------|--------------|--------|
| ❌ مش بنبعت `jobId` | ✅ `jobId` مطلوب | **هيرجع 422** |
| `boundaryGeoJson` (string) | `geoJson` (object) | **هيرجع 422** |
| `areaHectares` | مش موجود في الـ schema | هيتعمله ignore أو 422 |
| `centroidLatitude/Longitude` | مش موجودين | هيتعملوا ignore أو 422 |

**الحل المطلوب:**
- نعدل `ProccessTopographyJobAiRequest` (وكل الـ requests التانية: Soil, Risk, Borehole) عشان يبقوا متطابقين مع schema بايثون.
- نضيف فيلد `JobId` ونبعت فيه الـ `AnalysisJobId` بتاعنا.
- نحول `Boundary` من string GeoJSON لـ object فيه `type` و `coordinates`.
- نشيل الـ fields اللي مالهاش لازمة (`areaHectares`, `centroid*`).
- نضيف per-module options (هنشوفهم في النقطة الجاية).

---

### 4️⃣ الـ Per-Module Options ناقصة

كل module عايز options مخصوصة:

**Topography:**
```json
"options": {
  "contourInterval": 0.5,
  "slopeCategories": [2, 5, 15],
  "generateCutFill": true,
  "referencePlane": "auto"
}
```

**Soil:**
```json
"depths": ["0-20cm", "20-50cm", "50-100cm", "100-200cm"]
```

**Risk:**
```json
"riskTypes": ["flood", "seismic", "expansiveSoil", "liquefaction"],
"soilData": { "clayContent": ..., "sandContent": ..., "waterTableDepth": ... }
```

**Borehole:**
```json
"parameters": { "maxSpacing": 30, "minBoreholes": 12, "targetDepth": 20 }
```

**الحل:** نضيف الـ options دي في الـ requests (ممكن نخليها optional ونعتمد على الـ defaults في الأول).

---

### 5️⃣ الـ Base URL ممكن يكون ناقصه `/api/v1`

**المشكلة:** بايثون كل الـ endpoints تحت prefix `/api/v1`.

**التحقق المطلوب:** نفتح إعدادات الـ Refit client ونتأكد إن:
```
BaseUrl = http://{host}:8000/api/v1
```
مش:
```
BaseUrl = http://{host}:8000   ❌
```

**ملاحظة إضافية:** في الـ Refit endpoint بنكتب `/risks/jobs` وده **صح** (مش `/risk/`). كنت قلقان منها بس اتأكدت إنها سليمة.

---

## 🟠 مشاكل مهمة جداً (High Priority) — تتحل بعد اللي فوق

### 6️⃣ Webhook URL مش متظبط — السبب الأكبر للـ "Stuck Pending"

**المشكلة:** بايثون مش هتبعت أي webhook لو الـ env متغيرات دي مش متظبطة:
- `WEBHOOK_URL` — العنوان العام بتاع .NET backend
- `SHARED_SECRET` — السر بتاع الـ HMAC

لو واحد منهم فاضي، الـ webhook **بيتسكت تماماً** (silent skip) من غير أي error.

**التحقق المطلوب:**
1. نفتح ملف `.env` بتاع بايثون ونتأكد إن `WEBHOOK_URL` و `SHARED_SECRET` متظبطين.
2. لو شغالين locally، لازم نستخدم tunnel (ngrok أو cloudflared) عشان بايثون توصل لـ .NET.
3. نتأكد إن الـ URL ده يقدر يوصل من الـ Python container للـ .NET (firewall, network).

**الحل في .NET:** نتأكد إن HMAC verification شغال في الـ webhook endpoint بنفس الـ secret.

---

### 7️⃣ مفيش Webhook لما الـ Job يفشل (No Failure Webhooks)

**المشكلة:** بايثون **مش بتبعت** events زي `topography.failed` أو `soil.failed`. لما الـ job يفشل، الـ status بيتسجل بس في الـ in-memory dict.

**التأثير:** لو الـ AI Pipeline فشل، إحنا مش هنعرف. الـ AnalysisJob هيفضل في حالة `Running` للأبد.

**الحل المطلوب:**
- نضيف **Polling Fallback** في .NET: لو معدّى timeout (مثلاً 10 دقائق) من غير webhook، نعمل polling لـ:
  ```
  GET /api/v1/{module}/jobs/{pythonJobId}
  ```
- نشوف الـ status لو `failed` نسجل الفشل ونعمل update للـ AnalysisJob.

---

### 8️⃣ مفيش Bearing Endpoint في §3

**المشكلة:** الـ §3 internal API **مفيهاش** `/api/v1/bearing/jobs`. الـ bearing بتتحسب جوه `analysis.completed` بس.

**التأثير:** لو هنبعت per-module، الـ bearing هيستنانا للأبد عشان webhook مش هيوصل.

**الحلين المتاحين:**
- **الحل أ (الأسهل):** نستخدم `POST /api/v1/analysis/jobs` (unified pipeline) ونستنى `analysis.completed` بدل ما نبعت كل module لوحده.
- **الحل ب:** نشيل الـ bearing من الـ flow الحالي ونعمله لاحقاً.

**التوصية:** نراجع الـ business logic — هل فعلاً محتاجين per-module submission ولا الـ unified analysis أحسن؟

---

### 9️⃣ JobId بايثون شكله مختلف عن اللي إحنا متوقعينه

**الـ Format الفعلي:**
- Topography: `pyjob_topo_a1b2c3d4e5f6` (22 char)
- Soil: `pyjob_soil_{12 hex}`
- Risk: `pyjob_risk_{12 hex}`
- Borehole: `pyjob_bore_{12 hex}`
- PDF: `pyjob_pdf_{12 hex}`

**الحل:** نتأكد إن العمود `PythonJobId` في الـ DB يستحمل 32 char على الأقل.

---

### 🔟 الـ Webhook Envelope Shape

**الشكل اللي بييجي:**
```json
{
  "eventType": "topography.completed",
  "jobId": "pyjob_topo_a1b2c3d4e5f6",
  "data": { ... },
  "timestamp": "2026-06-27T12:34:56.789000Z"
}
```

**ملاحظات مهمة:**
- الـ `jobId` في الـ envelope = الـ `pythonJobId` (مش الـ .NET jobId).
- لازم نلاقي الـ AnalysisJob row عن طريق matching على `pythonJobId`.
- اسم الـ event: `risk.completed` (مش `risks.completed`).
- الـ `pdf.completed` matches اللي عندنا ✅.

---

## 🟡 مشاكل متوسطة (Medium) — تتعمل بعد ما الـ flow الأساسي يشتغل

### 1️⃣1️⃣ Idempotency — Hangfire Retries هيعمل Duplicate Jobs

**المشكلة:** بايثون مفيهاش idempotency keys. كل POST بيعمل job جديد. لو Hangfire عمل retry، هيتعملك duplicate.

**الحل:**
- لو الـ AnalysisJob عنده `PythonJobId` موجود بالفعل، نـ skip الـ re-submission في الـ Hangfire job.
- نتشيك على الـ status: لو `Running` خلاص متبعتش تاني.

---

### 1️⃣2️⃣ Webhook مفهاش Retry من جهة بايثون

**المشكلة:** بايثون بتبعت الـ webhook مرة واحدة بس بـ timeout 10 ثواني. لو فشل، بيتنسى تماماً.

**الحل:** الـ Polling Fallback (نقطة 7) هيغطي الموضوع ده كمان.

---

### 1️⃣3️⃣ مفيش Authentication على Endpoints بايثون

**ملاحظة:** الـ `AiApiKeyHandler` المعلق ميتاحش — مفيش auth أصلاً في بايثون.

**التوصية:** نخليه معلق دلوقتي، ونضيف auth لاحقاً (zerust سياسة).

---

## 📋 ترتيب العمل المقترح (Priority Order)

### المرحلة الأولى — خلي الـ Request يشتغل أصلاً
1. ✅ نعمل DTO `AiResponseEnvelope<T>` للـ response.
2. ✅ نعدل كل الـ Request DTOs (Topography, Soil, Risk, Borehole) عشان يبقوا match مع بايثون.
3. ✅ نضيف `JobId` و `GeoJson` (object) و نشيل الـ fields الزيادة.
4. ✅ نتأكد من الـ BaseUrl إن فيه `/api/v1`.

### المرحلة الثانية — خلي الـ Webhook يوصل
5. ✅ نتأكد من `WEBHOOK_URL` و `SHARED_SECRET` في إعدادات بايثون.
6. ✅ نـ enable الـ HMAC verification في .NET webhook endpoint.
7. ✅ نختبر end-to-end لـ Topography module بس.

### المرحلة الثالثة — Reliability
8. ✅ نضيف Polling Fallback للـ failure detection.
9. ✅ نضيف Idempotency check قبل الـ re-submission.
10. ✅ نراجع الـ Bearing strategy (unified vs per-module).

### المرحلة الرابعة — Polish
11. نضيف per-module options (contour interval, depths, etc.).
12. نراجع الـ timing expectations (2-6 دقايق مش ساعات).
13. نوثق الـ error codes mapping.

---

## 🐛 تفسير المشكلة الحالية (Parcel `cb00eec8...`)

السبب الأرجح إن الـ job عالق:

**الاحتمال الأول (الأرجح):** الـ Request اتبعت بشكل غلط → بايثون رجعت 422 → الـ Refit client حاول يفك الـ response كـ string ففشل → الـ exception اتقفلت أو الـ retry loop عمل لخبطة.

**الاحتمال التاني:** الـ Request اتبعت صح بالصدفة، بايثون شغلت الـ job، بس الـ `WEBHOOK_URL` مش متظبط أو مش reachable → الـ webhook متوصلش → الـ job عالق في `Running`.

**الاحتمال التالت:** الـ Python process عمل restart → الـ in-memory job state اتمسح → النتيجة ضاعت.

**الخطوات اللي لازم نعملها فوراً:**
1. نشوف logs بايثون ونبص على lines فيها `Job queued` أو `Webhook delivered` أو `Webhook delivery failed`.
2. نتأكد إن `WEBHOOK_URL` و `SHARED_SECRET` متظبطين في الـ `.env`.
3. نـ `curl` الـ `WEBHOOK_URL` من جوه container بايثون عشان نتأكد إنه reachable.
4. لو الـ jobs لسة موجودة في الـ in-memory store، نعمل polling عن طريق `GET /api/v1/topography/jobs/{pythonJobId}`.

---

## 📌 خلاصة المشاكل في جدول واحد

| # | المشكلة | الخطورة | الـ Effort |
|---|---------|---------|-----------|
| 1 | Response shape غلط (envelope) | 🔴 حرجة | صغير |
| 2 | اسم الفيلد `pythonJobId` | 🔴 حرجة | صغير |
| 3 | Request schema غلط 100% | 🔴 حرجة | متوسط |
| 4 | Per-module options ناقصة | 🔴 حرجة | متوسط |
| 5 | Base URL ممكن يكون ناقص `/api/v1` | 🔴 حرجة | صغير |
| 6 | Webhook URL مش متظبط | 🟠 عالية | إعدادات |
| 7 | مفيش failure webhooks | 🟠 عالية | كبير (polling) |
| 8 | مفيش bearing في §3 | 🟠 عالية | قرار معماري |
| 9 | JobId format | 🟡 متوسطة | صغير |
| 10 | Webhook envelope mapping | 🟠 عالية | صغير |
| 11 | Idempotency | 🟡 متوسطة | متوسط |
| 12 | مفيش webhook retry | 🟡 متوسطة | مغطاة بالـ polling |
| 13 | مفيش auth | 🟢 منخفضة | لاحقاً |

---

> **الخطوة الجاية المقترحة:** نبدأ بالمرحلة الأولى (نقاط 1-5) عشان نخلي أول request يشتغل صح، وبعدين نتحرك للمرحلة الثانية.

# شرح موديول الآبار (Boreholes) — `apps/ai-python`

> ⚠️ **ملاحظة مهمة:** ملف [`describe.md`](describe.md) الحالي **مش متحدث** — مكتوب وقت ما كان السيستم بس فيه
> `analyze` و `topography`، ومش بيذكر موديول الآبار (`boreholes`) ولا `risks` ولا `reports` ولا الـ
> client-facing routers (`app/routers/client/*`) أصلاً. الشرح ده مبني على **قراءة الكود الحالي فعليًا**
> (الراوترز + السيرفسز + الـ schemas)، مش على `describe.md`.

---

## 1. الآبار دي معناها إيه هنا؟

"Boreholes" = **آبار الاستكشاف الجيوتقني** (boring holes) اللي بتُحفر في الأرض لأخذ عينات تربة حقيقية
وتأكيد قراءات الموديولات التانية (soil / bearing capacity). الموديول هنا اسمه رسميًا:

> **Module 5 — Optimized Borehole Campaign Plan**

ومكانه في ترتيب خط الأنابيب (pipeline) المعرّف في [`app/services/store.py:29`](app/services/store.py):

```
topography → soil → bearing → risk → borehole → report
```

يعني الآبار هي **الموديول الخامس**، وبعده مباشرة موديول التقرير النهائي (`report`).

---

## 2. فيه مسارين منفصلين تمامًا للآبار (مهم جدًا)

النظام فيه **API داخلي (AI engine)** و **API خاص بالـ client (.NET)**، وكل واحد له تنفيذ مختلف
تمامًا للآبار:

| | المسار الداخلي (AI Engine) | المسار الخاص بالعميل (Client-facing) |
|---|---|---|
| **الراوتر** | [`app/routers/boreholes.py`](app/routers/boreholes.py) | [`app/routers/client/boreholes.py`](app/routers/client/boreholes.py) |
| **الـ endpoint** | `POST/GET /api/v1/boreholes/jobs[/{id}]` | `POST /api/boreholes/jobs` + `GET /api/boreholes/{parcelId}` |
| **المرجع في العقد** | §3.4 | §2.6 |
| **منطق الحساب** | حقيقي — `borehole_service.optimize_boreholes()` | **Mock ثابت** — `client_mocks.borehole_result()` |
| **بيستخدم الـ bbox الحقيقي؟** | ✅ نعم | ❌ لا — بيانات ثابتة (hardcoded) |
| **مخزّن الـ job** | dict داخلي `_jobs = {}` في الراوتر نفسه | `app/services/store.py` (مشترك مع كل الموديولات) |

**الخلاصة:** لو حد سأل "الآبار شغالة إزاي فعليًا؟" — لازم نفرّق: المسار `/api/v1/boreholes/jobs`
(الداخلي) فيه **خوارزمية حقيقية** بتحسب نقاط الحفر من شكل الأرض. أما المسار `/api/boreholes/...`
(اللي الفرونت إند المفروض يستخدمه) **لسه بيرجع داتا تجريبية ثابتة** مش متصلة بالخوارزمية الحقيقية —
ده موضّح صريح في تعليق الكود نفسه في [`client_mocks.py:8-10`](app/services/client_mocks.py):

> *"The real AI-engine services (... `borehole_service`) can replace these later — the JSON shape
> returned here is the contract the frontend depends on."*

---

## 3. الخوارزمية الحقيقية — `borehole_service.py`

الملف: [`app/services/borehole_service.py`](app/services/borehole_service.py)

### 3.1 الثوابت

```python
COST_PER_BOREHOLE_EGP = 14000   # تكلفة تقريبية لبئر واحد بعمق 20م (سعر سوق)
TRADITIONAL_SPACING_M = 15      # المسافة بين الآبار في الطريقة "التقليدية" المحافظة
```

### 3.2 الدالة الأساسية: `optimize_boreholes(bbox, max_spacing, min_boreholes, target_depth, hotspot_zones, homogeneous_zones)`

بترجع plan كامل بالخطوات التالية:

#### (أ) توليد شبكة نقاط — `_generate_grid_points(bbox, spacing_m, min_count)`

1. **تحويل من درجات إلى أمتار** (لأن الـ bbox جوّه بالـ lon/lat بالدرجات، والـ spacing بالأمتار):
   ```
   lat_mid     = (minY + maxY) / 2
   deg_to_m_lon = 111320 × cos(radians(lat_mid))     # طول درجة الطول عند خط العرض ده
   deg_to_m_lat = 110540                              # طول درجة العرض (تقريبًا ثابت)

   width_m  = (maxX − minX) × deg_to_m_lon
   height_m = (maxY − minY) × deg_to_m_lat
   ```
2. **عدد الأعمدة/الصفوف** بناءً على المسافة المطلوبة بين الآبار:
   ```
   n_cols = max(2, width_m  / spacing_m)
   n_rows = max(2, height_m / spacing_m)
   ```
3. **ضمان الحد الأدنى لعدد الآبار** (`min_boreholes`): لو `n_cols × n_rows` أقل من المطلوب،
   بيزوّد `n_cols` (وبعدين `n_rows` لو لسه مش كفاية) لحد ما يوصل للعدد:
   ```
   while n_cols × n_rows < min_count:
       n_cols += 1
       if n_cols × n_rows < min_count:
           n_rows += 1
   ```
4. **توليد الشبكة الفعلية**: نقطة عند **كل تقاطع** فى الشبكة (مش وسط الخلايا) — يعني عدد النقط
   النهائي هو `(n_rows + 1) × (n_cols + 1)`، أكبر من `n_cols × n_rows`:
   ```
   dx = (maxX − minX) / n_cols
   dy = (maxY − minY) / n_rows

   for r in 0..n_rows:
     for c in 0..n_cols:
       lat = minY + r×dy
       lng = minX + c×dx
       point = { id: "BH-{idx:03d}", lat, lng, priority: "Medium", reason: "Grid point" }
   ```

> ⚠️ **ملاحظة تقنية:** بسبب الخطوة 4، `optimalCount` النهائي يكون **أكبر من** `min_boreholes`
> اللي طلبها العميل غالبًا — لأنه عدد نقاط تقاطع الشبكة مش عدد الخلايا.

#### (ب) توزيع الأولويات — `_assign_priorities(points, hotspot_zones)`

- **لو مفيش `hotspotZones`** (مناطق تباين التربة) جاية من موديول التربة:
  - أول نقطة وآخر نقطة في الليستة → `priority = "High"`, `reason = "Corner reference point"`
  - النقطة في النص (`len(points)//2`) → `priority = "Critical"`, `reason = "Central reference point"`
  - باقي النقط تفضل `"Medium"` / `"Grid point"`

- **لو فيه `hotspotZones`**:
  ```python
  for point in points:
      for zone in hotspot_zones:
          point["priority"] = "High"
          point["reason"]   = "Soil variability hotspot"
          break
  ```
  > ⚠️ **ده تبسيط ملحوظ في الكود الحالي:** المنطق ده **بيرفع كل نقطة في الشبكة كلها لـ "High"**
  > بمجرد وجود ولو "hotspot zone" واحدة — مش فيه فحص جغرافي حقيقي (intersection/distance) إن
  > كانت النقطة فعلًا قريبة من المنطقة الساخنة ولا لأ. يعني لو فيه hotspot واحد، كل الآبار في
  > الخطة هتبقى أولوية "High" بدون تمييز.

#### (ج) المقارنة بالطريقة التقليدية + التكلفة

```
optimal_count      = عدد نقط الشبكة المُحسّنة (بالـ max_spacing المطلوب، افتراضي 30م)
traditional_count   = عدد نقط شبكة بنفس المنطقة لكن بمسافة TRADITIONAL_SPACING_M = 15م (أكثف)

traditional_cost = traditional_count × 14000
optimized_cost   = optimal_count    × 14000
savings_amount   = traditional_cost − optimized_cost
savings_pct      = round(savings_amount / traditional_cost × 100)     (0 لو traditional_cost = 0)
```

**الفايدة الأساسية من الخوارزمية:** المسافة الأكبر بين الآبار (30م بدل 15م) = عدد آبار أقل
= فلوس أقل، وده اللي بيتحسب في `costComparison.savings`.

### 3.3 شكل النتيجة النهائية (`BoreholeResults` — `app/schemas/boreholes.py`)

```json
{
  "minimumRequired": 12,
  "optimalCount": 20,
  "placementPoints": [
    { "id": "BH-001", "lat": 29.781, "lng": 31.497, "priority": "High", "reason": "Corner reference point" },
    ...
  ],
  "costComparison": {
    "traditional": { "count": 30, "cost": 420000 },
    "optimized":   { "count": 20, "cost": 280000 },
    "savings":     { "amount": 140000, "percentage": 33 }
  }
}
```

---

## 4. الـ API الداخلي خطوة بخطوة — `app/routers/boreholes.py`

```
POST /api/v1/boreholes/jobs
  body: BoreholeJobRequest {
    jobId, parcelId, geoJson, bbox?,
    soilVariability?: { hotspotZones: [...], homogeneousZones: [...] },
    parameters: { maxSpacing: 30, minBoreholes: 12, targetDepth: 20 }
  }
  →
  python_job_id = "pyjob_bore_" + uuid4().hex[:12]
  يتخزن في _jobs[python_job_id] = { status: "queued", results: None, error: None }
  bg.add_task(_run_borehole_pipeline, ...)        ← async، يرجع فورًا
  ←  202 Accepted  { pythonJobId, status: "queued", acceptedAt }

GET /api/v1/boreholes/jobs/{pythonJobId}
  لو الـ id مش موجود  → 404  JOB_NOT_FOUND
  لو موجود            → status (queued/processing/completed/failed) + results (لو completed)
```

**الـ background pipeline (`_run_borehole_pipeline`)**:
1. يحوّل `status → "processing"`
2. يستخرج `bbox`, `hotspotZones`, `homogeneousZones` من الـ request
3. يستدعي `optimize_boreholes(...)`
4. لو نجح: `status → "completed"`, `results = ...`
5. لو فشل (exception): `status → "failed"`, `error = { code: INTERNAL_ERROR, message }`

> ملحوظة: تخزين الـ jobs هنا (`_jobs = {}`) هو **dict في الذاكرة جوّه الراوتر نفسه** — يعني
> بيُمسح لو السيرفر اتعمل له restart، ومش متصل بـ Redis (اللي هو لسه stub فاضي حسب `describe.md`
> القديم، ولسه فاضي فعليًا في الكود الحالي كمان).

---

## 5. الـ API الخاص بالعميل (.NET-facing) — `app/routers/client/boreholes.py`

```
POST /api/boreholes/jobs
  body: BoreholeJobSubmit { parcelId, parameters: { maxSpacing, minBoreholes, targetDepth, unit } }
  →
  require_parcel(parcelId)              ← 404 PARCEL_NOT_FOUND لو القطعة مش موجودة في store
  job = make_job("borehole", parcelId)   ← jobId = "job_borehole_" + uuid4().hex[:8]
  bg.add_task(run_job, ...)
  ←  202 Accepted { jobId, parcelId, status: "queued", estimatedDuration: "2-6 hours" }

GET /api/boreholes/{parcelId}
  require_result(parcelId, "borehole")
    → 404 PARCEL_NOT_FOUND  لو القطعة مش موجودة
    → 409 JOB_NOT_COMPLETED لو لسه مفيش نتيجة جاهزة
  ←  200  BoreholeClientResult (لو جاهزة)
```

**الـ background (`run_job` في [`_helpers.py`](app/routers/client/_helpers.py))**:
1. `status → "processing"`, `progressPercentage = 50`
2. **بيستدعي `client_mocks.build_result("borehole", parcelId)` — يعني بيانات ثابتة دايمًا، مش
   مرتبطة بشكل القطعة أو بارامترات الطلب أصلًا** (نقطة واحدة فقط `BH-001` عند `lat=31.05, lng=30.02`
   ثابتة، بغض النظر عن الـ parcel).
3. `status → "completed"`, `progressPercentage = 100`

### شكل النتيجة (Mock) — `BoreholeClientResult`:

```json
{
  "parcelId": "...",
  "recommendation": { "minimumRequired": 12, "optimalCount": 18, "coveragePercentage": 85, "gridSize": "30m spacing" },
  "placement": {
    "strategy": "Adaptive grid with hotspots",
    "points": [ { "id": "BH-001", "latitude": 31.05, "longitude": 30.02, "priority": "High", "reason": "Soil variability hotspot", "estimatedDepth": 20 } ],
    "geoJsonUrl": "https://s3.amazonaws.com/geosense/{parcelId}/boreholes.geojson?signature=..."
  },
  "costAnalysis": {
    "traditionalApproach": { "boreholes": 30, "estimatedCost": 420000, "currency": "EGP" },
    "optimizedApproach":   { "boreholes": 12, "estimatedCost": 180000, "currency": "EGP" },
    "savings": { "amount": 240000, "currency": "EGP", "percentage": 57 }
  },
  "generatedAt": "..."
}
```

---

## 6. الفرق الجوهري بين المسارين — خلاصة

| الجانب | الداخلي `/api/v1/boreholes/jobs` | الخاص بالعميل `/api/boreholes/jobs` |
|---|---|---|
| نقاط الحفر بتُحسب من شكل الأرض الحقيقي (bbox)؟ | ✅ | ❌ (نقطة واحدة ثابتة دايمًا) |
| التكلفة بتُحسب من عدد النقط الفعلي؟ | ✅ (`count × 14000`) | ❌ (أرقام ثابتة: 30/420000 و 12/180000) |
| الـ `geoJsonUrl` لملف الآبار؟ | غير موجود في الـ schema الداخلي | موجود لكنه رابط S3 وهمي (مفيش فعليًا ملف موجود) |
| تخزين الـ job | dict مؤقت في الراوتر | `store.py` المشترك |

**عمليًا:** لو السؤال هو "إزاي الفرونت إند بيشوف خطة الآبار؟" — هو حاليًا بيشوف **mock ثابت**، لأن
موديول العميل (`client/boreholes.py`) **لسه مش متوصّل** بـ `borehole_service.optimize_boreholes()`
الحقيقي. التوصيل ده هو الخطوة التالية المنطقية لو الهدف تفعيل حسابات حقيقية على الـ endpoint
اللي الفرونت إند يستخدمه.

---

## 7. خريطة الملفات (Boreholes فقط)

```
app/
├── routers/
│   ├── boreholes.py            ← API داخلي حقيقي (§3.4) — POST/GET /api/v1/boreholes/jobs
│   └── client/
│       └── boreholes.py        ← API للعميل (§2.6) — POST /api/boreholes/jobs، GET /api/boreholes/{id}
├── schemas/
│   ├── boreholes.py            ← request/response الداخلي (BoreholeJobRequest, BoreholeResults, ...)
│   └── client/__init__.py      ← request/response الخاص بالعميل (BoreholeJobSubmit, BoreholeClientResult, ...)
└── services/
    ├── borehole_service.py     ← الخوارزمية الحقيقية (optimize_boreholes)
    ├── client_mocks.py         ← borehole_result() — الداتا الثابتة المستخدمة فعليًا في مسار العميل
    └── store.py                ← تخزين الـ jobs/results في الذاكرة + ترتيب الموديولات (MODULE_ORDER)
```

---

## 8. النقاط اللي تستحق اهتمام لو حابب تطور الموديول ده

1. **توصيل المسار الخاص بالعميل بالخوارزمية الحقيقية** — `client/boreholes.py` لسه بيستخدم
   `client_mocks.borehole_result()` مش `borehole_service.optimize_boreholes()`.
2. **منطق الأولوية حسب الـ hotspots ساذج** — `_assign_priorities` بيرفع **كل** النقط لـ "High"
   لو فيه أي hotspot zone، بدل ما يفحص هل النقطة فعلًا داخل/قريبة من الزون.
3. **`minimumRequired` مش محسوب فعليًا** — هو بس نفس القيمة اللي جت في الطلب (`min_boreholes`)،
   مفيش حساب هندسي/جيوتقني حقيقي للحد الأدنى المطلوب.
4. **تخزين الـ jobs الداخلي في الذاكرة** (`_jobs = {}`) — بدون Redis أو قاعدة بيانات، فبيُمسح
   عند أي إعادة تشغيل للسيرفر.
5. **`geoJsonUrl` في مسار العميل وهمي** — رابط S3 شكلي، مفيش ملف GeoJSON حقيقي بيتولد فعليًا.

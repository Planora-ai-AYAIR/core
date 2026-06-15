# شرح شامل لكود مشروع `ai-python`

> **المشروع:** GeoSense AI — نظام تحليل التضاريس الجغرافية  
> **التقنية:** FastAPI + Google Earth Engine (GEE)  
> **النطاق (Day 1):** مصادقة GEE + تصدير DEM (نموذج الارتفاع الرقمي)

---

## هيكل المشروع

```
ai-python/
├── .env.template           ← قالب متغيرات البيئة
├── .gitignore              ← ملفات مستثناة من Git
├── Dockerfile              ← تكوين Docker
├── pyproject.toml          ← بيانات المشروع
├── requirements.txt        ← المكتبات المطلوبة
│
├── app/
│   ├── __init__.py
│   ├── main.py             ← نقطة دخول التطبيق
│   ├── config.py           ← إدارة الإعدادات
│   │
│   ├── routers/
│   │   ├── __init__.py
│   │   └── topography.py   ← endpoints الـ API
│   │
│   ├── schemas/
│   │   ├── __init__.py
│   │   └── topography.py   ← نماذج البيانات (Pydantic)
│   │
│   └── services/
│       ├── __init__.py
│       ├── gee_service.py       ← خدمة Google Earth Engine (مكتملة)
│       ├── redis_service.py     ← خدمة Redis (مؤجلة لـ Day 2)
│       ├── terrain_service.py   ← تحليل التضاريس (مؤجل لـ Day 2)
│       ├── tiles_service.py     ← خدمة الـ tiles (مؤجلة لـ Day 2)
│       └── topography_service.py← خدمة التضاريس (مؤجلة لـ Day 2)
│
└── tests/
    ├── __init__.py
    ├── test_api.py             ← اختبارات الـ API
    ├── test_gee.py             ← اختبار اتصال GEE مباشرة
    ├── test_gee_drive_export.py← اختبارات تصدير DEM
    └── test_topography.py      ← (فارغ حالياً)
```

---

## ملفات الجذر (Root Level)

---

### `.env.template`

**الغرض:** قالب لملف الإعدادات السرية — يُنسخ إلى `.env` ويُملأ بالقيم الحقيقية، ولا يُرفع على Git أبداً.

**المتغيرات:**

| المتغير | الوصف | مثال |
|--------|-------|------|
| `GEE_PROJECT` | معرّف مشروع GEE على Google Cloud | `planora-497717` |
| `GEE_SERVICE_ACCOUNT_EMAIL` | إيميل الـ Service Account | `planora@planora-497717.iam.gserviceaccount.com` |
| `GEE_SERVICE_ACCOUNT_KEY` | مسار ملف JSON للمصادقة | `./secrets/gee_key.json` |
| `REDIS_URL` | رابط الاتصال بـ Redis | `redis://localhost:6379` |
| `LOCAL_OUT_DIR` | مجلد الملفات المؤقتة | `/tmp/geosense` |
| `EGYPT_BBOX` | حدود مصر الجغرافية | `24.0,22.0,37.0,32.0` |
| `API_TITLE` | عنوان الـ API في Swagger | `GeoSense API` |
| `API_VERSION` | إصدار الـ API | `0.1.0` |

**ملاحظة:** حدود مصر المستخدمة هي `[24°E → 37°E, 22°N → 32°N]` بنظام إحداثيات WGS84 (EPSG:4326).

---

### `.gitignore`

**الغرض:** يحدد الملفات والمجلدات التي يجب ألا تُرفع على Git لأسباب أمنية أو تشغيلية.

**أهم ما يستثنيه:**
- `venv/` — البيئة الافتراضية للـ Python
- `__pycache__/` — ملفات Python المُجمَّعة
- `.env` — الإعدادات السرية (مفاتيح GEE)
- `secrets/` — مجلد مفاتيح Service Account
- `*.pyc` — ملفات bytecode
- `*.tif`, `*.shp`, `*.geojson` — ملفات الخرائط والـ GeoTIFF الضخمة
- `.vscode/` — إعدادات المحرر الشخصية

---

### `Dockerfile`

**الغرض:** تعريف كيفية بناء container Docker للتطبيق.

```dockerfile
FROM python:3.11-slim    # صورة Python خفيفة الوزن
WORKDIR /app             # مجلد العمل داخل الـ container
COPY . .                 # نسخ كل الملفات
CMD ["python", "-c", "print('ai-python container placeholder')"]
```

**الحالة الحالية:** placeholder بسيط — لم يُكتمل بعد (لا تثبيت للمكتبات، لا تشغيل uvicorn). سيُكتمل في مراحل لاحقة.

---

### `pyproject.toml`

**الغرض:** بيانات تعريف المشروع وفق معيار Python الحديث (PEP 517/518).

```toml
[project]
name = "geosense-ai-python"
version = "0.1.0"
requires-python = ">=3.11"
```

يحدد أن المشروع يتطلب Python 3.11 أو أحدث (للاستفادة من `list[float]` كـ type hint مباشرةً).

---

### `requirements.txt`

**الغرض:** قائمة المكتبات الخارجية المطلوبة لتشغيل التطبيق.

**المكتبات مقسّمة حسب الغرض:**

| الفئة | المكتبة | الغرض |
|-------|---------|-------|
| **Web Framework** | `fastapi>=0.111.0` | إطار الـ API |
| **Web Server** | `uvicorn[standard]` | تشغيل الـ server |
| **Data Validation** | `pydantic>=2.0` | التحقق من البيانات |
| **Config** | `pydantic-settings>=2.0` | قراءة ملف `.env` |
| **GEE** | `earthengine-api>=0.1.380` | التواصل مع Google Earth Engine |
| **GEE Helper** | `geemap>=0.30.0` | أدوات مساعدة لـ GEE |
| **Terrain Analysis** | `whitebox>=2.3.0` | تحليل التضاريس (Day 2) |
| **Raster Processing** | `rasterio>=1.3.0` | قراءة/كتابة ملفات GeoTIFF |
| **Numerical** | `numpy>=1.26.0` | العمليات الرياضية على المصفوفات |
| **Vector Data** | `geopandas>=0.14.0` | معالجة البيانات الجغرافية المتجهة |
| **Geometry** | `shapely>=2.0.0` | عمليات الأشكال الهندسية |
| **Image** | `Pillow>=10.0.0` | معالجة الصور |
| **Visualization** | `matplotlib>=3.8.0` | رسم الخرائط والمخططات |
| **Cache** | `redis>=5.0.0` | تخزين حالة الـ jobs مؤقتاً (Day 2) |
| **Testing** | `pytest>=8.0.0` | إطار الاختبارات |
| **HTTP Client** | `httpx>=0.27.0` | عميل HTTP للاختبارات |

---

## مجلد `app/`

---

### `app/__init__.py`

ملف فارغ (docstring فقط) — يجعل `app` حزمة Python قابلة للاستيراد.

---

### `app/config.py`

**الغرض:** إدارة جميع إعدادات التطبيق من ملف `.env` باستخدام Pydantic Settings.

**الكلاس الرئيسي: `Settings`**

```python
class Settings(BaseSettings):
    model_config = ConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False
    )
```

**الحقول:**

| الحقل | النوع | القيمة الافتراضية | الوصف |
|-------|-------|------------------|-------|
| `gee_project` | `str` | `""` | معرّف مشروع GEE |
| `gee_service_account_email` | `str` | `""` | إيميل الـ Service Account |
| `gee_service_account_key` | `str` | `"./secrets/gee_key.json"` | مسار ملف المفتاح |
| `redis_url` | `str` | `"redis://localhost:6379"` | رابط Redis |
| `local_out_dir` | `str` | `"/tmp/geosense"` | مجلد الملفات المؤقتة |
| `egypt_bbox` | `list[float]` | `[24.0, 22.0, 37.0, 32.0]` | حدود مصر |
| `api_title` | `str` | `"GeoSense API"` | عنوان الـ API |
| `api_version` | `str` | `"0.1.0"` | إصدار الـ API |

**الدالة `validate_gee_credentials()`:**
تتحقق من توفر ثلاثة شروط:
1. `gee_project` غير فارغ
2. `gee_service_account_email` غير فارغ
3. ملف JSON المفتاح موجود فعلياً على الـ disk

**الاستخدام:**
```python
from app.config import settings
# instance واحد يُستخدم في كل مكان
print(settings.gee_project)
```

---

### `app/main.py`

**الغرض:** نقطة دخول التطبيق — يُنشئ تطبيق FastAPI ويضبط كل إعداداته.

**المكونات الرئيسية:**

#### 1. Lifespan Context Manager
```python
@asynccontextmanager
async def lifespan(app: FastAPI):
    # ← يُنفَّذ عند بدء التطبيق
    init_gee(...)      # يصادق على GEE
    gee_initialized = True/False
    redis_connected = False  # مؤجل لـ Day 2
    
    yield  # ← التطبيق يعمل هنا
    
    # ← يُنفَّذ عند إيقاف التطبيق
    logger.info("Shutting down...")
```
يضمن أن GEE يُهيَّأ مرة واحدة عند الـ startup، وليس في كل request.

#### 2. تطبيق FastAPI
```python
app = FastAPI(
    title   = settings.api_title,
    version = settings.api_version,
    lifespan = lifespan,
)
```

#### 3. CORS Middleware
```python
app.add_middleware(
    CORSMiddleware,
    allow_origins = ["*"],   # يسمح لأي domain (للتطوير)
    allow_methods = ["*"],
    allow_headers = ["*"],
)
```

#### 4. الـ Endpoints المباشرة

| الـ Endpoint | الطريقة | الوصف |
|-------------|---------|-------|
| `/` | GET | معلومات أساسية عن الـ API |
| `/api/v1/health` | GET | حالة GEE وRedis |

**مثال استجابة `/api/v1/health`:**
```json
{
  "status": "healthy",
  "gee_initialized": true,
  "redis_connected": false,
  "version": "0.1.0"
}
```

#### 5. معالجات الأخطاء (Error Handlers)
- **HTTPException handler:** يُعيد الـ `detail` مباشرة (مُنسَّق مسبقاً في الـ router)
- **Global exception handler:** يلتقط أي خطأ غير متوقع ويُعيد رسالة موحدة بـ status 500

---

## مجلد `app/routers/`

---

### `app/routers/__init__.py`

يُصدِّر `topography.router` لاستيراده في `main.py`.

---

### `app/routers/topography.py`

**الغرض:** يحتوي على جميع endpoints الخاصة بتحليل التضاريس.

**الـ Router:**
```python
router = APIRouter(prefix="/api/v1/topography", tags=["topography"])
_jobs: dict = {}  # تخزين مؤقت في الذاكرة (يُستبدل بـ Redis في Day 2)
```

---

#### Endpoint 1: إرسال طلب جديد
```
POST /api/v1/topography/jobs
```

**تدفق التنفيذ:**
1. يستقبل `TopographyRequest` (parcel_id, bbox, geo_json, options)
2. يتحقق من أن الـ bbox داخل مصر عبر `validate_bbox_egypt()`
3. إذا خارج مصر → يُعيد `400 INVALID_BBOX`
4. يُنشئ `python_job_id` (UUID فريد)
5. يحفظ الـ job في `_jobs` بحالة `"queued"`
6. يُطلق `_run_pipeline()` في الخلفية (BackgroundTasks)
7. يُعيد `202 Accepted` فوراً بدون انتظار الـ pipeline

**الاستجابة (202):**
```json
{
  "python_job_id": "550e8400-e29b-41d4-a716-446655440000",
  "parcel_id": "parcel-001",
  "status": "queued",
  "accepted_at": "2026-06-03T10:00:00+00:00"
}
```

---

#### Endpoint 2: استعلام عن حالة الـ job
```
GET /api/v1/topography/jobs/{python_job_id}
```

**تدفق التنفيذ:**
1. يبحث عن الـ `python_job_id` في `_jobs`
2. إذا غير موجود → يُعيد `404 JOB_NOT_FOUND`
3. إذا موجود → يُعيد `JobProcessing` بالحالة الحالية

**الاستجابة (200):**
```json
{
  "python_job_id": "550e8400-...",
  "parcel_id": "parcel-001",
  "status": "completed",
  "progress": 100,
  "results": {
    "task_id": "projects/ee-geosense/operations/xyz123",
    "dem_file": "dem_550e8400-....tif",
    "coordinate_system": "EPSG:32636",
    "resolution_m": 30,
    "processing_time_seconds": 2.3,
    "note": "Day 1 complete — terrain analysis coming Day 2"
  },
  "error": null
}
```

---

#### الـ Background Pipeline: `_run_pipeline()`

```python
async def _run_pipeline(python_job_id: str, req: TopographyRequest):
    upd("processing", 10)          # يُحدّث الحالة لـ 10%
    task_id = export_dem_for_parcel(...)  # يُرسل الـ DEM لـ Google Drive
    upd("completed", 100, results={...}) # يُحدّث الحالة لـ 100%
```

**حالات الـ job:**

```
queued → processing → completed
                   ↘ failed
```

| الحالة | progress | الوصف |
|--------|---------|-------|
| `queued` | 0 | تم الاستقبال، لم يبدأ التنفيذ بعد |
| `processing` | 10 | جاري تصدير الـ DEM إلى Google Drive |
| `completed` | 100 | اكتمل التصدير، task_id متاح |
| `failed` | 0 | فشل التنفيذ، error موجود |

---

## مجلد `app/schemas/`

---

### `app/schemas/topography.py`

**الغرض:** تعريف نماذج البيانات (Pydantic models) للـ request والـ response.

---

#### `ErrorDetail` و `ErrorResponse`
نموذج موحد للأخطاء:
```python
class ErrorResponse(BaseModel):
    status_code: int    # كود HTTP (400, 404, 500)
    error_code:  str    # كود مقروء: "INVALID_BBOX", "JOB_NOT_FOUND"
    message:     str    # رسالة للمستخدم
    retryable:   bool   # هل يجدر إعادة المحاولة؟
    details:     dict   # تفاصيل إضافية (مثل الـ bbox المرفوض)
```

---

#### `TopographyOptions`
خيارات اختيارية للتحليل (تُستخدم في Day 2):
```python
class TopographyOptions(BaseModel):
    contour_interval_m:  float = 0.5   # فاصل خطوط التسوية بالمتر
    reference_elevation: float = None  # ارتفاع مرجعي مخصص
    twi_threshold:       float = 8.0   # عتبة مؤشر TWI (Topographic Wetness Index)
    export_tiles:        bool  = True  # تصدير map tiles أم لا
```

---

#### `TopographyRequest`
نموذج الطلب الوارد من العميل:
```python
class TopographyRequest(BaseModel):
    parcel_id: str           # معرّف القطعة
    bbox:      list[float]   # [minLon, minLat, maxLon, maxLat]
    geo_json:  dict          # شكل GeoJSON للقطعة
    options:   TopographyOptions = TopographyOptions()  # خيارات اختيارية
```

---

#### `JobAccepted`
استجابة الـ 202:
```python
class JobAccepted(BaseModel):
    python_job_id: str
    parcel_id:     str
    status:        str = "queued"
    accepted_at:   str  # ISO 8601 timestamp
```

---

#### `JobProcessing`
استجابة الاستعلام عن الحالة:
```python
class JobProcessing(BaseModel):
    python_job_id: str
    parcel_id:     str
    status:   Literal["queued", "processing", "completed", "failed"]
    progress: int           # 0 إلى 100
    results:  Optional[dict] = None  # متاح عند completed
    error:    Optional[dict] = None  # متاح عند failed
```

---

#### `HealthResponse`
استجابة الـ health endpoint:
```python
class HealthResponse(BaseModel):
    status:          str   # "healthy" أو "degraded"
    gee_initialized: bool
    redis_connected: bool
    version:         str
```

---

## مجلد `app/services/`

---

### `app/services/gee_service.py`

**الغرض:** الخدمة الجوهرية للمشروع — تتولى كل عمليات Google Earth Engine.

---

#### الدالة `init_gee()`

```python
def init_gee(gee_project, service_account_email, service_account_key) -> None
```

**مسارا المصادقة:**

```
هل ملف JSON المفتاح موجود؟
        ↓ نعم                      ↓ لا
Service Account Auth          gcloud user auth
(للـ production/Docker)       (للتطوير المحلي فقط)
        ↓
ee.ServiceAccountCredentials(email, key_file)
ee.Initialize(creds, project=gee_project)
        ↓
اختبار التحقق: استعلام صغير على نقطة [0,0]
        ↓ نجح                    ↓ فشل
GEE جاهز ✅             RuntimeError ❌
```

**لماذا اختبار التحقق؟** الـ `ee.Initialize()` لا يُثبت نجاح الاتصال الفعلي بخوادم Google — الاستعلام الصغير هو الذي يؤكد أن بيانات الاعتماد صحيحة وصالحة.

---

#### الدالة `validate_bbox_egypt()`

```python
def validate_bbox_egypt(bbox: list[float]) -> bool
```

**المنطق:**
```python
egypt_bounds = [24.0, 22.0, 37.0, 32.0]  # [minLon, minLat, maxLon, maxLat]

is_valid = (
    egypt_bounds[0] <= min_lon  and  max_lon <= egypt_bounds[2]  and
    egypt_bounds[1] <= min_lat  and  max_lat <= egypt_bounds[3]
)
```

**أمثلة:**
```python
validate_bbox_egypt([31.2, 30.0, 31.5, 30.3])  # ✅ True  (القاهرة)
validate_bbox_egypt([31.2, 20.0, 31.5, 30.3])  # ❌ False (minLat خارج مصر)
validate_bbox_egypt([23.0, 30.0, 23.5, 30.3])  # ❌ False (غرب مصر)
validate_bbox_egypt([24.0, 22.0, 37.0, 32.0])  # ✅ True  (حدود مصر بالضبط)
```

---

#### الدالة `export_dem_for_parcel()`

```python
def export_dem_for_parcel(bbox, job_id, out_dir) -> str
```

**تدفق العملية خطوة بخطوة:**

```
1. إنشاء مستطيل من الـ bbox
         ↓
2. إضافة buffer 500m حول المستطيل
   (0.005° × 111320 متر/درجة ≈ 556 متر)
         ↓
3. جلب مجموعة صور Copernicus DEM GLO-30
   (ee.ImageCollection("COPERNICUS/DEM/GLO30"))
         ↓
4. تصفية الصور حسب الـ geometry
   .filterBounds(geometry_buffered)
         ↓
5. اختيار حزمة "DEM"
   .select("DEM")
         ↓
6. دمج الصور (في حالة وجود overlap)
   .mosaic()
         ↓
7. قطع الصورة على حدود الـ geometry
   .clip(geometry_buffered)
         ↓
8. بدء مهمة التصدير إلى Google Drive
   ee.batch.Export.image.toDrive(
       scale=30,           ← دقة 30 متر
       crs="EPSG:32636",   ← UTM Zone 36N (مناسب لمصر)
       maxPixels=1e9       ← حد أقصى للبكسلات
   )
         ↓
9. إعادة task_id
   "projects/ee-geosense/operations/XYZ..."
```

**لماذا EPSG:32636؟** هو نظام UTM Zone 36N المناسب لمنطقة مصر، يُعطي قياسات بالمتر (مفيد لحسابات المساحة والمسافة) بدلاً من الدرجات.

**لماذا buffer 500m؟** التضاريس المحيطة بالقطعة تؤثر على تدفق المياه والظلال — بدونه قد يفتقد التحليل سياقاً مهماً.

---

### الخدمات المؤجلة (Day 2+)

| الملف | الوضع | المحتوى المخطط |
|-------|-------|----------------|
| `redis_service.py` | فارغ | حفظ/استرجاع حالة الـ jobs في Redis |
| `terrain_service.py` | فارغ | حساب Slope, Aspect, TWI |
| `tiles_service.py` | فارغ | تحويل الـ GeoTIFF إلى map tiles |
| `topography_service.py` | فارغ | تنسيق الخدمات المختلفة معاً |

---

## مجلد `tests/`

---

### `tests/test_gee.py`

**الغرض:** اختبار يدوي سريع للتحقق من اتصال GEE — يُشغَّل مباشرةً، وليس عبر pytest.

```python
SERVICE_ACCOUNT = "planora@planora-497717.iam.gserviceaccount.com"
KEY_PATH = "secrets/gee_key.json"

credentials = ee.ServiceAccountCredentials(SERVICE_ACCOUNT, KEY_PATH)
ee.Initialize(credentials)
print(ee.String("GEE connected ✅").getInfo())
```

**الاستخدام:** `python tests/test_gee.py` للتحقق السريع أن ملف المفتاح والـ Service Account يعملان.

---

### `tests/test_api.py`

**الغرض:** اختبارات شاملة لجميع endpoints الـ API باستخدام FastAPI TestClient.

**الـ Fixtures:**
```python
@pytest.fixture
def client():
    return TestClient(app)  # client HTTP داخلي بدون رفع server حقيقي
```

**بيانات الاختبار:**
```python
VALID_PAYLOAD   = {"bbox": [31.2, 30.0, 31.5, 30.3], ...}  # ✅ داخل مصر (القاهرة)
INVALID_PAYLOAD = {"bbox": [10.0, 10.0, 15.0, 15.0], ...}  # ❌ خارج مصر
MOCK_TASK_ID    = "projects/ee-geosense-prod/operations/ABC123"
```

**مجموعات الاختبار:**

#### `TestApplicationStartup`
- يتحقق أن `app.title == "GeoSense API"`
- يتحقق وجود routes: `/`, `/api/v1/health`, `/api/v1/topography/jobs`

#### `TestHealthCheckEndpoint`
- يتحقق أن `/api/v1/health` يُعيد الحقول الصحيحة
- يختبر أن الحالة `"healthy"` حين `gee_initialized=True`

#### `TestRootEndpoint`
- يتحقق أن `/` يُعيد `title`, `version`, `docs`, `health`

#### `TestTopographyJobCreation`
- **valid bbox** → يتوقع `202` مع `python_job_id`
- **invalid bbox** → يتوقع `400` مع `error_code: "INVALID_BBOX"`
- **response schema** → يُحوَّل الـ JSON إلى `JobAccepted` بدون خطأ
- **missing parcel_id** → يتوقع `422 Unprocessable Entity`

#### `TestTopographyJobStatus`
- **job موجود** → يتوقع `200` مع حالة صحيحة
- **job غير موجود** → يتوقع `404` مع `error_code: "JOB_NOT_FOUND"`
- **response schema** → يُحوَّل الـ JSON إلى `JobProcessing` بدون خطأ

#### `TestAPIDocumentation`
- يتحقق أن `/docs` (Swagger UI) يعمل
- يتحقق أن `/openapi.json` يحتوي على الـ paths الصحيحة

**تقنية المحاكاة (Mocking):**
```python
@patch("app.routers.topography.validate_bbox_egypt")
@patch("app.routers.topography.export_dem_for_parcel")
def test_create_job_valid_bbox(self, mock_export, mock_validate, client):
    mock_validate.return_value = True       # نتجاهل التحقق الحقيقي
    mock_export.return_value   = MOCK_TASK_ID  # نتجاهل الـ GEE الحقيقي
```
يمنع الاتصال الفعلي بـ GEE أثناء الاختبارات.

---

### `tests/test_gee_drive_export.py`

**الغرض:** اختبارات تفصيلية لـ `gee_service.py` — تتحقق من كل جانب من جوانب التصدير.

**مجموعات الاختبار:**

#### `TestGEEAuthentication`
- **`test_init_gee_with_service_account`:** يتحقق أن `ee.ServiceAccountCredentials` يُستدعى بالمعاملات الصحيحة حين ملف المفتاح موجود
- **`test_init_gee_without_key_uses_fallback`:** يتحقق أن `ee.Initialize(project=...)` يُستدعى مباشرةً حين الملف مفقود

#### `TestBboxValidation`
يختبر 6 حالات:
```python
[31.2, 30.0, 31.5, 30.3]   # ✅ داخل مصر
[31.2, 33.0, 31.5, 33.3]   # ❌ شمال مصر
[31.2, 20.0, 31.5, 21.0]   # ❌ جنوب مصر
[23.0, 30.0, 23.5, 30.3]   # ❌ غرب مصر
[31.2, 30.0, 38.0, 30.3]   # ❌ شرق مصر
[24.0, 22.0, 37.0, 32.0]   # ✅ حدود مصر بالضبط
```

#### `TestDEMExportWorkflow`
- **`test_export_dem_returns_task_id_not_file_path`:** يتحقق أن النتيجة string تبدأ بـ `"projects/"` وتحتوي على `"operations/"`
- **`test_export_dem_task_parameters`:** يتحقق أن الـ export يُستدعى بـ `scale=30`, `crs="EPSG:32636"`, `maxPixels=1e9`
- **`test_export_dem_geometry_buffering`:** يتحقق أن `geometry.buffer()` يُستدعى مرة واحدة

#### `TestIntegrationWithGoogleDrive`
- **`test_multiple_exports_different_task_ids`:** يُنفذ 3 exports ويتحقق أن كل واحد يُعيد task_id مختلفاً

#### `TestErrorHandling`
- **`test_export_dem_handles_gee_exception`:** يتحقق أن `ee.EEException` تُحوَّل إلى `RuntimeError("GEE export failed")`

#### `TestDriveExportWorkflowEndToEnd`
- محاكاة كاملة من الـ export حتى بناء استجابة الـ API — يتحقق من كل حقل في الاستجابة

---

## تدفق البيانات الكامل

```
العميل (Frontend/Postman)
        ↓ POST /api/v1/topography/jobs
        {parcel_id, bbox, geo_json}
        
app/routers/topography.py
        ↓ validate_bbox_egypt(bbox)
        ↓ إنشاء python_job_id
        ↓ حفظ في _jobs[python_job_id] = {status: "queued"}
        ↓ إطلاق _run_pipeline() في الخلفية
        ↑ إعادة 202 {python_job_id, status: "queued"} فوراً

[في الخلفية]
app/services/gee_service.py
        ↓ export_dem_for_parcel(bbox, job_id, out_dir)
        ↓ إنشاء geometry + buffer 500m
        ↓ جلب Copernicus DEM من GEE
        ↓ بدء مهمة تصدير إلى Google Drive
        ↑ إعادة task_id

app/routers/topography.py
        ↓ تحديث _jobs[python_job_id] = {status: "completed", progress: 100}

العميل
        ↓ GET /api/v1/topography/jobs/{python_job_id}
        ↑ {status: "completed", results: {task_id, dem_file, ...}}
```

---

## الحالة الحالية (Day 1)

| المكون | الحالة | الملاحظة |
|--------|--------|----------|
| GEE Authentication | ✅ مكتمل | Service Account + fallback |
| Bbox Validation | ✅ مكتمل | حدود مصر [24-37°E, 22-32°N] |
| DEM Export to Drive | ✅ مكتمل | Copernicus GLO-30, 30m, EPSG:32636 |
| Job Queue (in-memory) | ✅ مكتمل | dict بسيط في الذاكرة |
| API Endpoints | ✅ مكتمل | POST + GET /jobs |
| Redis Integration | ⏳ مؤجل | Day 2 |
| Terrain Analysis | ⏳ مؤجل | Day 2 (Slope, TWI...) |
| Map Tiles Export | ⏳ مؤجل | Day 2+ |
| Docker (كامل) | ⏳ مؤجل | Placeholder حالياً |

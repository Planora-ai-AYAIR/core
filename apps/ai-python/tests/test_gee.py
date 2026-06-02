import ee

SERVICE_ACCOUNT = "planora@planora-497717.iam.gserviceaccount.com"
KEY_PATH = "secrets/gee_key.json"

credentials = ee.ServiceAccountCredentials(
    SERVICE_ACCOUNT,
    KEY_PATH
)

ee.Initialize(credentials)

print(ee.String("GEE connected ✅").getInfo())
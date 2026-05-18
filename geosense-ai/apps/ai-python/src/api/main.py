from fastapi import FastAPI

app = FastAPI(title="GeoSense AI API Placeholder")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}

from fastapi import FastAPI

app = FastAPI(title="Planora AI API Placeholder")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}

"""
GeoSense AI — local entry point.

Run from the ``apps/ai-python`` directory:

    py -3.13 run.py

Then send a POST request (Postman / curl) to:

    http://localhost:8000/api/v1/analyze

with a JSON body like:

    { "points": [[31.4976, 29.7811], [31.5033, 29.7780], [31.4987, 29.7745]] }

Useful URLs once running:
    UI       http://localhost:8000/ui
    Swagger  http://localhost:8000/docs
    Health   http://localhost:8000/api/v1/health
"""

import uvicorn

if __name__ == "__main__":
    uvicorn.run(
        "app.main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        log_level="info",
    )
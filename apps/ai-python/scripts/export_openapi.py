"""Export the live FastAPI schema to the shared OpenAPI contract file.

Generates ``shared/openapi/swagger.json`` (repo-root) from the running app so
the published contract always matches what ``app.main:app`` actually serves —
covering both §2 (client-facing) and §3 (internal AI engine) endpoints.

Run from anywhere:

    py -3.13 scripts/export_openapi.py
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

# Make `app` importable when run as a standalone script.
APP_DIR = Path(__file__).resolve().parents[1]          # apps/ai-python
REPO_ROOT = Path(__file__).resolve().parents[3]        # repo root (…/core/core)
sys.path.insert(0, str(APP_DIR))

from app.main import app  # noqa: E402

OUT_PATH = REPO_ROOT / "shared" / "openapi" / "swagger.json"


def main() -> None:
    spec = app.openapi()
    # This file is the cross-team shared contract, not just one service's title.
    spec.setdefault("info", {})
    spec["info"]["title"] = "GeoSense Shared Contract"

    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUT_PATH.write_text(json.dumps(spec, indent=2, ensure_ascii=False), encoding="utf-8")

    print(f"Wrote {OUT_PATH} ({len(spec.get('paths', {}))} paths)")


if __name__ == "__main__":
    main()

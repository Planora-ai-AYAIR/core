"""Client-facing API routers — API Contract §2.

Implemented in the Python AI engine as coherent mocks so the full §2 surface
(parcels, per-module jobs, status polling, PDF download) is browsable without
the .NET backend. Prefix is ``/api/...`` (no ``/v1``) — distinct from the
internal AI-engine endpoints in §3 (``/api/v1/...``).
"""

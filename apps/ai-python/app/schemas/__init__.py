"""
GeoSense API schemas package.

Contains all Pydantic request/response models:
- common: Unified response envelope and shared types
- topography: Topography analysis models (§3.1)
- soil: Soil composition models (§3.2)
- risks: Risk assessment models (§3.3)
- boreholes: Borehole optimization models (§3.4)
- reports: PDF report models (§3.5)
"""

from . import common
from . import topography
from . import soil
from . import risks
from . import boreholes
from . import reports

__all__ = [
    "common",
    "topography",
    "soil",
    "risks",
    "boreholes",
    "reports",
]

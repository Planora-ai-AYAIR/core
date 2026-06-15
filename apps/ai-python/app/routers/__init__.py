"""
GeoSense API routers package.

Contains all API endpoint routers:
- topography: Topography analysis endpoints (§3.1)
- soil: Soil composition analysis endpoints (§3.2)
- risks: Risk assessment endpoints (§3.3)
- boreholes: Borehole optimization endpoints (§3.4)
- reports: PDF report generation endpoints (§3.5)
- analyze: Debug/demo synchronous analysis endpoint
"""

from . import topography
from . import analyze
from . import soil
from . import risks
from . import boreholes
from . import reports

__all__ = [
    "topography",
    "analyze",
    "soil",
    "risks",
    "boreholes",
    "reports",
]

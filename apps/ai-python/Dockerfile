# Use the official slim Python 3.11 image as the base.
# "slim" removes unnecessary OS packages, keeping the image smaller.
FROM python:3.11-slim
WORKDIR /app

# Copy the project metadata file first, before copying source code.
# Layer-caching trick: pip install only re-runs when pyproject.toml changes,
# not every time a .py file changes.
COPY pyproject.toml .

# Install the project and its dependencies from pyproject.toml.
# --no-cache-dir prevents pip from storing a local wheel cache inside the image.
RUN pip install --no-cache-dir .

# Copy the actual source code AFTER install so the dependency layer is
# cached and reused on source-only changes.
COPY src/ ./src/

# Tell Python where to find the app modules.
ENV PYTHONPATH=/app/src

# The AI service listens on port 8000 (FastAPI / uvicorn default).
EXPOSE 8000

# Start the FastAPI app with uvicorn.
# --host 0.0.0.0 makes it reachable from outside the container.
CMD ["python", "-m", "uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]

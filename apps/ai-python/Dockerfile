FROM python:3.11-slim
WORKDIR /app

COPY pyproject.toml .

RUN pip install --no-cache-dir .

COPY src/ ./src/

ENV PYTHONPATH=/app/src

EXPOSE 8000

# Start the FastAPI app with uvicorn.
# --host 0.0.0.0 makes it reachable from outside the container.
CMD ["python", "-m", "uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]


# Base stage: official Python runtime image, pinned by digest
FROM python:3.11-slim@sha256:0a236bf8f0b6013383df04d1cd02cb0abe5fbfa1a7012141d7ff9202b7cb30a6 AS base

# Install curl for healthchecks and the fixed Brotli security package
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl libbrotli1 && \
    rm -rf /var/lib/apt/lists/*

# Set the application directory
WORKDIR /usr/local/app

# Install Python dependencies
COPY requirements.txt ./requirements.txt
RUN pip install --no-cache-dir --upgrade pip setuptools wheel && \
    pip install --no-cache-dir -r requirements.txt && \
    pip install --no-cache-dir --upgrade "jaraco.context>=6.1.0" "wheel>=0.46.2" && \
    find /usr/local/lib -path '*/pip/_vendor/bom.cdx.json' -delete

# Development stage
FROM base AS dev
RUN pip install watchdog
ENV FLASK_ENV=development
CMD ["python", "app.py"]

# Production stage
FROM base AS final

# Create a non-root user
RUN useradd --create-home --shell /usr/sbin/nologin appuser

# Copy application files with the correct ownership
COPY --chown=appuser:appuser . .

# Run as a non-root user
USER appuser

# Expose application port
EXPOSE 80

# Start the application
CMD ["gunicorn", "app:app", "-b", "0.0.0.0:80", "--log-file", "-", "--access-logfile", "-", "--workers", "4", "--keep-alive", "0"]

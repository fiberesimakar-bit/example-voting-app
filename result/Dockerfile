FROM node:24-slim@sha256:55fb5f5a527ea2c5c4a95f963e48ef90aa38c7b346054fdc8ee9957a2b55d205

# Update OS packages and install runtime tools
RUN apt-get update && \
    apt-get upgrade -y --no-install-recommends && \
    apt-get install -y --no-install-recommends curl tini libbrotli1 && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /usr/local/app

# Copy package manifests
COPY package*.json ./

# Install application dependencies and remove npm from the runtime image
RUN npm ci && \
    npm cache clean --force && \
   mv /usr/local/app/node_modules /node_modules && \
    rm -rf /usr/local/lib/node_modules/npm \
           /usr/local/bin/npm \
           /usr/local/bin/npx

# Copy application code
COPY . .

# Create a dedicated non-root user and set ownership
RUN useradd --create-home --shell /usr/sbin/nologin appuser && \
    chown -R appuser:appuser /usr/local/app /node_modules

# Run as a non-root user
USER appuser

ENV PORT=80
EXPOSE 80

# Run the application under tini
ENTRYPOINT ["/usr/bin/tini", "--"]
CMD ["node", "server.js"]
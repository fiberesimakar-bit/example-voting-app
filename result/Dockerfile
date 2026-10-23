FROM node:24-slim@sha256:55fb5f5a527ea2c5c4a95f963e48ef90aa38c7b346054fdc8ee9957a2b55d205

# add curl for healthcheck
RUN apt-get update && \
    apt-get upgrade -y && \
    apt-get install -y --no-install-recommends curl tini && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /usr/local/app


COPY package*.json ./

RUN npm install -g npm@11.21.1
RUN npm ci && npm cache clean --force && mv /usr/local/app/node_modules /node_modules


COPY . .

# Create a dedicated non-root user
RUN useradd --create-home --shell /usr/sbin/nologin appuser

# Give the user ownership of the application directory
RUN chown -R appuser:appuser /usr/local/app /node_modules

# Run the application as the non-root user
USER appuser

ENV PORT=80
EXPOSE 80

ENTRYPOINT ["/usr/bin/tini", "--"]
CMD ["node", "server.js"]

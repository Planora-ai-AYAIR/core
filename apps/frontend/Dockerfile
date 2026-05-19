# ── Stage 1: Build ──────────────────────────────────────────────────────────
# Use the official Node 20 Alpine image to build the Angular app.
# Alpine is a minimal Linux distro — much smaller than the default Debian node image.
FROM node:20-alpine AS build
WORKDIR /app

# Copy package manifests first (before source code).
# Docker caches this layer; npm ci only re-runs if package files change.
COPY package.json package-lock.json* ./

# Install exact versions from the lockfile.
# npm ci is stricter and faster than npm install for CI/CD scenarios.
RUN npm ci

# Copy the rest of the Angular source code.
COPY . .

# Build the Angular app in production mode.
# Output goes to dist/ — exact subfolder depends on angular.json project name.
RUN npm run build

# ── Stage 2: Serve ───────────────────────────────────────────────────────────
# Use a minimal nginx image to serve the compiled static files.
# No Node.js runtime in production — just a lean static file server.
FROM nginx:alpine AS runtime

# Copy the Angular build output into nginx's default static files directory.
# Adjust the dist subfolder name if the angular.json project name differs.
COPY --from=build /app/dist/ /usr/share/nginx/html

# nginx listens on port 80 by default.
EXPOSE 80

# Start nginx in the foreground (daemon off) so Docker can manage the process.
CMD ["nginx", "-g", "daemon off;"]

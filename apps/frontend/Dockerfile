# Stage 1 — Build
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./

RUN npm ci

COPY . .

RUN npm run build

# Stage 2 — Serve
# Use a minimal nginx image to serve the compiled static files.
# No Node.js runtime in production — just a lean static file server.
FROM nginx:1.27-alpine AS runtime

COPY --from=build /app/dist/planora-portal/browser/ /usr/share/nginx/html/
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

# Start nginx in the foreground (daemon off) so Docker can manage the process.
CMD ["nginx", "-g", "daemon off;"]

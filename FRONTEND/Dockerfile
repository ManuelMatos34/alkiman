# ── Etapa 1: build del frontend ───────────────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci --prefer-offline

COPY . .

# Las variables VITE_* se hornean en el bundle en tiempo de build, no en runtime.
# Se pasan como build-args desde GitHub Actions (--build-arg VITE_API_BASE_URL=...).
ARG VITE_API_BASE_URL
ARG VITE_APP_ENV=production
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_APP_ENV=$VITE_APP_ENV

RUN npm run build

# ── Etapa 2: Nginx sirve el build estático ────────────────────────────────────
FROM nginx:alpine AS runtime

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

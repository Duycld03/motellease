# Build Stage
FROM node:22-alpine AS build
WORKDIR /app

# Copy package manifests for efficient layer caching
COPY package.json package-lock.json* ./
RUN npm install

# Copy source code and build Nuxt app
COPY . .
ARG NUXT_PUBLIC_API_BASE=http://localhost:5004/api/v1
ARG NUXT_PUBLIC_HUB_URL=http://localhost:5004/hubs/notifications
ARG NUXT_PUBLIC_GOOGLE_CLIENT_ID=""
ENV NUXT_PUBLIC_API_BASE=$NUXT_PUBLIC_API_BASE
ENV NUXT_PUBLIC_HUB_URL=$NUXT_PUBLIC_HUB_URL
ENV NUXT_PUBLIC_GOOGLE_CLIENT_ID=$NUXT_PUBLIC_GOOGLE_CLIENT_ID

RUN npm run build

# Runtime Stage
FROM node:22-alpine AS runtime
WORKDIR /app

ENV NODE_ENV=production
ENV HOST=0.0.0.0
ENV PORT=3000

COPY --from=build /app/.output ./

EXPOSE 3000

CMD ["node", "server/index.mjs"]

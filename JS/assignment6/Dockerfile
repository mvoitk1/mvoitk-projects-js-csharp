# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS build
WORKDIR /app
RUN apk add --no-cache python3 make g++
COPY package.json package-lock.json ./
RUN npm ci
COPY tsconfig.json ./
COPY src ./src
RUN npm run build

FROM node:22-alpine AS deps
WORKDIR /app
RUN apk add --no-cache python3 make g++
COPY package.json package-lock.json ./
RUN npm ci --omit=dev

FROM node:22-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
ENV DB_PATH=/app/data/app.db
ENV JWT_SECRET_PATH=/app/data/.jwt_secret

RUN addgroup -S app && adduser -S app -G app && mkdir -p /app/data && chown -R app:app /app

COPY --from=deps  --chown=app:app /app/node_modules ./node_modules
COPY --from=build --chown=app:app /app/dist         ./dist
COPY --chown=app:app package.json ./
COPY --chown=app:app templates    ./templates

USER app
EXPOSE 3000
CMD ["node", "dist/index.js"]

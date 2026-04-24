FROM node:20-alpine AS build
WORKDIR /app
COPY frontend/backoffice/package*.json ./
RUN npm ci
COPY frontend/backoffice .
RUN npm run build

FROM node:20-alpine
WORKDIR /app
COPY --from=build /app/dist ./dist
COPY --from=build /app/server.mjs ./server.mjs

ENV HOST=0.0.0.0
ENV PORT=3001
ENV BACKOFFICE_API_ENTRYPOINT=http://localhost:18080
ENV BACKOFFICE_OPENAPI_URL=http://localhost:18080/openapi/backoffice.json
ENV BACKOFFICE_KEYCLOAK_AUTHORITY=http://localhost:18081/realms/demo-ecommerce
ENV BACKOFFICE_KEYCLOAK_CLIENT_ID=backoffice-web
EXPOSE 3001

CMD ["node", "server.mjs"]

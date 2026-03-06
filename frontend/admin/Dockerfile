FROM node:20-alpine AS build
WORKDIR /app
COPY frontend/admin/package.json ./
RUN npm install
COPY frontend/admin .
RUN npm run build

FROM node:20-alpine
WORKDIR /app
COPY --from=build /app/dist ./dist
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/package.json ./package.json
ENV HOST=0.0.0.0
ENV PORT=3001
EXPOSE 3001
CMD ["npm", "run", "start"]

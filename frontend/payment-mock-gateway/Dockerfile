FROM node:20-alpine
WORKDIR /app

COPY frontend/payment-mock-gateway/package.json ./
RUN npm install --omit=dev

COPY frontend/payment-mock-gateway ./

ENV HOST=0.0.0.0
ENV PORT=8080
EXPOSE 8080

CMD ["npm", "run", "start"]

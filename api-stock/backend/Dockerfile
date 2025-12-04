# Etapa de construcción
FROM node:20-alpine AS builder
WORKDIR /usr/src/app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# Etapa de producción
FROM node:20-alpine
WORKDIR /usr/src/app
COPY package*.json ./
RUN npm install --omit=dev
COPY --from=builder /usr/src/app/dist ./dist
EXPOSE 3000
CMD ["npm", "run", "start:prod"]

COPY entrypoint.sh .
RUN sed -i 's/\r$//' entrypoint.sh
RUN chmod +x entrypoint.sh

EXPOSE 3000

# Define el entrypoint
ENTRYPOINT ["./entrypoint.sh"]
# El CMD se pasa como argumento ('$@') al entrypoint
CMD ["npm", "run", "start:prod"]

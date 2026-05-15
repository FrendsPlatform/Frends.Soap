FROM node:20-alpine

WORKDIR /app

COPY server.js .

RUN apk add --no-cache openssl && \
    openssl genrsa -out server-key.pem 2048 && \
    openssl req -new -x509 -key server-key.pem -out server-cert.pem -days 365 \
    -subj "/C=US/ST=State/L=City/O=Org/CN=localhost"

EXPOSE 8080 8443

CMD ["node", "server.js"]

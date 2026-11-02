# Front (implantação, Entrega F): npm ci + npm run build (vue-tsc e vite) no Node e o
# estático servido pelo Caddy, que também faz o proxy de /api (deploy/Caddyfile, montado
# pelo docker-compose.prod.yml). Sem VITE_API_BASE_URL no build, clienteHttp.ts fica sem
# baseURL e as chamadas a /api vão para a mesma origem do site.
FROM node:22-bookworm-slim AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . ./
RUN npm run build

FROM caddy:2
COPY --from=build /src/dist /srv

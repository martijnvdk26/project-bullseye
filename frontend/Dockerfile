FROM node:20-alpine AS build
WORKDIR /src

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .
RUN npx ng build --configuration production

FROM nginx:alpine AS runtime
COPY --from=build /src/dist/frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

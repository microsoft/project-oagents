FROM node:18-alpine

ARG VITE_OAGENT_BASE_URL
ENV VITE_OAGENT_BASE_URL=$VITE_OAGENT_BASE_URL

RUN echo "VITE_OAGENT_BASE_URL:" $VITE_OAGENT_BASE_URL


WORKDIR /app

COPY package.json package-lock.json* .npmrc* ./

RUN npm install

RUN npm i -g serve

COPY . .

RUN npm run azure

EXPOSE 3000

CMD [ "serve", "-s", "dist" ]
FROM node:18-alpine

WORKDIR /app

COPY package.json package-lock.json* .npmrc* ./

RUN npm install

RUN npm i -g serve

COPY . .

RUN npm run azure

EXPOSE 3000

CMD [ "serve", "-s", "dist" ]
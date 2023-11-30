# 参考：https://maku.blog/p/ehxgwt4/
# ビルドステージ
FROM node:18.16.0-alpine as builder
    WORKDIR /work
    # ビルド用の依存パッケージをインストール
    COPY package*.json ./
    RUN npm install
    # Typescriptコードをコピーしてビルド
    COPY src tsconfig.json ./
    RUN npm run build

# 実行用イメージ
FROM node:18.16.0-alpine as runner
    WORKDIR /work
    ENV NODE_ENV production
    ENV APP_PORT 3030 \
        API_PORT 3031
    EXPOSE 3030 3031
    # 本番環境のパッケージをインストール
    COPY package*.json ./
    RUN npm install --omit=dev && npm cache clean --force
    # builderからビルド結果をコピー
    COPY --from=builder /work/dist ./dist
    # COPY src/videos.json ./
    # 実行
    CMD ["node", "./dist/index.js"]
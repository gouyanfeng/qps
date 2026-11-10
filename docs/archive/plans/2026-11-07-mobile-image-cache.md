# 移动端图片缓存 实施计划（图片组件封装）

## 一、目标与范围

只做图片缓存，封装一个统一图片组件，组件内部区分 H5 与非 H5 两条路径：

- **H5 路径**：不做自定义下载缓存，直接使用 `resolveFileUrl` 后的网络地址，由浏览器按 HTTP 缓存策略缓存；后端为 `/uploads` 图片补长缓存响应头。
- **非 H5 路径（App / 小程序）**：首次下载后 `uni.saveFile` 持久化到本地，后续直接用本地文件路径展示；失败回退网络地址。
- 页面只需把 `<image>` 换成新组件，业务逻辑、预览、上传均不改。
- 不做接口数据缓存、不做离线业务数据、不改请求拦截器、不改现有 `utils/http.ts`。

## 二、现状分析

- 移动端组件目录使用 `components/<name>/<name>.vue` 结构，页面直接以 `q-xxx` 使用（easycom 自动注册），可参照 [q-page-header-back.vue](file:///e:/Code/QPS/mobile/src/components/q-page-header-back/q-page-header-back.vue)。
- 图片全部通过 `resolveFileUrl`（[http.ts](file:///e:/Code/QPS/mobile/src/utils/http.ts#L115-L120)）拼成绝对地址，目前页面直接用 `<image :src>`。
- 后端静态文件由 [Program.cs](file:///e:/Code/QPS/web-api/src/QPS.Host/Program.cs#L110-L113) 的 `UseStaticFiles` 提供，未设置 `Cache-Control`，仅默认 `ETag` / `Last-Modified`。
- 上传文件名由 [FileUploadController.cs](file:///e:/Code/QPS/web-api/src/QPS.Host/Controllers/FileUploadController.cs#L74-L87) 生成 GUID，同一 URL 内容不变，适合长缓存。
- 需要替换的页面（已确认）：
  - [index/index.vue](file:///e:/Code/QPS/mobile/src/pages/index/index.vue#L27)：banner、分类 icon、商品图
  - [category/index.vue](file:///e:/Code/QPS/mobile/src/pages/category/index.vue#L67)
  - [search/index.vue](file:///e:/Code/QPS/mobile/src/pages/search/index.vue#L80)
  - [product-detail/index.vue](file:///e:/Code/QPS/mobile/src/pages/product-detail/index.vue#L18)：轮播图、[评价图片](file:///e:/Code/QPS/mobile/src/pages/product-detail/index.vue#L182-L188)
  - [product-reviews/index.vue](file:///e:/Code/QPS/mobile/src/pages/product-reviews/index.vue#L60-L66)
  - [orders/index.vue](file:///e:/Code/QPS/mobile/src/pages/orders/index.vue#L76-L80)
  - [order-detail/index.vue](file:///e:/Code/QPS/mobile/src/pages/order-detail/index.vue#L122)
  - [cart/index.vue](file:///e:/Code/QPS/mobile/src/pages/cart/index.vue#L71)：商品图、[推荐商品图](file:///e:/Code/QPS/mobile/src/pages/cart/index.vue#L140)
  - [checkout/index.vue](file:///e:/Code/QPS/mobile/src/pages/checkout/index.vue#L62)
  - [activity-detail/index.vue](file:///e:/Code/QPS/mobile/src/pages/activity-detail/index.vue#L6)
  - [review-create/index.vue](file:///e:/Code/QPS/mobile/src/pages/review-create/index.vue#L13)：商品图；用户本机选图原样返回

## 三、改动清单

### 3.1 后端：`/uploads` 图片长缓存头

修改 [Program.cs](file:///e:/Code/QPS/web-api/src/QPS.Host/Program.cs#L110-L113) 的 `StaticFileOptions`，增加 `OnPrepareResponse`：

- 路径以 `/uploads` 开头时设置 `Cache-Control: public, max-age=31536000, immutable`；
- 其他静态资源不设置；
- 保留默认 `ETag` / `Last-Modified`；
- 不改中间件顺序、路由、CORS。

### 3.2 新增组件 `mobile/src/components/q-cached-image/q-cached-image.vue`

**Props**

```ts
withDefaults(defineProps<{
  src?: string | null
  mode?: string
}>(), {
  src: '',
  mode: 'aspectFill'
})
```

**模板**：内部只渲染一个 `<image>`，`inheritAttrs: false` + `v-bind="$attrs"`，让外部传入的 `class`、`style`、`@click` 等原样落到图片上；`:mode="mode"`；`:src="displaySrc"`；`@error="handleError"`。

**H5 路径**（`// #ifdef H5`）：`displaySrc` 为 `resolveFileUrl(src)`，不做下载与存储。

**非 H5 路径**（`// #ifndef H5`）：

- 内存 `reactive` 映射 `url → 本地路径`，storage 键 `qps_image_cache_map`；缓存版本键 `qps_image_cache_version`。
- `watch src`：本地/静态路径原样展示；http(s) 地址命中映射直接显示本地路径，未命中先显示网络地址并在后台执行 `uni.downloadFile` → `uni.saveFile`，成功后更新显示路径并写入映射。
- 同一 URL 用 `pending` Map 去重。
- 上限 200 条，超限按写入顺序删除最旧文件（`uni.removeSavedFile`）并清理映射。
- 缓存版本变化时清空映射与文件。
- `@error`：当前是本地缓存路径时，删除该映射并回退到网络地址，避免坏文件导致图片永久不显示。

**缓存策略**：无 TTL，缓存长期有效；图片地址变化（管理端重传会生成新 GUID 文件名）自然重新下载。

### 3.3 页面替换

把上述页面中的远程图片标签从 `<image ...>` 换成 `<q-cached-image ...>`，保留原有 `:src`、`mode`、`class`、`@click`、`v-if`/`v-for` 写法。

- 商品详情轮播：`<q-cached-image :src="img" mode="aspectFill" class="swiper-img" />`。
- 评价图片：`<q-cached-image :src="img" ... @click="previewReviewImages(review.images, imgIdx)" />`（`img` 为原始路径，组件内部统一 `resolveFileUrl`，无需页面再包）。
- 订单/购物车/结算等已有 `resolveFileUrl` 的写法可继续保留（组件对已绝对地址原样处理），避免大范围改动。
- `uni.previewImage` 预览逻辑保持使用原 URL，不改。
- 不修改页面 script 中的图片加载、上传、评价等业务逻辑。

### 3.4 不改动项

- 不修改 `src/utils/http.ts`、`src/api/index.ts`、`src/api/types.ts`、`src/layout`、`src/App.vue`、`.env`。
- 不修改现有 `q-*` 组件。
- 不新增缓存过期时间、不做后台定时刷新、不加缓存管理界面。
- 不改后端图片接口与上传接口，只加静态响应头。

## 四、假设与决策

- H5 缓存完全交给浏览器，长缓存头 + GUID 文件名保证不会出现「旧图不更新」。
- 非 H5 用文件缓存而非 base64，避免 storage 膨胀；小程序本地文件上限约 10MB，用 200 条上限 + 淘汰控制。
- 组件承载全部平台差异，页面无感知；H5 上组件行为与现有 `<image>` 一致。
- App 升级通过版本号清空旧的本地缓存文件。
- 缓存不做手动清理界面；如需强制刷新，后续可再加清缓存入口。

## 五、验证步骤

1. 后端构建：`dotnet build src/QPS.Host/QPS.Host.csproj -c Release --no-restore` 0 错误，重启 5000。
2. 响应头：`curl -I http://localhost:5000/uploads/...` 返回 200 且含 `Cache-Control: public, max-age=31536000, immutable`；`/static/...` 不含该头。
3. H5（5174）：
   - 首页 banner/分类 icon/商品图、分类、搜索、商品详情轮播、评价图片、订单列表/详情、购物车、结算、活动详情、发表评价商品图均正常显示。
   - 关闭浏览器 Disable cache 后刷新页面，图片在 Network 显示 `(disk cache)` / `(memory cache)`。
   - 图片点击预览、轮播切换正常。
4. 移动端类型检查：`npm run type-check` 0 错误。
5. 非 H5 代码验证：
   - 确认 H5 构建产物中不包含 `downloadFile` / `saveFile` 调用；
   - 确认组件中非 H5 分支逻辑完整（命中映射、下载保存、失败回退、超限淘汰、版本清理）；
   - 若具备 App 构建环境，执行一次对应平台构建验证条件编译无语法错误。
6. 回归：评价图片上传与预览、订单/购物车操作无新增报错；控制台忽略既有 favicon 404。

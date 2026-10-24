# QPS 移动端开发指南

本文件约束 `E:\Code\QPS\mobile` 下的 uni-app 移动端项目。工作区通用规则见上级 `AGENTS.md`，本文件只补充移动端的技术栈、运行方式与踩坑经验。

## 技术栈

- uni-app（Vue3 + TypeScript + Vite），同时编译 H5 与 App（HBuilderX「运行到手机」）
- UI 组件库：`tdesign-mobile-vue`（仅用其基础组件，图标不用它的 `t-icon`）
- 样式：SCSS + rpx（750 设计基准），全局皮肤类与变量见 `src/App.vue`、`src/uni.scss`
- 数据：本地 mock，无真实后端
- 类型检查：`typescript@5.3.3` + `vue-tsc@1.8.27`；`tsconfig.json` **不再继承** `@vue/tsconfig`，基础选项直接内联，并启用 `verbatimModuleSyntax`

## 目录与职责

| 路径 | 说明 |
|---|---|
| `src/pages/` | 页面：`index`（首页）、`category`（分类）、`cart`（购物车）、`profile`（我的）、`product-detail`（商品详情）、`checkout`（确认订单）、`search`（搜索） |
| `src/components/` | 通用组件：`q-icon`（图标）、`q-page-header`（普通顶栏外壳）、`q-page-header-back`（带返回键顶栏），easycom 自动注册 |
| `src/icons/` | 图标字体子集（`iconfont.scss`）与码位映射（`iconfont.ts`） |
| `src/mock/` | `products.json` 等静态数据 |
| `src/api/` | mock API 层，购物车种子数据与持久化 key |
| `src/static/` | 本地图片、tabBar PNG，禁止引用外部 CDN |
| `src/App.vue` | 全局样式、顶部区域尺寸变量、页面底色变量 |
| `src/pages.json` | 页面注册、`navigationStyle: custom`、内置 tabBar 配置 |
| `tsconfig.json` | 类型检查配置，内联基础选项并启用 `verbatimModuleSyntax` |

## 运行与构建

```bash
npm run dev:h5      # 本地 H5 调试，默认 5173；端口被占用会自动递增
npm run build:h5    # 产物 dist/build/h5
npm run type-check  # vue-tsc --noEmit，改完 TS/组件后必跑
```

- App 端：在 HBuilderX 里「运行到手机」，产物在 `dist/dev/app-plus`
- 改完源码必须**停止再重新运行**到手机，差量编译不会把旧的逻辑层包替换掉
- 后端接口/本地服务端口按工作区规则：开发 `5173` + `5000`，UI 测试 `5174` + `5001`
- `node_modules`、`dist` 已在 `.gitignore` 中排除

## 图标规范（重要）

1. **只用 `<q-icon>`**，禁止 `<t-icon>`、emoji、字体图标之外的图片图标
2. `size` 支持 `px` / `rpx` / 纯数字（纯数字按 px 处理），`color` 默认继承 `currentColor`
3. 图标来源：TDesign 图标字体的**子集**，以 base64 内嵌在 `src/icons/iconfont.scss`（约 6KB），不发起任何网络请求
4. 新增图标步骤：
   - 在 TDesign 图标字体 CSS 里查该图标的码位（`.t-icon-xxx:before { content: "\eXXX" }`）
   - 在 `src/icons/iconfont.ts` 增加 `"图标名": "\uEXXX"`
   - 用 `fontTools.subset` 按新码位集合重新裁剪字体，更新 `iconfont.scss` 的 base64
   - 参考实现：`codex-temp/gen-iconfont.py`（临时目录脚本，若长期维护建议迁入 `mobile/scripts`）
5. 历史教训：`t-icon` 的 SVG 雪碧图用 `<use href="#id">` 引用，在 App 端视图层不可靠；`index.html` 注入、`main.ts` 静态导入、renderjs、`v-html` 四种方案在真机上全部无效，最终改为字体方案

## 布局规范

全局变量定义在 `src/App.vue` 的 `:root, page` 选择器里，页面直接引用，**不要各页重复定义**：

| 变量 | 作用 |
|---|---|
| `--page-bg` | 所有页面统一底色 `#FCF9F8` |
| `--safe-top` | 顶部安全区 `max(88rpx, env(safe-area-inset-top))` |
| `--topbar-height` | 顶部栏内容行高 `62rpx` |
| `--topbar-padding-x` / `--topbar-padding-bottom` | 顶部栏左右内边距 / 底部间距 |
| `--page-top-offset` | 安全区 + 行高 + 底部间距，`q-page-header` / `q-page-header-back` 的最小高度 |

### 页面结构（所有页面统一）

```
<view class="page">                          根节点，背景 var(--page-bg)
  <q-page-header background="#ffffff">       普通顶栏，只做外壳：安全区 + 高度 + 背景 + 横向内边距
    <view class="...">...</view>              页面自己的顶栏内容（左/中/右布局在页面里写）
  </q-page-header>
  <q-page-header-back title="..." subtitle="">带返回键的页头（详情、确认订单）
    <template #right>...</template>           可选：右侧操作
  <view class="page-subbar">                  可选：顶部第二行（如首页搜索行）
  <scroll-view class="page-body">             内容区
    <view class="page-body-inner">            可选：内容内边距容器
  <view class="page-footer">                  可选：底部固定栏
</view>
```

约定：

- 顶栏统一使用 `q-page-header` / `q-page-header-back`（`src/components`，easycom 自动注册），**不要再写** `page-topbar` / `header` / `top-bar` / `nav-bar` / `title-bar` / `navbar` 这类各自为政的容器
- `q-page-header` 只做外壳：安全区、`--page-top-offset` 高度、横向内边距、`background` / `gap` / `border` 三个属性，内容走默认插槽；**不要**把左/中/右布局、标题字号这类页面结构塞进组件
- `q-page-header-back` 带返回键，用于详情、确认订单等内页
- 内容区统一 `page-body`，底部固定栏统一 `page-footer`
- 顶部栏**不要加页面专属装饰**（渐变、投影、彩色收边线、图标方块、底部徽章）
- 搜索行、筛选行等属于内容，放标题栏**外面**；需要固定在顶栏下方时用 `page-subbar`
- 页面根节点背景用 `background: var(--page-bg)`，不要写死色值
- 模板层级保持精简：能合并的包裹层就合并，避免多层 `view` 套壳

## 组件规范

- 位置：`src/components/<组件名>/<组件名>.vue`，统一 `q-` 前缀，easycom 自动注册，不需要在 `main.ts` 手动注册
- 只封装**重复且稳定**的东西：重复交互（数量步进器等）或统一外壳（顶栏）
- 组件保持简单：props 只描述数据与状态（如 `background` / `gap` / `border`），**不要**把页面布局（左中右排布、标题字号、文案）塞进组件
- 组件内部不写业务数据、不做页面跳转；交互通过 `emits` 交回页面
- 新增前先比较各页实现：差异大（如首页与分类的商品卡片）就不要强行合并，避免组件堆满条件分支
- 现有组件：`q-icon`（字体图标）、`q-page-header`（普通顶栏外壳，默认插槽 + 3 个属性）、`q-page-header-back`（带返回键顶栏）

## 页面与样式约定

- 页面改写时**只改 `template` 与 `style`**，保留原有 `script` 逻辑与 mock 数据绑定
- 颜色优先用主题变量（`$theme-primary` 等），避免散落硬编码色值
- 特效统一走 `App.vue` 的 `.qps-*` 皮肤类（`qps-card` / `qps-search-bar` / `qps-hero` / `qps-tap` 等）
- 分部样式写在页面 `<style lang="scss">` 中，页面样式默认非 scoped，注意类名冲突
- `<script setup>` 里的 `computed` / `ref` 必须从 `vue` 导入，**不要从 `@dcloudio/uni-app` 导入 `computed`**（会导致模块加载失败）
- 已开启 `verbatimModuleSyntax`：纯类型导入必须写成 `import type { ... }`，值导入不能被编译器当作类型擦除
- 不使用 `<keep-alive>` 缓存页面，避免全局路由 watcher 跨页干扰

## tabBar

- 使用 `pages.json` 的**内置 tabBar** + `src/static/tabbar/*.png`（透明 PNG）
- uni-app H5 **不支持** `tabBar.custom: true`，自定义 tabBar 组件不会挂载，不要再尝试
- 内置 tabBar 只接受图片，不能放 Vue 图标组件
- 图标由 `codex-temp/make-tabbar-icons.py` 从 780px 设计稿裁切生成：灰度取非激活态、彩色取激活态，alpha 通道透明，保持原生尺寸不放大

## 数据层

- 商品数据：`src/mock/products.json`（`prod-101`~`106` 为新增商品，`prod-001`~`007` 图片已换成本地图）
- API：`src/api/index.ts` 提供 mock 接口，购物车种子数据为 3 件（`qps_cart_seeded_v2`）
- 页面通过 `src/api` 取数，不要在页面里直接读 JSON

## 视觉基准

设计稿在 `E:\Code\QPS\codex-temp\stitch-design-final\`（780px 宽 PNG，6 个页面）：

- `01-homepage`、`02-category`、`03-cart`、`04-product-detail`、`05-profile`、`06-checkout`
- 分类页左侧导航为 B3 Pro 风格：暖米白渐变、本草暗纹、多层卡片、朱红印章、双层三角引导
- 商品详情介绍区为纵向长图流，100% 屏宽无间距连续堆叠，不用横向轮播
- 还原以设计稿规范为准，不做像素级复刻，无法落地的装饰细节可省略

## 验收流程

UI 验收脚本在 `E:\Code\QPS\ui-tests`（Playwright + Edge，390×844、DPR2）：

| 脚本 | 用途 |
|---|---|
| `mobile-screenshots.mjs` | 截图首页/分类/购物车/我的到 `ui-tests/screenshots` |
| `mobile-topbar-measure.mjs` | 量测各页顶部栏高度与内边距，确认统一 |
| `mobile-icon-offline-check.mjs` | 阻断外部域名，校验图标缺失数与外部请求数 |

验收要求：

- 每改完一个页面都要截图确认，控制台除 favicon 404 外不应有错误
- 图标验收标准：`missing` 为 0，`external` 为空（favicon 除外）
- 不占用、不停止开发环境的 `5173` / `5000` 端口

## 常见坑

| 现象 | 原因与处理 |
|---|---|
| 手机上图标全部空白 | 曾因 `t-icon` 从 CDN 拉雪碧图 / App 无 DOM 注入失败；现统一用 `q-icon` 字体，勿改回 |
| 手机上改动不生效 | HBuilderX 差量编译沿用旧包，需停止后重新运行 |
| H5 正常、App 白屏或图标丢失 | 优先检查是否在逻辑层访问 DOM、是否依赖 `<use>` 引用 |
| 页面顶部高度不一致 | 检查是否漏挂 `q-page-header`，或顶部栏自带 padding 覆盖了 `--page-top-offset` |
| IDE 报 `选项“importsNotUsedAsValues”已删除` | 该选项来自 `@vue/tsconfig@0.1.3` 的基础配置，继承的选项无法单独覆盖；项目已改为不继承、内联配置并启用 `verbatimModuleSyntax`，不要把 `extends` 加回去 |
| 报 `Unknown compiler option 'verbatimModuleSyntax'` | TypeScript 低于 5.0 不认识该选项；当前锁定 `5.3.3`，不要降级 |
| 底部 tabBar 无图标 | 检查 `pages.json` 的 `iconPath` / `selectedIconPath` 是否指向 `static/tabbar` 下真实存在的 PNG |
| 图片在手机上不显示 | 图片必须放 `src/static`，禁止外链 |
| 控制台 404 | 多为浏览器默认请求 `favicon.ico`，可忽略 |

## 编码与产物

- 所有文本文件使用 UTF-8，注释与交互文案用中文
- 临时脚本、调试产物放 `E:\Code\QPS\codex-temp`；截图与测试脚本放 `E:\Code\QPS\ui-tests`
- 改动保持最小范围，不回退他人未提交的修改

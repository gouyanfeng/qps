# 商品评价功能完整化 实施计划

## 一、目标与范围

把评价从「后台可看、移动端只读文字」补成完整闭环：

1. 用户在已完成订单中，对每个商品发表一次评价（星级、标签、文字、最多 6 张图片）。
2. 新评价默认「待审核」，后台可明确执行审核（待审核 / 已通过 / 已隐藏），并可回复、删除、预览图片。
3. 移动端商品详情评价区展示评价图片与商家回复。
4. 订单中已评价的商品显示「已评价」，避免重复评价。
5. 移动端评价分页改为「详情页只展示后台配置的评价 + 独立全部评价列表页」，列表页支持标签筛选与上拉加载更多（每页 10 条），不在详情页堆叠大量评价。
6. 后台可对每条已通过评价设置「详情展示」开关与排序值；商品详情展示所有已勾选评价（不限制数量），按排序值升序，未勾选的只在全部评价页出现。

已确认决策：
- 范围：全链路。
- 发布入口：仅「已完成」订单，不做详情页直接评价。
- 评价与订单明细（OrderItem）绑定，一件商品只能评一次。
- 新评价默认待审核；历史演示数据保持已通过。
- 不做「评价奖励积分」，不扩展小程序端差异逻辑。
- 移动端分页：详情页展示后台勾选的全部评价；「查看全部」跳独立评价列表页；列表页每页 10 条、上拉加载；标签筛选走服务端。
- 详情展示配置：单条开关 `IsFeatured` + `SortOrder`，仅已通过评价可勾选；详情页不限制展示数量。未勾选任何评价时，详情卡展示「暂无精选评价」并提供查看全部入口。

## 二、现状分析

- 实体 `MallProductReview` 已有 `ProductId / MemberNickname / MemberLevelName / Rating / Content / SpecText / Tags / ReviewTime / AuditStatus / ReplyContent`，没有会员、订单、图片字段。
- 移动端 `GET /api/mall/products/{id}/reviews` 已返回已通过评价与标签统计，DTO 无图片、无回复。
- 移动端已有会员登录（`qps_member_token`）、订单列表/详情、已完成订单卡片已有「评价有礼」按钮，但只跳详情页。
- 后端 `ICurrentUserService.GetUserId()` + `[Authorize(Policy = "MallMember")]` 可用于取当前会员。
- 上传仅存在管理端 `api/admin/file/upload/img`（`QPS.Host/Controllers/FileUploadController.cs`），移动端无上传能力。
- 后台评价页 `/mall/review` 已有列表、回复、删除、通过/隐藏快捷按钮；已通过行只显示「隐藏」，没有显式「审核」入口。

## 三、改动清单

### 3.1 后端实体与迁移

**`web-api/src/QPS.Mall/Domain/Entities/MallProductReview.cs`**
- 新增字段：`MemberId`（Guid?）、`OrderId`（Guid?）、`OrderItemId`（Guid?）、`Images`（string，逗号分隔相对路径，默认空）、`IsFeatured`（bool，默认 false，是否在商品详情展示）、`SortOrder`（int，默认 0，详情展示排序，越小越靠前）。
- 新增静态工厂 `CreateFromOrder(...)`：接收 `productId/orderId/orderItemId/memberId/nickname/levelName/rating/content/specText/tags/images`，`AuditStatus = Pending`、`ReviewTime = DateTime.Now`。
- 新增 `SetImages(string images)`（内部校验 ≤6 张、单路径 ≤200 字符，超限抛 `BusinessException`）。
- 新增 `SetFeatured(bool isFeatured, int sortOrder)`：仅 `AuditStatus == Approved` 时可勾选展示，否则抛 `BusinessException(400, "请先审核通过再设置详情展示")`；关闭展示时排序值归 0。
- 保留原 `Create(...)` 供演示数据/兼容使用。

**`web-api/src/QPS.Persistence/Configurations/Mall/MallModelConfiguration.cs`**
- `Images` 配置 `HasMaxLength(1000)`、必填、默认 `""`。
- `IsFeatured` 默认 `false`、`SortOrder` 默认 `0`，均为必填。
- 唯一过滤索引：`HasIndex(x => x.OrderItemId).IsUnique().HasFilter("[OrderItemId] IS NOT NULL AND [IsDeleted] = 0")`。

**新增迁移** `AddMallProductReviewOrderLink`
- 增加 `MemberId / OrderId / OrderItemId / Images / IsFeatured / SortOrder` 六列（布尔与整数给默认值）。
- 增加上述唯一过滤索引；`Down` 对称删除。

**审核状态变更联动**（`ChangeMallProductReviewStatusCommand`）
- 审核状态改为非「已通过」时，同时把 `IsFeatured` 置为 false、`SortOrder` 置 0，避免已隐藏/待审核评价继续出现在详情页。

### 3.2 后端 DTO

**`Contracts/DTOs/MallProductReviewDtos.cs`**
- `MallProductReviewDto` 增加 `Images`（string[]）、`ReplyContent`（string）、`IsFeatured`（bool）、`SortOrder`（int）。
- `MallProductReviewPageDto` 增加 `AllTotalCount`（该商品全部已通过评价数）与 `AllPositiveRate`（全部好评率），用于详情页头部统计不受「精选」筛选影响。
- 新增 `MallProductReviewCreateRequest`：`OrderNo`、`OrderItemId`、`Rating`、`Content`、`Tags`（string[]）、`Images`（string[]）。

**`Contracts/DTOs/MallProductReviewAdminDtos.cs`**
- 增加 `Images`（string[]）、`MemberId`、`OrderId`、`IsFeatured`（bool）、`SortOrder`（int）。
- 新增 `MallProductReviewFeaturedRequest`：`IsFeatured`（bool）、`SortOrder`（int）。

**`Contracts/DTOs/MallOrderDtos.cs`**
- `MallOrderItemDto` 增加 `Reviewed`（bool），用于订单页按钮状态。

### 3.3 后端接口

**`Application/Features/MallProducts/GetMallProductReviewsQuery.cs`**
- 返回 DTO 时补 `Images`（拆分逗号）与 `ReplyContent`。
- 新增可选参数 `Tag`：传入时 `Tags.Contains(tag)` 过滤；`Items / TotalCount / PositiveRate` 均按筛选后的集合计算；`TagStats` 始终基于该商品全部已通过评价。
- 新增可选参数 `FeaturedOnly`：为 true 时只返回 `IsFeatured` 的评价；排序规则为 `SortOrder ASC, ReviewTime DESC, Id DESC`；恰好选中几条就返回几条（受 `PageSize` 上限 50 约束，详情页一次性取完）。
- `AllTotalCount / AllPositiveRate` 始终基于该商品全部已通过评价计算，不受 `Tag / FeaturedOnly` 影响。
- 分页仍为 `Page / PageSize`（`PageSize` 上限 50）；非精选查询排序 `ReviewTime DESC, Id DESC`。

**`Controllers/Mobile/MallProductController.cs`**
- `GET products/{id}/reviews` 增加可选查询参数 `tag`、`featuredOnly`，透传给 Query。

**新增后台接口 `PATCH api/admin/mall/reviews/{id}/featured`**
- 请求 `{ isFeatured, sortOrder }`，调用 `SetFeatured`；非「已通过」评价返回 400「请先审核通过再设置详情展示」；评价不存在 404。
- 响应返回更新后的 `MallProductReviewAdminDto`。
- 后台列表查询 DTO 同步映射 `Images / IsFeatured / SortOrder`。

**新增 `CreateMallProductReviewCommand.cs`（同目录）**
- 校验顺序：
  1. 会员 `MemberId` 非空，否则 401。
  2. 订单按 `OrderNo` 查询：不存在 404「订单不存在」；`MemberId` 不匹配 403「无权评价该订单」；`Status != Completed` 400「订单完成后才能评价」。
  3. 订单明细按 `OrderItemId` 查询且属于该订单、`ProductId == 路由 productId`，否则 400「评价商品与订单不一致」。
  4. 已存在 `OrderItemId` 且未删除的评价 → 400「该商品已评价」。
  5. 评分 1–5；内容去空格后 5–500 字；标签最多 3 个、每个 ≤10 字；图片最多 6 张且每张以 `/uploads/` 开头。
- 会员昵称、等级名从 `MallMembers` + `MallMemberLevels` 查询；规格取订单明细 `SpecName`。
- 落库后返回 `MallProductReviewDto`。

**新增 `Controllers/Mobile/MallProductReviewController.cs`**
- `[Route("api/mall")]`、`[Authorize(Policy = "MallMember")]`。
- `POST products/{productId:guid}/reviews`：读取 `ICurrentUserService.GetUserId()` 作为 `MemberId`，调用上述命令。
- 现有 `GET products/{id}/reviews` 保持在 `MallProductController`，不迁移。

**订单查询补 `Reviewed`**
- `GetMallOrdersQuery.cs`：分页装配 `Items` 后，按 `OrderItemId` 批量查询未删除评价，回填 `Reviewed`。
- `MallOrderDetailLoader.cs`：同样按明细批量回填。

**新增移动端上传接口 `web-api/src/QPS.Host/Controllers/MemberFileUploadController.cs`**
- `[Route("api/mall/file")]`、`[Authorize(Policy = "MallMember")]`。
- `POST upload/img?scene=review`：校验 ≤5MB、jpg/jpeg/png/gif/webp，保存到 `wwwroot/uploads/review/yyyyMM/`，返回 `{ fileUrl }`。
- 复用现有校验规则；不改动 `FileUploadController`。

后台查询 `GetMallProductReviewsAdminQuery.cs`：映射补 `Images`、`MemberId`、`OrderId`。

### 3.4 移动端

**`mobile/src/api/types.ts`**
- `ProductReview` 增加 `images: string[]`、`replyContent: string`。
- `MallOrderItem` 增加 `reviewed?: boolean`。

**`mobile/src/api/index.ts`（仅新增）**
- `getProductReviews(productId, page, pageSize, tag?, featuredOnly?)`：新增可选 `tag`、`featuredOnly` 参数拼到查询串；映射默认 `images: []`、`replyContent: ''`，并解析 `allTotalCount / allPositiveRate`。
- 新增 `createProductReview(productId, payload)` → `POST /api/mall/products/{id}/reviews`。
- 新增 `uploadReviewImage(filePath)`：`uni.uploadFile` 到 `/api/mall/file/upload/img?scene=review`，带 `Authorization`，解析统一响应后返回 `fileUrl`。

**`mobile/src/pages/product-detail/index.vue`**
- 评价条目内容下方增加图片宫格（最多 3 列，`resolveFileUrl` + `uni.previewImage`）。
- 增加商家回复块：仅 `review.replyContent` 非空时显示「商家回复」+ 浅绿底文本，位于规格/日期行下方。
- 详情展示按后台配置：
  - 首屏请求 `getProductReviews(id, 1, 50, undefined, true)`，只取 `IsFeatured` 的评价，按后台排序展示，不截断；若单页返回满 50 条，自动继续请求下一页直到取完（保证不限制数量）。
  - 移除 `reviewsExpanded / filteredReviews / canExpandReviews / expandReviews` 及页内标签过滤逻辑。
  - 标题条数使用 `reviews.allTotalCount`，好评率使用 `reviews.allPositiveRate`。
  - 标签胶囊改为跳转入口：点击任一标签 → `/pages/product-reviews/index?productId=...&tag=...`。
  - 底部按钮「查看全部 {{ allTotalCount }} 条评价 ›」，`allTotalCount > 0` 时显示，点击跳 `/pages/product-reviews/index?productId=...`。
  - `allTotalCount > 0` 但精选为空时，卡片内展示「暂无精选评价」，仍保留查看全部入口；`allTotalCount == 0` 时不渲染评价卡片。

**新增 `mobile/src/pages/product-reviews/index.vue`（全部评价列表页）**
- 入参：`productId`、可选 `tag`；使用 `q-page-header-back`，标题「商品评价」。
- 顶部汇总：好评率 `positiveRate`、总条数 `totalCount`。
- 标签筛选：`全部` + `tagStats` 胶囊；切换标签时重置到第 1 页并携带 `tag` 请求服务端。
- 列表：每条含头像、昵称、等级、星级、内容、图片宫格、规格、时间、商家回复；页大小 10。
- 分页：页面级 `onReachBottom` 触发下一页，维护 `page / hasMore / loading`，显示「加载中…」「没有更多了」；`hasMore = 当前已加载条数 < totalCount`。
- 空状态：无评价时展示「暂无评价」。
- 图片点击 `uni.previewImage` 预览。

**`mobile/src/pages/order-detail/index.vue`**
- 商品行操作区增加：`order.status === 'Completed'` 时显示「评价」（未评价）或「已评价」（`item.reviewed`）。
- 未评价点击跳转 `/pages/review-create/index?orderNo=...&itemId=...`；已评价点击提示「该商品已评价」。

**新增 `mobile/src/pages/review-create/index.vue`**
- 入参 `orderNo`、`itemId`；`onLoad` 调 `getOrderDetail` 找到明细，渲染商品图/名称/规格。
- 表单：星级（1–5，默认 5）、标签多选（预设：药材道地、包装精美、物流快、效果显著、客服专业、性价比高，最多 3 个）、文字（5–500，计数）、图片（最多 6 张，`uni.chooseImage` → `uploadReviewImage`，可删除/预览）。
- 提交校验 → `createProductReview` → 成功 toast → `uni.navigateBack()`；订单页 `onShow` 刷新显示「已评价」。
- 使用现有 `q-page-header-back`、`q-icon`；样式沿用详情页绿色系。

**`mobile/src/pages.json`**
- 注册 `pages/review-create/index`（标题「发表评价」）与 `pages/product-reviews/index`（标题「商品评价」），均 `navigationStyle: custom`。

### 3.5 管理端

**`admin/src/views/mall/review/index.vue`**
- 操作列改为：`审核`（打开审核弹窗）、`回复`、`删除`。
- 审核弹窗：展示评价内容，`el-radio-group` 选择「待审核 / 已通过 / 已隐藏」，保存调用 `changeReviewStatus`。
- 新增「评价图片」列：`el-image` 缩略图（40×40），`preview-src-list` 使用 `resolveFileUrl`；无图显示 `-`。
- 新增「详情展示」列：`el-switch` 绑定 `row.isFeatured` + `el-input-number`（排序值，0–99，宽度 80）；切换/失焦时调用 `PATCH /admin/mall/reviews/{id}/featured`。
  - 非「已通过」行开关禁用，提示「请先审核通过」。
  - 保存成功用 `ElMessage.success` 并刷新当前页；失败回滚显示。
- 保留现有筛选、回复弹窗、删除确认、`queryPageRef` 刷新逻辑。

**`admin/src/api/modules/mallReview.ts`**
- 新增 `changeReviewFeatured(id, { isFeatured, sortOrder })` → `PATCH /admin/mall/reviews/{id}/featured`。

## 四、验证步骤

1. 后端：`dotnet build QPS.sln -c Release --no-restore` 0 错误；`dotnet test` 全通过；迁移 `dotnet ef database update` 成功。
2. 前端：`mobile` 执行 `npm run type-check`；`admin` 执行 `npm run build`。
3. 接口验证（重启 5000 后）：
   - 用会员 `13800000001 / 123456` 登录拿 token。
   - 对已完成订单 `S20261031000004` 的人参明细 `POST` 评价（含 2 张图片），断言返回待审核。
   - 重复提交同一 `OrderItemId` 返回 400。
   - 移动端评价接口在审核前不返回该条；后台审核为已通过后返回，且 `images`、`replyContent` 有值。
   - 订单详情 `items[].reviewed` 为 true。
4. 接口分页验证：`GET /reviews?page=1&pageSize=10` 与 `page=2` 数据不重复；带 `tag=包装精美` 时 `totalCount` 与 `items` 均按标签收敛；`tagStats` 保持全局统计。
5. 精选配置验证：
   - 演示数据中给长白山人参勾选 3 条（SortOrder 1/2/3）、宁夏枸杞勾选 2 条（1/2）。
   - `GET /reviews?featuredOnly=true&pageSize=50` 只返回勾选评价且按 SortOrder 升序；`allTotalCount` 仍为全部已通过数量（人参 8）。
   - 后台把一条精选评价改为「已隐藏」，该条自动取消精选，详情接口不再返回。
   - 后台新勾选/取消勾选后，商品详情卡片同步增减。
6. Playwright（390×844，移动端 5174）：
   - 登录 → 订单「已完成」→ 订单详情 → 评价 → 选星级/标签/填文字/上传图片 → 提交 → 订单显示「已评价」。
   - 商品详情评价卡只显示后台勾选的 3 条（排序符合配置），点「查看全部 8 条评价」进入评价列表页；列表首屏 10 条，滚动到底加载下一页；点标签后列表重新从第 1 页加载且总数变化。
   - 商品详情评价卡显示新评价图片与商家回复。
7. Playwright（管理端 5173）：
   - 新评价出现在列表，图片缩略图可预览；审核弹窗切换为已通过后，行状态更新；回复后移动端显示。
   - 「详情展示」开关与排序值可保存，刷新后回显；未审核评价开关禁用。
8. 控制台无新增报错（忽略既有 favicon 404）。

## 五、假设与边界

- 历史演示评价 `OrderItemId` 为空，不参与唯一约束，仍正常展示。
- 评价图片保存相对路径，H5/App 均通过 `resolveFileUrl` / `VITE_API_BASE_URL` 访问。
- 移动端 H5 目前 API 指向 Cloudflare 隧道，上传接口随隧道可用；不改 `.env`。
- 不修改 `src/components`、`src/layout`、请求拦截器、路由守卫；仅在 `api/index.ts` 追加导出函数。
- 不改管理端菜单与权限结构；评价管理菜单已存在。
- 不做评价点赞、举报、敏感词过滤、积分奖励、小程序端适配，避免超范围。
- 评价列表页标签筛选走服务端；标签为空时只传 `page/pageSize`，不传 `tag`。

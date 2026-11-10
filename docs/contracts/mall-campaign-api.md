# 活动专题接口契约（MallCampaign）

> 适用范围：移动端活动专题页（图文 + 推荐商品 / 图文 + 领券）与管理端活动配置。
> 后端实现位置：`web-api/src/QPS.Mall`（`MallCampaigns` 功能目录）。
> 最后验证时间：2026-11-06，后端 `http://localhost:5000`，以下示例 JSON 均为真实响应。

## 1. 通用约定

- 基址：`http://localhost:5000`，接口前缀 `/api`。
- 响应信封：所有接口都返回
  ```json
  { "data": <业务数据>, "code": 200, "msg": "操作成功" }
  ```
- **业务错误统一为 HTTP 200 + 信封内 `code` 非 200**（项目全局异常处理约定），例如：
  ```json
  { "data": null, "code": 404, "msg": "活动专题不存在或已下架" }
  ```
  移动端请以 `code === 200` 判定成功，不要只看 HTTP 状态码。
- 鉴权：
  - `GET /api/mall/campaigns**` 全部匿名可访问；
  - 领券接口、以及 `/api/admin/mall/campaigns**` 需要登录（管理端接口走后台 JWT）。
- 时间格式：ISO 8601，无时区后缀（如 `2026-11-01T00:00:00`），按服务器本地时间解释。

## 2. 活动类型与状态

### 2.1 类型枚举 `type`

| 值 | 枚举 | 本期状态 | 说明 |
|---|---|---|---|
| 0 | `ProductShowcase` | 已实现 | 图文介绍 + 推荐商品组（可按分组名做“搭配组合”整组加购） |
| 1 | `Signup` | **未实现**（仅保留枚举） | 报名型，管理端无配置入口，创建时会被拒绝 |
| 2 | `Coupon` | 已实现 | 图文介绍 + 领券（一张或多张） |

### 2.2 状态 `status`

服务端按服务器本地 `DateTime.Now` 计算，**移动端不要自行用客户端时间推算**：

| 值 | 判定条件 | 含义 |
|---|---|---|
| `Upcoming` | `now < startAt` | 未开始 |
| `Ongoing` | `startAt <= now < endAt` | 进行中 |
| `Ended` | `now >= endAt` | 已结束 |

- 时间窗为**左闭右开** `[startAt, endAt)`。
- `isActive`（上架开关）与状态是两个维度：下架的活动移动端详情返回 404；未开始 / 已结束的活动仍可查看详情与图文，但不应允许下单或领券。

## 3. 移动端接口

### 3.1 活动列表（仅进行中）

`GET /api/mall/campaigns` — 匿名

- 无请求参数。
- 仅返回：`isActive = true`、未删除、且 `startAt <= now < endAt` 的活动，按 `sortOrder` 升序。
- 字段（`MallCampaignPublicListItemDto`）：

| 字段 | 类型 | 说明 |
|---|---|---|
| `id` | string(Guid) | 活动 id，详情页跳转用 |
| `type` | number | 0 商品推荐 / 2 领券（1 报名本期不会出现） |
| `name` | string | 活动名称 |
| `coverImageUrl` | string | 头图相对地址（见第 6 节） |
| `startAt` / `endAt` | string | 活动起止时间 |
| `status` | string | 此处恒为 `Ongoing`（保留字段方便前端统一渲染） |
| `sortOrder` | number | 排序值 |

真实响应（2026-11-06）：

```json
{
  "data": [
    {
      "id": "ce244b71-af4a-44e4-b7cc-f07a29500fad",
      "type": 2,
      "name": "本草礼券 · 秋日润燥券包（2611060904）",
      "coverImageUrl": "/uploads/activity/202611/1111a7abc86f425185ce999937c9f8a8.jpg",
      "startAt": "2026-11-01T00:00:00",
      "endAt": "2027-12-31T23:59:59",
      "status": "Ongoing",
      "sortOrder": 0
    },
    {
      "id": "48359294-c3da-4db7-8792-68d4ecf2c898",
      "type": 0,
      "name": "草本手记 · 霜降润燥手记（2611060904）",
      "coverImageUrl": "/uploads/activity/202611/bf2cd33f56304489aef6acd62b6f5441.jpg",
      "startAt": "2026-11-01T00:00:00",
      "endAt": "2027-12-31T23:59:59",
      "status": "Ongoing",
      "sortOrder": 0
    }
  ],
  "code": 200,
  "msg": "操作成功"
}
```

### 3.2 活动详情（含图文、券包）

`GET /api/mall/campaigns/{id}` — 匿名

- 活动不存在、已删除或已下架（`isActive = false`）→ `code: 404`，`msg: 活动专题不存在或已下架`。
- 未开始 / 已结束的活动**仍会返回**，由前端按 `status` 做展示差异。

字段（`MallCampaignPublicDetailDto`）：

| 字段 | 类型 | 说明 |
|---|---|---|
| `id` | string(Guid) | 活动 id |
| `type` | number | 0 商品推荐 / 2 领券 |
| `name` | string | 活动名称 |
| `coverImageUrl` | string | 头图相对地址 |
| `contentHtml` | string | 图文介绍富文本 HTML；`<img src>` 同样为相对地址 |
| `startAt` / `endAt` | string | 活动起止时间 |
| `status` | string | `Upcoming` / `Ongoing` / `Ended` |
| `coupons` | array | 领券型绑定的券；商品推荐型为 `[]` |

`coupons[]`（`MallCampaignCouponDto`）：

| 字段 | 类型 | 说明 |
|---|---|---|
| `id` | string(Guid) | 活动-券绑定记录 id |
| `couponId` | string(Guid) | **领券时传给领券接口的券 id** |
| `name` / `code` | string | 券名称 / 券码 |
| `couponType` | string | `fixed` 固定金额 / `percent` 折扣（`0.1` 表示 1 折） |
| `thresholdAmount` | number | 门槛金额，0 表示无门槛 |
| `discountAmount` | number | 固定金额时为减免金额；折扣时为折扣率 |
| `maxDiscount` | number \| null | 折扣券最高减免金额 |
| `totalCount` / `remainCount` | number | 券总量 / 剩余可领数量 |
| `perLimit` | number | 每人限领张数 |
| `validDays` | number | 领取后有效天数 |
| `startAt` / `endAt` | string \| null | 券自身有效期（可为空） |
| `scopeType` | string | `all` 全场 / `category` 指定品类 / `product` 指定商品 |
| `isActive` | boolean | 券是否启用 |
| `isClaimable` | boolean | 服务端判定当前是否可领（启用 + 有库存 + 在券有效期内） |
| `sortOrder` | number | 券在活动内的排序 |

真实响应（商品推荐型）：

```json
{
  "data": {
    "id": "48359294-c3da-4db7-8792-68d4ecf2c898",
    "type": 0,
    "name": "草本手记 · 霜降润燥手记（2611060904）",
    "coverImageUrl": "/uploads/activity/202611/bf2cd33f56304489aef6acd62b6f5441.jpg",
    "contentHtml": "<h2>卷首语 · 深秋润燥，在山野与草木间</h2><p>岁至晚秋，燥气当令。天地草木向内收敛精气，人亦如是。古人常言「秋收冬藏」，燥伤肺金，若能适时引草木灵秀入日常饮馔，既是顺应天时，亦是对心神的温柔观照。</p><p>本期寻药团队深入长白林海、岭南柑园与西北川泽，采撷三味节令本草，为您辑成这份暖秋食养手记。</p><h3>长白山深处的十五年守候</h3><p>在长白山苍茫的针阔混交林深处，腐殖土层厚达尺许。野山参年长一载，芦头方显一节。唯有经历过十五轮霜雪寒暑的洗礼，其参体方能聚敛厚实的天地之精。</p><h3>一两陈皮一两金的时间回甘</h3><p>岭南水乡，西江水暖。十年干仓守候，柑皮由鲜亮橙黄渐次转为深沉油褐，烈性尽褪，转为甘润温和。</p><p>午后冲泡时，取野参片两片、老陈皮一瓣、枸杞六七粒。热水倾注的一刹那，深山参香与陈年柑香交错升腾。</p>",
    "startAt": "2026-11-01T00:00:00",
    "endAt": "2027-12-31T23:59:59",
    "status": "Ongoing",
    "coupons": []
  },
  "code": 200,
  "msg": "操作成功"
}
```

真实响应（领券型，节选 `coupons`）：

```json
{
  "data": {
    "id": "ce244b71-af4a-44e4-b7cc-f07a29500fad",
    "type": 2,
    "name": "本草礼券 · 秋日润燥券包（2611060904）",
    "coverImageUrl": "/uploads/activity/202611/1111a7abc86f425185ce999937c9f8a8.jpg",
    "contentHtml": "<h2>本草手记 · 秋日润燥礼券包</h2><p>一券一诺，从产地到案头。秋日肺胃易燥易滞，我们为您备下这份时令礼券，愿草木的温润能抵达日常饮馔的每一个角落。</p><p>券会存入「我的 - 优惠券」，结算时自动可用。数量有限，先领先得。</p>",
    "startAt": "2026-11-01T00:00:00",
    "endAt": "2027-12-31T23:59:59",
    "status": "Ongoing",
    "coupons": [
      {
        "id": "bb04d6b6-2b1f-4f3c-bbd5-ef9f48c5630e",
        "couponId": "3107f65b-5711-470f-90e9-5ad38be0c465",
        "name": "[测试]满100减10",
        "code": "TEST-COUPON-001",
        "couponType": "fixed",
        "thresholdAmount": 100.00,
        "discountAmount": 10.00,
        "maxDiscount": null,
        "totalCount": 1000,
        "remainCount": 999,
        "perLimit": 1,
        "validDays": 30,
        "startAt": "2026-10-21T16:03:25.2795807",
        "endAt": "2026-12-30T16:03:25.2795807",
        "scopeType": "all",
        "isActive": true,
        "isClaimable": true,
        "sortOrder": 0
      },
      {
        "id": "f1505055-09f4-4e57-b677-d5253ba6f933",
        "couponId": "0dfc322f-4f83-4483-a51f-83093c82eebe",
        "name": "[测试]满200享9折",
        "code": "TEST-COUPON-003",
        "couponType": "percent",
        "thresholdAmount": 200.00,
        "discountAmount": 0.10,
        "maxDiscount": 100.00,
        "totalCount": 300,
        "remainCount": 300,
        "perLimit": 1,
        "validDays": 30,
        "startAt": "2026-10-21T16:03:25.2795807",
        "endAt": "2026-12-30T16:03:25.2795807",
        "scopeType": "all",
        "isActive": true,
        "isClaimable": true,
        "sortOrder": 1
      }
    ]
  },
  "code": 200,
  "msg": "操作成功"
}
```

### 3.3 活动商品分页

`GET /api/mall/campaigns/{id}/products?page=1&pageSize=10` — 匿名

- `page` 默认 1；`pageSize` 默认 10（建议 ≤ 20）。
- 商品数据**完全复用** `GET /api/mall/products` 的 `MallProductDto` 与促销富化（角标、折后价、标签），不存在第二套价格口径。
- 业务错误：
  - 活动不存在 / 已下架 → `code: 404`，`msg: 活动专题不存在或已下架`；
  - 活动已结束 → `code: 400`，`msg: 活动已结束`（已结束不返回商品列表）。
- 仅返回未删除、`isActive = true` 的商品；按活动内 `sortOrder` 升序。
- 分页结构：`PaginationResponse`

| 字段 | 类型 | 说明 |
|---|---|---|
| `list` | array | 当前页数据 |
| `totalCount` | number | 总条数 |
| `currentPage` | number | 当前页 |
| `pageSize` | number | 每页条数 |
| `totalPages` | number | 总页数 |

`list[]`（`MallCampaignProductDto`）：

| 字段 | 类型 | 说明 |
|---|---|---|
| `campaignProductId` | string(Guid) | 活动选品记录 id（前端列表 key 可用它） |
| `groupName` | string | 分组名，可为空；空串表示未分组 |
| `sortOrder` | number | 活动内排序 |
| `product` | object | 商品对象，字段与 `GET /api/mall/products` 的 `list[]` **完全一致** |

商品价格口径（重要）：

- 折后价取 `product.skus[].discountedPrice`；为空时用 `product.skus[].price`。
- 划线原价用 `price`，仅当 `discountedPrice < price` 时展示。
- 角标文案/颜色取 `product.promotionBadgeText` / `product.promotionBadgeColor`（后端富化结果），**前端不要自行推算**。
- 标签取 `product.tags`，其中 `type === "Promotion"` 的标签用于角标语义，展示时建议过滤。

真实响应（`pageSize=2`，节选第二个商品以控制篇幅；实际字段完整）：

```json
{
  "data": {
    "list": [
      {
        "campaignProductId": "18636089-28b3-4b54-ad85-0b6ab8b7cdbc",
        "groupName": "晨间润燥组",
        "sortOrder": 0,
        "product": {
          "id": "ee4372a7-66c6-4a6a-86ea-4a54b3df9de4",
          "storeId": "1a1b3a47-6c41-42dc-8e84-6ace57427037",
          "storeName": "本草堂旗舰店",
          "code": "GEN-PROD-0001-COPY-c2b1e4",
          "name": "人参 精选系列 #1拷贝",
          "description": "道地药材，GAP 规范种植基地直采，严选品质，放心服用。",
          "categoryId": "87ce627d-c3ef-4bab-83e8-4cd5c61710e8",
          "categoryName": "人参",
          "mainImageUrl": "/uploads/product/202611/c3e0ae27da2d13733142e10fd5268ff0.png",
          "albumImages": [
            "/uploads/product/202611/94525088eb0107c8d92807a7e47893ab.png",
            "/uploads/product/202611/13915cb26c77dd7856576d7e180575f1.png"
          ],
          "detailHtml": "<h2>人参 精选系列 #1</h2><p>道地药材，GAP 规范种植基地直采，严选品质，放心服用。</p><p><img src=\"/uploads/product/202611/c3e0ae27da2d13733142e10fd5268ff0.png\" /></p>",
          "sortOrder": 1,
          "isActive": true,
          "isHot": true,
          "tags": [
            {
              "id": "eb73490a-2bfe-4375-a584-ebab2a6b8006",
              "type": "Product",
              "code": "MOCK-TAG-ROOT_HERB",
              "name": "根茎类",
              "color": "#67C23A",
              "sortOrder": 10,
              "isActive": true
            },
            {
              "id": "dcdb52e2-b513-4440-a703-cac792cf0551",
              "type": "Promotion",
              "code": "PROMO_DISCOUNT",
              "name": "限时满减",
              "color": "#C2410C",
              "sortOrder": 1,
              "isActive": true
            }
          ],
          "sales": 0,
          "freightTemplateId": null,
          "createdAt": "2026-11-05T15:46:50.3263101",
          "updatedAt": "2026-11-05T15:47:01.5149076",
          "promotions": [
            {
              "id": "e93f61ab-adb5-45b1-bf43-62d674d0d8b8",
              "name": "验证-顶级分类满减-根茎类",
              "description": "",
              "promotionType": 0,
              "thresholdAmount": 100.00,
              "actionType": "DiscountAmount",
              "actionConfigJson": "{\"amount\":15}",
              "scopeType": "Category",
              "ruleText": "满 ¥100，减 ¥15",
              "startDate": null,
              "endDate": null
            }
          ],
          "promotionBadgeText": "限时满减",
          "promotionBadgeColor": "#C2410C",
          "images": [
            "/uploads/product/202611/c3e0ae27da2d13733142e10fd5268ff0.png"
          ],
          "skus": [
            {
              "id": "ad25d160-3a48-4b2a-8b24-32ed75164c38",
              "productId": "ee4372a7-66c6-4a6a-86ea-4a54b3df9de4",
              "skuCode": "GEN-SKU-0001-COPY-664219",
              "specName": "默认规格",
              "price": 150.00,
              "marketPrice": 200.00,
              "weightGram": 267,
              "volumeCm3": 0,
              "stock": 150,
              "warnStock": 10,
              "isActive": true,
              "createdAt": "2026-11-05T15:46:50.3496671",
              "updatedAt": "2026-11-05T15:53:58.8249435",
              "promotions": [],
              "discountedPrice": null,
              "discountLabel": ""
            }
          ]
        }
      },
      {
        "campaignProductId": "cda5bbe7-e143-434a-a5c2-3d9061d950ac",
        "groupName": "夜养安神组",
        "sortOrder": 1,
        "product": {
          "id": "bb9ccb10-51e1-4135-9709-747032446104",
          "name": "人参 精选系列 #1",
          "mainImageUrl": "/uploads/product/202611/c3e0ae27da2d13733142e10fd5268ff0.png",
          "tags": [
            { "type": "Product", "code": "MOCK-TAG-ROOT_HERB", "name": "根茎类", "color": "#67C23A", "isActive": true },
            { "type": "Promotion", "code": "PROMO_DISCOUNT", "name": "限时满减", "color": "#C2410C", "isActive": true }
          ],
          "sales": 179,
          "promotionBadgeText": "限时满减",
          "promotionBadgeColor": "#C2410C",
          "skus": [
            {
              "id": "e3b8a795-7a9d-4ffb-9624-5f670f0b8d3a",
              "specName": "默认规格",
              "price": 2.27,
              "marketPrice": 2.71,
              "stock": 150,
              "isActive": true,
              "discountedPrice": null,
              "discountLabel": ""
            }
          ]
        }
      }
    ],
    "totalCount": 2,
    "totalPages": 1,
    "currentPage": 1,
    "pageSize": 2
  },
  "code": 200,
  "msg": "操作成功"
}
```

### 3.4 领券（复用现有能力）

`POST /api/mall/member/me/coupons/{couponId}/claim` — **需要会员登录**

- 路径里的 `{couponId}` 取活动详情 `coupons[].couponId`。
- 没有新增专用领券接口，直接复用现有 `MallMemberCoupon` 领券能力，服务端已做：
  - 券存在、启用、在券自身有效期内；
  - 库存 `remainCount` 扣减（`CanIssue()` 判定，已领完返回业务错误）；
  - 每人限领 `perLimit` 校验（达到上限返回业务错误）。
- 成功返回 `MallMemberCouponDto`；失败返回 `code` 非 200 的中文 `msg`，前端直接展示即可。
- 移动端“一键领券”= 依次调用本接口；已领取的券建议前端本地标记（接口不返回“该会员是否已领”）。

## 4. 管理端接口

前缀 `/api/admin/mall`，均需后台登录。请求/响应同样是上面的信封格式。

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/admin/mall/campaigns?page=1&pageSize=10&keyword=&isActive=` | 活动分页列表 |
| GET | `/api/admin/mall/campaigns/coupons?page=1&pageSize=10&keyword=` | 可选券分页（领券型配置用） |
| GET | `/api/admin/mall/campaigns/{id}` | 活动详情（含 `items`、`coupons`） |
| POST | `/api/admin/mall/campaigns` | 新增活动，返回新 id |
| PUT | `/api/admin/mall/campaigns/{id}` | 编辑活动（选品/券整体替换） |
| PATCH | `/api/admin/mall/campaigns/{id}/status` | 上/下架 |
| DELETE | `/api/admin/mall/campaigns/{id}` | 软删除（同时软删选品与绑定券） |

### 4.1 新增 / 编辑请求

`POST` 与 `PUT` 使用同一组字段（`PUT` 不修改 `isActive`，上/下架走 `PATCH status`）：

```json
{
  "type": 0,
  "name": "草本手记 · 霜降润燥手记",
  "coverImageUrl": "/uploads/activity/202611/xxx.jpg",
  "contentHtml": "<h2>卷首语</h2><p>……</p>",
  "startAt": "2026-11-01T00:00:00",
  "endAt": "2027-12-31T23:59:59",
  "sortOrder": 0,
  "isActive": true,
  "items": [
    { "productId": "ee4372a7-66c6-4a6a-86ea-4a54b3df9de4", "groupName": "晨间润燥组", "sortOrder": 0 }
  ],
  "couponIds": []
}
```

- `type = 0`（商品推荐）：`items` 必填至少 1 条；`couponIds` 忽略。
- `type = 2`（领券）：`couponIds` 必填至少 1 个；`items` 忽略。
- `type = 1`（报名）：创建/编辑会被拒绝（本期不支持）。
- `PUT` 的 `isActive` 字段会被忽略，请调用 `PATCH /status`：
  ```json
  { "isActive": false }
  ```
- 服务端校验：名称必填/长度、结束时间晚于开始时间、商品或券存在且未删除、分组名长度；不通过返回 `code: 400` + 中文 `msg`。

### 4.2 管理端列表项字段

`MallCampaignListItemDto`：`id`、`type`、`name`、`coverImageUrl`、`startAt`、`endAt`、`status`、`isActive`、`sortOrder`、`productCount`、`createdAt`、`updatedAt`。

## 5. 数据模型选择说明（搭配加购）

本期**没有新增“组合 / 套装”实体**，搭配加购复用 `MallCampaignProduct.GroupName`：

- 一个活动 = 若干选品行；`GroupName` 相同的商品即为一组“搭配”，空分组归入默认组。
- 移动端按 `groupName` 分组渲染，每组可“整组加入购物车”（把组内每个商品的可售 SKU 逐条加入购物车）。
- 优点：模型最小、后台已有分组名编辑入口、无需维护组合价与组合库存；缺点：组内没有独立组合价，整组价 = 组内商品折后价之和。

## 6. 图片地址约定

- 头图 `coverImageUrl`、图文 `contentHtml` 中的 `<img src>`、商品图 `mainImageUrl` 等**都是相对路径**，形如 `/uploads/activity/202611/xxx.jpg`。
- 移动端需要拼 API 源站，项目已有 `resolveFileUrl()`（`mobile/src/utils/http.ts`）：相对路径拼 `VITE_API_BASE_URL`，`/static/` 保持原样，绝对地址原样返回。
- 富文本中的图片是直接在 HTML 字符串里，`rich-text` 不会走 `resolveFileUrl`，需要前端对 `contentHtml` 做前缀替换（项目实现为把 `src="/uploads` 替换成 `src="{API_ORIGIN}/uploads`，并给 `img` 注入 `max-width:100%`）。

## 7. 移动端表现建议

### 7.1 活动页按 `type` 渲染

- `type = 0`：头图 → 标题/时间/状态 → 富文本 → “本期搭配推荐”商品分组（组头 + 商品行）→ 底部“搭配合计 / 整组加入购物车”。
- `type = 2`：头图 → 标题/时间/状态 → 富文本 → 券包列表（金额/折扣、门槛、范围、剩余、每人限领、“立即领取”）→ 底部“一键领券（N）”。

### 7.2 三种状态

| 状态 | 建议表现 |
|---|---|
| `Upcoming` | 顶部状态 pill “未开始” + 提示“活动将于 xx 开始，可先浏览图文与商品”；商品可浏览但加购/领券按钮置灰或点击提示“活动尚未开始”。 |
| `Ongoing` | 正常展示；商品可加购、券可领取。 |
| `Ended` | 状态 pill “已结束” + 提示“活动已于 xx 结束，商品与领券暂不可用”；底部按钮置灰显示“已结束”，商品行仍可展示（点商品详情可跳转，但不要引导下单）。 |

### 7.3 空态与异常

- 商品为空：展示兜底文案（如“本期搭配正在准备中”），不要白屏；底部按钮置灰。
- 券为空 / 券已领完：券区展示兜底文案；`remainCount <= 0` 或 `isClaimable = false` 时按钮显示“已领完 / 不可领取”。
- 详情 404（活动下架/删除）：整页兜底“活动暂不可用”。
- 已结束再请求商品分页会返回 `code: 400`，前端应捕获并展示“活动已结束”，不要把空列表当成无商品。

### 7.4 价格与角标

- 一律使用后端字段：`skus[].discountedPrice`、`skus[].price`、`promotionBadgeText`、`promotionBadgeColor`、`tags`。
- 前端只做展示格式化（保留两位小数等），**不要按促销规则自行算价**。

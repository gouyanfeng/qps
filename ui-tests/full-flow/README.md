# 全流程 UI 回归

本目录收录两条端到端全流程 UI 回归用例，可按需在 CI、本地或远端部署环境复跑。

## 前置条件

- API 服务可访问
- admin 端可访问
- mobile 端可访问（仅商城全流程需要）

本地默认地址：

- API `http://127.0.0.1:5000`
- admin 端 `http://127.0.0.1:5173`
- mobile 端 `http://127.0.0.1:5174`

## 运行

```bash
cd ui-tests
npm ci
npx playwright install msedge
npx playwright test full-flow
```

单独跑某一条：

```bash
npx playwright test full-flow/crm-07-full.spec.mjs
npx playwright test full-flow/mall-07-full.spec.mjs
```

## 指定远端环境

通过环境变量切换目标地址，无需改动用例代码：

```bash
ADMIN_BASE_URL=http://47.114.97.160:5173 \
MOBILE_BASE_URL=http://47.114.97.160:5174 \
npx playwright test full-flow
```

PowerShell：

```powershell
$env:ADMIN_BASE_URL="http://47.114.97.160:5173"
$env:MOBILE_BASE_URL="http://47.114.97.160:5174"
npx playwright test full-flow
```

## 会员账号

商城全流程默认使用固定会员手机号 `13601758260`，默认密码 `111111`。可用环境变量覆盖：

```bash
MEMBER_PHONE=13601758260 MEMBER_PASSWORD=111111 npx playwright test full-flow/mall-07-full.spec.mjs
```

用例先尝试登录；若该手机号尚未注册，会自动切到注册模式用同一凭据完成注册并登录，因此目标环境无需预先准备会员。

## 用例说明

- `crm-07-full.spec.mjs`：基地建档（主体/基地/供应/联系人/沟通/流转）→ 厂商建档（主体/联系人/采购列表/沟通）→ 撮合机会（双侧候选/中选/阶段推进/撮合成功关闭）→ 我的待办跟进 → 全页刷新回查（采购列表/供应列表/基地成交记录/撮合列表/厂商流转）
- `mall-07-full.spec.mjs`：后台建门店/分类/运费/商品/SKU/优惠券 → 会员注册登录 → 下单 → 后台发货 → 会员收货 → 评价提交 → 后台审核

两条用例全程仅通过页面表单与按钮造数，不直接调用业务 API、不写数据库，关键节点刷新后回查。所有业务数据都带时间戳后缀，可反复执行不冲突。

## 截图产物

截图输出到 `ui-tests/playwright-output/`，按用例前缀分目录，例如 `playwright-output/crm-07-full/`、`playwright-output/mall-full/`。该目录不入库。
import assert from "node:assert/strict";
import { access, readFile } from "node:fs/promises";

const source = await readFile("E:/Code/QPS/qps-web-admin/src/views/home/index.vue", "utf8");
const dashboardApiSource = await readFile("E:/Code/QPS/qps-web-admin/src/api/modules/dashboard.ts", "utf8");

assert.ok(!source.includes("HomeMetricCards"), "首页不应保留统计卡片");
assert.ok(!source.includes("TodayFollowTable"), "首页不应保留今日待跟进明细");
assert.ok(!source.includes("RecentFollowRecords"), "首页不应保留最近沟通明细");
assert.ok(source.includes("FollowFunnelChart"), "首页应保留跟进漏斗图");
assert.ok(source.includes("FollowTrendChart"), "首页应保留跟进趋势图");
assert.ok(source.includes("NewBaseTrendChart"), "首页应保留新增基地趋势图");
assert.ok(source.includes("MainProductDistributionChart"), "首页应保留主营品类分布图");

for (const component of ["HomeMetricCards.vue", "TodayFollowTable.vue", "RecentFollowRecords.vue"]) {
  await assert.rejects(
    access(`E:/Code/QPS/qps-web-admin/src/views/home/components/${component}`),
    { code: "ENOENT" },
    `${component} 已不再被首页使用，应当移除`,
  );
}

assert.ok(source.includes("基地数据"), "首页应有独立的基地数据区域");
assert.ok(source.includes("厂商数据"), "首页应有独立的厂商数据区域");
assert.ok(source.includes("dashboard-grid"), "首页应将基地和厂商图表左右分栏");
assert.ok(source.includes("grid-template-columns: repeat(2, minmax(0, 1fr));"), "每类数据的四张图表应按两列展示");
assert.ok(source.includes("height: 400px;"), "首页图表卡片应固定统一高度");
assert.ok(source.includes(".chart-grid :deep(.el-empty)"), "空状态应填满图表卡片剩余高度");
assert.ok(source.includes("dashboard.vendorPriorityDistribution"), "首页应展示厂商优先级数据");
assert.ok(source.includes("dashboard.vendorFollowTrend"), "首页应展示厂商跟进趋势");
assert.ok(source.includes("dashboard.newPurchasePlanTrend"), "首页应展示新增采购计划趋势");
assert.ok(source.includes("dashboard.vendorPurchaseProductDistribution"), "首页应展示厂商采购品类");

for (const field of ["vendorPriorityDistribution", "vendorFollowTrend", "newPurchasePlanTrend", "vendorPurchaseProductDistribution"]) {
  assert.ok(dashboardApiSource.includes(field), `首页接口类型应包含 ${field}`);
}

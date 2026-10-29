# QPS 真实 UI 测试

这套测试只通过页面操作造数和回查，不在测试脚本中直接调用业务 API。

## 运行

```powershell
npm test
```

默认固定使用隔离验收环境：

- 前端：http://127.0.0.1:6174
- 页面环境变量：`VITE_API_URL=http://127.0.0.1:5001/api`

若端口已被占用，只处理隔离测试实例；不得停止或复用开发环境的 `5173`、`5174` 与 `5000`。

## 覆盖

- `auth-home.real-ui.spec.ts`：登录和首页仪表盘。
- `system-role.real-ui.spec.ts`：角色设置，验证列表、新增角色、搜索回查和编辑入口。
- `system-permission.real-ui.spec.ts`：权限设置，验证角色列表、角色切换和权限树展示；不保存权限变更。
- `system-users.real-ui.spec.ts`：用户管理，验证列表、用户名搜索、新增弹窗、角色下拉和编辑入口；不创建真实登录账号。
- `system-data-dictionary.real-ui.spec.ts`：数据字典，验证列表、新增字典、搜索回查和编辑入口。
- `system-region.real-ui.spec.ts`：地址区域维护，验证页面读取中国行政区划数据并支持筛选。
- `system-operation-log.real-ui.spec.ts`：操作日志，验证列表、操作类型/操作人筛选和变更内容详情。
- `system-error-log.real-ui.spec.ts`：错误日志，验证列表、用户/状态码筛选和错误详情。
- `herb-base.real-flow.spec.ts`：基地新增、必填校验、搜索、详情、联系人、沟通、流转操作人、状态、刷新回查；另含“页面可选品类保存”和“详情页供应信息入口、必填校验、全字段保存回查”缺陷回归。
- `vendor.real-flow.spec.ts`：厂商新增后关键词搜索定位缺陷回归；以及基于页面可见测试厂商继续覆盖联系人、采购需求、沟通、流转操作人、编辑、刷新回查。
- `purchase-demand.real-flow.spec.ts`：从页面选择测试厂商，在采购需求页新增、编辑、确认有效、关闭、刷新回查。
- `follow-task.real-ui.spec.ts`：跟进任务列表、筛选、详情入口。
- `crm-05-match-opportunity.spec.mjs`：通过页面创建厂商、有效黄芪采购需求和两条有效供应，完成候选筛选、创建、保留、淘汰、阶段推进、暂停后恢复并推进、成功关闭及刷新回查。

截图输出到 `E:\Code\QPS\ui-tests\playwright-output`。
HTML 报告输出到 `E:\Code\QPS\ui-tests\playwright-output\report`。

## 约束

- 不在测试里用 `request.get/post/put/patch` 直接调用业务 API。
- 不写数据库。
- 测试数据通过页面表单、按钮、下拉、搜索和刷新回查产生。
- 允许监听页面自然发出的响应和控制台错误，用于发现 HTTP 4xx/5xx、业务 `code >= 400` 和前端异常。
- 一个页面一个 spec 文件；同一页面的正向流程和缺陷回归放在同一文件内。

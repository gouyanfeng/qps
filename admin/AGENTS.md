# QPS 管理端开发指南

本文件只约束 `admin/` 内的管理端开发工作；权限体系与后端校验链路见 [`../docs/architecture/permissions.md`](../docs/architecture/permissions.md)，本文件不另立一份权限清单。

## 运行与验证

- Swagger JSON：`http://localhost:5000/swagger/v1/swagger.json`，改接口前先对齐后端契约。
- 提交前必须执行 `npm run build` 验证构建通过，不以编辑器无红线代替构建。

## 组件使用（强制执行）

- **已有封装组件必须使用**：项目已有封装的业务组件（如 `List`、`DetailDrawer`、`QueryPage`、`CandidateSelector`、`ChinaRegionCascader`、`Editor`、图表组件等），新增或改造功能时**必须优先使用**已有封装，不得绕过已有组件自行重复实现搜索、表格、分页、详情抽屉等通用能力。
- **禁止修改基础组件与公共组件**：`src/components` 下的公共组件、`src/layout` 下的布局组件、以及项目共享的路由、权限、请求拦截器等基础设施，**严禁直接修改**。如需扩展或修改，必须先向负责人说明现有能力的不足、影响范围和拟议改动，**得到明确确认后才能修改**。未经确认的改动一律退回。
- 未获确认时，只允许在业务页面内部完成实现，不得触碰共享层。

## 权限控制（强制执行）

- 权限码是唯一判断依据，页面里直接写后端 `SystemPermissions.Code` 的真实字符串，禁止在前端维护 `BUTTONS.xxx`、`useAuthButtons` 之类的映射表或别名，也禁止用变量拼接、大小写转换生成权限码。
- 模板内用 `Permission` 组件包裹受控 UI：`<Permission code="CRM_HERB_BASE_ADD">…</Permission>`；需要在脚本里先算业务条件时用 `authStore.hasPermissionGet("CRM_TRANSFER")`。两者都只做「权限列表是否包含该 code」判断，业务条件写在插槽或 `v-if` 里。
- 新增或修改权限码必须三处同步：后端迁移写入 `SystemPermissions`、`authMenuList.json` 的 `meta.permissionCode`、页面上的 `Permission` / `hasPermissionGet`；任一处缺失视为未完成。
- 菜单 `meta.permissionCode` 必须与后端权限码完全一致；`ROOT` 不出现在菜单中，分组码（`CRM`、`MALL`、`SYSTEM`）由勾选子页面时自动补齐，不在前端手工拼接祖先链。
- 完整权限清单、后端校验链路与废弃权限名单见 [`../docs/architecture/permissions.md`](../docs/architecture/permissions.md)，不得在本文件或页面里另立一份。

## 列表页与通用模式

- 新增或改造列表页前，先检查同类页面的既有实现；搜索、表格、分页等通用能力优先复用项目组件和既有页面模式。

## 语义重命名

- 后端实体、接口、字段、编码或菜单语义改名时，先读取后端的“语义重命名影响清单”，再修改前端；不得只改页面展示文本。
- 每次改名必须检查 API 模块路径、请求参数、TypeScript 接口字段、页面状态和方法名、动态菜单 JSON、权限码、首页图表契约、路由名称和本地缓存键；确认旧名称只保留在历史迁移或已废弃文档中。
- 完成后以浏览器实际调用验证接口路径、菜单可见性和详情回查，不以 TypeScript 编译通过代替接口验证。

## UI 测试

- 开发环境使用管理端 `http://127.0.0.1:5173`、移动端 H5 `http://127.0.0.1:5174` 和后端 `http://localhost:5000/api`；Playwright UI 测试固定使用隔离的 `http://127.0.0.1:6174` 和 `http://127.0.0.1:5001/api`。
- `6174` 或 `5001` 被占用时先释放测试进程，不得切换端口；UI 测试不使用本地 IIS 的 `20004`，也不得占用、停止开发环境的 `5173`、`5174` 或 `5000`。
- UI 测试完成后保留开发环境 `5173`、`5174` 与 `5000` 的服务进程，不得主动停止端口。
- UI 验收必须通过用户 Edge 中的实际页面操作创建、编辑和回查测试数据，不得以直接调用 API 替代业务流程；完成后保留浏览器标签供回归复用。
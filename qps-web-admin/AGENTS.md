

# swagger json url
http://localhost:5000/swagger/v1/swagger.json

# 验证构建
npm run dev

# 前端实现规则

- 新增或改造列表页前，先检查同类页面的既有实现；搜索、表格、分页等通用能力优先复用项目组件和既有页面模式，不能自行重复实现。
- 需要修改公共组件、布局、路由或其他共享能力时，先说明现有能力的不足、影响范围和拟议改动，得到用户确认后才能修改；未确认时只在业务页面内完成实现。

# 临时文件

- 临时测试、Playwright 临时 spec、调试脚本、截图、构建输出和分析产物统一放在 `C:\Users\Dust\AppData\Local\Temp\QPS`，不得使用 `CodexTemp`、`codex-temp` 或仓库内临时目录，也不要放入源码目录或提交仓库。

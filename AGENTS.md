# QPS 工作区开发指南

本文件约束 `E:\Code\QPS` 下的所有项目；子目录的 `AGENTS.md` 只补充其技术栈、运行方式和业务规则。

## 通用要求

- 所有回答使用中文。
- 修改范围保持最小，只改完成当前任务所必需的源码或配置；不得回退其他未提交改动。
- 普通临时产物（草稿、分析结果、实验脚本、调试输出、构建日志）统一放在 `E:\Code\QPS\codex-temp`；不得放入源码目录或提交。
- Playwright UI 测试的 spec、调试脚本、截图和报告统一放在 `E:\Code\QPS\ui-tests`；不得混入 `codex-temp` 或源码目录。
- 不得使用 `C:\Users\Dust\AppData\Local\Temp\QPS`、`CodexTemp` 或其他系统临时目录保存项目产物。

## UI 测试

- 本地前端与 Playwright UI 测试固定使用 `http://127.0.0.1:5173`，后端 API 使用 `http://localhost:5000/api`。
- `5173` 被占用时先释放占用进程，不得自动切换端口；UI 测试不使用本地 IIS 的 `20004`。
- UI 验收必须通过用户 Edge 中的实际页面操作创建、编辑和回查测试数据，不得以直接调用 API 替代业务流程；完成后保留浏览器标签供回归复用。

## 编码

- Windows 下读取 `.md`、`.txt`、`.json`、`.xml`、`.yml`、`.yaml`、`.cs`、`.js`、`.ts`、`.vue`、`.html`、`.css` 等文本文件时，优先按 UTF-8 编码处理。
- PowerShell 读取文件使用 `Get-Content -LiteralPath "路径" -Encoding UTF8`；除非项目已有其他明确要求，文本修改保持 UTF-8。
- 遇到 `鈥?`、`鈫?`、`鈮?`、`锟斤拷` 等乱码时，先判断控制台或输出链路编码，不能直接认定源文件已损坏。
- Windows PowerShell 5.1 运行含中文的临时 `.ps1` 时使用 UTF-8 with BOM，或改用 PowerShell 7。

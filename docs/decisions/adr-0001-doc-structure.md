# ADR-0001：文档结构以根 AGENTS.md 为唯一索引

> 状态：已接受 | 日期：2026-11-10

## 1. 背景

仓库内的文档此前散落在三处：`docs/` 下的长期文档、`docs/superpowers/` 下的 37 份过程产物、`.trae/documents/` 下 14 份在途文档。根 `AGENTS.md` 同时承担索引与正文两种职责，塞了通用要求、UI 测试、编码、组件边界共 5 章条文，共 31 行。

由此产生四个具体问题：

- 新会话读到的根 `AGENTS.md` 与真实文档不同步，条文改一处要改两处。
- 过程产物与长期事实文档平级，查找时无法区分「还在指导开发」和「已经完成」。
- 长期文档使用中文文件名，跨平台链接与脚本遍历容易出错。
- `.trae/` 整个目录被 gitignore，其中的在途文档从未入库，随时可能丢失。

## 2. 决策

- 根 `AGENTS.md` 改为纯索引：只有一张表加一节维护规则，表格外不写任何正文。
- `docs/` 固定 8 个一级目录：`product/`、`operations/`、`architecture/`、`domain/`、`contracts/`、`design/`、`decisions/`、`archive/`。
- 长期事实文档文件名一律小写英文 kebab-case；`docs/archive/` 保留原文件名与中文标题，只移动不改名。
- 根 `AGENTS.md` 原有 5 章条文按主题迁入被索引文档：协作约定与共享层边界进 `architecture/modular-monolith.md`，产物目录与编码进 `operations/getting-started.md`，UI 测试端口隔离进 `admin/AGENTS.md`。
- `.trae/documents/` 内容按类型分流到 `contracts/`、`design/`、`archive/`，不再保留第三处入口。

## 3. 被放弃的方案

- **保留根 `AGENTS.md` 正文 + 另建索引**：两处入口必然漂移，且违反「入口唯一」。
- **把条文留在根 `AGENTS.md`，只把文档清单挪走**：根文件仍是正文文件，新人读到的规则与代码不同步问题没有解决。
- **`docs/archive/` 也统一改英文名**：归档是历史快照，改名会让外部引用与历史提交对不上，收益低于成本。
- **保留 `.trae/documents/` 作为「在途」专用目录**：它被 gitignore，等于让在途文档处于无版本状态，风险高于便利。

## 4. 影响

- 根 `AGENTS.md` 不再是唯一被自动加载的规则文件，新会话必须按索引主动打开「必读」文件才能拿到完整约束。缓解方式是索引的说明列直接写关键词，让不打开文件也能判断相关性。
- 长期文档改名后，所有旧链接必须同步更新；本次已修正 `README.md`、`web-api/README.md`、`web-api/AGENTS.md` 三处，并重生成 `docs/docs-viewer.html`。
- 阅读器 `tools/build-docs-viewer.mjs` 跳过 `docs/archive/`，归档内容不进入 HTML，避免历史产物污染阅读视图。
- 后续新增文档必须与索引行同提交，否则根 `AGENTS.md` 会再次失去准确性。
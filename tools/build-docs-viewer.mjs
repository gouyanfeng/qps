import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// 仓库根目录由脚本位置推导，避免把本机绝对路径写死。
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

// 扫描 docs/ 下所有 Markdown；新增文档放进该目录即可自动收录，无需维护文件清单。
const DOC_ROOTS = ['docs'];

const GROUP_LABELS = {
  architecture: '架构',
  contracts: '接口契约',
  decisions: '决策记录',
  design: '页面设计',
  domain: '领域',
  operations: '运维',
  product: '产品'
};

// 归档目录只读留档，不进入阅读器。
const SKIP_DIRS = new Set(['archive']);

function walkMarkdown(dir) {
  if (!fs.existsSync(dir)) return [];
  const files = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (entry.name.startsWith('.')) continue;
    if (entry.isDirectory() && SKIP_DIRS.has(entry.name)) continue;
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      files.push(...walkMarkdown(full));
    } else if (entry.isFile() && entry.name.toLowerCase().endsWith('.md') && !entry.name.startsWith('_')) {
      files.push(full);
    }
  }
  return files;
}

const docFiles = [];
for (const root of DOC_ROOTS) {
  const absRoot = path.join(ROOT, root);
  for (const file of walkMarkdown(absRoot)) docFiles.push({ file, root });
}
docFiles.sort((a, b) => a.file.localeCompare(b.file));

const DOCS = docFiles.map(({ file, root }, index) => {
  const rel = path.relative(ROOT, file).replace(/\\/g, '/');
  const markdown = fs.readFileSync(file, 'utf8');
  const heading = markdown.match(/^#\s+(.+)$/m);
  const folder = path.relative(path.join(ROOT, root), path.dirname(file)).replace(/\\/g, '/');
  const groupKey = folder ? folder.split('/').pop() : '';
  return {
    id: `d${index + 1}`,
    group: GROUP_LABELS[groupKey] || groupKey || '文档',
    title: heading ? heading[1].replace(/`/g, '').trim() : path.basename(file, '.md'),
    file: rel
  };
});

const DOC_BY_PATH = new Map();
for (const d of DOCS) {
  DOC_BY_PATH.set(path.normalize(path.join(ROOT, d.file)).replace(/\\/g, '/'), d);
}

function escapeHtml(s) {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function escapeRegExp(s) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function joinParagraph(prev, next) {
  if (!prev) return next;
  if (/[\u3000-\u9fff，。；：！？、）】》]$/.test(prev) && /^[\u3000-\u9fff（【《]/.test(next)) {
    return prev + next;
  }
  return prev + ' ' + next;
}

function renderInline(text, ctx) {
  let s = escapeHtml(text);
  const codes = [];
  s = s.replace(/`([^`]+)`/g, (_, code) => {
    codes.push(code);
    return `\u0000CODE${codes.length - 1}\u0000`;
  });

  s = s.replace(/!\[([^\]]*)\]\(([^)]+)\)/g, (_, alt) => {
    return `<span class="img-ref" title="文档中的图片，未内嵌">[图片：${alt || '无说明'}]</span>`;
  });

  s = s.replace(/\[([^\]]+)\]\(([^)]+)\)/g, (_, label, href) => {
    const clean = href.split('#')[0];
    if (/^https?:/i.test(href)) {
      return `<a href="${href}" target="_blank" rel="noopener">${label}</a>`;
    }
    if (!clean) {
      return `<span class="doc-ref">${label}</span>`;
    }
    const abs = path.normalize(path.join(path.dirname(ctx.srcFile), clean)).replace(/\\/g, '/');
    const doc = DOC_BY_PATH.get(abs);
    if (doc) {
      return `<a class="doc-link" data-doc="${doc.id}" href="#doc-${doc.id}">${label}</a>`;
    }
    return `<span class="doc-ref" title="源文档链接，未包含在阅读器：${escapeHtml(clean)}">${label}</span>`;
  });

  s = s.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
  s = s.replace(/\u0000CODE(\d+)\u0000/g, (_, i) => `<code>${codes[Number(i)]}</code>`);
  return s;
}

function isListItem(line) {
  return /^\s*([-*+]|\d+\.)\s+/.test(line);
}

function isTableSeparator(line) {
  if (!line || !line.includes('-') || !line.includes('|')) return false;
  const cells = splitRow(line);
  return cells.length > 0 && cells.every(c => /^:?-{3,}:?$/.test(c.trim()));
}

function splitRow(line) {
  let s = line.trim();
  if (s.startsWith('|')) s = s.slice(1);
  if (s.endsWith('|')) s = s.slice(0, -1);
  const cells = [];
  let cur = '';
  let inCode = false;
  for (let i = 0; i < s.length; i++) {
    const ch = s[i];
    if (ch === '`') {
      inCode = !inCode;
      cur += ch;
    } else if (ch === '|' && !inCode) {
      cells.push(cur.trim());
      cur = '';
    } else if (ch === '\\' && s[i + 1] === '|') {
      cur += '|';
      i++;
    } else {
      cur += ch;
    }
  }
  cells.push(cur.trim());
  return cells;
}

function renderList(root) {
  const tag = root.ordered ? 'ol' : 'ul';
  const items = root.items.map(item => {
    const sub = item.children.map(renderList).join('');
    return `<li>${item.html}${sub}</li>`;
  }).join('');
  return `<${tag}>${items}</${tag}>`;
}

function parseList(lines, start) {
  const root = { indent: null, ordered: false, items: [], children: [] };
  const stack = [root];
  let i = start;
  let lastItem = null;

  while (i < lines.length) {
    const raw = lines[i];
    if (/^\s*$/.test(raw)) {
      let j = i + 1;
      while (j < lines.length && /^\s*$/.test(lines[j])) j++;
      if (j < lines.length && isListItem(lines[j])) {
        i = j;
        continue;
      }
      break;
    }
    const m = raw.match(/^(\s*)([-*+]|\d+\.)\s+(.*)$/);
    if (!m) {
      if (lastItem && /^\s+/.test(raw) && !/^\s*```/.test(raw)) {
        lastItem.html = joinParagraph(lastItem.html, renderInline(raw.trim(), parseList.ctx));
        i++;
        continue;
      }
      break;
    }
    const indent = m[1].replace(/\t/g, '    ').length;
    const ordered = /\d+\./.test(m[2]);
    let text = m[3];

    if (root.indent === null) {
      root.indent = indent;
      root.ordered = ordered;
    }
    while (stack.length > 1 && indent < stack[stack.length - 1].indent) {
      stack.pop();
    }
    let top = stack[stack.length - 1];
    if (indent > top.indent) {
      const child = { indent, ordered, items: [], children: [] };
      if (lastItem) lastItem.children.push(child);
      else top.children.push(child);
      stack.push(child);
      top = child;
    } else {
      while (stack.length > 1 && indent < stack[stack.length - 1].indent) stack.pop();
      top = stack[stack.length - 1];
    }

    const checkbox = text.match(/^\[([ xX])\]\s+(.*)$/);
    if (checkbox) {
      const box = checkbox[1].toLowerCase() === 'x' ? '☑' : '☐';
      text = `<span class="checkbox">${box}</span> ${checkbox[2]}`;
    }

    const item = { html: renderInline(text, parseList.ctx), children: [] };
    top.items.push(item);
    lastItem = item;
    i++;
  }

  return { html: renderList(root), next: i };
}

function parseMarkdown(markdown, doc) {
  const lines = markdown.replace(/\r\n/g, '\n').replace(/\r/g, '\n').split('\n');
  parseList.ctx = { srcFile: doc.file };
  const ctx = { srcFile: doc.file };
  const headings = [];
  const out = [];
  let i = 0;
  let headingCounter = 0;

  const blockStart = (line, nextLine) => {
    if (/^\s*$/.test(line)) return true;
    if (/^#{1,6}\s+/.test(line)) return true;
    if (/^```/.test(line)) return true;
    if (/^\s*>/.test(line)) return true;
    if (/^\s*([-*+]|\d+\.)\s+/.test(line)) return true;
    if (/^\s*(-{3,}|\*{3,})\s*$/.test(line)) return true;
    if (line.includes('|') && nextLine && isTableSeparator(nextLine)) return true;
    return false;
  };

  while (i < lines.length) {
    const line = lines[i];

    if (/^\s*$/.test(line)) {
      i++;
      continue;
    }

    const fence = line.match(/^\s*```(\S*)\s*$/);
    if (fence) {
      const lang = fence[1] || '';
      const code = [];
      i++;
      while (i < lines.length && !/^\s*```\s*$/.test(lines[i])) {
        code.push(lines[i]);
        i++;
      }
      i++;
      out.push(`<pre class="code-block"${lang ? ` data-lang="${escapeHtml(lang)}"` : ''}><code>${escapeHtml(code.join('\n'))}</code></pre>`);
      continue;
    }

    const heading = line.match(/^(#{1,6})\s+(.*)$/);
    if (heading) {
      const level = heading[1].length;
      headingCounter++;
      const id = `${doc.id}-h${headingCounter}`;
      const text = renderInline(heading[2].trim(), ctx);
      if (level <= 3) {
        headings.push({ level, text: heading[2].trim(), id });
      }
      out.push(`<h${level} id="${id}">${text}<a class="anchor" href="#${id}" aria-label="锚点">#</a></h${level}>`);
      i++;
      continue;
    }

    if (/^\s*(-{3,}|\*{3,})\s*$/.test(line)) {
      out.push('<hr>');
      i++;
      continue;
    }

    if (line.includes('|') && i + 1 < lines.length && isTableSeparator(lines[i + 1])) {
      const headerCells = splitRow(line);
      const alignCells = splitRow(lines[i + 1]);
      const aligns = alignCells.map(c => {
        const t = c.trim();
        const left = t.startsWith(':');
        const right = t.endsWith(':');
        if (left && right) return 'center';
        if (right) return 'right';
        return 'left';
      });
      i += 2;
      const rows = [];
      while (i < lines.length && lines[i].includes('|') && !/^\s*$/.test(lines[i])) {
        rows.push(splitRow(lines[i]));
        i++;
      }
      const head = headerCells.map((c, idx) =>
        `<th class="align-${aligns[idx] || 'left'}">${renderInline(c, ctx)}</th>`
      ).join('');
      const body = rows.map(row => {
        const cells = headerCells.map((_, idx) => {
          const value = row[idx] || '';
          return `<td class="align-${aligns[idx] || 'left'}">${renderInline(value, ctx)}</td>`;
        }).join('');
        return `<tr>${cells}</tr>`;
      }).join('');
      out.push(`<div class="table-wrap"><table><thead><tr>${head}</tr></thead><tbody>${body}</tbody></table></div>`);
      continue;
    }

    if (/^\s*>/.test(line)) {
      const quote = [];
      while (i < lines.length && /^\s*>/.test(lines[i])) {
        quote.push(lines[i].replace(/^\s*>\s?/, ''));
        i++;
      }
      out.push(`<blockquote><p>${quote.map(l => renderInline(l, ctx)).join('<br>')}</p></blockquote>`);
      continue;
    }

    if (isListItem(line)) {
      const list = parseList(lines, i);
      out.push(list.html);
      i = list.next;
      continue;
    }

    const para = [];
    while (i < lines.length && !blockStart(lines[i], lines[i + 1])) {
      para.push(lines[i].trim());
      i++;
    }
    if (para.length) {
      out.push(`<p>${renderInline(para.join(' '), ctx)}</p>`);
    } else {
      i++;
    }
  }

  return { html: out.join('\n'), headings };
}

const generatedAt = new Date().toLocaleString('zh-CN', { hour12: false });
const docData = DOCS.map(doc => {
  const markdown = fs.readFileSync(path.join(ROOT, doc.file), 'utf8');
  const parsed = parseMarkdown(markdown, doc);
  return { ...doc, ...parsed };
});

let navHtml = '';
let docHtml = '';

docData.forEach((doc, index) => {
  const isFirst = index === 0;
  const sections = doc.headings
    .filter(h => h.level === 2 || h.level === 3)
    .map(h => `<a class="nav-sec level-${h.level}" href="#${h.id}" data-doc="${doc.id}" data-target="${h.id}"><span class="dash"></span>${escapeHtml(h.text.replace(/`/g, ''))}</a>`)
    .join('');

  navHtml += `
    <div class="nav-group${isFirst ? ' open' : ''}" data-doc="${doc.id}">
      <button class="nav-doc" data-doc="${doc.id}">
        <span class="nav-no">${String(index + 1).padStart(2, '0')}</span>
        <span class="nav-name">${escapeHtml(doc.title)}</span>
        <span class="nav-tag">${escapeHtml(doc.group)}</span>
      </button>
      <div class="nav-sections">${sections || '<span class="nav-empty">无章节</span>'}</div>
    </div>`;

  docHtml += `
    <section class="doc${isFirst ? ' active' : ''}" id="doc-${doc.id}" data-doc="${doc.id}">
      <div class="doc-meta">
        <span class="meta-label">源文件</span>
        <code>${escapeHtml(doc.file)}</code>
        <span class="meta-sep">/</span>
        <span>${doc.headings.length} 个标题</span>
      </div>
      <article class="md-body">${doc.html}</article>
    </section>`;
});

const html = `<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>QPS 文档阅读器</title>
<style>
:root {
  --bg: #f3f5f4;
  --panel: #ffffff;
  --ink: #1f2933;
  --ink-soft: #52606d;
  --line: #e3e8e6;
  --accent: #0f766e;
  --accent-soft: #e7f3f1;
  --sidebar: #f8faf9;
  --code-bg: #f6f8f7;
  --shadow: 0 10px 30px rgba(15, 38, 32, 0.06);
}
* { box-sizing: border-box; }
html { scroll-padding-top: 92px; }
body {
  margin: 0;
  background: var(--bg);
  color: var(--ink);
  font-family: "PingFang SC", "Microsoft YaHei", "Source Han Sans SC", "Noto Sans CJK SC", sans-serif;
  font-size: 15.5px;
  line-height: 1.85;
}
a { color: var(--accent); text-decoration: none; }
a:hover { text-decoration: underline; }
.topbar {
  position: sticky;
  top: 0;
  z-index: 20;
  height: 68px;
  background: #10201c;
  color: #f5faf8;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  padding: 0 28px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.18);
}
.brand { display: flex; align-items: baseline; gap: 12px; min-width: 0; }
.brand strong { font-size: 18px; letter-spacing: 0.04em; white-space: nowrap; }
.brand span { font-size: 12.5px; color: #a9c5bd; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.top-note { font-size: 12px; color: #a9c5bd; text-align: right; line-height: 1.5; }
.top-note b { color: #d8ebe6; font-weight: 500; }
.layout {
  display: grid;
  grid-template-columns: 292px minmax(0, 1fr);
  gap: 26px;
  max-width: 1600px;
  margin: 0 auto;
  padding: 26px 28px 60px;
}
.sidebar {
  position: sticky;
  top: 92px;
  height: calc(100vh - 118px);
  display: flex;
  flex-direction: column;
  background: var(--panel);
  border: 1px solid var(--line);
  border-radius: 12px;
  box-shadow: var(--shadow);
  overflow: hidden;
}
.search-box {
  padding: 14px 14px 12px;
  border-bottom: 1px solid var(--line);
  background: var(--sidebar);
}
.search-box input {
  width: 100%;
  height: 38px;
  border: 1px solid var(--line);
  border-radius: 8px;
  padding: 0 12px;
  font: inherit;
  font-size: 13.5px;
  color: var(--ink);
  background: #fff;
  outline: none;
}
.search-box input:focus { border-color: var(--accent); box-shadow: 0 0 0 3px var(--accent-soft); }
.search-hint { margin-top: 7px; font-size: 11.5px; color: var(--ink-soft); display: flex; justify-content: space-between; gap: 8px; }
.search-hint b { color: var(--accent); font-weight: 600; }
.nav { overflow-y: auto; padding: 8px 8px 18px; }
.nav-group { margin-bottom: 6px; }
.nav-doc {
  width: 100%;
  border: 0;
  background: transparent;
  text-align: left;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 10px;
  border-radius: 8px;
  cursor: pointer;
  font: inherit;
  color: var(--ink-soft);
}
.nav-doc:hover { background: #f1f5f4; color: var(--ink); }
.nav-group.open .nav-doc { color: var(--accent); background: var(--accent-soft); font-weight: 600; }
.nav-no { font-size: 11px; color: #8a9a94; font-variant-numeric: tabular-nums; }
.nav-name { flex: 1; font-size: 13.5px; line-height: 1.4; }
.nav-tag { font-size: 10.5px; color: #7b8b85; border: 1px solid var(--line); border-radius: 999px; padding: 1px 7px; background: #fff; }
.nav-sections { display: none; padding: 4px 0 6px 12px; border-left: 1px solid var(--line); margin-left: 16px; }
.nav-group.open .nav-sections { display: block; }
.nav-sec {
  display: block;
  position: relative;
  padding: 5px 8px 5px 14px;
  border-radius: 6px;
  color: var(--ink-soft);
  font-size: 12.5px;
  line-height: 1.45;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.nav-sec.level-3 { padding-left: 24px; font-size: 12px; }
.nav-sec .dash {
  position: absolute;
  left: 5px;
  top: 50%;
  width: 4px;
  height: 1px;
  background: #b9c7c2;
}
.nav-sec:hover { background: #f4f7f6; color: var(--ink); text-decoration: none; }
.nav-sec.active { background: var(--accent-soft); color: var(--accent); font-weight: 600; }
.nav-sec.active .dash { background: var(--accent); width: 6px; height: 2px; }
.nav-empty { display: block; padding: 6px 12px; font-size: 12px; color: #9aa8a3; }
.main { min-width: 0; }
.doc { display: none; }
.doc.active { display: block; }
.doc-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  padding: 10px 18px;
  margin-bottom: 14px;
  background: #eef5f3;
  border: 1px solid #d9e7e3;
  border-radius: 10px;
  font-size: 12.5px;
  color: #4c615b;
}
.doc-meta code { background: #fff; border: 1px solid #dce8e4; border-radius: 5px; padding: 1px 7px; font-size: 12px; }
.meta-label { font-weight: 600; color: var(--accent); }
.meta-sep { color: #9bb2ab; }
.md-body {
  background: var(--panel);
  border: 1px solid var(--line);
  border-radius: 12px;
  box-shadow: var(--shadow);
  padding: 34px 40px 46px;
  max-width: 1120px;
}
.md-body h1 {
  font-size: 26px;
  line-height: 1.4;
  margin: 0 0 18px;
  letter-spacing: 0.01em;
}
.md-body h2 {
  font-size: 20px;
  line-height: 1.45;
  margin: 42px 0 14px;
  padding-bottom: 9px;
  border-bottom: 1px solid var(--line);
  scroll-margin-top: 92px;
}
.md-body h3 {
  font-size: 16.5px;
  margin: 30px 0 10px;
  color: #223b35;
  scroll-margin-top: 92px;
}
.md-body h4 { font-size: 15px; margin: 24px 0 8px; }
.md-body p { margin: 10px 0; }
.md-body ul, .md-body ol { margin: 10px 0 14px; padding-left: 26px; }
.md-body li { margin: 5px 0; }
.md-body li > ul, .md-body li > ol { margin: 5px 0 2px; }
.md-body strong { color: #163f37; }
.md-body code {
  font-family: Consolas, "Cascadia Mono", "Courier New", monospace;
  font-size: 0.9em;
  background: #edf2f0;
  border: 1px solid #dee7e4;
  border-radius: 4px;
  padding: 0.5px 5px;
  word-break: break-word;
}
.code-block {
  margin: 16px 0;
  padding: 16px 18px;
  background: var(--code-bg);
  border: 1px solid var(--line);
  border-left: 4px solid #8fb8ae;
  border-radius: 8px;
  overflow: auto;
  max-height: 70vh;
  line-height: 1.65;
}
.code-block code {
  background: transparent;
  border: 0;
  padding: 0;
  font-size: 13px;
  white-space: pre;
  word-break: normal;
}
.table-wrap {
  margin: 16px 0;
  border: 1px solid var(--line);
  border-radius: 9px;
  overflow-x: auto;
  overflow-y: hidden;
  background: #fff;
  max-width: 100%;
}
.table-wrap table {
  border-collapse: collapse;
  width: max-content;
  min-width: 100%;
  font-size: 13.5px;
  line-height: 1.65;
}
.table-wrap th, .table-wrap td {
  border-right: 1px solid #e6ecea;
  border-bottom: 1px solid #e6ecea;
  padding: 9px 12px;
  vertical-align: top;
  text-align: left;
  max-width: 420px;
  white-space: normal;
  word-break: break-word;
}
.table-wrap th {
  background: #f4f8f7;
  color: #25443d;
  font-weight: 600;
  white-space: nowrap;
}
.table-wrap th.align-right, .table-wrap td.align-right { text-align: right; }
.table-wrap th.align-center, .table-wrap td.align-center { text-align: center; }
.table-wrap tr:last-child td { border-bottom: 0; }
.table-wrap th:last-child, .table-wrap td:last-child { border-right: 0; }
.table-wrap tbody tr:nth-child(even) { background: #fbfdfc; }
.table-wrap td code { white-space: normal; word-break: normal; overflow-wrap: break-word; }
blockquote {
  margin: 14px 0;
  padding: 10px 16px;
  background: #f7faf9;
  border-left: 4px solid #b7cec7;
  color: #40544e;
}
blockquote p { margin: 4px 0; }
hr { border: 0; border-top: 1px solid var(--line); margin: 28px 0; }
.anchor {
  margin-left: 8px;
  color: #b6c6c1;
  font-weight: 400;
  opacity: 0;
  transition: opacity .15s;
}
h1:hover .anchor, h2:hover .anchor, h3:hover .anchor, h4:hover .anchor { opacity: 1; }
.doc-ref { color: var(--ink-soft); border-bottom: 1px dashed #b9c7c2; }
.img-ref { color: #7b8b85; font-size: 12px; }
.checkbox { color: var(--accent); font-weight: 600; }
mark { background: #fff1a8; color: inherit; padding: 0 1px; border-radius: 2px; }
.doc.hidden-by-search, .nav-group.hidden-by-search, .nav-sec.hidden-by-search { display: none !important; }
.footer-note {
  max-width: 1120px;
  margin: 18px 0 0;
  color: #7b8b85;
  font-size: 12px;
}
@media (max-width: 1100px) {
  .layout { grid-template-columns: 250px minmax(0, 1fr); gap: 18px; padding: 18px; }
  .md-body { padding: 26px 24px 36px; }
}
@media (max-width: 860px) {
  .topbar { height: auto; min-height: 64px; padding: 12px 16px; flex-direction: column; align-items: flex-start; gap: 4px; }
  .brand { width: 100%; max-width: 100%; }
  .brand span { flex: 1; min-width: 0; }
  .top-note { text-align: left; max-width: 100%; }
  .layout { display: block; padding: 14px; }
  .sidebar {
    position: static;
    height: auto;
    max-height: 42vh;
    margin-bottom: 16px;
  }
  .nav { max-height: 30vh; }
  .md-body { padding: 22px 16px 30px; }
  .md-body h1 { font-size: 22px; }
  .md-body h2 { font-size: 18px; }
}
</style>
</head>
<body>
<header class="topbar">
  <div class="brand">
    <strong>QPS 文档阅读器</strong>
    <span>由 Markdown 生成 · 修改源文件后需重新生成</span>
  </div>
  <div class="top-note">
    内联渲染 ${docData.length} 份文档 · 单文件离线可用<br>
    <b>生成时间：${escapeHtml(generatedAt)}</b>
  </div>
</header>
<div class="layout">
  <aside class="sidebar">
    <div class="search-box">
      <input id="search" type="search" placeholder="筛选章节 / 高亮关键字（按 / 聚焦）" autocomplete="off">
      <div class="search-hint"><span>共 ${docData.reduce((n, d) => n + d.headings.filter(h => h.level <= 3).length, 0)} 个章节</span><span id="matchCount"></span></div>
    </div>
    <nav class="nav" id="nav">${navHtml}</nav>
  </aside>
  <main class="main" id="main">
    ${docHtml}
    <div class="footer-note">本页面完全离线：CSS 与 JavaScript 均内联，不加载任何外部字体、脚本或图片。文档内容以对应 Markdown 源文件为准。</div>
  </main>
</div>
<script>
(function () {
  var docSections = Array.prototype.slice.call(document.querySelectorAll('.doc'));
  var navGroups = Array.prototype.slice.call(document.querySelectorAll('.nav-group'));
  var navSecs = Array.prototype.slice.call(document.querySelectorAll('.nav-sec'));
  var navDocs = Array.prototype.slice.call(document.querySelectorAll('.nav-doc'));
  var searchInput = document.getElementById('search');
  var matchCount = document.getElementById('matchCount');
  var originals = new Map();
  docSections.forEach(function (section) {
    var article = section.querySelector('.md-body');
    originals.set(section.dataset.doc, article.innerHTML);
  });
  var activeDoc = docSections[0] ? docSections[0].dataset.doc : '';
  var searchTimer = null;

  function setActiveNav(docId, targetId) {
    navDocs.forEach(function (b) { b.classList.toggle('active-doc', b.dataset.doc === docId); });
    navGroups.forEach(function (g) { g.classList.toggle('open', g.dataset.doc === docId); });
    navSecs.forEach(function (a) {
      a.classList.toggle('active', !!targetId && a.dataset.target === targetId);
    });
  }

  function showDoc(docId, targetId, doScroll) {
    if (docId === activeDoc && !targetId) return;
    activeDoc = docId;
    docSections.forEach(function (s) { s.classList.toggle('active', s.dataset.doc === docId); });
    setActiveNav(docId, targetId);
    if (doScroll !== false) {
      if (targetId) {
        var target = document.getElementById(targetId);
        if (target) target.scrollIntoView({ block: 'start', behavior: 'auto' });
      } else {
        window.scrollTo({ top: 0, behavior: 'auto' });
      }
    }
    if (searchInput.value.trim()) applySearch(searchInput.value.trim());
  }

  document.getElementById('nav').addEventListener('click', function (e) {
    var sec = e.target.closest('.nav-sec');
    if (sec) {
      e.preventDefault();
      showDoc(sec.dataset.doc, sec.dataset.target, true);
      return;
    }
    var docBtn = e.target.closest('.nav-doc');
    if (docBtn) {
      showDoc(docBtn.dataset.doc, '', true);
    }
  });

  document.getElementById('main').addEventListener('click', function (e) {
    var link = e.target.closest('a.doc-link');
    if (link) {
      e.preventDefault();
      showDoc(link.dataset.doc, '', true);
    }
  });

  window.addEventListener('scroll', function () {
    if (!activeDoc || searchInput.value.trim()) return;
    var section = document.getElementById('doc-' + activeDoc);
    if (!section) return;
    var heads = Array.prototype.slice.call(section.querySelectorAll('h2, h3'));
    var current = '';
    for (var i = 0; i < heads.length; i++) {
      if (heads[i].getBoundingClientRect().top <= 110) current = heads[i].id;
      else break;
    }
    setActiveNav(activeDoc, current);
  }, { passive: true });

  function highlight(container, query) {
    var needle = query.toLowerCase();
    var walker = document.createTreeWalker(container, NodeFilter.SHOW_TEXT, null);
    var nodes = [];
    var count = 0;
    while (walker.nextNode()) {
      var current = walker.currentNode;
      if (current.parentElement && /^(SCRIPT|STYLE)$/.test(current.parentElement.tagName)) continue;
      if (current.nodeValue.toLowerCase().indexOf(needle) >= 0) {
        var found = 0;
        var from = 0;
        while (true) {
          var at = current.nodeValue.toLowerCase().indexOf(needle, from);
          if (at < 0) break;
          found++;
          from = at + needle.length;
        }
        count += found;
        nodes.push(current);
      }
    }
    nodes.forEach(function (node) {
      var text = node.nodeValue;
      var lower = text.toLowerCase();
      var frag = document.createDocumentFragment();
      var last = 0;
      var at = lower.indexOf(needle);
      while (at >= 0) {
        if (at > last) frag.appendChild(document.createTextNode(text.slice(last, at)));
        var mark = document.createElement('mark');
        mark.textContent = text.slice(at, at + needle.length);
        frag.appendChild(mark);
        last = at + needle.length;
        at = lower.indexOf(needle, last);
      }
      if (last < text.length) frag.appendChild(document.createTextNode(text.slice(last)));
      node.parentNode.replaceChild(frag, node);
    });
    return count;
  }

  function sectionText(sec) {
    var head = document.getElementById(sec.dataset.target);
    if (!head) return sec.textContent || '';
    var text = head.textContent || '';
    var node = head.nextElementSibling;
    while (node && !/^H[123]$/.test(node.tagName)) {
      text += ' ' + (node.textContent || '');
      node = node.nextElementSibling;
    }
    return text;
  }

  function applySearch(q) {
    var lower = q.toLowerCase();
    var total = 0;
    docSections.forEach(function (section) {
      var article = section.querySelector('.md-body');
      article.innerHTML = originals.get(section.dataset.doc);
      if (!q) {
        section.classList.remove('hidden-by-search');
        return;
      }
      var count = highlight(article, q);
      total += count;
      section.classList.toggle('hidden-by-search', count === 0);
    });

    navGroups.forEach(function (group) {
      var any = false;
      var docId = group.dataset.doc;
      group.querySelectorAll('.nav-sec').forEach(function (sec) {
        var match = q ? sectionText(sec).toLowerCase().indexOf(lower) >= 0 : true;
        sec.classList.toggle('hidden-by-search', !match);
        if (match) any = true;
      });
      group.classList.toggle('hidden-by-search', !any);
      if (any && docId === activeDoc) group.classList.add('open');
    });
    matchCount.textContent = q ? total + ' 处匹配' : '';
    if (q && total === 0 && document.getElementById('doc-' + activeDoc)) {
      document.getElementById('doc-' + activeDoc).classList.add('hidden-by-search');
    }
  }

  searchInput.addEventListener('input', function () {
    clearTimeout(searchTimer);
    var q = searchInput.value.trim();
    searchTimer = setTimeout(function () { applySearch(q); }, 160);
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === '/' && document.activeElement !== searchInput) {
      e.preventDefault();
      searchInput.focus();
    }
    if (e.key === 'Escape' && document.activeElement === searchInput) {
      searchInput.value = '';
      applySearch('');
      searchInput.blur();
    }
  });

  setActiveNav(activeDoc, '');
})();
</script>
</body>
</html>`;

fs.writeFileSync(path.join(ROOT, 'docs/docs-viewer.html'), html, 'utf8');
console.log('written docs/docs-viewer.html', html.length, 'bytes');
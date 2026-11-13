<template>
    <div class="permission-page">
        <!-- 顶部：当前配置角色 + 全局动作 -->
        <section class="role-banner">
            <div class="banner-left">
                <span class="banner-label">当前配置角色:</span>
                <el-select v-model="selectedRole" class="role-select" placeholder="请选择角色">
                    <el-option v-for="role in roleList" :key="role.code"
                        :label="`${role.name} (${role.code})`" :value="role.code" />
                </el-select>
                <span class="banner-badge">
                    <span class="badge-dot" />
                    已授权 {{ currentLeafCodes.length }} 项
                </span>
            </div>
            <div class="banner-actions">
                <el-button :icon="Refresh" @click="resetPerm">重置</el-button>
                <Permission code="SYSTEM_PERMISSION_EDIT">
                    <el-button type="primary" :icon="Check" :loading="saving" @click="savePerm">保存权限</el-button>
                </Permission>
            </div>
        </section>

        <div class="workspace">
            <!-- 矩阵区 -->
            <section class="matrix-panel">
                <div class="matrix-toolbar">
                    <div class="module-chips">
                        <span class="toolbar-label">模块分类:</span>
                        <el-check-tag :checked="moduleFilter === 'ALL'" @change="moduleFilter = 'ALL'">全部模块</el-check-tag>
                        <el-check-tag v-for="group in matrixGroups" :key="group.code"
                            :checked="moduleFilter === group.code"
                            @change="moduleFilter = group.code">{{ group.name }}</el-check-tag>
                    </div>
                    <div class="toolbar-tools">
                        <el-input v-model="keyword" class="row-search" 
                            placeholder="搜索页面或操作按钮..." :prefix-icon="Search" clearable />
                        <el-button  :icon="Sort" @click="toggleAllGroups">
                            {{ allCollapsed ? '全部展开' : '全部收起' }}
                        </el-button>
                        <el-button :icon="Finished" @click="selectAllVisible">全选访问</el-button>
                    </div>
                </div>

                <div class="matrix-scroll">
                    <table class="perm-matrix">
                        <colgroup>
                            <col class="col-menu" />
                            <col style="width: 300px" />
                            <col />
                        </colgroup>
                        <thead>
                            <tr>
                                <th>功能模块 / 页面路由</th>
                                <th class="col-access">页面访问权限</th>
                                <th>操作按钮授权</th>
                            </tr>
                        </thead>
                        <tbody v-if="visibleGroups.length">
                            <template v-for="group in visibleGroups" :key="group.code">
                                <tr v-if="!group.leaf" class="group-row" @click="toggleCollapse(group.code)">
                                    <td colspan="3">
                                        <div class="group-inner">
                                            <el-icon class="group-caret"
                                                :class="{ collapsed: collapsedGroups.has(group.code) }">
                                                <ArrowRight />
                                            </el-icon>
                                            <el-icon class="folder-icon"><FolderOpened /></el-icon>
                                            <span class="group-name">{{ group.name }}</span>
                                            <span class="code">{{ group.code }}</span>
                                            <span class="group-count">{{ group.pages.length }} 个页面</span>
                                            <span class="spacer" />
                                            <span class="group-all" @click.stop>
                                                <el-checkbox :model-value="groupAllChecked(group)"
                                                    :indeterminate="groupIndeterminate(group)"
                                                    @change="(val: any) => toggleGroupAll(group, val)" />
                                                <span class="group-all-label">全选访问</span>
                                            </span>
                                        </div>
                                    </td>
                                </tr>
                                <template v-if="!collapsedGroups.has(group.code)">
                                    <tr v-for="page in group.pages" :key="page.code" class="page-row"
                                        :class="{ 'top-page': group.leaf, 'is-off': !isChecked(page.code) }">
                                        <td class="page-cell">
                                            <div class="page-inner">
                                                <el-icon v-if="group.leaf" class="folder-icon">
                                                    <FolderOpened />
                                                </el-icon>
                                                <span v-else class="page-dot" />
                                                <span class="page-name">{{ page.name }}</span>
                                                <span class="code">{{ page.code }}</span>
                                            </div>
                                        </td>
                                        <td class="access-cell">
                                            <div class="access-inner">
                                                <el-switch :model-value="isChecked(page.code)"
                                                    @change="(val: any) => togglePage(page, val)" />
                                                <span class="access-label" :class="{ on: isChecked(page.code) }">
                                                    {{ isChecked(page.code) ? '允许访问' : '已禁用' }}
                                                </span>
                                            </div>
                                        </td>
                                        <td class="buttons-cell">
                                            <div v-if="page.buttons.length" class="btn-chips">
                                                <el-checkbox v-for="btn in page.buttons" :key="btn.code"
                                                    class="btn-chip" :model-value="isChecked(btn.code)"
                                                    :disabled="!isChecked(page.code)" :title="btn.code"
                                                    @change="(val: any) => toggleButton(page, btn, val)">
                                                    <span class="btn-name">{{ btn.name }}</span>
                                                    <el-tag v-if="btn.highRisk" type="danger"
                                                        effect="plain" class="risk-tag">高危</el-tag>
                                                </el-checkbox>
                                            </div>
                                            <span v-else class="no-btn">无操作按钮</span>
                                        </td>
                                    </tr>
                                </template>
                            </template>
                        </tbody>
                        <tbody v-else>
                            <tr>
                                <td colspan="3" class="matrix-empty">没有匹配的权限</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </section>

            <!-- 右栏：修改记录 -->
            <aside class="changes-panel">
                <div class="panel-head">
                    <span>修改记录</span>
                    <span v-if="diffTotal" class="panel-count">{{ diffTotal }}</span>
                </div>
                <div class="changes-summary">
                    <span>待保存变更 ({{ diffTotal }}项)</span>
                    <el-button link type="primary" :disabled="!diffTotal"
                        @click="resetPerm">撤销所有</el-button>
                </div>
                <div class="changes-scroll">
                    <div v-if="!diffTotal" class="panel-empty">暂无修改</div>
                    <div v-for="item in diffAdded" :key="`a-${item.code}`" class="change-card">
                        <div class="change-head">
                            <span class="change-tag added">新增授权</span>
                            <span class="change-code">{{ item.code }}</span>
                        </div>
                        <div class="change-title">{{ item.path }}</div>
                    </div>
                    <div v-for="item in diffRemoved" :key="`r-${item.code}`" class="change-card">
                        <div class="change-head">
                            <span class="change-tag removed">取消授权</span>
                            <span class="change-code">{{ item.code }}</span>
                        </div>
                        <div class="change-title">{{ item.path }}</div>
                    </div>
                </div>
            </aside>
        </div>
    </div>
</template>

<script setup lang="ts" name="permission">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh, Check, Search, Sort, Finished, FolderOpened, ArrowRight } from '@element-plus/icons-vue'
import { permissionApi } from '@/api/modules/permission'
import { roleApi } from '@/api/modules/role'
import Permission from '@/components/Permission/index.vue'
import authMenuList from '@/assets/json/authMenuList.json'

// ── 类型 ──

interface TreeNode {
    id: string
    code: string
    name: string
    type: string
    children?: TreeNode[]
}

interface MatrixButton {
    id: string
    code: string
    name: string
    highRisk: boolean
}

interface MatrixPage {
    id: string
    code: string
    name: string
    buttons: MatrixButton[]
    groupCode: string
}

interface MatrixGroup {
    id: string
    code: string
    name: string
    pages: MatrixPage[]
    leaf: boolean
}

interface RoleItem {
    id: string
    name: string
    code: string
}

// ── 角色 ──

const roleList = ref<RoleItem[]>([])
const selectedRole = ref('')

// ── 矩阵数据 ──

const matrixGroups = ref<MatrixGroup[]>([])
const nodeByCode = new Map<string, TreeNode>()
const parentByCode = new Map<string, string>()
const leafCodeSet = ref<Set<string>>(new Set())
const checked = reactive(new Set<string>())
const rootChecked = ref(false)

const moduleFilter = ref('ALL')
const keyword = ref('')
const collapsedGroups = reactive(new Set<string>())

const isChecked = (code: string) => checked.has(code)

const visibleGroups = computed(() => {
    let groups = matrixGroups.value
    if (moduleFilter.value !== 'ALL') {
        groups = groups.filter(g => g.code === moduleFilter.value)
    }
    const kw = keyword.value.trim().toLowerCase()
    if (!kw) return groups
    return groups
        .map(group => {
            if (group.name.toLowerCase().includes(kw) || group.code.toLowerCase().includes(kw)) {
                return group
            }
            const pages = group.pages.filter(page =>
                page.name.toLowerCase().includes(kw)
                || page.code.toLowerCase().includes(kw)
                || page.buttons.some(btn => btn.name.toLowerCase().includes(kw) || btn.code.toLowerCase().includes(kw))
            )
            return { ...group, pages }
        })
        .filter(group => group.pages.length > 0)
})

const allCollapsed = computed(() =>
    visibleGroups.value.length > 0
    && visibleGroups.value.every(group => group.leaf || collapsedGroups.has(group.code))
)

const groupAllChecked = (group: MatrixGroup) =>
    group.pages.length > 0 && group.pages.every(page => checked.has(page.code))

const groupIndeterminate = (group: MatrixGroup) => {
    const count = group.pages.filter(page => checked.has(page.code)).length
    return count > 0 && count < group.pages.length
}

// ── 变更 ──

const savedLeafCodes = ref<string[]>([])

const currentLeafCodes = computed(() => [...checked])

const pathOf = (code: string) => {
    const names: string[] = []
    let current = code
    while (current && current !== 'ROOT') {
        const node = nodeByCode.get(current)
        if (!node) break
        names.unshift(node.name)
        current = parentByCode.get(current) || ''
    }
    return names.join(' / ')
}

const diffAdded = computed(() => {
    const savedSet = new Set(savedLeafCodes.value)
    return currentLeafCodes.value
        .filter(code => !savedSet.has(code))
        .map(code => ({ code, path: pathOf(code) }))
})

const diffRemoved = computed(() => {
    const currentSet = new Set(currentLeafCodes.value)
    return savedLeafCodes.value
        .filter(code => !currentSet.has(code))
        .map(code => ({ code, path: pathOf(code) }))
})

const diffTotal = computed(() => diffAdded.value.length + diffRemoved.value.length)

// ── 角色权限 ──

const rolePerms = reactive<Record<string, string[]>>({})

// ── 勾选交互 ──

const togglePage = (page: MatrixPage, value: any) => {
    if (Boolean(value)) {
        checked.add(page.code)
        return
    }
    // 页面访问关闭后，该页按钮自动取消并进入只读态
    checked.delete(page.code)
    page.buttons.forEach(btn => checked.delete(btn.code))
}

const toggleButton = (page: MatrixPage, btn: MatrixButton, value: any) => {
    if (!checked.has(page.code)) return
    if (Boolean(value)) checked.add(btn.code)
    else checked.delete(btn.code)
}

const toggleGroupAll = (group: MatrixGroup, value: any) => {
    if (Boolean(value)) {
        group.pages.forEach(page => checked.add(page.code))
        return
    }
    group.pages.forEach(page => {
        checked.delete(page.code)
        page.buttons.forEach(btn => checked.delete(btn.code))
    })
}

const selectAllVisible = () => {
    visibleGroups.value.forEach(group => {
        group.pages.forEach(page => checked.add(page.code))
    })
}

const toggleCollapse = (groupCode: string) => {
    if (collapsedGroups.has(groupCode)) collapsedGroups.delete(groupCode)
    else collapsedGroups.add(groupCode)
}

const toggleAllGroups = () => {
    if (allCollapsed.value) {
        collapsedGroups.clear()
        return
    }
    visibleGroups.value.forEach(group => {
        if (!group.leaf) collapsedGroups.add(group.code)
    })
}

// ── 角色切换 / 重置 ──

const applyPerm = (roleCode: string) => {
    const codes = rolePerms[roleCode] || []
    const leaves = codes.filter(code => leafCodeSet.value.has(code))
    savedLeafCodes.value = leaves
    rootChecked.value = codes.includes('ROOT')

    checked.clear()
    leaves.forEach(code => checked.add(code))

    // 页面未授权时，其按钮一律不保留勾选，保持与页面访问联动一致
    matrixGroups.value.forEach(group => group.pages.forEach(page => {
        if (!checked.has(page.code)) page.buttons.forEach(btn => checked.delete(btn.code))
    }))

    collapsedGroups.clear()
    moduleFilter.value = 'ALL'
    keyword.value = ''
}

watch(selectedRole, roleCode => {
    if (!roleCode || !nodeByCode.size) return
    applyPerm(roleCode)
})

const resetPerm = () => {
    applyPerm(selectedRole.value)
    ElMessage.info('已重置为已保存的权限')
}

// ── 保存 ──

const collectAncestors = (code: string, target: Set<string>) => {
    let parent = parentByCode.get(code)
    while (parent && parent !== 'ROOT') {
        target.add(parent)
        parent = parentByCode.get(parent)
    }
}

const buildSaveCodes = () => {
    const result = new Set<string>()
    currentLeafCodes.value.forEach(code => {
        result.add(code)
        collectAncestors(code, result)
    })
    if (rootChecked.value) result.add('ROOT')
    return [...result]
}

const saving = ref(false)

const savePerm = async () => {
    if (!selectedRole.value) {
        ElMessage.warning('请先选择角色')
        return
    }
    saving.value = true
    try {
        const permissions = buildSaveCodes()
        await permissionApi.updateRolePermission({ role: selectedRole.value, permissions })
        rolePerms[selectedRole.value] = permissions
        applyPerm(selectedRole.value)
        ElMessage.success('保存成功')
    } catch {
        ElMessage.error('保存失败')
    } finally {
        saving.value = false
    }
}

// ── 树构建 ──

const indexTree = (nodes: TreeNode[], parentCode: string) => {
    nodes.forEach(node => {
        nodeByCode.set(node.code, node)
        parentByCode.set(node.code, parentCode)
        if (node.children?.length) indexTree(node.children, node.code)
    })
}

const collectButtons = (node: TreeNode): MatrixButton[] =>
    (node.children || [])
        .filter(child => child.type === 'Button')
        .map(child => ({
            id: child.id,
            code: child.code,
            name: child.name,
            highRisk: child.code.endsWith('_DELETE')
        }))
        // 删除类按钮统一排到最后，避免字母序把高危操作放在最前
        .sort((a, b) => Number(a.highRisk) - Number(b.highRisk))

// 权限树来自数据库（按 code 排序），这里换成菜单声明顺序，保证矩阵与侧边栏一致
const menuOrder = new Map<string, number>()
{
    let seq = 0
    const walkMenu = (items: any[]) => {
        items.forEach(item => {
            const code = item?.meta?.permissionCode
            if (code && !menuOrder.has(code)) menuOrder.set(code, seq++)
            if (item.children?.length) walkMenu(item.children)
        })
    }
    walkMenu((authMenuList as any).data || [])
}
const orderOf = (code: string) => menuOrder.get(code) ?? Number.MAX_SAFE_INTEGER

const buildMatrix = (roots: TreeNode[]): MatrixGroup[] => {
    const root = roots.find(node => node.code === 'ROOT')
    if (!root) return []
    const groups: MatrixGroup[] = []
    const leafCodes = new Set<string>()

    ;(root.children || []).forEach(top => {
        if (top.type !== 'Menu') return
        const pages: MatrixPage[] = []
        const addPage = (node: TreeNode) => {
            const page: MatrixPage = {
                id: node.id,
                code: node.code,
                name: node.name,
                buttons: collectButtons(node),
                groupCode: top.code
            }
            pages.push(page)
            leafCodes.add(page.code)
            page.buttons.forEach(btn => leafCodes.add(btn.code))
        }
        const menuChildren = (top.children || []).filter(child => child.type === 'Menu')
        const addPageTree = (node: TreeNode) => {
            addPage(node)
            ;(node.children || [])
                .filter(child => child.type === 'Menu')
                .forEach(addPageTree)
        }
        if (menuChildren.length) {
            menuChildren.forEach(addPageTree)
        } else {
            addPage(top)
        }
        groups.push({
            id: top.id,
            code: top.code,
            name: top.name,
            pages,
            leaf: menuChildren.length === 0
        })
    })

    // 分组与页面都按菜单位置排序
    groups.sort((a, b) => orderOf(a.code) - orderOf(b.code))
    groups.forEach(group => group.pages.sort((a, b) => orderOf(a.code) - orderOf(b.code)))

    leafCodeSet.value = leafCodes
    return groups
}

// ── 初始化 ──

const loadData = async () => {
    try {
        const roleRes = await roleApi.getRoleList({ page: 1, pageSize: 100 })
        const roles = (roleRes as any).data?.list || []
        roleList.value = roles.map((role: any) => ({
            id: role.id,
            name: role.name,
            code: role.code
        }))

        const treeRes = await permissionApi.getPermissionTree()
        const roots = ((treeRes as any).data || []) as TreeNode[]
        indexTree(roots, '')
        matrixGroups.value = buildMatrix(roots)

        const permRes = await permissionApi.getPermissionList()
        const data = (permRes as any).data || {}
        Object.entries(data).forEach(([roleCode, value]) => {
            rolePerms[roleCode] = (value as any).permissions || []
        })

        selectedRole.value = roleList.value[0]?.code || ''
        if (selectedRole.value) applyPerm(selectedRole.value)
    } catch (error) {
        console.error('加载权限数据失败:', error)
        ElMessage.error('加载权限数据失败')
    }
}

onMounted(loadData)
</script>

<style scoped lang="scss">
.permission-page {
    display: flex;
    flex-direction: column;
    height: 100%;
    gap: 12px;
}

// 顶部角色栏
.role-banner {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    flex-shrink: 0;
    padding: 10px 16px;
    background: #fff;
    border: 1px solid var(--el-border-color-lighter);
    border-radius: 8px;
    box-shadow: 0 1px 3px rgba(15, 23, 42, 0.06);

    .banner-left {
        display: flex;
        align-items: center;
        gap: 10px;
        min-width: 0;
    }

    .banner-label {
        font-size: 14px;
        font-weight: 600;
        white-space: nowrap;
    }

    .role-select {
        width: 220px;
    }

    .banner-badge {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 2px 10px;
        border-radius: 999px;
        background: rgba(0, 150, 136, 0.1);
        color: var(--el-color-primary);
        font-size: 12px;
        font-weight: 500;
        white-space: nowrap;

        .badge-dot {
            width: 6px;
            height: 6px;
            border-radius: 50%;
            background: var(--el-color-primary);
        }
    }

    .banner-actions {
        display: flex;
        gap: 8px;
    }
}

// 三栏工作区
.workspace {
    display: flex;
    flex: 1;
    min-height: 0;
    gap: 12px;
}

.panel-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 12px 16px;
    font-size: 14px;
    font-weight: 600;
    border-bottom: 1px solid var(--el-border-color-lighter);
    background: #f5f7fa;

    .panel-count {
        min-width: 20px;
        padding: 1px 7px;
        border-radius: 999px;
        background: var(--el-color-primary);
        color: #fff;
        font-size: 11px;
        font-weight: 500;
        text-align: center;
    }
}

.matrix-panel,
.changes-panel {
    display: flex;
    flex-direction: column;
    min-height: 0;
    overflow: hidden;
    background: #fff;
    border: 1px solid var(--el-border-color-lighter);
    border-radius: 8px;
    box-shadow: 0 1px 3px rgba(15, 23, 42, 0.06);
}

.matrix-panel {
    flex: 1;
    min-width: 0;
}

.changes-panel {
    width: 450px;
    flex-shrink: 0;
}

// 修改记录空态
.panel-empty {
    padding: 32px 12px;
    text-align: center;
    font-size: 13px;
    color: var(--el-text-color-secondary);
}

// 工具栏
.matrix-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
    flex-wrap: wrap;
    padding: 10px 12px;
    border-bottom: 1px solid var(--el-border-color-lighter);
}

.module-chips {
    display: flex;
    align-items: center;
    gap: 6px;
    flex-wrap: wrap;

    .toolbar-label {
        margin-right: 2px;
        font-size: 12px;
        color: var(--el-text-color-secondary);
    }
}

.toolbar-tools {
    display: flex;
    align-items: center;
    gap: 8px;

    .row-search {
        width: 200px;
    }
}

// 矩阵表格
.matrix-scroll {
    flex: 1;
    min-height: 0;
    overflow: auto;
}

.perm-matrix {
    width: 100%;
    table-layout: fixed;
    border-collapse: collapse;
    font-size: 13px;

    // 菜单列宽：fixed 布局下 col 宽度优先，td 上的宽度会被忽略
    col.col-menu {
        width: 400px;
    }

    th {
        position: sticky;
        top: 0;
        z-index: 1;
        padding: 8px 12px;
        background: #f5f7fa;
        border-bottom: 1px solid var(--el-border-color-lighter);
        color: var(--el-text-color-secondary);
        font-size: 11px;
        font-weight: 600;
        letter-spacing: 0.04em;
        text-align: left;
        text-transform: uppercase;

        &.col-access {
            text-align: center;
        }
    }

    td {
        padding: 8px 12px;
        border-bottom: 1px solid var(--el-border-color-lighter);
        vertical-align: middle;
    }

    .group-row td {
        padding: 6px 12px;
        background: rgba(245, 247, 250, 0.75);
        cursor: pointer;
    }

    .page-row {
        &:hover td {
            background: rgba(0, 150, 136, 0.03);
        }

        &.is-off {
            .page-name,
            .code {
                color: var(--el-text-color-secondary);
            }
        }
    }
}

.group-inner {
    display: flex;
    align-items: center;
    gap: 8px;
    min-width: 0;

    .group-caret {
        font-size: 13px;
        color: var(--el-text-color-secondary);
        transform: rotate(90deg);
        transition: transform 0.15s;

        &.collapsed {
            transform: rotate(0);
        }
    }

    .folder-icon {
        color: var(--el-color-primary);
        font-size: 16px;
    }

    .group-name {
        font-size: 14px;
        font-weight: 600;
        color: var(--el-text-color-primary);
    }

    .group-count {
        flex-shrink: 0;
        padding: 1px 7px;
        border-radius: 999px;
        background: var(--el-fill-color-light);
        color: var(--el-text-color-secondary);
        font-size: 11px;
    }

    .spacer {
        flex: 1;
    }

    .group-all {
        display: inline-flex;
        align-items: center;
        gap: 2px;
        flex-shrink: 0;
    }

    .group-all-label {
        font-size: 11px;
        color: var(--el-text-color-secondary);
    }
}

.page-cell {
    width: 400px;
    max-width: 400px;
    overflow: hidden;
}

.page-cell .page-inner {
    display: flex;
    align-items: center;
    gap: 8px;
    min-width: 0;
    padding-left: 26px;
}

.top-page .page-inner {
    padding-left: 0;
}

.page-dot {
    width: 5px;
    height: 5px;
    border-radius: 50%;
    background: var(--el-text-color-placeholder);
    flex-shrink: 0;
}

.page-name {
    flex-shrink: 0;
    font-size: 13px;
    font-weight: 500;
    color: var(--el-text-color-primary);
}

.code {
    overflow: hidden;
    color: var(--el-text-color-placeholder);
    font-family: monospace;
    font-size: 11px;
    white-space: nowrap;
    text-overflow: ellipsis;
}

.access-cell .access-inner {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    width: 100%;
}

.access-label {
    font-size: 12px;
    color: var(--el-text-color-secondary);
    white-space: nowrap;

    &.on {
        color: var(--el-color-primary);
        font-weight: 500;
    }
}

.btn-chips {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 2px 14px;
}

.btn-chip {
    /* 固定宽度：跨行对齐成列（覆盖最长项“删除xx 高危”约 118px） */
    width: 120px;
    flex: none;
    margin-right: 0;

    :deep(.el-checkbox__label) {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding-left: 6px;
        font-size: 13px;
        white-space: nowrap;
    }

    &:has(.risk-tag) :deep(.el-checkbox__label) {
        color: var(--el-color-danger);
    }
}

.risk-tag {
    flex-shrink: 0;
}

.no-btn {
    font-size: 12px;
    color: var(--el-text-color-placeholder);
}

.matrix-empty {
    padding: 48px 12px !important;
    text-align: center;
    color: var(--el-text-color-secondary);
    font-size: 13px;
}

// 右栏
.changes-summary {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 12px;
    border-bottom: 1px solid var(--el-border-color-lighter);
    font-size: 12px;
    color: var(--el-text-color-secondary);
}

.changes-scroll {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
    padding: 10px;
}

.change-card {
    margin-bottom: 8px;
    padding: 8px 20px;
    border: 1px solid var(--el-border-color-lighter);
    border-radius: var(--el-border-radius-base);
    background: #fff;

    .change-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 8px;
    }

    .change-tag {
        padding: 1px 6px;
        border-radius: 3px;
        font-size: 11px;
        font-weight: 600;

        &.added {
            background: rgba(103, 194, 58, 0.12);
            color: var(--el-color-success);
        }

        &.removed {
            background: rgba(245, 108, 108, 0.12);
            color: var(--el-color-danger);
        }
    }

    .change-code {
        overflow: hidden;
        color: var(--el-text-color-placeholder);
        font-family: monospace;
        font-size: 11px;
        white-space: nowrap;
        text-overflow: ellipsis;
    }

    .change-title {
        margin-top: 4px;
        font-size: 13px;
        color: var(--el-text-color-primary);
        word-break: break-all;
    }
}

// 窄屏纵向堆叠
@media (max-width: 1360px) {
    .workspace {
        flex-direction: column;
    }

    .changes-panel {
        width: auto;
        max-height: 260px;
    }
}
</style>

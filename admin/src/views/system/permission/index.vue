<template>
    <div class="permission-page">
        <!-- 左栏：角色列表 -->
        <div class="role-panel">
            <div class="panel-header">角色列表</div>
            <div class="role-list">
                <div v-for="role in roleList" :key="role.code" class="role-item"
                    :class="{ active: selectedRole === role.code }" @click="selectRole(role.code)">
                    <span>{{ role.name }}</span>
                </div>
            </div>
        </div>

        <!-- 中栏：权限树 -->
        <div class="perm-panel">
            <div v-if="selectedRole" class="panel-header">{{ currentRoleName }} - 权限配置</div>
            <div v-else class="panel-header">权限配置</div>
            <div v-if="!selectedRole" class="empty-tip">
                <p>请从左侧选择一个角色进行权限设置</p>
            </div>
            <div v-else class="perm-body">
                <el-tree ref="permTreeRef" :data="permTreeData" node-key="id"
                    :props="{ label: 'name', children: 'children' }" default-expand-all>
                    <template #default="{ data }">
                        <div class="custom-node" :class="{ 'is-root': data.code === 'root' }">
                            <template v-if="data.code === 'root'">
                                <span class="root-label">{{ data.name }}</span>
                            </template>
                            <template v-else>
                                <el-checkbox v-model="data.checked"
                                    @change="(val: boolean) => onCheck(data, val)" @click.stop>
                                    <el-tooltip :content="data.code" placement="right" :show-after="300">
                                        <span class="node-label">{{ data.name }}</span>
                                    </el-tooltip>
                                </el-checkbox>
                            </template>
                        </div>
                    </template>
                </el-tree>
            </div>
        </div>

        <!-- 右栏：修改记录 -->
        <div class="changes-panel">
            <div class="panel-header">修改记录</div>
            <template v-if="!selectedRole">
                <div class="empty-tip">
                    <p>暂无修改</p>
                </div>
            </template>
            <template v-else-if="!hasChanges">
                <div class="empty-tip">
                    <p>暂无修改</p>
                </div>
            </template>
            <template v-else>
                <div class="changes-summary">
                    <el-tag size="small" type="success">+{{ changes.added.length }}</el-tag>
                    <el-tag size="small" type="danger">-{{ changes.removed.length }}</el-tag>
                    <span class="changes-spacer" />
                    <el-button size="small" :icon="Refresh" @click="resetPerm">重置</el-button>
                    <Permission code="SYSTEM_PERMISSION_EDIT"><el-button size="small" type="primary" :icon="Check" :loading="saving"
                        @click="savePerm">保存</el-button></Permission>
                </div>
                <div class="changes-scroll">
                    <div v-for="item in changes.added" :key="item.code" class="change-item added">
                        <el-icon>
                            <Plus />
                        </el-icon>
                        <span class="change-path">{{ item.path }}</span>
                        <span class="change-code">{{ item.code }}</span>
                    </div>
                    <div v-for="item in changes.removed" :key="item.code" class="change-item removed">
                        <el-icon>
                            <Minus />
                        </el-icon>
                        <span class="change-path">{{ item.path }}</span>
                        <span class="change-code">{{ item.code }}</span>
                    </div>
                </div>
            </template>
        </div>
    </div>
</template>

<script setup lang="ts" name="permission">
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh, Check, Plus, Minus } from '@element-plus/icons-vue'
import { permissionApi } from '@/api/modules/permission'
import { roleApi } from '@/api/modules/role'
import Permission from '@/components/Permission/index.vue'



// ── 类型 ──

interface PermNode {
    id: string
    code: string
    name: string
    checked: boolean
    children?: PermNode[]
}

// ── 角色 ──

interface RoleItem {
    id: string
    name: string
    code: string
}

const roleList = ref<RoleItem[]>([])
const selectedRole = ref('')
const currentRoleName = computed(() =>
    roleList.value.find(r => r.code === selectedRole.value)?.name || ''
)

// ── 树 ──

const permTreeData = ref<PermNode[]>([])

// 缓存角色权限（扁平 code 数组）
const rolePerms = reactive<Record<string, string[]>>({})

const hasChanges = ref(false)
const saving = ref(false)
let snapshot = ''
let savedCodes: string[] = []

// 根据 code 从树中查找层级路径（如 "商户管理 / 新增"）
const getPathByCode = (code: string): string => {
    const segs: string[] = []
    const find = (nodes: PermNode[]): boolean => {
        for (const n of nodes) {
            if (n.code === code) { segs.unshift(n.name); return true }
            if (n.children && find(n.children)) {
                if (n.code !== 'root') segs.unshift(n.name)
                return true
            }
        }
        return false
    }
    find(permTreeData.value)
    return segs.join(' / ')
}

const changes = computed(() => {
    if (!hasChanges.value) return { added: [], removed: [], total: 0 }
    const current = collectCodes()
    const curSet = new Set(current)
    const savSet = new Set(savedCodes)
    const added = current
        .filter(c => !savSet.has(c))
        .map(c => ({ code: c, path: getPathByCode(c) }))
    const removed = savedCodes
        .filter(c => !curSet.has(c))
        .map(c => ({ code: c, path: getPathByCode(c) }))
    return { added, removed, total: added.length + removed.length }
})

const walk = (nodes: PermNode[], fn: (n: PermNode) => void) => {
    for (const n of nodes) { fn(n); if (n.children) walk(n.children, fn) }
}

// 采集勾选的 code（扁平化）
const collectCodes = (): string[] => {
    const codes: string[] = []
    const scan = (nodes: PermNode[]) => {
        for (const n of nodes) {
            if (n.code === 'root') { scan(n.children || []); continue }
            if (n.checked) codes.push(n.code)
            if (n.children) scan(n.children)
        }
    }
    scan(permTreeData.value)
    return codes
}

const checkChanged = () => {
    const cur = JSON.stringify([...collectCodes()].sort())
    hasChanges.value = cur !== snapshot
}

// ============================================================
//  勾选
// ============================================================

const onCheck = (data: PermNode, val: boolean) => {
    checkChanged()
}

// ============================================================
//  角色切换
// ============================================================

const selectRole = (role: string) => {
    selectedRole.value = role
    nextTick(() => applyPerm(role))
}

const applyPerm = (role: string) => {
    walk(permTreeData.value, n => { n.checked = false })
    const codes = new Set(rolePerms[role] || [])
    const scan = (nodes: PermNode[]) => {
        for (const n of nodes) {
            if (n.code !== 'root' && codes.has(n.code)) n.checked = true
            if (n.children) scan(n.children)
        }
    }
    scan(permTreeData.value)

    snapshot = JSON.stringify([...collectCodes()].sort())
    savedCodes = [...collectCodes()].sort()
    hasChanges.value = false
}

// ============================================================
//  保存 & 重置
// ============================================================

const savePerm = async () => {
    saving.value = true
    try {
        const permissions = collectCodes()
        await permissionApi.updateRolePermission({
            role: selectedRole.value,
            permissions
        })
        rolePerms[selectedRole.value] = [...permissions]
        savedCodes = [...permissions].sort()
        snapshot = JSON.stringify(savedCodes)
        hasChanges.value = false
        ElMessage.success('保存成功')
    } catch {
        ElMessage.error('保存失败')
    } finally {
        saving.value = false
    }
}

const resetPerm = () => {
    applyPerm(selectedRole.value)
    ElMessage.info('已重置')
}

// ============================================================
//  初始化
// ============================================================

const loadTree = async () => {
    try {
        // 从数据库加载角色列表
        const roleRes = await roleApi.getRoleList({ page: 1, pageSize: 100 })
        const roles = (roleRes as any).data?.list || []
        roleList.value = roles.map((r: any) => ({
            id: r.id,
            name: r.name,
            code: r.code
        }))

        // 加载权限树
        const res = await permissionApi.getPermissionTree()
        const inject = (nodes: any[]): PermNode[] =>
            nodes.map(n => ({
                id: n.id, code: n.code, name: n.name,
                checked: false,
                children: n.children ? inject(n.children) : undefined
            }))
        permTreeData.value = inject((res as any).data || [])

        // 加载角色权限
        const permRes = await permissionApi.getPermissionList()
        const data = (permRes as any).data || {}
        for (const [role, p] of Object.entries(data)) {
            rolePerms[role] = (p as any).permissions || []
        }

        if (roleList.value.length > 0 && !selectedRole.value) {
            selectedRole.value = roleList.value[0].code
        }
        if (selectedRole.value) {
            nextTick(() => applyPerm(selectedRole.value))
        }
    } catch (error) {
        console.error('加载权限数据失败:', error)
        ElMessage.error('加载权限数据失败')
    }
}

onMounted(loadTree)
</script>

<style scoped lang="scss">
.permission-page {
    display: flex;
    height: 100%;
    gap: 16px;
}

// 左栏
.role-panel {
    width: 200px;
    min-width: 200px;
    background: #fff;
    border-radius: 6px;
    border: 1px solid var(--el-border-color-light);
    display: flex;
    flex-direction: column;
    overflow: hidden;

    .panel-header {
        padding: 16px;
        font-size: 15px;
        font-weight: 600;
        border-bottom: 1px solid var(--el-border-color-light);
        background: var(--el-fill-color-light);
    }

    .role-list {
        flex: 1;
        padding: 8px 0;
    }

    .role-item {
        padding: 12px 16px;
        cursor: pointer;
        font-size: 14px;
        border-left: 3px solid transparent;

        &:hover {
            background: var(--el-fill-color-light);
        }

        &.active {
            background: var(--el-color-primary-light-9);
            color: var(--el-color-primary);
            border-left-color: var(--el-color-primary);
            font-weight: 600;
        }
    }
}

// 中栏：权限树
.perm-panel {
    flex: 1;
    min-width: 0;
    background: #fff;
    border-radius: 6px;
    border: 1px solid var(--el-border-color-light);
    display: flex;
    flex-direction: column;
    overflow: hidden;

    .panel-header {
        padding: 16px;
        font-size: 15px;
        font-weight: 600;
        border-bottom: 1px solid var(--el-border-color-light);
        background: var(--el-fill-color-light);
    }

    .perm-body {
        flex: 1;
        overflow-y: auto;
        padding: 16px 24px;
    }
}

// 右栏：修改记录
.changes-panel {
    flex: 1;
    min-width: 0;
    background: #fff;
    border-radius: 6px;
    border: 1px solid var(--el-border-color-light);
    display: flex;
    flex-direction: column;
    overflow: hidden;

    .panel-header {
        padding: 16px;
        font-size: 15px;
        font-weight: 600;
        border-bottom: 1px solid var(--el-border-color-light);
        background: var(--el-fill-color-light);
    }

    .changes-summary {
        display: flex;
        align-items: center;
        gap: 6px;
        padding: 10px 12px;
        border-bottom: 1px solid var(--el-border-color-light);

        .changes-spacer {
            flex: 1;
        }
    }

    .changes-scroll {
        flex: 1;
        overflow-y: auto;
        padding: 8px 0;
    }

    .change-item {
        display: flex;
        align-items: center;
        gap: 6px;
        padding: 6px 16px;
        font-size: 13px;
        line-height: 1.4;

        .el-icon {
            font-size: 13px;
            flex-shrink: 0;
        }

        .change-path {
            word-break: break-all;
        }

        .change-code {
            margin-left: auto;
            font-size: 11px;
            color: var(--el-text-color-placeholder);
            font-family: monospace;
            flex-shrink: 0;
        }

        &.added {
            color: var(--el-color-success);
        }

        &.removed {
            color: var(--el-color-danger);
        }
    }
}

.empty-tip {
    display: flex;
    align-items: center;
    justify-content: center;
    flex: 1;
    color: var(--el-text-color-secondary);
    font-size: 14px;
}


// 树节点样式
.custom-node {
    display: flex;
    align-items: center;
    padding: 4px 0;

    .el-checkbox {
        margin-right: 4px;
    }

    .node-label {
        font-size: 14px;
    }
}

.is-root {
    font-weight: 600;
    font-size: 15px;
    padding: 6px 0 2px 0;

    .root-label {
        color: var(--el-text-color-primary);
        padding: 4px 0;
    }
}
</style>








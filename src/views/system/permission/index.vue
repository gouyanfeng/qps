<template>
    <div class="permission-page">
        <!-- 左栏：角色列表 -->
        <div class="role-panel">
            <div class="panel-header">角色列表</div>
            <div class="role-list">
                <div v-for="role in roleList" :key="role.value"
                    class="role-item"
                    :class="{ active: selectedRole === role.value }"
                    @click="selectRole(role.value)">
                    <span>{{ role.label }}</span>
                </div>
            </div>
        </div>

        <!-- 右栏 -->
        <div class="perm-panel">
            <div v-if="!selectedRole" class="empty-tip">
                <p>请从左侧选择一个角色进行权限设置</p>
            </div>

            <template v-else>
                <!-- 顶栏 -->
                <div class="perm-header">
                    <div class="perm-header-left">
                        <span class="perm-title">{{ currentRoleName }} - 权限配置</span>
                        <el-tag v-if="hasChanges" type="warning" size="small">有未保存的更改</el-tag>
                    </div>
                    <div class="perm-header-right">
                        <el-button :icon="Refresh" @click="resetPerm">重置</el-button>
                        <el-button type="primary" :icon="Check" :loading="saving" @click="savePerm">保存</el-button>
                    </div>
                </div>

                <el-divider />

                <!-- 权限树（只读勾选） -->
                <el-tree
                    ref="permTreeRef"
                    :data="permTreeData"
                    node-key="id"
                    :props="{ label: 'label', children: 'children' }"
                    default-expand-all
                >
                    <template #default="{ data }">
                        <div class="custom-node" :class="{ 'is-root': data.code === '_root' }">
                            <template v-if="data.code === '_root'">
                                <span class="root-label">{{ data.label }}</span>
                            </template>
                            <template v-else>
                                <el-checkbox
                                    v-model="data.checked"
                                    :indeterminate="hasChildren(data) && isPartial(data)"
                                    @change="(val: boolean) => onCheck(data, val)"
                                >
                                    <span class="node-label">{{ data.label }}</span>
                                </el-checkbox>
                            </template>
                        </div>
                    </template>
                </el-tree>
            </template>
        </div>
    </div>
</template>

<script setup lang="ts" name="permission">
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh, Check } from '@element-plus/icons-vue'
import { permissionApi } from '@/api/modules/permission'

// ── 类型 ──

interface PermNode {
    id: string
    code: string
    label: string
    checked: boolean
    children?: PermNode[]
}

// ── 角色 ──

const roleList = [
    { label: '管理员', value: 'admin' },
    { label: '商户', value: 'merchant' },
    { label: '用户', value: 'user' }
]

const selectedRole = ref('')
const currentRoleName = computed(() =>
    roleList.find(r => r.value === selectedRole.value)?.label || ''
)

// ── 树 ──

const permTreeData = ref<PermNode[]>([])
const permTreeRef = ref()

// 缓存角色权限（扁平 code 数组）
const rolePerms = reactive<Record<string, string[]>>({})

const hasChanges = ref(false)
const saving = ref(false)
let snapshot = ''

// ============================================================
//  工具
// ============================================================

const walk = (nodes: PermNode[], fn: (n: PermNode) => void) => {
    for (const n of nodes) { fn(n); if (n.children) walk(n.children, fn) }
}

const hasChildren = (n: PermNode) => !!n.children?.length

const isPartial = (n: PermNode) => {
    if (!n.children?.length) return false
    const checked = n.children.filter(c => c.checked).length
    return checked > 0 && checked < n.children.length
}

// 自底向上刷新所有父节点的 checked 状态
const refreshParents = () => {
    const sync = (nodes: PermNode[]): void => {
        for (const n of nodes) {
            if (n.children) {
                sync(n.children)
                if (n.code !== '_root') {
                    n.checked = n.children.every(c => c.checked)
                }
            }
        }
    }
    sync(permTreeData.value)
}

// 采集勾选的 code（扁平化）
const collectCodes = (): string[] => {
    const codes: string[] = []
    const scan = (nodes: PermNode[]) => {
        for (const n of nodes) {
            if (n.code === '_root') { scan(n.children || []); continue }
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
    if (hasChildren(data)) {
        walk(data.children!, c => { c.checked = val })
    }
    refreshParents()
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
            if (n.code !== '_root' && codes.has(n.code)) n.checked = true
            if (n.children) scan(n.children)
        }
    }
    scan(permTreeData.value)

    snapshot = JSON.stringify([...collectCodes()].sort())
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
        snapshot = JSON.stringify([...permissions].sort())
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
        const res = await permissionApi.getPermissionTree()
        const inject = (nodes: any[]): PermNode[] =>
            nodes.map(n => ({
                id: n.id, code: n.code, label: n.label,
                checked: false,
                children: n.children ? inject(n.children) : undefined
            }))
        permTreeData.value = inject((res as any).data || [])

        const permRes = await permissionApi.getPermissionList()
        const data = (permRes as any).data || {}
        for (const [role, p] of Object.entries(data)) {
            rolePerms[role] = (p as any).permissions || []
        }

        if (roleList.length > 0 && !selectedRole.value) {
            selectedRole.value = roleList[0].value
        }
        if (selectedRole.value) {
            nextTick(() => applyPerm(selectedRole.value))
        }
    } catch {
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
    .role-list { flex: 1; padding: 8px 0; }

    .role-item {
        padding: 12px 16px;
        cursor: pointer;
        font-size: 14px;
        border-left: 3px solid transparent;
        &:hover { background: var(--el-fill-color-light); }
        &.active {
            background: var(--el-color-primary-light-9);
            color: var(--el-color-primary);
            border-left-color: var(--el-color-primary);
            font-weight: 600;
        }
    }
}

// 右栏
.perm-panel {
    flex: 1;
    background: #fff;
    border-radius: 6px;
    border: 1px solid var(--el-border-color-light);
    padding: 24px;
    overflow-y: auto;
}

.empty-tip {
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100%;
    color: var(--el-text-color-secondary);
    font-size: 14px;
}

.perm-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    .perm-header-left { display: flex; align-items: center; gap: 12px; }
    .perm-title { font-size: 17px; font-weight: 600; }
    .perm-header-right { display: flex; gap: 8px; }
}

// 树节点样式
.custom-node {
    display: flex;
    align-items: center;
    padding: 4px 0;

    .el-checkbox { margin-right: 4px; }
    .node-label { font-size: 14px; }
}

.is-root {
    font-weight: 600;
    font-size: 15px;
    padding: 6px 0 2px 0;
    .root-label { color: var(--el-text-color-primary); padding: 4px 0; }
}
</style>

<template>
  <section class="detail-card contacts-card">
    <div class="section-title section-title-first">
      <h3>联系人</h3>
      <Permission v-if="addPermission" :code="addPermission">
        <el-button type="primary" link :icon="Plus" @click="$emit('add')">新增联系人</el-button>
      </Permission>
    </div>
    <el-table :data="contacts" border>
      <el-table-column label="姓名" width="150">
        <template #default="{ row }">
          {{ row.contactName || "-" }}
          <el-tag v-if="row.isPrimary" size="small" type="success" class="ml8">主</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="phone" label="电话" width="150" />
      <el-table-column label="类型" width="90">
        <template #default="{ row }">{{ row.phoneType || "-" }}</template>
      </el-table-column>
      <el-table-column prop="wechat" label="微信" min-width="140" />
      <el-table-column label="角色" width="130">
        <template #default="{ row }">{{ row.roleName || "-" }}</template>
      </el-table-column>
      <el-table-column prop="remark" label="备注" width="180" show-overflow-tooltip />
      <el-table-column label="状态" width="100">
        <template #default="{ row }">
          <el-tag size="small" :type="row.status === '无效' ? 'danger' : 'success'">{{ row.status || "-" }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="220">
        <template #default="{ row }">
          <Permission v-if="editPermission" :code="editPermission">
            <el-button type="primary" link :icon="Edit" @click="$emit('edit', row)">编辑</el-button>
          </Permission>
          <Permission v-if="primaryPermission" :code="primaryPermission">
            <el-button v-if="!row.isPrimary && row.status !== '无效'" type="primary" link @click="$emit('set-primary', row)">设为主</el-button>
          </Permission>
          <Permission v-if="showStatusAction && statusPermission" :code="statusPermission">
            <el-button type="primary" link @click="$emit('toggle-status', row)">{{ row.status === "无效" ? "启用" : "停用" }}</el-button>
          </Permission>
        </template>
      </el-table-column>
    </el-table>
  </section>
</template>

<script setup lang="ts">
import { Edit, Plus } from "@element-plus/icons-vue";
import Permission from "@/components/Permission/index.vue";

defineProps<{
  contacts: any[];
  addPermission?: string;
  editPermission?: string;
  primaryPermission?: string;
  statusPermission?: string;
  showStatusAction?: boolean;
}>();

defineEmits<{
  (event: "add"): void;
  (event: "edit", row: any): void;
  (event: "set-primary", row: any): void;
  (event: "toggle-status", row: any): void;
}>();
</script>

<style scoped lang="scss">
@import "./List.scss";
</style>
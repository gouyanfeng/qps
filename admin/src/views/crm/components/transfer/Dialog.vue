<template>
  <el-dialog :model-value="modelValue" :title="dialogTitle" width="520px" @update:model-value="handleVisibleChange">
    <el-form :model="form" label-width="90px">
      <el-form-item :label="selectedLabel">
        <span>{{ entityIds.length }} 个</span>
      </el-form-item>
      <el-form-item v-if="mode !== 'RETURN'" label="跟进人">
        <el-select v-model="form.ownerUserId" filterable placeholder="请选择跟进人">
          <el-option v-for="user in ownerOptions" :key="user.id" :label="getUserDisplayName(user)" :value="user.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="备注">
        <el-input v-model="form.remark" type="textarea" :rows="3" placeholder="可选" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">取消</el-button>
      <el-button type="primary" :loading="submitting" @click="submit">保存</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import { ElMessage } from "element-plus";
import { crmHerbBaseApi } from "@/api/modules/crmHerbBase";
import { crmVendorApi } from "@/api/modules/crmVendor";
import { userApi } from "@/api/modules/user";

type CrmEntityType = "CRM_HERB_BASE_SUBJECT" | "CRM_VENDOR";
type TransferMode = "ASSIGN" | "TRANSFER" | "RETURN";

const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    entityType: CrmEntityType | string;
    entityIds: string[];
    mode: TransferMode;
    selectedLabel?: string;
  }>(),
  {
    selectedLabel: "已选对象",
  },
);

const emit = defineEmits<{
  (event: "update:modelValue", value: boolean): void;
  (event: "saved"): void;
}>();

const submitting = ref(false);
const ownerOptions = ref<any[]>([]);
const form = reactive({
  ownerUserId: "",
  remark: "",
});

const dialogTitle = computed(
  () =>
    ({
      ASSIGN: "分配跟进人",
      TRANSFER: "转交跟进人",
      RETURN: "退回待分配池",
    })[props.mode],
);

watch(
  () => [props.modelValue, props.mode],
  async ([visible]) => {
    if (!visible) return;
    Object.assign(form, { ownerUserId: "", remark: "" });
    if (props.mode !== "RETURN") await loadOwnerOptions();
  },
  { immediate: true },
);

const handleVisibleChange = (value: boolean) => {
  emit("update:modelValue", value);
};

const getUserDisplayName = (user: any) => user.realName || user.username || user.name || "-";

const loadOwnerOptions = async () => {
  const res = await userApi.getUserList({ page: 1, pageSize: 100, username: "", realName: "", roleId: "", isActive: true });
  ownerOptions.value = (res.data?.list || []).filter((user: any) => user.isActive);
};

const submit = async () => {
  if (props.entityIds.length === 0) return;
  if (props.mode !== "RETURN" && !form.ownerUserId) {
    ElMessage.warning("请选择跟进人");
    return;
  }

  const request = {
    toOwnerUserId: props.mode === "RETURN" ? null : form.ownerUserId,
    remark: form.remark || undefined,
  };

  submitting.value = true;
  try {
    if (props.entityType === "CRM_VENDOR") {
      await Promise.all(props.entityIds.map(id => crmVendorApi.changeVendorOwner(id, request)));
    } else {
      await Promise.all(props.entityIds.map(id => crmHerbBaseApi.changeSubjectOwner(id, request)));
    }

    ElMessage.success("流转成功");
    emit("update:modelValue", false);
    emit("saved");
  } finally {
    submitting.value = false;
  }
};
</script>

<template>
  <el-dialog :model-value="modelValue" :title="form.id ? '编辑联系人' : '新增联系人'" width="520px" @update:model-value="handleVisibleChange">
    <el-form :model="form" label-width="100px">
      <el-form-item label="姓名">
        <el-input v-model="form.contactName" clearable placeholder="联系人姓名" />
      </el-form-item>
      <el-form-item label="电话">
        <el-input v-model="form.phone" clearable placeholder="联系电话" />
      </el-form-item>
      <el-form-item label="电话类型">
        <el-select v-model="form.phoneType">
          <el-option label="手机" value="手机" />
          <el-option label="座机" value="座机" />
          <el-option label="未知" value="未知" />
        </el-select>
      </el-form-item>
      <el-form-item label="微信">
        <el-input v-model="form.wechat" clearable placeholder="微信号" />
      </el-form-item>
      <el-form-item label="角色">
        <el-select v-model="form.roleName" clearable placeholder="请选择角色">
          <el-option v-for="item in contactRoles" :key="item" :label="item" :value="item" />
        </el-select>
      </el-form-item>
      <el-form-item label="主联系人">
        <el-switch v-model="form.isPrimary" />
      </el-form-item>
      <el-form-item label="备注">
        <el-input v-model="form.remark" type="textarea" :rows="3" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">取消</el-button>
      <el-button type="primary" :loading="submitting" @click="submit">保存</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { reactive, ref, watch } from "vue";
import { ElMessage } from "element-plus";
import { crmHerbBaseApi } from "@/api/modules/crmHerbBase";
import { crmVendorApi } from "@/api/modules/crmVendor";

type CrmEntityType = "CRM_HERB_BASE_SUBJECT" | "CRM_VENDOR";

const props = defineProps<{
  modelValue: boolean;
  entityType?: CrmEntityType | string;
  entityId?: string;
  contact?: any;
}>();

const emit = defineEmits<{
  (event: "update:modelValue", value: boolean): void;
  (event: "saved"): void;
}>();

const submitting = ref(false);
const contactRoles = ["负责人", "采购", "财务", "基地负责人", "合作社负责人", "其他"];
const form = reactive({
  id: "",
  contactName: "",
  phone: "",
  phoneType: "未知",
  wechat: "",
  roleName: "",
  isPrimary: false,
  remark: "",
});

const resetForm = () => {
  Object.assign(form, {
    id: "",
    contactName: "",
    phone: "",
    phoneType: "未知",
    wechat: "",
    roleName: "",
    isPrimary: false,
    remark: "",
  });
};

const fillForm = () => {
  resetForm();
  if (props.contact) {
    Object.assign(form, {
      ...props.contact,
      phoneType: props.contact.phoneType || "未知",
      roleName: props.contact.roleName || "",
      remark: props.contact.remark || "",
    });
  }
};

watch(
  () => [props.modelValue, props.contact],
  ([visible]) => {
    if (visible) fillForm();
  },
  { immediate: true },
);

const handleVisibleChange = (value: boolean) => {
  emit("update:modelValue", value);
};

const isValidPhone = (phone: string) => /^1[3-9]\d{9}$/.test(phone) || /^0\d{2,3}-?\d{7,8}(-\d{1,6})?$/.test(phone);

const submit = async () => {
  if (!props.entityId || !props.entityType) return;

  const phone = form.phone.trim();
  if (!form.contactName.trim() && !phone) {
    ElMessage.error("请填写联系人姓名或电话");
    return;
  }
  if (phone && !isValidPhone(phone)) {
    ElMessage.error("联系电话格式不正确");
    return;
  }

  const request = {
    contactName: form.contactName.trim(),
    phone,
    phoneType: form.phoneType || "未知",
    wechat: form.wechat || "",
    roleName: form.roleName || "",
    isPrimary: form.isPrimary,
    remark: form.remark || "",
  };

  submitting.value = true;
  try {
    if (props.entityType === "CRM_VENDOR") {
      if (form.id) await crmVendorApi.updateVendorContact(props.entityId, form.id, request);
      else await crmVendorApi.createVendorContact(props.entityId, request);
    } else {
      if (form.id) await crmHerbBaseApi.updateSubjectContact(props.entityId, form.id, request);
      else await crmHerbBaseApi.createSubjectContact(props.entityId, request);
    }

    ElMessage.success("联系人已保存");
    emit("update:modelValue", false);
    emit("saved");
  } finally {
    submitting.value = false;
  }
};
</script>

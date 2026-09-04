<template>
  <el-dialog :model-value="modelValue" title="记录沟通" width="560px" @update:model-value="handleVisibleChange">
    <el-form :model="form" label-width="100px">
      <el-form-item label="联系人">
        <el-select v-model="form.contactId" clearable placeholder="可不指定">
          <el-option v-for="contact in availableContacts" :key="contact.id" :label="contact.contactName || contact.phone" :value="contact.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="沟通方式">
        <el-select v-model="form.followType">
          <el-option label="电话" value="电话" />
          <el-option label="微信" value="微信" />
          <el-option label="拜访" value="拜访" />
        </el-select>
      </el-form-item>
      <el-form-item label="沟通结果" required>
        <el-select v-model="form.followResult" placeholder="请选择结果">
          <el-option label="已接通" value="已接通" />
          <el-option label="未接" value="未接" />
          <el-option label="空号" value="空号" />
          <el-option label="有意向" value="有意向" />
          <el-option label="无意向" value="无意向" />
        </el-select>
      </el-form-item>
      <el-form-item label="意向等级">
        <el-select v-model="form.intentLevel" clearable placeholder="意向等级">
          <el-option label="A" value="A" />
          <el-option label="B" value="B" />
          <el-option label="C" value="C" />
        </el-select>
      </el-form-item>
      <el-form-item label="沟通内容">
        <el-input v-model="form.content" type="textarea" :rows="4" placeholder="记录沟通要点" />
      </el-form-item>
      <el-form-item label="下次跟进">
        <el-date-picker
          v-model="form.nextFollowAt"
          type="datetime"
          value-format="YYYY-MM-DDTHH:mm:ss"
          :disabled-date="disablePastFollowDate"
          placeholder="请选择时间"
        />
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
  contacts?: any[];
}>();

const emit = defineEmits<{
  (event: "update:modelValue", value: boolean): void;
  (event: "saved"): void;
}>();

const submitting = ref(false);
const availableContacts = ref<any[]>([]);
const form = reactive({
  contactId: undefined as string | undefined,
  followType: "电话",
  followResult: "",
  intentLevel: "",
  content: "",
  nextFollowAt: "",
});

const resetForm = () => {
  Object.assign(form, {
    contactId: undefined,
    followType: "电话",
    followResult: "",
    intentLevel: "",
    content: "",
    nextFollowAt: "",
  });
};

const loadContacts = async () => {
  if (!props.entityId || !props.entityType) {
    availableContacts.value = [];
    return;
  }

  const source = props.contacts?.length
    ? props.contacts
    : props.entityType === "CRM_VENDOR"
      ? (await crmVendorApi.getVendor(props.entityId)).data?.contacts || []
      : (await crmHerbBaseApi.getSubject(props.entityId)).data?.contacts || [];

  availableContacts.value = source.filter((contact: any) => contact.status !== "无效");
  form.contactId = availableContacts.value.find((contact: any) => contact.isPrimary)?.id;
};

watch(
  () => [props.modelValue, props.entityId, props.entityType],
  async ([visible]) => {
    if (!visible) return;
    resetForm();
    await loadContacts();
  },
  { immediate: true },
);

const handleVisibleChange = (value: boolean) => {
  emit("update:modelValue", value);
};

const disablePastFollowDate = (date: Date) => date.getTime() < new Date().setHours(0, 0, 0, 0);

const submit = async () => {
  if (!props.entityId || !props.entityType) return;
  if (!form.followResult) {
    ElMessage.error("请选择沟通结果");
    return;
  }
  if (form.nextFollowAt && new Date(form.nextFollowAt).getTime() <= Date.now()) {
    ElMessage.error("下次跟进时间必须晚于当前时间");
    return;
  }

  submitting.value = true;
  try {
    const request = {
      ...form,
      contactId: form.contactId || null,
      followType: form.followType || "电话",
      followResult: form.followResult || "",
      nextFollowAt: form.nextFollowAt || null,
    };

    if (props.entityType === "CRM_VENDOR") {
      await crmVendorApi.createVendorFollowRecord(props.entityId, request);
    } else {
      await crmHerbBaseApi.createSubjectFollowRecord(props.entityId, request);
    }

    ElMessage.success("沟通记录已保存");
    emit("update:modelValue", false);
    emit("saved");
  } finally {
    submitting.value = false;
  }
};
</script>

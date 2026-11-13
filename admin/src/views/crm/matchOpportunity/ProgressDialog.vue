<template>
  <el-dialog :model-value="modelValue" title="推进机会" width="560px" @update:model-value="emit('update:modelValue', $event)">
    <el-form :model="form" label-width="108px">
      <el-form-item label="阶段" required>
        <el-select v-model="form.stage" :loading="loadingOptions" placeholder="请选择阶段" style="width: 100%">
          <el-option v-for="stage in availableStages" :key="stage" :label="stage" :value="stage" />
        </el-select>
      </el-form-item>
      <el-form-item label="本次结果" required><el-input v-model="form.currentResult" type="textarea" :rows="3" maxlength="1000" show-word-limit /></el-form-item>
      <el-form-item label="下一步动作" required>
        <el-select v-model="form.nextAction" :loading="loadingOptions" placeholder="请选择下一步动作" style="width: 100%">
          <el-option v-for="action in nextActions" :key="action" :label="action" :value="action" />
        </el-select>
      </el-form-item>
      <el-form-item v-if="form.nextAction === otherAction" label="其他待办说明" required><el-input v-model="form.nextActionRemark" maxlength="500" show-word-limit /></el-form-item>
      <el-form-item label="计划时间" required><el-date-picker v-model="form.nextActionAt" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width: 100%" /></el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">取消</el-button>
      <el-button type="primary" :disabled="!ready" :loading="saving" @click="submit">{{ opportunity?.status === '暂停' ? '恢复并推进' : '确认推进' }}</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import { ElMessage } from "element-plus";
import crmMatchOpportunityApi from "@/api/modules/crm/crmMatchOpportunity";

const props = defineProps<{ modelValue: boolean; opportunity?: any }>();
const emit = defineEmits<{ 'update:modelValue': [value: boolean]; submitted: [] }>();
const otherAction = "其他";
const loadingOptions = ref(false);
const saving = ref(false);
const ready = ref(false);
const options = ref<any>({ stages: [], nextActionsByStage: {} });
const form = reactive({ stage: "", currentResult: "", nextAction: "", nextActionRemark: "", nextActionAt: "" });
const nextActions = computed(() => options.value.nextActionsByStage?.[form.stage] || []);
const availableStages = computed(() => {
  const stages = options.value.stages || [];
  const currentIndex = stages.indexOf(props.opportunity?.stage);
  return currentIndex < 0 ? stages : stages.filter((_: string, index: number) => Math.abs(index - currentIndex) <= 1);
});

const toDateTimeInputValue = (value?: string | null) => value ? value.replace(" ", "T").slice(0, 19) : "";
const loadOptions = async () => {
  loadingOptions.value = true;
  try {
    const result = await crmMatchOpportunityApi.getProgressOptions();
    options.value = result.data || { stages: [], nextActionsByStage: {} };
    return true;
  } catch {
    ElMessage.error("加载推进选项失败");
    return false;
  } finally {
    loadingOptions.value = false;
  }
};
const resetForm = (keepCurrentValues: boolean) => {
  Object.assign(form, {
    stage: props.opportunity?.stage || "匹配核实",
    currentResult: "",
    nextAction: keepCurrentValues ? props.opportunity?.nextAction || "" : "",
    nextActionRemark: keepCurrentValues ? props.opportunity?.nextActionRemark || "" : "",
    nextActionAt: keepCurrentValues ? toDateTimeInputValue(props.opportunity?.nextActionAt) : ""
  });
};
const prepare = async () => {
  const opportunityId = props.opportunity?.id;
  ready.value = false;
  resetForm(false);
  if (await loadOptions() && props.modelValue && props.opportunity?.id === opportunityId) {
    resetForm(true);
    ready.value = true;
  }
};
const submit = async () => {
  if (!props.opportunity?.id || !form.stage || !form.currentResult.trim() || !form.nextAction || !form.nextActionAt) {
    ElMessage.warning("请填写阶段、本次结果、下一步动作和计划时间");
    return;
  }
  if (form.nextAction === otherAction && !form.nextActionRemark.trim()) {
    ElMessage.warning("请填写其他待办说明");
    return;
  }
  saving.value = true;
  try {
    await crmMatchOpportunityApi.advance(props.opportunity.id, {
      stage: form.stage,
      currentResult: form.currentResult.trim(),
      nextAction: form.nextAction,
      nextActionRemark: form.nextActionRemark.trim(),
      nextActionAt: form.nextActionAt
    });
    ElMessage.success(props.opportunity.status === "暂停" ? "撮合机会已恢复并推进" : "撮合机会已推进");
    emit('update:modelValue', false);
    emit('submitted');
  } finally {
    saving.value = false;
  }
};

watch(() => props.modelValue, visible => {
  if (visible) {
    void prepare();
  }
});
watch(() => form.stage, () => {
  if (form.nextAction && !nextActions.value.includes(form.nextAction)) {
    form.nextAction = "";
    form.nextActionRemark = "";
  }
});
watch(() => form.nextAction, action => {
  if (action !== otherAction) form.nextActionRemark = "";
});
</script>

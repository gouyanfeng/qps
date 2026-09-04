<template>
  <el-dialog v-model="dialogVisible" :title="form.id ? '编辑采购需求' : '新增采购需求'" width="min(1600px, calc(100vw - 48px))" destroy-on-close>
    <el-form :model="form" label-width="116px">
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="需求方" required>
            <el-select v-model="form.vendorId" filterable remote clearable :disabled="lockVendor" placeholder="请选择厂商" :remote-method="loadVendorOptions" @change="handleVendorChange">
              <el-option v-for="vendor in vendorOptions" :key="vendor.id" :label="vendor.vendorName" :value="vendor.id" />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="需求联系人">
            <el-select v-model="form.contactId" clearable placeholder="请选择联系人">
              <el-option v-for="contact in contacts" :key="contact.id" :label="contact.contactName || contact.phone" :value="contact.id" />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="采购需求名称" required><el-input v-model="form.demandName" clearable /></el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="提出日期"><el-date-picker v-model="form.demandAt" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" /></el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="期望到货日期"><el-date-picker v-model="form.expectedDeliveryAt" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" clearable /></el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="收货地"><ChinaRegionCascader v-model="receivingRegionPath" placeholder="请选择收货地区" @change="handleReceivingRegionChange" /></el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="详细地址"><el-input v-model="detailedAddress" clearable placeholder="街道、门牌号、仓库等" @input="composeReceivingAddress" /></el-form-item>
        </el-col>
        <el-col :span="24"><el-form-item label="来源网页"><el-input v-model="form.sourceUrl" clearable /></el-form-item></el-col>
        <el-col :span="24"><el-form-item label="总体备注"><el-input v-model="form.remark" type="textarea" :rows="2" /></el-form-item></el-col>
      </el-row>

      <div class="item-header">
        <span>采购明细</span>
        <el-button type="primary" link :icon="Plus" @click="addItem">新增明细</el-button>
      </div>
      <el-table :data="form.items" border size="small" class="item-table">
        <el-table-column label="采购品类" min-width="90">
          <template #default="{ row }">
            <ProductSelect v-model="row.productName" />
          </template>
        </el-table-column>
        <el-table-column label="数量" width="150"><template #default="{ row }"><el-input-number v-model="row.quantity" :min="0" /></template></el-table-column>
        <el-table-column label="单位" width="100"><template #default="{ row }"><el-select v-model="row.quantityUnit" clearable><el-option v-for="unit in quantityUnitOptions" :key="unit" :label="unit" :value="unit" /></el-select></template></el-table-column>
        <el-table-column label="规格要求" min-width="75"><template #default="{ row }"><el-input v-model="row.specification" /></template></el-table-column>
        <el-table-column label="质量要求" min-width="75"><template #default="{ row }"><el-input v-model="row.qualityRequirement" /></template></el-table-column>
        <el-table-column label="目标价格" width="190"><template #default="{ row }"><el-input-number v-model="row.targetPrice" :min="0" :precision="2" /></template></el-table-column>
        <el-table-column label="价格单位" width="120"><template #default="{ row }"><el-select v-model="row.priceUnit" clearable><el-option v-for="unit in priceUnitOptions" :key="unit" :label="unit" :value="unit" /></el-select></template></el-table-column>
        <el-table-column label="明细交期" width="180"><template #default="{ row }"><el-date-picker v-model="row.expectedDeliveryAt" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" clearable /></template></el-table-column>
        <el-table-column label="明细备注" min-width="75"><template #default="{ row }"><el-input v-model="row.remark" /></template></el-table-column>
        <el-table-column label="操作" width="60"><template #default="{ $index }"><el-button type="danger" link @click="removeItem($index)">删除</el-button></template></el-table-column>
      </el-table>
    </el-form>
    <template #footer><el-button @click="dialogVisible = false">取消</el-button><el-button type="primary" :loading="saving" @click="save">保存</el-button></template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import { Plus } from "@element-plus/icons-vue";
import { ElMessage } from "element-plus";
import ChinaRegionCascader from "@/components/ChinaRegionCascader/index.vue";
import ProductSelect from "@/components/ProductSelect/index.vue";
import crmVendorDemandApi from "@/api/modules/crmVendorDemand";
import { crmVendorApi } from "@/api/modules/crmVendor";

const props = withDefaults(defineProps<{ modelValue: boolean; vendorId?: string; demand?: any; lockVendor?: boolean }>(), { vendorId: "", demand: null, lockVendor: false });
const emit = defineEmits<{ "update:modelValue": [value: boolean]; saved: [] }>();
const dialogVisible = computed({ get: () => props.modelValue, set: value => emit("update:modelValue", value) });
const contacts = ref<any[]>([]);
const vendorOptions = ref<any[]>([]);
const saving = ref(false);
const receivingRegionPath = ref<string[]>([]);
const detailedAddress = ref("");
const quantityUnitOptions = ["吨", "公斤", "克", "件", "批"];
const priceUnitOptions = ["元/公斤", "元/克", "元/吨", "元/件", "元/批"];
const createItem = () => ({ productName: "", quantity: undefined as number | undefined, quantityUnit: "", specification: "", qualityRequirement: "", targetPrice: undefined as number | undefined, priceUnit: "", expectedDeliveryAt: "", remark: "" });
const form = reactive<any>({ id: "", vendorId: "", demandName: "", demandAt: "", contactId: undefined, expectedDeliveryAt: "", receivingAddress: "", sourceUrl: "", remark: "", items: [] });
const toDateTimeInputValue = (value?: string | Date | null) => {
  if (!value) return "";
  if (typeof value === "string") return value.replace(" ", "T").slice(0, 19);
  const pad = (part: number) => `${part}`.padStart(2, "0");
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`;
};

const loadVendorOptions = async (keyword = "") => {
  const result = await crmVendorApi.getVendorList({ page: 1, pageSize: 50, keyword });
  vendorOptions.value = result.data?.list || [];
};
const loadContacts = async () => {
  if (!form.vendorId) { contacts.value = []; return; }
  const result = await crmVendorApi.getVendor(form.vendorId);
  const vendor = result.data || {};
  contacts.value = (vendor.contacts || []).filter((contact: any) => contact.status !== "INVALID");
  if (!vendorOptions.value.some(item => item.id === vendor.id)) vendorOptions.value.unshift(vendor);
};
const handleVendorChange = async () => { form.contactId = undefined; await loadContacts(); };
const composeReceivingAddress = () => {
  form.receivingAddress = [...receivingRegionPath.value, detailedAddress.value.trim()].filter(Boolean).join(" / ");
};
const handleReceivingRegionChange = (regions: string[]) => {
  receivingRegionPath.value = regions;
  composeReceivingAddress();
};
const addItem = () => form.items.push(createItem());
const removeItem = (index: number) => form.items.splice(index, 1);
const reset = async () => {
  const demand = props.demand || {};
  Object.assign(form, {
    id: demand.id || "", vendorId: props.vendorId || demand.vendorId || "", demandName: demand.demandName || "", demandAt: demand.demandAt ? toDateTimeInputValue(demand.demandAt) : toDateTimeInputValue(new Date()), contactId: demand.contactId || undefined,
    expectedDeliveryAt: toDateTimeInputValue(demand.expectedDeliveryAt), receivingAddress: demand.receivingAddress || "", sourceUrl: demand.sourceUrl || "", remark: demand.remark || "",
    items: demand.items?.length ? demand.items.map((item: any) => ({ ...createItem(), ...item, expectedDeliveryAt: toDateTimeInputValue(item.expectedDeliveryAt) })) : [createItem()],
  });
  const receivingAddressParts = form.receivingAddress ? form.receivingAddress.split(" / ").filter(Boolean) : [];
  receivingRegionPath.value = receivingAddressParts.slice(0, 3);
  detailedAddress.value = receivingAddressParts.slice(3).join(" / ");
  await loadVendorOptions();
  await loadContacts();
};
  const save = async () => {
    if (!form.vendorId || !form.demandName.trim()) { ElMessage.error("请选择需求方并填写采购需求名称"); return; }
    const items = form.items
      .filter((item: any) =>
        Boolean(item.productName?.trim()) ||
        item.quantity !== undefined && item.quantity !== null ||
        Boolean(item.quantityUnit) ||
        Boolean(item.specification?.trim()) ||
        Boolean(item.qualityRequirement?.trim()) ||
        item.targetPrice !== undefined && item.targetPrice !== null ||
        Boolean(item.priceUnit) ||
        Boolean(item.expectedDeliveryAt) ||
        Boolean(item.remark?.trim()))
      .map((item: any) => ({ id: item.id || null, productName: item.productName || "", quantity: item.quantity, quantityUnit: item.quantityUnit || "", specification: item.specification || "", qualityRequirement: item.qualityRequirement || "", targetPrice: item.targetPrice, priceUnit: item.priceUnit || "", expectedDeliveryAt: item.expectedDeliveryAt || null, remark: item.remark || "" }));
    if (!items.length) { ElMessage.error("请至少填写一条采购明细"); return; }
    saving.value = true;
    try {
      const payload = { vendorId: form.vendorId, demandName: form.demandName.trim(), demandAt: form.demandAt, contactId: form.contactId || null, expectedDeliveryAt: form.expectedDeliveryAt || null, receivingAddress: form.receivingAddress, sourceUrl: form.sourceUrl, remark: form.remark, items };
    if (form.id) await crmVendorDemandApi.update(form.id, payload); else await crmVendorDemandApi.create(payload);
    ElMessage.success("采购需求已保存"); dialogVisible.value = false; emit("saved");
  } finally { saving.value = false; }
};
watch(() => props.modelValue, visible => { if (visible) void reset(); });
</script>

<style scoped>
.item-header { display: flex; align-items: center; justify-content: space-between; margin: 8px 0 12px; font-weight: 600; }
.item-table :deep(.el-input-number), .item-table :deep(.el-select), .item-table :deep(.el-date-editor) { width: 100%; }
.el-form :deep(.el-form-item__label) { white-space: nowrap; }
</style>

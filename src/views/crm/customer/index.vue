<template>
  <div class="customer-page">
    <QueryPage api="/admin/crm/customers" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键词">
            <el-input v-model="searchForm.keyword" clearable placeholder="客户 / 联系人 / 电话" />
          </el-form-item>
          <el-form-item label="主营品类">
            <el-input v-model="searchForm.mainProduct" clearable placeholder="黄芪、当归等" />
          </el-form-item>
          <el-form-item label="类型">
            <el-select v-model="searchForm.customerType" clearable placeholder="客户类型">
              <el-option label="基地" value="基地" />
              <el-option label="合作社" value="合作社" />
              <el-option label="企业" value="企业" />
              <el-option label="流通商" value="流通商" />
              <el-option label="待判断" value="待判断" />
            </el-select>
          </el-form-item>
          <el-form-item label="等级">
            <el-select v-model="searchForm.grade" clearable placeholder="等级">
              <el-option label="A" value="A" />
              <el-option label="B" value="B" />
              <el-option label="C" value="C" />
              <el-option label="无效" value="无效" />
            </el-select>
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" clearable placeholder="状态">
              <el-option label="待联系" value="待联系" />
              <el-option label="跟进中" value="跟进中" />
              <el-option label="已成交" value="已成交" />
              <el-option label="已流失" value="已流失" />
            </el-select>
          </el-form-item>
          <el-form-item label="跟进">
            <el-select v-model="followFilter" clearable placeholder="跟进节奏" @change="applyFollowFilter">
              <el-option label="已逾期" value="overdue" />
              <el-option label="今天" value="today" />
              <el-option label="未来7天" value="next7" />
              <el-option label="未设置" value="none" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <el-button type="primary" :icon="Plus" @click="handleAdd">新增客户</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :row-key="'id'" :row-class-name="getRowClassName" border>
          <el-table-column label="客户名称" min-width="210" fixed="left">
            <template #default="{ row }">
              <div class="cell-main">
                <el-button type="primary" link class="customer-link" @click="openDetail(row)">
                  {{ row.customerName }}
                </el-button>
                <span class="muted">{{ row.sourcePlatform || "-" }} · {{ row.sourceLeadId || "-" }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="主联系人 / 电话" min-width="170">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ row.primaryContactName || "-" }}</span>
                <span class="phone-text">{{ row.primaryContactPhone || "-" }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="类型 / 品类" min-width="150">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ row.customerType || "-" }}</span>
                <span class="muted">{{ row.mainProduct || "-" }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="地区" min-width="150">
            <template #default="{ row }">{{ formatRegion(row) }}</template>
          </el-table-column>
          <el-table-column label="评分 / 等级" width="110">
            <template #default="{ row }">
              <div class="score-cell">
                <strong>{{ row.score ?? 0 }}</strong>
                <el-tag size="small" :type="getGradeType(row.grade)">{{ row.grade || "-" }}</el-tag>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="状态" width="100">
            <template #default="{ row }">
              <el-tag :type="getStatusType(row.status)">{{ row.status || "-" }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="最近沟通" min-width="180">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ row.lastFollowResult || "未沟通" }}</span>
                <span class="muted">{{ formatNullableDate(row.lastFollowAt) }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="下次跟进" min-width="170">
            <template #default="{ row }">
              <span :class="{ overdue: isOverdue(row.nextFollowAt) }">
                {{ formatNullableDate(row.nextFollowAt) }}
              </span>
            </template>
          </el-table-column>
          <el-table-column label="负责人" width="120">
            <template #default="{ row }">{{ row.ownerUserName || "-" }}</template>
          </el-table-column>
          <el-table-column label="操作" width="230" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link :icon="View" @click="openDetail(row)">详情</el-button>
              <el-button type="primary" link :icon="Phone" @click="openFollowDialog(row)">记录沟通</el-button>
              <el-button type="primary" link :icon="Edit" @click="handleEdit(row)">编辑</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑客户' : '新增客户'" width="680px">
      <el-form :model="form" label-width="110px">
        <el-form-item label="客户名称">
          <el-input v-model="form.customerName" placeholder="请输入客户名称" />
        </el-form-item>
        <el-form-item label="客户类型">
          <el-select v-model="form.customerType" placeholder="请选择客户类型">
            <el-option label="基地" value="基地" />
            <el-option label="合作社" value="合作社" />
            <el-option label="企业" value="企业" />
            <el-option label="流通商" value="流通商" />
            <el-option label="待判断" value="待判断" />
          </el-select>
        </el-form-item>
        <el-form-item label="主营品类">
          <el-input v-model="form.mainProduct" placeholder="请输入主营品类" />
        </el-form-item>
        <el-form-item label="等级 / 评分">
          <div class="inline-fields">
            <el-select v-model="form.grade" placeholder="等级">
              <el-option label="A" value="A" />
              <el-option label="B" value="B" />
              <el-option label="C" value="C" />
              <el-option label="无效" value="无效" />
            </el-select>
            <el-input-number v-model="form.score" :min="0" :max="100" />
          </div>
        </el-form-item>
        <el-form-item label="地区">
          <div class="inline-fields">
            <el-input v-model="form.province" placeholder="省份" />
            <el-input v-model="form.city" placeholder="城市" />
            <el-input v-model="form.area" placeholder="区县" />
          </div>
        </el-form-item>
        <el-form-item label="详细地址">
          <el-input v-model="form.address" placeholder="请输入详细地址" />
        </el-form-item>
        <el-form-item label="主联系人">
          <div class="inline-fields">
            <el-input v-model="form.primaryContactName" placeholder="姓名" />
            <el-input v-model="form.primaryContactPhone" placeholder="电话" />
          </div>
        </el-form-item>
        <el-form-item label="来源">
          <div class="inline-fields">
            <el-input v-model="form.sourcePlatform" placeholder="来源平台" />
            <el-input-number v-model="form.sourceLeadId" :min="0" />
          </div>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="form.status" placeholder="请选择状态">
            <el-option label="待联系" value="待联系" />
            <el-option label="跟进中" value="跟进中" />
            <el-option label="已成交" value="已成交" />
            <el-option label="已流失" value="已流失" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input type="textarea" v-model="form.remark" :rows="3" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">{{ isEdit ? "保存" : "创建" }}</el-button>
      </template>
    </el-dialog>

    <el-drawer v-model="detailDrawerVisible" size="80%" :with-header="false" class="customer-drawer">
      <div v-if="currentCustomer" class="drawer-layout">
        <section class="drawer-head">
          <div>
            <div class="eyebrow">{{ currentCustomer.customerType || "客户" }} · {{ currentCustomer.mainProduct || "未填品类" }}</div>
            <h2>{{ currentCustomer.customerName }}</h2>
            <div class="head-meta">
              <el-tag :type="getGradeType(currentCustomer.grade)">等级 {{ currentCustomer.grade || "-" }}</el-tag>
              <el-tag :type="getStatusType(currentCustomer.status)">{{ currentCustomer.status || "-" }}</el-tag>
              <span>评分 {{ currentCustomer.score ?? 0 }}</span>
              <span>{{ formatRegion(currentCustomer) }}</span>
            </div>
          </div>
          <div class="head-actions">
            <el-button type="primary" :icon="Phone" @click="openFollowDialog(currentCustomer)">记录沟通</el-button>
            <el-button :icon="Plus" @click="openContactDialog()">新增联系人</el-button>
            <el-button :icon="Edit" @click="handleEdit(currentCustomer)">编辑资料</el-button>
            <el-button type="success" plain @click="markCustomerStatus('已成交')">标记成交</el-button>
            <el-button type="danger" plain @click="markCustomerStatus('已流失')">标记流失</el-button>
          </div>
        </section>

        <section class="summary-band">
          <div>
            <span class="label">主联系人</span>
            <strong>{{ currentCustomer.primaryContactName || "-" }}</strong>
            <span>{{ currentCustomer.primaryContactPhone || "-" }}</span>
          </div>
          <div>
            <span class="label">最近沟通</span>
            <strong>{{ currentCustomer.lastFollowResult || "未沟通" }}</strong>
            <span>{{ formatNullableDate(currentCustomer.lastFollowAt) }}</span>
          </div>
          <div>
            <span class="label">下次跟进</span>
            <strong :class="{ overdue: isOverdue(currentCustomer.nextFollowAt) }">
              {{ formatNullableDate(currentCustomer.nextFollowAt) }}
            </strong>
          </div>
        </section>

        <section class="drawer-grid">
          <div class="profile-panel">
            <h3>客户资料</h3>
            <el-descriptions :column="2" border>
              <el-descriptions-item label="来源">{{ currentCustomer.sourcePlatform || "-" }}</el-descriptions-item>
              <el-descriptions-item label="来源ID">{{ currentCustomer.sourceLeadId || "-" }}</el-descriptions-item>
              <el-descriptions-item label="地址">{{ currentCustomer.address || "-" }}</el-descriptions-item>
              <el-descriptions-item label="负责人">{{ currentCustomer.ownerUserName || "-" }}</el-descriptions-item>
              <el-descriptions-item label="备注" :span="2">{{ currentCustomer.remark || "-" }}</el-descriptions-item>
            </el-descriptions>

            <div class="section-title">
              <h3>联系人</h3>
              <el-button type="primary" link :icon="Plus" @click="openContactDialog()">新增</el-button>
            </div>
            <el-table :data="contacts" border>
              <el-table-column label="姓名" min-width="120">
                <template #default="{ row }">
                  <span>{{ row.contactName || "-" }}</span>
                  <el-tag v-if="row.isPrimary" size="small" type="success" class="ml8">主</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="phone" label="电话" min-width="130" />
              <el-table-column prop="wechat" label="微信" min-width="120" />
              <el-table-column prop="roleName" label="角色" min-width="120" />
              <el-table-column label="操作" width="190">
                <template #default="{ row }">
                  <el-button type="primary" link :icon="Edit" @click="openContactDialog(row)">编辑</el-button>
                  <el-button v-if="!row.isPrimary && row.status !== '无效'" type="primary" link @click="setPrimaryContact(row)">设为主</el-button>
                  <el-button type="danger" link @click="markContactInvalid(row)">无效</el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>

          <div class="timeline-panel">
            <div class="section-title">
              <h3>沟通记录</h3>
              <el-button type="primary" link :icon="Phone" @click="openFollowDialog(currentCustomer)">记录</el-button>
            </div>
            <el-timeline>
              <el-timeline-item
                v-for="record in followRecords"
                :key="record.id"
                :timestamp="formatNullableDate(record.createdAt)"
                placement="top"
              >
                <div class="follow-item">
                  <div class="follow-title">
                    <strong>{{ record.followResult || "沟通" }}</strong>
                    <el-tag size="small">{{ record.followType || "-" }}</el-tag>
                    <el-tag v-if="record.intentLevel" size="small" type="warning">{{ record.intentLevel }}</el-tag>
                  </div>
                  <p>{{ record.content || "-" }}</p>
                  <span class="muted">
                    {{ record.contactName || "未指定联系人" }} · 下次 {{ formatNullableDate(record.nextFollowAt) }}
                  </span>
                </div>
              </el-timeline-item>
              <el-empty v-if="followRecords.length === 0" description="暂无沟通记录" />
            </el-timeline>
          </div>
        </section>
      </div>
    </el-drawer>

    <el-dialog v-model="contactDialogVisible" :title="contactForm.id ? '编辑联系人' : '新增联系人'" width="520px">
      <el-form :model="contactForm" label-width="100px">
        <el-form-item label="姓名">
          <el-input v-model="contactForm.contactName" placeholder="联系人姓名" />
        </el-form-item>
        <el-form-item label="电话">
          <el-input v-model="contactForm.phone" placeholder="联系电话" />
        </el-form-item>
        <el-form-item label="电话类型">
          <el-select v-model="contactForm.phoneType">
            <el-option label="手机" value="手机" />
            <el-option label="座机" value="座机" />
            <el-option label="未知" value="未知" />
          </el-select>
        </el-form-item>
        <el-form-item label="微信">
          <el-input v-model="contactForm.wechat" placeholder="微信号" />
        </el-form-item>
        <el-form-item label="角色">
          <el-input v-model="contactForm.roleName" placeholder="负责人、采购、财务等" />
        </el-form-item>
        <el-form-item label="主联系人">
          <el-switch v-model="contactForm.isPrimary" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input type="textarea" v-model="contactForm.remark" :rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="contactDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitContact">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="followDialogVisible" title="记录沟通" width="560px">
      <el-form :model="followForm" label-width="100px">
        <el-form-item label="联系人">
          <el-select v-model="followForm.contactId" clearable placeholder="可不指定">
            <el-option v-for="contact in contacts" :key="contact.id" :label="contact.contactName" :value="contact.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="方式">
          <el-select v-model="followForm.followType">
            <el-option label="电话" value="电话" />
            <el-option label="微信" value="微信" />
            <el-option label="拜访" value="拜访" />
          </el-select>
        </el-form-item>
        <el-form-item label="结果">
          <el-select v-model="followForm.followResult" placeholder="请选择结果">
            <el-option label="已接通" value="已接通" />
            <el-option label="未接" value="未接" />
            <el-option label="空号" value="空号" />
            <el-option label="有意向" value="有意向" />
            <el-option label="无意向" value="无意向" />
          </el-select>
        </el-form-item>
        <el-form-item label="意向">
          <el-select v-model="followForm.intentLevel" clearable placeholder="意向等级">
            <el-option label="A" value="A" />
            <el-option label="B" value="B" />
            <el-option label="C" value="C" />
          </el-select>
        </el-form-item>
        <el-form-item label="沟通内容">
          <el-input type="textarea" v-model="followForm.content" :rows="4" placeholder="记录销售跟进要点" />
        </el-form-item>
        <el-form-item label="下次跟进">
          <el-date-picker
            v-model="followForm.nextFollowAt"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm:ss"
            placeholder="选择时间"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="followDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitFollowRecord">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="customer">
import { reactive, ref } from "vue";
import { Edit, Phone, Plus, View } from "@element-plus/icons-vue";
import { ElMessage } from "element-plus";
import QueryPage from "@/components/QueryPage/index.vue";
import { crmCustomerApi } from "@/api/modules/crmCustomer";

interface CustomerDetail {
  id: string;
  parentCustomerId?: string | null;
  customerName: string;
  customerType: string;
  mainProduct: string;
  grade: string;
  score: number;
  province: string;
  city: string;
  area: string;
  address: string;
  lat?: number | null;
  lng?: number | null;
  sourcePlatform: string;
  sourceLeadId?: number | null;
  status: string;
  ownerUserId?: string | null;
  ownerUserName?: string | null;
  remark: string;
  primaryContactName: string;
  primaryContactPhone: string;
  lastFollowAt?: string | null;
  lastFollowResult: string;
  nextFollowAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

const queryPageRef = ref();
const dialogVisible = ref(false);
const detailDrawerVisible = ref(false);
const contactDialogVisible = ref(false);
const followDialogVisible = ref(false);
const isEdit = ref(false);
const followFilter = ref("");
const currentCustomer = ref<CustomerDetail | null>(null);
const contacts = ref<any[]>([]);
const followRecords = ref<any[]>([]);

const searchForm = reactive({
  keyword: "",
  customerName: "",
  customerType: "",
  grade: "",
  status: "",
  mainProduct: "",
  onlyOverdue: undefined as boolean | undefined,
  onlyNoNextFollow: undefined as boolean | undefined,
  nextFollowFrom: "",
  nextFollowTo: "",
});

const form = reactive({
  id: "",
  customerName: "",
  customerType: "",
  mainProduct: "",
  grade: "B",
  score: 0,
  province: "",
  city: "",
  area: "",
  address: "",
  lat: undefined as number | undefined,
  lng: undefined as number | undefined,
  sourcePlatform: "百度地图",
  sourceLeadId: undefined as number | undefined,
  status: "待联系",
  ownerUserId: undefined as string | undefined,
  remark: "",
  parentCustomerId: undefined as string | undefined,
  primaryContactName: "",
  primaryContactPhone: "",
});

const contactForm = reactive({
  id: "",
  contactName: "",
  phone: "",
  phoneType: "未知",
  wechat: "",
  roleName: "",
  isPrimary: false,
  remark: "",
});

const followForm = reactive({
  contactId: undefined as string | undefined,
  followType: "电话",
  followResult: "",
  intentLevel: "",
  content: "",
  nextFollowAt: "",
});

const getStatusType = (status: string) => {
  const types: Record<string, string> = {
    待联系: "info",
    跟进中: "warning",
    已成交: "success",
    已流失: "danger",
  };
  return types[status] || "info";
};

const getGradeType = (grade: string) => {
  const types: Record<string, string> = {
    A: "danger",
    B: "warning",
    C: "info",
    无效: "danger",
  };
  return types[grade] || "info";
};

const formatNullableDate = (date?: string | null) => {
  if (!date) return "-";
  return new Date(date).toLocaleString("zh-CN");
};

const formatRegion = (row: any) => [row.province, row.city, row.area].filter(Boolean).join(" / ") || "-";

const isOverdue = (date?: string | null) => {
  if (!date) return false;
  return new Date(date).getTime() < Date.now();
};

const formatDateParam = (date: Date) => {
  const pad = (num: number) => String(num).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
};

const getRowClassName = ({ row }: any) => {
  if (row.status === "已流失" || row.grade === "无效") return "row-disabled";
  if (isOverdue(row.nextFollowAt)) return "row-overdue";
  return "";
};

const applyFollowFilter = () => {
  searchForm.onlyOverdue = undefined;
  searchForm.onlyNoNextFollow = undefined;
  searchForm.nextFollowFrom = "";
  searchForm.nextFollowTo = "";

  const now = new Date();
  const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const todayEnd = new Date(todayStart);
  todayEnd.setDate(todayEnd.getDate() + 1);
  todayEnd.setSeconds(todayEnd.getSeconds() - 1);

  if (followFilter.value === "overdue") {
    searchForm.onlyOverdue = true;
  } else if (followFilter.value === "none") {
    searchForm.onlyNoNextFollow = true;
  } else if (followFilter.value === "today") {
    searchForm.nextFollowFrom = formatDateParam(todayStart);
    searchForm.nextFollowTo = formatDateParam(todayEnd);
  } else if (followFilter.value === "next7") {
    const sevenDays = new Date(todayStart);
    sevenDays.setDate(sevenDays.getDate() + 7);
    searchForm.nextFollowFrom = formatDateParam(todayStart);
    searchForm.nextFollowTo = formatDateParam(sevenDays);
  }
};

const handleReset = () => {
  Object.assign(searchForm, {
    keyword: "",
    customerName: "",
    customerType: "",
    grade: "",
    status: "",
    mainProduct: "",
    onlyOverdue: undefined,
    onlyNoNextFollow: undefined,
    nextFollowFrom: "",
    nextFollowTo: "",
  });
  followFilter.value = "";
};

const resetCustomerForm = () => {
  Object.assign(form, {
    id: "",
    customerName: "",
    customerType: "",
    mainProduct: "",
    grade: "B",
    score: 0,
    province: "",
    city: "",
    area: "",
    address: "",
    lat: undefined,
    lng: undefined,
    sourcePlatform: "百度地图",
    sourceLeadId: undefined,
    status: "待联系",
    ownerUserId: undefined,
    remark: "",
    parentCustomerId: undefined,
    primaryContactName: "",
    primaryContactPhone: "",
  });
};

const handleAdd = () => {
  isEdit.value = false;
  resetCustomerForm();
  dialogVisible.value = true;
};

const handleEdit = (row: any) => {
  isEdit.value = true;
  Object.assign(form, {
    id: row.id,
    customerName: row.customerName || "",
    customerType: row.customerType || "",
    mainProduct: row.mainProduct || "",
    grade: row.grade || "B",
    score: row.score || 0,
    province: row.province || "",
    city: row.city || "",
    area: row.area || "",
    address: row.address || "",
    lat: row.lat ?? undefined,
    lng: row.lng ?? undefined,
    sourcePlatform: row.sourcePlatform || "",
    sourceLeadId: row.sourceLeadId ?? undefined,
    status: row.status || "待联系",
    ownerUserId: row.ownerUserId || undefined,
    remark: row.remark || "",
    parentCustomerId: row.parentCustomerId || undefined,
    primaryContactName: row.primaryContactName || "",
    primaryContactPhone: row.primaryContactPhone || "",
  });
  dialogVisible.value = true;
};

const reloadList = () => {
  queryPageRef.value?.getTableList();
};

const loadCustomerDetail = async (customerId: string) => {
  const [customerRes, contactsRes, recordsRes] = await Promise.all([
    crmCustomerApi.getCustomer(customerId),
    crmCustomerApi.getContacts(customerId),
    crmCustomerApi.getFollowRecords(customerId),
  ]);
  currentCustomer.value = customerRes.data;
  contacts.value = contactsRes.data || [];
  followRecords.value = recordsRes.data || [];
};

const openDetail = async (row: any) => {
  await loadCustomerDetail(row.id);
  detailDrawerVisible.value = true;
};

const handleSubmit = async () => {
  if (!form.customerName) {
    ElMessage.error("请输入客户名称");
    return;
  }

  if (isEdit.value) {
    await crmCustomerApi.updateCustomer(form.id, form);
    ElMessage.success("保存成功");
  } else {
    await crmCustomerApi.createCustomer(form);
    ElMessage.success("创建成功");
  }

  dialogVisible.value = false;
  reloadList();
  if (currentCustomer.value?.id === form.id) {
    await loadCustomerDetail(form.id);
  }
};

const resetContactForm = () => {
  Object.assign(contactForm, {
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

const openContactDialog = (row?: any) => {
  resetContactForm();
  if (row) Object.assign(contactForm, row);
  contactDialogVisible.value = true;
};

const submitContact = async () => {
  if (!currentCustomer.value) return;
  if (!contactForm.contactName && !contactForm.phone) {
    ElMessage.error("请填写联系人姓名或电话");
    return;
  }

  if (contactForm.id) {
    await crmCustomerApi.updateContact(contactForm.id, contactForm);
  } else {
    await crmCustomerApi.createContact(currentCustomer.value.id, contactForm);
  }

  ElMessage.success("联系人已保存");
  contactDialogVisible.value = false;
  await loadCustomerDetail(currentCustomer.value.id);
  reloadList();
};

const setPrimaryContact = async (row: any) => {
  if (!currentCustomer.value) return;
  await crmCustomerApi.setPrimaryContact(row.id);
  ElMessage.success("主联系人已更新");
  await loadCustomerDetail(currentCustomer.value.id);
  reloadList();
};

const markContactInvalid = async (row: any) => {
  if (!currentCustomer.value) return;
  await crmCustomerApi.updateContactStatus(row.id, { status: "无效", remark: row.remark || "标记为无效" });
  ElMessage.success("联系人已标记无效");
  await loadCustomerDetail(currentCustomer.value.id);
  reloadList();
};

const resetFollowForm = () => {
  Object.assign(followForm, {
    contactId: undefined,
    followType: "电话",
    followResult: "",
    intentLevel: "",
    content: "",
    nextFollowAt: "",
  });
};

const openFollowDialog = async (row: any) => {
  if (!currentCustomer.value || currentCustomer.value.id !== row.id) {
    await loadCustomerDetail(row.id);
  }
  resetFollowForm();
  const primaryContact = contacts.value.find(contact => contact.isPrimary);
  followForm.contactId = primaryContact?.id;
  followDialogVisible.value = true;
};

const submitFollowRecord = async () => {
  if (!currentCustomer.value) return;
  if (!followForm.followResult) {
    ElMessage.error("请选择沟通结果");
    return;
  }

  await crmCustomerApi.createFollowRecord(currentCustomer.value.id, {
    ...followForm,
    nextFollowAt: followForm.nextFollowAt || null,
  });
  ElMessage.success("沟通记录已保存");
  followDialogVisible.value = false;
  await loadCustomerDetail(currentCustomer.value.id);
  reloadList();
};

const markCustomerStatus = async (status: string) => {
  if (!currentCustomer.value) return;
  await crmCustomerApi.updateCustomer(currentCustomer.value.id, {
    ...currentCustomer.value,
    status,
  });
  ElMessage.success("客户状态已更新");
  await loadCustomerDetail(currentCustomer.value.id);
  reloadList();
};
</script>

<style scoped lang="scss">
.customer-page {
  .cell-main {
    display: flex;
    flex-direction: column;
    gap: 4px;
    line-height: 1.35;
  }

  .customer-link {
    align-self: flex-start;
    padding: 0;
    font-weight: 600;
  }

  .muted {
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }

  .phone-text {
    color: var(--el-color-primary);
    font-family: Consolas, monospace;
  }

  .score-cell {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .overdue {
    color: var(--el-color-danger);
    font-weight: 600;
  }

  .inline-fields {
    display: flex;
    width: 100%;
    gap: 10px;

    > * {
      flex: 1;
    }
  }

  :deep(.row-disabled) {
    opacity: 0.58;
  }

  :deep(.row-overdue) {
    background: #fff7f7;
  }
}

.drawer-layout {
  padding: 24px;
}

.drawer-head {
  display: flex;
  justify-content: space-between;
  gap: 24px;
  padding-bottom: 18px;
  border-bottom: 1px solid var(--el-border-color-light);

  h2 {
    margin: 4px 0 10px;
    font-size: 24px;
    font-weight: 700;
  }

  .eyebrow {
    color: var(--el-text-color-secondary);
    font-size: 13px;
  }

  .head-meta,
  .head-actions {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
  }
}

.summary-band {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
  padding: 18px 0;

  > div {
    display: flex;
    min-height: 74px;
    flex-direction: column;
    justify-content: center;
    gap: 5px;
    padding: 12px 14px;
    border: 1px solid var(--el-border-color-light);
    border-radius: 6px;
    background: #f8fafc;
  }

  .label {
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }
}

.drawer-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.25fr) minmax(360px, 0.75fr);
  gap: 22px;
}

.profile-panel,
.timeline-panel {
  min-width: 0;

  h3 {
    margin: 0 0 12px;
    font-size: 16px;
  }
}

.section-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 22px 0 12px;
}

.follow-item {
  padding-bottom: 4px;

  .follow-title {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 6px;
  }

  p {
    margin: 0 0 6px;
    color: var(--el-text-color-primary);
    line-height: 1.55;
  }
}

.ml8 {
  margin-left: 8px;
}

@media (max-width: 960px) {
  .drawer-head,
  .summary-band,
  .drawer-grid {
    display: flex;
    flex-direction: column;
  }
}
</style>

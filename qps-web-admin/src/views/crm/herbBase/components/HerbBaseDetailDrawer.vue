<template>
  <el-drawer :model-value="modelValue" size="80%" :with-header="false" class="customer-drawer" @update:model-value="handleVisibleChange">
    <div v-if="currentHerbBase" class="drawer-layout">
      <section class="drawer-head">
        <div class="head-main">
          <div class="detail-kicker">基地主体详情</div>
          <div class="title-row">
            <h2>{{ currentHerbBase.subjectName || "" }}</h2>
            <el-tag effect="dark">{{ currentHerbBase.status || "-" }}</el-tag>
          </div>
          <div class="head-meta">
            <el-tag>等级 {{ currentHerbBase.grade || "-" }}</el-tag>
            <span>类型 {{ currentHerbBase.subjectType || "-" }}</span>
            <span>评分 {{ currentHerbBase.score ?? 0 }}</span>
            <span>跟进人 {{ currentHerbBase.ownerUserName || "-" }}</span>
            <span :class="{ overdue: isOverdue(currentHerbBase.nextFollowAt) }">下次跟进 {{ formatDate(currentHerbBase.nextFollowAt) }}</span>
            <span>{{ formatRegions(currentHerbBase.regions) }}</span>
          </div>
          <p v-if="currentHerbBase.remark" class="head-remark">{{ currentHerbBase.remark }}</p>
        </div>
        <div class="head-actions">
          <Permission code="CRM_FOLLOW">
            <el-button type="primary" :icon="Phone" @click="openFollowDialog">记录沟通</el-button>
          </Permission>
          <Permission code="CRM_HERB_BASE_CONTACT_ADD">
            <el-button :icon="Plus" @click="openContactDialog()">新增联系人</el-button>
          </Permission>
          <el-button v-if="canManageTransfer" :icon="Edit" @click="openTransferDialog(currentHerbBase.ownerUserId ? 'TRANSFER' : 'ASSIGN')">
            {{ currentHerbBase.ownerUserId ? "转交" : "分配" }}
          </el-button>
          <el-button v-if="canManageTransfer || canReturn(currentHerbBase)" :icon="Edit" @click="openTransferDialog('RETURN')">退回</el-button>
          <Permission code="CRM_HERB_BASE_EDIT">
            <el-button :icon="Edit" @click="openSubjectDialog">编辑主体</el-button>
          </Permission>
          <Permission code="CRM_HERB_BASE_STATUS">
            <el-button type="primary" plain @click="markCustomerStatus('有意向')">标记有意向</el-button>
          </Permission>
          <Permission code="CRM_HERB_BASE_STATUS">
            <el-button type="success" plain @click="markCustomerStatus('已成交')">标记成交</el-button>
          </Permission>
          <Permission code="CRM_HERB_BASE_STATUS">
            <el-button type="danger" plain @click="markCustomerStatus('已流失')">标记流失</el-button>
          </Permission>
          <el-button :icon="Refresh" @click="refreshDetail()">刷新</el-button>
        </div>
      </section>
      <section class="drawer-grid">
        <div class="profile-panel">
          <section class="detail-card">
            <div class="section-title section-title-first">
              <div class="section-heading">
                <h3>基地明细</h3>
                <el-tag size="small" effect="plain">基地数 {{ currentHerbBase.baseCount || 0 }}</el-tag>
                <el-tag size="small" effect="plain">总规模 {{ currentHerbBase.totalScale ?? "-" }} 亩</el-tag>
              </div>
              <Permission code="CRM_HERB_BASE_ADD">
                <el-button type="primary" link :icon="Plus" @click="handleAdd">新增基地</el-button>
              </Permission>
            </div>
            <el-empty v-if="!currentHerbBase.herbBases?.length" description="暂无基地明细" />
            <div v-else class="base-detail-cards">
              <article v-for="base in currentHerbBase.herbBases" :key="base.id" class="base-detail-card">
                <div class="base-card-head">
                  <div>
                    <strong>{{ base.baseName || "-" }}</strong>
                    <div class="muted">{{ [base.province, base.city, base.area].filter(Boolean).join(" / ") || "-" }}　{{ base.address || "-" }}</div>
                  </div>
                  <div class="base-card-actions">
                    <Permission code="CRM_HERB_BASE_SUPPLY_MANAGE">
                      <el-button type="primary" link :icon="Plus" @click="openSupplyDialog(base)">新增供应</el-button>
                    </Permission>
                    <Permission code="CRM_HERB_BASE_EDIT">
                      <el-button type="primary" link :icon="Edit" @click="handleEdit(base)">编辑基地</el-button>
                    </Permission>
                    <Permission code="CRM_HERB_BASE_DELETE">
                      <el-button type="danger" link :icon="Delete" @click="deleteBase(base)">删除基地</el-button>
                    </Permission>
                  </div>
                </div>
                <el-table :data="base.supplies || []" size="small" class="supply-table" empty-text="暂无供应信息">
                  <el-table-column prop="productName" label="品类" min-width="120" />
                  <el-table-column label="可供量" width="130">
                    <template #default="{ row }">{{ row.availableQuantity ?? "-" }}{{ row.availableQuantity != null && row.quantityUnit ? ` ${row.quantityUnit}` : "" }}</template>
                  </el-table-column>
                  <el-table-column prop="specification" label="规格" min-width="120" />
                  <el-table-column prop="supplyCycle" label="供货周期" min-width="120" />
                  <el-table-column label="状态" width="96">
                    <template #default="{ row }"><el-tag size="small">{{ row.isExpired ? "已过期" : row.status || "-" }}</el-tag></template>
                  </el-table-column>
                  <el-table-column label="有效期" width="150">
                    <template #default="{ row }">{{ formatDate(row.validUntil) }}</template>
                  </el-table-column>
                  <el-table-column label="操作" width="126" align="center" class-name="supply-actions-column" header-class-name="supply-actions-column">
                    <template #default="{ row }">
                      <Permission code="CRM_HERB_BASE_SUPPLY_MANAGE">
                        <el-dropdown trigger="click" @command="handleSupplyAction(base, row, String($event))">
                          <el-button class="supply-action-button">
                            操作
                            <el-icon class="el-icon--right"><ArrowDown /></el-icon>
                          </el-button>
                          <template #dropdown>
                            <el-dropdown-menu>
                              <el-dropdown-item command="edit">编辑</el-dropdown-item>
                              <el-dropdown-item v-if="row.id && row.status === '待确认'" command="delete">删除</el-dropdown-item>
                              <el-dropdown-item v-if="row.id" divided command="status:有效">设为有效</el-dropdown-item>
                              <el-dropdown-item v-if="row.id" command="status:暂停">设为暂停</el-dropdown-item>
                              <el-dropdown-item v-if="row.id" command="status:已售罄">设为售罄</el-dropdown-item>
                              <el-dropdown-item v-if="row.id" command="status:已失效">设为失效</el-dropdown-item>
                            </el-dropdown-menu>
                          </template>
                        </el-dropdown>
                      </Permission>
                    </template>
                  </el-table-column>
                </el-table>
              </article>
            </div>
          </section>
          <section class="detail-card">
            <div class="section-title section-title-first">
              <h3>联系人</h3>
              <Permission code="CRM_HERB_BASE_CONTACT_ADD">
                <el-button type="primary" link :icon="Plus" @click="openContactDialog()">新增</el-button>
              </Permission>
            </div>
            <el-table :data="contacts" border>
              <el-table-column label="姓名" width="160">
                <template #default="{ row }">{{ row.contactName || "-" }}<el-tag v-if="row.isPrimary" size="small" type="success" class="ml8">主</el-tag></template>
              </el-table-column>
              <el-table-column prop="phone" label="电话" width="150" />
              <el-table-column prop="wechat" label="微信" min-width="180" />
              <el-table-column label="角色" width="150">
                <template #default="{ row }">{{ row.roleName || "-" }}</template>
              </el-table-column>
              <el-table-column prop="remark" label="备注" min-width="240" />
              <el-table-column label="状态" width="96">
                <template #default="{ row }"><el-tag size="small" :type="row.status === '无效' ? 'danger' : 'success'">{{ row.status || "-" }}</el-tag></template>
              </el-table-column>
              <el-table-column label="操作" width="190">
                <template #default="{ row }">
                  <Permission code="CRM_HERB_BASE_CONTACT_EDIT">
                    <el-button type="primary" link :icon="Edit" @click="openContactDialog(row)">编辑</el-button>
                  </Permission>
                  <Permission code="CRM_HERB_BASE_CONTACT_PRIMARY">
                    <el-button v-if="!row.isPrimary && row.status !== '无效'" type="primary" link @click="setPrimaryContact(row)">设为主</el-button>
                  </Permission>
                </template>
              </el-table-column>
            </el-table>
          </section>
        </div>
        <div class="timeline-panel">
          <section class="detail-card">
            <div class="section-title section-title-first">
              <h3>沟通记录</h3>
              <Permission code="CRM_FOLLOW">
                <el-button type="primary" link :icon="Phone" @click="openFollowDialog">记录</el-button>
              </Permission>
            </div>
            <el-timeline>
              <el-timeline-item v-for="record in followRecords" :key="record.id" :timestamp="formatDate(record.createdAt)" placement="top">
                <div class="follow-item">
                  <strong>{{ record.followResult || "沟通" }}</strong> <el-tag size="small">{{ record.followType || "-" }}</el-tag>
                  <p>{{ record.content || "-" }}</p>
                  <span class="muted">{{ record.contactName || "未指定联系人" }} · 下次 {{ formatDate(record.nextFollowAt) }}</span>
                </div>
              </el-timeline-item>
              <el-empty v-if="!followRecords.length" description="暂无沟通记录" />
            </el-timeline>
          </section>
          <section class="detail-card">
            <div class="section-title section-title-first"><h3>流转记录</h3></div>
            <el-timeline>
              <el-timeline-item v-for="record in transferRecords" :key="record.id" :timestamp="formatDate(record.createdAt)" placement="top">
                <div class="follow-item">
                  <strong>{{ record.actionType || "流转" }}：{{ record.fromOwnerUserName || "未分配" }} 至 {{ record.toOwnerUserName || "未分配" }}</strong>
                  <p v-if="record.remark">{{ record.remark }}</p>
                  <span class="muted">操作人 {{ record.operatorUserName || "-" }}</span>
                </div>
              </el-timeline-item>
              <el-empty v-if="!transferRecords.length" description="暂无流转记录" />
            </el-timeline>
          </section>
        </div>
      </section>
    </div>
  </el-drawer>

  <el-dialog v-model="subjectDialogVisible" title="编辑主体" width="560px">
    <el-form :model="subjectForm" label-width="100px">
      <el-form-item label="主体名称">
        <el-input v-model="subjectForm.subjectName" placeholder="请输入主体名称" />
      </el-form-item>
      <el-form-item label="主体类型">
        <el-input v-model="subjectForm.subjectType" placeholder="请输入主体类型" />
      </el-form-item>
      <el-form-item label="状态">
        <el-select v-model="subjectForm.status" placeholder="请选择状态">
          <el-option label="待联系" value="待联系" />
          <el-option label="跟进中" value="跟进中" />
          <el-option label="有意向" value="有意向" />
          <el-option label="已成交" value="已成交" />
          <el-option label="已流失" value="已流失" />
        </el-select>
      </el-form-item>
      <el-form-item label="备注">
        <el-input type="textarea" v-model="subjectForm.remark" :rows="3" placeholder="请输入备注" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="subjectDialogVisible = false">取消</el-button>
      <el-button type="primary" @click="submitSubject">保存</el-button>
    </template>
  </el-dialog>

  <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑基地' : '新增基地'" width="680px">
    <el-form :model="form" label-width="110px">
      <el-form-item label="基地名称">
        <el-input v-model="form.baseName" placeholder="请输入基地名称" />
      </el-form-item>
      <el-form-item label="规模(亩)">
        <el-input-number v-model="form.scale" :min="0" :precision="2" />
      </el-form-item>
      <el-form-item label="地区">
        <ChinaRegionCascader v-model="regionPath" @change="handleRegionChange" />
      </el-form-item>
      <el-form-item label="详细地址">
        <el-input v-model="form.address" placeholder="请输入详细地址" />
      </el-form-item>
      <el-form-item label="来源">
        <el-select v-model="form.sourcePlatform" placeholder="请选择来源">
          <el-option v-for="item in sourcePlatforms" :key="item" :label="item" :value="item" />
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

  <el-dialog v-model="supplyDialogVisible" :title="supplyForm.id ? '编辑供应信息' : '新增供应信息'" width="720px" class="supply-dialog">
    <el-form :model="supplyForm" label-width="82px" class="supply-form">
      <div class="supply-form-grid">
        <el-form-item label="品类" required class="span-2">
          <ProductSelect v-model="supplyForm.productName" placeholder="请选择品类" />
        </el-form-item>
        <el-form-item label="可供量">
          <div class="inline-field">
            <el-input-number v-model="supplyForm.availableQuantity" :min="0" controls-position="right" placeholder="数量" />
            <el-input v-model="supplyForm.quantityUnit" placeholder="单位" />
          </div>
        </el-form-item>
        <el-form-item label="预期价格">
          <div class="inline-field">
            <el-input-number v-model="supplyForm.expectedPrice" :min="0" controls-position="right" placeholder="价格" />
            <el-input v-model="supplyForm.priceUnit" placeholder="单位" />
          </div>
        </el-form-item>
        <el-form-item label="规格"><el-input v-model="supplyForm.specification" placeholder="如：统货、选货、干品" /></el-form-item>
        <el-form-item label="质量要求"><el-input v-model="supplyForm.qualityRequirement" placeholder="如：水分、含硫、杂质要求" /></el-form-item>
        <el-form-item label="产新期"><el-input v-model="supplyForm.harvestSeason" placeholder="如：9月-11月" /></el-form-item>
        <el-form-item label="供货周期"><el-input v-model="supplyForm.supplyCycle" placeholder="如：现货、预售、长期供应" /></el-form-item>
        <el-form-item label="核实日期"><el-date-picker v-model="supplyForm.confirmedAt" type="date" value-format="YYYY-MM-DD" placeholder="选择日期" /></el-form-item>
        <el-form-item label="有效截止"><el-date-picker v-model="supplyForm.validUntil" type="date" value-format="YYYY-MM-DD" placeholder="选择日期" /></el-form-item>
        <el-form-item label="备注" class="span-2"><el-input v-model="supplyForm.remark" type="textarea" :rows="3" placeholder="补充说明" /></el-form-item>
      </div>
    </el-form>
    <template #footer>
      <el-button @click="supplyDialogVisible = false">取消</el-button>
      <el-button type="primary" @click="submitSupply">保存</el-button>
    </template>
  </el-dialog>

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
        <el-select v-model="contactForm.roleName" clearable placeholder="请选择角色">
          <el-option v-for="item in contactRoles" :key="item" :label="item" :value="item" />
        </el-select>
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
          <el-option label="高" value="高" />
          <el-option label="中" value="中" />
          <el-option label="低" value="低" />
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
          :disabled-date="disablePastFollowDate"
          placeholder="选择时间"
        />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="followDialogVisible = false">取消</el-button>
      <el-button type="primary" @click="submitFollowRecord">保存</el-button>
    </template>
  </el-dialog>

  <el-dialog v-model="transferDialogVisible" :title="transferDialogTitle" width="520px">
    <el-form :model="transferForm" label-width="100px">
      <el-form-item v-if="transferMode !== 'RETURN'" label="跟进人">
        <el-select v-model="transferForm.ownerUserId" placeholder="请选择跟进人">
          <el-option v-for="user in ownerOptions" :key="user.id" :label="getUserDisplayName(user)" :value="user.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="备注">
        <el-input v-model="transferForm.remark" type="textarea" :rows="3" placeholder="请输入流转备注" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="transferDialogVisible = false">取消</el-button>
      <el-button type="primary" @click="submitTransfer">保存</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import { ArrowDown, Delete, Edit, Phone, Plus, Refresh } from "@element-plus/icons-vue";
import { ElMessage, ElMessageBox } from "element-plus";
import ChinaRegionCascader from "@/components/ChinaRegionCascader/index.vue";
import ProductSelect from "@/components/ProductSelect/index.vue";
import Permission from "@/components/Permission/index.vue";
import { crmHerbBaseApi } from "@/api/modules/crmHerbBase";
import { userApi } from "@/api/modules/user";
import { useAuthStore } from "@/stores/modules/auth";
import { useUserStore } from "@/stores/modules/user";

interface HerbBaseSubjectDetail {
  id: string;
  subjectName?: string;
  subjectType?: string;
  productName?: string[];
  grade?: string;
  score?: number;
  status?: string;
  ownerUserId?: string | null;
  ownerUserName?: string | null;
  remark?: string;
  lastFollowAt?: string | null;
  nextFollowAt?: string | null;
  baseCount?: number;
  totalScale?: number | null;
  regions?: string[];
  herbBases?: any[];
  transferRecords?: any[];
}

const props = defineProps<{
  modelValue: boolean;
  subjectId?: string;
}>();

const emit = defineEmits<{
  (event: "update:modelValue", value: boolean): void;
  (event: "refresh-list"): void;
}>();

const authStore = useAuthStore();
const userStore = useUserStore();

const currentHerbBase = ref<HerbBaseSubjectDetail | null>(null);
const contacts = ref<any[]>([]);
const followRecords = ref<any[]>([]);
const transferRecords = ref<any[]>([]);
const subjectDialogVisible = ref(false);
const dialogVisible = ref(false);
const contactDialogVisible = ref(false);
const followDialogVisible = ref(false);
const transferDialogVisible = ref(false);
const supplyDialogVisible = ref(false);
const isEdit = ref(false);
const ownerOptions = ref<any[]>([]);
const regionPath = ref<string[]>([]);
const supplyBaseId = ref("");

const subjectForm = reactive({
  id: "",
  subjectName: "",
  subjectType: "",
  status: "待联系",
  remark: "",
});

const form = reactive({
  id: "",
  baseName: "",
  herbBaseSubjectId: undefined as string | undefined,
  subjectName: "",
  scale: undefined as number | undefined,
  province: "",
  city: "",
  area: "",
  address: "",
  lat: undefined as number | undefined,
  lng: undefined as number | undefined,
  sourcePlatform: "百度地图",
  sourceId: undefined as number | undefined,
  status: "待联系",
  remark: "",
  primaryContactName: "",
  primaryContactPhone: "",
});

const supplyForm = reactive({
  id: "",
  productName: "",
  availableQuantity: undefined as number | undefined,
  quantityUnit: "",
  specification: "",
  qualityRequirement: "",
  harvestSeason: "",
  expectedPrice: undefined as number | undefined,
  priceUnit: "",
  supplyCycle: "",
  confirmedAt: null as string | null,
  validUntil: null as string | null,
  remark: "",
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

const transferForm = reactive({
  entityIds: [] as string[],
  ownerUserId: "",
  remark: "",
});

const transferMode = ref<"ASSIGN" | "TRANSFER" | "RETURN">("TRANSFER");
const canManageTransfer = computed(() => authStore.userPermissions.includes("CRM_TRANSFER"));
const transferDialogTitle = computed(
  () =>
    ({
      ASSIGN: "分配跟进人",
      TRANSFER: "转交跟进人",
      RETURN: "退回待分配池",
    })[transferMode.value],
);

const sourcePlatforms = ["百度地图", "政府网站", "手工录入", "Excel导入", "其他"];
const contactRoles = ["负责人", "采购", "财务", "基地负责人", "合作社负责人", "其他"];

watch(
  () => [props.modelValue, props.subjectId],
  async ([visible, subjectId]) => {
    if (visible && subjectId) await openDetail(String(subjectId));
  },
  { immediate: true },
);

const handleVisibleChange = (value: boolean) => {
  emit("update:modelValue", value);
};

const notifyListChanged = () => {
  emit("refresh-list");
};

const loadCustomerDetail = async (herbBaseSubjectId: string) => {
  const response = await crmHerbBaseApi.getSubject(herbBaseSubjectId);
  currentHerbBase.value = response.data;
  contacts.value = response.data?.contacts || [];
  followRecords.value = response.data?.followRecords || [];
  transferRecords.value = response.data?.transferRecords || [];
};

const openDetail = async (herbBaseSubjectId: string) => {
  currentHerbBase.value = null;
  contacts.value = [];
  followRecords.value = [];
  transferRecords.value = [];
  await loadCustomerDetail(herbBaseSubjectId);
};

const refreshDetail = async (showMessage = true) => {
  if (!currentHerbBase.value) return;
  await loadCustomerDetail(currentHerbBase.value.id);
  if (showMessage) ElMessage.success("基地主体详情已刷新");
};

const openSubjectDialog = () => {
  if (!currentHerbBase.value) return;
  Object.assign(subjectForm, {
    id: currentHerbBase.value.id,
    subjectName: currentHerbBase.value.subjectName || "",
    subjectType: currentHerbBase.value.subjectType || "",
    status: currentHerbBase.value.status || "待联系",
    remark: currentHerbBase.value.remark || "",
  });
  subjectDialogVisible.value = true;
};

const submitSubject = async () => {
  if (!subjectForm.subjectName) {
    ElMessage.error("请输入主体名称");
    return;
  }

  await crmHerbBaseApi.updateSubject(subjectForm.id, {
    subjectName: subjectForm.subjectName,
    subjectType: subjectForm.subjectType,
    status: subjectForm.status || "待联系",
    remark: subjectForm.remark || "",
  });
  ElMessage.success("主体已保存");
  subjectDialogVisible.value = false;
  await loadCustomerDetail(subjectForm.id);
  notifyListChanged();
};

const markCustomerStatus = async (status: string) => {
  if (!currentHerbBase.value) return;
  await crmHerbBaseApi.updateSubject(currentHerbBase.value.id, {
    subjectName: currentHerbBase.value.subjectName || "",
    subjectType: currentHerbBase.value.subjectType || "",
    status,
    remark: currentHerbBase.value.remark || "",
  });
  ElMessage.success("药材基地状态已更新");
  await refreshDetail(false);
  notifyListChanged();
};

const handleRegionChange = (value: string[] | string) => {
  const path = Array.isArray(value) ? value : [];
  form.province = path[0] || "";
  form.city = path[1] || "";
  form.area = path[2] || "";
};

const resetCustomerForm = () => {
  Object.assign(form, {
    id: "",
    baseName: "",
    herbBaseSubjectId: currentHerbBase.value?.id,
    subjectName: currentHerbBase.value?.subjectName || "",
    scale: undefined,
    province: "",
    city: "",
    area: "",
    address: "",
    lat: undefined,
    lng: undefined,
    sourcePlatform: "百度地图",
    sourceId: undefined,
    status: "待联系",
    remark: "",
    primaryContactName: "",
    primaryContactPhone: "",
  });
  regionPath.value = [];
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
    baseName: row.baseName,
    herbBaseSubjectId: row.herbBaseSubjectId || currentHerbBase.value?.id,
    subjectName: row.subjectName || currentHerbBase.value?.subjectName || "",
    scale: row.scale ?? undefined,
    province: row.province || "",
    city: row.city || "",
    area: row.area || "",
    address: row.address || "",
    lat: row.lat ?? undefined,
    lng: row.lng ?? undefined,
    sourcePlatform: row.sourcePlatform || "百度地图",
    sourceId: row.sourceId ?? undefined,
    status: row.status || "待联系",
    remark: row.remark || "",
    primaryContactName: row.primaryContactName || "",
    primaryContactPhone: row.primaryContactPhone || "",
  });
  regionPath.value = [form.province, form.city, form.area].filter(Boolean);
  dialogVisible.value = true;
};

const handleSubmit = async () => {
  if (!form.baseName) {
    ElMessage.error("请输入基地名称");
    return;
  }

  const request = {
    ...form,
    herbBaseSubjectId: form.herbBaseSubjectId || currentHerbBase.value?.id,
    subjectName: currentHerbBase.value?.subjectName || form.subjectName || "",
    sourcePlatform: form.sourcePlatform || "百度地图",
    status: form.status || "待联系",
  };

  if (isEdit.value) {
    await crmHerbBaseApi.updateHerbBase(form.id, request);
    ElMessage.success("保存成功");
  } else {
    await crmHerbBaseApi.createHerbBase(request);
    ElMessage.success("创建成功");
  }

  dialogVisible.value = false;
  await refreshDetail(false);
  notifyListChanged();
};

const deleteBase = async (base: any) => {
  if (!base?.id) return;
  try {
    await ElMessageBox.confirm("删除后不可恢复，确定删除该基地明细吗？", "删除基地明细", {
      confirmButtonText: "确定",
      cancelButtonText: "取消",
      type: "warning",
    });
  } catch {
    return;
  }
  await crmHerbBaseApi.deleteHerbBase(base.id);
  ElMessage.success("基地已删除");
  await refreshDetail(false);
  notifyListChanged();
};

const openSupplyDialog = (base: any, supply?: any) => {
  supplyBaseId.value = base.id;
  Object.assign(supplyForm, {
    id: supply?.id || "",
    productName: supply?.productName || "",
    availableQuantity: supply?.availableQuantity,
    quantityUnit: supply?.quantityUnit || "",
    specification: supply?.specification || "",
    qualityRequirement: supply?.qualityRequirement || "",
    harvestSeason: supply?.harvestSeason || "",
    expectedPrice: supply?.expectedPrice,
    priceUnit: supply?.priceUnit || "",
    supplyCycle: supply?.supplyCycle || "",
    confirmedAt: supply?.confirmedAt || null,
    validUntil: supply?.validUntil || null,
    remark: supply?.remark || "",
  });
  supplyDialogVisible.value = true;
};

const submitSupply = async () => {
  if (!supplyForm.productName) {
    ElMessage.error("请选择品类");
    return;
  }

  const request = { ...supplyForm, confirmedAt: supplyForm.confirmedAt || null, validUntil: supplyForm.validUntil || null };
  delete (request as any).id;
  if (supplyForm.id) await crmHerbBaseApi.updateSupply(supplyForm.id, request);
  else await crmHerbBaseApi.createSupply(supplyBaseId.value, request);

  supplyDialogVisible.value = false;
  ElMessage.success("供应信息已保存");
  await refreshDetail(false);
  notifyListChanged();
};

const deleteSupply = async (supply: any) => {
  if (!supply?.id) return;
  try {
    await ElMessageBox.confirm("删除后不可恢复，确定删除该待确认供应信息吗？", "删除供应信息", {
      confirmButtonText: "确定",
      cancelButtonText: "取消",
      type: "warning",
    });
  } catch {
    return;
  }
  await crmHerbBaseApi.deleteSupply(supply.id);
  ElMessage.success("供应信息已删除");
  await refreshDetail(false);
  notifyListChanged();
};

const changeSupplyStatus = async (supply: any, status: string) => {
  await crmHerbBaseApi.changeSupplyStatus(supply.id, { status });
  ElMessage.success("供应信息状态已更新");
  await refreshDetail(false);
  notifyListChanged();
};

const handleSupplyAction = async (base: any, supply: any, command: string) => {
  if (command === "edit") {
    openSupplyDialog(base, supply);
    return;
  }
  if (command === "delete") {
    await deleteSupply(supply);
    return;
  }
  if (command.startsWith("status:")) {
    await changeSupplyStatus(supply, command.replace("status:", ""));
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
  if (row) {
    Object.assign(contactForm, {
      ...row,
      phoneType: row.phoneType || "未知",
      roleName: row.roleName || "",
    });
  }
  contactDialogVisible.value = true;
};

const isValidPhone = (phone: string) => /^1[3-9]\d{9}$/.test(phone) || /^0\d{2,3}-?\d{7,8}(-\d{1,6})?$/.test(phone);

const submitContact = async () => {
  if (!currentHerbBase.value) return;
  const phone = contactForm.phone.trim();
  if (!contactForm.contactName.trim() && !phone) {
    ElMessage.error("请填写联系人姓名或电话");
    return;
  }
  if (phone && !isValidPhone(phone)) {
    ElMessage.error("联系电话格式不正确");
    return;
  }

  const request = {
    ...contactForm,
    contactName: contactForm.contactName.trim(),
    phone,
    phoneType: contactForm.phoneType || "未知",
    roleName: contactForm.roleName || "",
  };

  if (contactForm.id) await crmHerbBaseApi.updateContact(contactForm.id, request);
  else await crmHerbBaseApi.createSubjectContact(currentHerbBase.value.id, request);

  ElMessage.success("联系人已保存");
  contactDialogVisible.value = false;
  await refreshDetail(false);
  notifyListChanged();
};

const setPrimaryContact = async (row: any) => {
  if (!currentHerbBase.value) return;
  await crmHerbBaseApi.setPrimaryContact(row.id);
  ElMessage.success("主联系人已更新");
  await refreshDetail(false);
  notifyListChanged();
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

const openFollowDialog = () => {
  if (!currentHerbBase.value) return;
  resetFollowForm();
  const primaryContact = contacts.value.find(contact => contact.isPrimary);
  followForm.contactId = primaryContact?.id;
  followDialogVisible.value = true;
};

const submitFollowRecord = async () => {
  if (!currentHerbBase.value) return;
  if (!followForm.followResult) {
    ElMessage.error("请选择沟通结果");
    return;
  }
  if (followForm.nextFollowAt && new Date(followForm.nextFollowAt).getTime() <= Date.now()) {
    ElMessage.error("下次跟进时间必须晚于当前时间");
    return;
  }

  await crmHerbBaseApi.createSubjectFollowRecord(currentHerbBase.value.id, {
    ...followForm,
    followType: followForm.followType || "电话",
    followResult: followForm.followResult || "",
    intentLevel: followForm.intentLevel,
    nextFollowAt: followForm.nextFollowAt || null,
  });
  ElMessage.success("沟通记录已保存");
  followDialogVisible.value = false;
  await refreshDetail(false);
  notifyListChanged();
};

const disablePastFollowDate = (date: Date) => date.getTime() < new Date().setHours(0, 0, 0, 0);

const getUserDisplayName = (user: any) => user.realName || user.username || user.name || "-";

const loadOwnerOptions = async () => {
  const res = await userApi.getUserList({ page: 1, pageSize: 100, username: "", realName: "", roleId: "", isActive: true });
  ownerOptions.value = (res.data?.list || []).filter((user: any) => user.isActive);
};

const openTransferDialog = async (mode: "ASSIGN" | "TRANSFER" | "RETURN" = "TRANSFER") => {
  if (!currentHerbBase.value) return;
  transferMode.value = mode;
  Object.assign(transferForm, {
    entityIds: [currentHerbBase.value.id],
    ownerUserId: "",
    remark: "",
  });
  if (mode !== "RETURN") await loadOwnerOptions();
  transferDialogVisible.value = true;
};

const submitTransfer = async () => {
  if (transferMode.value !== "RETURN" && !transferForm.ownerUserId) {
    ElMessage.warning("请选择跟进人");
    return;
  }

  await crmHerbBaseApi.changeOwner({
    entityIds: [...transferForm.entityIds],
    toOwnerUserId: transferMode.value === "RETURN" ? null : transferForm.ownerUserId,
    remark: transferForm.remark || undefined,
  });
  ElMessage.success("流转成功");
  transferDialogVisible.value = false;
  await refreshDetail(false);
  notifyListChanged();
};

const canReturn = (row?: Partial<HerbBaseSubjectDetail> | null) =>
  !!row?.ownerUserId && row.ownerUserId === userStore.userInfo.userId;

const formatDate = (date?: string | null) => {
  if (!date) return "-";
  return new Date(date).toLocaleString("zh-CN");
};

const formatRegions = (regions?: string[]) => regions?.filter(Boolean).join(" / ") || "-";

const isOverdue = (date?: string | null) => {
  if (!date) return false;
  return new Date(date).getTime() < Date.now();
};

</script>

<style scoped lang="scss">
.drawer-layout {
  min-height: 100%;
  background: #fff;
}

.drawer-head {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 20px;
  padding: 22px 0 18px;
  border-bottom: 1px solid var(--el-border-color-light);
}

.supply-table :deep(.el-table__cell:first-child .cell) {
  padding-left: 16px;
}

.supply-table :deep(.supply-actions-column .cell) {
  padding-left: 14px;
  padding-right: 14px;
  overflow: visible;
  text-overflow: clip;
}

.supply-action-button {
  width: 78px;
}

.title-row,
.head-meta,
.head-actions,
.base-card-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.title-row h2 {
  margin: 0;
  font-size: 25px;
}

.detail-kicker,
.muted,
.head-meta,
.head-remark {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.head-meta {
  margin-top: 10px;
}

.head-actions {
  justify-content: flex-end;
  max-width: 560px;
}

.drawer-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.75fr) minmax(360px, 0.9fr);
  gap: 16px;
  padding: 16px 0 24px;
}

.profile-panel,
.timeline-panel {
  display: grid;
  gap: 14px;
  align-content: start;
  min-width: 0;
}

.detail-card {
  min-width: 0;
  padding: 16px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
}

.section-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin: 0 -16px 18px;
  padding: 0 16px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.section-heading {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.section-title h3 {
  margin: 0;
  font-size: 16px;
}

.base-detail-cards {
  display: grid;
  gap: 14px;
}

.base-detail-card {
  overflow: hidden;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
}

.base-card-head {
  display: flex;
  justify-content: space-between;
  gap: 18px;
  padding: 15px 16px 13px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.follow-item {
  padding-bottom: 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.follow-item p {
  margin: 6px 0;
}

.ml8 {
  margin-left: 8px;
}

.overdue {
  color: var(--el-color-danger);
  font-weight: 600;
}

.supply-dialog :deep(.el-dialog__body) {
  padding: 18px 22px 8px;
}

.supply-dialog :deep(.el-dialog__footer) {
  padding: 14px 22px 18px;
  border-top: 1px solid var(--el-border-color-lighter);
}

.supply-form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  column-gap: 18px;
  row-gap: 2px;
}

.supply-form-grid .span-2 {
  grid-column: 1 / -1;
}

.supply-form :deep(.el-form-item) {
  margin-bottom: 16px;
}

.supply-form :deep(.el-select),
.supply-form :deep(.el-date-editor),
.supply-form :deep(.el-input-number) {
  width: 100%;
}

.inline-field {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 92px;
  gap: 8px;
  width: 100%;
}

.supply-form :deep(.el-textarea__inner) {
  min-height: 82px !important;
}

@media (max-width: 960px) {
  .drawer-head,
  .drawer-grid {
    display: flex;
    flex-direction: column;
  }

  .base-card-head {
    flex-direction: column;
  }

  .supply-form-grid {
    grid-template-columns: 1fr;
  }

  .supply-form-grid .span-2 {
    grid-column: auto;
  }

  .inline-field {
    grid-template-columns: minmax(0, 1fr) 84px;
  }
}
</style>

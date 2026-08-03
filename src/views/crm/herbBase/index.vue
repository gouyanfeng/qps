<template>
  <div class="customer-page">
    <QueryPage api="/admin/crm/herb-base-subjects" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键词">
            <el-input v-model="searchForm.keyword" clearable placeholder="基地 / 主体 / 联系人 / 电话" />
          </el-form-item>
          <el-form-item label="主营品类">
            <el-select v-model="searchForm.mainProducts" multiple collapse-tags collapse-tags-tooltip clearable placeholder="主营品类">
              <el-option v-for="item in mainProductOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
          </el-form-item>
          <el-form-item label="来源">
            <el-select v-model="searchForm.sourcePlatform" clearable placeholder="来源">
              <el-option v-for="item in sourcePlatformOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
          </el-form-item>
          <el-form-item label="等级">
            <el-select v-model="searchForm.grade" clearable placeholder="等级">
              <el-option label="高" value="高" />
              <el-option label="中" value="中" />
              <el-option label="低" value="低" />
              <el-option label="无效" value="INVALID" />
            </el-select>
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" clearable placeholder="状态">
              <el-option label="待联系" value="PENDING" />
              <el-option label="跟进中" value="FOLLOWING" />
              <el-option label="有意向" value="INTERESTED" />
              <el-option label="已成交" value="DEAL" />
              <el-option label="已流失" value="LOST" />
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
        <Permission code="CRM_HERB_BASE_ASSIGN"><el-button :icon="Edit" @click="openAssignDialog()">分配</el-button></Permission>
        <Permission code="CRM_HERB_BASE_ADD"><el-button type="primary" :icon="Plus" @click="handleAdd">新增药材基地</el-button></Permission>
      </template>

      <template #table="{ tableData }">
        <el-table
          :data="tableData"
          :row-key="'id'"
          :row-class-name="getRowClassName"
          :fit="true"
          class="wide-list-table"
          style="--table-min-width: 1860px"
          border
          @selection-change="handleSelectionChange"
        >
          <el-table-column type="selection" width="44" fixed="left" />
          <el-table-column label="基地主体" min-width="220" fixed="left" show-overflow-tooltip>
            <template #default="{ row }">
              <div class="cell-main">
                <el-button type="primary" link class="customer-link" @click="openDetail(row)">
                  {{ row.displayName || row.subjectName || "-" }}
                </el-button>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="基地数" width="86" align="right">
            <template #default="{ row }">{{ row.baseCount || 0 }}</template>
          </el-table-column>
          <el-table-column label="来源" min-width="130" show-overflow-tooltip>
            <template #default="{ row }">{{ formatSourcePlatforms(row.sourcePlatforms) }}</template>
          </el-table-column>
          <el-table-column label="主联系人 / 电话" width="150">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ row.primaryContactName || "-" }}</span>
                <span class="phone-text">{{ row.primaryContactPhone || "-" }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="主营品类" width="156">
            <template #default="{ row }">
              <div v-if="normalizeMainProducts(row).length" class="main-product-tags">
                <el-tag
                  v-for="value in normalizeMainProducts(row)"
                  :key="value"
                  size="small"
                  type="info"
                  effect="plain"
                >
                  {{ formatEnumLabel(mainProductLabels, value) }}
                </el-tag>
              </div>
              <span v-else class="muted">-</span>
            </template>
          </el-table-column>
          <el-table-column label="总规模(亩)" width="124" align="right">
            <template #default="{ row }">{{ formatScale(row.totalScale) }}</template>
          </el-table-column>
          <el-table-column label="地区" min-width="190" show-overflow-tooltip>
            <template #default="{ row }">{{ formatRegions(row.regions) }}</template>
          </el-table-column>
          <el-table-column label="评分 / 等级" width="132">
            <template #default="{ row }">
              <div class="score-cell">
                <strong>{{ row.score ?? 0 }}</strong>
                <el-tag size="small" :type="getGradeType(row.grade)">{{ formatGrade(row.grade) }}</el-tag>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="状态" width="92">
            <template #default="{ row }">
              <el-tag :type="getStatusType(row.status)">{{ formatCustomerStatus(row.status) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="最近沟通" min-width="180">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ formatFollowResult(row.lastFollowResult, "未沟通") }}</span>
                <span class="muted">{{ formatNullableDate(row.lastFollowAt) }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="下次跟进" width="142">
            <template #default="{ row }">
              <span :class="{ overdue: isOverdue(row.nextFollowAt) }">
                {{ formatNullableDate(row.nextFollowAt) }}
              </span>
            </template>
          </el-table-column>
          <el-table-column label="跟进人" width="104" show-overflow-tooltip>
            <template #default="{ row }">{{ row.ownerUserName || "-" }}</template>
          </el-table-column>
          <el-table-column label="操作" width="230" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <div class="table-actions">
                <el-button type="primary" link :icon="View" @click="openDetail(row)">详情</el-button>
                <Permission code="CRM_HERB_BASE_ASSIGN"><el-button type="primary" link :icon="Edit" @click="openAssignDialog([row])">分配</el-button></Permission>
                <Permission code="CRM_HERB_BASE_FOLLOW"><el-button type="primary" link :icon="Phone" @click="openFollowDialog(row)">记录沟通</el-button></Permission>
              </div>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑药材基地' : '新增药材基地'" width="680px">
      <el-form :model="form" label-width="110px">
        <el-form-item label="基地名称">
          <el-input v-model="form.baseName" placeholder="请输入基地名称" />
        </el-form-item>
        <el-form-item label="主体名称">
          <el-input v-model="form.subjectName" placeholder="请输入主体名称" />
        </el-form-item>
        <el-form-item label="主营品类">
          <el-select v-model="form.mainProducts" multiple collapse-tags collapse-tags-tooltip placeholder="请选择主营品类">
            <el-option v-for="item in mainProductOptions" :key="item.value" :label="item.label" :value="item.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="等级 / 评分">
          <div class="inline-fields">
            <el-select v-model="form.grade" placeholder="等级">
              <el-option label="高" value="高" />
              <el-option label="中" value="中" />
              <el-option label="低" value="低" />
              <el-option label="无效" value="INVALID" />
            </el-select>
            <el-input-number v-model="form.score" :min="0" :max="100" />
          </div>
        </el-form-item>
        <el-form-item label="地区">
          <ChinaRegionCascader v-model="regionPath" @change="handleRegionChange" />
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
          <el-select v-model="form.sourcePlatform" placeholder="请选择来源">
            <el-option v-for="item in sourcePlatformOptions" :key="item.value" :label="item.label" :value="item.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="form.status" placeholder="请选择状态">
            <el-option label="待联系" value="PENDING" />
            <el-option label="跟进中" value="FOLLOWING" />
            <el-option label="有意向" value="INTERESTED" />
            <el-option label="已成交" value="DEAL" />
            <el-option label="已流失" value="LOST" />
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
      <div v-if="currentHerbBase" class="drawer-layout">
        <section class="drawer-head">
          <div class="head-main">
            <div class="detail-kicker">基地主体详情</div>
            <div class="title-row">
              <h2>{{ getDetailTitle(currentHerbBase) }}</h2>
              <el-tag :type="getStatusType(currentHerbBase.status)" effect="dark">{{ formatCustomerStatus(currentHerbBase.status) }}</el-tag>
            </div>
            <div class="head-meta">
              <el-tag :type="getGradeType(currentHerbBase.grade)">等级 {{ formatGrade(currentHerbBase.grade) }}</el-tag>
              <span>评分 {{ currentHerbBase.score ?? 0 }}</span>
              <span>跟进人 {{ currentHerbBase.ownerUserName || "-" }}</span>
              <span>{{ formatRegions(currentHerbBase.regions) }}</span>
            </div>
          </div>
          <div class="head-actions">
            <Permission code="CRM_HERB_BASE_FOLLOW"><el-button type="primary" :icon="Phone" @click="openFollowDialog(currentHerbBase)">记录沟通</el-button></Permission>
            <Permission code="CRM_HERB_BASE_CONTACT_ADD"><el-button :icon="Plus" @click="openContactDialog()">新增联系人</el-button></Permission>
            <Permission code="CRM_HERB_BASE_ASSIGN"><el-button :icon="Edit" @click="openAssignDialog([currentHerbBase])">分配</el-button></Permission>
          </div>
        </section>

        <section class="summary-band">
          <div class="summary-card detail-main-products">
            <span class="label">主营品类</span>
            <div v-if="normalizeMainProducts(currentHerbBase).length" class="main-product-tags">
              <el-tag
                v-for="value in normalizeMainProducts(currentHerbBase)"
                :key="value"
                type="info"
                effect="plain"
              >
                {{ formatEnumLabel(mainProductLabels, value) }}
              </el-tag>
            </div>
            <strong v-else>-</strong>
            <span>{{ formatSourcePlatforms(currentHerbBase.sourcePlatforms) }}</span>
          </div>
          <div class="summary-card detail-summary-owner">
            <span class="label">主联系人</span>
            <strong>{{ currentHerbBase.primaryContactName || "-" }}</strong>
            <span>{{ currentHerbBase.primaryContactPhone || "-" }}</span>
          </div>
          <div class="summary-card">
            <span class="label">最近沟通</span>
            <strong>{{ formatFollowResult(currentHerbBase.lastFollowResult, "未沟通") }}</strong>
            <span>{{ formatNullableDate(currentHerbBase.lastFollowAt) }}</span>
          </div>
          <div class="summary-card">
            <span class="label">下次跟进</span>
            <strong :class="{ overdue: isOverdue(currentHerbBase.nextFollowAt) }">
              {{ formatNullableDate(currentHerbBase.nextFollowAt) }}
            </strong>
          </div>
        </section>

        <section class="drawer-grid">
          <div class="profile-panel detail-profile-panel">
            <section class="detail-card">
              <div class="section-title section-title-first">
                <h3>主体资料</h3>
              </div>
              <el-descriptions class="customer-profile-descriptions" :column="3" border>
                <el-descriptions-item label="主体名称" :span="2">{{ currentHerbBase.displayName || currentHerbBase.subjectName || "-" }}</el-descriptions-item>
                <el-descriptions-item label="主体类型">{{ currentHerbBase.subjectType || "-" }}</el-descriptions-item>
                <el-descriptions-item label="跟进人">{{ currentHerbBase.ownerUserName || "-" }}</el-descriptions-item>
                <el-descriptions-item label="主营品类">
                  <div v-if="normalizeMainProducts(currentHerbBase).length" class="main-product-tags">
                    <el-tag
                      v-for="value in normalizeMainProducts(currentHerbBase)"
                      :key="value"
                      size="small"
                      type="info"
                      effect="plain"
                    >
                      {{ formatEnumLabel(mainProductLabels, value) }}
                    </el-tag>
                  </div>
                  <span v-else>-</span>
                </el-descriptions-item>
                <el-descriptions-item label="基地数">{{ currentHerbBase.baseCount || 0 }}</el-descriptions-item>
                <el-descriptions-item label="总规模(亩)">{{ formatScale(currentHerbBase.totalScale) }}</el-descriptions-item>
                <el-descriptions-item label="等级 / 评分">{{ formatGrade(currentHerbBase.grade) }} / {{ currentHerbBase.score ?? 0 }}</el-descriptions-item>
                <el-descriptions-item label="地区" :span="3">{{ formatRegions(currentHerbBase.regions) }}</el-descriptions-item>
                <el-descriptions-item label="来源" :span="3">{{ formatSourcePlatforms(currentHerbBase.sourcePlatforms) }}</el-descriptions-item>
                <el-descriptions-item label="备注" :span="3">{{ currentHerbBase.remark || "-" }}</el-descriptions-item>
              </el-descriptions>
            </section>

            <section class="detail-card">
              <div class="section-title section-title-first"><h3>基地明细</h3></div>
              <el-table :data="currentHerbBase.herbBases || []" border>
                <el-table-column prop="baseName" label="基地名称" min-width="180" show-overflow-tooltip />
                <el-table-column label="品类" min-width="150">
                  <template #default="{ row }">{{ formatMainProducts(row) }}</template>
                </el-table-column>
                <el-table-column label="规模(亩)" width="110" align="right">
                  <template #default="{ row }">{{ formatScale(row.scale) }}</template>
                </el-table-column>
                <el-table-column label="地区" min-width="160">
                  <template #default="{ row }">{{ formatRegion(row) }}</template>
                </el-table-column>
                <el-table-column label="来源" width="100">
                  <template #default="{ row }">{{ formatSourcePlatform(row.sourcePlatform) }}</template>
                </el-table-column>
              </el-table>
            </section>

            <section class="detail-card detail-contacts-panel">
              <div class="section-title section-title-first">
                <h3>联系人</h3>
                <Permission code="CRM_HERB_BASE_CONTACT_ADD"><el-button type="primary" link :icon="Plus" @click="openContactDialog()">新增</el-button></Permission>
              </div>
              <el-table :data="contacts" border>
                <el-table-column label="姓名" width="160">
                  <template #default="{ row }">
                    <span>{{ row.contactName || "-" }}</span>
                    <el-tag v-if="row.isPrimary" size="small" type="success" class="ml8">主</el-tag>
                  </template>
                </el-table-column>
                <el-table-column prop="phone" label="电话" width="150" />
                <el-table-column prop="wechat" label="微信" min-width="180" show-overflow-tooltip />
                <el-table-column label="角色" width="150">
                  <template #default="{ row }">{{ formatContactRole(row.roleName) }}</template>
                </el-table-column>
                <el-table-column prop="remark" label="备注" min-width="180" show-overflow-tooltip />
                <el-table-column label="状态" width="96">
                  <template #default="{ row }">
                    <el-tag size="small" :type="row.status === 'INVALID' ? 'danger' : 'success'">
                      {{ row.status === "INVALID" ? "无效" : "有效" }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="190" class-name="actions-column" header-class-name="actions-column">
                  <template #default="{ row }">
                    <div class="table-actions">
                      <Permission code="CRM_HERB_BASE_CONTACT_EDIT"><el-button type="primary" link :icon="Edit" @click="openContactDialog(row)">编辑</el-button></Permission>
                      <Permission code="CRM_HERB_BASE_CONTACT_PRIMARY"><el-button v-if="!row.isPrimary && row.status !== 'INVALID'" type="primary" link @click="setPrimaryContact(row)">设为主</el-button></Permission>
                    </div>
                  </template>
                </el-table-column>
              </el-table>
            </section>
          </div>

          <div class="timeline-panel">
            <section class="detail-card detail-follow-panel">
              <div class="section-title section-title-first">
                <h3>沟通记录</h3>
                <Permission code="CRM_HERB_BASE_FOLLOW"><el-button type="primary" link :icon="Phone" @click="openFollowDialog(currentHerbBase)">记录</el-button></Permission>
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
                      <strong>{{ formatFollowResult(record.followResult, "沟通") }}</strong>
                      <el-tag size="small">{{ formatFollowType(record.followType) }}</el-tag>
                      <el-tag v-if="record.intentLevel" size="small" type="warning">{{ formatGrade(record.intentLevel) }}</el-tag>
                    </div>
                    <p>{{ record.content || "-" }}</p>
                    <span class="muted">
                      {{ record.contactName || "未指定联系人" }} · 下次 {{ formatNullableDate(record.nextFollowAt) }}
                    </span>
                  </div>
                </el-timeline-item>
                <el-empty v-if="followRecords.length === 0" description="暂无沟通记录" />
              </el-timeline>
            </section>

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
            <el-option label="手机" value="MOBILE" />
            <el-option label="座机" value="LANDLINE" />
            <el-option label="未知" value="UNKNOWN" />
          </el-select>
        </el-form-item>
        <el-form-item label="微信">
          <el-input v-model="contactForm.wechat" placeholder="微信号" />
        </el-form-item>
        <el-form-item label="角色">
          <el-select v-model="contactForm.roleName" clearable placeholder="请选择角色">
            <el-option v-for="item in contactRoleOptions" :key="item.value" :label="item.label" :value="item.value" />
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
            <el-option label="电话" value="PHONE" />
            <el-option label="微信" value="WECHAT" />
            <el-option label="拜访" value="VISIT" />
          </el-select>
        </el-form-item>
        <el-form-item label="结果">
          <el-select v-model="followForm.followResult" placeholder="请选择结果">
            <el-option label="已接通" value="CONNECTED" />
            <el-option label="未接" value="MISSED" />
            <el-option label="空号" value="EMPTY_NUMBER" />
            <el-option label="有意向" value="INTERESTED" />
            <el-option label="无意向" value="NOT_INTERESTED" />
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
            placeholder="选择时间"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="followDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitFollowRecord">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="assignDialogVisible" title="分配负责人" width="520px">
      <el-form :model="assignForm" label-width="100px">
        <el-form-item label="负责人">
          <el-select v-model="assignForm.ownerUserId" placeholder="请选择负责人">
            <el-option label="未分配" value="" />
            <el-option v-for="user in ownerOptions" :key="user.id" :label="getUserDisplayName(user)" :value="user.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="assignForm.remark" type="textarea" :rows="3" placeholder="请输入分配备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="assignDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitAssignOwner">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="customer">
import { onMounted, reactive, ref } from "vue";
import { useRoute } from "vue-router";
import { Edit, Phone, Plus, View } from "@element-plus/icons-vue";
import { ElMessage } from "element-plus";
import QueryPage from "@/components/QueryPage/index.vue";
import ChinaRegionCascader from "@/components/ChinaRegionCascader/index.vue";
import { crmHerbBaseApi } from "@/api/modules/crmHerbBase";
import { userApi } from "@/api/modules/user";
import Permission from "@/components/Permission/index.vue";

interface HerbBaseSubjectDetail {
  id: string;
  subjectName?: string;
  displayName: string;
  subjectType?: string;
  mainProducts: string[];
  grade: string;
  score: number;
  status: string;
  ownerUserId?: string | null;
  ownerUserName?: string | null;
  remark: string;
  primaryContactName: string;
  primaryContactPhone: string;
  lastFollowAt?: string | null;
  lastFollowResult: string;
  nextFollowAt?: string | null;
  baseCount?: number;
  totalScale?: number | null;
  regions?: string[];
  sourcePlatforms?: string[];
  herbBases?: any[];
  createdAt: string;
  updatedAt: string;
}

const queryPageRef = ref();
const route = useRoute();

const dialogVisible = ref(false);
const detailDrawerVisible = ref(false);
const contactDialogVisible = ref(false);
const followDialogVisible = ref(false);
const assignDialogVisible = ref(false);
const isEdit = ref(false);
const followFilter = ref("");
const currentHerbBase = ref<HerbBaseSubjectDetail | null>(null);
const contacts = ref<any[]>([]);
const followRecords = ref<any[]>([]);
const selectedHerbBases = ref<HerbBaseSubjectDetail[]>([]);
const ownerOptions = ref<any[]>([]);
const regionPath = ref<string[]>([]);

const searchForm = reactive({
  keyword: "",
  herbBaseName: "",
  sourcePlatform: "",
  grade: "",
  status: "",
  mainProducts: [] as string[],
  onlyOverdue: undefined as boolean | undefined,
  onlyNoNextFollow: undefined as boolean | undefined,
  nextFollowFrom: "",
  nextFollowTo: "",
  sortField: "CreatedAt",
  sortDirection: "Descending",
});

const form = reactive({
  id: "",
  baseName: "",
  subjectName: "",
  herbBaseName: "",
  mainProducts: [] as string[],
  grade: "B",
  score: 0,
  province: "",
  city: "",
  area: "",
  address: "",
  lat: undefined as number | undefined,
  lng: undefined as number | undefined,
  sourcePlatform: "BAIDU_MAP",
  sourceId: undefined as number | undefined,
  status: "PENDING",
  ownerUserId: undefined as string | undefined,
  remark: "",
  parentId: undefined as string | undefined,
  primaryContactName: "",
  primaryContactPhone: "",
});

const contactForm = reactive({
  id: "",
  contactName: "",
  phone: "",
  phoneType: "UNKNOWN",
  wechat: "",
  roleName: "",
  isPrimary: false,
  remark: "",
});

const followForm = reactive({
  contactId: undefined as string | undefined,
  followType: "PHONE",
  followResult: "",
  intentLevel: "",
  content: "",
  nextFollowAt: "",
});

const assignForm = reactive({
  herbBaseSubjectIds: [] as string[],
  ownerUserId: "",
  remark: "",
});

const sourcePlatformLabels: Record<string, string> = {
  BAIDU_MAP: "百度地图",
  GOV_HERB_BASE: "政府网站",
  MANUAL: "手工录入",
  EXCEL: "Excel导入",
  OTHER: "其他",
  百度地图: "百度地图",
  政府网站: "政府网站",
  手工录入: "手工录入",
  Excel导入: "Excel导入",
  其他: "其他",
};

const sourcePlatformValues: Record<string, string> = {
  BAIDU_MAP: "BAIDU_MAP",
  GOV_HERB_BASE: "GOV_HERB_BASE",
  MANUAL: "MANUAL",
  EXCEL: "EXCEL",
  OTHER: "OTHER",
  百度地图: "BAIDU_MAP",
  政府中药材基地: "GOV_HERB_BASE",
  政府网站: "GOV_HERB_BASE",
  手工录入: "MANUAL",
  Excel导入: "EXCEL",
  其他: "OTHER",
};

const sourcePlatformOptions = [
  { label: "百度地图", value: "BAIDU_MAP" },
  { label: "政府网站", value: "GOV_HERB_BASE" },
  { label: "手工录入", value: "MANUAL" },
  { label: "Excel导入", value: "EXCEL" },
  { label: "其他", value: "OTHER" },
];

const statusLabels: Record<string, string> = {
  PENDING: "待联系",
  FOLLOWING: "跟进中",
  INTERESTED: "有意向",
  DEAL: "已成交",
  LOST: "已流失",
  待联系: "待联系",
  跟进中: "跟进中",
  有意向: "有意向",
  已成交: "已成交",
  已流失: "已流失",
};

const statusValues: Record<string, string> = {
  PENDING: "PENDING",
  FOLLOWING: "FOLLOWING",
  INTERESTED: "INTERESTED",
  DEAL: "DEAL",
  LOST: "LOST",
  待联系: "PENDING",
  跟进中: "FOLLOWING",
  有意向: "INTERESTED",
  已成交: "DEAL",
  已流失: "LOST",
};

const gradeLabels: Record<string, string> = {
  高: "高",
  中: "中",
  低: "低",
  A: "高",
  B: "中",
  C: "低",
  INVALID: "无效",
  无效: "无效",
};

const gradeValues: Record<string, string> = {
  高: "高",
  中: "中",
  低: "低",
  A: "高",
  B: "中",
  C: "低",
  INVALID: "INVALID",
  无效: "INVALID",
};

const mainProductLabels: Record<string, string> = {
  HUANG_QI: "黄芪",
  DANG_GUI: "当归",
  DANG_SHEN: "党参",
  TIAN_MA: "天麻",
  OTHER: "其他",
  黄芪: "黄芪",
  黃芪: "黄芪",
  当归: "当归",
  當歸: "当归",
  党参: "党参",
  黨參: "党参",
  天麻: "天麻",
  多品类: "其他",
  多品類: "其他",
  其他: "其他",
};

const mainProductValues: Record<string, string> = {
  HUANG_QI: "HUANG_QI",
  DANG_GUI: "DANG_GUI",
  DANG_SHEN: "DANG_SHEN",
  TIAN_MA: "TIAN_MA",
  OTHER: "OTHER",
  黄芪: "HUANG_QI",
  黃芪: "HUANG_QI",
  当归: "DANG_GUI",
  當歸: "DANG_GUI",
  党参: "DANG_SHEN",
  黨參: "DANG_SHEN",
  天麻: "TIAN_MA",
  多品类: "OTHER",
  多品類: "OTHER",
  其他: "OTHER",
};

const mainProductOptions = [
  { label: "黄芪", value: "HUANG_QI" },
  { label: "当归", value: "DANG_GUI" },
  { label: "党参", value: "DANG_SHEN" },
  { label: "天麻", value: "TIAN_MA" },
  { label: "其他", value: "OTHER" },
];

const phoneTypeValues: Record<string, string> = {
  MOBILE: "MOBILE",
  LANDLINE: "LANDLINE",
  UNKNOWN: "UNKNOWN",
  手机: "MOBILE",
  座机: "LANDLINE",
  未知: "UNKNOWN",
};

const contactRoleLabels: Record<string, string> = {
  OWNER: "负责人",
  PURCHASE: "采购",
  FINANCE: "财务",
  BASE_OWNER: "基地负责人",
  COOPERATIVE_OWNER: "合作社负责人",
  OTHER: "其他",
  负责人: "负责人",
  采购: "采购",
  财务: "财务",
  基地负责人: "基地负责人",
  合作社负责人: "合作社负责人",
  其他: "其他",
};

const contactRoleValues: Record<string, string> = {
  OWNER: "OWNER",
  PURCHASE: "PURCHASE",
  FINANCE: "FINANCE",
  BASE_OWNER: "BASE_OWNER",
  COOPERATIVE_OWNER: "COOPERATIVE_OWNER",
  OTHER: "OTHER",
  负责人: "OWNER",
  采购: "PURCHASE",
  财务: "FINANCE",
  基地负责人: "BASE_OWNER",
  合作社负责人: "COOPERATIVE_OWNER",
  其他: "OTHER",
};

const contactRoleOptions = [
  { label: "负责人", value: "OWNER" },
  { label: "采购", value: "PURCHASE" },
  { label: "财务", value: "FINANCE" },
  { label: "基地负责人", value: "BASE_OWNER" },
  { label: "合作社负责人", value: "COOPERATIVE_OWNER" },
  { label: "其他", value: "OTHER" },
];

const followTypeLabels: Record<string, string> = {
  PHONE: "电话",
  WECHAT: "微信",
  VISIT: "拜访",
  电话: "电话",
  微信: "微信",
  拜访: "拜访",
};

const followTypeValues: Record<string, string> = {
  PHONE: "PHONE",
  WECHAT: "WECHAT",
  VISIT: "VISIT",
  电话: "PHONE",
  微信: "WECHAT",
  拜访: "VISIT",
};

const followResultLabels: Record<string, string> = {
  CONNECTED: "已接通",
  MISSED: "未接",
  EMPTY_NUMBER: "空号",
  INTERESTED: "有意向",
  NOT_INTERESTED: "无意向",
  已接通: "已接通",
  未接: "未接",
  空号: "空号",
  有意向: "有意向",
  无意向: "无意向",
};

const followResultValues: Record<string, string> = {
  CONNECTED: "CONNECTED",
  MISSED: "MISSED",
  EMPTY_NUMBER: "EMPTY_NUMBER",
  INTERESTED: "INTERESTED",
  NOT_INTERESTED: "NOT_INTERESTED",
  已接通: "CONNECTED",
  未接: "MISSED",
  空号: "EMPTY_NUMBER",
  有意向: "INTERESTED",
  无意向: "NOT_INTERESTED",
};

const formatEnumLabel = (labels: Record<string, string>, value?: string | null, fallback = "-") => {
  if (!value) return fallback;
  return labels[value] || value;
};

const toEnumValue = (values: Record<string, string>, value?: string | null, fallback = "") => {
  if (!value) return fallback;
  return values[value] || value;
};

const normalizeMainProducts = (row: any): string[] => {
  const rawValues: string[] = Array.isArray(row?.mainProducts) ? row.mainProducts : [];

  return Array.from(new Set<string>(
    rawValues
      .map((item: string) => toEnumValue(mainProductValues, item))
      .filter(Boolean)
  ));
};

const formatMainProducts = (row: any, fallback = "-") => {
  const values = normalizeMainProducts(row);
  if (values.length === 0) return fallback;
  return values.map(value => formatEnumLabel(mainProductLabels, value)).join("、");
};

const formatSourcePlatform = (value?: string | null, fallback = "-") => formatEnumLabel(sourcePlatformLabels, value, fallback);
const formatCustomerStatus = (value?: string | null, fallback = "-") => formatEnumLabel(statusLabels, value, fallback);
const formatGrade = (value?: string | null, fallback = "-") => formatEnumLabel(gradeLabels, value, fallback);
const formatContactRole = (value?: string | null, fallback = "-") => formatEnumLabel(contactRoleLabels, value, fallback);
const formatFollowType = (value?: string | null, fallback = "-") => formatEnumLabel(followTypeLabels, value, fallback);
const formatFollowResult = (value?: string | null, fallback = "-") => formatEnumLabel(followResultLabels, value, fallback);
const getBaseName = (row: any) => row?.baseName?.trim?.() || row?.herbBaseName?.trim?.() || "";
const getDetailTitle = (row: Partial<HerbBaseSubjectDetail> | any) => row?.displayName?.trim?.() || row?.subjectName?.trim?.() || "";
const getUserDisplayName = (user: any) => user.realName || user.username || user.name || "-";
const formatTransferOwner = (fromName?: string | null, toName?: string | null) => `${fromName || "未分配"} -> ${toName || "未分配"}`;
const formatScale = (value?: number | string | null) => {
  if (value === null || value === undefined || value === "") return "-";
  const numberValue = Number(value);
  return Number.isFinite(numberValue) ? numberValue.toLocaleString("zh-CN", { maximumFractionDigits: 2 }) : "-";
};

const getStatusType = (status: string) => {
  const types: Record<string, string> = {
    PENDING: "info",
    FOLLOWING: "warning",
    INTERESTED: "primary",
    DEAL: "success",
    LOST: "danger",
    待联系: "info",
    跟进中: "warning",
    有意向: "primary",
    已成交: "success",
    已流失: "danger",
  };
  return types[status] || "info";
};

const getGradeType = (grade: string) => {
  const types: Record<string, string> = {
    高: "danger",
    中: "warning",
    低: "info",
    A: "danger",
    B: "warning",
    C: "info",
    INVALID: "danger",
    无效: "danger",
  };
  return types[grade] || "info";
};

const formatNullableDate = (date?: string | null) => {
  if (!date) return "-";
  return new Date(date).toLocaleString("zh-CN");
};

const formatRegion = (row: any) => [row.province, row.city, row.area].filter(Boolean).join(" / ") || "-";
const formatRegions = (regions?: string[]) => regions?.filter(Boolean).join(" / ") || "-";
const formatSourcePlatforms = (sources?: string[]) => (sources || []).map(source => formatSourcePlatform(source)).join("、") || "-";

const syncRegionPathFromForm = () => {
  regionPath.value = [form.province, form.city, form.area].filter(Boolean);
};

const handleRegionChange = (value: string[] | string) => {
  const path = Array.isArray(value) ? value : [];
  form.province = path[0] || "";
  form.city = path[1] || "";
  form.area = path[2] || "";
};

const isOverdue = (date?: string | null) => {
  if (!date) return false;
  return new Date(date).getTime() < Date.now();
};

const formatDateParam = (date: Date) => {
  const pad = (num: number) => String(num).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
};

const getRowClassName = ({ row }: any) => {
  if (row.status === "LOST" || row.status === "已流失" || row.grade === "INVALID" || row.grade === "无效") return "row-disabled";
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
    herbBaseName: "",
    sourcePlatform: "",
    grade: "",
    status: "",
    mainProducts: [],
    onlyOverdue: undefined,
    onlyNoNextFollow: undefined,
    nextFollowFrom: "",
    nextFollowTo: "",
    sortField: "CreatedAt",
    sortDirection: "Descending",
  });
  followFilter.value = "";
};

const resetCustomerForm = () => {
  Object.assign(form, {
    id: "",
    baseName: "",
    subjectName: "",
    herbBaseName: "",
    mainProducts: [],
    grade: "中",
    score: 0,
    province: "",
    city: "",
    area: "",
    address: "",
    lat: undefined,
    lng: undefined,
    sourcePlatform: "BAIDU_MAP",
    sourceId: undefined,
    status: "PENDING",
    ownerUserId: undefined,
    remark: "",
    parentId: undefined,
    primaryContactName: "",
    primaryContactPhone: "",
  });
  regionPath.value = [];
};

const handleAdd = async () => {
  isEdit.value = false;
  resetCustomerForm();
  dialogVisible.value = true;
};

const handleEdit = async (row: any) => {
  isEdit.value = true;
  Object.assign(form, {
    id: row.id,
    baseName: getBaseName(row),
    subjectName: row.subjectName || "",
    herbBaseName: getBaseName(row),
    mainProducts: normalizeMainProducts(row),
    grade: toEnumValue(gradeValues, row.grade, "中"),
    score: row.score || 0,
    province: row.province || "",
    city: row.city || "",
    area: row.area || "",
    address: row.address || "",
    lat: row.lat ?? undefined,
    lng: row.lng ?? undefined,
    sourcePlatform: toEnumValue(sourcePlatformValues, row.sourcePlatform, "BAIDU_MAP"),
    sourceId: row.sourceId ?? undefined,
    status: toEnumValue(statusValues, row.status, "PENDING"),
    ownerUserId: row.ownerUserId || undefined,
    remark: row.remark || "",
    parentId: row.parentId || undefined,
    primaryContactName: row.primaryContactName || "",
    primaryContactPhone: row.primaryContactPhone || "",
  });
  syncRegionPathFromForm();
  dialogVisible.value = true;
};

const reloadList = () => {
  queryPageRef.value?.getTableList();
};

const handleSortChange = ({ prop, order }: { prop: "totalScale" | "score"; order: "ascending" | "descending" | null }) => {
  const sortFieldMap = {
    totalScale: "TotalScale",
    score: "Score",
  };

  searchForm.sortField = order ? sortFieldMap[prop] : "CreatedAt";
  searchForm.sortDirection = order === "ascending" ? "Ascending" : "Descending";
  reloadList();
};

const handleSelectionChange = (rows: HerbBaseSubjectDetail[]) => {
  selectedHerbBases.value = rows;
};

const loadOwnerOptions = async () => {
  const res = await userApi.getUserList({ page: 1, pageSize: 100, username: "", realName: "", roleId: "", isActive: true });
  ownerOptions.value = (res.data?.list || []).filter((user: any) => user.isActive);
};

const loadCustomerDetail = async (herbBaseSubjectId: string) => {
  const response = await crmHerbBaseApi.getSubject(herbBaseSubjectId);
  currentHerbBase.value = response.data;
  contacts.value = response.data?.contacts || [];
  followRecords.value = response.data?.followRecords || [];
};

const openDetail = async (row: any) => {
  await loadCustomerDetail(row.id);
  detailDrawerVisible.value = true;
};

const handleSubmit = async () => {
  if (!form.baseName && !form.subjectName) {
    ElMessage.error("请输入基地名称或主体名称");
    return;
  }

  const request = {
    ...form,
    herbBaseName: form.baseName,
    mainProducts: [...form.mainProducts],
    grade: toEnumValue(gradeValues, form.grade, "中"),
    sourcePlatform: toEnumValue(sourcePlatformValues, form.sourcePlatform, "BAIDU_MAP"),
    status: toEnumValue(statusValues, form.status, "PENDING"),
  };

  if (isEdit.value) {
    await crmHerbBaseApi.updateCustomer(form.id, request);
    ElMessage.success("保存成功");
  } else {
    await crmHerbBaseApi.createCustomer(request);
    ElMessage.success("创建成功");
  }

  dialogVisible.value = false;
  reloadList();
};

const openAssignDialog = async (rows?: HerbBaseSubjectDetail[]) => {
  const customers = rows?.length ? rows : selectedHerbBases.value;
  if (customers.length === 0) {
    ElMessage.warning("请选择要分配的药材基地");
    return;
  }

  Object.assign(assignForm, {
    herbBaseSubjectIds: customers.map(customer => customer.id),
    ownerUserId: customers.length === 1 ? customers[0].ownerUserId || "" : "",
    remark: "",
  });
  await loadOwnerOptions();
  assignDialogVisible.value = true;
};

const submitAssignOwner = async () => {
  await crmHerbBaseApi.assignSubjectOwner({
    herbBaseSubjectIds: [...assignForm.herbBaseSubjectIds],
    ownerUserId: assignForm.ownerUserId || null,
    remark: assignForm.remark || undefined,
  });
  if (currentHerbBase.value && assignForm.herbBaseSubjectIds.includes(currentHerbBase.value.id)) {
    await loadCustomerDetail(currentHerbBase.value.id);
  }
  ElMessage.success("分配成功");
  assignDialogVisible.value = false;
  reloadList();
};

const resetContactForm = () => {
  Object.assign(contactForm, {
    id: "",
    contactName: "",
    phone: "",
    phoneType: "UNKNOWN",
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
      phoneType: toEnumValue(phoneTypeValues, row.phoneType, "UNKNOWN"),
      roleName: toEnumValue(contactRoleValues, row.roleName),
    });
  }
  contactDialogVisible.value = true;
};

const submitContact = async () => {
  if (!currentHerbBase.value) return;
  if (!contactForm.contactName && !contactForm.phone) {
    ElMessage.error("请填写联系人姓名或电话");
    return;
  }

  const request = {
    ...contactForm,
    phoneType: toEnumValue(phoneTypeValues, contactForm.phoneType, "UNKNOWN"),
    roleName: toEnumValue(contactRoleValues, contactForm.roleName),
  };

  if (contactForm.id) {
    await crmHerbBaseApi.updateContact(contactForm.id, request);
  } else {
    await crmHerbBaseApi.createSubjectContact(currentHerbBase.value.id, request);
  }

  ElMessage.success("联系人已保存");
  contactDialogVisible.value = false;
  await loadCustomerDetail(currentHerbBase.value.id);
  reloadList();
};

const setPrimaryContact = async (row: any) => {
  if (!currentHerbBase.value) return;
  await crmHerbBaseApi.setPrimaryContact(row.id);
  ElMessage.success("主联系人已更新");
  await loadCustomerDetail(currentHerbBase.value.id);
  reloadList();
};

const resetFollowForm = () => {
  Object.assign(followForm, {
    contactId: undefined,
    followType: "PHONE",
    followResult: "",
    intentLevel: "",
    content: "",
    nextFollowAt: "",
  });
};

const openFollowDialog = async (row: any) => {
  if (!currentHerbBase.value || currentHerbBase.value.id !== row.id) {
    await loadCustomerDetail(row.id);
  }
  resetFollowForm();
  const primaryContact = contacts.value.find(contact => contact.isPrimary);
  followForm.contactId = primaryContact?.id;
  followDialogVisible.value = true;
};

const getQueryValue = (value: unknown) => {
  if (Array.isArray(value)) return value[0] || "";
  return typeof value === "string" ? value : "";
};

const applyRouteEntrypoint = async () => {
  const followQuery = getQueryValue(route.query.followFilter);
  const gradeQuery = getQueryValue(route.query.grade);
  const onlyOverdueQuery = getQueryValue(route.query.onlyOverdue);
  const actionQuery = getQueryValue(route.query.action);
  const detailId = getQueryValue(route.query.detailId);
  const followId = getQueryValue(route.query.followId);

  if (followQuery) {
    followFilter.value = followQuery;
    applyFollowFilter();
    reloadList();
  } else if (onlyOverdueQuery === "true") {
    followFilter.value = "overdue";
    applyFollowFilter();
    reloadList();
  }

  if (gradeQuery) {
    searchForm.grade = gradeQuery;
    reloadList();
  }

  if (actionQuery === "add") {
    await handleAdd();
  }

  if (followId) {
    await openFollowDialog({ id: followId });
  } else if (detailId) {
    await openDetail({ id: detailId });
  }
};

onMounted(() => {
  void applyRouteEntrypoint();
});

const submitFollowRecord = async () => {
  if (!currentHerbBase.value) return;
  if (!followForm.followResult) {
    ElMessage.error("请选择沟通结果");
    return;
  }

  await crmHerbBaseApi.createSubjectFollowRecord(currentHerbBase.value.id, {
    ...followForm,
    followType: toEnumValue(followTypeValues, followForm.followType, "PHONE"),
    followResult: toEnumValue(followResultValues, followForm.followResult),
    intentLevel: toEnumValue(gradeValues, followForm.intentLevel),
    nextFollowAt: followForm.nextFollowAt || null,
  });
  ElMessage.success("沟通记录已保存");
  followDialogVisible.value = false;
  await loadCustomerDetail(currentHerbBase.value.id);
  reloadList();
};

const markCustomerStatus = async (status: string) => {
  if (!currentHerbBase.value) return;
  await crmHerbBaseApi.updateCustomer(currentHerbBase.value.id, {
    ...currentHerbBase.value,
    status,
  });
  ElMessage.success("药材基地状态已更新");
  await loadCustomerDetail(currentHerbBase.value.id);
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
    max-width: 100%;
    padding: 0;
    overflow: hidden;
    font-weight: 600;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .table-actions {
    display: flex;
    align-items: center;
    gap: 6px;
    width: 100%;
    white-space: nowrap;

    :deep(.el-button) {
      margin-left: 0;
    }
  }

  :deep(.actions-column .cell) {
    padding-right: 10px;
    padding-left: 10px;
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
  min-height: 100%;
  padding: 0;
  background: #ffffff;
}

.drawer-head {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 20px;
  align-items: flex-start;
  padding: 22px 0 18px;
  border-bottom: 1px solid var(--el-border-color-light);
  background: #ffffff;

  h2 {
    margin: 0;
    color: #111827;
    font-size: 25px;
    font-weight: 700;
    line-height: 1.25;
  }

  .detail-kicker {
    margin-bottom: 6px;
    color: var(--el-text-color-secondary);
    font-size: 12px;
    font-weight: 600;
  }

  .title-row {
    display: flex;
    min-width: 0;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
  }

  .head-meta,
  .head-actions {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
  }

  .head-meta {
    margin-top: 10px;
    color: var(--el-text-color-secondary);
    line-height: 1.7;

    > span {
      display: inline-flex;
      align-items: center;
      min-height: 24px;
      padding-right: 10px;
      border-right: 1px solid var(--el-border-color-lighter);
      color: var(--el-text-color-secondary);
      font-size: 13px;

      &:last-child {
        border-right: 0;
      }
    }
  }

  .head-actions {
    justify-content: flex-end;
    max-width: 560px;

    :deep(.el-button) {
      margin-left: 0;
    }
  }
}

.summary-band {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
  padding: 14px 0;
  background: #ffffff;

  > div {
    display: flex;
    min-height: 76px;
    flex-direction: column;
    justify-content: center;
    gap: 5px;
    min-width: 0;
    padding: 13px 16px;
    border: 1px solid var(--el-border-color-light);
    border-radius: 8px;
    background: #ffffff;

    strong,
    span:not(.label) {
      overflow-wrap: anywhere;
    }

    strong {
      color: #111827;
      font-size: 15px;
      line-height: 1.35;
    }
  }

  .label {
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }
}

.main-product-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  min-width: 0;
}

.drawer-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.75fr) minmax(360px, 0.9fr);
  gap: 16px;
  padding: 0 0 24px;
  background: #ffffff;
}

.profile-panel {
  display: grid;
  grid-column: auto;
  gap: 14px;
  align-content: start;

  :deep(.customer-profile-descriptions .el-descriptions__label) {
    width: 88px;
    min-width: 88px;
    text-align: center;
    white-space: nowrap;
  }
}

.timeline-panel {
  display: grid;
  grid-column: auto;
  gap: 14px;
  align-content: start;

  :deep(.el-timeline) {
    margin: 0;
    padding-left: 0;
  }

  :deep(.el-timeline-item__wrapper) {
    padding-left: 26px;
  }
}

.profile-panel,
.timeline-panel {
  min-width: 0;

  h3 {
    margin: 0;
    font-size: 16px;
  }
}

.detail-card {
  min-width: 0;
  padding: 16px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  background: #ffffff;

  :deep(.el-table) {
    border-radius: 6px;
  }

  :deep(.el-table th.el-table__cell) {
    background: #f8fafc;
    color: var(--el-text-color-secondary);
    font-weight: 600;
  }
}

.section-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 18px -16px 14px;
  padding: 0 16px 10px;
  border-bottom: 1px solid var(--el-border-color-lighter);

  h3 {
    color: #111827;
    font-weight: 700;
  }
}

.section-title-first {
  margin-top: 0;
}

.follow-item {
  padding: 0 0 10px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: transparent;

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

.timeline-panel :deep(.el-timeline-item:last-child .follow-item) {
  border-bottom: 0;
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

















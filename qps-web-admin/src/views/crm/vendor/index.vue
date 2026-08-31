<template>
  <div class="vendor-page">
    <QueryPage api="/admin/crm/vendors" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键词">
            <el-input v-model="searchForm.keyword" clearable placeholder="厂商 / 联系人 / 电话 / 品类" />
          </el-form-item>
          <el-form-item>
            <template #label>
              <span class="filter-label-with-help">
                优先级
                <el-tooltip :content="getPriorityRule()" placement="top">
                  <el-icon class="filter-help-icon" :title="getPriorityRule()">
                    <QuestionFilled />
                  </el-icon>
                </el-tooltip>
              </span>
            </template>
            <el-select v-model="searchForm.priorityLevel" clearable placeholder="优先级">
              <el-option label="高" value="High" />
              <el-option label="中" value="Medium" />
              <el-option label="低" value="Low" />
            </el-select>
          </el-form-item>
          <el-form-item label="电话">
            <el-select v-model="searchForm.hasPhone" clearable placeholder="电话状态">
              <el-option label="有电话" :value="true" />
              <el-option label="无电话" :value="false" />
            </el-select>
          </el-form-item>
          <el-form-item label="品类">
            <el-select v-model="searchForm.hasProduct" clearable placeholder="品类状态">
              <el-option label="有品类" :value="true" />
              <el-option label="无品类" :value="false" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <Permission code="CRM_TRANSFER"><el-button :icon="Edit" @click="openTransferDialog()">批量流转</el-button></Permission>
        <Permission code="CRM_VENDOR_ADD"><el-button type="primary" :icon="Plus" @click="openCreateDialog">新增厂商</el-button></Permission>
      </template>

      <template #table="{ tableData }">
        <el-table
          :data="tableData"
          :row-key="'id'"
          :fit="true"
          class="wide-list-table"
          style="--table-min-width: 1560px"
          border
          @selection-change="handleSelectionChange"
        >
          <el-table-column type="selection" width="44" fixed="left" />
          <el-table-column label="厂商名称" min-width="220" fixed="left" show-overflow-tooltip>
            <template #default="{ row }">
              <el-button type="primary" link class="vendor-link" @click="openDetail(row)">
                {{ row.vendorName || "-" }}
              </el-button>
            </template>
          </el-table-column>
          <el-table-column label="优先级" width="88">
            <template #default="{ row }">
              <el-tooltip :content="getPriorityRule(row.priorityLevel)" placement="top">
                <el-tag :type="getPriorityType(row.priorityLevel)" :title="getPriorityRule(row.priorityLevel)">
                  {{ formatPriority(row.priorityLevel) }}
                </el-tag>
              </el-tooltip>
            </template>
          </el-table-column>
          <el-table-column label="主联系人 / 电话" width="160">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ row.primaryContactName || "-" }}</span>
                <span class="phone-text">{{ row.primaryContactPhone || "-" }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="跟进人" width="96" show-overflow-tooltip>
            <template #default="{ row }">{{ row.ownerUserName || "未分配" }}</template>
          </el-table-column>
          <el-table-column label="采购计划" width="84" align="right">
            <template #default="{ row }">{{ row.purchasePlanCount || 0 }}</template>
          </el-table-column>
          <el-table-column label="品类" width="72" align="right">
            <template #default="{ row }">{{ row.productCount || 0 }}</template>
          </el-table-column>
          <el-table-column label="最近采购时间" width="150">
            <template #default="{ row }">{{ formatDate(row.latestPurchaseTime) }}</template>
          </el-table-column>
          <el-table-column label="最近采购计划" min-width="260" show-overflow-tooltip>
            <template #default="{ row }">{{ row.latestPurchasePlanName || "-" }}</template>
          </el-table-column>
          <el-table-column label="更新时间" width="150">
            <template #default="{ row }">{{ formatDate(row.updatedAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" width="310" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <el-button type="primary" link :icon="View" @click="openDetail(row)">详情</el-button>
              <Permission code="CRM_FOLLOW"><el-button type="primary" link :icon="Phone" @click="openFollowDialog(row)">记录沟通</el-button></Permission>
              <Permission code="CRM_VENDOR_EDIT"><el-button type="primary" link :icon="Edit" @click="openEditDialog(row)">编辑</el-button></Permission>
              <el-button v-if="canManageTransfer" type="primary" link :icon="Edit" @click="openTransferDialog([row], row.ownerUserId ? 'TRANSFER' : 'ASSIGN')">
                {{ row.ownerUserId ? "转交" : "分配" }}
              </el-button>
              <el-button v-if="canManageTransfer || canReturn(row)" type="primary" link :icon="Edit" @click="openTransferDialog([row], 'RETURN')">退回</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-drawer v-model="detailDrawerVisible" size="82%" :with-header="false" class="vendor-drawer">
      <div v-if="currentVendor" class="drawer-layout">
        <section class="drawer-head">
          <div class="head-main">
            <div class="detail-kicker">厂商详情</div>
            <div class="title-row">
              <h2>{{ currentVendor.vendorName }}</h2>
              <el-tooltip :content="getPriorityRule(currentVendor.priorityLevel)" placement="top">
                <el-tag :type="getPriorityType(currentVendor.priorityLevel)" effect="dark" :title="getPriorityRule(currentVendor.priorityLevel)">
                  {{ formatPriority(currentVendor.priorityLevel) }}
                </el-tag>
              </el-tooltip>
            </div>
            <div class="head-meta">
              <span>采购计划 {{ currentVendor.purchasePlanCount || 0 }}</span>
              <span>品类 {{ currentVendor.productCount || 0 }}</span>
              <span>联系人 {{ currentVendor.contactCount || 0 }}</span>
            </div>
          </div>
          <div class="head-actions">
            <el-button v-if="canManageTransfer" :icon="Edit" @click="openTransferDialog([currentVendor], currentVendor.ownerUserId ? 'TRANSFER' : 'ASSIGN')">
              {{ currentVendor.ownerUserId ? "转交" : "分配" }}
            </el-button>
            <el-button v-if="canManageTransfer || canReturn(currentVendor)" :icon="Edit" @click="openTransferDialog([currentVendor], 'RETURN')">退回</el-button>
            <Permission code="CRM_VENDOR_EDIT"><el-button :icon="Edit" @click="openEditDialog(currentVendor)">编辑</el-button></Permission>
            <el-button :icon="Refresh" @click="refreshDetail">刷新</el-button>
          </div>
        </section>

        <section class="summary-band">
          <div class="summary-card">
            <span class="label">采购概况</span>
            <strong>{{ currentVendor.purchasePlanCount || 0 }} 个计划</strong>
            <span>品类 {{ currentVendor.productCount || 0 }} · 联系人 {{ currentVendor.contactCount || 0 }}</span>
          </div>
          <div class="summary-card">
            <span class="label">主联系人</span>
            <strong>{{ currentVendor.primaryContactName || "-" }}</strong>
            <span>{{ currentVendor.primaryContactPhone || "-" }}</span>
          </div>
          <div class="summary-card">
            <span class="label">最近采购</span>
            <strong>{{ formatDate(currentVendor.latestPurchaseTime) }}</strong>
            <span>{{ currentVendor.latestPurchasePlanName || "-" }}</span>
          </div>
          <div class="summary-card">
            <span class="label">更新时间</span>
            <strong>{{ formatDate(currentVendor.updatedAt) }}</strong>
            <span>{{ currentVendor.remark || "-" }}</span>
          </div>
        </section>

        <section class="detail-grid">
          <div class="detail-column detail-card">
            <div class="section-title section-title-first">
              <h3>联系人</h3>
              <Permission code="CRM_VENDOR_EDIT">
                <el-button type="primary" link :icon="Plus" @click="openContactDialog">新增联系人</el-button>
              </Permission>
            </div>
            <el-table :data="currentVendor.contacts || []" border>
              <el-table-column label="姓名" width="130" show-overflow-tooltip>
                <template #default="{ row }">
                  <span>{{ row.contactName || "-" }}</span>
                  <el-tag v-if="row.isPrimary" size="small" type="success" class="ml8">主</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="phone" label="电话" width="150" show-overflow-tooltip />
              <el-table-column label="类型" width="90">
                <template #default="{ row }">{{ formatPhoneType(row.phoneType) }}</template>
              </el-table-column>
              <el-table-column label="角色" min-width="110" show-overflow-tooltip>
                <template #default="{ row }">{{ formatRole(row.roleName) }}</template>
              </el-table-column>
              <el-table-column prop="remark" label="备注" min-width="180" show-overflow-tooltip />
              <el-table-column label="状态" width="90">
                <template #default="{ row }">
                  <el-tag :type="row.status === 'INVALID' ? 'danger' : 'info'" size="small">
                    {{ formatContactStatus(row.status) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="210" class-name="actions-column" header-class-name="actions-column">
                <template #default="{ row }">
                  <div class="table-actions">
                    <Permission code="CRM_VENDOR_EDIT">
                      <el-button type="primary" link :icon="Edit" @click="openContactDialog(row)">编辑</el-button>
                    </Permission>
                    <Permission code="CRM_VENDOR_EDIT">
                      <el-button v-if="!row.isPrimary && row.status !== 'INVALID'" type="primary" link @click="setPrimaryContact(row)">设为主</el-button>
                    </Permission>
                    <Permission code="CRM_VENDOR_EDIT">
                      <el-button type="primary" link @click="toggleContactStatus(row)">
                        {{ row.status === "INVALID" ? "启用" : "停用" }}
                      </el-button>
                    </Permission>
                  </div>
                </template>
              </el-table-column>
            </el-table>

            <div class="section-title">
              <h3>采购品类</h3>
              <Permission code="CRM_VENDOR_EDIT">
                <el-button type="primary" link :icon="Plus" @click="openProductDialog">新增品类</el-button>
              </Permission>
            </div>
            <div v-if="currentVendor.products?.length" class="product-tags">
              <el-tooltip v-for="item in currentVendor.products" :key="item.id" :content="item.remark || item.productName" placement="top">
                <el-tag effect="plain" closable @close="removeProduct(item.productName)">{{ item.productName }}</el-tag>
              </el-tooltip>
            </div>
            <el-empty v-else description="暂无品类" />
          </div>

          <div class="detail-column">
            <section class="detail-card">
            <div class="section-title section-title-first">
              <h3>采购计划</h3>
              <Permission code="CRM_VENDOR_EDIT">
                <el-button type="primary" link :icon="Plus" @click="openPurchasePlanDialog">新增计划</el-button>
              </Permission>
            </div>
            <el-table :data="purchasePlans" v-loading="purchasePlanLoading" border>
              <el-table-column label="计划名称" min-width="220" show-overflow-tooltip>
                <template #default="{ row }">{{ row.purchasePlanName || "-" }}</template>
              </el-table-column>
              <el-table-column label="采购时间" width="150">
                <template #default="{ row }">{{ formatDate(row.purchaseTime) }}</template>
              </el-table-column>
              <el-table-column label="品类数量" min-width="240" show-overflow-tooltip>
                <template #default="{ row }">{{ row.products || "-" }}</template>
              </el-table-column>
              <el-table-column label="网页" width="92">
                <template #default="{ row }">
                  <el-button v-if="row.pageUrl" type="primary" link :icon="Link" @click="openPage(row.pageUrl)">打开</el-button>
                  <span v-else>-</span>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="120" class-name="actions-column" header-class-name="actions-column">
                <template #default="{ row }">
                  <div class="table-actions">
                    <Permission code="CRM_VENDOR_EDIT"><el-button type="primary" link :icon="Edit" @click="openPurchasePlanDialog(row)">编辑</el-button></Permission>
                    <Permission code="CRM_VENDOR_EDIT"><el-button type="danger" link @click="deletePurchasePlan(row)">删除</el-button></Permission>
                  </div>
                </template>
              </el-table-column>
            </el-table>
            <div class="table-footer">
              <el-pagination
                v-model:current-page="purchasePlanPage"
                v-model:page-size="purchasePlanPageSize"
                :page-sizes="[10, 20, 50, 100]"
                :total="purchasePlanTotal"
                layout="total, sizes, prev, pager, next, jumper"
                background
                @size-change="handlePurchasePlanSizeChange"
                @current-change="handlePurchasePlanPageChange"
              />
            </div>
            </section>

            <div class="activity-row">
              <section class="detail-card follow-card activity-panel">
                <div class="section-title section-title-first">
                  <h3>沟通记录</h3>
                  <Permission code="CRM_FOLLOW">
                    <el-button type="primary" link :icon="Phone" @click="openFollowDialog">记录</el-button>
                  </Permission>
                </div>
                <el-timeline v-if="followRecords.length" class="follow-timeline">
                  <el-timeline-item v-for="record in followRecords" :key="record.id" :timestamp="formatDate(record.createdAt)" placement="top">
                    <div class="follow-item">
                      <div class="follow-title">
                        <strong>{{ formatFollowResult(record.followResult, "沟通") }}</strong>
                        <el-tag size="small">{{ formatFollowType(record.followType) }}</el-tag>
                      </div>
                      <p>{{ record.content || "-" }}</p>
                      <span class="muted">{{ record.contactName || "未指定联系人" }} · 下次 {{ formatDate(record.nextFollowAt) }}</span>
                    </div>
                  </el-timeline-item>
                </el-timeline>
                <el-empty v-else description="暂无沟通记录" />
              </section>

              <section class="detail-card follow-card activity-panel">
                <div class="section-title section-title-first">
                  <h3>流转记录</h3>
                </div>
                <el-timeline v-if="currentVendor.transferRecords?.length">
                  <el-timeline-item
                    v-for="record in currentVendor.transferRecords"
                    :key="record.id"
                    :timestamp="formatDate(record.createdAt)"
                    placement="top"
                  >
                    <div class="follow-item">
                      <div class="follow-title">
                        <strong>{{ formatTransferAction(record.actionType) }}：{{ formatTransferOwner(record.fromOwnerUserName, record.toOwnerUserName) }}</strong>
                      </div>
                      <p v-if="record.remark">{{ record.remark }}</p>
                      <span class="muted">操作人 {{ record.operatorUserName || "-" }}</span>
                    </div>
                  </el-timeline-item>
                </el-timeline>
                <el-empty v-else description="暂无流转记录" />
              </section>
            </div>
          </div>
        </section>
      </div>
    </el-drawer>

    <el-dialog v-model="vendorDialogVisible" :title="isEdit ? '编辑厂商' : '新增厂商'" width="560px">
      <el-form :model="vendorForm" label-width="110px">
        <el-form-item label="厂商名称" required>
          <el-input v-model="vendorForm.vendorName" clearable placeholder="请输入厂商名称" />
        </el-form-item>
        <el-form-item label="优先级">
          <el-select v-model="vendorForm.priorityLevel" placeholder="请选择优先级">
            <el-option label="高" value="High" />
            <el-option label="中" value="Medium" />
            <el-option label="低" value="Low" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="vendorForm.remark" type="textarea" :rows="3" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="vendorDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitVendorForm">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="transferDialogVisible" :title="transferDialogTitle" width="520px">
      <el-form :model="transferForm" label-width="90px">
        <el-form-item label="已选厂商">
          <span>{{ transferForm.entityIds.length }} 个</span>
        </el-form-item>
        <el-form-item v-if="transferMode !== 'RETURN'" label="跟进人">
          <el-select v-model="transferForm.ownerUserId" filterable placeholder="请选择跟进人">
            <el-option v-for="user in ownerOptions" :key="user.id" :label="getUserDisplayName(user)" :value="user.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="transferForm.remark" type="textarea" :rows="3" placeholder="可选" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="transferDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitTransfer">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="contactDialogVisible" :title="contactForm.id ? '编辑联系人' : '新增联系人'" width="520px">
      <el-form :model="contactForm" label-width="100px">
        <el-form-item label="姓名">
          <el-input v-model="contactForm.contactName" clearable placeholder="联系人姓名" />
        </el-form-item>
        <el-form-item label="电话">
          <el-input v-model="contactForm.phone" clearable placeholder="联系电话" />
        </el-form-item>
        <el-form-item label="电话类型">
          <el-select v-model="contactForm.phoneType">
            <el-option label="手机" value="MOBILE" />
            <el-option label="座机" value="LANDLINE" />
            <el-option label="未知" value="UNKNOWN" />
          </el-select>
        </el-form-item>
        <el-form-item label="微信">
          <el-input v-model="contactForm.wechat" clearable placeholder="微信号" />
        </el-form-item>
        <el-form-item label="角色">
          <el-select v-model="contactForm.roleName" clearable placeholder="请选择角色">
            <el-option label="负责人" value="OWNER" />
            <el-option label="采购" value="PURCHASE" />
            <el-option label="财务" value="FINANCE" />
            <el-option label="其他" value="OTHER" />
          </el-select>
        </el-form-item>
        <el-form-item label="主联系人">
          <el-switch v-model="contactForm.isPrimary" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="contactForm.remark" type="textarea" :rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="contactDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitContact">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="productDialogVisible" title="新增采购品类" width="520px">
      <el-form label-width="90px">
        <el-form-item label="品类">
          <el-select
            v-model="productForm.productName"
            filterable
            remote
            allow-create
            default-first-option
            clearable
            placeholder="输入品类名称"
            :remote-method="loadProductOptions"
          >
            <el-option v-for="item in productOptions" :key="item.value" :label="item.label" :value="item.value" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="productDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitProduct">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="followDialogVisible" title="记录沟通" width="560px">
      <el-form :model="followForm" label-width="100px">
        <el-form-item label="联系人">
          <el-select v-model="followForm.contactId" clearable placeholder="可不指定">
            <el-option v-for="contact in followContacts" :key="contact.id" :label="contact.contactName || contact.phone" :value="contact.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="沟通方式">
          <el-select v-model="followForm.followType">
            <el-option label="电话" value="PHONE" />
            <el-option label="微信" value="WECHAT" />
            <el-option label="拜访" value="VISIT" />
          </el-select>
        </el-form-item>
        <el-form-item label="沟通结果" required>
          <el-select v-model="followForm.followResult" placeholder="请选择结果">
            <el-option label="已接通" value="CONNECTED" />
            <el-option label="未接" value="MISSED" />
            <el-option label="空号" value="EMPTY_NUMBER" />
            <el-option label="有意向" value="INTERESTED" />
            <el-option label="无意向" value="NOT_INTERESTED" />
          </el-select>
        </el-form-item>
        <el-form-item label="意向等级">
          <el-select v-model="followForm.intentLevel" clearable placeholder="意向等级">
            <el-option label="A" value="A" />
            <el-option label="B" value="B" />
            <el-option label="C" value="C" />
          </el-select>
        </el-form-item>
        <el-form-item label="沟通内容">
          <el-input v-model="followForm.content" type="textarea" :rows="4" placeholder="记录沟通要点" />
        </el-form-item>
        <el-form-item label="下次跟进">
          <el-date-picker
            v-model="followForm.nextFollowAt"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm:ss"
            :disabled-date="disablePastFollowDate"
            placeholder="请选择时间"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="followDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitFollowRecord">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="purchasePlanDialogVisible" :title="purchasePlanForm.id ? '编辑采购计划' : '新增采购计划'" width="560px">
      <el-form :model="purchasePlanForm" label-width="110px">
        <el-form-item label="计划名称" required>
          <el-input v-model="purchasePlanForm.purchasePlanName" clearable placeholder="请输入计划名称" />
        </el-form-item>
        <el-form-item label="采购时间">
          <el-date-picker v-model="purchasePlanForm.purchaseTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" placeholder="请选择时间" />
        </el-form-item>
        <el-form-item label="品类数量">
          <el-input v-model="purchasePlanForm.products" clearable placeholder="例如：黄芪 10 吨、当归 5 吨" />
        </el-form-item>
        <el-form-item label="网页">
          <el-input v-model="purchasePlanForm.pageUrl" clearable placeholder="来源网页地址" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="purchasePlanForm.remark" type="textarea" :rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="purchasePlanDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitPurchasePlan">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="vendor">
import { computed, onMounted, reactive, ref } from "vue";
import { Edit, Link, Phone, Plus, QuestionFilled, Refresh, View } from "@element-plus/icons-vue";
import { ElMessage } from "element-plus";
import { useRoute } from "vue-router";
import QueryPage from "@/components/QueryPage/index.vue";
import { crmVendorApi } from "@/api/modules/crmVendor";
import { userApi } from "@/api/modules/user";
import Permission from "@/components/Permission/index.vue";
import { useAuthStore } from "@/stores/modules/auth";
import { useUserStore } from "@/stores/modules/user";

interface VendorDetail {
  id: string;
  vendorName: string;
  normalizedVendorName: string;
  priorityLevel: string;
  latestPurchaseTime?: string | null;
  latestPurchasePlanName: string;
  remark: string;
  ownerUserId?: string | null;
  ownerUserName?: string | null;
  primaryContactName: string;
  primaryContactPhone: string;
  purchasePlanCount: number;
  productCount: number;
  contactCount: number;
  createdAt: string;
  updatedAt: string;
  contacts: any[];
  products: any[];
  purchasePlans: any[];
  transferRecords: any[];
}

const queryPageRef = ref();
const route = useRoute();
const authStore = useAuthStore();
const userStore = useUserStore();

const detailDrawerVisible = ref(false);
const vendorDialogVisible = ref(false);
const transferDialogVisible = ref(false);
const contactDialogVisible = ref(false);
const purchasePlanDialogVisible = ref(false);
const productDialogVisible = ref(false);
const followDialogVisible = ref(false);
const isEdit = ref(false);
const currentVendor = ref<VendorDetail | null>(null);
const selectedVendors = ref<VendorDetail[]>([]);
const ownerOptions = ref<any[]>([]);
const purchasePlans = ref<any[]>([]);
const purchasePlanLoading = ref(false);
const purchasePlanPage = ref(1);
const purchasePlanPageSize = ref(10);
const purchasePlanTotal = ref(0);
const productOptions = ref<any[]>([]);
const followRecords = ref<any[]>([]);
const followContacts = computed(() => (currentVendor.value?.contacts || []).filter((contact: any) => contact.status !== "INVALID"));

const searchForm = reactive({
  keyword: "",
  priorityLevel: "",
  hasPhone: undefined as boolean | undefined,
  hasProduct: undefined as boolean | undefined,
});

const vendorForm = reactive({
  id: "",
  vendorName: "",
  priorityLevel: "Medium",
  remark: "",
});

const transferForm = reactive({
  entityIds: [] as string[],
  ownerUserId: "",
  remark: "",
});
const transferMode = ref<"ASSIGN" | "TRANSFER" | "RETURN">("TRANSFER");
const canManageTransfer = computed(() => authStore.userPermissions.includes("CRM_TRANSFER"));
const transferDialogTitle = computed(() => ({
  ASSIGN: "分配跟进人",
  TRANSFER: "转交跟进人",
  RETURN: "退回待分配池",
})[transferMode.value]);

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

const purchasePlanForm = reactive({
  id: "",
  purchasePlanName: "",
  purchaseTime: "",
  products: "",
  pageUrl: "",
  remark: "",
});

const productForm = reactive({
  productName: "",
});

const followForm = reactive({
  contactId: undefined as string | undefined,
  followType: "PHONE",
  followResult: "",
  intentLevel: "",
  content: "",
  nextFollowAt: "",
});

const priorityLabels: Record<string, string> = {
  High: "高",
  Medium: "中",
  Low: "低",
};

const priorityRules: Record<string, string> = {
  High: "高：有电话、有联系人、90天内有采购、品类数 >= 3",
  Medium: "中：有电话、有品类",
  Low: "低：干净可导入，但缺电话、缺品类、采购时间较旧或联系人信息较弱",
};

const phoneTypeLabels: Record<string, string> = {
  MOBILE: "手机",
  LANDLINE: "座机",
  UNKNOWN: "未知",
};

const roleLabels: Record<string, string> = {
  OWNER: "负责人",
  PURCHASE: "采购",
  FINANCE: "财务",
  BASE_OWNER: "基地负责人",
  COOPERATIVE_OWNER: "合作社负责人",
  OTHER: "其他",
  采购: "采购",
};

const contactStatusLabels: Record<string, string> = {
  UNVERIFIED: "未验证",
  VALID: "有效",
  INVALID: "无效",
};

const followTypeLabels: Record<string, string> = {
  PHONE: "电话",
  WECHAT: "微信",
  VISIT: "拜访",
};

const followResultLabels: Record<string, string> = {
  CONNECTED: "已接通",
  MISSED: "未接",
  EMPTY_NUMBER: "空号",
  INTERESTED: "有意向",
  NOT_INTERESTED: "无意向",
};

const handleReset = () => {
  searchForm.keyword = "";
  searchForm.priorityLevel = "";
  searchForm.hasPhone = undefined;
  searchForm.hasProduct = undefined;
};

const reloadList = () => {
  queryPageRef.value?.getTableList();
};

const handleSelectionChange = (rows: VendorDetail[]) => {
  selectedVendors.value = rows;
};

const getUserDisplayName = (user: any) => user.realName || user.username || user.name || "-";

const loadOwnerOptions = async () => {
  const res = await userApi.getUserList({ page: 1, pageSize: 100, username: "", realName: "", roleId: "", isActive: true });
  ownerOptions.value = (res.data?.list || []).filter((user: any) => user.isActive);
};

const resetVendorForm = () => {
  Object.assign(vendorForm, {
    id: "",
    vendorName: "",
    priorityLevel: "Medium",
    remark: "",
  });
};

const openCreateDialog = async () => {
  isEdit.value = false;
  resetVendorForm();
  await loadOwnerOptions();
  vendorDialogVisible.value = true;
};

const openEditDialog = async (row: VendorDetail) => {
  isEdit.value = true;
  await loadOwnerOptions();
  Object.assign(vendorForm, {
    id: row.id,
    vendorName: row.vendorName || "",
    priorityLevel: row.priorityLevel || "Medium",
    remark: row.remark || "",
  });
  vendorDialogVisible.value = true;
};

const submitVendorForm = async () => {
  if (!vendorForm.vendorName.trim()) {
    ElMessage.warning("请输入厂商名称");
    return;
  }

  const payload = {
    vendorName: vendorForm.vendorName,
    priorityLevel: vendorForm.priorityLevel,
    remark: vendorForm.remark,
  };

  if (isEdit.value) {
    await crmVendorApi.updateVendor(vendorForm.id, payload);
    ElMessage.success("编辑成功");
    if (currentVendor.value?.id === vendorForm.id) {
      const result = await crmVendorApi.getVendor(vendorForm.id);
      currentVendor.value = result.data;
    }
  } else {
    await crmVendorApi.createVendor(payload);
    ElMessage.success("新增成功");
  }

  vendorDialogVisible.value = false;
  reloadList();
};

const openTransferDialog = async (vendors?: VendorDetail[], mode: "ASSIGN" | "TRANSFER" | "RETURN" = "TRANSFER") => {
  const rows = vendors || selectedVendors.value;
  if (rows.length === 0) {
    ElMessage.warning("请选择要流转的厂商");
    return;
  }

  transferMode.value = mode;
  if (mode !== "RETURN") await loadOwnerOptions();
  Object.assign(transferForm, {
    entityIds: rows.map(item => item.id),
    ownerUserId: "",
    remark: "",
  });
  transferDialogVisible.value = true;
};

const submitTransfer = async () => {
  if (transferMode.value !== "RETURN" && !transferForm.ownerUserId) {
    ElMessage.warning("请选择跟进人");
    return;
  }

  await crmVendorApi.changeOwner({
    entityIds: transferForm.entityIds,
    toOwnerUserId: transferMode.value === "RETURN" ? null : transferForm.ownerUserId,
    remark: transferForm.remark,
  });
  ElMessage.success("流转成功");
  transferDialogVisible.value = false;
  reloadList();
  if (currentVendor.value && transferForm.entityIds.includes(currentVendor.value.id)) {
    await refreshDetail();
  }
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
    Object.assign(contactForm, { ...row });
  }
  contactDialogVisible.value = true;
};

const isValidPhone = (phone: string) => /^1[3-9]\d{9}$/.test(phone) || /^0\d{2,3}-?\d{7,8}(-\d{1,6})?$/.test(phone);

const submitContact = async () => {
  if (!currentVendor.value) return;
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
  };

  if (contactForm.id) {
    await crmVendorApi.updateContact(contactForm.id, request);
  } else {
    await crmVendorApi.createContact(currentVendor.value.id, request);
  }

  ElMessage.success("联系人已保存");
  contactDialogVisible.value = false;
  await refreshDetail();
  reloadList();
};

const setPrimaryContact = async (row: any) => {
  if (!currentVendor.value) return;
  await crmVendorApi.setPrimaryContact(row.id);
  ElMessage.success("主联系人已更新");
  await refreshDetail();
  reloadList();
};

const toggleContactStatus = async (row: any) => {
  if (!currentVendor.value) return;
  const status = row.status === "INVALID" ? "VALID" : "INVALID";
  await crmVendorApi.updateContactStatus(row.id, {
    status,
    remark: row.remark || "",
  });
  ElMessage.success(status === "INVALID" ? "联系人已停用" : "联系人已启用");
  await refreshDetail();
  reloadList();
};

const getProductValues = () => (currentVendor.value?.products || []).map((item: any) => item.productName).filter(Boolean);

const saveProductValues = async (values: string[]) => {
  if (!currentVendor.value) return;
  await crmVendorApi.saveBusinessEntityAttributes({
    entityType: "CRM_VENDOR",
    entityId: currentVendor.value.id,
    attributeCode: "PURCHASE_PRODUCT",
    values,
  });
  await refreshDetail(false);
  reloadList();
};

const openProductDialog = async () => {
  productForm.productName = "";
  productDialogVisible.value = true;
  await loadProductOptions("");
};

const loadProductOptions = async (keyword: string) => {
  const result = await crmVendorApi.getBusinessEntityAttributeOptions({
    entityType: "CRM_VENDOR",
    attributeCode: "PURCHASE_PRODUCT",
    keyword,
    pageSize: 30,
  });
  productOptions.value = result.data || [];
};

const submitProduct = async () => {
  const productName = productForm.productName.trim();
  if (!productName) {
    ElMessage.error("请输入采购品类");
    return;
  }

  const values = Array.from(new Set([...getProductValues(), productName]));
  await saveProductValues(values);
  ElMessage.success("采购品类已保存");
  productDialogVisible.value = false;
};

const removeProduct = async (productName: string) => {
  const values = getProductValues().filter(value => value !== productName);
  await saveProductValues(values);
  ElMessage.success("采购品类已删除");
};

const resetPurchasePlanForm = () => {
  Object.assign(purchasePlanForm, {
    id: "",
    purchasePlanName: "",
    purchaseTime: "",
    products: "",
    pageUrl: "",
    remark: "",
  });
};

const toDateTimeInputValue = (value?: string | null) => {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;

  const pad = (num: number) => `${num}`.padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
};

const openPurchasePlanDialog = (row?: any) => {
  resetPurchasePlanForm();
  if (row) {
    Object.assign(purchasePlanForm, {
      id: row.id,
      purchasePlanName: row.purchasePlanName || "",
      purchaseTime: toDateTimeInputValue(row.purchaseTime),
      products: row.products || "",
      pageUrl: row.pageUrl || "",
      remark: row.remark || "",
    });
  }
  purchasePlanDialogVisible.value = true;
};

const submitPurchasePlan = async () => {
  if (!currentVendor.value) return;
  if (!purchasePlanForm.purchasePlanName.trim()) {
    ElMessage.error("请输入采购计划名称");
    return;
  }

  const request = {
    ...purchasePlanForm,
    purchasePlanName: purchasePlanForm.purchasePlanName.trim(),
    purchaseTime: purchasePlanForm.purchaseTime || null,
  };

  if (purchasePlanForm.id) {
    await crmVendorApi.updateVendorPurchasePlan(currentVendor.value.id, purchasePlanForm.id, request);
  } else {
    await crmVendorApi.createVendorPurchasePlan(currentVendor.value.id, request);
  }

  ElMessage.success("采购计划已保存");
  purchasePlanDialogVisible.value = false;
  purchasePlanPage.value = 1;
  await refreshDetail();
  reloadList();
};

const deletePurchasePlan = async (row: any) => {
  if (!currentVendor.value) return;
  await crmVendorApi.deleteVendorPurchasePlan(currentVendor.value.id, row.id);
  ElMessage.success("采购计划已删除");
  purchasePlanPage.value = 1;
  await refreshDetail();
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

const openFollowDialog = async (row?: any) => {
  if (row?.id && currentVendor.value?.id !== row.id) {
    const result = await crmVendorApi.getVendor(row.id);
    currentVendor.value = result.data;
  }
  if (!currentVendor.value) return;
  resetFollowForm();
  const primaryContact = followContacts.value.find((contact: any) => contact.isPrimary);
  followForm.contactId = primaryContact?.id;
  followDialogVisible.value = true;
};

const submitFollowRecord = async () => {
  if (!currentVendor.value) return;
  if (!followForm.followResult) {
    ElMessage.error("请选择沟通结果");
    return;
  }
  if (followForm.nextFollowAt && new Date(followForm.nextFollowAt).getTime() <= Date.now()) {
    ElMessage.error("下次跟进时间必须晚于当前时间");
    return;
  }

  await crmVendorApi.createFollowRecord(currentVendor.value.id, {
    ...followForm,
    contactId: followForm.contactId || null,
    nextFollowAt: followForm.nextFollowAt || null,
  });
  ElMessage.success("沟通记录已保存");
  followDialogVisible.value = false;
  await refreshDetail(false);
  reloadList();
};

const disablePastFollowDate = (date: Date) => date.getTime() < new Date().setHours(0, 0, 0, 0);

const openDetail = async (row: any) => {
  detailDrawerVisible.value = false;
  currentVendor.value = null;
  purchasePlans.value = [];
  followRecords.value = [];
  const result = await crmVendorApi.getVendor(row.id);
  currentVendor.value = result.data;
  detailDrawerVisible.value = true;
  purchasePlanPage.value = 1;
  await loadPurchasePlans();
  await loadFollowRecords();
};

const getQueryValue = (value: unknown) => {
  if (Array.isArray(value)) return value[0] || "";
  return typeof value === "string" ? value : "";
};

const applyRouteEntrypoint = async () => {
  const followId = getQueryValue(route.query.followId);
  const detailId = getQueryValue(route.query.detailId);

  if (followId) {
    await openDetail({ id: followId });
    openFollowDialog();
  } else if (detailId) {
    await openDetail({ id: detailId });
  }
};

onMounted(() => {
  void applyRouteEntrypoint();
});

const refreshDetail = async (showMessage = true) => {
  if (!currentVendor.value) return;
  const result = await crmVendorApi.getVendor(currentVendor.value.id);
  currentVendor.value = result.data;
  await loadPurchasePlans();
  await loadFollowRecords();
  if (showMessage) {
    ElMessage.success("厂商详情已刷新");
  }
};

const loadPurchasePlans = async () => {
  if (!currentVendor.value) return;

  purchasePlanLoading.value = true;
  try {
    const result = await crmVendorApi.getVendorPurchasePlans(currentVendor.value.id, {
      page: purchasePlanPage.value,
      pageSize: purchasePlanPageSize.value,
      sortField: "PurchaseTime",
      sortDirection: "Descending",
    });
    purchasePlans.value = result.data?.list || [];
    purchasePlanTotal.value = result.data?.totalCount || 0;
  } finally {
    purchasePlanLoading.value = false;
  }
};

const loadFollowRecords = async () => {
  if (!currentVendor.value) return;
  const result = await crmVendorApi.getFollowRecords(currentVendor.value.id);
  followRecords.value = result.data || [];
};

const handlePurchasePlanPageChange = async (page: number) => {
  purchasePlanPage.value = page;
  await loadPurchasePlans();
};

const handlePurchasePlanSizeChange = async (pageSize: number) => {
  purchasePlanPageSize.value = pageSize;
  purchasePlanPage.value = 1;
  await loadPurchasePlans();
};

const openPage = (url: string) => {
  window.open(url, "_blank", "noopener,noreferrer");
};

const formatPriority = (value?: string | null) => {
  if (!value) return "-";
  return priorityLabels[value] || value;
};

const getPriorityRule = (value?: string | null) => {
  if (!value) return "优先级规则：高=电话、联系人、近90天采购、品类数>=3；中=有电话、有品类；低=信息较弱但可导入";
  return priorityRules[value] || value;
};

const getPriorityType = (value?: string | null) => {
  if (value === "High") return "danger";
  if (value === "Medium") return "warning";
  if (value === "Low") return "info";
  return "";
};

const formatPhoneType = (value?: string | null) => {
  if (!value) return "-";
  return phoneTypeLabels[value] || value;
};

const formatRole = (value?: string | null) => {
  if (!value) return "-";
  return roleLabels[value] || value;
};

const formatContactStatus = (value?: string | null) => {
  if (!value) return "-";
  return contactStatusLabels[value] || value;
};

const formatFollowType = (value?: string | null, fallback = "-") => {
  if (!value) return fallback;
  return followTypeLabels[value] || value;
};

const formatFollowResult = (value?: string | null, fallback = "-") => {
  if (!value) return fallback;
  return followResultLabels[value] || value;
};

const formatTransferOwner = (fromName?: string | null, toName?: string | null) => `${fromName || "未分配"} 至 ${toName || "未分配"}`;
const formatTransferAction = (actionType?: string | null) => ({
  ENTRY: "入库",
  ASSIGN: "分配",
  TRANSFER: "转交",
  RETURN: "退回",
})[actionType || ""] || "流转";
const canReturn = (row?: Partial<VendorDetail> | null) =>
  !!row?.ownerUserId && row.ownerUserId === userStore.userInfo.userId;

const formatDate = (value?: string | null) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;

  const pad = (num: number) => `${num}`.padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
};
</script>

<style scoped lang="scss">
.vendor-page {
  .vendor-link {
    padding: 0;
    font-weight: 600;
  }

  :deep(.wide-list-table .el-table__fixed-right) {
    box-shadow: -8px 0 14px -12px rgba(15, 23, 42, 0.28);
  }

  :deep(.wide-list-table .el-table__fixed-right::before) {
    display: none;
  }

  :deep(.wide-list-table .actions-column .cell) {
    gap: 2px;
    padding-left: 8px;
    padding-right: 8px;
    white-space: nowrap;
  }

  .cell-main {
    display: flex;
    min-width: 0;
    flex-direction: column;
    gap: 2px;
    line-height: 1.35;
  }

  .phone-text {
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }

  .filter-label-with-help {
    display: inline-flex;
    align-items: center;
    gap: 4px;
  }

  .filter-help-icon {
    color: var(--el-text-color-secondary);
    cursor: help;
    font-size: 15px;
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
  }

  .head-main {
    min-width: 0;
  }

  .title-row {
    display: flex;
    min-width: 0;
    flex-wrap: wrap;
    align-items: center;
    gap: 10px;
    margin-top: 6px;
  }

  .detail-kicker {
    margin-bottom: 6px;
    color: var(--el-text-color-secondary);
    font-size: 12px;
    font-weight: 600;
  }

  .eyebrow {
    color: var(--el-text-color-secondary);
    font-size: 12px;
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

    :deep(.el-button) {
      margin-left: 0;
    }
  }

  .head-meta span {
    color: var(--el-text-color-secondary);
    font-size: 13px;
  }

  .summary-band {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 12px;
    padding: 14px 0;
    background: #ffffff;

    > div {
      display: flex;
      min-width: 0;
      min-height: 76px;
      flex-direction: column;
      justify-content: center;
      gap: 5px;
      padding: 13px 16px;
      border: 1px solid var(--el-border-color-light);
      border-radius: 8px;
      background: #ffffff;
    }

    .label {
      color: var(--el-text-color-secondary);
      font-size: 12px;
    }

    strong {
      color: #111827;
      font-size: 15px;
      line-height: 1.35;
    }

    span:last-child {
      overflow-wrap: anywhere;
      color: var(--el-text-color-secondary);
    }
  }

  .detail-grid {
    display: grid;
    grid-template-columns: minmax(360px, 0.9fr) minmax(0, 1.75fr);
    gap: 16px;
    padding: 0 0 24px;
    background: #ffffff;
  }

  .detail-column {
    min-width: 0;
  }

  .detail-column > .detail-card + .detail-card {
    margin-top: 16px;
  }

  .detail-card {
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
      margin: 0;
      color: #111827;
      font-size: 16px;
      font-weight: 700;
    }
  }

  .section-title-first {
    margin-top: 0;
  }

  .product-tags {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    min-height: 40px;
    padding: 2px 0 4px;
  }

  .activity-row {
    display: flex;
    gap: 16px;
    margin-top: 16px;
  }

  .activity-panel {
    flex: 1;
    min-width: 0;
  }

  .follow-card {
    .follow-timeline {
      padding-top: 4px;
    }

    .follow-item {
      display: flex;
      flex-direction: column;
      gap: 6px;
      padding: 10px 12px;
      border: 1px solid var(--el-border-color-lighter);
      border-radius: 8px;
      background: #fbfdff;

      p {
        margin: 0;
        color: var(--el-text-color-primary);
        line-height: 1.55;
      }
    }

    .follow-title {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;

      strong {
        color: #111827;
      }
    }

    .muted {
      color: var(--el-text-color-secondary);
      font-size: 12px;
    }
  }

  .table-footer {
    display: flex;
    justify-content: flex-end;
    padding-top: 12px;
  }

  .ml8 {
    margin-left: 8px;
  }

  :deep(.actions-column .cell) {
    display: flex;
    justify-content: center;
  }
}

@media (max-width: 1200px) {
  .vendor-page {
    .summary-band,
    .detail-grid {
      grid-template-columns: 1fr;
    }

    .activity-row {
      flex-direction: column;
    }
  }
}
</style>









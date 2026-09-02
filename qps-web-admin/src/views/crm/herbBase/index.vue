<template>
  <div class="customer-page">
    <QueryPage api="/admin/crm/herb-base-subjects" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键词">
            <el-input v-model="searchForm.keyword" clearable placeholder="基地 / 主体 / 联系人 / 电话" />
          </el-form-item>
          <el-form-item label="主营品类">
            <el-select
              v-model="searchForm.mainProducts"
              multiple
              collapse-tags
              collapse-tags-tooltip
              clearable
              filterable
              remote
              reserve-keyword
              :remote-method="loadMainProductOptions"
              :loading="mainProductLoading"
              placeholder="主营品类"
            >
              <el-option v-for="item in mainProductOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <template #label>
              <span class="grade-filter-label">
                <span>等级</span>
                <el-tooltip placement="top" effect="light" :show-after="150">
                  <template #content>
                    <div class="grade-rule-tooltip">
                      <div>高：80-100 分</div>
                      <div>中：60-79 分</div>
                      <div>低：30-59 分</div>
                      <div>无效：0-29 分</div>
                    </div>
                  </template>
                  <el-icon class="grade-help-icon" title="查看等级分数区间"><QuestionFilled /></el-icon>
                </el-tooltip>
              </span>
            </template>
            <el-select v-model="searchForm.grade" clearable placeholder="等级">
              <el-option label="高" value="高" />
              <el-option label="中" value="中" />
              <el-option label="低" value="低" />
              <el-option label="无效" value="无效" />
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
        <Permission code="CRM_TRANSFER"><el-button :icon="Edit" @click="openTransferDialog()">批量流转</el-button></Permission>
        <Permission code="CRM_HERB_BASE_ADD"><el-button type="primary" :icon="Plus" @click="handleAdd">新增药材基地</el-button></Permission>
      </template>

      <template #table="{ tableData }">
        <el-table
          :data="tableData"
          :row-key="'id'"
          :row-class-name="getRowClassName"
          :fit="true"
          class="wide-list-table"
          style="--table-min-width: 2060px"
          border
          @selection-change="handleSelectionChange"
          @sort-change="handleSortChange"
        >
          <el-table-column type="selection" width="44" fixed="left" />
          <el-table-column label="基地主体" min-width="220" fixed="left" show-overflow-tooltip>
            <template #default="{ row }">
              <div class="cell-main">
                <el-button type="primary" link class="customer-link" @click="openDetail(row)">
                  {{ row.subjectName || "-" }}
                </el-button>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="基地数" width="82" align="right">
            <template #default="{ row }">
              <span class="count-cell">{{ row.baseCount || 0 }}</span>
            </template>
          </el-table-column>
          <el-table-column label="主联系人 / 电话" width="150">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ row.primaryContactName || "-" }}</span>
                <span class="phone-text">{{ row.primaryContactPhone || "-" }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="主营品类" width="148">
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
          <el-table-column prop="totalScale" label="总规模(亩)" width="118" align="right" sortable="custom">
            <template #default="{ row }">
              <span class="scale-cell">{{ formatListScale(row.totalScale) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="地区" min-width="180" show-overflow-tooltip>
            <template #default="{ row }">{{ formatRegions(row.regions) }}</template>
          </el-table-column>
          <el-table-column prop="score" width="160" sortable="custom">
            <template #header>
              <span class="score-column-header">
                <span>评分 / 等级</span>
                <el-tooltip placement="top" effect="light" :show-after="150">
                  <template #content>
                    <div class="score-rule-tooltip">
                      <strong>主体评分规则（满分 100 分）</strong>
                      <div>规模：&gt;0 为 10 分，&ge;100 为 15 分，&ge;200 为 20 分，&ge;500 为 25 分</div>
                      <div>基地数：1 个 5 分，2 个 8 分，&ge;3 个 10 分</div>
                      <div>主营品类：1 个 10 分，&ge;2 个 15 分</div>
                      <div>联系人：姓名 4 分，主体电话 12 分，有效联系人电话 4 分，无电话 2 分，最高 20 分</div>
                      <div>跟进：已成交 20 分，有意向 18 分，近 30 天有效跟进 14 分，跟进中 10 分</div>
                      <div>资料：地区 2 分，地址 2 分，备注 1 分</div>
                      <div>来源：人工录入/政府公示 5 分，行业平台 4 分，百度地图 3 分，其他 2 分（取最高来源分）</div>
                      <div>等级：高 80-100 分，中 60-79 分，低 30-59 分，无效 0-29 分</div>
                      <div>限制：无基地最高 59 分；无主体主要联系人电话最高 79 分；已流失为 0 分/无效</div>
                    </div>
                  </template>
                  <el-icon class="score-help-icon" title="查看评分规则"><QuestionFilled /></el-icon>
                </el-tooltip>
              </span>
            </template>
            <template #default="{ row }">
              <div class="score-cell">
                <strong>{{ row.score ?? 0 }}</strong>
                <el-tag size="small" :type="getGradeType(row.grade)">{{ formatGrade(row.grade) }}</el-tag>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="状态" width="88">
            <template #default="{ row }">
              <el-tag :type="getStatusType(row.status)">{{ formatCustomerStatus(row.status) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="最近沟通" min-width="170">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ formatFollowResult(row.lastFollowResult, "未沟通") }}</span>
                <span class="muted">{{ formatNullableDate(row.lastFollowAt) }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="下次跟进" width="138">
            <template #default="{ row }">
              <span :class="{ overdue: isOverdue(row.nextFollowAt) }">
                {{ formatNullableDate(row.nextFollowAt) }}
              </span>
            </template>
          </el-table-column>
          <el-table-column label="跟进人" width="96" show-overflow-tooltip>
            <template #default="{ row }">{{ row.ownerUserName || "-" }}</template>
          </el-table-column>
          <el-table-column prop="createdAt" label="创建时间" width="146" sortable="custom">
            <template #default="{ row }">{{ formatNullableDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" width="320" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <div class="table-actions">
                <el-button type="primary" link :icon="View" @click="openDetail(row)">详情</el-button>
                <el-button v-if="canManageTransfer" type="primary" link :icon="Edit" @click="openTransferDialog([row], row.ownerUserId ? 'TRANSFER' : 'ASSIGN')">
                  {{ row.ownerUserId ? "转交" : "分配" }}
                </el-button>
                <el-button v-if="canManageTransfer || canReturn(row)" type="primary" link :icon="Edit" @click="openTransferDialog([row], 'RETURN')">退回</el-button>
                <Permission code="CRM_FOLLOW"><el-button type="primary" link :icon="Phone" @click="openFollowDialog(row)">记录沟通</el-button></Permission>
              </div>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

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
            <el-option label="待联系" value="PENDING" />
            <el-option label="跟进中" value="FOLLOWING" />
            <el-option label="有意向" value="INTERESTED" />
            <el-option label="已成交" value="DEAL" />
            <el-option label="已流失" value="LOST" />
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
        <el-form-item label="主营品类">
          <el-select
            v-model="form.mainProducts"
            multiple
            collapse-tags
            collapse-tags-tooltip
            filterable
            remote
            reserve-keyword
            :remote-method="loadMainProductOptions"
            :loading="mainProductLoading"
            placeholder="请选择主营品类"
          >
            <el-option v-for="item in mainProductOptions" :key="item.value" :label="item.label" :value="item.value" />
          </el-select>
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
            <el-option v-for="item in sourcePlatformOptions" :key="item.value" :label="item.label" :value="item.value" />
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
              <span>类型 {{ currentHerbBase.subjectType || "-" }}</span>
              <span>评分 {{ currentHerbBase.score ?? 0 }}</span>
              <span>跟进人 {{ currentHerbBase.ownerUserName || "-" }}</span>
              <span>{{ formatRegions(currentHerbBase.regions) }}</span>
            </div>
            <p v-if="currentHerbBase.remark" class="head-remark">{{ currentHerbBase.remark }}</p>
          </div>
          <div class="head-actions">
            <Permission code="CRM_FOLLOW"><el-button type="primary" :icon="Phone" @click="openFollowDialog(currentHerbBase)">记录沟通</el-button></Permission>
            <Permission code="CRM_HERB_BASE_CONTACT_ADD"><el-button :icon="Plus" @click="openContactDialog()">新增联系人</el-button></Permission>
            <el-button v-if="canManageTransfer" :icon="Edit" @click="openTransferDialog([currentHerbBase], currentHerbBase.ownerUserId ? 'TRANSFER' : 'ASSIGN')">
              {{ currentHerbBase.ownerUserId ? "转交" : "分配" }}
            </el-button>
            <el-button v-if="canManageTransfer || canReturn(currentHerbBase)" :icon="Edit" @click="openTransferDialog([currentHerbBase], 'RETURN')">退回</el-button>
            <Permission code="CRM_HERB_BASE_EDIT"><el-button :icon="Edit" @click="openSubjectDialog">编辑主体</el-button></Permission>
            <Permission code="CRM_HERB_BASE_STATUS"><el-button type="primary" plain @click="markCustomerStatus('INTERESTED')">标记有意向</el-button></Permission>
            <Permission code="CRM_HERB_BASE_STATUS"><el-button type="success" plain @click="markCustomerStatus('DEAL')">标记成交</el-button></Permission>
            <Permission code="CRM_HERB_BASE_STATUS"><el-button type="danger" plain @click="markCustomerStatus('LOST')">标记流失</el-button></Permission>
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
          </div>
          <div class="summary-card">
            <span class="label">基地数</span>
            <strong>{{ currentHerbBase.baseCount || 0 }}</strong>
          </div>
          <div class="summary-card">
            <span class="label">总规模(亩)</span>
            <strong>{{ formatScale(currentHerbBase.totalScale) }}</strong>
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
                <h3>基地明细</h3>
                <Permission code="CRM_HERB_BASE_ADD"><el-button type="primary" link :icon="Plus" @click="handleAdd">新增基地</el-button></Permission>
              </div>
              <el-empty v-if="!currentHerbBase.herbBases?.length" description="暂无基地明细" />
              <div v-else class="base-card-list">
                <article v-for="base in currentHerbBase.herbBases" :key="base.id || base.baseName" class="base-card">
                  <div class="base-card-head">
                    <h4>{{ base.baseName || base.herbBaseName || "-" }}</h4>
                    <el-tag size="small" type="info" effect="plain">{{ formatSourcePlatform(base.sourcePlatform) }}</el-tag>
                  </div>
                  <div class="base-card-products">{{ formatMainProducts(base) }}</div>
                  <div class="base-card-fields">
                    <div>
                      <span>规模(亩)</span>
                      <strong>{{ formatScale(base.scale) }}</strong>
                    </div>
                    <div>
                      <span>地区</span>
                      <strong>{{ formatRegion(base) }}</strong>
                    </div>
                    <div>
                      <span>地址</span>
                      <strong>{{ base.address || "-" }}</strong>
                    </div>
                  </div>
                  <div class="base-card-actions">
                    <Permission code="CRM_HERB_BASE_EDIT">
                      <el-button type="primary" link @click="handleEdit(base)">编辑</el-button>
                    </Permission>
                    <Permission code="CRM_HERB_BASE_DELETE">
                      <el-button type="danger" link @click="deleteBase(base)">删除</el-button>
                    </Permission>
                  </div>
                  <div class="base-supply-list">
                    <div class="section-title"><h4>供应信息</h4></div>
                    <el-empty v-if="!base.supplies?.length" description="暂无供应信息" :image-size="48" />
                    <el-table v-else :data="base.supplies" size="small" border>
                      <el-table-column prop="productName" label="品类" min-width="100" />
                      <el-table-column label="可供量" width="130"><template #default="{ row }">{{ row.availableQuantity ?? '-' }} {{ row.quantityUnit }}</template></el-table-column>
                      <el-table-column prop="specification" label="规格" min-width="100" />
                      <el-table-column prop="supplyCycle" label="供货周期" min-width="110" />
                      <el-table-column label="状态" width="100"><template #default="{ row }"><el-tag size="small" :type="row.isExpired ? 'danger' : row.status === '有效' ? 'success' : 'info'">{{ row.isExpired ? '已过期' : row.status }}</el-tag></template></el-table-column>
                      <el-table-column label="有效期" width="120"><template #default="{ row }">{{ formatNullableDate(row.validUntil) }}</template></el-table-column>
                    </el-table>
                  </div>
                </article>
              </div>
            </section>

            <section class="detail-card detail-contacts-panel">
              <div class="section-title section-title-first">
                <h3>联系人</h3>
                <Permission code="CRM_HERB_BASE_CONTACT_ADD"><el-button type="primary" link :icon="Plus" @click="openContactDialog()">新增</el-button></Permission>
              </div>
              <el-table :data="contacts" class="contacts-table" border>
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
                <el-table-column prop="remark" label="备注" min-width="240" show-overflow-tooltip />
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
                <Permission code="CRM_FOLLOW"><el-button type="primary" link :icon="Phone" @click="openFollowDialog(currentHerbBase)">记录</el-button></Permission>
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

            <section class="detail-card detail-transfer-panel">
              <div class="section-title section-title-first">
                <h3>流转记录</h3>
              </div>
              <el-timeline>
                <el-timeline-item
                  v-for="record in transferRecords"
                  :key="record.id"
                  :timestamp="formatNullableDate(record.createdAt)"
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
                <el-empty v-if="transferRecords.length === 0" description="暂无流转记录" />
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
  </div>
</template>

<script setup lang="ts" name="customer">
import { computed, onMounted, reactive, ref } from "vue";
import { useRoute } from "vue-router";
import { Delete, Edit, Phone, Plus, QuestionFilled, View } from "@element-plus/icons-vue";
import { ElMessage } from "element-plus";
import QueryPage from "@/components/QueryPage/index.vue";
import ChinaRegionCascader from "@/components/ChinaRegionCascader/index.vue";
import { crmHerbBaseApi } from "@/api/modules/crmHerbBase";
import { userApi } from "@/api/modules/user";
import Permission from "@/components/Permission/index.vue";
import { useAuthStore } from "@/stores/modules/auth";
import { useUserStore } from "@/stores/modules/user";

interface HerbBaseSubjectDetail {
  id: string;
  subjectName?: string;
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
  herbBases?: any[];
  transferRecords?: any[];
  createdAt: string;
  updatedAt: string;
}

const queryPageRef = ref();
const route = useRoute();
const authStore = useAuthStore();
const userStore = useUserStore();

const dialogVisible = ref(false);
const subjectDialogVisible = ref(false);
const detailDrawerVisible = ref(false);
const contactDialogVisible = ref(false);
const followDialogVisible = ref(false);
const transferDialogVisible = ref(false);
const isEdit = ref(false);
const followFilter = ref("");
const currentHerbBase = ref<HerbBaseSubjectDetail | null>(null);
const contacts = ref<any[]>([]);
const followRecords = ref<any[]>([]);
const transferRecords = ref<any[]>([]);
const selectedHerbBases = ref<HerbBaseSubjectDetail[]>([]);
const ownerOptions = ref<any[]>([]);
const regionPath = ref<string[]>([]);

const searchForm = reactive({
  keyword: "",
  herbBaseName: "",
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
  herbBaseSubjectId: undefined as string | undefined,
  subjectName: "",
  herbBaseName: "",
  mainProducts: [] as string[],
  scale: undefined as number | undefined,
  province: "",
  city: "",
  area: "",
  address: "",
  lat: undefined as number | undefined,
  lng: undefined as number | undefined,
  sourcePlatform: "BAIDU_MAP",
  sourceId: undefined as number | undefined,
  status: "PENDING",
  remark: "",
  primaryContactName: "",
  primaryContactPhone: "",
});

const subjectForm = reactive({
  id: "",
  subjectName: "",
  subjectType: "",
  status: "PENDING",
  remark: "",
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

const mainProductOptions = ref<Array<{ label: string; value: string }>>([]);
const mainProductLoading = ref(false);

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

const toMainProductOption = (item: any) => {
  const value = String(item?.value || item?.attributeValue || "").trim();
  return value ? { label: formatEnumLabel(mainProductLabels, value, value), value } : null;
};

let mainProductTimer: ReturnType<typeof setTimeout> | undefined;

const loadMainProductOptions = (keyword = "") => {
  if (mainProductTimer) clearTimeout(mainProductTimer);
  mainProductTimer = setTimeout(async () => {
    mainProductLoading.value = true;
    try {
      const response = await crmHerbBaseApi.getBusinessEntityAttributeOptions({
        entityType: "CRM_HERB_BASE",
        attributeCode: "CRM_MAIN_PRODUCT",
        keyword: keyword.trim(),
        pageSize: 100,
      });
      const options = (response.data || [])
        .map(toMainProductOption)
        .filter(Boolean) as Array<{ label: string; value: string }>;
      mainProductOptions.value = options;
    } finally {
      mainProductLoading.value = false;
    }
  }, 250);
};

const formatMainProducts = (row: any, fallback = "-") => {
  const values = normalizeMainProducts(row);
  if (values.length === 0) return fallback;
  return values.map(value => formatEnumLabel(mainProductLabels, value)).join("、");
};

const formatSourcePlatform = (value?: string | null, fallback = "-") => formatEnumLabel(sourcePlatformLabels, value, fallback);
const formatCustomerStatus = (value?: string | null, fallback = "-") => formatEnumLabel(statusLabels, value, fallback);
const formatGrade = (value?: string | null, fallback = "-") => value || fallback;
const formatContactRole = (value?: string | null, fallback = "-") => formatEnumLabel(contactRoleLabels, value, fallback);
const formatFollowType = (value?: string | null, fallback = "-") => formatEnumLabel(followTypeLabels, value, fallback);
const formatFollowResult = (value?: string | null, fallback = "-") => formatEnumLabel(followResultLabels, value, fallback);
const getBaseName = (row: any) => row?.baseName?.trim?.() || row?.herbBaseName?.trim?.() || "";
const getDetailTitle = (row: Partial<HerbBaseSubjectDetail> | any) => row?.subjectName?.trim?.() || "";
const getUserDisplayName = (user: any) => user.realName || user.username || user.name || "-";
const formatTransferOwner = (fromName?: string | null, toName?: string | null) => `${fromName || "未分配"} 至 ${toName || "未分配"}`;
const formatTransferAction = (actionType?: string | null) => ({
  ENTRY: "入库",
  ASSIGN: "分配",
  TRANSFER: "转交",
  RETURN: "退回",
})[actionType || ""] || "流转";
const canReturn = (row?: Partial<HerbBaseSubjectDetail> | null) =>
  !!row?.ownerUserId && row.ownerUserId === userStore.userInfo.userId;
const formatScale = (value?: number | string | null) => {
  if (value === null || value === undefined || value === "") return "-";
  const numberValue = Number(value);
  return Number.isFinite(numberValue) ? numberValue.toLocaleString("zh-CN", { maximumFractionDigits: 2 }) : "-";
};
const formatListScale = (value?: number | string | null) => {
  if (value === null || value === undefined || value === "") return "-";
  const numberValue = Number(value);
  return Number.isFinite(numberValue) ? Math.round(numberValue).toLocaleString("zh-CN") : "-";
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
    herbBaseSubjectId: currentHerbBase.value?.id,
    subjectName: "",
    herbBaseName: "",
    mainProducts: [],
    scale: undefined,
    province: "",
    city: "",
    area: "",
    address: "",
    lat: undefined,
    lng: undefined,
    sourcePlatform: "BAIDU_MAP",
    sourceId: undefined,
    status: "PENDING",
    remark: "",
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
    herbBaseSubjectId: row.herbBaseSubjectId || currentHerbBase.value?.id,
    subjectName: row.subjectName || currentHerbBase.value?.subjectName || "",
    herbBaseName: getBaseName(row),
    mainProducts: normalizeMainProducts(row),
    scale: row.scale ?? undefined,
    province: row.province || "",
    city: row.city || "",
    area: row.area || "",
    address: row.address || "",
    lat: row.lat ?? undefined,
    lng: row.lng ?? undefined,
    sourcePlatform: toEnumValue(sourcePlatformValues, row.sourcePlatform, "BAIDU_MAP"),
    sourceId: row.sourceId ?? undefined,
    status: toEnumValue(statusValues, row.status, "PENDING"),
    remark: row.remark || "",
    primaryContactName: row.primaryContactName || "",
    primaryContactPhone: row.primaryContactPhone || "",
  });
  syncRegionPathFromForm();
  dialogVisible.value = true;
};

const reloadList = () => {
  queryPageRef.value?.getTableList();
};

const handleSortChange = ({ prop, order }: { prop: "totalScale" | "score" | "createdAt"; order: "ascending" | "descending" | null }) => {
  const sortFieldMap = {
    totalScale: "TotalScale",
    score: "Score",
    createdAt: "CreatedAt",
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
  transferRecords.value = response.data?.transferRecords || [];
};

const openDetail = async (row: any) => {
  detailDrawerVisible.value = false;
  currentHerbBase.value = null;
  contacts.value = [];
  followRecords.value = [];
  transferRecords.value = [];
  await loadCustomerDetail(row.id);
  detailDrawerVisible.value = true;
};

const openSubjectDialog = () => {
  if (!currentHerbBase.value) return;
  Object.assign(subjectForm, {
    id: currentHerbBase.value.id,
    subjectName: currentHerbBase.value.subjectName || "",
    subjectType: currentHerbBase.value.subjectType || "",
    status: toEnumValue(statusValues, currentHerbBase.value.status, "PENDING"),
    remark: currentHerbBase.value.remark || "",
  });
  subjectDialogVisible.value = true;
};

const handleSubmit = async () => {
  if (!form.baseName) {
    ElMessage.error("请输入基地名称");
    return;
  }

  const request = {
    ...form,
    herbBaseName: form.baseName,
    herbBaseSubjectId: form.herbBaseSubjectId || currentHerbBase.value?.id,
    subjectName: currentHerbBase.value?.subjectName || form.subjectName || "",
    mainProducts: [...form.mainProducts],
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
  if (currentHerbBase.value) {
    await loadCustomerDetail(currentHerbBase.value.id);
  }
  reloadList();
};

const deleteBase = async (base: any) => {
  if (!base?.id || !window.confirm("确定删除这个基地明细吗？")) return;
  await crmHerbBaseApi.deleteCustomer(base.id);
  ElMessage.success("基地已删除");
  if (currentHerbBase.value) {
    await loadCustomerDetail(currentHerbBase.value.id);
  }
  reloadList();
};

const openTransferDialog = async (rows?: HerbBaseSubjectDetail[], mode: "ASSIGN" | "TRANSFER" | "RETURN" = "TRANSFER") => {
  const customers = rows?.length ? rows : selectedHerbBases.value;
  if (customers.length === 0) {
    ElMessage.warning("请选择要流转的基地主体");
    return;
  }

  transferMode.value = mode;
  Object.assign(transferForm, {
    entityIds: customers.map(customer => customer.id),
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
  if (currentHerbBase.value && transferForm.entityIds.includes(currentHerbBase.value.id)) {
    await loadCustomerDetail(currentHerbBase.value.id);
  }
  ElMessage.success("流转成功");
  transferDialogVisible.value = false;
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
  void loadMainProductOptions();
  void applyRouteEntrypoint();
});

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
    followType: toEnumValue(followTypeValues, followForm.followType, "PHONE"),
    followResult: toEnumValue(followResultValues, followForm.followResult),
    intentLevel: followForm.intentLevel,
    nextFollowAt: followForm.nextFollowAt || null,
  });
  ElMessage.success("沟通记录已保存");
  followDialogVisible.value = false;
  await loadCustomerDetail(currentHerbBase.value.id);
  reloadList();
};

const disablePastFollowDate = (date: Date) => date.getTime() < new Date().setHours(0, 0, 0, 0);

const submitSubject = async () => {
  if (!subjectForm.subjectName) {
    ElMessage.error("请输入主体名称");
    return;
  }

  await crmHerbBaseApi.updateSubject(subjectForm.id, {
    subjectName: subjectForm.subjectName,
    subjectType: subjectForm.subjectType,
    status: toEnumValue(statusValues, subjectForm.status, "PENDING"),
    remark: subjectForm.remark || "",
  });
  ElMessage.success("主体已保存");
  subjectDialogVisible.value = false;
  await loadCustomerDetail(subjectForm.id);
  reloadList();
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

  .score-column-header {
    display: inline-flex;
    align-items: center;
    gap: 4px;
  }

  .grade-filter-label {
    display: inline-flex;
    align-items: center;
    gap: 3px;
  }

  .grade-help-icon {
    color: var(--el-text-color-secondary);
    cursor: help;
    font-size: 14px;
  }

  .score-help-icon {
    color: var(--el-text-color-secondary);
    cursor: help;
    font-size: 18px;
  }

  .score-rule-tooltip {
    max-width: 520px;
    line-height: 1.7;
    white-space: normal;
  }

  .score-rule-tooltip strong {
    display: block;
    margin-bottom: 4px;
    color: var(--el-text-color-primary);
  }

  .grade-rule-tooltip {
    line-height: 1.8;
    white-space: nowrap;
  }

  .scale-cell {
    font-weight: 700;
  }

  .count-cell {
    font-weight: 700;
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
  :deep(.row-overdue) {
    background: #fff7f7;
  }
}

.drawer-layout {
  min-height: 100%;
  padding: 0;
  background: #ffffff;
  overscroll-behavior: contain;
}

.drawer-head {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(240px, auto);
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

  .head-remark {
    max-width: 900px;
    margin: 10px 0 0;
    color: var(--el-text-color-secondary);
    font-size: 13px;
    line-height: 1.6;
    overflow-wrap: anywhere;
  }

  .head-actions {
    justify-content: flex-end;
    max-width: 560px;
    min-width: 0;

    :deep(.el-button) {
      margin-left: 0;
    }
  }
}

.summary-band {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
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

.detail-contacts-panel {
  overflow-x: auto;
}

.detail-contacts-panel :deep(.contacts-table) {
  min-width: 980px;
}

.detail-follow-panel,
.detail-transfer-panel {
  min-height: 220px;
  max-height: calc((100vh - 252px) / 2);
  overflow: auto;
  overscroll-behavior: contain;
}

.base-card-list {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}

.base-card {
  position: relative;
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  min-height: 178px;
  padding: 14px 14px 42px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  background: #fbfdff;
  transition:
    border-color 0.15s ease,
    box-shadow 0.15s ease,
    background-color 0.15s ease;
}

.base-card-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;

  h4 {
    margin: 0;
    color: #111827;
    font-size: 15px;
    line-height: 1.4;
    overflow-wrap: anywhere;
    display: -webkit-box;
    overflow: hidden;
    -webkit-box-orient: vertical;
    -webkit-line-clamp: 2;
  }
}

.base-card-head :deep(.el-tag) {
  flex: 0 0 auto;
}

.base-card-actions {
  position: absolute;
  right: 14px;
  bottom: 12px;
  display: flex;
  gap: 10px;

  :deep(.el-button) {
    margin-left: 0;
    padding: 0;
  }
}

.base-card:hover,
.base-card:focus-within {
  border-color: var(--el-color-primary-light-7);
  background: #ffffff;
  box-shadow: 0 6px 18px rgba(17, 24, 39, 0.05);
}

.base-card-products {
  margin-top: 8px;
  color: var(--el-color-primary);
  font-size: 13px;
  line-height: 1.5;
  display: -webkit-box;
  min-height: 20px;
  overflow: hidden;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 1;
}

.base-card-fields {
  display: grid;
  gap: 8px;
  margin-top: 12px;

  div {
    display: grid;
    grid-template-columns: 72px minmax(0, 1fr);
    gap: 10px;
    align-items: baseline;
  }

  span {
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }

  strong {
    color: var(--el-text-color-primary);
    font-size: 13px;
    font-weight: 500;
    line-height: 1.5;
    overflow-wrap: anywhere;
  }

  div:last-child strong {
    display: -webkit-box;
    overflow: hidden;
    -webkit-box-orient: vertical;
    -webkit-line-clamp: 2;
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
  padding: 0 0 12px;
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
    overflow-wrap: anywhere;
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

  .detail-follow-panel,
  .detail-transfer-panel {
    max-height: none;
  }

  .base-card-list {
    grid-template-columns: minmax(0, 1fr);
  }
}

@media (max-width: 520px) {
  .base-card-list {
    grid-template-columns: minmax(0, 1fr);
  }

  .base-card {
    width: 100%;
  }
}
</style>

















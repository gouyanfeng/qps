<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/campaigns" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="请输入活动名称" clearable />
          </el-form-item>
          <el-form-item label="类型">
            <el-select v-model="searchForm.type" placeholder="全部" clearable>
              <el-option label="商品推荐" :value="0" />
              <el-option label="领券型" :value="2" />
            </el-select>
          </el-form-item>
          <el-form-item label="上下架">
            <el-select v-model="searchForm.isActive" placeholder="全部">
              <el-option label="全部" value="" />
              <el-option label="已上架" :value="true" />
              <el-option label="已下架" :value="false" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增专题活动</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column label="头图" width="110" align="center">
            <template #default="{ row }">
              <el-image
                v-if="row.coverImageUrl"
                :src="resolveFileUrl(row.coverImageUrl)"
                :preview-src-list="[resolveFileUrl(row.coverImageUrl)]"
                preview-teleported
                fit="cover"
                class="activity-cover"
              />
              <span v-else class="empty-image">-</span>
            </template>
          </el-table-column>
          <el-table-column prop="name" label="活动名称" min-width="220" />
          <el-table-column label="活动类型" width="110" align="center">
            <template #default="{ row }">
              <el-tag :type="row.type === 2 ? 'warning' : 'primary'" effect="plain">
                {{ row.type === 2 ? '领券型' : '商品推荐' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="活动时间" width="250">
            <template #default="{ row }">
              <span class="activity-time">{{ formatDate(row.startAt) }} ~ {{ formatDate(row.endAt) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="活动状态" width="140" align="center">
            <template #default="{ row }">
              <el-tag v-if="!row.isActive" type="info">已下架</el-tag>
              <el-tag v-else :type="statusTagType(row.status)">{{ statusText(row.status) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="productCount" label="商品数" width="90" align="center" />
          <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
          <el-table-column
            label="操作"
            align="center"
            width="220"
            fixed="right"
            class-name="actions-column"
            header-class-name="actions-column"
          >
            <template #default="{ row }">
              <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
              <el-button :type="row.isActive ? 'warning' : 'success'" link @click="changeStatus(row)">
                {{ row.isActive ? '下架' : '上架' }}
              </el-button>
              <el-button type="danger" link :icon="Delete" @click="remove(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="900px"
      destroy-on-close
      class="campaign-dialog"
    >
      <div v-loading="loading">
        <el-form :model="form" label-width="100px">
          <el-tabs v-model="activeTab" type="border-card" class="campaign-tabs">
            <el-tab-pane label="基本信息" name="basic">
              <el-row :gutter="16">
                <el-col :span="12">
                  <el-form-item label="活动名称" required>
                    <el-input v-model="form.name" maxlength="100" show-word-limit placeholder="请输入活动名称" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="活动类型" required>
                    <el-radio-group v-model="form.type">
                      <el-radio :value="0">商品推荐型</el-radio>
                      <el-radio :value="2">领券型</el-radio>
                    </el-radio-group>
                  </el-form-item>
                </el-col>
              </el-row>
              <el-form-item label="活动时间" required>
                <el-date-picker
                  v-model="form.dateRange"
                  type="datetimerange"
                  range-separator="至"
                  start-placeholder="开始时间"
                  end-placeholder="结束时间"
                  value-format="YYYY-MM-DDTHH:mm:ss"
                  style="width: 100%"
                />
              </el-form-item>
              <el-row :gutter="16">
                <el-col :span="12">
                  <el-form-item label="排序">
                    <el-input-number v-model="form.sortOrder" :min="0" :max="9999" style="width: 100%" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="上下架">
                    <el-switch v-model="form.isActive" active-text="上架" inactive-text="下架" />
                  </el-form-item>
                </el-col>
              </el-row>
              <el-form-item label="活动头图">
                <UploadImg v-model:imageUrl="coverImageModel" scene="activity" :width="'120px'" :height="'120px'">
                  <template #tip>建议上传 1:1 图片，大小不超过 5MB</template>
                </UploadImg>
              </el-form-item>

              <el-form-item label="活动内容" class="campaign-content-item">
                <div v-if="form.type === 0" class="campaign-content-section">
                  <div class="tab-toolbar">
                    <span class="tab-tip">共 {{ form.items.length }} 个推荐商品，可调整顺序与分组</span>
                    <el-button type="primary" plain :icon="Plus" @click="openProductDialog">添加商品</el-button>
                  </div>

                  <el-table v-if="form.items.length" :data="form.items" border style="width: 100%">
                    <el-table-column label="排序" width="100" align="center">
                      <template #default="{ $index }">
                        <div class="sort-actions">
                          <span class="sort-index">{{ $index + 1 }}</span>
                          <el-button link :icon="Top" :disabled="$index === 0" @click="moveItem($index, -1)" />
                          <el-button
                            link
                            :icon="Bottom"
                            :disabled="$index === form.items.length - 1"
                            @click="moveItem($index, 1)"
                          />
                        </div>
                      </template>
                    </el-table-column>
                    <el-table-column label="商品" min-width="200">
                      <template #default="{ row }">
                        <div class="product-cell">
                          <el-image
                            v-if="row.mainImageUrl"
                            :src="resolveFileUrl(row.mainImageUrl)"
                            fit="cover"
                            class="product-thumb"
                          />
                          <span v-else class="product-thumb product-thumb-empty">-</span>
                          <div class="product-info">
                            <div class="product-name">{{ row.productName }}</div>
                            <div class="product-code">{{ row.productCode }}</div>
                          </div>
                        </div>
                      </template>
                    </el-table-column>
                    <el-table-column label="最低售价" width="100" align="center">
                      <template #default="{ row }">
                        <span v-if="row.minPrice !== null">{{ formatPrice(row.minPrice) }}</span>
                        <span v-else>-</span>
                      </template>
                    </el-table-column>
                    <el-table-column label="分组名" width="150">
                      <template #default="{ row }">
                        <el-input v-model="row.groupName" maxlength="50" placeholder="可空，留空归入未分组" />
                      </template>
                    </el-table-column>
                    <el-table-column label="商品状态" width="90" align="center">
                      <template #default="{ row }">
                        <el-tag :type="row.productIsActive ? 'success' : 'info'" size="small">
                          {{ row.productIsActive ? '上架中' : '已下架' }}
                        </el-tag>
                      </template>
                    </el-table-column>
                    <el-table-column label="操作" width="80" align="center">
                      <template #default="{ $index }">
                        <el-button type="danger" link :icon="Delete" @click="removeItem($index)">移除</el-button>
                      </template>
                    </el-table-column>
                  </el-table>
                  <div v-else class="products-empty">尚未选择商品，点击右上角「添加商品」从商品库中挑选。</div>
                </div>

                <div v-else class="campaign-content-section">
                  <div class="tab-toolbar">
                    <span class="tab-tip">共 {{ form.coupons.length }} 张优惠券，可调整领取顺序</span>
                    <el-button type="primary" plain :icon="Plus" @click="openCouponDialog">选择券</el-button>
                  </div>

                  <el-table v-if="form.coupons.length" :data="form.coupons" border style="width: 100%">
                    <el-table-column label="排序" width="100" align="center">
                      <template #default="{ $index }">
                        <div class="sort-actions">
                          <span class="sort-index">{{ $index + 1 }}</span>
                          <el-button link :icon="Top" :disabled="$index === 0" @click="moveCoupon($index, -1)" />
                          <el-button
                            link
                            :icon="Bottom"
                            :disabled="$index === form.coupons.length - 1"
                            @click="moveCoupon($index, 1)"
                          />
                        </div>
                      </template>
                    </el-table-column>
                    <el-table-column label="优惠券" min-width="160">
                      <template #default="{ row }">
                        <div class="coupon-name">{{ row.name }}</div>
                        <div class="coupon-code">{{ row.code }}</div>
                      </template>
                    </el-table-column>
                    <el-table-column label="优惠内容" width="120">
                      <template #default="{ row }">
                        <span class="coupon-value">{{ couponValueText(row) }}</span>
                      </template>
                    </el-table-column>
                    <el-table-column label="门槛" width="100" align="center">
                      <template #default="{ row }">
                        <span v-if="row.thresholdAmount > 0">满 {{ formatMoney(row.thresholdAmount) }}</span>
                        <span v-else>无门槛</span>
                      </template>
                    </el-table-column>
                    <el-table-column label="库存" width="100" align="center">
                      <template #default="{ row }">{{ row.remainCount }} / {{ row.totalCount }}</template>
                    </el-table-column>
                    <el-table-column label="每人限领" width="90" align="center">
                      <template #default="{ row }">{{ row.perLimit }} 张</template>
                    </el-table-column>
                    <el-table-column label="操作" width="80" align="center">
                      <template #default="{ $index }">
                        <el-button type="danger" link :icon="Delete" @click="removeCoupon($index)">移除</el-button>
                      </template>
                    </el-table-column>
                  </el-table>
                  <div v-else class="products-empty">尚未选择优惠券，点击右上角「选择券」从券库中挑选。</div>
                </div>
              </el-form-item>
            </el-tab-pane>

            <el-tab-pane label="图文详情" name="detail" lazy>
              <el-form-item label="活动图文" class="full-editor">
                <div class="campaign-content-editor">
                  <ElementTiptap
                    v-model:content="form.contentHtml"
                    :extensions="extensions"
                    :locale="editorLocale"
                    height="460px"
                  />
                  <div class="content-editor-hint" :class="{ 'is-over-limit': form.contentHtml.length > 50000 }">
                    HTML 长度 {{ form.contentHtml.length.toLocaleString() }} / 50,000 字符；图片请使用工具栏上传。
                  </div>
                </div>
              </el-form-item>
            </el-tab-pane>
          </el-tabs>
        </el-form>
      </div>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="dialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="submitting" @click="submitForm">确定</el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 商品选择弹窗 -->
    <el-dialog v-model="productDialogVisible" title="选择商品" width="1000px" top="6vh" destroy-on-close>
      <div class="product-search-bar">
        <el-input
          v-model="productSearch.keyword"
          placeholder="搜索商品名称 / 编码 / 标签"
          clearable
          style="width: 320px"
          @keyup.enter="searchProducts(1)"
          @clear="searchProducts(1)"
        />
        <el-button type="primary" :icon="Search" :loading="productSearch.loading" @click="searchProducts(1)">
          查询
        </el-button>
        <span class="product-search-tip">已选 {{ form.items.length }} 个商品</span>
      </div>

      <el-table v-loading="productSearch.loading" :data="productSearch.rows" border style="width: 100%">
        <el-table-column label="商品" min-width="300">
          <template #default="{ row }">
            <div class="product-cell">
              <el-image
                v-if="row.mainImageUrl"
                :src="resolveFileUrl(row.mainImageUrl)"
                fit="cover"
                class="product-thumb"
              />
              <span v-else class="product-thumb product-thumb-empty">-</span>
              <div class="product-info">
                <div class="product-name">{{ row.name }}</div>
                <div class="product-code">{{ row.code }}</div>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="最低售价" width="120" align="center">
          <template #default="{ row }">
            <span v-if="minPriceOf(row) !== null">{{ formatPrice(minPriceOf(row)) }}</span>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="商品状态" width="110" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'" size="small">
              {{ row.isActive ? '上架中' : '已下架' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="110" align="center">
          <template #default="{ row }">
            <el-button v-if="!isSelected(row.id)" type="primary" link :icon="Plus" @click="addProduct(row)">
              添加
            </el-button>
            <span v-else class="selected-tip">已添加</span>
          </template>
        </el-table-column>
      </el-table>

      <div class="product-pagination">
        <el-pagination
          v-model:current-page="productSearch.page"
          :page-size="productSearch.pageSize"
          :total="productSearch.total"
          layout="total, prev, pager, next"
          background
          @current-change="searchProducts"
        />
      </div>

      <template #footer>
        <el-button type="primary" @click="productDialogVisible = false">完成</el-button>
      </template>
    </el-dialog>

    <!-- 选券弹窗 -->
    <el-dialog v-model="couponDialogVisible" title="选择优惠券" width="900px" top="8vh" destroy-on-close>
      <div class="product-search-bar">
        <el-input
          v-model="couponSearch.keyword"
          placeholder="搜索券名称 / 券码"
          clearable
          style="width: 300px"
          @keyup.enter="searchCoupons(1)"
          @clear="searchCoupons(1)"
        />
        <el-button type="primary" :icon="Search" :loading="couponSearch.loading" @click="searchCoupons(1)">
          查询
        </el-button>
        <span class="product-search-tip">已选 {{ form.coupons.length }} 张</span>
      </div>

      <el-table v-loading="couponSearch.loading" :data="couponSearch.rows" border style="width: 100%">
        <el-table-column label="优惠券" min-width="220">
          <template #default="{ row }">
            <div class="coupon-name">{{ row.name }}</div>
            <div class="coupon-code">{{ row.code }}</div>
          </template>
        </el-table-column>
        <el-table-column label="优惠内容" width="160">
          <template #default="{ row }">
            <span class="coupon-value">{{ couponValueText(row) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="门槛" width="120" align="center">
          <template #default="{ row }">
            <span v-if="row.thresholdAmount > 0">满 {{ formatMoney(row.thresholdAmount) }}</span>
            <span v-else>无门槛</span>
          </template>
        </el-table-column>
        <el-table-column label="库存" width="120" align="center">
          <template #default="{ row }">{{ row.remainCount }} / {{ row.totalCount }}</template>
        </el-table-column>
        <el-table-column label="操作" width="110" align="center">
          <template #default="{ row }">
            <el-button v-if="!isCouponSelected(row.id)" type="primary" link :icon="Plus" @click="addCoupon(row)">
              添加
            </el-button>
            <span v-else class="selected-tip">已添加</span>
          </template>
        </el-table-column>
      </el-table>

      <div class="product-pagination">
        <el-pagination
          v-model:current-page="couponSearch.page"
          :page-size="couponSearch.pageSize"
          :total="couponSearch.total"
          layout="total, prev, pager, next"
          background
          @current-change="searchCoupons"
        />
      </div>

      <template #footer>
        <el-button type="primary" @click="couponDialogVisible = false">完成</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="mallCampaign">
import { computed, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Bottom, CirclePlus, Delete, EditPen, Plus, Search, Top } from '@element-plus/icons-vue'
import { ElementTiptap } from 'element-tiptap'
import editorLocale from 'element-tiptap/lib/locales/zh'
import 'element-tiptap/lib/style.css'
import { mallCampaignApi } from '@/api/modules/mallCampaign'
import { mallProductApi } from '@/api/modules/mallProduct'
import QueryPage from '@/components/QueryPage/index.vue'
import UploadImg from '@/components/Upload/Img.vue'
import { formatDate, resolveFileUrl } from '@/utils'
import { extensions } from './tiptapExtensions'

interface CampaignItem {
  id?: string
  productId: string
  productName: string
  productCode: string
  mainImageUrl: string
  minPrice: number | null
  productIsActive: boolean
  groupName: string
  sortOrder: number
}

interface CampaignCoupon {
  id?: string
  couponId: string
  name: string
  code: string
  couponType: string
  thresholdAmount: number
  discountAmount: number
  maxDiscount: number | null
  totalCount: number
  remainCount: number
  perLimit: number
  scopeType: string
  isActive: boolean
  isClaimable: boolean
  sortOrder: number
}

const queryPageRef = ref()
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')
const submitting = ref(false)
const loading = ref(false)
const activeTab = ref('basic')

const searchForm = reactive({
  keyword: '',
  type: '' as number | '',
  isActive: '' as boolean | ''
})

const form = reactive({
  type: 0,
  name: '',
  coverImageUrl: '',
  contentHtml: '',
  dateRange: [] as string[],
  sortOrder: 0,
  isActive: true,
  items: [] as CampaignItem[],
  coupons: [] as CampaignCoupon[]
})

const apiOrigin = (import.meta.env.VITE_API_URL || 'http://localhost:5000/api').replace(/\/api\/?$/, '')

const coverImageModel = computed({
  get: () => resolveFileUrl(form.coverImageUrl),
  set: (value: string) => {
    form.coverImageUrl = value.startsWith(apiOrigin) ? value.slice(apiOrigin.length) : value
  }
})

const formatPrice = (value: number) => `¥${Number(value).toFixed(2)}`

const formatMoney = (value: number) => `¥${Number(value).toFixed(2)}`

const couponValueText = (row: any) => {
  if (row.couponType === 'percent') {
    return `${(Number(row.discountAmount) * 10).toFixed(1)} 折`
  }
  return `¥${Number(row.discountAmount).toFixed(2)}`
}

const minPriceOf = (row: any): number | null => {
  const prices = (row.skus || [])
    .filter((sku: any) => sku.isActive)
    .map((sku: any) => Number(sku.price))
    .filter((price: number) => !Number.isNaN(price))
  return prices.length ? Math.min(...prices) : null
}

const statusText = (status: string) => {
  if (status === 'Upcoming') return '未开始'
  if (status === 'Ongoing') return '进行中'
  return '已结束'
}

const statusTagType = (status: string) => {
  if (status === 'Ongoing') return 'success'
  if (status === 'Upcoming') return 'warning'
  return 'info'
}

const handleReset = () => {
  searchForm.keyword = ''
  searchForm.type = ''
  searchForm.isActive = ''
}

const resetForm = () => {
  form.type = 0
  form.name = ''
  form.coverImageUrl = ''
  form.contentHtml = ''
  form.dateRange = []
  form.sortOrder = 0
  form.isActive = true
  form.items = []
  form.coupons = []
  activeTab.value = 'basic'
}

// ---- 选品排序 ----
const reindexItems = () => {
  form.items.forEach((item, index) => {
    item.sortOrder = index
  })
}

const moveItem = (index: number, offset: number) => {
  const target = index + offset
  if (target < 0 || target >= form.items.length) return
  const items = form.items
  const current = items[index]
  items.splice(index, 1)
  items.splice(target, 0, current)
  reindexItems()
}

const removeItem = (index: number) => {
  form.items.splice(index, 1)
  reindexItems()
}

const isSelected = (productId: string) => form.items.some(item => item.productId === productId)

const addProduct = (row: any) => {
  if (isSelected(row.id)) {
    ElMessage.warning('该商品已在推荐列表中')
    return
  }
  form.items.push({
    productId: row.id,
    productName: row.name,
    productCode: row.code,
    mainImageUrl: row.mainImageUrl || '',
    minPrice: minPriceOf(row),
    productIsActive: row.isActive,
    groupName: '',
    sortOrder: form.items.length
  })
  ElMessage.success('已添加商品')
}

// ---- 商品选择弹窗 ----
const productDialogVisible = ref(false)
const productSearch = reactive({
  keyword: '',
  page: 1,
  pageSize: 10,
  total: 0,
  rows: [] as any[],
  loading: false
})

const openProductDialog = () => {
  productDialogVisible.value = true
  if (!productSearch.rows.length) {
    searchProducts(1)
  }
}

const searchProducts = async (page?: number) => {
  if (typeof page === 'number') {
    productSearch.page = page
  }
  productSearch.loading = true
  try {
    const { data } = await mallProductApi.getProductList({
      page: productSearch.page,
      pageSize: productSearch.pageSize,
      keyword: productSearch.keyword.trim()
    })
    productSearch.rows = data.list || []
    productSearch.total = data.totalCount || 0
  } catch {
    productSearch.rows = []
    productSearch.total = 0
  } finally {
    productSearch.loading = false
  }
}

// ---- 选券弹窗 ----
const couponDialogVisible = ref(false)
const couponSearch = reactive({
  keyword: '',
  page: 1,
  pageSize: 10,
  total: 0,
  rows: [] as any[],
  loading: false
})

const openCouponDialog = () => {
  couponDialogVisible.value = true
  if (!couponSearch.rows.length) {
    searchCoupons(1)
  }
}

const searchCoupons = async (page?: number) => {
  if (typeof page === 'number') {
    couponSearch.page = page
  }
  couponSearch.loading = true
  try {
    const { data } = await mallCampaignApi.getCouponOptions({
      page: couponSearch.page,
      pageSize: couponSearch.pageSize,
      keyword: couponSearch.keyword.trim()
    })
    couponSearch.rows = data.list || []
    couponSearch.total = data.totalCount || 0
  } catch {
    couponSearch.rows = []
    couponSearch.total = 0
  } finally {
    couponSearch.loading = false
  }
}

const isCouponSelected = (couponId: string) => form.coupons.some(coupon => coupon.couponId === couponId)

const addCoupon = (row: any) => {
  if (isCouponSelected(row.id)) {
    ElMessage.warning('该优惠券已添加')
    return
  }
  form.coupons.push({
    couponId: row.id,
    name: row.name,
    code: row.code,
    couponType: row.couponType,
    thresholdAmount: row.thresholdAmount,
    discountAmount: row.discountAmount,
    maxDiscount: row.maxDiscount ?? null,
    totalCount: row.totalCount,
    remainCount: row.remainCount,
    perLimit: row.perLimit,
    scopeType: row.scopeType,
    isActive: row.isActive,
    isClaimable: true,
    sortOrder: form.coupons.length
  })
  ElMessage.success('已添加优惠券')
}

const reindexCoupons = () => {
  form.coupons.forEach((coupon, index) => {
    coupon.sortOrder = index
  })
}

const moveCoupon = (index: number, offset: number) => {
  const target = index + offset
  if (target < 0 || target >= form.coupons.length) return
  const current = form.coupons[index]
  form.coupons.splice(index, 1)
  form.coupons.splice(target, 0, current)
  reindexCoupons()
}

const removeCoupon = (index: number) => {
  form.coupons.splice(index, 1)
  reindexCoupons()
}

// ---- 弹窗打开、加载与保存 ----
const formatPicker = (value: string) => {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const pad = (number: number) => String(number).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(
    date.getMinutes()
  )}:${pad(date.getSeconds())}`
}

const loadCampaign = async (id: string) => {
  loading.value = true
  try {
    const { data } = await mallCampaignApi.getCampaign(id)
    form.type = data.type ?? 0
    form.name = data.name || ''
    form.coverImageUrl = data.coverImageUrl || ''
    form.contentHtml = data.contentHtml || ''
    form.dateRange = data.startAt && data.endAt ? [formatPicker(data.startAt), formatPicker(data.endAt)] : []
    form.sortOrder = data.sortOrder || 0
    form.isActive = Boolean(data.isActive)
    form.items = (data.items || []).map((item: any) => ({
      id: item.id,
      productId: item.productId,
      productName: item.productName,
      productCode: item.productCode,
      mainImageUrl: item.mainImageUrl || '',
      minPrice: item.minPrice ?? null,
      productIsActive: Boolean(item.productIsActive),
      groupName: item.groupName || '',
      sortOrder: item.sortOrder
    }))
    form.coupons = (data.coupons || []).map((coupon: any) => ({
      id: coupon.id,
      couponId: coupon.couponId,
      name: coupon.name,
      code: coupon.code,
      couponType: coupon.couponType,
      thresholdAmount: coupon.thresholdAmount,
      discountAmount: coupon.discountAmount,
      maxDiscount: coupon.maxDiscount ?? null,
      totalCount: coupon.totalCount,
      remainCount: coupon.remainCount,
      perLimit: coupon.perLimit,
      scopeType: coupon.scopeType,
      isActive: Boolean(coupon.isActive),
      isClaimable: Boolean(coupon.isClaimable),
      sortOrder: coupon.sortOrder
    }))
  } catch {
    ElMessage.error('加载专题活动失败')
    return false
  } finally {
    loading.value = false
  }
  return true
}

const openDialog = async (type: string, row?: any) => {
  resetForm()
  dialogType.value = type
  dialogTitle.value = type === '新增' ? '新增专题活动' : '编辑专题活动'
  currentId.value = ''
  if (type === '新增') {
    dialogVisible.value = true
    return
  }
  if (!row) return
  const loaded = await loadCampaign(row.id)
  if (!loaded) return
  currentId.value = row.id
  dialogVisible.value = true
}

const validateForm = () => {
  if (!form.name.trim()) {
    ElMessage.warning('请输入活动名称')
    return false
  }
  if (form.dateRange.length !== 2 || !form.dateRange[0] || !form.dateRange[1]) {
    ElMessage.warning('请选择活动开始时间和结束时间')
    return false
  }
  if (form.dateRange[1] <= form.dateRange[0]) {
    ElMessage.warning('活动结束时间必须晚于开始时间')
    return false
  }
  if (form.type === 0 && form.items.length === 0) {
    ElMessage.warning('请至少选择一个推荐商品')
    return false
  }
  if (form.type === 2 && form.coupons.length === 0) {
    ElMessage.warning('请至少选择一张优惠券')
    return false
  }
  const groupOverflow = form.items.find(item => (item.groupName || '').trim().length > 50)
  if (groupOverflow) {
    ElMessage.warning('分组名不能超过 50 个字符')
    return false
  }
  return true
}

const submitForm = async () => {
  if (!validateForm()) return
  reindexItems()

  const payload = {
    type: form.type,
    name: form.name.trim(),
    coverImageUrl: form.coverImageUrl,
    contentHtml: form.contentHtml,
    startAt: form.dateRange[0],
    endAt: form.dateRange[1],
    sortOrder: form.sortOrder,
    items: form.type === 0
      ? form.items.map(item => ({
          productId: item.productId,
          groupName: (item.groupName || '').trim(),
          sortOrder: item.sortOrder
        }))
      : [],
    couponIds: form.type === 2 ? form.coupons.map(coupon => coupon.couponId) : []
  }

  submitting.value = true
  try {
    if (dialogType.value === '新增') {
      await mallCampaignApi.addCampaign({ ...payload, isActive: form.isActive })
      ElMessage.success('新增成功')
    } else {
      await mallCampaignApi.updateCampaign(currentId.value, payload)
      await mallCampaignApi.changeCampaignStatus(currentId.value, form.isActive)
      ElMessage.success('更新成功')
    }
    dialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {} finally {
    submitting.value = false
  }
}

const changeStatus = async (row: any) => {
  const next = !row.isActive
  try {
    await ElMessageBox.confirm(`确定要${next ? '上架' : '下架'}专题活动“${row.name}”吗？`, '状态变更', {
      type: 'warning'
    })
    await mallCampaignApi.changeCampaignStatus(row.id, next)
    ElMessage.success(`已${next ? '上架' : '下架'}`)
    queryPageRef.value?.getTableList()
  } catch {}
}

const remove = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除专题活动“${row.name}”吗？`, '删除', { type: 'warning' })
    await mallCampaignApi.deleteCampaign(row.id)
    ElMessage.success('删除成功')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss">
.activity-cover {
  width: 80px;
  height: 50px;
  border-radius: 4px;
}

.activity-time {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.campaign-dialog :deep(.el-dialog__body) {
  max-height: 68vh;
  overflow-y: auto;
}

.campaign-tabs {
  min-height: 420px;
}

.campaign-content-item {
  margin-bottom: 0;
}

.campaign-content-section {
  width: 100%;
}

.tab-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.tab-tip {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.campaign-content-editor {
  width: 100%;
}

.content-editor-hint {
  margin-top: 6px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.content-editor-hint.is-over-limit {
  color: var(--el-color-danger);
  font-weight: 600;
}

.type-hint {
  margin-left: 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.sort-actions {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 2px;
}

.sort-index {
  width: 20px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.product-cell {
  display: flex;
  align-items: center;
  gap: 10px;
}

.product-thumb {
  width: 48px;
  height: 48px;
  border-radius: 4px;
  flex-shrink: 0;
}

.product-thumb-empty {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: var(--el-fill-color-light);
  color: var(--el-text-color-placeholder);
}

.product-info {
  min-width: 0;
}

.product-name {
  font-weight: 500;
  word-break: break-all;
}

.product-code {
  margin-top: 2px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.products-empty {
  padding: 32px 0;
  text-align: center;
  color: var(--el-text-color-secondary);
}

.product-search-bar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.product-search-tip {
  margin-left: auto;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.product-pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
}

.selected-tip {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.coupon-name {
  font-weight: 500;
}

.coupon-code {
  margin-top: 2px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.coupon-value {
  font-weight: 600;
  color: var(--el-color-danger);
}
</style>

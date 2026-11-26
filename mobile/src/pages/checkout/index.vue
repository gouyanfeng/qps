<template>
  <q-page refresh-enabled :refresher-triggered="refreshing" @refresh="onRefresh">
    <!-- ============ 顶栏 ============ -->
    <q-page-header-back title="确认订单" />

    <view class="page-body">

      <!-- 冷链配送地址卡 -->
      <section class="card address-card">
        <view class="address-head">
          <view class="addr-pill">
            <q-icon name="snowflake" size="24" color="#2E7D32" />
            <text>顺丰冷链 · 避光恒温保效直发</text>
          </view>
          <view class="address-head-right" @tap="editAddress">
            <text class="link">更换地址</text>
            <q-icon name="chevron-right" size="24" color="#9A9A96" />
          </view>
        </view>
        <view class="address-user">
          <text class="name">{{ address?.receiverName || '请选择收货地址' }}</text>
          <text class="phone">{{ address?.receiverPhone || '' }}</text>
          <view v-if="address?.isDefault" class="tag-default tag-sm">默认</view>
        </view>
        <view class="address-detail">
          <q-icon name="location" size="28" color="#2E7D32" />
          <text class="address-text">{{ address?.fullAddress || '点击更换地址并新增收货地址' }}</text>
        </view>
        <view class="time-pill">
          <q-icon name="time" size="24" color="#E2601F" />
          <text class="time-text">预计<text class="time-strong">明天 14:00</text>前送达（中药密封温控箱配送）</text>
        </view>
        <!-- 卡片底部的信封封边 -->
        <view class="rx-divider"></view>
      </section>

      <!-- 配送方式 -->
      <section class="card delivery-card">
        <view class="delivery-icon">
          <q-icon name="vehicle" size="30" color="#2E7D32" />
        </view>
        <view class="delivery-text">
          <text class="delivery-name">顺丰特快</text>
          <text class="delivery-desc">{{ address ? (freightAmount > 0 ? `按运费模板计费 ¥${freightAmount.toFixed(2)}` : '已免运费') : '选择收货地址后计算运费' }}</text>
        </view>
        <view class="delivery-tag">包邮专享</view>
      </section>

      <!-- 商品清单店铺分组 -->
      <section class="card shop-card">
        <view class="shop-head">
          <view class="shop-head-left">
            <q-icon name="shop" size="32" color="#2E7D32" />
            <text class="shop-name">{{ shopName }}</text>
            <view class="tag tag-green tag-sm">
              <q-icon name="check-circle-filled" size="20" color="#FFFFFF" />
              <text>道地认证</text>
            </view>
          </view>
        </view>
        <view class="goods-list">
          <view class="goods-item" v-for="item in items" :key="item.productId">
            <q-image class="goods-img" :src="item.productImage" thumb :width="120" mode="aspectFill" />
            <view class="goods-info">
              <view class="goods-name">{{ item.productName }}</view>
              <view class="goods-spec">{{ item.specName }}</view>
              <view class="goods-bottom">
                <text class="goods-price">¥{{ (item.discountedPrice ?? item.price).toFixed(2) }}</text>
                <text v-if="item.discountedPrice !== undefined && item.discountedPrice < item.price" class="goods-price-origin">¥{{ item.price.toFixed(2) }}</text>
                <text class="goods-qty">×{{ item.quantity }}</text>
              </view>
            </view>
          </view>
        </view>
        <!-- 赠品清单：0 元，不参与金额计算，提交前明确展示 -->
        <view v-if="gifts.length" class="gift-list">
          <view class="gift-list-title">
            <q-icon name="gift" size="16" color="#E2601F" />
            <text>赠品（随单赠送，¥0 不参与金额）</text>
          </view>
          <view v-for="gift in gifts" :key="gift.skuId" class="gift-row">
            <q-image class="gift-img" :src="gift.productImage" thumb :width="120" mode="aspectFill" />
            <view class="gift-info">
              <view class="gift-name-line">
                <text class="gift-name">{{ gift.productName }}</text>
                <view class="gift-tag"><text>赠品</text></view>
              </view>
              <text class="gift-spec">{{ gift.specName }}</text>
              <text class="gift-promo">满赠活动：{{ gift.promotionName }}</text>
            </view>
            <view class="gift-right">
              <text class="gift-price">¥0</text>
              <text class="gift-qty">×{{ gift.quantity }}</text>
            </view>
          </view>
        </view>
        <view class="dashed-divider"></view>
        <view class="shop-subtotal">
          <text>商品小计</text>
          <text class="subtotal-price">¥{{ subtotal.toFixed(2) }}</text>
        </view>
      </section>

      <!-- 优惠明细 -->
      <section class="card discount-card">
        <view class="discount-row" @tap="selectCoupon">
          <view class="discount-row-left">
            <q-icon name="coupon" size="30" color="#FF9800" />
            <text>优惠券</text>
          </view>
          <view class="discount-row-right">
            <text v-if="couponDiscount > 0" class="discount-neg">-¥{{ couponDiscount.toFixed(2) }}</text>
            <text v-else class="discount-hint">{{ availableCouponCount ? `${availableCouponCount}张可用` : '暂无可用优惠券' }}</text>
            <q-icon name="chevron-right" size="28" color="#999999" />
          </view>
        </view>
        <view v-if="totalPromotionDiscount > 0" class="discount-row promotion-row">
          <view class="discount-row-left">
            <q-icon name="coupon" size="30" color="#E2601F" />
            <text>促销优惠</text>
          </view>
          <view class="discount-row-right">
            <text class="discount-neg">-¥{{ totalPromotionDiscount.toFixed(2) }}</text>
          </view>
        </view>
        <view v-else class="discount-row promotion-row">
          <view class="discount-row-left">
            <q-icon name="coupon" size="30" color="#E2601F" />
            <text>促销优惠</text>
          </view>
          <view class="discount-row-right">
            <text class="discount-hint">暂无可用促销</text>
          </view>
        </view>

        <!-- 优惠构成：逐条列出命中的促销，各行金额之和等于上方合计，不额外计入 -->
        <view v-if="promotionDetails.length" class="promotion-detail">
          <text class="promotion-detail-title">优惠构成</text>
          <view v-for="detail in promotionDetails" :key="detail.promotionId" class="promotion-detail-row">
            <text class="promotion-detail-name">{{ detail.name }}</text>
            <text class="promotion-detail-value">-¥{{ detail.discountAmount.toFixed(2) }}</text>
          </view>
        </view>

        <view class="discount-row" @tap="addRemark">
          <view class="discount-row-left">
            <q-icon name="edit" size="30" color="#2E7D32" />
            <text>订单备注</text>
          </view>
          <view class="discount-row-right">
            <text class="discount-hint" :class="{ placeholder: !remark }">
              {{ remark || '选填：如忌口、优先发顺丰等' }}
            </text>
            <q-icon name="chevron-right" size="28" color="#999999" />
          </view>
        </view>

      </section>

      <!-- 选择支付方式 -->
      <view class="pay-section">
        <view class="pay-section-title">选择支付方式</view>
        <view :class="['pay-option', { 'pay-option-on': payMethod === 'wechat' }]" @tap="payMethod = 'wechat'">
          <view class="pay-logo wechat">微</view>
          <view class="pay-main">
            <view class="pay-title-row">
              <text class="pay-name">微信支付</text>
              <view class="pay-tag">推荐</view>
            </view>
            <text class="pay-desc">微信立减金可用，支持微信分期</text>
          </view>
          <view :class="['pay-radio', { 'pay-radio-on': payMethod === 'wechat' }]">
            <q-icon v-if="payMethod === 'wechat'" name="check" size="18" color="#ffffff" />
          </view>
        </view>
        <view :class="['pay-option', { 'pay-option-on': payMethod === 'alipay' }]" @tap="payMethod = 'alipay'">
          <view class="pay-logo alipay">支</view>
          <view class="pay-main">
            <view class="pay-title-row">
              <text class="pay-name">支付宝支付</text>
            </view>
            <text class="pay-desc">支持花呗分期还款</text>
          </view>
          <view :class="['pay-radio', { 'pay-radio-on': payMethod === 'alipay' }]">
            <q-icon v-if="payMethod === 'alipay'" name="check" size="18" color="#ffffff" />
          </view>
        </view>
      </view>

      <!-- 药房品质保证 -->
      <section class="card guar-card">
        <view class="guar-item">
          <q-icon name="check-circle" size="28" color="#2E7D32" />
          <text>道地保真</text>
        </view>
        <view class="guar-item">
          <q-icon name="check-circle" size="28" color="#2E7D32" />
          <text>老药师鉴选</text>
        </view>
        <view class="guar-item">
          <q-icon name="check-circle" size="28" color="#2E7D32" />
          <text>溯源可查</text>
        </view>
      </section>

      <view class="bottom-placeholder"></view>
    </view>

    <template #footer>
    <!-- 底部固定结算栏：应付/合计 + 明细抽屉入口 + 提交订单 -->
    <q-checkout-bar
      :payable-amount="finalPrice"
      :goods-amount="subtotal"
      :promotion-discount="totalPromotionDiscount"
      :coupon-discount="couponDiscount"
      :coupon-name="couponName"
      :freight-amount="freightAmount"
    >
      <template #action>
        <view class="submit-btn" @tap="submitOrder">
          <text>提交订单</text>
          <text class="submit-count">({{ itemCount }})</text>
        </view>
      </template>
    </q-checkout-bar>

    <!-- 优惠券选择弹窗 -->
    <view v-if="showCouponPicker" class="coupon-picker-mask" @tap="showCouponPicker = false">
      <view class="coupon-picker" @tap.stop>
        <view class="picker-header">
          <text class="picker-title">选择优惠券</text>
          <view class="picker-close" @tap="showCouponPicker = false">
            <q-icon name="close" size="22" color="#707A6C" />
          </view>
        </view>
        <scroll-view scroll-y class="picker-body">
          <view class="picker-body-inner">
          <view
            v-for="c in availableCoupons"
            :key="c.id"
            class="picker-coupon-item"
            :class="{ selected: selectedCoupon?.id === c.id, disabled: !c.isAvailable }"
            @tap="pickCoupon(c)"
          >
            <view class="picker-coupon-left">
              <text v-if="isPercentCoupon(c.couponType)" class="picker-coupon-val">{{ couponDiscountRateText(c.faceValue) }}</text>
              <template v-else>
                <text class="picker-coupon-yen">¥</text>
                <text class="picker-coupon-val">{{ Math.floor(c.faceValue) }}</text>
              </template>
            </view>
            <view class="picker-coupon-right">
              <text class="picker-coupon-name">{{ c.name }}</text>
              <text class="picker-coupon-threshold">{{ c.thresholdAmount > 0 ? `满${Math.floor(c.thresholdAmount)}元可用` : '无门槛' }}</text>
              <text v-if="c.isAvailable" class="picker-coupon-expire">{{ formatCouponExpire(c.expireAt) }}</text>
              <text v-else class="picker-coupon-expire">{{ c.unavailableReason || '当前不可用' }}</text>
            </view>
            <view v-if="selectedCoupon?.id === c.id" class="picker-check">
              <q-icon name="check" size="18" color="#ffffff" />
            </view>
          </view>
          <view v-if="!availableCoupons.length" class="picker-empty">
            <text class="picker-empty-text">暂无优惠券</text>
          </view>
          </view>
        </scroll-view>
        <view v-if="selectedCoupon" class="picker-footer">
          <view class="picker-clear-btn" @tap="pickCoupon(null)">
            <text class="picker-clear-text">不使用优惠券</text>
          </view>
        </view>
      </view>
    </view>
    <!-- 收货地址选择弹窗：结算页原地切换，避免页面跳转影响立即购买上下文 -->
    <view v-if="showAddressPicker" class="coupon-picker-mask" @tap="showAddressPicker = false">
      <view class="coupon-picker address-picker" @tap.stop>
        <view class="picker-header">
          <text class="picker-title">选择收货地址</text>
          <view class="picker-close" @tap="showAddressPicker = false">
            <q-icon name="close" size="22" color="#707A6C" />
          </view>
        </view>
        <scroll-view scroll-y class="picker-body address-picker-body">
          <view class="picker-body-inner">
          <view
            v-for="item in addressOptions"
            :key="item.id"
            class="address-option"
            :class="{ selected: address?.id === item.id }"
            @tap="pickAddress(item)"
          >
            <view class="address-option-main">
              <view class="address-option-head">
                <text class="address-option-name">{{ item.receiverName }}</text>
                <text class="address-option-phone">{{ item.receiverPhone }}</text>
                <view v-if="item.isDefault" class="tag-default tag-sm">默认</view>
              </view>
              <text class="address-option-detail">{{ item.fullAddress }}</text>
            </view>
            <view v-if="address?.id === item.id" class="picker-check">
              <q-icon name="check" size="18" color="#ffffff" />
            </view>
          </view>
          <view v-if="!addressOptions.length" class="picker-empty">
            <text class="picker-empty-text">还没有收货地址，请先新增</text>
          </view>
          </view>
        </scroll-view>
        <view class="picker-footer">
          <view class="picker-clear-btn address-add-btn" @tap="addAddressFromPicker">
            <text class="picker-clear-text">＋ 新增收货地址</text>
          </view>
        </view>
      </view>
    </view>
    </template>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow, onUnload } from '@dcloudio/uni-app'
import { getCartItems, previewCartItems, removeCartItems, getAddresses, createOrder, getCheckoutCoupons, previewOrder } from '@/api'
import { couponDiscountRateText, isPercentCoupon } from '@/utils/coupon'
import type { CheckoutCoupon, MallAddress, OrderPreview } from '@/api'
import type { CartItem } from '@/api/types'
import { UnauthorizedError } from '@/utils/http'

const items = ref<CartItem[]>([])
const address = ref<MallAddress | null>(null)
const payMethod = ref('wechat')
const remark = ref('')
const availableCoupons = ref<CheckoutCoupon[]>([])
const selectedCoupon = ref<CheckoutCoupon | null>(null)
const showCouponPicker = ref(false)
const showAddressPicker = ref(false)
const addressOptions = ref<MallAddress[]>([])
const promotionPreview = ref<OrderPreview | null>(null)
const refreshing = ref(false)
// 立即购买走临时行试算（不落购物车）；购物车结算为 false
const isPreviewCheckout = ref(false)

// 商品金额一律取后端试算结果，前端不再自行累加（价格公式只在后端一处）
const subtotal = computed(() => promotionPreview.value?.goodsAmount ?? 0)
// 店铺名取商品所属门店，与购物车/订单页保持一致，无门店信息时回退默认名
const shopName = computed(() => items.value.find(i => i.storeName)?.storeName || '本草国医馆')

const promotionDiscount = computed(() => promotionPreview.value?.promotionDiscountAmount ?? 0)
const rateDiscount = computed(() => promotionPreview.value?.rateDiscountAmount ?? 0)
const amountDiscount = computed(() => promotionPreview.value?.amountDiscountAmount ?? 0)
// 合计只取后端汇总字段；万一接口没带汇总，用分项之和兜底，避免合计行显示 0
const totalPromotionDiscount = computed(() =>
  promotionDiscount.value > 0 ? promotionDiscount.value : rateDiscount.value + amountDiscount.value
)
const promotionDetails = computed(() => promotionPreview.value?.promotionDetails ?? [])
const couponDiscount = computed(() => promotionPreview.value?.couponDiscountAmount ?? 0)
const couponName = computed(() => promotionPreview.value?.couponName ?? '')
// 运费后端口径：无地址/免邮/无模板时为 0
const freightAmount = computed(() => promotionPreview.value?.freightAmount ?? 0)
const availableCouponCount = computed(() =>
  availableCoupons.value.filter(item => item.isAvailable).length
)

// 实付金额一律取后端预览，前端不再兜底算价（公式只在后端 PriceBreakdown 一处）
const finalPrice = computed(() => promotionPreview.value?.payableAmount ?? 0)
const gifts = computed(() => promotionPreview.value?.gifts ?? [])
// 件数含赠品：赠品 0 元但按件计入
const itemCount = computed(() =>
  items.value.reduce((s, i) => s + i.quantity, 0) +
  gifts.value.reduce((s, gift) => s + gift.quantity, 0)
)

/** 按当前商品与已选券重新试算，金额一律采用后端返回。 */
async function refreshPreview() {
  if (items.value.length === 0) {
    promotionPreview.value = null
    return
  }
  // 统一试算接口：地址可选，未选地址时运费按 0，商品金额/促销仍由后端出
  promotionPreview.value = await previewOrder(
    items.value.map(item => ({ skuId: item.skuId, quantity: item.quantity })),
    address.value?.id ?? null,
    selectedCoupon.value?.id
  ).catch(error => {
    console.error('订单试算失败', error)
    return null
  })
}

onShow(() => { loadCheckout() })

async function loadCheckout() {
  try {
    // 先确定收货地址，预览的运费才知道按哪个地区计算
    const addresses = await getAddresses()
    addressOptions.value = addresses
    // 从新增地址页返回时重新打开弹层；地址页会把新地址 ID 回写到 selectedAddressId
    const reopenAddressPicker = Boolean(uni.getStorageSync('qps_checkout_add_address'))
    if (reopenAddressPicker) uni.removeStorageSync('qps_checkout_add_address')
    const selectedAddressId = uni.getStorageSync('qps_checkout_address_id')
    address.value = addresses.find(item => item.id === selectedAddressId)
      ?? addresses.find(item => item.isDefault)
      ?? addresses[0]
      ?? null

    // 立即购买：只带 skuId + 数量走临时行试算，不写购物车、数量不与购物车累加
    const previewItems = uni.getStorageSync('qps_checkout_preview_items') as
      | Array<{ skuId: string; quantity: number }> | null
    if (Array.isArray(previewItems) && previewItems.length > 0) {
      isPreviewCheckout.value = true
      items.value = await previewCartItems(previewItems)
    } else {
      isPreviewCheckout.value = false
      items.value = (await getCartItems()).filter(item => item.selected)
    }
    if (items.value.length > 0) {
      // 券列表按本次结算商品返回可用性与不可用原因
      const coupons = await getCheckoutCoupons(
        items.value.map(item => ({ skuId: item.skuId, quantity: item.quantity }))
      ).catch(error => {
        console.error('优惠券查询失败', error)
        return [] as CheckoutCoupon[]
      })
      availableCoupons.value = coupons
      if (selectedCoupon.value) {
        const current = coupons.find(item => item.id === selectedCoupon.value!.id)
        if (!current || !current.isAvailable) selectedCoupon.value = null
      }
      await refreshPreview()
    }
    if (reopenAddressPicker) showAddressPicker.value = true
  } catch (error) {
    console.error('加载结算数据失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '结算数据加载失败', icon: 'none' })
  }
}

async function onRefresh() {
  refreshing.value = true
  try {
    await loadCheckout()
  } finally {
    refreshing.value = false
  }
}

// 离开结算页（返回或切换页面）时清理临时试算上下文，避免下次购物车结算串单
onUnload(() => {
  uni.removeStorageSync('qps_checkout_preview_items')
})

function editAddress() { showAddressPicker.value = true }
/** 弹层内选中地址：只重算运费与应付，商品明细保持不变 */
async function pickAddress(item: MallAddress) {
  address.value = item
  uni.setStorageSync('qps_checkout_address_id', item.id)
  showAddressPicker.value = false
  await refreshPreview()
}
/** 跳现有新增地址页，保存后返回结算页自动选中新地址 */
function addAddressFromPicker() {
  showAddressPicker.value = false
  uni.setStorageSync('qps_checkout_add_address', '1')
  uni.navigateTo({ url: '/pages/addresses/index' })
}
function selectCoupon() {
  if (!availableCoupons.value.length) {
    uni.showToast({ title: '暂无优惠券', icon: 'none' })
    return
  }
  showCouponPicker.value = true
}
async function pickCoupon(coupon: CheckoutCoupon | null) {
  if (coupon && !coupon.isAvailable) {
    uni.showToast({ title: coupon.unavailableReason || '该优惠券当前不可用', icon: 'none' })
    return
  }
  selectedCoupon.value = coupon
  showCouponPicker.value = false
  await refreshPreview()
}
function formatCouponExpire(expireAt: string): string {
  if (!expireAt) return ''
  const d = new Date(expireAt)
  return `${d.getFullYear()}.${String(d.getMonth() + 1).padStart(2, '0')}.${String(d.getDate()).padStart(2, '0')} 到期`
}
function addRemark() {
  uni.showModal({
    title: '订单备注', editable: true, placeholderText: '选填',
    success: r => { if (r.confirm && r.content) remark.value = r.content }
  })
}
function submitOrder() {
  if (!address.value) {
    uni.showToast({ title: '请先添加收货地址', icon: 'none' })
    editAddress()
    return
  }
  if (items.value.length === 0) {
    uni.showToast({ title: '购物车没有已选商品', icon: 'none' })
    return
  }
  // 未拿到后端试算结果时禁止下单，避免前端 0 元占位被误当成实付金额
  if (!promotionPreview.value) {
    uni.showToast({ title: '价格试算未完成，请重试', icon: 'none' })
    refreshPreview()
    return
  }

  const giftLine = gifts.value.length
    ? `\n赠品：${gifts.value.map(gift => `${gift.productName}${gift.specName ? `·${gift.specName}` : ''}×${gift.quantity}`).join('，')}（¥0，不参与金额）`
    : ''

  uni.showModal({
    title: '确认提交',
    content: `商品金额 ¥${subtotal.value.toFixed(2)}，促销优惠 ¥${totalPromotionDiscount.value.toFixed(2)}，优惠券抵扣 ¥${couponDiscount.value.toFixed(2)}，应付 ¥${finalPrice.value.toFixed(2)}。${giftLine}确认创建订单？`,
    success: async r => {
      if (!r.confirm || !address.value) return
      try {
        const order = await createOrder({
          addressId: address.value.id,
          remark: remark.value,
          items: items.value.map(item => ({ skuId: item.skuId, quantity: item.quantity })),
          memberCouponId: selectedCoupon.value?.id
        })
        // 购物车结算才移除本次下单的规格，保留未购买的商品；
        // 立即购买走临时行、从未写入购物车，无需也不能删购物车行
        if (!isPreviewCheckout.value) {
          try {
            await removeCartItems(items.value.map(item => item.skuId))
          } catch (error) {
            console.error('订单已创建但同步购物车失败', error)
            uni.showToast({ title: '订单已创建，购物车同步失败', icon: 'none' })
          }
        }
        uni.showModal({
          title: '订单已创建',
          content: `订单号：${order.orderNo}\n促销优惠：¥${Number(order.promotionDiscountAmount || 0).toFixed(2)}\n实付金额：¥${Number(order.payableAmount).toFixed(2)}`,
          showCancel: false,
          success: () => uni.switchTab({ url: '/pages/profile/index' })
        })
      } catch (error) {
        console.error('创建订单失败', error)
        if (error instanceof UnauthorizedError) return
        uni.showToast({ title: '下单失败，请检查库存和收货信息', icon: 'none' })
      }
    }
  })
}
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>
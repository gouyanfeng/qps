<template>
  <view class="page">
    <!-- ============ 顶栏 ============ -->
    <q-page-header-back title="确认订单" />

    <scroll-view scroll-y class="page-body">
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
            <text class="shop-name">本草国医馆·道地药房</text>
            <view class="tag tag-green tag-sm">
              <q-icon name="check-circle-filled" size="20" color="#FFFFFF" />
              <text>道地认证</text>
            </view>
          </view>
        </view>
        <view class="goods-list">
          <view class="goods-item" v-for="item in items" :key="item.productId">
            <q-cached-image class="goods-img" :src="item.productImage" mode="aspectFill" />
            <view class="goods-info">
              <view class="goods-name">{{ item.productName }}</view>
              <view class="goods-spec">{{ item.specName }}</view>
              <view class="goods-bottom">
                <text class="goods-price">¥{{ item.price.toFixed(2) }}</text>
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
            <q-cached-image class="gift-img" :src="gift.productImage" mode="aspectFill" />
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
        <view v-if="rateDiscount > 0" class="discount-row promotion-row">
          <view class="discount-row-left">
            <q-icon name="coupon" size="30" color="#E2601F" />
            <text>折后优惠</text>
          </view>
          <view class="discount-row-right">
            <text class="discount-neg">-¥{{ rateDiscount.toFixed(2) }}</text>
          </view>
        </view>
        <view v-if="amountDiscount > 0" class="discount-row promotion-row">
          <view class="discount-row-left">
            <q-icon name="coupon" size="30" color="#E2601F" />
            <text>满减优惠</text>
          </view>
          <view class="discount-row-right">
            <text class="discount-neg">-¥{{ amountDiscount.toFixed(2) }}</text>
          </view>
        </view>
        <view v-if="rateDiscount === 0 && amountDiscount === 0" class="discount-row promotion-row">
          <view class="discount-row-left">
            <q-icon name="coupon" size="30" color="#E2601F" />
            <text>促销优惠</text>
          </view>
          <view class="discount-row-right">
            <text class="discount-hint">暂无可用促销</text>
          </view>
        </view>

        <!-- 促销优惠明细 -->
        <view v-if="promotionDetails.length" class="promotion-detail">
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
    </scroll-view>

    <!-- 底部固定结算栏：应付/合计 + 明细抽屉入口 + 提交订单 -->
    <q-checkout-bar
      :payable-amount="finalPrice"
      :goods-amount="subtotal"
      :promotion-discount="promotionDiscount"
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
        </scroll-view>
        <view class="picker-footer">
          <view class="picker-clear-btn address-add-btn" @tap="addAddressFromPicker">
            <text class="picker-clear-text">＋ 新增收货地址</text>
          </view>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow, onUnload } from '@dcloudio/uni-app'
import { getCartItems, removeCartItems, getAddresses, createOrder, getCheckoutCoupons, previewOrder } from '@/api'
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
// 立即购买不经过购物车，下单成功后不能改动购物车
const isBuyNow = ref(false)

const subtotal = computed(() =>
  items.value.reduce((s, i) => s + i.price * i.quantity, 0)
)

const promotionDiscount = computed(() => promotionPreview.value?.promotionDiscountAmount ?? 0)
const rateDiscount = computed(() => promotionPreview.value?.rateDiscountAmount ?? 0)
const amountDiscount = computed(() => promotionPreview.value?.amountDiscountAmount ?? 0)
const promotionDetails = computed(() => promotionPreview.value?.promotionDetails ?? [])
const couponDiscount = computed(() => promotionPreview.value?.couponDiscountAmount ?? 0)
const couponName = computed(() => promotionPreview.value?.couponName ?? '')
// 运费后端口径：无地址/免邮/无模板时为 0
const freightAmount = computed(() => promotionPreview.value?.freightAmount ?? 0)
const availableCouponCount = computed(() =>
  availableCoupons.value.filter(item => item.isAvailable).length
)

// 实付金额以后端预览为准，预览失败时回退为商品金额
const finalPrice = computed(() => Math.max(0, promotionPreview.value?.payableAmount ?? subtotal.value))
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
  // 预览必须带收货地址：未选地址时不请求，避免后端 400
  if (!address.value) {
    promotionPreview.value = null
    uni.showToast({ title: '请选择收货地址', icon: 'none' })
    return
  }
  promotionPreview.value = await previewOrder(
    items.value.map(item => ({ skuId: item.skuId, quantity: item.quantity })),
    address.value.id,
    selectedCoupon.value?.id
  ).catch(error => {
    console.error('订单试算失败', error)
    return null
  })
}

onShow(async () => {
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

    const buyNow = uni.getStorageSync('qps_buy_now_item')
    if (buyNow && typeof buyNow === 'object' && buyNow.skuId) {
      isBuyNow.value = true
      items.value = [{
        skuId: buyNow.skuId,
        productId: buyNow.productId,
        productName: buyNow.productName,
        productImage: buyNow.productImage,
        specName: buyNow.specName,
        price: buyNow.price,
        quantity: buyNow.quantity,
        weightGram: buyNow.weightGram ?? 0,
        selected: true
      }]
      // 立即购买上下文要保留到结算流程结束：去地址页/优惠券页再返回仍按原商品结算
    } else {
      isBuyNow.value = false
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
})

// 离开结算页（返回或切换页面）时清理立即购买上下文，避免下次购物车结算串单
onUnload(() => {
  uni.removeStorageSync('qps_buy_now_item')
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

  const giftLine = gifts.value.length
    ? `\n赠品：${gifts.value.map(gift => `${gift.productName}${gift.specName ? `·${gift.specName}` : ''}×${gift.quantity}`).join('，')}（¥0，不参与金额）`
    : ''

  uni.showModal({
    title: '确认提交',
    content: `商品金额 ¥${subtotal.value.toFixed(2)}，促销优惠 ¥${promotionDiscount.value.toFixed(2)}，优惠券抵扣 ¥${couponDiscount.value.toFixed(2)}，应付 ¥${finalPrice.value.toFixed(2)}。${giftLine}确认创建订单？`,
    success: async r => {
      if (!r.confirm || !address.value) return
      try {
        const order = await createOrder({
          addressId: address.value.id,
          remark: remark.value,
          items: items.value.map(item => ({ skuId: item.skuId, quantity: item.quantity })),
          memberCouponId: selectedCoupon.value?.id
        })
        const buyNowOrder = isBuyNow.value
        // 下单成功即为结算流程结束，立即购买上下文可以清理
        if (buyNowOrder) uni.removeStorageSync('qps_buy_now_item')
        try {
          // 只移除本次下单的规格明细，保留购物车中未购买的商品；立即购买不改动购物车
          if (!buyNowOrder) {
            await removeCartItems(items.value.map(item => item.skuId))
          }
        } catch (error) {
          console.error('订单已创建但同步购物车失败', error)
          uni.showToast({ title: '订单已创建，购物车同步失败', icon: 'none' })
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
$theme-primary: #2E7D32;
$theme-primary-dark: #1B5E20;
$theme-accent: #FF9800;
$theme-bg: #FCF9F8;
$card-radius: 24rpx;
$page-padding: 24rpx;

.page {
    min-height: 100vh;
  background: var(--page-bg);
  display: flex;
  flex-direction: column;
}

/* ===== 滚动容器 ===== */
.page-body {
  flex: 1;
  padding: 0 $page-padding;
  box-sizing: border-box;
}

/* ===== 卡片通用 ===== */
.card {
  background: #FFFFFF;
  border-radius: $card-radius;
  padding: 28rpx;
  margin-top: 24rpx;
  box-shadow: 0 4rpx 16rpx rgba(46, 125, 50, 0.04);
}
.card-title {
  font-size: 30rpx;
  font-weight: 700;
  color: #1B5E20;
  margin-bottom: 20rpx;
}

/* ===== Tag ===== */
.tag {
  display: inline-flex;
  align-items: center;
  gap: 6rpx;
  border-radius: 40rpx;
  font-size: 22rpx;
  padding: 6rpx 16rpx;
  line-height: 1;
}
.tag-sm { font-size: 20rpx; padding: 4rpx 12rpx; }
.tag-green { background: $theme-primary; color: #FFFFFF; }
.tag-orange { background: $theme-accent; color: #FFFFFF; }
.tag-default { background: #F0F0F0; color: #666; }

/* ===== 地址卡 ===== */
.address-card {
  position: relative;
  overflow: hidden;
  padding: 26rpx 28rpx 34rpx;
  border: 1rpx solid #F0EBE1;
}
.address-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.addr-pill {
  display: inline-flex;
  align-items: center;
  gap: 6rpx;
  height: 48rpx;
  padding: 0 18rpx;
  background: #EDF4E4;
  border-radius: 100rpx;
  font-size: 22rpx;
  font-weight: 600;
  color: $theme-primary;
}
.address-head-right {
  display: flex;
  align-items: center;
  gap: 2rpx;
  color: #9A9A96;
  font-size: 24rpx;
}
.link { color: #9A9A96; font-size: 24rpx; }
.address-user {
  display: flex;
  align-items: center;
  gap: 14rpx;
  margin-top: 20rpx;
}
.address-user .name {
  font-size: 34rpx;
  font-weight: 700;
  color: #1B1C1C;
  letter-spacing: 1rpx;
}
.address-user .phone {
  font-size: 26rpx;
  color: #555;
}
.address-user .tag-default {
  background: $theme-primary;
  color: #FFFFFF;
  font-weight: 600;
}
.address-detail {
  display: flex;
  align-items: flex-start;
  gap: 8rpx;
  margin-top: 14rpx;
}
.address-detail .q-icon { flex-shrink: 0; margin-top: 4rpx; }
.address-text {
  flex: 1;
  font-size: 27rpx;
  color: #333;
  line-height: 1.55;
}
.time-pill {
  margin-top: 18rpx;
  display: flex;
  align-items: center;
  gap: 8rpx;
  background: #FDF1E7;
  padding: 14rpx 18rpx;
  border-radius: 12rpx;
}
.time-text {
  flex: 1;
  font-size: 24rpx;
  color: #C0622F;
  line-height: 1.4;
}
.time-strong { font-weight: 700; color: #C24A18; }

/* ===== 红绿相间药方虚线（信封封边，贴在地址卡内侧底部） ===== */
.rx-divider {
  position: absolute;
  left: -24rpx;
  right: -24rpx;
  bottom: 0;
  height: 10rpx;
  transform: skewX(-20deg);
  background: repeating-linear-gradient(
    to right,
    $theme-primary 0 20rpx,
    transparent 20rpx 26rpx,
    #C62828 26rpx 46rpx,
    transparent 46rpx 52rpx
  );
}

/* ===== 配送方式 ===== */
.delivery-card {
  display: flex;
  align-items: center;
  gap: 16rpx;
  padding: 20rpx 24rpx;
  border: 1rpx solid #F0EBE1;
}
.delivery-icon {
  width: 76rpx;
  height: 76rpx;
  flex-shrink: 0;
  border-radius: 50%;
  background: #EFF5E8;
  display: flex;
  align-items: center;
  justify-content: center;
}
.delivery-text {
  flex: 1;
  min-width: 0;
  font-size: 23rpx;
  color: #6B6B6B;
  line-height: 1.5;
}
.delivery-name {
  font-weight: 700;
  color: #1B1C1C;
}
.delivery-tag {
  flex-shrink: 0;
  height: 44rpx;
  padding: 0 18rpx;
  display: flex;
  align-items: center;
  border-radius: 100rpx;
  background: $theme-primary;
  color: #FFFFFF;
  font-size: 22rpx;
  font-weight: 600;
}

/* ===== 商品清单 ===== */
.shop-card { padding: 24rpx 28rpx; }
.shop-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.shop-head-left {
  display: flex;
  align-items: center;
  gap: 12rpx;
}
.shop-name {
  font-size: 28rpx;
  font-weight: 700;
  color: #1B5E20;
}
.goods-list { margin-top: 16rpx; }
.goods-item {
  display: flex;
  gap: 20rpx;
  padding: 16rpx 0;
}
.goods-img {
  width: 160rpx;
  height: 160rpx;
  border-radius: 16rpx;
  background: #F3F3F3;
  flex-shrink: 0;
}
.goods-info { flex: 1; display: flex; flex-direction: column; justify-content: space-between; }
.goods-name {
  font-size: 28rpx;
  color: #222;
  font-weight: 600;
  line-height: 1.4;
}
.goods-spec {
  font-size: 22rpx;
  color: #999;
  margin-top: 4rpx;
}
.goods-bottom {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.goods-price {
  color: $theme-accent;
  font-size: 30rpx;
  font-weight: 700;
}
.goods-qty {
  color: #888;
  font-size: 26rpx;
}
.gift-list {
  margin-top: 16rpx;
  padding: 16rpx 18rpx;
  border-radius: 14rpx;
  background: #FDF6EC;
  border: 1rpx dashed #E9C9A6;
}
.gift-list-title {
  display: flex;
  align-items: center;
  gap: 8rpx;
  margin-bottom: 10rpx;
  font-size: 22rpx;
  font-weight: 600;
  color: #A85A00;
}
.gift-row {
  display: flex;
  align-items: center;
  gap: 14rpx;
  padding: 10rpx 0;
}
.gift-img { width: 88rpx; height: 88rpx; border-radius: 12rpx; flex-shrink: 0; }
.gift-info { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 6rpx; }
.gift-name-line { display: flex; align-items: center; gap: 10rpx; }
.gift-name {
  font-size: 24rpx;
  font-weight: 600;
  color: #1B1C1C;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.gift-tag { flex-shrink: 0; padding: 2rpx 10rpx; border-radius: 6rpx; background: #E2601F; }
.gift-tag text { font-size: 18rpx; font-weight: 600; color: #FFFFFF; }
.gift-spec { font-size: 21rpx; color: #8A8F86; }
.gift-promo {
  font-size: 20rpx;
  color: #A85A00;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.gift-right { display: flex; flex-direction: column; align-items: flex-end; gap: 4rpx; flex-shrink: 0; }
.gift-price { font-size: 28rpx; font-weight: 700; color: #8B5000; }
.gift-qty { font-size: 21rpx; color: #8A8F86; }

.dashed-divider {
  height: 2rpx;
  margin: 16rpx 0;
  background: repeating-linear-gradient(
    to right,
    #CCCCCC 0,
    #CCCCCC 10rpx,
    transparent 10rpx,
    transparent 20rpx
  );
}
.shop-subtotal {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 16rpx;
  font-size: 26rpx;
  color: #555;
}
.subtotal-price {
  font-size: 28rpx;
  font-weight: 700;
  color: #222;
}

/* ===== 优惠明细 ===== */
.discount-card { padding: 8rpx 28rpx; }

/* ===== 促销优惠明细 ===== */
.promotion-detail {
  padding: 4rpx 0 12rpx;
  margin-left: 58rpx;
  border-bottom: 1rpx solid #F2EEE9;
}
.promotion-detail-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16rpx;
  padding: 4rpx 0;
}
.promotion-detail-name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 24rpx;
  color: #707A6C;
}
.promotion-detail-value {
  font-size: 24rpx;
  font-weight: 600;
  color: #E2601F;
}
.discount-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 28rpx 0;
  border-bottom: 1rpx solid #F3F3F3;
}
.discount-row:last-of-type { border-bottom: none; }
.discount-row-left {
  display: flex;
  align-items: center;
  gap: 12rpx;
  font-size: 28rpx;
  color: #333;
}
.discount-row-right {
  display: flex;
  align-items: center;
  gap: 10rpx;
}
.discount-neg {
  color: $theme-accent;
  font-size: 28rpx;
  font-weight: 700;
}
.discount-hint {
  font-size: 24rpx;
  color: #999;
}
.discount-hint.placeholder { color: #BBB; }

/* ===== 选择支付方式 ===== */
.pay-section { margin-top: 24rpx; }
.pay-section-title {
  font-size: 32rpx;
  font-weight: 700;
  color: #1B1C1C;
  margin: 4rpx 0 18rpx;
}
.pay-option {
  display: flex;
  align-items: center;
  gap: 20rpx;
  padding: 22rpx 24rpx;
  margin-bottom: 16rpx;
  background: #FFFFFF;
  border: 2rpx solid #E8E3D8;
  border-radius: 16rpx;
}
.pay-option-on {
  background: #F4F9EF;
  border-color: $theme-primary;
}
.pay-logo {
  width: 76rpx;
  height: 76rpx;
  flex-shrink: 0;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32rpx;
  font-weight: 700;
}
.pay-logo.wechat { background: #E4F3E5; color: $theme-primary; }
.pay-logo.alipay { background: #E8F0FE; color: #1677FF; }
.pay-main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 8rpx;
}
.pay-title-row {
  display: flex;
  align-items: center;
  gap: 10rpx;
}
.pay-name {
  font-size: 30rpx;
  font-weight: 700;
  color: #1B1C1C;
}
.pay-desc {
  font-size: 22rpx;
  color: #8A8578;
}
.pay-tag {
  height: 34rpx;
  padding: 0 10rpx;
  display: flex;
  align-items: center;
  border-radius: 6rpx;
  background: $theme-primary;
  color: #FFFFFF;
  font-size: 20rpx;
  font-weight: 600;
}
.pay-radio {
  width: 44rpx;
  height: 44rpx;
  flex-shrink: 0;
  border-radius: 50%;
  border: 2rpx solid #C9C5BA;
  background: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
}
.pay-radio-on {
  background: $theme-primary;
  border-color: $theme-primary;
}

/* ===== 品质保证 ===== */
.guar-card {
  display: flex;
  justify-content: space-around;
  padding: 28rpx 16rpx;
}
.guar-item {
  display: flex;
  align-items: center;
  gap: 8rpx;
  font-size: 24rpx;
  color: $theme-primary;
}

/* ===== 底部占位 ===== */
.bottom-placeholder { height: 180rpx; }

/* ===== 提交订单按钮（结算栏操作区 slot） ===== */
.submit-btn {
  flex-shrink: 0;
  height: 84rpx;
  padding: 0 44rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6rpx;
  background: $theme-primary;
  color: #FFFFFF;
  font-size: 28rpx;
  font-weight: 500;
  border-radius: 100rpx;
  box-shadow: 0 8rpx 24rpx rgba(46, 125, 50, 0.28);
}
.submit-count {
  font-size: 23rpx;
  opacity: 0.9;
}

/* ===== 收货地址选择弹窗 ===== */
.address-picker {
  max-height: 62vh;
}

.address-picker-body {
  max-height: 46vh;
}

.address-option {
  display: flex;
  align-items: center;
  gap: 20rpx;
  padding: 24rpx;
  margin-bottom: 16rpx;
  border-radius: 16rpx;
  background: #F6F3F2;
  border: 2rpx solid transparent;
}

.address-option.selected {
  border-color: $theme-primary;
  background: #F4F9EF;
}

.address-option-main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 8rpx;
}

.address-option-head {
  display: flex;
  align-items: center;
  gap: 14rpx;
}

.address-option-name {
  font-size: 27rpx;
  font-weight: 700;
  color: #1B1C1C;
}

.address-option-phone {
  font-size: 23rpx;
  color: #6B6B6B;
}

.address-option-detail {
  font-size: 22rpx;
  color: #40493D;
  line-height: 1.5;
}

.address-add-btn {
  background: #F4F9EF;
}

.address-add-btn .picker-clear-text {
  color: $theme-primary;
}

/* ===== 优惠券选择弹窗 ===== */
.coupon-picker-mask {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: 200;
  display: flex;
  align-items: flex-end;
}

.coupon-picker {
  width: 100%;
  max-height: 70vh;
  background: #ffffff;
  border-radius: 32rpx 32rpx 0 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.picker-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 28rpx 32rpx;
  border-bottom: 1rpx solid #F3F3F3;
}

.picker-title {
  font-size: 32rpx;
  font-weight: 700;
  color: #1B1C1C;
}

.picker-close {
  width: 56rpx;
  height: 56rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.picker-body {
  flex: 1;
  padding: 16rpx 32rpx;
  max-height: 50vh;
}

.picker-coupon-item {
  display: flex;
  align-items: center;
  gap: 20rpx;
  padding: 24rpx;
  margin-bottom: 16rpx;
  border-radius: 16rpx;
  background: #F6F3F2;
  border: 2rpx solid transparent;
}

.picker-coupon-item.selected {
  border-color: $theme-primary;
  background: #F4F9EF;
}

.picker-coupon-item.disabled {
  opacity: 0.5;
}
.picker-coupon-item.disabled .picker-coupon-val,
.picker-coupon-item.disabled .picker-coupon-yen {
  color: #999999;
}

.picker-coupon-left {
  width: 100rpx;
  height: 100rpx;
  border-radius: 12rpx;
  background: #ffffff;
  display: flex;
  align-items: baseline;
  justify-content: center;
  flex-shrink: 0;
  gap: 2rpx;
}

.picker-coupon-yen {
  font-size: 24rpx;
  font-weight: 600;
  color: $theme-accent;
}

.picker-coupon-val {
  font-size: 40rpx;
  font-weight: 700;
  color: $theme-accent;
  line-height: 1;
}

.picker-coupon-right {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 6rpx;
}

.picker-coupon-name {
  font-size: 26rpx;
  font-weight: 600;
  color: #1B1C1C;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.picker-coupon-threshold {
  font-size: 22rpx;
  color: #40493D;
}

.picker-coupon-expire {
  font-size: 20rpx;
  color: #707A6C;
}

.picker-check {
  width: 40rpx;
  height: 40rpx;
  border-radius: 20rpx;
  background: $theme-primary;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.picker-empty {
  padding: 80rpx 0;
  text-align: center;
}

.picker-empty-text {
  font-size: 26rpx;
  color: #8A8A8A;
}

.picker-footer {
  padding: 16rpx 32rpx 28rpx;
  border-top: 1rpx solid #F3F3F3;
}

.picker-clear-btn {
  text-align: center;
  padding: 16rpx;
  border-radius: 12rpx;
  background: #F6F3F2;
}

.picker-clear-text {
  font-size: 26rpx;
  font-weight: 600;
  color: #707A6C;
}
</style>
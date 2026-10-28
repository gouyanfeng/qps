<template>
  <view class="page">
    <q-page-header-back title="收货地址" subtitle="DELIVERY ADDRESSES" />
    <scroll-view scroll-y class="page-body">
      <view v-for="address in addresses" :key="address.id" class="address-card" @click="selectAddress(address)">
        <view class="address-heading">
          <text class="receiver">{{ address.receiverName }}</text>
          <text class="phone">{{ address.receiverPhone }}</text>
          <text v-if="address.isDefault" class="default-tag">默认</text>
        </view>
        <text class="address-text">{{ address.fullAddress }}</text>
        <view class="address-actions">
          <text @click.stop="removeAddress(address.id)">删除</text>
          <text class="select-tip">点击选择</text>
        </view>
      </view>

      <view class="form-card">
        <text class="form-title">新增收货地址</text>
        <input v-model.trim="form.receiverName" class="form-input" placeholder="收货人" />
        <input v-model.trim="form.receiverPhone" class="form-input" type="number" placeholder="联系电话" />
        <view class="region-row">
          <input v-model.trim="form.provinceName" class="form-input" placeholder="省份" />
          <input v-model.trim="form.cityName" class="form-input" placeholder="城市" />
          <input v-model.trim="form.districtName" class="form-input" placeholder="区县" />
        </view>
        <input v-model.trim="form.detailAddress" class="form-input" placeholder="详细地址" />
        <view class="default-option" @click="form.isDefault = !form.isDefault">
          <checkbox :checked="form.isDefault" color="#2E7D32" />
          <text>设为默认地址</text>
        </view>
        <button class="submit-button" :loading="saving" @click="submit">保存地址</button>
      </view>
    </scroll-view>
  </view>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { deleteAddress, getAddresses, isLoggedIn, saveAddress } from '@/api'
import type { MallAddress } from '@/api'

const addresses = ref<MallAddress[]>([])
const saving = ref(false)
const form = reactive({
  receiverName: '',
  receiverPhone: '',
  provinceName: '',
  cityName: '',
  districtName: '',
  detailAddress: '',
  isDefault: false
})

onShow(async () => {
  if (!isLoggedIn()) {
    uni.navigateTo({ url: '/pages/account/index' })
    return
  }
  await loadAddresses()
})

async function loadAddresses() {
  try {
    addresses.value = await getAddresses()
  } catch (error) {
    console.error('加载收货地址失败', error)
    uni.showToast({ title: '地址加载失败', icon: 'none' })
  }
}

function selectAddress(address: MallAddress) {
  uni.setStorageSync('qps_checkout_address_id', address.id)
  uni.navigateBack()
}

async function removeAddress(addressId: string) {
  try {
    await deleteAddress(addressId)
    addresses.value = addresses.value.filter(address => address.id !== addressId)
  } catch (error) {
    console.error('删除收货地址失败', error)
    uni.showToast({ title: '删除失败', icon: 'none' })
  }
}

async function submit() {
  if (!form.receiverName || !/^1\d{10}$/.test(form.receiverPhone) ||
      !form.provinceName || !form.cityName || !form.districtName || !form.detailAddress) {
    uni.showToast({ title: '请完整填写收货信息', icon: 'none' })
    return
  }
  saving.value = true
  try {
    await saveAddress({
      ...form,
      provinceCode: form.provinceName,
      cityCode: form.cityName,
      districtCode: form.districtName
    })
    Object.assign(form, {
      receiverName: '',
      receiverPhone: '',
      provinceName: '',
      cityName: '',
      districtName: '',
      detailAddress: '',
      isDefault: false
    })
    await loadAddresses()
    uni.showToast({ title: '地址已保存', icon: 'success' })
  } catch (error) {
    console.error('保存收货地址失败', error)
    uni.showToast({ title: '地址保存失败', icon: 'none' })
  } finally {
    saving.value = false
  }
}
</script>

<style lang="scss" scoped>
.page {
  min-height: 100vh;
  background: var(--page-bg);
}

.page-body {
  height: calc(100vh - var(--page-top-offset));
  padding: 24rpx;
  box-sizing: border-box;
}

.address-card,
.form-card {
  margin-bottom: 20rpx;
  padding: 24rpx;
  background: #ffffff;
  border-radius: 18rpx;
}

.address-heading {
  display: flex;
  align-items: center;
  gap: 16rpx;
}

.receiver {
  color: #1B1C1C;
  font-size: 28rpx;
  font-weight: 700;
}

.phone,
.address-text,
.select-tip {
  color: #6B6B6B;
  font-size: 23rpx;
}

.address-text {
  display: block;
  margin-top: 12rpx;
}

.default-tag {
  padding: 4rpx 12rpx;
  color: #2E7D32;
  background: #E8F5E9;
  border-radius: 10rpx;
  font-size: 20rpx;
}

.address-actions {
  display: flex;
  justify-content: space-between;
  margin-top: 18rpx;
  color: #C62828;
  font-size: 22rpx;
}

.form-title {
  display: block;
  margin-bottom: 16rpx;
  color: #1B1C1C;
  font-size: 28rpx;
  font-weight: 700;
}

.form-input {
  height: 78rpx;
  margin-bottom: 14rpx;
  padding: 0 16rpx;
  background: #F7F6F2;
  border-radius: 10rpx;
  font-size: 23rpx;
}

.region-row {
  display: flex;
  gap: 10rpx;
}

.region-row .form-input {
  width: 0;
  flex: 1;
}

.default-option {
  display: flex;
  align-items: center;
  margin: 10rpx 0;
  color: #40493D;
  font-size: 23rpx;
}

.submit-button {
  margin-top: 16rpx;
  color: #ffffff;
  background: #2E7D32;
  border-radius: 42rpx;
  font-size: 26rpx;
}
</style>

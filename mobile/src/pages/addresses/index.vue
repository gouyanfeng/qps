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

        <picker mode="selector" :range="provinceNames" :value="provinceIndex" @change="onProvinceChange">
          <view class="form-picker">
            <text :class="{ placeholder: !form.provinceName }">
              {{ form.provinceName || '请选择省份' }}
            </text>
            <text class="picker-arrow">›</text>
          </view>
        </picker>
        <picker
          mode="selector"
          :range="cityNames"
          :value="cityIndex"
          :disabled="!selectedProvinceCode"
          @change="onCityChange"
        >
          <view class="form-picker" :class="{ disabled: !selectedProvinceCode }">
            <text :class="{ placeholder: !form.cityName }">
              {{ form.cityName || (selectedProvinceCode ? '请选择城市' : '请先选省份') }}
            </text>
            <text class="picker-arrow">›</text>
          </view>
        </picker>
        <picker
          mode="selector"
          :range="districtNames"
          :value="districtIndex"
          :disabled="!selectedCityCode"
          @change="onDistrictChange"
        >
          <view class="form-picker" :class="{ disabled: !selectedCityCode }">
            <text :class="{ placeholder: !form.districtName }">
              {{ form.districtName || (selectedCityCode ? '请选择区县' : '请先选城市') }}
            </text>
            <text class="picker-arrow">›</text>
          </view>
        </picker>

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
import { computed, reactive, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { deleteAddress, getAddresses, getChinaRegions, saveAddress } from '@/api'
import type { ChinaRegionNode, MallAddress } from '@/api'
import { UnauthorizedError } from '@/utils/http'

const addresses = ref<MallAddress[]>([])
const saving = ref(false)
const regionAll = ref<ChinaRegionNode[]>([])

const form = reactive({
  receiverName: '',
  receiverPhone: '',
  provinceName: '',
  cityName: '',
  districtName: '',
  detailAddress: '',
  isDefault: false
})

const provinces = computed(() => regionAll.value.filter(r => r.level === 1))
const provinceNames = computed(() => provinces.value.map(r => r.name))
const provinceIndex = ref(0)
const selectedProvinceCode = computed(() => provinces.value[provinceIndex.value]?.code ?? '')

const cities = computed(() => regionAll.value.filter(r => r.level === 2 && r.parentCode === selectedProvinceCode.value))
const cityNames = computed(() => cities.value.map(r => r.name))
const cityIndex = ref(0)
const selectedCityCode = computed(() => cities.value[cityIndex.value]?.code ?? '')

const districts = computed(() => regionAll.value.filter(r => r.level === 3 && r.parentCode === selectedCityCode.value))
const districtNames = computed(() => districts.value.map(r => r.name))
const districtIndex = ref(0)

onShow(async () => {
  await Promise.all([loadAddresses(), loadRegions()])
  syncFormFromIndex()
})

async function loadRegions() {
  try {
    regionAll.value = await getChinaRegions()
  } catch (error) {
    console.error('加载地区数据失败', error)
  }
}

function syncFormFromIndex() {
  const p = provinces.value[provinceIndex.value]
  form.provinceName = p?.name ?? ''
  const c = cities.value[cityIndex.value]
  form.cityName = c?.name ?? ''
  const d = districts.value[districtIndex.value]
  form.districtName = d?.name ?? ''
}

async function loadAddresses() {
  try {
    addresses.value = await getAddresses()
  } catch (error) {
    console.error('加载收货地址失败', error)
    if (error instanceof UnauthorizedError) return
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
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '删除失败', icon: 'none' })
  }
}

function onProvinceChange(e: { detail: { value: number } }) {
  provinceIndex.value = e.detail.value
  const picked = provinces.value[provinceIndex.value]
  form.provinceName = picked?.name ?? ''
  form.cityName = cities.value[0]?.name ?? ''
  form.districtName = districts.value[0]?.name ?? ''
  cityIndex.value = 0
  districtIndex.value = 0
}

function onCityChange(e: { detail: { value: number } }) {
  cityIndex.value = e.detail.value
  const picked = cities.value[cityIndex.value]
  form.cityName = picked?.name ?? ''
  form.districtName = districts.value[0]?.name ?? ''
  districtIndex.value = 0
}

function onDistrictChange(e: { detail: { value: number } }) {
  districtIndex.value = e.detail.value
  const picked = districts.value[districtIndex.value]
  form.districtName = picked?.name ?? ''
}

async function submit() {
  if (!form.receiverName) { uni.showToast({ title: '请填写收货人', icon: 'none' }); return }
  if (!/^1\d{10}$/.test(form.receiverPhone)) { uni.showToast({ title: '手机号格式不对', icon: 'none' }); return }
  if (!form.provinceName) { uni.showToast({ title: '请选择省份', icon: 'none' }); return }
  if (!form.cityName) { uni.showToast({ title: '请选择城市', icon: 'none' }); return }
  if (!form.districtName) { uni.showToast({ title: '请选择区县', icon: 'none' }); return }
  if (!form.detailAddress) { uni.showToast({ title: '请填写详细地址', icon: 'none' }); return }
  saving.value = true
  try {
    await saveAddress({ ...form })
    Object.assign(form, {
      receiverName: '',
      receiverPhone: '',
      provinceName: '',
      cityName: '',
      districtName: '',
      detailAddress: '',
      isDefault: false
    })
    provinceIndex.value = 0
    cityIndex.value = 0
    districtIndex.value = 0
    await loadAddresses()
    uni.showToast({ title: '地址已保存', icon: 'success' })
  } catch (error) {
    console.error('保存收货地址失败', error)
    if (error instanceof UnauthorizedError) return
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

.form-picker {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 78rpx;
  margin-bottom: 14rpx;
  padding: 0 16rpx;
  background: #F7F6F2;
  border-radius: 10rpx;
  font-size: 23rpx;
  color: #1B1C1C;

  &.placeholder {
    color: #999;
  }

  &.disabled {
    color: #bbb;
  }
}

.picker-arrow {
  color: #bbb;
  font-size: 32rpx;
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
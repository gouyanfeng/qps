<template>
  <q-page refresh-enabled :refresher-triggered="refreshing" @refresh="onRefresh">
    <q-page-header-back title="收货地址" />
    <view v-for="address in addresses" :key="address.id" class="address-card" @click="selectAddress(address)">
      <view class="address-heading">
        <text class="receiver">{{ address.receiverName }}</text>
        <text class="phone">{{ address.receiverPhone }}</text>
        <text v-if="address.isDefault" class="default-tag">默认</text>
      </view>
      <text class="address-text">{{ address.fullAddress }}</text>
      <view class="address-actions">
        <text class="edit-action" @click.stop="startEdit(address)">编辑</text>
        <text @click.stop="removeAddress(address.id)">删除</text>
        <text class="select-tip">点击选择</text>
      </view>
    </view>

    <view class="form-card">
      <text class="form-title">{{ editingId ? '编辑收货地址' : '新增收货地址' }}</text>
      <q-field v-model.trim="form.receiverName" class="form-input" placeholder="收货人" height="78rpx" />
      <q-field v-model.trim="form.receiverPhone" class="form-input" type="number" placeholder="联系电话" height="78rpx" />

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

      <q-field v-model.trim="form.detailAddress" class="form-input" placeholder="详细地址" height="78rpx" />
      <view class="default-option" @click="form.isDefault = !form.isDefault">
        <checkbox :checked="form.isDefault" color="#2E7D32" />
        <text>设为默认地址</text>
      </view>
      <view class="form-actions">
        <button v-if="editingId" class="cancel-button" @click="cancelEdit">取消编辑</button>
        <button class="submit-button" :loading="saving" @click="submit">
          {{ editingId ? '保存修改' : '保存地址' }}
        </button>
      </view>
    </view>
  </q-page>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { deleteAddress, getAddresses, getChinaRegions, saveAddress, updateAddress } from '@/api'
import type { ChinaRegionNode, MallAddress } from '@/api'
import { UnauthorizedError } from '@/utils/http'

const addresses = ref<MallAddress[]>([])
const saving = ref(false)
const refreshing = ref(false)
const regionAll = ref<ChinaRegionNode[]>([])
const editingId = ref<string | null>(null)

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
  cancelEdit()
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

async function onRefresh() {
  refreshing.value = true
  try {
    await Promise.all([loadAddresses(), loadRegions()])
    syncFormFromIndex()
  } finally {
    refreshing.value = false
  }
}

function resetForm() {
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
  editingId.value = null
  syncFormFromIndex()
}

function startEdit(address: MallAddress) {
  editingId.value = address.id
  provinceIndex.value = Math.max(0, provinces.value.findIndex(r => r.name === address.provinceName))
  cityIndex.value = Math.max(0, cities.value.findIndex(r => r.name === address.cityName))
  districtIndex.value = Math.max(0, districts.value.findIndex(r => r.name === address.districtName))
  Object.assign(form, {
    receiverName: address.receiverName,
    receiverPhone: address.receiverPhone,
    provinceName: address.provinceName,
    cityName: address.cityName,
    districtName: address.districtName,
    detailAddress: address.detailAddress,
    isDefault: address.isDefault
  })
  uni.showToast({ title: '请修改后保存', icon: 'none' })
}

function cancelEdit() {
  resetForm()
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
  const isEdit = !!editingId.value
  try {
    if (isEdit) {
      await updateAddress(editingId.value!, { ...form })
    } else {
      const saved = await saveAddress({ ...form })
      // 从结算页「新增收货地址」进入时回写新地址 ID，返回结算页后可自动选中
      const savedId = typeof saved === 'string' ? saved : ''
      if (savedId && uni.getStorageSync('qps_checkout_add_address')) {
        uni.setStorageSync('qps_checkout_address_id', savedId)
      }
    }
    resetForm()
    await loadAddresses()
    uni.showToast({ title: isEdit ? '修改成功' : '地址已保存', icon: 'success' })
  } catch (error) {
    console.error('保存收货地址失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: isEdit ? '修改失败' : '地址保存失败', icon: 'none' })
  } finally {
    saving.value = false
  }
}
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>
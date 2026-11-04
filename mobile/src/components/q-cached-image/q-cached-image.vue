<template>
  <!-- #ifdef H5 -->
  <image v-bind="$attrs" :src="networkSrc" :mode="mode" />
  <!-- #endif -->
  <!-- #ifndef H5 -->
  <image
    v-bind="$attrs"
    :src="localSrc || networkSrc"
    :mode="mode"
    @error="onLocalError"
  />
  <!-- #endif -->
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { resolveFileUrl } from '@/utils/http'

const props = withDefaults(defineProps<{
  src?: string | null
  mode?: string
}>(), {
  src: '',
  mode: 'aspectFill'
})

defineOptions({ inheritAttrs: false })

const networkSrc = computed(() => resolveFileUrl(props.src))

// #ifndef H5
import { useLocalImageCache } from '@/utils/imageCache'

const { get: getCached, getAsync, onError } = useLocalImageCache()
const localSrc = ref('')

// 首次渲染先用同步 get()：已有缓存直接展示本地文件，否则触发后台下载
// 并先回退到 networkSrc 展示（localSrc 为空，模板里 localSrc || networkSrc 兜底）。
// 下载完成后 await getAsync() 拿到本地路径赋值 localSrc，Vue 响应式替换 src。
watch(networkSrc, async url => {
  localSrc.value = getCached(url)
  if (!localSrc.value && url) {
    const path = await getAsync(url)
    if (path && url === networkSrc.value) {
      localSrc.value = path
    }
  }
}, { immediate: true })

function onLocalError() {
  onError(networkSrc.value, localSrc.value)
  localSrc.value = ''
}
// #endif
</script>

/**
 * 非 H5 平台图片本地文件缓存。
 *
 * - 键：图片绝对 URL；值：uni.saveFile 返回的本地临时/持久路径。
 * - 最多保留 200 条，超过后按写入顺序淘汰最旧文件。
 * - 缓存版本号变化（例如升级 App）会清空并重建。
 * - H5 下本模块会被条件编译裁掉，完全不参与打包。
 */

// #ifndef H5
import { reactive } from 'vue'

const CACHE_MAP_KEY = 'qps_image_cache_map'
const CACHE_VERSION_KEY = 'qps_image_cache_version'
const CACHE_VERSION = 'v1'
const MAX_CACHE_COUNT = 200

const cachedMap = reactive<Record<string, string>>({})
const pending = new Map<string, Promise<void>>()
let initialized = false

function persist() {
  uni.setStorageSync(CACHE_MAP_KEY, { ...cachedMap })
}

function removeFile(path: string) {
  if (typeof uni.removeSavedFile !== 'function') return
  uni.removeSavedFile({ filePath: path, complete: () => {} })
}

function init() {
  if (initialized) return
  initialized = true

  const version = uni.getStorageSync(CACHE_VERSION_KEY) as string
  const stored = uni.getStorageSync(CACHE_MAP_KEY) as Record<string, string>

  if (version !== CACHE_VERSION) {
    if (stored && typeof stored === 'object') {
      Object.values(stored).forEach(removeFile)
    }
    uni.setStorageSync(CACHE_MAP_KEY, {})
    uni.setStorageSync(CACHE_VERSION_KEY, CACHE_VERSION)
    return
  }

  if (stored && typeof stored === 'object') {
    Object.assign(cachedMap, stored)
  }
}

function evictOverflow() {
  const keys = Object.keys(cachedMap)
  if (keys.length <= MAX_CACHE_COUNT) return
  keys.slice(0, keys.length - MAX_CACHE_COUNT).forEach(key => {
    removeFile(cachedMap[key])
    delete cachedMap[key]
  })
  persist()
}

function download(url: string): Promise<void> {
  if (!/^https?:\/\//i.test(url)) return Promise.resolve()
  if (cachedMap[url] || pending.has(url)) return pending.get(url)!

  const task = new Promise<void>(resolve => {
    uni.downloadFile({
      url,
      success: res => {
        if (res.statusCode !== 200) {
          resolve()
          return
        }
        uni.saveFile({
          tempFilePath: res.tempFilePath,
          success: save => {
            cachedMap[url] = save.savedFilePath
            persist()
            evictOverflow()
            resolve()
          },
          fail: () => resolve()
        })
      },
      fail: () => resolve()
    })
  }).finally(() => pending.delete(url))

  pending.set(url, task)
  return task
}

export function useLocalImageCache() {
  init()

  /**
   * 同步获取本地缓存路径；若未缓存则触发后台下载并返回空串，
   * 组件会先回退到网络地址展示，待 download 完成后可通过 getAsync 回填。
   */
  function get(url: string): string {
    if (!url) return ''
    if (cachedMap[url]) return cachedMap[url]

    if (/^https?:\/\//i.test(url)) {
      download(url)
    }
    return ''
  }

  /**
   * 异步获取本地缓存路径：已缓存则立即 resolve，否则等待下载完成。
   * 组件首次渲染先用 get() 回退到 networkSrc，再 await 此函数拿到本地路径，
   * 响应式替换 src，实现「首屏网图 → 本地缓存」的无闪烁切换。
   */
  async function getAsync(url: string): Promise<string> {
    if (!url) return ''
    if (cachedMap[url]) return cachedMap[url]
    if (!/^https?:\/\//i.test(url)) return ''

    await download(url)
    return cachedMap[url] ?? ''
  }

  /** 本地文件展示失败时调用，清理坏缓存并让组件回退到网络地址。 */
  function onError(url: string, currentPath: string) {
    if (!url || !currentPath) return
    if (cachedMap[url] === currentPath) {
      removeFile(currentPath)
      delete cachedMap[url]
      persist()
    }
  }

  return { get, getAsync, onError }
}

/** 清除全部本地图片缓存（含已保存文件），返回清除的缓存条数。 */
export function clearLocalImageCache(): number {
  const stored = uni.getStorageSync(CACHE_MAP_KEY) as Record<string, string>
  const count = stored && typeof stored === 'object' ? Object.keys(stored).length : 0
  if (count > 0) {
    Object.values(stored).forEach(removeFile)
  }
  Object.keys(cachedMap).forEach(key => delete cachedMap[key])
  uni.setStorageSync(CACHE_MAP_KEY, {})
  return count
}
// #endif

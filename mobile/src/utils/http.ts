// API 基址：未显式设置 VITE_API_BASE_URL 时本地默认连 5000；
// 容器部署时设为空串，请求走同源相对路径（BASE_URL + "/api/..."），由网关转发。
export const BASE_URL =
  import.meta.env.VITE_API_BASE_URL === undefined
    ? 'http://localhost:5000'
    : import.meta.env.VITE_API_BASE_URL.replace(/\/+$/, '')

/** 静态文件（图片等）访问域名，可通过 VITE_FILE_BASE_URL 覆盖；未配置时默认走阿里云 OSS */
export const FILE_BASE_URL = (import.meta.env.VITE_FILE_BASE_URL || 'https://qps.oss-rg-china-mainland.aliyuncs.com').replace(/\/+$/, '')

const TOKEN_KEY = 'qps_member_token'
const LOGIN_PATH = '/pages/account/index'
const LOGIN_REDIRECT_PATH_KEY = 'qps_login_redirect_path'

let unauthorizedPromptVisible = false

export class UnauthorizedError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'UnauthorizedError'
  }
}

export interface ApiResp<T> {
  data: T
  code: number
  msg: string
}

export interface PagedData<T> {
  list: T[]
  totalCount: number
  currentPage: number
  pageSize: number
  totalPages: number
}

function getToken(): string {
  return uni.getStorageSync(TOKEN_KEY) || ''
}

export function setToken(token: string): void {
  uni.setStorageSync(TOKEN_KEY, token)
}

export function clearToken(): void {
  uni.removeStorageSync(TOKEN_KEY)
}

export function isLoggedIn(): boolean {
  return !!getToken()
}

function isLoginPath(path: string): boolean {
  const route = path.split('?')[0]
  return route === LOGIN_PATH || route === '/pages/login/index'
}

export function consumeLoginRedirectPath(): string | null {
  const path = uni.getStorageSync(LOGIN_REDIRECT_PATH_KEY)
  uni.removeStorageSync(LOGIN_REDIRECT_PATH_KEY)

  if (typeof path !== 'string' || !path.startsWith('/pages/') || path.startsWith('//')) return null
  if (isLoginPath(path)) return null
  return path
}

function saveLoginRedirectPath(): void {
  const currentPages = getCurrentPages()
  const page = currentPages[currentPages.length - 1] as
    | (typeof currentPages[number] & { $page?: { fullPath?: string } })
    | undefined
  const path = page?.$page?.fullPath || (page?.route ? `/${page.route}` : '')

  if (path.startsWith('/pages/') && !isLoginPath(path)) {
    uni.setStorageSync(LOGIN_REDIRECT_PATH_KEY, path)
  }
}

function handleUnauthorized(path: string): UnauthorizedError {
  const error = new UnauthorizedError('登录状态已失效')
  if (path.startsWith('/api/mall/auth/')) return error

  clearToken()
  saveLoginRedirectPath()
  if (unauthorizedPromptVisible) return error

  unauthorizedPromptVisible = true
  uni.showModal({
    title: '请先登录',
    content: '登录后将自动返回刚才的页面。',
    confirmText: '去登录',
    showCancel: false,
    success(result) {
      if (!result.confirm) return

      const currentPages = getCurrentPages()
      if (currentPages[currentPages.length - 1]?.route === LOGIN_PATH.slice(1)) return

      uni.navigateTo({
        url: LOGIN_PATH,
        fail(navigationError) {
          console.error('跳转会员登录页失败', navigationError)
          uni.showToast({ title: '打开登录页失败，请重试', icon: 'none' })
        }
      })
    },
    fail(modalError) {
      console.error('显示登录提示失败', modalError)
      uni.showToast({ title: '请先登录后继续操作', icon: 'none' })
    },
    complete() {
      unauthorizedPromptVisible = false
    }
  })

  return error
}

/**
 * 把后端返回的文件地址补全为可直接加载的 URL。
 * 本地静态资源（/static）与绝对地址原样返回，上传目录等相对路径拼接 API 基址。
 */
export function resolveFileUrl(path?: string | null): string {
  if (!path) return ''
  if (/^(https?:)?\/\//i.test(path) || path.startsWith('data:')) return path
  if (path.startsWith('/static/')) return path
  return `${FILE_BASE_URL}${path.startsWith('/') ? '' : '/'}${path}`
}

export function request<T>(
  path: string,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' = 'GET',
  data?: any
): Promise<T> {
  return new Promise((resolve, reject) => {
    const header: Record<string, string> = {
      'Content-Type': 'application/json'
    }
    const token = getToken()
    if (token) header['Authorization'] = `Bearer ${token}`

    let settled = false
    const timer = setTimeout(() => {
      if (settled) return
      settled = true
      reject(new Error('请求超时，请检查网络'))
    }, 15000)

    uni.request({
      url: BASE_URL + path,
      method,
      data,
      header,
      timeout: 15000,
      success(res) {
        if (settled) return
        settled = true
        clearTimeout(timer)
        const body = res.data as ApiResp<T>
        if (res.statusCode === 200 && body && body.code === 200) {
          resolve(body.data)
        } else if (res.statusCode === 401 || body?.code === 401) {
          const error = handleUnauthorized(path)
          error.message = body?.msg || error.message
          reject(error)
        } else {
          reject(new Error(body?.msg || `请求失败(${res.statusCode})`))
        }
      },
      fail(err) {
        if (settled) return
        settled = true
        clearTimeout(timer)
        reject(new Error(err.errMsg || '网络请求失败'))
      }
    })
  })
}

export function requestRaw<T>(
  path: string,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' = 'GET',
  data?: any
): Promise<ApiResp<T>> {
  return new Promise((resolve, reject) => {
    const header: Record<string, string> = { 'Content-Type': 'application/json' }
    const token = getToken()
    if (token) header['Authorization'] = `Bearer ${token}`

    let settled = false
    const timer = setTimeout(() => {
      if (settled) return
      settled = true
      reject(new Error('请求超时，请检查网络'))
    }, 15000)

    uni.request({
      url: BASE_URL + path,
      method,
      data,
      header,
      timeout: 15000,
      success(res) {
        if (settled) return
        settled = true
        clearTimeout(timer)
        const body = res.data as ApiResp<T>
        if (res.statusCode === 401 || body?.code === 401) {
          handleUnauthorized(path)
        }
        resolve(body)
      },
      fail(err) {
        if (settled) return
        settled = true
        clearTimeout(timer)
        reject(new Error(err.errMsg || '网络请求失败'))
      }
    })
  })
}
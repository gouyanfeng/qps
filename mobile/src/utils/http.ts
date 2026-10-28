const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'
const TOKEN_KEY = 'qps_member_token'

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

/**
 * 把后端返回的文件地址补全为可直接加载的 URL。
 * 本地静态资源（/static）与绝对地址原样返回，上传目录等相对路径拼接 API 基址。
 */
export function resolveFileUrl(path?: string | null): string {
  if (!path) return ''
  if (/^(https?:)?\/\//i.test(path) || path.startsWith('data:')) return path
  if (path.startsWith('/static/')) return path
  return `${BASE_URL}${path.startsWith('/') ? '' : '/'}${path}`
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

    uni.request({
      url: BASE_URL + path,
      method,
      data,
      header,
      success(res) {
        const body = res.data as ApiResp<T>
        if (res.statusCode === 200 && body && body.code === 200) {
          resolve(body.data)
        } else if (res.statusCode === 401 || body?.code === 401) {
          clearToken()
          reject(new Error(body?.msg || '未登录或令牌已过期'))
        } else {
          reject(new Error(body?.msg || `请求失败(${res.statusCode})`))
        }
      },
      fail(err) {
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

    uni.request({
      url: BASE_URL + path,
      method,
      data,
      header,
      success(res) {
        resolve(res.data as ApiResp<T>)
      },
      fail(err) {
        reject(new Error(err.errMsg || '网络请求失败'))
      }
    })
  })
}
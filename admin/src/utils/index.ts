const mode = import.meta.env.VITE_ROUTER_MODE;

/**
 * @description 缓存数据到 localStorage（带过期时间）
 * @param {String} key 缓存键名
 * @param {*} data 缓存数据
 * @param {Number} ttlMinutes 过期时间（分钟）
 * @returns {void}
 */
export function cacheSet(key: string, data: any, ttlMinutes: number) {
  const cacheData = {
    timestamp: Date.now(),
    ttl: ttlMinutes * 60 * 1000,
    data
  };
  window.localStorage.setItem(key, JSON.stringify(cacheData));
}

/**
 * @description 读取 localStorage 缓存（检查过期）
 * @param {String} key 缓存键名
 * @param {Number} ttlMinutes 过期时间（分钟）
 * @returns {*|null} 未过期返回数据，过期或不存在返回 null
 */
export function cacheGet(key: string, ttlMinutes: number) {
  const raw = window.localStorage.getItem(key);
  if (!raw) return null;

  try {
    const parsed = JSON.parse(raw);
    if (!parsed || !parsed.timestamp || !parsed.ttl) return null;

    const elapsed = Date.now() - parsed.timestamp;
    if (elapsed > parsed.ttl) {
      window.localStorage.removeItem(key);
      return null;
    }

    return parsed.data;
  } catch {
    return null;
  }
}

/**
 * @description 生成唯一 uuid
 * @returns {String}
 */
export function generateUUID() {
  let uuid = "";
  for (let i = 0; i < 32; i++) {
    let random = (Math.random() * 16) | 0;
    if (i === 8 || i === 12 || i === 16 || i === 20) uuid += "-";
    uuid += (i === 12 ? 4 : i === 16 ? (random & 3) | 8 : random).toString(16);
  }
  return uuid;
}

/**
 * @description 获取浏览器默认语言
 * @returns {String}
 */
export function getBrowserLang() {
  let browserLang = navigator.language
    ? navigator.language
    : navigator.browserLanguage;
  let defaultBrowserLang = "";
  if (["cn", "zh", "zh-cn"].includes(browserLang.toLowerCase())) {
    defaultBrowserLang = "zh";
  } else {
    defaultBrowserLang = "en";
  }
  return defaultBrowserLang;
}

/**
 * @description 获取不同路由模式所对应的 url + params
 * @returns {String}
 */
export function getUrlWithParams() {
  const url = {
    hash: location.hash.substring(1),
    history: location.pathname + location.search,
  };
  return url[mode];
}

/**
 * @description 使用递归扁平化菜单，方便添加动态路由
 * @param {Array} menuList 菜单列表
 * @returns {Array}
 */
export function getFlatMenuList(
  menuList: Menu.MenuOptions[],
): Menu.MenuOptions[] {
  let newMenuList: Menu.MenuOptions[] = JSON.parse(JSON.stringify(menuList));
  return newMenuList.flatMap((item) => [
    item,
    ...(item.children ? getFlatMenuList(item.children) : []),
  ]);
}

/**
 * @description 使用递归过滤出需要渲染在左侧菜单的列表 (需剔除 isHide == true 的菜单)
 * @param {Array} menuList 菜单列表
 * @returns {Array}
 * */
export function getShowMenuList(menuList: Menu.MenuOptions[], userPermissions: string[] = []) {
  let newMenuList: Menu.MenuOptions[] = JSON.parse(JSON.stringify(menuList));
  return newMenuList.filter((item) => {
    const hadChildren = !!item.children?.length;

    // 递归过滤子菜单
    if (hadChildren) {
      item.children = getShowMenuList(item.children!, userPermissions);
      // 原本是模块容器，子菜单全部被过滤后不再保留空模块
      if (!item.children.length) return false;
    }

    // 菜单自身（含模块级）声明的 permissionCode 必须命中
    if (item.meta?.permissionCode && !userPermissions.includes(item.meta.permissionCode)) return false;

    return !item.meta?.isHide;
  });
}

/**
 * @description 递归收集菜单路径对应的权限码链（含父级模块码），用于路由守卫校验
 * @param {Array} menuList 完整菜单列表
 * @param {Array} parentCodes 父级已声明的权限码
 * @returns {Object} path -> permissionCode[]
 */
export function getMenuPermissionMap(
  menuList: Menu.MenuOptions[],
  parentCodes: string[] = [],
): Record<string, string[]> {
  const map: Record<string, string[]> = {};
  menuList.forEach((item) => {
    const codes = item.meta?.permissionCode ? [...parentCodes, item.meta.permissionCode] : [...parentCodes];
    if (item.path) map[item.path] = [...new Set(codes)];
    if (item.children?.length) {
      Object.assign(map, getMenuPermissionMap(item.children, codes));
    }
  });
  return map;
}

/**
 * @description 使用递归找出所有面包屑存储到 pinia/vuex 中
 * @param {Array} menuList 菜单列表
 * @param {Array} parent 父级菜单
 * @param {Object} result 处理后的结果
 * @returns {Object}
 */
export const getAllBreadcrumbList = (
  menuList: Menu.MenuOptions[],
  parent = [],
  result: { [key: string]: any } = {},
) => {
  for (const item of menuList) {
    result[item.path] = [...parent, item];
    if (item.children)
      getAllBreadcrumbList(item.children, result[item.path], result);
  }
  return result;
};

/**
 * 静态文件（图片等）访问域名，可通过环境变量 VITE_FILE_BASE_URL 覆盖；
 * 未配置时默认走阿里云 OSS。
 */
export const FILE_BASE_URL: string = (import.meta.env.VITE_FILE_BASE_URL || "https://qps-bucket.oss-cn-hangzhou.aliyuncs.com").replace(/\/+$/, "");

/**
 * @description 将后端返回的相对文件地址拼接为可访问地址
 * @param path 相对地址或完整地址
 * @returns 可直接用于 img src 的地址
 */
export function resolveFileUrl(path?: string | null): string {
  if (!path) return "";
  if (/^(https?:)?\/\//i.test(path) || path.startsWith("data:")) return path;

  return `${FILE_BASE_URL}${path.startsWith("/") ? "" : "/"}${path}`;
}

/**
 * @description 将文件地址规整为相对路径存库（剥离文件域名或旧 API 来源前缀）
 * @param url 用户输入或上传回显的地址
 * @returns 相对路径，如 /uploads/product/202610/xxx.png
 */
export function toRelativeFileUrl(url: string): string {
  if (url.startsWith(FILE_BASE_URL)) return url.slice(FILE_BASE_URL.length) || "/";
  const apiUrl = (import.meta.env.VITE_API_URL || "http://localhost:5000/api").replace(/\/api\/?$/, "");
  if (url.startsWith(apiUrl)) return url.slice(apiUrl.length) || "/";
  return url;
}

/**
 * @description 格式化日期显示，避免 "Invalid Date"
 * @param dateString 后端返回的日期字符串
 * @param fallback 空值时的回退文案
 * @returns 格式化后的日期字符串
 */
export function formatDate(dateString?: string | null, fallback = '-'): string {
  if (!dateString) return fallback;
  try {
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return fallback;
    return date.toLocaleString();
  } catch {
    return fallback;
  }
}



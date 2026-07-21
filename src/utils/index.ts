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
    // 递归过滤子菜单
    if (item.children?.length) {
      item.children = getShowMenuList(item.children, userPermissions);
    }

    // 有子菜单且子菜单有数据 → 保留父菜单
    if (item.children?.length) {
      return true;
    }

    // 叶子菜单：检查 permissionCode
    if (item.meta?.permissionCode && userPermissions.length > 0) {
      return userPermissions.includes(item.meta.permissionCode);
    }

    // 无 permissionCode 或 userPermissions 为空 → 按原有 isHide 逻辑
    return !item.meta?.isHide;
  });
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

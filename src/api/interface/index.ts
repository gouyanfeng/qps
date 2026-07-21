// 请求响应参数（不包含data）
export interface Result {
  code: string;
  msg: string;
}

// 请求响应参数（包含data）
export interface ResultData<T = any> extends Result {
  data: T;
}

// 分页请求参数
export interface ReqPage {
  page: number;
  pageSize: number;
}

// 文件上传模块
export namespace Upload {
  export interface ResFileUrl {
    fileUrl: string;
  }
}

// 登录模块
export namespace Login {
  export interface ReqLoginForm {
    username: string;
    password: string;
  }
  export interface ResLogin {
    token: string;
    userId: string;
    username: string;
    realName: string;
    role: string;
  }
  export interface AuthButtonItem {
    action: string;
    permissionCode?: string;
  }
}

// 商户管理模块
export namespace Merchant {
  export interface ReqMerchantParams extends ReqPage {
    name: string;
    phone: string;
    isActive: boolean;
  }
  export interface ResMerchantList {
    id: string;
    name: string;
    phone: string;
    expiryDate: string;
    isActive: boolean;
    createdAt: string;
  }
  export interface ReqMerchantForm {
    name: string;
    phone: string;
    expiryDate: string;
    isActive?: boolean;
  }
  export interface ReqMerchantUpdate {
    name?: string;
    phone?: string;
    expiryDate?: string;
    isActive?: boolean;
  }
  export interface ResMerchantPagination {
    list: ResMerchantList[];
    totalCount: number;
    totalPages: number;
    currentPage: number;
    pageSize: number;
  }
}

// 用户管理模块
export namespace User {
  export interface ReqUserParams extends ReqPage {
    username: string;
    realName: string;
    roleId: string;
    isActive: boolean;
  }
  export interface ResUserList {
    id: string;
    username: string;
    realName: string;
    roleId: string;
    roleName: string;
    isActive: boolean;
    createdAt: string;
  }
  export interface ReqUserForm {
    username: string;
    password: string;
    realName: string;
    roleId: string;
    isActive: boolean;
  }
  export interface ReqUserUpdate {
    username: string;
    realName: string;
    roleId: string;
    isActive: boolean;
  }
  export interface ResUserPagination {
    list: ResUserList[];
    totalCount: number;
    totalPages: number;
    currentPage: number;
    pageSize: number;
  }
}

// 权限管理模块
export namespace Permission {
  // 更新角色权限请求
  export interface ReqRolePermission {
    role: string;
    permissions: string[];
  }
  // 权限树节点
  export interface TreeNode {
    id: string;
    code: string;
    name: string;
    children?: TreeNode[];
  }
}

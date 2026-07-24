export interface Result {
  code: string;
  msg: string;
}

export interface ResultData<T = any> extends Result {
  data: T;
}

export interface ReqPage {
  page: number;
  pageSize: number;
}

export namespace Upload {
  export interface ResFileUrl {
    fileUrl: string;
  }
}

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

export namespace Permission {
  export interface ReqRolePermission {
    role: string;
    permissions: string[];
  }

  export interface TreeNode {
    id: string;
    code: string;
    name: string;
    children?: TreeNode[];
  }
}

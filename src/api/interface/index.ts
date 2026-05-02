// 请求响应参数（不包含data）
export interface Result {
  code: string;
  msg: string;
}

// 请求响应参数（包含data）
export interface ResultData<T = any> extends Result {
  data: T;
}

// 分页响应参数
export interface ResPage<T> {
  list: T[];
  pageNum: number;
  pageSize: number;
  total: number;
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
    merchantId: string;
  }
  export interface ResAuthButtons {
    [key: string]: string[];
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
    role: string;
    isActive: boolean;
  }
  export interface ResUserList {
    id: string;
    username: string;
    realName: string;
    role: string;
    isActive: boolean;
    createdAt: string;
  }
  export interface ReqUserForm {
    username: string;
    password: string;
    realName: string;
    role: string;
    isActive: boolean;
  }
  export interface ReqUserUpdate {
    username: string;
    realName: string;
    role: string;
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

// 订单管理模块
export namespace Order {
  export interface ReqOrderParams {
    OrderNo?: string;
    Status?: string;
    ShopId?: string;
    RoomId?: string;
    CustomerId?: string;
    StartDate?: string;
    EndDate?: string;
    Page?: number;
    PageSize?: number;
    SortField?: string;
    SortDirection?: string;
  }
  export interface ResOrderList {
    id: string;
    orderNo: string;
    shopId: string;
    shopName: string;
    roomId: string;
    roomNumber: string;
    customerId: string;
    customerName: string;
    amount: number;
    durationMinutes: number;
    status: string;
    createdAt: string;
    updatedAt: string;
  }
  export interface ReqOrderForm {
    roomId: string;
    amount: number;
    durationMinutes: number;
  }
  export interface ResOrderPagination {
    list: ResOrderList[];
    totalCount: number;
    totalPages: number;
    currentPage: number;
    pageSize: number;
  }
}

// 标签管理模块
export namespace Tag {
  export interface ReqTagParams {
    Name?: string;
    Page?: number;
    PageSize?: number;
    SortField?: string;
    SortDirection?: string;
  }
  export interface ResTagList {
    id: string;
    tagName: string;
    color: string;
    createdAt: string;
    updatedAt: string;
  }
  export interface ReqTagForm {
    tagName: string;
    color?: string;
  }
  export interface ResTagPagination {
    list: ResTagList[];
    totalCount: number;
    totalPages: number;
    currentPage: number;
    pageSize: number;
  }
}

// 房间管理模块
export namespace Room {
  export interface ReqRoomParams {
    RoomNumber?: string;
    Status?: string;
    IsEnabled?: boolean;
    Page?: number;
    PageSize?: number;
    SortField?: string;
    SortDirection?: string;
  }
  export interface ResRoomList {
    id: string;
    roomNumber: string;
    shopId: string;
    shopName?: string;
    unitPrice: number;
    images?: string[];
    tags?: string[];
    plans?: string[];
    status: string;
    isEnabled: boolean;
    createdAt: string;
    updatedAt: string;
  }
  export interface ReqRoomForm {
    roomNumber: string;
    shopId: string;
    unitPrice: number;
    images?: string[];
    tags?: string[];
    isEnabled?: boolean;
  }
  export interface ResRoomPagination {
    list: ResRoomList[];
    totalCount: number;
    totalPages: number;
    currentPage: number;
    pageSize: number;
  }
}

// 门店管理模块
export namespace Shop {
  export interface ReqShopParams {
    Name?: string;
    Page?: number;
    PageSize?: number;
    SortField?: string;
    SortDirection?: string;
  }
  export interface ResShopList {
    id: string;
    name: string;
    images?: string[];
    tags?: string[];
    address: string;
    phone: string;
    createdAt: string;
    updatedAt: string;
  }
  export interface ReqShopForm {
    name: string;
    images?: string[];
    tags?: string[];
    address: string;
    phone: string;
  }
  export interface ResShopPagination {
    list: ResShopList[];
    totalCount: number;
    totalPages: number;
    currentPage: number;
    pageSize: number;
  }
}

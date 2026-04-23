import { User } from "@/api/interface/index";
import http from "@/api";

/**
 * @description 用户管理模块
 */
export const userApi = {
  // 获取用户列表
  getUserList: (params: User.ReqUserParams) => {
    return http.get<User.ResUserPagination>("/admin/users", params);
  },
  // 获取用户详情
  getUserDetail: (id: string) => {
    return http.get<User.ResUserList>(`/admin/users/${id}`);
  },
  // 新增用户
  addUser: (params: User.ReqUserForm) => {
    return http.post<User.ResUserList>("/admin/users", params);
  },
  // 更新用户
  updateUser: (id: string, params: User.ReqUserUpdate) => {
    return http.put<User.ResUserList>(`/admin/users/${id}`, params);
  },
  // 删除用户
  deleteUser: (id: string) => {
    return http.delete(`/admin/users/${id}`);
  },
};

export default userApi;

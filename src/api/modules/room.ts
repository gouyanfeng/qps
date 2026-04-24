import http from "@/api";

/**
 * @description 房间管理模块
 */
export const roomApi = {
  // 获取房间列表
  getRoomList: (params: any) => {
    return http.get<any>("/admin/rooms", params);
  },
  // 新增房间
  addRoom: (params: any) => {
    return http.post<any>("/admin/rooms", params);
  },
  // 更新房间
  updateRoom: (id: string, params: any) => {
    return http.put<any>(`/admin/rooms/${id}`, params);
  },
  // 删除房间
  deleteRoom: (id: string) => {
    return http.delete(`/admin/rooms/${id}`);
  },
};

export default roomApi;

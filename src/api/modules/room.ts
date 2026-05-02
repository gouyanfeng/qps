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
  // 获取房间详情
  getRoomById: (id: string) => {
    return http.get<any>(`/admin/rooms/${id}`);
  },
  // 更新房间
  updateRoom: (id: string, params: any) => {
    return http.put<any>(`/admin/rooms/${id}`, params);
  },
  // 删除房间
  deleteRoom: (id: string) => {
    return http.delete(`/admin/rooms/${id}`);
  },
  // 电源控制
  togglePower: (roomId: string, powerOn: boolean) => {
    return http.post<any>("/admin/rooms/toggle-power", { roomId, powerOn });
  },

  // ========== 房间图片接口 RoomImage ==========
  // 获取房间图片列表
  getRoomImages: (roomId: string) => {
    return http.get<any>("/admin/room-images", { roomId });
  },
  // 添加房间图片
  addRoomImage: (params: {
    roomId: string;
    imageUrl: string;
    isMain?: boolean;
    sortOrder?: number;
  }) => {
    return http.post<any>("/admin/room-images", params);
  },
  // 删除房间图片
  deleteRoomImage: (imageId: string) => {
    return http.delete(`/admin/room-images/${imageId}`);
  },

  // ========== 房间标签接口 RoomTag ==========
  // 获取房间标签列表
  getRoomTags: (roomId: string) => {
    return http.get<any>(`/admin/room-tags`, { roomId });
  },
  // 添加房间标签
  addRoomTag: (params: { roomId: string; tagId: string }) => {
    return http.post<any>("/admin/room-tags", params);
  },
  // 删除房间标签
  deleteRoomTag: (id: string) => {
    return http.delete(`/admin/room-tags/${id}`);
  },

  // ========== 房间套餐接口 RoomPlan ==========
  // 获取房间套餐列表
  getRoomPlans: (roomId: string) => {
    return http.get<any>(`/admin/room-plans`, { roomId });
  },
  // 添加房间套餐
  addRoomPlan: (params: { roomId: string; planId: string }) => {
    return http.post<any>("/admin/room-plans", params);
  },
  // 删除房间套餐
  deleteRoomPlan: (id: string) => {
    return http.delete(`/admin/room-plans/${id}`);
  },
};

export default roomApi;

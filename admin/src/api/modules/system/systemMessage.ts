import http from "@/api";

/** 枚举值与后端 SystemMessageType / MyMessageBox 对齐 */
export const systemMessageApi = {
  // 管理端
  getList: (params: any) => {
    return http.get<any>("/admin/system-messages", params);
  },
  get: (id: string) => {
    return http.get<any>(`/admin/system-messages/${id}`);
  },
  add: (params: any) => {
    return http.post<any>("/admin/system-messages", params);
  },
  update: (id: string, params: any) => {
    return http.put<any>(`/admin/system-messages/${id}`, params);
  },
  remove: (id: string) => {
    return http.delete(`/admin/system-messages/${id}`);
  },
  changePublishStatus: (id: string, isPublished: boolean) => {
    return http.put<any>(`/admin/system-messages/${id}/publish-status`, null, {
      params: { isPublished }
    });
  },

  // 当前用户（铃铛 / 通知中心）
  getMyMessages: (params: { box: "Notices" | "Inbox"; page?: number; pageSize?: number; unreadOnly?: boolean }) => {
    return http.get<any>("/admin/system-messages/my", params);
  },
  getUnreadCount: () => {
    return http.get<any>("/admin/system-messages/my/unread-count");
  },
  markRead: (id: string) => {
    return http.put<any>(`/admin/system-messages/my/${id}/read`);
  },
  markAllRead: (box: "Notices" | "Inbox" = "Notices") => {
    return http.put<any>("/admin/system-messages/my/read-all", null, { params: { box } });
  }
};

export default systemMessageApi;
